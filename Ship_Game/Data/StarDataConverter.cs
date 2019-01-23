using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Data
{
    public abstract class TypeConverter
    {
        public abstract object Convert(object value, Type source);
    }

    public class EnumConverter : TypeConverter
    {
        readonly Type ToEnum;
        public EnumConverter(Type enumType)
        {
            ToEnum = enumType;
        }
        public override object Convert(object value, Type source)
        {
            if (value is string s)
                return Enum.Parse(ToEnum, s, ignoreCase:true);
            if (value is int i)
                return Enum.ToObject(ToEnum, i);
            throw new Exception($"StarDataConverter could not convert '{value}' to Enum '{ToEnum.Name}'");
        }
    }

    public class RangeConverter : TypeConverter
    {
        static float Number(object value)
        {
            if (value is float f) return f;
            if (value is int i) return i;
            return float.Parse((string)value, CultureInfo.InvariantCulture);
        }
        public override object Convert(object value, Type source)
        {
            if (value is int i)   return new Range(i);
            if (value is float f) return new Range(f);
            if (!(value is object[] objects) || objects.Length < 2)
                throw new Exception($"StarDataConverter could not convert '{value}' to Range");
            return new Range(Number(objects[0]), Number(objects[1]));
        }
    }

    public class LocTextConverter : TypeConverter
    {
        public override object Convert(object value, Type source)
        {
            if (!(value is int id))
                throw new Exception($"StarDataConverter could not convert '{value}' to LocText");
            return new LocText(id);
        }
    }

    public class ColorConverter : TypeConverter
    {
        public override object Convert(object value, Type source)
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

    public class IntConverter : TypeConverter
    {
        public override object Convert(object value, Type source)
        {
            if (value is string s)
            {
                int.TryParse(s, out int i);
                return i;
            }

            if (value is float f)
                return (int)f;

            throw new Exception($"StarDataConverter could not convert '{value}' to Int");
        }
    }

    public class FloatConverter : TypeConverter
    {
        public override object Convert(object value, Type source)
        {
            if (value is string s)
            {
                float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float f);
                return f;
            }

            if (value is int i)
                return (float)i;

            throw new Exception($"StarDataConverter could not convert '{value}' to Float");
        }
    }

    public class BoolConverter : TypeConverter
    {
        public override object Convert(object value, Type source)
        {
            if (value is string s)
            {
                return s == "true" || s == "True";
            }

            throw new Exception($"StarDataConverter could not convert '{value}' to Bool");
        }
    }

    public class StringConverter : TypeConverter
    {
        public override object Convert(object value, Type source)
        {
            return value.ToString();
        }
    }

    public class DefaultConverter : TypeConverter
    {
        readonly Type ToType;
        public DefaultConverter(Type toType)
        {
            ToType = toType;
        }
        public override object Convert(object value, Type source)
        {
            return System.Convert.ChangeType(value, ToType);
        }
    }

    public static class StarDataConverter
    {
        public static object Convert(object value, Type targetT)
        {
            return System.Convert.ChangeType(value, targetT);
        }
    }
}
