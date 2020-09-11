#pragma once
#include <vector>
#include <memory>
#include <cstdint>

namespace tree
{
    struct SpatialObj;
    struct QtreeNode;

    class QtreeAllocator
    {
        // single-use linear slab of memory
        struct Slab
        {
            uint32_t remaining;
            uint8_t* ptr;
            void reset();
        };

        std::vector<std::unique_ptr<Slab, void(*)(void*)>> Slabs;
        Slab* CurrentSlab = nullptr;
        size_t CurrentSlabIndex = 0;

        int FirstNodeId = 0;
        int NextNodeId = 0;

    public:

        explicit QtreeAllocator(int firstNodeId = 0);
        ~QtreeAllocator();
        
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
        QtreeNode* newNode(int level, float x1, float y1, float x2, float y2);

    private:

        // raw alloc from current slab
        void* alloc(uint32_t numBytes);
        Slab* nextSlab();
    };
}