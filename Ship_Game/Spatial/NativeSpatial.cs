using Microsoft.Xna.Framework;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
#pragma warning disable 0649 // uninitialized struct

namespace Ship_Game.Spatial
{
    public enum SpatialType : int
    {
        Grid, // spatial::Grid
        QuadTree, // spatial::QuadTree
    };

    public sealed unsafe class NativeSpatial : ISpatial, IDisposable
    {
        const string Lib = "SDNative.dll";
        const CallingConvention CC = CallingConvention.Cdecl;

        [DllImport(Lib, CallingConvention=CC)]
        static extern IntPtr SpatialCreate(SpatialType type, int worldSize, int cellSize);
        [DllImport(Lib, CallingConvention=CC)]
        static extern void SpatialDestroy(IntPtr spatial);
        
        [DllImport(Lib, CallingConvention=CC)]
        static extern SpatialType SpatialGetType(IntPtr spatial);
        [DllImport(Lib, CallingConvention=CC)]
        static extern int SpatialWorldSize(IntPtr spatial);
        [DllImport(Lib, CallingConvention=CC)]
        static extern int SpatialFullSize(IntPtr spatial);
        [DllImport(Lib, CallingConvention=CC)]
        static extern int SpatialCount(IntPtr spatial);
        
        [DllImport(Lib, CallingConvention=CC)]
        static extern void SpatialClear(IntPtr spatial);
        [DllImport(Lib, CallingConvention=CC)]
        static extern void SpatialRebuild(IntPtr spatial);

        [DllImport(Lib, CallingConvention=CC)]
        static extern int SpatialInsert(IntPtr spatial, ref NativeSpatialObject o);
        [DllImport(Lib, CallingConvention=CC)]
        static extern void SpatialUpdate(IntPtr spatial, int objectId, int x, int y);
        [DllImport(Lib, CallingConvention=CC)]
        static extern void SpatialRemove(IntPtr spatial, int objectId);
        
        enum CollisionResult : int
        {
            NoSideEffects, // no visible side effect from collision (both objects still alive)
            ObjectAKilled, // objects collided and objectA was killed (no further collision possible)
            ObjectBKilled, // objects collided and objectB was killed (no further collision possible)
            BothKilled,    // both objects were killed during collision (no further collision possible)
        }

        [UnmanagedFunctionPointer(CC)]
        delegate CollisionResult CollisionF(IntPtr voidPtr, int objectA, int objectB);
        
        [DllImport(Lib, CallingConvention=CC)]
        static extern void SpatialCollideAll(IntPtr spatial, float timeStep, IntPtr voidPtr, CollisionF onCollide);
        
        [DllImport(Lib, CallingConvention=CC)]
        static extern int SpatialFindNearby(IntPtr spatial, int* outResults, ref NativeSearchOptions opt);

        IntPtr Spat;
        readonly Array<GameplayObject> ObjectFlatMap = new Array<GameplayObject>(capacity:512);

        public SpatialType Type => SpatialGetType(Spat);
        public float UniverseSize => SpatialWorldSize(Spat);
        public float FullSize => SpatialFullSize(Spat);
        public int Count => SpatialCount(Spat);

