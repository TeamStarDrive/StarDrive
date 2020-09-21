#include "Search.h"
#include <algorithm>

namespace spatial
{
    int findNearby(int* outResults, const SearchOptions& opt, FoundNodes& found)
    {
        int exclLoyaltyMask = (opt.FilterExcludeByLoyalty == 0)     ? 0xffffffff : ~opt.FilterExcludeByLoyalty;
        int onlyLoyaltyMask = (opt.FilterIncludeOnlyByLoyalty == 0) ? 0xffffffff : opt.FilterIncludeOnlyByLoyalty;
        int filterMask      = (opt.FilterByType == 0)               ? 0xffffffff : opt.FilterByType;
        int objectMask      = (opt.FilterExcludeObjectId == -1)     ? 0xffffffff : ~(opt.FilterExcludeObjectId+1);
        int x = opt.OriginX;
        int y = opt.OriginY;
        float radius = opt.SearchRadius;
        SearchFilterFunc filterFunc = opt.FilterFunction;
        int maxResults = opt.MaxResults;

        FoundNode* nodes = found.nodes;

        // if total candidates is more than we can fit, we need to sort LEAF nodes by distance to Origin
        if (found.totalObjects > maxResults)
        {
            std::sort(nodes, nodes+found.count, [x,y](const FoundNode& a, const FoundNode& b) -> bool
            {
                float adx = x - a.worldX;
                float ady = y - a.worldY;
                float sqDist1 = adx*adx + ady*ady;
                float bdx = x - b.worldX;
                float bdy = y - b.worldY;
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
                if (o.active
                    && (o.loyalty & exclLoyaltyMask)
                    && (o.loyalty & onlyLoyaltyMask)
                    && (o.type     & filterMask)
                    && ((o.objectId+1) & objectMask))
                {
                    // check if inside radius, inlined for perf
                    float dx = x - o.x;
                    float dy = y - o.y;
                    float r2 = radius + o.rx;
                    if ((dx*dx + dy*dy) <= (r2*r2))
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
