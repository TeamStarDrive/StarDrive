using System;
using System.Collections.Generic;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Serialization.Types;

namespace Ship_Game.Data.Binary
{
    public class BinarySerializerWriter
    {
        public Writer BW;

        // total number of objects
        public int NumObjects;
 
        // index of the root object which is being serialized/deserialized
        public uint RootObjectIndex;

        // which types were used, includes: classes, Enums and Structs
        public TypeSerializer[] UsedTypes;

        // different generic collection types such as: T[], Array<T>, Map<K,V>
        public CollectionSerializer[] CollectionTypes;

        // Object lists grouped by their type
        // includes UserClasses, Array<T>, Map<K,V>, strings
        SerializationTypeGroup[] TypeGroups;

        // how many TypeGroups are going to be written to the binary stream?
        public int NumUsedGroups => TypeGroups.Count(g => g.GroupedObjects.NotEmpty);

        public bool Verbose;

        public BinarySerializerWriter(Writer writer)
        {
            BW = writer;
        }

        public void ScanObjects(BinarySerializer rootSer, object rootObject)
        {
            var rs = new RecursiveScanner(rootSer, rootObject);

            NumObjects = rs.NumObjects;
            TypeGroups = rs.TypeGroups;
            UsedTypes = rs.UsedTypes;
            CollectionTypes = rs.CollectionTypes;

            // find the root object index from the neatly sorted type groups
            RootObjectIndex = (uint)rs.RootObjectId;
        }

        string[] MapSingle(Func<TypeSerializer, string> selector)
        {
            var names = new HashSet<string>();
            foreach (TypeSerializer s in UsedTypes)
                names.Add(selector(s));
            return names.ToArr();
        }

        string[] MapFields()
        {
            var names = new HashSet<string>();
            foreach (TypeSerializer s in UsedTypes)
                if (s is UserTypeSerializer us)
                    foreach (DataField field in us.Fields)
                        names.Add(field.Name);
            return names.ToArr();
        }

        static Map<string, int> CreateIndexMap(string[] names)
        {
            Array.Sort(names);
            var indexMap = new Map<string, int>();
            for (int i = 0; i < names.Length; ++i)
                indexMap.Add(names[i], i);
            return indexMap;
        }

        (string[] Names, Map<string, int> Index) GetMappedNames(Func<TypeSerializer, string> selector)
        {
            string[] names = MapSingle(selector);
            return (names, CreateIndexMap(names));
        }

        (string[] Names, Map<string, int> Index) GetMappedFields()
        {
            string[] names = MapFields();
            return (names, CreateIndexMap(names));
        }

        static string GetAssembly(TypeSerializer s) => s.Type.Assembly.GetName().Name;
        static string GetTypeName(TypeSerializer s) => s.TypeName;
        static string GetNamespace(TypeSerializer s)
        {
            // for nested types nameSpace is the full name of the containing type
            return s.Type.IsNested ? s.Type.DeclaringType!.FullName : s.Type.Namespace;
        }

        void WriteArray(string[] strings)
        {
            BW.WriteVLu32((uint)strings.Length);
            foreach (string s in strings)
                BW.Write(s);
        }

        public void WriteTypesList()
        {
            // [assemblies]
            // [namespaces]
            // [typenames]
            // [fieldnames]
            // [used types]
            // [collection types]
            (string[] assemblies, var assemblyMap)  = GetMappedNames(GetAssembly);
            (string[] namespaces, var namespaceMap) = GetMappedNames(GetNamespace);
            (string[] typeNames,  var typenameMap)  = GetMappedNames(GetTypeName);
            (string[] fieldNames, var fieldNameMap) = GetMappedFields();
            WriteArray(assemblies);
            WriteArray(namespaces);
            WriteArray(typeNames);
            WriteArray(fieldNames);

            foreach (TypeSerializer s in UsedTypes)
            {
                if (Verbose) Log.Info($"WriteType={s.TypeId} {s.NiceTypeName}");
                // [type ID]
                // [assembly ID]
                // [namespace ID]
                // [typename ID]
                // [type flags]
                // [fields info]
                int assemblyId  = assemblyMap[GetAssembly(s)];
                int namespaceId = namespaceMap[GetNamespace(s)];
                int typenameId  = typenameMap[GetTypeName(s)];
                int flags = 0;
                if (s.IsPointerType) flags |= 0b0000_0001; // pointer, not valuetype
                if (s.IsEnumType)    flags |= 0b0000_0010; // enum
                if (s.Type.IsNested) flags |= 0b0000_0100; // requires nested namespace resolution
                BW.WriteVLu32((uint)s.TypeId);
                BW.WriteVLu32((uint)assemblyId);
                BW.WriteVLu32((uint)namespaceId);
                BW.WriteVLu32((uint)typenameId);
                BW.WriteVLu32((uint)flags);

                if (s is UserTypeSerializer us)
                {
                    BW.WriteVLu32((uint)us.Fields.Length);
                    foreach (DataField field in us.Fields)
                    {
                        // [field type ID]
                        // [field name index]
                        BW.WriteVLu32((uint)field.Serializer.TypeId);
                        BW.WriteVLu32((uint)fieldNameMap[field.Name]);
                    }
                }
                else
                {
                    BW.WriteVLu32(0); // count = 0
                }
            }

            foreach (CollectionSerializer s in CollectionTypes)
            {
                if (Verbose) Log.Info($"WriteCollection={s.TypeId} {s.NiceTypeName}");
                // [type ID]
                // [collection type]   1:T[] 2:Array<T> 3:Map<K,V> 4:HashSet<T>
                // [element type ID]
                // [key type ID] (only for Map<K,V>)
                uint type = 0;
                if      (s is RawArraySerializer)  type = 1;
                else if (s is ArrayListSerializer) type = 2;
                else if (s is MapSerializer)       type = 3;
                else if (s is HashSetSerializer)   type = 4;
                BW.WriteVLu32((uint)s.TypeId);
                BW.WriteVLu32(type);
                BW.WriteVLu32((uint)s.ElemSerializer.TypeId);
                if (s is MapSerializer ms)
                    BW.WriteVLu32((uint)ms.KeySerializer.TypeId);
            }
        }

        public void WriteObjects()
        {
            foreach (SerializationTypeGroup tg in TypeGroups)
            {
                if (tg.GroupedObjects.Count == 0)
                    continue;
                if (Verbose) Log.Info($"WriteGroup={tg.Type.TypeId} {tg.Type.NiceTypeName} count={tg.GroupedObjects.Count}");
                BW.WriteVLu32((uint)tg.Type.TypeId);
                BW.WriteVLu32((uint)tg.GroupedObjects.Count); // int32 because we allow > 65k objects
            }

            if (Verbose) Log.Info($"WriteObjects {NumObjects}");

            foreach (SerializationTypeGroup tg in TypeGroups)
            {
                if (tg.GroupedObjects.Count == 0)
                    continue;
                if (Verbose) Log.Info($"WriteGroupedObjects Count={tg.GroupedObjects.Count} {tg.Type}");

                // for error checking we add the correct typeId
                // skipping over type groups is currently not possible,
                // because of WriteVLu32 giving variable length data
                BW.WriteVLu32((uint)tg.Type.TypeId);
                BW.WriteVLu32((uint)tg.GroupedObjects.Count);

                foreach (ObjectState state in tg.GroupedObjects)
                {
                    state.Serialize(this, tg.Type);
                }
            }
        }
    }
}
