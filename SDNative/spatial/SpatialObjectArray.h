#pragma once
#include "SlabAllocator.h"
#include "SpatialObject.h"

namespace spatial
{
    struct SpatialObjectArray
    {
        SpatialObject** objects;
        int size;

        // fast adding of an object
        SPATIAL_FINLINE void addObject(SlabAllocator& allocator,
                                       SpatialObject* item, int defaultCapacity)
        {
            if (size == 0)
            {
                objects = allocator.allocArray<SpatialObject*>(defaultCapacity);
            }
            objects[size++] = item;
        }
    };
}
