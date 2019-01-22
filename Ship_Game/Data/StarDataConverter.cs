using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Xna.Framework.Graphics;

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

            if (targetT == ColorType)
                return ToColor(value);

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

        static readonly Type ColorType = typeof(Color);

        static object ToColor(object value)
        {
            if (!(value is object[] objects))
                throw new Exception($"StarDataConverter could not convert '{value}' to Color");

            if (objects[0] is int)
            {
                byte r = 255, g = 255, b = 255, a = 255;
                if (objects.Length >= 1) r = (byte)(int)objects[0];
                if (objects.Length >= 2) g = (byte)(int)objects[1];
                if (objects.Length >= 3) b = (byte)(int)objects[2];
                if (objects.Length >= 4) a = (byte)(int)objects[3];
                return new Color(r, g, b, a);
            }
            else
            {
                float r = 1f, g = 1f, b = 1f, a = 1f;
                if (objects.Length >= 1) r = (float)objects[0];
                if (objects.Length >= 2) g = (float)objects[1];
                if (objects.Length >= 3) b = (float)objects[2];
                if (objects.Length >= 4) a = (float)objects[3];
                return new Color(r, g, b, a);
            }
        }
    }
}
