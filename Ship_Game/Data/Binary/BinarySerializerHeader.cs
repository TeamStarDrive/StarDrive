using System.IO;

namespace Ship_Game.Data.Binary
{
    struct BinarySerializerHeader
    {
        public byte Version;
        public byte Options;
        public ushort NumTypes;
        public int NumObjects;
        public uint StreamSize;

        public BinarySerializerHeader(BinaryReader reader)
        {
            Version = reader.ReadByte();
            Options = reader.ReadByte();
            NumTypes   = reader.ReadUInt16();
            NumObjects = reader.ReadInt32();
            StreamSize = reader.ReadUInt32();
        }

        public BinarySerializerHeader(bool stable, int numTypes, int numObjects)
        {
            Version = BinarySerializer.CurrentVersion;
            Options = 0;
            NumTypes = (ushort)numTypes;
            NumObjects = numObjects;
            StreamSize = 1 + 1 + 4 + 4 + 4;
            UseStableMapping = stable;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)Version);
            writer.Write((byte)Options);
            writer.Write((ushort)NumTypes);
            writer.Write((int)NumObjects);
            writer.Write((uint)StreamSize);
        }

        public bool UseStableMapping
        {
            get => (Options & (1 << 1)) != 0;
            set => Options = (byte)(value ? Options | (1 << 1) : Options & ~(1 << 1));
        }
    }
}
