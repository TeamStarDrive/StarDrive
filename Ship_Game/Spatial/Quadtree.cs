using System;
using System.Diagnostics.CodeAnalysis;

namespace Ship_Game
{
    ///////////////////////////////////////////////////////////////////////////////////////////

    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    public sealed partial class Quadtree : IDisposable
    {
        public static readonly SpatialObj[] NoObjects = new SpatialObj[0];

        public readonly int   Levels;
        public readonly float FullSize;

        /// <summary>
        /// New: Instead of the complex update-reinsert routine,
        ///      rebuild the entire tree from scratch during every update
        ///
        ///      This will grant thread safety for the entire tree
        /// </summary>
        public bool RebuildFullTree = true;

        /// <summary>
        /// How many objects to store per cell before subdividing
        /// </summary>
        public const int CellThreshold = 64;

        QtreeNode Root;
        int FrameId;
        FixedSimTime SimulationStep;

        ///////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////

        // Create a quadtree to fit the universe
        public Quadtree(float universeSize, float smallestCell = 512f)
        {
            Levels = 1;
            FullSize = smallestCell;
            while (FullSize < universeSize)
            {
                ++Levels;
                FullSize *= 2;
            }
            Reset();
        }
        ~Quadtree() { Dispose(); }

        public void Dispose()
        {
            Root = null;
            GC.SuppressFinalize(this);
        }

        public void Reset()
        {
            // universe is centered at [0,0], so Root node goes from [-half, +half)
            float half = FullSize / 2;
            Root = new QtreeNode(-half, -half, +half, +half);
        }

        static void SplitNode(QtreeNode node, int level)
        {
            float midX = (node.X + node.LastX) / 2;
            float midY = (node.Y + node.LastY) / 2;

            node.NW = new QtreeNode(node.X, node.Y, midX,       midY);
            node.NE = new QtreeNode(midX,   node.Y, node.LastX, midY);
            node.SE = new QtreeNode(midX,   midY,   node.LastX, node.LastY);
            node.SW = new QtreeNode(node.X, midY,   midX,       node.LastY);

            int count = node.Count;
            SpatialObj[] arr = node.Items;
            node.Items = NoObjects;
            node.Count = 0;

            // reinsert all items:
            for (int i = 0; i < count; ++i)
                InsertAt(node, level, ref arr[i]);
        }

        static QtreeNode PickSubQuadrant(QtreeNode node, ref SpatialObj obj)
        {
            float midX = (node.X + node.LastX) / 2;
            float midY = (node.Y + node.LastY) / 2;

            obj.OverlapsQuads = 0;
            if (obj.X < midX && obj.LastX < midX) // left
            {
                if (obj.Y <  midY && obj.LastY < midY) return node.NW; // top left
                if (obj.Y >= midY)                     return node.SW; // bot left
            }
            else if (obj.X >= midX) // right
            {
                if (obj.Y <  midY && obj.LastY < midY) return node.NE; // top right
                if (obj.Y >= midY)                     return node.SE; // bot right
            }
            obj.OverlapsQuads = 1;
            return null; // obj does not perfectly fit inside a quadrant
        }

        static void InsertAt(QtreeNode node, int level, ref SpatialObj obj)
        {
            for (;;)
            {
                if (level <= 1) // no more subdivisions possible
                {
                    node.Add(ref obj);
                    return;
                }

                if (node.NW != null)
                {
                    QtreeNode quad = PickSubQuadrant(node, ref obj);
                    if (quad != null)
                    {
                        node = quad; // go deeper!
                        --level;
                        continue;
                    }
                }

                // item belongs to this node
                node.Add(ref obj);

                // actually, are we maybe over Threshold and should Divide ?
                if (node.NW == null && node.Count >= CellThreshold)
                    SplitNode(node, level);
                return;
            }
        }

        public void Insert(GameplayObject go)
        {
            var obj = new SpatialObj(go);
            InsertAt(Root, Levels, ref obj);
        }

        static bool RemoveAt(QtreeNode node, GameplayObject go)
        {
            for (int i = 0; i < node.Count; ++i)
            {
                if (node.Items[i].Obj != go) continue;
                node.RemoveAtSwapLast(ref i);
                return true;
            }
            return node.NW != null
                && (RemoveAt(node.NW, go) || RemoveAt(node.NE, go)
                ||  RemoveAt(node.SE, go) || RemoveAt(node.SW, go));
        }

        public void Remove(GameplayObject go) => RemoveAt(Root, go);

        static void FastRemoval(GameplayObject obj, QtreeNode node, ref int index)
        {
            UniverseScreen.SpaceManager.FastNonTreeRemoval(obj);
            node.RemoveAtSwapLast(ref index);
        }

        void UpdateNode(QtreeNode node, int level, byte frameId)
        {
            if (node.Count > 0)
            {
                float nx = node.X, ny = node.Y; // L1 cache warm node bounds
                float nlastX = node.LastX, nlastY = node.LastY;

                for (int i = 0; i < node.Count; ++i)
                {
                    ref SpatialObj obj = ref node.Items[i]; // .Items may be modified by InsertAt and RemoveAtSwapLast
                    if (obj.LastUpdate == frameId) continue; // we reinserted this node so it doesn't require updating
                    if (obj.Loyalty == 0)          continue; // loyalty 0: static world object, so don't bother updating

                    GameplayObject go = obj.Obj;
                    if (go == null) // FIX: this is a threading issue, this node already removed
                        continue;

                    if (go.Active == false)
                    {
                        FastRemoval(go, node, ref i);
                        continue;
                    }

                    obj.UpdateBounds();
                    obj.LastUpdate = frameId;

                    if (obj.X < nx || obj.Y < ny || obj.LastX > nlastX || obj.LastY > nlastY) // out of Node bounds??
                    {
                        SpatialObj reinsert = obj;
                        node.RemoveAtSwapLast(ref i);
                        InsertAt(Root, Levels, ref reinsert); // warning: this call can modify our node.Items
                    }
                    // we previously overlapped the boundary, so insertion was at parent node;
                    // ... so now check if we're completely inside a subquadrant and reinsert into it
                    else if (obj.OverlapsQuads != 0)
                    {
                        QtreeNode quad = PickSubQuadrant(node, ref obj);
                        if (quad != null)
                        {
                            SpatialObj reinsert = obj;
                            node.RemoveAtSwapLast(ref i);
                            InsertAt(quad, level-1, ref reinsert); // warning: this call can modify our node.Items
                        }
                    }
                }
            }
            if (node.NW != null)
            {
                int sublevel = level - 1;
                UpdateNode(node.NW, sublevel, frameId);
                UpdateNode(node.NE, sublevel, frameId);
                UpdateNode(node.SE, sublevel, frameId);
                UpdateNode(node.SW, sublevel, frameId);
            }
        }

        static int RemoveEmptyChildNodes(QtreeNode node)
        {
            if (node.NW == null)
                return node.Count;

            int subItems = 0;
            subItems += RemoveEmptyChildNodes(node.NW);
            subItems += RemoveEmptyChildNodes(node.NE);
            subItems += RemoveEmptyChildNodes(node.SE);
            subItems += RemoveEmptyChildNodes(node.SW);
            if (subItems == 0) // discard these empty quads:
            {
                node.NW = node.NE = node.SE = node.SW = null;
            }
            return node.Count + subItems;
        }

        public void UpdateAll(FixedSimTime timeStep)
        {
            // we don't really care about int32 precision here... 
            // actually a single bit flip would work fine as well
            byte frameId = (byte)++FrameId;
            SimulationStep = timeStep;
            UpdateNode(Root, Levels, frameId);
            RemoveEmptyChildNodes(Root);
        }

        // finds the node that fully encloses this spatial object
        QtreeNode FindEnclosingNode(ref SpatialObj obj)
        {
            int level = Levels;
            QtreeNode node = Root;
            for (;;)
            {
                if (level <= 1) // no more subdivisions possible
                    break;
                QtreeNode quad = PickSubQuadrant(node, ref obj);
                if (quad == null)
                    break;
                node = quad; // go deeper!
                --level;
            }
            return node;
        }

        // Traverse the entire tree to get the # of items
        // NOTE: This is SLOW
        public int CountItemsSlow() => CountItemsRecursive(Root);

        static int CountItemsRecursive(QtreeNode node)
        {
            int count = node.Count;
            if (node.NW != null)
            {
                count += CountItemsRecursive(node.NW);
                count += CountItemsRecursive(node.NE);
                count += CountItemsRecursive(node.SE);
                count += CountItemsRecursive(node.SW);
            }
            return count;
        }
    }
}
