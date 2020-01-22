using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    /**
     * A queue with efficient push and pop
     */
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}  Capacity = {Capacity}")]
    [Serializable]
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Local")]
    public class Deque<T> : IList<T>, IReadOnlyList<T>, ICollection<T>, IEnumerable<T>, IList, ICollection, IEnumerable
    {
        protected T[] Items = Empty<T>.Array;
        // start of first valid item, this enables Deque to efficiently insert to front and back
        protected int Start; 
        public int Count { get; protected set; }
        public bool IsReadOnly => false;
        public bool IsFixedSize => false;
        public bool IsEmpty    => Count == 0;
        public bool NotEmpty   => Count != 0;
        public object SyncRoot       => this;  // ICollection
        public bool   IsSynchronized => false; // ICollection

        public Deque()
        {
        }

        public Deque(ICollection<T> collection)
        {
            int count = collection.Count;
            if (count > 0)
            {
                Count = count;
                Grow(count);
                collection.CopyTo(Items, Start);
            }
        }

        // Separated throw from this[] to enable MSIL inlining
        void ThrowIndexOutOfBounds(int index)
        {
            throw new IndexOutOfRangeException($"Index [{index}] out of range({Count}) {ToString()}");
        }

        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Count)
                    ThrowIndexOutOfBounds(index);
                return Items[Start + index];
            }
            set
            {
                if ((uint)index >= (uint)Count)
                    ThrowIndexOutOfBounds(index);
                Items[Start + index] = value;
            }
        }

        // First element in the list
        public T First => this[Start];

        // Last element in the list
        public T Last  => this[Start + Count - 1];

        // Get/Set the exact capacity of this Array<T>
        public int Capacity
        {
            get => Items.Length;
            set
            {
                if (value > Items.Length) // manually inlined to improve performance
                {
                    var newArray = new T[value];
                    Array.Copy(Items, 0, newArray, 0, Items.Length);
                    Items = newArray;
                }
            }
        }

        static int AlignCapacity(int capacity)
        {
            int rem = capacity & 3; // align capacity to a multiple of 4
            if (rem != 0) return capacity + (4 - rem);
            return capacity;
        }

        void Grow(int capacity)
        {
            if (capacity >= 12)
            {
                // Dequeue<T> will grow by 3.0x during Add/Insert
                // In our tests this had less GC pressure and reallocations than 1.5x
                capacity *= 3;
                capacity = AlignCapacity(capacity);
            }
            else capacity = 12;

            var newArray = new T[capacity];
            int newStart = capacity/3;
            if (Count != 0)
                Array.Copy(Items, Start, newArray, newStart, Count);
            Start = newStart;
            Items = newArray;
        }

        // Amortized push to the back of the deque
        public void Add(T item)
        {
            if ((Start + Count) == Items.Length)
                Grow(Items.Length);
            Items[(Start + Count)] = item;
            ++Count;
        }

        // Amortized push to the front of the Deque
        public void PushToFront(T item)
        {
            if (Start == 0)
                Grow(Items.Length);
            Items[--Start] = item;
            ++Count;
        }

        public T PopLast()
        {
            if (Count == 0)
                ThrowIndexOutOfBounds(0);
            --Count;
            T item = Items[Start+Count];
            Items[Start+Count] = default;
            return item;
        }

        public T PopFirst()
        {
            if (Count == 0)
                ThrowIndexOutOfBounds(0);
            T item = Items[Start];
            Items[Start] = default;
            --Count;
            ++Start;
            return item;
        }

        public void Clear()
        {
            int count = Count;
            if (count == 0)
                return;
            // nulls all references/structfields to avoid GC leaks
            Array.Clear(Items, Start, count); 
            Start = Items.Length / 3;
            Count = 0;
        }

        public bool Contains(T item)
        {
            int count = Count;
            if (count == 0)
                return false;
            
            int start = Start;
            int end = start + count;
            T[] items = Items;
            if (item == null)
            {
                for (int i = start; i < end; ++i)
                    if (items[i] == null) return true;
                return false;
            }
            EqualityComparer<T> c = EqualityComparer<T>.Default;
            for (int i = start; i < end; ++i)
                if (c.Equals(items[i], item)) return true;
            return false;
        }
        
        // Removes a single occurence of an item
        public bool Remove(T item)
        {
            int i = IndexOf(item);
            if (i < 0) return false;
            RemoveAt(i);
            return true;
        }
        
        public int IndexOf(T item)
        {
            EqualityComparer<T> c = EqualityComparer<T>.Default;
            int start = Start;
            int end = start + Count;
            T[] items = Items;
            for (int i = start; i < end; ++i)
                if (c.Equals(items[i], item)) return i-start;
            return -1;
        }

        public void Insert(int index, T item)
        {
            int count = Count;
            if ((uint)index > (uint)count)
                ThrowIndexOutOfBounds(index);

            int middle = Items.Length / 2;
            if (index < middle) // deque insert to front, shift towards start
            {
                if (Start == 0)
                    Grow(Items.Length);

                if (index > 0) Array.Copy(Items, Start, Items, Start-1, index);
                Items[--Start + index] = item;
            }
            else // deque insert to back, shift towards end
            {
                if ((Start+count) == Items.Length)
                    Grow(Items.Length);

                int startIndex = Start + index;
                if (index < count) Array.Copy(Items, startIndex, Items, startIndex + 1, count - index);
                Items[startIndex] = item;
            }
            Count = count + 1;
        }

        public void RemoveAt(int index)
        {
            int count = Count;
            if ((uint)index >= (uint)count)
                ThrowIndexOutOfBounds(index);

            Count = --count;
            int middle = Items.Length / 2;
            if (index < middle) // deque erase from front, shift towards middle
            {
                if (index != 0)
                {
                    Array.Copy(Items, Start, Items, Start + 1, index);
                }
                Items[Start++] = default;
            }
            else // deque erase from back, shift towards middle
            {
                if (index < count)
                {
                    int startIndex = Start + index;
                    Array.Copy(Items, startIndex + 1, Items, startIndex, count - index);
                }
                Items[Start+count] = default;
            }
        }
        
        public void CopyTo(T[] array, int arrayIndex)
        {
            int count = Count;
            if (count != 0) Array.Copy(Items, Start, array, arrayIndex, count);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            int count = Count;
            if (count != 0) Array.Copy(Items, Start, array, index, count);
        }

        /////////// IList interface /////////

        object IList.this[int index]  { get => this[index]; set => this[index] = (T)value; }
        int IList.Add(object value)
        {
            int insertedAt = Count; Add((T)value); return insertedAt;
        }
        bool IList.Contains(object value) => Contains((T)value);
        int IList.IndexOf(object value)   => IndexOf((T)value);
        void IList.Remove(object value)   => Remove((T)value);
        void IList.Insert(int index, object value) => Insert(index, (T)value);

        ///////////////////////////////////////

        public override string ToString()
        {
            return GetType().GetTypeName();
        }

        public IEnumerator<T> GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        public struct Enumerator : IEnumerator<T>
        {
            int Index;
            readonly int End;
            readonly Deque<T> Collection;
            public T Current { get; private set; }
            object IEnumerator.Current => Current;

            public Enumerator(Deque<T> arr)
            {
                Collection = arr;
                int start = arr.Start;
                Index = start;
                End   = start + arr.Count;
                Current = default;
            }
            public void Dispose()
            {
            }
            public bool MoveNext()
            {
                if (Index >= End)
                    return false;
                Current = Collection.Items[Index++];
                return true;
            }
            public void Reset()
            {
                Index = Collection.Start;
            }
        }
    }
}
