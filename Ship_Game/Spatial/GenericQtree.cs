using System;
using System.Collections.Generic;
using SDGraphics;
using SDUtils;

namespace Ship_Game.Spatial;

/// <summary>
/// A less optimized version of Qtree which can be used to represent
/// any kind of data which changes less frequently.
///
/// There is no persistent state kept in the SpatialObjectBase which makes this
/// ideal for alternative use cases.
///
/// Such as: SolarSystem locations, Planet locations, ThreatMatrix clusters,
/// everything which inherits from SpatialObjectBase
/// </summary>
public partial class GenericQtree
{
    /// <summary>
    /// How many objects to store per cell before subdividing
    /// </summary>
    readonly int CellThreshold;

    /// <summary>
    /// Max number of levels to subdivide
    /// </summary>
    readonly int Levels;

    public float FullSize { get; }
    public float WorldSize { get; }

    Node Root;
    int NextObjectId = 1;

    Array<SpatialObjectBase> AllObjects = new();
    public IReadOnlyList<SpatialObjectBase> Objects => AllObjects;
    public int Count => AllObjects.Count;

    public string Name => "GenericQtree";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="universeWidth">Width of the searchable universe</param>
    /// <param name="cellThreshold">How many objects per Cell before subdividing it into 4 quads</param>
    /// <param name="smallestCell">The lowest possible size of a cell</param>
    public GenericQtree(float universeWidth, int cellThreshold = 16, float smallestCell = 8_000)
    {
        CellThreshold = cellThreshold;
        WorldSize = universeWidth;
        Levels = 1;
        FullSize = smallestCell;
        while (FullSize < universeWidth)
        {
            ++Levels;
            FullSize *= 2;
        }
        Clear();
    }

    public void Clear()
    {
        // universe is centered at [0,0], so Root node goes from [-half, +half)
        float half = FullSize / 2;
        Root = new(-half, -half, +half, +half);
    }

    public void Insert(SpatialObjectBase obj)
    {
        // TODO: Thread safety?
        if (!AllObjects.AddUniqueRef(obj))
        {
            return; // this object already exists in this Qtree
        }

        int objectId = NextObjectId++;
        var spatialObj = new ObjectRef(obj, objectId);
        InsertAt(Root, Levels, spatialObj);
    }

    public void Remove(SpatialObjectBase obj)
    {
        // TODO: Thread safety?
        if (!AllObjects.RemoveRef(obj))
        {
            return; // this object does not exist in this Qtree
        }

        AABoundingBox2D objectRect = new(obj);
        RemoveAt(Root, Levels, obj, objectRect);
    }

    // update the position of a single item, this is a slow operation
    public void Update(SpatialObjectBase obj)
    {
        if (AllObjects.ContainsRef(obj))
        {
            AABoundingBox2D objectRect = new(obj);
            RemoveAt(Root, Levels, obj, objectRect);

            // TODO: find the ObjectRef instead of creating new
            int objectId = NextObjectId++;
            var spatialObj = new ObjectRef(obj, objectId);
            InsertAt(Root, Levels, spatialObj);
        }
    }

    void InsertAt(Node node, int level, ObjectRef obj)
    {
        AABoundingBox2D objectRect = obj.AABB;
        for (;;)
        {
            if (node.NW != null) // isBranch
            {
                var over = new OverlapsRect(node.AABB, objectRect);
                int overlaps = over.NW + over.NE + over.SE + over.SW;

                // this is an optimal case, we only overlap 1 sub-quadrant, so we go deeper
                if (overlaps == 1)
                {
                    if      (over.NW != 0) { node = node.NW; --level; }
                    else if (over.NE != 0) { node = node.NE; --level; }
                    else if (over.SE != 0) { node = node.SE; --level; }
                    else if (over.SW != 0) { node = node.SW; --level; }
                }
                else // target overlaps multiple quadrants, so it has to be inserted into several of them:
                {
                    if (over.NW != 0) { InsertAt(node.NW, level-1, obj); }
                    if (over.NE != 0) { InsertAt(node.NE, level-1, obj); }
                    if (over.SE != 0) { InsertAt(node.SE, level-1, obj); }
                    if (over.SW != 0) { InsertAt(node.SW, level-1, obj); }
                    return;
                }
            }
            else // isLeaf
            {
                InsertAtLeaf(node, level, obj);
                return;
            }
        }
    }

    void InsertAtLeaf(Node leaf, int level, ObjectRef obj)
    {
        // are we maybe over Threshold and should Subdivide ?
        if (level > 0 && leaf.Count >= CellThreshold)
        {
            float x1 = leaf.AABB.X1;
            float x2 = leaf.AABB.X2;
            float y1 = leaf.AABB.Y1;
            float y2 = leaf.AABB.Y2;
            float midX = (x1 + x2) * 0.5f;
            float midY = (y1 + y2) * 0.5f;

            leaf.NW = new(x1, y1, midX, midY);
            leaf.NE = new(midX, y1, x2, midY);
            leaf.SE = new(midX, midY, x2, y2);
            leaf.SW = new(x1, midY, midX, y2);

            int count = leaf.Count;
            ObjectRef[] oldItems = leaf.Items;
            leaf.Items = Node.NoObjects;
            leaf.Count = 0;

            // and now reinsert all items one by one
            for (int i = 0; i < count; ++i)
                InsertAt(leaf, level, oldItems[i]);

            // and now try to insert our object again
            InsertAt(leaf, level, obj);
        }
        else // expand LEAF
        {
            leaf.Add(obj, CellThreshold);
        }
    }

