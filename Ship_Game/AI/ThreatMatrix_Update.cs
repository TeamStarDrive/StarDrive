using SDUtils;
using Ship_Game.Ships;
using Ship_Game.Spatial;
using System.Collections.Generic;
using SDGraphics;

namespace Ship_Game.AI;

public sealed partial class ThreatMatrix
{
    /// <summary>
    /// The default distance for joining an existing cluster
    /// and thus expanding it. For OUR clusters.
    /// </summary>
    const float OwnClusterJoinRadius = 12_000f;
    
    /// <summary>
    /// The default distance for joining an existing cluster
    /// and thus expanding it. For RIVALS.
    /// </summary>
    const float RivalClusterJoinRadius = 8000f;

    /// <summary>
    /// How much beyond a Cluster's center until a cluster
    /// is considered as fully observed.
    /// If a fully observed cluster contains no ships, it is then deleted.
    /// </summary>
    const float FullyObservedClusterRadius = 1000f;
    
    /// <summary>
    /// Other loyalty ships which were SCANNED between two updates
    /// </summary>
    readonly HashSet<Ship> Seen = new();

    /// <summary>
    /// Mark this other loyalty ship as seen, so that
    /// ThreatMatrix Update can group them together into ThreatClusters
    /// </summary>
    public void SetSeen(Ship other, bool fromBackgroundThread)
    {
        if (other.Loyalty == Owner)
        {
            Log.Error("ThreatMatrix.SetSeen does not accept our own ships!");
            return;
        }

        other.KnownByEmpires.SetSeen(Owner);

        // if the call comes from one of the Ship scan background threats, we need to sync it
        if (fromBackgroundThread)
        {
            lock (Seen) Seen.Add(other);
        }
        else
        {
            Seen.Add(other);
        }
    }

    /// <summary>
    /// Atomically updates the ThreatMatrix and
    /// creates a new array of threat clusters
    /// </summary>
    public void Update()
    {
        Ship[] ourShips = Owner.EmpireShips.OwnedShips;
        Ship[] ourProjectors = Owner.EmpireShips.OwnedProjectors;

        // 1. our clusters are always visible, so just get them out
        ClusterUpdates ourClusters = new(this);
        ourClusters.CreateOurClusters(Owner, ourShips);
        ThreatCluster[] ours = ourClusters.GetResults(Owner, isOwnerCluster:true);

        // 2. get all the clusters for rivals
        ClusterUpdates rivalClusters = new(this);
        rivalClusters.CreateAndUpdateRivalClusters(ours, ourProjectors);
        ThreatCluster[] rivals = rivalClusters.GetResults(Owner, isOwnerCluster:false);

        // 3. Update the list of clusters and UpdateAll ClustersMap
        //    to handle deleted clusters
        Seen.Clear();
        OurClusters = ours;
        RivalClusters = rivals;
        ThreatCluster[] allClusters = ours.Concat(rivals);
        ClustersMap.UpdateAll(allClusters);
    }

    class ClusterUpdates
    {
        readonly ThreatMatrix Threats;
        readonly Map<ThreatCluster, ClusterUpdate> Updates = new();

        public ClusterUpdates(ThreatMatrix threats)
        {
            Threats = threats;
        }
        
        public ThreatCluster[] GetResults(Empire owner, bool isOwnerCluster)
        {
            Array<ThreatCluster> results = new();
            foreach (KeyValuePair<ThreatCluster, ClusterUpdate> kv in Updates)
                if (kv.Value.Apply(owner, isOwnerCluster: isOwnerCluster))
                    results.Add(kv.Key);
            return results.ToArr();
        }

        public void CreateOurClusters(Empire owner, Ship[] ourShips)
        {
            // create an observation of our own forces, these are always fully observed
            for (int i = 0; i < ourShips.Length; ++i)
            {
                var u = AddSeenShip(owner, ourShips[i], OwnClusterJoinRadius);
                u.FullyObserved = true;
            }
        }

