#pragma once
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
        uint8_t active;  // 1 if this item is active, 0 if this item is DEAD and REMOVED from world
        uint8_t loyalty; // if loyalty == 0xff, then this is a STATIC world object !!!
        uint8_t type; // object type used in filtering findNearby queries
        uint8_t collisionMask; // mask which matches objects this object can collide with
        int objectId; // handle to the object
        Rect rect; // Axis-Aligned Bounding Box

        SpatialObject() = default;

        SpatialObject(uint8_t loyalty, uint8_t type, uint8_t collisionMask, int objectId, Rect rect)
            : active{1}, loyalty{loyalty}, type{type}
            , collisionMask{collisionMask}, objectId{objectId}
            , rect{rect}
        {
        }
    };
}

