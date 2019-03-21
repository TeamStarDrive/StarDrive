using System.IO;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    internal class BoolSerializer : TypeSerializer
    {
        public override string ToString() => "BoolSerializer";

        public override object Convert(object value)
        {
            if (value is bool) return value;
            if (value is string s)
            {
                return s == "true" || s == "True";
            }
            Error(value, "Bool -- expected string 'true' or 'false'");
            return false;
        }

        public override void Serialize(BinaryWriter writer, object obj)
        {
            bool value = (bool)obj;
            writer.Write((byte)(value ? 1 : 0));
        }

        public override object Deserialize(BinaryReader reader)
        {
            bool value = reader.ReadByte() != 0;
            return value;
        }
    }
    
    internal class ByteSerializer : TypeSerializer
    {
        public override string ToString() => "ByteSerializer";

        public override void Serialize(BinaryWriter writer, object obj)
        {
            byte value = (byte)obj;
            writer.Write(value);
        }

        public override object Deserialize(BinaryReader reader)
        {
            byte value = reader.ReadByte();
            return value;
        }
    }

    internal class ShortSerializer : TypeSerializer
    {
        public override string ToString() => "ShortSerializer";

        public override void Serialize(BinaryWriter writer, object obj)
        {
            short value = (short)obj;
            writer.Write(value);
        }

        public override object Deserialize(BinaryReader reader)
        {
            short value = reader.ReadInt16();
            return value;
        }
    }

    internal class UShortSerializer : TypeSerializer
    {
        public override string ToString() => "UShortSerializer";
        
        public override void Serialize(BinaryWriter writer, object obj)
        {
            ushort value = (ushort)obj;
            writer.Write(value);
        }

        public override object Deserialize(BinaryReader reader)
        {
            ushort value = reader.ReadUInt16();
            return value;
        }
    }

    internal class IntSerializer : TypeSerializer
    {
        public override string ToString() => "IntSerializer";

        public override object Convert(object value)
        {
            if (value is int)      return value;
            if (value is float f)  return (int)f;
            if (value is string s) return StringView.ToInt(s);
            Error(value, "Int -- expected string or float");
            return 0;
        }

        public override void Serialize(BinaryWriter writer, object obj)
        {
            int value = (int)obj;
            writer.Write(value);
        }

        public override object Deserialize(BinaryReader reader)
        {
            int value = reader.ReadInt32();
            return value;
        }
    }

    internal class UIntSerializer : TypeSerializer
    {
        public override string ToString() => "UIntSerializer";

        public override void Serialize(BinaryWriter writer, object obj)
        {
            uint value = (uint)obj;
            writer.Write(value);
        }

        public override object Deserialize(BinaryReader reader)
        {
            uint value = reader.ReadUInt32();
            return value;
        }
    }

    internal class LongSerializer : TypeSerializer
    {
        public override string ToString() => "LongSerializer";

        public override void Serialize(BinaryWriter writer, object obj)
        {
            long value = (long)obj;
            writer.Write(value);
        }

        public override object Deserialize(BinaryReader reader)
        {
            long value = reader.ReadInt64();
            return value;
        }
    }

    internal class ULongSerializer : TypeSerializer
    {
        public override string ToString() => "ULongSerializer";

        public override void Serialize(BinaryWriter writer, object obj)
        {
            ulong value = (ulong)obj;
            writer.Write(value);
        }

        public override object Deserialize(BinaryReader reader)
        {
            ulong value = reader.ReadUInt64();
            return value;
        }
    }
}
