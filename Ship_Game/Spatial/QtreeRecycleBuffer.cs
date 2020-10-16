using System;

namespace Ship_Game.Spatial
{
    /// <summary>
    /// Utility buffer for recycling QtreeNodes
    /// </summary>
    public class QtreeRecycleBuffer
    {
        // Active nodes that have been initialized
        readonly Array<QtreeNode> Active = new Array<QtreeNode>();

        // Inactive nodes are ready to be reused
        readonly Array<QtreeNode> Inactive  = new Array<QtreeNode>();

        int NodeIds;

        /// <summary>
        /// Number of active nodes
        /// </summary>
        public int NumActiveNodes => Active.Count;

        public QtreeRecycleBuffer(int idStart)
        {
            NodeIds = idStart;
        }

        public QtreeNode Create(float x1, float y1, float x2, float y2)
        {
            var bounds = new AABoundingBox2D(x1, y1, x2, y2);
            // Reuse existing node from front buffer
            if (Inactive.TryPopLast(out QtreeNode node))
            {
                node.InitializeForReuse(bounds);
            }
            else // create a new node
            {
                node = new QtreeNode(bounds);
            }

            // always add this frame's node to the back buffer
            Active.Add(node);
            return node;
        }

        public void MarkAllNodesInactive()
        {
            Inactive.AddRange(Active);
            Active.Clear();
        }
    }
}
