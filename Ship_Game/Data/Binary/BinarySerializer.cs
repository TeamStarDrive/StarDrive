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
    public partial class BinarySerializer : UserTypeSerializer
    {
        public override string ToString() => $"BinarySerializer {Type.GetTypeName()}";

        // The currently supported version
        public const int CurrentVersion = 1;

        // Version from deserialized data
        public int Version { get; private set; } = CurrentVersion;

        // Serialize: set true to output TypesList with field names and perform Type mapping
        //            set false to omit field names (smaller TypesList but crashes if field order changes)
        // Deserialize: always overwritten by stream data
        public bool UseStableMapping { get; set; } = true;

        // for binary serializer, only Root object can invoke Serialize()/Deserialize()
        // this is for performance reasons and to share one single type cache
        bool IsRoot;

        public BinarySerializer(Type type) : base(type, new BinaryTypeMap())
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

        // cache for binary type converters
        class BinaryTypeMap : TypeSerializerMap
        {
            public override TypeSerializer AddUserTypeSerializer(Type type)
            {
                return Add(type, new BinarySerializer(type, this));
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
            var ctx = new BinarySerializerWriter();
            ctx.GatherObjects(this, obj);
            ctx.GatherUsedTypes();

            Stream stream = writer.BaseStream;
            long streamStart = stream.Position;
            var header = new BinarySerializerHeader(UseStableMapping, ctx.UsedTypes.Count, ctx.ObjectsList.Count);

            // [header]
            // [types list]
            // [object type groups]
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
            var header = new BinarySerializerHeader(reader);
            if (header.NumObjects == 0)
                return null;

            Version = header.Version;
            UseStableMapping = header.UseStableMapping;
            if (Version != CurrentVersion)
            {
                Log.Warning($"BinarySerializer.Deserialize version mismatch: file({Version}) != current({CurrentVersion})");
            }

            var ctx = new BinarySerializerReader(header);
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
