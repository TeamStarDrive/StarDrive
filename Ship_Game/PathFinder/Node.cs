using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Ship_Game.PathFinder
{
    /**
     * @brief A* node representing its position AND state in the graph
     * A list of links is precomputed for faster traversal
     */
    public class Node
    {
        public float FScore = 0; // F = H + G(current score from src to dst)
        public float HScore = 0; // heuristic score: XY.manhattanDistance + deltaHeight*10
        public float GScore = 0; // accumulated sum of H values, giving total distance
        public int   OpenId = 0; // optimization: used to check if we've opened this node or not

        // A closed node is one that has been evaluated by A* and should be ignored in further checks
        public bool Closed = false;

        public Node Prev    = null; // previous node in the open path
        public int NumLinks = 0;

        // pre-allocated array of links, check NumLinks for actual length
        public Link[] Links = new Link[8];

        // position in world coords for heuristic pre-init
        public Vector2 Pos;

        Node(in Vector2 pos)
        {
            Pos = pos;
        }

        public Vector2 DeltaTo(Node b)
        {
            return Pos - b.Pos;
        }

        public override string ToString()
        {
            return $"{Pos.X:F2},{Pos.Y:F2}";
        }
    }

    /**
     * @brief Represents an A* [A]-->[B] node link with its traversal cost
     */
    public class Link
    {
        public Node Node;   // endpoint(B) of the link
        public float Cost;  // pre-calculated cost of traversing this link

        // does not initialize cost
        public Link(Node node)
        {
            Node = node;
        }

        public Link(Node node, float cost)
        {
            Node = node;
            Cost = cost;
        }
    }

}
