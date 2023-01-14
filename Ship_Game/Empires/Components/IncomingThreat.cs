using SDUtils;
using Ship_Game.AI.Tasks;
using Ship_Game.Data.Serialization;
using Ship_Game.Fleets;

namespace Ship_Game.Empires.Components;

/// <summary>
/// A solar system which has threats incoming
/// </summary>
[StarDataType]
public class IncomingThreat
{
    [StarData] readonly Empire Owner;
    [StarData] public readonly SolarSystem TargetSystem;

    [StarData] public float ThreatTimer { get; private set; }

    [StarData] Fleet[] Fleets;
    [StarData] public Fleet NearestFleet { get; private set; }
    [StarData] public float ThreatDistance { get; private set; }
    [StarData] public float Strength { get; private set; }
    [StarData] public bool HighPriority { get; private set; }

    const float ThreatResetTime = 10;
    public bool ThreatTimedOut => ThreatTimer <= 0;
    public Empire[] Enemies => Fleets?.FilterSelect(f => f != null, f=>f.Owner);

    [StarDataConstructor] IncomingThreat() {}

    public IncomingThreat(Empire owner, SolarSystem system, Fleet[] fleets)
    {
        Owner = owner;
        TargetSystem = system;
        UpdateThreats(fleets);
        ProcessFleetThreat();
    }

    public bool Update(FixedSimTime simTime)
    {
        ThreatTimer -= simTime.FixedTime;
        if (ThreatTimedOut)
        {
            Fleets = Empty<Fleet>.Array; 
            return false;
        }

        NearestFleet = Fleets.FindMin(f => f.AveragePosition().SqDist(TargetSystem.Position));
        ThreatDistance = NearestFleet?.AveragePosition().Distance(TargetSystem.Position) ?? float.MaxValue;
        ProcessFleetThreat();
        return true;
    }
    
    // the update now happens in ~5sec intervals
    public void UpdateThreats(Fleet[] fleets)
    {
        ThreatTimer = ThreatResetTime;
        Fleets = fleets;
        Strength = fleets.Sum(f => f.GetStrength() * Owner.GetFleetStrEmpireMultiplier(f.Owner));
    }

    void ProcessFleetThreat()
    {
        HighPriority = false;
        
        MilitaryTask.TaskCategory cat = MilitaryTask.TaskCategory.FleetNeeded | MilitaryTask.TaskCategory.War;

        foreach (Fleet f in Fleets)
        {
            if (f.Owner.isPlayer && f.GetStrength() > 0 || f.FleetTask?.GetTaskCategory() == cat)
            {
                HighPriority = true;
                break;
            }
        }
    }
}