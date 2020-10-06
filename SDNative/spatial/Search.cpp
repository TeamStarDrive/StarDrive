#include "Search.h"
#include <algorithm>

namespace spatial
{
    int findNearby(int* outResults, const SearchOptions& opt, FoundNodes& found)
    {
        const int MATCH_ALL = 0xffffffff; // mask that passes any filter

        int loyaltyMask = MATCH_ALL;
        if (opt.FilterIncludeOnlyByLoyalty) loyaltyMask = opt.FilterIncludeOnlyByLoyalty;
        if (opt.FilterExcludeByLoyalty)     loyaltyMask = ~opt.FilterExcludeByLoyalty;

        int filterMask = (opt.FilterByType == 0)           ? MATCH_ALL : opt.FilterByType;
        int objectMask = (opt.FilterExcludeObjectId == -1) ? MATCH_ALL : ~(opt.FilterExcludeObjectId+1);
        int x = opt.OriginX;
        int y = opt.OriginY;
        int radius = opt.SearchRadius;
        SearchFilterFunc filterFunc = opt.FilterFunction;
        int maxResults = opt.MaxResults;

        FoundNode* nodes = found.nodes;

        // if total candidates is more than we can fit, we need to sort LEAF nodes by distance to Origin
        if (found.totalObjects > maxResults)
        {
            std::sort(nodes, nodes+found.count, [x,y](const FoundNode& a, const FoundNode& b) -> bool
            {
                float adx = x - a.world.x;
                float ady = y - a.world.y;
                float sqDist1 = adx*adx + ady*ady;
                float bdx = x - b.world.x;
                float bdy = y - b.world.y;
                float sqDist2 = bdx*bdx + bdy*bdy;
                return sqDist1 < sqDist2;
            });
        }

        int numResults = 0;
        for (int leafIndex = 0; leafIndex < found.count; ++leafIndex)
        {
            const FoundNode& node = nodes[leafIndex];
            const int size = node.count;
            SpatialObject** const objects = node.objects;
            for (int i = 0; i < size; ++i)
            {
                const SpatialObject& o = *objects[i];
                if ((o.loyalty & loyaltyMask) &&
                    (o.type    & filterMask) && 
                    ((o.objectId+1) & objectMask))
                {
                    if (inRadius(x,y, o.x,o.y, radius, o.rx))
                    {
                        if (!filterFunc || filterFunc(o.objectId) != 0)
                        {
                            outResults[numResults++] = o.objectId;
                            if (numResults >= maxResults)
                                return numResults; // we are done !
                        }
                    }
                }
            }
        }
        return numResults;
    }
}
