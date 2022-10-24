using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Ship_Game.Spatial;

namespace Ship_Game.AI
{
    [StarDataType]
    public sealed class ThreatCluster : SpatialObjectBase
    {
        // @baseclass Position: the averaged center of this cluster
        // @baseclass Radius: the radius of this cluster

        // Only one type of loyalty for all ships in this cluster
        [StarData] public Empire Loyalty;

        // If the cluster is within a solarsystem
        [StarData] public SolarSystem System;

        // Strength of all known ships in this threat cluster
        [StarData] public float Strength;

        // These are the ships which we saw in this clusters
        // @warning DO NOT USE Ships[i].Position !! Because that would violate AI vision rules !!
        [StarData] public Array<Ship> Ships = new();

        // TRUE if this cluster contains any starbases or pirate bases
        [StarData] public bool HasStarBases;

        // TRUE if this PIN is inside the parent owner's borders
        [StarData] public bool InBorders;

        // Timer for when this cluster is considered stale
        [StarData] public float ScanTimer;

        [StarDataConstructor]
        public ThreatCluster(Empire loyalty) : base(GameObjectType.Ship)
        {
            Loyalty = loyalty;
        }

        public bool IsHostileTo(Empire threatMatrixOwner)
        {
            // Important to check `threatMatrixOwner` hostileTo `Loyalty`,
            // because that is the correct way from ThreatMatrix perspective
            return Loyalty != threatMatrixOwner && threatMatrixOwner.IsEmpireHostile(Loyalty);
        }
    }
}