using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SDUtils;

/// <summary>
/// A stable collection whose item indices never
/// change even if items are removed.
/// 
/// </summary>
public class StableCollection<T> : ICollection<T>
{
    const int SlabSize = 256;

    public int Count { get; private set; }
    public bool IsReadOnly => false;

    readonly Array<Slab> Slabs = new(); // for random access

    class Slab
    {
        public readonly int BaseIndex;
        // number of reserved items
        public int MaxItems;
        public readonly T[] Items = new T[SlabSize];

        // if a bit is set, that index is free
        // this makes finding free slots easier since we can check `bits != 0`
        BitArray FreeSlots = new(SlabSize);

        public Slab() {}

        public Slab(T item, int baseIndex)
        {
            Items[0] = item;
            BaseIndex = baseIndex;
            MaxItems = 1;
        }

        public void Clear()
        {
            MaxItems = 0;
            FreeSlots.Clear();
            Array.Clear(Items, 0, SlabSize);
        }

        public void Free(int itemIndex)
        {
            FreeSlots.Set(itemIndex);
            Items[itemIndex] = default;
        }

        public bool IsFree(int itemIndex)
        {
            return FreeSlots.IsSet(itemIndex);
        }

        public bool PopFreeSlot(out int globalIndex)
        {
            int freeItemIndex = FreeSlots.GetFirstSetBitIndex();
            if (freeItemIndex != -1)
            {
                globalIndex = BaseIndex + freeItemIndex;
                return true;
            }
            globalIndex = -1;
            return false;
        }
    }

    public StableCollection()
    {
        Slabs.Add(new());
    }

    public void Clear()
    {
        Count = 0;
        Slabs.Resize(1);
        Slabs.Last.Clear();
    }

    /// <summary>
    /// Get a stable reference to an item in this collection
    /// Throws an error if the item was freed
    /// </summary>
    public ref T GetReferenceOf(int index)
    {
        int whichSlab = index / SlabSize;
        int itemIndex = index % SlabSize;
        Slab slab = Slabs[whichSlab];
        if (slab.IsFree(itemIndex))
            throw new IndexOutOfRangeException($"Item was freed at index={index}");
        return ref slab.Items[itemIndex];
    }

    /// <summary>
    /// Inserts an item into the StableCollection
    /// and returns the index where it was inserted.
    /// </summary>
    public int Insert(T item)
    {
        if (item == null)
            throw new NullReferenceException(nameof(item));

        if (GetFreeSlot(out int index)) // got any free slots?
        {
            int whichSlab = index / SlabSize;
            int itemIndex = index % SlabSize;
            Slabs[whichSlab].Items[itemIndex] = item;
            ++Count;
            return index;
        }

        Slab last = Slabs.Last;
        if (last.MaxItems == SlabSize) // need to add new
        {
            int baseIndex = Slabs.Count * SlabSize;
            Slabs.Add(new(item, baseIndex));
            ++Count;
            return baseIndex;
        }
        else // append to last Slab
        {
            index = last.MaxItems++;
            last.Items[index] = item;
            ++Count;
            return last.BaseIndex + index;
        }
    }

    bool GetFreeSlot(out int index)
    {
        for (int i = 0; i < Slabs.Count; ++i)
            if (Slabs[i].PopFreeSlot(out index))
                return true;
        index = -1;
        return false;
    }

    public void RemoveAt(int index)
    {
        int whichSlab = index / SlabSize;
        int itemIndex = index % SlabSize;
        Slab slab = Slabs[whichSlab];
        if (!slab.IsFree(itemIndex)) // make sure it's not already freed
        {
            slab.Free(itemIndex);
            --Count;
        }
    }

    void ICollection<T>.Add(T item)
    {
        Insert(item);
    }

    public bool Contains(T item)
    {
        return IndexOf(item) != -1;
    }

    public bool Remove(T item)
    {
        if (item == null)
            throw new NullReferenceException(nameof(item));

        EqualityComparer<T> c = EqualityComparer<T>.Default;
        for (int slabIndex = 0; slabIndex < Slabs.Count; ++slabIndex)
        {
            Slab slab = Slabs[slabIndex];
            for (int itemIndex = 0; itemIndex < slab.MaxItems; ++itemIndex)
            {
                if (!slab.IsFree(itemIndex) && 
                    c.Equals(slab.Items[itemIndex], item))
                {
                    slab.Free(itemIndex);
                    --Count;
                    return true;
                }
            }
        }
        return false;
    }

    public int IndexOf(T item)
    {
        if (item == null)
            throw new NullReferenceException(nameof(item));

        EqualityComparer<T> c = EqualityComparer<T>.Default;
        for (int slabIndex = 0; slabIndex < Slabs.Count; ++slabIndex)
        {
            Slab slab = Slabs[slabIndex];
            for (int itemIndex = 0; itemIndex < slab.MaxItems; ++itemIndex)
            {
                if (!slab.IsFree(itemIndex) && 
                    c.Equals(slab.Items[itemIndex], item))
                    return slab.BaseIndex + itemIndex;
            }
        }
        return -1;
    }
    
    /// <summary>
    /// Enumerates through all Items using an IEnumerable generator
    /// </summary>
    public IEnumerable<T> Items
    {
        get
        {
            for (int slabIndex = 0; slabIndex < Slabs.Count; ++slabIndex)
            {
                Slab slab = Slabs[slabIndex];
                for (int itemIndex = 0; itemIndex < slab.MaxItems; ++itemIndex)
                {
                    if (!slab.IsFree(itemIndex))
                        yield return slab.Items[itemIndex];
                }
            }
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int slabIndex = 0; slabIndex < Slabs.Count; ++slabIndex)
        {
            Slab slab = Slabs[slabIndex];
            for (int itemIndex = 0; itemIndex < slab.MaxItems; ++itemIndex)
            {
                if (!slab.IsFree(itemIndex))
                    yield return slab.Items[itemIndex];
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void CopyTo(T[] array, int arrayIndex)
    {
        int offset = arrayIndex;
        foreach (T item in Items)
            array[offset++] = item;
    }

    public T[] ToArr()
    {
        var arr = new T[Count];
        CopyTo(arr, 0);
        return arr;
    }
}
