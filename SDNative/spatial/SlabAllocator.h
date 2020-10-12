#pragma once
#include <cstdint>
#include <vector>

namespace spatial
{
    class SlabAllocator
    {
        // single-use linear slab of memory
        // NOTE: Cannot use std::unique_ptr here due to dll-interface
        struct Slab;

        struct SlabArray
        {
            Slab** Data;
            int Size;
            int Capacity;
            SlabArray() noexcept;
            ~SlabArray() noexcept;
            void clear() noexcept { Size = 0; }
            void push_back(Slab* slab) noexcept;
            void assign(const SlabArray& other) noexcept;
            Slab** begin() const noexcept { return Data; }
            Slab** end() const noexcept { return Data + Size; }
            Slab* try_pop(int size) noexcept;
        };

        SlabArray Slabs; // All recorded slabs
        SlabArray Active; // active slabs that should be checked for alloc

        SlabArray ReuseArrayAlloc; // reused sub-slabs for allocArray

        // default size in bytes of a single slab
        // as memory demand increases, slab size will be increased
        size_t SlabSizeBytes = 0;

    public:

        explicit SlabAllocator(size_t slabSizeBytes) noexcept;
        ~SlabAllocator() noexcept;

        SlabAllocator(SlabAllocator&&) = delete;
        SlabAllocator(const SlabAllocator&) = delete;
        SlabAllocator& operator=(SlabAllocator&&) = delete;
        SlabAllocator& operator=(const SlabAllocator&) = delete;

        /// <summary>
        /// Total bytes used by this Allocator
        /// </summary>
        uint32_t totalBytes() const noexcept;

        /// <summary>
        /// Reset all linear pools
        /// </summary>
        void reset() noexcept;

        /// <summary>
        /// Allocate a new array for spatial object status's
        /// </summary>
        void* allocArray(void* oldArray, int oldCount, int newCapacity, int sizeOf) noexcept;

        /// <summary>
        /// Reuse an array during next allocArray
        /// </summary>
        void reuseArray(void* arr, int capacity, int sizeOf) noexcept;

        template<class T>
        T* allocArray(T* oldArray, int oldCount, int newCapacity) noexcept
        {
            return (T*)allocArray(oldArray, oldCount, newCapacity, sizeof(T));
        }

        template<class T> T* allocArray(int size) noexcept
        {
            return (T*)alloc(sizeof(T) * size);
        }

        template<class T> void reuseArray(T* arr, int capacity) noexcept
        {
            reuseArray((void*)arr, capacity, sizeof(T));
        }

        /// <summary>
        /// Allocate a generic object and call its default constructor
        /// </summary>
        template<class T> T* alloc() noexcept
        {
            void* ptr = alloc(sizeof(T));
            return new (ptr) T{};
        }

        /// <summary>
        /// Uninitialized allocation of an object
        /// </summary>
        template<class T> T* allocUninitialized() noexcept
        {
            return (T*)alloc(sizeof(T));
        }

        /// <summary>
        /// Allocates an array of elements are zeroes all fields
        /// </summary>
        template<class T> T* allocArrayZeroed(int n) noexcept
        {
            size_t bytes = sizeof(T) * n;
            void* ptr = alloc(bytes);
            memset(ptr, 0, bytes);
            return (T*)ptr;
        }

    private:

        // raw alloc from current slab
        void* alloc(uint32_t numBytes) noexcept;

        Slab* addSlab(uint32_t slabSizeInBytes) noexcept;
        Slab* getSlabForAlloc(int allocationSize) noexcept;
    };
}