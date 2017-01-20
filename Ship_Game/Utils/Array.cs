using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ship_Game
{
    /// <summary>
    /// The empty object pattern. Provides safe to use immutable empty objects
    /// </summary>
    public static class Empty<T>
    {
        /// <summary>This is safe to reference everywhere, because an empty array is fully immutable</summary>
        public static readonly T[] Array = new T[0];
    }

    /// <summary>
    /// This is a custom version of List, to make debugging easier
    /// and optimize for game relate performance requirements
    /// </summary>
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}  Capacity = {Capacity}")]
    [Serializable]
    public class Array<T> : IList<T>, IReadOnlyList<T>
    {
        protected static readonly int SizeOf = typeof(T).SizeOfRef();
        protected T[] Items;
        public int Count { get; protected set; }

        public bool IsReadOnly => false;
        public bool IsEmpty    => Count == 0;
        public bool NotEmpty   => Count != 0;

        public Array()
        {
            Items = Empty<T>.Array;
        }
        public Array(int capacity)
        {
            Items = new T[capacity];
        }
        /// <summary> Fastest method to copying an ArrayT.</summary>
        public Array(Array<T> list)
        {
            Items = Empty<T>.Array;
            if ((Count = list.Count) > 0)
                list.CopyTo(Items = new T[Count], 0);
        }
        /// <summary>Array capacity is reserved exactly, CopyTo is used so the speed will vary
        /// on the container implementation. ArrayT CopyTo uses an extremly fast internal C++ copy routine
        /// which is the best case scenario. Lists that use Array.Copy() are about 2 times slower.</summary>
        public Array(ICollection<T> collection)
        {
            Items = Empty<T>.Array;
            if ((Count = collection.Count) > 0)
                collection.CopyTo(Items = new T[Count], 0);
        }
        /// <summary>Array capacity is reserved exactly, CopyTo is used if list is ICollection and indexing operator is used for element access (kinda slow, but ok)</summary>
        public Array(IReadOnlyList<T> list)
        {
            unchecked
            {
                Items = Empty<T>.Array;
                int count = list.Count;
                if ((Count = count) > 0)
                {
                    Items = new T[count];
                    if (list is ICollection<T> c)
                        c.CopyTo(Items, 0);
                    else for (int i = 0; i < count; ++i)
                        Items[i] = list[i];
                }
            }
        }
        /// <summary>Array capacity is reserved exactly, but dynamic enumeration is used if collection is not an ICollection (very slow)</summary>
        public Array(IReadOnlyCollection<T> collection)
        {
            unchecked
            {
                Items = Empty<T>.Array;
                int count = collection.Count;
                if ((Count = count) > 0)
                {
                    Items = new T[count];
                    if (collection is ICollection<T> c)
                        c.CopyTo(Items, 0);
                    else using (var e = collection.GetEnumerator())
                        for (int i = 0; i < count && e.MoveNext(); ++i)
                            Items[i] = e.Current;
                }
            }
        }
        /// <summary>The slowest way to construct an new ArrayT.
        /// This will check for multiple subtypes to try and optimize creation, dynamic enumeration would be too slow
        /// Going from fastest implementations to the slowest:
        /// ICollection, IReadOnlyList, IReadOnlyCollection, IEnumerable
        /// If </summary>
        public Array(IEnumerable<T> sequence)
        {
            unchecked
            {
                Items = Empty<T>.Array;
                if (sequence is ICollection<T> c) // might also call this.CopyTo(), best case scenario
                {
                    if ((Count = c.Count) > 0)
                        c.CopyTo(Items = new T[Count], 0);
                }
                else if (sequence is IReadOnlyList<T> rl)
                {
                    int count = rl.Count;
                    if ((Count = count) > 0) {
                        Items = new T[count];
                        for (int i = 0; i < count; ++i)
                            Items[i] = rl[i];
                    }
                }
                else if (sequence is IReadOnlyCollection<T> rc)
                {
                    int count = rc.Count;
                    if ((Count = count) > 0) {
                        Items = new T[count];
                        using (var e = rc.GetEnumerator())
                            for (int i = 0; i < count && e.MoveNext(); ++i)
                                Items[i] = e.Current;
                    }
                }
                else // fall back to epicly slow enumeration
                {
                    using (var e = sequence.GetEnumerator())
                        while (e.MoveNext()) Add(e.Current);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Array<T> Clone() => new Array<T>(this);

        // If you KNOW what you are doing, I will allow you to access internal items for optimized looping
        // But don't blame me if you mess something up
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] GetInternalArrayItems() => Items;

        // Separated throw from this[] to enable MSIL inlining
        private void ThrowIndexOutOfBounds(int index)
        {
            throw new IndexOutOfRangeException($"Index [{index}] out of range({Count}) {ToString()}");
        }

        public T this[int index]
        {
            get
            {
                unchecked
                {
                    if ((uint)index >= (uint)Count)
                        ThrowIndexOutOfBounds(index);
                    return Items[index];
                }
            }
            set
            {
                unchecked
                {
                    if ((uint)index >= (uint)Count)
                        ThrowIndexOutOfBounds(index);
                    Items[index] = value;
                }
            }
        }

        // First element in the list
        public T First => this[0];

        // Last element in the list
        public T Last  => this[Count - 1];

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

        // If TRUE, Array<T> will grow by 2.0x during Add/Insert
        // else, growth is 1.5x
        // 1.5x may use less memory, but can potentially cause more reallocations
        // This should be tested to measure memory usage and GC pressure
        private const bool AgressiveGrowth = false;

        private void Grow(int capacity)
        {
            if (capacity >= 4)
            {
                capacity = AgressiveGrowth ? capacity * 2 : (capacity * 3) / 2;

                int rem = capacity & 3; // align capacity to a multiple of 4
                if (rem != 0) capacity += 4 - rem;
            }
            else capacity = 4;

            var newArray = new T[capacity];
            Array.Copy(Items, 0, newArray, 0, Items.Length);
            Items = newArray;
        }

        public void Add(T item)
        {
            unchecked
            {
                int capacity = Items.Length;
                int count    = Count;
                if (count == capacity)
                    Grow(capacity);
                Items[count] = item;
                Count = count + 1;
            }
        }

        public void Insert(int index, T item)
        {
            unchecked
            {
                int capacity = Items.Length;
                int count    = Count;
                if (count == capacity)
                    Grow(capacity);
                if (index < count) Array.Copy(Items, index, Items, index + 1, count - index);
                Items[index] = item;
                Count = count + 1;
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
            //if (arrayIndex == 0)
            //    Memory.CopyBytes(array, Items, Count * SizeOf);
            //else
            //    Memory.CopyBytes(array, arrayIndex, Items, Count, SizeOf);

            // if we get crashes, we should fall back to this implementation:
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
                int count = Count;
                if ((uint)index >= (uint)count)
                    ThrowIndexOutOfBounds(index);
                Count = --count;
                if (index < count) Array.Copy(Items, index + 1, Items, index, count - index);
                Items[Count] = default(T);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);
        public IEnumerator<T> GetEnumerator()   => new Enumerator(this);

        // Get a subslice enumerator from this Array<T>
        public SubrangeEnumerator<T> SubRange(int start, int end)
        {
            unchecked
            {
                int count = Count;
                if ((uint)start >= (uint)count) ThrowIndexOutOfBounds(start);
                if ((uint)end >= (uint)count)   ThrowIndexOutOfBounds(end);
                return new SubrangeEnumerator<T>(start, end, Items);
            }
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
                while (en.MoveNext()) Add(en.Current);
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
            CopyTo(arr);

            // if we get errors with this, restore to Array.Copy
            //Memory.CopyBytes(arr, Items, arr.Length * SizeOf);
            //Array.Copy(Items, arr, Count);
            return arr;
        }

        // So you accidentally called ToArrayList() which is an Array<T> as well, are you trying to clone the Array<T> ?
        public Array<T> ToArrayList()
        {
            throw new InvalidOperationException("You are trying to convert Array<T> to Array<T>. Are you trying to Clone() the Array<T>?");
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
