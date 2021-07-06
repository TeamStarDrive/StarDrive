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

        public ArrayListSerializer(Type elemType, TypeSerializer elemSerializer)
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

        // YAML array serialization is pretty dope, so we need a full utility method and reuse it

        public static void Serialize(IList list, TypeSerializer ser, TextSerializerContext context)
        {
            int count = list.Count;
            var tw = context.Writer;

            if (count == 0)
            {
                tw.Write(" []");
                return;
            }

            // [StarData] Array<int> Primitives;
            // Primitives: [1,2,3,4]
            if (!ser.IsUserClass)
            {
                tw.Write(" [");
                for (int i = 0; i < count; ++i)
                {
                    object element = list[i];
                    ser.Serialize(context, element);
                    if (i != count-1)
                        tw.Write(',');
                }
                tw.Write(']');
                return;
            }

            // [StarData] Array<Ship> Ships;
            // Ships:
            //   - Ship: ship1
            //     Position: [1,2,3]
            //   - Ship: ship2
            //     Position: [1,2,3]

            tw.Write('\n');
            context.Depth += 2;
            string prefix = new string(' ', context.Depth) + "- ";

            for (int i = 0; i < count; ++i)
            {
                tw.Write(prefix); // "  - "
                object element = list[i];
                context.IgnoreSpacePrefixOnce = true;
                ser.Serialize(context, element);
            }

            context.Depth -= 2;
        }

        public override void Serialize(TextSerializerContext context, object obj)
        {
            var list = (IList)obj;
            Serialize(list, ElemSerializer, context);
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