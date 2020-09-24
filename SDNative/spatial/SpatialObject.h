#pragma once
#include "Config.h"
#include "Primitives.h"
#include <cstdint>

namespace spatial
{
    /**
     * Describes a generic spatial object with minimal data for
     * spatial subdivision
     */
    struct SpatialObject
    {
        uint8_t active;  // 1 if this item is active, 0 if this item is DEAD
        uint8_t loyalty; // if loyalty == 0, then this is a STATIC world object !!!
        uint8_t type; // GameObjectType : byte
        uint8_t update; // if 1, this object should be updated in the Spatial collection
        int objectId; // handle to the object

        int x, y; // Center x y
        int rx, ry; // Radius x y

        SpatialObject() = default;

        SpatialObject(uint8_t loyalty, uint8_t type, int objectId,
                      int centerX, int centerY, int radiusX, int radiusY)
            : active{1}, loyalty{loyalty}, type{type}, update{0}, objectId{objectId}
            , x{centerX}, y{centerY}, rx{radiusX}, ry{radiusY}
        {
        }

        SPATIAL_FINLINE int left()   const { return x - rx; }
        SPATIAL_FINLINE int right()  const { return x + rx; }
        SPATIAL_FINLINE int top()    const { return y - ry; }
        SPATIAL_FINLINE int bottom() const { return y + ry; }
        SPATIAL_FINLINE Rect rect()  const { return { x-rx, y-ry, x+rx, y+ry }; }

        SPATIAL_FINLINE bool overlaps(const SpatialObject& o) const
        {
            return left() <= o.right()  && right()  > o.left()
                && top()  <= o.bottom() && bottom() > o.top();
        }
    };
}