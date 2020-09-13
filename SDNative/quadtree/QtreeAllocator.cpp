#include "QtreeAllocator.h"

#include <stdexcept>

#include "QtreeNode.h"
#include "QtreeConstants.h"

namespace tree
{
    const int SlabAlign = 16;

    struct QtreeAllocator::Slab
    {
        int remaining;
        uint8_t* ptr;
        void reset()
        {
            // everything after Slab member fields is free-to-use memory
            remaining = QuadLinearAllocatorSlabSize - SlabAlign;
            ptr = reinterpret_cast<uint8_t*>(this) + SlabAlign;
        }
    };

    QtreeAllocator::QtreeAllocator(int firstNodeId)
        : FirstNodeId{firstNodeId}, NextNodeId{firstNodeId}
    {
        nextSlab();
    }

    QtreeAllocator::~QtreeAllocator()
    {
        for (Slab* slab : Slabs)
            _aligned_free(slab);
    }
    
    void QtreeAllocator::reset()
    {
        CurrentSlab = Slabs.front();
        CurrentSlab->reset();
        CurrentSlabIndex = 0;
        NextNodeId = FirstNodeId;
    }

    SpatialObj* QtreeAllocator::allocArray(SpatialObj* oldArray, int oldCount, int newCapacity)
    {
        auto* newArray = static_cast<SpatialObj*>( alloc(newCapacity*sizeof(SpatialObj)) );
        if (oldArray != nullptr)
        {
            memmove(newArray, oldArray, oldCount*sizeof(SpatialObj));
        }
        return newArray;
    }

    QtreeNode* QtreeAllocator::newNode(int level, float x1, float y1, float x2, float y2)
    {
        void* ptr = alloc(sizeof(QtreeNode));
        int id = NextNodeId++;
        return new (ptr) QtreeNode{id, level, x1, y1, x2, y2};
    }

    void* QtreeAllocator::alloc(uint32_t numBytes)
    {
        Slab* slab = CurrentSlab;
        if (slab->remaining < (int)numBytes)
        {
            slab = nextSlab();
            if (slab->remaining < (int)numBytes)
            {
                throw std::runtime_error{"QtreeAllocator::alloc() failed: numBytes is greater than slab size"};
            }
        }

        uint32_t alignedBytes = numBytes;
        if (uint32_t rem = numBytes % SlabAlign)
            alignedBytes += (SlabAlign - rem);

        void* ptr = slab->ptr;
        slab->remaining -= alignedBytes;
        slab->ptr       += alignedBytes;
        return ptr;
    }

    QtreeAllocator::Slab* QtreeAllocator::nextSlab()
    {
        Slab* slab;
        size_t next_index = CurrentSlabIndex + 1;
        if (next_index < Slabs.size()) // reuse existing Slabs
        {
            CurrentSlabIndex = next_index;
            slab = Slabs[next_index];
        }
        else // make a new slab
        {
            CurrentSlabIndex = Slabs.size();
            slab = static_cast<Slab*>( _aligned_malloc(QuadLinearAllocatorSlabSize, SlabAlign) );
            Slabs.push_back(slab);
        }
        slab->reset();
        CurrentSlab = slab;
        return slab;
    }
}
