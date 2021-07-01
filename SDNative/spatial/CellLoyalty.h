#pragma once
#include <cstdint>

namespace spatial
{
    constexpr int MAX_LOYALTIES = 24;
    constexpr uint32_t MATCH_ALL = 0x00ff'ffff; // mask that passes MAX_LOYALTIES set filter

    // Convert loyalty ID [1..32] to a loyalty bit mask
    // Only up to 32 id-s are supported
    // For loyalty > 32, mask MATCH_ALL is returned
    SPATIAL_FINLINE uint32_t getLoyaltyMask(uint32_t loyaltyId)
    {
        uint32_t id = (loyaltyId - 1);
        return id < MAX_LOYALTIES ? (1 << id) : MATCH_ALL;
    }

    // Contains information about different loyalties found in a single cell
    struct CellLoyalty
    {
        // Mask for which loyalties are present in this Node
        // Allows for a special optimization during collision and search
        uint32_t mask:24 = 0;

        // How many different loyalties are present in this cell
        uint32_t count:8 = 0;

        SPATIAL_FINLINE void addLoyalty(uint8_t loyalty)
        {
            uint32_t loyaltyMask = getLoyaltyMask(loyalty);
            addLoyaltyMask(loyaltyMask);
        }

        SPATIAL_FINLINE void addLoyaltyMask(uint32_t loyaltyMask)
        {
            if ((mask & loyaltyMask) == 0) // this mask not present yet?
                ++count;
            mask |= loyaltyMask;
        }
    };

    static_assert(sizeof(CellLoyalty) == 4, "CellLoyalty must be 4 bytes");
}
