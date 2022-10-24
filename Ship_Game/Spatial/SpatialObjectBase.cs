using Ship_Game.Data.Serialization;
using SDGraphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Diagnostics.Contracts;
using Ship_Game.AI;

namespace Ship_Game.Spatial;

[StarDataType]
public class SpatialObjectBase
{
    // this must be set during concrete object Initialization
    [StarData] public bool Active;
    [StarData] public readonly GameObjectType Type;
    [StarData] public Vector2 Position;
    [StarData] public float Radius = 1f;

    public int SpatialIndex = -1;

    // if true, object is never added to spatial manager
    public bool DisableSpatialCollision = false;

    // if true, this object should be reinserted to spatial manager
    public bool ReinsertSpatial = false;

    [StarDataConstructor]
    public SpatialObjectBase(GameObjectType type)
    {
        Type = type;
    }
    
    // TODO: make this abstract
    [Pure] public int GetLoyaltyId()
    {
        if (Type == GameObjectType.Proj) return ((Projectile)this).Loyalty?.Id ?? 0;
        if (Type == GameObjectType.Beam) return ((Beam)this).Loyalty?.Id ?? 0;
        if (Type == GameObjectType.Ship) return ((Ship)this).Loyalty.Id;
        if (Type == GameObjectType.ShipModule) return ((ShipModule)this).GetParent().Loyalty.Id;

        //if (Type == GameObjectType.SolarSystem) return 0;
        //if (Type == GameObjectType.SolarBody) return 0; // Asteroid, Moon
        if (Type == GameObjectType.Planet) return ((Planet)this).Owner?.Id ?? 0;
        if (Type == GameObjectType.ThreatCluster) return ((ThreatCluster)this).Loyalty?.Id ?? 0;
        return 0;
    }
    
    // TODO: make this abstract
    [Pure] public Empire GetLoyalty()
    {
        if (Type == GameObjectType.Proj) return ((Projectile)this).Loyalty;
        if (Type == GameObjectType.Beam) return ((Beam)this).Loyalty;
        if (Type == GameObjectType.Ship) return ((Ship)this).Loyalty;
        if (Type == GameObjectType.ShipModule) return ((ShipModule)this).GetParent().Loyalty;

        if (Type == GameObjectType.Planet) return ((Planet)this).Owner;
        if (Type == GameObjectType.ThreatCluster) return ((ThreatCluster)this).Loyalty;
        return null;
    }
}