using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using System.Linq;

namespace Ship_Game
{
    /// <summary>
    /// This contains multiple simple yet useful extension algorithms for different data structures
    /// The goal is to increase performance by specializing for concrete container types,
    /// which helps to eliminate virtual dispatch, greatly speeding up iteration times
    /// 
    /// As much as possible, we try to avoid any kind of IEnumerable or foreach loops, because
    /// they have appalling performance and .NET JIT fails to optimize most of our use cases.
    /// 
    /// We don't benefit from lazy evaluation either, because most of the algorithms are very data-heavy,
    /// with no way to exclude elements.
    /// 
    /// If you find these extensions repetitive, then yes, this is your worst nightmare --- however,
    /// all of this repetitive looping provides the best possible performance on .NET JIT. It's just not good enough.
    /// </summary>
    public static class CollectionExt
    {
        public static int IndexOf<T>(this IReadOnlyList<T> list, T item) where T : class
        {
            if (list is IList<T> aList)
                return aList.IndexOf(item);

            for (int i = 0, n = list.Count; i < n; ++i)
                if (item == list[i])
                    return i;
            return -1;
        }

        public static int IndexOf<T>(this IReadOnlyList<T> list, Predicate<T> predicate)
        {
            for (int i = 0, n = list.Count; i < n; ++i)
                if (predicate(list[i]))
                    return i;
            return -1;
        }

        // @return First item found or NULL if nothing passes the predicate
        public static T Find<T>(this T[] items, Predicate<T> predicate) where T : class
        {
            for (int i = 0; i < items.Length; ++i)
            {
                T item = items[i];
                if (predicate(item))
                    return item;
            }
            return null;
        }

        // @return Find with index of next element
        public static bool FindFirstValid<T>(this T[] items, int count, Predicate<T> predicate, out int nextIndex, out T firstValid)
        {
            int i = 0;
            for (; i < count; ++i)
            {
                T item = items[i];
                if (predicate(item))
                {
                    nextIndex = i+1;
                    firstValid = item;
                    return true;
                }
            }
            nextIndex = i;
            firstValid = default;
            return false;
        }

        /// <summary>
        /// NOTE: This is an intentional replacement to System.Linq.Count()
        ///       Mostly so that we won't have to import it, which will
        ///       cause an ambiguous overload clash with other utils.
        /// WARNING: This is much slower than calling `.Count` property, so use with caution.
        /// </summary>
        public static int Count<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable is IReadOnlyList<T> rl) return rl.Count;
            if (enumerable is ICollection<T> c)    return c.Count;

