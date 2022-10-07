using System;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.ExpansionAI;
using Ship_Game.AI.Tasks;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Fleets;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game;

public sealed partial class Empire
{
    public const int FirstFleetId = 1;
    public const int LastFleetId = 9;

    // All 9 fleets (most are placeholders by default)
    [StarData] public Fleet[] Fleets { get; private set; }

    float FleetUpdateTimer = 5f;

    public Fleet FirstFleet
    {
        get => GetFleet(FirstFleetId);
        set
        {
            Fleet existing = GetFleet(FirstFleetId);
            if (existing != value)
            {
                existing.Reset();
                SetFleet(FirstFleetId, value);
            }
        }
    }

    void InitializeFleets()
    {
        if (Fleets == null) // first time init
        {
            Fleets = new Fleet[LastFleetId];
            for (int i = FirstFleetId; i <= LastFleetId; ++i)
            {
                var fleet = new Fleet(Universe.CreateId(), this);
                fleet.SetNameByFleetIndex(i);
                SetFleet(i, fleet);
            }
        }
    }

    void ResetFleets(bool returnShipsToEmpireAI = true)
    {
        foreach (Fleet fleet in Fleets)
            fleet.Reset(returnShipsToEmpireAI);
    }

    public int CreateFleetKey()
    {
        for (int fleetId = FirstFleetId; fleetId <= LastFleetId; ++fleetId)
        {
            Fleet fleet = GetFleet(fleetId);
            if (fleet.Key == 0 || fleet.Ships.IsEmpty)
                return fleetId;
        }
        throw new("No available fleet keys!");
    }

    public Fleet GetFleetOrNull(int fleetKey)
    {
        int fleetIdx = fleetKey - 1;
        return (uint)fleetIdx < Fleets.Length ? Fleets[fleetIdx] : null;
    }

    public Fleet GetFleet(int fleetId)
    {
        return GetFleetOrNull(fleetId) ?? throw new IndexOutOfRangeException($"No fleetId={fleetId}");
    }

    public bool GetFleet(int fleetId, out Fleet fleet)
    {
        return (fleet = GetFleetOrNull(fleetId)) != null;
    }

    public void SetFleet(int fleetId, Fleet fleet)
    {
        fleet.Key = fleetId;
        Fleets[fleetId - 1] = fleet;
    }

    public void RemoveFleet(Fleet fleet)
    {
        var newFleet = new Fleet(Universe.CreateId(), this);
        newFleet.SetNameByFleetIndex(fleet.Key);
        SetFleet(fleet.Key, newFleet);
    }

    void UpdateFleets(FixedSimTime timeStep)
    {
        FleetUpdateTimer -= timeStep.FixedTime;
        foreach (Fleet fleet in Fleets)
        {
            fleet.Update(timeStep);
            if (FleetUpdateTimer <= 0f && !AI.Disabled)
                fleet.UpdateAI(timeStep, fleet.Key);
        }
        if (FleetUpdateTimer < 0.0)
            FleetUpdateTimer = 5f;
    }

    public void TrySendInitialFleets(Planet p)
    {
        if (isPlayer)
            return;

        if (p.EventsOnTiles())
            AI.SendExplorationFleet(p);

        if (Universe.Difficulty <= GameDifficulty.Hard || p.ParentSystem.IsExclusivelyOwnedBy(this))
            return;

        if (PlanetRanker.IsGoodValueForUs(p, this) && KnownEnemyStrengthIn(p.ParentSystem).AlmostZero())
        {
            var task = MilitaryTask.CreateGuardTask(this, p);
            AI.AddPendingTask(task);
        }
    }

    public void TryAutoRequisitionShip(Fleet fleet, Ship ship)
    {
        if (!isPlayer || fleet == null || !fleet.AutoRequisition)
            return;

        if (!ShipsWeCanBuild.Contains(ship.Name) || !fleet.FindShipNode(ship, out FleetDataNode node))
            return;
        var ships = OwnedShips;

        for (int i = 0; i < ships.Count; i++)
        {
            Ship s = ships[i];
            if (s.Fleet == null
                && s.Name == ship.Name
                && s.OnLowAlert
                && !s.IsHangarShip
                && !s.IsHomeDefense
                && s.AI.State != AIState.Refit
                && !s.AI.HasPriorityOrder
                && !s.AI.HasPriorityTarget)
            {
                s.AI.ClearOrders();
                fleet.AddExistingShip(s, node);
                return;
            }
        }

        var g = new FleetRequisition(ship.Name, this, fleet, false);
        node.Goal = g;
        AI.AddGoalAndEvaluate(g);
    }

    public Array<Ship> AllFleetReadyShips()
    {
        //Get all available ships from AO's
        var ships = isPlayer ? new(OwnedShips)
                             : AIManagedShips.GetShipsFromOffensePools();

        var readyShips = new Array<Ship>();
        for (int i = 0; i < ships.Count; i++)
        {
            Ship ship = ships[i];
            if (ship == null || ship.Fleet != null)
                continue;

            if (ship.AI.State == AIState.Resupply
                || ship.AI.State == AIState.ResupplyEscort
                || ship.AI.State == AIState.Refit
                || ship.AI.State == AIState.Scrap
                || ship.AI.State == AIState.Scuttle
                || ship.IsPlatformOrStation)
            {
                continue;
            }

            readyShips.Add(ship);
        }

        return readyShips;
    }

    public Array<Fleet> GetKnownHostileFleets()
    {
        var knownFleets = new Array<Fleet>();
        foreach (Relationship rel in AllRelations)
        {
            if (IsAtWarWith(rel.Them) || (rel.Them.isPlayer && !IsNAPactWith(rel.Them)))
            {
                // using fleet id-s here to avoid collection modification issues
                for (int fleetId = FirstFleetId; fleetId <= LastFleetId; ++fleetId)
                {
                    var f = rel.Them.GetFleet(fleetId);
                    if (f.Ships.NotEmpty)
                    {
                        if (IsHostileFleetKnown(f))
                            knownFleets.Add(f);
                    }
                }
            }
        }
        return knownFleets;
    }

    bool IsHostileFleetKnown(Fleet f)
    {
        foreach (Ship s in f.Ships)
            if (s?.IsInBordersOf(this) == true || s?.KnownByEmpires.KnownBy(this) == true)
                return true;
        return false;
    }
}
