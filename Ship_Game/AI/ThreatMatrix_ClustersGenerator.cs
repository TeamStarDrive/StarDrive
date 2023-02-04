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
        readonly HashSet<ThreatCluster> Clusters = new();

        public ClustersGenerator(ThreatMatrix threats)
        {
            Threats = threats;
        }
        
        public ThreatCluster[] UpdateAndGetResults(FixedSimTime timeStep, Empire owner, bool isOwnerCluster)
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
                    // this will update all cluster stats and bounds
                    if (c.Update.Update(timeStep, owner, isOwnerCluster: isOwnerCluster))
                        Threats.ClustersMap.Update(c);

                    results.Add(c);
                }
            }
            return results.ToArr();
        }

        void InitClusters(ThreatCluster[] clusters, bool isOwnerCluster)
        {
            // reset the clusters for observation
            for (int i = 0; i < clusters.Length; ++i)
            {
                ThreatCluster c = clusters[i];
                Clusters.Add(c);
                c.Update.ResetForObservation(isOwnerCluster);
            }
        }

        public void CreateOurClusters(Empire owner, Ship[] ourShips)
        {
            InitClusters(Threats.OurClusters, isOwnerCluster: true);

            // create an observation of our own forces, these are always fully observed
            for (int i = 0; i < ourShips.Length; ++i)
            {
                Ship s = ourShips[i];
                if (s.Loyalty == owner) // this can mismatch if our ship suddenly gets captured
                {
                    var u = AddSeenShip(owner, ourShips[i], OwnClusterJoinRadius);
                    u.Update.FullyObserved = true; // our clusters always fully explored
                }
                // else: this is no longer our ship, it was probably captured by someone
            }

            MergeOverlappingClusters();
        }

        public void CreateAndUpdateRivalClusters(Empire owner, ThreatCluster[] ours, Span<Ship> nonCombatShips)
        {
            InitClusters(Threats.RivalClusters, isOwnerCluster: false);

            // set whether these clusters were fully observed or not
            HashSet<ThreatCluster> observed = ObserveRivalClusters(ours, nonCombatShips);
            foreach (ThreatCluster c in observed)
                c.Update.FullyObserved = true;

            // insert new observation, creating&merging clusters along the way
            lock (Threats.Seen)
            {
                foreach (Ship seen in Threats.Seen)
                {
                    Empire sLoyalty = seen.Loyalty;
                    if (sLoyalty != owner) // this can accidentally equal if THEIR ship gets captured by US
                    {
                        AddSeenShip(sLoyalty, seen, RivalClusterJoinRadius);
                    }
                    // else: we have captured one of the seen ships, just ignore it
                }
            }

            MergeOverlappingClusters();
        }

        HashSet<ThreatCluster> ObserveRivalClusters(ThreatCluster[] ours, Span<Ship> nonCombatShips)
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

            // noncombat ships are not part of any clusters, so we have to scan from each one
            foreach (Ship nonCombat in nonCombatShips)
            {
                float scanRadius = nonCombat.AI.GetSensorRadius(out Ship source);
                ObserveRivalsFrom(observed, source.Position, scanRadius);
            }

            return observed;
        }

        void ObserveRivalsFrom(HashSet<ThreatCluster> observed, Vector2 pos, float scanRadius)
        {
            SearchOptions opt = new(pos, scanRadius, GameObjectType.ThreatCluster)
            {
                ExcludeLoyalty = Threats.Owner,
                Type = GameObjectType.ThreatCluster,
            };
            ThreatCluster[] clusters = Threats.FindClusters(ref opt);

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
            Clusters.Add(c);
            Threats.ClustersMap.Insert(c);
            return c;
        }

        ThreatCluster AddToExistingCluster(ThreatCluster c, Ship ship)
        {
            bool boundsChanged = c.Update.AddShip(ship);

            #if DEBUG
                // !!! IF THIS HAPPENS, THEN THERE IS A MAJOR DEFECT IN THE THREAT MATRIX GENERATOR !!!
                // !!! THIS SHOULD NORMALLY NEVER HAPPEN, UNLESS WE HAVE A BUG !!!
                if (!Clusters.Contains(c))
                    throw new KeyNotFoundException("Existing Qtree ThreatCluster was not found in generator Clusters");
            #endif

            if (boundsChanged)
            {
                Threats.ClustersMap.Update(c);
            }
            else
            {
                #if DEBUG
                    if (!Threats.ClustersMap.Contains(c))
                        throw new KeyNotFoundException("Existing Qtree ThreatCluster was not found in ClustersMap");
                #endif
            }

            return c;
        }

        ThreatCluster AddSeenShip(Empire loyalty, Ship ship, float clusterJoinRadius)
        {
            SearchOptions opt = new(ship.Position, clusterJoinRadius) { OnlyLoyalty = loyalty };

            ThreatCluster[] clusters = Threats.FindClusters(ref opt);
            if (clusters.Length == 0) // no existing cluster, add one
            {
                return AddNewCluster(loyalty, ship);
            }
            if (clusters.Length == 1 && CanJoinCluster(clusters[0], ship)) // add to existing cluster
            {
                return AddToExistingCluster(clusters[0], ship);
            }

            // there are more than 1 clusters, perhaps because new ship
            // was inserted between 2 existing ones
            ThreatCluster c = ChooseClosestJoinableCluster(clusters, ship);
            if (c == null) // we could not join any of the clusters
            {
                return AddNewCluster(loyalty, ship);
            }
            
            // attempt to merge clusters if they are close enough
            // and don't forget to add the ship to the new merged cluster
            MergeClusters(c, clusters);
            return AddToExistingCluster(c, ship);
        }

        void MergeOverlappingClusters()
        {
            foreach (ThreatCluster c in Clusters)
            {
                if (c.Update.ObservedShips.NotEmpty)
                {
                    SearchOptions opt = new(c.Position, c.Radius) { OnlyLoyalty = c.Loyalty };
                    ThreatCluster[] clusters = Threats.FindClusters(ref opt);
                    if (clusters.Length > 1)
                    {
                        ThreatCluster root = ChooseBiggestCluster(clusters);
                        if (MergeClusters(root, clusters))
                        {
                            if (!Threats.ClustersMap.Update(root))
                                throw new("Failed to update ThreatCluster");
                        }
                    }
                }
            }
        }

        static bool CanJoinCluster(ThreatCluster cluster, Ship ship)
        {
            return ship.Position.InRadius(cluster.Position, MaxClusterJoinDistance);
        }

        // choose a closest cluster that we can actually join as well
        static ThreatCluster ChooseClosestJoinableCluster(ThreatCluster[] clusters, Ship toShip)
        {
            // this is FindMinFiltered, but inlined for perf reasons
            float closestDist = float.MaxValue;
            ThreatCluster closest = null;
            for (int i = 0 ; i < clusters.Length; ++i)
            {
                ThreatCluster c = clusters[i];
                float sqDist = c.Position.SqDist(toShip.Position);
                if (sqDist < closestDist && CanJoinCluster(c, toShip))
                {
                    closestDist = sqDist;
                    closest = c;
                }
            }
            return closest;
        }

        static ThreatCluster ChooseBiggestCluster(ThreatCluster[] clusters)
        {
            // this is a series of FindMax's inlined for perf reasons
            ThreatCluster biggest = null;
            int biggestCount = 0;
            for (int i = 0; i < clusters.Length; ++i)
            {
                ThreatCluster c = clusters[i];
                if (biggestCount < c.Update.ObservedShips.Count)
                {
                    biggestCount = c.Update.ObservedShips.Count;
                    biggest = c;
                }
                else if (biggestCount < c.Ships.Length)
                {
                    biggestCount = c.Ships.Length;
                    biggest = c;
                }
            }
            if (biggestCount > 0)
                return biggest;

            // fall back to finding cluster with biggest area
            float biggestArea = 0f;
            for (int i = 0; i < clusters.Length; ++i)
            {
                ThreatCluster c = clusters[i];
                float area = c.Update.Bounds.Area;
                if (biggestArea < area)
                {
                    biggestArea = area;
                    biggest = c;
                }
            }
            return biggest;
        }

        // @return TRUE if anything was merged
        bool MergeClusters(ThreatCluster root, ThreatCluster[] clusters)
        {
            bool anyMerges = false;
            for (int i = 0; i < clusters.Length; ++i)
            {
                ThreatCluster c = clusters[i];
                if (root != c && c.Update.ObservedShips.NotEmpty &&
                    root.Position.InRadius(c.Position, MaxClustersMergeDistance))
                {
                    anyMerges = true;
                    bool boundsChanged = root.Update.Merge(c.Update);
                    Threats.ClustersMap.Remove(c);
                    if (boundsChanged)
                        Threats.ClustersMap.Update(root);
                }
            }
            return anyMerges;
        }
    }
}
