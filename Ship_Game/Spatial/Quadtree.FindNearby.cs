using System;
using System.Threading;
using Microsoft.Xna.Framework;

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

        public GameplayObject[] FindNearby(GameObjectType type,
                                           Vector2 worldPos,
                                           float radius,
                                           int maxResults,
                                           GameplayObject toIgnore,
                                           Empire excludeLoyalty,
                                           Empire onlyLoyalty)
        {
            // we create a dummy object which covers our search radius
            var enclosingRectangle = new SpatialObj(worldPos, radius);
            enclosingRectangle.Obj = toIgnore; // This object will be excluded from the search
            enclosingRectangle.Loyalty = (byte) (onlyLoyalty?.Id ?? 0); // filter by loyalty?

            // find the deepest enclosing node
            QtreeNode root = Root;
            QtreeNode enclosing = FindEnclosingNode(root, ref enclosingRectangle);
            if (enclosing == null)
                return Empty<GameplayObject>.Array;

            // If enclosing object is the Root object and radius is huge,
            // switch to linear search because we need to traverse the ENTIRE universe anyway
            if (enclosing == root && radius > QuadToLinearSearchThreshold)
            {
                return FindLinear(type, worldPos, radius, maxResults,
                                  toIgnore, excludeLoyalty, onlyLoyalty);
            }

            FindResultBuffer buffer = GetThreadLocalTraversalBuffer(root);
            if (buffer.Items.Length < maxResults)
            {
                buffer.Items = new GameplayObject[maxResults];
            }

            // NOTE: to avoid a few branches, we used pre-calculated masks
            int excludeLoyaltyVal = (excludeLoyalty?.Id ?? 0);
            int excludeLoyaltyMask = (excludeLoyaltyVal == 0) ? 0xff : ~excludeLoyaltyVal;
            int onlyLoyaltyVal = (onlyLoyalty?.Id ?? 0); // filter by loyalty?
            int onlyLoyaltyMask = (onlyLoyaltyVal == 0) ? 0xff : onlyLoyaltyVal;
            int filterMask = type == GameObjectType.Any ? 0xff : (int)type;

            float cx = enclosingRectangle.CX;
            float cy = enclosingRectangle.CY;
            float r = enclosingRectangle.Radius;
            GameplayObject sourceObject = enclosingRectangle.Obj;
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
                        // check if inside radius, inlined for perf
                        float dx = cx - so.CX;
                        float dy = cy - so.CY;
                        float r2 = r + so.Radius;
                        if ((dx * dx + dy * dy) <= (r2 * r2))
                        {
                            buffer.Items[buffer.Count++] = so.Obj;
                            if (buffer.Count >= maxResults)
                                break; // we are done !
                        }
                    }

                }

                if (buffer.Count >= maxResults)
                    break; // we are done !

                if (node.NW != null)
                {
                    if (node.NW.Overlaps(enclosingRectangle))
                        buffer.NodeStack[++buffer.NextNode] = node.NW;

                    if (node.NE.Overlaps(enclosingRectangle))
                        buffer.NodeStack[++buffer.NextNode] = node.NE;

                    if (node.SE.Overlaps(enclosingRectangle))
                        buffer.NodeStack[++buffer.NextNode] = node.SE;

                    if (node.SW.Overlaps(enclosingRectangle))
                        buffer.NodeStack[++buffer.NextNode] = node.SW;
                }
            } while (buffer.NextNode >= 0);
            
            return buffer.GetArrayAndClearBuffer();
        }

        public GameplayObject[] FindLinear(GameObjectType type,
                                           Vector2 worldPos,
                                           float radius,
                                           int maxResults,
                                           GameplayObject toIgnore,
                                           Empire excludeLoyalty,
                                           Empire onlyLoyalty)
        {
            FindResultBuffer nearby = FindBuffer.Value;
            if (nearby.Items.Length < maxResults)
            {
                nearby.Items = new GameplayObject[maxResults];
            }
            
            float cx = worldPos.X;
            float cy = worldPos.Y;
            bool filterByLoyalty = (excludeLoyalty != null) || (onlyLoyalty != null);

            GameplayObject[] objects = Objects.GetInternalArrayItems();
            int count = Objects.Count;
            for (int i = 0; i < count; ++i)
            {
                GameplayObject obj = objects[i];
                if (obj == null || (toIgnore != null && obj == toIgnore)
                    || (type != GameObjectType.Any && obj.Type != type))
                    continue;

                if (filterByLoyalty)
                {
                    Empire loyalty = obj.GetLoyalty();
                    if ((excludeLoyalty != null && loyalty == excludeLoyalty)
                        || (onlyLoyalty != null && loyalty != onlyLoyalty))
                        continue;
                }

                // check if inside radius, inlined for perf
                float dx = cx - obj.Center.X;
                float dy = cy - obj.Center.Y;
                float r2 = radius + obj.Radius;
                if ((dx*dx + dy*dy) <= (r2*r2))
                {
                    nearby.Items[nearby.Count++] = obj;
                    if (nearby.Count >= maxResults)
                        break; // we are done !
                }
            }

            return nearby.GetArrayAndClearBuffer();
        }
    }
}