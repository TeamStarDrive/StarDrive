using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Data
{
    public class StartDataParseError : Exception
    {
        public StartDataParseError(object value, string couldNotConvertToWhat)
            : base($"StarDataConverter could not convert '{value}' ({value?.GetType()}) to {couldNotConvertToWhat}")
        {
            
        }
    }

    internal static class ConvertTo
    {
        public static float Float(object value)
        {
            if (value is int i)   return i;
            if (value is float f) return f;
            throw new StartDataParseError(value, "Float -- expected float");
        }

        public static byte Byte(object value)
        {
            if (value is int i)   return (byte)i;
            if (value is float f) return (byte)(int)f;
            throw new StartDataParseError(value, "Byte -- expected float");
        }

        public static Map<Type, TypeConverter> CreateDefaultConverters()
        {
            return new Map<Type, TypeConverter>
            {
                (typeof(Range),   new RangeConverter()),
                (typeof(LocText), new LocTextConverter()),
                (typeof(Color),   new ColorConverter()),
                (typeof(int),     new IntConverter()),
                (typeof(float),   new FloatConverter()),
                (typeof(bool),    new BoolConverter()),
                (typeof(string),  new StringConverter()),
                (typeof(Vector2), new Vector2Converter()),
                (typeof(Vector3), new Vector3Converter()),
                (typeof(Vector4), new Vector4Converter())
            };
        }
    }


    public abstract class TypeConverter
    {
        public abstract object Convert(object value);
    }

    public class EnumConverter : TypeConverter
    {
        readonly Type ToEnum;
        public EnumConverter(Type enumType)
        {
            ToEnum = enumType;
        }
        public override object Convert(object value)
        {
            if (value is string s)
                return Enum.Parse(ToEnum, s, ignoreCase:true);
            if (value is int i)
                return Enum.ToObject(ToEnum, i);
            throw new StartDataParseError(value, $"Enum '{ToEnum.Name}' -- expected a string or int");
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
        public override object Convert(object value)
        {
            if (value is int i)   return new Range(i);
            if (value is float f) return new Range(f);
            if (!(value is object[] objects) || objects.Length < 2)
                throw new StartDataParseError(value, "Range -- expected [float,float] or [int,int] or float or int");
            return new Range(Number(objects[0]), Number(objects[1]));
        }
    }

    public class LocTextConverter : TypeConverter
    {
        public override object Convert(object value)
        {
            if (value is int id)
                return new LocText(id);

            if (value is string s)
                return new LocText(s);

            throw new StartDataParseError(value, "LocText -- expected int or format string");
        }
    }
    
    public class ColorConverter : TypeConverter
    {
        public override object Convert(object value)
        {
            if (value is object[] objects)
            {
                if (objects[0] is int)
                {
                    byte r = 255, g = 255, b = 255, a = 255;
                    if (objects.Length >= 1) r = ConvertTo.Byte(objects[0]);
                    if (objects.Length >= 2) g = ConvertTo.Byte(objects[1]);
                    if (objects.Length >= 3) b = ConvertTo.Byte(objects[2]);
                    if (objects.Length >= 4) a = ConvertTo.Byte(objects[3]);
                    return new Color(r, g, b, a);
                }
                else
                {
                    float r = 1f, g = 1f, b = 1f, a = 1f;
                    if (objects.Length >= 1) r = ConvertTo.Float(objects[0]);
                    if (objects.Length >= 2) g = ConvertTo.Float(objects[1]);
                    if (objects.Length >= 3) b = ConvertTo.Float(objects[2]);
                    if (objects.Length >= 4) a = ConvertTo.Float(objects[3]);
                    return new Color(r, g, b, a);
                }
            }
            if (value is int i) // short hand to get [i,i,i,i]
            {
                i = i.Clamped(0, 255);
                return new Color((byte)i, (byte)i, (byte)i, (byte)i);
            }
            if (value is float f) // short hand to get [f,f,f,f]
            {
                f = f.Clamped(0f, 1f);
                return new Color(f, f, f, f);
            }
            throw new StartDataParseError(value, "Color -- expected [int,int,int,int] or [float,float,float,float] or int or number");
        }
    }

    public class IntConverter : TypeConverter
    {
        public override object Convert(object value)
        {
            if (value is string s)
            {
                int.TryParse(s, out int i);
                return i;
            }

            if (value is float f)
                return (int)f;

            throw new StartDataParseError(value, "Int -- expected string or float");
        }
    }

    public class FloatConverter : TypeConverter
    {
        public override object Convert(object value)
        {
            if (value is string s)
            {
                float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float f);
                return f;
            }

            if (value is int i)
                return (float)i;
            
            throw new StartDataParseError(value, "Float -- expected string or int");
        }
    }

    public class BoolConverter : TypeConverter
    {
        public override object Convert(object value)
        {
            if (value is string s)
            {
                return s == "true" || s == "True";
            }
            
            throw new StartDataParseError(value, "Bool -- expected string 'true' or 'false'");
        }
    }

    public class StringConverter : TypeConverter
    {
        public override object Convert(object value)
        {
            return value.ToString();
        }
    }

    public class Vector2Converter : TypeConverter
    {
        public override object Convert(object value)
        {
            if (value is object[] objects)
            {
                Vector2 v = default;
                if (objects.Length >= 1) v.X = ConvertTo.Float(objects[0]);
                if (objects.Length >= 2) v.Y = ConvertTo.Float(objects[1]);
                return v;
            }

            throw new StartDataParseError(value, "Vector2 -- expected [float,float]");
        }
    }

    public class Vector3Converter : TypeConverter
    {
        public override object Convert(object value)
        {
            if (value is object[] objects)
            {
                Vector3 v = default;
                if (objects.Length >= 1) v.X = ConvertTo.Float(objects[0]);
                if (objects.Length >= 2) v.Y = ConvertTo.Float(objects[1]);
                if (objects.Length >= 3) v.Z = ConvertTo.Float(objects[2]);
                return v;
            }

            throw new StartDataParseError(value, "Vector3 -- expected [float,float,float]");
        }
    }

    public class Vector4Converter : TypeConverter
    {
        public override object Convert(object value)
        {
            if (value is object[] objects)
            {
                Vector4 v = default;
                if (objects.Length >= 1) v.X = ConvertTo.Float(objects[0]);
                if (objects.Length >= 2) v.Y = ConvertTo.Float(objects[1]);
                if (objects.Length >= 3) v.Z = ConvertTo.Float(objects[2]);
                if (objects.Length >= 4) v.W = ConvertTo.Float(objects[3]);
                return v;
            }

            throw new StartDataParseError(value, "Vector4 -- expected [float,float,float,float]");
        }
    }

}
