using System;
using System.IO;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Binary
{
    public class BinarySerializer : UserTypeSerializer
    {
        public override string ToString() => $"BinarySerializer {NiceTypeName}:{TypeId}";

        // The currently supported version
        public const uint CurrentVersion = 1;

        // Version from deserialized data
        public uint Version { get; private set; } = CurrentVersion;

        public BinarySerializer(Type type) : base(type, new BinaryTypeMap())
        {
            TypeMap.Add(this);
        }

        public BinarySerializer(Type type, TypeSerializerMap typeMap) : base(type, typeMap)
        {
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
            throw new NotImplementedException($"Serialize (yaml) not supported for {this}");
        }

        public override object Deserialize(YamlNode node)
        {
            throw new NotImplementedException($"Deserialize (yaml) not supported for {this}");
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            throw new NotImplementedException();
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            throw new NotImplementedException();
        }

        public void Serialize(BinaryWriter writer, object obj, bool verbose = false)
        {
            // pre-scan all unique objects
            var ctx = new BinarySerializerWriter(writer) { Verbose = verbose };
            ctx.ScanObjects(this, obj);

            // [header]
            // [types list]
            // [object type groups]
            // [objects list]
            var header = new BinarySerializerHeader(ctx);
            if (verbose) Log.Info($"Serialize {header}");

            header.Write(writer);
            if (ctx.NumObjects != 0)
            {
                ctx.WriteTypesList();
                ctx.WriteObjectTypeGroups();
                ctx.WriteObjects();
            }
        }

        public object Deserialize(BinaryReader reader, bool verbose = false)
        {
            // [header]
            // [types list]
            // [object type groups]
            // [objects list]
            var header = new BinarySerializerHeader(reader);
            if (verbose) Log.Info($"Deserialize {header}");

            // this is not a binary serializer file!
            if (header.Signature != BinarySerializerHeader.ValidSignature)
            {
                if (verbose) Log.Info("Invalid BinarySerializerHeader Signature");
                return null;
            }

            Version = header.Version;
            if (Version != CurrentVersion)
            {
                Log.Warning($"BinarySerializer.Deserialize version mismatch: file({Version}) != current({CurrentVersion})");
            }

            if (header.NumTypeGroups != 0)
            {
                var ctx = new BinarySerializerReader(reader, TypeMap, header) { Verbose = verbose };
                ctx.ReadTypesList();
                ctx.ReadTypeGroups();
                ctx.ReadObjectsList();
                object root = ctx.ObjectsList[header.RootObjectIndex];
                return root;
            }

            return null;
        }

        // Aggregates multiple different types into a single binary writer stream
        public static void SerializeMultiType(BinaryWriter writer, object[] objects, bool verbose = false)
        {
            var serializers = new BinarySerializer[objects.Length];
            for (int i = 0; i < objects.Length; ++i) // using loop for more accurate perf stats
                serializers[i] = new BinarySerializer(objects[i].GetType());

            for (int i = 0; i < objects.Length; ++i)
            {
                object o = objects[i];
                if (o.GetType().IsValueType)
                    throw new InvalidOperationException($"ValueType {o.GetType()} cannot be top-level serialized! Change it into a class");
                serializers[i].Serialize(writer, o, verbose);
            }
        }

        // Deserializes multiple different typed objects from a single binary reader stream
        public static object[] DeserializeMultiType(BinaryReader reader, Type[] types, bool verbose = false)
        {
            var serializers = new BinarySerializer[types.Length];
            for (int i = 0; i < types.Length; ++i) // using loop for more accurate perf stats
                serializers[i] = new BinarySerializer(types[i]);

            var objects = new object[serializers.Length];

            for (int i = 0; i < objects.Length; ++i)
            {
                objects[i] = serializers[i].Deserialize(reader, verbose);
            }

            return objects;
        }
    }
}
