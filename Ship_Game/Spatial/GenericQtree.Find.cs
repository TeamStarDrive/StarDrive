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

    static IEnumerable<Node> TraverseLeafNodes(FindResultBuffer buffer, AABoundingBox2D searchRect)
    {
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
                yield return current;
            }
        }
        while (buffer.NextNode >= 0);
    }

    /// <summary>
    /// Finds the first object that matches the search criteria
    /// </summary>
    public SpatialObjectBase FindOne(in SearchOptions opt)
    {
        // TODO: figure out a faster way to test C# objects
        HashSet<int> alreadyChecked = new();
        
        AABoundingBox2D searchRect = opt.SearchRect;
        bool useSearchRadius = opt.FilterRadius > 0f;
        uint loyaltyMask = NativeSpatialObject.GetLoyaltyMask(opt);
        uint typeMask = opt.Type == GameObjectType.Any ? 0xff : (uint)opt.Type;
        SpatialObjectBase exclude = opt.Exclude;
        DebugQtreeFind dfn = GetFindDebug(opt);
        
        FindResultBuffer buffer = GetThreadLocalTraversalBuffer(Root);
        foreach (Node leaf in TraverseLeafNodes(buffer, searchRect))
        {
            int count = leaf.Count;
            if (count == 0 || (leaf.LoyaltyMask & loyaltyMask) == 0)
                continue;

            dfn?.FindCells.Add(leaf.AABB);

            ObjectRef[] items = leaf.Items;
            for (int i = 0; i < count; ++i) // this is the perf hotspot
            {
                ObjectRef so = items[i];

                // FLAGS: either 0x00 (failed) or some bits 0100 (success)
                if ((so.LoyaltyMask & loyaltyMask) != 0 &&
                    ((uint)so.Type & typeMask) != 0 &&
                    (exclude == null || so.Source != exclude))
                {
                    if (so.AABB.Overlaps(searchRect) &&
                        (!useSearchRadius || so.AABB.Overlaps(opt.FilterOrigin.X, opt.FilterOrigin.Y, opt.FilterRadius)) &&
                        alreadyChecked.Add(so.ObjectId))
                    {
                        SpatialObjectBase go = so.Source;
                        if (opt.FilterFunction == null || opt.FilterFunction(go))
                        {
                            dfn?.SearchResults.Add(go);
                            return go;
                        }
                    }
                }
            }
        }
        return null;
    }
    
    /// <summary>
    /// Finds all objects that match the search criteria
    /// </summary>
    public SpatialObjectBase[] Find(in SearchOptions opt)
    {
        // TODO: figure out a faster way to test C# objects
        HashSet<int> alreadyChecked = new();
        
        AABoundingBox2D searchRect = opt.SearchRect;
        bool useSearchRadius = opt.FilterRadius > 0f;
        uint loyaltyMask = NativeSpatialObject.GetLoyaltyMask(opt);
        uint typeMask = opt.Type == GameObjectType.Any ? 0xff : (uint)opt.Type;
        SpatialObjectBase exclude = opt.Exclude;
        DebugQtreeFind dfn = GetFindDebug(opt);

        int maxResults = opt.MaxResults > 0 ? opt.MaxResults : 1;
        
        FindResultBuffer buffer = GetThreadLocalTraversalBuffer(Root, maxResults);
        foreach (Node leaf in TraverseLeafNodes(buffer, searchRect))
        {
            int count = leaf.Count;
            if (count == 0 || (leaf.LoyaltyMask & loyaltyMask) == 0)
                continue; // no objects here, or the loyalty we are searching is not present

            dfn?.FindCells.Add(leaf.AABB);

            ObjectRef[] items = leaf.Items;
            for (int i = 0; i < count; ++i) // this is the perf hotspot
            {
                ObjectRef so = items[i];

                // FLAGS: either 0x00 (failed) or some bits 0100 (success)
                if ((so.LoyaltyMask & loyaltyMask) != 0 &&
                    ((uint)so.Type & typeMask) != 0 &&
                    (exclude == null || so.Source != exclude))
                {
                    if (so.AABB.Overlaps(searchRect) &&
                        (!useSearchRadius || so.AABB.Overlaps(opt.FilterOrigin.X, opt.FilterOrigin.Y, opt.FilterRadius)) &&
                        alreadyChecked.Add(so.ObjectId))
                    {
                        SpatialObjectBase go = so.Source;
                        if (opt.FilterFunction == null || opt.FilterFunction(go))
                        {
                            dfn?.SearchResults.Add(go);
                            buffer.Items[buffer.Count++] = go;
                            if (buffer.Count == maxResults)
                                goto done; // we are done !
                        }
                    }
                }
            }
        }
        done:
        SpatialObjectBase[] results = buffer.GetArrayAndClearBuffer();
        if (opt.SortByDistance)
            LinearSearch.SortByDistance(opt, results);
        return results;
    }

    // NOTE: For debugging only
    public SpatialObjectBase[] FindLinear(in SearchOptions opt, SpatialObjectBase[] objects)
    {
        return LinearSearch.FindNearby(in opt, objects, objects.Length);
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

    FindResultBuffer GetThreadLocalTraversalBuffer(Node root, int maxResults = 0)
    {
        FindResultBuffer buffer = FindBuffer.Value;
        buffer.NextNode = 0;
        buffer.NodeStack[0] = root;
        if (maxResults != 0 && buffer.Items.Length < maxResults)
        {
            buffer.Items = new SpatialObjectBase[maxResults];
        }
        return buffer;
    }
}
