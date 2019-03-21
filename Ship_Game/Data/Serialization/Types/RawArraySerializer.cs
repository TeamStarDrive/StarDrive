using System;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    internal class RawArraySerializer : TypeSerializer
    {
        public override string ToString() => $"RawArraySerializer {ElemType.GenericName()}";
        readonly Type ElemType;
        readonly TypeSerializer ElemSerializer;

        public RawArraySerializer(Type elemType, TypeSerializer elemSerializer)
        {
            ElemType = elemType;
            ElemSerializer = elemSerializer;
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
                    converted.SetValue(ElemSerializer.Convert(array[i]), i);
                }
                return converted;
            }

            Error(value, "Array convert failed -- expected a list of values [*, *, *]");
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
            Array<YamlNode> nodes = node.SequenceOrSubNodes;
            if (nodes?.Count > 0)
            {
                Array converted = Array.CreateInstance(ElemType, nodes.Count);
                for (int i = 0; i < nodes.Count; ++i)
                {
                    converted.SetValue(ElemSerializer.Deserialize(nodes[i]), i);
                }
                return converted;
            }

            return base.Deserialize(node); // try to deserialize value as Array
        }
    }
}