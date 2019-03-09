using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Data
{
    internal static class ConvertTo
    {
        public static void Error(object value, string couldNotConvertToWhat)
        {
            string e = $"StarDataConverter could not convert '{value}' ({value?.GetType()}) to {couldNotConvertToWhat}";
            Log.Error(e);
        }

        public static float Float(object value)
        {
            if (value is int i)   return i;
            if (value is float f) return f;
            Error(value, "Float -- expected float");
            return 0f;
        }

        public static byte Byte(object value)
        {
            if (value is int i)   return (byte)i;
            if (value is float f) return (byte)(int)f;
            Error(value, "Byte -- expected float");
            return 0;
        }
    }
    
    // type mapping cache for converters
    internal class Converters
    {
        readonly Map<Type, TypeConverter> Types;

        public Converters()
        {
            Types = new Map<Type, TypeConverter>
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

        public bool IsListType(Type type)
        {
            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(Array<>))
                    return true;
                if (type.GetInterfaces().Contains(typeof(IList)))
                    return true;
            }
            return false;
        }

        public Type GetListType(Type type)
        {
            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(Array<>))
                    return type.GenericTypeArguments[0];
                if (type.GetInterfaces().Contains(typeof(IList)))
                    return type.GenericTypeArguments[0];
            }
            return null;
        }

        public TypeConverter Get(Type type)
        {
            if (Types.TryGetValue(type, out TypeConverter converter))
                return converter;

            if (type.IsArray)
            {
                Type elemType = type.GetElementType();
                return Add(type, new RawArrayConverter(elemType, Get(elemType)));
            }

            if (IsListType(type))
                return Add(type, new StarDataSerializer(type.GenericTypeArguments[0], this));
            
            if (type.IsEnum)
                return Add(type, new EnumConverter(type));

            if (type.GetCustomAttribute<StarDataTypeAttribute>() != null)
                return Add(type, new StarDataSerializer(type, this));

            // Nullable<T>, ex: `[StarData] Color? MinColor;`
            Type nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
                return Add(type, Get(nullableType));

            throw new InvalidDataException($"Unsupported type {type} - is it missing [StarDataType] attribute?");
        }

        TypeConverter Add(Type type, TypeConverter converter)
        {
            Types[type] = converter;
            return converter;
        }
    }


    public abstract class TypeConverter
    {
        public abstract object Convert(object value);
    }

    public class RawArrayConverter : TypeConverter
    {
        readonly Type ElemType;
        readonly TypeConverter Converter;
        public RawArrayConverter(Type elemType, TypeConverter converter)
        {
            ElemType = elemType;
            Converter = converter;
        }
        public override object Convert(object value)
        {
            if (value == null)
                return null;

            Array converted;
            if (value is Array<StarDataNode> sequence)
            {
                converted = Array.CreateInstance(ElemType, sequence.Count);
                for (int i = 0; i < sequence.Count; ++i)
                {
                    converted.SetValue(Converter.Convert(sequence[i].Value), i);
                }
                return converted;
            }

            if (value is object[] array)
            {
                converted = Array.CreateInstance(ElemType, array.Length);
                for (int i = 0; i < array.Length; ++i)
                {
                    converted.SetValue(Converter.Convert(array[i]), i);
                }
                return converted;
            }

            ConvertTo.Error(value, "Array convert failed -- expected a list of values [*, *, *]");
            return value;
        }
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

            ConvertTo.Error(value, $"Enum '{ToEnum.Name}' -- expected a string or int");
            return ToEnum.GetEnumValues().GetValue(0);
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
            {
                ConvertTo.Error(value, "Range -- expected [float,float] or [int,int] or float or int");
                return new Range(0);
            }
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

            ConvertTo.Error(value, "LocText -- expected int or format string");
            return new LocText("INVALID TEXT");
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
            
            ConvertTo.Error(value, "Color -- expected [int,int,int,int] or [float,float,float,float] or int or number");
            return Color.Red;
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

            ConvertTo.Error(value, "Int -- expected string or float");
            return 0;
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

            if (value is int i)    return (float)i;
            if (value is float ff) return ff;
            
            ConvertTo.Error(value, "Float -- expected string or int");
            return 0.0f;
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
            
            ConvertTo.Error(value, "Bool -- expected string 'true' or 'false'");
            return false;
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
            
            ConvertTo.Error(value, "Vector2 -- expected [float,float]");
            return Vector2.Zero;
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
            
            ConvertTo.Error(value, "Vector3 -- expected [float,float,float]");
            return Vector3.Zero;
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
            
            ConvertTo.Error(value, "Vector4 -- expected [float,float,float,float]");
            return Vector4.Zero;
        }
    }

}
