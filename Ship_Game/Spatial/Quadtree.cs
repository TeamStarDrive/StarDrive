using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SpatialObj
    {
        public int Id;
        public float X, Y, LastX, LastY;
        public int Loyalty;
    }

    public sealed unsafe class Quadtree : IDisposable
    {
        private readonly DynamicMemoryPool Pool = new DynamicMemoryPool();
        private readonly int   Levels;
        private readonly float SmallestCell;
        private readonly float FullSize;

        public const int CellThreshold = 4;
        private Node* Root;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Node
        {
            public float X, Y, LastX, LastY;
            public Node* Parent, NW, NE, SE, SW;
            public PoolArraySpatialObj Items;
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
            Pool?.Dispose();
            Root = null;
            GC.SuppressFinalize(this);
        }

        public void Reset()
        {
            Pool.Reset(); // reset and invalidate all pointers

            // universe is centered at [0,0], so Root node goes from [-half, +half)
            float half = FullSize / 2;
            Root = CreateNode(null, -half, -half, +half, +half);
        }

        private static bool Overlaps(Node* n, ref SpatialObj b)
        {
            return n->X <= b.LastX && n->LastX > b.X
                && n->Y <= b.LastY && n->LastY > b.Y;
        }

        private static bool Overlaps(SpatialObj* a, SpatialObj* b)
        {
            return a->X <= b->LastX && a->LastX > b->X
                && a->Y <= b->LastY && a->LastY > b->Y;
        }

        // squared distance; if negative, we have a collision
        private static float DistanceTo(SpatialObj* a, SpatialObj* b)
        {
            float ra = (a->LastX - a->X) / 2;
            float rb = (b->LastX - b->X) / 2;
            float acx = a->X + ra, acy = a->Y + ra;
            float bcx = b->X + rb, bcy = b->Y + rb;
            float dx = acx - bcx;
            float dy = acy - bcy;
            return (dx*dx + dy*dy) - (ra*ra + rb*rb);
        }

        private static bool HitTest(SpatialObj* a, SpatialObj* b)
        {
            float ra = (a->LastX - a->X) / 2;
            float rb = (b->LastX - b->X) / 2;
            float acx = a->X + ra, acy = a->Y + ra;
            float bcx = b->X + rb, bcy = b->Y + rb;
            float dx = acx - bcx;
            float dy = acy - bcy;
            return (dx*dx + dy*dy) < (ra*ra + rb*rb);
        }

        private Node* CreateNode(Node* parent, float x, float y, float lastX, float lastY)
        {
            var node = (Node*)Pool.Alloc(sizeof(Node));
            node->X      = x;
            node->Y      = y;
            node->LastX  = lastX;
            node->LastY  = lastY;
            node->Parent = parent;
            node->NW = node->NE = node->SE = node->SW = null;
            node->Items = default(PoolArraySpatialObj);
            return node;
        }

        private void SplitNode(Node* node, int level)
        {
            float midX = (node->X + node->LastX) / 2;
            float midY = (node->Y + node->LastY) / 2;

            node->NW = CreateNode(node, node->X, node->Y, midX,        midY);
            node->NE = CreateNode(node, midX,    node->Y, node->LastX, midY);
            node->SE = CreateNode(node, midX,    midY,    node->LastX, node->LastY);
            node->SW = CreateNode(node, node->X, midY,    midX,        node->LastY);

            PoolArraySpatialObj arr = node->Items;
            node->Items = default(PoolArraySpatialObj);

            // reinsert all items:
            for (int i = 0; i < arr.Count; ++i)
                InsertAt(node, level, arr.Items[i]);
        }

        private static Node* PickSubQuadrant(Node* node, SpatialObj* obj)
        {
            float midX = (node->X + node->LastX) / 2;
            float midY = (node->Y + node->LastY) / 2;

            if (obj->X < midX && obj->LastX < midX) // left
            {
                if (obj->Y <  midY && obj->LastY < midY) return node->NW; // top left
                if (obj->Y >= midY)                      return node->SW; // bot left
            }
            else if (obj->X >= midX) // right
            {
                if (obj->Y <  midY && obj->LastY < midY) return node->NE; // top right
                if (obj->Y >= midY)                      return node->SE; // bot right
            }
            return null; // obj does not perfectly fit inside a quadrant
        }

        private void InsertAt(Node* node, int level, SpatialObj* obj)
        {
            for (;;)
            {
                if (level <= 1) // no more subdivisions possible
                {
                    Pool.ArrayAdd(&node->Items, obj);
                    return;
                }

                Node* quad = PickSubQuadrant(node, obj);
                if (quad != null)
                {
                    node = quad; // go deeper!
                    level -= 1;
                    continue;
                }

                // item belongs to this node
                Pool.ArrayAdd(&node->Items, obj);

                // actually, are we maybe over Threshold and should Divide ?
                if (node->NW != null && node->Items.Count >= CellThreshold)
                    SplitNode(node, level);
                return;
            }
        }

        public void Insert(int id, Vector2 pos, float radius)
        {
            var obj = (SpatialObj*)Pool.Alloc(sizeof(SpatialObj));
            obj->Id    = id;
            obj->X     = pos.X - radius;
            obj->Y     = pos.Y - radius;
            obj->LastX = pos.X + radius;
            obj->LastY = pos.Y + radius;
            InsertAt(Root, Levels, obj);
        }

        // finds the node that fully encloses this spatial object
        private Node* FindEnclosingNode(SpatialObj* obj)
        {
            int level = Levels;
            Node* node = Root;
            for (;;)
            {
                if (level <= 1) // no more subdivisions possible
                    break;

                Node* quad = PickSubQuadrant(node, obj);
                if (quad == null)
                    break;

                node = quad; // go deeper!
                level -= 1;
            }
            return node;
        }

        private static SpatialObj* CollideAtNode(Node* node, SpatialObj* obj)
        {
            for (;;)
            {
                PoolArraySpatialObj arr = node->Items;
                for (int i = 0; i < arr.Count; ++i)
                {
                    SpatialObj* item = arr.Items[i];
                    // ignore friendly fire:
                    if (item->Loyalty != obj->Loyalty && HitTest(item, obj))
                        return item;
                }

                if (node->NW == null)
                    return null;
                SpatialObj* result;
                if ((result = CollideAtNode(node->NW, obj)) != null) return result;
                if ((result = CollideAtNode(node->NE, obj)) != null) return result;
                if ((result = CollideAtNode(node->SE, obj)) != null) return result;
                if ((result = CollideAtNode(node->SW, obj)) != null) return result;
            }
        }

        // finds the nearest collision
        public bool CheckCollision(Vector2 pos, float radius, int loyalty, out int collidedWith)
        {
            SpatialObj obj; // dummy object to simplify our search interface
            obj.X     = pos.X - radius;
            obj.Y     = pos.Y - radius;
            obj.LastX = pos.X + radius;
            obj.LastY = pos.Y + radius;
            obj.Loyalty = loyalty;

            Node* node = FindEnclosingNode(&obj);
            if (node != null) // aha! we overlap with something
            {
                SpatialObj* collided = CollideAtNode(node, &obj);
                if (collided != null)
                {
                    collidedWith = collided->Id;
                    return true;
                }
            }
            collidedWith = -1;
            return false;
        }

        public int[] FindNearby(Vector2 pos, float radius)
        {
            var result = new Array<int>();

            SpatialObj obj;
            obj.X     = pos.X - radius;
            obj.Y     = pos.Y - radius;
            obj.LastX = pos.X + radius;
            obj.LastY = pos.Y + radius;

            // find the deepest enclosing node
            Node* node = FindEnclosingNode(&obj);

            // now work back upwards
            while (node != null)
            {
                PoolArraySpatialObj nodeArr = node->Items;
                for (int i = 0; i < nodeArr.Count; ++i)
                {
                    result.Add(nodeArr.Items[i]->Id);
                }

                node = node->Parent;
                if (node == null || ((node->X + node->LastX)/2f) > radius)
                    break;
            }
            return result.ToArray();
        }

        public void DebugVisualize(UniverseScreen screen)
        {
            
        }
    }
}
