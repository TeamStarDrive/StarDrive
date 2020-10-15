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
        void addObject(SlabAllocator& allocator, SpatialObject* item, int defaultCapacity)
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

                SpatialObject** oldObjects = objects;
                objects = allocator.allocArray(oldObjects, size, newCapacity);
                allocator.reuseArray(oldObjects, size); // reuse this array next time
                capacity = newCapacity;
            }
            objects[size++] = item;
        }

        void removeObject(int objectId)
        {
            int n = size;
            SpatialObject** objects = this->objects;
            for (int i = 0; i < n; ++i)
            {
                if (objects[i]->objectId == objectId)
                {
                    int last = --size;
                    objects[i] = objects[last];
                    break;
                }
            }
        }
    };

}