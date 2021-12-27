using System.IO;

namespace Ship_Game.Data.Binary
{
    struct BinarySerializerHeader
    {
        public byte Version;
        public byte Options;
        public ushort NumTypes;
        public ushort NumTypeGroups;
        public int RootObjectIndex;

        public BinarySerializerHeader(bool stable, BinarySerializerWriter writer)
        {
            Version = BinarySerializer.CurrentVersion;
            Options = 0;
            NumTypes = (ushort)writer.UsedTypes.Length;
            NumTypeGroups = (ushort)writer.TypeGroups.Length;
            RootObjectIndex = writer.RootObjectIndex;
            UseStableMapping = stable;
        }

        public BinarySerializerHeader(BinaryReader reader)
        {
            Version = reader.ReadByte();
            Options = reader.ReadByte();
            NumTypes = reader.ReadUInt16();
            NumTypeGroups = reader.ReadUInt16();
            RootObjectIndex = reader.ReadInt32();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)Version);
            writer.Write((byte)Options);
            writer.Write((ushort)NumTypes);
            writer.Write((ushort)NumTypeGroups);
            writer.Write((int)RootObjectIndex);
        }

        public bool UseStableMapping
        {
            get => (Options & (1 << 1)) != 0;
            set => Options = (byte)(value ? Options | (1 << 1) : Options & ~(1 << 1));
        }
    }
}
