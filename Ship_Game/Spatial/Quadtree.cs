using System;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    public struct SpatialObj
    {
        public GameplayObject Obj;
        public float X, Y, LastX, LastY;
        public int Loyalty; // if loyalty == 0, then this is a STATIC world object !!!
        public int LastCollided;
        public int TypeFlags;

        public void UpdateBounds()
        {
            
        }
    }

    public struct SpatialCollision // two spatial objects that collided
    {
        public GameplayObject Obj1, Obj2;
    }

    public sealed class Quadtree : IDisposable
    {
        private static readonly SpatialObj[] NoObjects = new SpatialObj[0];

        private readonly int   Levels;
        private readonly float SmallestCell;
        private readonly float FullSize;

        public const int CellThreshold = 4;
        private Node Root;
        private int FrameId;

        private class Node
        {
            public readonly float X, Y, LastX, LastY;
            public readonly Node Parent;
            public Node NW, NE, SE, SW;
            public int Count;
            public SpatialObj[] Items;
            public Node(Node parent, float x, float y, float lastX, float lastY)
            {
                X = x; Y = y;
                LastX = lastX; LastY = lastY;
                Parent = parent;
                NW = null; NE = null; SE = null; SW = null;
                Count = 0;
                Items = NoObjects;
            }

            public void Add(ref SpatialObj obj)
            {
                if (Items.Length == 0)
                    Items = new SpatialObj[CellThreshold];
                Items[Count++] = obj;
            }

            public void RemoveAtSwapLast(int index)
            {
                ref SpatialObj last = ref Items[--Count];
                Items[index] = last;
                last.Obj = null;
                if (Count == 0) Items = NoObjects;
            }
        }

        // Create a quadtree to fit the universe
        public Quadtree(float universeSize, float smallestCell = 512f)
        {
            Levels       = 1;
            SmallestCell = smallestCell;
            FullSize     = smallestCell;
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
            Root = new Node(null, -half, -half, +half, +half);
        }

        private static bool Overlaps(Node n, ref SpatialObj b)
        {
            return n.X <= b.LastX && n.LastX > b.X
                && n.Y <= b.LastY && n.LastY > b.Y;
        }

        private static bool Overlaps(ref SpatialObj a, ref SpatialObj b)
        {
            return a.X <= b.LastX && a.LastX > b.X
                && a.Y <= b.LastY && a.LastY > b.Y;
        }

        // squared distance; if negative, we have a collision
        private static float DistanceTo(ref SpatialObj a, ref SpatialObj b)
        {
            float ra  = (a.LastX - a.X) / 2;
            float rb  = (b.LastX - b.X) / 2;
            float acx = a.X + ra, acy = a.Y + ra;
            float bcx = b.X + rb, bcy = b.Y + rb;
            float dx  = acx - bcx;
            float dy  = acy - bcy;
            return (dx*dx + dy*dy) - (ra*ra + rb*rb);
        }

        private static bool HitTest(ref SpatialObj a, ref SpatialObj b)
        {
            float ra  = (a.LastX - a.X) / 2;
            float rb  = (b.LastX - b.X) / 2;
            float acx = a.X + ra, acy = a.Y + ra;
            float bcx = b.X + rb, bcy = b.Y + rb;
            float dx  = acx - bcx;
            float dy  = acy - bcy;
            return (dx*dx + dy*dy) < (ra*ra + rb*rb);
        }

        private static void SplitNode(Node node, int level)
        {
            float midX = (node.X + node.LastX) / 2;
            float midY = (node.Y + node.LastY) / 2;

            node.NW = new Node(node, node.X, node.Y, midX,       midY);
            node.NE = new Node(node, midX,   node.Y, node.LastX, midY);
            node.SE = new Node(node, midX,   midY,   node.LastX, node.LastY);
            node.SW = new Node(node, node.X, midY,   midX,       node.LastY);

            int count = node.Count;
            SpatialObj[] arr = node.Items;
            node.Items = NoObjects;

            // reinsert all items:
            for (int i = 0; i < count; ++i)
                InsertAt(node, level, ref arr[i]);
        }

        private static Node PickSubQuadrant(Node node, ref SpatialObj obj)
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

        private static void InsertAt(Node node, int level, ref SpatialObj obj)
        {
            for (;;)
            {
                if (level <= 1) // no more subdivisions possible
                {
                    node.Add(ref obj);
                    return;
                }

                Node quad = PickSubQuadrant(node, ref obj);
                if (quad != null)
                {
                    node = quad; // go deeper!
                    --level;
                    continue;
                }

                // item belongs to this node
                node.Add(ref obj);

                // actually, are we maybe over Threshold and should Divide ?
                if (node.NW != null && node.Count >= CellThreshold)
                    SplitNode(node, level);
                return;
            }
        }

        public void Insert(GameplayObject go)
        {
            var obj = new SpatialObj
            {
                Obj   = go,
                X     = go.Center.X - go.Radius,
                Y     = go.Center.Y - go.Radius,
                LastX = go.Center.X + go.Radius,
                LastY = go.Center.Y + go.Radius,
                Loyalty = 0,
            };
            InsertAt(Root, Levels, ref obj);
        }

        private static bool RemoveAt(Node node, GameplayObject go)
        {
            for (int i = 0; i < node.Count; ++i)
            {
                if (node.Items[i].Obj != go) continue;
                node.RemoveAtSwapLast(i);
                return true;
            }
            return node.NW != null
                && RemoveAt(node.NW, go) || RemoveAt(node.NE, go)
                || RemoveAt(node.SE, go) || RemoveAt(node.SW, go);
        }

        public void Remove(GameplayObject go) => RemoveAt(Root, go);

        private void UpdateNode(Node node)
        {
            if (node.Count > 0)
            {
                float nx = node.X, ny = node.Y; // L1 cache warm node bounds
                float nlastX = node.LastX, nlastY = node.LastY;

                SpatialObj[] items = node.Items;
                for (int i = 0; i < node.Count; ++i)
                {
                    ref SpatialObj obj = ref items[i];
                    if (obj.Loyalty == 0)
                        continue; // seems to be a static world object, so don't bother updating

                    GameplayObject go = obj.Obj;
                    if (go.Active == false)
                    {
                        node.RemoveAtSwapLast(i--);
                        continue;
                    }

                    // Update SpatialObj bounding box
                    obj.X     = go.Center.X - go.Radius;
                    obj.Y     = go.Center.Y - go.Radius;
                    obj.LastX = go.Center.X + go.Radius;
                    obj.LastY = go.Center.Y + go.Radius;

                    if (obj.X < nx || obj.Y < ny || // out of Node bounds??
                        obj.LastX > nlastX || obj.LastY > nlastY)
                    {
                        SpatialObj reinsert = obj;
                        node.RemoveAtSwapLast(i--);
                        InsertAt(Root, Levels, ref reinsert);
                        continue;
                    }
                }
            }
            if (node.NW != null)
            {
                UpdateNode(node.NW);
                UpdateNode(node.NE);
                UpdateNode(node.SE);
                UpdateNode(node.SW);
            }
        }

        private static int RemoveEmptyChildNodes(Node node)
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

        public void UpdateAll()
        {
            ++FrameId;
            UpdateNode(Root);
            RemoveEmptyChildNodes(Root);
        }


        // finds the node that fully encloses this spatial object
        private Node FindEnclosingNode(ref SpatialObj obj)
        {
            int level = Levels;
            Node node = Root;
            for (;;)
            {
                if (level <= 1) // no more subdivisions possible
                    break;
                Node quad = PickSubQuadrant(node, ref obj);
                if (quad == null)
                    break;
                node = quad; // go deeper!
                --level;
            }
            return node;
        }

        private static bool CollideAtNode(Node node, int frameId, ref SpatialObj obj, out GameplayObject collided)
        {
            for (;;)
            {
                int count = node.Count;
                SpatialObj[] arr = node.Items;
                for (int i = 0; i < count; ++i)
                {
                    ref SpatialObj item = ref arr[i];
                    if (frameId      != item.LastCollided && // already collided this frame
                        item.Loyalty != obj.Loyalty       && // friendlies don't collide
                        item.Obj     != obj.Obj           && // ignore self
                        HitTest(ref item, ref obj)) // actual radial distance check
                    {
                        collided = item.Obj;
                        item.LastCollided = frameId;
                        obj.LastCollided  = frameId;
                        return true;
                    }
                }
                if (node.NW == null)
                {
                    collided = null;
                    return false;
                }
                if (CollideAtNode(node.NW, frameId, ref obj, out collided)) return true;
                if (CollideAtNode(node.NE, frameId, ref obj, out collided)) return true;
                if (CollideAtNode(node.SE, frameId, ref obj, out collided)) return true;
                if (CollideAtNode(node.SW, frameId, ref obj, out collided)) return true;
            }
        }

        // finds the nearest collision
        public bool CheckCollision(Vector2 pos, float radius, int loyalty, out GameplayObject collided)
        {
            var obj = new SpatialObj // dummy object to simplify our search interface
            {
                X     = pos.X - radius,
                Y     = pos.Y - radius,
                LastX = pos.X + radius,
                LastY = pos.Y + radius,
                Loyalty = loyalty,
            };
            Node node = FindEnclosingNode(ref obj);
            if (node != null) return CollideAtNode(node, FrameId, ref obj, out collided);
            collided = null;
            return false;
        }

        private static void CollideAllAt(Node node, int frameId, Array<SpatialCollision> results)
        {
            if (node.NW != null) // depth first approach, to early filter LastCollided
            {
                CollideAllAt(node.NW, frameId, results);
                CollideAllAt(node.NE, frameId, results);
                CollideAllAt(node.SE, frameId, results);
                CollideAllAt(node.SW, frameId, results);
            }

            int count = node.Count;
            if (count <= 1) // can't collide with self :)
                return;
            SpatialObj[] items = node.Items;
            for (int i = 0; i < count; ++i)
            {
                ref SpatialObj so = ref items[i];
                if (frameId == so.LastCollided)
                    continue; // already collided inside this loop

                if (!CollideAtNode(node, frameId, ref so, out GameplayObject collided))
                    continue;

                results.Add(new SpatialCollision { Obj1 = so.Obj, Obj2 = collided });
            }
        }

        public bool CollideAll(Array<SpatialCollision> results)
        {
            results.Clear();
            CollideAllAt(Root, FrameId, results);
            return results.Count > 0;
        }

        public GameplayObject[] FindNearby(Vector2 pos, float radius)
        {
            // assume most results will be either empty, or only from a single quadrant
            // this means using a dynamic Array will be way more wasteful
            int numNearby = 0;
            GameplayObject[] nearby = Empty<GameplayObject>.Array;

            var obj = new SpatialObj // dummy object to simplify our search interface
            {
                X     = pos.X - radius,
                Y     = pos.Y - radius,
                LastX = pos.X + radius,
                LastY = pos.Y + radius,
            };

            // find the deepest enclosing node
            Node node = FindEnclosingNode(ref obj);

            // now work back upwards
            while (node != null)
            {
                int count = node.Count;
                if (count > 0)
                {
                    int maxNewLength = numNearby + count;
                    if (nearby.Length < maxNewLength) // optimistic: all in range
                        Array.Resize(ref nearby, maxNewLength);

                    SpatialObj[] items = node.Items;
                    for (int i = 0; i < count; ++i)
                        if (HitTest(ref obj, ref items[i]))
                            nearby[numNearby++] = items[i].Obj;
                }

                node = node.Parent;
                if (node == null || ((node.X + node.LastX)/2f) > radius)
                    break;
            }

            if (numNearby != nearby.Length)
                Array.Resize(ref nearby, numNearby);
            return nearby;
        }

        public void DebugVisualize(UniverseScreen screen)
        {
            
        }
    }
}
