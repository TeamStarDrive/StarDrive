#pragma once
#include "Config.h"
#include "SpatialObject.h"

namespace spatial
{
    /**
     * Function type for final filtering of search results
     * @return 0 Search filter failed and this object should be excluded
     * @return 1 Search filter Succeeded and object should be included
     */
    using SearchFilterFunc = int (SPATIAL_CC*)(int objectA);

    /**
     * Configuration parameters for a single search operation
     */
    struct SearchOptions
    {
        /// Search rectangle
        Rect SearchRect = Rect::Zero();

        /// Radial filter
        /// If set, search results will have an additional filtering pass using this circle
        Circle RadialFilter = Circle::Zero();

        /// Maximum number of filtered final results until search is terminated
        /// Must be at least 1
        int MaxResults = 10;

        /// Filter search results by object type
        /// 0: disabled
        int Type = 0;

        /// Filter search results by excluding this specific object
        /// -1: disabled
        int Exclude = -1;

        /// Filter search results by excluding objects with this loyalty
        /// 0: disabled
        int ExcludeLoyalty = 0;

        /// Filter search results by only matching objects with this loyalty
        /// 0: disabled
        int OnlyLoyalty = 0;

        /// Filter search results by passing the matched object through this function
        /// null: disabled
        /// Return 0: filter failed, object discarded
        /// Return 1: filter passed, object added to results
        SearchFilterFunc FilterFunction = nullptr;

        /// <summary>
        /// If set to nonzero, this search will be displayed
        /// as an unique entry
        /// </summary>
        int DebugId = 0;
    };

    struct FoundNode
    {
        SpatialObject** objects;
        int count;
        Point world;
        int radius;
    };
    
    #pragma warning(disable:26495)
    struct FoundNodes
    {
        static constexpr int MAX = 4096;
        int count = 0;
        int totalObjects = 0;
        FoundNode nodes[MAX];

        void add(SpatialObject** objects, int size, Point world, int radius)
        {
            if (count != MAX)
            {
                nodes[count++] = FoundNode{ objects, size, world, radius };
                totalObjects += size;
            }
        }
    };

    // Gets the loyalty mask from Search Options
    SPATIAL_FINLINE uint32_t getLoyaltyMask(const SearchOptions& opt)
    {
        uint32_t loyaltyMask = MATCH_ALL;
        if (opt.OnlyLoyalty)    loyaltyMask = getLoyaltyMask(opt.OnlyLoyalty);
        if (opt.ExcludeLoyalty) loyaltyMask = ~getLoyaltyMask(opt.ExcludeLoyalty);
        return loyaltyMask;
    }

    int findNearby(int* outResults, int maxObjectId, const SearchOptions& opt, FoundNodes& found);

}
