using SDUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Ships;

namespace Ship_Game.Universe
{
    public struct InfluenceConnection : IEquatable<InfluenceConnection>
    {
        public Empire.InfluenceNode Node1;
        public Empire.InfluenceNode Node2;

        public InfluenceConnection(in Empire.InfluenceNode node1, in Empire.InfluenceNode node2)
        {
            if (node1.Source.Id < node2.Source.Id)
            {
                Node1 = node1;
                Node2 = node2;
            }
            else
            {
                Node1 = node2;
                Node2 = node1;
            }
        }

        public bool Equals(InfluenceConnection other)
        {
            return Node1.Source == other.Node1.Source && Node2.Source == other.Node2.Source;
        }

        public override bool Equals(object obj)
        {
            return obj is InfluenceConnection other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Node1.Source.Id.GetHashCode() + Node2.Source.Id.GetHashCode();
        }
    }


    /// <summary>
    /// Caches an Empire's border node connections
    /// </summary>
    public class BorderNodeCache
    {
        Empire Owner;
        readonly HashSet<GameObject> KnownNodes = new();

        public HashSet<InfluenceConnection> Connections = new();
        public Empire.InfluenceNode[] BorderNodes;

        public BorderNodeCache()
        {
        }

        public void Update(Empire empire)
        {
            Owner = empire;
            RemoveInActiveNodes();

            // NOTE: currently BorderNodes array is rebuilt every frame
            Empire.InfluenceNode[] nodes = empire.BorderNodes;
            BorderNodes = nodes;

            for (int i = 0; i < nodes.Length; ++i)
            {
                ref Empire.InfluenceNode node = ref nodes[i];

                bool tracked = KnownNodes.Contains(node.Source);
                if (tracked && !node.KnownToPlayer)
                {
                    // stop tracking if we don't know about it anymore
                    RemoveConnections(node.Source);
                }
                else if (!tracked && node.KnownToPlayer)
                {
                    // start tracking if we know this node
                    CreateConnections(node, nodes);
                }
            }
        }

        void CreateConnections(in Empire.InfluenceNode node, Empire.InfluenceNode[] nodes)
        {
            KnownNodes.Add(node.Source);

            // make connection bridges
            for (int i = 0; i < nodes.Length; i++)
            {
                ref Empire.InfluenceNode in2 = ref nodes[i];
                if (in2.KnownToPlayer &&
                    // ignore self
                    in2.Source != node.Source &&
                    // require one of the sources to be a projector
                    in2.Source is Ship &&
                    // ensure we don't connect too far
                    in2.Position.InRadius(node.Position, node.Radius + in2.Radius + 50_000f))
                {
                    Connections.Add(new InfluenceConnection(node, in2));
                }
            }
        }

        void RemoveConnections(GameObject source)
        {
            KnownNodes.Remove(source);
            RemoveConnectionBridges(source);
        }

        void RemoveConnectionBridges(GameObject source)
        {
            Connections.RemoveWhere(c => c.Node1.Source == source || c.Node2.Source == source);
        }

        void RemoveInActiveNodes()
        {
            KnownNodes.RemoveWhere(source =>
            {
                // source has died, or owner changed
                if (!source.Active || (source is Planet p && p.Owner != Owner))
                {
                    RemoveConnectionBridges(source);
                    return true;
                }
                return false;
            });
        }
    }
}
