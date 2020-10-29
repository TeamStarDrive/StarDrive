#pragma once
#include "Primitives.h"
#include <cstdint>

namespace spatial
{
    const uint32_t MATCH_ALL = 0xffff'ffff; // mask that passes any filter

    // Convert loyalty ID [1..32] to a loyalty bit mask
    // Only up to 32 id-s are supported
    // For loyalty > 32, mask MATCH_ALL is returned
    SPATIAL_FINLINE uint32_t getLoyaltyMask(uint32_t loyaltyId)
    {
        uint32_t id = (loyaltyId - 1);
        return id < 32 ? (1 << id) : MATCH_ALL;
    }

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
