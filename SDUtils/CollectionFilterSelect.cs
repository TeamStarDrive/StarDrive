using System;
using System.Collections.Generic;

namespace SDUtils
{
    public static class CollectionFilterSelect
    {
        // performs a combined and optimized:
        // list.Filter(x => x.Condition).Select(x => x.Value).ToArray();
        // concrete example:
        // string[] names = ships.FilterSelect(s => s.IsPlatform, s => s.Name);
        public static unsafe U[] FilterSelect<T,U>(this IReadOnlyList<T> items, 
                                                   Predicate<T> filter, Func<T,U> select)
        {
            int count = items.Count;
            if (count == 0) return Empty<U>.Array;
            byte* map = stackalloc byte[count];

            int resultCount = 0;
            for (int i = 0; i < count; ++i)
            {
                bool keep = filter(items[i]);
                if (keep) ++resultCount;
                map[i] = keep ? (byte)1 : (byte)0;
            }

            if (resultCount == 0)
                return Empty<U>.Array;

            var results = new U[resultCount];
            resultCount = 0;
            for (int i = 0; i < count; ++i)
                if (map[i] > 0) results[resultCount++] = select(items[i]);

            return results;
        }
        // performs a combined and optimized:
        // set.Filter(x => x.Condition).Select(x => x.Value).ToArray();
        // concrete example:
        // string[] names = ships.FilterSelect(s => s.IsPlatform, s => s.Name);
        public static unsafe U[] FilterSelect<T,U>(this ISet<T> items, 
                                                   Predicate<T> filter, Func<T,U> select)
        {
            int count = items.Count;
            byte* map = stackalloc byte[count];

            int resultCount = 0;
            int i = 0;
            foreach (T item in items) {
                bool keep = filter(item);
                if (keep) ++resultCount;
                map[i++] = keep ? (byte)1 : (byte)0;
            }

            if (resultCount == 0)
                return Empty<U>.Array;

            var results = new U[resultCount];
            resultCount = 0;
            i = 0;
            foreach (T item in items) {
                if (map[i] > 0) {
                    results[resultCount++] = select(item);
                }
                ++i;
            }

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
