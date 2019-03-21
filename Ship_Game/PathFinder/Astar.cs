using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Ship_Game.PathFinder
{
    /**
     * Basic A* algorithm
     * @note This class is NOT thread safe
     */
    public class Astar
    {
        public int NumOpened   { get; private set; } // statistic, number of Nodes opened
        public int NumReopened { get; private set; } // statistic, number of Nodes reopened for more accurate path
        public int MaxDepth    { get; private set; } // statistic, maximum achieved depth of OpenList

        int OpenId; // pathfinder session ID

        readonly Array<Node> UnorderedNodes = new Array<Node>();
        readonly NodeVector OpenList = new NodeVector();

        /** @note This method is slow. Number of iterations is ~ Nodes.Count*Node.Links */
        void RecalculateCosts()
        {
            foreach (Node n in UnorderedNodes)
            {
                int numLinks = n.NumLinks;
                for (int i = 0; i < numLinks; ++i)
                {
                    Link link = n.Links[i];
                    link.Cost = CalculateCost(n, link.Node);
                }
            }
        }

        static float CalculateCost(Node a, Node b)
        {
            Vector2 dv = a.DeltaTo(b); // direction: values can be negative
            float distance = dv.Length();
            return distance;
        }


        /** @brief Finds a node corresponding to the given terrain position */
        Node FindClosestNode(Vector2 to)
        {
            return UnorderedNodes.FindMin(node => node.Pos.SqDist(to));
        }

        /**
         * @brief This is the core A* search routine
         * @param startPos Valid starting position in WORLD coords
         * @param endPos   Valid end position in WORLD coords
         * @param explored If not null, then all explored links are output in pairs as:  [A,B,  B,C,  C,D]
         * @return Final path expressed as a node list: [A,B,C,D,E,F,...,Z]
         */
        public Array<Vector2> FindPath(Vector2 startPos, Vector2 endPos, Array<Vector2> explored)
        {
            NumOpened   = 0; // statistics
            NumReopened = 0;
            MaxDepth    = 0;

            var path = new Array<Vector2>();
            Node start = FindClosestNode(startPos);
            Node end   = FindClosestNode(endPos);
            if (start == null || end == null)
                return path; // empty path

            Node head = start;
            head.Prev = null;
            int openId = ++OpenId; // get new ID to differ between unexplored and reopenable nodes

            while (head != end)
            {
                Node prev = head.Prev;
                float headGScore = head.GScore;
                int     numLinks = head.NumLinks;

                for (int i = 0; i < numLinks; ++i)
                {
                    Link link = head.Links[i];
                    Node n    = link.Node;
                    if (n == prev)
                        continue; // avoid circular references

                    if (openId == n.OpenId) // we have opened this Node before; it's a reopen
                    {
                        if (n.Closed)
                            continue; // don't touch if it's CLOSED

                        float gScore = headGScore + link.Cost; // new gain score
                        if (gScore >= n.GScore)
                            continue; // if the new gain is worse, then don't touch it

                        n.FScore = n.HScore + gScore;
                        n.GScore = gScore;
                        n.Prev   = head;

                        /** @note: this was a reopen */
                        ++NumOpened;
                        ++NumReopened;
                        OpenList.repos(n); // reposition item (due to new FScore)
                    }
                    else // open this 'fresh' node for path consideration
                    {
                        // calculate HScore
                        // (abs(distX) + abs(distY))*8
                        float hScore = CalculateCost(n, end);
                        float gScore = headGScore + link.Cost;
                        n.HScore = hScore;
                        n.GScore = gScore;
                        n.FScore = hScore + gScore;
                        n.Closed = false;
                        n.Prev   = head;
                        n.OpenId = openId;

                        /** @note: first open: insert to openlist (sorted insert!)*/
                        ++NumOpened;
                        OpenList.insert(n);
                    }

                    int size = OpenList.Size;
                    if (size > MaxDepth) MaxDepth = size; // statistic, max openlist depth

                    if (explored != null) // output graph of all explored nodes
                    {
                        explored.Add(head.Pos);
                        explored.Add(n.Pos);
                    }
                }

                // after inserting into the sorted list we get the heuristically best node available
                if (OpenList.empty())
                    break; // or not.

                head = OpenList.pop();
                head.Closed = true;
            }

            // finalization: build the out path
            head = end;
            do
            {
                path.Add(head.Pos);
            } while ((head = head.Prev) != null && head != start/**@note fix for looping path*/);

            OpenList.Clear(); // reset open-list
            return path;
        }

    }
}
