using System;
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

            public QtreeNode Pop()
            {
                QtreeNode node = NodeStack[NextNode];
                NodeStack[NextNode] = default; // don't leak refs
                --NextNode;
                return node;
            }

            public void ResetAndPush(QtreeNode pushFirst)
            {
                NextNode = 0;
                NodeStack[0] = pushFirst;
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

            // find the deepest enclosing node
            QtreeNode root = Root;
            QtreeNode enclosing = FindEnclosingNode(root, searchRect);
            if (enclosing == null)
                return Empty<GameplayObject>.Array;

            // If enclosing object is the Root object and radius is huge,
            // switch to linear search because we need to traverse the ENTIRE universe anyway
            if (enclosing == root && (searchRect.Width/2) > QuadToLinearSearchThreshold)
            {
                return FindLinear(opt);
            }

            FindResultBuffer buffer = GetThreadLocalTraversalBuffer(enclosing);
            if (buffer.Items.Length < opt.MaxResults)
            {
                buffer.Items = new GameplayObject[opt.MaxResults];
            }

            // NOTE: to avoid a few branches, we used pre-calculated masks
            int excludeLoyaltyVal = (opt.FilterExcludeByLoyalty?.Id ?? 0);
            int excludeLoyaltyMask = (excludeLoyaltyVal == 0) ? 0xff : ~excludeLoyaltyVal;
            int onlyLoyaltyVal = (opt.FilterIncludeOnlyByLoyalty?.Id ?? 0); // filter by loyalty?
            int onlyLoyaltyMask = (onlyLoyaltyVal == 0) ? 0xff : onlyLoyaltyVal;
            int filterMask = opt.FilterByType == GameObjectType.Any ? 0xff : (int)opt.FilterByType;

            GameplayObject sourceObject = opt.FilterExcludeObject;
            do
            {
                QtreeNode node = buffer.Pop();

                int count = node.Count;
                SpatialObj[] items = node.Items;
                for (int i = 0; i < count; ++i)
                {
                    ref SpatialObj so = ref items[i];

                    // FLAGS: either 0x00 (failed) or some bits 0100 (success)
                    if (so.Active != 0
                        && (so.Loyalty & excludeLoyaltyMask) != 0
                        && (so.Loyalty & onlyLoyaltyMask) != 0
                        && ((int)so.Type & filterMask) != 0
                        && (so.Obj != sourceObject))
                    {
                        if (so.AABB.Overlaps(searchRect))
                        {
                            buffer.Items[buffer.Count++] = so.Obj;
                            if (buffer.Count >= opt.MaxResults)
                                break; // we are done !
                        }
                    }
                }

                if (buffer.Count >= opt.MaxResults)
                    break; // we are done !

                if (node.NW != null)
                {
                    if (node.NW.AABB.Overlaps(searchRect))
                        buffer.NodeStack[++buffer.NextNode] = node.NW;

                    if (node.NE.AABB.Overlaps(searchRect))
                        buffer.NodeStack[++buffer.NextNode] = node.NE;

                    if (node.SE.AABB.Overlaps(searchRect))
                        buffer.NodeStack[++buffer.NextNode] = node.SE;

                    if (node.SW.AABB.Overlaps(searchRect))
                        buffer.NodeStack[++buffer.NextNode] = node.SW;
                }
            } while (buffer.NextNode >= 0);
            
            return buffer.GetArrayAndClearBuffer();
        }

        public GameplayObject[] FindLinear(in SearchOptions opt)
        {
            FindResultBuffer nearby = FindBuffer.Value;
            if (nearby.Items.Length < opt.MaxResults)
            {
                nearby.Items = new GameplayObject[opt.MaxResults];
            }
            
            AABoundingBox2D searchRect = opt.SearchRect;
            bool filterByLoyalty = (opt.FilterExcludeByLoyalty != null)
                                || (opt.FilterIncludeOnlyByLoyalty != null);

            GameplayObject[] objects = Objects.GetInternalArrayItems();
            int count = Objects.Count;

            for (int i = 0; i < count; ++i)
            {
                GameplayObject obj = objects[i];
                if (obj == null
                    || (opt.FilterExcludeObject != null && obj == opt.FilterExcludeObject)
                    || (opt.FilterByType != GameObjectType.Any && obj.Type != opt.FilterByType))
                    continue;

                if (filterByLoyalty)
                {
                    Empire loyalty = obj.GetLoyalty();
                    if ((opt.FilterExcludeByLoyalty != null && loyalty == opt.FilterExcludeByLoyalty) ||
                        (opt.FilterIncludeOnlyByLoyalty != null && loyalty != opt.FilterIncludeOnlyByLoyalty))
                        continue;
                }

                var objectRect = new AABoundingBox2D(obj);
                if (objectRect.Overlaps(searchRect))
                {
                    nearby.Items[nearby.Count++] = obj;
                    if (nearby.Count >= opt.MaxResults)
                        break; // we are done !
                }
            }

            return nearby.GetArrayAndClearBuffer();
        }
    }
}