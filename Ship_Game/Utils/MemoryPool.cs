using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    // yeah, we're doing some pretty crazy optimizations here. This is a 'bump the pointer' memory pool
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct MemoryPool
    {
        private const int PoolSize = 65536;
        private byte* Base;
        private byte* Ptr;
        private int Available; // @warning Read-only access please :))
        public void Reset() // reset to original state
        {
            Available += (int)(Ptr - Base);
            Ptr = Base;
        }
        public void Destroy()
        {
            if (Base == null) return;
            Marshal.FreeHGlobal(new IntPtr(Base));
            Ptr = Base = null;
            Available = 0;
        }
        public void* Alloc(int numBytes)
        {
            if (Base == null)
                Ptr = Base = (byte*)Marshal.AllocHGlobal(Available = PoolSize).ToPointer();
            else if (Available < numBytes)
                return null;
            void* mem = Ptr;
            Ptr += numBytes;
            Available -= numBytes;
            return mem;
        }
    }

    public sealed unsafe class DynamicMemoryPool : IDisposable
    {
        private MemoryPool[] MemoryPools = new MemoryPool[1];

        // burn some memory from the pools. this is never 'freed', we just clear the bucket pointers
        // if you want to 'free' all allocated pool memory for REUSE, call Reset()
        // if you intend to completely abandon and free all memory, call Destroy() or Dispose()
        public void* Alloc(int numBytes)
        {
            for (;;)
            {
                void* mem = MemoryPools[0].Alloc(numBytes);
                if (mem != null)
                    return mem;

                // first pool is full, resize array by 1 and rotate the items
                int len = MemoryPools.Length;
                Array.Resize(ref MemoryPools, len + 1); // non-amortized growth

                // rotate
                MemoryPool full = MemoryPools[0];
                Array.Copy(MemoryPools, 1, MemoryPools, 0, len);
                MemoryPools[len] = full;
            }
        }

        // reset the pools to their default max-available state
        public void Reset()
        {
            for (int i = 0; i < MemoryPools.Length; ++i)
                MemoryPools[i].Reset();
        }

        // actually free all mem in the pools, however Alloc can be
        // called again to reallocate the pools
        public void Destroy()
        {
            // leaving this uncommented to catch odd Destroy() bugs :)
            // if you get a crash here, it means you're not properly disposing the MemoryPool !
            //if (MemoryPools == null) return; // Disposed!
            for (int i = 0; i < MemoryPools.Length; ++i)
                MemoryPools[i].Destroy();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (MemoryPools == null) return;
            Destroy();
            MemoryPools = null;
        }

        ~DynamicMemoryPool()
        {
            if (MemoryPools == null) return;
            Destroy();
            MemoryPools = null;
        }

        // this would be so much easier in C++... *sigh*
        public void ArrayAdd(PoolArrayU16** bucketRef, ushort id)
        {
            PoolArrayU16* bucket = *bucketRef;
            if (bucket == null)
            {
                *bucketRef = bucket = (PoolArrayU16*)Alloc(64); // first alloc: exactly cache line size
                bucket->Count = 0;
                // Bucket takes 8 bytes in 32-bit build, so 64-8=56 bytes will be used as the storage for the items
                bucket->Capacity = (ushort)((64 - sizeof(PoolArrayU16)) / sizeof(ushort));
                bucket->Items    = (ushort*)((byte*)bucket + sizeof(PoolArrayU16));
            }
            else if (bucket->Count == bucket->Capacity)
            {
                // allocate a new bucket twice the size of previous bucket. growth is amortized 64, 128, 256, 512, ...
                int newAlloc  = (bucket->Capacity * sizeof(short) + sizeof(PoolArrayU16)) * 2;
                var newBucket = (PoolArrayU16*)Alloc(newAlloc);
                newBucket->Capacity = (ushort)((newAlloc - sizeof(PoolArrayU16)) / sizeof(ushort));

                ushort* newItems = newBucket->Items = (ushort*)((byte*)newBucket + sizeof(PoolArrayU16));
                ushort* oldItems = bucket->Items;

                ushort count = newBucket->Count = bucket->Count;
                for (ushort i = 0; i < count; ++i)
                    newItems[i] = oldItems[i];

                *bucketRef = bucket = newBucket;
            }

            bucket->Items[bucket->Count++] = id;
        }
    }

    // custom dynamic array for ushort values, this is an 'inline array', memory layout:
    // [ushort][ushort][ushort*][ ushort[Capacity] ]
    // can support max 65536 items
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [DebuggerDisplay("Count = {Count}  Capacity = {Capacity}")]
    public unsafe struct PoolArrayU16
    {
        public ushort Count;
        public ushort Capacity;
        public ushort* Items;

        public ushort[] ItemsArray
        {
            get
            {
                var items = new ushort[Count];
                for (int i = 0; i < items.Length; ++i)
                    items[i] = Items[i];
                return items;
            }
        }
    }

}
