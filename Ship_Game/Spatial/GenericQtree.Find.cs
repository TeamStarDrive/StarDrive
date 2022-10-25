using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using SDGraphics;
using SDUtils;

namespace Ship_Game.Spatial;

public partial class GenericQtree
{
    public SpatialObjectBase FindOne(Vector2 pos, float radius)
    {
        SearchOptions opt = new(pos, radius);
        return FindOne(opt);
    }
    
    public SpatialObjectBase FindOne(in AABoundingBox2D searchArea)
    {
        SearchOptions opt = new(searchArea);
        return FindOne(opt);
    }

    public SpatialObjectBase[] Find(Vector2 pos, float radius)
    {
        SearchOptions opt = new(pos, radius);
        return Find(opt);
    }

    public SpatialObjectBase[] Find(in AABoundingBox2D searchArea)
    {
        SearchOptions opt = new(searchArea);
        return Find(opt);
    }

    public SpatialObjectBase FindOne(in SearchOptions opt)
    {
        AABoundingBox2D searchRect = opt.SearchRect;
        
        // TODO: figure out a faster way to test C# objects
        HashSet<int> alreadyChecked = new();

        uint loyaltyMask = NativeSpatialObject.GetLoyaltyMask(opt);
        uint typeMask = opt.Type == GameObjectType.Any ? 0xff : (uint)opt.Type;
        SpatialObjectBase exclude = opt.Exclude;

        float searchFX = opt.FilterOrigin.X;
        float searchFY = opt.FilterOrigin.Y;
        float searchFR = opt.FilterRadius;
        bool useSearchRadius = searchFR > 0f;

        Node root = Root;
        FindResultBuffer buffer = GetThreadLocalTraversalBuffer(root);

        DebugQtreeFind dfn = null;
        if (opt.DebugId != 0)
        {
            dfn = new DebugQtreeFind();
            dfn.SearchArea = opt.SearchRect;
            dfn.FilterOrigin = opt.FilterOrigin;
            dfn.RadialFilter = opt.FilterRadius;
            FindNearbyDbg[opt.DebugId] = dfn;
        }

        do
        {
            Node current = buffer.Pop();
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

                ObjectRef[] items = current.Items;
                for (int i = 0; i < count; ++i)
                {
                    ObjectRef so = items[i];

                    // FLAGS: either 0x00 (failed) or some bits 0100 (success)
                    if ((so.LoyaltyMask & loyaltyMask) != 0 &&
                        ((uint)so.Type & typeMask) != 0 &&
                        (exclude == null || so.Source != exclude))
                    {
                        if (!so.AABB.Overlaps(searchRect))
                            continue;

                        if (useSearchRadius)
                        {
                            if (!so.AABB.Overlaps(searchFX, searchFY, searchFR))
                                continue; // AABB not in SearchRadius
                        }
                        
                        if (!alreadyChecked.Add(so.ObjectId))
                            continue; // object was already checked

                        SpatialObjectBase go = so.Source;
                        if (opt.FilterFunction == null || opt.FilterFunction(go))
                        {
                            dfn?.SearchResults.Add(go);
                            return go;
                        }
                    }
                }
            }
        } while (buffer.NextNode >= 0);

        return null;
    }
    
    /// <summary>
    /// Finds all objects that match the search criteria
    /// </summary>
    /// <param name="opt"></param>
    /// <returns></returns>
    public SpatialObjectBase[] Find(in SearchOptions opt)
    {
        AABoundingBox2D searchRect = opt.SearchRect;
        int maxResults = opt.MaxResults > 0 ? opt.MaxResults : 1;
        
        // TODO: figure out a faster way to test C# objects
        HashSet<int> alreadyChecked = new();

        uint loyaltyMask = NativeSpatialObject.GetLoyaltyMask(opt);
        uint typeMask = opt.Type == GameObjectType.Any ? 0xff : (uint)opt.Type;
        SpatialObjectBase exclude = opt.Exclude;

        float searchFX = opt.FilterOrigin.X;
        float searchFY = opt.FilterOrigin.Y;
        float searchFR = opt.FilterRadius;
        bool useSearchRadius = searchFR > 0f;

        Node root = Root;
        FindResultBuffer buffer = GetThreadLocalTraversalBuffer(root);
        if (buffer.Items.Length < maxResults)
            buffer.Items = new SpatialObjectBase[maxResults];

        DebugQtreeFind dfn = null;
        if (opt.DebugId != 0)
        {
            dfn = new DebugQtreeFind();
            dfn.SearchArea = opt.SearchRect;
            dfn.FilterOrigin = opt.FilterOrigin;
            dfn.RadialFilter = opt.FilterRadius;
            FindNearbyDbg[opt.DebugId] = dfn;
        }

        do
        {
            Node current = buffer.Pop();
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

                ObjectRef[] items = current.Items;
                for (int i = 0; i < count; ++i)
                {
                    ObjectRef so = items[i];

                    // FLAGS: either 0x00 (failed) or some bits 0100 (success)
                    if ((so.LoyaltyMask & loyaltyMask) != 0 &&
                        ((uint)so.Type & typeMask) != 0 &&
                        (exclude == null || so.Source != exclude))
                    {
                        if (!so.AABB.Overlaps(searchRect))
                            continue;

                        if (useSearchRadius)
                        {
                            if (!so.AABB.Overlaps(searchFX, searchFY, searchFR))
                                continue; // AABB not in SearchRadius
                        }
                        
                        if (!alreadyChecked.Add(so.ObjectId))
                            continue; // object was already checked

                        SpatialObjectBase go = so.Source;
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

        SpatialObjectBase[] results = buffer.GetArrayAndClearBuffer();
        if (opt.SortByDistance)
            LinearSearch.SortByDistance(opt, results);
        return results;
    }

    /// <summary>
    /// Optimized temporary search buffer for FindNearby
    /// </summary>
    class FindResultBuffer
    {
        public int Count = 0;
        public SpatialObjectBase[] Items = new SpatialObjectBase[128];
        public int NextNode = 0; // next node to pop
        public Node[] NodeStack = new Node[512];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Node Pop()
        {
            Node node = NodeStack[NextNode];
            NodeStack[NextNode] = default; // don't leak refs
            --NextNode;
            return node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushBack(Node node)
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

    FindResultBuffer GetThreadLocalTraversalBuffer(Node root)
    {
        FindResultBuffer buffer = FindBuffer.Value;
        buffer.NextNode = 0;
        buffer.NodeStack[0] = root;
        return buffer;
    }
}