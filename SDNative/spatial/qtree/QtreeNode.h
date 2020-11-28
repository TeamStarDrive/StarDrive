#pragma once
#include <stdexcept>
#include "../SlabAllocator.h"
#include "../Config.h"
#include "../SpatialObject.h"
#include "../Utilities.h"
#include "../Primitives.h"
#include "../CellLoyalty.h"

namespace spatial
{
    struct QtreeNode
    {
        // Our node can be in 2 states:
        // BRANCH: It has no objects and has 4 sub nodes
        // LEAF:   It has only objects and NO sub nodes
        // This is done to save one pointer storage
        union
        {
            QtreeNode* nodes = nullptr;
            SpatialObject** objects;
        };

        static constexpr uint16_t BRANCH_ID = -1;

        // if size == -1, this is a BRANCH
        // if size >= 0, this is a LEAF
        // by default start as a LEAF
        uint16_t size = 0;
        uint16_t capacity = 0;

        // node center-x, center-y, radius
        int cx = 0;
        int cy = 0;
        int radius = 0;

        CellLoyalty loyalty;

        bool isBranch() const { return size == BRANCH_ID; }
        bool isLeaf()   const { return size != BRANCH_ID; }

        SPATIAL_FINLINE QtreeNode* nw() const { return &nodes[0]; }
        SPATIAL_FINLINE QtreeNode* ne() const { return &nodes[1]; }
        SPATIAL_FINLINE QtreeNode* se() const { return &nodes[2]; }
        SPATIAL_FINLINE QtreeNode* sw() const { return &nodes[3]; }

        SPATIAL_FINLINE int height() const { return (radius<<1); }
        SPATIAL_FINLINE int width()  const { return (radius<<1); }
        SPATIAL_FINLINE int left()   const { return cx - radius; }
        SPATIAL_FINLINE int right()  const { return cx + radius; }
        SPATIAL_FINLINE int top()    const { return cy - radius; }
        SPATIAL_FINLINE int bottom() const { return cy + radius; }
        SPATIAL_FINLINE Rect rect() const { return { cx-radius, cy-radius, cx+radius, cy+radius }; }

        SPATIAL_FINLINE void setCoords(int centerX, int centerY, int nodeRadius)
        {
            cx = centerX;
            cy = centerY;
            radius = nodeRadius;
        }

        // fast adding of an object
        SPATIAL_FINLINE void addObject(SlabAllocator& allocator, SpatialObject* item, int defaultCapacity)
        {
            if (size == capacity)
            {
                int newCapacity = capacity == 0 ? defaultCapacity : capacity*2;
                if (newCapacity > UINT16_MAX)
                {
                    newCapacity = UINT16_MAX;
                    if (size == UINT16_MAX)
                        throw std::runtime_error{"QtreeNode::addObject failed: UINT16_MAX capacity reached"};
                }

                SpatialObject** oldObjects = objects;
                objects = allocator.allocArray(oldObjects, size, newCapacity);
                allocator.reuseArray(oldObjects, size); // reuse this array next time
                capacity = newCapacity;
            }
            objects[size++] = item;
            loyalty.addLoyaltyMask(item->loyaltyMask);
        }

        // Converts a LEAF node into a BRANCH node which contains sub-QtreeNode's
        SPATIAL_FINLINE void convertToBranch(SlabAllocator& allocator)
        {
            // create 4 LEAF nodes
            nodes = allocator.allocArrayZeroed<QtreeNode>(4);
            size = BRANCH_ID;
            
            const int r2 = radius >> 1;
            nodes[0].setCoords(cx-r2, cy-r2, r2); // NW
            nodes[1].setCoords(cx+r2, cy-r2, r2); // NE
            nodes[2].setCoords(cx+r2, cy+r2, r2); // SE
            nodes[3].setCoords(cx-r2, cy+r2, r2); // SW
        }
    };
}
