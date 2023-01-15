using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA1060

#pragma warning disable 0649 // uninitialized struct

namespace Ship_Game.Spatial
{
    public enum SpatialType : int
    {
        /// <summary>
        /// spatial::Grid
        /// Not that good because the universe is just too damn big
        /// </summary>
        Grid,

        /// <summary>
        /// spatial::Qtree
        /// Really good performance, and very low memory usage
        /// Only downside is C# to C++ bridge overhead. So it's a good Volkswagen.
        /// </summary>
        Qtree, 

        /// <summary>
        /// spatial::GridL2
        /// A bit trickier, gives fine grain where we really need it and leaves
        /// vast emptiness of space relatively empty
        /// </summary>
        GridL2,

        /// <summary>
        /// C# Qtree
        /// Almost identical to spatial::Qtree, ported from C++ to C#
        /// Very fast because there is no conversion layer
        /// A bit of a memory hog - needs Array allocator support
        /// </summary>
        ManagedQtree,
    };

    public sealed unsafe class NativeSpatial : ISpatial, IDisposable
    {
        const string Lib = "SDNative.dll";
        const CallingConvention CC = CallingConvention.StdCall;

        [DllImport(Lib)] static extern IntPtr SpatialCreate(SpatialType type, int worldSize, int cellSize, int cellSize2);
        [DllImport(Lib)] static extern void SpatialDestroy(IntPtr spatial);
        
        [DllImport(Lib)] static extern IntPtr SpatialGetRoot(IntPtr spatial);
        [DllImport(Lib)] static extern SpatialType SpatialGetType(IntPtr spatial);
        [DllImport(Lib)] static extern int SpatialWorldSize(IntPtr spatial);
        [DllImport(Lib)] static extern int SpatialFullSize(IntPtr spatial);
        [DllImport(Lib)] static extern int SpatialNumActive(IntPtr spatial);
        [DllImport(Lib)] static extern int SpatialMaxObjects(IntPtr spatial);

        [DllImport(Lib)] static extern void SpatialClear(IntPtr spatial);
        [DllImport(Lib)] static extern IntPtr SpatialRebuild(IntPtr spatial);

        [DllImport(Lib)] static extern int SpatialInsert(IntPtr spatial, ref NativeSpatialObject o);
        [DllImport(Lib)] static extern void SpatialUpdate(IntPtr spatial, int objectId, ref AABoundingBox2Di rect);
        [DllImport(Lib)] static extern void SpatialRemove(IntPtr spatial, int objectId);

        [DllImport(Lib)] static extern void SpatialCollideAll(IntPtr spatial, IntPtr root, ref CollisionParams param, ref CollisionPairs outResults);
        [DllImport(Lib)] static extern int SpatialFindNearby(IntPtr spatial, IntPtr root, int* outResults, ref NativeSearchOptions opt);

        IntPtr Spat; // The spatial structure interface
        IntPtr Root; // Current active Root
        SpatialObjectBase[] Objects = Empty<SpatialObjectBase>.Array;
        readonly ReaderWriterLockSlim Lock = new(LockRecursionPolicy.NoRecursion);

        public SpatialType Type { get; }
        public float WorldSize { get; }
        public float FullSize { get; }
        public int Count => SpatialNumActive(Spat);
        public int MaxObjects => SpatialMaxObjects(Spat);
        public string Name { get; }

        /// <summary>
        /// Allows to access C++ spatial containers implemented in SDNative.dll
        /// </summary>
        /// <param name="type">What type of spatial structure to create</param>
        /// <param name="worldSize">Width and Height of the game world</param>
        /// <param name="cellSize">
        /// Size of a single spatial cell. For Grid, this is the Cell Size.
        /// For QuadTree, this is the smallest possible subdivision cell size
        /// </param>
        /// <param name="cellSize2">Size of secondary cells, for example L2 Grid's second level cell size</param>
        public NativeSpatial(SpatialType type, int worldSize, int cellSize, int cellSize2 = 0)
        {
            Type = type;
            Spat = SpatialCreate(type, worldSize, cellSize, cellSize2);
            Root = SpatialGetRoot(Spat);

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
            Root = IntPtr.Zero;
            SpatialDestroy(tree);
        }

