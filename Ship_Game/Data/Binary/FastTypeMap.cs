using System;
using System.Collections.Generic;
using System.Diagnostics;
using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Data.Binary
{
    /// <summary>
    /// Abusing some internal details, this provides
    /// a fast flat-map implementation equivalent to
    /// Map&lt;TypeSerializer, TValue&gt;
    /// </summary>
    [DebuggerTypeProxy(typeof(FastMapDebugView<>))]
    public class FastTypeMap<TValue> where TValue : class
    {
        TValue[] TValues;

        public FastTypeMap(TypeSerializerMap map)
        {
            TValues = new TValue[map.NumTypes];
        }

        public bool ContainsKey(TypeSerializer key)
        {
            return TValues[key.TypeId] != null;
        }

        public TValue GetValue(TypeSerializer key)
        {
            return TValues[key.TypeId];
        }

        public void SetValue(TypeSerializer key, TValue value)
        {
            int index = key.TypeId;
            if (index >= TValues.Length) // we've encountered brand new abstract types, so map needs to expand
            {
                int newLength = index + 32;
                Array.Resize(ref TValues, newLength);
            }
            TValues[index] = value;
        }

        public bool TryGetValue(TypeSerializer key, out TValue value)
        {
            int index = key.TypeId;
            if (index >= TValues.Length)
            {
                value = default;
                return false;
            }
            return (value = TValues[index]) != null;
        }

        public TValue[] Values => TValues.Filter(v => v != null);

        public TValue[] GetValues(Predicate<TValue> filter)
        {
            return TValues.Filter(v => v != null && filter(v));
        }

        public IEnumerable<TValue> GetValues()
        {
            foreach (TValue v in TValues)
                if (v != null) yield return v;
        }
    }

    internal sealed class FastMapDebugView<TValue> where TValue : class
    {
        readonly FastTypeMap<TValue> Map;

        public FastMapDebugView(FastTypeMap<TValue> map) { Map = map; }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public TValue[] Items => Map.Values;
    }
}
