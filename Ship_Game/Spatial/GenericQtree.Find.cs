using System;
using System.Threading;
using SDGraphics;

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
    /// <summary>
    /// Finds all objects that match the search criteria
    /// </summary>
    public SpatialObjectBase[] Find(in SearchOptions opt)
    {
        return Find<SpatialObjectBase>(opt);
    }
    
    public T[] Find<T>(in AABoundingBox2D searchArea) where T : SpatialObjectBase
    {
        SearchOptions opt = new(searchArea);
        return Find<T>(opt);
    }
    
    public T[] Find<T>(Vector2 pos, float radius) where T : SpatialObjectBase
    {
        SearchOptions opt = new(pos, radius);
        return Find<T>(opt);
    }
    
    /// <summary>
    /// Finds the first object that matches the search criteria
    /// If SortByDistance is enabled, the closest item is returned,
    /// the accuracy of closest result depends on opt.MaxResults
    /// </summary>
    public SpatialObjectBase FindOne(SearchOptions opt)
    {
        // if not sorting by distance, then always fetch 1 result
        if (!opt.SortByDistance)
            opt.MaxResults = 1;

        FindResultBuffer<Node> buffer = FindNearby(opt);
        SpatialObjectBase[] results = buffer.GetArrayAndClearBuffer<SpatialObjectBase>();
        return results.Length > 0 ? results[0] : null;
    }

    /// <summary>
    /// Finds all objects that match the search criteria,
    /// while casting the results array into T
    /// </summary>
    public T[] Find<T>(in SearchOptions opt) where T : SpatialObjectBase
    {
        FindResultBuffer<Node> buffer = FindNearby(opt);
        return buffer.GetArrayAndClearBuffer<T>();
    }

    FindResultBuffer<Node> FindNearby(in SearchOptions opt)
    {
        FindResultBuffer<Node> buffer = GetThreadLocalTraversalBuffer(Root, opt.MaxResults);
        
        AABoundingBox2D searchRect = opt.SearchRect;
        uint loyaltyMask = NativeSpatialObject.GetLoyaltyMask(opt);
        do
        {
            Node current = buffer.Pop();
            if (current.NW != null) // isBranch
            {
                buffer.PushOverlappingQuadrants(current, searchRect);
            }
            else // isLeaf
            {
                if ((current.LoyaltyMask & loyaltyMask) != 0) // empty cell mask is 0
                {
                    buffer.AddFound(current, current.Count);
                }
            }
        } while (buffer.NextNode >= 0 && buffer.NumCellsFound != FindResultBuffer<Node>.MaxFound);

        if (buffer.NumCellsFound > 0)
        {
            FilterResults(buffer, opt);
            if (opt.SortByDistance) // sort the final results
                buffer.Items.SortByDistance(buffer.Count, searchRect.Center);
        }

        if (opt.DebugId != 0) // add some debug stuff
        {
            DebugQtreeFind dfn = GetFindDebug(opt);
            for (int i = 0; i < buffer.NumCellsFound; ++i) dfn.FindCells.Add(buffer.FoundCells[i].AABB);
            for (int i = 0; i < buffer.Count; ++i) dfn.SearchResults.Add(buffer.Items[i]);
        }
        return buffer;
    }

    // TODO: find a way to share this between Qtree implementations
    // NOTE: this is translated 1-to-1 from SDNative Search.cpp
    unsafe void FilterResults(FindResultBuffer<Node> buffer, in SearchOptions opt)
    {
        // don't crash if someone asks for 0 results
        int maxResults = opt.MaxResults;
        if (maxResults <= 0)
            return;

        // we use a bit array to ignore duplicate objects
        // duplication is present by design to handle grid border overlap
        // this filtering is faster than other more complicated structural methods
        int idBitArraySize = ((Count / 32) + 1) * sizeof(uint);
        uint* idBitArray = stackalloc uint[idBitArraySize]; // C# spec says contents undefined
        for (int i = 0; i < idBitArraySize; ++i) idBitArray[i] = 0; // so we need to zero the idBitArray

        uint loyaltyMask = NativeSpatialObject.GetLoyaltyMask(opt);
        uint typeMask = opt.Type == GameObjectType.Any ? 0xff : (uint)opt.Type;
        SpatialObjectBase exclude = opt.Exclude;
        
        AABoundingBox2D searchRect = opt.SearchRect;
        bool useSearchRadius = opt.FilterRadius > 0f;

        Node[] cells = buffer.FoundCells;
        int numCells = buffer.NumCellsFound;

        // if total candidates is more than we can fit, we need to sort LEAF nodes by distance to Origin
        bool sortByDistance = opt.SortByDistance || buffer.NumObjectsFound > maxResults;
        if (sortByDistance)
        {
            Vector2 searchCenter = searchRect.Center;
            var keys = new float[numCells];
            for (int i = 0; i < numCells; ++i)
                keys[i] = cells[i].AABB.Center.SqDist(searchCenter);
            Array.Sort(keys, cells, 0, numCells);
        }

        for (int leafIndex = 0; leafIndex < numCells; ++leafIndex)
        {
            Node cell = cells[leafIndex];
            int size = cell.Count;
            ObjectRef[] items = cell.Items;
            for (int i = 0; i < size; ++i) // this is the perf hotspot
            {
                ObjectRef o = items[i];
                // BUG: Thread-Safety issue here; in order to fix it, requires total refactor of GenericQtree
                if (o == null)
                    continue;

                // FLAGS: either 0x00 (failed) or some bits 0100 (success)
                if ((o.LoyaltyMask & loyaltyMask) != 0 &&
                    ((uint)o.Type & typeMask) != 0 &&
                    (o.Source != exclude))
                {
                    if (!searchRect.Overlaps(o.AABB))
                        continue; // AABB not in SearchRadius

                    if (useSearchRadius)
                    {
                        if (!o.AABB.Overlaps(opt.FilterOrigin.X, opt.FilterOrigin.Y, opt.FilterRadius))
                            continue;
                    }

                    // PERF: this is the fastest point for duplicate check
                    int id = o.ObjectId;
                    int wordIndex = id / 32;
                    uint idMask = (uint)(1 << (id % 32));
                    if ((idBitArray[wordIndex] & idMask) != 0)
                        continue; // object was already checked

                    idBitArray[wordIndex] |= idMask; // flag it as checked

                    SpatialObjectBase go = o.Source;
                    if (opt.FilterFunction == null || opt.FilterFunction(go))
                    {
                        buffer.Items[buffer.Count++] = go;
                        if (buffer.Count == maxResults)
                            return; // we are done !
                    }
                }
            }
        }
    }

    // NOTE: For debugging only
    public SpatialObjectBase[] FindLinear(in SearchOptions opt, SpatialObjectBase[] objects)
    {
        return LinearSearch.FindNearby(in opt, objects, objects.Length);
    }

    // NOTE: This is really fast
    readonly ThreadLocal<FindResultBuffer<Node>> FindBuffer = new(() => new());

    FindResultBuffer<Node> GetThreadLocalTraversalBuffer(Node root, int maxResults = 0)
    {
        return FindBuffer.Value.Get(root, maxResults);
    }
}
