using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ship_Game
{
    [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
    public static class ExtraTypeInfo<T>
    {
        public static readonly Type Type;
        public static readonly bool IsRefType;
        public static readonly int SizeOfRef;
        public static readonly int ForHybridLimit; // cutoff point when to stop using for-loop in hybrid copy

        static ExtraTypeInfo()
        {
            Type type = typeof(T);
            bool reftype = type.IsClass || type.IsSealed;
            Type = type;
            IsRefType = reftype;
            SizeOfRef = reftype ? IntPtr.Size : Marshal.SizeOf(type);
            ForHybridLimit = reftype ? 12 : 64;
        }
    }

    /// <summary>
    /// Memory utilities and extensions
    /// </summary>
    public static class Memory
    {
        /// <summary>
        /// Performs an 'unsafe' block memory copy using APEX MemMove library
        /// </summary>
        [DllImport("SDNative.dll")]
        public static extern unsafe void MemCopy(void* dst, void* src, int bytes);

        //private static readonly Type DoubleType = ExtraTypeInfo<double>.Type;

        /// <summary>
        /// Roughly ~2.1x faster than Array.Copy for HUGE arrays only
        /// 
        /// Warning: No bounds checking is done :D
        /// If you crash, it's your own fault
        /// </summary>
        public static unsafe void ApexCopy<T>(T[] dst, int dstIndex, T[] src, int count)
        {
            // If we want to use native memcopy, we must pin the arrays, otherwise
            // the pointers will be invalidated during GC pause+heap compact.
            // This will end up with MemCopy copying memory blocks of 'freed' memory regions

            // HOWEVER, Manually pinning objects introduces an insanely big overhead
            // So it's better to just give up and use Array.Copy

            // Special case is Large Object Heap memory blocks, which are not compacted automatically
            // So no pinning is required, which is the fastest code path.
            // double arrays of size 1000 are also on LOH because they are 16-byte aligned
            //bool largeObjectHeap = (sizeOf * dst.Length >= 85000 /*&& sizeOf * src.Length >= 85000*/)
            //    || (ReferenceEquals(ExtraTypeInfo<T>.Type, DoubleType) && dst.Length > 1000/* && src.Length > 1000*/);

            //if (largeObjectHeap)
            //{
                int sizeOf = ExtraTypeInfo<T>.SizeOfRef; // size of items in the array
                var pDst = (byte*)Marshal.UnsafeAddrOfPinnedArrayElement(dst, 0).ToPointer();
                var pSrc = (byte*)Marshal.UnsafeAddrOfPinnedArrayElement(src, 0).ToPointer();
                MemCopy(pDst + dstIndex * sizeOf, pSrc, count * sizeOf);
            //}
            //else
            //{
            //    Array.Copy(src, 0, dst, dstIndex, count);
            //}
        }

        // A specialized ApexCopy, which is actually decently fast
        public static unsafe void ApexCopy(int[] dst, int dstIndex, int[] src, int count)
        {
            fixed(int* pDst = dst) fixed(int* pSrc = src)
            {
                MemCopy(pDst + dstIndex, pSrc, count * 4);
            }
        }

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
            unchecked {
                // not going to do this check here. let the caller worry about it
                //if (count == 0 || src.Length == 0)
                //    return;
                // Reference types are slower to copy (for some weird reason),
                // so the for-loop cutoff point is earlier than struct
                // "src[0] is ValueType" seems to be the fastest way to check if T is struct
                int limit = src[0] is ValueType ? 28 : 10;
                if (count <= limit) {
                    for (int i = 0; i < count; ++i)
                        dst[dstIndex + i] = src[i];
                } else Array.Copy(src, 0, dst, dstIndex, count);
            }
        }

        // HybridCopy reference types
        public static void HybridCopyRefs<T>(T[] dst, int dstIndex, T[] src, int count) where T : class
        {
            unchecked {
                if (count <= 28) {
                    for (int i = 0; i < count; ++i)
                        dst[dstIndex + i] = src[i];
                } else Array.Copy(src, 0, dst, dstIndex, count);
            }
        }

        // HybridCopy value types
        public static void HybridCopyValues<T>(T[] dst, int dstIndex, T[] src, int count) where T : struct
        {
            unchecked {
                if (count <= 28) {
                    for (int i = 0; i < count; ++i)
                        dst[dstIndex + i] = src[i];
                } else Array.Copy(src, 0, dst, dstIndex, count);
            }
        }

        /// <summary>Will use a for-loop copy</summary>
        public static void ForCopy<T>(T[] dst, int dstIndex, T[] src, int count)
        {
            unchecked
            {
                for (int i = 0; i < count; ++i)
                    dst[dstIndex + i] = src[i];
            }
        }
    }
}
