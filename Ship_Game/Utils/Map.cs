using System;
using System.Collections.Generic;

namespace Ship_Game
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
    public class Map<TKey, TValue> : Dictionary<TKey, TValue>
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

        // Separated throw from this[] to enable MSIL inlining
        private void ThrowMapKeyNotFound(TKey key)
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

        public override string ToString()
        {
            return GetType().GenericName();
        }

        // map[key] = map[key] + valueToAdd;
        // Starting value is default(TValue): 0 for numeric types
        // TValue must have operator + defined
        public void AddToValue(TKey key, dynamic valueToAdd)
        {
            TryGetValue(key, out TValue old);
            base[key] = (dynamic)old + valueToAdd;
        }

        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            return (this as ICollection<KeyValuePair<TKey, TValue>>).ToArray();
        }
    }
}
