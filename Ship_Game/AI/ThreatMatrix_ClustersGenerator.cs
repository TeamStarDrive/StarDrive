using System;
using System.Collections.Generic;
using SDGraphics;
using SDUtils;
using Ship_Game.Ships;
using Ship_Game.Spatial;

namespace Ship_Game.AI;

public sealed partial class ThreatMatrix
{
    class ClustersGenerator
    {
        readonly ThreatMatrix Threats;
        readonly Array<ThreatCluster> Clusters = new();

        public ClustersGenerator(ThreatMatrix threats)
        {
            Threats = threats;
        }
        
        public ThreatCluster[] GetResults(Empire owner, bool isOwnerCluster)
        {
            Array<ThreatCluster> results = new();
            foreach (ThreatCluster c in Clusters)
            {
                if (c.Update.ShouldBeRemoved)
                {
                    Threats.ClustersMap.Remove(c);
                }
                else
                {
                    c.Update.Update(owner, isOwnerCluster: isOwnerCluster);
                    results.Add(c);
                }
            }
            return results.ToArr();
        }

        void InitClusters(ThreatCluster[] clusters)
        {
            Clusters.Capacity = clusters.Length;
            // also reset the clusters for observation
            for (int i = 0; i < clusters.Length; ++i)
            {
                ThreatCluster c = clusters[i];
                Clusters.Add(c);
                c.Update.ResetForObservation();
            }
        }

        public void CreateOurClusters(Empire owner, Ship[] ourShips)
        {
            InitClusters(Threats.OurClusters);

            // create an observation of our own forces, these are always fully observed
            for (int i = 0; i < ourShips.Length; ++i)
            {
                var u = AddSeenShip(owner, ourShips[i], OwnClusterJoinRadius);
                u.Update.FullyObserved = true; // our clusters always fully explored
            }

            MergeOverlappingClusters();
        }

        public void CreateAndUpdateRivalClusters(ThreatCluster[] ours, Ship[] ourProjectors)
        {
            InitClusters(Threats.RivalClusters);

            // set whether these clusters were fully observed or not
            HashSet<ThreatCluster> observed = ObserveRivalClusters(ours, ourProjectors);
            foreach (ThreatCluster c in observed)
                c.Update.FullyObserved = true;

            // insert new observation, creating&merging clusters along the way
            foreach (Ship seen in Threats.Seen)
                AddSeenShip(seen.Loyalty, seen, RivalClusterJoinRadius);

            MergeOverlappingClusters();
        }

        HashSet<ThreatCluster> ObserveRivalClusters(ThreatCluster[] ours, Ship[] ourProjectors)
        {
            HashSet<ThreatCluster> observed = new();

            // scan for rival clusters from all of our new clusters
            foreach (ThreatCluster c in ours)
            {
                if (CanObserveRivals(c))
                {
                    float maxSensorRange = c.Ships.Max(s => s.AI.GetSensorRadius());
                    float scanRadius = c.Radius + maxSensorRange;
                    ObserveRivalsFrom(observed, c.Position, scanRadius);
                }
            }

            // TODO: should we also scan from planets?

            // projectors are not part of any clusters, so we have to scan from each one
            for (int i = 0; i < ourProjectors.Length; ++i)
            {
                float scanRadius = ourProjectors[i].AI.GetSensorRadius(out Ship source);
                ObserveRivalsFrom(observed, source.Position, scanRadius);
            }

            return observed;
        }

        void ObserveRivalsFrom(HashSet<ThreatCluster> observed, Vector2 pos, float scanRadius)
        {
            ThreatCluster[] clusters = Threats.FindClusters(new(pos, scanRadius, GameObjectType.ThreatCluster)
            {
                ExcludeLoyalty = Threats.Owner,
                Type = GameObjectType.ThreatCluster,
            });

            for (int i = 0; i < clusters.Length; ++i)
            {
                ThreatCluster c = clusters[i];
                // in order for a cluster to register as observed, its center must be fully scanned
                if (c.Position.InRadius(pos, scanRadius+FullyObservedClusterRadius))
                    observed.Add(c);
            }
        }

        // A ThreatCluster can only observe rivals if it has alive ships
        // Dying ships would just fail to scan anything
        static bool CanObserveRivals(ThreatCluster c)
        {
            foreach (Ship s in c.Ships)
                if (s.IsAlive) return true;
            return false;
        }

        ThreatCluster AddNewCluster(Empire loyalty, Ship ship)
        {
            ThreatCluster c = new(loyalty, ship);
            // new clusters are always fully explored, this ensures they get removed
            // after merging
            c.Update.FullyObserved = true;
            Clusters.Add(c);
            Threats.ClustersMap.Insert(c);
            return c;
        }

        ThreatCluster AddToExistingCluster(ThreatCluster c, Ship ship)
        {
            c.Update.AddShip(ship);
            #if DEBUG
            if (!Clusters.Contains(c))
                throw new KeyNotFoundException("Existing Qtree ThreatCluster was not found in generator Clusters");
            #endif
            Threats.ClustersMap.InsertOrUpdate(c);
            return c;
        }

        ThreatCluster AddSeenShip(Empire loyalty, Ship ship, float clusterJoinRadius)
        {
            SearchOptions opt = new(ship.Position, clusterJoinRadius) { OnlyLoyalty = loyalty };

            ThreatCluster[] clusters = Threats.FindClusters(opt);
            if (clusters.Length == 0) // no existing cluster, add one
            {
                return AddNewCluster(loyalty, ship);
            }
            if (clusters.Length == 1) // add to existing cluster
            {
                return AddToExistingCluster(clusters[0], ship);
            }

            // there are more than 1 clusters, perhaps because new ship
            // was inserted between 2 existing ones

            // choose an existing ThreatCluster with the most ships
            // and merge all other clusters into it
            ThreatCluster c = ChooseBiggestCluster(clusters);
            MergeClusters(c, clusters);

            // and don't forget to add the ship to the new merged cluster
            return AddToExistingCluster(c, ship);
        }

        void MergeOverlappingClusters()
        {
            foreach (ThreatCluster c in Clusters)
            {
                if (c.Update.Ships.NotEmpty)
                {
                    SearchOptions opt = new(c.Position, c.Radius) { OnlyLoyalty = c.Loyalty };
                    ThreatCluster[] clusters = Threats.FindClusters(opt);
                    if (clusters.Length > 1)
                    {
                        ThreatCluster root = ChooseBiggestCluster(clusters);
                        MergeClusters(root, clusters);
                        if (!Threats.ClustersMap.Update(root))
                            throw new("Failed to update ThreatCluster");
                    }
                }
            }
        }

        static ThreatCluster ChooseBiggestCluster(ThreatCluster[] clusters)
        {
            ThreatCluster c = clusters.FindMax(cl => cl.Update.Ships.Count);
            if (c.Update.Ships.Count != 0) return c;

            c = clusters.FindMax(cl => cl.Ships.Length);
            if (c.Ships.Length != 0) return c;

            return clusters.FindMax(cl => cl.Update.Bounds.Area);
        }

        void MergeClusters(ThreatCluster root, ThreatCluster[] clusters)
        {
            for (int i = 0; i < clusters.Length; ++i)
            {
                ThreatCluster c = clusters[i];
                if (root != c && c.Update.Ships.NotEmpty)
                {
                    root.Update.Merge(c.Update);
                    Threats.ClustersMap.Remove(c);
                }
            }
        }
    }
}
