using System;
using System.IO;
using System.Reflection;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.YamlSerializer
{
    // This class has the ability to take parsed StarData tree
    // And turn it into usable game objects
    internal class YamlSerializer : UserTypeSerializer
    {
        public override string ToString() => $"YamlSerializer {TheType.GetTypeName()}";

        public YamlSerializer(Type type) : base(type)
        {
        }

        protected override TypeSerializerMap CreateTypeMap()
        {
            return new YamlSerializerMap();
        }

        public override object Deserialize(YamlNode node)
        {
            if (Mapping == null)
                ResolveTypes();

            object item = Activator.CreateInstance(TheType);
            bool hasPrimaryValue = (node.Value != null);
            if (hasPrimaryValue)
            {
                Primary?.SetConverted(item, node.Value);
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

                    if (hasPrimaryValue && leafInfo == Primary)
                        continue; // ignore primary key if we already set it

                    leafInfo.SetDeserialized(item, leaf);
                }
            }
            else if (node.HasSequence)
            {
                Log.Warning(ConsoleColor.DarkRed, $"YamlSerializer no SEQUENCE mapping for '{node.Key}': '{node.Value}'");
            }
            return item;
        }

        public override void Serialize(BinaryWriter writer, object obj)
        {
            Log.Error($"Serialize not supported for {ToString()}");
        }

        public override object Deserialize(BinaryReader reader)
        {
            Log.Error($"Deserialize not supported for {ToString()}");
            return null;
        }
    }
}