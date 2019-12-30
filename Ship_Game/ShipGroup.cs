using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
    public class ShipGroup
    {
        public readonly Array<Ship> Ships = new Array<Ship>();
        public Empire Owner;

        // Speed LIMIT of the entire ship group, so the ships can stay together
        public float SpeedLimit { get; private set; }

        // FINAL DESTINATION center position of the ship group
        // This can also be considered as the ASSEMBLY POSITION
        // If you set this to X location, ships will gather around it when idle
        public Vector2 FinalPosition;

        // FINAL direction facing of this ship group
        public Vector2 FinalDirection = Vectors.Up;

        // Holo-Projection of the ship group
        public Vector2 ProjectedPos;
        public Vector2 ProjectedDirection;

        // WORK IN PROGRESS
        protected readonly Stack<Fleet.FleetGoal> GoalStack = new Stack<Fleet.FleetGoal>();

        // cached average position of the fleet
        Vector2 AveragePos;

        // entire ship group average offset from [0,0]
        // this is relevant because ships are not perfectly aligned
        protected Vector2 AverageOffsetFromZero;
        int LastAveragePosUpdate = -1;

        public int CountShips => Ships.Count;
        public override string ToString() => $"FleetGroup ships={Ships.Count}";

        //// Fleet Goal Access | We don't want to expose the inner details ////
        public bool HasFleetGoal => GoalStack.Count > 0;
        public Vector2 NextGoalMovePosition => GoalStack.Peek().MovePosition;
        public Fleet.FleetGoal PopGoalStack() => GoalStack.Pop();
        public void ClearFleetGoals() => GoalStack.Clear();
        ///////////////////////////////////////////////////////////////////////

        public ShipGroup()
        {
        }

        public ShipGroup(Array<Ship> shipList, Vector2 start, Vector2 end, Vector2 direction, Empire owner)
        {
            Owner = owner;
            Vector2 fleetCenter = AssembleDefaultGroup(shipList, start, end);
            ProjectPos(fleetCenter, direction);
        }

        public void ProjectPos(Vector2 projectedPos, Vector2 direction)
        {
            ProjectedPos = projectedPos;
            ProjectedDirection = direction;
            float facing = direction.ToRadians();

            for (int i = 0; i < Ships.Count; ++i)
            {
                Ship ship = Ships[i];
                float angle = ship.RelativeFleetOffset.ToRadians() + facing;
                float distance = ship.RelativeFleetOffset.Length();
                ship.projectedPosition = projectedPos + angle.RadiansToDirection()*distance;
            }
        }

        // This is used for single-ship groups
        public void ProjectPosNoOffset(Vector2 projectedPos, Vector2 direction)
        {
            ProjectedPos = projectedPos;
            ProjectedDirection = direction;
            for (int i = 0; i < Ships.Count; ++i)
                Ships[i].projectedPosition = projectedPos + direction;
        }

        public bool ContainsShip(Ship ship)
        {
            return Ships.ContainsRef(ship);
        }

        public virtual void AddShip(Ship ship)
        {
            Ships.Add(ship);
            LastAveragePosUpdate = -1; // deferred position refresh
        }

        protected void AssignPositionTo(Ship ship)
        {
            float angle = ship.RelativeFleetOffset.ToRadians() + FinalDirection.ToRadians();
            float distance = ship.RelativeFleetOffset.Length();
            ship.FleetOffset = angle.RadiansToDirection()*distance;
        }

        public void AssignPositions(Vector2 newDirection)
        {
            if (!newDirection.IsUnitVector())
                Log.Error($"AssignPositions newDirection {newDirection} must be a direction unit vector!");

            FinalDirection = newDirection;
            float facing = newDirection.ToRadians();

            for (int i = 0; i < Ships.Count; ++i) // rotate the existing fleet offsets
            {
                Ship ship = Ships[i];
                float angle = ship.RelativeFleetOffset.ToRadians() + facing;
                float distance = ship.RelativeFleetOffset.Length();
                ship.FleetOffset = angle.RadiansToDirection()*distance;
            }
        }

        public void AssembleFleet(Vector2 finalPosition, Vector2 finalDirection, bool forceAssembly = false)
        {
            if (!finalDirection.IsUnitVector())
                Log.Error($"AssembleFleet newDirection {finalDirection} must be a direction unit vector!");
            
            FinalPosition  = finalPosition;
            FinalDirection = finalDirection;
            float facing = finalDirection.ToRadians();

            for (int i = 0; i < Ships.Count; ++i)
            {
                Ship ship = Ships[i];
                if (ship.AI.State == AIState.AwaitingOrders || forceAssembly)
                {
                    float angle = ship.RelativeFleetOffset.ToRadians() + facing;
                    float distance = ship.RelativeFleetOffset.Length();
                    ship.FleetOffset = angle.RadiansToDirection()*distance;
                }
            }
        }

        static float GetMaxRadius(Ship[] shipList)
        {
            float maxRadius = 0.0f;
            for (int i = 0; i < shipList.Length; ++i)
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

        Vector2 AssembleDefaultGroup(Array<Ship> shipList, Vector2 start, Vector2 end)
        {
            if (shipList.IsEmpty)
                return start;

            Ship[] ships = ConsistentSort(shipList);
            Ships.AddRange(ships);
            LastAveragePosUpdate = -1; // deferred position refresh

            float shipSpacing = GetMaxRadius(ships) + 500f;
            float fleetWidth = start.Distance(end);

            if (fleetWidth > shipSpacing * ships.Length)
                fleetWidth = shipSpacing * ships.Length;

            int w = ships.Length, h = 1; // virtual layout grid

            if (fleetWidth.AlmostZero()) // no width provided, probably RIGHT CLICK
            {
                // SO, we perform automatic layout to rows and columns
                // until w/h ratio is < 4 resulting in: 2x1 3x1 4x2 5x2 6x3 7x4 8x4...
                while (w / (float)h > 4f)
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
            return GetProjectedMidPoint(start, end, new Vector2(fleetWidth, 0));
        }

        public Vector2 GetProjectedMidPoint(Vector2 start, Vector2 end, Vector2 size)
        {
            Vector2 dir = start.DirectionToTarget(end);
            float width = size.X * 0.5f;
            Vector2 center = start + dir * width;

            float height = size.Y * 0.75f;
            return center + dir.RightVector() * height;
        }

        public Vector2 GetRelativeSize()
        {
            Vector2 min = default, max = default;
            foreach (Ship ship in Ships)
            {
                if (ship.FleetOffset.X < min.X) min.X = ship.FleetOffset.X;
                if (ship.FleetOffset.X > max.X) max.X = ship.FleetOffset.X;
                if (ship.FleetOffset.Y < min.Y) min.Y = ship.FleetOffset.Y;
                if (ship.FleetOffset.Y > max.Y) max.Y = ship.FleetOffset.Y;
            }
            return max - min;
        }

        public bool IsShipListEqual(Array<Ship> ships)
        {
            if (Ships.Count != ships.Count)
                return false;
            for (int i = 0; i < Ships.Count; ++i)
                if (!ships.ContainsRef(Ships[i]))
                    return false;
            return true;
        }

        public static Vector2 GetAveragePosition(Array<Ship> ships)
        {
            int count = ships.Count;
            if (count == 0)
                return Vector2.Zero;

            Ship[] items = ships.GetInternalArrayItems();
            Vector2 avg = items[0].Center;
            for (int i = 1; i < count; ++i)
            {
                Vector2 p = items[i].Center;
                avg.X += p.X;
                avg.Y += p.Y;
            }
            return avg / count;
        }

        static Vector2 GetAverageOffsetFromZero(Array<Ship> ships)
        {
            int count = ships.Count;
            if (count == 0)
                return Vector2.Zero;

            Ship[] items = ships.GetInternalArrayItems();
            Vector2 avg = items[0].FleetOffset;
            for (int i = 1; i < count; ++i)
            {
                Vector2 p = items[i].FleetOffset;
                avg.X += p.X;
                avg.Y += p.Y;
            }
            return avg / count;
        }

        public Vector2 AveragePosition()
        {
            // Update Pos once per frame, OR if LastAveragePosUpdate was invalidated
            if (LastAveragePosUpdate != StarDriveGame.Instance.FrameId)
            {
                LastAveragePosUpdate = StarDriveGame.Instance.FrameId;
                AveragePos = GetAveragePosition(Ships);
                AverageOffsetFromZero = GetAverageOffsetFromZero(Ships);
            }
            return AveragePos;
        }

        public Ship GetClosestShipTo(Vector2 worldPos)
        {
            return Ships.FindMin(ship => ship.Center.SqDist(worldPos));
        }

        protected bool IsFleetSupplied(float wantedSupplyRatio =.1f)
        {
            float currentAmmo = 0.0f;
            float maxAmmo = 0.0f;
            float ammoDps = 0.0f;
            float energyDps = 0.0f;

            //TODO: make sure this is the best way. Likely these values can be done in ship update and totaled here rather than recalculated.
            for (int i = 0; i < Ships.Count; ++i)
            {
                Ship ship = Ships[i];
                if (!ship.AI.HasPriorityOrder)
                {
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
            }
            return !(maxAmmo > 0)
                || !(ammoDps >= (ammoDps + energyDps) * 0.5f)
                || !(currentAmmo <= maxAmmo * wantedSupplyRatio);
        }

        public void FormationWarpTo(Vector2 finalPosition, Vector2 finalDirection, bool queueOrder = false)
        {
            GoalStack.Clear();
            AssembleFleet(finalPosition, finalDirection, forceAssembly:true);

            for (int i = 0; i < Ships.Count; ++i)
            {
                Ship ship = Ships[i];
                ship.AI.SetPriorityOrder(!queueOrder);
                if (queueOrder)
                    ship.AI.OrderFormationWarpQ(FinalPosition + ship.FleetOffset, finalDirection);
                else
                    ship.AI.OrderFormationWarp(FinalPosition + ship.FleetOffset, finalDirection);
            }
        }

        public void MoveToDirectly(Vector2 finalPosition, Vector2 finalDirection)
        {
            GoalStack.Clear();
            AssembleFleet(finalPosition, finalDirection);
            
            foreach (Ship ship in Ships)
            {
                //Prevent fleets with no tasks from and are near their distination from being dumb.
                if (Owner.isPlayer || ship.AI.State == AIState.AwaitingOrders || ship.AI.State == AIState.AwaitingOffenseOrders)
                {
                    ship.AI.SetPriorityOrder(true);
                    ship.AI.OrderMoveDirectlyTo(FinalPosition + ship.FleetOffset, finalDirection, true);
                }
            }
        }

        public void MoveToNow(Vector2 finalPosition, Vector2 finalDirection)
        {
            AssembleFleet(finalPosition, finalDirection, true);

            foreach (Ship ship in Ships)
            {
                ship.AI.SetPriorityOrder(false);
                ship.AI.OrderMoveTo(FinalPosition + ship.FleetOffset, finalDirection, true, null);
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
            foreach (Ship ship in Ships)
            {
                if (ship.AI.State != AIState.Orbit && ship.Center.InRadius(ship.Center, radius))
                    ship.OrderToOrbit(planet);
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
                position = FinalPosition;

            MoveStatus moveStatus = MoveStatus.Assembled;
            bool inCombat = false;
            for (int i = 0; i < Ships.Count; i++)
            {
                Ship ship = Ships[i];
                if (!ship.EMPdisabled && ship.hasCommand && ship.Active)
                {
                    inCombat |= ship.InCombat;
                    if (!ship.Center.InRadius(position + ship.FleetOffset, radius))
                    {
                        moveStatus = MoveStatus.Dispersed;
                        if (inCombat)
                            break;
                    }
                }
            }

            moveStatus = (inCombat && moveStatus == MoveStatus.Dispersed) ? MoveStatus.InCombat : moveStatus;
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
            for (int i = 0; i < Ships.Count; ++i)
            {
                Ship ship = Ships[i];
                CombatStatus status = SetCombatMoveAtPosition(ship, position, radius);
                if (status != CombatStatus.ClearSpace)
                    return status;
            }
            return CombatStatus.ClearSpace;
        }

        protected CombatStatus SetCombatMoveAtPosition(Ship ship, Vector2 position, float radius)
        {
            if (ship.Center.OutsideRadius(position, radius))
                return CombatStatus.ClearSpace;

            if (ship.engineState != Ship.MoveState.Warp && ship.AI.State == AIState.FormationWarp)
                ship.AI.ClearPriorityOrder();

            if (ship.InCombat) return CombatStatus.InCombat;
            if (ship.AI.BadGuysNear) return CombatStatus.EnemiesNear;
            return CombatStatus.ClearSpace;
        }

        public void SetSpeed()
        {
            if (Ships.Count == 0)
                return;

            float slowestSpeed = Ships[0].VelocityMaximum;
            for (int i = 1; i < Ships.Count; i++)
            {
                Ship ship = Ships[i];
                if (!ship.EnginesKnockedOut)
                    slowestSpeed = Math.Min(ship.VelocityMaximum, slowestSpeed);
            }
            SpeedLimit = Math.Max(200, (float)Math.Round(slowestSpeed));
        }
    }
}