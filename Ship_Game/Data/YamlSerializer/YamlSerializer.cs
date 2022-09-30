using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using SDUtils;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Serialization.Types;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.YamlSerializer
{
    // This class has the ability to take parsed StarData tree
    // And turn it into usable game objects
    public class YamlSerializer : UserTypeSerializer
    {
        public override string ToString() => $"YamlSerializer {NiceTypeName}";

        public YamlSerializer(Type type) : base(type, new YamlTypeMap())
        {
            ResolveTypes();
        }

        public YamlSerializer(Type type, TypeSerializerMap typeMap) : base(type, typeMap)
        {
        }

        // cache for yaml type converters
        class YamlTypeMap : TypeSerializerMap
        {
            public YamlTypeMap()
            {
                Add(new ObjectSerializer());
            }

            public override TypeSerializer AddUserTypeSerializer(Type type)
            {
                return Add(new YamlSerializer(type, this));
            }
        }

        public override object Deserialize(YamlNode node)
        {
            object item = Activator.CreateInstance(Type);

            bool hasKey = (node.Key != null);
            bool hasValue = (node.Value != null);
            if (hasKey)
            {
                PrimaryKeyName?.SetConverted(item, node.Key);
            }
            if (hasValue)
            {
                PrimaryKeyValue?.SetConverted(item, node.Value);
            }

            if (node.HasSubNodes && node.HasSequence)
            {
                Log.Warning(ConsoleColor.DarkRed, $"YamlSerializer '{node.Key}' has both Sub-Nodes and Sequence elements. But only one can exist. Preferring SubNodes.");
            }

            if (node.HasSubNodes)
            {
                foreach (YamlNode leaf in node.Nodes)
                {
                    if (!Mapping.TryGetValue(leaf.Name, out DataField leafInfo))
                    {
                        Log.Warning(ConsoleColor.DarkRed, $"YamlSerializer no OBJECT mapping for '{leaf.Key}': '{leaf.Value}'");
                        continue;
                    }

                    if (hasKey && leafInfo == PrimaryKeyName)
                        continue;
                    if (hasValue && leafInfo == PrimaryKeyValue)
                        continue; // ignore primary key value if we already set it

                    leafInfo.SetDeserialized(item, leaf);
                }
            }
            else if (node.HasSequence)
            {
                Log.Warning(ConsoleColor.DarkRed, $"YamlSerializer no SEQUENCE mapping for '{node.Key}': '{node.Value}'");
            }
            return item;
        }

        bool HasOnlyPrimitiveFields
        {
            get
            {
                foreach (KeyValuePair<string, DataField> kv in Mapping)
                {
                    if (kv.Value.Serializer.IsCollection || kv.Value.Serializer.IsUserClass)
                        return false;
                }
                return true;
            }
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            foreach (KeyValuePair<string, DataField> kv in Mapping)
            {
                object value = kv.Value.Get(obj);
                if (value != null)
                {
                    // handle primary key in a special way so we can have
                    // - Panel:    instead of    - Type: Panel
                    if (kv.Value == PrimaryKeyName)
                    {
                        var childNode = new YamlNode { Key = value };
                        parent.AddSubNode(childNode);
                    }
                    else
                    {
                        var childNode = new YamlNode { Key = kv.Key };
                        parent.AddSubNode(childNode);
                        kv.Value.Serializer.Serialize(childNode, value);
                    }
                }
            }
        }

        // Serializes root object without outputting its Key
        public void SerializeRoot(TextWriter writer, object obj)
        {
            var root = new YamlNode();
            Serialize(root, obj);
            root.SerializeTo(writer, depth:-2, noSpacePrefix:true);
        }

        public override void Serialize(TextWriter writer, object obj)
        {
            var root = new YamlNode
            {
                Key = Type.Name
            };

            Serialize(root, obj);
            root.SerializeTo(writer);
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            Log.Error($"Serialize (binary) not supported for {this}");
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            Log.Error($"Deserialize (binary) not supported for {this}");
            return null;
        }
    }
}