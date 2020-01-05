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

    internal class LocalizedTextSerializer : TypeSerializer
    {
        public override string ToString() => "LocalizedTextSerializer";

        public override object Convert(object value)
        {
            if (value is int id)   return new LocalizedText(id);
            if (value is string s)
            {
                // this is sort of a pre-optimization
                // only set Parse if text contains {id} token bracket
                if (s.IndexOf('{') != -1)
                    return new LocalizedText(s, LocalizationMethod.Parse);
                return new LocalizedText(s, LocalizationMethod.RawText);
            }
            Error(value, "LocalizedText -- expected int or format string");
            return new LocalizedText("INVALID TEXT", LocalizationMethod.RawText);
        }

        public override void Serialize(BinaryWriter writer, object obj)
        {
            var localizedText = (LocalizedText)obj;
            writer.Write(localizedText.Id);
            writer.Write(localizedText.String);
            writer.Write((int)localizedText.Method);
        }

        public override object Deserialize(BinaryReader reader)
        {
            int id = reader.ReadInt32();
            string str = reader.ReadString();
            var method = (LocalizationMethod)reader.ReadInt32();

            var localizedText = new LocalizedText(id, str, method);
            return localizedText;
        }
    }
}
