using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Binary
{
    public class BinarySerializer : UserTypeSerializer
    {
        public override string ToString() => $"BinarySerializer {Type.GetTypeName()}";

        struct ObjectReference
        {
            public object Instance;
            public TypeSerializer Serializer;
            public ObjectReference(object obj, TypeSerializer ser)
            {
                Instance = obj;
                Serializer = ser;
            }
        }

        // The currently supported version
        public const int CurrentVersion = 1;

        // Version from deserialized data
        public int Version { get; private set; } = CurrentVersion;

        // Serialize: set true to output TypesList with field names and perform Type mapping
        //            set false to omit field names (smaller TypesList but crashes if field order changes)
        // Deserialize: always overwritten by stream data
        public bool UseStableMapping { get; set; } = true;

        bool IsRoot;

        public BinarySerializer(Type type) : base(type, new BinarySerializerMap())
        {
            IsRoot = true;
            IsUserClass = true;
            TypeMap.Add(type, this);
            ResolveTypes();
        }

        public BinarySerializer(Type type, TypeSerializerMap typeMap) : base(type, typeMap)
        {
            IsRoot = false;
            IsUserClass = true;
        }

        struct Header
        {
            public byte Version;
            public byte Options;
            public int NumTypes;
            public int NumObjects;
            public uint StreamSize;

            public Header(BinaryReader reader)
            {
                Version = reader.ReadByte();
                Options = reader.ReadByte();
                NumTypes   = reader.ReadInt32();
                NumObjects = reader.ReadInt32();
                StreamSize = reader.ReadUInt32();
            }

            public Header(bool stable, int numTypes, int numObjects)
            {
                Version = CurrentVersion;
                Options = 0;
                NumTypes = numTypes;
                NumObjects = numObjects;
                StreamSize = 1 + 1 + 4 + 4 + 4;
                UseStableMapping = stable;
            }

            public void Write(BinaryWriter writer)
            {
                writer.Write((byte)Version);
                writer.Write((byte)Options);
                writer.Write((int)NumTypes);
                writer.Write((int)NumObjects);
                writer.Write((uint)StreamSize);
            }

            public bool UseStableMapping
            {
                get => (Options & (1 << 1)) != 0;
                set => Options = (byte)(value ? Options | (1 << 1) : Options & ~(1 << 1));
            }
        }

        class SerializerContext
        {
            public Array<ObjectReference> ObjectsList = new Array<ObjectReference>();
            public Map<object, int> ObjectToPointer = new Map<object, int>();
            public uint[] ObjectOffsetTable;
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

        class DeserializerContext
        {
            public Header Header;
            public Array<ObjectReference> ObjectsList;
            public uint[] ObjectOffsetTable;

            // maps stream DataField Idx to actual DataFields in memory
            // this is needed because serialized data will almost always
            // be out of sync with latest data field changes
            // 
            // This mapping allows us to completely avoid field name checks
            // during Deserialize() itself
            public ushort[][] StreamFieldsToActual;

            // And this maps Stream Type ID-s to Actual Type ID-s
            // to handle cases where types are added/removed from type list
            public ushort[] StreamTypesToActual;

            public bool StableMapping;

            public DeserializerContext(in Header header)
            {
                Header = header;
                StableMapping = header.UseStableMapping;
                if (StableMapping)
                {
                    StreamFieldsToActual = new ushort[header.NumTypes][];
                    StreamTypesToActual = new ushort[header.NumTypes];
                }

                ObjectsList = new Array<ObjectReference>();
                ObjectsList.Resize(header.NumObjects);
                ObjectOffsetTable = new uint[header.NumObjects];
            }

            // Converts Stream typeId into an Actual typeId
            public ushort GetActualTypeId(ushort streamTypeId)
            {
                if (StableMapping && streamTypeId >= MaxFundamentalTypes)
                    return StreamTypesToActual[streamTypeId - MaxFundamentalTypes];
                return streamTypeId;
            }

            // Converts Stream field index into an Actual fieldIdx
            public ushort GetActualFieldIdx(ushort streamTypeId, ushort streamFieldIdx)
            {
                if (StableMapping && streamTypeId >= MaxFundamentalTypes)
                    return StreamFieldsToActual[streamTypeId - MaxFundamentalTypes][streamFieldIdx];
                return streamFieldIdx;
            }

            public void ReadTypesList(BinaryReader reader, UserTypeSerializer owner)
            {
                for (int i = 0; i < Header.NumTypes; ++i)
                {
                    ushort typeId = reader.ReadUInt16();
                    string typeNameAndAssembly = reader.ReadString();

                    // if type is not found, we completely give up
                    // TODO: we should mark this type as invalid and use `null` during deserialize
                    Type type = Type.GetType(typeNameAndAssembly, throwOnError: true);
                    if (!owner.TypeMap.TryGet(type, out TypeSerializer serializer))
                    {
                        serializer = owner.TypeMap.AddUserTypeSerializer(type);
                    }

                    if (StableMapping && serializer is UserTypeSerializer userSer)
                    {
                        ushort numFields = reader.ReadUInt16();
                        ushort[] mapping = new ushort[numFields];

                        for (ushort fieldIdx = 0; fieldIdx < numFields; ++fieldIdx)
                        {
                            string fieldName = reader.ReadString();
                            DataField field = userSer.GetFieldOrNull(fieldName);
                            if (field != null)
                            {
                                mapping[fieldIdx] = (ushort)field.FieldIdx;
                            }
                            else // field not found, which means the DataField is deleted or name has changed
                            {
                                mapping[fieldIdx] = ushort.MaxValue;
                            }
                        }

                        StreamFieldsToActual[typeId - MaxFundamentalTypes] = mapping;
                        StreamTypesToActual[typeId - MaxFundamentalTypes] = serializer.Id;
                    }
                }
            }

            public void ReadOffsetTable(BinaryReader reader)
            {
                for (int objectIdx = 0; objectIdx < ObjectOffsetTable.Length; ++objectIdx)
                    ObjectOffsetTable[objectIdx] = reader.ReadUInt32();
            }

            public object CreateObject(BinaryReader reader, TypeSerializerMap typeMap, long streamStart, int objectIdx)
            {
                // if object was already created, just return it
                // this handles multiple refs and cyclic refs
                object instance = ObjectsList[objectIdx].Instance;
                if (instance != null)
                    return instance;

                var stream = reader.BaseStream;
                long previousPos = stream.Position;
                long objectPos = streamStart + ObjectOffsetTable[objectIdx];
                if (objectPos != previousPos)
                    stream.Seek(objectPos, SeekOrigin.Begin);

                ushort typeId = GetActualTypeId(reader.ReadUInt16());
                TypeSerializer serializer = typeMap.Get(typeId);

                if (serializer is UserTypeSerializer userSer)
                {
                    instance = Activator.CreateInstance(userSer.Type);
                    // need to add it right away, to handle cyclic references
                    ObjectsList[objectIdx] = new ObjectReference(instance, serializer);

                    int numFields = reader.ReadUInt16();
                    for (int i = 0; i < numFields; ++i)
                    {
                        // serializer Id from the stream
                        ushort fieldTypeId = GetActualTypeId(reader.ReadUInt16());
                        ushort fieldIdx = GetActualFieldIdx(fieldTypeId, reader.ReadUInt16());

                        if (typeMap.TryGet(fieldTypeId, out TypeSerializer fieldSer))
                        {
                            object fieldValue;
                            if (fieldSer.IsUserClass || fieldSer.Type == typeof(string))
                            {
                                int pointer = reader.ReadInt32();
                                if (pointer == 0)
                                    continue;
                                fieldValue = CreateObject(reader, typeMap, streamStart, pointer - 1);
                            }
                            else
                            {
                                fieldValue = fieldSer.Deserialize(reader);
                            }

                            DataField field = userSer.GetFieldOrNull(fieldIdx);
                            field?.Set(instance, fieldValue);

                        }
                        else // it's an unknown user class, try skipping 1 pointer
                        {
                            reader.ReadInt32();
                        }
                    }
                }
                else // string, object[], stuff like that
                {
                    instance = serializer.Deserialize(reader);
                    ObjectsList[objectIdx] = new ObjectReference(instance, serializer);
                }

                if (objectPos != previousPos)
                    stream.Seek(previousPos, SeekOrigin.Begin);
                return instance;
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
            var ctx = new SerializerContext();
            ctx.GatherObjects(this, obj);
            ctx.GatherUsedTypes();

            Stream stream = writer.BaseStream;
            long streamStart = stream.Position;
            var header = new Header(UseStableMapping, ctx.UsedTypes.Count, ctx.ObjectsList.Count);

            // [header]
            // [types list]
            // [object offset table]
            // [objects list]
            header.Write(writer);
            if (ctx.ObjectsList.Count == 0)
                return;

            ctx.WriteTypesList(writer, header.UseStableMapping);
            long offsetTablePos = stream.Position; // offset of the [placeholders]
            ctx.WriteOffsetTable(writer);
            ctx.WriteObjects(writer);

            // now flush finalized header
            header.StreamSize = (uint)(stream.Position - streamStart);
            stream.Seek(streamStart, SeekOrigin.Begin);
            header.Write(writer);

            // flush object offset table
            ctx.WriteOffsetTable(writer, offsetTablePos);

            // seek back to end
            stream.Seek(0, SeekOrigin.End);
        }

        public override object Deserialize(BinaryReader reader)
        {
            if (!IsRoot)
                throw new InvalidOperationException($"Deserialize() can only be called on Root Serializer");

            long streamStart = reader.BaseStream.Position;

            // [header]
            // [types list]
            // [object offset table]
            // [objects list]
            var header = new Header(reader);
            if (header.NumObjects == 0)
                return null;

            Version = header.Version;
            UseStableMapping = header.UseStableMapping;
            if (Version != CurrentVersion)
            {
                Log.Warning($"BinarySerializer.Deserialize version mismatch: file({Version}) != current({CurrentVersion})");
            }

            var ctx = new DeserializerContext(header);
            ctx.ReadTypesList(reader, this);
            ctx.ReadOffsetTable(reader);

            // first object is always the root object
            // so all we need to do is just create object:0 and all
            // other object instances will be created recursively
            object root = ctx.CreateObject(reader, TypeMap, streamStart, objectIdx:0);

            // properly seek to the end of current stream
            reader.BaseStream.Seek(streamStart + header.StreamSize, SeekOrigin.Begin);

            return root;
        }
    }
}
