using System;
using System.Runtime.InteropServices;

namespace Ship_Game
{
    /// <summary>
    /// Memory utilities and extensions
    /// </summary>
    public static class Memory
    {
        /// <summary>
        /// Combines 2 different copy methods depending on count
        /// This achieves a balanced tradeoff in performance, allowing for small copies (10-28 items)
        /// to be fast, while large copies use a large and fast copy method
        /// Note: Source array cannot be empty!
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="dstIndex"></param>
        /// <param name="src">Note: Source array cannot be empty!</param>
        /// <param name="count">This should not be 0</param>
        public static void HybridCopy<T>(T[] dst, int dstIndex, T[] src, int count)
        {
            // not going to do this check here. let the caller worry about it
            //if (count == 0 || src.Length == 0)
            //    return;
            // Reference types are slower to copy (for some weird reason),
            // so the for-loop cutoff point is earlier than struct
            // "src[0] is ValueType" seems to be the fastest way to check if T is struct
            int limit = src[0] is ValueType ? 28 : 10;
            if (count <= limit)
            {
                for (int i = 0; i < count; ++i)
                {
                    dst[dstIndex + i] = src[i];
                }
            }
            else
            {
                Array.Copy(src, 0, dst, dstIndex, count);
            }
        }

        // HybridCopy reference types
        public static void HybridCopyRefs<T>(T[] dst, int dstIndex, T[] src, int count) where T : class
        {
            if (count <= 28)
            {
                for (int i = 0; i < count; ++i)
                {
                    dst[dstIndex + i] = src[i];
                }
            }
            else
            {
                Array.Copy(src, 0, dst, dstIndex, count);
            }
        }
    }
}
