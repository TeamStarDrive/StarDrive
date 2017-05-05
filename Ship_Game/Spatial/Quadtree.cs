using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public struct SpatialObj
    {
        public GameplayObject Obj;
        public float X, Y, LastX, LastY;
        public int Loyalty; // if loyalty == 0, then this is a STATIC world object !!!
        public int LastCollided;

        public float Cx, Cy, Radius;
        public GameObjectType Type;

        public bool Is(GameObjectType flags) => (Type & flags) != 0;

        public SpatialObj(GameplayObject go)
        {
            Obj    = go;
            Type   = go.Type;

            if ((Type & GameObjectType.Beam) != 0)
            {
                var beam = (Beam)go;
                Vector2 source = beam.Source;
                Vector2 target = beam.Destination;
                X     = Math.Min(source.X, target.X);
                Y     = Math.Min(source.Y, target.Y);
                LastX = Math.Max(source.X, target.X);
                LastY = Math.Max(source.Y, target.Y);
                Cx = Cy = Radius = 0f;
            }
            else
            {
                Cx = Obj.Center.X;
                Cy = Obj.Center.Y;
                Radius   = Obj.Radius;
                X        = Cx - Radius;
                Y        = Cy - Radius;
                LastX    = Cx + Radius;
                LastY    = Cy + Radius;
            }
            if ((Type & GameObjectType.Projectile) != 0) Loyalty = ((Projectile)go).Loyalty.Id;
            else if ((Type & GameObjectType.Ship) != 0)  Loyalty = ((Ship)go).loyalty.Id;
            else                                         Loyalty = 0;
            LastCollided = 0;
        }

        public void UpdateBounds() // Update SpatialObj bounding box
        {
            if ((Type & GameObjectType.Beam) != 0)
            {
                var beam = (Beam)Obj;
                Vector2 source = beam.Source;
                Vector2 target = beam.Destination;
                X     = Math.Min(source.X, target.X);
                Y     = Math.Min(source.Y, target.Y);
                LastX = Math.Max(source.X, target.X);
                LastY = Math.Max(source.Y, target.Y);
            }
            else
            {
                Cx = Obj.Center.X;
                Cy = Obj.Center.Y;
                Radius   = Obj.Radius;
                X        = Cx - Radius;
                Y        = Cy - Radius;
                LastX    = Cx + Radius;
                LastY    = Cy + Radius;
            }
        }

        private bool Overlaps(ref SpatialObj b)
        {
            return X <= b.LastX && LastX > b.X
                && Y <= b.LastY && LastY > b.Y;
        }

        private bool BeamHitTest(ref SpatialObj target)
        {
            if (!this.Overlaps(ref target))
                return false;
            var obj = (Beam)Obj;
            return new Vector2(target.Cx, target.Cy)
                .RayHitTestCircle(target.Radius, obj.Source, obj.Destination, rayWidth: 8.0f);
        }

        private bool HitTestBeams(ref SpatialObj beam)
        {
            // @todo Add Beam <-> Beam redirected collision in the future
            return false;
        }

        public bool HitTest(ref SpatialObj b)
        {
            bool beamA = Is(GameObjectType.Beam);
            bool beamB = b.Is(GameObjectType.Beam);
            if (beamA || beamB)
            {
                if (beamA && beamB)
                    return false; // HitTestBeams(ref b);
                return beamA ? BeamHitTest(ref b) : b.BeamHitTest(ref this);
            }
            float dx = Cx - b.Cx;
            float dy = Cy - b.Cy;
            float ra = Radius, rb = b.Radius;
            return (dx*dx + dy*dy) < (ra*ra + rb*rb);
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
                if (Items.Length == Count)
                {
                    if (Count == 0) Items = new SpatialObj[CellThreshold];
                    else
                    {
                        //Array.Resize(ref Items, Count * 2);
                        var newItems = new SpatialObj[Count * 2];
                        for (int i = 0; i < Count; ++i)
                            newItems[i] = Items[i];
                        Items = newItems;
                    }
                }
                Items[Count++] = obj;
            }

            public void RemoveAtSwapLast(int index)
            {
                ref SpatialObj last = ref Items[--Count];
                Items[index] = last;
                last.Obj = null;
                if (Count == 0) Items = NoObjects;
            }

            public bool Overlaps(ref Vector2 topleft, ref Vector2 topright)
            {
                return X <= topright.X && LastX > topleft.X
                    && Y <= topright.Y && LastY > topleft.Y;
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
            node.Count = 0;

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

        private static bool RemoveAt(Node node, GameplayObject go)
        {
            for (int i = 0; i < node.Count; ++i)
            {
                if (node.Items[i].Obj != go) continue;
                node.RemoveAtSwapLast(i);
                return true;
            }
            return node.NW != null
                && (RemoveAt(node.NW, go) || RemoveAt(node.NE, go)
                ||  RemoveAt(node.SE, go) || RemoveAt(node.SW, go));
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

                    if (obj.Obj.Active == false)
                    {
                        node.RemoveAtSwapLast(i--);
                        continue;
                    }

                    obj.UpdateBounds();

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
                        item.HitTest(ref obj)) // actual collision test
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

        //// finds the nearest collision
        //public bool CheckCollision(Vector2 pos, float radius, int loyalty, out GameplayObject collided)
        //{
        //    var nearbyDummy = new SpatialObj // dummy object to simplify our search interface
        //    {
        //        X     = pos.X - radius,
        //        Y     = pos.Y - radius,
        //        LastX = pos.X + radius,
        //        LastY = pos.Y + radius,
        //        Loyalty = loyalty,
        //    };
        //    Node node = FindEnclosingNode(ref nearbyDummy);
        //    if (node != null) return CollideAtNode(node, FrameId, ref nearbyDummy, out collided);
        //    collided = null;
        //    return false;
        //}

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

        public GameplayObject[] FindNearby(Vector2 pos, float radius, GameObjectType filter = GameObjectType.None)
        {
            // assume most results will be either empty, or only from a single quadrant
            // this means using a dynamic Array will be way more wasteful
            int numNearby = 0;
            GameplayObject[] nearby = Empty<GameplayObject>.Array;

            var nearbyDummy = new SpatialObj // dummy object to simplify our search interface
            {
                X     = pos.X - radius,
                Y     = pos.Y - radius,
                LastX = pos.X + radius,
                LastY = pos.Y + radius,
            };

            // find the deepest enclosing node
            Node node = FindEnclosingNode(ref nearbyDummy);

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
                    {
                        ref SpatialObj so = ref items[i];
                        if (filter != GameObjectType.None && (so.Obj.Type & filter) == 0)
                            continue; // no filter match

                        if (nearbyDummy.HitTest(ref so))
                            nearby[numNearby++] = so.Obj;
                    }

                }

                node = node.Parent;
                if (node == null || ((node.X + node.LastX)/2f) > radius)
                    break;
            }

            if (numNearby != nearby.Length)
                Array.Resize(ref nearby, numNearby);
            return nearby;
        }

        private static void DebugVisualize(UniverseScreen screen, ref Vector2 topleft, ref Vector2 botright, Node node)
        {
            var center = new Vector2((node.X + node.LastX) / 2, (node.Y + node.LastY) / 2);
            var size   = new Vector2(node.LastX - node.X, node.LastY - node.Y);
            screen.DrawRectangleProjected(center, size, 0f, Color.SaddleBrown, 1f);

            for (int i = 0; i < node.Count; ++i)
            {
                ref SpatialObj so = ref node.Items[i];
                var ocenter = new Vector2((so.X + so.LastX) / 2, (so.Y + so.LastY) / 2);
                var osize   = new Vector2(so.LastX - so.X, so.LastY - so.Y);
                screen.DrawRectangleProjected(ocenter, osize, 0f, Color.MediumVioletRed);
            }
            if (node.NW != null)
            {
                if (node.NW.Overlaps(ref topleft, ref botright)) DebugVisualize(screen, ref topleft, ref botright, node.NW);
                if (node.NE.Overlaps(ref topleft, ref botright)) DebugVisualize(screen, ref topleft, ref botright, node.NE);
                if (node.SE.Overlaps(ref topleft, ref botright)) DebugVisualize(screen, ref topleft, ref botright, node.SE);
                if (node.SW.Overlaps(ref topleft, ref botright)) DebugVisualize(screen, ref topleft, ref botright, node.SW);
            }
        }

        public void DebugVisualize(UniverseScreen screen)
        {
            var screenSize = new Vector2(screen.Viewport.Width, screen.Viewport.Height);
            Vector2 topleft  = screen.UnprojectToWorldPosition(new Vector2(0f, 0f));
            Vector2 botright = screen.UnprojectToWorldPosition(screenSize);
            DebugVisualize(screen, ref topleft, ref botright, Root);
        }
    }
}
