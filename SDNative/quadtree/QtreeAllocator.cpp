#include "QtreeAllocator.h"
#include "QtreeNode.h"
#include "QtreeConstants.h"

namespace tree
{
    QtreeAllocator::QtreeAllocator(int firstNodeId)
        : FirstNodeId{firstNodeId}, NextNodeId{firstNodeId}
    {
        nextSlab();
    }

    QtreeAllocator::~QtreeAllocator() = default;
    
    void QtreeAllocator::reset()
    {
        CurrentSlab = Slabs.front().get();
        CurrentSlab->reset();
        CurrentSlabIndex = 0;
        NextNodeId = FirstNodeId;
    }

    SpatialObj* QtreeAllocator::allocArray(SpatialObj* oldArray, int oldCount, int newCapacity)
    {
        auto* newArray = static_cast<SpatialObj*>( alloc(newCapacity*sizeof(SpatialObj)) );
        if (oldArray != nullptr)
        {
            memcpy(newArray, oldArray, oldCount*sizeof(SpatialObj));
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
        if (slab->remaining < numBytes)
        {
            slab = nextSlab();
        }
        void* ptr = slab->ptr;
        slab->remaining -= numBytes;
        slab->ptr       += numBytes;
        return ptr;
    }

    QtreeAllocator::Slab* QtreeAllocator::nextSlab()
    {
        size_t next_index = CurrentSlabIndex + 1;
        if (next_index < Slabs.size()) // reuse existing Slabs
        {
            CurrentSlabIndex = next_index;
            Slab* slab = CurrentSlab = Slabs[next_index].get();
            slab->reset();
            return slab;
        }
        else // make a new slab
        {
            CurrentSlabIndex = Slabs.size();
            Slab* slab = CurrentSlab = static_cast<Slab*>( ::malloc(QuadLinearAllocatorSlabSize) );
            slab->reset();
            Slabs.emplace_back(slab, &::free);
            return slab;
        }
    }

    void QtreeAllocator::Slab::reset()
    {
        // everything after Slab member fields is free-to-use memory
        remaining = QuadLinearAllocatorSlabSize - sizeof(Slab);
        ptr = reinterpret_cast<uint8_t*>(this) + sizeof(Slab);
    }

}
