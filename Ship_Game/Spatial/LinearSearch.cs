using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Spatial
{
    /// <summary>
    /// Shared utility for performing linear searches
    /// This is used for validation and error detection of Spatial algorithms
    /// </summary>
    public unsafe class LinearSearch
    {
        public static GameplayObject[] FindNearby(in SearchOptions opt, GameplayObject[] objects, int count)
        {
            int maxResults = opt.MaxResults > 0 ? opt.MaxResults : 1;
            int* objectIds = stackalloc int[maxResults];
            int resultCount = 0;

            AABoundingBox2D searchRect = opt.SearchRect;
            bool filterByLoyalty = (opt.ExcludeLoyalty != null)
                                || (opt.OnlyLoyalty != null);

            float searchFX = opt.FilterOrigin.X;
            float searchFY = opt.FilterOrigin.Y;
            float searchFR = opt.FilterRadius;
            bool useSearchRadius = searchFR > 0f;

            for (int i = 0; i < count; ++i)
            {
                GameplayObject obj = objects[i];
                if (obj == null
                    || (opt.Exclude != null && obj == opt.Exclude)
                    || (opt.Type != GameObjectType.Any && obj.Type != opt.Type))
                    continue;
                
                if (filterByLoyalty)
                {
                    Empire loyalty = obj.GetLoyalty();
                    if ((opt.ExcludeLoyalty != null && loyalty == opt.ExcludeLoyalty) ||
                        (opt.OnlyLoyalty != null && loyalty != opt.OnlyLoyalty))
                        continue;
                }

                var objectRect = new AABoundingBox2D(obj);
                if (!objectRect.Overlaps(searchRect))
                    continue;

                if (useSearchRadius)
                {
                    if (!objectRect.Overlaps(searchFX, searchFY, searchFR))
                        continue; // AABB not in SearchRadius
                }

                objectIds[resultCount++] = i;
                if (resultCount == maxResults)
                    break; // we are done !
            }
            return Copy(objectIds, resultCount, objects);
        }
        public static GameplayObject[] Copy(int* objectIds, int count, GameplayObject[] objects)
        {
            if (count == 0)
                return Empty<GameplayObject>.Array;

            var found = new GameplayObject[count];
            for (int i = 0; i < found.Length; ++i)
            {
                int spatialIndex = objectIds[i];
                GameplayObject go = objects[spatialIndex];
                found[i] = go;
            }
            return found;
        }
    }
}
