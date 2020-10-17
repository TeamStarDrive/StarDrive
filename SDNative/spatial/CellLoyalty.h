#pragma once
#include "Search.h"

namespace spatial
{
    // Contains information about different loyalties found in a single cell
    struct CellLoyalty
    {
        // Mask for which loyalties are present in this Node
        // Allows for a special optimization during collision and search
        uint32_t mask = 0;

        // How many different loyalties are present in this cell
        uint8_t count = 0;

        SPATIAL_FINLINE void addLoyalty(uint8_t loyalty)
        {
            uint32_t thisMask = getLoyaltyMask(loyalty);
            if ((mask & thisMask) == 0) // this mask not present yet?
                ++count;
            mask |= thisMask;
        }
    };
}
