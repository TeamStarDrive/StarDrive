#pragma once
#include "Config.h"
#include "SlabAllocator.h"
#include "SpatialObject.h"

namespace spatial
{
    struct SpatialObjectArray
    {
        SpatialObject** objects;
        int size;
    };

    struct SpatialIdArray
    {
        int* ids = nullptr;
        int size = 0;
        int capacity = 0;

        SPATIAL_FINLINE void addId(SlabAllocator& allocator, int id, int defaultCapacity)
        {
            if (size == capacity)
            {
                capacity = (capacity == 0) ? defaultCapacity : capacity*2;
                ids = allocator.allocArray(ids, size, capacity);
            }
            ids[size++] = id;
        }
    };
}
