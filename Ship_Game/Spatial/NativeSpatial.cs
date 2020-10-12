using Microsoft.Xna.Framework;
using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

#pragma warning disable 0649 // uninitialized struct

namespace Ship_Game.Spatial
{
    public enum SpatialType : int
    {
        Grid, // spatial::Grid
        QuadTree, // spatial::QuadTree
        ManagedQtree, // C# Quadtree
    };

    public sealed unsafe class NativeSpatial : ISpatial, IDisposable
    {
        const string Lib = "SDNative.dll";
        const CallingConvention CC = CallingConvention.StdCall;

        [DllImport(Lib)] static extern IntPtr SpatialCreate(SpatialType type, int worldSize, int cellSize);
        [DllImport(Lib)] static extern void SpatialDestroy(IntPtr spatial);
        
        [DllImport(Lib)] static extern SpatialType SpatialGetType(IntPtr spatial);
        [DllImport(Lib)] static extern int SpatialWorldSize(IntPtr spatial);
        [DllImport(Lib)] static extern int SpatialFullSize(IntPtr spatial);
        [DllImport(Lib)] static extern int SpatialNumActive(IntPtr spatial);
        [DllImport(Lib)] static extern int SpatialMaxObjects(IntPtr spatial);

        [DllImport(Lib)] static extern void SpatialClear(IntPtr spatial);
        [DllImport(Lib)] static extern void SpatialRebuild(IntPtr spatial);
        [DllImport(Lib)] static extern void SpatialRebuildAll(IntPtr spatial);

        [DllImport(Lib)] static extern int SpatialInsert(IntPtr spatial, ref NativeSpatialObject o);
        [DllImport(Lib)] static extern void SpatialUpdate(IntPtr spatial, int objectId, int x, int y, int rx, int ry);
        [DllImport(Lib)] static extern void SpatialRemove(IntPtr spatial, int objectId);

        [DllImport(Lib)] static extern void SpatialCollideAll(IntPtr spatial, ref CollisionParams param, ref CollisionPairs outResults);
        [DllImport(Lib)] static extern int SpatialFindNearby(IntPtr spatial, int* outResults, ref NativeSearchOptions opt);

        IntPtr Spat;
        Array<GameplayObject> FrontObjects = new Array<GameplayObject>(capacity:512);
        Array<GameplayObject> BackObjects  = new Array<GameplayObject>(capacity:512);

        public SpatialType Type { get; }
        public float WorldSize { get; }
        public float FullSize { get; }
        public int Count => SpatialNumActive(Spat);
        public int MaxObjects => SpatialMaxObjects(Spat);
        public string Name { get; }

        /// <param name="type">What type of spatial structure to create</param>
        /// <param name="worldSize">Width and Height of the game world</param>
        /// <param name="cellSize">
        /// Size of a single spatial cell. For Grid, this is the Cell Size.
        /// For QuadTree, this is the smallest possible subdivision cell size
        /// </param>
        public NativeSpatial(SpatialType type, int worldSize, int cellSize)
        {
            Type = type;
            Spat = SpatialCreate(type, worldSize, cellSize);

            WorldSize = worldSize;
            FullSize = SpatialFullSize(Spat);
            Name = "C++" + Type;
        }

        ~NativeSpatial()
        {
            SpatialDestroy(Spat);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            IntPtr tree = Spat;
            Spat = IntPtr.Zero;
            SpatialDestroy(tree);
        }

        public void Clear()
        {
            SpatialClear(Spat);
            FrontObjects.Clear();
            BackObjects.Clear();
        }

