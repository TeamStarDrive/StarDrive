using System.IO;
using System.Text;
using Microsoft.Xna.Framework;

namespace Ship_Game.Data.Serialization.Types
{
    public class ObjectSerializer : TypeSerializer
    {
        public override string ToString() => "ObjectSerializer";
        
        public override object Convert(object value)
        {
            return value;
        }

        public override void Serialize(BinaryWriter writer, object obj)
        {
            Log.Error($"Serialize not supported for {ToString()}");
        }

        public override object Deserialize(BinaryReader reader)
        {
            Log.Error($"Deserialize not supported for {ToString()}");
            return null;
        }
    }

    internal class StringSerializer : TypeSerializer
    {
        public override string ToString() => "StringSerializer";

        public override object Convert(object value)
        {
            return value?.ToString();
        }

        public override void Serialize(BinaryWriter writer, object obj)
        {
            string value = (string)obj;
            writer.Write(value);
        }

        public override object Deserialize(BinaryReader reader)
        {
            string value = reader.ReadString();
            return value;
        }
    }

    internal class RangeSerializer : TypeSerializer
    {
        public override string ToString() => "RangeSerializer";

        public override object Convert(object value)
        {
            if (value is int i)   return new Range(i);
            if (value is float f) return new Range(f);
            if (!(value is object[] objects) || objects.Length < 2)
            {
                Error(value, "Range -- expected [float,float] or [int,int] or float or int");
                return new Range(0);
            }
            return new Range(Float(objects[0]), Float(objects[1]));
        }

        public override void Serialize(BinaryWriter writer, object obj)
        {
            var range = (Range)obj;
            writer.Write(range.Min);
            writer.Write(range.Max);
        }

        public override object Deserialize(BinaryReader reader)
        {
            Range range;
            range.Min = reader.ReadSingle();
            range.Max = reader.ReadSingle();
            return range;
        }
    }

    internal class LocTextSerializer : TypeSerializer
    {
        public override string ToString() => "LocTextSerializer";

        public override object Convert(object value)
        {
            if (value is int id)   return new LocText(id);
            if (value is string s) return new LocText(s);
            Error(value, "LocText -- expected int or format string");
            return new LocText("INVALID TEXT");
        }

        public override void Serialize(BinaryWriter writer, object obj)
        {
            var localizedText = (LocText)obj;
            writer.Write(localizedText.Text);
        }

        public override object Deserialize(BinaryReader reader)
        {
            var localizedText = new LocText(reader.ReadString(), true);
            return localizedText;
        }
    }
}
