using SDUtils;
using Ship_Game.Ships;
using Ship_Game.Spatial;
using System.Collections.Generic;
using SDGraphics;

namespace Ship_Game.AI;

public sealed partial class ThreatMatrix
{
    HashSet<Ship> Seen = new();

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
    /// The default distance for joining an existing cluster
    /// and thus expanding it. For OUR clusters.
    /// </summary>
    const float OwnClusterJoinRadius = 20_000f;
    
    /// <summary>
    /// The default distance for joining an existing cluster
    /// and thus expanding it. For RIVALS.
    /// </summary>
    const float RivalClusterJoinRadius = 10_000f;

    /// <summary>
    /// Atomically updates the ThreatMatrix and
    /// creates a new array of threat clusters
    /// </summary>
    public void Update()
    {
        Ship[] ourShips = Owner.EmpireShips.OwnedShips;
        Ship[] ourProjectors = Owner.EmpireShips.OwnedProjectors;
        
        // 1. get new clusters for us
        // 2. get new clusters for rivals
        // 3. update the Qtree
        var newClusters = new Array<ThreatCluster>();
        UpdateOurClusters(newClusters, ourShips);
        UpdateRivalClusters(newClusters, ourShips, ourProjectors);

        Seen.Clear();

        // update the clusters map
        AllClusters = newClusters.ToArr();
        ClustersMap.UpdateAll(AllClusters);
    }

    void UpdateOurClusters(Array<ThreatCluster> newClusters, Ship[] ourShips)
    {
        // first create an observation of our own forces, these are always fully explored
        Map<ThreatCluster, ClusterUpdate> ourClusterUpdates = new();
        for (int i = 0; i < ourShips.Length; ++i)
        {
            var u = AddSeenShip(ourClusterUpdates, Owner, ourShips[i], OwnClusterJoinRadius);
            u.FullyExplored = true;
        }

        // initialize with our own clusters first
        foreach (var kv in ourClusterUpdates)
        {
            ClusterUpdate update = kv.Value;
            if (update.Apply(Owner))
                newClusters.Add(update.Cluster);
        }
    }

    void UpdateRivalClusters(Array<ThreatCluster> newClusters, Ship[] ourShips, Ship[] ourProjectors)
    {
        HashSet<ThreatCluster> observed = new();

        // this should be pretty slow 😬
        for (int i = 0; i < ourShips.Length; ++i)
            ScanForRivalClusters(observed, ourShips[i]);
        for (int i = 0; i < ourProjectors.Length; ++i)
            ScanForRivalClusters(observed, ourProjectors[i]);

        // create initial updates map
        Map<ThreatCluster, ClusterUpdate> updates = new();
        foreach (ThreatCluster c in observed)
            updates.Add(c, new(c, fullyObserved:true));

        foreach (Ship seen in Seen)
        {
            AddSeenShip(updates, seen.Loyalty, seen, RivalClusterJoinRadius);
        }
        
        // apply the updates
        foreach (var kv in updates)
        {
            ClusterUpdate update = kv.Value;
            if (update.Apply(Owner))
            {
                newClusters.Add(update.Cluster);
            }
        }
    }

    void ScanForRivalClusters(HashSet<ThreatCluster> observed, Ship source)
    {
        float scanRadius = source.AI.GetSensorRadius(out source);
        Vector2 pos = source.Position;
        ThreatCluster[] clusters = FindClusters(new(pos, scanRadius, GameObjectType.ThreatCluster)
        {
            ExcludeLoyalty = Owner,
            Type = GameObjectType.ThreatCluster,
        });

        for (int i = 0; i < clusters.Length; ++i)
        {
            ThreatCluster c = clusters[i];
            // in order for a cluster to register as observed, its center must be fully scanned
            if (c.Position.InRadius(pos, scanRadius+1000f))
                observed.Add(c);
        }
    }

    ClusterUpdate AddSeenShip(Map<ThreatCluster, ClusterUpdate> updates, 
                              Empire loyalty, Ship ship, float clusterJoinRadius)
    {
        SearchOptions opt = new(ship.Position, clusterJoinRadius)
        {
            OnlyLoyalty = loyalty
        };

        ThreatCluster[] clusters = FindClusters(opt);
        ThreatCluster cluster = MergeClusters(updates, clusters);

        bool addNew = (cluster == null);
        ClusterUpdate update;

        if (addNew) // create new!
        {
            cluster = new(loyalty);
            update = new(cluster, fullyObserved:false);
            updates.Add(cluster, update);
        }
        else if (!updates.TryGetValue(cluster, out update)) // update existing
        {
            update = new(cluster, fullyObserved:false);
            updates.Add(cluster, update);
        }

        update.AddShip(ship);

        if (addNew)
            ClustersMap.Insert(cluster);
        else
            ClustersMap.Update(cluster);
        return update;
    }
    
    ThreatCluster MergeClusters(Map<ThreatCluster, ClusterUpdate> updates, ThreatCluster[] clusters)
    {
        if (clusters.Length > 1)
        {
            ThreatCluster c1 = clusters[0];
            for (int i = 1; i < clusters.Length; ++i)
            {
                ThreatCluster c2 = clusters[i];

                updates[c1].Merge(updates[c2]);
                updates.Remove(c2);
                ClustersMap.Remove(c2);
            }
            return c1;
        }
        return clusters.Length == 1 ? clusters[0] : null;
    }

    // Utility for atomically updating a ThreatCluster
    class ClusterUpdate
    {
        public ThreatCluster Cluster; // cluster to be updated
        public Array<Ship> Ships = new(); // ships to be added
        public AABoundingBox2D Bounds;

        public bool FullyExplored;

        public ClusterUpdate(ThreatCluster cluster, bool fullyObserved)
        {
            Cluster = cluster;
            FullyExplored = fullyObserved;
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
            Bounds = Bounds.Merge(u.Bounds);
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
        /// <param name="owner"></param>
        /// <returns>TRUE if cluster was updated, FALSE if it's empty and cleared</returns>
        public bool Apply(Empire owner)
        {
            if (Ships.IsEmpty)
                return false;

            // TODO: filter out old ships versus new ships?
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
            }

            //// TODO: add a fast way to test with Radius in InfluenceTree
            //bool inBorders = owner.Universe.Influence.IsInInfluenceOf(owner, AveragePos);

            ApplyBoundsOnly();
            Cluster.Strength = strength;
            Cluster.Ships = ships;
            Cluster.HasStarBases = hasStarBases;
            Cluster.InBorders = inBorders;
            Cluster.System = system ?? (SolarSystem)owner.Universe.SystemsTree.FindOne(Cluster.Position, Cluster.Radius);
            return true;
        }
    }

}
