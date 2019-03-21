using System;
using System.Collections;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    internal class ArrayListSerializer : TypeSerializer
    {
        public override string ToString() => $"ArrayListSerializer {ElemType.GenericName()}";
        readonly Type ElemType;
        readonly TypeSerializer ElemSerializer;

        public ArrayListSerializer(Type elemType, TypeSerializer elemSerializer)
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
                IList list = ArrayHelper.NewArrayOfT(ElemType);
                for (int i = 0; i < array.Length; ++i)
                {
                    list.Add(ElemSerializer.Convert(array[i]));
                }
                return list;
            }

            Error(value, "Array convert failed -- expected a list of values [*, *, *]");
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
            Array<YamlNode> nodes = node.SequenceOrSubNodes;
            if (nodes?.Count > 0)
            {
                IList list = ArrayHelper.NewArrayOfT(ElemType);
                for (int i = 0; i < nodes.Count; ++i)
                {
                    list.Add(ElemSerializer.Deserialize(nodes[i]));
                }
                return list;
            }

            return base.Deserialize(node); // try to deserialize value as Array
        }
    }
}