using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Utils
{
    // Using a secondary base class, so that we can try multiple
    // different Locking schemes
    public class SafeArrayBase<T>
    {
        protected T[] Items = Empty<T>.Array;
        public int Count { get; protected set; }

        public SafeArrayBase()
        {
        }

        public SafeArrayBase(ICollection<T> collection)
        {
            if ((Count = collection.Count) <= 0) Items = Empty<T>.Array;
            else collection.CopyTo(Items = new T[Count], 0);
        }

        public T Get(int index)
        {
            if ((uint)index >= (uint)Count)
                ThrowIndexOutOfBounds(index);
            return Items[index];
        }

        public void Set(int index, T value)
        {
            if ((uint)index >= (uint)Count)
                ThrowIndexOutOfBounds(index);
            Items[index] = value;
        }

        public override string ToString()
        {
            return GetType().GetTypeName();
        }

        // Separated throw from this[] to enable MS IL inlining
        protected void ThrowIndexOutOfBounds(int index)
        {
            throw new IndexOutOfRangeException($"Index [{index}] out of range({Count}) {ToString()}");
        }

        protected void GrowUnlocked(int capacity)
        {
            if (capacity >= 4)
            {
                // Array<T> will grow by 2.0x during Add/Insert
                // In our tests this had less GC pressure and re-allocations than 1.5x
                capacity *= 2;

                int rem = capacity & 3; // align capacity to a multiple of 4
                if (rem != 0) capacity += 4 - rem;
            }
            else capacity = 4;

            var newArray = new T[capacity];
            if (Count != 0)
                Array.Copy(Items, 0, newArray, 0, Count);
            Items = newArray;
        }

        protected void AddUnlocked(T item)
        {
            int capacity = Items.Length;
            int count = Count;
            if (count == capacity)
                GrowUnlocked(capacity);
            Items[count] = item;
            Count = count + 1;
        }

        protected void ClearUnlocked()
        {
            // nulls all references/struct fields to avoid GC leaks
            Array.Clear(Items, 0, Count);
            Count = 0;
        }

        protected bool ContainsUnlocked(T item)
        {
            int count = Count;
            if (item == null)
            {
                T[] items = Items;
                for (int i = 0; i < count; ++i)
                    if (items[i] == null) return true;
            }
            else
            {
                EqualityComparer<T> c = EqualityComparer<T>.Default;
                T[] items = Items;
                for (int i = 0; i < count; ++i)
                    if (c.Equals(items[i], item)) return true;
            }
            return false;
        }

        protected void CopyToUnlocked(T[] array, int arrayIndex)
        {
            Memory.HybridCopy(array, arrayIndex, Items, Count);
        }

        protected int IndexOfUnlocked(T item)
        {
            int count = Count;
            T[] items = Items;
            EqualityComparer<T> c = EqualityComparer<T>.Default;
            for (int i = 0; i < count; ++i)
                if (c.Equals(items[i], item)) return i;
            return -1;
        }

        protected void RemoveAtUnlocked(int index)
        {
            int count = Count;
            if ((uint)index >= (uint)count)
                ThrowIndexOutOfBounds(index);

            Count = --count;
            if (index < count) Array.Copy(Items, index + 1, Items, index, count - index);
            Items[count] = default;
        }

        protected bool RemoveUnlocked(T item)
        {
            int i = IndexOfUnlocked(item);
            if (i < 0) return false;
            RemoveAtUnlocked(i);
            return true;
        }

        protected void InsertUnlocked(int index, T item)
        {
            int count = Count;
            if ((uint)index > (uint)count)
                ThrowIndexOutOfBounds(index);

            if (count == Items.Length)
                GrowUnlocked(Items.Length);

            if (index < count) Array.Copy(Items, index, Items, index + 1, count - index);
            Items[index] = item;
            Count = count + 1;
        }

        protected bool RemoveSwapLastUnlocked(T item)
        {
            int i = IndexOfUnlocked(item);
            if (i < 0) return false;

            int last = --Count;
            Items[i]    = Items[last];
            Items[last] = default;
            return true;
        }

        protected void RemoveAtSwapLastUnlocked(int index)
        {
            if ((uint)index >= (uint)Count)
                ThrowIndexOutOfBounds(index);
            int last = --Count;
            Items[index] = Items[last];
            Items[last]  = default;
        }

        protected T PopFirstUnlocked()
        {
            if (Count == 0)
                ThrowIndexOutOfBounds(0);

            T item = Items[0];
            --Count;
            Array.Copy(Items, 1, Items, 0, Count); // unshift
            Items[Count] = default;
            return item;
        }

        protected T PopLastUnlocked()
        {
            if (Count == 0)
                ThrowIndexOutOfBounds(0);
            --Count;
            T item = Items[Count];
            Items[Count] = default;
            return item;
        }

        protected bool TryPopLastUnlocked(out T item)
        {
            if (Count == 0)
            {
                item = default;
                return false;
            }
            item = Items[--Count];
            Items[Count] = default;
            return true;
        }
    }
}
