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

        // Return the element with the greatest selector value, or null if list empty
        public static T FindMax<T>(this List<T> list, Func<T, float> selector) where T : class
        {
            int n = list.Count;
            if (n == 0) return null;
            int  imax = 0;
            float max = selector(list[0]);
            for (int i = 1; i < n; ++i)
            {
                float value = selector(list[i]);
                if (value <= max) continue;
                max  = value;
                imax = i;
            }
            return list[imax];
        }
        public static bool FindMax<T>(this List<T> list, out T elem, Func<T, float> selector) where T : class
        {
            return (elem = FindMax(list, selector)) != null;
        }


        // Return the element with the smallest selector value, or null if list empty
        public static T FindMin<T>(this List<T> list, Func<T, float> selector) where T : class
        {
            int n = list.Count;
            if (n == 0) return null;
            int  imin = 0;
            float min = selector(list[0]);
            for (int i = 1; i < n; ++i)
            {
                float value = selector(list[i]);
                if (value > min) continue;
                min  = value;
                imin = i;
            }
            return list[imin];
        }
        public static bool FindMin<T>(this List<T> list, out T elem, Func<T, float> selector) where T : class
        {
            return (elem = FindMin(list, selector)) != null;
        }
    }
}
