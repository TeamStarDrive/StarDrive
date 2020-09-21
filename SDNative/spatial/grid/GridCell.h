#pragma once
#include <stdexcept>
#include "../SpatialObject.h"
#include "../SlabAllocator.h"

namespace spatial
{
    struct GridCell
    {
        // all objects within this grid node
        SpatialObject** objects = nullptr;

        // # of objects
        uint16_t size = 0;
        uint16_t capacity = 0;

        // adds another object
        SPATIAL_FINLINE void addObject(SlabAllocator& allocator,
                                       SpatialObject* item, int defaultCapacity)
        {
            if (size == capacity)
            {
                int newCapacity = capacity == 0 ? defaultCapacity : capacity*2;
                if (newCapacity > UINT16_MAX)
                {
                    newCapacity = UINT16_MAX;
                    if (size == UINT16_MAX)
                        throw std::runtime_error{"GridCell::addObject failed: UINT16_MAX capacity reached"};
                }
                objects = allocator.allocArray(objects, size, newCapacity);
                capacity = newCapacity;
            }
            objects[size++] = item;
        }
    };

}