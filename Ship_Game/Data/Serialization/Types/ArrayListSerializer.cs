using System;
using System.Collections;
using SDUtils;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    internal class ArrayListSerializer : CollectionSerializer
    {
        public override string ToString() => $"ArrayListSerializer<{ElemType.GetTypeName()}:{ElemSerializer.TypeId}>:{TypeId}";
        readonly Type GenericArrayType;

        public ArrayListSerializer(Type type, Type elemType, TypeSerializer elemSerializer)
            : base(type, elemType, elemSerializer)
        {
            GenericArrayType = typeof(Array<>).MakeGenericType(elemType);
        }

        public override object Convert(object value)
        {
            if (value == null)
                return null;

            if (value is object[] array)
            {
                var list = (IList)Activator.CreateInstance(GenericArrayType);
                return ConvertList(list, array, ElemSerializer);
            }

            Error(value, "Array convert failed -- expected a list of values [*, *, *]");
            return value;
        }

        public static object ConvertList(IList list, object[] array, TypeSerializer elemSer)
        {
            for (int i = 0; i < array.Length; ++i)
            {
                object element = elemSer.Convert(array[i]);
                list.Add(element);
            }
            return list;
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
                var list = (IList)Activator.CreateInstance(GenericArrayType);
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
                    for (int i = 0; i < items.Length; ++i)
                    {
                        object element = list[i];
                        // use `parent.Value` as a buffer for Serializing
                        // primitives like Int, String, Range, Vector4...
                        ser.Serialize(parent, element);
                        items[i] = parent.Value;
                    }
                    parent.Value = items;
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

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            var list = (IList)obj;

            int count = list.Count;
            writer.BW.WriteVLu32((uint)count);
            for (int i = 0; i < count; ++i)
            {
                object element = list[i];
                writer.WriteElement(ElemSerializer, element);
            }
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            var list = (IList)Activator.CreateInstance(GenericArrayType);
            Deserialize(reader, list, ElemSerializer);
            return list;
        }

        public override int Count(object instance)
        {
            return ((IList)instance).Count;
        }

        public override object GetElementAt(object instance, int index)
        {
            return ((IList)instance)[index];
        }

        public override object CreateInstance(int length)
        {
            return CreateInstanceOf(GenericArrayType);
        }

        public override void Deserialize(BinarySerializerReader reader, object instance)
        {
            var list = (IList)instance;
            Deserialize(reader, list, ElemSerializer);
        }

        public static void Deserialize(BinarySerializerReader reader, IList list, TypeSerializer elemSer)
        {
            int count = (int)reader.BR.ReadVLu32();
            TypeInfo elementType = reader.GetType(elemSer);
            for (int i = 0; i < count; ++i)
            {
                object element = reader.ReadElement(elementType, elemSer);
                list.Add(element);
            }
        }
    }
}