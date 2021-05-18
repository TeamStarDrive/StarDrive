using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
    public static class CollectionFilter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] Filter<T>(this T[] items, Predicate<T> predicate)
        {
            return items.Filter(items.Length, predicate);
        }

        // A quite memory efficient filtering function to replace Where clauses
        public static unsafe T[] Filter<T>(this T[] items, int count, Predicate<T> predicate)
        {
            byte* map = stackalloc byte[count];

            int resultCount = 0;
            for (int i = 0; i < count; ++i)
            {
                bool keep = predicate(items[i]);
                if (keep) ++resultCount;
                map[i] = keep ? (byte)1 : (byte)0;
            }

            if (resultCount == 0)
                return Empty<T>.Array;

            var results = new T[resultCount];
            resultCount = 0;
            for (int i = 0; i < count; ++i)
                if (map[i] > 0) results[resultCount++] = items[i];

            return results;
        }

        // Copy paste from above. Purely because I don't want to ruin T[] access optimizations
        public static unsafe T[] Filter<T>(this IReadOnlyList<T> items, Predicate<T> predicate)
        {
            int count = items.Count;
            byte* map = stackalloc byte[count];

            int resultCount = 0;
            for (int i = 0; i < count; ++i)
            {
                bool keep = predicate(items[i]);
                if (keep) ++resultCount;
                map[i] = keep ? (byte)1 : (byte)0;
            }

            if (resultCount == 0)
                return Empty<T>.Array;
            
            var results = new T[resultCount];
            resultCount = 0;
            for (int i = 0; i < count; ++i)
                if (map[i] > 0) results[resultCount++] = items[i];

            return results;
        }

        public static TValue[] Filter<TKey,TValue>(this Map<TKey, TValue>.ValueCollection items,
                                                   Predicate<TValue> predicate)
        {
            return items.ToArray().Filter(predicate);
        }

        public static TKey[] Filter<TKey,TValue>(this Map<TKey, TValue>.KeyCollection items,
                                                 Predicate<TKey> predicate)
        {
            return items.ToArray().Filter(predicate);
        }

        public static unsafe TValue[] FilterValues<TKey,TValue>(this Map<TKey,TValue> dict, 
                                                                Predicate<TValue> predicate)
        {
            var items = new TValue[dict.Count];
            dict.Values.CopyTo(items, 0);

            int count = items.Length;
            byte* map = stackalloc byte[count];

            int resultCount = 0;
            for (int i = 0; i < items.Length; ++i)
            {
                bool keep = predicate(items[i]);
                if (keep) ++resultCount;
                map[i] = keep ? (byte)1 : (byte)0;
            }

            var results = new TValue[resultCount];
            resultCount = 0;
            for (int i = 0; i < count; ++i)
                if (map[i] > 0) results[resultCount++] = items[i];

            return results;
        }

        // performs a combined and optimized:
        // list.Filter(x => x.Condition).Select(x => x.Value).ToArray();
        // concrete example:
        // string[] names = ships.FilterSelect(s => s.IsPlatform, s => s.Name);
        public static unsafe U[] FilterSelect<T,U>(this IReadOnlyList<T> items, 
                                                   Predicate<T> filter, Func<T,U> select)
        {
            int count = items.Count;
            byte* map = stackalloc byte[count];

            int resultCount = 0;
            for (int i = 0; i < count; ++i)
            {
                bool keep = filter(items[i]);
                if (keep) ++resultCount;
                map[i] = keep ? (byte)1 : (byte)0;
            }

            var results = new U[resultCount];
            resultCount = 0;
            for (int i = 0; i < count; ++i)
                if (map[i] > 0) results[resultCount++] = select(items[i]);

            return results;
        }
        
        // FilterSelect a Dictionary
        // Example usage:
        // Map<string, bool> unlockedHulls = ...;
        // string[] hulls = unlockedHulls.FilterSelect(filter: (hull,unlocked) => unlocked,
        //                                             select: (hull,unlocked) => hull);
        public static unsafe U[] FilterSelect<TKey,TValue,U>(this Map<TKey, TValue> items, 
                                                             Func<TKey, TValue, bool> filter,
                                                             Func<TKey, TValue, U> select)
        {
            int count = items.Count;
            byte* map = stackalloc byte[count];

            int resultCount = 0;
            int i = 0;
            foreach (KeyValuePair<TKey, TValue> kv in items)
            {
                bool keep = filter(kv.Key, kv.Value);
                if (keep) ++resultCount;
                map[i] = keep ? (byte)1 : (byte)0;
                ++i;
            }

            var results = new U[resultCount];
            resultCount = 0;
            i = 0;
            foreach (KeyValuePair<TKey, TValue> kv in items)
            {
                if (map[i] > 0) results[resultCount++] = select(kv.Key, kv.Value);
                ++i;
            }
            return results;
        }
    }
}
