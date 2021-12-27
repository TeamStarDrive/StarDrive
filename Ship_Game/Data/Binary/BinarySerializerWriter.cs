using System;
using System.Collections.Generic;
using System.IO;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Data.Binary
{
    class BinarySerializerWriter
    {
        public struct ObjectReference
        {
            public object Instance;
            public TypeSerializer Serializer;
            public ObjectReference(object obj, TypeSerializer ser)
            {
                Instance = obj;
                Serializer = ser;
            }
        }

        // total number of objects
        public int NumObjects;
 
        // index of the root object which is being serialized/deserialized
        public int RootObjectIndex;

        // which UserClass types were used (excludes strings or other fundamental types)
        public TypeSerializer[] UsedTypes;

        // Object lists grouped by their type (includes strings)
        public (TypeSerializer Ser, object[] Objects)[] TypeGroups;

        // Recursively gathers all UserType instances,
        // the order here is unimportant because they get sorted later
        void RecursiveGatherObjs(TypeSerializer ser, object instance, 
                                 Map<TypeSerializer, Array<object>> groups,
                                 HashSet<object> objects)
        {
            if (instance == null || !BinarySerializer.IsPointerType(ser))
                return; // we don't map nulls OR non-pointer types

            if (!objects.Add(instance))
                return; // object already mapped

            if (!groups.TryGetValue(ser, out Array<object> list))
                groups.Add(ser, (list = new Array<object>()));

            list.Add(instance);

            if (ser is UserTypeSerializer userType)
            {
                foreach (DataField field in userType.Fields)
                {
                    object obj = field.Get(instance);
                    RecursiveGatherObjs(field.Serializer, obj, groups, objects);
                }
            }
        }

        public void ScanObjects(TypeSerializer ser, object rootObject)
        {
            var objectGroups = new Map<TypeSerializer, Array<object>>();
            var uniqueObjects = new HashSet<object>();
            RecursiveGatherObjs(ser, rootObject, objectGroups, uniqueObjects);

            NumObjects = uniqueObjects.Count;

            // make the types somewhat stable by sorting them by name
            // new/deleted types will of course offset this list immediately
            // and deleted types can't be reconstructed during Reading
            // strings have to be first
            var groups = objectGroups.ToArrayList();
            groups.Sort((a, b) =>
            {
                // strings must always be first, because they are a
                // pointer type which is not an UserClass
                if (a.Key.Type == typeof(string)) return -1;
                if (b.Key.Type == typeof(string)) return +1;

                // non-pointer types must come second
                bool isPointerA = BinarySerializer.IsPointerType(a.Key);
                bool isPointerB = BinarySerializer.IsPointerType(b.Key);
                if (!isPointerA && isPointerB) return -1;
                if (isPointerA && !isPointerB) return +1;

                // the rest, sort by type name
                return string.CompareOrdinal(a.Key.Type.Name, b.Key.Type.Name);
            });
            TypeGroups = groups.Select(kv => (kv.Key, kv.Value.ToArray()));

            // only User Types, ignore Map<,>, Array<>, T[], ...
            UsedTypes = TypeGroups.FilterSelect(sl => sl.Ser.IsUserClass, sl => sl.Ser);

            RootObjectIndex = IndexOfRootObject(rootObject);
        }

        int IndexOfRootObject(object rootObject)
        {
            int count = 0;
            foreach ((TypeSerializer ser, object[] list) in TypeGroups)
            {
                for (int i = 0; i < list.Length; ++i)
                    if (list[i] == rootObject)
                        return count + i;
                count += list.Length;
            }
            return -1;
        }

        public void WriteTypesList(BinaryWriter bw, bool useStableMapping)
        {
            foreach (TypeSerializer serializer in UsedTypes)
            {
                string typeName = serializer.Type.FullName;
                string assemblyName = serializer.Type.Assembly.GetName().Name;
                //Type type = Type.GetType($"{typeName},{assemblyName}", throwOnError: true);
                bw.Write(serializer.Id);
                // by outputting the full type name and assembly name, we will be able
                // to always locate the type, unless its assembly is changed
                bw.Write(typeName + "," + assemblyName);

                if (useStableMapping && serializer is UserTypeSerializer userSer)
                {
                    bw.Write((ushort)userSer.Fields.Count);
                    foreach (DataField field in userSer.Fields)
                        bw.Write(field.Name);
                }
            }
        }

        public void WriteObjectTypeGroups(BinaryWriter bw)
        {
            foreach ((TypeSerializer ser, object[] list) in TypeGroups)
            {
                bw.Write((ushort)ser.Id);
                bw.Write((int)list.Length); // int32 because we allow > 65k objects
            }
        }

        public void WriteObjects(BinaryWriter bw)
        {
            var objects = new Array<object>(NumObjects);
            var pointers = new Map<object, int>(NumObjects);

            // pre-pass: create integer pointers of all objects
            foreach ((TypeSerializer ser, object[] list) in TypeGroups)
            {
                foreach (object o in list)
                {
                    objects.Add(o);
                    pointers[o] = objects.Count; // pointer = objectIndex + 1
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

        void WriteObject(BinaryWriter bw, TypeSerializer ser, object instance, Map<object, int> pointers)
        {
            // NOTE: the object typeId is already handled by TypeGroup data

            if (ser is UserTypeSerializer userSer)
            {
                // number of fields, so we know how many to parse later
                bw.Write((ushort)userSer.Fields.Count);

                // @note This is not recursive, because we only write object "Pointers" ID-s
                foreach (DataField field in userSer.Fields)
                {
                    // always include the type, this allows us to handle
                    // fields which get deleted, so they can be skipped
                    bw.Write((ushort)field.Serializer.Id);

                    // write the field IDX to Stream so we can remap it
                    // to actual FieldIdx during deserialize
                    bw.Write((ushort)field.FieldIdx);

                    object fieldObject = field.Get(instance);
                    if (fieldObject == null)
                    {
                        bw.Write(0); // NULL pointer
                    }
                    else if (pointers.TryGetValue(fieldObject, out int pointer))
                    {
                        bw.Write(pointer); // write the object pointer
                    }
                    else // it's a float, int, Vector2, etc. dump it directly
                    {
                        field.Serializer.Serialize(bw, fieldObject);
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
