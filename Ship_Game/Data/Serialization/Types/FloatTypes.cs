using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    internal class FloatSerializer : TypeSerializer
    {
        public override string ToString() => "FloatSerializer";

        public override object Convert(object value)
        {
            if (value is float f)  return f;
            if (value is int i)    return (float)i;
            if (value is string s) return StringView.ToFloat(s);
            Error(value, "Float -- expected int or float or string");
            return 0.0f;
        }

        public override void Serialize(BinaryWriter writer, object obj)
        {
            float value = (float)obj;
            writer.Write(value);
        }

        public override object Deserialize(BinaryReader reader)
        {
            float value = reader.ReadSingle();
            return value;
        }
    }

    internal class DoubleSerializer : TypeSerializer
    {
        public override string ToString() => "DoubleSerializer";

        public override object Convert(object value)
        {
            if (value is float f)  return (double)f;
            if (value is int i)    return (double)i;
            if (value is string s) return StringView.ToDouble(s);
            Error(value, "Double -- expected int or float or string");
            return 0.0;
        }

        public override void Serialize(BinaryWriter writer, object obj)
        {
            double value = (double)obj;
            writer.Write(value);
        }

        public override object Deserialize(BinaryReader reader)
        {
            double value = reader.ReadDouble();
            return value;
        }
    }
}
