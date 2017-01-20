using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    /// <summary>
    /// Memory utilities and extensions
    /// </summary>
    public static class Memory
    {
        /// <summary>
        /// Performs an 'unsafe' block memory copy using APEX MemMove library
        /// </summary>
        [DllImport("SDNative.dll")]
        public static extern void MemCopy(IntPtr dst, IntPtr src, int bytes);

        /// <summary>
        /// Roughly ~2.1x faster than Array.Copy
        /// Warning: No bounds checking is done :D
        /// If you crash, it's your own fault
        /// </summary>
        public static void CopyArray<T>(T[] dst, T[] src)
        {
            // @note This may crash during garbage collection
            IntPtr pDst = Marshal.UnsafeAddrOfPinnedArrayElement((Array)dst, 0);
            IntPtr pSrc = Marshal.UnsafeAddrOfPinnedArrayElement((Array)src, 0);
            MemCopy(pDst, pSrc, typeof(T).SizeOfRef() * dst.Length);
        }

        /// <summary>
        /// Roughly ~2.1x faster than Array.Copy
        /// Warning: No bounds checking is done :D
        /// If you crash, it's your own fault
        /// </summary>
        public static void CopyBytes(Array dst, Array src, int numBytes)
        {
            IntPtr pDst = Marshal.UnsafeAddrOfPinnedArrayElement(dst, 0);
            IntPtr pSrc = Marshal.UnsafeAddrOfPinnedArrayElement(src, 0);
            MemCopy(pDst, pSrc, numBytes);
        }

        public static void CopyBytes(Array dst, int dstIndex, Array src, int count, int sizeOf)
        {
            IntPtr pDst = Marshal.UnsafeAddrOfPinnedArrayElement(dst, 0);
            IntPtr pSrc = Marshal.UnsafeAddrOfPinnedArrayElement(src, 0);
            pDst += dstIndex * sizeOf;
            MemCopy(pDst, pSrc, count * sizeOf);
        }
    }
}
