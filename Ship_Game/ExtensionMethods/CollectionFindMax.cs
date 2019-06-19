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
    public static class CollectionFindMax
    {
        [Conditional("DEBUG")] static void CheckForNaNInfinity(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                Log.Error($"FindMax invalid selector result: {value} ; This may produce incorrect results!");
        }
        

        // @return the element with the greatest selector value, or NULL if empty
        public static T FindMax<T>(this T[] items, int count, Func<T, float> selector) 
            where T : class
        {
            if (count <= 0) return null;

            T found = items[0]; // @note This prevents the NaN and +Infinity float compare issue
            float max = selector(found);
            for (int i = 1; i < count; ++i)
            {
                T item = items[i];
                float value = selector(item);
                CheckForNaNInfinity(value);
                if (value > max)
                {
                    max = value;
                    found = item;
                }
            }
            return found;
        }
        

        // @return the element with the greatest selector value, or NULL if empty
        public static T FindMax<T>(this IReadOnlyList<T> list, Func<T, float> selector)
            where T : class
        {
            int count = list.Count;
            if (count <= 0) return null;

            T found = list[0]; // @note This prevents the NaN and +Infinity float compare issue
            float max = selector(found);
            for (int i = 1; i < count; ++i)
            {
                T item = list[i];
                float value = selector(item);
                CheckForNaNInfinity(value);
                if (value > max)
                {
                    max = value;
                    found = item;
                }
            }
            return found;
        }


        // @return the element with the greatest filtered selector value, or NULL if empty
        public static T FindMaxFiltered<T>(this T[] items, int count, Predicate<T> filter, Func<T, float> selector)
            where T : class
        {
            if (count <= 0) return null;

            // find first valid item @note This prevents the NaN and +Infinity float compare issue
            T found = items.FindFirstValid(count, filter, out int i);
            if (found == null) // no elements passed the filter!
                return null;

            float max = selector(found);
            CheckForNaNInfinity(max);
            for (; i < count; ++i)
            {
                T item = items[i];
                if (filter(item))
                {
                    float value = selector(item);
                    CheckForNaNInfinity(value);
                    if (value > max)
                    {
                        max   = value;
                        found = item;
                    }
                }
            }
            return found;
        }


        // @return the element with the greatest filtered selector value, or NULL if empty
        public static TKey FindMaxKeyByValuesFiltered<TKey,TValue>(
            this Map<TKey, TValue> map, Predicate<TValue> filter, Func<TValue, float> selector)
        {
            TKey found = default;
            float max = float.MinValue;
            foreach (KeyValuePair<TKey, TValue> kv in map)
            {
                if (filter(kv.Value))
                {
                    float value = selector(kv.Value);
                    CheckForNaNInfinity(value);
                    if (value > max || found == null) // @note found==null prevents float NaN/Infinity comparison issue
                    {
                        max = value;
                        found = kv.Key;
                    }
                }
            }
            return found;
        }


        // @return default(KeyValuePair) if list is empty! Or the item with smallest selected value
        public static KeyValuePair<TKey, TValue> FindMax<TKey, TValue>(this Map<TKey, TValue> map, 
                                                                       Func<TKey, TValue, float> selector)
        {
            KeyValuePair<TKey, TValue> found = default;
            float max = float.MinValue;
            CheckForNaNInfinity(max);
            foreach (KeyValuePair<TKey, TValue> kv in map)
            {
                float value = selector(kv.Key, kv.Value);
                CheckForNaNInfinity(value);
                if (value > max)
                {
                    max = value;
                    found = kv;
                }
            }
            return found;
        }


        // @return NULL if list is empty! Or the item with smallest selected value
        public static TKey FindMaxKey<TKey, TValue>(this Map<TKey, TValue> map, 
                                                    Func<TKey, float> selector)
        {
            TKey found = default;
            float max = float.MinValue;
            CheckForNaNInfinity(max);
            foreach (KeyValuePair<TKey, TValue> kv in map)
            {
                TKey item = kv.Key;
                float value = selector(item);
                CheckForNaNInfinity(value);
                if (value > max)
                {
                    max = value;
                    found = item;
                }
            }
            return found;
        }


        // @return NULL if list is empty! Or the item with smallest selected value
        public static TValue FindMaxValue<TKey, TValue>(this Map<TKey, TValue> map, 
                                                        Func<TValue, float> selector)
        {
            TValue found = default;
            float max = float.MinValue;
            foreach (KeyValuePair<TKey, TValue> kv in map)
            {
                TValue item = kv.Value;
                float value = selector(item);
                CheckForNaNInfinity(value);
                if (value > max)
                {
                    max = value;
                    found = item;
                }
            }
            return found;
        }


        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindMax<T>(this T[] items, Func<T, float> selector)
            where T : class
        {
            return items.FindMax(items.Length, selector);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindMax<T>(this Array<T> list, Func<T, float> selector)
            where T : class
        {
            return list.GetInternalArrayItems().FindMax(list.Count, selector);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindMaxFiltered<T>(this T[] items, Predicate<T> filter, Func<T, float> selector)
            where T : class
        {
            return items.FindMaxFiltered(items.Length, filter, selector);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindMaxFiltered<T>(this Array<T> list, Predicate<T> filter, Func<T, float> selector)
            where T : class
        {
            return list.GetInternalArrayItems().FindMaxFiltered(list.Count, filter, selector);
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FindMax<T>(this Array<T> list, out T elem, Func<T, float> selector)
            where T : class
        {
            return (elem = FindMax(list, selector)) != null;
        }

        // @note Hand-crafted Max() extension for float arrays
        public static float Max(this float[] floats)
        {
            if (floats.Length == 0)
                return 0f;

            float max = floats[0];
            for (int i = 1; i < floats.Length; ++i)
            {
                float value = floats[i];
                if (value > max) max = value;
            }
            return max;
        }
    }
}
