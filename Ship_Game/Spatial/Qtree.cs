using System;
using System.Runtime.InteropServices;
using System.Threading;
using SDUtils;

namespace Ship_Game.Spatial;
///////////////////////////////////////////////////////////////////////////////////////////

/// <summary>
/// This is a more specialized Spatial Qtree designed for Ship - Projectile/Beam collisions
/// and can also be used for scanning for ships/projectiles.
///
/// For a more general solution, look at GenericQtree which does not specialize as heavily.
///
/// This is a 1:1 copy of C++ Qtree.h/.cpp, as a performance comparison between C# and C++ versions.
/// </summary>
public sealed unsafe partial class Qtree : ISpatial, IDisposable
{
    int Levels { get; }
    public float FullSize { get; }

    /// <summary>
    /// How many objects to store per cell before subdividing
    /// </summary>
    public const int CellThreshold = 64;

    QtreeNode Root;
    SpatialObjectBase[] Objects = Empty<SpatialObjectBase>.Array;
    readonly ReaderWriterLockSlim Lock = new(LockRecursionPolicy.NoRecursion);

    SpatialObj* SpatialObjects = null;
    QtreeRecycleBuffer FrontBuffer = new();
    QtreeRecycleBuffer BackBuffer  = new();

    public float WorldSize { get; }
    public int Count { get; private set; }

    public string Name => "C#-Qtree";

    // Create a quadtree to fit the universe
    public Qtree(float universeSize, float smallestCell = 512f)
    {
        WorldSize = universeSize;
        Levels = 1;
        FullSize = smallestCell;
        while (FullSize < universeSize)
        {
            ++Levels;
            FullSize *= 2;
        }
        Clear();
    }

    ~Qtree() { Dispose(false); }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        Lock.Dispose();
        FindBuffer.Dispose();
    }

    public void Clear()
    {
        // universe is centered at [0,0], so Root node goes from [-half, +half)
        float half = FullSize / 2;
        using (Lock.AcquireWriteLock())
        {
            Root = FrontBuffer.Create(-half, -half, +half, +half);
            Objects = Empty<SpatialObjectBase>.Array;
        }
    }

    (SpatialObjectBase[], QtreeNode) GetObjectsAndRootSafe()
    {
        using (Lock.AcquireReadLock())
            return (Objects, Root);
    }

    public void UpdateAll(SpatialObjectBase[] allObjects)
    {
        // prepare our node buffer for allocation
        FrontBuffer.MarkAllNodesInactive();

        // create the new tree from current world state
        var pOldObjects = SpatialObjects;
        var pSpatialObjects = (SpatialObj*)Marshal.AllocCoTaskMem(sizeof(SpatialObj) * allObjects.Length);
        QtreeNode newRoot = CreateFullTree(allObjects, pSpatialObjects);

        // we need to lock down the entire structure while updating
        using (Lock.AcquireWriteLock())
        {
            Objects = allObjects;
            Root = newRoot;
            SpatialObjects = pSpatialObjects;

            // Swap recycle lists
            // We move last frame's nodes to front and start overwriting them
            (FrontBuffer, BackBuffer) = (BackBuffer, FrontBuffer);
        }

        if (pOldObjects != null)
        {
            Marshal.FreeCoTaskMem((IntPtr)pOldObjects);
        }
    }
        
    QtreeNode CreateFullTree(SpatialObjectBase[] allObjects, SpatialObj* spatialObjects)
    {
        // universe is centered at [0,0], so Root node goes from [-half, +half)
        float half = FullSize / 2;
        QtreeNode newRoot = FrontBuffer.Create(-half, -half, +half, +half);

        for (int objectId = 0; objectId < allObjects.Length; ++objectId)
        {
            SpatialObjectBase go = allObjects[objectId];
            if (go.Active)
            {
                go.SpatialIndex = objectId;
                spatialObjects[objectId] = new SpatialObj(go, objectId);
                InsertAt(newRoot, Levels, &spatialObjects[objectId]);
            }
        }
        Count = allObjects.Length;
        return newRoot;
    }

    void InsertAt(QtreeNode node, int level, SpatialObj* obj)
    {
        AABoundingBox2D objectRect = obj->AABB;
        for (;;)
        {
            if (node.NW != null) // isBranch
            {
                (bool NW, bool NE, bool SE, bool SW, int overlaps) = OverlapsRect.GetWithCount(node.AABB, objectRect);
                if (overlaps == 0)
                    return; // fast exit

                // this is an optimal case, we only overlap 1 sub-quadrant, so we go deeper
                if (overlaps == 1)
                {
                    if      (NW) { node = node.NW; --level; }
                    else if (NE) { node = node.NE; --level; }
                    else if (SE) { node = node.SE; --level; }
                    else if (SW) { node = node.SW; --level; }
                }
                else // target overlaps multiple quadrants, so it has to be inserted into several of them:
                {
                    if (NW) { InsertAt(node.NW, level-1, obj); }
                    if (NE) { InsertAt(node.NE, level-1, obj); }
                    if (SE) { InsertAt(node.SE, level-1, obj); }
                    if (SW) { InsertAt(node.SW, level-1, obj); }
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

    void InsertAtLeaf(QtreeNode leaf, int level, SpatialObj* obj)
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

            leaf.NW = FrontBuffer.Create(x1, y1, midX, midY);
            leaf.NE = FrontBuffer.Create(midX, y1, x2, midY);
            leaf.SE = FrontBuffer.Create(midX, midY, x2, y2);
            leaf.SW = FrontBuffer.Create(x1, midY, midX, y2);

            int count = leaf.Count;
            SpatialObj*[] arr = leaf.Items;
            leaf.Items = QtreeNode.NoObjects;
            leaf.Count = 0;

            // and now reinsert all items one by one
            for (int i = 0; i < count; ++i)
                InsertAt(leaf, level, arr[i]);

            // and now try to insert our object again
            InsertAt(leaf, level, obj);
        }
        else // expand LEAF
        {
            leaf.Add(obj);
        }
    }
}