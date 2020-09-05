using System;
using System.Diagnostics.CodeAnalysis;

namespace Ship_Game
{
    ///////////////////////////////////////////////////////////////////////////////////////////

    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    public sealed partial class Quadtree
    {
        public static readonly SpatialObj[] NoObjects = new SpatialObj[0];

        public readonly int   Levels;
        public readonly float FullSize;

        /// <summary>
        /// How many objects to store per cell before subdividing
        /// </summary>
        public const int CellThreshold = 64;

        QtreeNode Root;
        FixedSimTime SimulationStep;

        readonly Array<GameplayObject> Pending = new Array<GameplayObject>();
        readonly Array<GameplayObject> Objects = new Array<GameplayObject>();

        QtreeRecycleBuffer FrontBuffer = new QtreeRecycleBuffer();
        QtreeRecycleBuffer BackBuffer  = new QtreeRecycleBuffer();

        /// <summary>
        /// Number of pending and active objects in the Quadtree
        /// </summary>
        public int Count => Pending.Count + Objects.Count;

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

        public void Reset()
        {
            // universe is centered at [0,0], so Root node goes from [-half, +half)
            float half = FullSize / 2;
            Root = new QtreeNode(-half, -half, +half, +half);
            lock (Pending)
            {
                Pending.Clear();
                Objects.Clear();
            }
        }

        void SplitNode(QtreeNode node, int level)
        {
            float midX = (node.X + node.LastX) / 2;
            float midY = (node.Y + node.LastY) / 2;

            node.NW = FrontBuffer.Create(node.X, node.Y, midX,       midY);
            node.NE = FrontBuffer.Create(midX,   node.Y, node.LastX, midY);
            node.SE = FrontBuffer.Create(midX,   midY,   node.LastX, node.LastY);
            node.SW = FrontBuffer.Create(node.X, midY,   midX,       node.LastY);

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
            return null; // obj does not perfectly fit inside a quadrant
        }

        void InsertAt(QtreeNode node, int level, ref SpatialObj obj)
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

        /// <summary>
        /// Insert the item as Pending.
        /// This means it will be visible in the Quadtree after next update
        /// </summary>
        public void InsertPending(GameplayObject go)
        {
            // this can be called from UI Thread, so we'll insert it later during Update()
            lock (Pending)
            {
                Pending.Add(go);
                go.SpatialIndex = -2;
            }
        }

        /// <summary>
        /// Object will be marked as PendingRemove and will be removed next frame
        /// </summary>
        public void Remove(GameplayObject go)
        {
            if (go.SpatialPending)
            {
                lock (Pending)
                {
                    Pending.RemoveRef(go);
                    go.SpatialIndex = -1;
                }
            }
            else if (go.InSpatial)
            {
                RemoveAt(Root, go);
            }
        }

        void RemoveAt(QtreeNode root, GameplayObject go)
        {
            FindResultBuffer buffer = FindBuffer.Value;
            buffer.NextNode = 0;
            buffer.NodeStack[0] = root;
            do
            {
                // inlined POP
                QtreeNode node = buffer.NodeStack[buffer.NextNode];
                buffer.NodeStack[buffer.NextNode] = default; // don't leak refs
                --buffer.NextNode;

                int count = node.Count;
                SpatialObj[] items = node.Items;
                for (int i = 0; i < count; ++i)
                {
                    ref SpatialObj so = ref items[i];
                    if (so.Obj == go)
                    {
                        MarkForRemoval(go, ref so);
                        return;
                    }
                }
                if (node.NW != null)
                {
                    buffer.NodeStack[++buffer.NextNode] = node.NW;
                    buffer.NodeStack[++buffer.NextNode] = node.NE;
                    buffer.NodeStack[++buffer.NextNode] = node.SE;
                    buffer.NodeStack[++buffer.NextNode] = node.SW;
                }
            } while (buffer.NextNode >= 0);
        }

        void MarkForRemoval(GameplayObject go, ref SpatialObj obj)
        {
            Objects[go.SpatialIndex] = null;
            go.SpatialIndex = -1;
            obj.PendingRemove = 1;
            obj.Obj = null; // don't leak refs
        }

        void InsertPending()
        {
            lock (Pending)
            {
                for (int i = 0; i < Pending.Count; ++i)
                {
                    GameplayObject obj = Pending[i];
                    obj.SpatialIndex = Objects.Count;
                    Objects.Add(obj);
                }
                Pending.Clear();
            }
        }

        // remove inactive objects which are designated by null
        void RemoveEmptySpots()
        {
            GameplayObject[] objects = Objects.GetInternalArrayItems();
            for (int i = 0; i < Objects.Count; ++i)
            {
                GameplayObject obj = objects[i];
                if (obj != null)
                    obj.SpatialIndex = i;
                else
                    Objects.RemoveAtSwapLast(i--);
            }
        }

        QtreeNode CreateFullTree()
        {
            // universe is centered at [0,0], so Root node goes from [-half, +half)
            float half = FullSize / 2;
            var newRoot = new QtreeNode(-half, -half, +half, +half);;
            for (int i = 0; i < Objects.Count; ++i)
            {
                GameplayObject go = Objects[i];
                var obj = new SpatialObj(go);
                InsertAt(newRoot, Levels, ref obj);
            }
            return newRoot;
        }

        public void UpdateAll(FixedSimTime timeStep)
        {
            SimulationStep = timeStep;
            
            RemoveEmptySpots();
            InsertPending();

            // prepare our node buffer for allocation
            FrontBuffer.MarkAllNodesInactive();

            // atomic exchange of old root and new root
            Root = CreateFullTree();
            SwapRecycleLists();
        }

        void SwapRecycleLists()
        {
            // Swap recycle lists
            // 
            // We move last frame's nodes to front and start overwriting them
            QtreeRecycleBuffer newBackBuffer = FrontBuffer;
            FrontBuffer = BackBuffer; // move backbuffer to front
            BackBuffer = newBackBuffer;

        }

        // finds the node that fully encloses this spatial object
        QtreeNode FindEnclosingNode(QtreeNode node, ref SpatialObj obj)
        {
            int level = Levels;
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
    }
}
