#pragma once
#include "QtreeConstants.h"
#include "QtreeObject.h"
#include "QtreeArray.h"

namespace tree
{
    class QtreeAllocator;

    struct QtreeNode
    {
        QtreeNode* nodes = nullptr;
        QtreeArray<QtreeObject, QuadCellThreshold> objects;

        QtreeNode* nw() const { return nodes; }
        QtreeNode* ne() const { return (nodes + 1); }
        QtreeNode* se() const { return (nodes + 2); }
        QtreeNode* sw() const { return (nodes + 3); }
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

        QtreeBoundedNode nw() const
        {
            return QtreeBoundedNode{ node->nw(), (left+cx)>>1, (top+cy)>>1, left, top, cx, cy };
        }

        QtreeBoundedNode ne() const
        {
            return QtreeBoundedNode{ node->ne(), (cx+right)>>1, (top+cy)>>1, cx, top, right, cy };
        }

        QtreeBoundedNode se() const
        {
            return QtreeBoundedNode{ node->se(), (cx+right)>>1, (cy+bottom)>>1, cx, cy, right, bottom };
        }
        
        QtreeBoundedNode sw() const
        {
            return QtreeBoundedNode{ node->sw(), (left+cx)>>1, (cy+bottom)>>1, left, cy, cx, bottom };
        }

        bool overlaps(const QtreeRect& r) const
        {
            return left <= r.right  && right  > r.left
                && top  <= r.bottom && bottom > r.top;
        }
    };
}
