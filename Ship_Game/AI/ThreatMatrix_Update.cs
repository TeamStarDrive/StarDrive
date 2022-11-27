using System;
using SDUtils;
using Ship_Game.Ships;
using System.Collections.Generic;

namespace Ship_Game.AI;

public sealed partial class ThreatMatrix
{
    /// <summary>
    /// The default distance for joining an existing cluster
    /// and thus expanding it. For OUR clusters.
    /// </summary>
    public const float OwnClusterJoinRadius = 12_000f;
    
    /// <summary>
    /// The default distance for joining an existing cluster
    /// and thus expanding it. For RIVALS.
    /// </summary>
    public const float RivalClusterJoinRadius = 8000f;

    /// <summary>
    /// This will prevent clusters from growing infinitely big,
    /// by setting a maximum participation radius.
    /// Ships that diverge outside of this radius will form a new cluster
    /// </summary>
    public const float MaxClusterJoinDistance = 40_000f;

    /// <summary>
    /// Clusters can only be merged together if they are closer than this
    /// distance
    /// </summary>
    public const float MaxClustersMergeDistance = 30_000f;

    /// <summary>
    /// How much beyond a Cluster's center until a cluster
    /// is considered as fully observed.
    /// If a fully observed cluster contains no ships, it is then deleted.
    /// </summary>
    public const float FullyObservedClusterRadius = 1000f;

    /// <summary>
    /// Initial radius of a new cluster. This ensures we don't get too
    /// many tiny clusters and miss some cluster merging opportunities
    /// </summary>
    public const float InitialClusterRadius = 1000f;
    
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
        ClustersGenerator ourClusters = new(this);
        ourClusters.CreateOurClusters(Owner, ourShips);
        ThreatCluster[] ours = ourClusters.GetResults(Owner, isOwnerCluster:true);

        // 2. get all the clusters for rivals
        ClustersGenerator rivalClusters = new(this);
        rivalClusters.CreateAndUpdateRivalClusters(Owner, ours, ourProjectors);
        ThreatCluster[] rivals = rivalClusters.GetResults(Owner, isOwnerCluster:false);

        // 3. Update the list of clusters and UpdateAll ClustersMap
        //    to handle deleted clusters
        lock (Seen) Seen.Clear();
        OurClusters = ours;
        RivalClusters = rivals;

        // TODO: Based on playtesting, figure out if we need this anymore
        //       the new GenericQtree design should make it unnecessary
        //// Deletes excluded clusters from the clusters map
        //// Empty clusters which were fully explored will be removed here
        //ThreatCluster[] allClusters = ours.Concat(rivals);
        //ClustersMap.UpdateAll(allClusters);
    }
}
