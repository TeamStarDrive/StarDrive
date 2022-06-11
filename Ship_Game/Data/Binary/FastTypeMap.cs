using System;
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
        readonly TValue[] TValues;

        public FastTypeMap(TypeSerializerMap map)
        {
            TValues = new TValue[map.NumTypes];
        }

        public TValue this[TypeSerializer key]
        {
            get => TValues[key.TypeId];
            set => TValues[key.TypeId] = value;
        }

        public bool ContainsKey(TypeSerializer key)
        {
            return TValues[key.TypeId] != null;
        }

        public bool TryGetValue(TypeSerializer key, out TValue value)
        {
            return (value = TValues[key.TypeId]) != null;
        }

        public TValue[] Values => TValues.Filter(v => v != null);

        public TValue[] GetValues(Predicate<TValue> filter)
        {
            return TValues.Filter(v => v != null && filter(v));
        }
    }

    internal sealed class FastMapDebugView<TValue> where TValue : class
    {
        readonly FastTypeMap<TValue> Map;

        public FastMapDebugView(FastTypeMap<TValue> map) { Map = map; }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public TValue[] Items => Map.Values.ToArr();
    }
}
