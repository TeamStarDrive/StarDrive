#pragma once
#include "SpatialObj.h"

namespace tree
{
    class QtreeAllocator;

    struct QtreeNode
    {
        QtreeNode* NW = nullptr;
        QtreeNode* NE = nullptr;
        QtreeNode* SE = nullptr;
        QtreeNode* SW = nullptr;
        int Count = 0;
        int Capacity = 0;
        SpatialObj* Items = nullptr;

        QtreeNode() = default;

        QtreeNode(QtreeNode&&) = delete;
        QtreeNode(const QtreeNode&) = delete;
        QtreeNode& operator=(QtreeNode&&) = delete;
        QtreeNode& operator=(const QtreeNode&) = delete;

        void add(QtreeAllocator& allocator, const SpatialObj& obj);
    };

    // Simple helper: Node pointer, with Center XY and Bounds AABB
    // We don't store these in the tree itself, in order to save space and improve cache locality
    struct QtreeBoundedNode
    {
        QtreeNode* node;
        float cx, cy;
        float left;
        float top;
        float right;
        float bottom;

        //QtreeBoundedNode(QtreeNode* node, const QtreeRect& r)
        //    : node{node}, cx{r.centerX()}, cy{r.centerY()}, bounds{r}
        //{
        //}
        
        QtreeBoundedNode nw() const
        {
            return QtreeBoundedNode{ node->NW, (left+cx)/2, (top+cy)/2, left, top, cx, cy };
        }

        QtreeBoundedNode ne() const
        {
            return QtreeBoundedNode{ node->NE, (cx+right)/2, (top+cy)/2, cx, top, right, cy };
        }

        QtreeBoundedNode se() const
        {
            return QtreeBoundedNode{ node->SE, (cx+right)/2, (cy+bottom)/2, cx, cy, right, bottom };
        }
        
        QtreeBoundedNode sw() const
        {
            return QtreeBoundedNode{ node->SW, (left+cx)/2, (cy+bottom)/2, left, cy, cx, bottom };
        }

        bool overlaps(const QtreeRect& r) const
        {
            return left <= r.right  && right  > r.left
                && top  <= r.bottom && bottom > r.top;
        }
    };
}
