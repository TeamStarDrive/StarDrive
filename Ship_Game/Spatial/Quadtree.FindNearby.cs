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

        public bool WasLinearSearch;

        /// <summary>
        /// Finds nearby GameplayObjects using multiple filters
        /// </summary>
        /// <param name="worldPos">Origin of the search</param>
        /// <param name="radius">Radius of the search area</param>
        /// <param name="filter">Game object types to filter by, eg Ships or Projectiles</param>
        /// <param name="toIgnore">Single game object to ignore (usually our own ship), null (default): no ignore</param>
        /// <param name="loyaltyFilter">Filter results by loyalty, usually friendly, null (default): no filtering</param>
        /// <returns></returns>
        public GameplayObject[] FindNearby(Vector2 worldPos, float radius,
            GameObjectType filter = GameObjectType.Any,
            GameplayObject toIgnore = null, // null: accept all results
            Empire loyaltyFilter = null)
        {
            // we create a dummy object which covers our search radius
            var enclosingRectangle = new SpatialObj(worldPos, radius);
            enclosingRectangle.Obj = toIgnore; // This object will be excluded from the search
            enclosingRectangle.Loyalty = (byte) (loyaltyFilter?.Id ?? 0); // filter by loyalty?

            WasLinearSearch = false;

            // find the deepest enclosing node
            QtreeNode root = Root;
            QtreeNode enclosing = FindEnclosingNode(root, ref enclosingRectangle);
            if (enclosing == null)
                return Empty<GameplayObject>.Array;

            // If enclosing object is the Root object and radius is huge,
            // switch to linear search because we need to traverse the ENTIRE universe anyway
            if (enclosing == root && radius > QuadToLinearSearchThreshold)
            {
                WasLinearSearch = true;
                return FindLinear(worldPos, radius, filter, toIgnore, loyaltyFilter);
            }

            FindResultBuffer buffer = GetThreadLocalTraversalBuffer(root);

            // NOTE: to avoid a few branches, we used pre-calculated bitmasks
            int loyalty = enclosingRectangle.Loyalty;
            int loyaltyMask = (loyalty == 0) ? 0xff : loyalty;
            int filterMask = (int) filter;

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

                    // either 0x00 (failed) or some bits 0100 (success)
                    int typeFlags = ((int) so.Type & filterMask);

                    // either 0x00 (failed) or some bits 0011 (success)
                    int loyaltyFlags = (so.Loyalty & loyaltyMask);

                    if (typeFlags == 0 || loyaltyFlags == 0 ||
                        so.PendingRemove != 0 || so.Obj == sourceObject)
                        continue;

                    // check if inside radius, inlined for perf
                    float dx = cx - so.CX;
                    float dy = cy - so.CY;
                    float r2 = r + so.Radius;
                    if ((dx * dx + dy * dy) <= (r2 * r2))
                    {
                        // inline array expand
                        if (buffer.Count == buffer.Items.Length)
                        {
                            var arr = new GameplayObject[buffer.Items.Length * 2];
                            Array.Copy(buffer.Items, arr, buffer.Count);
                            buffer.Items = arr;
                        }
                        buffer.Items[buffer.Count++] = so.Obj;
                    }
                }

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

        // Performs a linear search instead of using the Quadtree
        // This is for special cases
        public GameplayObject[] FindLinear(Vector2 worldPos, float radius,
            GameObjectType filter = GameObjectType.Any,
            GameplayObject toIgnore = null, // null: accept all results
            Empire loyaltyFilter = null)
        {
            float cx = worldPos.X;
            float cy = worldPos.Y;
            float r  = radius;
            FindResultBuffer nearby = FindBuffer.Value;

            GameplayObject[] objects = Objects.GetInternalArrayItems();
            int count = Objects.Count;
            for (int i = 0; i < count; ++i)
            {
                GameplayObject obj = objects[i];
                if (obj == null || (toIgnore != null && obj == toIgnore)
                    || (obj.Type & filter) == 0
                    || (loyaltyFilter != null && obj.GetLoyalty() != loyaltyFilter))
                    continue;

                // check if inside radius, inlined for perf
                float dx = cx - obj.Center.X;
                float dy = cy - obj.Center.Y;
                float r2 = r + obj.Radius;
                if ((dx*dx + dy*dy) <= (r2*r2))
                {
                    // inline array expand
                    if (nearby.Count == nearby.Items.Length)
                    {
                        var arr = new GameplayObject[nearby.Items.Length * 2];
                        Array.Copy(nearby.Items, arr, nearby.Count);
                        nearby.Items = arr;
                    }
                    nearby.Items[nearby.Count++] = obj;
                }
            }

            return nearby.GetArrayAndClearBuffer();
        }
    }
}