using System;
using System.Collections;
using System.Linq.Expressions;
using SDUtils;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    using E = Expression;

    internal class MapSerializer : CollectionSerializer
    {
        public override string ToString() => $"{TypeId}:MapSer<{KeySerializer.TypeId}:{KeyType.GetTypeName()},{ElemSerializer.TypeId}:{ElemType.GetTypeName()}>";
        public readonly Type KeyType;
        public readonly TypeSerializer KeySerializer;
        public readonly Type GenericMapType;

        delegate IDictionary New();
        readonly New NewMap;

        public MapSerializer(Type type,
                             Type keyType, TypeSerializer keySerializer,
                             Type valType, TypeSerializer valSerializer)
            : base(type, valType, valSerializer)
        {
            KeyType = keyType;
            KeySerializer = keySerializer;
            IsMapType = true;
            GenericMapType = typeof(Map<,>).MakeGenericType(keyType, valType);

            // () => (IDictionary)new Map<TKey, TValue>();
            NewMap = E.Lambda<New>(E.Convert(E.New(GenericMapType), typeof(IDictionary))).Compile();
        }

        public override object Convert(object value)
        {
            if (value == null)
                return null;
            Error(value, "Map convert is not supported");
            return value;
        }

        public override object Deserialize(YamlNode node)
        {
            // [StarData] Map<Type, float> Settings;
            // Settings:
            //   House: 1.0
            //   Ship: 2.0
            Array<YamlNode> nodes = node.SequenceOrSubNodes;
            if (nodes?.Count > 0)
            {
                var dict = NewMap();
                for (int i = 0; i < nodes.Count; ++i)
                {
                    YamlNode keyVal = nodes[i];
                    object key = KeySerializer.Convert(keyVal.Key);
                    object val = ElemSerializer.Convert(keyVal.Value);
                    dict.Add(key, val);
                }
                return dict;
            }
            return base.Deserialize(node); // try to deserialize value as Array
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            // [StarData] Map<Type, float> Settings;
            // Settings:
            //   House: 1.0
            //   Ship: 2.0
            var dict = (IDictionary)obj;
            if (dict.Count != 0)
            {
                var e = dict.GetEnumerator();
                while (e.MoveNext())
                {
                    var childObject = new YamlNode();
                    parent.AddSubNode(childObject);

                    // first get value for the key
                    KeySerializer.Serialize(childObject, e.Key);
                    childObject.Key = childObject.Value;

                    // get the value
                    childObject.Value = null;
                    ElemSerializer.Serialize(childObject, e.Value);
                }
            }
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            var dict = (IDictionary)obj;
            writer.BW.WriteVLu32((uint)dict.Count);
            var e = dict.GetEnumerator();
            while (e.MoveNext())
            {
                throw new NotImplementedException();
                //writer.WriteElement(KeySerializer, e.Key);
                //writer.WriteElement(ElemSerializer, e.Value);
            }
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            IDictionary dict = NewMap();
            Deserialize(reader, dict);
            return dict;
        }

        public override int Count(object instance)
        {
            var dict = (IDictionary)instance;
            return dict.Count;
        }

        public override object GetElementAt(object instance, int index)
        {
            throw new NotImplementedException("GetElementAt(index) not supported for Map types");
        }

        public override object CreateInstance()
        {
            return NewMap();
        }

        public override void Deserialize(BinarySerializerReader reader, object instance)
        {
            var dict = (IDictionary)instance;
            int count = (int)reader.BR.ReadVLu32();

            for (int i = 0; i < count; i += 2)
            {
                object key = reader.ReadPointer();
                object val = reader.ReadPointer();
                dict.Add(key, val);
            }
        }
    }
}
