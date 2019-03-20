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
            Error(value, "Float -- expected int or float");
            return 0f;
        }

        public static byte Byte(object value)
        {
            if (value is int i)   return (byte)i;
            if (value is float f) return (byte)(int)f;
            Error(value, "Byte -- expected int or float");
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
                (typeof(object),  new ObjectConverter()),
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

        TypeConverter AddStarDataSerializer(Type mappedType, Type underlyingType)
        {
            // @note We can have recursive deserialization, so resolving must happen in several steps
            // 1. create serializer instance
            var serializer = new StarDataSerializer(underlyingType);
            // 2. record the mapping reference, so ResolveTypes can see this instance
            Add(mappedType, serializer);
            return serializer;
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

            Type listElemType = GetListType(type);
            if (listElemType != null)
                return Add(type, new ArrayListConverter(listElemType, Get(listElemType)));

            if (type.IsEnum)
                return Add(type, new EnumConverter(type));

            if (type.GetCustomAttribute<StarDataTypeAttribute>() != null)
                return AddStarDataSerializer(type, type);

            // Nullable<T>, ex: `[StarData] Color? MinColor;`
            Type nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
                return Add(type, Get(nullableType));

            throw new InvalidDataException($"Unsupported type {type} - is the class missing [StarDataType] attribute?");
        }

        TypeConverter Add(Type type, TypeConverter converter)
        {
            Types[type] = converter;
            return converter;
        }
    }

    public abstract class TypeConverter
    {
        public virtual object Convert(object value)
        {
            Log.Error($"Direct Convert not supported for {ToString()}. Value: {value}");
            return null;
        }

        public virtual object Deserialize(YamlNode node)
        {
            object value = node.Value;
            if (value == null)
                return null;
            return Convert(value);
        }
    }

    public class RawArrayConverter : TypeConverter
    {
        readonly Type ElemType;
        public readonly TypeConverter Converter;
        public override string ToString() => $"RawArrayConverter {ElemType.GenericName()}";

        public RawArrayConverter(Type elemType, TypeConverter converter)
        {
            ElemType = elemType;
            Converter = converter;
        }

        public override object Convert(object value)
        {
            if (value == null)
                return null;

            if (value is object[] array)
            {
                Array converted = Array.CreateInstance(ElemType, array.Length);
                for (int i = 0; i < array.Length; ++i)
                {
                    converted.SetValue(Converter.Convert(array[i]), i);
                }
                return converted;
            }

            ConvertTo.Error(value, "Array convert failed -- expected a list of values [*, *, *]");
            return value;
        }

        public override object Deserialize(YamlNode node)
        {
            // [StarData] Ship[] Ships;
            // Ships: my_ships
            //   - Ship: ship1
            //     Position: ...
            //   - Ship: ship2
            //     Position: ...
            var nodes = node.HasSequence ? node.Sequence :
                        node.HasSubNodes ? node.Nodes : null;
            if (nodes?.Count > 0)
            {
                Array converted = Array.CreateInstance(ElemType, nodes.Count);
                for (int i = 0; i < nodes.Count; ++i)
                {
                    converted.SetValue(Converter.Deserialize(nodes[i]), i);
                }
                return converted;
            }

            return base.Deserialize(node); // try to deserialize value as Array
        }
    }

    public class ArrayListConverter : TypeConverter
    {
        readonly Type ElemType;
        public readonly TypeConverter Converter;
        public override string ToString() => $"ArrayListConverter {ElemType.GenericName()}";

        public ArrayListConverter(Type elemType, TypeConverter converter)
        {
            ElemType = elemType;
            Converter = converter;
        }

        public override object Convert(object value)
        {
            if (value == null)
                return null;

            if (value is object[] array)
            {
                IList list = ArrayHelper.NewArrayOfT(ElemType);
                for (int i = 0; i < array.Length; ++i)
                {
                    list.Add(Converter.Convert(array[i]));
                }
                return list;
            }

            ConvertTo.Error(value, "Array convert failed -- expected a list of values [*, *, *]");
            return value;
        }

        public override object Deserialize(YamlNode node)
        {
            // [StarData] Array<Ship> Ships;
            // Ships: my_ships
            //   - Ship: ship1
            //     Position: ...
            //   - Ship: ship2
            //     Position: ...
            var nodes = node.HasSequence ? node.Sequence :
                        node.HasSubNodes ? node.Nodes : null;
            if (nodes?.Count > 0)
            {
                IList list = ArrayHelper.NewArrayOfT(ElemType);
                for (int i = 0; i < nodes.Count; ++i)
                {
                    list.Add(Converter.Deserialize(nodes[i]));
                }
                return list;
            }

            return base.Deserialize(node); // try to deserialize value as Array
        }
    }

    public class EnumConverter : TypeConverter
    {
        readonly Type ToEnum;
        public override string ToString() => $"EnumConverter {ToEnum.GenericName()}";

        public EnumConverter(Type enumType)
        {
            ToEnum = enumType;
        }
        public override object Convert(object value)
        {
            try
            {
                if (value is string enumLiteral)
                    return Enum.Parse(ToEnum, enumLiteral, ignoreCase:true);
                if (value is int enumIndex)
                    return Enum.ToObject(ToEnum, enumIndex);
                ConvertTo.Error(value, $"Enum '{ToEnum.Name}' -- expected a string or int");
            }
            catch (Exception e)
            {
                ConvertTo.Error(value, $"Enum '{ToEnum.Name}' -- {e.Message}");
            }
            return ToEnum.GetEnumValues().GetValue(0);
        }
    }

    public class ObjectConverter : TypeConverter
    {
        public override string ToString() => "ObjectConverter";

        public override object Convert(object value)
        {
            return value;
        }
    }

    public class RangeConverter : TypeConverter
    {
        public override string ToString() => "RangeConverter";

        static float Number(object value)
        {
            if (value is float f) return f;
            if (value is int i)   return i;
            if (value is string s) return StringView.ToFloat(s);
            ConvertTo.Error(value, "Float -- expected string or int");
            return 0f;
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
        public override string ToString() => "LocTextConverter";

        public override object Convert(object value)
        {
            if (value is int id)   return new LocText(id);
            if (value is string s) return new LocText(s);
            ConvertTo.Error(value, "LocText -- expected int or format string");
            return new LocText("INVALID TEXT");
        }
    }
    
    public class ColorConverter : TypeConverter
    {
        public override string ToString() => "ColorConverter";

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
        public override string ToString() => "IntConverter";

        public override object Convert(object value)
        {
            if (value is int)      return value;
            if (value is float f)  return (int)f;
            if (value is string s) return StringView.ToInt(s);
            ConvertTo.Error(value, "Int -- expected string or float");
            return 0;
        }
    }

    public class FloatConverter : TypeConverter
    {
        public override string ToString() => "FloatConverter";

        public override object Convert(object value)
        {
            if (value is float)    return value;
            if (value is int i)    return (float)i;
            if (value is string s) return StringView.ToFloat(s);
            ConvertTo.Error(value, "Float -- expected string or int");
            return 0f;
        }
    }

    public class BoolConverter : TypeConverter
    {
        public override string ToString() => "BoolConverter";

        public override object Convert(object value)
        {
            if (value is bool) return value;
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
        public override string ToString() => "StringConverter";

        public override object Convert(object value)
        {
            return value?.ToString();
        }
    }

    public class Vector2Converter : TypeConverter
    {
        public override string ToString() => "Vector2Converter";

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
        public override string ToString() => "Vector3Converter";

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
        public override string ToString() => "Vector4Converter";

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
