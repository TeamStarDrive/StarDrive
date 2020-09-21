#pragma once
#include "QtreeAllocator.h"
#include "../Config.h"
#include "../SpatialObject.h"

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

        static constexpr int BRANCH_ID = -1;

        // if size == -1, this is a BRANCH
        // if size >= 0, this is a LEAF
        // by default start as a LEAF
        int size = 0;

        // node center-x, center-y, radius
        int cx = 0;
        int cy = 0;
        int radius = 0;

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

        SPATIAL_FINLINE void setCoords(int centerX, int centerY, int nodeRadius)
        {
            cx = centerX;
            cy = centerY;
            radius = nodeRadius;
        }

        // fast adding of an object
        SPATIAL_FINLINE void addObject(QtreeAllocator& allocator, SpatialObject* item, int defaultCapacity)
        {
            if (size == 0)
            {
                objects = allocator.allocArray<SpatialObject*>(defaultCapacity);
            }
            objects[size++] = item;
        }

        // compute the next highest power of 2 of 32-bit v
        SPATIAL_FINLINE static int upperPowerOf2(unsigned int v)
        {
            --v;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            ++v;
            return v;
        }

        // adds another object, growth is only limited by QuadLinearAllocatorSlabSize
        SPATIAL_FINLINE void addObjectUnbounded(QtreeAllocator& allocator, SpatialObject* item, int defaultCapacity)
        {
            if (size == defaultCapacity)
            {
                int capacity = upperPowerOf2(defaultCapacity+1);
                objects = allocator.allocArray(objects, size, capacity);
            }
            else
            {
                int capacity = upperPowerOf2(size+1);
                if (size == capacity)
                {
                    objects = allocator.allocArray(objects, size, capacity);
                }
            }
            objects[size++] = item;
        }

        // Converts a LEAF node into a BRANCH node which contains sub-QtreeNode's
        SPATIAL_FINLINE void convertToBranch(QtreeAllocator& allocator)
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
