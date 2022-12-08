using System;
using System.Collections.Generic;
using SDUtils;

namespace Ship_Game.Utils
{
    public class DependencySorter<T> where T : class
    {
        record struct Entry(T Value, HashSet<T> Dependencies);

        readonly T[] Values;
        readonly Entry[] Entries;

        public DependencySorter(T[] values, Func<T, T[]> getDependencies)
        {
            Values = values;
            Entries = Get(values, getDependencies);
        }

        public static void Sort(T[] values, Func<T, T[]> getDependencies)
        {
            var s = new DependencySorter<T>(values, getDependencies);
            s.Sort();
        }

        // Gets the dependencies array for each value
        static Entry[] Get(T[] values, Func<T, T[]> getDependencies)
        {
            var result = new Entry[values.Length];
            for (int i = 0; i < values.Length; ++i)
            {
                T value = values[i];
                var dependencies = getDependencies(values[i]);
                bool hasDependencies = dependencies != null && dependencies.Length != 0;

                // require dependencies to not have a cyclic reference to Self
                // this simplifies the sorting algorithm greatly
                // and the easiest step where to do this is always `getDependencies()`
                if (hasDependencies && dependencies.Contains(value))
                    throw new($"Entry=`{value}` cannot reference itself in Dependencies!");

                result[i] = new(value, hasDependencies ? new(dependencies) : null);
            }
            return result;
        }

        // Sorts the values array passed in the constructor
        public void Sort()
        {
            // NOTE: There are many algorithms for dependency sorting
            //       this current implementation is much simplified,
            //       which is why we run multiple iterations until we converge on a valid solution

            for (int i = 0; i < 5; ++i)
            {
                int shifts = ReorderDependencies(Values, Entries);
                if (shifts == 0) break;
            }
        }

        static int ReorderDependencies(T[] values, Entry[] entries)
        {
            int shifts = 0;
            foreach ((T value, HashSet<T> dependencies) in entries)
            {
                if (dependencies == null)
                    continue; // no dependencies for this entry

                // need to get the idx every loop, because previous iteration could have reordered `values` array
                int valIdx = values.IndexOf(value);

                // we assume dependencies are half-sorted already
                // so we just scan [i+1..N) elements if they are dependencies or not
                for (int i = valIdx+1; i < values.Length; ++i)
                {
                    T next = values[i];
                    // next element is a dependency? so we need to move value forward
                    if (dependencies.Contains(next))
                    {
                        int targetIdx = i;
                        //Log.Info($"[{valIdx}]={value} depends on [{i}]={next}, moving {value} to [{targetIdx}]");
                        Unshift(values, valIdx, targetIdx);
                        values[targetIdx] = value;

                        ++shifts;
                        i = valIdx = targetIdx; // continue after this element
                    }
                }
            }
            return shifts;
        }

        // unshifts elements by 1, effectively deleting [startIdx] element
        static void Unshift(T[] values, int startIdx, int endIdx)
        {
            for (int i = startIdx + 1; i <= endIdx; ++i)
                values[i-1] = values[i];
            values[endIdx] = null; // for correctness sake, and to catch invalid endIdx cases
        }
    }
}
