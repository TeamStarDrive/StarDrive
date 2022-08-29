using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Data.Binary
{
    public class BinarySerializerReader
    {
        public readonly Reader BR;
        public readonly TypeSerializerMap TypeMap;
        readonly BinarySerializerHeader Header;

        // Object counts grouped by their type (includes strings)
        record struct TypeGroup(TypeInfo Type, TypeSerializer Ser, int Count);
        TypeGroup[] TypeGroups;
        TypeInfo[] StreamTypes;
        TypeInfo[] ActualTypes;

        // flat list of deserialized objects
        public object[] ObjectsList;

        public bool Verbose;

        public BinarySerializerReader(Reader reader, TypeSerializerMap typeMap, in BinarySerializerHeader header)
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
                AddTypeInfo(typeId, s.Type.Name, s, null, s.IsPointerType, SerializerCategory.Fundamental);
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

        TypeInfo GetTypeOrNull(uint streamTypeId)
        {
            return streamTypeId < StreamTypes.Length ? StreamTypes[streamTypeId] : null;
        }

        public TypeInfo GetType(TypeSerializer ser)
        {
            return ActualTypes[ser.TypeId];
        }

        static string[] ReadStringArray(Reader br)
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

            if (Verbose) Log.Info($"Reading {Header.NumUsedTypes} Types");
            for (uint i = 0; i < Header.NumUsedTypes; ++i)
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
                bool isEnumType    = (flags & 0b0000_0010) != 0;
                bool isNestedType  = (flags & 0b0000_0100) != 0;

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

                var c = isEnumType ? SerializerCategory.Enums : SerializerCategory.UserClass;
                string name = typeNames[nameId];
                Type type = GetTypeFrom(assemblies[asmId], namespaces[nsId], typeNames[nameId], isNestedType);
                if (type != null)
                {
                    if (!TypeMap.TryGet(type, out TypeSerializer s))
                        s = TypeMap.Get(type);

                    if (Verbose) Log.Info($"ReadType={typeId} {name} {c}");
                    AddTypeInfo(typeId, name, s, fields, isPointerType, c);
                }
                else
                {
                    if (Verbose) Log.Warning($"ReadDeletedType={typeId} {name} {c}");
                    AddDeletedTypeInfo(typeId, name, fields, isPointerType, c);
                }
            }
            
            if (Verbose) Log.Info($"Reading {Header.NumCollectionTypes} Collections");
            for (uint i = 0; i < Header.NumCollectionTypes; ++i)
            {
                // [type ID]
                // [collection type]   1:T[] 2:Array<T> 3:Map<K,V> 4:HashSet<T>
                // [element type ID]
                // [key type ID] (only for Map<K,V>)

                uint streamTypeId = BR.ReadVLu32();
                uint cTypeId = BR.ReadVLu32();
                uint valTypeId = BR.ReadVLu32();
                uint keyTypeId = cTypeId == 3 ? BR.ReadVLu32() : 0;

                TypeInfo keyTypeInfo = keyTypeId != 0 ? StreamTypes[keyTypeId] : null;
                TypeInfo valTypeInfo = StreamTypes[valTypeId];

                // if type info is null, it means there are no valid instances
                // of this type in the serialized file, so this type info can be safely ignored
                if (valTypeInfo == null) // element type does not exist anywhere
                {
                    if (Verbose) Log.Warning($"DiscardCollection={streamTypeId} valType={valTypeId} was null (deleted?)");
                    continue;
                }

                if (cTypeId == 3 && keyTypeInfo == null) // map key does not exist anywhere
                {
                    if (Verbose) Log.Warning($"DiscardCollection={streamTypeId} keyType={keyTypeId} was null (deleted?)");
                    continue;
                }

                Type cType = null;
                if (cTypeId == 1)
                    cType = valTypeInfo.Type.MakeArrayType();
                else if (cTypeId == 2)
                    cType = typeof(Array<>).MakeGenericType(valTypeInfo.Type);
                else if (cTypeId == 3)
                    cType = typeof(Map<,>).MakeGenericType(keyTypeInfo.Type, valTypeInfo.Type);
                else if (cTypeId == 4)
                    cType = typeof(HashSet<>).MakeGenericType(valTypeInfo.Type);
                else
                    Log.Error($"Unrecognized cTypeId={cTypeId} for Collection<{valTypeInfo}>");

                if (cType != null)
                {
                    TypeSerializer cTypeSer = TypeMap.Get(cType);
                    SerializerCategory c = cTypeId == 1 ? SerializerCategory.RawArray : SerializerCategory.Collection;

                    if (Verbose) Log.Info($"ReadCollection={streamTypeId} {cType.GetTypeName()} {c}");
                    AddTypeInfo(streamTypeId, cType.GetTypeName(), cTypeSer, null, isPointer:true, c);
                }
            }
        }

        Map<Assembly, Map<string, Type>> TypeNameCache;

        Type GetTypeFrom(string assemblyName, string nameSpace, string typeName, bool isNested)
        {
            // nested types are prefixed by '+', global types by '.'
            // for nested types nameSpace is the full name of the containing type
            string fullName =  $"{nameSpace}{(isNested?'+':'.')}{typeName},{assemblyName}";
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
            if (Verbose) Log.Info($"Reading {Header.NumTypeGroups} TypeGroups");
            TypeGroups = new TypeGroup[Header.NumTypeGroups];

            int totalCount = 0;
            for (int i = 0; i < TypeGroups.Length; ++i)
            {
                uint streamTypeId = BR.ReadVLu32();
                int count = (int)BR.ReadVLu32();
                totalCount += count;

                if (count == 0) // count must not be 0
                    throw new InvalidDataException($"ReadGroup Type={streamTypeId} Count was 0");

                var type = GetType(streamTypeId);
                if (type == null)
                {
                    Log.Error($"ReadGroup Type={streamTypeId} was null");
                    TypeGroups[i] = new(null, null, count);
                }
                else
                {
                    if (Verbose) Log.Info($"ReadGroup Type={streamTypeId} Count={count} {type.Ser}");
                    TypeGroups[i] = new(type, type.Ser, count);
                }
            }

            ObjectsList = new object[totalCount];
        }

        // populate all object instances by reading the object fields
        public void ReadObjectsList()
        {
            if (Verbose) Log.Info($"ReadObjectsList {ObjectsList.Length}");

            PreInstantiate();

            // read types in the order they were Serialized
            // if there is a sequencing/dependency problem, the issue must be
            // solved in the Type sorting stage during Serialization

            if (Verbose) Log.Info("Read Values and Fields");
            foreach (var g in GetTypeGroups())
            {
                ReadObjects(g.Type, g.Ser, g.Count, g.BaseIndex);
            }

            AfterDeserialization();
        }

        void PreInstantiate()
        {
            // pre-instantiate UserClass instances
            if (Verbose) Log.Info("PreInstantiate objects");
            foreach (var g in GetTypeGroups(t => t.Category is (SerializerCategory.UserClass or SerializerCategory.Collection)))
            {
                for (int i = 0; i < g.Count; ++i)
                    ObjectsList[g.BaseIndex + i] = g.Ser.CreateInstance();
            }
        }

        void AfterDeserialization()
        {
            // now all instances should be initialized, we can call events
            if (Verbose) Log.Info("Invoke UserClass events");
            foreach (var g in GetTypeGroups(t => t.Category == SerializerCategory.UserClass))
            {
                if (g.Ser is UserTypeSerializer us && us.OnDeserialized != null)
                {
                    for (int i = 0; i < g.Count; ++i)
                    {
                        object instance = ObjectsList[g.BaseIndex + i];
                        us.OnDeserialized.Invoke(instance, null);
                    }
                }
            }
        }

        record struct TypeGroupWithBaseIndex(TypeInfo Type, TypeSerializer Ser, int Count, int BaseIndex);

        IEnumerable<TypeGroupWithBaseIndex> GetTypeGroups(Func<TypeInfo, bool> condition = null)
        {
            int baseIndex = 0;
            foreach (var g in TypeGroups)
            {
                if (condition == null || (g.Type?.Ser != null && condition(g.Type)))
                {
                    yield return new(g.Type, g.Ser, g.Count, baseIndex);
                }
                baseIndex += g.Count;
            }
        }

        void ReadObjects(TypeInfo type, TypeSerializer ser, int count, int baseIndex)
        {
            if (Verbose) Log.Info($"ReadObjects Count={count} {type}");

            // we are trying to read a typegroup but the type does not match
            uint typeId = BR.ReadVLu32();
            if (typeId != type.StreamTypeId)
            {
                TypeInfo actual = GetTypeOrNull(typeId);
                throw new InvalidDataException($"Invalid TypeGroup Id={typeId} Expected={type} Encountered={actual}");
            }

            // the count does not match, there's an invalid offset in the deserializer and we're reading bad data
            uint actualCount = BR.ReadVLu32();
            if (actualCount != count)
                throw new InvalidDataException($"Invalid TypeGroup Count={actualCount} Expected={count} for Type={type}");

            if (type.Category is SerializerCategory.UserClass)
            {
                for (int i = 0; i < count; ++i)
                {
                    object instance = ObjectsList[baseIndex + i];
                    ReadUserClass(type, instance);
                }
            }
            else if (type.Category is SerializerCategory.Collection)
            {
                var cs = (CollectionSerializer)ser;
                for (int i = 0; i < count; ++i)
                {
                    object instance = ObjectsList[baseIndex + i];
                    cs.Deserialize(this, instance);
                }
            }
            // fundamental types such as int, Vector2, String, Byte[], Point. @see TypeSerializerMap.cs
            // also RawArrays like Ship[]
            else
            {
                for (int i = 0; i < count; ++i)
                {
                    ObjectsList[baseIndex + i] = ser.Deserialize(this);
                }
            }
        }

        void ReadUserClass(TypeInfo instanceType, object instance)
        {
            for (uint i = 0; i < instanceType.Fields.Length; ++i)
            {
                FieldInfo fi = instanceType.Fields[i];

                // all types are now using pointers
                // the pointer has to be read, even if Field or Type is deleted
                object fieldValue = ReadPointer();

                // if Type has been deleted, we skip and read the next field
                if (instance == null) continue;

                // if field has been deleted, then Field==null and Set() is ignored
                if (fi.Field == null) continue;

                try
                {
                    fi.Field.Set(instance, fieldValue);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Failed to set FieldValue={fieldValue} into Field={fi}");
                }
            }
        }

        // Reads a pointer from the stream and looks it up from the ObjectsList
        public object ReadPointer()
        {
            uint pointer = BR.ReadVLu32();
            if (pointer == 0)
                return null;
            return ObjectsList[pointer - 1]; // pointer = objectIndex + 1
        }
    }
}
