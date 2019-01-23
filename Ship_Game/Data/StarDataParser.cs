using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Data
{
    // StarDrive Generic Data Node
    public class SDNode : IEnumerable<SDNode>
    {
        public string Name;
        public object Value;

        public Array<SDNode> Items;

        public override string ToString() => SerializedText();
        public bool HasItems => Items != null && Items.Count > 0;
        public int Count => Items?.Count ?? 0;

        public void AddItem(SDNode item)
        {
            if (Items == null)
                Items = new Array<SDNode>();
            Items.Add(item);
        }

        public IEnumerator<SDNode> GetEnumerator()
        {
            if (Items == null) yield break;
            foreach (SDNode node in Items)
                yield return node;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public string SerializedText()
        {
            var sb = new StringBuilder();
            SerializeTo(sb);
            return sb.ToString();
        }

        public void SerializeTo(StringBuilder sb, int depth = 0)
        {
            for (int i = 0; i < depth; ++i)
                sb.Append(' ');
            sb.Append(Name).Append(": ").Append(Value).AppendLine();

            if (Items != null)
                foreach (SDNode child in Items)
                    child.SerializeTo(sb, depth+2);
        }
    }

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

    // Simplified text parser for StarDrive data files
    public class StarDataParser : IDisposable
    {
        StreamReader Reader;
        public SDNode Root { get; }
        static readonly char[] Colon = { ':' };
        static readonly char[] Array = { '[', ']' };
        static readonly char[] Commas = { ',' };

        public StarDataParser(string file)
        {
            FileInfo f = ResourceManager.GetModOrVanillaFile(file);
            if (f == null || !f.Exists)
                throw new FileNotFoundException($"Required StarData file not found! {file}");

            Reader = f.OpenText();
            Root = new SDNode
            {
                Name = f.NameNoExt(),
                Value = "",
            };
            Parse();
        }
        public StarDataParser(FileInfo f)
        {
            if (f == null || !f.Exists)
                throw new FileNotFoundException($"Required StarData file not found! {f?.FullName}");

            Reader = f.OpenText();
            Root = new SDNode
            {
                Name = f.NameNoExt(),
                Value = "",
            };
            Parse();
        }
        public void Dispose()
        {
            Reader?.Close(); Reader = null;
        }

        struct DepthSave
        {
            public int Depth;
            public SDNode Node;
        }

        void Parse()
        {
            int depth = 0;
            string line;
            var saved = new Stack<DepthSave>();

            SDNode root = Root;
            SDNode prev = Root;
            
            while ((line = Reader.ReadLine()) != null)
            {
                if (line.Length == 0 || line[0] == '#')
                    continue;

                int newDepth = Depth(line);
                if (newDepth >= line.Length)
                    continue; // all spaces

                SDNode node = ParseLineAsNode(line);
                if (newDepth > depth)
                {
                    saved.Push(new DepthSave{ Depth=depth, Node=root });
                    root = prev; // root changed
                }
                else if (newDepth < depth)
                {
                    for (;;) // try to pop down until we get to right depth
                    {
                        DepthSave save = saved.Pop();
                        if (save.Depth > newDepth && saved.Count > 0)
                            continue;
                        root = save.Node;
                        break;
                    }
                }

                root.AddItem(node);
                depth = newDepth;
                prev = node;
            }
        }

        static SDNode ParseLineAsNode(string line)
        {
            string[] parts = line.Split(Colon, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) // no value
            {
                return new SDNode
                {
                    Name  = parts[0].Trim(),
                    Value = null
                };
            }

            string value = parts[1];
            int comment = value.IndexOf('#');
            if (comment != -1)
            {
                value = value.Substring(0, comment);
            }

            return new SDNode
            {
                Name  = parts[0].Trim(),
                Value = BoxValue(value)
            };
        }

        static object BoxValue(string value)
        {
            value = value.Trim();
            if (value.Length == 0) return null;
            if (value == "null")   return null;
            if (value == "true")   return true;
            if (value == "false")  return false;
            char c = value[0];
            if (c == '[')
            {
                value = value.Trim(Array);
                string[] elements = value.Split(Commas, StringSplitOptions.None);

                // now individually box each element into an object array
                var array = new object[elements.Length];
                for (int i = 0; i < elements.Length; ++i)
                    array[i] = BoxValue(elements[i]);
                return array;
            }
            if (('0' <= c && c <= '9') || c == '-' || c == '+')
            {
                if (value.IndexOf('.') != -1 && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                    return f;
                if (int.TryParse(value, out int i))
                    return i;
            }
            return value; // probably some sort of text?
        }

        static int Depth(string line)
        {
            int depth = 0;
            for (int i = 0; i < line.Length; ++i)
            {
                char c = line[i];
                if      (c == ' ')  ++depth;
                else if (c == '\t') depth += 4;
                else break;
            }
            return depth;
        }

        class SimpleSerializer : TypeConverter
        {
            class Info
            {
                public readonly Type Type;
                public readonly PropertyInfo Prop;
                public readonly FieldInfo Field;
                public TypeConverter Converter;
                public Type ListType; // this is an Array<ListType>
                public Info(PropertyInfo prop, FieldInfo field)
                {
                    Prop = prop;
                    Field = field;
                    Type = prop != null ? prop.PropertyType : field.FieldType;
                    Converter = null;
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
                            value = Converter.Convert(value, sourceT);
                    }
                    Set(instance, value); // allow setting null, sometimes it's an override
                }
            }

            readonly Map<string, Info> Mapping = new Map<string, Info>();
            readonly Map<Type, TypeConverter> Types;
            string PrimaryName;
            Info PrimaryInfo;
            Type TheType;

            public SimpleSerializer(Type type, Map<Type, TypeConverter> types = null)
            {
                TheType = type;
                Types = types ?? CreateBaseTypes();
                Type shouldSerialize = typeof(StarDataAttribute);
                PropertyInfo[] props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (PropertyInfo p in props)
                {
                    if (p.GetCustomAttribute(shouldSerialize) is StarDataAttribute a)
                    {
                        MethodInfo setter = p.GetSetMethod(nonPublic: true);
                        if (setter == null) throw new Exception($"Property {p.Name} has no setter!");
                        AddMapping(p.Name, a, new Info(p, null));
                    }
                }
                foreach (FieldInfo f in fields)
                {
                    if (f.GetCustomAttribute(shouldSerialize) is StarDataAttribute a)
                        AddMapping(f.Name, a, new Info(null, f));
                }
            }

            Map<Type, TypeConverter> CreateBaseTypes()
            {
                return new Map<Type, TypeConverter>
                {
                    (typeof(Range),   new RangeConverter()),
                    (typeof(LocText), new LocTextConverter()),
                    (typeof(Color),   new ColorConverter()),
                    (typeof(int),     new IntConverter()),
                    (typeof(float),   new FloatConverter()),
                    (typeof(bool),    new BoolConverter()),
                    (typeof(string),  new StringConverter())
                };
            }

            Type GetListType(Type type)
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

            void SetConverter(Info info)
            {
                Type toType = info.Type;
                info.ListType = GetListType(toType);
                if (Types.TryGetValue(toType, out info.Converter))
                    return;

                if (info.ListType != null)
                    info.Converter = new SimpleSerializer(toType.GenericTypeArguments[0]);
                else if (toType.IsEnum)
                    info.Converter = new EnumConverter(toType);
                else
                    throw new InvalidDataException($"Unsupported type {toType}!");

                Types[toType] = info.Converter;
            }

            void AddMapping(string name, StarDataAttribute a, Info info)
            {
                SetConverter(info);
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

            public override object Convert(object value, Type source)
            {
                return null;
            }

            public object Deserialize(SDNode node)
            {
                object item = Activator.CreateInstance(TheType);
                if (node.Value != null && PrimaryName != null)
                {
                    PrimaryInfo.SetValue(item, node.Value);
                }
                if (node.Items == null)
                    return item;

                foreach (SDNode leaf in node.Items)
                {
                    if (leaf.HasItems) // it appears to be an array M'Lord
                    {
                        if (Mapping.TryGetValue(leaf.Name, out Info inf) && inf.Converter is SimpleSerializer s)
                        {
                            object subItem = s.Deserialize(leaf);
                            inf.SetValue(item, subItem);
                        }
                        continue;
                    }
                    else
                    {
                        object value = leaf.Value;
                        if (value == null || !Mapping.TryGetValue(leaf.Name, out Info info))
                            continue;

                        info.SetValue(item, value);
                    }
                }
                return item;
            }
        }

        public Array<T> DeserializeArray<T>() where T : new()
        {
            var items = new Array<T>();
            var ser = new SimpleSerializer(typeof(T));
            foreach (SDNode child in Root)
            {
                items.Add((T)ser.Deserialize(child));
            }
            return items;
        }
    }
}
