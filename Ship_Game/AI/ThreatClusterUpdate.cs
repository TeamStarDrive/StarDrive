using System;
using System.Collections.Generic;
using SDGraphics;
using SDUtils;
using Ship_Game.Ships;

namespace Ship_Game.AI;

// Utility for atomically updating a ThreatCluster
public sealed class ClusterUpdate
{
    public readonly ThreatCluster Cluster; // cluster to be updated
    public readonly Array<Ship> ObservedShips = new(); // ships to be added
    // platforms and stations it held that were out of view, which cannot have moved off
    readonly Array<Ship> UnseenStatics = new();
    // platforms and stations the clusters merged into this one held before this observation
    readonly Array<Ship> HeldByMerged = new();
    public AABoundingBox2D Bounds;

    // whether this cluster was fully observed by us
    // if a cluster is fully observed and none of its ships is seen, it will be removed from threats,
    // unless it holds platforms or stations out of view
    public bool FullyObserved;

    // if true, this cluster will be dumped at the end of the update
    // otherwise it is kept in ThreatMatrix 'memory'
    bool ForceRemove;

    public ClusterUpdate(ThreatCluster cluster, Ship s)
    {
        Cluster = cluster;
        ObservedShips.Add(s);
        
        AABoundingBox2D newBounds = new(s.Position, ThreatMatrix.InitialClusterRadius);
        SetBounds(newBounds);

        // new clusters are always fully explored, this ensures they get removed after merging
        FullyObserved = true;
    }

    public ClusterUpdate(ThreatCluster cluster, Vector2 pos, float radius)
    {
        Cluster = cluster;
        Bounds = new(pos, radius);
    }

    // Reset for the start of a new observation update
    public void ResetForObservation(bool isOwnerCluster)
    {
        ObservedShips.Clear();
        UnseenStatics.Clear();
        HeldByMerged.Clear();
        ForceRemove = false;
        // owner's own clusters are always fully observed
        FullyObserved = isOwnerCluster;
    }

    /// <summary>
    /// Adds a ship to the cluster update.
    /// Returns TRUE if ThreatCluster bounds was updated
    /// </summary>
    public bool AddShip(Ship s)
    {
        ObservedShips.Add(s);

        AABoundingBox2D newBounds = new(s.Position, s.Radius);
        return SetBounds(newBounds);
    }

    /// <summary>
    /// Merge this cluster with `u`, and mark `u` for deletion
    /// Returns true if Merge resulted in bounds change
    /// </summary>
    public bool Merge(ClusterUpdate u)
    {
        if (u.ObservedShips.NotEmpty)
        {
            AABoundingBox2D newBounds = Bounds;
            foreach (Ship s in u.ObservedShips)
            {
                newBounds = newBounds.Merge(new(s.Position, s.Radius));
                #if DEBUG
                    if (ObservedShips.ContainsRef(s))
                        throw new InvalidOperationException("ThreatCluster Merge Double Insert bug");
                #endif
                ObservedShips.Add(s);
            }

            HoldStatics(u.Cluster.Ships);
            HeldByMerged.AddRange(u.HeldByMerged);

            // mark for deletion
            u.ObservedShips.Clear();
            u.ForceRemove = true;

            return SetBounds(newBounds);
        }
        return false;
    }

    void HoldStatics(Ship[] ships)
    {
        for (int i = 0; i < ships.Length; ++i)
        {
            Ship s = ships[i];
            if (s.Active && s.IsPlatformOrStation)
                HeldByMerged.Add(s);
        }
    }

    bool SetBounds(in AABoundingBox2D newBounds)
    {
        if (Bounds != newBounds)
        {
            Bounds = newBounds;
            Cluster.Position = Bounds.Center;
            // this is a bit bigger than the actual radius, but only way to ensure
            // that all ships are within the radius, without having to loop over all ships
            Cluster.Radius = Bounds.Diagonal*0.5f;
            return true;
        }
        return false;
    }

    // TRUE if this cluster should be removed from Threats
    public bool ShouldBeRemoved
    {
        // fully observed clusters with nothing left in them MUST be removed, and memory runs out only for clusters not seen this pass
        get => ForceRemove
            || (Cluster.TimeToLive < 0f && ObservedShips.IsEmpty)
            || (FullyObserved && ObservedShips.IsEmpty && UnseenStatics.IsEmpty);
        set => ForceRemove = value;
    }

    // a cluster seen in part, or fully observed, keeps its live platforms and stations that were not seen anywhere
    public void KeepUnseenStatics(HashSet<Ship> seen)
    {
        if (ForceRemove || (ObservedShips.IsEmpty && !FullyObserved))
            return;

        AddUnseenStatics(Cluster.Ships, seen);
        AddUnseenStatics(HeldByMerged.AsSpan(), seen);
    }

    // removes and returns the unseen platforms and stations with no ship seen in this cluster within the radius, null if none
    public Ship[] TakeStaticsAwayFromSeen(float radius)
    {
        if (ObservedShips.IsEmpty || UnseenStatics.IsEmpty)
            return null;

        Array<Ship> away = null;
        for (int i = UnseenStatics.Count - 1; i >= 0; --i)
        {
            Ship s = UnseenStatics[i];
            if (!AnySeenWithin(s.Position, radius))
            {
                (away ??= new()).Add(s);
                UnseenStatics.RemoveAtSwapLast(i);
            }
        }
        return away?.ToArray();
    }

