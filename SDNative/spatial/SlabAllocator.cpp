#include "SlabAllocator.h"
#include <stdexcept>
#include <rpp/debugging.h>

namespace spatial
{
    ////////////////////////////////////////////////////////////////////////////

    const int SlabAlign = 16;

    struct SlabAllocator::Slab
    {
        uint32_t capacity;
        int remaining;
        uint8_t* ptr;
        explicit Slab(uint32_t cap) noexcept : capacity{cap} { reset(); }
        void reset() noexcept
        {
            // everything after Slab member fields is free-to-use memory
            remaining = capacity - SlabAlign;
            ptr = reinterpret_cast<uint8_t*>(this) + SlabAlign;
        }
    };

    ////////////////////////////////////////////////////////////////////////////

    SlabAllocator::SlabAllocator(size_t slabSizeBytes) noexcept : SlabSizeBytes{ slabSizeBytes }
    {
        addSlab(SlabSizeBytes);
    }

    SlabAllocator::~SlabAllocator() noexcept
    {
        for (Slab* slab : Slabs)
            _aligned_free(slab);
    }
    
    uint32_t SlabAllocator::totalBytes() const noexcept
    {
        uint32_t bytes = sizeof(SlabAllocator);
        for (Slab* slab : Slabs)
            bytes += slab->capacity + SlabAlign;
        return bytes;
    }

    void SlabAllocator::reset() noexcept
    {
        Active.assign(Slabs);
        for (Slab* active : Active)
            active->reset();

        ReuseArrayAlloc.clear();
    }

    void* SlabAllocator::allocArray(void* oldArray, int oldCount, int newCapacity, int sizeOf) noexcept
    {
        int newBytes = newCapacity*sizeOf;
        void* newArray = ReuseArrayAlloc.try_pop(newBytes);
        if (!newArray)
        {
            newArray = alloc(newCapacity*sizeOf);
        }

        if (oldArray != nullptr)
        {
            memcpy(newArray, oldArray, oldCount*sizeOf);
        }
        return newArray;
    }

    void SlabAllocator::reuseArray(void* arr, int capacity, int sizeOf) noexcept
    {
        if (Slab* slab = static_cast<Slab*>(arr))
        {
            slab->remaining = slab->capacity = capacity * sizeOf;
            slab->ptr = nullptr;
            ReuseArrayAlloc.push_back(slab);
        }
    }

    void* SlabAllocator::alloc(uint32_t numBytes) noexcept
    {
        uint32_t alignedBytes = numBytes;
        if (uint32_t rem = numBytes % SlabAlign)
            alignedBytes += (SlabAlign - rem);

        Slab* slab = getSlabForAlloc(alignedBytes);
        void* ptr = slab->ptr;
        slab->remaining -= alignedBytes;
        slab->ptr       += alignedBytes;

        if (slab->remaining < 0)
        {
            __assertion_failure("SlabAllocator::alloc error");
        }
        return ptr;
    }

    SlabAllocator::Slab* SlabAllocator::addSlab(uint32_t slabSizeInBytes) noexcept
    {
        Slab* slab = static_cast<Slab*>(_aligned_malloc(slabSizeInBytes, SlabAlign));
        #pragma warning (disable:6386)
        new (slab) Slab{ slabSizeInBytes };
        Slabs.push_back(slab);
        Active.push_back(slab);
        return slab;
    }

    SlabAllocator::Slab* SlabAllocator::getSlabForAlloc(int allocationSize) noexcept
    {
        // work on raw arrays for speeeeed
        int count = Active.Size;
        Slab** activeSlabs = Active.Data;

        for (int i = count - 1; i >= 0; --i)
        {
            Slab* slab = activeSlabs[i];
            if (slab->remaining <= SlabAlign) // this slab is depleted, remove from active
            {
                // RemoveAtSwapLast:
                int last = --count;
                activeSlabs[i] = activeSlabs[last];
                --Active.Size; // pop_back: update the actual collection
                continue;
            }

            if (slab->remaining >= allocationSize)
                return slab;
        }

        // increase slab size dynamically
        size_t requiredSlabSize = allocationSize + SlabAlign;
        while (SlabSizeBytes < requiredSlabSize)
            SlabSizeBytes *= 2;

        return addSlab(SlabSizeBytes);
    }

    ////////////////////////////////////////////////////////////////////////////

    inline SlabAllocator::SlabArray::SlabArray() noexcept
    {
        Size = 0;
        Capacity = 32;
        Data = (Slab**)_aligned_malloc(sizeof(Slab*) * Capacity, 16);
    }

    inline SlabAllocator::SlabArray::~SlabArray() noexcept
    {
        _aligned_free(Data);
    }

    inline void SlabAllocator::SlabArray::push_back(Slab* slab) noexcept
    {
        if (Size == Capacity)
        {
            Capacity *= 2;
            Data = (Slab**)_aligned_realloc(Data, sizeof(Slab*) * Capacity, 16);
            if (Data == nullptr) std::terminate();
        }
        Data[Size++] = slab;
    }

    inline void SlabAllocator::SlabArray::assign(const SlabArray& other) noexcept
    {
        Size = other.Size;
        if (Capacity < Size)
        {
            _aligned_free(Data);
            Capacity = other.Capacity;
            Data = (Slab**)_aligned_malloc(sizeof(Slab*) * Capacity, 16);
            if (Data == nullptr) std::terminate();
        }
        for (int i = 0; i < Size; ++i)
            Data[i] = other.Data[i];
    }

    inline SlabAllocator::Slab* SlabAllocator::SlabArray::try_pop(int size) noexcept
    {
        if (Size)
        {
            Slab* slab = Data[Size - 1];
            if (slab->remaining >= size)
            {
                --Size;
                return slab;
            }
        }
        return nullptr;
    }

    ////////////////////////////////////////////////////////////////////////////
}
