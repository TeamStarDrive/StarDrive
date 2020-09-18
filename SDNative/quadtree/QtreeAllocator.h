#pragma once
#include <cstdint>
#include <vector>

namespace tree
{
    class QtreeAllocator
    {
        // single-use linear slab of memory
        // NOTE: Cannot use std::unique_ptr here due to dll-interface
        struct Slab;
        std::vector<Slab*> Slabs;
        Slab* CurrentSlab = nullptr;
        size_t CurrentSlabIndex = 0;

    public:

        explicit QtreeAllocator();
        ~QtreeAllocator();

        QtreeAllocator(QtreeAllocator&&) = delete;
        QtreeAllocator(const QtreeAllocator&) = delete;
        QtreeAllocator& operator=(QtreeAllocator&&) = delete;
        QtreeAllocator& operator=(const QtreeAllocator&) = delete;
        
        /// <summary>
        /// Reset all linear pools
        /// </summary>
        void reset();

        /// <summary>
        /// Allocate a new array for spatial object status's
        /// </summary>
        void* allocArray(void* oldArray, int oldCount, int newCapacity, int sizeOf);

        template<class T>
        T* allocArray(T* oldArray, int oldCount, int newCapacity)
        {
            return (T*)allocArray(oldArray, oldCount, newCapacity, sizeof(T));
        }

        template<class T> T* allocArray(int size)
        {
            return (T*)alloc(sizeof(T) * size);
        }

        /// <summary>
        /// Allocate a generic object and call its default constructor
        /// </summary>
        template<class T> T* alloc()
        {
            void* ptr = alloc(sizeof(T));
            return new (ptr) T{};
        }

        /// <summary>
        /// Uninitialized allocation of an object
        /// </summary>
        template<class T> T* allocUninitialized()
        {
            return (T*)alloc(sizeof(T));
        }

        /// <summary>
        /// Allocates an array of elements are zeroes all fields
        /// </summary>
        template<class T> T* allocArrayZeroed(int n)
        {
            size_t bytes = sizeof(T) * n;
            void* ptr = alloc(bytes);
            memset(ptr, 0, bytes);
            return (T*)ptr;
        }

    private:

        // raw alloc from current slab
        void* alloc(uint32_t numBytes);
        Slab* nextSlab();
    };
}