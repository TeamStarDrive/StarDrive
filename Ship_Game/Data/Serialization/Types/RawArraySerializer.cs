using System;
using System.Collections;
using System.IO;
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
                    object element = ElemSerializer.Convert(array[i]);
                    converted.SetValue(element, i);
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
                    object element = ElemSerializer.Deserialize(nodes[i]);
                    converted.SetValue(element, i);
                }
                return converted;
            }
            return base.Deserialize(node); // try to deserialize value as Array
        }

        public override void Serialize(BinaryWriter writer, object obj)
        {
            var array = (Array)obj;
            int count = array.Length;
            writer.Write(count);
            for (int i = 0; i < count; ++i)
            {
                object element = array.GetValue(i);
                ElemSerializer.Serialize(writer, element);
            }
        }
        
        public override object Deserialize(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            Array converted = Array.CreateInstance(ElemType, count);
            for (int i = 0; i < count; ++i)
            {
                object element = ElemSerializer.Deserialize(reader);
                converted.SetValue(element, i);
            }
            return converted;
        }
    }
}