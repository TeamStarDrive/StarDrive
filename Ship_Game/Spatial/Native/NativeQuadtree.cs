using Microsoft.Xna.Framework;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
#pragma warning disable 0649 // uninitialized struct

namespace Ship_Game.Spatial.Native
{
    public delegate bool CollisionDelegate(int objectA, int objectB);

    public sealed unsafe class NativeQuadtree : IQuadtree, IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        struct NativeQtree
        {
            public int Levels;
            public int FullSize;
            public int UniverseSize;
        }

        const string Lib = "SDNative.dll";

        [DllImport(Lib)] static extern NativeQtree* QtreeCreate(int universeSize, int smallestCell);
        [DllImport(Lib)] static extern void QtreeDestroy(NativeQtree* tree);
        
        [DllImport(Lib)] static extern NativeQtreeNode* QtreeClear(NativeQtree* tree);
        [DllImport(Lib)] static extern NativeQtreeNode* QtreeRebuild(NativeQtree* tree);
        [DllImport(Lib)] static extern NativeQtreeNode* QtreeRebuildObjects(NativeQtree* tree,
                                                                   NativeQtreeObject* objects, int numObjects);
        
        [DllImport(Lib)] static extern int QtreeInsert(NativeQtree* tree, ref NativeQtreeObject o);
        [DllImport(Lib)] static extern void QtreeUpdate(NativeQtree* tree, int objectId, int x, int y);
        [DllImport(Lib)] static extern void QtreeRemove(NativeQtree* tree, int objectId);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int CollisionF(int objectA, int objectB);
        [DllImport(Lib)] static extern void QtreeCollideAll(NativeQtree* tree, float timeStep, CollisionF onCollide);
        [DllImport(Lib)] static extern int QtreeFindNearby(NativeQtree* tree, int* outResults, ref NativeSearchOptions opt);

        NativeQtree* Tree;
        readonly Array<GameplayObject> Pending = new Array<GameplayObject>();
        readonly Array<GameplayObject> Objects = new Array<GameplayObject>();

        public float UniverseSize => Tree->UniverseSize;
        public float FullSize => Tree->FullSize;
        public int Levels => Tree->Levels;

        /// <summary>
        /// Number of pending and active objects in the Quadtree
        /// </summary>
        public int Count => Pending.Count + Objects.Count;

        public NativeQuadtree(int universeSize, int smallestCell = 512)
        {
            Tree = QtreeCreate(universeSize, smallestCell);
        }

        ~NativeQuadtree()
        {
            QtreeDestroy(Tree);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            NativeQtree* tree = Tree;
            Tree = null;
            QtreeDestroy(tree);
        }

        public void Reset()
        {
            QtreeClear(Tree);
        }

        static bool IsObjectDead(GameplayObject go)
        {
            // this is related to QuadTree fast-removal
            return !go.Active || (go.Type == GameObjectType.Proj && ((Projectile)go).DieNextFrame);
        }
        static bool IsObjectDead(Projectile proj)
        {
            return !proj.Active || proj.DieNextFrame;
        }

