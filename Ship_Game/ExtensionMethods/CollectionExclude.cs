using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    public static class CollectionExclude
    {
        /// <summary>
        /// Excludes items from this array. All elements in the arrays must be unique to speed this algorithm up.
        /// The resulting exclusion array will be UNSTABLE, meaning item ordering will be changed for performance reasons
        /// Warning: this method has not been well optimized!
        /// </summary>
        public static T[] UniqueExclude<T>(this T[] arr, T[] itemsToExclude) where T : class
        {
            int count = arr.Length;
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

            return TrimItems(unique, count);
        }

        /// <summary>
        /// Similar to UniqueExclude, takes `list` and removes any element from `toExclude`
        /// </summary>
        public static T[] Except<T>(this IReadOnlyList<T> list, IReadOnlyList<T> toExclude) where T : class
        {
            int count = list.Count;
            if (count == 0)
                return Empty<T>.Array;

            var alreadyAdded = new HashSet<T>();
            foreach (var exclude in toExclude)
                alreadyAdded.Add(exclude);

            var except = new T[count];
            int exceptSize = 0;
            for (int i = 0; i < except.Length; ++i)
            {
                T candidate = list[i];
                if (alreadyAdded.Add(candidate)) // only append candidate if it hasn't been added before
                    except[exceptSize++] = candidate;
            }

            return TrimItems(except, exceptSize);
        }
        
        /// <summary>
        /// Similar to UniqueExclude, takes `list` and removes any element from `toExclude`
        /// </summary>
        public static T[] Except<T>(this IReadOnlyList<T> list, ISet<T> toExclude) where T : class
        {
            int count = list.Count;
            if (count == 0)
                return Empty<T>.Array;

            var alreadyAdded = new HashSet<T>();
            foreach (var exclude in toExclude)
                alreadyAdded.Add(exclude);

            var except = new T[count];
            int exceptSize = 0;
            for (int i = 0; i < except.Length; ++i)
            {
                T candidate = list[i];
                if (alreadyAdded.Add(candidate)) // only append candidate if it hasn't been added before
                    except[exceptSize++] = candidate;
            }

            return TrimItems(except, exceptSize);
        }

        /// <summary>
        /// Utility for reducing the size of an array if there are excess items
        /// </summary>
        public static T[] TrimItems<T>(this T[] tooMany, int trimmedSize) where T : class
        {
            if (tooMany.Length <= trimmedSize)
                return tooMany; // already ok

            var trimmed = new T[trimmedSize];
            Memory.HybridCopyRefs(trimmed, 0, tooMany, trimmedSize);
            return trimmed;
        }
    }
}
