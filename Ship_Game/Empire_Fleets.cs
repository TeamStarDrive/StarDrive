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
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game;

public sealed partial class Empire
{
    // The first player-addressable key, this is also the first fleet key for AI-s
    public const int FirstFleetKey = 1;

    // The last player-addressable key, but AI is not limited by this
    public const int LastFleetKey = 9;

    // Refactored how fleets are stored, now only valid fleets exist in this list
    // This list is no longer sorted by Fleet.Key
    [StarData] Array<Fleet> FleetsList = new();

    // TODO: remove once we no longer need backwards compatibility to testing saves
    // TODO: OR upgrade the serializer to auto-convert T[] <-> Array<T>
    [StarData] Fleet[] Fleet
    {
        set => FleetsList = new(value);
    }

    float FleetUpdateTimer = 5f;

    /// <summary>Returns only active fleets which have ships</summary>
    public IEnumerable<Fleet> ActiveFleets
    {
        get
        {
            for (int i = 0; i < FleetsList.Count; ++i)
            {
                Fleet f = FleetsList[i];
                // we need this check, because current code allows
                // clearing f.Ships without calling Owner.RemoveFleet()
                if (f.Ships.NotEmpty)
                    yield return f;
            }
        }
    }

    /// <summary>Check if any of the active fleets matches the predicate test</summary>
    public IEnumerable<Fleet> GetActiveFleetsTargetingEmpire(Empire empire)
    {
        foreach (Fleet f in ActiveFleets)
            if (f.FleetTask?.TargetPlanet?.Owner == empire)
                yield return f;
    }

    public bool AnyActiveFleetsTargetingSystem(SolarSystem system)
    {
        foreach (Fleet f in ActiveFleets)
            if (f.FleetTask?.TargetPlanet?.ParentSystem == system)
                return true;
        return false;
    }

    void ResetFleets(bool returnShipsToEmpireAI = true)
    {
        foreach (Fleet fleet in FleetsList)
            fleet.Reset(returnShipsToEmpireAI);
        FleetsList.Clear();
    }
    
    public void RemoveFleet(Fleet fleet)
    {
        FleetsList.RemoveRef(fleet);
    }

    public int CreateFleetKey()
    {
        // find any keys that could be reused
        for (int fleetId = FirstFleetKey; fleetId <= LastFleetKey; ++fleetId)
        {
            Fleet fleet = GetFleetOrNull(fleetId);
            if (fleet == null || fleet.Key == 0 || fleet.Ships.IsEmpty)
                return fleetId;
        }

        // we got more than LastFleetKey fleets?
        if (isPlayer) // for players, fall back to LastFleetKey
            return LastFleetKey;
        return FleetsList.Max(f => f.Key) + 1;
    }

    public Fleet GetFleetOrNull(int fleetKey)
    {
        for (int i = 0; i < FleetsList.Count; ++i)
        {
            Fleet fleet = FleetsList[i];
            if (fleet.Key == fleetKey)
                return fleet;
        }
        return null;
    }

    public Fleet GetFleet(int fleetId)
    {
        return GetFleetOrNull(fleetId) ?? throw new IndexOutOfRangeException($"No fleetId={fleetId}");
    }

    public bool GetFleet(int fleetId, out Fleet fleet)
    {
        return (fleet = GetFleetOrNull(fleetId)) != null;
    }

    // Adds a new fleet or Replaces an existing fleet at [fleetId]
    public void SetFleet(int fleetId, Fleet fleet)
    {
        if (fleet.Owner != this)
        {
            // this is a mandatory requirement, otherwise AI or Player could manipulate AI fleets
            throw new($"Empire.SetFleet({fleetId}) fleet.Owner:{fleet.Owner} != {this}");
        }

        fleet.Key = fleetId;

        // replace existing?
        int index = FleetsList.IndexOf(f => f.Key == fleetId);
        if (index != -1)
            FleetsList[index] = fleet;
        else
            FleetsList.Add(fleet);
    }

    void UpdateFleets(FixedSimTime timeStep)
    {
        FleetUpdateTimer -= timeStep.FixedTime;
        foreach (Fleet fleet in ActiveFleets)
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

        if (!ShipsWeCanBuild.Contains(ship.ShipData) || !fleet.FindShipNode(ship, out FleetDataNode node))
            return;

        var ships = EmpireShips.OwnedShips; // grab temp ref because OwnedShips can be reassigned
        for (int i = 0; i < ships.Length; i++)
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
        var ships = isPlayer ? new(EmpireShips.OwnedShips)
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
                foreach (Fleet f in rel.Them.ActiveFleets)
                {
                    if (IsHostileFleetKnown(f))
                        knownFleets.Add(f);
                }
            }
        }
        return knownFleets;
    }

    bool IsHostileFleetKnown(Fleet f)
    {
        foreach (Ship s in f.Ships)
            if (s != null && (s.IsInBordersOf(this) || s.KnownByEmpires.KnownBy(this)))
                return true;
        return false;
    }
}