        public void Clear()
        {
            using (Lock.AcquireWriteLock())
            {
                SpatialClear(Spat);
                Root = SpatialGetRoot(Spat);
                Objects = Empty<SpatialObjectBase>.Array;
            }
        }

        public void UpdateAll(SpatialObjectBase[] allObjects)
        {
            int maxObjects = Math.Max(MaxObjects, allObjects.Length);

            var objects = new SpatialObjectBase[maxObjects];
            for (int i = 0; i < allObjects.Length; ++i)
            {
                SpatialObjectBase go = allObjects[i];
                int objectId = go.SpatialIndex;
                if (go.Active)
                {
                    if (go.ReinsertSpatial) // if marked for reinsert, remove from Spat
                    {
                        go.ReinsertSpatial = false;
                        if (objectId != -1)
                        {
                            SpatialRemove(Spat, objectId);
                            go.SpatialIndex = -1;
                            objectId = -1;
                        }
                    }

                    if (objectId == -1) // insert new
                    {
                        var so = new NativeSpatialObject(go);
                        objectId = SpatialInsert(Spat, ref so);
                        go.SpatialIndex = objectId;
                    }
                    else // update existing
                    {
                        var rect = new AABoundingBox2Di(go);
                        SpatialUpdate(Spat, objectId, ref rect);
                    }
                    objects[objectId] = go;
                }
                else if (objectId != -1)
                {
                    SpatialRemove(Spat, objectId);
                    go.SpatialIndex = -1;
                    objects[objectId] = go;
                }
            }

            // we need to lock down the entire structure while updating
            using (Lock.AcquireWriteLock())
            {
                Root = SpatialRebuild(Spat);
                Objects = objects;
            }
        }

        (SpatialObjectBase[], IntPtr) GetObjectsAndRootSafe()
        {
            using (Lock.AcquireReadLock())
                return (Objects, Root);
        }

        public int CollideAll(FixedSimTime timeStep, bool showCollisions)
        {
            var p = new CollisionParams
            {
                IgnoreSameLoyalty = 1,
                SortCollisionsById = 0,
                ShowCollisions = (byte)(showCollisions ? 1 : 0),
            };
            
            SpatialObjectBase[] objects = Objects;
            IntPtr root = Root;
            //(GameplayObject[] objects, IntPtr root) = GetObjectsAndRootSafe();

            // get the collisions
            CollisionPairs results = default;
            SpatialCollideAll(Spat, root, ref p, ref results);
            
            int numCollisions = NarrowPhase.Collide(timeStep, results.Data, results.Size, objects);
            return numCollisions;
        }

        public SpatialObjectBase[] FindNearby(in SearchOptions opt)
        {
            if (opt.MaxResults == 0)
                return Empty<SpatialObjectBase>.Array;

            int ignoreId = -1;
            if (opt.Exclude != null && opt.Exclude.SpatialIndex >= 0)
                ignoreId = opt.Exclude.SpatialIndex;

            var nso = new NativeSearchOptions
            {
                SearchRect = new AABoundingBox2Di(opt.SearchRect),
                RadialFilter = new Circle
                {
                    X=(int)opt.FilterOrigin.X,
                    Y=(int)opt.FilterOrigin.Y,
                    Radius=(int)(opt.FilterRadius + 0.5f) // ceil
                },
                MaxResults = opt.MaxResults,
                SortByDistance = opt.SortByDistance ? 1 : 0,
                Type = (int)opt.Type,
                ExcludeObjectId = ignoreId,
                ExcludeLoyalty = opt.ExcludeLoyalty?.Id ?? 0,
                IncludeLoyalty = opt.OnlyLoyalty?.Id ?? 0,
                FilterFunction = null,
                DebugId = opt.DebugId,
            };
            
            (SpatialObjectBase[] objects, IntPtr root) = GetObjectsAndRootSafe();

            if (opt.FilterFunction != null)
            {
                SearchFilterFunc filterFunc = opt.FilterFunction;
                nso.FilterFunction = (int objectId) =>
                {
                    SpatialObjectBase go = objects[objectId];
                    bool success = filterFunc(go);
                    return success ? 1 : 0;
                };
            }

            int* objectIds = stackalloc int[opt.MaxResults];
            int resultCount = SpatialFindNearby(Spat, root, objectIds, ref nso);
            return LinearSearch.Copy(objectIds, resultCount, objects);
        }

