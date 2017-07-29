using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Ship_Game.AI;


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
        [XmlIgnore][JsonIgnore] public Vector2 StoredFleetPosition;
        [XmlIgnore][JsonIgnore] public float StoredFleetDistancetoMove;
        [XmlIgnore][JsonIgnore] public IReadOnlyList<Ship> GetShips => Ships;
        public override string ToString() => $"FleetGroup size={Ships.Count}";

        public Stack<Fleet.FleetGoal> GetStack() => GoalStack;

        public Fleet.FleetGoal PopGoalStack => GoalStack.Pop();

        public ShipGroup()
        {
            Ships = new BatchRemovalCollection<Ship>();
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
                if (ship.AI.State == AIState.AwaitingOrders || forceAssembly)
                {
                    float angle = ship.RelativeFleetOffset.ToRadians() + facing;
                    float distance = ship.RelativeFleetOffset.Length();
                    ship.FleetOffset = Vector2.Zero.PointFromRadians(angle, distance);
                }
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
            else if (Ships.Count != 0)
                StoredFleetPosition = Ships[0].Center;
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
                    distances.Add(Vector2.Distance(distance.Center, this.Position + distance.FleetOffset) - 100);
                }

            if (distances.Count <= 2)
            {
                this.StoredFleetDistancetoMove = Vector2.Distance(this.StoredFleetPosition, this.Position);
                return;
            }
            float avgdistance = distances.Average();
            float sum = (float)distances.Sum(distance => Math.Pow(distance - avgdistance, 2));
            float stddev = (float)Math.Sqrt((sum) / (distances.Count - 1));
            this.StoredFleetDistancetoMove = distances.Where(distance => distance <= avgdistance + stddev).Average();
        }

        protected bool IsFleetSupplied()
        {
            float currentAmmo = 0.0f;
            float maxAmmo = 0.0f;
            float ammoDps = 0.0f;
            float energyDps = 0.0f;
            //TODO: make sure this is the best way. Likely these values can be done in ship update and totaled here rather than recalculated.
            for (int index = 0; index < this.Ships.Count; index++)
            {
                Ship ship = this.Ships[index];
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
            if (ammoDps >= (ammoDps + energyDps) * 0.5f && currentAmmo <= maxAmmo * 0.1f) //is ammo really needed and if so is ammo < 1/10th of max
            {
                return false;
            }
            return true;
        }

        public void MoveDirectlyNow(Vector2 movePosition, float facing, Vector2 fVec)
        {
            Position = movePosition;
            Facing = facing;
            AssembleFleet(facing, fVec);
            foreach (Ship ship in this.Ships)
            {
                //Prevent fleets with no tasks from and are near their distination from being dumb.
                if (Owner.isPlayer || ship.AI.State == AIState.AwaitingOrders || ship.AI.State == AIState.AwaitingOffenseOrders)
                {
                    ship.AI.SetPriorityOrder();
                    ship.AI.OrderMoveDirectlyTowardsPosition(movePosition + ship.FleetOffset, facing, fVec, true);
                }
            }
        }




        public virtual void FormationWarpTo(Vector2 movePosition, float facing, Vector2 fvec, bool queueOrder = false)
        {
            GoalStack?.Clear();
            Position = movePosition;
            Facing = facing;
            AssembleFleet(facing, fvec);
            using(Ships.AcquireReadLock())
            foreach (Ship ship in Ships)
            {
                ship.AI.SetPriorityOrder();
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
            this.Position = movePosition;
            this.Facing = facing;
            this.AssembleFleet(facing, fVec);
            foreach (Ship ship in this.Ships)
            {
                ship.AI.SetPriorityOrder();
                ship.AI.OrderMoveTowardsPosition(movePosition + ship.FleetOffset, facing, fVec, true, null);
            }
        }

        public void AttackMoveTo(Vector2 movePosition)
        {
            this.GoalStack.Clear();
            Vector2 fVec = this.FindAveragePosition().DirectionToTarget(movePosition);
            this.Position = this.FindAveragePosition() + fVec * 3500f;
            this.GoalStack.Push(new Fleet.FleetGoal(this, movePosition, FindAveragePosition().RadiansToTarget(movePosition), fVec, Fleet.FleetGoalType.AttackMoveTo));
        }

        public float GetStrength()
        {
            float num = 0.0f;
            for (int index = 0; index < this.Ships.Count; index++)
            {
                Ship ship = this.Ships[index];
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
            for (int index = 0; index < this.Ships.Count; index++)
            {
                Ship ship = this.Ships[index];
                if (ship.Active && ship.GetStrength() > 0)
                    num++;
                if (any) break;
            }
            return num;
        }

        public int CountShipsWithStrength(out int troops)
        {
            int num = 0;
            troops = 0;
            for (int index = 0; index < this.Ships.Count; index++)
            {
                Ship ship = this.Ships[index];
                if (ship.Active && ship.GetStrength() > 0)
                    num++;
                troops += ship.PlanetAssaultCount;
            }
            return num;
        }
        public bool IsFleetAssembled(float radius, out bool endTask, Vector2 position = default(Vector2))
        {
            if (position == default(Vector2)) position = Position;
            endTask = false;
            bool assembled = true;
            //using (Ships.AcquireReadLock())
            {
                for (int index = 0; index < Ships.Count; index++)
                {
                    Ship ship = Ships[index];
                    if (ship.EMPdisabled || !ship.hasCommand || !ship.Active)
                        continue;
                    if (ship.Center.OutsideRadius(position + ship.FleetOffset, radius))
                    {
                        assembled = false;
                        continue;
                    }

                    if (!ship.InCombat) continue;
                    endTask = true;
                }
            }
            return assembled;
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
                float slowestSpeed = Ships[0].speed;
                for (int i = 0; i < Ships.Count; i++)     //Modified this so speed of a fleet is only set in one place -Gretman
                {
                    Ship ship = Ships[i];
                    if (ship.Inhibited || ship.EnginesKnockedOut || !ship.Active)
                        continue;
                    if (ship.speed < slowestSpeed) slowestSpeed = ship.speed;
                }
                if (slowestSpeed < 200) slowestSpeed = 200;
                Speed = slowestSpeed;
            }
        }
    }
}