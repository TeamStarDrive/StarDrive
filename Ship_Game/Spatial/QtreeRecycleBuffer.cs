using System;
using System.Collections.Generic;
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

        public QtreeRecycleBuffer(int idStart)
        {
            NodeIds = idStart;
        }

        public QtreeNode Create(float x, float y, float lastX, float lastY)
        {
            // Reuse existing node from front buffer
            if (Inactive.TryPopLast(out QtreeNode node))
            {
                node.InitializeForReuse(x, y, lastX, lastY);
            }
            else // create a new node
            {
                node = new QtreeNode(x, y, lastX, lastY);
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
