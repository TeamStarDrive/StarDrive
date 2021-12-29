using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Serialization.Types;

namespace Ship_Game.Data.Binary
{
    public class BinarySerializerWriter
    {
        public BinaryWriter BW;

        // total number of objects
        public int NumObjects;
 
        // index of the root object which is being serialized/deserialized
        public uint RootObjectIndex;

        // which UserClass types were used (excludes strings or other fundamental types)
        public UserTypeSerializer[] UserTypes;

        // different generic collection types such as: T[], Array<T>, Map<K,V>
        public CollectionSerializer[] CollectionTypes;

        // Object lists grouped by their type
        // includes UserClasses, Array<T>, Map<K,V>, strings
        public (TypeSerializer Ser, object[] Objects)[] TypeGroups;

        Map<object, uint> Pointers;

        public BinarySerializerWriter(BinaryWriter writer)
        {
            BW = writer;
        }

        // Recursively gathers all UserType instances,
        // the order here is unimportant because they get sorted later
        void RecursiveScan(TypeSerializer ser, object instance, 
                           Map<TypeSerializer, Array<object>> groups,
                           HashSet<object> objects,
                           HashSet<TypeSerializer> structs)
        {
            if (instance == null)
                return; // don't map nulls

            if (ser.IsPointerType)
            {
                if (!objects.Add(instance))
                    return; // this object instance already mapped

                if (!groups.TryGetValue(ser, out Array<object> list))
                    groups.Add(ser, (list = new Array<object>()));

                list.Add(instance);
            }
            else
            {
                // only record the struct type, instance is not needed
                if (ser.IsUserClass)
                    structs.Add(ser);
            }

            // recurse into objects and collections to find more objects
            if (ser.IsUserClass && ser is UserTypeSerializer userType)
            {
                foreach (DataField field in userType.Fields)
                {
                    object obj = field.Get(instance);
                    RecursiveScan(field.Serializer, obj, groups, objects, structs);
                }
            }
            else if (ser.IsCollection && ser is CollectionSerializer collectionType)
            {
                if (collectionType.IsMapType && collectionType is MapSerializer mapType)
                {
                    var e = ((IDictionary)instance).GetEnumerator();
                    while (e.MoveNext())
                    {
                        RecursiveScan(mapType.KeySerializer, e.Key, groups, objects, structs);
                        RecursiveScan(mapType.ElemSerializer, e.Value, groups, objects, structs);
                    }
                }
                else
                {
                    int count = collectionType.Count(instance);
                    for (int i = 0; i < count; ++i)
                    {
                        object obj = collectionType.GetElementAt(instance, i);
                        RecursiveScan(collectionType.ElemSerializer, obj, groups, objects, structs);
                    }
                }
            }
        }

        public void ScanObjects(TypeSerializer rootSer, object rootObject)
        {
            var objectGroups = new Map<TypeSerializer, Array<object>>();
            var uniqueObjects = new HashSet<object>();
            var uniqueStructs = new HashSet<TypeSerializer>();
            RecursiveScan(rootSer, rootObject, objectGroups, uniqueObjects, uniqueStructs);

            NumObjects = uniqueObjects.Count;

            // make the types somewhat stable by sorting them by name
            // new/deleted types will of course offset this list immediately
            // and deleted types can't be reconstructed during Reading

            // types ordering [incredibly important]:
            // - structs
            // - strings
            // - raw arrays
            // - collections
            // - user classes
            var structs = uniqueStructs.ToArrayList();
            var groups = objectGroups.ToArrayList();
            structs.Sort((a, b) => string.CompareOrdinal(a.Type.Name, b.Type.Name));
            groups.Sort((a, b) =>
            {
                if (a.Key.Type == typeof(string)) return -1;
                if (b.Key.Type == typeof(string)) return +1;

                bool isPointerA = a.Key.IsPointerType;
                bool isPointerB = b.Key.IsPointerType;
                if (!isPointerA && isPointerB) return -1;
                if (isPointerA && !isPointerB) return +1;

                if (a.Key.Type.IsArray && !b.Key.Type.IsArray) return -1;
                if (!a.Key.Type.IsArray && b.Key.Type.IsArray) return +1;

                if (a.Key.IsCollection && !b.Key.IsCollection) return -1;
                if (!a.Key.IsCollection && b.Key.IsCollection) return +1;

                // the rest, sort by type name
                return string.CompareOrdinal(a.Key.Type.Name, b.Key.Type.Name);
            });

            // for TypeGroups we only use Object groups
            TypeGroups = groups.Select(kv => (kv.Key, kv.Value.ToArray()));

            // for UsedTypes we take both structs and objects
            var userTypes = TypeGroups.FilterSelect(kv => kv.Ser.IsUserClass, kv => (UserTypeSerializer)kv.Ser);
            UserTypes = structs.Cast<UserTypeSerializer>().Concat(userTypes).ToArray();
            CollectionTypes = TypeGroups.FilterSelect(kv => kv.Ser.IsCollection, kv => (CollectionSerializer)kv.Ser);

            // find the root object index from the neatly sorted type groups
            RootObjectIndex = IndexOfRootObject(rootObject);
        }

        uint IndexOfRootObject(object rootObject)
        {
            uint count = 0;
            foreach ((TypeSerializer _, object[] list) in TypeGroups)
            {
                for (uint i = 0; i < list.Length; ++i)
                    if (list[i] == rootObject)
                        return count + i;
                count += (uint)list.Length;
            }
            return 0;
        }

