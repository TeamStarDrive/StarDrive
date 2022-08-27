using System;
using System.IO;

namespace Ship_Game.Data.Binary
{
    public struct BinarySerializerHeader
    {
        // Signature of a valid BinarySerializerHeader
        public const uint ValidSignature = 0x1f2f3f4f;

        public uint Signature; // is this a BinarySerializerHeader ?
        public uint Version;
        public uint Options; // reserved for additional options
        public uint NumUsedTypes;
        public uint NumCollectionTypes;
        public uint MaxTypeId;
        public uint NumTypeGroups;
        public uint RootObjectId;

        public BinarySerializerHeader(BinarySerializerWriter writer)
        {
            Signature = ValidSignature;
            Version = BinarySerializer.CurrentVersion;
            Options = 0;
            NumUsedTypes = (uint)writer.UsedTypes.Length;
            NumCollectionTypes = (uint)writer.CollectionTypes.Length;
            MaxTypeId = (uint)Math.Max(writer.UsedTypes.Max(s => s.TypeId),
                                       writer.CollectionTypes.Max(s => s.TypeId));
            NumTypeGroups = (uint)writer.NumUsedGroups;
            RootObjectId = writer.RootObjectId;
        }

        public BinarySerializerHeader(Reader reader)
        {
            Signature = reader.ReadUInt32(); // always UInt32
            Version = reader.ReadVLu32();
            Options = reader.ReadVLu32();
            NumUsedTypes = reader.ReadVLu32();
            NumCollectionTypes = reader.ReadVLu32();
            MaxTypeId = reader.ReadVLu32();
            NumTypeGroups = reader.ReadVLu32();
            RootObjectId = reader.ReadVLu32();
        }

        public void Write(Writer writer)
        {
            writer.Write(Signature); // always UInt32
            writer.WriteVLu32(Version);
            writer.WriteVLu32(Options);
            writer.WriteVLu32(NumUsedTypes);
            writer.WriteVLu32(NumCollectionTypes);
            writer.WriteVLu32(MaxTypeId);
            writer.WriteVLu32(NumTypeGroups);
            writer.WriteVLu32(RootObjectId);
        }

        public override string ToString()
        {
            return $"v{Version} Opt={Options} Types={NumUsedTypes} Coll={NumCollectionTypes} Groups={NumTypeGroups} Root={RootObjectId}";
        }
    }
}
