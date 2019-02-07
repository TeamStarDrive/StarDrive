using System;
using System.Collections;
using System.IO;
using System.Reflection;

namespace Ship_Game.Data
{
    // Note: StarDataParser is opt-in, so properties/fields
    //       must be marked with [StarData]
    [AttributeUsage(AttributeTargets.Property|AttributeTargets.Field)]
    public class StarDataAttribute : Attribute
    {
        public bool IsPrimaryKey;
        public StarDataAttribute(bool key=false)
        {
            IsPrimaryKey = key;
        }
    }

    // Note: This can be used for Key attributes
    public sealed class StarDataKeyAttribute : StarDataAttribute
    {
        public StarDataKeyAttribute() : base(true)
        {
        }
    }

    // This class has the ability to take parsed StarData tree
    // And turn it into usable game objects
    internal class StarDataSerializer : TypeConverter
    {
        class Info
        {
            readonly Type Type;
            readonly PropertyInfo Prop;
            readonly FieldInfo Field;
            public readonly TypeConverter Converter;
            readonly Type ListType; // this is an Array<ListType>

            public Info(Map<Type, TypeConverter> types, PropertyInfo prop, FieldInfo field)
            {
                Prop = prop;
                Field = field;
                Type = prop != null ? prop.PropertyType : field.FieldType;
                ListType = GetListType(Type);
                if (!types.TryGetValue(Type, out Converter))
                {
                    if (ListType != null) Converter = new StarDataSerializer(Type.GenericTypeArguments[0], types);
                    else if (Type.IsEnum) Converter = new EnumConverter(Type);
                    else throw new InvalidDataException($"Unsupported type {Type}!");
                    types[Type] = Converter;
                }
            }

            void Set(object instance, object value)
            {
                if (Field != null) Field.SetValue(instance, value);
                else               Prop.SetValue(instance, value);
            }

            object Get(object instance)
            {
                return Field != null ? Field.GetValue(instance) : Prop.GetValue(instance);
            }

            public void SetValue(object instance, object value)
            {
                if (ListType != null)
                {
                    if (!(Get(instance) is IList list))
                    {
                        list = ArrayHelper.NewArrayOfT(ListType);
                        Set(instance, list);
                    }
                    list.Add(value);
                    return;
                }
                
                if (value != null)
                {
                    Type sourceT = value.GetType();
                    if (Type != sourceT)
                        value = Converter.Convert(value);
                }
                Set(instance, value); // allow setting null, sometimes it's an override
            }
        }

        readonly Map<string, Info> Mapping = new Map<string, Info>();
        string PrimaryName;
        Info PrimaryInfo;
        readonly Type TheType;

        public StarDataSerializer(Type type, Map<Type, TypeConverter> types = null)
        {
            TheType = type;
            types = types ?? ConvertTo.CreateDefaultConverters();

            Type shouldSerialize = typeof(StarDataAttribute);
            PropertyInfo[] props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo[]   fields = type.GetFields(    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            for (int i = 0; i < fields.Length; ++i)
            {
                FieldInfo f = fields[i];
                if (f.GetCustomAttribute(shouldSerialize) is StarDataAttribute a)
                    AddMapping(f.Name, a, new Info(types, null, f));
            }
            
            for (int i = 0; i < props.Length; ++i)
            {
                PropertyInfo p = props[i];
                if (p.GetCustomAttribute(shouldSerialize) is StarDataAttribute a)
                {
                    MethodInfo setter = p.GetSetMethod(nonPublic: true);
                    if (setter == null) throw new Exception($"Property {p.Name} has no setter!");
                    AddMapping(p.Name, a, new Info(types, p, null));
                }
            }
        }

        static Type GetListType(Type type)
        {
            //if (toType.IsArray) // @todo Figure out how to array append :|
            //    converter = new SimpleSerializer(toType.GetElementType(), Types);
            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(Array<>))
                    return type.GenericTypeArguments[0];
                if (type.GetInterfaces().Contains(typeof(IList)))
                    return type.GenericTypeArguments[0];
            }
            return null;
        }

        void AddMapping(string name, StarDataAttribute a, Info info)
        {
            if (a.IsPrimaryKey)
            {
                PrimaryName = name;
                PrimaryInfo = info;
            }
            else
            {
                Mapping.Add(name, info);
            }
        }

        public override object Convert(object value)
        {
            return null;
        }

        public object Deserialize(StarDataNode node)
        {
            object item = Activator.CreateInstance(TheType);
            if (node.Value != null && PrimaryName != null)
            {
                PrimaryInfo.SetValue(item, node.Value);
            }
            if (!node.HasItems)
                return item;

            foreach (StarDataNode leaf in node.Items)
            {
                if (leaf.HasItems) // it appears to be an array M'Lord
                {
                    if (Mapping.TryGetValue(leaf.Key, out Info inf) && inf.Converter is StarDataSerializer s)
                    {
                        object subItem = s.Deserialize(leaf);
                        inf.SetValue(item, subItem);
                    }
                }
                else
                {
                    object value = leaf.Value;
                    if (value == null || !Mapping.TryGetValue(leaf.Key, out Info info))
                        continue;

                    info.SetValue(item, value);
                }
            }
            return item;
        }
    }
}