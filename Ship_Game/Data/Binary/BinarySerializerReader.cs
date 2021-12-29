using System;
using System.IO;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Serialization.Types;

namespace Ship_Game.Data.Binary
{
    public class BinarySerializerReader
    {
        public readonly BinaryReader BR;
        public readonly TypeSerializerMap TypeMap;
        readonly BinarySerializerHeader Header;

        // Object counts grouped by their type (includes strings)
        (TypeInfo Type, TypeSerializer Ser, int Count)[] TypeGroups;
        TypeInfo[] StreamTypes;
        TypeInfo[] ActualTypes;

        // flat list of deserialized objects
        public object[] ObjectsList;

        public BinarySerializerReader(BinaryReader reader, TypeSerializerMap typeMap, in BinarySerializerHeader header)
        {
            BR = reader;
            TypeMap = typeMap;
            Header = header;
            StreamTypes = new TypeInfo[header.MaxTypeId + 1];
            ActualTypes = new TypeInfo[Math.Max(StreamTypes.Length, typeMap.MaxTypeId + 1)];
            SetFundamentalTypes(typeMap);
        }

        void SetFundamentalTypes(TypeSerializerMap typeMap)
        {
            for (uint typeId = 1; typeId < TypeSerializer.MaxFundamentalTypes; ++typeId)
            {
                if (!typeMap.TryGet(typeId, out TypeSerializer s))
                    break;
                AddTypeInfo(typeId, s, null);
            }
        }

        void AddTypeInfo(uint streamTypeId, TypeSerializer s, FieldInfo[] fields)
        {
            var info = new TypeInfo((ushort)streamTypeId, s, fields);
            StreamTypes[streamTypeId] = info;

            if (s.TypeId >= ActualTypes.Length)
                Array.Resize(ref ActualTypes, s.TypeId + 1);
            ActualTypes[s.TypeId] = info;
        }

        TypeInfo GetType(uint streamTypeId)
        {
            return StreamTypes[streamTypeId];
        }

        public TypeInfo GetType(TypeSerializer ser)
        {
            return ActualTypes[ser.TypeId];
        }

        static string[] ReadStringArray(BinaryReader br)
        {
            string[] items = new string[br.ReadVLu32()];
            for (int i = 0; i < items.Length; ++i)
                items[i] = br.ReadString();
            return items;
        }

        public void ReadTypesList()
        {
            // [assemblies]
            // [namespaces]
            // [typenames]
            // [fieldnames]
            // [types]
            string[] assemblies = ReadStringArray(BR);
            string[] namespaces = ReadStringArray(BR);
            string[] typeNames  = ReadStringArray(BR);
            string[] fieldNames = ReadStringArray(BR);

            for (uint i = 0; i < Header.NumUserTypes; ++i)
            {
                // [type ID]
                // [assembly ID]
                // [namespace ID]
                // [typename ID]
                // [type flags]
                // [fields info]
                uint typeId = BR.ReadVLu32();
                uint asmId  = BR.ReadVLu32();
                uint nsId   = BR.ReadVLu32();
                uint nameId = BR.ReadVLu32();
                uint flags  = BR.ReadVLu32();
                uint numFields = BR.ReadVLu32();

                var fields = new FieldInfo[numFields];

                for (uint fieldIdx = 0; fieldIdx < fields.Length; ++fieldIdx)
                {
                    // [field type ID]
                    // [field name index]
                    uint fieldTypeId = BR.ReadVLu32();
                    uint nameIdx = BR.ReadVLu32();
                    fields[fieldIdx] = new FieldInfo
                    {
                        StreamTypeId = (ushort)fieldTypeId,
                        Name = nameIdx < fieldNames.Length ? fieldNames[nameIdx] : null,
                    };
                }

                string fullName = $"{namespaces[nsId]}+{typeNames[nameId]},{assemblies[asmId]}";
                bool isPointerType = (flags & 0b0000_0001) != 0;

                // TODO: we should mark this type as invalid if fails and use `null` during deserialize
                Type type = Type.GetType(fullName, throwOnError: true);
                if (!TypeMap.TryGet(type, out TypeSerializer s))
                    s = TypeMap.AddUserTypeSerializer(type);

                AddTypeInfo(typeId, s, fields);
            }

            for (uint i = 0; i < Header.NumCollectionTypes; ++i)
            {
                // [type ID]
                // [collection type]   1:T[] 2:Array<T> 3:Map<K,V>
                // [element type ID]
                // [key type ID] (only for Map<K,V>)

                uint streamTypeId = BR.ReadVLu32();
                uint cTypeId = BR.ReadVLu32();
                uint valTypeId = BR.ReadVLu32();
                uint keyTypeId = cTypeId == 3 ? BR.ReadVLu32() : 0;

                TypeSerializer keyType = keyTypeId != 0 ? StreamTypes[keyTypeId].Ser : null;
                TypeSerializer valType = StreamTypes[valTypeId].Ser;

                Type cType = null;
                if (cTypeId == 1)
                    cType = valType.Type.MakeArrayType();
                else if (cTypeId == 2)
                    cType = typeof(Array<>).MakeGenericType(valType.Type);
                else if (cTypeId == 3)
                    cType = typeof(Map<,>).MakeGenericType(keyType.Type, valType.Type);

                if (cType != null)
                {
                    TypeSerializer cTypeSer = TypeMap.Get(cType);
                    AddTypeInfo(streamTypeId, cTypeSer, null);
                }
            }
        }

