using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ship_Game
{
    /// <summary>
    /// This is a custom version of List, to make debugging easier
    /// and optimize for game relate performance requirements
    /// </summary>
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}  Capacity = {Capacity}")]
    [Serializable]
    public class Array<T> : IList<T>, IReadOnlyList<T>
    {
        protected static readonly T[] Empty = new T[0];
        protected T[] Items;
        public int Count { get; protected set; }

        public bool IsReadOnly => false;
        public bool IsEmpty    => Count == 0;
        public bool NotEmpty   => Count != 0;

        public Array()
        {
            Items = Empty;
        }

        public Array(int capacity)
        {
            Items = new T[capacity];
        }

        public Array(IReadOnlyList<T> list)
        {
            unchecked
            {
                int count = list.Count;
                Count = count;
                Items = new T[count];
                for (int i = 0; i < count; ++i)
                    Items[i] = list[i];
            }
        }

        public Array(Array<T> list)
        {
            unchecked
            {
                int count = list.Count;
                Count = count;
                Items = new T[count];
                for (int i = 0; i < count; ++i)
                    Items[i] = list[i];
            }
        }

        public Array(ICollection<T> collection)
        {
            unchecked
            {
                int count = collection.Count;
                Count = count;
                Items = new T[count];

                using (var e = collection.GetEnumerator())
                {
                    for (int i = 0; i < count; ++i)
                    {
                        e.MoveNext();
                        Items[i] = e.Current;
                    }
                }
            }
        }

        public Array(IEnumerable<T> sequence)
        {
            Items = Empty;
            using (var e = sequence.GetEnumerator())
                while (e.MoveNext())
                    Add(e.Current);
        }

        public T this[int index]
        {
            get
            {
                unchecked
                {
                    if ((uint)index >= (uint)Count)
                        throw new IndexOutOfRangeException($"Index [{index}] out of range({Count}) {ToString()}");
                    return Items[index];
                }
            }
            set
            {
                unchecked
                {
                    if ((uint)index >= (uint)Count)
                        throw new IndexOutOfRangeException($"Index [{index}] out of range({Count}) {ToString()}");
                    Items[index] = value;
                }
            }
        }

        // Get/Set the exact capacity of this Array<T>
        public int Capacity
        {
            get { return Items.Length; }
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

        private void Grow(int capacity)
        {
            if (capacity >= 4)
            {
                capacity = AgressiveGrowth ? capacity * 2 : (capacity * 3) / 2;

                int rem = capacity % 4; // align capacity to a multiple of 4
                if (rem != 0) capacity += 4 - rem;
            }
            else capacity = 4;

            var newArray = new T[capacity];
            Array.Copy(Items, 0, newArray, 0, Items.Length);
            Items = newArray;
        }

        // If TRUE, Array<T> will grow by 2.0x during Add/Insert
        // else, growth is 1.5x
        // 1.5x may use less memory, but can potentially cause more reallocations
        // This should be tested to measure memory usage and GC pressure
        private const bool AgressiveGrowth = false;

        public void Add(T item)
        {
            unchecked
            {
                int capacity = Items.Length;
                if (Count == capacity) // manually inlined to improve performance
                {
                    Grow(capacity);
                }
                Items[Count++] = item;
            }
        }

        public void Insert(int index, T item)
        {
            unchecked
            {
                int capacity = Items.Length;
                if (Count == capacity) // manually inlined to improve performance
                {
                    Grow(capacity);
                }
                if (index < Count) Array.Copy(Items, index, Items, index + 1, Count - index);
                Items[index] = item;
                ++Count;
            }
        }

        public void Clear()
        {
            Array.Clear(Items, 0, Count);
            Count = 0;
        }

        public void ClearAndDispose()
        {
            if (Count <= 0)
                return;
            if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
                for (int i = 0; i < Count; ++i)
                    (Items[i] as IDisposable)?.Dispose();
            Array.Clear(Items, 0, Count);
            Count = 0;
        }

        public bool Contains(T item)
        {
            if (item == null)
            {
                for (int i = 0; i < Count; i++)
                    if (Items[i] == null) return true;
                return false;
            }
            var c = EqualityComparer<T>.Default;
            for (int i = 0; i < Count; i++)
                if (c.Equals(Items[i], item)) return true;
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex = 0)
        {
            Array.Copy(Items, 0, array, arrayIndex, Count);
        }

        public bool Remove(T item)
        {
            int i = Array.IndexOf(Items, item);
            if (i < 0) return false;
            RemoveAt(i);
            return true;
        }

        public int IndexOf(T item)
        {
            return Array.IndexOf(Items, item);
        }

        public void RemoveAt(int index)
        {
            unchecked
            {
                if ((uint)index >= (uint)Count)
                    throw new IndexOutOfRangeException($"Index [{index}] out of range({Count}) {ToString()}");
                --Count;
                if (index < Count) Array.Copy(Items, index + 1, Items, index, Count - index);
                Items[Count] = default(T);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);
        public IEnumerator<T> GetEnumerator()   => new Enumerator(this);

        // Get a subslice enumerator from this Array<T>
        public SubrangeEnumerator<T> SubRange(int start, int end)
        {
            if (end > Count)
                throw new IndexOutOfRangeException($"SubRange end [{end}] out of range({Count}) {ToString()}");
            return new SubrangeEnumerator<T>(start, end, Items);
        }

        public override string ToString()
        {
            return GetType().GenericName();
        }

        public struct Enumerator : IEnumerator<T>
        {
            private int Index;
            private readonly int Count;
            private readonly T[] Items;
            public T Current { get; private set; }
            object IEnumerator.Current => Current;

            public Enumerator(Array<T> arr)
            {
                Index = 0;
                Count = arr.Count;
                Items = arr.Items;
                Current = default(T);
            }
            public void Dispose()
            {
            }
            public bool MoveNext()
            {
                unchecked
                {
                    if (Index >= Count)
                        return false;
                    Current = Items[Index++];
                    return true;
                }
            }
            public void Reset()
            {
                Index = 0;
            }
        }

        ////////////////// Collection Utilities ////////////////////

        public T Find(Predicate<T> match)
        {
            for (int i = 0; i < Count; i++)
            {
                T item = Items[i];
                if (match(item))
                    return item;
            }
            return default(T);
        }

        public void AddRange(ICollection<T> collection)
        {
            unchecked
            {
                int n = collection.Count;
                Capacity = Count + n;
                collection.CopyTo(Items, Count);
                Count += n;
            }
        }

        public void AddRange(IEnumerable<T> enumerable)
        {
            using (var en = enumerable.GetEnumerator())
                while (en.MoveNext())
                    Add(en.Current);
        }

        public void Sort()
        {
            Array.Sort(Items, 0, Count, null);
        }

        public void Sort(IComparer<T> comparer)
        {
            Array.Sort(Items, 0, Count, comparer);
        }

        public void Sort(int index, int count, IComparer<T> comparer)
        {
            Array.Sort(Items, index, count, comparer);
        }

        internal struct Comparer : IComparer<T>
        {
            private readonly Comparison<T> Comparison;
            public Comparer(Comparison<T> comparison)
            {
                Comparison = comparison;
            }
            public int Compare(T x, T y)
            {
                return Comparison(x, y);
            }
        }

        public void Sort(Comparison<T> comparison)
        {
            Array.Sort(Items, 0, Count, new Comparer(comparison));
        }

        public void RemoveAll(Predicate<T> match)
        {
            unchecked
            {
                int oldCount = Count;
                int newCount = 0;
                for (int i = 0; i < oldCount; ++i) // stable erasure, O(n)
                {
                    T item = Items[i];
                    if (!match(item)) // rebuild list from non-removing items
                        Items[newCount++] = item;
                }
                // throw away any remaining references
                Array.Clear(Items, newCount, oldCount - newCount);
                Count = newCount;
            }
        }

        public void RemoveRange(int index, int count)
        {
            if (count <= 0)
                return;

            Count -= count;
            if (index < Count)
            {
                Array.Copy(Items, index + count, Items, index, Count - index);
            }
            Array.Clear(Items, Count, count);
        }

        public int FindIndex(Predicate<T> match)
        {
            unchecked
            {
                int n = Count;
                for (int i = 0; i < n; ++i)
                    if (match(Items[i]))
                        return i;
                return -1;
            }
        }

        public void Reverse()
        {
            for (int i = 0, j = Count - 1; i < j; ++i, --j)
            {
                T temp   = Items[i];
                Items[i] = Items[j];
                Items[j] = temp;
            }
        }

        public void ForEach(Action<T> action)
        {
            int n = Count;
            for (int i = 0; i < n; ++i)
                action(Items[i]);
        }

        public T[] ToArray()
        {
            var arr = new T[Count];
            Array.Copy(Items, arr, Count);
            return arr;
        }
    }


    internal sealed class CollectionDebugView<T>
    {
        private readonly ICollection<T> Collection;

        public CollectionDebugView(ICollection<T> collection)
        {
            Collection = collection;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                var items = new T[Collection.Count];
                Collection.CopyTo(items, 0);
                return items;
            }
        }
    }

    public struct SubrangeEnumerator<T> : IEnumerable<T>
    {
        private readonly int Start;
        private readonly int End;
        private readonly T[] Items;
        public SubrangeEnumerator(int start, int end, T[] items)
        {
            Start = start;
            End   = end;
            Items = items;
        }
        public IEnumerator<T> GetEnumerator()   => new Enumerator(Start, End, Items);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(Start, End, Items);

        public struct Enumerator : IEnumerator<T>
        {
            private int Index;
            private readonly int End;
            private readonly T[] Items;
            public T Current { get; private set; }
            object IEnumerator.Current => Current;

            public Enumerator(int start, int end, T[] arr)
            {
                Index = start;
                End   = end;
                Items = arr;
                Current = default(T);
            }
            public void Dispose()
            {
            }
            public bool MoveNext()
            {
                unchecked
                {
                    if (Index >= End)
                        return false;
                    Current = Items[Index++];
                    return true;
                }
            }
            public void Reset()
            {
                throw new InvalidOperationException();
            }
        }
    }


}