    static void RemoveAt(Node node, int level, SpatialObjectBase obj, in AABoundingBox2D objectRect)
    {
        for (;;)
        {
            if (node.NW != null) // isBranch
            {
                var over = new OverlapsRect(node.AABB, objectRect);
                int overlaps = over.NW + over.NE + over.SE + over.SW;

                // this is an optimal case, we only overlap 1 sub-quadrant, so we go deeper
                if (overlaps == 1)
                {
                    if      (over.NW != 0) { node = node.NW; --level; }
                    else if (over.NE != 0) { node = node.NE; --level; }
                    else if (over.SE != 0) { node = node.SE; --level; }
                    else if (over.SW != 0) { node = node.SW; --level; }
                }
                else // target overlaps multiple quadrants, so it has to be removed from several of them:
                {
                    if (over.NW != 0) { RemoveAt(node.NW, level-1, obj, in objectRect); }
                    if (over.NE != 0) { RemoveAt(node.NE, level-1, obj, in objectRect); }
                    if (over.SE != 0) { RemoveAt(node.SE, level-1, obj, in objectRect); }
                    if (over.SW != 0) { RemoveAt(node.SW, level-1, obj, in objectRect); }
                    return;
                }
            }
            else // isLeaf
            {
                node.Remove(obj);
                return;
            }
        }
    }
    
    public class ObjectRef
    {
        // NOTE: These are ordered by the order of access pattern
        public byte Active;  // 1 if this item is active, 0 if DEAD and pending removal
        public byte Type; // for filtering by type, application defined
        public byte Loyalty; // Loyalty ID
        public uint LoyaltyMask; // mask for matching loyalty, see GetLoyaltyMask
        public SpatialObjectBase Source; // the actual object
        public int ObjectId; // unique object id for this ref
        public AABoundingBox2D AABB;

        public ObjectRef(SpatialObjectBase go, int objectId)
        {
            Active = 1;
            var type = go.Type;
            Type = (byte)type;
            int loyaltyId = go.GetLoyaltyId();
            Loyalty = (byte)loyaltyId;
            LoyaltyMask = NativeSpatialObject.GetLoyaltyMask(loyaltyId);
            Source = go;
            ObjectId = objectId;
            AABB = new(go);
        }
    }

    public class Node
    {
        public static readonly ObjectRef[] NoObjects = Empty<ObjectRef>.Array;

        public AABoundingBox2D AABB;
        public Node NW, NE, SE, SW;
        public int Count;
        public ObjectRef[] Items;

        public uint LoyaltyMask; // matches up to 32 loyalties
        public int LoyaltyCount;

        public Node(float x1, float y1, float x2, float y2)
        {
            AABB = new(x1, y1, x2, y2);
            Items = NoObjects;
        }

        public override string ToString()
        {
            return $"N={Count} {AABB}";
        }

        public void Add(ObjectRef obj, int cellThreshold)
        {
            int count = Count;
            ObjectRef[] oldItems = Items;
            if (oldItems.Length == count)
            {
                if (count == 0)
                {
                    var newItems = new ObjectRef[cellThreshold];
                    newItems[count] = obj;
                    Items = newItems;
                    Count = 1;
                }
                else // oldItems.Length == Count
                {
                    //Array.Resize(ref Items, Count * 2);
                    var newItems = new ObjectRef[oldItems.Length * 2];
                    for (int i = 0; i < oldItems.Length; ++i)
                        newItems[i] = oldItems[i];
                    newItems[count] = obj;
                    Items = newItems;
                    Count = count+1;
                }
            }
            else
            {
                oldItems[count] = obj;
                Count = count+1;
            }

            uint thisMask = obj.LoyaltyMask;
            if ((LoyaltyMask & thisMask) == 0) // this mask not present yet?
                ++LoyaltyCount;
            LoyaltyMask |= thisMask;
        }

        public void Remove(SpatialObjectBase obj)
        {
            // every time we remove an item, we need to recalculate the LoyaltyMask
            LoyaltyMask = 0;

            ObjectRef[] items = Items;
            for (int i = items.Length - 1; i >= 0; --i)
            {
                ObjectRef item = items[i];
                if (item.Source == obj)
                {
                    // RemoveAtSwapLast
                    int last = --Count;
                    Items[i] = Items[last];
                    Items[last] = default;
                }
                else
                {
                    uint thisMask = item.LoyaltyMask;
                    if ((LoyaltyMask & thisMask) == 0) // this mask not present yet?
                        ++LoyaltyCount;
                    LoyaltyMask |= thisMask;
                }
            }
        }
    }
}