        public void CreateAndUpdateRivalClusters(ThreatCluster[] ours, Ship[] ourProjectors)
        {
            foreach (ThreatCluster cluster in Threats.RivalClusters)
                Updates.Add(cluster, new(cluster));

            // set whether these clusters were fully observed or not
            HashSet<ThreatCluster> observed = ObserveRivalClusters(ours, ourProjectors);
            foreach (ThreatCluster c in observed)
                Updates[c].FullyObserved = true;

            // insert new observation, creating&merging clusters along the way
            foreach (Ship seen in Threats.Seen)
                AddSeenShip(seen.Loyalty, seen, RivalClusterJoinRadius);
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

        ClusterUpdate AddUpdate(ThreatCluster cluster)
        {
            ClusterUpdate update = new(cluster, fullyObserved:false);
            Updates.Add(cluster, update);
            return update;
        }

        ClusterUpdate GetOrCreateUpdate(ThreatCluster cluster)
        {
            if (Updates.TryGetValue(cluster, out ClusterUpdate update)) // update existing
                return update;
            return AddUpdate(cluster); // add new
        }

        ClusterUpdate AddSeenShip(Empire loyalty, Ship ship, float clusterJoinRadius)
        {
            SearchOptions opt = new(ship.Position, clusterJoinRadius)
            {
                OnlyLoyalty = loyalty
            };

            ThreatCluster[] clusters = Threats.FindClusters(opt);
            ThreatCluster cluster = MergeClusters(clusters);

            bool addNew = (cluster == null);
            ClusterUpdate update = addNew ? AddUpdate(new(loyalty))
                                          : GetOrCreateUpdate(cluster);
            update.AddShip(ship);

            if (addNew) Threats.ClustersMap.Insert(update.Cluster);
            else        Threats.ClustersMap.Update(update.Cluster);
            return update;
        }

        ThreatCluster MergeClusters(ThreatCluster[] clusters)
        {
            if (clusters.Length > 1)
            {
                ThreatCluster c1 = clusters[0];
                for (int i = 1; i < clusters.Length; ++i)
                {
                    ThreatCluster c2 = clusters[i];

                    GetOrCreateUpdate(c1).Merge(GetOrCreateUpdate(c2));
                    Updates.Remove(c2);
                    Threats.ClustersMap.Remove(c2);
                }
                return c1;
            }
            return clusters.Length == 1 ? clusters[0] : null;
        }
    }

    // Utility for atomically updating a ThreatCluster
    class ClusterUpdate
    {
        public readonly ThreatCluster Cluster; // cluster to be updated
        readonly Array<Ship> Ships = new(); // ships to be added
        AABoundingBox2D Bounds;

        // whether this cluster was fully observed by us
        // if a cluster is fully observed and has no ships, it will be removed from threats
        public bool FullyObserved;

        public ClusterUpdate(ThreatCluster cluster, bool fullyObserved = false)
        {
            Cluster = cluster;
            FullyObserved = fullyObserved;
        }
        
        public void AddShip(Ship s)
        {
            if (Ships.Count == 0)
                Bounds = new(s.Position, 500);
            else
                Bounds.Expand(s.Position);

            if (Ships.ContainsRef(s))
                throw new($"ClusterUpdate.AddShip failed: ship already exists! {s}");
            
            Ships.Add(s);
            ApplyBoundsOnly();
        }

        public void Merge(ClusterUpdate u)
        {
            // if ships are empty, then Bounds will be invalid
            if (u.Ships.NotEmpty) // is it even worth to merge?
            {
                if (Ships.NotEmpty) // full merge
                    Bounds = Bounds.Merge(u.Bounds);
                else
                    Bounds = u.Bounds; // use u.Bounds, whatever it is
            }

            Ships.AddRange(u.Ships);
            ApplyBoundsOnly();
        }

        void ApplyBoundsOnly()
        {
            Cluster.Position = Bounds.Center;
            // this is a bit bigger than the actual radius, but only way to ensure
            // that all ships are within the radius, without having to loop over all ships
            Cluster.Radius = Bounds.Diagonal*0.5f;
        }

        /// <summary>
        /// Updates the ThreatCluster with its new state.
        /// </summary>
        /// <param name="owner">The empire which owns the ThreatMatrix</param>
        /// <param name="isOwnerCluster">if true, this cluster belongs to Owner (observation of self)</param>
        /// <returns>TRUE if cluster is valid and should be added, FALSE if it should be removed</returns>
        public bool Apply(Empire owner, bool isOwnerCluster)
        {
            if (Ships.IsEmpty)
            {
                if (FullyObserved)
                    return false; // fully observed but empty clusters MUST be removed

                // keep it, BUT remove inactive ships to avoid ships remaining
                // in stale clusters and causing OOM
                RemoveInactiveShips();
                return true;
            }

            // In the case when we have observed some ships
            // we will always take the observed amount at face value.
            // This makes everything simpler and Proper sensor ranges
            // will most edge cases automatically
            //
            // The AI difficulty settings should ensure a little bit more
            // ships are always sent.

            Ship[] ships = Ships.ToArr();

            bool inBorders = false;
            bool hasStarBases = false;
            float strength = 0f;
            SolarSystem system = null;

            for (int i = 0; i < ships.Length; ++i)
            {
                Ship s = ships[i];
                strength += s.GetStrength();
                if (s.IsStation)
                    hasStarBases = true;

                if (!inBorders && s.IsInBordersOf(owner))
                    inBorders = true;

                system ??= s.System;

                if (isOwnerCluster)
                    s.CurrentCluster = Cluster;
            }

            //// TODO: add a fast way to test with Radius in InfluenceTree
            //bool inBorders = owner.Universe.Influence.IsInInfluenceOf(owner, AveragePos);

            ApplyBoundsOnly();
            Cluster.Strength = strength;
            Cluster.Ships = ships;
            Cluster.HasStarBases = hasStarBases;
            Cluster.InBorders = inBorders;
            Cluster.System = system ?? (SolarSystem)owner.Universe.SystemsTree.FindOne(Cluster.Position, Cluster.Radius);
            return true; // keep it
        }

        void RemoveInactiveShips()
        {
            Cluster.Ships = Cluster.Ships.Filter(s => s.Active);
        }
    }

}
