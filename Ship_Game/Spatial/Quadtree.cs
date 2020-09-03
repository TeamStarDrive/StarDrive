﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    [StructLayout(LayoutKind.Sequential, Pack=4)]
    public struct SpatialObj // sizeof: 36 bytes, neatly fits in one cache line
    {
        public GameplayObject Obj;

        public GameObjectType Type; // GameObjectType : byte
        public byte Loyalty;        // if loyalty == 0, then this is a STATIC world object !!!
        public byte OverlapsQuads;  // does it overlap multiple quads?
        public byte LastUpdate;

        public Vector2 Center;
        public float Radius;
        public float X, Y, LastX, LastY;

        public override string ToString() => Obj.ToString();

        public SpatialObj(GameplayObject go)
        {
            Obj           = go;
            Type          = go.Type;
            Loyalty       = (byte)go.GetLoyaltyId();
            OverlapsQuads = 0;
            LastUpdate    = 0;
            if ((Type & GameObjectType.Beam) != 0)
            {
                var beam = (Beam)go;
                Vector2 source = beam.Source;
                Vector2 target = beam.Destination;
                X     = Math.Min(source.X, target.X);
                Y     = Math.Min(source.Y, target.Y);
                LastX = Math.Max(source.X, target.X);
                LastY = Math.Max(source.Y, target.Y);
                Center = default;
                Radius = 0f;
            }
            else
            {
                Center   = Obj.Center;
                Radius   = Obj.Radius;
                X        = Center.X - Radius;
                Y        = Center.Y - Radius;
                LastX    = Center.X + Radius;
                LastY    = Center.Y + Radius;
            }
        }

        public SpatialObj(Vector2 center, float radius)
        {
            Obj           = null;
            Type          = GameObjectType.Any;
            Loyalty       = 0;
            OverlapsQuads = 0;
            LastUpdate    = 0;
            Center        = center;
            Radius        = radius;
            X             = Center.X - radius;
            Y             = Center.Y - radius;
            LastX         = Center.X + radius;
            LastY         = Center.Y + radius;
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
                Center   = Obj.Center;
                Radius   = Obj.Radius;
                X        = Center.X - Radius;
                Y        = Center.Y - Radius;
                LastX    = Center.X + Radius;
                LastY    = Center.Y + Radius;
            }
        }

        public bool HitTestBeam(ref SpatialObj target, out ShipModule hitModule, out float distanceToHit)
        {
            var beam = (Beam)Obj;
            ++GlobalStats.BeamTests;

            Vector2 beamStart = beam.Source;
            Vector2 beamEnd   = beam.Destination;

            if ((target.Type & GameObjectType.Ship) != 0) // beam-ship is special collision
            {
                var ship = (Ship)target.Obj;
                hitModule = ship.RayHitTestSingle(beamStart, beamEnd, 8f, beam.IgnoresShields);
                if (hitModule == null)
                {
                    distanceToHit = float.NaN;
                    return false;
                }
                return hitModule.RayHitTest(beamStart, beamEnd, 8f, out distanceToHit);
            }

            hitModule = null;
            if ((target.Type & GameObjectType.Proj) != 0)
            {
                var proj = (Projectile)target.Obj;
                if (!proj.Weapon.Tag_Intercept) // for projectiles, make sure they are physical and can be killed
                {
                    distanceToHit = float.NaN;
                    return false;
                }
            }

            // intersect projectiles or anything else that can collide
            return target.Center.RayCircleIntersect(target.Radius, beamStart, beamEnd, out distanceToHit);
        }

        // assumes THIS is a projectile
        public bool HitTestProj(float simTimeStep, ref SpatialObj target, out ShipModule hitModule)
        {
            hitModule = null;
            float dx = Center.X - target.Center.X;
            float dy = Center.Y - target.Center.Y;
            float r2 = Radius + target.Radius;
            if ((dx*dx + dy*dy) > (r2*r2)) // filter out by target Ship or target Projectile radius
                return false;
            // NOTE: this is for Projectile<->Projectile collision!
            if ((target.Type & GameObjectType.Ship) == 0) // target not a ship, collision success
                return true;

            // ship collision, target modules instead
            var proj = (Projectile)Obj;
            var ship = (Ship)target.Obj;
            if (ship == null) { Log.Warning("HitTestProj had a null ship."); return false; }

            float velocity = proj.Velocity.Length();
            float maxDistPerFrame = velocity * simTimeStep;

            // if this projectile will move more than 15 units (1 module grid = 16x16) within one simulation step
            // we have to use ray-casting to avoid projectiles clipping through objects
            if (maxDistPerFrame > 15f)
            {
                Vector2 dir = proj.Velocity / velocity;
                Vector2 prevPos = Center - (dir*maxDistPerFrame);
                hitModule = ship.RayHitTestSingle(prevPos, Center, Radius, proj.IgnoresShields);
            }
            else
            {
                hitModule = ship.HitTestSingle(proj.Center, proj.Radius, proj.IgnoresShields);
            }
            return hitModule != null;
        }
    }


    ///////////////////////////////////////////////////////////////////////////////////////////

    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    public sealed class Quadtree : IDisposable
    {
        static readonly SpatialObj[] NoObjects = new SpatialObj[0];

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

        Node Root;
        int FrameId;
        FixedSimTime SimulationStep;

        ///////////////////////////////////////////////////////////////////////////////////////////
        class Node
        {
            public readonly float X, Y, LastX, LastY;
            public Node NW, NE, SE, SW;
            public int Count;
            public SpatialObj[] Items;
            public Node(float x, float y, float lastX, float lastY)
            {
                X = x; Y = y;
                LastX = lastX; LastY = lastY;
                Items = NoObjects;
            }
            public void Add(ref SpatialObj obj)
            {
                int count = Count;
                SpatialObj[] oldItems = Items;
                if (oldItems.Length == count)
                {
                    if (count == 0)
                    {
                        var newItems = new SpatialObj[CellThreshold];
                        newItems[count] = obj;
                        Items = newItems;
                        ++Count;
                    }
                    else // oldItems.Length == Count
                    {
                        //Array.Resize(ref Items, Count * 2);
                        var newItems = new SpatialObj[oldItems.Length * 2];
                        int i = 0;
                        for (; i < oldItems.Length; ++i)
                            newItems[i] = oldItems[i];
                        newItems[count] = obj;
                        Items = newItems;
                        ++Count;
                    }
                }
                else
                {
                    oldItems[count] = obj;
                    ++Count;
                }
            }

            // Because SwapLast reorders elements, we decrement the ref index to allow loops to continue
            // naturally. Index is not decremented if it is the last element
            public void RemoveAtSwapLast(ref int index)
            {
                int newCount = Count-1;
                if (newCount < 0) // FIX: this is a threading issue, the item was already removed
                    return; 

                Count = newCount;
                ref SpatialObj last = ref Items[newCount];
                if (index != newCount) // only swap and change ref index if it wasn't the last element
                {
                    Items[index] = last;
                    --index;
                }
                last.Obj = null; // prevent zombie objects
                if (newCount == 0) Items = NoObjects;
            }

            public bool Overlaps(ref Vector2 topLeft, ref Vector2 topRight)
            {
                return X <= topRight.X && LastX > topLeft.X
                    && Y <= topRight.Y && LastY > topLeft.Y;
            }
        }
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
            Root    = null;
            GC.SuppressFinalize(this);
        }

        public void Reset()
        {
            // universe is centered at [0,0], so Root node goes from [-half, +half)
            float half = FullSize / 2;
            Root = new Node(-half, -half, +half, +half);
        }

        static void SplitNode(Node node, int level)
        {
            float midX = (node.X + node.LastX) / 2;
            float midY = (node.Y + node.LastY) / 2;

            node.NW = new Node(node.X, node.Y, midX,       midY);
            node.NE = new Node(midX,   node.Y, node.LastX, midY);
            node.SE = new Node(midX,   midY,   node.LastX, node.LastY);
            node.SW = new Node(node.X, midY,   midX,       node.LastY);

            int count = node.Count;
            SpatialObj[] arr = node.Items;
            node.Items = NoObjects;
            node.Count = 0;

            // reinsert all items:
            for (int i = 0; i < count; ++i)
                InsertAt(node, level, ref arr[i]);
        }

        static Node PickSubQuadrant(Node node, ref SpatialObj obj)
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

        static void InsertAt(Node node, int level, ref SpatialObj obj)
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
                    Node quad = PickSubQuadrant(node, ref obj);
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

        static bool RemoveAt(Node node, GameplayObject go)
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

        static void FastRemoval(GameplayObject obj, Node node, ref int index)
        {
            UniverseScreen.SpaceManager.FastNonTreeRemoval(obj);
            node.RemoveAtSwapLast(ref index);
        }

        void UpdateNode(Node node, int level, byte frameId)
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
                        Node quad = PickSubQuadrant(node, ref obj);
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

        static int RemoveEmptyChildNodes(Node node)
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
        Node FindEnclosingNode(ref SpatialObj obj)
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

        // ship collision; this can collide with multiple projectiles..
        // beams are ignored because they may intersect multiple objects and thus require special CollideBeamAtNode
        static void CollideShipAtNode(float simTimeStep, Node node, ref SpatialObj ship)
        {
            for (int i = 0; i < node.Count; ++i)
            {
                ref SpatialObj proj = ref node.Items[i]; // potential projectile ?
                if (proj.Loyalty != ship.Loyalty      && // friendlies don't collide
                    (proj.Type & GameObjectType.Proj) != 0 && // only collide with projectiles
                    (proj.Type & GameObjectType.Beam) == 0 && // forbid obj-beam tests; beam-obj is handled by CollideBeamAtNode
                    proj.HitTestProj(simTimeStep, ref ship, out ShipModule hitModule))
                {
                    var projectile = proj.Obj as Projectile;
                    if (!HandleProjCollision(projectile, hitModule ?? ship.Obj))
                        continue; // there was no collision

                    if (projectile.DieNextFrame) FastRemoval(projectile, node, ref i);
                }
            }
            if (node.NW == null) return;
            CollideShipAtNode(simTimeStep, node.NW, ref ship);
            CollideShipAtNode(simTimeStep, node.NE, ref ship);
            CollideShipAtNode(simTimeStep, node.SE, ref ship);
            CollideShipAtNode(simTimeStep, node.SW, ref ship);
        }

        //@HACK sometime Obj is null and crash the game. added if null mark dienextframe false. 
        //This is surely a bug but the hack might need to be true?
        static bool ProjectileIsDying(ref SpatialObj obj)
            => (obj.Type & GameObjectType.Proj) != 0 && ((obj.Obj as Projectile)?.DieNextFrame ?? false);


        // projectile collision, return the first match because the projectile destroys itself anyway
        static bool CollideProjAtNode(float simTimeStep, Node node, ref SpatialObj proj)
        {
            for (int i = 0; i < node.Count; ++i)
            {
                ref SpatialObj item = ref node.Items[i];
                if (item.Loyalty != proj.Loyalty &&           // friendlies don't collide, also ignores self
                    (item.Type & GameObjectType.Beam) == 0 && // forbid obj-beam tests; beam-obj is handled by CollideBeamAtNode
                    proj.HitTestProj(simTimeStep, ref item, out ShipModule hitModule))
                {
                    if (!HandleProjCollision(proj.Obj as Projectile, hitModule ?? item.Obj)) // module OR projectile
                        continue; // there was no collision

                    if ((item.Type & GameObjectType.Proj) != 0 && (item.Obj as Projectile).DieNextFrame)
                        FastRemoval(item.Obj, node, ref i);
                    return true;
                }
            }
            if (node.NW == null) return false;
            return CollideProjAtNode(simTimeStep, node.NW, ref proj)
                || CollideProjAtNode(simTimeStep, node.NE, ref proj)
                || CollideProjAtNode(simTimeStep, node.SE, ref proj)
                || CollideProjAtNode(simTimeStep, node.SW, ref proj);
        }

        struct BeamHitResult : IComparable<BeamHitResult>
        {
            public GameplayObject Collided;
            public float Distance;

            public int CompareTo(BeamHitResult other)
            {
                return Distance.CompareTo(other.Distance);
            }
        }

        // we keep this list as a cache to reduce memory pressure
        readonly Array<BeamHitResult> BeamHitCache = new Array<BeamHitResult>();

        static void CollideBeamRecursive(Node node, ref SpatialObj beam, Array<BeamHitResult> outHitResults)
        {
            for (int i = 0; i < node.Count; ++i)
            {
                ref SpatialObj item = ref node.Items[i];
                if (item.Loyalty != beam.Loyalty &&        // friendlies don't collide
                   (item.Type & GameObjectType.Beam) == 0) // forbid beam-beam collision            
                {
                    if (beam.HitTestBeam(ref item, out ShipModule hitModule, out float dist))
                    {
                        outHitResults.Add(new BeamHitResult
                        {
                            Distance = dist,
                            Collided = hitModule ?? item.Obj
                        });
                    }
                }
            }
            if (node.NW == null) return;
            CollideBeamRecursive(node.NW, ref beam, outHitResults);
            CollideBeamRecursive(node.NE, ref beam, outHitResults);
            CollideBeamRecursive(node.SE, ref beam, outHitResults);
            CollideBeamRecursive(node.SW, ref beam, outHitResults);
        }

        static void CollideBeamAtNode(Node node, ref SpatialObj beam, Array<BeamHitResult> beamHitCache)
        {
            CollideBeamRecursive(node, ref beam, beamHitCache);
            if (beamHitCache.Count > 0)
            {
                // for beams it's important to only collide the CLOSEST object
                // so we need to sort the hits by distance
                // and then work from closest to farthest until we get a valid collision
                // 
                // Some missiles/projectiles have special dodge features, so we need to check all touches.
                if (beamHitCache.Count > 1)
                    beamHitCache.Sort();
                for (int i = 0; i < beamHitCache.Count; ++i)
                {
                    BeamHitResult hit = beamHitCache[i];
                    if (HandleBeamCollision(beam.Obj as Beam, hit.Collided, hit.Distance))
                        break; // and we're done
                }
                beamHitCache.Clear();
            }
        }

        public void CollideAll()
        {
            CollideAllAt(SimulationStep.FixedTime, Root, BeamHitCache);
        }

        static void CollideAllAt(float simTimeStep, Node node, Array<BeamHitResult> beamHitCache)
        {
            if (node.NW != null) // depth first approach, to early filter LastCollided
            {
                CollideAllAt(simTimeStep, node.NW, beamHitCache);
                CollideAllAt(simTimeStep, node.NE, beamHitCache);
                CollideAllAt(simTimeStep, node.SE, beamHitCache);
                CollideAllAt(simTimeStep, node.SW, beamHitCache);
            }
            for (int i = 0; i < node.Count; ++i)
            {
                ref SpatialObj so = ref node.Items[i];
                GameplayObject go = so.Obj;
                if (go == null) // FIX: concurrency issue, someone already removed this item
                    continue;
                if (go.Active == false)
                    continue; // already collided inside this loop

                // each collision instigator type has a very specific recursive handler
                if ((so.Type & GameObjectType.Beam) != 0)
                {
                    CollideBeamAtNode(node, ref so, beamHitCache);
                }
                else if ((so.Type & GameObjectType.Proj) != 0)
                {
                    if (CollideProjAtNode(simTimeStep, node, ref so) && ProjectileIsDying(ref so))
                        FastRemoval(go, node, ref i);
                }
                else if ((so.Type & GameObjectType.Ship) != 0)
                {
                    CollideShipAtNode(simTimeStep, node, ref so);
                }
            }
        }


        // Traverse the entire tree to get the # of items
        // NOTE: This is SLOW
        public int CountItemsSlow() => CountItemsRecursive(Root);

        static int CountItemsRecursive(Node node)
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


        static bool HandleBeamCollision(Beam beam, GameplayObject victim, float hitDistance)
        {
            if (!beam.Touch(victim))
                return false;

            Vector2 beamStart = beam.Source;
            Vector2 beamEnd   = beam.Destination;
            Vector2 hitPos;
            if (hitDistance > 0f)
                hitPos = beamStart + (beamEnd - beamStart).Normalized()*hitDistance;
            else // the beam probably glanced the module from side, so just get the closest point:
                hitPos = victim.Center.FindClosestPointOnLine(beamStart, beamEnd);

            beam.BeamCollidedThisFrame = true;
            beam.ActualHitDestination = hitPos;
            return true;
        }

        static bool HandleProjCollision(Projectile projectile, GameplayObject victim)
        {
            return projectile.Touch(victim);
        }

        /// <summary>
        /// Optimized temporary search buffer for FindNearby
        /// </summary>
        class FindResultBuffer
        {
            public int Count = 0;
            public GameplayObject[] Items = new GameplayObject[128];
            public int NextNode = 0; // next node to pop
            public Node[] NodeStack = new Node[512];
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
            int filterMask = (int)filter;

            float cx = searchAreaRect.Center.X;
            float cy = searchAreaRect.Center.Y;
            float r  = searchAreaRect.Radius;
            GameplayObject sourceObject = searchAreaRect.Obj;
            do
            {
                // inlined POP
                Node node = nearby.NodeStack[nearby.NextNode];
                nearby.NodeStack[nearby.NextNode] = default; // don't leak refs
                --nearby.NextNode;

                int count = node.Count; 
                SpatialObj[] items = node.Items;
                for (int i = 0; i < count; ++i)
                {
                    ref SpatialObj so = ref items[i];

                    // either 0x00 (failed) or some bits 0100 (success)
                    int typeFlags = ((int)so.Type & filterMask);

                    // either 0x00 (failed) or some bits 0011 (success)
                    int loyaltyFlags = (so.Loyalty & loyaltyMask);
                    
                    if (typeFlags == 0 || loyaltyFlags == 0 || so.Obj == sourceObject)
                        continue;

                    // check if inside radius, inlined for perf
                    float dx = cx - so.Center.X;
                    float dy = cy - so.Center.Y;
                    float r2 = r + so.Radius;
                    if ((dx*dx + dy*dy) <= (r2*r2))
                    {
                        // inline array expand
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
            }
            while (nearby.NextNode >= 0);
        }

        readonly Map<int, FindResultBuffer> ThreadLocalFindBuffers = new Map<int, FindResultBuffer>();

        FindResultBuffer GetThreadLocalBuffer()
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            if (!ThreadLocalFindBuffers.TryGetValue(threadId, out FindResultBuffer buffer))
            {
                buffer = new FindResultBuffer();
                ThreadLocalFindBuffers.Add(threadId, buffer);
            }
            return buffer;
        }

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
            enclosingRectangle.Loyalty = (byte)(loyaltyFilter?.Id ?? 0); // filter by loyalty?

            FindResultBuffer nearby = GetThreadLocalBuffer();

            // find the deepest enclosing node
            Node node = FindEnclosingNode(ref enclosingRectangle);
            if (node != null)
            {
                nearby.NextNode = 0;
                nearby.NodeStack[0] = node;
                FindNearbyAtNode(nearby, ref enclosingRectangle, filter);
            }

            return nearby.GetArrayAndClearBuffer();
        }

        static bool ShouldStoreDebugInfo => Empire.Universe.Debug
                                         && Empire.Universe.DebugWin != null
                                         && Debug.DebugInfoScreen.Mode == Debug.DebugModes.SpatialManager;

        static void AddNearbyDebug(GameplayObject obj, float radius, GameplayObject[] nearby)
        {
            var debug = new FindNearbyDebug { Obj = obj, Radius = radius, Nearby = nearby, Timer = 2f };
            for (int i = 0; i < DebugFindNearby.Count; ++i)
                if (DebugFindNearby[i].Obj == obj) {
                    DebugFindNearby[i] = debug;
                    return;           
                }
            DebugFindNearby.Add(debug);
        }

        struct FindNearbyDebug
        {
            public GameplayObject Obj;
            public float Radius;
            public GameplayObject[] Nearby;
            public float Timer;
        }

        static readonly Array<FindNearbyDebug> DebugFindNearby = new Array<FindNearbyDebug>();
        static SpatialObj[] DebugDrawBuffer = NoObjects;

        static readonly Color Brown  = new Color(Color.SaddleBrown, 150);
        static readonly Color Violet = new Color(Color.MediumVioletRed, 100);

        // "Allies are Blue, Enemies are Red, what should I do, with our Quadtree?" - RedFox
        static readonly Color Blue   = new Color(Color.CadetBlue, 100);
        static readonly Color Red    = new Color(Color.OrangeRed, 100);
        static readonly Color Yellow = new Color(Color.Yellow, 100);

        static void DebugVisualize(GameScreen screen, ref Vector2 topleft, ref Vector2 botright, Node node)
        {
            var center = new Vector2((node.X + node.LastX) / 2, (node.Y + node.LastY) / 2);
            var size   = new Vector2(node.LastX - node.X, node.LastY - node.Y);
            screen.DrawRectangleProjected(center, size, 0f, Brown);

            // @todo This is a hack to reduce concurrency related bugs.
            //       once the main drawing and simulation loops are stable enough, this copying can be removed
            //       In most cases it doesn't matter, because this is only used during DEBUG display...
            int count = node.Count;
            if (DebugDrawBuffer.Length < count) DebugDrawBuffer = new SpatialObj[count];
            Array.Copy(node.Items, DebugDrawBuffer, count);

            for (int i = 0; i < count; ++i)
            {
                ref SpatialObj so = ref DebugDrawBuffer[i];
                var soCenter = new Vector2((so.X + so.LastX) / 2, (so.Y + so.LastY) / 2);
                var soSize   = new Vector2(so.LastX - so.X, so.LastY - so.Y);
                screen.DrawRectangleProjected(soCenter, soSize, 0f, Violet);
                screen.DrawCircleProjected(soCenter, so.Radius, Violet);
                screen.DrawLineProjected(center, soCenter, Violet);
            }
            if (node.NW != null)
            {
                if (node.NW.Overlaps(ref topleft, ref botright)) DebugVisualize(screen, ref topleft, ref botright, node.NW);
                if (node.NE.Overlaps(ref topleft, ref botright)) DebugVisualize(screen, ref topleft, ref botright, node.NE);
                if (node.SE.Overlaps(ref topleft, ref botright)) DebugVisualize(screen, ref topleft, ref botright, node.SE);
                if (node.SW.Overlaps(ref topleft, ref botright)) DebugVisualize(screen, ref topleft, ref botright, node.SW);
            }
        }

        public void DebugVisualize(GameScreen screen)
        {
            var screenSize = new Vector2(screen.Viewport.Width, screen.Viewport.Height);
            Vector2 topLeft  = screen.UnprojectToWorldPosition(new Vector2(0f, 0f));
            Vector2 botRight = screen.UnprojectToWorldPosition(screenSize);
            DebugVisualize(screen, ref topLeft, ref botRight, Root);

            Array.Clear(DebugDrawBuffer, 0, DebugDrawBuffer.Length); // prevent zombie objects

            //for (int i = 0; i < DebugFindNearby.Count; ++i)
            //{
            //    FindNearbyDebug debug = DebugFindNearby[i];
            //    if (debug.Obj == null) continue;
            //    screen.DrawCircleProjected(debug.Obj.Center, debug.Radius, 36, Golden);
            //    for (int j = 0; j < debug.Nearby.Length; ++j)
            //    {
            //        GameplayObject nearby = debug.Nearby[j];
            //        screen.DrawLineProjected(debug.Obj.Center, nearby.Center, GetRelationColor(debug.Obj, nearby));
            //    }

            //    debug.Timer -= screen.SimulationDeltaTime;
            //    if (debug.Timer > 0f)
            //        DebugFindNearby[i] = debug;
            //    else
            //        DebugFindNearby.RemoveAtSwapLast(i--);
            //}
        }

        static Color GetRelationColor(GameplayObject a, GameplayObject b)
        {
            Empire e1 = EmpireManager.GetEmpireById(a.GetLoyaltyId());
            Empire e2 = EmpireManager.GetEmpireById(b.GetLoyaltyId());
            if (e1 != null && e2 != null)
            {
                if (e1 == e2)
                    return Blue;
                if (e1.IsEmpireAttackable(e2)) // hostile?
                    return Red;
                if (e1.TryGetRelations(e2, out Relationship relations) && relations.Treaty_Alliance)
                    return Blue;
            }
            return Yellow; // neutral relation
        }

    }
}
