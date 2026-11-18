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
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game;

public sealed partial class Empire
{
    // The first player-addressable key, this is also the first fleet key for AI-s
    public const int FirstFleetKey = 1;

    // The last player-addressable key, but AI is not limited by this
    public const int LastFleetKey = 20;

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
                // a fleet stays in the list after its ships are cleared
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

    public FleetShips ShipsReadyForFleet;

    /// <summary>Returns only active fleets which have ships</summary>
    public FleetEnumerator ActiveFleets => new FleetEnumerator(Fleets.GetInternalArrayItems(), Fleets.Count);

    /// <summary> Gets All Fleets for data nodes hnadling (refit, for isntance)</summary>
    public IReadOnlyList<Fleet> AllFleets => Fleets;

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
            if (f.FleetTask?.TargetPlanet?.System == system)
                return true;
        return false;
    }

    void ResetFleets(bool returnShipsToEmpireAI = true)
    {
        foreach (Fleet fleet in Fleets)
            fleet.Reset(fleeIfOutmatched: false, returnShipsToEmpireAI);
        Fleets.Clear();
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
        {
            Fleet replaced = Fleets[index];
            if (replaced != fleet && replaced.IsIdle) // a kept fleet with every ship away refitting: the refits go to the pool
                AI?.RemoveFleetFromGoals(replaced);
            Fleets[index] = fleet;
        }
        else
        {
            Fleets.Add(fleet);
        }
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

    internal void UpdateFleets(FixedSimTime timeStep)
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

        // the UI thread also writes the player's fleets, so only AI fleets are pruned on the sim thread
        if (!isPlayer)
            RemoveResetFleets();
    }

    // a reset fleet has key 0 and no ships; a kept fleet with no task goes once it has lost its ships, and its ships away refitting go to the pool
    void RemoveResetFleets()
    {
        for (int i = Fleets.Count - 1; i >= 0; --i)
        {
            Fleet fleet = Fleets[i];
            if (fleet.Ships.IsEmpty && (fleet.Key == 0 || fleet.IsIdle))
            {
                if (fleet.Key != 0)
                    AI?.RemoveFleetFromGoals(fleet);
                Fleets.RemoveAt(i);
            }
        }
    }

    // the AI's strength free for a new task: its warships in no fleet and its idle kept fleets
    public float StrengthReadyForTasks()
    {
        float strength = ShipsReadyForFleet?.AccumulatedStrength ?? 0f;
        foreach (Fleet fleet in ActiveFleets)
        {
            if (fleet.IsIdle)
                strength += fleet.ReadyStrength;
        }
        return strength;
    }

    // a major AI empire keeps its war, defense and claim fleets after their tasks
    public bool KeepsFleets => !isPlayer && !IsFaction;

    [StarData] int RatioSeed;

    // every random fleet ratio of the empire rolls from this seed, so the ratio holds from turn to turn
    public int FleetRatioSeed
    {
        get
        {
            if (RatioSeed == 0)
                RatioSeed = Random.Int(1, int.MaxValue - 1);
            return RatioSeed;
        }
    }

    [StarData] int PeakFleetCap;

    // the highest cap the empire has had
    public int PeakKeptFleetCap => PeakFleetCap;

    // the most fleets the empire keeps: one per ten planet levels, one more at Normal and one more for each difficulty step above it, never below half the most it has had
    public int KeptFleetCap() => Math.Max(FleetCapByLevels(levelMargin: 0), PeakFleetCap / 2);

    // once per AI turn, so the floor of the cap follows the most it has been
    public void UpdatePeakFleetCap() => PeakFleetCap = Math.Max(PeakFleetCap, FleetCapByLevels(levelMargin: 0));

    // idle fleets merge only over this: the levels count half a step more, so a planet changing level at the line merges nothing
    public int KeptFleetCapForJoins() => Math.Max(KeptFleetCap(), FleetCapByLevels(levelMargin: 5));

    int FleetCapByLevels(int levelMargin)
    {
        int levels = 0;
        for (int i = 0; i < OwnedPlanets.Count; ++i)
            levels += OwnedPlanets[i].Level;
        return (levels + levelMargin) / 10 + (int)Universe.P.Difficulty + 1;
    }

    public int KeptFleetCount()
    {
        int count = 0;
        foreach (Fleet fleet in ActiveFleets)
        {
            if (fleet.CountsTowardFleetCap)
                ++count;
        }
        return count;
    }

    // the empire's kept fleets with no task, nearest to the position first
    public Fleet[] IdleFleetsNearest(Vector2 position)
    {
        var idle = new Array<Fleet>();
        foreach (Fleet fleet in ActiveFleets)
        {
            if (fleet.IsIdle)
                idle.Add(fleet);
        }
        return idle.Sorted(f => f.AveragePosition(force: true).SqDist(position));
    }

    public void TrySendInitialFleets(Planet p)
    {
        if (isPlayer)
            return;

        if (p.EventsOnTiles())
            AI.SendExplorationFleet(p);

        if (Universe.P.Difficulty <= GameDifficulty.Hard || p.System.IsExclusivelyOwnedBy(this))
            return;

        if (PlanetRanker.IsGoodValueForUs(p, this) && KnownEnemyStrengthIn(p.System).AlmostZero())
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
                && !s.AI.HasPriorityTarget
                && s.CanBeAddedToFleets())
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
        var ships = EmpireShips.OwnedShips;
        var readyShips = new Array<Ship>();
        for (int i = 0; i < ships.Length; i++)
        {
            Ship ship = ships[i];
            if (ship == null
                || ship.Fleet != null
                || ship.IsFreighter
                || ship.IsPlatformOrStation
                || ship.IsHomeDefense
                || ship.IsConstructor
                || ship.IsSalvageShip
                || ship.IsHangarShip
                || ship.InCombat) // a ship in a fight stays in it, and is ready once the fight is over
            {
                continue;
            }

            switch (ship.AI.State)
            {
                case AIState.Resupply:
                case AIState.ResupplyEscort:
                case AIState.Refit:
                case AIState.Scrap:
                case AIState.Scuttle: continue;
            }

            readyShips.Add(ship);
        }

        return readyShips;
    }

    public Array<Fleet> GetKnownHostileFleets()
    {
        var knownFleets = new Array<Fleet>();
        bool legacyEspionage = LegacyEspionageEnabled;
        foreach (Relationship rel in AllRelations)
        {
            if (IsAtWarWith(rel.Them) || (rel.Them.isPlayer && !IsNAPactWith(rel.Them))
                && (legacyEspionage || rel.Espionage.ProjectorsCanAlert))
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
