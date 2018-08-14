using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    public class ShipGroup : IDisposable
    {
        public BatchRemovalCollection<Ship> Ships;
        public float ProjectedFacing;
        public float Speed;
        public Empire Owner;
        public Vector2 Position;
        public float Facing;
        protected Stack<Fleet.FleetGoal> GoalStack;
        public Vector2 GoalMovePosition;
        public Array<Ship> FleetTargetList;
        [XmlIgnore][JsonIgnore] public Vector2 StoredFleetPosition;
        [XmlIgnore][JsonIgnore] public float StoredFleetDistancetoMove;
        [XmlIgnore][JsonIgnore] public IReadOnlyList<Ship> GetShips => Ships;
        public override string ToString() => $"FleetGroup size={Ships.Count}";

        public Stack<Fleet.FleetGoal> GetStack() => GoalStack;

        public Fleet.FleetGoal PopGoalStack => GoalStack.Pop();

        public ShipGroup()
        {
            Ships = new BatchRemovalCollection<Ship>();
            FleetTargetList = new Array<Ship>();
        }

        public void InitializeGoalStack()
        {
            GoalStack = new Stack<Fleet.FleetGoal>();
            GoalMovePosition = new Vector2();
        }

        public void ProjectPos(Vector2 projectedPosition, float facing)
        {
            ProjectedFacing = facing;
            foreach (Ship ship in Ships)
            {
                float angle = ship.RelativeFleetOffset.ToRadians() + facing;
                float distance = ship.RelativeFleetOffset.Length();
                ship.projectedPosition = projectedPosition + Vector2.Zero.PointFromRadians(angle, distance);
            }
        }

        public void ProjectPosNoOffset(Vector2 projectedPosition, float facing)
        {
            ProjectedFacing = facing;
            foreach (Ship ship in Ships)
            {
                //float angle = ship.RelativeFleetOffset.ToRadians() + facing;
                //float distance = ship.RelativeFleetOffset.Length();
                ship.projectedPosition = projectedPosition + Vector2.Zero.PointFromRadians(facing, 1);
            }
        }

        public bool ContainsShip(Ship ship)
        {
            using (Ships.AcquireReadLock())
            {
                if (Ships.Contains(ship))
                    return true;
            }
            return false;
        }

        public virtual void AddShip(Ship ship)
        {
            using (Ships.AcquireWriteLock())
            {
                Ships.Add(ship);
            }
        }
        public int CountShips => Ships.Count;

        public void AssignPositions(float facing)
        {
            Facing = facing;
            foreach (Ship ship in Ships)
            {
                float angle = ship.RelativeFleetOffset.ToRadians() + facing;
                float distance = ship.RelativeFleetOffset.Length();
                ship.FleetOffset = Vector2.Zero.PointFromRadians(angle, distance);
            }
        }

        public void AssembleFleet(float facing, Vector2 facingVec, bool forceAssembly = false)
        {
            Facing = facing;
            foreach (Ship ship in Ships)
            {
                if (ship.AI.State != AIState.AwaitingOrders && !forceAssembly)
                    continue;
                float angle = ship.RelativeFleetOffset.ToRadians() + facing;
                float distance = ship.RelativeFleetOffset.Length();
                ship.FleetOffset = Vector2.Zero.PointFromRadians(angle, distance);
            }
        }
        public void AssembleAdhocGroup(Array<Ship> shipList, Vector2 fleetRightCorner, Vector2 fleetLeftCorner,  float facingRadians, Vector2 fVec,  Empire owner)
        {                                    
            float clickDistance = Vector2.Distance(fleetLeftCorner, fleetRightCorner);
            int row = 0;
            int column = 0;
            float maxRadius = 0.0f;
            for (int i = 0; i < shipList.Count; ++i)                            
                maxRadius = Math.Max(maxRadius, shipList[i].GetSO().WorldBoundingSphere.Radius);
            
            if (shipList.Count * maxRadius > clickDistance)
            {
                for (int i = 0; i < shipList.Count; ++i)
                {
                    AddShip(shipList[i]);
                    Ships[i].RelativeFleetOffset =
                        new Vector2((maxRadius + 200f) * column, row * (maxRadius + 200f));
                    ++column;
                    if (!(Ships[i].RelativeFleetOffset.X + maxRadius > clickDistance)) continue;
                    column = 0;
                    ++row;
                }
            }
            else
            {
                float num7 = clickDistance / shipList.Count;
                for (int i = 0; i < shipList.Count; ++i)
                {
                    AddShip(shipList[i]);
                    Ships[i].RelativeFleetOffset = new Vector2(num7 * i, 0.0f);
                }
            }
            ProjectPos(fleetLeftCorner, facingRadians - 1.570796f);
        }
        public Vector2 FindAveragePosition()
        {
            if (StoredFleetPosition == Vector2.Zero)
                StoredFleetPosition = FindAveragePositionset();
            //else if (Ships.Count != 0)
            //    StoredFleetPosition = Ships[0].Center;
            return StoredFleetPosition;
        }

        public Vector2 FindAveragePositionset()
        {
            if (Ships.Count == 0)
                return Vector2.Zero;

            Vector2 center = Vector2.Zero;
            using (Ships.AcquireReadLock())
            {
                foreach (var ship in Ships) center += ship.Position;
            }
            center /= Ships.Count;
            return center;
        }

        public void Setavgtodestination()
        {
            Array<float> distances = new Array<float>();
            using (Ships.AcquireReadLock())
                foreach (Ship distance in Ships)
                {
                    if (distance.EnginesKnockedOut || !distance.Active || distance.InCombat)
                        continue;
                    distances.Add(Vector2.Distance(distance.Center, Position + distance.FleetOffset) - 100);
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

        public void MoveDirectlyNow(Vector2 movePosition, float facing, Vector2 fVec)
        {
            Position = movePosition;
            Facing = facing;
            AssembleFleet(facing, fVec);
            foreach (Ship ship in Ships)
            {
                //Prevent fleets with no tasks from and are near their distination from being dumb.
                if (Owner.isPlayer || ship.AI.State == AIState.AwaitingOrders || ship.AI.State == AIState.AwaitingOffenseOrders)
                {
                    ship.AI.SetPriorityOrder(true);
                    ship.AI.OrderMoveDirectlyTowardsPosition(movePosition + ship.FleetOffset, facing, fVec, true);
                }
            }
        }




        public virtual void FormationWarpTo(Vector2 movePosition, float facing, Vector2 fvec, bool queueOrder = false)
        {
            GoalStack?.Clear();
            Position = movePosition;
            Facing = facing;            
            AssembleFleet(facing, fvec, !queueOrder);
            using(Ships.AcquireReadLock())
            foreach (Ship ship in Ships)
            {
                ship.AI.SetPriorityOrder(!queueOrder);
                if (queueOrder) ship.AI.OrderFormationWarpQ(movePosition + ship.FleetOffset, facing, fvec);
                else ship.AI.OrderFormationWarp(movePosition + ship.FleetOffset, facing, fvec);
            }
        }


        public void MoveToDirectly(Vector2 movePosition, float facing, Vector2 fVec)
        {
            Position = FindAveragePosition();
            GoalStack?.Clear();
            MoveDirectlyNow(movePosition, facing, fVec);
        }
        

        public void MoveToNow(Vector2 movePosition, float facing, Vector2 fVec)
        {
            Position = movePosition;
            Facing = facing;
            AssembleFleet(facing, fVec, true);
            foreach (Ship ship in Ships)
            {
                ship.AI.SetPriorityOrder(false);
                ship.AI.OrderMoveTowardsPosition(movePosition + ship.FleetOffset, facing, fVec, true, null);
            }
        }

        public void AttackMoveTo(Vector2 movePosition)
        {
            GoalStack.Clear();
            Vector2 fVec = FindAveragePosition().DirectionToTarget(movePosition);
            Position = FindAveragePosition() + fVec * 3500f;
            GoalStack.Push(new Fleet.FleetGoal(this, movePosition, FindAveragePosition().RadiansToTarget(movePosition), fVec, Fleet.FleetGoalType.AttackMoveTo));
        }

        public float GetStrength()
        {
            float num = 0.0f;            
            for (int index = 0; index < Ships.Count; index++)
            {
                Ship ship = Ships[index];
                if (ship.Active)
                    num += ship.GetStrength();
            }
            return num;
        }

        public bool AnyShipWithShipWithStrength()
        {
            return CountShipsWithStrength(true) == 1;
        }

        public int CountShipsWithStrength(bool any = false)
        {
            int num = 0;
            for (int index = 0; index < Ships.Count; index++)
            {
                Ship ship = Ships[index];
                if (ship.Active && ship.GetStrength() > 0)
                    num++;
                if (any) break;
            }
            return num;
        }
        /// <summary>
        /// This will force all ships in fleet to orbit planet. 
        /// There are no checks here for ships already in some action.
        /// this can cause a cancel current order and orbit loop.
        /// </summary>
        /// <param name="planet"></param>
        internal void DoOrbitAreaRestricted(Planet planet, Vector2 position, float radius)
        {
            foreach (var ship in Ships)
            {
                if (ship.AI.State == AIState.Orbit) continue;
                if (ship.Center.OutsideRadius(ship.Center, radius)) continue;
                ship.DoOrbit(planet);
            }
        }

        public int CountShipsWithStrength()
        {
            using (Ships.AcquireReadLock())
            {
                return Ships.Count(ship => ship.GetStrength() > 0);
            }
        }
        public int CountFleetAssaultTroops()
        {
            using (Ships.AcquireReadLock())
            {
                return Ships.Sum(ship => ship.Carrier.PlanetAssaultCount);
            }
        }

        public enum MoveStatus
        {
            InCombat = 0,
            Dispersed,
            Assembled            
        }
        
        public MoveStatus IsFleetAssembled(float radius, Vector2 position = default(Vector2))
        {
            if (position == default(Vector2))
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
                var ship = Ships[x];
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
            // using (Ships.AcquireReadLock())
            {
                if (Ships.Count == 0)
                    return;
                float slowestSpeed = Ships[0].Speed;
                for (int i = 0; i < Ships.Count; i++)     //Modified this so speed of a fleet is only set in one place -Gretman
                {
                    Ship ship = Ships[i];
                    if (ship.Inhibited || ship.EnginesKnockedOut || !ship.Active || ship.AI.State != AIState.FormationWarp)
                        continue;
                    if (ship.Speed < slowestSpeed) slowestSpeed = ship.Speed;
                }
                if (slowestSpeed < 200) slowestSpeed = 200;
                Speed = slowestSpeed;
            }
        }
    }
}