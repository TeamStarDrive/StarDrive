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

        /// <summary>
        /// Fast traversal array: deepest nodes first, Root node is last
        /// </summary>
        readonly Array<QtreeNode> DeepestNodesFirst = new Array<QtreeNode>();

        readonly Array<Array<QtreeNode>> LevelNodes = new Array<Array<QtreeNode>>();

        int NodeIds;

        /// <summary>
        /// Number of active nodes
        /// </summary>
        public int NumActiveNodes => Active.Count;

        public QtreeRecycleBuffer(int idStart)
        {
            NodeIds = idStart;
        }

        public QtreeNode Create(int level, float x, float y, float lastX, float lastY)
        {
            // Reuse existing node from front buffer
            if (Inactive.TryPopLast(out QtreeNode node))
            {
                node.InitializeForReuse(level, x, y, lastX, lastY);
            }
            else // create a new node
            {
                node = new QtreeNode(level, x, y, lastX, lastY);
                node.Id = ++NodeIds;
            }

            // always add this frame's node to the back buffer
            Active.Add(node);

            // record this node to a particular level
            if (LevelNodes.Count <= level)
            {
                LevelNodes.Resize(level+1);
                for (int i = 0; i < LevelNodes.Count; ++i)
                {
                    if (LevelNodes[i] == null)
                    {
                        LevelNodes[i] = new Array<QtreeNode>();
                    }
                }
            }
            LevelNodes[level].Add(node);
            return node;
        }

        public void MarkAllNodesInactive()
        {
            Inactive.AddRange(Active);
            Active.Clear();
            DeepestNodesFirst.Clear();
        }

        public Array<QtreeNode> GetDeepestNodesFirst()
        {
            if (DeepestNodesFirst.IsEmpty)
            {
                for (int i = 0; i < LevelNodes.Count; ++i)
                {
                    Array<QtreeNode> nodes = LevelNodes[i];
                    for (int j = 0; j < nodes.Count; ++j)
                    {
                        QtreeNode node = nodes[j];
                        if (node.Count != 0)
                            DeepestNodesFirst.Add(node);
                    }
                }
            }
            return DeepestNodesFirst;
        }
    }
}