            // fall back to epicly slow enumeration
            int count = 0;
            using (IEnumerator<T> e = enumerable.GetEnumerator())
            {
                while (e.MoveNext())
                    ++count;
            }
            return count;
        }

        public static int Count<T>(this Array<T> list, Predicate<T> match)
        {
            int n = 0;
            int count = list.Count;
            T[] items = list.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
                if (match(items[i])) ++n;
            return n;
        }

        public static int Count<TKey, TValue>(this Map<TKey, TValue>.ValueCollection values, Predicate<TValue> match)
        {
            int count = 0;
            foreach (TValue value in values)
                if (match(value))
                    ++count;
            return count;
        }

        // warning, this is O(n*m), worst case O(n^2)
        // Special optimized version that only works with reference types
        public static bool ContainsAnyRef<T>(this T[] arr1, T[] arr2) where T : class
        {
            for (int i = 0; i < arr1.Length; ++i) // @note CLR can only optimize away bounds checking if we use .Length directly in the loop condition
            {
                T item = arr1[i];
                for (int j = 0; j < arr2.Length; ++j)
                    if (item == arr2[i]) return true;
            }
            return false;
        }
        
        /// <summary>
        /// Excludes items from this array. All elements in the arrays must be unique to speed this algorithm up.
        /// The resulting exclusion array will be UNSTABLE, meaning item ordering will be changed for performance reasons
        /// </summary>
        public static T[] UniqueExclude<T>(this T[] arr, T[] itemsToExclude) where T : class
        {
            int count  = arr.Length;
            if (count == 0)
                return Empty<T>.Array;

            var unique = new T[count];
            Memory.HybridCopyRefs(unique, 0, arr, count); // good average copy performance

            for (int i = 0; i < itemsToExclude.Length; ++i) {
                T item = itemsToExclude[i];
                for (int j = 0; j < count; ++j) {
                    if (unique[j] == item) {
                        unique[j] = unique[--count];
                        break;
                    }
                }
            }

            if (count >= unique.Length)
                return unique;

            var items = new T[count]; // trim excess
            Memory.HybridCopyRefs(items, 0, unique, count);
            return items;
        }

        // The following methods are all specific implementations
        // of ToArray() and ToList() as ToArrayList(); Main goal is to improve performance
        // compared to generic .NET ToList() which doesn't reserve capacity etc.
        // ToArrayList() will return an Array<T> as opposed to .NET ToList() which returns List<T>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Array<T> ToArrayList<T>(this ICollection<T> source) => new Array<T>(source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Array<T> ToArrayList<T>(this IReadOnlyList<T> source) => new Array<T>(source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Array<T> ToArrayList<T>(this IEnumerable<T> source) => new Array<T>(source);

        public static T[] ToArray<T>(this ICollection<T> source)
        {
            int count = source.Count;
            if (count == 0) return Empty<T>.Array;
            var items = new T[count];
            source.CopyTo(items, 0);
            return items;
        }

        public static T[] ToArray<T>(this IReadOnlyList<T> source)
        {
            int count = source.Count;
            if (count == 0) return Empty<T>.Array;
            var items = new T[count];
            if (source is ICollection<T> c)
                c.CopyTo(items, 0);
            else for (int i = 0; i < count; ++i)
                items[i] = source[i];
            return items;
        }

        public static T[] ToArray<T>(this IEnumerable<T> source)
        {
            if (source is ICollection<T> c)    return c.ToArray();
            if (source is IReadOnlyList<T> rl) return rl.ToArray();

            // fall back to epicly slow enumeration
            T[] items = Empty<T>.Array;
            int count = 0;
            using (var e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    if (items.Length == count)
                    {
                        int len = count == 0 ? 4 : count * 2; // aggressive growth
                        Array.Resize(ref items, len);
                    }
                    items[count++] = e.Current;
                }
            }
            if (items.Length != count)
                Array.Resize(ref items, count);
            return items;
        }

        public static bool Contains<T>(this IReadOnlyList<T> list, T item)
        {
            int count = list.Count;
            if (count == 0)
                return false;

            if (item == null)
            {
                for (int i = 0; i < count; ++i)
                    if (list[i] == null) return true;
                return false;
            }
            EqualityComparer<T> c = EqualityComparer<T>.Default;
            for (int i = 0; i < count; ++i)
                if (c.Equals(list[i], item)) return true;
            return false;
        }

        public static bool ContainsRef<T>(this IReadOnlyList<T> list, T item) where T : class
        {
            int count = list.Count;
            for (int i = 0; i < count; ++i)
                if (list[i] == item) return true;
            return false;
        }

        public static bool ContainsRef<T>(this T[] array, T item) where T : class
        {
            for (int i = 0; i < array.Length; ++i)
                if (array[i] == item) return true;
            return false;
        }

        public static bool ContainsRef<T>(this T[] array, int length, T item) where T : class
        {
            for (int i = 0; i < length; ++i)
                if (array[i] == item) return true;
            return false;
        }

        // @return TRUE if a.Count == b.Count and all elements are equivalent
        public static bool EqualElements<T>(this IReadOnlyList<T> a, IReadOnlyList<T> b)
        {
            int count = a.Count;
            if (count != b.Count)
                return false;

            EqualityComparer<T> c = EqualityComparer<T>.Default;
            for (int i = 0; i < count; ++i)
                if (!c.Equals(a[i], b[i]))
                    return false;
            return true;
        }

        public static bool EqualElements<T>(this IReadOnlyList<T> a, IReadOnlyList<T> b, Func<T, T, bool> equals)
        {
            int count = a.Count;
            if (count != b.Count)
                return false;
            for (int i = 0; i < count; ++i)
                if (!equals(a[i], b[i]))
                    return false;
            return true;
        }

        /// <summary>
        /// Sorts by generating a secondary list of values.
        /// Example:
        /// // sort ascending, where sorted[0] will be ship closest to homeWorld
        /// var sorted = ships.Sort(ship => ship.Center.DistanceTo(homeWorld.Center));
        /// </summary>
        public static void Sort<T, TKey>(this T[] array, Func<T, TKey> keyPredicate)
        {
            if (array.Length <= 1)
                return;

            var keys = new TKey[array.Length];
            for (int i = 0; i < array.Length; ++i)
                keys[i] = keyPredicate(array[i]);

            Array.Sort(keys, array, 0, array.Length);
        }

        /// <summary>
        /// Returns a sorted copy of the original items
        /// The ordering of elements is decided by the keyPredicate.
        /// If you want descending, just negate keyPredicate result!
        /// </summary>
        public static T[] Sorted<T, TKey>(this IEnumerable<T> items, Func<T, TKey> keyPredicate)
        {
            T[] array = ToArray(items);
            Sort(array, keyPredicate);
            return array;
        }
        public static T[] Sorted<T, TKey>(this T[] items, Func<T, TKey> keyPredicate)
        {
            T[] array = items.CloneArray();
            Sort(array, keyPredicate);
            return array;
        }

        public static T[] Sorted<T, TKey>(this IEnumerable<T> items, bool ascending, Func<T, TKey> keyPredicate)
        {
            T[] sorted = Sorted(items, keyPredicate);
            if (!ascending) Reverse(sorted);
            return sorted;
        }
        public static T[] Sorted<T, TKey>(this T[] items, bool ascending, Func<T, TKey> keyPredicate)
        {
            T[] sorted = Sorted(items, keyPredicate);
            if (!ascending) Reverse(sorted);
            return sorted;
        }

        public static T[] SortedDescending<T, TKey>(this IEnumerable<T> items, Func<T, TKey> keyPredicate)
        {
            T[] sorted = Sorted(items, keyPredicate);
            Reverse(sorted);
            return sorted;
        }
        public static T[] SortedDescending<T, TKey>(this T[] items, Func<T, TKey> keyPredicate)
        {
            T[] sorted = Sorted(items, keyPredicate);
            Reverse(sorted);
            return sorted;
        }

        public static void SortByDistance<T>(this T[] array, Vector2 fromPos) where T : GameplayObject
        {
            if (array.Length <= 1)
                return;

            var keys = new float[array.Length];
            for (int i = 0; i < array.Length; ++i)
                keys[i] = array[i].Center.SqDist(fromPos);

            Array.Sort(keys, array, 0, array.Length);
        }

        public static void Reverse<T>(this T[] mutableItems)
        {
            int i = 0;
            int j = mutableItems.Length - 1;
            while (i < j)
            {
                T temp = mutableItems[i];
                mutableItems[i] = mutableItems[j];
                mutableItems[j] = temp;
                ++i;
                --j;
            }
        }

        public static T[] Reversed<T>(this T[] immutableItems)
        {
            var result = new T[immutableItems.Length];
            int src = immutableItems.Length - 1;
            for (int dst = 0; dst < result.Length; ++dst, --src)
            {
                result[dst] = immutableItems[src];
            }
            return result;
        }

        // this will mess up the ordering of your items due to SwapLast optimization and it will shrink the array by 1 element
        // the result will be passed to the out parameter
        public static void Remove<T>(this T[] array, T item, out T[] result) where T : class
        {
            for (int i = 0; i < array.Length; ++i)
            {
                if (array[i] != item)
                    continue;

                int newLength = array.Length - 1;
                array[i] = array[newLength];
                Memory.HybridCopyRefs(result = new T[newLength], 0, array, newLength);
                return;
            }
            result = array;
        }

        // Warning! This array add does not have amortized growth and will resize the array every time you Add !
        // DO NOT USE THIS IN A LOOP
        public static void Add<T>(this T[] array, T item, out T[] result) where T : class
        {
            int newLength = array.Length + 1;
            Memory.HybridCopyRefs(result = new T[newLength], 0, array, array.Length);
            result[newLength] = item;
        }

        public static U[] Select<T, U>(this T[] items, Func<T, U> selector)
        {
            var selected = new U[items.Length];
            for (int i = 0; i < items.Length; ++i)
                selected[i] = selector(items[i]);
            return selected;
        }

        public static U[] Select<T, U>(this IReadOnlyList<T> items, Func<T, U> selector)
        {
            var selected = new U[items.Count];
            for (int i = 0; i < selected.Length; ++i)
                selected[i] = selector(items[i]);
            return selected;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] UniqueGameObjects<T>(this T[] items) where T : GameplayObject
            => items.UniqueGameObjects(items.Length);

        // Returns a new copy of the array, with all duplicates removed. Even if no duplicates are found, a copy is made.
        // This version is optimized to handle GameplayObjects and manages to exclude dups in just Theta(3n)
        public static unsafe T[] UniqueGameObjects<T>(this T[] items, int count) where T : GameplayObject
        {
            if (count == 0)
                return Empty<T>.Array;

            // find min-max Id
            int min = items[0].Id;
            int max = min;
            for (int i = 1; i < count; ++i)
            {
                int value = items[i].Id;
                if      (value < min) min = value;
                else if (value > max) max = value;
            }

            int mapSpan = (max - min) + 1; // number of elements in the sparse map
            int* sparseMap = stackalloc int[mapSpan]; // a sparse map of (index + 1) values, so 0 (default value) is invalid index

            int numUniques = 0;
            for (int i = 0; i < count; ++i) // populate the index map
            {
                int mapIdx = items[i].Id - min;
                if (sparseMap[mapIdx] == 0)
                {
                    sparseMap[mapIdx] = i + 1; // store 1-based index
                    ++numUniques;
                }
            }

            // write out the results
            var results = new T[numUniques];
            int resultsCount = 0;
            for (int mapIdx = 0; mapIdx < mapSpan; ++mapIdx)
            {
                int itemIdx = sparseMap[mapIdx];
                if (itemIdx > 0) results[resultsCount++] = items[itemIdx - 1];
            }
            return results;
        }

        public static T[] CloneArray<T>(this T[] items)
        {
            int count = items.Length;
            if (count == 0)
                return items;

            var copy = new T[count];
            Memory.HybridCopy(copy, 0, items, count);
            return copy;
        }

        // Does a type-safe cast from TSource[] into Array<TResult>
        // Items which fail the cast are discarded
        public static TResult[] FastCast<TSource, TResult>(this TSource[] items)
        {
            var results = new TResult[items.Length];
            int count = 0;

            for (int i = 0; i < items.Length; ++i)
                if (items[i] is TResult item)
                    results[count++] = item;

            if (count == 0)
                return Empty<TResult>.Array;
            if (count == results.Length)
                return results;

            // slow case: need to truncate end of the array
            var truncated = new TResult[count];
            Array.Copy(results, 0, truncated, 0, count);
            return truncated;
        }

        public static T RandItem<T>(this T[] items)
        {
            return RandomMath.RandItem(items);
        }

        public static T RandItem<T>(this Array<T> items)
        {
            return RandomMath.RandItem(items);
        }

        public static T RandItem<T>(this IReadOnlyList<T> items)
        {
            return RandomMath.RandItem(items);
        }

        /// <summary>
        /// Group items by selector
        /// </summary>
        public static Map<TKey, T> GroupBy<T, TKey>(this ICollection<T> items, Func<T, TKey> keySelector)
        {
            var unique = new Map<TKey, T>();
            foreach (T item in items)
            {
                TKey key = keySelector(item);
                if (!unique.ContainsKey(key))
                    unique.Add(key, item);
            }
            return unique;
        }

        /// <summary>
        /// Group items by selector with filter
        /// </summary>
        public static Map<TKey, T> GroupByFiltered<T, TKey>(this ICollection<T> items, Func<T, TKey> groupBy, Predicate<T> include)
        {
            var unique = new Map<TKey, T>();
            foreach (T item in items)
            {
                if (include(item))
                {
                    TKey key = groupBy(item);
                    if (!unique.ContainsKey(key))
                        unique.Add(key, item);
                }
            }
            return unique;
        }

        /// <summary>
        /// Group items by selection
        /// </summary>
        public static Array<T> GroupBySelector<T, TKey>(this ICollection<T> items, Func<T, TKey> keySelector)
        {
            var unique = new Map<TKey, T>();
            foreach (T item in items)
            {
                TKey key = keySelector(item);
                if (!unique.ContainsKey(key))
                    unique.Add(key, item);
            }
            return unique.Values.ToArrayList();
        }

        /// <summary>
        /// Returns the unique groups Found in item data
        /// <returns></returns>
        public static Array<TKey> UniqueValues<T, TKey>(this ICollection<T> items, Func<T, TKey> keySelector)
        {
            var unique = new Map<TKey, T>();
            foreach (T item in items)
            {
                TKey key = keySelector(item);
                if (!unique.ContainsKey(key))
                    unique.Add(key, item);
            }
            return unique.Keys.ToArrayList();
        }
    }
}
