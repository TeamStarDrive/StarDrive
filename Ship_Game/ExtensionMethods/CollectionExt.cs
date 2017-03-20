using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ship_Game
{
    public static class CollectionExtensions
    {
        public static TValue ConsumeValue<TKey,TValue>(this Dictionary<TKey, TValue> dict, TKey key)
        {
            if (!dict.TryGetValue(key, out TValue value)) return default(TValue);
            dict[key] = default(TValue);
            return value;
        }

        public static int IndexOf<T>(this IReadOnlyList<T> list, T item) where T : class
        {
            // ReSharper disable once CollectionNeverUpdated.Local
            if (list is IList<T> ilist)
                return ilist.IndexOf(item);

            for (int i = 0, n = list.Count; i < n; ++i)
                if (item == list[i])
                    return i;
            return -1;
        }


        // Return the element with the greatest selector value, or null if empty
        public static T FindMax<T>(this T[] items, int count, Func<T, float> selector) where T : class
        {
            T found = null;
            float max = float.MinValue;
            for (int i = 0; i < count; ++i)
            {
                T item = items[i];
                float value = selector(item);
                if (value <= max) continue;
                max   = value;
                found = item;
            }
            return found;
        }

        public static T FindMax<T>(this IReadOnlyList<T> list, Func<T, float> selector) where T : class
        {
            int count = list.Count;
            T found = null;
            float max = float.MinValue;
            for (int i = 0; i < count; ++i)
            {
                T item = list[i];
                float value = selector(item);
                if (value <= max) continue;
                max = value;
                found = item;
            }
            return found;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindMax<T>(this T[] items, Func<T, float> selector) where T : class
            => items.FindMax(items.Length, selector);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindMax<T>(this Array<T> list, Func<T, float> selector) where T : class
            => list.GetInternalArrayItems().FindMax(list.Count, selector);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FindMax<T>(this Array<T> list, out T elem, Func<T, float> selector) where T : class
            => (elem = FindMax(list, selector)) != null;

        public static T FindMaxFiltered<T>(this T[] items, int count, Predicate<T> filter, Func<T, float> selector) where T : class
        {
            T found = null;
            float max = float.MinValue;
            for (int i = 0; i < count; ++i)
            {
                T item = items[i];
                if (!filter(item)) continue;
                float value = selector(item);
                if (value <= max) continue;
                max   = value;
                found = item;
            }
            return found;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindMaxFiltered<T>(this T[] items, Predicate<T> filter, Func<T, float> selector) where T : class
            => items.FindMaxFiltered(items.Length, filter, selector);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindMaxFiltered<T>(this Array<T> list, Predicate<T> filter, Func<T, float> selector) where T : class
            => list.GetInternalArrayItems().FindMaxFiltered(list.Count, filter, selector);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FindMaxFiltered<T>(this Array<T> list, out T elem, Predicate<T> filter, Func<T, float> selector) where T : class
            => (elem = FindMaxFiltered(list, filter, selector)) != null;


        // Return the element with the smallest selector value, or null if empty
        public static T FindMin<T>(this T[] items, int count, Func<T, float> selector) where T : class
        {
            T found = null;
            float min = float.MaxValue;
            for (int i = 0; i < count; ++i)
            {
                T item = items[i];
                float value = selector(item);
                if (value > min) continue;
                min = value;
                found = item;
            }
            return found;
        }

        public static T FindMin<T>(this IReadOnlyList<T> list, Func<T, float> selector) where T : class
        {
            int count = list.Count;
            T found = null;
            float min = float.MaxValue;
            for (int i = 0; i < count; ++i)
            {
                T item = list[i];
                float value = selector(item);
                if (value > min) continue;
                min = value;
                found = item;
            }
            return found;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindMin<T>(this T[] items, Func<T, float> selector) where T : class
            => items.FindMin(items.Length, selector);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindMin<T>(this Array<T> list, Func<T, float> selector) where T : class
            => list.GetInternalArrayItems().FindMin(list.Count, selector);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FindMin<T>(this Array<T> list, out T elem, Func<T, float> selector) where T : class
            => (elem = FindMin(list, selector)) != null;


        public static T FindMinFiltered<T>(this Array<T> list, Predicate<T> filter, Func<T, float> selector) where T : class
        {
            T found = null;
            int n = list.Count;
            float min = float.MaxValue;
            T[] items = list.GetInternalArrayItems();
            for (int i = 0; i < n; ++i)
            {
                T item = items[i];
                if (!filter(item)) continue;     
                
                float value = selector(item);
                if (value > min) continue;
                min   = value;
                found = item;
            }
            return found;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FindMinFiltered<T>(this Array<T> list, out T elem, Predicate<T> filter, Func<T, float> selector) where T : class
        {
            return (elem = FindMinFiltered(list, filter, selector)) != null;
        }

        public static bool Any<T>(this Array<T> list, Predicate<T> match)
        {
            int count = list.Count;
            T[] items = list.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
                if (match(items[i])) return true;
            return false;
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

        // For Crunchy: Take a look at this and see if this works for you @todo remove this comment after review
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
        public static Array<T> ToArrayList<T>(this IReadOnlyCollection<T> source) => new Array<T>(source);

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

        public static T[] ToArray<T>(this IReadOnlyCollection<T> source)
        {
            unchecked
            {
                int count = source.Count;
                if (count == 0) return Empty<T>.Array;
                var items = new T[count];
                if (source is ICollection<T> c)
                    c.CopyTo(items, 0);
                else using (var e = source.GetEnumerator())
                    for (int i = 0; i < count && e.MoveNext(); ++i)
                        items[i] = e.Current;
                return items;
            }
        }

        public static T[] ToArray<T>(this IEnumerable<T> source)
        {
            if (source is ICollection<T> c)          return c.ToArray();
            if (source is IReadOnlyList<T> rl)       return rl.ToArray();
            if (source is IReadOnlyCollection<T> rc) return rc.ToArray();

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
            unchecked
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
        }

        public static bool ContainsRef<T>(this IReadOnlyList<T> list, T item) where T : class
        {
            unchecked
            {
                int count = list.Count;
                for (int i = 0; i < count; ++i)
                    if (list[i] == item) return true;
                return false;
            }
        }

        public static bool ContainsRef<T>(this T[] array, T item) where T : class
        {
            unchecked
            {
                for (int i = 0; i < array.Length; ++i)
                    if (array[i] == item) return true;
                return false;
            }
        }
        public static bool ContainsRef<T>(this T[] array, int length, T item) where T : class
        {
            unchecked
            {
                for (int i = 0; i < length; ++i)
                    if (array[i] == item) return true;
                return false;
            }
        }

        public static void Sort<T, TKey>(this T[] array, Func<T, TKey> keyPredicate) where T : class
        {
            int count = array.Length;
            if (count <= 1)
                return;

            var keys = new TKey[count];
            for (int i = 0; i < count; ++i)
                keys[i] = keyPredicate(array[i]);

            Array.Sort(keys, array, 0, count);
        }
    }
}
