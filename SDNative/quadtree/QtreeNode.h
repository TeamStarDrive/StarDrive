#pragma once
#include "SpatialObj.h"

namespace tree
{
    class QtreeAllocator;

    struct QtreeNode
    {
        float X1, Y1, X2, Y2;
        QtreeNode* NW = nullptr;
        QtreeNode* NE = nullptr;
        QtreeNode* SE = nullptr;
        QtreeNode* SW = nullptr;
        int Count = 0;
        int Capacity = 0;
        SpatialObj* Items = nullptr;
        int Id;
        int Level;
        int TotalTreeDepthCount = 0;

        QtreeNode(int id, int level, float x1, float y1, float x2, float y2)
            : X1{x1}, Y1{y1}, X2{x2}, Y2{y2}, Id{id}, Level{level}
        {
        }

        QtreeNode(QtreeNode&&) = delete;
        QtreeNode(const QtreeNode&) = delete;
        QtreeNode& operator=(QtreeNode&&) = delete;
        QtreeNode& operator=(const QtreeNode&) = delete;

        void add(QtreeAllocator& allocator, const SpatialObj& obj);

        bool overlaps(const SpatialObj& o) const
        {
            return X1 <= o.LastX && X2 > o.X
                && Y1 <= o.LastY && Y2 > o.Y;
        }
    };
}
