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
using System.Collections;
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
    [StarData] readonly Array<Fleet> Fleets = new();

    float FleetUpdateTimer = 5f;

    // Using an Enumerator to increase performance. IEnumerable didn't cut it.
    public struct FleetEnumerator : IEnumerator<Fleet>
    {
        int Index;
        readonly int Count;
        readonly Fleet[] Items;
        public Fleet Current { get; private set; }
        object IEnumerator.Current => Current;
        public FleetEnumerator(Fleet[] items, int count)
        {
            Index = 0;
            Count = count;
            Items = items;
            Current = null;
        }
        public void Dispose()
        {
        }
        public bool MoveNext()
        {
            while (Index < Count)
            {
                Fleet f = Items[Index++];
                // we need Ships.NotEmpty check, because current code allows
                // clearing f.Ships without calling Owner.RemoveFleet()
                if (f != null && f.Ships.NotEmpty)
                {
                    Current = f;
                    return true;
                }
            }
            return false;
        }
        public void Reset()
        {
            Index = 0;
        }
        public FleetEnumerator GetEnumerator() => this;
        public Array<Fleet> ToArrayList()
        {
            Array<Fleet> items = new();
            foreach (Fleet f in this) items.Add(f);
            return items;
        }
    }

    /// <summary>Returns only active fleets which have ships</summary>
    public FleetEnumerator ActiveFleets => new FleetEnumerator(Fleets.GetInternalArrayItems(), Fleets.Count);

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
        foreach (Fleet fleet in Fleets)
            fleet.Reset(returnShipsToEmpireAI);
        Fleets.Clear();
    }
    
    public void RemoveFleet(Fleet fleet)
    {
        Fleets.RemoveRef(fleet);
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
        return Fleets.Max(f => f.Key) + 1;
    }

    public Fleet GetFleetOrNull(int fleetKey)
    {
        for (int i = 0; i < Fleets.Count; ++i)
        {
            Fleet fleet = Fleets[i];
            if (fleet.Key == fleetKey)
                return fleet;
        }
        return null;
    }

    public Fleet GetFleet(int fleetId)
    {
        return GetFleetOrNull(fleetId) ?? throw new IndexOutOfRangeException($"No fleetId={fleetId}");
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
        int index = Fleets.IndexOf(f => f.Key == fleetId);
        if (index != -1)
            Fleets[index] = fleet;
        else
            Fleets.Add(fleet);
    }

    // Creates a fleet with specified ID and overwrites that fleet slot
    // Any existing fleets will be lost
    public Fleet CreateFleet(int fleetId, string name)
    {
        Fleet fleet = new(Universe, this)
        {
            Name = name ?? Fleet.GetDefaultFleetName(fleetId)
        };
        SetFleet(fleetId, fleet);
        return fleet;
    }

    void UpdateFleets(FixedSimTime timeStep)
    {
        FleetUpdateTimer -= timeStep.FixedTime;
        foreach (Fleet fleet in ActiveFleets)
        {
            fleet.Update(timeStep);
            if (FleetUpdateTimer <= 0f && !AI.Disabled)
                fleet.UpdateAI(timeStep);
        }
        if (FleetUpdateTimer < 0f)
            FleetUpdateTimer = 5f;
    }

    public void TrySendInitialFleets(Planet p)
    {
        if (isPlayer)
            return;

        if (p.EventsOnTiles())
            AI.SendExplorationFleet(p);

        if (Universe.P.Difficulty <= GameDifficulty.Hard || p.ParentSystem.IsExclusivelyOwnedBy(this))
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

        if (!CanBuildShip(ship.ShipData) || !fleet.FindShipNode(ship, out FleetDataNode node))
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
