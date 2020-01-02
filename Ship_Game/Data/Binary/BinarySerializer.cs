using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Data.Binary
{
    public class BinarySerializer : UserTypeSerializer
    {
        public override string ToString() => $"BinarySerializer {TheType.GetTypeName()}";

        struct ObjectReference
        {
            public object Instance;
            public TypeSerializer Serializer;
        }

        readonly Array<ObjectReference> ObjectsList = new Array<ObjectReference>();
        readonly Map<object, int> ObjectPointers = new Map<object, int>();

        public BinarySerializer(Type type) : base(type)
        {
        }

        protected override TypeSerializerMap CreateTypeMap()
        {
            return new BinarySerializerMap();
        }

        void GatherObjects(TypeSerializer ser, object instance)
        {
            if (instance == null || instance is ValueType)
                return; // we don't map null OR value types

            if (ObjectPointers.ContainsKey(instance))
                return; // object already mapped

            int pointer = ObjectsList.Count + 1;
            ObjectPointers[instance] = pointer;
            ObjectsList.Add(new ObjectReference
            {
                Instance = instance,
                Serializer = ser,
            });

            if (ser is UserTypeSerializer userType)
            {
                foreach (DataField field in userType.Fields)
                {
                    GatherObjects(field.Serializer, field.Get(instance));
                }
            }
        }

        void BuildSerializeCache(object instance)
        {
            ResolveTypes();
            ObjectsList.Capacity = 8192*4;
            GatherObjects(this, instance);
        }

        void WriteTypesList(BinaryWriter writer)
        {
            writer.Write(TypeMap.TypesList.Count);
            foreach (TypeSerializer serializer in TypeMap.TypesList)
            {
                string typeName = serializer.Type.FullName;
                string assemblyName = serializer.Type.Assembly.GetName().Name;
                //Type type = Type.GetType($"{typeName},{assemblyName}", throwOnError: true);
                writer.Write(serializer.Id);
                writer.Write(typeName + "," + assemblyName);
            }
        }

        void SerializeObject(BinaryWriter writer, TypeSerializer serializer, object instance)
        {
            // type ID so we can recognize what TYPE this object is when deserializing
            writer.Write(serializer.Id);

            if (serializer is UserTypeSerializer userSer)
            {
                // number of fields, so we know how many to parse later
                writer.Write(userSer.Fields.Count);

                // @note This is not recursive, because we only write object "Pointers" ID-s
                foreach (DataField field in userSer.Fields)
                {
                    // write the field ID so we can remap it during parsing
                    TypeSerializer.WriteFieldId(writer, field.Id);
                    object fieldObject = field.Get(instance);
                    if (fieldObject == null)
                    {
                        writer.Write(0); // NULL pointer :)
                    }
                    else if (ObjectPointers.TryGetValue(fieldObject, out int id))
                    {
                        writer.Write(id); // write the object ID. kind of like a "pointer"
                    }
                    else // it's a float, int, Vector2, etc. dump it directly
                    {
                        writer.Write(field.Serializer.Id); // also include the type
                        field.Serializer.Serialize(writer, fieldObject);
                    }
                }
            }
            else // string, object[], stuff like that
            {
                serializer.Serialize(writer, instance);
            }
        }

        public override void Serialize(BinaryWriter writer, object obj)
        {
            if (Mapping == null)
            {
                BuildSerializeCache(obj);
            }

            WriteTypesList(writer);

            writer.Write(ObjectsList.Count);
            foreach (ObjectReference refType in ObjectsList)
            {
                SerializeObject(writer, refType.Serializer, refType.Instance);
            }
        }
        
        public override object Deserialize(BinaryReader reader)
        {
            if (Mapping == null)
            {

            }
            return null;
        }
    }
}
