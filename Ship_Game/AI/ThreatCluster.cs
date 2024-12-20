﻿using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Ship_Game.Spatial;

namespace Ship_Game.AI;

[StarDataType]
public sealed class ThreatCluster : SpatialObjectBase
{
    // @baseclass Position: the averaged center of this cluster
    // @baseclass Radius: the radius of this cluster

    // Only one type of loyalty for all ships in this cluster
    [StarData] public Empire Loyalty;

    // If the cluster is within a solarsystem
    [StarData] public SolarSystem System;

    // Strength of all known ships in this threat cluster minus research stations
    [StarData] public float StrengthNoResearchStations;

    // Strength of all known ships in this threat cluster
    [StarData] public float Strength;

    // These are the ships which we saw in this clusters
    // @warning DO NOT USE Ships[i].Position !! Because that would violate AI vision rules !!
    // @warning InActive ships will be deleted automatically from this list !!
    [StarData] public Ship[] Ships = Empty<Ship>.Array;

    // TRUE if this cluster contains any starbases or pirate bases
    [StarData] public bool HasStarBases;

    // TRUE if this PIN is inside the parent owner's borders
    [StarData] public bool InBorders;

    // The maximum time to live for this cluster,
    // the default value depends on what kind of a cluster this is
    // Generally in-system clusters survive longer
    [StarData] public float TimeToLive;

    // This is scratch-space for the ThreatMatrix
    public ClusterUpdate Update;

    [StarDataConstructor] ThreatCluster() : base(GameObjectType.ThreatCluster)
    {
    }

    [StarDataDeserialized]
    public void OnDeserialized()
    {
        // calling inside OnDeserialized so that we have a valid Pos and Radius
        Update = new(this, Position, Radius);
    }

    public ThreatCluster(Empire loyalty, Ship ship) : base(GameObjectType.ThreatCluster)
    {
        Loyalty = loyalty;
        Active = true;
        Update = new(this, ship);

        // set some defaults to avoid this cluster being immediately deleted
        Strength = ship.GetStrength();
        SetTimeToLive(ship.System != null);
    }

    public void SetTimeToLive(bool inSystem)
    {
        // in system clusters should live remarkably longer
        if (inSystem)
        {
            // extra time to live based on strength factor
            float extraTime = (Strength / 5_000f) * ThreatMatrix.TimeToLiveInSystem;
            TimeToLive = ThreatMatrix.TimeToLiveInSystem + extraTime;
        }
        else
        {
            TimeToLive = ThreatMatrix.TimeToLiveInDeepSpace;
        }
    }

    public override string ToString()
    {
        return $"Ships={Ships.Length} Strength={Strength} InBorders={InBorders} Loyalty={Loyalty}";
    }

    public bool IsHostileTo(Empire threatMatrixOwner)
    {
        // Important to check `threatMatrixOwner` hostileTo `Loyalty`,
        // because that is the correct way from ThreatMatrix perspective
        if (threatMatrixOwner.WeAreRemnants || Ships.Any(s => !s.IsResearchStation))
            return Loyalty != threatMatrixOwner && threatMatrixOwner.IsEmpireHostile(Loyalty);

        // If the matrix has only research stations, we are hostiles only at war
        return threatMatrixOwner.IsAtWarWith(Loyalty);  

    }
}
