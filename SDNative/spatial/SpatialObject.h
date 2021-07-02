#pragma once
#include "Primitives.h" // Rect
#include "CellLoyalty.h"

namespace spatial
{
    /**
     * Describes a generic spatial object with minimal data for
     * spatial subdivision
     */
    struct SpatialObject
    {
        uint8_t active;  // 1 if this item is active, 0 if this item is DEAD and REMOVED from world
        uint8_t type; // object type used in filtering findNearby queries
        uint8_t collisionMask; // mask which matches objects this object can collide with
        uint8_t loyalty; // original loyalty id
        uint32_t loyaltyMask; // mask for matching loyalty, see getLoyaltyMask
        int objectId; // handle to the object
        Rect rect; // Axis-Aligned Bounding Box

        SpatialObject() = default;

        SpatialObject(uint8_t loyalty, uint8_t type, uint8_t collisionMask, int objectId, Rect rect)
            : active{1}, type{type}, collisionMask{collisionMask}, loyalty{loyalty}
            , loyaltyMask{getLoyaltyMask(loyalty)}, objectId{objectId}, rect{rect}
        {
        }
    };
}
