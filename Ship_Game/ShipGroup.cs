using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using Ship_Game.Fleets;
using Ship_Game.Fleets.FleetGoals;

namespace Ship_Game
{
    public class ShipGroup
    {
        public readonly Array<Ship> Ships = new Array<Ship>();
        public Empire Owner;
        protected bool IsAssembling = false;
        public Ship CommandShip
        {
            get         => LeadShip?.Leader;
            private set => LeadShip = new GroupLeader(value, value?.fleet);
        }

        GroupLeader LeadShip;

        public void SetCommandShip(Ship ship) => CommandShip = ship;

        // Speed LIMIT of the entire ship group, so the ships can stay together
        public float SpeedLimit { get; private set; }

        // FINAL DESTINATION center position of the ship group
        // This can also be considered as the ASSEMBLY POSITION
        // If you set this to X location, ships will gather around it when idle
        public Vector2 FinalPosition;
        public Vector2 BattlePosition;

        // FINAL direction facing of this ship group
        public Vector2 FinalDirection = Vectors.Up;

        // Holo-Projection of the ship group
        public Vector2 ProjectedPos;
        public Vector2 ProjectedDirection;

        // WORK IN PROGRESS
        protected readonly Stack<FleetGoal> GoalStack = new Stack<FleetGoal>();

        // cached average position of the fleet
        protected Vector2 AveragePos;

        // entire ship group average offset from [0,0]
        // this is relevant because ships are not perfectly aligned
        protected Vector2 AverageOffsetFromZero;
        int LastAveragePosUpdate = -1;

        protected float Strength;
        int LastStrengthUpdate = -1;

        public int CountShips => Ships.Count;
        public override string ToString() => $"FleetGroup ships={Ships.Count}";

        //// Fleet Goal Access | We don't want to expose the inner details ////
        public bool HasFleetGoal => GoalStack.Count > 0;
        public Vector2 NextGoalMovePosition => GoalStack.Peek().MovePosition;
        public FleetGoal PopGoalStack() => GoalStack.Pop();
        public void ClearFleetGoals() => GoalStack.Clear();
        ///////////////////////////////////////////////////////////////////////

        public ShipGroup()
        {
        }

