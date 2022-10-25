using System;
using System.Runtime.CompilerServices;
using System.Threading;
using SDGraphics;
using SDUtils;

namespace Ship_Game.Spatial;

public partial class GenericQtree
{
    public SpatialObjectBase FindOne(Vector2 pos, float radius)
    {
        var searchArea = new AABoundingBox2D(pos, radius);
        return FindOne(searchArea);
    }

    public Array<SpatialObjectBase> Find(Vector2 pos, float radius)
    {
        var searchArea = new AABoundingBox2D(pos, radius);
        return Find(searchArea);
    }

    public SpatialObjectBase FindOne(in AABoundingBox2D searchArea)
    {
        return null;
    }

    public Array<SpatialObjectBase> Find(in AABoundingBox2D searchArea)
    {
        Array<SpatialObjectBase> results = new();

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