using SDUtils;
using Ship_Game.Ships;

namespace Ship_Game.AI;

// Utility for atomically updating a ThreatCluster
public sealed class ClusterUpdate
{
    public readonly ThreatCluster Cluster; // cluster to be updated
    public readonly Array<Ship> Ships = new(); // ships to be added
    public AABoundingBox2D Bounds;

    // whether this cluster was fully observed by us
    // if a cluster is fully observed and has no ships, it will be removed from threats
    public bool FullyObserved;

    public ClusterUpdate(ThreatCluster cluster, Ship s)
    {
        Cluster = cluster;
        Bounds = new(s.Position, ThreatMatrix.InitialClusterRadius);
        Ships.Add(s);
        ApplyBoundsOnly();
    }

    public void Reset()
    {
        Ships.Clear();
    }

    public void AddShip(Ship s)
    {
        Bounds = Bounds.Merge(new(s.Position, s.Radius));
        Ships.Add(s);
        ApplyBoundsOnly();
    }

    public void Merge(ClusterUpdate u)
    {
        if (u.Ships.NotEmpty)
        {
            foreach (Ship s in u.Ships)
            {
                Bounds = Bounds.Merge(new(s.Position, s.Radius));
                Ships.Add(s);
            }
            ApplyBoundsOnly();
        }
        u.Reset();
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
            {
                Reset();
                return false; // fully observed but empty clusters MUST be removed
            }

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
