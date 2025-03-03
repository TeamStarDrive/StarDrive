﻿using System;
using System.Collections;
using System.Linq.Expressions;
using SDUtils;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    using E = Expression;

    internal class ArrayListSerializer : CollectionSerializer
    {
        public override string ToString() => $"{TypeId}:ListSer<{ElemSerializer.TypeId}:{ElemType.GetTypeName()}>";
        readonly Type GenericArrayType;

        delegate IList New();
        delegate void Add(IList list, object item);
        delegate void Resize(IList list, int newSize);

        New NewList;
        Add AddToList;
        Resize ResizeTo;

        public ArrayListSerializer(Type type, Type elemType, TypeSerializer elemSerializer)
            : base(type, elemType, elemSerializer)
        {
            GenericArrayType = typeof(Array<>).MakeGenericType(elemType);

            // precompile array accesses to avoid horrible performance of naive Reflection
            // read more at: https://docs.microsoft.com/en-us/dotnet/api/system.linq.expressions.expression?view=netframework-4.8
            // lazy initialization pattern, each method will replace itself with the compiled version when called
            NewList = InitNewList;
            AddToList = InitAdd;
        }

        IList InitNewList()
        {
            // () => (IList)new Array<T>();
            NewList = E.Lambda<New>(E.Convert(E.New(GenericArrayType), typeof(IList))).Compile();
            return NewList();
        }

        void InitAdd(IList lst, object itm)
        {
            var list = E.Parameter(typeof(IList), "list");
            var item = E.Parameter(typeof(object), "item");
            var toArrayT = E.Convert(list, GenericArrayType);

            // (object list, object item) => ((Array<T>)list).Add((T)item);
            var add = GenericArrayType.GetMethod("Add") ?? throw new MissingMethodException("Array<T>.Add(T)");
            AddToList = E.Lambda<Add>(E.Call(toArrayT, add, E.Convert(item, ElemType)), list, item).Compile();
            AddToList(lst, itm);
        }

        bool InitializedResizeTo;

        bool TryResize(IList lst, int count)
        {
            if (!InitializedResizeTo)
            {
                InitializedResizeTo = true;

                var list = E.Parameter(typeof(IList), "list");
                var newSize = E.Parameter(typeof(int), "newSize");
                var toArrayT = E.Convert(list, GenericArrayType);

                // (object list, int newSize) => ((Array<T>)list).Resize(newSize);
                var resize = GenericArrayType.GetMethod("Resize");
                if (resize != null)
                    ResizeTo = E.Lambda<Resize>(E.Call(toArrayT, resize, newSize), list, newSize).Compile();
            }

            if (ResizeTo != null)
            {
                ResizeTo(lst, count);
                return true;
            }
            return false;
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

        object ConvertList(IList list, object[] array, TypeSerializer elemSer)
        {
            if (TryResize(list, array.Length))
            {
                for (int i = 0; i < array.Length; ++i)
                {
                    object element = elemSer.Convert(array[i]);
                    list[i] = element;
                }
            }
            else
            {
                for (int i = 0; i < array.Length; ++i)
                {
                    object element = elemSer.Convert(array[i]);
                    list.Add(element);
                }
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
                if (TryResize(list, nodes.Count))
                {
                    for (int i = 0; i < nodes.Count; ++i)
                    {
                        object element = ElemSerializer.Deserialize(nodes[i]);
                        list[i] = element;
                    }
                }
                else
                {
                    for (int i = 0; i < nodes.Count; ++i)
                    {
                        object element = ElemSerializer.Deserialize(nodes[i]);
                        list.Add(element);
                    }
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
                        var seqElem = new YamlNode() { Key = ser.TypeName };
                        ser.Serialize(seqElem, element);
                        parent.AddSequenceElement(seqElem);
                    }
                }
            }
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            Serialize((IList)obj, ElemSerializer, parent);
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            throw new NotSupportedException();
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            var list = NewList();
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

        public override object CreateInstance()
        {
            return NewList();
        }

        public override void Deserialize(BinarySerializerReader reader, object instance)
        {
            Deserialize(reader, (IList)instance, ElemSerializer);
        }

        void Deserialize(BinarySerializerReader reader, IList list, TypeSerializer elemSer)
        {
            int count = (int)reader.BR.ReadVLu32();
            if (count <= 0)
                return;

            if (TryResize(list, count))
            {
                for (int i = 0; i < count; ++i)
                {
                    object element = reader.ReadCollectionElement(elemSer);
                    list[i] = element;
                }
            }
            else
            {
                for (int i = 0; i < count; ++i)
                {
                    object element = reader.ReadCollectionElement(elemSer);
                    AddToList(list, element);
                }
            }
        }
    }
}