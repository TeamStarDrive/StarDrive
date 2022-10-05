using System;
using System.Collections.Generic;
using System.Diagnostics;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Data.Binary;

/// <summary>
/// Abusing some internal details, this provides
/// a fast flat-map implementation equivalent to
/// Map&lt;TypeSerializer, TValue&gt;
/// </summary>
[DebuggerTypeProxy(typeof(FastMapDebugView))]
public class FastTypeMap
{
    ObjectStateMap[] StateMaps;

    public FastTypeMap(TypeSerializerMap map)
    {
        StateMaps = new ObjectStateMap[map.NumTypes];
    }

    public (ObjectStateMap instMap, bool existing) GetOrAddNew(TypeSerializer ser)
    {
        int index = ser.TypeId;
        // we've encountered brand new abstract types, so map needs to expand
        if (index >= StateMaps.Length)
        {
            int newLength = index.RoundUpToMultipleOf(32);
            Array.Resize(ref StateMaps, newLength);
        }

        ObjectStateMap map = StateMaps[index];
        if (map != null) return (map, existing:true);

        // add brand new map
        StateMaps[index] = map = new(ser);
        return (map, existing:false);
    }

    public void SetValue(TypeSerializer key, ObjectStateMap value)
    {
        int index = key.TypeId;
        // we've encountered brand new abstract types, so map needs to expand
        if (index >= StateMaps.Length) 
        {
            int newLength = index + 32;
            Array.Resize(ref StateMaps, newLength);
        }
        StateMaps[index] = value;
    }

    public bool TryGetValue(TypeSerializer key, out ObjectStateMap value)
    {
        int index = key.TypeId;
        if (index >= StateMaps.Length)
        {
            value = default;
            return false;
        }
        return (value = StateMaps[index]) != null;
    }

    // inefficient access to all valid items
    public ObjectStateMap[] Values => StateMaps.Filter(v => v != null);

    public IEnumerable<ObjectStateMap> GetValues(TypeSerializer[] types)
    {
        for (int i = 0; i < types.Length; ++i)
        {
            ObjectStateMap v = StateMaps[types[i].TypeId];
            if (v != null) // it is allowed to be null, if no types were inserted
                yield return v;
        }
    }
}

internal sealed class FastMapDebugView
{
    readonly FastTypeMap Map;

    public FastMapDebugView(FastTypeMap map) { Map = map; }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public ObjectStateMap[] Items => Map.Values;
}
