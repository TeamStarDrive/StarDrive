using System;
using System.IO;
using System.Reflection;

namespace Ship_Game.Data.YamlSerializer
{

    // This class has the ability to take parsed StarData tree
    // And turn it into usable game objects
    internal class YamlSerializer : YamlConverter
    {
        Map<string, Info> Mapping;
        Info PrimaryInfo;
        readonly Type TheType;
        public override string ToString() => $"StarDataSerializer {TheType.GenericName()}";

        public YamlSerializer(Type type)
        {
            TheType = type;
            if (type.GetCustomAttribute<StarDataTypeAttribute>() == null)
                throw new InvalidDataException($"Unsupported type {type} - is the class missing [StarDataType] attribute?");
        }

        internal void ResolveTypes(YamlConverters types)
        {
            Mapping = new Map<string, Info>();
            types = types ?? new YamlConverters();

            Type shouldSerialize = typeof(StarDataAttribute);
            PropertyInfo[] props = TheType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo[]   fields = TheType.GetFields(    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
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
                        throw new Exception($"StarDataSerializer Class {TheType.Name} Property {p.Name} has no setter!");

                    string id = a.Id.NotEmpty() ? a.Id : p.Name;
                    AddMapping(id, a, new Info(types, p, null));
                }
            }
        }

        void AddMapping(string name, StarDataAttribute a, Info info)
        {
            Mapping.Add(name, info);
            if (a.IsPrimaryKey)
            {
                if (PrimaryInfo != null)
                    throw new InvalidDataException($"StarDataSerializer cannot have more than 1 [StarDataKey] attributes! Original {PrimaryInfo}, New {info}");
                PrimaryInfo = info;
            }
        }

        public override object Deserialize(YamlNode node)
        {
            if (Mapping == null)
                ResolveTypes(null);

            object item = Activator.CreateInstance(TheType);
            bool hasPrimaryValue = (node.Value != null);
            if (hasPrimaryValue && PrimaryInfo != null)
            {
                object primaryValue = PrimaryInfo.Converter.Convert(node.Value);
                PrimaryInfo.Set(item, primaryValue);
            }

            if (node.HasSubNodes && node.HasSequence)
            {
                Log.Warning(ConsoleColor.DarkRed, $"StarDataSerializer '{node.Key}' has both Sub-Nodes and Sequence elements. But only one can exist. Preferring SubNodes.");
            }

            if (node.HasSubNodes)
            {
                foreach (YamlNode leaf in node.Nodes)
                {
                    if (!Mapping.TryGetValue(leaf.Name, out Info leafInfo))
                    {
                        Log.Warning(ConsoleColor.DarkRed, $"StarDataSerializer no OBJECT mapping for '{leaf.Key}': '{leaf.Value}'");
                        continue;
                    }

                    if (hasPrimaryValue && leafInfo == PrimaryInfo)
                        continue; // ignore primary key if we already set it

                    object leafValue = leafInfo.Converter.Deserialize(leaf);
                    leafInfo.Set(item, leafValue);
                }
            }
            else if (node.HasSequence)
            {
                Log.Warning(ConsoleColor.DarkRed, $"StarDataSerializer no SEQUENCE mapping for '{node.Key}': '{node.Value}'");
            }
            return item;
        }

        class Info
        {
            readonly PropertyInfo Prop;
            readonly FieldInfo Field;
            public readonly YamlConverter Converter;

            public override string ToString() => Prop?.ToString() ?? Field?.ToString() ?? "invalid";

            public Info(YamlConverters yamlConverters, PropertyInfo prop, FieldInfo field)
            {
                Prop = prop;
                Field = field;
                Type type = prop != null ? prop.PropertyType : field.FieldType;
                Converter = yamlConverters.Get(type);
            }

            public void Set(object instance, object value)
            {
                if (Field != null) Field.SetValue(instance, value);
                else               Prop.SetValue(instance, value);
            }
        }
    }
}