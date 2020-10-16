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

    // Contains information about different loyalties found in a single cell
    struct CellLoyalty
    {
        // Mask for which loyalties are present in this Node
        // Allows for a special optimization during collision and search
        uint8_t mask = 0;

        // How many different loyalties are present in this cell
        uint8_t count = 0;

        SPATIAL_FINLINE void addLoyalty(uint8_t loyalty)
        {
            if ((mask & (~loyalty)) != 0)
            {
                ++count;
            }
            mask |= loyalty;
        }
    };
}
