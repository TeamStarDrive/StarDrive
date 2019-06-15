using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public static class CollectionFindMin
    {
        [Conditional("DEBUG")] static void CheckForNaNInfinity(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                Log.Error($"FindMin invalid selector result: {value} ; This may produce incorrect results!");
        }


        // @return NULL if list is empty! Or the item with smallest selected value
        public static T FindMin<T>(this T[] items, int count, Func<T, float> selector) where T : class
        {
            if (count <= 0) return null;
            T found = items[0]; // @note This prevents the NaN and +Infinity float compare issue
            float min = selector(found);
            CheckForNaNInfinity(min);
            for (int i = 1; i < count; ++i)
            {
                T item = items[i];
                float value = selector(item);
                CheckForNaNInfinity(value);
                if (value < min)
                {
                    min = value;
                    found = item;
                }
            }
            return found;
        }


        // @return NULL if list is empty! Or the item with smallest selected value
        public static T FindMin<T>(this IReadOnlyList<T> list, Func<T, float> selector) where T : class
        {
            int count = list.Count;
            if (count <= 0) return null;
            T found = list[0]; // @note This prevents the NaN and +Infinity float compare issue
            float min = selector(found);
            CheckForNaNInfinity(min);
            for (int i = 1; i < count; ++i)
            {
                T item = list[i];
                float value = selector(item);
                CheckForNaNInfinity(value);
                if (value < min)
                {
                    min = value;
                    found = item;
                }
            }
            return found;
        }


        // @return default(KeyValuePair) if list is empty! Or the item with smallest selected value
        public static KeyValuePair<TKey, TValue> FindMin<TKey, TValue>(this Map<TKey, TValue> map, 
                                                                       Func<TKey, TValue, float> selector)
        {
            KeyValuePair<TKey, TValue> found = default;
            float min = float.MaxValue;
            CheckForNaNInfinity(min);
            foreach (KeyValuePair<TKey, TValue> kv in map)
            {
                float value = selector(kv.Key, kv.Value);
                CheckForNaNInfinity(value);
                if (value < min)
                {
                    min = value;
                    found = kv;
                }
            }
            return found;
        }
        

        // @return NULL if list is empty! Or the item with smallest selected value
        public static TKey FindMinKey<TKey, TValue>(this Map<TKey, TValue> map, 
                                                    Func<TKey, float> selector)
        {
            TKey found = default;
            float min = float.MaxValue;
            CheckForNaNInfinity(min);
            foreach (KeyValuePair<TKey, TValue> kv in map)
            {
                TKey item = kv.Key;
                float value = selector(item);
                CheckForNaNInfinity(value);
                if (value < min)
                {
                    min = value;
                    found = item;
                }
            }
            return found;
        }


        // @return NULL if list is empty! Or the item with smallest selected value
        public static TValue FindMinValue<TKey, TValue>(this Map<TKey, TValue> map, 
                                                        Func<TValue, float> selector)
        {
            TValue found = default;
            float min = float.MaxValue;
            foreach (KeyValuePair<TKey, TValue> kv in map)
            {
                TValue item = kv.Value;
                float value = selector(item);
                CheckForNaNInfinity(value);
                if (value < min)
                {
                    min = value;
                    found = item;
                }
            }
            return found;
        }


        // @return NULL if list is empty! Or the item with smallest selected value
        public static T FindMinFiltered<T>(this T[] items, int count, Predicate<T> filter, 
                                           Func<T, float> selector) where T : class
        {
            if (count <= 0) return null;

            T found = items.FindFirstValid(count, filter, out int i);
            if (found == null) // no elements passed the filter!
                return null;

            float min = selector(found);
            CheckForNaNInfinity(min);
            for (; i < count; ++i)
            {
                T item = items[i];
                if (filter(item))
                {
                    float value = selector(item);
                    if (value < min)
                    {
                        min = value;
                        found = item;
                    }
                }
            }
            return found;
        }


        // finds a limited number of items, filtered and sorted by selector
        public static Array<T> FindMinItemsFiltered<T>(this Array<T> list, int maxCount, 
                                                       Predicate<T> filter, Func<T, float> selector)
        {
            T[] filtered = list.Filter(filter);
            filtered.Sort(selector);

            var found = new Array<T>();
            for (int i = 0; i < filtered.Length && found.Count < maxCount; ++i)
                found.Add(filtered[i]);
            return found;
        }


        // @return NULL if list is empty! Or the item with smallest selected value
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindMinFiltered<T>(this T[] items, Predicate<T> filter, 
            Func<T, float> selector) where T : class
        {
            return items.FindMinFiltered(items.Length, filter, selector);
        }

        // @return NULL if list is empty! Or the item with smallest selected value
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindMinFiltered<T>(this Array<T> list, Predicate<T> filter, 
                                           Func<T, float> selector) where T : class
        {
            return list.GetInternalArrayItems().FindMinFiltered(list.Count, filter, selector);
        }

        
        // @return NULL if list is empty! Or the item with smallest selected value
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindMin<T>(this T[] items, Func<T, float> selector) where T : class
        {
            return items.FindMin(items.Length, selector);
        }

        
        // @return NULL if list is empty! Or the item with smallest selected value
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindMin<T>(this Array<T> list, Func<T, float> selector) where T : class
        {
            return list.GetInternalArrayItems().FindMin(list.Count, selector);
        }


        // @note Hand-crafted Min() extension for float arrays
        public static float Min(this float[] floats)
        {
            if (floats.Length == 0)
                return 0f;

            float min = floats[0];
            for (int i = 1; i < floats.Length; ++i)
            {
                float value = floats[i];
                if (value < min) min = value;
            }
            return min;
        }
    }
}
