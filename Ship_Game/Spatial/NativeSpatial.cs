using Microsoft.Xna.Framework;
using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

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
        
        [DllImport(Lib)] static extern SpatialType SpatialGetType(IntPtr spatial);
        [DllImport(Lib)] static extern int SpatialWorldSize(IntPtr spatial);
        [DllImport(Lib)] static extern int SpatialFullSize(IntPtr spatial);
        [DllImport(Lib)] static extern int SpatialNumActive(IntPtr spatial);
        [DllImport(Lib)] static extern int SpatialMaxObjects(IntPtr spatial);

        [DllImport(Lib)] static extern void SpatialClear(IntPtr spatial);
        [DllImport(Lib)] static extern void SpatialRebuild(IntPtr spatial);

        [DllImport(Lib)] static extern int SpatialInsert(IntPtr spatial, ref NativeSpatialObject o);
        [DllImport(Lib)] static extern void SpatialUpdate(IntPtr spatial, int objectId, ref AABoundingBox2Di rect);
        [DllImport(Lib)] static extern void SpatialRemove(IntPtr spatial, int objectId);

        [DllImport(Lib)] static extern void SpatialCollideAll(IntPtr spatial, ref CollisionParams param, ref CollisionPairs outResults);
        [DllImport(Lib)] static extern int SpatialFindNearby(IntPtr spatial, int* outResults, ref NativeSearchOptions opt);

        IntPtr Spat;
        readonly Array<GameplayObject> Objects = new Array<GameplayObject>(capacity:512);

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
        /// <param name="cellSize2">Size of secondary cells, for example L2 Grid's second level cell size</param>
        public NativeSpatial(SpatialType type, int worldSize, int cellSize, int cellSize2 = 0)
        {
            Type = type;
            Spat = SpatialCreate(type, worldSize, cellSize, cellSize2);

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
            lock (Objects)
            {
                SpatialClear(Spat);
                Objects.Clear();
            }
        }

        public void UpdateAll(Array<GameplayObject> allObjects)
        {
            int count = allObjects.Count;
            int maxObjects = Math.Max(MaxObjects, count);

            lock (Objects)
            {
                Objects.Resize(maxObjects);
                GameplayObject[] gameObjects = allObjects.GetInternalArrayItems();
                for (int i = 0; i < count; ++i)
                {
                    GameplayObject go = gameObjects[i];
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
                            Objects[objectId] = go;
                        }
                        else // update existing
                        {
                            var rect = new AABoundingBox2Di(go);
                            SpatialUpdate(Spat, objectId, ref rect);
                            Objects[objectId] = go;
                        }
                    }
                    else if (objectId != -1)
                    {
                        Objects[objectId] = null;
                        SpatialRemove(Spat, objectId);
                        go.SpatialIndex = -1;
                    }
                }
                SpatialRebuild(Spat);
            }
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
        public int CollideAll(FixedSimTime timeStep)
        {
            var p = new CollisionParams
            {
                IgnoreSameLoyalty = 1,
                SortCollisionsById = 0,
                ShowCollisions = (byte)(Empire.Universe?.Debug == true ? 1 : 0),
            };

            // get the collisions
            CollisionPairs results = default;
            SpatialCollideAll(Spat, ref p, ref results);
            
            int numCollisions = NarrowPhase.Collide(timeStep, results.Data, results.Size, Objects);
            return numCollisions;
        }

        [UnmanagedFunctionPointer(CC)]
        delegate int SearchFilter(int objectId);

        [StructLayout(LayoutKind.Sequential)]
        struct NativeSearchOptions
        {
            public AABoundingBox2Di SearchRect;
            public Circle RadialFilter;
            public int MaxResults;
            public int FilterByType;
            public int FilterExcludeObjectId;
            public int FilterExcludeByLoyalty;
            public int FilterIncludeOnlyByLoyalty;
            public SearchFilter FilterFunction;
            public int EnableSearchDebugId;
        };

        public GameplayObject[] FindNearby(in SearchOptions opt)
        {
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
                FilterByType = (int)opt.Type,
                FilterExcludeObjectId = ignoreId,
                FilterExcludeByLoyalty = opt.ExcludeLoyalty?.Id ?? 0,
                FilterIncludeOnlyByLoyalty = opt.OnlyLoyalty?.Id ?? 0,
                FilterFunction = null,
                EnableSearchDebugId = opt.DebugId,
            };
            
            lock (Objects)
            {
                if (opt.FilterFunction != null)
                {
                    SearchFilterFunc filterFunc = opt.FilterFunction;
                    nso.FilterFunction = (int objectId) =>
                    {
                        GameplayObject go = Objects[objectId];
                        bool success = filterFunc(go);
                        return success ? 1 : 0;
                    };
                }

                int* objectIds = stackalloc int[opt.MaxResults];
                GameplayObject[] objects = Objects.GetInternalArrayItems();
                int resultCount = SpatialFindNearby(Spat, objectIds, ref nso);
                return LinearSearch.Copy(objectIds, resultCount, objects);
            }
        }

        public GameplayObject[] FindLinear(in SearchOptions opt)
        {
            lock (Objects)
            {
                GameplayObject[] objects = Objects.GetInternalArrayItems();
                int count = Objects.Count;
                return LinearSearch.FindNearby(opt, objects, count);
            }
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
        static extern void SpatialDebugVisualize(IntPtr spatial, ref NativeVisOptions opt, ref QtreeVisualizerBridge vis);
        
        static GameScreen Screen;
        static void DrawRect(AABoundingBox2Di r, SpatialColor c)
        {
            Screen.DrawRectangleProjected(new Rectangle(r.X1, r.Y1, r.Width, r.Height),
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

        public void DebugVisualize(GameScreen screen, VisualizerOptions opt)
        {
            bool enabled = opt.Enabled;
            var nativeOpt = new NativeVisOptions
            {
                VisibleWorldRect  = new AABoundingBox2Di(screen.GetVisibleWorldRect()),
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
            SpatialDebugVisualize(Spat, ref nativeOpt, ref vis);
            Screen = null;
        }
    }
}
