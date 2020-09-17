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
            public NativeQtreeNode* Root;
        }

        [DllImport("SDNative.dll")]
        static extern NativeQtree* QtreeCreate(int universeSize, int smallestCell);
        
        [DllImport("SDNative.dll")]
        static extern void QtreeDestroy(NativeQtree* tree);
        
        [DllImport("SDNative.dll")]
        static extern NativeQtreeNode* QtreeCreateRoot(NativeQtree* tree);
        
        [DllImport("SDNative.dll")]
        static extern void QtreeInsert(NativeQtree* tree, NativeQtreeNode* root, ref NativeSpatialObj so);
        
        [DllImport("SDNative.dll")]
        static extern void QtreeRemoveAt(NativeQtree* tree, NativeQtreeNode* node, int objectId);

        [DllImport("SDNative.dll")]
        static extern void QtreeCollideAll(NativeQtree* tree, float timeStep, IntPtr onCollide);
        
        [DllImport("SDNative.dll")]
        static extern void QtreeCollideAllRecursive(NativeQtree* tree, float timeStep, IntPtr onCollide);

        [DllImport("SDNative.dll")]
        static extern int QtreeFindNearby(NativeQtree* tree, int* outResults, ref NativeSearchOptions opt);

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
            Reset();
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
            Tree->Root = QtreeCreateRoot(Tree);
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
                QtreeRemoveAt(Tree, Tree->Root, objectId);
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

            NativeQtreeNode* newRoot = QtreeCreateRoot(Tree);

            for (int i = 0; i < Objects.Count; ++i)
            {
                var obj = new NativeSpatialObj(Objects[i], i);
                QtreeInsert(Tree, newRoot, ref obj);
            }

            Tree->Root = newRoot;
        }

        bool OnCollision(int objectA, int objectB)
        {
            GameplayObject go1 = Objects[objectA];
            GameplayObject go2 = Objects[objectB];
            return false;
        }

        IntPtr CollisionFunc;
        IntPtr OnCollisionFunc
        {
            get
            {
                if (CollisionFunc == IntPtr.Zero)
                {
                    var d = new CollisionDelegate(OnCollision);
                    CollisionFunc = Marshal.GetFunctionPointerForDelegate(d);
                }
                return CollisionFunc;
            }
        }

        public void CollideAll(FixedSimTime timeStep)
        {
            QtreeCollideAll(Tree, timeStep.FixedTime, OnCollisionFunc);
        }

        public void CollideAllRecursive(FixedSimTime timeStep)
        {
            QtreeCollideAllRecursive(Tree, timeStep.FixedTime, OnCollisionFunc);
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

        class TraversalBuffer
        {
            public int Next = 0; // next node to pop
            public NativeQtreeNode*[] Stack = new NativeQtreeNode*[512];
            public NativeQtreeNode* Pop()
            {
                NativeQtreeNode* node = Stack[Next];
                Stack[Next] = default; // don't leak refs
                --Next;
                return node;
            }
        }

        readonly ThreadLocal<FindResultBuffer> FindBuffer
           = new ThreadLocal<FindResultBuffer>(() => new FindResultBuffer());

        readonly ThreadLocal<TraversalBuffer> TraverseBuffer
           = new ThreadLocal<TraversalBuffer>(() => new TraversalBuffer());

        TraversalBuffer GetThreadLocalTraversalBuffer(NativeQtreeNode* root)
        {
            TraversalBuffer buffer = TraverseBuffer.Value;
            buffer.Next = 0;
            buffer.Stack[0] = root;
            return buffer;
        }

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
        
        static NativeSpatialObj[] DebugDrawBuffer = Empty<NativeSpatialObj>.Array;

        static readonly Color Brown = new Color(Color.SaddleBrown, 150);
        // "Allies are Blue, Enemies are Red, what should I do, with our Quadtree?" - RedFox
        static readonly Color Violet = new Color(Color.MediumVioletRed, 100);
        static readonly Color Blue = new Color(Color.CadetBlue, 100);
        static readonly Color Red = new Color(Color.OrangeRed, 100);
        static readonly Color Yellow = new Color(Color.Yellow, 100);

        void DebugVisualize(GameScreen screen, in Vector2 topLeft, in Vector2 botRight, NativeQtreeNode* root)
        {
            TraversalBuffer buffer = GetThreadLocalTraversalBuffer(root);

            do
            {
                NativeQtreeNode* node = buffer.Pop();
                
                var center = new Vector2((node->X + node->LastX) / 2, (node->Y + node->LastY) / 2);
                var size = new Vector2(node->LastX - node->X, node->LastY - node->Y);
                screen.DrawRectangleProjected(center, size, 0f, Brown);

                // @todo This is a hack to reduce concurrency related bugs.
                //       once the main drawing and simulation loops are stable enough, this copying can be removed
                //       In most cases it doesn't matter, because this is only used during DEBUG display...
                int count = node->Count;
                if (DebugDrawBuffer.Length < count) DebugDrawBuffer = new NativeSpatialObj[count];
                for (int i = 0; i < count; ++i)
                    DebugDrawBuffer[i] = node->Items[i];

                for (int i = 0; i < count; ++i)
                {
                    ref NativeSpatialObj so = ref DebugDrawBuffer[i];
                    var soCenter = new Vector2((so.X1 + so.X2) * 0.5f, (so.Y1 + so.Y2) * 0.5f);
                    var soSize = new Vector2(so.X2 - so.X1, so.Y2 - so.Y1);
                    screen.DrawRectangleProjected(soCenter, soSize, 0f, Violet);
                    screen.DrawCircleProjected(soCenter, so.Radius, Violet);
                    screen.DrawLineProjected(center, soCenter, Violet);
                }

                if (node->NW != null)
                {
                    if (node->NW->Overlaps(topLeft, botRight))
                        buffer.Stack[++buffer.Next] = node->NW;

                    if (node->NE->Overlaps(topLeft, botRight))
                        buffer.Stack[++buffer.Next] = node->NE;

                    if (node->SE->Overlaps(topLeft, botRight))
                        buffer.Stack[++buffer.Next] = node->SE;

                    if (node->SW->Overlaps(topLeft, botRight))
                        buffer.Stack[++buffer.Next] = node->SW;
                }
            } while (buffer.Next >= 0);

        }

        public void DebugVisualize(GameScreen screen)
        {
            var screenSize = new Vector2(screen.Viewport.Width, screen.Viewport.Height);
            Vector2 topLeft = screen.UnprojectToWorldPosition(new Vector2(0f, 0f));
            Vector2 botRight = screen.UnprojectToWorldPosition(screenSize);
            DebugVisualize(screen, topLeft, botRight, Tree->Root);

            Array.Clear(DebugDrawBuffer, 0, DebugDrawBuffer.Length); // prevent zombie objects
        }
    }
}
