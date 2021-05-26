using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

    public interface IArray<T> : IList<T>
    {
        int Capacity { get; }
        bool IsEmpty { get; }
        bool NotEmpty { get; }
        bool RemoveSwapLast(T item);
        void RemoveAtSwapLast(int index);
        T PopFirst();
        T PopLast();
        bool TryPopLast(out T item);
    }

    /// <summary>
    /// This is a custom version of List, to make debugging easier
    /// and optimize for game relate performance requirements
    /// </summary>
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}  Capacity = {Capacity}")]
    [Serializable]
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Local")]
    public class Array<T> : IArray<T>, IList<T>, IReadOnlyList<T>, 
        ICollection<T>, IEnumerable<T>, IList, ICollection, IEnumerable
    {
        protected T[] Items;
        public int Count { get; protected set; }
        public bool IsReadOnly  => false;
        public bool IsFixedSize => false;
        public bool IsEmpty     => Count == 0;
        public bool NotEmpty    => Count != 0;
        public object SyncRoot       => this;  // ICollection
        public bool   IsSynchronized => false; // ICollection

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
            if ((Count = list.Count) <= 0) Items = Empty<T>.Array;
            else list.CopyTo(Items = new T[Count]);
        }

        /// <summary>Array capacity is reserved exactly, CopyTo is used so the speed will vary
        /// on the container implementation. ArrayT CopyTo uses an extremely fast internal C++ copy routine
        /// which is the best case scenario. Lists that use Array.Copy() are about 2 times slower.</summary>
        public Array(ICollection<T> collection)
        {
            if ((Count = collection.Count) <= 0) Items = Empty<T>.Array;
            else collection.CopyTo(Items = new T[Count], 0);
        }

        /// <summary>Array capacity is reserved exactly, CopyTo is used</summary>
        public Array(T[] array)
        {
            if ((Count = array.Length) <= 0) Items = Empty<T>.Array;
            else array.CopyTo(Items = new T[Count], 0);
        }

        /// <summary>Array capacity is reserved exactly, CopyTo is used if list is ICollection and indexing operator is used for element access (kinda slow, but ok)</summary>
        public Array(IReadOnlyList<T> list)
        {
            int count = list.Count;
            if ((Count = count) > 0)
            {
                Items = new T[count];
                if (list is ICollection<T> c)
                    c.CopyTo(Items, 0);
                else
                    for (int i = 0; i < count; ++i)
                        Items[i] = list[i];
            }
            else Items = Empty<T>.Array;
        }

        public Array(IReadOnlyCollection<T> collection)
        {
            int count = collection.Count;
            if ((Count = count) > 0)
            {
                Items = new T[count];
                using (IEnumerator<T> e = collection.GetEnumerator())
                    for (int i = 0; i < count && e.MoveNext(); ++i)
                        Items[i] = e.Current;
            }
            else Items = Empty<T>.Array;
        }

        /// <summary>The slowest way to construct an new ArrayT.
        /// This will check for multiple subtypes to try and optimize creation, dynamic enumeration would be too slow
        /// Going from fastest implementations to the slowest:
        /// ICollection, IReadOnlyList, IReadOnlyCollection, IEnumerable
        /// If </summary>
        public Array(IEnumerable<T> sequence)
        {
            if (sequence is ICollection<T> c) // might also call this.CopyTo(), best case scenario
            {
                if ((Count = c.Count) <= 0) Items = Empty<T>.Array;
                else c.CopyTo(Items = new T[Count], 0);
            }
            else if (sequence is IReadOnlyList<T> rl)
            {
                int count = rl.Count;
                if ((Count = count) > 0)
                {
                    Items = new T[count];
                    for (int i = 0; i < count; ++i)
                        Items[i] = rl[i];
                }
                else Items = Empty<T>.Array;
            }
            else if (sequence is IReadOnlyCollection<T> rc)
            {
                int count = rc.Count;
                if ((Count = count) > 0)
                {
                    Items = new T[count];
                    using (IEnumerator<T> e = rc.GetEnumerator())
                        for (int i = 0; i < count && e.MoveNext(); ++i)
                            Items[i] = e.Current;
                }
                else Items = Empty<T>.Array;
            }
            else // fall back to epic slow enumeration
            {
                Items = Empty<T>.Array;
                using (IEnumerator<T> e = sequence.GetEnumerator())
                    while (e.MoveNext()) Add(e.Current);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Array<T> Clone() => new Array<T>(this);

        // If you KNOW what you are doing, I will allow you to access internal items for optimized looping
        // But don't blame me if you mess something up
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] GetInternalArrayItems() => Items;

        // Separated throw from this[] to enable MS IL inlining
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
                return Items[index];
            }
            set
            {
                if ((uint)index >= (uint)Count)
                    ThrowIndexOutOfBounds(index);
                Items[index] = value;
            }
        }

        // First element in the list
        public T First => this[0];

        // Last element in the list
        public T Last  => this[Count - 1];

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

        void Grow(int capacity)
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

        public void Resize(int newSize)
        {
            if (newSize > Items.Length) // grow requires realloc
            {
                int capacity = newSize;
                int rem = capacity & 3; // align capacity to a multiple of 4
                if (rem != 0) capacity += 4 - rem;

                var newArray = new T[capacity];
                Array.Copy(Items, 0, newArray, 0, Items.Length);
                Items = newArray;
            }
            else // shrink, clear the upper part of the array to avoid ref leaks
            {
                Array.Clear(Items, newSize, Items.Length - newSize);
            }
            Count = newSize;
        }

        public void Add(T item)
        {
            int capacity = Items.Length;
            int count = Count;
            if (count == capacity)
                Grow(capacity);
            Items[count] = item;
            Count = count + 1;
        }

        // @return TRUE if item was Added, FALSE if it already exists
        public bool AddUnique(T item)
        {
            if (Contains(item))
                return false;
            Add(item);
            return true;
        }

        public void Insert(int index, T item)
        {
            int count = Count;
            if ((uint)index > (uint)count)
                ThrowIndexOutOfBounds(index);

            if (count == Items.Length)
                Grow(Items.Length);

            if (index < count) Array.Copy(Items, index, Items, index + 1, count - index);
            Items[index] = item;
            Count = count + 1;
        }

        public void Clear()
        {
            int count = Count;
            if (count == 0)
                return;
            // nulls all references/struct fields to avoid GC leaks
            Array.Clear(Items, 0, count);
            Count = 0;
        }

        public void ClearAndDispose()
        {
            int count = Count;
            if (count == 0)
                return;
            if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
                for (int i = 0; i < count; ++i)
                    (Items[i] as IDisposable)?.Dispose();
            Array.Clear(Items, 0, count);
            Count = 0;
        }

        // This is slower than ContainsRef if T is a class
        public bool Contains(T item)
        {
            int count = Count;
            if (count == 0)
                return false;

            T[] items = Items;
            if (item == null)
            {
                for (int i = 0; i < count; ++i)
                    if (items[i] == null) return true;
            }
            else
            {
                EqualityComparer<T> c = EqualityComparer<T>.Default;
                for (int i = 0; i < count; ++i)
                    if (c.Equals(items[i], item)) return true;
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex = 0)
        {
            int count = Count;
            if (count != 0) Memory.HybridCopy(array, arrayIndex, Items, count);
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            int count = Count;
            if (count != 0) Memory.HybridCopy((T[])array, arrayIndex, Items, count);
        }

        // Removes a single occurence of an item
        public bool Remove(T item)
        {
            int i = IndexOf(item);
            if (i < 0) return false;
            RemoveAt(i);
            return true;
        }

        // Removes the first single occurence of item matching predicate
        public bool RemoveFirst(Predicate<T> predicate)
        {
            int i = FirstIndexOf(predicate);
            if (i < 0) return false;
            RemoveAt(i);
            return true;
        }

        // Removes the last single occurence of item matching predicate
        public bool RemoveLast(Predicate<T> predicate)
        {
            int i = LastIndexOf(predicate);
            if (i < 0) return false;
            RemoveAt(i);
            return true;
        }

        // does a fast removal by swapping the item with the last element in the array
        public bool RemoveSwapLast(T item)
        {
            int i = IndexOf(item);
            if (i < 0) return false;

            int last = --Count;
            Items[i]    = Items[last];
            Items[last] = default;
            return true;
        }

        // This is slower than IndexOfRef if T is a class
        public int IndexOf(T item)
        {
            int count = Count;
            T[] items = Items;
            EqualityComparer<T> c = EqualityComparer<T>.Default;
            for (int i = 0; i < count; ++i)
                if (c.Equals(items[i], item)) return i;
            return -1;
        }

        // Forward iterates and returns the first predicate matching index
        public int FirstIndexOf(Predicate<T> predicate)
        {
            int count = Count;
            T[] items = Items;
            for (int i = 0; i < count; ++i)
                if (predicate(items[i])) return i;
            return -1;
        }
        
        // Reverse iterates and returns the first predicate matching index
        public int LastIndexOf(Predicate<T> predicate)
        {
            int count = Count;
            T[] items = Items;
            for (int i = count - 1; i >= 0; --i)
                if (predicate(items[i])) return i;
            return -1;
        }

        public void RemoveAt(int index)
        {
            int count = Count;
            if ((uint)index >= (uint)count)
                ThrowIndexOutOfBounds(index);

            Count = --count;
            if (index < count) Array.Copy(Items, index + 1, Items, index, count - index);
            Items[count] = default;
        }

        // Does a fast removal by swapping the item at index with the last element in the array
        // if index is outside bounds, an OOB exception will throw
        public void RemoveAtSwapLast(int index)
        {
            if ((uint)index >= (uint)Count)
                ThrowIndexOutOfBounds(index);
            int last = --Count;
            Items[index] = Items[last];
            Items[last]  = default;
        }

        public T PopFirst()
        {
            if (Count == 0)
                ThrowIndexOutOfBounds(0);

            T item = Items[0];
            --Count;
            Array.Copy(Items, 1, Items, 0, Count); // unshift
            Items[Count] = default;
            return item;
        }

        public T PopLast()
        {
            if (Count == 0)
                ThrowIndexOutOfBounds(0);
            --Count;
            T item = Items[Count];
            Items[Count] = default;
            return item;
        }

        public bool TryPopLast(out T item)
        {
            if (Count == 0)
            {
                item = default;
                return false;
            }
            item         = Items[--Count];
            Items[Count] = default;
            return true;
        }

        /// <summary>
        /// Reorders an item from oldIndex to newIndex
        /// The elements are shifted up and down as necessary
        /// This is equivalent to RemoveAt(oldIndex) + InsertAt(newIndex),
        /// however it's significantly faster since shift reinsertion is done in one pass
        /// </summary>
        /// <param name="oldIndex">Old index of the element</param>
        /// <param name="newIndex">New index of the element</param>
        public void Reorder(int oldIndex, int newIndex)
        {
            if ((uint)oldIndex >= (uint)Count) ThrowIndexOutOfBounds(oldIndex);
            if ((uint)newIndex >= (uint)Count) ThrowIndexOutOfBounds(newIndex);
            
            T itemToMove = Items[oldIndex];

            // destination item is BELOW dragged item:
            // [ oldIndex  ]
            // [ oldIndex+1]
            // [ newIndex  ]
            if (newIndex > oldIndex)
            {
                // UNSHIFT: move everyone up by one
                for (int j = oldIndex + 1; j <= newIndex; ++j)
                    Items[j-1] = Items[j];
            }
            // destination item is ABOVE dragged item:
            // [ newIndex  ]
            // [ oldIndex-1]
            // [ oldIndex  ]
            else if (newIndex < oldIndex)
            {
                // SHIFT: move everyone down by one
                for (int j = oldIndex - 1; j >= newIndex; --j)
                    Items[j+1] = Items[j];
            }
            
            Items[newIndex] = itemToMove;
        }

        // A quite memory efficient filtering function to replace Where clauses
        public T[] Filter(Predicate<T> predicate) => Items.Filter(Count, predicate);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);
        public IEnumerator<T> GetEnumerator()   => new Enumerator(this);

        /// <summary>Get a sub-slice enumerator from this ArrayT</summary>
        /// <param name="start">Start of range (inclusive)</param>
        /// <param name="end">End of range (exclusive)</param>
        public ArrayView<T> SubRange(int start, int end)
        {
            int count = Count;
            if ((uint)start >= (uint)count) ThrowIndexOutOfBounds(start);
            if ((uint)end   >  (uint)count) ThrowIndexOutOfBounds(end);
            return new ArrayView<T>(start, end, Items);
        }

        public override string ToString()
        {
            return GetType().GetTypeName();
        }

        public struct Enumerator : IEnumerator<T>
        {
            int Index;
            readonly int Count;
            readonly T[] Items;
            public T Current { get; private set; }
            object IEnumerator.Current => Current;

            public Enumerator(Array<T> arr)
            {
                Index = 0;
                Count = arr.Count;
                Items = arr.Items;
                Current = default;
            }
            public void Dispose()
            {
            }
            public bool MoveNext()
            {
                if (Index >= Count)
                    return false;
                Current = Items[Index++];
                return true;
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
            return default;
        }

        public bool Any(Predicate<T> match)
        {
            for (int i = 0; i < Count; i++)
                if (match(Items[i]))
                    return true;
            return false;
        }

        public void AddRange(Array<T> collection)
        {
            int n = collection.Count;
            int i = Count;
            Resize(i + n);
            collection.CopyTo(Items, i);
        }

        public void AddRange(IReadOnlyList<T> list)
        {
            int n = list.Count;
            int i = Count;
            Resize(i + n);

            if (list is ICollection<T> collection)
            {
                collection.CopyTo(Items, i);
            }
            else
            {
                for (int x = 0; x < n; ++x)
                    Items[i++] = list[x];
            }
        }

        public void AddRange<U>(Array<U> collection) where U : T
        {
            int n = collection.Count;
            int i = Count;
            Resize(i + n);
            for (int x = 0; x < n; ++x)
                Items[i++] = collection.Items[x];
        }

        //public void AddRange(IEnumerable<T> enumerable)
        //{
        //    using (IEnumerator<T> en = enumerable.GetEnumerator())
        //        while (en.MoveNext()) Add(en.Current);
        //}

        /// Assigns all items from `array` to `this`
        /// Internal buffer is resized only if needed
        public void Assign(Array<T> array)
        {
            int count = array.Count;
            Resize(count);
            array.CopyTo(Items);
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
            readonly Comparison<T> Comparison;
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


        public T[] Sorted(Comparison<T> comparison)
        {
            T[] items = ToArray();
            Array.Sort(items, 0, items.Length, new Comparer(comparison));
            return items;
        }


        // sorts the array by using the key-values generated by keyPredicate
        public void Sort<TKey>(Func<T, TKey> keyPredicate)
        {
            int count = Count;
            if (count <= 1)
                return;

            var items = Items;
            var keys = new TKey[count];
            for (int i = 0; i < count; ++i)
                keys[i] = keyPredicate(items[i]);

            Array.Sort(keys, items, 0, count);
        }

        public T[] Sorted<TKey>(Func<T, TKey> keyPredicate)
        {
            int count = Count;
            if (count <= 0)
                return Empty<T>.Array;

            var items = new T[count];
            if (count == 1)
            {
                items[0] = Items[0];
                return items;
            }
            Memory.HybridCopy(items, 0, Items, count);

            var keys = new TKey[items.Length];
            for (int i = 0; i < items.Length; ++i) // items.Length allows CLR to optimize the loop
                keys[i] = keyPredicate(items[i]);

            Array.Sort(keys, items, 0, items.Length);
            return items;
        }

        public T[] Sorted<TKey>(bool ascending, Func<T, TKey> keyPredicate)
        {
            T[] sorted = Sorted(keyPredicate);
            if (!ascending) sorted.Reverse();
            return sorted;
        }

        public T[] SortedDescending<TKey>(Func<T, TKey> keyPredicate)
        {
            T[] sorted = Sorted(keyPredicate);
            sorted.Reverse();
            return sorted;
        }

        public void RemoveAll(Predicate<T> match)
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

        // This is slower than RemoveDuplicateRefs if T is a class
        // @note RemoveDuplicates is an UNSTABLE algorithm, which means
        //       item ordering can change
        public int RemoveDuplicates()
        {
            int removed = 0;
            int count = Count;
            T[] items = Items;
            EqualityComparer<T> c = EqualityComparer<T>.Default;
            for (int i = 0; i < count; ++i)
            {
                T item = items[i];
                for (int j = count - 1; j >= 0; --j)
                {
                    if (i == j || !c.Equals(items[j], item)) continue;
                    int last = --count; // RemoveAtSwapLast():
                    Count = last;
                    Items[j] = Items[last];
                    Items[last] = default;
                    ++removed;
                }
            }
            return removed;
        }

        public void Reverse()
        {
            for (int i = 0, j = Count - 1; i < j; ++i, --j)
            {
                T temp = Items[i];
                Items[i] = Items[j];
                Items[j] = temp;
            }
        }

        public void ForEach(Action<T> action)
        {
            int count = Count;
            for (int i = 0; i < count; ++i)
                action(Items[i]);
        }

        public T[] ToArray()
        {
            int count = Count;
            if (count == 0)
                return Empty<T>.Array;

            var arr = new T[count];
            Memory.HybridCopy(arr, 0, Items, count);
            return arr;
        }

        // So you accidentally called ToArrayList() which is an Array<T> as well,
        // are you trying to clone the Array<T> ? Use new Array<T>(other) instead.
        public Array<T> ToArrayList()
        {
            throw new InvalidOperationException("You are trying to convert Array<T> to Array<T>. Are you trying to Clone() the Array<T>?");
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
    }

    public static class ArrayHelper
    {
        public static IList NewArrayOfT(Type elementType)
        {
            Type arrayType = typeof(Array<>);
            Type genericArray = arrayType.MakeGenericType(elementType);
            return Activator.CreateInstance(genericArray) as IList;
        }
    }

    // Optimized specializations for speeding up reference based lookup
    public static class ArrayOptimizations
    {
        public static bool ContainsRef<T>(this Array<T> list, T item) where T : class
        {
            int count = list.Count;
            if (count == 0)
                return false;

            T[] items = list.GetInternalArrayItems();
            if (item == null)
            {
                for (int i = 0; i < count; ++i)
                    if (items[i] == null) return true;
                return false;
            }
            for (int i = 0; i < count; ++i)
                if (items[i] == item) return true;
            return false;
        }

        public static int CountRef<T>(this Array<T> list, T item) where T : class
        {
            int count = list.Count;
            if (count == 0)
                return 0;

            int n = 0;
            T[] items = list.GetInternalArrayItems();
            if (item == null)
            {
                for (int i = 0; i < count; ++i)
                    if (items[i] == null) ++n;
            }
            else
            {
                for (int i = 0; i < count; ++i)
                    if (items[i] == item) ++n;
            }
            return n;
        }

        public static bool AddUniqueRef<T>(this Array<T> list, T item) where T : class
        {
            if (!list.ContainsRef(item))
            {
                list.Add(item);
                return true;
            }
            return false;
        }

        public static bool AddUniqueRef<T>(this Array<T> list, Array<T> items) where T : class
        {
            bool allAdded = true;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (!list.AddUniqueRef(item) && allAdded)
                    allAdded = false;
            }
            return allAdded;
        }

        public static void RemoveInActiveObjects<T>(this Array<T> list) where T : GameplayObject
        {
            int size = 0;
            int count = list.Count;
            T[] items = list.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
            {
                if (items[i].Active)
                {
                    items[size] = items[i];
                    ++size;
                }
            }
            list.Resize(size);
        }

        public static int IndexOfRef<T>(this Array<T> list, T item) where T : class
        {
            int count = list.Count;
            T[] items = list.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
                if (items[i] == item) return i;
            return -1;
        }

        public static bool RemoveRef<T>(this Array<T> list, T item) where T : class
        {
            int index = list.IndexOfRef(item);
            if (index == -1) return false;
            list.RemoveAtSwapLast(index);
            return true;
        }

        public static int RemoveDuplicateRefs<T>(this Array<T> list) where T : class
        {
            int removed = 0;
            int count = list.Count;
            T[] items = list.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
            {
                T item = items[i];
                for (int j = count - 1; j >= 0; --j)
                {
                    if (i == j || items[j] != item) continue;
                    list.RemoveAtSwapLast(j);
                    --count;
                    ++removed;
                }
            }
            return removed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] UniqueGameObjects<T>(this Array<T> list) where T : GameplayObject
            => list.GetInternalArrayItems().UniqueGameObjects(list.Count);
    }

    internal sealed class CollectionDebugView<T>
    {
        readonly ICollection<T> Collection;

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

    public struct ArrayView<T> : IReadOnlyList<T>, ICollection
    {
        readonly int Start;
        public int Count { get; }
        readonly T[] Items;

        // start (inclusive), end (exclusive)
        public ArrayView(int start, int end, T[] items)
        {
            Start = start;
            Count = end - start;
            Items = items;
        }

        public IEnumerator<T> GetEnumerator()   => new Enumerator(Start, Start + Count, Items);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(Start, Start + Count, Items);
        
        public object SyncRoot => this;
        public bool IsSynchronized => false;
        public void CopyTo(Array array, int index)
            => Array.Copy(Items, Start, array, index, Count);

        public T this[int index]
        {
            get
            {
                int idx = Start + index;
                if ((uint)index >= (uint)Count)
                    ThrowIndexOutOfRange(idx);
                return Items[idx];
            }
        }

        void ThrowIndexOutOfRange(int index)
        {
            throw new IndexOutOfRangeException($"Index [{index}] out of range({Count}) {ToString()}");
        }

        public struct Enumerator : IEnumerator<T>
        {
            int Index;
            readonly int End;
            readonly T[] Items;
            public T Current { get; private set; }
            object IEnumerator.Current => Current;

            public Enumerator(int start, int end, T[] arr)
            {
                Index = start;
                End   = end;
                Items = arr;
                Current = default;
            }
            public void Dispose()
            {
            }
            public bool MoveNext()
            {
                if (Index >= End) // end index is considered invalid, since it's exclusive
                    return false;
                Current = Items[Index++];
                return true;
            }
            public void Reset()
            {
                throw new InvalidOperationException();
            }
        }
    }
}
