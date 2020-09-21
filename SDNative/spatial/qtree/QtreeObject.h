#pragma once
#include "QtreeConstants.h"
#include <cstdint>

namespace spatial
{
    struct QtreeObject
    {
        uint8_t active;  // 1 if this item is active, 0 if this item is DEAD
        uint8_t loyalty; // if loyalty == 0, then this is a STATIC world object !!!
        uint8_t type; // GameObjectType : byte
        uint8_t reserved;
        int objectId; // handle to the object

        int x, y; // Center x y
        int rx, ry; // Radius x y

        QtreeObject() = default;

        QtreeObject(uint8_t loyalty, uint8_t type, int objectId,
                    int centerX, int centerY, int radiusX, int radiusY)
            : active{1}, loyalty{loyalty}, type{type}, reserved{}, objectId{objectId}
            , x{centerX}, y{centerY}, rx{radiusX}, ry{radiusY}
        {
        }

        TREE_FINLINE int left()   const { return x - rx; }
        TREE_FINLINE int right()  const { return x + rx; }
        TREE_FINLINE int top()    const { return y - ry; }
        TREE_FINLINE int bottom() const { return y + ry; }

        TREE_FINLINE bool overlaps(const QtreeObject& o) const
        {
            return left() <= o.right()  && right()  > o.left()
                && top()  <= o.bottom() && bottom() > o.top();
        }
    };
}