        public ShipGroup(Array<Ship> shipList, Vector2 start, Vector2 end, Vector2 direction, Empire owner)
        {
            Owner = owner;
            FinalDirection = direction;
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

        public virtual bool AddShip(Ship ship)
        {
            if (!Ships.AddUnique(ship)) return false;
            LastAveragePosUpdate = -1; // deferred position refresh
            LastStrengthUpdate = -1;
            return true;
        }

        protected void AssignPositionTo(Ship ship) => ship.FleetOffset = GetPositionFromDirection(ship, FinalDirection);

        public Vector2 GetPositionFromDirection(Ship ship, Vector2 direction)
        {
            float angle = ship.RelativeFleetOffset.ToRadians() + direction.ToRadians();
            float distance = ship.RelativeFleetOffset.Length();
            return angle.RadiansToDirection() * distance;
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
            IsAssembling = true;

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
                maxRadius = Math.Max(maxRadius, shipList[i].Radius);
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
            LastStrengthUpdate = -1;

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
                    {
                        var ship = Ships[i++]; 
                        ship.RelativeFleetOffset = new Vector2(x-cx, y) * shipSpacing;
                        AssignPositionTo(ship);
                    }
                }
                else
                {
                    int remaining = ships.Length - i;
                    float cx2 = remaining*0.5f - 0.5f; // last line center offset by remaining ships
                    for (int x = 0; x < remaining; ++x)
                    {
                        var ship = Ships[i++];
                        ship.RelativeFleetOffset = new Vector2(x-cx2, y) * shipSpacing;
                        AssignPositionTo(ship);
                    }
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

        public static Vector2 GetAveragePosition(Array<Ship> ships, Ship commandShip = null)
        {
            int count = ships.Count;
            if (count == 0)
                return Vector2.Zero;

            if (commandShip != null) return commandShip.Center - commandShip.FleetOffset; 

            float fleetCapableShipCount = 1;
            Ship[] items                = ships.GetInternalArrayItems();
            commandShip                 = commandShip ?? items[0];
            Vector2 avg = commandShip.Center - commandShip.FleetOffset;
            float commandShipSize       = commandShip.SurfaceArea;
 
            for (int i = 0; i < count; ++i)
            {
                Ship ship = items[i];
                if (ship != commandShip && ship.CanTakeFleetMoveOrders())
                {
                    float ratio            = ship.SurfaceArea / commandShipSize;
                    fleetCapableShipCount += (1f * ratio);
                    Vector2 p = (ship.Center -  ship.FleetOffset) * ratio;
                    avg.X += p.X;
                    avg.Y += p.Y;
                }
            }
            return avg / fleetCapableShipCount;
        }

        static Vector2 GetAverageOffsetFromZero(Array<Ship> ships)
        {
            int count = ships.Count;
            if (count == 0)
                return Vector2.Zero;

            Ship[] items = ships.GetInternalArrayItems();
            Vector2 avg = items[0].AI.FleetNode?.FleetOffset ?? items[0].FleetOffset;
            for (int i = 1; i < count; ++i)
            {
                Vector2 p = items[i].AI.FleetNode?.FleetOffset ?? items[i].FleetOffset;
                avg.X += p.X;
                avg.Y += p.Y;
            }
            return avg / count;
        }

        /// <summary> Use for DrawThread </summary>
        public Vector2 CachedAveragePos => AveragePos;

        public Vector2 AveragePosition(bool force = false)
        {
            // Update Pos once per frame, OR if LastAveragePosUpdate was invalidated
            // force check is pretty rare so evaluate last
            if (LastAveragePosUpdate != (StarDriveGame.Instance?.FrameId ?? -1) || force)
            {
                LastAveragePosUpdate = StarDriveGame.Instance?.FrameId ?? LastAveragePosUpdate;
                AveragePos = GetAveragePosition(Ships, CommandShip);
                AverageOffsetFromZero = GetAverageOffsetFromZero(Ships);
            }
            return AveragePos;
        }

        // Needs a storage value
        public float GetStrength()
        {
            // Update Strength once per frame, OR if LastStrengthUpdate was invalidated
            if (LastStrengthUpdate != StarDriveGame.Instance.FrameId)
            {
                LastStrengthUpdate = StarDriveGame.Instance.FrameId;
                Strength = 0f;
                for (int i = 0; i < Ships.Count; i++)
                {
                    Ship ship = Ships[i];
                    if (ship.Active)
                        Strength += ship.GetStrength();
                }
            }
            return Strength;
        }

        public float GetBomberStrength()
        {
            float str = 0f;
            for (int i = 0; i < Ships.Count; i++)
            {
                Ship ship = Ships[i];
                if (ship.Active && ship.DesignRole == ShipData.RoleName.bomber)
                    str += ship.GetStrength();
            }

            return str;
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

        public void FormationWarpTo(Vector2 finalPosition, Vector2 finalDirection, bool queueOrder, bool offensiveMove = false, bool forceAssembly = false)
        {
            GoalStack.Clear();
            AssembleFleet(finalPosition, finalDirection, forceAssembly: forceAssembly);

            for (int i = 0; i < Ships.Count; ++i)
            {
                Ship ship = Ships[i];
                if (queueOrder)
                    ship.AI.OrderFormationWarpQ(FinalPosition + ship.FleetOffset, finalDirection, offensiveMove: offensiveMove);
                else
                    ship.AI.OrderFormationWarp(FinalPosition + ship.FleetOffset, finalDirection, offensiveMove: offensiveMove);

                if (ship.loyalty == EmpireManager.Player)
                    ship.AI.OrderHoldPositionOffensive(FinalPosition + ship.FleetOffset, finalDirection);
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
                    ship.AI.ResetPriorityOrder(true);
                    ship.AI.OrderMoveDirectlyTo(FinalPosition + ship.FleetOffset, finalDirection, true, AIState.MoveTo);
                }
            }
        }

        public void MoveToNow(Vector2 finalPosition, Vector2 finalDirection, bool offensiveMove = false)
        {
            AssembleFleet(finalPosition, finalDirection, true);

            foreach (Ship ship in Ships)
            {
                ship.AI.SetPriorityOrder(true);
                
                ship.AI.ResetPriorityOrder(false);
                ship.AI.OrderMoveTo(FinalPosition + ship.FleetOffset, finalDirection, true, 
                    AIState.MoveTo, null, offensiveMove);
            }
        }

        /// <summary>
        /// This will force all ships in fleet to orbit planet.
        /// There are no checks here for ships already in some action.
        /// this can cause a cancel current order and orbit loop.
        /// </summary>
        internal void DoOrbitAreaRestricted(Planet planet, Vector2 position, float radius, bool excludeInvade = false)
        {
            for (int i = 0; i < Ships.Count; i++)
            {
                Ship ship = Ships[i];
                if (excludeInvade && (ship.DesignRole == ShipData.RoleName.troopShip || ship.shipData.Role == ShipData.RoleName.troop))
                    continue;

                if (ship.AI.State == AIState.Orbit || !ship.Center.InRadius(position, radius))
                    continue;

                ship.OrderToOrbit(planet);
            }
        }

        [Flags]
        public enum MoveStatus
        {
            None              = 0,
            Dispersed         = 1,
            Assembled         = 2,
            DispersedInCombat = 4,
            AssembledInCombat = 8,
            MajorityAssembled = 16,
            All               = ~(~0 << 5)
            
        }

        public MoveStatus FleetMoveStatus(float radius = 0, Vector2 ao = default)
        {
            if (ao == default)
                ao = FinalPosition;
            radius = radius.AlmostZero() ? GetRelativeSize().Length() : radius;

            float netStrengthInAO = Owner.GetEmpireAI().ThreatMatrix.PingNetHostileStr(ao, radius, Owner);

            MoveStatus moveStatus = MoveStatus.None;
            float assembled       = 0;
            int totalShipCount    = 0;

            for (int i = 0; i < Ships.Count; i++)
            {
                if (moveStatus.HasFlag(MoveStatus.All)) break;

                Ship ship = Ships[i];
                if (ship.AI.State == AIState.HoldPosition || ship.AI.State == AIState.Bombard 
                                                          || ship.AI.State == AIState.AssaultPlanet
                                                          || ship.AI.State == AIState.Resupply)
                {
                    continue;
                }

                totalShipCount++;
                if (!ship.IsSpoolingOrInWarp)
                {
                    var combatRadius = radius;
                    if (ship.Center.OutsideRadius(ao , combatRadius))
                    {
                        if (ship.CanTakeFleetOrders)
                            moveStatus |= MoveStatus.Dispersed;

                        bool cantAttackValidTarget = ship.AI.Target?.BaseStrength > 0 && ship.AI.HasPriorityOrder;

                        if (cantAttackValidTarget && ship.AI.Target.Center.InRadius(ship.Center, ship.AI.FleetNode.OrdersRadius))
                        {
                            moveStatus |= MoveStatus.DispersedInCombat;
                        }
                    }
                    else //Ship is in AO
                    {
                        assembled++;

                        moveStatus |= MoveStatus.Assembled;

                        if (netStrengthInAO > 0 && ship.AI.Target?.BaseStrength > 0 && ship.AI.Target.Center.InRadius(ship.Center, ship.AI.FleetNode.OrdersRadius))
                        {
                            moveStatus |= MoveStatus.AssembledInCombat;
                        }
                    }
                }
                else if (ship.CanTakeFleetOrders)
                    moveStatus |= MoveStatus.Dispersed;
            }
            if (assembled / totalShipCount > 0.5f)
                moveStatus |= MoveStatus.MajorityAssembled;
            return moveStatus;
        }

        public enum CombatStatus
        {
            InCombat = 0,
            EnemiesNear,
            ClearSpace,
            NotApplicable
        }

        public CombatStatus FleetInAreaInCombat(Vector2 position, float radius)
        {
            for (int i = 0; i < Ships.Count; ++i)
            {
                Ship ship = Ships[i];
                CombatStatus status = CombatStatusOfShipInArea(ship, position, radius);
                if (status != CombatStatus.ClearSpace)
                {
                    ClearPriorityOrderIfSubLight(ship);
                    return status;
                }
            }
            return CombatStatus.ClearSpace;
        }

        protected CombatStatus CombatStatusOfShipInArea(Ship ship, Vector2 position, float radius)
        {
            float combatRadius = Math.Min(radius, ship.AI.FleetNode.OrdersRadius);
            if (!ship.CanTakeFleetOrders || ship.Center.OutsideRadius(position + ship.FleetOffset, combatRadius))
                return CombatStatus.ClearSpace;

            if (ship.InCombat) return CombatStatus.InCombat;
            if (ship.AI.BadGuysNear) return CombatStatus.EnemiesNear;
            return CombatStatus.ClearSpace;
        }

        protected bool ClearPriorityOrderIfSubLight(Ship ship)
        {
            if (!ship.IsSpoolingOrInWarp)
            {
                ship.AI.ClearPriorityOrderAndTarget();
                ship.AI.ChangeAIState(AIState.AwaitingOrders);
                return true;
            }
            return false;
        }

        public float GetSpeedLimitFor(Ship ship)
        {
            if (ship.Center.InRadius(AveragePos + ship.FleetOffset, 1000))
                return SpeedLimit;
            return 0;
        }

        public void SetSpeed()
        {
            if (Ships.Count == 0)
                return;
            float slowestSpeed = float.MaxValue;
            for (int i = 0; i < Ships.Count; i++) 
            {
                Ship ship = Ships[i];

                if (ship.CanTakeFleetMoveOrders() && !ship.InCombat)
                {
                    if (CommandShip == null || ship.Center.InRadius(AveragePos + ship.FleetOffset, 15000))
                        slowestSpeed = Math.Min(ship.VelocityMaximum, slowestSpeed);
                }
            }
            SpeedLimit = Math.Max(200, (float)Math.Round(slowestSpeed));
        }
    }
}