using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
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

        public QtreeNode Create(int level, in AABoundingBox2D bounds)
        {
            // Reuse existing node from front buffer
            if (Inactive.TryPopLast(out QtreeNode node))
            {
                node.InitializeForReuse(level, bounds);
            }
            else // create a new node
            {
                node = new QtreeNode(level, bounds);
                node.Id = ++NodeIds;
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
