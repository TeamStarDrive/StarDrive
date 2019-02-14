using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Ship_Game
{
    public class ShipGroup : IDisposable
    {
        public BatchRemovalCollection<Ship> Ships = new BatchRemovalCollection<Ship>();
        public Vector2 ProjectedDirection;
        public float Speed;
        public Empire Owner;
        public Vector2 Position;  // center of the ship group
        public Vector2 Direction; // direction facing of this ship group
        protected Stack<Fleet.FleetGoal> GoalStack = new Stack<Fleet.FleetGoal>();
        public Vector2 GoalMovePosition;
        public Array<Ship> FleetTargetList = new Array<Ship>();
        [XmlIgnore][JsonIgnore] public Vector2 StoredFleetPosition;
        [XmlIgnore][JsonIgnore] public float StoredFleetDistancetoMove;
        [XmlIgnore][JsonIgnore] public IReadOnlyList<Ship> GetShips => Ships;
        public override string ToString() => $"FleetGroup size={Ships.Count}";

        public Stack<Fleet.FleetGoal> GetStack() => GoalStack;

        public Fleet.FleetGoal PopGoalStack() => GoalStack.Pop();

        public ShipGroup()
        {
        }

        public ShipGroup(Array<Ship> shipList, Vector2 start, Vector2 end, Vector2 direction, Empire owner)
        {
            Owner = owner;
            AssembleDefaultGroup(shipList, start, end);
            ProjectPos((start+end)*0.5f, direction);
        }

        public void ProjectPos(Vector2 projectedPos, Vector2 direction)
        {
            ProjectedDirection = direction;
            float facing = direction.ToRadians();
            foreach (Ship ship in Ships)
            {
                float angle = ship.RelativeFleetOffset.ToRadians() + facing;
                float distance = ship.RelativeFleetOffset.Length();
                ship.projectedPosition = projectedPos + Vector2.Zero.PointFromRadians(angle, distance);
            }
        }

        public void ProjectPosNoOffset(Vector2 projectedPos, Vector2 direction)
        {
            ProjectedDirection = direction;
            foreach (Ship ship in Ships)
                ship.projectedPosition = projectedPos + direction;
        }

        public bool ContainsShip(Ship ship)
        {
            return Ships.Contains(ship);
        }

        public virtual void AddShip(Ship ship)
        {
            Ships.Add(ship);
        }

        void AddShips(IReadOnlyList<Ship> ships)
        {
            Ships.AddRange(ships);
        }

        public int CountShips => Ships.Count;

        public void AssignPositions(Vector2 newDirection)
        {
            if (!newDirection.IsUnitVector())
                Log.Error($"AssembleFleet newDirection {newDirection} must be a direction unit vector!");
            Direction = newDirection;
            float facing = newDirection.ToRadians();
            foreach (Ship ship in Ships) // rotate the existing fleet offsets
            {
                float angle = ship.RelativeFleetOffset.ToRadians() + facing;
                float distance = ship.RelativeFleetOffset.Length();
                ship.FleetOffset = Vector2.Zero.PointFromRadians(angle, distance);
            }
        }

        public void AssembleFleet(Vector2 newDirection, bool forceAssembly = false)
        {
            if (!newDirection.IsUnitVector())
                Log.Error($"AssembleFleet newDirection {newDirection} must be a direction unit vector!");
            Direction = newDirection;
            float facing = newDirection.ToRadians();
            foreach (Ship ship in Ships)
            {
                if (ship.AI.State != AIState.AwaitingOrders && !forceAssembly)
                    continue;
                float angle = ship.RelativeFleetOffset.ToRadians() + facing;
                float distance = ship.RelativeFleetOffset.Length();
                ship.FleetOffset = Vector2.Zero.PointFromRadians(angle, distance);
            }
        }

        static float GetMaxRadius(IReadOnlyList<Ship> shipList)
        {
            float maxRadius = 0.0f;
            for (int i = 0; i < shipList.Count; ++i)
                maxRadius = Math.Max(maxRadius, shipList[i].GetSO().WorldBoundingSphere.Radius);
            return maxRadius;
        }

        static int GetShipOrder(Ship ship)
        {
            switch (ship.DesignRole)
            {
                case ShipData.RoleName.fighter:   return 1;
                case ShipData.RoleName.gunboat:   return 1;
                case ShipData.RoleName.corvette:  return 1;
                case ShipData.RoleName.bomber:    return 2; // bombers behind fighters
                case ShipData.RoleName.frigate:   return 3;
                case ShipData.RoleName.destroyer: return 3;
                case ShipData.RoleName.cruiser:   return 3;
                case ShipData.RoleName.prototype: return 3;
                case ShipData.RoleName.carrier:   return 4; // carriers behind cruisers
                case ShipData.RoleName.capital:   return 4;
                default: return 5; // everything else to the back
            }
        }

        // this performs a consistent sort of input ships so that they are always ordered to same
        // fleet offsets even if ship groups are recreated
        static Ship[] ConsistentSort(Array<Ship> ships)
        {
            return ships.Sorted((a,b) =>
            {
                int order = GetShipOrder(a) - GetShipOrder(b);
                if (order != 0) return order;
                return a.guid.CompareTo(b.guid); // otherwise sort by ship GUID which never changes
            });
        }

        void AssembleDefaultGroup(Array<Ship> shipList, Vector2 leftCorner, Vector2 rightCorner)
        {
            if (shipList.IsEmpty)
                return;
            Ship[] ships = ConsistentSort(shipList);
            AddShips(ships);

            float shipSpacing = GetMaxRadius(ships) + 500f;
            float fleetWidth = rightCorner.Distance(leftCorner);

            int w = ships.Length, h = 1; // virtual layout grid

            if (fleetWidth.AlmostZero()) // no width provided, probably RIGHT CLICK
            {
                // SO, we perform automatic layout to rows and columns
                // until w/h ratio is <= 2 resulting in: 2x1 3x1 4x2 5x2 6x3 7x4 8x4...
                while (w / (float)h > 2.0f)
                {
                    w -= w / 2;
                    h = (int)Math.Ceiling(ships.Length / (double)w);
                }
            }
            else // automatically calculate layout depth based on provided fleetWidth
            {
                fleetWidth = Math.Max(1600f, fleetWidth); // fleets cannot be smaller than this
                w = Math.Min((int)(fleetWidth / shipSpacing), ships.Length);
                h = (int)Math.Ceiling(ships.Length / (double)w);
            }

            // center offset, this makes our Ad-Hoc group be centered
            // to mouse position
            float cx = w * 0.5f - 0.5f; 
            int i = 0;
            for (int y = 0; y < h; ++y)
            {
                bool lastLine = (y == h-1);
                if (!lastLine) // fill front lines:
                {
                    for (int x = 0; x < w; ++x)
                        Ships[i++].RelativeFleetOffset = new Vector2(x-cx, y) * shipSpacing;
                }
                else
                {
                    int remaining = ships.Length - i;
                    float cx2 = remaining*0.5f - 0.5f; // last line center offset by remaining ships
                    for (int x = 0; x < remaining; ++x)
                        Ships[i++].RelativeFleetOffset = new Vector2(x-cx2, y) * shipSpacing;
                }
            }
            Log.Assert(i == ships.Length, "Some ships were not assigned virtual fleet positions!");
        }

        public bool IsShipListEqual(Array<Ship> ships)
        {
            using (Ships.AcquireReadLock())
            {
                if (Ships.Count != ships.Count)
                    return false;
                foreach (Ship ship in Ships)
                    if (!ships.Contains(ship))
                        return false;
                return true;
            }
        }

        public static Vector2 AveragePosition(Array<Ship> ships)
        {
            if (ships.Count == 0)
                return Vector2.Zero;
            Vector2 pos = ships[0].Position;
            for (int i = 1; i < ships.Count; ++i)
                pos = (ships[i].Position + pos) * 0.5f;
            return pos;
        }

        public Vector2 FindAveragePosition()
        {
            if (StoredFleetPosition == Vector2.Zero)
                StoredFleetPosition = FindAveragePositionset();
            return StoredFleetPosition;
        }

        public Vector2 FindAveragePositionset()
        {
            using (Ships.AcquireReadLock())
                return AveragePosition(Ships);
        }

        public void Setavgtodestination()
        {
            var distances = new Array<float>();
            using (Ships.AcquireReadLock())
                foreach (Ship distance in Ships)
                {
                    if (distance.EnginesKnockedOut || !distance.Active || distance.InCombat)
                        continue;
                    distances.Add(distance.Center.Distance(Position + distance.FleetOffset) - 100);
                }

            if (distances.Count <= 2)
            {
                StoredFleetDistancetoMove = Vector2.Distance(StoredFleetPosition, Position);
                return;
            }
            float avgdistance = distances.Average();
            float sum = (float)distances.Sum(distance => Math.Pow(distance - avgdistance, 2));
            float stddev = (float)Math.Sqrt((sum) / (distances.Count - 1));
            StoredFleetDistancetoMove = distances.Where(distance => distance <= avgdistance + stddev).Average();
        }

        protected bool IsFleetSupplied(float wantedSupplyRatio =.1f)
        {
            float currentAmmo = 0.0f;
            float maxAmmo = 0.0f;
            float ammoDps = 0.0f;
            float energyDps = 0.0f;
            //TODO: make sure this is the best way. Likely these values can be done in ship update and totaled here rather than recalculated.
            for (int index = 0; index < Ships.Count; index++)
            {
                Ship ship = Ships[index];
                if (ship.AI.HasPriorityOrder) continue;
                currentAmmo += ship.Ordinance;
                maxAmmo += ship.OrdinanceMax;
                foreach (Weapon weapon in ship.Weapons)
                {
                    if (weapon.OrdinanceRequiredToFire > 0.0)
                        ammoDps = weapon.DamageAmount / weapon.fireDelay;
                    if (weapon.PowerRequiredToFire > 0.0)
                        energyDps = weapon.DamageAmount / weapon.fireDelay;
                }
            }
            return !(maxAmmo > 0) || !(ammoDps >= (ammoDps + energyDps) * 0.5f) || !(currentAmmo <= maxAmmo * wantedSupplyRatio);
        }

        public void MoveDirectlyNow(Vector2 movePosition, Vector2 direction)
        {
            if (!direction.IsUnitVector())
                Log.Error($"MoveDirectlyNow direction {direction} must be a direction unit vector!");
            Position = movePosition;
            Direction = direction;
            AssembleFleet(direction);
            foreach (Ship ship in Ships)
            {
                //Prevent fleets with no tasks from and are near their distination from being dumb.
                if (Owner.isPlayer || ship.AI.State == AIState.AwaitingOrders || ship.AI.State == AIState.AwaitingOffenseOrders)
                {
                    ship.AI.SetPriorityOrder(true);
                    ship.AI.OrderMoveDirectlyTowardsPosition(Position + ship.FleetOffset, direction, true);
                }
            }
        }

        public virtual void FormationWarpTo(Vector2 movePosition, Vector2 facingDir, bool queueOrder = false)
        {
            GoalStack?.Clear();
            Position = movePosition;
            AssembleFleet(facingDir, !queueOrder);
            using(Ships.AcquireReadLock())
            foreach (Ship ship in Ships)
            {
                ship.AI.SetPriorityOrder(!queueOrder);
                if (queueOrder)
                    ship.AI.OrderFormationWarpQ(Position + ship.FleetOffset, facingDir);
                else
                    ship.AI.OrderFormationWarp(Position + ship.FleetOffset, facingDir);
            }
        }

        public void MoveToDirectly(Vector2 movePosition, Vector2 facingDir)
        {
            GoalStack?.Clear();
            MoveDirectlyNow(movePosition, facingDir);
        }

        public void MoveToNow(Vector2 movePosition, Vector2 facingDir)
        {
            Position = movePosition;
            AssembleFleet(facingDir, true);
            foreach (Ship ship in Ships)
            {
                ship.AI.SetPriorityOrder(false);
                ship.AI.OrderMoveTowardsPosition(Position + ship.FleetOffset, facingDir, true, null);
            }
        }

        public float GetStrength()
        {
            float totalStrength = 0.0f;
            for (int i = 0; i < Ships.Count; i++)
            {
                Ship ship = Ships[i];
                if (ship.Active) totalStrength += ship.GetStrength();
            }
            return totalStrength;
        }

        /// <summary>
        /// This will force all ships in fleet to orbit planet.
        /// There are no checks here for ships already in some action.
        /// this can cause a cancel current order and orbit loop.
        /// </summary>
        internal void DoOrbitAreaRestricted(Planet planet, Vector2 position, float radius)
        {
            foreach (var ship in Ships)
            {
                if (ship.AI.State != AIState.Orbit && ship.Center.InRadius(ship.Center, radius))
                    ship.DoOrbit(planet);
            }
        }

        public enum MoveStatus
        {
            InCombat = 0,
            Dispersed,
            Assembled
        }

        public MoveStatus IsFleetAssembled(float radius, Vector2 position = default)
        {
            if (position == default)
                position = Position;
            MoveStatus moveStatus = MoveStatus.Assembled;
            bool inCombat = false;
            for (int index = 0; index < Ships.Count; index++)
            {
                Ship ship = Ships[index];
                if (ship.EMPdisabled || !ship.hasCommand || !ship.Active)
                    continue;
                inCombat |= ship.InCombat;
                if (ship.Center.InRadius(position + ship.FleetOffset, radius)) continue;
                moveStatus = MoveStatus.Dispersed;
                if (inCombat)
                    break;
            }
            moveStatus = inCombat && moveStatus == MoveStatus.Dispersed ? MoveStatus.InCombat : moveStatus;

            return moveStatus;
        }

        public enum CombatStatus
        {
            InCombat = 0,
            EnemiesNear,
            ClearSpace
        }

        public CombatStatus FleetInAreaInCombat(Vector2 position, float radius)
        {
            for (int x = 0; x < Ships.Count; ++x)
            {
                Ship ship = Ships[x];
                CombatStatus status = SetCombatMoveAtPositon(ship, position, radius);
                if (status != CombatStatus.ClearSpace)
                    return status;
            }
            return CombatStatus.ClearSpace;
        }
        protected CombatStatus SetCombatMoveAtPositon(Ship ship, Vector2 position, float radius)
        {
            if (ship.Center.OutsideRadius(position, radius))
                return CombatStatus.ClearSpace;

            if (ship.engineState != Ship.MoveState.Warp && ship.AI.State == AIState.FormationWarp)
                ship.AI.HasPriorityOrder = false;

            if (ship.InCombat) return CombatStatus.InCombat;
            if (ship.AI.BadGuysNear) return CombatStatus.EnemiesNear;
            return CombatStatus.ClearSpace;
        }
        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }
        ~ShipGroup() { Destroy(); }

        protected virtual void Destroy()
        {
            Ships?.Dispose(ref Ships);
        }

        public void SetSpeed()
        {
            if (Ships.Count == 0)
                return;
            float slowestSpeed = Ships[0].Speed;
            for (int i = 0; i < Ships.Count; i++)
                // Modified this so speed of a fleet is only set in one place -Gretman
            {
                Ship ship = Ships[i];
                if (ship.Inhibited || ship.EnginesKnockedOut || !ship.Active || ship.AI.State != AIState.FormationWarp)
                    continue;
                if (ship.Speed < slowestSpeed)
                    slowestSpeed = ship.Speed;
            }
            if (slowestSpeed < 200)
                slowestSpeed = 200;
            Speed = slowestSpeed;
        }
    }
}