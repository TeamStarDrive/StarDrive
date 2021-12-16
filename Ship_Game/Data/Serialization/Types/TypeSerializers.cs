using System;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    public class ObjectSerializer : TypeSerializer
    {
        public override string ToString() => "ObjectSerializer";
        
        public override object Convert(object value)
        {
            return value;
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            parent.Value = obj;
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

    internal class RangeSerializer : TypeSerializer
    {
        public override string ToString() => "RangeSerializer";

        public override object Convert(object value)
        {
            return ToRange(value);
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            var r = (Range)obj;
            parent.Value = new object[]{ r.Min, r.Max };
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

        public static Range ToRange(object value)
        {
            if (value is int i)   return new Range(i);
            if (value is float f) return new Range(f);

            object[] objects = value as object[];
            if (objects != null && 1 <= objects.Length && objects.Length <= 2)
            {
                if (objects.Length == 1)
                {
                    if (objects[0] is int i2)   return new Range(i2);
                    if (objects[0] is float f2) return new Range(f2);
                }
                return new Range(Float(objects[0]), Float(objects[1]));
            }
            Error(value, "Range -- expected [float,float] or [int,int] or [float] or [int] or float or int");
            return new Range(0);
        }
    }

    internal class TimeSpanSerializer : TypeSerializer
    {
        public override string ToString() => "TimeSpanSerializer";

        public override object Convert(object value)
        {
            if (value is int i)   return TimeSpan.FromSeconds(i);
            if (value is float f) return TimeSpan.FromSeconds(f);
            Error(value, "TimeSpan -- expected float or int");
            return TimeSpan.Zero;
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            var span = (TimeSpan)obj;
            parent.Value = (float)span.TotalSeconds;
        }

        public override void Serialize(BinaryWriter writer, object obj)
        {
            var span = (TimeSpan)obj;
            writer.Write(span.Ticks);
        }

        public override object Deserialize(BinaryReader reader)
        {
            return new TimeSpan(reader.ReadInt64());
        }
    }
}
