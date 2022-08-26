using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using SDUtils;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Yaml;
using TypeInfo = Ship_Game.Data.Binary.TypeInfo;

namespace Ship_Game.Data.Serialization.Types
{
    using E = Expression;

    internal class HashSetSerializer : CollectionSerializer
    {
        public override string ToString() => $"HashSetSerializer<{ElemType.GetTypeName()}:{ElemSerializer.TypeId}>:{TypeId}";

        delegate object New();
        delegate int Length(object set);
        delegate void Add(object set, object value);
        delegate IEnumerator Enumerate(object set);
        readonly New NewSet;
        readonly Length LengthOfSet;
        readonly Add AddToSet;
        readonly Enumerate EnumerateSet;

        // The type itself can be ISet<> etc
        public HashSetSerializer(Type type, Type elemType, TypeSerializer elemSerializer)
            : base(type, elemType, elemSerializer)
        {
            var HashSetType = typeof(HashSet<>).MakeGenericType(elemType);
            var ISetType = typeof(ISet<>).MakeGenericType(elemType);
            var ICollectionType = typeof(ICollection<>).MakeGenericType(elemType);
            var IEnumerableType = typeof(IEnumerable);

            // all accessor methods have to be precompiled for Set types
            var set = E.Parameter(typeof(object), "set");
            var value = E.Parameter(typeof(object), "value");
            var toISet = E.Convert(set, ISetType);
            var toType = E.Convert(value, elemType);

            // () => (object)new HashSet<T>();
            NewSet = E.Lambda<New>(E.Convert(E.New(HashSetType), typeof(object))).Compile();

            // (object set) => ((ICollection<T>)set).Count;
            var count = ICollectionType.GetProperty("Count") ?? throw new MissingMemberException("ISet<T>.Count");
            LengthOfSet = E.Lambda<Length>(E.Property(toISet, count), set).Compile();

            // (object set, object value) => ((ISet<T>)set).Add(value);
            var add = ISetType.GetMethod("Add") ?? throw new MissingMethodException("ISet<T>.Add(object)");
            AddToSet = E.Lambda<Add>(E.Call(toISet, add, toType), set, value).Compile();

            // (object set) => ((ISet<T>)set).GetEnumerator();
            var getEnumerator = IEnumerableType.GetMethod("GetEnumerator") ?? throw new MissingMethodException("ISet<T>.GetEnumerator()");
            EnumerateSet = E.Lambda<Enumerate>(E.Call(toISet, getEnumerator), set).Compile();
        }

        public override object Convert(object value)
        {
            if (value == null)
                return null;

            if (value is object[] array)
            {
                return ConvertSet(NewSet(), array, ElemSerializer);
            }

            Error(value, "Array convert failed -- expected a list of values [*, *, *]");
            return value;
        }

        object ConvertSet(object set, object[] array, TypeSerializer elemSer)
        {
            for (int i = 0; i < array.Length; ++i)
            {
                object element = elemSer.Convert(array[i]);
                AddToSet(set, element);
            }
            return set;
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
                var set = NewSet();
                for (int i = 0; i < nodes.Count; ++i)
                {
                    object element = ElemSerializer.Deserialize(nodes[i]);
                    AddToSet(set, element);
                }
                return set;
            }
            return base.Deserialize(node); // try to deserialize value as Array
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            int count = LengthOfSet(obj);
            if (count == 0)
            {
                parent.Value = Empty<object>.Array; // so we format as "Parent: []"
            }
            else
            {
                IEnumerator enumerator = EnumerateSet(obj);
                if (!ElemSerializer.IsUserClass)
                {
                    object[] items = new object[count];
                    for (int i = 0; i < items.Length; ++i)
                    {
                        enumerator.MoveNext();
                        // use `parent.Value` as a buffer for Serializing
                        // primitives like Int, String, Range, Vector4...
                        ElemSerializer.Serialize(parent, enumerator.Current);
                        items[i] = parent.Value;
                    }
                    parent.Value = items;
                }
                else
                {
                    for (int i = 0; i < count; ++i)
                    {
                        enumerator.MoveNext();
                        var seqElem = new YamlNode();
                        ElemSerializer.Serialize(seqElem, enumerator.Current);
                        parent.AddSequenceElement(seqElem);
                    }
                }
            }
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            int count = LengthOfSet(obj);
            IEnumerator enumerator = EnumerateSet(obj);
            writer.BW.WriteVLu32((uint)count);
            for (int i = 0; i < count; ++i)
            {
                enumerator.MoveNext();
                throw new NotImplementedException();
                //writer.WriteElement(ElemSerializer, enumerator.Current);
            }
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            object set = NewSet();
            Deserialize(reader, set, ElemSerializer);
            return set;
        }

        public override int Count(object instance)
        {
            return LengthOfSet(instance);
        }

        public override object GetElementAt(object instance, int index)
        {
            throw new NotImplementedException("GetElementAt(index) not supported for Set types");
        }

        public override object CreateInstance(int length)
        {
            return NewSet();
        }

        public override void Deserialize(BinarySerializerReader reader, object instance)
        {
            Deserialize(reader, instance, ElemSerializer);
        }

        void Deserialize(BinarySerializerReader reader, object set, TypeSerializer elemSer)
        {
            int count = (int)reader.BR.ReadVLu32();
            if (count <= 0)
                return;

            TypeInfo elementType = reader.GetType(elemSer);
            for (int i = 0; i < count; ++i)
            {
                object element = reader.ReadElement(elementType, elemSer);
                AddToSet(set, element);
            }
        }
    }
}
