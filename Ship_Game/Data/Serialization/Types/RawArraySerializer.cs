using System;
using System.Linq.Expressions;
using SDUtils;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    using E = Expression;

    internal class RawArraySerializer : CollectionSerializer
    {
        public override string ToString() => $"RawArraySerializer<{ElemType.GetTypeName()}:{ElemSerializer.TypeId}>:{TypeId}";

        delegate object New(int length);
        delegate int GetLength(object arr);
        delegate object GetValue(object arr, int index);
        delegate void SetValue(object arr, object value, int index);
        readonly New NewArray;
        readonly GetLength GetLengthOf;
        readonly GetValue GetValueAt;
        readonly SetValue SetValueAt;

        public RawArraySerializer(Type type, Type elemType, TypeSerializer elemSerializer)
            : base(type, elemType, elemSerializer)
        {
            Category = SerializerCategory.RawArray;

            // precompile array accesses to avoid horrible performance of naive Reflection
            // read more at: https://docs.microsoft.com/en-us/dotnet/api/system.linq.expressions.expression?view=netframework-4.8
            var length = E.Parameter(typeof(int), "length");
            var obj = E.Parameter(typeof(object), "obj");
            var value = E.Parameter(typeof(object), "value");
            var index = E.Parameter(typeof(int), "index");
            var objAsArray = E.Convert(obj, type);
            var valueAsElem = E.Convert(value, elemType);
            var arrayAt = E.ArrayAccess(objAsArray, index);
            var newArray = E.NewArrayBounds(elemType, length);

            // (int length) => (object)new T[length];
            NewArray = E.Lambda<New>(E.Convert(newArray, typeof(object)), length).Compile();
            
            // (object arr) => ((T[])arr).Length;
            GetLengthOf = E.Lambda<GetLength>(E.ArrayLength(objAsArray), obj).Compile();
            
            // (object arr, int index) => (object)((T[])arr)[index];
            GetValueAt = E.Lambda<GetValue>(E.Convert(arrayAt, typeof(object)), obj, index).Compile();
            
            // (object arr, object value, int index) => ((T[])arr)[index] = (T)value;
            SetValueAt = E.Lambda<SetValue>(E.Assign(arrayAt, valueAsElem), obj, value, index).Compile();
        }

        public override object Convert(object value)
        {
            if (value == null)
                return null;

            if (value is object[] array)
            {
                object converted = NewArray(array.Length);
                for (int i = 0; i < array.Length; ++i)
                {
                    object element = ElemSerializer.Convert(array[i]);
                    SetValueAt(converted, element, i);
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
                object converted = NewArray(nodes.Count);
                for (int i = 0; i < nodes.Count; ++i)
                {
                    object element = ElemSerializer.Deserialize(nodes[i]);
                    SetValueAt(converted, element, i);
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
            object converted = NewArray(count);
            TypeInfo elementType = reader.GetType(ElemSerializer);
            for (int i = 0; i < count; ++i)
            {
                object element = reader.ReadElement(elementType, ElemSerializer);
                SetValueAt(converted, element, i);
            }
            return converted;
        }

        public override int Count(object instance)
        {
            return GetLengthOf(instance);
        }

        public override object GetElementAt(object instance, int index)
        {
            return GetValueAt(instance, index);
        }

        public override object CreateInstance(int length)
        {
            return NewArray(length);
        }

        public override void Deserialize(BinarySerializerReader reader, object instance)
        {
            throw new NotSupportedException();
        }
    }
}