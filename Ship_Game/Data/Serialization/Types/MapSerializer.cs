using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    internal class MapSerializer : TypeSerializer
    {
        public override string ToString() => $"MapSerializer<{KeyType.GetTypeName()}, {ValType.GetTypeName()}>";
        readonly Type KeyType;
        readonly Type ValType;
        readonly TypeSerializer KeySerializer;
        readonly TypeSerializer ValSerializer;

        public MapSerializer(Type type,
                             Type keyType, TypeSerializer keySerializer,
                             Type valType, TypeSerializer valSerializer) : base(type)
        {
            KeyType = keyType;
            ValType = valType;
            KeySerializer = keySerializer;
            ValSerializer = valSerializer;
            IsCollection = true;
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
                IDictionary dict = MapHelper.NewMapOfT(KeyType, ValType);
                for (int i = 0; i < nodes.Count; ++i)
                {
                    YamlNode keyVal = nodes[i];
                    object key = KeySerializer.Convert(keyVal.Key);
                    object val = ValSerializer.Convert(keyVal.Value);
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
                    ValSerializer.Serialize(childObject, e.Value);
                }
            }
        }

        public override void Serialize(BinaryWriter writer, object obj)
        {
            var dict = (IDictionary)obj;
            writer.Write(dict.Count);
            var e = dict.GetEnumerator();
            while (e.MoveNext())
            {
                KeySerializer.Serialize(writer, e.Key);
                ValSerializer.Serialize(writer, e.Value);
            }
        }
        
        public override object Deserialize(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            IDictionary dict = MapHelper.NewMapOfT(KeyType, ValType);
            for (int i = 0; i < count; ++i)
            {
                object key = KeySerializer.Deserialize(reader);
                object val = ValSerializer.Deserialize(reader);
                dict.Add(key, val);
            }
            return dict;
        }
    }
}