        public void Insert(GameplayObject go)
        {
            if (IsObjectDead(go))
                return;

            // this can be called from UI Thread, so we'll insert it later during Update()
            lock (Pending)
            {
                Pending.Add(go);
                go.SpatialIndex = -2;
            }
        }

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
                int objectId = go.SpatialIndex;
                Objects[objectId] = null;
                go.SpatialIndex = -1;
                QtreeRemove(Tree, objectId);
            }
        }

        void InsertPending()
        {
            lock (Pending)
            {
                for (int i = 0; i < Pending.Count; ++i)
                {
                    GameplayObject go = Pending[i];
                    // NOTE: This happens sometimes with beam weapons. Seems like a bug
                    if (IsObjectDead(go))
                    {
                        Log.Warning($"Quadtree.InsertPending object has died while pending: {go}");
                    }
                    else
                    {
                        go.SpatialIndex = Objects.Count;
                        Objects.Add(go);
                    }
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
                GameplayObject go = objects[i];
                if (go != null)
                {
                    // NOTE: this is very common, we have dead projectiles still in the objects list
                    //       (which died last frame)
                    if (IsObjectDead(go))
                    {
                        go.SpatialIndex = -1;
                        Objects.RemoveAtSwapLast(i--);
                    }
                    else
                    {
                        go.SpatialIndex = i;
                    }
                }
                else // empty slot
                {
                    Objects.RemoveAtSwapLast(i--);
                }
            }
        }

        public void UpdateAll()
        {
            RemoveEmptySpots();
            InsertPending();

            NativeQtreeObject* objects = stackalloc NativeQtreeObject[Objects.Count];
            for (int i = 0; i < Objects.Count; ++i)
            {
                objects[i] = new NativeQtreeObject(Objects[i], i);
            }
            QtreeRebuildObjects(Tree, objects, Objects.Count);
        }

        int OnCollision(int objectA, int objectB)
        {
            GameplayObject go1 = Objects[objectA];
            GameplayObject go2 = Objects[objectB];
            return 0;
        }

        public void CollideAll(FixedSimTime timeStep)
        {
            QtreeCollideAll(Tree, timeStep.FixedTime, OnCollision);
        }

        public void CollideAllRecursive(FixedSimTime timeStep)
        {
            QtreeCollideAll(Tree, timeStep.FixedTime, OnCollision);
        }

        static GameplayObject[] CopyOutput(GameplayObject[] objects, int* objectIds, int count)
        {
            var found = new GameplayObject[count];
            for (int i = 0; i < found.Length; ++i)
            {
                int spatialIndex = objectIds[i];
                GameplayObject go = objects[spatialIndex];
                if (go.SpatialIndex == spatialIndex)
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
            int count = QtreeFindNearby(Tree, objectIds, ref nso);
            if (count != 0)
                return CopyOutput(Objects.GetInternalArrayItems(), objectIds, count);
            return Empty<GameplayObject>.Array;
        }

        class FindResultBuffer
        {
            public int Count = 0;
            public GameplayObject[] Items = new GameplayObject[128];
            public GameplayObject[] GetArrayAndClearBuffer()
            {
                int count = Count;
                if (count == 0)
                    return Empty<GameplayObject>.Array;

                Count = 0;
                var arr = new GameplayObject[count];
                Memory.HybridCopy(arr, 0, Items, count);
                Array.Clear(Items, 0, count);
                return arr;
            }
        }

        readonly ThreadLocal<FindResultBuffer> FindBuffer
           = new ThreadLocal<FindResultBuffer>(() => new FindResultBuffer());

        public GameplayObject[] FindLinear(GameObjectType type,
                                           Vector2 worldPos,
                                           float radius,
                                           int maxResults,
                                           GameplayObject toIgnore,
                                           Empire excludeLoyalty,
                                           Empire onlyLoyalty)
        {
            FindResultBuffer nearby = FindBuffer.Value;
            if (nearby.Items.Length < maxResults)
            {
                nearby.Items = new GameplayObject[maxResults];
            }
            
            float cx = worldPos.X;
            float cy = worldPos.Y;
            bool filterByLoyalty = (excludeLoyalty != null) || (onlyLoyalty != null);

            GameplayObject[] objects = Objects.GetInternalArrayItems();
            int count = Objects.Count;
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
                    nearby.Items[nearby.Count++] = obj;
                    if (nearby.Count >= maxResults)
                        break; // we are done !
                }
            }

            return nearby.GetArrayAndClearBuffer();
        }
        
        struct QtreeColor { public byte r, g, b, a; }
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void DrawRectF(int x1, int y1, int x2, int y2, QtreeColor c);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void DrawCircleF(int x, int y, int radius, QtreeColor c);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void DrawLineF(int x1, int y1, int x2, int y2, QtreeColor c);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void DrawTextF(int x, int y, int size, sbyte* text, QtreeColor c);
        struct QtreeVisualizerBridge
        {
            public DrawRectF   DrawRect;
            public DrawCircleF DrawCircle;
            public DrawLineF   DrawLine;
            public DrawTextF   DrawText;
        }
        [DllImport(Lib)] static extern void QtreeDebugVisualize(NativeQtree* tree,
                                                NativeQtreeRect visible, ref QtreeVisualizerBridge vis);

        static GameScreen Screen;

        static void DrawRect(int x1, int y1, int x2, int y2, QtreeColor c)
        {
            Screen.DrawLineProjected(new Vector2(x1,y1), new Vector2(x2,y2), new Color(c.r,c.g,c.b,c.a));
        }
        static void DrawCircle(int x, int y, int radius, QtreeColor c)
        {
            Screen.DrawCircleProjected(new Vector2(x,y), radius, new Color(c.r,c.g,c.b,c.a));
        }
        static void DrawLine(int x1, int y1, int x2, int y2, QtreeColor c)
        {
            Screen.DrawLineProjected(new Vector2(x1,y1), new Vector2(x2,y2), new Color(c.r,c.g,c.b,c.a));
        }
        static void DrawText(int x, int y, int size, sbyte* text, QtreeColor c)
        {
            float scale = size / 5f;
            Screen.DrawStringProjected(new Vector2(x,y), 0f, scale, new Color(c.r,c.g,c.b,c.a), new string(text));
        }

        public void DebugVisualize(GameScreen screen)
        {
            var screenSize = new Vector2(screen.Viewport.Width, screen.Viewport.Height);
            Vector2 topLeft  = screen.UnprojectToWorldPosition(Vector2.Zero);
            Vector2 botRight = screen.UnprojectToWorldPosition(screenSize);

            var worldRect = new NativeQtreeRect
            {
                Left = (int)topLeft.X,
                Top = (int)topLeft.Y,
                Right = (int)botRight.X,
                Bottom = (int)botRight.Y,
            };

            var vis = new QtreeVisualizerBridge
            {
                DrawRect = DrawRect,
                DrawCircle = DrawCircle,
                DrawLine = DrawLine,
                DrawText = DrawText,
            };

            Screen = screen;
            QtreeDebugVisualize(Tree, worldRect, ref vis);
            Screen = null;
        }
    }
}
