using System;
using System.IO;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    internal class BoolSerializer : TypeSerializer
    {
        public BoolSerializer() : base(typeof(bool)) { }
        public override string ToString() => $"BoolSerializer:{TypeId}";

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

        public override void Serialize(YamlNode parent, object obj)
        {
            parent.Value = obj;
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            bool value = (bool)obj;
            writer.BW.Write((byte)(value ? 1 : 0));
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            bool value = reader.BR.ReadByte() != 0;
            return value;
        }
    }
    
    internal class ByteSerializer : TypeSerializer
    {
        public ByteSerializer() : base(typeof(byte)) { }
        public override string ToString() => $"ByteSerializer:{TypeId}";

        public override void Serialize(YamlNode parent, object obj)
        {
            parent.Value = (int)(byte)obj;
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            byte value = (byte)obj;
            writer.BW.Write(value);
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            byte value = reader.BR.ReadByte();
            return value;
        }
    }

    internal class SByteSerializer : TypeSerializer
    {
        public SByteSerializer() : base(typeof(sbyte)) { }
        public override string ToString() => $"SByteSerializer:{TypeId}";

        public override void Serialize(YamlNode parent, object obj)
        {
            parent.Value = (int)(sbyte)obj;
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            sbyte value = (sbyte)obj;
            writer.BW.Write(value);
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            sbyte value = reader.BR.ReadSByte();
            return value;
        }
    }

    internal class ShortSerializer : TypeSerializer
    {
        public ShortSerializer() : base(typeof(short)) { }
        public override string ToString() => $"ShortSerializer:{TypeId}";

        public override void Serialize(YamlNode parent, object obj)
        {
            parent.Value = (int)(short)obj;
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            short value = (short)obj;
            writer.BW.WriteVLi32(value);
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            short value = (short)reader.BR.ReadVLi32();
            return value;
        }
    }

    internal class UShortSerializer : TypeSerializer
    {
        public UShortSerializer() : base(typeof(ushort)) { }
        public override string ToString() => $"UShortSerializer:{TypeId}";

        public override void Serialize(YamlNode parent, object obj)
        {
            parent.Value = (int)(ushort)obj;
        }
        
        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            ushort value = (ushort)obj;
            writer.BW.WriteVLu32(value);
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            ushort value = (ushort)reader.BR.ReadVLu32();
            return value;
        }
    }

    internal class IntSerializer : TypeSerializer
    {
        public IntSerializer() : base(typeof(int)) { }
        public override string ToString() => $"IntSerializer:{TypeId}";

        public override object Convert(object value)
        {
            if (value is int)      return value;
            if (value is float f)  return (int)f;
            if (value is string s) return StringView.ToInt(s);
            Error(value, "Int -- expected string or float");
            return 0;
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            parent.Value = obj;
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            int value = (int)obj;
            writer.BW.WriteVLi32(value);
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            int value = reader.BR.ReadVLi32();
            return value;
        }
    }

    internal class UIntSerializer : TypeSerializer
    {
        public UIntSerializer() : base(typeof(uint)) { }
        public override string ToString() => $"UIntSerializer:{TypeId}";

        public override void Serialize(YamlNode parent, object obj)
        {
            parent.Value = (int)(uint)obj;
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            uint value = (uint)obj;
            writer.BW.WriteVLu32(value);
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            uint value = reader.BR.ReadVLu32();
            return value;
        }
    }

    internal class LongSerializer : TypeSerializer
    {
        public LongSerializer() : base(typeof(long)) { }
        public override string ToString() => $"LongSerializer:{TypeId}";

        public override void Serialize(YamlNode parent, object obj)
        {
            long value = (long)obj;
            parent.Value = value.ToString();
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            long value = (long)obj;
            writer.BW.WriteVLi64(value);
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            long value = reader.BR.ReadVLi64();
            return value;
        }
    }

    internal class ULongSerializer : TypeSerializer
    {
        public ULongSerializer() : base(typeof(ulong)) { }
        public override string ToString() => $"ULongSerializer:{TypeId}";

        public override void Serialize(YamlNode parent, object obj)
        {
            ulong value = (ulong)obj;
            parent.Value = value.ToString();
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            ulong value = (ulong)obj;
            writer.BW.WriteVLu64(value);
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            ulong value = reader.BR.ReadVLu64();
            return value;
        }
    }
}
