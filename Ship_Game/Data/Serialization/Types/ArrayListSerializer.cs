using System;
using System.Collections;
using System.IO;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    internal class ArrayListSerializer : TypeSerializer
    {
        public override string ToString() => $"ArrayListSerializer {ElemType.GetTypeName()}";
        readonly Type ElemType;
        readonly TypeSerializer ElemSerializer;

        public ArrayListSerializer(Type type, Type elemType, TypeSerializer elemSerializer) : base(type)
        {
            ElemType = elemType;
            ElemSerializer = elemSerializer;
            IsCollection = true;
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
                    object element = ElemSerializer.Convert(array[i]);
                    list.Add(element);
                }
                return list;
            }

            Error(value, "Array convert failed -- expected a list of values [*, *, *]");
            return value;
        }

        public override object Deserialize(YamlNode node)
        {
            // [StarData] Array<Ship> Ships;
            // Ships:
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
                    object element = ElemSerializer.Deserialize(nodes[i]);
                    list.Add(element);
                }
                return list;
            }
            return base.Deserialize(node); // try to deserialize value as Array
        }

        public static void Serialize(IList list, TypeSerializer ser, YamlNode parent)
        {
            int count = list.Count;
            if (count == 0)
            {
                parent.Value = Empty<object>.Array; // so we format as "Parent: []"
            }
            else
            {
                bool isPrimitive = !ser.IsUserClass;
                if (isPrimitive)
                {
                    object[] items = new object[count];
                    parent.Value = items;
                    for (int i = 0; i < items.Length; ++i)
                        items[i] = list[i];
                }
                else
                {
                    for (int i = 0; i < count; ++i)
                    {
                        object element = list[i];
                        var seqElem = new YamlNode();
                        ser.Serialize(seqElem, element);
                        parent.AddSequenceElement(seqElem);
                    }
                }
            }
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            var list = (IList)obj;
            Serialize(list, ElemSerializer, parent);
        }

        public override void Serialize(BinaryWriter writer, object obj)
        {
            var list = (IList)obj;

            int count = list.Count;
            writer.Write(count);
            for (int i = 0; i < count; ++i)
            {
                object element = list[i];
                ElemSerializer.Serialize(writer, element);
            }
        }
        
        public override object Deserialize(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            IList list = ArrayHelper.NewArrayOfT(ElemType);
            for (int i = 0; i < count; ++i)
            {
                object element = ElemSerializer.Deserialize(reader);
                list.Add(element);
            }
            return list;
        }
    }
}