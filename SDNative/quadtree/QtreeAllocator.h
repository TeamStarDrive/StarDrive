#pragma once
#include <cstdint>
#include <vector>

namespace tree
{
    struct SpatialObj;
    struct QtreeNode;

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
        /// Allocate a new array for spatial objects
        /// </summary>
        SpatialObj* allocArray(SpatialObj* oldArray, int oldCount, int newCapacity);

        /// <summary>
        /// Allocate and initialize a new QtreeNode
        /// </summary>
        QtreeNode* newNode();

    private:

        // raw alloc from current slab
        void* alloc(uint32_t numBytes);
        Slab* nextSlab();
    };
}