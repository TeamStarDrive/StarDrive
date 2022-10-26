using System.Threading;

namespace Ship_Game.Spatial
{
    public partial class Qtree
    {
        // NOTE: This .NET ThreadLocal implementation is incredibly fast
        readonly ThreadLocal<FindResultBuffer<QtreeNode>> FindBuffer = new(() => new());

        FindResultBuffer<QtreeNode> GetThreadLocalTraversalBuffer(QtreeNode root, int maxResults = 0)
        {
            return FindBuffer.Value.Get(root, maxResults);
        }

        public unsafe SpatialObjectBase[] FindNearby(in SearchOptions opt)
        {
            AABoundingBox2D searchRect = opt.SearchRect;
            int maxResults = opt.MaxResults > 0 ? opt.MaxResults : 1;

            (SpatialObjectBase[] objects, QtreeNode root) = GetObjectsAndRootSafe();

            int idBitArraySize = ((objects.Length / 32) + 1) * sizeof(uint);
            uint* idBitArray = stackalloc uint[idBitArraySize]; // C# spec says contents undefined
            for (int i = 0; i < idBitArraySize; ++i) idBitArray[i] = 0; // so we need to zero the idBitArray

            uint loyaltyMask = NativeSpatialObject.GetLoyaltyMask(opt);
            uint typeMask = opt.Type == GameObjectType.Any ? 0xff : (uint)opt.Type;
            int excludeObject = opt.Exclude?.SpatialIndex ?? -1;

            float searchFX = opt.FilterOrigin.X;
            float searchFY = opt.FilterOrigin.Y;
            float searchFR = opt.FilterRadius;
            bool useSearchRadius = searchFR > 0f;

            FindResultBuffer<QtreeNode> buffer = GetThreadLocalTraversalBuffer(root, maxResults);
            DebugQtreeFind dfn = GetFindDebug(opt);

            do
            {
                QtreeNode current = buffer.Pop();
                if (current.NW != null) // isBranch
                {
                    buffer.PushOverlappingQuadrants(current, searchRect);
                }
                else // isLeaf
                {
                    int count = current.Count;
                    if (count == 0 || (current.LoyaltyMask & loyaltyMask) == 0)
                        continue;

                    dfn?.FindCells.Add(current.AABB);

                    SpatialObj*[] items = current.Items;
                    for (int i = 0; i < count; ++i)
                    {
                        SpatialObj* so = items[i];

                        // FLAGS: either 0x00 (failed) or some bits 0100 (success)
                        if ((so->LoyaltyMask & loyaltyMask) != 0 &&
                            ((uint)so->Type & typeMask) != 0 &&
                            (so->ObjectId != excludeObject))
                        {
                            if (!so->AABB.Overlaps(searchRect))
                                continue;

                            if (useSearchRadius)
                            {
                                if (!so->AABB.Overlaps(searchFX, searchFY, searchFR))
                                    continue; // AABB not in SearchRadius
                            }
                            
                            // PERF: this is the fastest point for duplicate check
                            int id = so->ObjectId;
                            int wordIndex = id / 32;
                            uint idMask = (uint)(1 << (id % 32));
                            if ((idBitArray[wordIndex] & idMask) != 0)
                                continue; // object was already checked

                            idBitArray[wordIndex] |= idMask; // flag it as checked

                            SpatialObjectBase go = objects[id];
                            if (opt.FilterFunction == null || opt.FilterFunction(go))
                            {
                                dfn?.SearchResults.Add(go);
                                buffer.Items[buffer.Count++] = go;
                                if (buffer.Count == opt.MaxResults)
                                    break; // we are done !
                            }
                        }
                    }
                    if (buffer.Count == maxResults)
                        break; // we are done !
                }
            } while (buffer.NextNode >= 0 && buffer.Count < maxResults);

            SpatialObjectBase[] results = buffer.GetArrayAndClearBuffer();
            if (opt.SortByDistance)
                LinearSearch.SortByDistance(opt, results);
            return results;
        }

        public SpatialObjectBase[] FindLinear(in SearchOptions opt)
        {
            (SpatialObjectBase[] objects, _) = GetObjectsAndRootSafe();
            return LinearSearch.FindNearby(in opt, objects, objects.Length);
        }
    }
}