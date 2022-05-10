using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDUtils;

namespace Ship_Game.Spatial
{
    /// <summary>
    /// Shared utility for performing linear searches
    /// This is used for validation and error detection of Spatial algorithms
    /// </summary>
    public unsafe class LinearSearch
    {
        public static GameObject[] FindNearby(ref SearchOptions opt, GameObject[] objects, int count)
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
                GameObject obj = objects[i];
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

        public static GameObject[] Copy(int* objectIds, int count, GameObject[] objects)
        {
            if (count == 0)
                return Empty<GameObject>.Array;

            var found = new GameObject[count];
            for (int i = 0; i < found.Length; ++i)
            {
                int spatialIndex = objectIds[i];
                GameObject go = objects[spatialIndex];
                found[i] = go;
            }
            return found;
        }
    }
}
