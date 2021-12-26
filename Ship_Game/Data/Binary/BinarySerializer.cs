using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;

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

        Array<ObjectReference> ObjectsList;
        Map<object, int> ObjectToPointer;
        uint[] ObjectOffsetTable;

        // The currently supported version
        public const int CurrentVersion = 1;

        // Version from deserialized data
        public int Version { get; private set; } = CurrentVersion;

        // Serialize: set true to output TypesList with field names
        //            set false to omit field names (smaller TypesList but crashes if field order changes)
        // Deserialize: always overwritten by stream data
        public bool UseStableBinaryTypes { get; set; } = true;

        bool IsRoot;

        public BinarySerializer(Type type) : base(type, new BinarySerializerMap())
        {
            IsRoot = true;
            IsUserClass = true;
        }

        public BinarySerializer(Type type, TypeSerializerMap typeMap) : base(type, typeMap)
        {
            IsRoot = false;
            IsUserClass = true;
        }

        void WriteTypesList(BinaryWriter writer)
        {
            var usedTypes = new HashSet<TypeSerializer>();
            foreach (ObjectReference oref in ObjectsList)
                usedTypes.Add(oref.Serializer);

            writer.Write(UseStableBinaryTypes);
            writer.Write(usedTypes.Count);
            foreach (TypeSerializer serializer in usedTypes)
            {
                string typeName = serializer.Type.FullName;
                string assemblyName = serializer.Type.Assembly.GetName().Name;
                //Type type = Type.GetType($"{typeName},{assemblyName}", throwOnError: true);
                writer.Write(serializer.Id);
                // by outputting the full type name and assembly name, we will be able
                // to always locate the type, unless its assembly is changed
                writer.Write(typeName + "," + assemblyName);

                if (UseStableBinaryTypes && serializer is UserTypeSerializer userSer)
                {
                    writer.Write((ushort)userSer.Fields.Count);
                    foreach (DataField field in userSer.Fields)
                        writer.Write(field.Name);
                }
            }
        }

        void ReadTypesList(BinaryReader reader)
        {
            UseStableBinaryTypes = reader.ReadBoolean();
            int numTypes = reader.ReadInt32();
            for (int i = 0; i < numTypes; ++i)
            {
                ushort typeId = reader.ReadUInt16();
                string typeNameAndAssembly = reader.ReadString();

                Type type = Type.GetType(typeNameAndAssembly, throwOnError: true);
                var serializer = TypeMap.AddUserTypeSerializer(type);
                TypeMap.Set(typeId, type, serializer);

                if (serializer is UserTypeSerializer userSer)
                {
                    var fields = userSer.Fields;

                    if (UseStableBinaryTypes)
                    {
                        ushort numFields = reader.ReadUInt16();
                        for (ushort fieldIdx = 0; fieldIdx < numFields; ++fieldIdx)
                        {

                        }
                    }
                }
            }
        }

        void CreateObjectOffsetTable()
        {
            if (ObjectsList.Count == 0)
                throw new InvalidOperationException($"Object count cannot be zero!");
            ObjectOffsetTable = new uint[ObjectsList.Count];
        }

        void ResetObjectPointers()
        {
            ObjectToPointer = new Map<object, int>();
            ObjectsList = new Array<ObjectReference>();
            //ObjectsList.Capacity = 8192 * 4;
        }

        // Adds a new unique object to the ObjectList
        void AddObjectPointer(TypeSerializer ser, object instance)
        {
            int pointer = ObjectsList.Count + 1;
            ObjectToPointer[instance] = pointer;
            ObjectsList.Add(new ObjectReference
            {
                Instance = instance,
                Serializer = ser,
            });
        }

        // Recursively gathers all UserType instances and records them
        // in a Depth-First approach
        void GatherObjects(TypeSerializer ser, object instance)
        {
            if (instance == null || instance is ValueType)
                return; // we don't map null OR value types

            if (ObjectToPointer.ContainsKey(instance))
                return; // object already mapped

            AddObjectPointer(ser, instance);

            if (ser is UserTypeSerializer userType)
            {
                foreach (DataField field in userType.Fields)
                {
                    GatherObjects(field.Serializer, field.Get(instance));
                }
            }
        }

        void SerializeObject(BinaryWriter writer, int objectIdx)
        {
            ObjectReference oref = ObjectsList[objectIdx];
            TypeSerializer serializer = oref.Serializer;
            object instance = oref.Instance;

            ObjectOffsetTable[objectIdx] = (uint)writer.BaseStream.Position;

            // type ID so we can recognize what TYPE this object is when deserializing
            writer.Write(serializer.Id);

            if (serializer is UserTypeSerializer userSer)
            {
                // number of fields, so we know how many to parse later
                writer.Write((ushort)userSer.Fields.Count);

                // @note This is not recursive, because we only write object "Pointers" ID-s
                foreach (DataField field in userSer.Fields)
                {
                    // write the field IDX so we can remap it during parsing
                    writer.Write((ushort)field.FieldIdx);
                    object fieldObject = field.Get(instance);
                    if (fieldObject == null)
                    {
                        writer.Write(0); // NULL pointer :)
                    }
                    else if (ObjectToPointer.TryGetValue(fieldObject, out int pointer))
                    {
                        writer.Write(pointer); // write the object ID. kind of like a "pointer"
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

        object CreateObject(BinaryReader reader, int objectIdx)
        {
            int typeId = reader.ReadInt32();
            TypeSerializer serializer = TypeMap.Get(typeId);
            object instance;

            if (serializer is UserTypeSerializer userSer)
            {
                instance = Activator.CreateInstance(userSer.Type);

                int numFields = reader.ReadUInt16();
                for (int i = 0; i < numFields; ++i)
                {
                    // fieldId which maps to the deserialized 
                    int fieldId = reader.ReadUInt16();
                }
            }
            else // string, object[], stuff like that
            {
                instance = serializer.Deserialize(reader);
            }

            AddObjectPointer(serializer, instance);
            return instance;
        }

        void DeserializeObject(BinaryReader reader, int objectPointer)
        {
            ObjectReference oref = ObjectsList[objectPointer];
            if (oref.Serializer is UserTypeSerializer userSer)
            {
                foreach (DataField field in userSer.Fields)
                {
                    
                }
            }
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            throw new NotImplementedException($"Serialize (yaml) not supported for {ToString()}");
        }

        public override void Serialize(BinaryWriter writer, object obj)
        {
            if (!IsRoot)
                throw new InvalidOperationException($"Serialize() can only be called on Root Serializer");

            // pre-scan all unique objects
            ResetObjectPointers();
            GatherObjects(this, obj);
            CreateObjectOffsetTable();

            // 1. Version of Binary Serializer
            writer.Write((byte)CurrentVersion);

            // 2. Custom Types List
            WriteTypesList(writer);

            // 3. Number of Serialized Objects
            writer.Write(ObjectsList.Count);
            if (ObjectsList.Count == 0)
                return;

            Stream stream = writer.BaseStream;
            long objecTablePos = stream.Position; // global offset of the [offsets table]

            // 4. Stream Size [placeholder]
            writer.Write((uint)0);
            // 5. Object Offset Table [placeholder]
            for (int i = 0; i < ObjectOffsetTable.Length; ++i)
                writer.Write(ObjectOffsetTable[i]);

            // Serialized Objects data
            for (int i = 0; i < ObjectsList.Count; ++i)
                SerializeObject(writer, i);

            // (4) and (5) flush the filled out stream size and offsets table
            long streamSize = stream.Position;
            stream.Seek(objecTablePos, SeekOrigin.Begin);
            writer.Write((uint)streamSize);
            for (int i = 0; i < ObjectOffsetTable.Length; ++i)
                writer.Write(ObjectOffsetTable[i]);
            stream.Seek(streamSize, SeekOrigin.Begin);
        }

        public override object Deserialize(BinaryReader reader)
        {
            if (!IsRoot)
                throw new InvalidOperationException($"Deserialize() can only be called on Root Serializer");

            Stream stream = reader.BaseStream;
            long streamStart = stream.Position;

            // 1. Version
            Version = (int)reader.ReadByte();
            if (Version != CurrentVersion)
            {
                Log.Warning($"BinarySerializer.Deserialize version mismatch: file({Version}) != current({CurrentVersion})");
            }

            // 2. Custom Types list
            ReadTypesList(reader);

            // 3. Number of serialized objects
            int numObjects = reader.ReadInt32();
            if (numObjects == 0)
                return null;

            // 4. Stream size
            long streamSize = reader.ReadUInt32();

            // 5.  Object Offset Table
            ResetObjectPointers();
            ObjectsList.Resize(numObjects);
            CreateObjectOffsetTable();

            for (int i = 0; i < numObjects; ++i)
                ObjectOffsetTable[i] = reader.ReadUInt32();

            // first object is always the root object
            // so all we need to do is just create object:0 and all
            // other object instances will be created recursively
            object root = CreateObject(reader, objectIdx:0);

            // properly seek to the end of current stream
            stream.Seek(streamStart + streamSize, SeekOrigin.Begin);

            return root;
        }
    }
}
