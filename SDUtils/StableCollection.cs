using System;
using System.Collections;
using System.Collections.Generic;

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
    public int Capacity => Slabs.Count * SlabSize;

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

        public Slab(int baseIndex)
        {
            BaseIndex = baseIndex;
        }

        public Slab(T item, int baseIndex)
        {
            Items[0] = item;
            BaseIndex = baseIndex;
            MaxItems = 1;
        }

        public void Clear()
        {
            Array.Clear(Items, 0, MaxItems);
            FreeSlots.Clear();
            MaxItems = 0;
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
                FreeSlots.Unset(freeItemIndex);
                globalIndex = BaseIndex + freeItemIndex;
                return true;
            }
            globalIndex = -1;
            return false;
        }
    }

    /// <summary>
    /// Create a new StableCollection with a single default slab
    /// </summary>
    public StableCollection()
    {
        Slabs.Add(new(baseIndex:0));
    }

    /// <summary>
    /// Dumps all slabs and only keeps the first one as reserve
    /// </summary>
    public void Clear()
    {
        Count = 0;
        Slabs.Resize(1);
        Slabs.Last.Clear();
    }

    /// <summary>
    /// A convenience method which resets all items
    /// without having to free the underlying slabs
    /// </summary>
    public void Reset(IReadOnlyList<T> newItems)
    {
        foreach (Slab s in Slabs)
        {
            s.Clear();
        }
        while (Capacity < newItems.Count)
        {
            int baseIndex = Slabs.Count * SlabSize;
            Slabs.Add(new(baseIndex: baseIndex));
        }

        for (int slab = 0; slab < Slabs.Count; ++slab)
        {
            Slab s = Slabs[slab];
            int src = slab*SlabSize;
            for (int i = 0; i < SlabSize && src < newItems.Count; ++i)
            {
                s.Items[s.MaxItems++] = newItems[src++];
            }
        }

        Count = newItems.Count;
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

    void ICollection<T>.Add(T item)
    {
        Insert(item);
    }

    bool GetFreeSlot(out int index)
    {
        for (int i = 0; i < Slabs.Count; ++i)
            if (Slabs[i].PopFreeSlot(out index))
                return true;
        index = -1;
        return false;
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

    /// <summary>
    /// Removes an item at `index` if that item is not already freed
    /// </summary>
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

    /// <summary>
    /// Removes an item if it exists and isn't freed.
    /// Item cannot be null.
    /// </summary>
    public bool Remove(T item)
    {
        if (item == null)
            throw new NullReferenceException(nameof(item));

        var c = EqualityComparer<T>.Default;
        for (int slabIndex = 0; slabIndex < Slabs.Count; ++slabIndex)
        {
            Slab slab = Slabs[slabIndex];
            for (int itemIndex = 0; itemIndex < slab.MaxItems; ++itemIndex)
            {
                if (!slab.IsFree(itemIndex) && c.Equals(slab.Items[itemIndex], item))
                {
                    slab.Free(itemIndex);
                    --Count;
                    return true;
                }
            }
        }
        return false;
    }
    
    /// <summary>
    /// TRUE if an item exists and isn't freed. Item cannot be null
    /// </summary>
    public bool Contains(T item)
    {
        return IndexOf(item) != -1;
    }
    
    /// <summary>
    /// Index of an item, or -1 if it doesn't exist or has been freed.
    /// Item cannot be null.
    /// </summary>
    public int IndexOf(T item)
    {
        if (item == null)
            throw new NullReferenceException(nameof(item));

        var c = EqualityComparer<T>.Default;
        for (int slabIndex = 0; slabIndex < Slabs.Count; ++slabIndex)
        {
            Slab slab = Slabs[slabIndex];
            for (int itemIndex = 0; itemIndex < slab.MaxItems; ++itemIndex)
                if (!slab.IsFree(itemIndex) && c.Equals(slab.Items[itemIndex], item))
                    return slab.BaseIndex + itemIndex;
        }
        return -1;
    }
    
    /// <summary>
    /// Enumerates through all items using an IEnumerable generator
    /// </summary>
    public IEnumerable<T> Items
    {
        get
        {
            for (int slabIndex = 0; slabIndex < Slabs.Count; ++slabIndex)
            {
                Slab slab = Slabs[slabIndex];
                for (int itemIndex = 0; itemIndex < slab.MaxItems; ++itemIndex)
                    if (!slab.IsFree(itemIndex))
                        yield return slab.Items[itemIndex];
            }
        }
    }

    /// <summary>
    /// Enumerates through all items using a generator
    /// </summary>
    public IEnumerator<T> GetEnumerator()
    {
        for (int slabIndex = 0; slabIndex < Slabs.Count; ++slabIndex)
        {
            Slab slab = Slabs[slabIndex];
            for (int itemIndex = 0; itemIndex < slab.MaxItems; ++itemIndex)
                if (!slab.IsFree(itemIndex))
                    yield return slab.Items[itemIndex];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Copies all elements to destination `array`.
    /// It must have at least `this.Count` items of free space.
    /// </summary>
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

    // For TESTING
    public bool IsFreeSlot(int index)
    {
        int whichSlab = index / SlabSize;
        int itemIndex = index % SlabSize;
        return Slabs[whichSlab].IsFree(itemIndex);
    }
}
