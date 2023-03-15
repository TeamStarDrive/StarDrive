using Ship_Game.AI;
using Ship_Game.Ships;
using System;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Fleets;
using Vector2 = SDGraphics.Vector2;
using System.Diagnostics;

namespace Ship_Game
{
    [StarDataType]
    public class ShipGroup
    {
        [StarData] public readonly Array<Ship> Ships = new();
        [StarData] public Empire Owner;

        public Ship CommandShip
        {
            get         => LeadShip?.Leader;
            private set => LeadShip = new GroupLeader(value, value?.Fleet);
        }

        GroupLeader LeadShip;

        public void SetCommandShip(Ship ship) => CommandShip = ship;

        /// <summary>
        /// STL Speed LIMIT of the entire ship group, so the ships can stay together
        /// </summary>
        [StarData] public float STLSpeedLimit { get; private set; }

        /// <summary>
        /// FTL Speed LIMIT of the entire ship group, so the ships can stay together in warp
        /// </summary>
        [StarData] public float FTLSpeedLimit { get; private set; }

        // FINAL DESTINATION center position of the ship group
        // This can also be considered as the ASSEMBLY POSITION
        // If you set this to X location, ships will gather around it when idle
        [StarData] public Vector2 FinalPosition;

        // FINAL direction facing of this ship group
        [StarData] public Vector2 FinalDirection = Vectors.Up;

        // Holo-Projection of the ship group
        public Vector2 ProjectedPos;
        public Vector2 ProjectedDirection;

        /// <summary>
        /// Cached average position of the fleet.
        /// The average pos is the command ship's pos, if exists
        /// </summary>
        protected Vector2 AveragePos;
        int LastAveragePosUpdate = -1;

        protected float Strength;
        int LastStrengthUpdate = -1;

