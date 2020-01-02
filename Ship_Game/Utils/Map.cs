using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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

        public Map(IEnumerable<ValueTuple<TKey, TValue>> elements) : base(0, null)
        {
            foreach ((TKey key, TValue value) in elements)
                Add(key, value);
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
            return (this as ICollection<KeyValuePair<TKey, TValue>>).ToArray();
        }

        public TValue[] AtomicValuesArray()
        {
            lock (this) return Values.ToArray();
        }
    }
}