    bool AnySeenWithin(Vector2 pos, float radius)
    {
        for (int i = 0; i < ObservedShips.Count; ++i)
            if (ObservedShips[i].Position.InRadius(pos, radius))
                return true;
        return false;
    }

    // a new cluster holding only these platforms and stations out of view, where they are
    public void HoldOnlyUnseen(Ship[] statics)
    {
        ObservedShips.Clear();
        UnseenStatics.AddRange(statics, statics.Length);

        // remembered as long as a group of all their strength
        Cluster.Strength = statics.Sum(s => s.GetStrength());
        Cluster.SetTimeToLive(statics[0].System != null);
    }

    void AddUnseenStatics(ReadOnlySpan<Ship> held, HashSet<Ship> seen)
    {
        for (int i = 0; i < held.Length; ++i)
        {
            Ship s = held[i];
            if (s.Active && s.IsPlatformOrStation && s.Loyalty == Cluster.Loyalty
                && !seen.Contains(s) && !UnseenStatics.ContainsRef(s))
            {
                UnseenStatics.Add(s);
            }
        }
    }

    /// <summary>
    /// Updates the ThreatCluster with its new state.
    /// Returns TRUE if cluster bounds changed.
    /// </summary>
    /// <param name="timeStep">Elapsed time since last update</param>
    /// <param name="owner">The empire which owns the ThreatMatrix</param>
    public bool Update(FixedSimTime timeStep, Empire owner)
    {
        // nothing was observed this frame
        if (ObservedShips.IsEmpty)
        {
            bool staticsMoved = false;
            // fully observed: only its platforms and stations out of view are left
            if (UnseenStatics.NotEmpty)
            {
                Ship[] statics = UnseenStatics.ToArr();
                Cluster.System = SetShips(statics, owner) ?? Cluster.System;
                staticsMoved = SetBounds(BoundsOf(statics));
            }
            else
            {
                // remove inactive ships to avoid ships remaining
                // in stale clusters and causing OOM
                Cluster.Ships = Cluster.Ships.Filter(s => s.Active);
            }
            Cluster.TimeToLive -= timeStep.FixedTime;
            return staticsMoved;
        }

        // In the case when we have observed some ships
        // we will always take the observed amount at face value.
        // This makes everything simpler and Proper sensor ranges
        // will most edge cases automatically
        //
        // The AI difficulty settings should ensure a little bit more
        // ships are always sent.
        // Only platforms and stations out of view stay, since they cannot have moved off.

        Ship[] observed = ObservedShips.ToArr();
        Ship[] ships = UnseenStatics.IsEmpty ? observed : observed.Concat(UnseenStatics.ToArr());
        SolarSystem system = SetShips(ships, owner);

        // Unfortunately we also need to recalculate the Bounds
        // since the cluster can be a traveling fleet, in which case
        // the regular AddShip() bounds would forever expand the cluster
        bool boundsChanged = SetBounds(BoundsOf(ships));

        //// TODO: add a fast way to test with Radius in InfluenceTree
        //bool inBorders = owner.Universe.Influence.IsInInfluenceOf(owner, AveragePos);

        Cluster.System = system ?? (SolarSystem)owner.Universe.SystemsTree.FindOne(Cluster.Position, Cluster.Radius);
        Cluster.SetTimeToLive(Cluster.System != null);

        return boundsChanged;
    }

    // platforms and stations out of view count where they are, so they never travel with the ships seen beside them
    static AABoundingBox2D BoundsOf(Ship[] ships)
    {
        AABoundingBox2D bounds = new(ships[0].Position, ThreatMatrix.InitialClusterRadius);
        for (int i = 1; i < ships.Length; ++i)
        {
            Ship s = ships[i];
            bounds = bounds.Merge(new(s.Position, s.Radius));
        }
        return bounds;
    }

    // sets the cluster's ships and their totals, returning the first system any of them is in
    SolarSystem SetShips(Ship[] ships, Empire owner)
    {
        bool inBorders = false;
        bool hasStarBases = false;
        float strength = 0f;
        float researchStationsStrength = 0f;
        float inhibitionRadius = 0f;
        SolarSystem system = null;

        for (int i = 0; i < ships.Length; ++i)
        {
            Ship s = ships[i];
            strength += s.GetStrength();
            if (s.IsStation)
                hasStarBases = true;

            inhibitionRadius = Math.Max(inhibitionRadius, s.InhibitionRadius);

            if (s.IsResearchStation)
                researchStationsStrength += s.GetStrength();

            if (!inBorders && s.IsInBordersOf(owner))
                inBorders = true;

            system ??= s.System;
        }

        Cluster.Strength = strength;
        Cluster.StrengthNoResearchStations = strength - researchStationsStrength;
        Cluster.Ships = ships;
        Cluster.HasStarBases = hasStarBases;
        Cluster.InhibitionRadius = inhibitionRadius;
        Cluster.InBorders = inBorders;
        return system;
    }
}