        public NativeSpatial(SpatialType type, int universeSize, int smallestCell = 1024)
        {
            Spat = SpatialCreate(type, universeSize, smallestCell);
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

        public void Reset()
        {
            SpatialClear(Spat);
        }

        static bool IsObjectDead(GameplayObject go)
        {
            // this is related to QuadTree fast-removal
            return !go.Active || (go.Type == GameObjectType.Proj && ((Projectile)go).DieNextFrame);
        }

        public void Insert(GameplayObject go)
        {
            if (IsObjectDead(go))
                return;

            var so = new NativeSpatialObject(go);
            int objectId = SpatialInsert(Spat, ref so);
            go.SpatialIndex = objectId;

            if (ObjectFlatMap.Count <= objectId)
            {
                ObjectFlatMap.Resize(objectId+1);
            }
        }

        public void Remove(GameplayObject go)
        {
            int objectId = go.SpatialIndex;
            if (objectId != -1)
            {
                go.SpatialIndex = -1;
                ObjectFlatMap[objectId] = null;
                SpatialRemove(Spat, objectId);
            }
        }

        public void UpdateAll()
        {
            SpatialRebuild(Spat);
        }

        CollisionResult OnCollision(IntPtr voidPtr, int objectA, int objectB)
        {
            GameplayObject go1 = ObjectFlatMap[objectA];
            GameplayObject go2 = ObjectFlatMap[objectB];

            return CollisionResult.NoSideEffects;
        }

        public void CollideAll(FixedSimTime timeStep)
        {
            SpatialCollideAll(Spat, timeStep.FixedTime, IntPtr.Zero, OnCollision);
        }

        public void CollideAllRecursive(FixedSimTime timeStep)
        {
            SpatialCollideAll(Spat, timeStep.FixedTime, IntPtr.Zero, OnCollision);
        }

        GameplayObject[] CopyOutput(int* objectIds, int count)
        {
            if (count == 0)
                return Empty<GameplayObject>.Array;

            GameplayObject[] objects = ObjectFlatMap.GetInternalArrayItems();
            var found = new GameplayObject[count];
            for (int i = 0; i < found.Length; ++i)
            {
                int spatialIndex = objectIds[i];
                GameplayObject go = objects[spatialIndex];
                if (go == null)
                {
                    Log.Warning($"FindNearby ObjectId points to null at:{spatialIndex} - this is a threading issue!");
                }
                else if (go.SpatialIndex == spatialIndex)
                {
                    found[i] = go;
                }
                else
                {
                    Log.Error($"FindNearby returned invalid ObjectId:{spatialIndex}\n"
                              +$"Does not match expected:{go.SpatialIndex}\nFor {go}");
                }
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
        };

        public GameplayObject[] FindNearby(GameObjectType type,
                                           Vector2 worldPos,
                                           float radius,
                                           int maxResults,
                                           GameplayObject toIgnore,
                                           Empire excludeLoyalty,
                                           Empire onlyLoyalty)
        {
            int ignoreId = -1;
            if (toIgnore != null && toIgnore.SpatialIndex < 0)
                ignoreId = toIgnore.SpatialIndex;

            var nso = new NativeSearchOptions
            {
                OriginX = (int)worldPos.X,
                OriginY = (int)worldPos.Y,
                SearchRadius = (int)radius,
                MaxResults = maxResults,
                FilterByType = (int)type,
                FilterExcludeObjectId = ignoreId,
                FilterExcludeByLoyalty = excludeLoyalty?.Id ?? 0,
                FilterIncludeOnlyByLoyalty = onlyLoyalty?.Id ?? 0
            };

            int* objectIds = stackalloc int[maxResults];
            int resultCount = SpatialFindNearby(Spat, objectIds, ref nso);
            return CopyOutput(objectIds, resultCount);
        }

        public GameplayObject[] FindLinear(GameObjectType type,
                                           Vector2 worldPos,
                                           float radius,
                                           int maxResults,
                                           GameplayObject toIgnore,
                                           Empire excludeLoyalty,
                                           Empire onlyLoyalty)
        {
            float cx = worldPos.X;
            float cy = worldPos.Y;
            bool filterByLoyalty = (excludeLoyalty != null) || (onlyLoyalty != null);

            GameplayObject[] objects = ObjectFlatMap.GetInternalArrayItems();
            int count = ObjectFlatMap.Count;
            
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

            return CopyOutput(objectIds, resultCount);
        }
        

        struct SpatialColor { public byte r, g, b, a; }
        [UnmanagedFunctionPointer(CC)] delegate void DrawRectF(int x1, int y1, int x2, int y2, SpatialColor c);
        [UnmanagedFunctionPointer(CC)] delegate void DrawCircleF(int x, int y, int radius, SpatialColor c);
        [UnmanagedFunctionPointer(CC)] delegate void DrawLineF(int x1, int y1, int x2, int y2, SpatialColor c);
        [UnmanagedFunctionPointer(CC)] delegate void DrawTextF(int x, int y, int size, sbyte* text, SpatialColor c);
        struct QtreeVisualizerBridge
        {
            public DrawRectF   DrawRect;
            public DrawCircleF DrawCircle;
            public DrawLineF   DrawLine;
            public DrawTextF   DrawText;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct NativeSpatialRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        struct QtreeVisualizerOptions
        {
            public NativeSpatialRect visibleWorldRect;
            public byte objectBounds;
            public byte objectToLeafLines;
            public byte objectText;
            public byte nodeText;
            public byte nodeBounds;
        }

        [DllImport(Lib, CallingConvention=CC)]
        static extern void SpatialDebugVisualize(IntPtr spatial, ref QtreeVisualizerOptions opt, ref QtreeVisualizerBridge vis);
        
        static GameScreen Screen;
        static void DrawRect(int x1, int y1, int x2, int y2, SpatialColor c)
        {
            Screen.DrawLineProjected(new Vector2(x1,y1), new Vector2(x2,y2), new Color(c.r,c.g,c.b,c.a));
        }
        static void DrawCircle(int x, int y, int radius, SpatialColor c)
        {
            Screen.DrawCircleProjected(new Vector2(x,y), radius, new Color(c.r,c.g,c.b,c.a));
        }
        static void DrawLine(int x1, int y1, int x2, int y2, SpatialColor c)
        {
            Screen.DrawLineProjected(new Vector2(x1,y1), new Vector2(x2,y2), new Color(c.r,c.g,c.b,c.a));
        }
        static void DrawText(int x, int y, int size, sbyte* text, SpatialColor c)
        {
            float scale = size / 5f;
            Screen.DrawStringProjected(new Vector2(x,y), 0f, scale, new Color(c.r,c.g,c.b,c.a), new string(text));
        }

        public void DebugVisualize(GameScreen screen)
        {
            var screenSize = new Vector2(screen.Viewport.Width, screen.Viewport.Height);
            Vector2 topLeft  = screen.UnprojectToWorldPosition(Vector2.Zero);
            Vector2 botRight = screen.UnprojectToWorldPosition(screenSize);

            var opt = new QtreeVisualizerOptions
            {
                visibleWorldRect = new NativeSpatialRect
                {
                    Left = (int)topLeft.X,
                    Top = (int)topLeft.Y,
                    Right = (int)botRight.X,
                    Bottom = (int)botRight.Y,
                },
                objectBounds = 1,
                objectToLeafLines = 1,
                objectText = 0,
                nodeText = 0,
                nodeBounds = 1,
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
