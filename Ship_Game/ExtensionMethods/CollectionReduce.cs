using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    /// <summary>
    /// Contains optimized reduction utilities like Any, Sum, Max, Count, ...
    /// </summary>
    static class CollectionSum
    {
        
        /// <summary>
        /// Optimized version of LINQ Any(x => x.MatchesCondition), tailored specifically to T[].
        /// </summary>
        /// <param name="items"></param>
        /// <param name="itemMatchesPredicate">example: item => item.IsExplosive</param>
        /// <returns>TRUE if any item matches the predicate condition, false otherwise</returns>
        public static bool Any<T>(this T[] items, Predicate<T> itemMatchesPredicate)
        {
            for (int i = 0; i < items.Length; ++i)
                if (itemMatchesPredicate(items[i]))
                    return true;
            return false;
        }

        /// <summary>
        /// Optimized version of LINQ Count(x => x.IsTrue), tailored specifically to T[].
        /// </summary>
        /// <param name="itemMatchesPredicate">example: item => item.IsExplosive</param>
        /// <returns>Number of total items that match the predicate. Result is always in range of [0, items.Length) </returns>
        public static int Count<T>(this T[] items, Predicate<T> itemMatchesPredicate)
        {
            int count = 0;
            for (int i = 0; i < items.Length; ++i)
                if (itemMatchesPredicate(items[i]))
                    unchecked { ++count; }
            return count;
        }

        /// <summary>
        /// Optimized version of LINQ Sum(x => x.NumItems), tailored specifically to T[].
        /// </summary>
        /// <returns>Total sum from each item</returns>
        public static int Sum<T>(this T[] items, Func<T, int> sumFromItem)
        {
            int sum = 0;
            for (int i = 0; i < items.Length; ++i)
                unchecked { sum += sumFromItem(items[i]); }
            return sum;
        }

        /// <summary>
        /// Optimized version of LINQ Sum(x => x.NumItems), tailored specifically to T[].
        /// </summary>
        /// <returns>Total sum from each item</returns>
        public static float Sum<T>(this T[] items, Func<T, float> sumFromItem)
        {
            float sum = 0.0f;
            for (int i = 0; i < items.Length; ++i)
                sum += sumFromItem(items[i]);
            return sum;
        }

        /// <summary>
        /// Optimized version of LINQ Sum(x => x.NumItems)
        /// </summary>
        /// <returns>Total sum from each item</returns>
        public static float Sum<T>(this Array<T> items, Func<T, float> sumFromItem)
        {
            float sum = 0.0f;
            int count = items.Count;
            T[] arr = items.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
                sum += sumFromItem(arr[i]);
            return sum;
        }

        /// <summary>
        /// Optimized version of LINQ Sum(x => x.NumItems)
        /// </summary>
        /// <returns>Total sum from each item</returns>
        public static int Sum<T>(this Array<T> items, Func<T, int> sumFromItem)
        {
            int sum = 0;
            int count = items.Count;
            T[] arr = items.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
                sum += sumFromItem(arr[i]);
            return sum;
        }

        /// <summary>
        /// Optimized version of LINQ Sum(x => x.NumItems), tailored specifically to T[].
        /// </summary>
        /// <returns>Total sum from each item</returns>
        public static double Sum<T>(this T[] items, Func<T, double> sumFromItem)
        {
            double sum = 0.0;
            for (int i = 0; i < items.Length; ++i)
                sum += sumFromItem(items[i]);
            return sum;
        }

        /// <summary>
        /// Optimized version of LINQ Max(x => x.Value), tailored specifically to T[].
        /// </summary>
        /// <returns>Max item from selected range</returns>
        public static int Max<T>(this T[] items, Func<T, int> valueFromItem)
        {
            if (items.Length == 0)
                return 0;

            int max = valueFromItem(items[0]);
            for (int i = 1; i < items.Length; ++i)
                max = Math.Max(max, valueFromItem(items[i]));
            return max;
        }

        /// <summary>
        /// Optimized version of LINQ Max(x => x.Value), tailored specifically to T[].
        /// </summary>
        /// <returns>Max item from selected range</returns>
        public static float Max<T>(this T[] items, Func<T, float> valueFromItem)
        {
            if (items.Length == 0)
                return 0f;

            float max = valueFromItem(items[0]);
            for (int i = 1; i < items.Length; ++i)
                max = Math.Max(max, valueFromItem(items[i]));
            return max;
        }

    }
}
