using System;
using System.Runtime.CompilerServices;
using System.Threading;
using SDUtils;

namespace Ship_Game.Spatial
{
    public partial class Qtree
    {
        /// <summary>
        /// Optimized temporary search buffer for FindNearby
        /// </summary>
        class FindResultBuffer
        {
            public int Count = 0;
            public SpatialObjectBase[] Items = new SpatialObjectBase[128];
            public int NextNode = 0; // next node to pop
            public QtreeNode[] NodeStack = new QtreeNode[512];
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public QtreeNode Pop()
            {
                QtreeNode node = NodeStack[NextNode];
                NodeStack[NextNode] = default; // don't leak refs
                --NextNode;
                return node;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void PushBack(QtreeNode node)
            {
                NodeStack[++NextNode] = node;
            }

            public SpatialObjectBase[] GetArrayAndClearBuffer()
            {
                int count = Count;
                if (count == 0)
                    return Empty<SpatialObjectBase>.Array;

                Count = 0;
                var arr = new SpatialObjectBase[count];
                Memory.HybridCopy(arr, 0, Items, count);
                Array.Clear(Items, 0, count);
                return arr;
            }
        }

        // NOTE: This is really fast
        readonly ThreadLocal<FindResultBuffer> FindBuffer = new(() => new());

        FindResultBuffer GetThreadLocalTraversalBuffer(QtreeNode root)
        {
            FindResultBuffer buffer = FindBuffer.Value;
            buffer.NextNode = 0;
            buffer.NodeStack[0] = root;
            return buffer;
        }

        public unsafe SpatialObjectBase[] FindNearby(in SearchOptions opt)
        {
            AABoundingBox2D searchRect = opt.SearchRect;
            int maxResults = opt.MaxResults > 0 ? opt.MaxResults : 1;
            SpatialObj[] spatialObjects = SpatialObjects;
            
            int idBitArraySize = ((spatialObjects.Length / 32) + 1) * sizeof(uint);
            uint* idBitArray = stackalloc uint[idBitArraySize]; // C# spec says contents undefined
            for (int i = 0; i < idBitArraySize; ++i) // so we need to zero the idBitArray
                idBitArray[i] = 0;

            uint loyaltyMask = NativeSpatialObject.GetLoyaltyMask(opt);
            uint typeMask = opt.Type == GameObjectType.Any ? 0xff : (uint)opt.Type;
            int excludeObject = opt.Exclude?.SpatialIndex ?? -1;

            float searchFX = opt.FilterOrigin.X;
            float searchFY = opt.FilterOrigin.Y;
            float searchFR = opt.FilterRadius;
            bool useSearchRadius = searchFR > 0f;

            QtreeNode root = Root;
            FindResultBuffer buffer = GetThreadLocalTraversalBuffer(root);
            if (buffer.Items.Length < maxResults)
                buffer.Items = new SpatialObjectBase[maxResults];

            DebugFindNearby dfn = null;
            if (opt.DebugId != 0)
            {
                dfn = new DebugFindNearby();
                dfn.SearchArea = opt.SearchRect;
                dfn.FilterOrigin = opt.FilterOrigin;
                dfn.RadialFilter = opt.FilterRadius;
                FindNearbyDbg[opt.DebugId] = dfn;
            }

            SpatialObjectBase[] objects = Objects;
            do
            {
                QtreeNode current = buffer.Pop();
                if (current.NW != null) // isBranch
                {
                    var over = new OverlapsRect(current.AABB, searchRect);
                    if (over.SW != 0) buffer.PushBack(current.SW);
                    if (over.SE != 0) buffer.PushBack(current.SE);
                    if (over.NE != 0) buffer.PushBack(current.NE);
                    if (over.NW != 0) buffer.PushBack(current.NW);
                }
                else // isLeaf
                {
                    int count = current.Count;
                    if (count == 0 || (current.LoyaltyMask & loyaltyMask) == 0)
                        continue;

                    dfn?.FindCells.Add(current.AABB);

                    SpatialObj*[] items = current.Items;
                    for (int i = 0; i < count; ++i)
                    {
                        SpatialObj* so = items[i];

                        // FLAGS: either 0x00 (failed) or some bits 0100 (success)
                        if ((so->LoyaltyMask & loyaltyMask) != 0 &&
                            ((uint)so->Type & typeMask) != 0 &&
                            (so->ObjectId != excludeObject))
                        {
                            if (!so->AABB.Overlaps(searchRect))
                                continue;

                            if (useSearchRadius)
                            {
                                if (!so->AABB.Overlaps(searchFX, searchFY, searchFR))
                                    continue; // AABB not in SearchRadius
                            }
                            
                            // PERF: this is the fastest point for duplicate check
                            int id = so->ObjectId;
                            int wordIndex = id / 32;
                            uint idMask = (uint)(1 << (id % 32));
                            if ((idBitArray[wordIndex] & idMask) != 0)
                                continue; // object was already checked

                            idBitArray[wordIndex] |= idMask; // flag it as checked

                            SpatialObjectBase go = objects[id];
                            if (opt.FilterFunction == null || opt.FilterFunction(go))
                            {
                                dfn?.SearchResults.Add(go);
                                buffer.Items[buffer.Count++] = go;
                                if (buffer.Count == opt.MaxResults)
                                    break; // we are done !
                            }
                        }
                    }
                    if (buffer.Count == maxResults)
                        break; // we are done !
                }
            } while (buffer.NextNode >= 0 && buffer.Count < maxResults);

            return buffer.GetArrayAndClearBuffer();
        }

        public SpatialObjectBase[] FindLinear(in SearchOptions opt)
        {
            SpatialObjectBase[] objects = Objects;
            return LinearSearch.FindNearby(in opt, objects, objects.Length);
        }
    }
}