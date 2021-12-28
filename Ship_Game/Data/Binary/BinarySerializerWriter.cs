using System;
using System.Collections.Generic;
using System.IO;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Data.Binary
{
    class BinarySerializerWriter
    {
        // total number of objects
        public int NumObjects;
 
        // index of the root object which is being serialized/deserialized
        public uint RootObjectIndex;

        // which UserClass types were used (excludes strings or other fundamental types)
        public TypeSerializer[] UsedTypes;

        // Object lists grouped by their type (includes strings)
        public (TypeSerializer Ser, object[] Objects)[] TypeGroups;

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

        public void WriteTypesList(BinaryWriter bw, bool useStableMapping)
        {
            foreach (TypeSerializer serializer in UsedTypes)
            {
                string typeName = serializer.Type.FullName;
                string assemblyName = serializer.Type.Assembly.GetName().Name;
                //Type type = Type.GetType($"{typeName},{assemblyName}", throwOnError: true);
                bw.WriteVLu32(serializer.Id);
                // by outputting the full type name and assembly name, we will be able
                // to always locate the type, unless its assembly is changed
                bw.Write(typeName + "," + assemblyName);

                if (useStableMapping && serializer is UserTypeSerializer userSer)
                {
                    bw.WriteVLu32((uint)userSer.Fields.Count);
                    foreach (DataField field in userSer.Fields)
                        bw.Write(field.Name);
                }
            }
        }

        public void WriteObjectTypeGroups(BinaryWriter bw)
        {
            foreach ((TypeSerializer ser, object[] list) in TypeGroups)
            {
                bw.WriteVLu32(ser.Id);
                bw.WriteVLu32((uint)list.Length); // int32 because we allow > 65k objects
            }
        }

        public void WriteObjects(BinaryWriter bw)
        {
            var objects = new Array<object>(NumObjects);
            var pointers = new Map<object, uint>(NumObjects);

            // pre-pass: create integer pointers of all objects
            foreach ((TypeSerializer ser, object[] list) in TypeGroups)
            {
                foreach (object o in list)
                {
                    objects.Add(o);
                    pointers[o] = (uint)objects.Count; // pointer = objectIndex + 1
                }
            }

            foreach ((TypeSerializer ser, object[] list) in TypeGroups)
            {
                foreach (object o in list)
                {
                    WriteObject(bw, ser, o, pointers);
                }
            }
        }

        void WriteObject(BinaryWriter bw, TypeSerializer ser, object instance, Map<object, uint> pointers)
        {
            // NOTE: the object typeId is already handled by TypeGroup data

            if (ser is UserTypeSerializer userSer)
            {
                // number of fields, so we know how many to parse later
                bw.WriteVLu32((ushort)userSer.Fields.Count);

                // @note This is not recursive, because we only write object "Pointers" ID-s
                foreach (DataField field in userSer.Fields)
                {
                    // always include the type, this allows us to handle
                    // fields which get deleted, so they can be skipped
                    TypeSerializer fieldSer = field.Serializer;
                    bw.WriteVLu32(fieldSer.Id);

                    // write the field IDX to Stream so we can remap it
                    // to actual FieldIdx during deserialize
                    bw.WriteVLu32((ushort)field.FieldIdx);

                    object fieldObject = field.Get(instance);
                    if (fieldObject == null)
                    {
                        bw.WriteVLu32(0); // NULL pointer
                    }
                    else if (pointers.TryGetValue(fieldObject, out uint pointer))
                    {
                        bw.WriteVLu32(pointer); // write the object pointer
                    }
                    else if (fieldSer.IsUserClass)
                    {
                        // an UserClass struct which has to be serialized inline
                        WriteObject(bw, fieldSer, fieldObject, pointers);
                    }
                    else // it's a float, int, Vector2, etc. dump it directly
                    {
                        fieldSer.Serialize(bw, fieldObject);
                    }
                }
            }
            else // string, object[], stuff like that
            {
                ser.Serialize(bw, instance);
            }
        }
    }
}