        // reads the type groups
        public void ReadTypeGroups()
        {
            TypeGroups = new (TypeInfo Info, TypeSerializer Ser, int Count)[Header.NumTypeGroups];

            int totalCount = 0;
            for (int i = 0; i < TypeGroups.Length; ++i)
            {
                uint streamTypeId = BR.ReadVLu32();
                int count = (int)BR.ReadVLu32();
                totalCount += count;
                var type = GetType(streamTypeId);
                TypeGroups[i] = (type, type.Ser, count);
            }

            ObjectsList = new object[totalCount];
        }

        // populate all object instances by reading the object fields
        public void ReadObjectsList()
        {
            // pre-instantiate UserClass instances
            ForEachTypeGroup(SerializerCategory.UserClass, (type, ser, count, baseIndex) =>
            {
                for (int i = 0; i < count; ++i)
                    ObjectsList[baseIndex + i] = Activator.CreateInstance(ser.Type);
            });

            // read strings
            ForEachTypeGroup(SerializerCategory.None, (type, ser, count, baseIndex) =>
            {
                for (int i = 0; i < count; ++i)
                    ObjectsList[baseIndex + i] = ser.Deserialize(this);
            });

            // create collection instances, but don't read them yet
            // also, skip raw arrays, because we can't create them without Deserializing them
            ForEachTypeGroup(SerializerCategory.Collection, (type, ser, count, baseIndex) =>
            {
                var cs = (CollectionSerializer)ser;
                for (int i = 0; i < count; ++i)
                    ObjectsList[baseIndex + i] = cs.CreateInstance();
            });

            // now deserialize raw arrays
            ForEachTypeGroup(SerializerCategory.RawArray, (type, ser, count, baseIndex) =>
            {
                for (int i = 0; i < count; ++i)
                    ObjectsList[baseIndex + i] = ser.Deserialize(this);
            });

            // --- now all instances should be created ---

            // read Collections
            ForEachTypeGroup(SerializerCategory.Collection, (type, ser, count, baseIndex) =>
            {
                var cs = (CollectionSerializer)ser;
                for (int i = 0; i < count; ++i)
                {
                    object instance = ObjectsList[baseIndex + i];
                    ReadCollection(cs, instance);
                }
            });

            // read UserClass fields
            ForEachTypeGroup(SerializerCategory.UserClass, (type, ser, count, baseIndex) =>
            {
                var us = (UserTypeSerializer)ser;
                for (int i = 0; i < count; ++i)
                {
                    object instance = ObjectsList[baseIndex + i];
                    ReadUserClass(type, us, instance);
                }
            });
        }

        void ForEachTypeGroup(SerializerCategory category, Action<TypeInfo, TypeSerializer, int, int> action)
        {
            int objectIdx = 0;
            foreach ((TypeInfo type, TypeSerializer ser, int count) in TypeGroups)
            {
                if (ser.Category == category)
                    action(type, ser, count, objectIdx);
                objectIdx += count;
            }
        }

        void ReadUserClass(TypeInfo instanceType, UserTypeSerializer ser, object instance)
        {
            if (instance == null)
            {
                Log.Error($"Failed to deserialize {ser}");
                return;
            }

            uint numFields = BR.ReadVLu32();
            for (uint i = 0; i < numFields; ++i)
            {
                // [field type ID]
                // [field index]     (in type metadata)
                uint streamFieldTypeId = BR.ReadVLu32();
                uint streamFieldIdx = BR.ReadVLu32();

                TypeInfo fieldType = GetType(streamFieldTypeId);
                TypeSerializer fieldSer = fieldType.Ser;
                if (fieldSer != null)
                {
                    object fieldValue = ReadElement(fieldType, fieldSer);
                    FieldInfo field = instanceType.Fields[streamFieldIdx];
                    field.Field?.Set(instance, fieldValue);
                }
                else
                {
                    // it's an unknown user class, try skipping 1 pointer
                    // it will probably crash here with invalid stream error
                    BR.ReadVLu32();
                }
            }
        }

        void ReadCollection(CollectionSerializer ser, object instance)
        {
            ser.Deserialize(this, instance);
        }

        // Reads an inline element from the stream
        // For pointer types, it reads the pointer value and fetches the right instance
        // For primitive value types, it reads the inline value
        // For UserClass value types, it reads the inline struct fields
        public object ReadElement(TypeInfo elementType, TypeSerializer ser)
        {
            if (ser.IsPointerType)
            {
                uint pointer = BR.ReadVLu32();
                if (pointer == 0)
                    return null;
                return ObjectsList[pointer - 1]; // pointer = objectIndex + 1
            }

            if (ser.IsUserClass && ser is UserTypeSerializer fieldUserSer)
            {
                // a custom struct
                object inlineStruct = Activator.CreateInstance(fieldUserSer.Type);
                ReadUserClass(elementType, fieldUserSer, inlineStruct);
                return inlineStruct;
            }
            else // int, float, object[], Vector2[], etc
            {
                return ser.Deserialize(this);
            }
        }
    }
}
