#include "Search.h"
#include <algorithm>

namespace spatial
{
    int findNearby(int* outResults, int maxObjectId, const SearchOptions& opt, FoundNodes& found)
    {
        // we use a bit array to ignore duplicate objects
        // duplication is present by design to handle grid border overlap
        // this filtering is faster than other more complicated structural methods
        int idBitArraySize = ((maxObjectId / 32) + 1) * sizeof(uint32_t);
        #pragma warning(disable:6255)
        uint32_t* idBitArray = (uint32_t*)_alloca(idBitArraySize);
        memset(idBitArray, 0, idBitArraySize);

        const int MATCH_ALL = 0xffffffff; // mask that passes any filter

        int loyaltyMask = MATCH_ALL;
        if (opt.FilterIncludeOnlyByLoyalty) loyaltyMask = opt.FilterIncludeOnlyByLoyalty;
        if (opt.FilterExcludeByLoyalty)     loyaltyMask = ~opt.FilterExcludeByLoyalty;

        int filterMask = (opt.FilterByType == 0)           ? MATCH_ALL : opt.FilterByType;
        int objectMask = (opt.FilterExcludeObjectId == -1) ? MATCH_ALL : ~(opt.FilterExcludeObjectId+1);

        Rect searchRect = opt.SearchRect;
        float radialFR = opt.RadialFilter.radius;
        float radialFX = opt.RadialFilter.x;
        float radialFY = opt.RadialFilter.y;
        bool useSearchRadius = radialFR > 0.0f;

        SearchFilterFunc filterFunc = opt.FilterFunction;
        int maxResults = opt.MaxResults > 0 ? opt.MaxResults : 1;

        FoundNode* nodes = found.nodes;

        // if total candidates is more than we can fit, we need to sort LEAF nodes by distance to Origin
        if (found.totalObjects > maxResults)
        {
            int x = searchRect.centerX();
            int y = searchRect.centerY();
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
                    Rect rect = o.rect();
                    if (!searchRect.overlaps(rect))
                        continue; // AABB's don't overlap

                    if (useSearchRadius)
                    {
                        float dx = radialFX - o.x;
                        float dy = radialFY - o.y;
                        float rr = radialFR + std::max(o.rx, o.ry);
                        if ((dx*dx + dy*dy) > (rr*rr))
                            continue; // not in squared radius
                    }

                    int id = o.objectId;
                    int wordIndex = id / 32;
                    int wordOffset = id % 32;
                    if (idBitArray[wordIndex] & (1<<wordOffset))
                        continue; // already present in results array

                    if (!filterFunc || filterFunc(id) != 0)
                    {
                        outResults[numResults++] = id;
                        idBitArray[wordIndex] |= (1<<wordOffset); // set unique result
                        if (numResults == maxResults)
                            return numResults; // we are done !
                    }
                }
            }
        }
        return numResults;
    }
}
