using System;
using System.IO;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    internal class FloatSerializer : TypeSerializer
    {
        public FloatSerializer() : base(typeof(float)) { }
        public override string ToString() => $"{TypeId}:FloatSerializer";

        public override object Convert(object value)
        {
            return ToFloat(value);
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            parent.Value = obj;
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            float value = (float)obj;
            writer.BW.Write(value);
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            float value = reader.BR.ReadSingle();
            return value;
        }

        public static float ToFloat(object value)
        {
            if (value is float f) return f;
            if (value is int i) return (float)i;
            if (value is string s) return StringView.ToFloat(s);
            Error(value, "Float -- expected int or float or string");
            return 0.0f;
        }
    }

    internal class DoubleSerializer : TypeSerializer
    {
        public DoubleSerializer() : base(typeof(double)) { }
        public override string ToString() => $"{TypeId}:DoubleSerializer";

        public override object Convert(object value)
        {
            if (value is float f)  return (double)f;
            if (value is int i)    return (double)i;
            if (value is string s) return StringView.ToDouble(s);
            Error(value, "Double -- expected int or float or string");
            return 0.0;
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            parent.Value = obj;
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            double value = (double)obj;
            writer.BW.Write(value);
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            double value = reader.BR.ReadDouble();
            return value;
        }
    }
}
