using System;
using System.Collections.Generic;

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
            var arrayList = list as List<T>;
            if (arrayList != null)
                return arrayList.IndexOf(item);

            for (int i = 0, n = list.Count; i < n; ++i)
                if (item == list[i])
                    return i;
            return -1;
        }
    }
}
