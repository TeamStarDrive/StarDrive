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

            public GameplayObject[] GetArrayAndClearBuffer()
            {
                int count = Count;
                if (count == 0)
                    return Empty<GameplayObject>.Array;

                Count = 0;
                var arr = new GameplayObject[count];
                Memory.HybridCopy(arr, 0, Items, count);
                //Array.Clear(Items, 0, count);
                return arr;
            }
        }

        static void FindNearbyAtNode(FindResultBuffer nearby, ref SpatialObj searchAreaRect, GameObjectType filter)
        {
            // NOTE: to avoid a few branches, we used pre-calculated bitmasks
            int loyalty = searchAreaRect.Loyalty;
            int loyaltyMask = (loyalty == 0) ? 0xff : loyalty;
            int filterMask = (int) filter;

            float cx = searchAreaRect.CX;
            float cy = searchAreaRect.CY;
            float r = searchAreaRect.Radius;
            GameplayObject sourceObject = searchAreaRect.Obj;
            do
            {
                // inlined POP
                QtreeNode node = nearby.NodeStack[nearby.NextNode];
                nearby.NodeStack[nearby.NextNode] = default; // don't leak refs
                --nearby.NextNode;

                int count = node.Count;
                SpatialObj[] items = node.Items;
                for (int i = 0; i < count; ++i)
                {
                    ref SpatialObj so = ref items[i];

                    // either 0x00 (failed) or some bits 0100 (success)
                    int typeFlags = ((int) so.Type & filterMask);

                    // either 0x00 (failed) or some bits 0011 (success)
                    int loyaltyFlags = (so.Loyalty & loyaltyMask);

                    if (typeFlags == 0 || loyaltyFlags == 0 || so.Obj == sourceObject)
                        continue;

                    // check if inside radius, inlined for perf
                    float dx = cx - so.CX;
                    float dy = cy - so.CY;
                    float r2 = r + so.Radius;
                    if ((dx * dx + dy * dy) <= (r2 * r2))
                    {
                        //inline array expand
                        if (nearby.Count == nearby.Items.Length)
                        {
                            var arr = new GameplayObject[nearby.Items.Length * 2];
                            Array.Copy(nearby.Items, arr, nearby.Count);
                            nearby.Items = arr;
                        }

                        nearby.Items[nearby.Count++] = so.Obj;
                    }
                }

                if (node.NW != null)
                {
                    nearby.NodeStack[++nearby.NextNode] = node.NW;
                    nearby.NodeStack[++nearby.NextNode] = node.NE;
                    nearby.NodeStack[++nearby.NextNode] = node.SE;
                    nearby.NodeStack[++nearby.NextNode] = node.SW;
                }
            } while (nearby.NextNode >= 0);
        }
        
        // NOTE: This is really fast
        readonly ThreadLocal<FindResultBuffer> FindBuffer
           = new ThreadLocal<FindResultBuffer>(() => new FindResultBuffer());

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

            FindResultBuffer nearby = FindBuffer.Value;

            // find the deepest enclosing node
            QtreeNode node = FindEnclosingNode(ref enclosingRectangle);
            if (node != null)
            {
                nearby.NextNode = 0;
                nearby.NodeStack[0] = node;
                FindNearbyAtNode(nearby, ref enclosingRectangle, filter);
            }

            return nearby.GetArrayAndClearBuffer();
        }
    }
}