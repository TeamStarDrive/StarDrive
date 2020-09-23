#include "SlabAllocator.h"
#include <stdexcept>

namespace spatial
{
    const int SlabAlign = 16;

    struct SlabAllocator::Slab
    {
        uint32_t capacity;
        int remaining;
        uint8_t* ptr;
        explicit Slab(uint32_t cap) : capacity{cap} { reset(); }
        void reset()
        {
            // everything after Slab member fields is free-to-use memory
            remaining = capacity - SlabAlign;
            ptr = reinterpret_cast<uint8_t*>(this) + SlabAlign;
        }
    };

    SlabAllocator::SlabAllocator(size_t slabSize) : SlabSize{slabSize}
    {
        CurrentSlab = nextSlab(0);
    }

    SlabAllocator::~SlabAllocator()
    {
        for (Slab* slab : Slabs)
            _aligned_free(slab);
    }
    
    uint32_t SlabAllocator::totalBytes() const
    {
        uint32_t bytes = sizeof(SlabAllocator);
        for (Slab* slab : Slabs)
            bytes += slab->capacity + SlabAlign;
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
            CurrentSlab = slab = nextSlab(numBytes);
            if (slab->remaining < (int)numBytes)
            {
                throw std::runtime_error{"SlabAllocator::alloc() failed: numBytes is greater than slab size"};
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

    SlabAllocator::Slab* SlabAllocator::nextSlab(uint32_t allocationSize)
    {
        Slab* slab;
        size_t nextIndex = CurrentSlabIndex + 1;
        if (nextIndex < Slabs.size()) // try to reuse existing Slabs
        {
            slab = Slabs[nextIndex];
            if (slab->capacity < allocationSize) // next slab is not big enough
            {
                // kill all slabs ahead of us:
                for (size_t i = nextIndex; i < Slabs.size(); ++i)
                    _aligned_free(Slabs[i]);

                Slabs.erase(Slabs.begin() + nextIndex, Slabs.end());
                return nextSlab(allocationSize);
            }
            CurrentSlabIndex = nextIndex;
            slab->reset();
        }
        else // make a new slab
        {
            while (SlabSize < allocationSize)
                SlabSize *= 2;

            slab = static_cast<Slab*>( _aligned_malloc(SlabSize, SlabAlign) );
            CurrentSlabIndex = Slabs.size();
            Slabs.push_back(slab);
            #pragma warning(disable:6386)
            new (slab) Slab{SlabSize};
        }
        return slab;
    }
}
