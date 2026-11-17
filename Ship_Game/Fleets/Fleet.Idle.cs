using System;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
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

        // the task is over but the fleet stays: single troop ships go home, outmatched fighters flee, the rest head for the nearest rally planet in a system the fleet can hold, else the nearest safe one
        internal void KeepAfterTask()
        {
            Ship[] fleeing = ShipsFightingWhereOutmatched(); // before the task's orders are dropped
            Ship[] fighting = Ships.Filter(s => s.InCombat); // before the fleet's move ends their combat
            SendSingleTroopShipsHome();
            IdleRally = null; // the rally planet from before the task is no fallback where the task ended

            Planet rally = Ships.IsEmpty ? null : FindIdleRally();
            if (rally == null) // nothing left to keep, or no planet of the empire to wait at
            {
                Reset(fleeIfOutmatched: true);
                return;
            }

            Owner.AI?.RemoveFleetFromGoals(this);
            TaskStep = 0;
            FleetTask = null;
            DropFlankPlans();
            FlankPlansTask = null;
            Kept = true;

            GoToIdleRally(rally); // the move replaces every ship's task orders
            LeaveFightsToTheShips(fighting, fleeing);
        }

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

        void GoToIdleRally(Planet rally)
        {
            IdleRally = rally;
            Vector2 center = AveragePosition(force: true);
            MoveTo(rally.Position, center.DirectionToTarget(rally.Position));
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
