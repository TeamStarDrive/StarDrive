using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace Ship_Game.Data
{
    public static class StarDataConverter
    {
        public static object Convert(object value, Type targetT)
        {
            if (value == null)
                return null;

            Type sourceT = value.GetType();
            if (sourceT == targetT)
                return value;

            if (targetT.IsEnum)
                return ToEnum(value, targetT);

            if (targetT == RangeType)
                return ToRange(value);

            if (targetT == LocTextType)
                return ToLocText(value);

            return System.Convert.ChangeType(value, targetT);
        }

        static object ToEnum(object value, Type targetT)
        {
            if (value is string s)
                return Enum.Parse(targetT, s, ignoreCase:true);
            if (value is int i)
                return Enum.ToObject(targetT, i);
            throw new Exception($"StarDataConverter could not convert '{value}' to Enum '{targetT.Name}'");
        }

        static float Number(object value)
        {
            if (value is float f) return f;
            if (value is int i) return i;
            return float.Parse((string)value, CultureInfo.InvariantCulture);
        }

        static readonly Type RangeType = typeof(Range);

        static object ToRange(object value)
        {
            if (value is int i)   return new Range(i);
            if (value is float f) return new Range(f);
            if (!(value is object[] objects) || objects.Length < 2)
                throw new Exception($"StarDataConverter could not convert '{value}' to Range");
            return new Range(Number(objects[0]), Number(objects[1]));
        }

        static readonly Type LocTextType = typeof(LocText);

        static object ToLocText(object value)
        {
            if (!(value is int id))
                throw new Exception($"StarDataConverter could not convert '{value}' to LocText");
            return new LocText(id);
        }
    }
}
