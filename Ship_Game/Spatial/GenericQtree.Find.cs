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
    /// Finds the first object that matches the search criteria
    /// </summary>
    public unsafe SpatialObjectBase FindOne(in SearchOptions opt)
    {
        AABoundingBox2D searchRect = opt.SearchRect;
        bool useSearchRadius = opt.FilterRadius > 0f;

        int idBitArraySize = ((Count / 32) + 1) * sizeof(uint);
        uint* idBitArray = stackalloc uint[idBitArraySize]; // C# spec says contents undefined
        for (int i = 0; i < idBitArraySize; ++i) idBitArray[i] = 0; // so we need to zero the idBitArray

        uint loyaltyMask = NativeSpatialObject.GetLoyaltyMask(opt);
        uint typeMask = opt.Type == GameObjectType.Any ? 0xff : (uint)opt.Type;
        SpatialObjectBase exclude = opt.Exclude;
        DebugQtreeFind dfn = GetFindDebug(opt);
        
        FindResultBuffer<Node> buffer = GetThreadLocalTraversalBuffer(Root);
        do
        {
            Node current = buffer.Pop();
            if (current.NW != null) // isBranch
            {
                buffer.PushOverlappingQuadrants(current, searchRect);
            }
            else // isLeaf
            {
                int count = current.Count;
                if (count == 0 || (current.LoyaltyMask & loyaltyMask) == 0)
                    continue;

                dfn?.FindCells.Add(current.AABB);

                ObjectRef[] items = current.Items;
                for (int i = 0; i < count; ++i) // this is the perf hotspot
                {
                    ObjectRef so = items[i];

                    // FLAGS: either 0x00 (failed) or some bits 0100 (success)
                    if ((so.LoyaltyMask & loyaltyMask) != 0 &&
                        ((uint)so.Type & typeMask) != 0 &&
                        (exclude == null || so.Source != exclude))
                    {
                        if (!so.AABB.Overlaps(searchRect) ||
                            useSearchRadius && !so.AABB.Overlaps(opt.FilterOrigin.X, opt.FilterOrigin.Y, opt.FilterRadius))
                            continue; // AABB not in SearchRadius

                        // PERF: this is the fastest point for duplicate check
                        int id = so.ObjectId;
                        int wordIndex = id / 32;
                        uint idMask = (uint)(1 << (id % 32));
                        if ((idBitArray[wordIndex] & idMask) != 0)
                            continue; // object was already checked

                        idBitArray[wordIndex] |= idMask; // flag it as checked

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
    public unsafe SpatialObjectBase[] Find(in SearchOptions opt)
    {
        AABoundingBox2D searchRect = opt.SearchRect;
        bool useSearchRadius = opt.FilterRadius > 0f;

        int idBitArraySize = ((Count / 32) + 1) * sizeof(uint);
        uint* idBitArray = stackalloc uint[idBitArraySize]; // C# spec says contents undefined
        for (int i = 0; i < idBitArraySize; ++i) idBitArray[i] = 0; // so we need to zero the idBitArray

        uint loyaltyMask = NativeSpatialObject.GetLoyaltyMask(opt);
        uint typeMask = opt.Type == GameObjectType.Any ? 0xff : (uint)opt.Type;
        SpatialObjectBase exclude = opt.Exclude;
        DebugQtreeFind dfn = GetFindDebug(opt);

        int maxResults = opt.MaxResults > 0 ? opt.MaxResults : 1;
        
        FindResultBuffer<Node> buffer = GetThreadLocalTraversalBuffer(Root, maxResults);
        do
        {
            Node current = buffer.Pop();
            if (current.NW != null) // isBranch
            {
                buffer.PushOverlappingQuadrants(current, searchRect);
            }
            else // isLeaf
            {
                int count = current.Count;
                if (count == 0 || (current.LoyaltyMask & loyaltyMask) == 0)
                    continue; // no objects here, or the loyalty we are searching is not present

                dfn?.FindCells.Add(current.AABB);

                ObjectRef[] items = current.Items;
                for (int i = 0; i < count; ++i) // this is the perf hotspot
                {
                    ObjectRef so = items[i];

                    // FLAGS: either 0x00 (failed) or some bits 0100 (success)
                    if ((so.LoyaltyMask & loyaltyMask) != 0 &&
                        ((uint)so.Type & typeMask) != 0 &&
                        (exclude == null || so.Source != exclude))
                    {
                        if (!so.AABB.Overlaps(searchRect) ||
                            useSearchRadius && !so.AABB.Overlaps(opt.FilterOrigin.X, opt.FilterOrigin.Y, opt.FilterRadius))
                            continue; // AABB not in SearchRadius

                        // PERF: this is the fastest point for duplicate check
                        int id = so.ObjectId;
                        int wordIndex = id / 32;
                        uint idMask = (uint)(1 << (id % 32));
                        if ((idBitArray[wordIndex] & idMask) != 0)
                            continue; // object was already checked

                        idBitArray[wordIndex] |= idMask; // flag it as checked

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
        } while (buffer.NextNode >= 0);

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

    // NOTE: This is really fast
    readonly ThreadLocal<FindResultBuffer<Node>> FindBuffer = new(() => new());

    FindResultBuffer<Node> GetThreadLocalTraversalBuffer(Node root, int maxResults = 0)
    {
        return FindBuffer.Value.Get(root, maxResults);
    }
}
