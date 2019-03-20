using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Data.YamlSerializer
{
    // type mapping cache for converters
    class YamlConverters
    {
        readonly Map<Type, YamlConverter> Types;

        public YamlConverters()
        {
            Types = new Map<Type, YamlConverter>
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

        YamlConverter AddStarDataSerializer(Type mappedType, Type underlyingType)
        {
            // @note We can have recursive deserialization, so resolving must happen in several steps
            // 1. create serializer instance
            var serializer = new YamlSerializer(underlyingType);
            // 2. record the mapping reference, so ResolveTypes can see this instance
            Add(mappedType, serializer);
            return serializer;
        }

        public YamlConverter Get(Type type)
        {
            if (Types.TryGetValue(type, out YamlConverter converter))
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

        YamlConverter Add(Type type, YamlConverter converter)
        {
            Types[type] = converter;
            return converter;
        }
    }
}