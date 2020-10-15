using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Xna.Framework;
using Ship_Game.Spatial;

namespace Ship_Game
{
    public sealed partial class Quadtree
    {
        /// <summary>
        /// Optimized temporary search buffer for FindNearby
        /// </summary>
        class FindResultBuffer
        {
            public int Count = 0;
            public GameplayObject[] Items = new GameplayObject[128];
            public int NextNode = 0; // next node to pop
            public QtreeNode[] NodeStack = new QtreeNode[512];
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public QtreeNode Pop()
            {
                QtreeNode node = NodeStack[NextNode];
                NodeStack[NextNode] = default; // don't leak refs
                --NextNode;
                return node;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void PushBack(QtreeNode node)
            {
                NodeStack[++NextNode] = node;
            }

            public GameplayObject[] GetArrayAndClearBuffer()
            {
                int count = Count;
                if (count == 0)
                    return Empty<GameplayObject>.Array;

                Count = 0;
                var arr = new GameplayObject[count];
                Memory.HybridCopy(arr, 0, Items, count);
                Array.Clear(Items, 0, count);
                return arr;
            }
        }

        // NOTE: This is really fast
        readonly ThreadLocal<FindResultBuffer> FindBuffer
           = new ThreadLocal<FindResultBuffer>(() => new FindResultBuffer());

        FindResultBuffer GetThreadLocalTraversalBuffer(QtreeNode root)
        {
            FindResultBuffer buffer = FindBuffer.Value;
            buffer.NextNode = 0;
            buffer.NodeStack[0] = root;
            return buffer;
        }

        public GameplayObject[] FindNearby(in SearchOptions opt)
        {
            AABoundingBox2D searchRect = opt.SearchRect;
            int maxResults = opt.MaxResults > 0 ? opt.MaxResults : 1;
            
            // NOTE: to avoid a few branches, we used pre-calculated masks
            int excludeLoyaltyVal = (opt.FilterExcludeByLoyalty?.Id ?? 0);
            int excludeLoyaltyMask = (excludeLoyaltyVal == 0) ? 0xff : ~excludeLoyaltyVal;
            int onlyLoyaltyVal = (opt.FilterIncludeOnlyByLoyalty?.Id ?? 0); // filter by loyalty?
            int onlyLoyaltyMask = (onlyLoyaltyVal == 0) ? 0xff : onlyLoyaltyVal;
            int filterMask = opt.FilterByType == GameObjectType.Any ? 0xff : (int)opt.FilterByType;
            float searchFX = opt.FilterOrigin.X;
            float searchFY = opt.FilterOrigin.Y;
            float searchFR = opt.FilterRadius;
            bool useSearchRadius = searchFR > 0f;
            GameplayObject sourceObject = opt.FilterExcludeObject;

            QtreeNode root = Root;
            SpatialObj[] spatialObjects = SpatialObjects;
            FindResultBuffer buffer = GetThreadLocalTraversalBuffer(root);
            if (buffer.Items.Length < maxResults)
                buffer.Items = new GameplayObject[maxResults];

            do
            {
                QtreeNode current = buffer.Pop();
                if (current.NW != null) // isBranch
                {
                    var over = new OverlapsRect(current.AABB, searchRect);
                    if (over.SW != 0) buffer.PushBack(current.SW);
                    if (over.SE != 0) buffer.PushBack(current.SE);
                    if (over.NE != 0) buffer.PushBack(current.NE);
                    if (over.NW != 0) buffer.PushBack(current.NW);
                }
                else // isLeaf
                {
                    int count = current.Count;
                    int[] items = current.Items;
                    for (int i = 0; i < count; ++i)
                    {
                        int objectId = items[i];
                        ref SpatialObj so = ref spatialObjects[objectId];

                        // FLAGS: either 0x00 (failed) or some bits 0100 (success)
                        if (so.Active != 0
                            && (so.Loyalty & excludeLoyaltyMask) != 0
                            && (so.Loyalty & onlyLoyaltyMask) != 0
                            && ((int)so.Type & filterMask) != 0
                            && (so.Obj != sourceObject))
                        {
                            if (!so.AABB.Overlaps(searchRect))
                                continue;

                            if (useSearchRadius)
                            {
                                float dx = searchFX - so.CX;
                                float dy = searchFY - so.CY;
                                float rr = searchFR + so.Radius;
                                if ((dx*dx + dy*dy) > (rr*rr))
                                    continue; // not in squared radius
                            }

                            buffer.Items[buffer.Count++] = so.Obj;
                            if (buffer.Count == opt.MaxResults)
                                break; // we are done !
                        }
                    }
                    if (buffer.Count == maxResults)
                        break; // we are done !
                }
            } while (buffer.NextNode >= 0 && buffer.Count < maxResults);

            return buffer.GetArrayAndClearBuffer();
        }

        public GameplayObject[] FindLinear(in SearchOptions opt)
        {
            return NativeSpatial.FindLinear(opt, Objects.GetInternalArrayItems(), Objects.Count);
        }
    }
}