using System;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Fleets
{
    public sealed partial class Fleet
    {
        // an AI fleet that stays together after its task ends, to take the next one
        [StarData] public bool Kept { get; private set; }

        // the planet a kept fleet with no task waits at
        [StarData] public Planet IdleRally { get; private set; }

        public bool IsIdle => Kept && FleetTask == null;

        // the strength a split leaves the fleet: the most its tasks needed, fading while it is idle
        [StarData] public float StrengthToKeep { get; private set; }

        const float StrengthToKeepFadePerTurn = 0.995f;

        internal void FadeStrengthToKeep() => StrengthToKeep *= StrengthToKeepFadePerTurn;

        // the strength of the ships ready for a task, without those away resupplying
        public float ReadyStrength => Ships.Sum(s => s is { Active: true, Resupplying: false } ? s.GetStrength() : 0f);

        // the bombing the bombers ready for a task carry
        public int ReadyBombSecs => (int)Ships.Sum(s => s is { Active: true, Resupplying: false, DesignRole: RoleName.bomber } ? s.BombsGoodFor60Secs : 0f);

        // the troops the troop ships ready for a task carry
        public float ReadyTroopStrength => Ships.Sum(s => s is { Active: true, Resupplying: false, DesignRoleType: RoleType.Troop } ? s.Carrier.PlanetAssaultStrength : 0f);

        // a kept fleet, or one on a task that keeps its fleet; a temporary force does not count
        public bool CountsTowardFleetCap => Kept || FleetTask != null && MilitaryTask.UsesKeptFleets(FleetTask.Type);

        // pool ships join, the places held for ships away are dropped so those ships go to the pool when done, and the task starts from its first step
        internal void TakeTask(MilitaryTask task, string name, Array<Ship> joining)
        {
            DropEmptyNodes();
            for (int i = 0; i < joining.Count; ++i)
            {
                Ship ship = joining[i];
                ship.RemoveFromPoolAndFleet(clearOrders: true);
                AddShip(ship);
            }

            IdleRally = null;
            Name = name;
            FleetTask = task;
            AutoArrange();
        }

        // the ships of this idle fleet join another fleet, and this one is gone
        internal void JoinInto(Fleet other)
        {
            DropEmptyNodes();
            Ship[] ships = Ships.ToArray();
            for (int i = 0; i < ships.Length; ++i)
            {
                Ship ship = ships[i];
                if (RemoveShip(ship, clearOrders: true) && ship.Active)
                    other.AddShip(ship);
            }
            Reset(fleeIfOutmatched: false);
        }

        // over the fleet cap, another idle fleet joins this one, which gathers at its rally planet
        internal void TakeInIdleFleet(Fleet other)
        {
            var both = new Array<Ship>(Ships.ToArray());
            both.AddRange(other.Ships.ToArray());
            Ship[] fleeing = ShipsFightingWhereOutmatched(both); // both fleets weigh their fights together, before the join ends them
            Ship[] fighting = both.Filter(s => s.InCombat);

            StrengthToKeep = Math.Max(StrengthToKeep, other.StrengthToKeep);
            other.JoinInto(this);
            AutoArrange();
            if (IdleRally != null)
                GoToIdleRally(IdleRally);
            LeaveFightsToTheShips(fighting, fleeing);
        }

        // after the fleet's move, the ships that were fighting where outmatched flee and the rest are left to fight
        void LeaveFightsToTheShips(Ship[] fighting, Ship[] fleeing)
        {
            foreach (Ship ship in fighting)
            {
                if (ship.Fleet != this || !ship.Active)
                    continue;

                if (Array.IndexOf(fleeing, ship) >= 0)
                    ship.AI.OrderFlee();
                else
                    ship.AI.ClearOrders(); // a fight it can take goes on, and the ship rejoins at the rally planet after
            }
        }

        // a place with no ship goes, with any refit goal holding it, so the refitted ship does not come back
        void DropEmptyNodes()
        {
            for (int i = DataNodes.Count - 1; i >= 0; --i)
            {
                if (DataNodes[i].Ship == null)
                    DataNodes.RemoveAt(i);
            }
        }

        // the task is over but the fleet stays: single troop ships go home, outmatched fighters flee, the rest wait at the planet its claim won, else the nearest rally planet in a system the fleet can hold, else the nearest safe one
        internal void KeepAfterTask()
        {
            Ship[] fleeing = ShipsFightingWhereOutmatched(); // before the task's orders are dropped
            Ship[] fighting = Ships.Filter(s => s.InCombat); // before the fleet's move ends their combat
            SendSingleTroopShipsHome();
            IdleRally = null; // the rally planet from before the task is no fallback where the task ended

            Planet rally = Ships.IsEmpty || TooSmallToKeep() ? null : ClaimedPlanetToHold() ?? FindIdleRally();
            if (rally == null) // nothing worth keeping, or no planet of the empire to wait at
            {
                Reset(fleeIfOutmatched: true);
                return;
            }

            StrengthToKeep = Math.Max(StrengthToKeep, FleetTask?.MinimumTaskForceStrength ?? 0f);
            Owner.AI?.RemoveFleetFromGoals(this);
            TaskStep = 0;
            FleetTask = null;
            DropFlankPlans();
            FlankPlansTask = null;
            Kept = true;

            GoToIdleRally(rally); // the move or orbit replaces the ships' task orders, but a ship already orbiting the rally planet keeps its orbit
            LeaveFightsToTheShips(fighting, fleeing);
        }

        // fewer ships than half a fleet ratio, ships away refitting counted
        internal bool TooSmallToKeep()
            => Owner.ShipsReadyForFleet is { } pool && Ships.Count + ShipsAwayRefitting() < pool.ShipsPerFullFleet / 2f;

        // the planet a claim won, while the fleet can hold its system
        Planet ClaimedPlanetToHold()
            => FleetTask is { Type: MilitaryTask.TaskType.DefendClaim, TargetPlanet: { } claimed } && claimed.Owner == Owner && CanHold(claimed.System)
                ? claimed : null;

        void SendSingleTroopShipsHome()
        {
            for (int i = Ships.Count - 1; i >= 0; --i)
            {
                Ship ship = Ships[i];
                if (ship.DesignRole == RoleName.troop && RemoveShip(ship, clearOrders: true) && ship.Active)
                    ship.AI.OrderRebaseToNearest();
            }
        }

        // a kept fleet with no task waits in orbit of a rally planet of its empire, and breaks up when the empire has none
        void UpdateIdle()
        {
            if (!IdleRallyHolds())
            {
                Planet rally = FindIdleRally();
                if (rally == null)
                {
                    Reset(fleeIfOutmatched: true);
                    return;
                }

                if (rally != IdleRally)
                {
                    GoToIdleRally(rally);
                    return;
                }
            }

            if (ArrivedAtIdleRally())
                OrbitIdleRally();
        }

        // the fleet stays at its rally planet while the empire holds it and the fleet can beat the hostiles in its system
        bool IdleRallyHolds() => IdleRally != null && IdleRally.Owner == Owner && CanHold(IdleRally.System);

        // no hostiles are known in the system, or the whole fleet, ships away resupplying included, can beat them
        bool CanHold(SolarSystem system)
        {
            float hostiles = Owner.Threats.GetWeightedHostileStrengthAt(system.Position, system.Radius);
            return hostiles <= 0f || CanTakeWeightedFight(Owner, GetStrength(), hostiles);
        }

        // the nearest planet of the empire in a system the fleet can hold, safe planets first; with none, the rally planet it has while the empire holds it, else the nearest safe one, else the nearest
        Planet FindIdleRally()
        {
            Vector2 center = AveragePosition(force: true);
            var holdable = new Map<SolarSystem, bool>(); // worked out once per system
            return NearestPlanetToHold(Owner.SafePlanets, center, holdable)
                ?? NearestPlanetToHold(Owner.UnsafePlanets, center, holdable)
                ?? (IdleRally?.Owner == Owner ? IdleRally : Owner.FindNearestSafeRallyPoint(center));
        }

        Planet NearestPlanetToHold(Planet[] planets, Vector2 center, Map<SolarSystem, bool> holdable)
        {
            Planet nearest = null;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < planets.Length; ++i)
            {
                Planet planet = planets[i];
                float distance = planet.Position.SqDist(center);
                if (distance >= nearestDistance)
                    continue;

                if (!holdable.TryGetValue(planet.System, out bool canHold))
                {
                    canHold = CanHold(planet.System);
                    holdable[planet.System] = canHold;
                }

                if (canHold)
                {
                    nearest = planet;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }

        // the fleet heads for its rally planet, or orbits it at once when most of it is already there
        void GoToIdleRally(Planet rally)
        {
            IdleRally = rally;
            RefitCheckDue = true;
            if (InOrbitOfIdleRally() || ArrivedAtIdleRally())
            {
                FinalPosition = rally.Position; // where the fleet is, for those that read where it is headed
                OrbitIdleRally();
                return;
            }

            Vector2 center = AveragePosition(force: true);
            MoveTo(rally.Position, center.DirectionToTarget(rally.Position));
        }

        // the refit check is due once the fleet is in orbit of its rally planet with no enemies near it, after it heads there and when new tech comes in
        [StarData(DefaultValue = true)] bool RefitCheckDue = true;

        internal void RefitCheckOnNewTech() => RefitCheckDue = true;

        internal bool TakeRefitCheckAtRally()
        {
            if (!RefitCheckDue || IdleRally == null || !InOrbitOfIdleRally() || Ships.Any(s => s.AI.BadGuysNear))
                return false;

            RefitCheckDue = false;
            return true;
        }

        bool InOrbitOfIdleRally()
        {
            int orbiting = 0;
            for (int i = 0; i < Ships.Count; ++i)
            {
                if (Ships[i].AI.IsOrbiting(IdleRally))
                    ++orbiting;
            }
            return orbiting * 2 > Ships.Count;
        }

        // the ships away refitting whose places the fleet holds
        internal int ShipsAwayRefitting()
        {
            int away = 0;
            for (int i = 0; i < DataNodes.Count; ++i)
            {
                if (DataNodes[i].Ship == null && DataNodes[i].Goal is RefitShip)
                    ++away;
            }
            return away;
        }

        // a third of the fleet, rounded up and ships away refitting counted, may be away refitting at a time, and one ship always stays
        internal int RoomToRefit()
        {
            int away = ShipsAwayRefitting();
            return Math.Min((Ships.Count + away + 2) / 3 - away, Ships.Count - 1);
        }

        // in orbit of its rally planet with no ship away refitting, the fleet splits: the new fleet takes half of each role from its repaired ships, in turns across the roles, while this one keeps its strength to keep, and each keeps a full fleet ratio's number of ships
        internal void SplitOff(float shipsPerFullFleet)
        {
            if (IdleRally == null || ShipsAwayRefitting() > 0 || !InOrbitOfIdleRally())
                return;

            var shares = new Map<RoleName, int>();
            var healthy = new Map<RoleName, Array<Ship>>(); // damaged, resupplying and fighting ships stay
            for (int i = 0; i < Ships.Count; ++i)
            {
                Ship ship = Ships[i];
                shares.TryGetValue(ship.DesignRole, out int count);
                shares[ship.DesignRole] = count + 1;
                if (ship.Active && !ship.Resupplying && !ship.InCombat && ship.HealthPercent >= ShipResupply.RepairDoneThreshold)
                {
                    if (!healthy.TryGetValue(ship.DesignRole, out Array<Ship> ships))
                        healthy[ship.DesignRole] = ships = new();
                    ships.Add(ship);
                }
            }

            var candidates = new Array<(int Turn, RoleName Role, Ship Ship)>();
            foreach (var role in healthy)
            {
                for (int turn = 0; turn < shares[role.Key] / 2 && turn < role.Value.Count; ++turn)
                    candidates.Add((turn, role.Key, role.Value[turn]));
            }

            var leaving = new Array<Ship>();
            float staying = ReadyStrength;
            foreach (var (_, _, ship) in candidates.Sorted(c => (c.Turn, c.Role)))
            {
                float strength = ship.GetStrength();
                if (staying - strength < StrengthToKeep)
                    break;

                staying -= strength;
                leaving.Add(ship);
            }

            if (leaving.Count < shipsPerFullFleet)
                return;

            Fleet split = Owner.CreateFleet(Owner.CreateFleetKey(), null);
            for (int i = 0; i < leaving.Count; ++i)
            {
                Ship ship = leaving[i];
                if (RemoveShip(ship, clearOrders: true))
                    split.AddShip(ship);
            }

            DropEmptyNodes(); // not rearranged, so the staying ships keep their orbit
            split.Kept = true;
            split.AutoArrange();
            split.GoToIdleRally(IdleRally);
        }

        bool ArrivedAtIdleRally()
        {
            float radius = Math.Max(1000f, GetRelativeSize().Length() * 2f);
            return FleetMoveStatus(radius, IdleRally.Position).IsSet(MoveStatus.MajorityAssembled);
        }

        // ships away resupplying, refitting or fighting are left to it
        void OrbitIdleRally()
        {
            for (int i = 0; i < Ships.Count; ++i)
            {
                Ship ship = Ships[i];
                if (ship.Active && !ship.InCombat && ship.CanTakeFleetOrders
                    && !(ship.AI.FindGoal(ShipAI.Plan.Orbit, out ShipAI.ShipGoal orbit) && orbit.TargetPlanet == IdleRally))
                {
                    ship.OrderToOrbit(IdleRally, clearOrders: true);
                }
            }
        }
    }
}
