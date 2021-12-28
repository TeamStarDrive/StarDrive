using System.IO;

namespace Ship_Game.Data.Binary
{
    public struct BinarySerializerHeader
    {
        public uint Version;
        public uint Options;
        public uint NumTypes;
        public uint NumTypeGroups;
        public uint RootObjectIndex;

        public BinarySerializerHeader(bool stable, BinarySerializerWriter writer)
        {
            Version = BinarySerializer.CurrentVersion;
            Options = 0;
            NumTypes = (uint)writer.UsedTypes.Length;
            NumTypeGroups = (uint)writer.TypeGroups.Length;
            RootObjectIndex = writer.RootObjectIndex;
            UseStableMapping = stable;
        }

        public BinarySerializerHeader(BinaryReader reader)
        {
            Version = reader.ReadVLu32();
            Options = reader.ReadVLu32();
            NumTypes = reader.ReadVLu32();
            NumTypeGroups = reader.ReadVLu32();
            RootObjectIndex = reader.ReadVLu32();
        }

        public void Write(BinaryWriter writer)
        {
            writer.WriteVLu32(Version);
            writer.WriteVLu32(Options);
            writer.WriteVLu32(NumTypes);
            writer.WriteVLu32(NumTypeGroups);
            writer.WriteVLu32(RootObjectIndex);
        }

        public bool UseStableMapping
        {
            get => (Options & (1 << 1)) != 0;
            set => Options = (byte)(value ? Options | (1 << 1) : Options & ~(1 << 1));
        }
    }
}