        string[] MapSingle(Func<TypeSerializer, string> selector)
        {
            var names = new HashSet<string>();
            foreach (UserTypeSerializer s in UserTypes)
                names.Add(selector(s));
            return names.ToArray();
        }

        string[] MapFields()
        {
            var names = new HashSet<string>();
            foreach (UserTypeSerializer s in UserTypes)
                foreach (DataField field in s.Fields)
                    names.Add(field.Name);
            return names.ToArray();
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
        static string GetNamespace(TypeSerializer s) => s.Type.FullName?.Split('+')[0];
        static string GetTypeName(TypeSerializer s) => s.Type.Name;

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
            // [user types]
            // [collection types]
            (string[] assemblies, var assemblyMap)  = GetMappedNames(GetAssembly);
            (string[] namespaces, var namespaceMap) = GetMappedNames(GetNamespace);
            (string[] typeNames,  var typenameMap)  = GetMappedNames(GetTypeName);
            (string[] fieldNames, var fieldNameMap) = GetMappedFields();
            WriteArray(assemblies);
            WriteArray(namespaces);
            WriteArray(typeNames);
            WriteArray(fieldNames);

            foreach (UserTypeSerializer s in UserTypes)
            {
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
                if (s.IsPointerType) flags |= 0b0000_0001;
                BW.WriteVLu32(s.TypeId);
                BW.WriteVLu32((uint)assemblyId);
                BW.WriteVLu32((uint)namespaceId);
                BW.WriteVLu32((uint)typenameId);
                BW.WriteVLu32((uint)flags);

                BW.WriteVLu32((uint)s.Fields.Count);
                foreach (DataField field in s.Fields)
                {
                    // [field type ID]
                    // [field name index]
                    BW.WriteVLu32(field.Serializer.TypeId);
                    BW.WriteVLu32((uint)fieldNameMap[field.Name]);
                }
            }

            foreach (CollectionSerializer s in CollectionTypes)
            {
                // [type ID]
                // [collection type]   1:T[] 2:Array<T> 3:Map<K,V>
                // [element type ID]
                // [key type ID] (only for Map<K,V>)
                uint type = 0;
                if      (s is RawArraySerializer)  type = 1;
                else if (s is ArrayListSerializer) type = 2;
                else if (s is MapSerializer)       type = 3;
                BW.WriteVLu32(s.TypeId);
                BW.WriteVLu32(type);
                BW.WriteVLu32(s.ElemSerializer.TypeId);
                if (s is MapSerializer ms)
                    BW.WriteVLu32(ms.KeySerializer.TypeId);
            }
        }

        public void WriteObjectTypeGroups()
        {
            foreach ((TypeSerializer ser, object[] list) in TypeGroups)
            {
                BW.WriteVLu32(ser.TypeId);
                BW.WriteVLu32((uint)list.Length); // int32 because we allow > 65k objects
            }
        }

        public void WriteObjects()
        {
            var objects = new Array<object>(NumObjects);
            Pointers = new Map<object, uint>(NumObjects);

            // pre-pass: create integer pointers of all objects
            foreach ((TypeSerializer _, object[] list) in TypeGroups)
            {
                foreach (object o in list)
                {
                    objects.Add(o);
                    Pointers[o] = (uint)objects.Count; // pointer = objectIndex + 1
                }
            }

            foreach ((TypeSerializer ser, object[] list) in TypeGroups)
            {
                foreach (object o in list)
                {
                    WriteObjectRoot(ser, o);
                }
            }
        }

        public void WriteObjectRoot(TypeSerializer ser, object instance)
        {
            // NOTE: the object typeId is already handled by TypeGroup data

            if (ser is UserTypeSerializer userSer)
            {
                // number of fields, so we know how many to parse later
                BW.WriteVLu32((uint)userSer.Fields.Count);

                // @note This is not recursive, because we only write object "Pointers" ID-s
                foreach (DataField field in userSer.Fields)
                {
                    // [field type ID]
                    // [field index]     (in type metadata)
                    TypeSerializer fieldSer = field.Serializer;
                    BW.WriteVLu32(fieldSer.TypeId);
                    BW.WriteVLu32((uint)field.FieldIdx);

                    object fieldObject = field.Get(instance);
                    WriteElement(fieldSer, fieldObject);
                }
            }
            else // string, int, float, object[], Vector2[], etc
            {
                ser.Serialize(this, instance);
            }
        }

        // Writes a single element
        // For pointer types, this writes their pointer reference
        // For primitive value types, this writes an inline value
        // For UserClass value types, this writes struct fields inline
        public void WriteElement(TypeSerializer ser, object element)
        {
            if (element == null)
            {
                BW.WriteVLu32(0); // NULL pointer
            }
            else if (ser.IsUserClass)
            {
                if (ser.IsPointerType)
                {
                    if (!Pointers.TryGetValue(element, out uint pointer))
                    {
                        // an UserClass object which was somehow missed by Pointer scan
                        Log.Error($"BinarySerializer object pointer is missing: {element}");
                    }
                    BW.WriteVLu32(pointer); // write the object pointer or NULL if not found
                }
                else
                {
                    // an UserClass struct which has to be serialized inline
                    WriteObjectRoot(ser, element);
                }
            }
            // handle strings, T[], Array<T>, Map<T> etc
            else if (Pointers.TryGetValue(element, out uint pointer))
            {
                BW.WriteVLu32(pointer); // write the object pointer
            }
            else // it's a float, int, Vector2, etc. dump it directly
            {
                ser.Serialize(this, element);
            }
        }
    }
}
