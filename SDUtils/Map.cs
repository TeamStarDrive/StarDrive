using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SDUtils
{
    public class MapKeyNotFoundException : Exception
    {
        public MapKeyNotFoundException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// This is a custom wrapper of Dictionary to make debugging easier
    /// </summary>
    [Serializable]
    [XmlRoot("dictionary")]
    public class Map<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    {
        public Map() : base(0, null)
        {
        }
        
        public Map(int capacity) : base(capacity, null)
        {
        }

        public Map(IEqualityComparer<TKey> comparer) : base(0, comparer)
        {
        }

        public Map(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer)
        {
        }

        public Map(IEnumerable<ValueTuple<TKey, TValue>> elements) : base(0, null)
        {
            foreach ((TKey key, TValue value) in elements)
                Add(key, value);
        }

        public Map(Dictionary<TKey, TValue> dictionary) : base (dictionary)
        {
        }
        

        // Separated throw from this[] to enable MSIL inlining
        void ThrowMapKeyNotFound(TKey key)
        {
            throw new MapKeyNotFoundException($"Key [{key}] was not found in {ToString()} (len={Count})");
        }

        public new TValue this[TKey key]
        {
            get
            {
                if (!TryGetValue(key, out TValue val))
                    ThrowMapKeyNotFound(key);
                return val;
            }
            set
            {
                base[key] = value;
            }
        }

        public void Add(ValueTuple<TKey, TValue> pair)
        {
            base.Add(pair.Item1, pair.Item2);
        }

        public override string ToString()
        {
            return GetType().GetTypeName();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(TKey key, out TValue value)
        {
            return base.TryGetValue(key, out value);
        }

        // map[key] = map[key] + valueToAdd;
        // Starting value is default(TValue): 0 for numeric types
        // TValue must have operator + defined
        public void AddToValue(TKey key, dynamic valueToAdd)
        {
            TryGetValue(key, out TValue old);
            base[key] = (dynamic)old + valueToAdd;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            return (this as ICollection<KeyValuePair<TKey, TValue>>).ToArr();
        }

        public TValue[] AtomicValuesArray()
        {
            lock (this) return Values.ToArr();
        }

        // LEGACY XML SUPPORT //

        public XmlSchema GetSchema()
        {
            return null;
        }
        
        public void ReadXml(XmlReader reader) // IXmlSerializable
        {
            var keySerializer   = new XmlSerializer(typeof(TKey));
            var valueSerializer = new XmlSerializer(typeof(TValue));
            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();
            if (wasEmpty)
            {
                return;
            }
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                reader.ReadStartElement("item");
                reader.ReadStartElement("key");
                var key = (TKey)keySerializer.Deserialize(reader);
                reader.ReadEndElement();
                reader.ReadStartElement("value");
                var value = (TValue)valueSerializer.Deserialize(reader);
                reader.ReadEndElement();
                Add(key, value);
                reader.ReadEndElement();
                reader.MoveToContent();
            }
            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer) // IXmlSerializable
        {
            var keySerializer = new XmlSerializer(typeof(TKey));
            var valueSerializer = new XmlSerializer(typeof(TValue));
            foreach (TKey key in Keys)
            {
                writer.WriteStartElement("item");
                writer.WriteStartElement("key");
                keySerializer.Serialize(writer, key);
                writer.WriteEndElement();
                writer.WriteStartElement("value");
                valueSerializer.Serialize(writer, base[key]);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }
    }
}
