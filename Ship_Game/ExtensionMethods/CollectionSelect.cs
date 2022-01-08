using System;
using System.Collections.Generic;

namespace Ship_Game
{
    public static class CollectionSelect
    {
        /// <summary>
        /// Converts items from one type T to another type U
        /// Also look at `FilterSelect`
        /// </summary>
        public static U[] Select<T, U>(this T[] items, Func<T, U> selector)
        {
            var selected = new U[items.Length];
            for (int i = 0; i < items.Length; ++i)
                selected[i] = selector(items[i]);
            return selected;
        }

        /// <summary>
        /// Converts items from one type T to another type U
        /// Also look at `FilterSelect`
        /// </summary>
        public static U[] Select<T, U>(this IReadOnlyList<T> items, Func<T, U> selector)
        {
            var selected = new U[items.Count];
            for (int i = 0; i < selected.Length; ++i)
                selected[i] = selector(items[i]);
            return selected;
        }
        

        /// <summary>
        /// Converts items from one type T to another type U
        /// Also look at `FilterSelect`
        /// </summary>
        public static U[] Select<T, U>(this ISet<T> items, Func<T, U> selector)
        {
            var selected = new U[items.Count];
            int size = 0;
            foreach (T item in items)
                selected[size++] = selector(item);
            return selected;
        }
    }
}
