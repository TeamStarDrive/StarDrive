using System;
using System.IO;
using SDGraphics;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Yaml;
using Ship_Game.Utils;

namespace Ship_Game.Data.Serialization.Types
{
    public class ObjectSerializer : TypeSerializer
    {
        public ObjectSerializer() : base(typeof(object)) { }
        public override string ToString() => $"{TypeId}:ObjectSerializer";
        
        public override object Convert(object value)
        {
            return value;
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            parent.Value = obj;
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            Log.Error($"Serialize(binary) not supported for {ToString()}");
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            Log.Error($"Deserialize(binary) not supported for {ToString()}");
            return null;
        }
    }

    internal class RangeSerializer : TypeSerializer
    {
        public RangeSerializer() : base(typeof(Range)) { }
        public override string ToString() => $"{TypeId}:RangeSerializer";

        public override object Convert(object value)
        {
            return ToRange(value);
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            var r = (Range)obj;
            parent.Value = new object[]{ r.Min, r.Max };
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            var range = (Range)obj;
            writer.BW.Write(range.Min);
            writer.BW.Write(range.Max);
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            Range range;
            range.Min = reader.BR.ReadSingle();
            range.Max = reader.BR.ReadSingle();
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

    // UTC DateTime Serializer
    internal class DateTimeSerializer : TypeSerializer
    {
        public DateTimeSerializer() : base(typeof(DateTime)) { }
        public override string ToString() => $"{TypeId}:DateTimeSerializer";

        public override object Convert(object value)
        {
            if (value is string s)
            {
                string[] parts = s.Split(' ');
                if (parts.Length != 2)
                {
                    Error(value, "DateTime -- expected string 'yyyy-mm-dd hh:mm:ss' separated by a space");
                    return DateTime.MinValue;
                }

                string[] date = parts[0].Split('-');
                if (date.Length != 3)
                {
                    Error(value, "DateTime -- expected string 'yyyy-mm-dd hh:mm:ss' with 3 date components separated by dashes -");
                    return DateTime.MinValue;
                }

                string[] time = parts[1].Split(':');
                if (time.Length != 3)
                {
                    Error(value, "DateTime -- expected string 'yyyy-mm-dd hh:mm:ss' with 3 timeofday components separated by colons :");
                    return DateTime.MinValue;
                }

                if (!int.TryParse(date[0], out int year) ||
                    !int.TryParse(date[1], out int month) ||
                    !int.TryParse(date[2], out int day) ||
                    !int.TryParse(time[0], out int hour) ||
                    !int.TryParse(time[1], out int minute) ||
                    !int.TryParse(time[2], out int second))
                {
                    Error(value, "DateTime -- expected string 'yyyy-mm-dd hh:mm:ss' with all components being integers");
                    return DateTime.MinValue;
                }

                return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
            }
            if (value is long l) return new DateTime(l, DateTimeKind.Utc);
            if (value is int i) return new DateTime(i, DateTimeKind.Utc);
            Error(value, "DateTime -- expected string 'yyyy-mm-dd hh:mm:ss' or long ticks or int ticks");
            return DateTime.MinValue;
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            var dt = (DateTime)obj;
            var tod = dt.TimeOfDay;
            // 'yyyy-mm-dd hh:mm:ss'
            parent.Value = $"{dt.Year,4}-{dt.Month,2}-{dt.Day} {tod.Hours,2}:{tod.Minutes,2}:{tod.Seconds}";
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            var dt = (DateTime)obj;
            writer.BW.Write(dt.Ticks);
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            return new DateTime(reader.BR.ReadInt64(), DateTimeKind.Utc);
        }
    }

    internal class TimeSpanSerializer : TypeSerializer
    {
        public TimeSpanSerializer() : base(typeof(TimeSpan)) { }
        public override string ToString() => $"{TypeId}:TimeSpanSerializer";

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

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            var span = (TimeSpan)obj;
            writer.BW.WriteVLi64(span.Ticks);
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            return new TimeSpan(reader.BR.ReadVLi64());
        }
    }

    internal class SmallBitSetSerializer : TypeSerializer
    {
        public SmallBitSetSerializer() : base(typeof(SmallBitSet)) { }
        public override string ToString() => $"{TypeId}:SmallBitSetSerializer";

        public override object Convert(object value)
        {
            throw new NotSupportedException();
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            throw new NotSupportedException();
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            var set = (SmallBitSet)obj;
            writer.BW.WriteVLu32(set.Values);
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            return new SmallBitSet{ Values = reader.BR.ReadVLu32() };
        }
    }

    internal class BitArraySerializer : TypeSerializer
    {
        public BitArraySerializer() : base(typeof(BitArray)) { }
        public override string ToString() => $"{TypeId}:BitArraySerializer";

        public override object Convert(object value)
        {
            throw new NotSupportedException();
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            throw new NotSupportedException();
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            uint[] values = ((BitArray)obj).Values;
            if (values == null)
            {
                writer.BW.WriteVLu32(0);
            }
            else
            {
                writer.BW.WriteVLu32((uint)values.Length);
                for (int i = 0; i < values.Length; ++i)
                    writer.BW.WriteVLu32(values[i]);
            }
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            int length = (int)reader.BR.ReadVLu32();
            if (length == 0)
                return new BitArray(0);

            uint[] values = new uint[length];
            for (int i = 0; i < length; ++i)
                values[i] = reader.BR.ReadVLu32();

            return new BitArray(values);
        }
    }
}
