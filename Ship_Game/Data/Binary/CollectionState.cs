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

    public override void Scan(ObjectScanner scanner, TypeSerializer ser)
    {
        var coll = (CollectionSerializer)ser;
        int count = coll.Count(Obj);
        if (count == 0)
            return;

        var typeMap = scanner.RootSer.TypeMap;
        bool valCanBeNull = coll.ElemSerializer.IsPointerType;

        if (coll is MapSerializer maps)
        {
            Items = new uint[count * 2];
            var e = ((IDictionary)Obj).GetEnumerator();
            for (int i = 0; i < count && e.MoveNext(); ++i)
            {
                uint keyId = scanner.ScanObjectState(maps.KeySerializer, e.Key);
                Items[i*2+0] = keyId;
                SetValue(scanner, i*2+1, e.Value, valCanBeNull, typeMap);
            }
        }
        else if (coll is HashSetSerializer)
        {
            Items = new uint[count];
            var e = ((IEnumerable)Obj).GetEnumerator();
            for (int i = 0; i < count && e.MoveNext(); ++i)
            {
                SetValue(scanner, i, e.Current, valCanBeNull, typeMap);
            }
        }
        else
        {
            Items = new uint[count];
            for (int i = 0; i < count; ++i)
            {
                object element = coll.GetElementAt(Obj, i);
                SetValue(scanner, i, element, valCanBeNull, typeMap);
            }
        }
    }

    void SetValue(ObjectScanner scanner, int index, object instance, bool valCanBeNull, TypeSerializerMap typeMap)
    {
        if (valCanBeNull && instance == null)
        {
            Items[index] = 0;
        }
        else
        {
            // NOTE: VALUES CAN USE ABSTRACT TYPES, SO TYPE CHECK IS REQUIRED FOR EACH ELEMENT
            TypeSerializer item = typeMap.Get(instance!.GetType());
            uint valId = scanner.ScanObjectState(item, instance);
            Items[index] = valId;
        }
    }
}
