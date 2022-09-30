using System;
using System.Collections;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Serialization.Types;

namespace Ship_Game.Data.Binary;

// T[], Array<T>, Map<K,V> or HashSet<T>
public class CollectionState : ObjectState
{
    public uint[] Items;

    public CollectionState(object obj, uint id) : base(obj, id)
    {
    }

    public override void Serialize(BinarySerializerWriter w, TypeSerializer ser)
    {
        if (Items == null)
        {
            w.BW.WriteVLu32(0u); // collection length
        }
        else
        {
            w.BW.WriteVLu32((uint)Items.Length); // collection length
            for (int i = 0; i < Items.Length; ++i)
                w.BW.WriteVLu32(Items[i]);
        }
    }

    public override void Remap(uint[] map)
    {
        Remap(map, Items);
    }

    public override void Scan(ObjectScanner scanner, TypeSerializer ser, DataField owner)
    {
        var coll = (CollectionSerializer)ser;
        int count = coll.Count(Obj);
        if (count == 0)
            return;

        int i = 0;
        try
        {
            if (coll is MapSerializer maps)
            {
                Items = new uint[count * 2];
                var e = ((IDictionary)Obj).GetEnumerator();
                for (; i < count && e.MoveNext(); ++i)
                {
                    Items[i*2+0] = scanner.ScanObjectState(maps.KeySerializer, e.Key, owner);
                    Items[i*2+1] = scanner.ScanObjectState(maps.ElemSerializer, e.Value, owner);
                }
            }
            else if (coll is HashSetSerializer)
            {
                Items = new uint[count];
                var e = ((IEnumerable)Obj).GetEnumerator();
                for (; i < count && e.MoveNext(); ++i)
                {
                    Items[i] = scanner.ScanObjectState(coll.ElemSerializer, e.Current, owner);
                }
            }
            else
            {
                Items = new uint[count];
                for (; i < count; ++i)
                {
                    object element = coll.GetElementAt(Obj, i);
                    Items[i] = scanner.ScanObjectState(coll.ElemSerializer, element, owner);
                }
            }
        }
        catch (Exception ex)
        {
            // This can happen due to a multi-threading violation during Autosave
            Log.Error(ex, $"{owner?.DeclaringType.Name}::{owner?.Name} {coll} get element {i} failed! ExpectedLen={count} ActualLen={coll.Count(Obj)} Another thread has modified the collection!");
        }
    }
}
