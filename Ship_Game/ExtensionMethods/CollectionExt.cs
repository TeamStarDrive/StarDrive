using System;
using System.Collections.Generic;
using System.Text;

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
            if (list is Array<T> arrayList)
                return arrayList.IndexOf(item);

            for (int i = 0, n = list.Count; i < n; ++i)
                if (item == list[i])
                    return i;
            return -1;
        }


        // Return the element with the greatest selector value, or null if list empty
        public static T FindMax<T>(this Array<T> list, Func<T, float> selector) where T : class
        {
            int n = list.Count;
            T found = null;
            float max = float.MinValue;
            for (int i = 0; i < n; ++i)
            {
                T item = list[i];
                float value = selector(item);
                if (value <= max) continue;
                max   = value;
                found = item;
            }
            return found;
        }
        public static bool FindMax<T>(this Array<T> list, out T elem, Func<T, float> selector) where T : class
        {
            return (elem = FindMax(list, selector)) != null;
        }


        public static T FindMaxFiltered<T>(this Array<T> list, Predicate<T> filter, Func<T, float> selector) where T : class
        {
            T found = null;
            float max = float.MinValue;
            int n = list.Count;
            for (int i = 0; i < n; ++i)
            {
                T item = list[i];
                if (!filter(item)) continue;
                float value = selector(item);
                if (value <= max) continue;
                max   = value;
                found = item;
            }
            return found;
        }
        public static bool FindMaxFiltered<T>(this Array<T> list, out T elem, Predicate<T> filter, Func<T, float> selector) where T : class
        {
            return (elem = FindMaxFiltered(list, filter, selector)) != null;
        }


        // Return the element with the smallest selector value, or null if list empty
        public static T FindMin<T>(this Array<T> list, Func<T, float> selector) where T : class
        {
            T found = null;
            float min = float.MaxValue;
            int n = list.Count;
            for (int i = 0; i < n; ++i)
            {
                T item = list[i];
                float value = selector(item);
                if (value > min) continue;
                min   = value;
                found = item;
            }
            return found;
        }
        public static bool FindMin<T>(this Array<T> list, out T elem, Func<T, float> selector) where T : class
        {
            return (elem = FindMin(list, selector)) != null;
        }


        public static T FindMinFiltered<T>(this Array<T> list, Predicate<T> filter, Func<T, float> selector) where T : class
        {
            T found = null;
            float min = float.MaxValue;
            int n = list.Count;
            for (int i = 0; i < n; ++i)
            {
                T item = list[i];
                if (!filter(item)) continue;     
                
                float value = selector(item);
                if (value > min) continue;
                min   = value;
                found = item;
            }
            return found;
        }
        public static bool FindMinFiltered<T>(this Array<T> list, out T elem, Predicate<T> filter, Func<T, float> selector) where T : class
        {
            return (elem = FindMinFiltered(list, filter, selector)) != null;
        }

        public static bool Any<T>(this Array<T> list, Predicate<T> match)
        {
            int n = list.Count;
            for (int i = 0; i < n; ++i)
                if (match(list[i]))
                    return true;
            return false;
        }

        public static int Count<T>(this Array<T> list, Predicate<T> match)
        {
            int count = 0;
            int n = list.Count;
            for (int i = 0; i < n; ++i)
                if (match(list[i]))
                    ++count;
            return count;
        }

        public static Array<T> ToArrayList<T>(this IEnumerable<T> source)
        {
            var list = new Array<T>();
            foreach (T item in source)
                list.Add(item);
            return list;
        }

        public static T[] ToArray<T>(this IReadOnlyCollection<T> source)
        {
            var items = new T[source.Count];
            int i = 0;
            foreach (var item in source)
                items[i++] = item;
            return items;
        }

        public static Array<T> ToArrayList<T>(this IReadOnlyCollection<T> source)
        {
            var items = new Array<T>(source.Count);
            int i = 0;
            foreach (var item in source)
                items[i++] = item;
            return items;
        }
    }


}
