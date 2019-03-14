using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;

namespace Ship_Game.Data
{
    // Note: StarDataParser is opt-in, so properties/fields
    //       must be marked with [StarData]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class StarDataAttribute : Attribute
    {
        public string Id;
        public bool IsPrimaryKey;
        public StarDataAttribute()
        {
        }
        public StarDataAttribute(string id, bool key = false)
        {
            Id = id;
            IsPrimaryKey = key;
        }
    }

    // Note: This can be used for Key attributes
    public sealed class StarDataKeyAttribute : StarDataAttribute
    {
        public StarDataKeyAttribute() : base(null, true)
        {
        }
        public StarDataKeyAttribute(string id) : base(id, true)
        {
        }
    }

    // Note: This can be applied to classes, so StarData classes could have nested types
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class StarDataTypeAttribute : Attribute
    {
        public StarDataTypeAttribute()
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
            readonly TypeConverter Converter;
            readonly Type ListType; // this is an Array<ListType>

            public Info(Converters converters, PropertyInfo prop, FieldInfo field)
            {
                Prop = prop;
                Field = field;
                Type = prop != null ? prop.PropertyType : field.FieldType;
                ListType = converters.GetListType(Type);
                Converter = converters.Get(Type);
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

            object Deserialize(StarDataNode node, bool recursiveDeserialize)
            {
                if (recursiveDeserialize && node.HasItems) // we have sub-nodes, delegate to deserialize
                {
                    return Converter.Deserialize(node);
                }

                object value = node.Value;
                if (value == null) // allow null, sometimes it's an override
                    return null;
                Type sourceT = value.GetType();
                if (Type == sourceT)
                    return value;
                return Converter.Convert(value); // direct convert
            }

            public void SetValue(object instance, StarDataNode node, bool recursiveDeserialize)
            {
                if (ListType != null)
                {
                    if (!(Get(instance) is IList list))
                    {
                        list = ArrayHelper.NewArrayOfT(ListType);
                        Set(instance, list);
                    }
                    list.Add(Deserialize(node, recursiveDeserialize));
                }
                else
                {
                    Set(instance, Deserialize(node, recursiveDeserialize));
                }
            }
        }

        readonly Map<string, Info> Mapping = new Map<string, Info>();
        string PrimaryName;
        Info PrimaryInfo;
        readonly Type TheType;

        public StarDataSerializer(Type type, Converters types = null)
        {
            TheType = type;
            types = types ?? new Converters();

            Type shouldSerialize = typeof(StarDataAttribute);
            PropertyInfo[] props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo[]   fields = type.GetFields(    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            for (int i = 0; i < fields.Length; ++i)
            {
                FieldInfo f = fields[i];
                if (f.GetCustomAttribute(shouldSerialize) is StarDataAttribute a)
                {
                    string id = a.Id.NotEmpty() ? a.Id : f.Name;
                    AddMapping(id, a, new Info(types, null, f));
                }
            }
            
            for (int i = 0; i < props.Length; ++i)
            {
                PropertyInfo p = props[i];
                if (p.GetCustomAttribute(shouldSerialize) is StarDataAttribute a)
                {
                    MethodInfo setter = p.GetSetMethod(nonPublic: true);
                    if (setter == null)
                        throw new Exception($"StarDataSerializer Class {type.Name} Property {p.Name} has no setter!");

                    string id = a.Id.NotEmpty() ? a.Id : p.Name;
                    AddMapping(id, a, new Info(types, p, null));
                }
            }
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

        public override object Deserialize(StarDataNode node)
        {
            object item = Activator.CreateInstance(TheType);
            if (node.Value != null && PrimaryName != null)
            {
                PrimaryInfo.SetValue(item, node, recursiveDeserialize: false);
            }

            if (node.HasItems)
            {
                foreach (StarDataNode leaf in node.Items)
                {
                    if (Mapping.TryGetValue(leaf.Name, out Info info))
                    {
                        info.SetValue(item, leaf, recursiveDeserialize: true);
                    }
                    else
                    {
                        Log.Warning($"StarDataSerializer no mapping for '{leaf.Key}': '{leaf.Value}'");
                    }
                }
            }
            return item;
        }
    }
}