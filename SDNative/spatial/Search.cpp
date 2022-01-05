#include "Search.h"
#include "ObjectCollection.h"
#include <algorithm>

namespace spatial
{
    int FoundCells::filterResults(int* outResults,
                                  const ObjectCollection& objects,
                                  const SearchOptions& opt)
    {
        // don't crash if someone asks for 0 results
        int maxResults = opt.MaxResults;
        if (outResults == nullptr || maxResults <= 0)
            return 0;

        // we use a bit array to ignore duplicate objects
        // duplication is present by design to handle grid border overlap
        // this filtering is faster than other more complicated structural methods
        int idBitArraySize = ((objects.maxObjects() / 32) + 1) * sizeof(uint32_t);
        #pragma warning(disable:6255)
        uint32_t* idBitArray = (uint32_t*)_alloca(idBitArraySize);
        memset(idBitArray, 0, idBitArraySize);

        uint32_t loyaltyMask = getLoyaltyMask(opt);
        uint32_t typeMask = (opt.Type == 0) ? MATCH_ALL : opt.Type;
        int excludeObject = opt.Exclude == -1 ? -1 : opt.Exclude;

        Rect searchRect = opt.SearchRect;
        CircleF radialFilter = opt.RadialFilter;
        bool useSearchRadius = opt.RadialFilter.radius > 0;

        SearchFilterFunc filterFunc = opt.FilterFunction;

        FoundCell* cells = this->cells;
        int numCells = this->count;

        // if total candidates is more than we can fit, we need to sort LEAF nodes by distance to Origin
        bool sortByDistance = opt.SortByDistance != 0 || this->totalObjects > maxResults;
        if (sortByDistance)
        {
            int x = searchRect.centerX();
            int y = searchRect.centerY();
            std::sort(cells, cells+numCells, [x,y](const FoundCell& a, const FoundCell& b) -> bool
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
        for (int leafIndex = 0; leafIndex < numCells; ++leafIndex)
        {
            const FoundCell& cell = cells[leafIndex];
            const int size = cell.count;
            SpatialObject** const nodeObjects = cell.objects;
            for (int i = 0; i < size; ++i)
            {
                const SpatialObject& o = *nodeObjects[i];
                
                if ((o.loyaltyMask & loyaltyMask) &&
                    (o.type & typeMask) && 
                    (o.objectId != excludeObject))
                {
                    if (!searchRect.overlaps(o.rect))
                        continue; // AABB's don't overlap

                    if (useSearchRadius)
                    {
                        //if (!o.rect.overlaps(radialFilter)) // RECT and Circle overlap check
                        if (!overlapsRadius(o.rect, radialFilter)) // Convert rect to circle and do Circle/Circle overlap
                            continue;
                    }
                    
                    // PERF: this is the fastest point for duplicate check
                    int id = o.objectId;
                    int wordIndex = id / 32;
                    int idMask = (1 << (id % 32));
                    if (idBitArray[wordIndex] & idMask)
                        continue; // object was already checked

                    idBitArray[wordIndex] |= idMask; // flag it as checked

                    if (!filterFunc || filterFunc(id) != 0)
                    {
                        outResults[numResults++] = id;
                        if (numResults == maxResults)
                            goto finalize; // we are done !
                    }
                }
            }
        }

    finalize:

        // and now sort all results by distance
        //if (sortByDistance)
        //{
        //    int x = searchRect.centerX();
        //    int y = searchRect.centerY();
        //    std::sort(outResults, outResults+numResults, [x,y,objects](int idA, int idB) -> bool
        //    {
        //        const SpatialObject& a = objects[idA];
        //        const SpatialObject& b = objects[idB];
        //        float adx = x - a.rect.centerX();
        //        float ady = y - a.rect.centerY();
        //        float sqDist1 = adx*adx + ady*ady;
        //        float bdx = x - b.rect.centerX();
        //        float bdy = y - b.rect.centerY();
        //        float sqDist2 = bdx*bdx + bdy*bdy;
        //        return sqDist1 < sqDist2;
        //    });
        //}
        return numResults;
    }
}
