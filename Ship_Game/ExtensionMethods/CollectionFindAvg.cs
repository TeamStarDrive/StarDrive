using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public static class CollectionFindAvg
    {
        // @note Hand-crafted Sum() extension for float arrays
        public static float Sum(this float[] floats)
        {
            float sum = 0f;
            for (int i = 0; i < floats.Length; ++i)
                sum += floats[i];
            return sum;
        }
        
        // @note Hand-crafted Sum(filter) extension for float arrays
        public static float Sum(this float[] floats, Predicate<float> filter)
        {
            float sum = 0f;
            for (int i = 0; i < floats.Length; ++i)
            {
                float value = floats[i];
                if (filter(value))
                    sum += value;
            }
            return sum;
        }

        // @note Hand-crafted Avg() extension for float arrays
        public static float Avg(this float[] floats) => Sum(floats) / floats.Length;

        // @note Hand-crafted Avg(filter) extension for float arrays
        public static float Avg(this float[] floats, Predicate<float> filter)
        {
            int count = 0;
            float sum = 0f;
            for (int i = 0; i < floats.Length; ++i)
            {
                float value = floats[i];
                if (filter(value))
                {
                    ++count;
                    sum += value;
                }
            }
            return count == 0 ? 0f : sum / count;
        }
    }
}
