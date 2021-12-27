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
            TypeMap.Add(this);
        }

        public BinarySerializer(Type type, TypeSerializerMap typeMap) : base(type, typeMap)
        {
            IsRoot = false;
        }

        // cache for binary type converters
        class BinaryTypeMap : TypeSerializerMap
        {
            public override TypeSerializer AddUserTypeSerializer(Type type)
            {
                return Add(new BinarySerializer(type, this));
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
            ctx.ScanObjects(this, obj);

            // [header]
            // [types list]
            // [object type groups]
            // [objects list]
            var header = new BinarySerializerHeader(UseStableMapping, ctx);
            header.Write(writer);
            if (ctx.NumObjects != 0)
            {
                ctx.WriteTypesList(writer, header.UseStableMapping);
                ctx.WriteObjectTypeGroups(writer);
                ctx.WriteObjects(writer);
            }
        }

        public override object Deserialize(BinaryReader reader)
        {
            if (!IsRoot)
                throw new InvalidOperationException($"Deserialize() can only be called on Root Serializer");

            // [header]
            // [types list]
            // [object type groups]
            // [objects list]
            var header = new BinarySerializerHeader(reader);
            Version = header.Version;
            UseStableMapping = header.UseStableMapping;
            if (Version != CurrentVersion)
            {
                Log.Warning($"BinarySerializer.Deserialize version mismatch: file({Version}) != current({CurrentVersion})");
            }

            if (header.NumTypeGroups != 0)
            {
                var ctx = new BinarySerializerReader(header);
                ctx.ReadTypesList(reader, TypeMap);
                ctx.ReadTypeGroups(reader, TypeMap);
                ctx.ReadObjectsList(reader, TypeMap);
                object root = ctx.ObjectsList[header.RootObjectIndex];
                return root;
            }

            return null;
        }
    }
}