        public SpatialObjectBase[] FindLinear(in SearchOptions opt)
        {
            SpatialObjectBase[] objects = Objects;
            return LinearSearch.FindNearby(in opt, objects, objects.Length);
        }
        
        [StructLayout(LayoutKind.Sequential)]
        struct CollisionParams
        {
            public byte IgnoreSameLoyalty; // if 1, same loyalty objects don't collide
            public byte SortCollisionsById; // if 1, collision results are sorted by object Id-s, ascending
            public byte ShowCollisions; // if 1, collisions are shown as debug
        }
        public struct CollisionPairs
        {
            public CollisionPair* Data;
            public int Size;
            public int Capacity;
        }
        
        [UnmanagedFunctionPointer(CC)]
        delegate int SearchFilter(int objectId);

        [StructLayout(LayoutKind.Sequential)]
        struct NativeSearchOptions
        {
            public AABoundingBox2Di SearchRect;
            public Circle RadialFilter;
            public int MaxResults;
            public int SortByDistance;
            public int Type;
            public int ExcludeObjectId;
            public int ExcludeLoyalty;
            public int IncludeLoyalty;
            public SearchFilter FilterFunction;
            public int DebugId;
        };

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

        struct SpatialColor
        {
            public byte r, g, b, a;
        }
        [UnmanagedFunctionPointer(CC)] delegate void DrawRectF(AABoundingBox2Di r, SpatialColor c);
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
        struct NativeVisOptions
        {
            public AABoundingBox2Di VisibleWorldRect;
            public byte ObjectBounds;
            public byte ObjectToLeafLines;
            public byte ObjectText;
            public byte NodeText;
            public byte NodeBounds;
            public byte SearchDebug;
            public byte SearchResults;
            public byte Collisions;
        }

        [DllImport(Lib)]
        static extern void SpatialDebugVisualize(IntPtr spatial, IntPtr root, ref NativeVisOptions opt, ref QtreeVisualizerBridge vis);
        
        static GameScreen Screen;
        static void DrawRect(AABoundingBox2Di r, SpatialColor c)
        {
            Screen.DrawRectProjected(r, new Color(c.r, c.g, c.b, c.a));
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

        public void DebugVisualize(GameScreen screen, VisualizerOptions opt)
        {
            bool enabled = opt.Enabled;
            var nativeOpt = new NativeVisOptions
            {
                VisibleWorldRect  = new AABoundingBox2Di(screen.VisibleWorldRect),
                ObjectBounds      = (byte)(enabled & opt.ObjectBounds?1:0),
                ObjectToLeafLines = (byte)(enabled & opt.ObjectToLeaf?1:0),
                ObjectText    = (byte)(enabled & opt.ObjectText?1:0),
                NodeText      = (byte)(enabled & opt.NodeText?1:0),
                NodeBounds    = (byte)(enabled & opt.NodeBounds?1:0),
                SearchDebug   = (byte)(enabled & opt.SearchDebug?1:0),
                SearchResults = (byte)(enabled & opt.SearchResults?1:0),
                Collisions    = (byte)(enabled & opt.Collisions?1:0),
            };

            var vis = new QtreeVisualizerBridge
            {
                DrawRect = DrawRect,
                DrawCircle = DrawCircle,
                DrawLine = DrawLine,
                DrawText = DrawText,
            };

            Screen = screen;
            SpatialDebugVisualize(Spat, Root, ref nativeOpt, ref vis);
            Screen = null;
        }
    }
}
