#pragma once
#include "Config.h"
#include "SlabAllocator.h"
#include "SpatialObject.h"

namespace spatial
{
    // Transient View of a SpatialObject Cell
    struct SpatialObjectsView
    {
        SpatialObject** objects;
        int size;
    };

    template<class T> struct Array
    {
        T* data = nullptr;
        int size = 0;
        int capacity = 0;

        
        T* begin() { return data; }
        T* end()   { return data+size; }
        const T* begin() const { return data; }
        const T* end()   const { return data+size; }

        SPATIAL_FINLINE void add(SlabAllocator& allocator, const T& item, int defaultCapacity)
        {
            if (size == capacity)
            {
                capacity = (capacity == 0) ? defaultCapacity : capacity*2;
                data = allocator.allocArray(data, size, capacity);
            }
            data[size++] = item;
        }
    };

}
