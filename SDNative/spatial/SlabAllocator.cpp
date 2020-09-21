#include "SlabAllocator.h"
#include "Config.h"
#include <stdexcept>

namespace spatial
{
    const int SlabAlign = 16;

    struct SlabAllocator::Slab
    {
        int remaining;
        uint8_t* ptr;
        void reset()
        {
            // everything after Slab member fields is free-to-use memory
            remaining = AllocatorSlabSize - SlabAlign;
            ptr = reinterpret_cast<uint8_t*>(this) + SlabAlign;
        }
    };

    SlabAllocator::SlabAllocator()
    {
        nextSlab();
    }

    SlabAllocator::~SlabAllocator()
    {
        for (Slab* slab : Slabs)
            _aligned_free(slab);
    }
    
    uint32_t SlabAllocator::totalBytes() const
    {
        uint32_t bytes = sizeof(SlabAllocator) + Slabs.size()*AllocatorSlabSize;
        return bytes;
    }

    void SlabAllocator::reset()
    {
        CurrentSlab = Slabs.front();
        CurrentSlab->reset();
        CurrentSlabIndex = 0;
    }

    void* SlabAllocator::allocArray(void* oldArray, int oldCount, int newCapacity, int sizeOf)
    {
        void* newArray = alloc(newCapacity*sizeOf);
        if (oldArray != nullptr)
        {
            memcpy(newArray, oldArray, oldCount*sizeOf);
        }
        return newArray;
    }

    void* SlabAllocator::alloc(uint32_t numBytes)
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

    SlabAllocator::Slab* SlabAllocator::nextSlab()
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
            slab = static_cast<Slab*>( _aligned_malloc(AllocatorSlabSize, SlabAlign) );
            Slabs.push_back(slab);
        }
        slab->reset();
        CurrentSlab = slab;
        return slab;
    }
}
