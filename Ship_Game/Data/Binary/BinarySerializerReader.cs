using System;
using System.IO;
using System.Linq;
using System.Reflection;
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
                AddTypeInfo(typeId, s.Type.Name, s, null, s.IsPointerType, SerializerCategory.None);
            }
        }

        void AddTypeInfo(uint streamTypeId, string name, TypeSerializer s, FieldInfo[] fields, bool isPointer, SerializerCategory c)
        {
            var info = new TypeInfo(streamTypeId, name, s, fields, isPointer, c);
            StreamTypes[streamTypeId] = info;

            if (s.TypeId >= ActualTypes.Length)
                Array.Resize(ref ActualTypes, s.TypeId + 1);
            ActualTypes[s.TypeId] = info;
        }

        void AddDeletedTypeInfo(uint streamTypeId, string name, FieldInfo[] fields, bool isPointer, SerializerCategory c)
        {
            var info = new TypeInfo(streamTypeId, name, null, fields, isPointer, c);
            StreamTypes[streamTypeId] = info;
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
                bool isPointerType = (flags & 0b0000_0001) != 0;

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

                string name = typeNames[nameId];
                Type type = GetTypeFrom(assemblies[asmId], namespaces[nsId], typeNames[nameId]);
                if (type != null)
                {
                    if (!TypeMap.TryGet(type, out TypeSerializer s))
                        s = TypeMap.AddUserTypeSerializer(type);
                    AddTypeInfo(typeId, name, s, fields, isPointerType, SerializerCategory.UserClass);
                }
                else
                {
                    AddDeletedTypeInfo(typeId, name, fields, isPointerType, SerializerCategory.UserClass);
                }
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
                    SerializerCategory c = cTypeId == 1 ? SerializerCategory.RawArray : SerializerCategory.Collection;
                    AddTypeInfo(streamTypeId, cType.GetTypeName(), cTypeSer, null, isPointer:true, c);
                }
            }
        }

        Map<Assembly, Map<string, Type>> TypeNameCache;

        Type GetTypeFrom(string assemblyName, string nameSpace, string typeName)
        {
            string fullName = $"{nameSpace}+{typeName},{assemblyName}";
            Type type = Type.GetType(fullName, throwOnError: false);
            if (type != null)
                return type; // perfect match

            // type has been moved, deleted or renamed
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Assembly a = assemblies.Find(asm => asm.GetName().Name == assemblyName);
            if (a == null) // not in loaded assembly? then give up. we don't want to load new assemblies
                return null;

            if (TypeNameCache == null)
                TypeNameCache = new Map<Assembly, Map<string, Type>>();

            if (!TypeNameCache.TryGetValue(a, out Map<string, Type> typeNamesMap))
            {
                TypeNameCache.Add(a, (typeNamesMap = new Map<string, Type>()));

                // find type by name from all module types (including nested types)
                Module module = a.Modules.First();
                Type[] moduleTypes = module.GetTypes();
                foreach (Type t in moduleTypes)
                {
                    var attr = t.GetCustomAttribute<StarDataTypeAttribute>();
                    if (attr != null)
                        typeNamesMap.Add(attr.TypeName ?? t.Name, t);
                }
            }

            typeNamesMap.TryGetValue(typeName, out type);
            return type;
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
                if (ser == null)
                    return; // type not found (it was deleted or renamed)
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
                    cs.Deserialize(this, instance);
                }
            });

            // read UserClass fields
            ForEachTypeGroup(SerializerCategory.UserClass, (type, ser, count, baseIndex) =>
            {
                for (int i = 0; i < count; ++i)
                {
                    object instance = ObjectsList[baseIndex + i];
                    ReadUserClass(type, instance);
                }
            });
        }

        void ForEachTypeGroup(SerializerCategory category, Action<TypeInfo, TypeSerializer, int, int> action)
        {
            int objectIdx = 0;
            foreach ((TypeInfo type, TypeSerializer ser, int count) in TypeGroups)
            {
                if (type.Category == category)
                    action(type, ser, count, objectIdx);
                objectIdx += count;
            }
        }

        void ReadUserClass(TypeInfo instanceType, object instance)
        {
            // raise alarm on null pointers
            if (instance == null && instanceType.IsPointerType)
            {
                Log.Error($"NullReference {instanceType.Name} - this is a bug in binary reader");
                return;
            }

            for (uint i = 0; i < instanceType.Fields.Length; ++i)
            {
                // [field type ID]
                // [field index]     (in type metadata)
                uint streamFieldTypeId = BR.ReadVLu32();
                uint streamFieldIdx = BR.ReadVLu32();

                TypeInfo fieldType = GetType(streamFieldTypeId);
                object fieldValue = ReadElement(fieldType, fieldType.Ser);

                if (instance != null)
                {
                    // if field has been deleted, then mapping is null and Set() will not called
                    FieldInfo field = instanceType.Fields[streamFieldIdx];
                    field.Field?.Set(instance, fieldValue);
                }
            }
        }

        // Reads an inline element from the stream
        // For pointer types, it reads the pointer value and fetches the right instance
        // For primitive value types, it reads the inline value
        // For UserClass value types, it reads the inline struct fields
        public object ReadElement(TypeInfo elementType, TypeSerializer ser)
        {
            if (elementType.IsPointerType)
            {
                uint pointer = BR.ReadVLu32();
                if (pointer == 0)
                    return null;
                return ObjectsList[pointer - 1]; // pointer = objectIndex + 1
            }

            if (elementType.Category == SerializerCategory.UserClass)
            {
                // if ser == null, then Type has been deleted, skip with instance=null
                object inlineStruct = ser != null ? Activator.CreateInstance(ser.Type) : null;
                ReadUserClass(elementType, inlineStruct);
                return inlineStruct;
            }
            else // int, float, object[], Vector2[], etc
            {
                return ser.Deserialize(this);
            }
        }
    }
}
