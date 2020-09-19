#pragma once
#include "QtreeConstants.h"
#include "QtreeObject.h"
#include "QtreeAllocator.h"

namespace tree
{
    class QtreeAllocator;

    struct QtreeNode
    {
        // Our node can be two things:
        // BRANCH: It has no objects and has 4 sub nodes
        // LEAF:   It has only objects and NO sub nodes
        // This is done to save one pointer storage and fit this QtreeNode into 8-byte boundary on x86
        union
        {
            QtreeNode* nodes = nullptr;
            QtreeObject* objects;
        };

        static constexpr int BRANCH_ID = -1;

        // if size == -1, this is a BRANCH
        // if size >= 0, this is a LEAF
        // by default start as a LEAF
        int size = 0;

        bool isBranch() const { return size == BRANCH_ID; }
        bool isLeaf()   const { return size != BRANCH_ID; }

        QtreeNode* nw() const { return &nodes[0]; }
        QtreeNode* ne() const { return &nodes[1]; }
        QtreeNode* se() const { return &nodes[2]; }
        QtreeNode* sw() const { return &nodes[3]; }

        // fast adding of an object
        __forceinline void addObject(QtreeAllocator& allocator, const QtreeObject& item)
        {
            if (size == 0)
            {
                objects = allocator.allocArray<QtreeObject>(QuadCellThreshold);
            }
            objects[size++] = item;
        }

        // compute the next highest power of 2 of 32-bit v
        static constexpr int upperPowerOf2(unsigned int v)
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
        __forceinline void addObjectUnbounded(QtreeAllocator& allocator, const QtreeObject& item)
        {
            if (size == QuadCellThreshold)
            {
                constexpr int capacity = upperPowerOf2(QuadCellThreshold+1);
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
        __forceinline void convertToBranch(QtreeAllocator& allocator)
        {
            // create 4 LEAF nodes
            nodes = allocator.allocArrayZeroed<QtreeNode>(4);
            size = BRANCH_ID;
        }
    };

    // Simple helper: Node pointer, with Center XY and Bounds AABB
    // We don't store these in the tree itself, in order to save space and improve cache locality
    struct QtreeBoundedNode
    {
        QtreeNode* node;
        int cx, cy;
        int left;
        int top;
        int right;
        int bottom;

        /** @return TRUE if this is LEAF node with no child nodes */
        __forceinline bool isLeaf() const { return node->isLeaf(); }

        /** @return TRUE if this is a branch with child nodes */
        __forceinline bool isBranch() const { return node->isBranch(); }

        __forceinline int height() const { return bottom - top; }
        __forceinline int width()  const { return right - left; }

        __forceinline QtreeBoundedNode nw() const
        {
            return QtreeBoundedNode{ node->nw(), (left+cx)>>1, (top+cy)>>1, left, top, cx, cy };
        }

        __forceinline QtreeBoundedNode ne() const
        {
            return QtreeBoundedNode{ node->ne(), (cx+right)>>1, (top+cy)>>1, cx, top, right, cy };
        }

        __forceinline QtreeBoundedNode se() const
        {
            return QtreeBoundedNode{ node->se(), (cx+right)>>1, (cy+bottom)>>1, cx, cy, right, bottom };
        }
        
        __forceinline QtreeBoundedNode sw() const
        {
            return QtreeBoundedNode{ node->sw(), (left+cx)>>1, (cy+bottom)>>1, left, cy, cx, bottom };
        }

        __forceinline bool overlaps(const QtreeRect& r) const
        {
            return left <= r.right  && right  > r.left
                && top  <= r.bottom && bottom > r.top;
        }
    };
}
