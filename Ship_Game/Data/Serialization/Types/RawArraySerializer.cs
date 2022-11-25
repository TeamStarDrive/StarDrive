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
        public override string ToString() => $"{TypeId}:RawArrSer<{ElemSerializer.TypeId}:{ElemType.GetTypeName()}>";

        public delegate object New(int length);
        delegate int GetLength(object arr);
        delegate object GetValue(object arr, int index);
        delegate void SetValue(object arr, object value, int index);
        public New NewArray;
        GetLength GetLengthOf;
        GetValue GetValueAt;
        SetValue SetValueAt;

        public RawArraySerializer(Type type, Type elemType, TypeSerializer elemSerializer)
            : base(type, elemType, elemSerializer)
        {
            Category = SerializerCategory.RawArray;

            // precompile array accesses to avoid horrible performance of naive Reflection
            // read more at: https://docs.microsoft.com/en-us/dotnet/api/system.linq.expressions.expression?view=netframework-4.8

            // lazy initialization pattern, each method will replace itself with the compiled version when called
            NewArray = InitNewArray;
            GetLengthOf = InitGetLengthOf;
            GetValueAt = InitGetValueAt;
            SetValueAt = InitSetValueAt;
        }

        object InitNewArray(int length)
        {
            var len = E.Parameter(typeof(int), "length");
            var newArray = E.NewArrayBounds(ElemType, len);

            // (int length) => (object)new T[length];
            NewArray = E.Lambda<New>(E.Convert(newArray, typeof(object)), len).Compile();
            return NewArray(length);
        }

        int InitGetLengthOf(object arr)
        {
            var obj = E.Parameter(typeof(object), "obj");
            var objAsArray = E.Convert(obj, Type);

            // (object arr) => ((T[])arr).Length;
            GetLengthOf = E.Lambda<GetLength>(E.ArrayLength(objAsArray), obj).Compile();
            return GetLengthOf(arr);
        }

        object InitGetValueAt(object arr, int idx)
        {
            var obj = E.Parameter(typeof(object), "obj");
            var index = E.Parameter(typeof(int), "index");
            var arrayAt = E.ArrayAccess(E.Convert(obj, Type), index);

            // (object arr, int index) => (object)((T[])arr)[index];
            GetValueAt = E.Lambda<GetValue>(E.Convert(arrayAt, typeof(object)), obj, index).Compile();
            return GetValueAt(arr, idx);
        }

        void InitSetValueAt(object arr, object val, int idx)
        {
            var obj = E.Parameter(typeof(object), "obj");
            var value = E.Parameter(typeof(object), "value");
            var index = E.Parameter(typeof(int), "index");
            var valueAsElem = E.Convert(value, ElemType);
            var arrayAt = E.ArrayAccess(E.Convert(obj, Type), index);

            // (object arr, object value, int index) => ((T[])arr)[index] = (T)value;
            SetValueAt = E.Lambda<SetValue>(E.Assign(arrayAt, valueAsElem), obj, value, index).Compile();
            SetValueAt(arr, val, idx);
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
            throw new NotSupportedException();
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            int count = (int)reader.BR.ReadVLu32();
            object converted = NewArray(count);
            for (int i = 0; i < count; ++i)
            {
                object element = reader.ReadCollectionElement(ElemSerializer);
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

        public override object CreateInstance()
        {
            throw new NotSupportedException();
        }

        public override void Deserialize(BinarySerializerReader reader, object instance)
        {
            throw new NotSupportedException();
        }
    }
}