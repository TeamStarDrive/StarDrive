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

        public new TValue this[TKey key]
        {
            get
            {
                if (TryGetValue(key, out TValue val))
                    return val;
                throw new MapKeyNotFoundException($"Key [{key}] was not found in {ToString()} (len={Count})");
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

        public void AddOrUpdate(TKey key, Func<TValue,TValue> update, Func<TValue> Default)
        {
            if (TryGetValue(key, out TValue val))
                this[key] = update(val);                    
            Add(key,Default());            
        }
    }
}
