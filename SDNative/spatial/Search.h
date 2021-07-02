#pragma once
#include "Config.h"
#include "SearchOptions.h"
#include "SpatialObject.h"

namespace spatial
{
    class ObjectCollection;

    struct FoundCell
    {
        SpatialObject** objects;
        int count;
        Point world; // cell center
        int radius;
    };
    
    #pragma warning(disable:26495)
    struct SPATIAL_API FoundCells
    {
        static constexpr int MAX = 4096;
        int count = 0;
        int totalObjects = 0;
        FoundCell cells[MAX];

        void add(SpatialObject** objects, int size, Point world, int radius)
        {
            if (count != MAX)
            {
                cells[count++] = FoundCell{ objects, size, world, radius };
                totalObjects += size;
            }
        }

        int filterResults(int* outResults,
                          const ObjectCollection& objects,
                          const SearchOptions& opt);
    };

    // Gets the loyalty mask from Search Options
    SPATIAL_FINLINE uint32_t getLoyaltyMask(const SearchOptions& opt)
    {
        uint32_t loyaltyMask = MATCH_ALL;
        if (opt.OnlyLoyalty)    loyaltyMask = getLoyaltyMask(opt.OnlyLoyalty);
        if (opt.ExcludeLoyalty) loyaltyMask = ~getLoyaltyMask(opt.ExcludeLoyalty);
        return loyaltyMask;
    }
}
