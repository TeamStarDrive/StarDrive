#pragma once
#include "QtreeConstants.h"
#include "QtreeObject.h"
#include "QtreeArray.h"

namespace tree
{
    class QtreeAllocator;

    struct QtreeNode
    {
        QtreeNode* nodes;
        QtreeArray<QtreeObject, QuadCellThreshold> objects;

        QtreeNode& nw() const { return *nodes; }
        QtreeNode& ne() const { return *(nodes + 1); }
        QtreeNode& se() const { return *(nodes + 2); }
        QtreeNode& sw() const { return *(nodes + 3); }
    };

    // Simple helper: Node pointer, with Center XY and Bounds AABB
    // We don't store these in the tree itself, in order to save space and improve cache locality
    struct QtreeBoundedNode
    {
        QtreeNode* nodes;
        QtreeArray<QtreeObject, QuadCellThreshold> objects;

        int cx, cy;
        int left;
        int top;
        int right;
        int bottom;

        //QtreeBoundedNode(QtreeNode* node, const QtreeRect& r)
        //    : node{node}, cx{r.centerX()}, cy{r.centerY()}, bounds{r}
        //{
        //}
        
        QtreeBoundedNode nw() const
        {
            QtreeNode& node = *nodes;
            return QtreeBoundedNode{ node.nodes, node.objects, (left+cx)>>1, (top+cy)>>1, left, top, cx, cy };
        }

        QtreeBoundedNode ne() const
        {
            QtreeNode& node = *(nodes + 1);
            return QtreeBoundedNode{ node.nodes, node.objects, (cx+right)>>1, (top+cy)>>1, cx, top, right, cy };
        }

        QtreeBoundedNode se() const
        {
            QtreeNode& node = *(nodes + 2);
            return QtreeBoundedNode{ node.nodes, node.objects, (cx+right)>>1, (cy+bottom)>>1, cx, cy, right, bottom };
        }
        
        QtreeBoundedNode sw() const
        {
            QtreeNode& node = *(nodes + 3);
            return QtreeBoundedNode{ node.nodes, node.objects, (left+cx)>>1, (cy+bottom)>>1, left, cy, cx, bottom };
        }

        bool overlaps(const QtreeRect& r) const
        {
            return left <= r.right  && right  > r.left
                && top  <= r.bottom && bottom > r.top;
        }
    };
}