        public int CountShips => Ships.Count;
        public override string ToString() => $"FleetGroup ships={Ships.Count}";

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
                ship.ProjectedPosition = projectedPos + angle.RadiansToDirection()*distance;
            }
        }

        // This is used for single-ship groups
        public void ProjectPosNoOffset(Vector2 projectedPos, Vector2 direction)
        {
            ProjectedPos = projectedPos;
            ProjectedDirection = direction;
            for (int i = 0; i < Ships.Count; ++i)
                Ships[i].ProjectedPosition = projectedPos + direction;
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

        /// <summary>
        /// Assembles the fleet at final position and direction
        /// </summary>
        /// <param name="finalPosition">Final position of the formation</param>
        /// <param name="finalDirection">Final direction of the formation</param>
        /// <param name="forceAssembly">Whether to always recalculate FleetOffset</param>
        public void AssembleFleet(Vector2 finalPosition, Vector2 finalDirection, bool forceAssembly)
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
                maxRadius = Math.Max(maxRadius, shipList[i].Radius);
            return maxRadius;
        }

        static int GetShipOrder(Ship ship)
        {
            switch (ship.DesignRole)
            {
                case RoleName.fighter:    return 1;
                case RoleName.gunboat:    return 1;
                case RoleName.corvette:   return 1;
                case RoleName.bomber:     return 2; // bombers behind fighters
                case RoleName.frigate:    return 3;
                case RoleName.destroyer:  return 3;
                case RoleName.cruiser:    return 3;
                case RoleName.prototype:  return 3;
                case RoleName.battleship: return 4;
                case RoleName.capital:    return 5;
                default: return 6; // everything else to the back
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
                return a.Id.CompareTo(b.Id); // otherwise sort by ship ID which never changes
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
            for (int i = 0; i < Ships.Count; i++)
            {
                Ship ship = Ships[i];
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

        // Gets the average position of a group of ships, weighing the center
        // towards the biggest ship
        public static Vector2 GetAveragePosition(Array<Ship> ships)
        {
            int count = ships.Count;
            if (count == 0)
                return Vector2.Zero;

            Ship[] items = ships.GetInternalArrayItems();
            Ship biggestShip = items[0];
            float biggestSize = biggestShip.SurfaceArea;
            for (int i = 1; i < count; ++i)
            {
                Ship ship = items[i];
                // TODO: this IsAlive (with null) check is a concurrency issue,
                // TODO: the `ships` array is being modified while we calculate the avg pos
                if (ship is { IsAlive: true } && biggestSize < ship.SurfaceArea && ship.CanTakeFleetMoveOrders())
                {
                    biggestShip = ship;
                    biggestSize = ship.SurfaceArea;
                }
            }

            float totalRatioSum = 1f;
            Vector2 avg = biggestShip.Position - biggestShip.FleetOffset;
 
            for (int i = 1; i < count; ++i)
            {
                Ship ship = items[i];
                if (ship is { IsAlive: true } && ship.CanTakeFleetMoveOrders())
                {
                    float ratio = ship.SurfaceArea / biggestSize;
                    totalRatioSum += ratio;
                    Vector2 p = (ship.Position -  ship.FleetOffset) * ratio;
                    avg.X += p.X;
                    avg.Y += p.Y;
                }
            }
            return avg / totalRatioSum;
        }

        public Vector2 FleetCommandShipPosition => CommandShip != null ? CommandShip.Position : AveragePosition();

        /// <summary> Use for DrawThread </summary>
        public Vector2 CachedAveragePos => AveragePos;

        public Vector2 AveragePosition(bool force = false)
        {
            // Update Pos once per frame, OR if LastAveragePosUpdate was invalidated
            // force check is pretty rare so evaluate last
            if (force || StarDriveGame.Instance == null || LastAveragePosUpdate != StarDriveGame.Instance.FrameId)
            {
                LastAveragePosUpdate = StarDriveGame.Instance?.FrameId ?? 0;
                AveragePos = GetAveragePosition(Ships);
            }
            return AveragePos;
        }

        public float GetStrength()
        {
            // Update Strength once per frame, OR if LastStrengthUpdate was invalidated
            if (StarDriveGame.Instance == null || LastStrengthUpdate != StarDriveGame.Instance.FrameId)
            {
                LastStrengthUpdate = StarDriveGame.Instance?.FrameId ?? 0;
                Strength = 0f;
                for (int i = 0; i < Ships.Count; i++)
                {
                    Ship ship = Ships[i];
                    if (ship?.Active == true)
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
                if (ship.Active && ship.DesignRole == RoleName.bomber)
                    str += ship.GetStrength();
            }

            return str;
        }

        public Ship GetClosestShipTo(Vector2 worldPos)
        {
            return Ships.FindMin(ship => ship.Position.SqDist(worldPos));
        }

        /// <summary>
        /// Unified MoveTo command with ability to Queue WayPoints
        /// </summary>
        /// <param name="finalPos">Final position of the fleet</param>
        /// <param name="finalDir">Final direction of the fleet</param>
        /// <param name="order">MoveTo parameters, @see MoveOrder enum</param>
        public void MoveTo(Vector2 finalPos, Vector2 finalDir, MoveOrder order = MoveOrder.Regular/*DO NOT ADD ANY MORE PARAMETERS HERE FOR GOD SAKES, USE FLAGS*/)
        {
            if (this is Fleet) order |= MoveOrder.ReformAtWayPoint;
            bool forceAssembly = order.IsSet(MoveOrder.AddWayPoint) || order.IsSet(MoveOrder.ForceReassembly);
            AssembleFleet(finalPos, finalDir, forceAssembly: forceAssembly);

            UpdateSpeedLimit();

            foreach (Ship ship in Ships)
            {
                if (ship.PlayerShipCanTakeFleetOrders())
                {
                    // set PriorityOrder = true, so that our ships don't scatter after arriving
                    ship.AI.ResetPriorityOrder(clearOrders: false);

                    // Allow AI ships in gravity wells to react to incoming attacks
                    if (!ship.Loyalty.isPlayer && ship.IsInhibitedByUnfriendlyGravityWell)
                        order |= MoveOrder.Aggressive;

                    Vector2 finalShipPos = FinalPosition + ship.FleetOffset;
                    ship.AI.OrderMoveTo(finalShipPos, finalDir, AIState.FormationMoveTo, order);
                }
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
                if (excludeInvade && (ship.DesignRole == RoleName.troopShip || ship.ShipData.Role == RoleName.troop))
                    continue;

                if (ship.AI.State == AIState.Orbit || !ship.Position.InRadius(position, radius))
                    continue;

                ship.OrderToOrbit(planet, clearOrders:true, MoveOrder.Aggressive);
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

        public MoveStatus FleetMoveStatus(float radius, Vector2? ao = null)
        {
            Vector2 finalPos = ao ?? FinalPosition;
            float ourStrengthInAO = Owner.AI.ThreatMatrix.GetStrengthAt(Owner, finalPos, radius);
            float enemyStrengthInAO = Owner.AI.ThreatMatrix.GetHostileStrengthAt(finalPos, radius);

            MoveStatus moveStatus = MoveStatus.None;
            float surfaceAssembled = 0;
            int totalShipSurface = 0;

            for (int i = 0; i < Ships.Count; i++)
            {
                Ship ship = Ships[i];
                if (ship.AI.State is AIState.Bombard or AIState.AssaultPlanet or AIState.Resupply)
                    continue;

                totalShipSurface += ship.SurfaceArea;
                if (!ship.IsSpoolingOrInWarp)
                {
                    if (ship.Position.OutsideRadius(finalPos, radius))
                    {
                        if (ship.CanTakeFleetOrders)
                            moveStatus |= MoveStatus.Dispersed;

                        bool cantAttackValidTarget = ship.AI.Target?.BaseStrength > 0 && ship.AI.HasPriorityOrder;
                        if (cantAttackValidTarget && ship.AI.Target.Position.InRadius(ship.Position, ship.AI.FleetNode.OrdersRadius))
                            moveStatus |= MoveStatus.DispersedInCombat;
                    }
                    else // Ship is in AO
                    {
                        surfaceAssembled += ship.SurfaceArea;
                        moveStatus |= MoveStatus.Assembled;

                        if (ourStrengthInAO > enemyStrengthInAO && 
                            ship.AI.Target?.BaseStrength > 0 && ship.AI.Target.Position.InRadius(ship.Position, ship.AI.FleetNode.OrdersRadius))
                        {
                            moveStatus |= MoveStatus.AssembledInCombat;
                        }
                    }
                }
                else if (ship.CanTakeFleetOrders)
                    moveStatus |= MoveStatus.Dispersed;
            }

            if (surfaceAssembled / totalShipSurface > 0.85f)
                moveStatus |= MoveStatus.MajorityAssembled;

            return moveStatus;
        }

        public enum CombatStatus
        {
            InCombat = 0,
            EnemiesNear,
            ClearSpace,
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

        public bool IsAnyShipInCombat()
        {
            for (int i = 0; i < Ships.Count; ++i)
            {
                Ship ship = Ships[i];
                if (!ship.OnLowAlert)
                    return true;
            }
            return false;
        }

        protected CombatStatus CombatStatusOfShipInArea(Ship ship, Vector2 position, float radius)
        {
            float combatRadius = Math.Min(radius, ship.AI.FleetNode.OrdersRadius);
            if (!ship.CanTakeFleetOrders || ship.Position.OutsideRadius(position + ship.FleetOffset, combatRadius))
                return CombatStatus.ClearSpace;

            if (ship.InCombat) return CombatStatus.InCombat;
            if (ship.AI.BadGuysNear) return CombatStatus.EnemiesNear;
            return CombatStatus.ClearSpace;
        }

        protected void ClearPriorityOrderIfSubLight(Ship ship)
        {
            if (!ship.Loyalty.isPlayer && !ship.IsSpoolingOrInWarp)
            {
                ship.AI.ClearPriorityOrderAndTarget();
                ship.AI.ChangeAIState(AIState.AwaitingOrders);
            }
        }

        // @return The desired formation pos for this ship
        public Vector2 GetFormationPos(Ship ship)
        {
            return AveragePosition() + ship.FleetOffset;
        }

        /// <summary>
        /// The Final destination position for this ship:
        ///   Fleet.FinalPosition + ship.FleetOffset
        /// </summary>
        public Vector2 GetFinalPos(Ship ship)
        {
            if (CommandShip?.InCombat == true && 
                FinalPosition.InRadius(CommandShip.Position, CommandShip.AI.FleetNode.OrdersRadius))
            {
                return CommandShip.Position + ship.FleetOffset;
            }

            return FinalPosition + ship.FleetOffset;
        }

        /// <summary>
        /// Return TRUE if ship is within `range` of its designated Fleet formation Offset
        /// </summary>
        public bool IsShipInFormation(Ship ship, float range)
        {
            return ship.Position.InRadius(GetFormationPos(ship), range);
        }

        /// <summary>
        /// Returns TRUE if ship is within `range` of this fleets FinalPosition + ship.FleetOffset
        /// </summary>
        /// <param name="ship"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public bool IsShipAtFinalPosition(Ship ship, float range)
        {
            return ship.Position.InRadius(GetFinalPos(ship), range);
        }

        /// <summary>
        /// Gets the imposed speed limit set by the formation, or 0 which means there is no speed limit
        /// </summary>
        public float GetSTLSpeedLimitFor(Ship ship)
        {
            // if ship is within its formation position, use formation speed limit
            if (IsShipInFormation(ship, 1000f))
                return STLSpeedLimit;
            return 0; // otherwise, there is no limit
        }

        public void UpdateSpeedLimit()
        {
            if (Ships.Count == 0)
                return;

            // some ships are staying behind, but they can still catch up
            bool gotCapableStragglers = false;
            float speedCapSTL = 0f; // important that the default values are 0 (uncapped)
            float speedCapFTL = 0f;

            for (int i = 0; i < Ships.Count; ++i)
            {
                Ship ship = Ships[i];
                if (ship.CanTakeFleetMoveOrders() && !ship.InCombat)
                {
                    // the MaxFTLSpeed is often limited by WarpPercent, because the ship direction drifts too much
                    // from its final target position, see UpdateWarpThrust() for more details on SetWarpPercent
                    speedCapSTL = speedCapSTL != 0f ? Math.Min(speedCapSTL, ship.MaxSTLSpeed) : ship.MaxSTLSpeed;
                    speedCapFTL = speedCapFTL != 0f ? Math.Min(speedCapFTL, ship.MaxFTLSpeed) : ship.MaxFTLSpeed;

                    // this ship can take orders, but it's straggling really far
                    if (!IsShipInFormation(ship, 30000f))
                        gotCapableStragglers = true;
                }
            }

            // in order to allow ships to speed up / slow down
            // slightly to hold formation, set the fleet speed a bit lower (if some ships are not in formation)
            if (!gotCapableStragglers)
            {
                // full speed ahead
                STLSpeedLimit = speedCapSTL;
                FTLSpeedLimit = speedCapFTL;
            }
            else
            {
                // set the fleet speed a bit lower if some ships are not in formation
                STLSpeedLimit = speedCapSTL != 0f ? Math.Max(50, (float)Math.Round(speedCapSTL * 0.8f)) : 0f;
                FTLSpeedLimit = speedCapFTL != 0f ? Math.Max(50, (float)Math.Round(speedCapFTL * 0.8f)) : 0f;
            }
        }
    }
}