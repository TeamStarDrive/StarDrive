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
    }

    internal class StringSerializer : TypeSerializer
    {
        public override string ToString() => "StringSerializer";

        public override object Convert(object value)
        {
            return value?.ToString();
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
    }
}
