using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Ship_Game.Utils;

namespace Ship_Game.Empires.Components;

/// <summary>
/// Analyzes an Empire's solar systems to detect incoming enemies
/// </summary>
[StarDataType]
public class IncomingThreatDetector
{
    [StarData] Array<IncomingThreat> Threats = new();

    // for UI access, this must be modified atomically
    [StarData] public IncomingThreat[] SystemsWithThreat = Empty<IncomingThreat>.Array;

    // Only for Player warnings
    // Whether a solar system under threat has been notified to the player
    [StarData] BitArray Notified;


    [StarDataConstructor]
    public IncomingThreatDetector()
    {
    }

    public void Clear()
    {
        Threats.Clear();
        SystemsWithThreat = Empty<IncomingThreat>.Array;
    }

    public void Update(Empire owner, FixedSimTime timeStep)
    {
        bool changed = false;
        for (int i = Threats.Count - 1 ; i >= 0; i--)
        {
            if (!Threats[i].Update(timeStep))
            {
                Threats.RemoveAt(i);
                changed = true;
            }
        }

        Array<Fleet> knownFleets = owner.GetKnownHostileFleets();
        if (knownFleets.NotEmpty)
        {
            var systems = owner.GetOwnedSystems();

            for (int i = 0; i < systems.Count; i++)
            {
                var s = systems[i];
                var fleetsInSys = GetFleetsInSys(s, knownFleets, s.Radius*2.2f);
                
                IncomingThreat threat = Threats.Find(t => t.TargetSystem == s);
                if (threat != null)
                {
                    threat.UpdateThreats(fleetsInSys);
                }
                else if (fleetsInSys.Length > 0)
                {
                    Threats.Add(new(owner, s, fleetsInSys));
                    changed = true;
                }
            }
        }

        if (changed)
            SystemsWithThreat = Threats.ToArr();
    }

    Fleet[] GetFleetsInSys(SolarSystem s, Array<Fleet> knownFleets, float radiusToCheck)
    {
        if (knownFleets.IsEmpty)
            return Empty<Fleet>.Array;
        return knownFleets.Filter(f => f.FinalPosition.InRadius(s.Position, radiusToCheck * (f.Owner.isPlayer ? 1.5f : 1f)));
    }

    // only for player
    public void AssessHostilePresenceForPlayerWarnings(Empire owner)
    {
        if (!owner.isPlayer)
            return;

        if (Notified.Values == null)
        {
            var allSystems = owner.Universe.Systems;
            Notified = new BitArray(allSystems.Count);
            for (int i = 0; i < allSystems.Count; i++)
            {
                SolarSystem sys = allSystems[i];
                bool hostilesPresent = !GlobalStats.NotifyEnemyInSystemAfterLoad && IsHostilesPresent(owner, sys);
                Notified.Set(i, hostilesPresent);
            }
        }

        var systems = owner.GetOwnedSystems();
        for (int i = 0; i < systems.Count; i++)
        {
            SolarSystem sys = systems[i];

            // if system is already notified, keep checking while it's under threat
            // once the threat disappears, the flag will be cleared and player will be notified again
            if (Notified.IsSet(i))
            {
                Notified.Set(i, sys.HostileForcesPresent(owner));
                continue;
            }

            for (int j = 0; j < sys.ShipList.Count; j++)
            {
                Ship ship = sys.ShipList[j];
                Empire loyalty = ship.Loyalty;
                if (owner.GetRelations(loyalty, out var r) && r.IsHostile && IsShipAThreat(ship))
                {
                    owner.Universe.Notifications?.AddBeingInvadedNotification(sys, loyalty, StrRatio(sys, owner, loyalty));
                    Notified.Set(i);
                    break;
                }
            }
        }
    }

    static bool IsShipAThreat(Ship s)
    {
        return s.InPlayerSensorRange && !s.IsInWarp && s.BaseStrength > 0 && !s.IsFreighter && !s.IsResearchStation;
    }

    bool IsHostilesPresent(Empire owner, SolarSystem s)
    {
        if (s.HasPlanetsOwnedBy(owner.Universe.Player))
        {
            for (int j = 0; j < s.ShipList.Count; j++)
            {
                Ship ship = s.ShipList[j];
                if (owner.GetRelations(ship.Loyalty).IsHostile && IsShipAThreat(ship))
                    return true;
            }
        }

        return false;
    }

    float StrRatio(SolarSystem s, Empire owner, Empire hostile)
    {
        float hostileOffense = s.ShipList.Sum(s => s.Loyalty == hostile ? s.BaseStrength : 0);
        float ourSpaceOffense = s.ShipList.Sum(s => s.Loyalty == owner ? s.BaseStrength : 0);
        float ourPlanetsOffense = s.PlanetList.Sum(p => p.Owner == owner ? p.BuildingGeodeticOffense : 0);

        float ourOffense = (ourSpaceOffense + ourPlanetsOffense).LowerBound(1);
        return hostileOffense / ourOffense;
    }
}
