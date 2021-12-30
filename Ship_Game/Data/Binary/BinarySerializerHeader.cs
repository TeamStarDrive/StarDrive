using System;
using System.IO;

namespace Ship_Game.Data.Binary
{
    public struct BinarySerializerHeader
    {
        public uint Version;
        public uint Options;
        public uint NumUserTypes;
        public uint NumCollectionTypes;
        public uint MaxTypeId;
        public uint NumTypeGroups;
        public uint RootObjectIndex;

        public BinarySerializerHeader(BinarySerializerWriter writer)
        {
            Version = BinarySerializer.CurrentVersion;
            Options = 0;
            NumUserTypes = (uint)writer.UserTypes.Length;
            NumCollectionTypes = (uint)writer.CollectionTypes.Length;
            MaxTypeId = (uint)Math.Max(writer.UserTypes.Max(s => s.TypeId),
                                       writer.CollectionTypes.Max(s => s.TypeId));
            NumTypeGroups = (uint)writer.TypeGroups.Length;
            RootObjectIndex = writer.RootObjectIndex;
        }

        public BinarySerializerHeader(BinaryReader reader)
        {
            Version = reader.ReadVLu32();
            Options = reader.ReadVLu32();
            NumUserTypes = reader.ReadVLu32();
            NumCollectionTypes = reader.ReadVLu32();
            MaxTypeId = reader.ReadVLu32();
            NumTypeGroups = reader.ReadVLu32();
            RootObjectIndex = reader.ReadVLu32();
        }

        public void Write(BinaryWriter writer)
        {
            writer.WriteVLu32(Version);
            writer.WriteVLu32(Options);
            writer.WriteVLu32(NumUserTypes);
            writer.WriteVLu32(NumCollectionTypes);
            writer.WriteVLu32(MaxTypeId);
            writer.WriteVLu32(NumTypeGroups);
            writer.WriteVLu32(RootObjectIndex);
        }
    }
}
