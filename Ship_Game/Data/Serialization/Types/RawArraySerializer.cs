using System;
using System.Collections;
using System.IO;
using SDUtils;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    internal class RawArraySerializer : CollectionSerializer
    {
        public override string ToString() => $"RawArraySerializer<{ElemType.GetTypeName()}:{ElemSerializer.TypeId}>:{TypeId}";

        public RawArraySerializer(Type type, Type elemType, TypeSerializer elemSerializer)
            : base(type, elemType, elemSerializer)
        {
            Category = SerializerCategory.RawArray;
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
            // Ships:
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

        public override void Serialize(YamlNode parent, object obj)
        {
            var array = (Array)obj;
            ArrayListSerializer.Serialize(array, ElemSerializer, parent);
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            var array = (Array)obj;
            int count = array.Length;
            writer.BW.WriteVLu32((uint)count);
            for (int i = 0; i < count; ++i)
            {
                object element = array.GetValue(i);
                writer.WriteElement(ElemSerializer, element);
            }
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            int count = (int)reader.BR.ReadVLu32();
            Array converted = Array.CreateInstance(ElemType, count);
            TypeInfo elementType = reader.GetType(ElemSerializer);
            for (int i = 0; i < count; ++i)
            {
                object element = reader.ReadElement(elementType, ElemSerializer);
                converted.SetValue(element, i);
            }
            return converted;
        }

        public override int Count(object instance)
        {
            var array = (Array)instance;
            return array.Length;
        }

        public override object GetElementAt(object instance, int index)
        {
            var array = (Array)instance;
            return array.GetValue(index);
        }

        public override object CreateInstance(int length)
        {
            return Array.CreateInstance(ElemType, length);
        }

        public override void Deserialize(BinarySerializerReader reader, object instance)
        {
            throw new NotSupportedException();
        }
    }
}