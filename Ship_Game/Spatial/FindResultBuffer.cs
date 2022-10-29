using System;
using System.Runtime.CompilerServices;
using SDUtils;

namespace Ship_Game.Spatial;

/// <summary>
/// Optimized temporary search buffer for FindNearby and fast traversal of the Qtree
/// </summary>
internal sealed class FindResultBuffer<T> where T : QtreeNodeBase<T>
{
    public int Count;
    public SpatialObjectBase[] Items = new SpatialObjectBase[128];
    public int NextNode; // next node to pop
    public readonly T[] NodeStack = new T[512];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Pop()
    {
        unchecked
        {
            int current = NextNode--;
            T[] stack = NodeStack;
            T node = stack[current];
            stack[current] = null; // don't leak refs
            return node;
        }
    }

    public TResult[] GetArrayAndClearBuffer<TResult>() where TResult : SpatialObjectBase
    {
        int count = Count;
        if (count == 0)
            return Empty<TResult>.Array;

        Count = 0;
        var arr = new TResult[count];
        Memory.HybridCopy(arr, 0, Items, count);
        Array.Clear(Items, 0, count);
        return arr;
    }

    // resets this result buffer
    public FindResultBuffer<T> Get(T root, int maxResults = 0)
    {
        NextNode = 0;
        NodeStack[0] = root;
        if (maxResults != 0 && Items.Length < maxResults)
        {
            Items = new SpatialObjectBase[maxResults];
        }
        return this;
    }

    public void PushAllQuadrants(T node)
    {
        unchecked
        {
            NodeStack[++NextNode] = node.SW; // in reverse order
            NodeStack[++NextNode] = node.SE;
            NodeStack[++NextNode] = node.NE;
            NodeStack[++NextNode] = node.NW;
        }
    }

    /// <summary>
    /// Utility which quickly figures out which QuadTree sub-node is being overlapped
    /// and pushes them to the node stack
    /// </summary>
    public void PushOverlappingQuadrants(T node, in AABoundingBox2D rect)
    {
        unchecked
        {
            // by value is faster
            AABoundingBox2D quad = node.AABB;
            float midX = (quad.X1 + quad.X2) * 0.5f;
            float midY = (quad.Y1 + quad.Y2) * 0.5f;

            // @see struct OverlapsRect for explanation of the math
            // PERF: duplicating the conditions is faster due to speculative execution
            if (rect.Y1 < midY && rect.X1 < midX) // overlaps NW
            {
                NodeStack[++NextNode] = node.NW;
            }
            if (rect.Y1 < midY && rect.X2 >= midX) // overlaps NE
            {
                NodeStack[++NextNode] = node.NE;
            }
            if (rect.Y2 >= midY && rect.X2 >= midX) // overlaps SE
            {
                NodeStack[++NextNode] = node.SE;
            }
            if (rect.Y2 >= midY && rect.X1 < midX) // overlaps SW
            {
                NodeStack[++NextNode] = node.SW;
            }
        }
    }
}
