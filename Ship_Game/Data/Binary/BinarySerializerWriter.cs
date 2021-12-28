using System;
using System.Collections.Generic;
using System.IO;
using Ship_Game.Data.Serialization;

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
        public TypeSerializer[] UsedTypes;

        // Object lists grouped by their type (includes strings)
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

            if (ser.IsUserClass && ser is UserTypeSerializer userType)
            {
                foreach (DataField field in userType.Fields)
                {
                    object obj = field.Get(instance);
                    RecursiveScan(field.Serializer, obj, groups, objects, structs);
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

            var structs = uniqueStructs.ToArrayList();
            structs.Sort((a, b) => string.CompareOrdinal(a.Type.Name, b.Type.Name));

            var groups = objectGroups.ToArrayList();
            groups.Sort((a, b) =>
            {
                // strings must always be first, because they are a
                // pointer type which is not an UserClass
                if (a.Key.Type == typeof(string)) return -1;
                if (b.Key.Type == typeof(string)) return +1;

                // non-pointer types must come second
                bool isPointerA = a.Key.IsPointerType;
                bool isPointerB = b.Key.IsPointerType;
                if (!isPointerA && isPointerB) return -1;
                if (isPointerA && !isPointerB) return +1;

                // the rest, sort by type name
                return string.CompareOrdinal(a.Key.Type.Name, b.Key.Type.Name);
            });

            // for TypeGroups we only use Object groups
            TypeGroups = groups.Select(kv => (kv.Key, kv.Value.ToArray()));

            // for UsedTypes we take both structs and objects
            var usedTypes = new Array<TypeSerializer>();
            
            foreach (TypeSerializer ser in structs)
                usedTypes.Add(ser);

            foreach ((TypeSerializer ser, object[] list) in TypeGroups)
                if (ser.IsUserClass)
                    usedTypes.Add(ser);

            UsedTypes = usedTypes.ToArray();

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

        (string[] Names, Map<string, int> Index) GetMappedNames(Func<TypeSerializer, string> selector)
        {
            var names = new HashSet<string>();
            foreach (TypeSerializer serializer in UsedTypes)
                names.Add(selector(serializer));

            string[] sorted = names.ToArray();
            Array.Sort(sorted);

            var indexMap = new Map<string, int>();
            for (int i = 0; i < sorted.Length; ++i)
                indexMap.Add(sorted[i], i);

            return (sorted, indexMap);
        }

        static string GetAssembly(TypeSerializer s) => s.Type.Assembly.GetName().Name;
        static string GetNamespace(TypeSerializer s) => s.Type.FullName?.Split('+')[0];

        public void WriteTypesList(bool useStableMapping)
        {
            // [assemblies]
            // [namespaces]
            // [types]
            (string[] assemblies, var assemblyMap) = GetMappedNames(GetAssembly);
            BW.WriteVLu32((uint)assemblies.Length);
            foreach (string assembly in assemblies)
                BW.Write(assembly);

            (string[] namespaces, var namespaceMap) = GetMappedNames(GetNamespace);
            BW.WriteVLu32((uint)namespaces.Length);
            foreach (string ns in namespaces)
                BW.Write(ns);

            foreach (TypeSerializer serializer in UsedTypes)
            {
                // [type ID]
                // [assembly ID]
                // [namespace ID]
                // [type flags]
                // [type name]
                int assemblyId = assemblyMap[GetAssembly(serializer)];
                int namespaceId = namespaceMap[GetNamespace(serializer)];
                int flags = (serializer.IsPointerType ? 1 : 0);
                string typeName = serializer.Type.Name;
                BW.WriteVLu32(serializer.Id);
                BW.WriteVLu32((uint)assemblyId);
                BW.WriteVLu32((uint)namespaceId);
                BW.WriteVLu32((uint)flags);
                BW.Write(typeName);

                if (useStableMapping && serializer is UserTypeSerializer userSer)
                {
                    BW.WriteVLu32((uint)userSer.Fields.Count);
                    foreach (DataField field in userSer.Fields)
                        BW.Write(field.Name);
                }
            }
        }

        public void WriteObjectTypeGroups()
        {
            foreach ((TypeSerializer ser, object[] list) in TypeGroups)
            {
                BW.WriteVLu32(ser.Id);
                BW.WriteVLu32((uint)list.Length); // int32 because we allow > 65k objects
            }
        }

        public void WriteObjects()
        {
            var objects = new Array<object>(NumObjects);
            Pointers = new Map<object, uint>(NumObjects);

            // pre-pass: create integer pointers of all objects
            foreach ((TypeSerializer ser, object[] list) in TypeGroups)
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
                BW.WriteVLu32((ushort)userSer.Fields.Count);

                // @note This is not recursive, because we only write object "Pointers" ID-s
                foreach (DataField field in userSer.Fields)
                {
                    // always include the type, this allows us to handle
                    // fields which get deleted, so they can be skipped
                    TypeSerializer fieldSer = field.Serializer;
                    BW.WriteVLu32(fieldSer.Id);

                    // write the field IDX to Stream so we can remap it
                    // to actual FieldIdx during deserialize
                    BW.WriteVLu32((ushort)field.FieldIdx);

                    object fieldObject = field.Get(instance);
                    WriteElement(fieldSer, fieldObject);
                }
            }
            else // int, float, object[], Vector2[], etc
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
            else if (Pointers.TryGetValue(element, out uint pointer))
            {
                BW.WriteVLu32(pointer); // write the object pointer
            }
            else if (ser.IsUserClass)
            {
                // an UserClass struct which has to be serialized inline
                WriteObjectRoot(ser, element);
            }
            else // it's a float, int, Vector2, etc. dump it directly
            {
                ser.Serialize(this, element);
            }
        }
    }
}
