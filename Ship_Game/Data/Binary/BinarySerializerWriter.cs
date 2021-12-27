using System;
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

        public Array<ObjectReference> ObjectsList = new Array<ObjectReference>();
        Map<object, int> ObjectToPointer = new Map<object, int>();
        uint[] ObjectOffsetTable;
        public Array<TypeSerializer> UsedTypes;

        // Recursively gathers all UserType instances and records them
        // in a Depth-First approach
        public void GatherObjects(TypeSerializer ser, object instance)
        {
            if (instance == null || instance is ValueType)
                return; // we don't map null OR value types

            if (ObjectToPointer.ContainsKey(instance))
                return; // object already mapped

            int pointer = ObjectsList.Count + 1;
            ObjectToPointer[instance] = pointer;
            ObjectsList.Add(new ObjectReference(instance, ser));

            if (ser is UserTypeSerializer userType)
            {
                foreach (DataField field in userType.Fields)
                {
                    GatherObjects(field.Serializer, field.Get(instance));
                }
            }
        }

        public void GatherUsedTypes()
        {
            // only User Types, ignore Map<,>, Array<>, T[], ...
            UsedTypes = ObjectsList.FilterSelect(o => o.Serializer.IsUserClass, o => o.Serializer).Unique();

            // make the types somewhat stable by sorting them by name
            // (however deleted types is still a problem)
            UsedTypes.Sort((a, b) => string.CompareOrdinal(a.Type.Name, b.Type.Name));
        }

        public void WriteTypesList(BinaryWriter writer, bool useStableMapping)
        {
            foreach (TypeSerializer serializer in UsedTypes)
            {
                string typeName = serializer.Type.FullName;
                string assemblyName = serializer.Type.Assembly.GetName().Name;
                //Type type = Type.GetType($"{typeName},{assemblyName}", throwOnError: true);
                writer.Write(serializer.Id);
                // by outputting the full type name and assembly name, we will be able
                // to always locate the type, unless its assembly is changed
                writer.Write(typeName + "," + assemblyName);

                if (useStableMapping && serializer is UserTypeSerializer userSer)
                {
                    writer.Write((ushort)userSer.Fields.Count);
                    foreach (DataField field in userSer.Fields)
                        writer.Write(field.Name);
                }
            }
        }

        public void WriteOffsetTable(BinaryWriter writer, long seekTo = -1)
        {
            if (seekTo != -1)
                writer.BaseStream.Seek(seekTo, SeekOrigin.Begin);

            if (ObjectOffsetTable == null)
                ObjectOffsetTable = new uint[ObjectsList.Count];

            for (int i = 0; i < ObjectOffsetTable.Length; ++i)
                writer.Write(ObjectOffsetTable[i]);
        }

        public void WriteObjects(BinaryWriter writer)
        {
            for (int objectIdx = 0; objectIdx < ObjectsList.Count; ++objectIdx)
                SerializeObject(writer, objectIdx);
        }

        public void SerializeObject(BinaryWriter writer, int objectIdx)
        {
            ObjectReference oref = ObjectsList[objectIdx];
            TypeSerializer serializer = oref.Serializer;
            object instance = oref.Instance;

            ObjectOffsetTable[objectIdx] = (uint)writer.BaseStream.Position;

            // type ID so we can recognize what TYPE this object is when deserializing
            writer.Write((ushort)serializer.Id);

            if (serializer is UserTypeSerializer userSer)
            {
                // number of fields, so we know how many to parse later
                writer.Write((ushort)userSer.Fields.Count);

                // @note This is not recursive, because we only write object "Pointers" ID-s
                foreach (DataField field in userSer.Fields)
                {
                    // always include the type, this allows us to handle
                    // fields which get deleted, so they can be skipped
                    writer.Write((ushort)field.Serializer.Id);

                    // write the field IDX to Stream so we can remap it
                    // to actual FieldIdx during deserialize
                    writer.Write((ushort)field.FieldIdx);

                    object fieldObject = field.Get(instance);
                    if (fieldObject == null)
                    {
                        writer.Write(0); // NULL pointer
                    }
                    else if (ObjectToPointer.TryGetValue(fieldObject, out int pointer))
                    {
                        writer.Write(pointer); // write the object pointer
                    }
                    else // it's a float, int, Vector2, etc. dump it directly
                    {
                        field.Serializer.Serialize(writer, fieldObject);
                    }
                }
            }
            else // string, object[], stuff like that
            {
                serializer.Serialize(writer, instance);
            }
        }
    }
}