        public void UpdateAll(Array<GameplayObject> allObjects)
        {
            int count = allObjects.Count;
            int maxObjects = Math.Max(MaxObjects, count);

            var objectsMap = BackObjects;
            objectsMap.Clear(); // avoid any ref leaks
            objectsMap.Resize(maxObjects);

            GameplayObject[] objects = allObjects.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
            {
                GameplayObject go = objects[i];
                int objectId = go.SpatialIndex;
                if (go.Active && !go.DisableSpatialCollision)
                {
                    if (objectId == -1)
                    {
                        var so = new NativeSpatialObject(go);
                        objectId = SpatialInsert(Spat, ref so);
                        go.SpatialIndex = objectId;
                        objectsMap[objectId] = go;
                    }
                    else
                    {
                        int rx, ry;
                        if (go.Type == GameObjectType.Beam)
                        {
                            var beam = (Beam)go;
                            rx = beam.RadiusX;
                            ry = beam.RadiusY;
                        }
                        else
                        {
                            rx = ry = (int)go.Radius;
                        }

                        SpatialUpdate(Spat, objectId, (int)go.Center.X, (int)go.Center.Y, rx, ry);
                        objectsMap[objectId] = go;
                    }
                }
                else if (objectId != -1)
                {
                    SpatialRemove(Spat, objectId);
                    go.SpatialIndex = -1;
                }
            }

            SpatialRebuild(Spat);

            // now swap front and back
            BackObjects = FrontObjects;
            FrontObjects = objectsMap;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct CollisionParams
        {
            public byte IgnoreSameLoyalty; // if 1, same loyalty objects don't collide
            public byte SortCollisionsById; // if 1, collision results are sorted by object Id-s, ascending
            public byte ShowCollisions; // if 1, collisions are shown as debug
        }
        struct CollisionPair
        {
            public int A;
            public int B;
            public static readonly CollisionPair Empty = new CollisionPair{ A = -1, B = -1 };
        }
        struct CollisionPairs
        {
            public CollisionPair* Data;
            public int Size;
            public int Capacity;
        }

        public int CollideAll(FixedSimTime timeStep)
        {
            var p = new CollisionParams
            {
                IgnoreSameLoyalty = 1,
                SortCollisionsById = 1,
                ShowCollisions = (byte)(Empire.Universe?.Debug == true ? 1 : 0),
            };

            // get the collisions
            CollisionPairs collisions = default;
            SpatialCollideAll(Spat, ref p, ref collisions);
            
            int numCollisions = CollideObjects(timeStep, collisions);
            return numCollisions;
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

        int CollideObjects(FixedSimTime timeStep, CollisionPairs collisions)
        {
            int numCollisions = 0;

            // handle the sorted collision pairs
            // for beam weapons, we need to gather all overlaps and find the nearest
            var beamHits = new Array<BeamHitResult>();

            for (int i = 0; i < collisions.Size; ++i)
            {
                CollisionPair pair = collisions.Data[i];
                if (pair.A == -1) // object removed by beam collision
                    continue;

                GameplayObject objectA = FrontObjects[pair.A];
                GameplayObject objectB = FrontObjects[pair.B];
                if (objectB == null)
                {
                    Log.Error($"CollideObjects objectB was null at {pair.B}");
                    continue;
                }
                if (!objectA.Active || !objectB.Active)
                    continue; // a collision participant already died

                // beam collision is a special case
                if (objectA.Type == GameObjectType.Beam ||
                    objectB.Type == GameObjectType.Beam)
                {
                    bool isBeamA = objectA.Type == GameObjectType.Beam;
                    int beamId = isBeamA ? pair.A : pair.B;
                    var beam = (Beam)(isBeamA ? objectA : objectB);
                    GameplayObject victim = isBeamA ? objectB : objectA;

                    AddBeamHit(beamHits, beam, victim);

                    // gather and remove all other overlaps with this beam
                    for (int j = i+1; j < collisions.Size; ++j)
                    {
                        pair = collisions.Data[j];
                        if (pair.A == beamId)
                        {
                            AddBeamHit(beamHits, beam, FrontObjects[pair.B]);
                            collisions.Data[j] = CollisionPair.Empty; // remove
                        }
                        else if (pair.B == beamId)
                        {
                            AddBeamHit(beamHits, beam, FrontObjects[pair.A]);
                            collisions.Data[j] = CollisionPair.Empty; // remove
                        }
                    }

                    if (beamHits.Count > 0)
                    {
                        // for beams, it's important to only collide the CLOSEST object
                        // so we need to sort the hits by distance
                        // and then work from closest to farthest until we get a valid collision
                        // Some missiles/projectiles have special dodge features,
                        // so we need to check all touches.
                        if (beamHits.Count > 1)
                            beamHits.Sort();

                        for (int hitIndex = 0; hitIndex < beamHits.Count; ++hitIndex)
                        {
                            BeamHitResult hit = beamHits[hitIndex];
                            if (HandleBeamCollision(beam, hit.Collided, hit.Distance))
                            {
                                ++numCollisions;
                                break; // and we're done
                            }
                        }
                        beamHits.Clear();
                    }
                }
                else if (objectA.Type == GameObjectType.Proj ||
                         objectB.Type == GameObjectType.Proj)
                {
                    bool isProjA = objectA.Type == GameObjectType.Proj;
                    var proj = (Projectile)(isProjA ? objectA : objectB);
                    GameplayObject victim = isProjA ? objectB : objectA;

                    if (HitTestProj(timeStep.FixedTime, proj, victim, out ShipModule hitModule))
                    {
                        if (proj.Touch(hitModule ?? victim))
                            ++numCollisions;
                    }
                }
            }
            return numCollisions;
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


        static void AddBeamHit(Array<BeamHitResult> beamHits, Beam beam, GameplayObject victim)
        {
            if (HitTestBeam(beam, victim, out ShipModule hitModule, out float dist))
            {
                beamHits.Add(new BeamHitResult
                {
                    Distance = dist,
                    Collided = hitModule ?? victim
                });
            }
        }

        static bool HitTestBeam(Beam beam, GameplayObject victim, out ShipModule hitModule, out float distanceToHit)
        {
            ++GlobalStats.BeamTests;

            Vector2 beamStart = beam.Source;
            Vector2 beamEnd   = beam.Destination;

            if (victim.Type == GameObjectType.Ship) // beam-ship is special collision
            {
                var ship = (Ship)victim;
                hitModule = ship.RayHitTestSingle(beamStart, beamEnd, 8f, beam.IgnoresShields);
                if (hitModule == null)
                {
                    distanceToHit = float.NaN;
                    return false;
                }
                return hitModule.RayHitTest(beamStart, beamEnd, 8f, out distanceToHit);
            }

            hitModule = null;
            if (victim.Type == GameObjectType.Proj)
            {
                var proj = (Projectile)victim;
                if (!proj.Weapon.Tag_Intercept) // for projectiles, make sure they are physical and can be killed
                {
                    distanceToHit = float.NaN;
                    return false;
                }
            }

            // intersect projectiles or anything else that can collide
            return victim.Center.RayCircleIntersect(victim.Radius, beamStart, beamEnd, out distanceToHit);
        }

        public bool HitTestProj(float simTimeStep, Projectile proj, GameplayObject victim, out ShipModule hitModule)
        {
            // NOTE: this is for Projectile<->Projectile collision!
            if (victim.Type != GameObjectType.Ship) // target not a ship, collision success
            {
                hitModule = null;
                return true;
            }

            // ship collision, target modules instead
            var ship = (Ship)victim;
            float velocity = proj.Velocity.Length();
            float maxDistPerFrame = velocity * simTimeStep;

            // if this projectile will move more than 15 units (1 module grid = 16x16) within one simulation step
            // we have to use ray-casting to avoid projectiles clipping through objects
            if (maxDistPerFrame > 15f)
            {
                Vector2 dir = proj.Velocity / velocity;
                float cx = proj.Center.X;
                float cy = proj.Center.Y;
                var prevPos = new Vector2(cx - dir.X*maxDistPerFrame, cy - dir.Y*maxDistPerFrame);
                var center = new Vector2(cx, cy);
                hitModule = ship.RayHitTestSingle(prevPos, center, proj.Radius, proj.IgnoresShields);
            }
            else
            {
                hitModule = ship.HitTestSingle(proj.Center, proj.Radius, proj.IgnoresShields);
            }
            return hitModule != null;
        }

        GameplayObject[] CopyOutput(int* objectIds, int count, GameplayObject[] objects)
        {
            if (count == 0)
                return Empty<GameplayObject>.Array;

            var found = new GameplayObject[count];
            for (int i = 0; i < found.Length; ++i)
            {
                int spatialIndex = objectIds[i];
                GameplayObject go = objects[spatialIndex];
                found[i] = go;
            }
            return found;
        }

        [UnmanagedFunctionPointer(CC)]
        delegate int SearchFilter(int objectId);

        [StructLayout(LayoutKind.Sequential)]
        struct NativeSearchOptions
        {
            public int OriginX;
            public int OriginY;
            public int SearchRadius;
            public int MaxResults;
            public int FilterByType;
            public int FilterExcludeObjectId;
            public int FilterExcludeByLoyalty;
            public int FilterIncludeOnlyByLoyalty;
            public SearchFilter FilterFunction;
            public int EnableSearchDebugId;
        };

        public GameplayObject[] FindNearby(GameObjectType type,
                                           Vector2 worldPos,
                                           float radius,
                                           int maxResults,
                                           GameplayObject toIgnore,
                                           Empire excludeLoyalty,
                                           Empire onlyLoyalty,
                                           int debugId = 0)
        {
            int ignoreId = -1;
            if (toIgnore != null && toIgnore.SpatialIndex < 0)
                ignoreId = toIgnore.SpatialIndex;

            var nso = new NativeSearchOptions
            {
                OriginX = (int)worldPos.X,
                OriginY = (int)worldPos.Y,
                SearchRadius = (int)(radius + 0.5f), // ceil
                MaxResults = maxResults,
                FilterByType = (int)type,
                FilterExcludeObjectId = ignoreId,
                FilterExcludeByLoyalty = excludeLoyalty?.Id ?? 0,
                FilterIncludeOnlyByLoyalty = onlyLoyalty?.Id ?? 0,
                FilterFunction = null,
                EnableSearchDebugId = debugId,
            };

            GameplayObject[] objects = FrontObjects.GetInternalArrayItems();
            int count = FrontObjects.Count;

            int* objectIds = stackalloc int[maxResults];
            int resultCount = SpatialFindNearby(Spat, objectIds, ref nso);
            return CopyOutput(objectIds, resultCount, objects);
        }

        public GameplayObject[] FindLinear(GameObjectType type,
                                           Vector2 worldPos,
                                           float radius,
                                           int maxResults,
                                           GameplayObject toIgnore,
                                           Empire excludeLoyalty,
                                           Empire onlyLoyalty,
                                           int debugId = 0)
        {
            float cx = worldPos.X;
            float cy = worldPos.Y;
            bool filterByLoyalty = (excludeLoyalty != null) || (onlyLoyalty != null);

            GameplayObject[] objects = FrontObjects.GetInternalArrayItems();
            int count = FrontObjects.Count;
            
            int resultCount = 0;
            int* objectIds = stackalloc int[maxResults];

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
                    objectIds[resultCount++] = obj.SpatialIndex;
                    if (resultCount >= maxResults)
                        break; // we are done !
                }
            }
            return CopyOutput(objectIds, resultCount, objects);
        }
        
        struct Point
        {
            public int X;
            public int Y;
        }
        struct Circle
        {
            public int X;
            public int Y;
            public int Radius;
        }
        struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        struct SpatialColor
        {
            public byte r, g, b, a;
        }
        [UnmanagedFunctionPointer(CC)] delegate void DrawRectF(Rect r, SpatialColor c);
        [UnmanagedFunctionPointer(CC)] delegate void DrawCircleF(Circle ci, SpatialColor c);
        [UnmanagedFunctionPointer(CC)] delegate void DrawLineF(Point a, Point b, SpatialColor c);
        [UnmanagedFunctionPointer(CC)] delegate void DrawTextF(Point p, int size, sbyte* text, SpatialColor c);
        struct QtreeVisualizerBridge
        {
            public DrawRectF   DrawRect;
            public DrawCircleF DrawCircle;
            public DrawLineF   DrawLine;
            public DrawTextF   DrawText;
        }
        struct QtreeVisualizerOptions
        {
            public Rect visibleWorldRect;
            public byte objectBounds;
            public byte objectToLeafLines;
            public byte objectText;
            public byte nodeText;
            public byte nodeBounds;
            public byte searchDebug;
            public byte searchResults;
            public byte collisions;
        }

        [DllImport(Lib)]
        static extern void SpatialDebugVisualize(IntPtr spatial, ref QtreeVisualizerOptions opt, ref QtreeVisualizerBridge vis);
        
        static GameScreen Screen;
        static void DrawRect(Rect r, SpatialColor c)
        {
            Screen.DrawRectangleProjected(new Rectangle(r.Left, r.Top, r.Right- r.Left, r.Bottom- r.Top),
                                          new Color(c.r, c.g, c.b, c.a));
        }
        static void DrawCircle(Circle ci, SpatialColor c)
        {
            Screen.DrawCircleProjected(new Vector2(ci.X, ci.Y), ci.Radius,
                                       new Color(c.r, c.g, c.b, c.a));
        }
        static void DrawLine(Point a, Point b, SpatialColor c)
        {
            Screen.DrawLineProjected(new Vector2(a.X, a.Y), new Vector2(b.X, b.Y),
                                     new Color(c.r, c.g, c.b, c.a));
        }
        static void DrawText(Point p, int size, sbyte* text, SpatialColor c)
        {
            float scale = size / 5f;
            Screen.DrawStringProjected(new Vector2(p.X, p.Y), 0f, scale,
                                       new Color(c.r, c.g, c.b, c.a), new string(text));
        }

        public void DebugVisualize(GameScreen screen)
        {
            Rectangle worldRect = screen.GetVisibleWorldRect();

            var opt = new QtreeVisualizerOptions
            {
                visibleWorldRect = new Rect
                {
                    Left = worldRect.Left,
                    Top = worldRect.Top,
                    Right = worldRect.Right,
                    Bottom = worldRect.Bottom,
                },
                objectBounds = 1,
                objectToLeafLines = 1,
                objectText = 0,
                nodeText = 0,
                nodeBounds = 1,
                searchDebug = 1,
                searchResults = 1,
                collisions = 1,
            };

            var vis = new QtreeVisualizerBridge
            {
                DrawRect = DrawRect,
                DrawCircle = DrawCircle,
                DrawLine = DrawLine,
                DrawText = DrawText,
            };

            Screen = screen;
            SpatialDebugVisualize(Spat, ref opt, ref vis);
            Screen = null;
        }
    }
}
