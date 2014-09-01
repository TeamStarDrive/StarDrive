// Type: Ship_Game.Gameplay.Fleet
// Assembly: StarDrive, Version=1.0.9.0, Culture=neutral, PublicKeyToken=null
// MVID: C34284EE-F947-460F-BF1D-3C6685B19387
// Assembly location: E:\Games\Steam\steamapps\common\StarDrive\oStarDrive.exe

using Microsoft.Xna.Framework;
using Ship_Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ship_Game.Gameplay
{
    public class Fleet : ShipGroup
    {
        public BatchRemovalCollection<FleetDataNode> DataNodes = new BatchRemovalCollection<FleetDataNode>();
        public Guid guid = Guid.NewGuid();
        public string Name = "";
        private Stack<Fleet.FleetGoal> GoalStack = new Stack<Fleet.FleetGoal>();
        private List<Ship> CenterShips = new List<Ship>();
        private List<Ship> LeftShips = new List<Ship>();
        private List<Ship> RightShips = new List<Ship>();
        private List<Ship> RearShips = new List<Ship>();
        private List<Ship> ScreenShips = new List<Ship>();
        public List<Fleet.Squad> CenterFlank = new List<Fleet.Squad>();
        public List<Fleet.Squad> LeftFlank = new List<Fleet.Squad>();
        public List<Fleet.Squad> RightFlank = new List<Fleet.Squad>();
        public List<Fleet.Squad> ScreenFlank = new List<Fleet.Squad>();
        public List<Fleet.Squad> RearFlank = new List<Fleet.Squad>();
        public List<List<Fleet.Squad>> AllFlanks = new List<List<Fleet.Squad>>();
        public Vector2 GoalMovePosition = new Vector2();
        private Dictionary<Vector2, List<Ship>> EnemyClumpsDict = new Dictionary<Vector2, List<Ship>>();
        private Dictionary<Ship, List<Ship>> InterceptorDict = new Dictionary<Ship, List<Ship>>();
        private int defenseTurns = 50;
        private Vector2 targetPosition = Vector2.Zero;
        public MilitaryTask Task;
        public Fleet.FleetCombatStatus fcs;
        public Empire Owner;
        public Vector2 Position;
        public float facing;
        public float speed;
        public int FleetIconIndex;
        private Fleet.FleetCombatStatus CenterCS;
        private Fleet.FleetCombatStatus ScreenCS;
        private Fleet.FleetCombatStatus LeftCS;
        private Fleet.FleetCombatStatus RightCS;
        private Fleet.FleetCombatStatus RearCS;
        private bool HasPriorityOrder;
        public static UniverseScreen screen;
        private bool InCombat;
        public int TaskStep;
        public bool IsCoreFleet;

        public Fleet()
        {
            this.FleetIconIndex = (int)RandomMath2.RandomBetween(1f, 10f);
        }

        public Fleet(bool temp, List<Ship> shiplist)
        {
            Fleet.Squad squad = new Fleet.Squad();
            squad.Fleet = this;
            for (int index = 0; index < Enumerable.Count<Ship>((IEnumerable<Ship>)shiplist); ++index)
            {
                if (squad.Ships.Count < 4)
                    squad.Ships.Add(Enumerable.ElementAt<Ship>((IEnumerable<Ship>)shiplist, index));
                if (squad.Ships.Count == 4 || index == Enumerable.Count<Ship>((IEnumerable<Ship>)shiplist) - 1)
                {
                    this.CenterFlank.Add(squad);
                    squad = new Fleet.Squad();
                    squad.Fleet = this;
                }
                this.Ships.Add(shiplist[index]);
            }
            this.AllFlanks.Add(this.CenterFlank);
            int num1 = 0;
            int num2 = 0;
            for (int index = 0; index < this.CenterFlank.Count; ++index)
            {
                if (index == 0)
                    this.CenterFlank[index].Offset = new Vector2(0.0f, 0.0f);
                else if (index % 2 == 1)
                {
                    ++num1;
                    this.CenterFlank[index].Offset = new Vector2((float)(num1 * -1400), 0.0f);
                }
                else
                {
                    ++num2;
                    this.CenterFlank[index].Offset = new Vector2((float)(num2 * 1400), 0.0f);
                }
            }
        }

        public void PushToStack(Fleet.FleetGoal g)
        {
            this.GoalStack.Push(g);
        }

        public Stack<Fleet.FleetGoal> GetStack()
        {
            return this.GoalStack;
        }

        public void AddShipORIG(Ship shiptoadd)
        {
            this.Ships.Add(shiptoadd);
            shiptoadd.fleet = this;
            IOrderedEnumerable<Ship> orderedEnumerable = Enumerable.OrderBy<Ship, float>((IEnumerable<Ship>)this.Ships, (Func<Ship, float>)(ship => ship.speed));
            this.speed = Enumerable.Count<Ship>((IEnumerable<Ship>)orderedEnumerable) > 0 ? Enumerable.ElementAt<Ship>((IEnumerable<Ship>)orderedEnumerable, 0).speed : 200f;
            Vector2 vector2 = shiptoadd.RelativeFleetOffset;
            this.AssignPositions(this.facing);
        }
        public void AddShip(Ship shiptoadd)
        {
            if (shiptoadd.Role == "station")
            {
                //dotoughnutrequisition is the actual cause I found
                return;
            }
            this.Ships.Add(shiptoadd);
            shiptoadd.fleet = this;
            IOrderedEnumerable<Ship> speedSorted =
                from ship in this.Ships
                orderby ship.speed
                select ship;
            this.speed = (speedSorted.Count<Ship>() > 0 ? speedSorted.ElementAt<Ship>(0).speed : 200f);
            Vector2 relativeFleetOffset = shiptoadd.RelativeFleetOffset;
            this.AssignPositions(this.facing);
        }

        public void SetSpeedORIG()
        {
            IOrderedEnumerable<Ship> orderedEnumerable = Enumerable.OrderBy<Ship, float>((IEnumerable<Ship>)this.Ships, (Func<Ship, float>)(ship => ship.speed));
            this.speed = Enumerable.Count<Ship>((IEnumerable<Ship>)orderedEnumerable) > 0 ? Enumerable.ElementAt<Ship>((IEnumerable<Ship>)orderedEnumerable, 0).speed : 200f;
            if ((double)this.speed != 0.0)
                return;
            this.speed = 200f;
        }
        //added by gremlin make fleet speed average not include warpless ships.
        public void SetSpeed()
        {//Vector2.Distance(this.findAveragePosition(),ship.Center) <10000 
            IOrderedEnumerable<Ship> speedSorted =
                from ship in this.Ships
                where !ship.EnginesKnockedOut && ship.IsWarpCapable && !ship.InCombat && !ship.Inhibited && ship.Active
                orderby ship.speed
                select ship;
            this.speed = (speedSorted.Count<Ship>() > 0 ? speedSorted.ElementAt<Ship>(0).speed : 200f);
            if (this.speed == 0f)
            {
                this.speed = 200f;
            }
        }

        public void IncrementFCS()
        {
            ++this.fcs;
            if (this.fcs > Fleet.FleetCombatStatus.Free)
                this.fcs = Fleet.FleetCombatStatus.Maintain;
            foreach (List<Fleet.Squad> Flank in this.AllFlanks)
                this.SetCombatStatusTo(this.fcs, Flank);
        }

        public void SetCombatStatusTo(Fleet.FleetCombatStatus fcs)
        {
            this.CenterCS = fcs;
            this.ScreenCS = fcs;
            this.LeftCS = fcs;
            this.RightCS = fcs;
            this.RearCS = fcs;
            foreach (Ship ship in (List<Ship>)this.Ships)
                ship.FleetCombatStatus = fcs;
        }

        public Fleet.FleetCombatStatus GetCombatStatus(List<Fleet.Squad> Flank)
        {
            if (this.CenterFlank == Flank)
                return this.CenterCS;
            if (this.ScreenFlank == Flank)
                return this.ScreenCS;
            if (this.LeftFlank == Flank)
                return this.LeftCS;
            if (this.RightFlank == Flank)
                return this.RightCS;
            if (this.RearFlank == Flank)
                return this.RearCS;
            else
                return Fleet.FleetCombatStatus.Maintain;
        }

        public void SetCombatStatusTo(Fleet.FleetCombatStatus fcs, List<Fleet.Squad> Flank)
        {
            if (this.CenterFlank == Flank)
            {
                this.CenterCS = fcs;
                foreach (Fleet.Squad squad in Flank)
                {
                    foreach (Ship ship in (List<Ship>)squad.Ships)
                        ship.FleetCombatStatus = fcs;
                }
            }
            else if (this.ScreenFlank == Flank)
            {
                this.ScreenCS = fcs;
                foreach (Fleet.Squad squad in Flank)
                {
                    foreach (Ship ship in (List<Ship>)squad.Ships)
                        ship.FleetCombatStatus = fcs;
                }
            }
            else if (this.LeftFlank == Flank)
            {
                this.LeftCS = fcs;
                foreach (Fleet.Squad squad in Flank)
                {
                    foreach (Ship ship in (List<Ship>)squad.Ships)
                        ship.FleetCombatStatus = fcs;
                }
            }
            else if (this.RightFlank == Flank)
            {
                this.RightCS = fcs;
                foreach (Fleet.Squad squad in Flank)
                {
                    foreach (Ship ship in (List<Ship>)squad.Ships)
                        ship.FleetCombatStatus = fcs;
                }
            }
            else if (this.RearFlank == Flank)
            {
                this.RearCS = fcs;
                foreach (Fleet.Squad squad in Flank)
                {
                    foreach (Ship ship in (List<Ship>)squad.Ships)
                        ship.FleetCombatStatus = fcs;
                }
            }
            else
            {
                foreach (Fleet.Squad squad in Flank)
                {
                    foreach (Ship ship in (List<Ship>)squad.Ships)
                        ship.FleetCombatStatus = fcs;
                }
            }
        }

        public void AttackPlanet(Planet planet)
        {
            foreach (Ship ship in (List<Ship>)this.Ships)
            {
                if (ship.Role != "troop")
                    ship.GetAI().OrderToOrbit(planet, true);
                else if (planet.Owner != null && planet.Owner == ship.loyalty)
                    ship.GetAI().GoRebase(planet);
                else
                    ship.GetAI().OrderToOrbit(planet, true);
            }
        }

        public void FlankAttackPlanet(Planet planet, List<Fleet.Squad> Flank)
        {
            foreach (Fleet.Squad squad in Flank)
            {
                foreach (Ship ship in (List<Ship>)squad.Ships)
                {
                    if (ship.Role != "troop")
                        ship.GetAI().OrderToOrbit(planet, true);
                    else if (planet.Owner != null && planet.Owner == ship.loyalty)
                        ship.GetAI().GoRebase(planet);
                    else
                        ship.GetAI().OrderToOrbit(planet, true);
                }
            }
        }

        public void SetAllShipsToHoldPosition()
        {
        }

        public void AutoArrange()
        {
            this.CenterShips.Clear();
            this.LeftShips.Clear();
            this.RightShips.Clear();
            this.ScreenShips.Clear();
            this.RearShips.Clear();
            this.CenterFlank.Clear();
            this.LeftFlank.Clear();
            this.RightFlank.Clear();
            this.ScreenFlank.Clear();
            this.RearFlank.Clear();
            this.AllFlanks.Add(this.CenterFlank);
            this.AllFlanks.Add(this.LeftFlank);
            this.AllFlanks.Add(this.RightFlank);
            this.AllFlanks.Add(this.ScreenFlank);
            this.AllFlanks.Add(this.RearFlank);
            BatchRemovalCollection<Ship> removalCollection = new BatchRemovalCollection<Ship>();
            foreach (Ship ship in (List<Ship>)this.Ships)
                removalCollection.Add(ship);
            foreach (Ship ship in (List<Ship>)removalCollection)
            {
                if (ship.Role == "scout")
                {
                    this.ScreenShips.Add(ship);
                    removalCollection.QueuePendingRemoval(ship);
                }
                if (ship.Role == "freighter")
                {
                    this.RearShips.Add(ship);
                    removalCollection.QueuePendingRemoval(ship);
                }
                if (ship.Role == "capital" || ship.Role == "carrier" || ship.Role == "cruiser")
                {
                    this.CenterShips.Add(ship);
                    removalCollection.QueuePendingRemoval(ship);
                }
            }
            removalCollection.ApplyPendingRemovals();
            IOrderedEnumerable<Ship> orderedEnumerable1 = Enumerable.OrderBy<Ship, float>((IEnumerable<Ship>)this.Ships, (Func<Ship, float>)(ship => ship.speed));
            this.speed = Enumerable.Count<Ship>((IEnumerable<Ship>)orderedEnumerable1) > 0 ? Enumerable.ElementAt<Ship>((IEnumerable<Ship>)orderedEnumerable1, 0).speed : 200f;
            IOrderedEnumerable<Ship> orderedEnumerable2 = Enumerable.OrderByDescending<Ship, float>((IEnumerable<Ship>)removalCollection, (Func<Ship, float>)(ship => ship.GetStrength() + (float)ship.Size));
            int num1 = 0;
            foreach (Ship ship in (IEnumerable<Ship>)orderedEnumerable2)
            {
                if (num1 < 4)
                {
                    this.CenterShips.Add(ship);
                    ++num1;
                }
                else if (num1 < 7)
                {
                    this.LeftShips.Add(ship);
                    ++num1;
                }
                else if (num1 < 11)
                {
                    this.RightShips.Add(ship);
                    ++num1;
                }
                else if (num1 < 15)
                {
                    this.ScreenShips.Add(ship);
                    ++num1;
                }
                if (num1 == 15)
                    num1 = 0;
            }
            IOrderedEnumerable<Ship> orderedEnumerable3 = Enumerable.OrderByDescending<Ship, int>((IEnumerable<Ship>)this.CenterShips, (Func<Ship, int>)(ship => ship.Size));
            Fleet.Squad squad1 = new Fleet.Squad();
            squad1.Fleet = this;
            for (int index = 0; index < Enumerable.Count<Ship>((IEnumerable<Ship>)orderedEnumerable3); ++index)
            {
                if (squad1.Ships.Count < 4)
                    squad1.Ships.Add(Enumerable.ElementAt<Ship>((IEnumerable<Ship>)orderedEnumerable3, index));
                if (squad1.Ships.Count == 4 || index == Enumerable.Count<Ship>((IEnumerable<Ship>)orderedEnumerable3) - 1)
                {
                    this.CenterFlank.Add(squad1);
                    squad1 = new Fleet.Squad();
                    squad1.Fleet = this;
                }
            }
            IOrderedEnumerable<Ship> orderedEnumerable4 = Enumerable.OrderByDescending<Ship, float>((IEnumerable<Ship>)this.LeftShips, (Func<Ship, float>)(ship => ship.speed));
            Fleet.Squad squad2 = new Fleet.Squad();
            squad2.Fleet = this;
            for (int index = 0; index < Enumerable.Count<Ship>((IEnumerable<Ship>)orderedEnumerable4); ++index)
            {
                if (squad2.Ships.Count < 4)
                    squad2.Ships.Add(Enumerable.ElementAt<Ship>((IEnumerable<Ship>)orderedEnumerable4, index));
                if (squad2.Ships.Count == 4 || index == Enumerable.Count<Ship>((IEnumerable<Ship>)orderedEnumerable4) - 1)
                {
                    this.LeftFlank.Add(squad2);
                    squad2 = new Fleet.Squad();
                    squad2.Fleet = this;
                }
            }
            IOrderedEnumerable<Ship> orderedEnumerable5 = Enumerable.OrderByDescending<Ship, float>((IEnumerable<Ship>)this.RightShips, (Func<Ship, float>)(ship => ship.speed));
            Fleet.Squad squad3 = new Fleet.Squad();
            squad3.Fleet = this;
            for (int index = 0; index < Enumerable.Count<Ship>((IEnumerable<Ship>)orderedEnumerable5); ++index)
            {
                if (squad3.Ships.Count < 4)
                    squad3.Ships.Add(Enumerable.ElementAt<Ship>((IEnumerable<Ship>)orderedEnumerable5, index));
                if (squad3.Ships.Count == 4 || index == Enumerable.Count<Ship>((IEnumerable<Ship>)orderedEnumerable5) - 1)
                {
                    this.RightFlank.Add(squad3);
                    squad3 = new Fleet.Squad();
                    squad3.Fleet = this;
                }
            }
            IOrderedEnumerable<Ship> orderedEnumerable6 = Enumerable.OrderByDescending<Ship, float>((IEnumerable<Ship>)this.ScreenShips, (Func<Ship, float>)(ship => ship.speed));
            Fleet.Squad squad4 = new Fleet.Squad();
            squad4.Fleet = this;
            for (int index = 0; index < Enumerable.Count<Ship>((IEnumerable<Ship>)orderedEnumerable6); ++index)
            {
                if (squad4.Ships.Count < 4)
                    squad4.Ships.Add(Enumerable.ElementAt<Ship>((IEnumerable<Ship>)orderedEnumerable6, index));
                if (squad4.Ships.Count == 4 || index == Enumerable.Count<Ship>((IEnumerable<Ship>)orderedEnumerable6) - 1)
                {
                    this.ScreenFlank.Add(squad4);
                    squad4 = new Fleet.Squad();
                    squad4.Fleet = this;
                }
            }
            IOrderedEnumerable<Ship> orderedEnumerable7 = Enumerable.OrderByDescending<Ship, float>((IEnumerable<Ship>)this.RearShips, (Func<Ship, float>)(ship => ship.speed));
            Fleet.Squad squad5 = new Fleet.Squad();
            squad5.Fleet = this;
            for (int index = 0; index < Enumerable.Count<Ship>((IEnumerable<Ship>)orderedEnumerable7); ++index)
            {
                if (squad5.Ships.Count < 4)
                    squad5.Ships.Add(Enumerable.ElementAt<Ship>((IEnumerable<Ship>)orderedEnumerable7, index));
                if (squad5.Ships.Count == 4 || index == Enumerable.Count<Ship>((IEnumerable<Ship>)orderedEnumerable7) - 1)
                {
                    this.RearFlank.Add(squad5);
                    squad5 = new Fleet.Squad();
                    squad5.Fleet = this;
                }
            }
            this.Position = this.findAveragePosition();
            int num2 = 0;
            int num3 = 0;
            for (int index = 0; index < this.CenterFlank.Count; ++index)
            {
                if (index == 0)
                    this.CenterFlank[index].Offset = new Vector2(0.0f, 0.0f);
                else if (index % 2 == 1)
                {
                    ++num2;
                    this.CenterFlank[index].Offset = new Vector2((float)(num2 * -1400), 0.0f);
                }
                else
                {
                    ++num3;
                    this.CenterFlank[index].Offset = new Vector2((float)(num3 * 1400), 0.0f);
                }
            }
            int num4 = 0;
            int num5 = 0;
            for (int index = 0; index < this.ScreenFlank.Count; ++index)
            {
                if (index == 0)
                    this.ScreenFlank[index].Offset = new Vector2(0.0f, -2500f);
                else if (index % 2 == 1)
                {
                    ++num4;
                    this.ScreenFlank[index].Offset = new Vector2((float)(num4 * -1400), -2500f);
                }
                else
                {
                    ++num5;
                    this.ScreenFlank[index].Offset = new Vector2((float)(num5 * 1400), -2500f);
                }
            }
            int num6 = 0;
            int num7 = 0;
            for (int index = 0; index < this.RearFlank.Count; ++index)
            {
                if (index == 0)
                    this.RearFlank[index].Offset = new Vector2(0.0f, 2500f);
                else if (index % 2 == 1)
                {
                    ++num6;
                    this.RearFlank[index].Offset = new Vector2((float)(num6 * -1400), 2500f);
                }
                else
                {
                    ++num7;
                    this.RearFlank[index].Offset = new Vector2((float)(num7 * 1400), 2500f);
                }
            }
            for (int index = 0; index < this.LeftFlank.Count; ++index)
                this.LeftFlank[index].Offset = new Vector2((float)(-this.CenterFlank.Count * 1400 - (this.LeftFlank.Count == 1 ? 1400 : index * 1400)), 0.0f);
            for (int index = 0; index < this.RightFlank.Count; ++index)
                this.RightFlank[index].Offset = new Vector2((float)(this.CenterFlank.Count * 1400 + (this.RightFlank.Count == 1 ? 1400 : index * 1400)), 0.0f);
            this.AutoAssembleFleet(0.0f, new Vector2(0.0f, -1f));
            foreach (Ship s in (List<Ship>)this.Ships)
            {
                lock (GlobalStats.WayPointLock)
                    s.GetAI().OrderThrustTowardsPosition(this.Position + s.FleetOffset, this.facing, new Vector2(0.0f, -1f), true);
                FleetDataNode fleetDataNode = new FleetDataNode();
                fleetDataNode.SetShip(s);
                fleetDataNode.ShipName = s.Name;
                fleetDataNode.FleetOffset = s.RelativeFleetOffset;
                fleetDataNode.OrdersOffset = s.RelativeFleetOffset;
                this.DataNodes.Add(fleetDataNode);
            }


            //foreach (List<Fleet.Squad> list in this.AllFlanks)
            ////Parallel.ForEach(this.AllFlanks, list =>
            //{
            //    foreach (Fleet.Squad squad6 in list)
            //    {


            //        foreach (Ship ship in (List<Ship>)squad6.Ships)
            //        {
            //            foreach (FleetDataNode fleetDataNode in (List<FleetDataNode>)this.DataNodes)
            //            {
            //                if (ship == fleetDataNode.GetShip())
            //                    squad6.DataNodes.Add(fleetDataNode);
            //            }
            //        }
            //    }
            //}//);
        }

        public override void MoveTo(Vector2 MovePosition, float facing, Vector2 fVec)
        {
            this.Position = this.findAveragePosition();
            if (this.InCombat)
                this.HasPriorityOrder = true;
            this.GoalStack.Clear();
            this.MoveToNow(MovePosition, facing, fVec);
        }

        public void MoveToAddQ(Vector2 MovePosition, float facing, Vector2 fVec)
        {
            this.Position = this.findAveragePosition();
            if (this.InCombat)
                this.HasPriorityOrder = true;
            this.GoalStack.Clear();
            this.MoveToQueue(MovePosition, facing, fVec);
        }

        public void MoveToDirectly(Vector2 MovePosition, float facing, Vector2 fVec)
        {
            this.Position = this.findAveragePosition();
            if (this.InCombat)
                this.HasPriorityOrder = true;
            this.GoalStack.Clear();
            this.MoveDirectlyNow(MovePosition, facing, fVec);
        }

        public void FormationWarpTo(Vector2 MovePosition, float facing, Vector2 fvec)
        {
            this.GoalStack.Clear();
            this.Position = MovePosition;
            this.facing = facing;
            this.AssembleFleet(facing, fvec);
            foreach (Ship ship in (List<Ship>)this.Ships)
            {
                if (ship.fleet != null)
                {
                    ship.GetAI().SetPriorityOrder();
                    ship.GetAI().OrderFormationWarp(MovePosition + ship.FleetOffset, facing, fvec);
                }
            }
        }

        public void FormationWarpToQ(Vector2 MovePosition, float facing, Vector2 fvec)
        {
            this.GoalStack.Clear();
            this.Position = MovePosition;
            this.facing = facing;
            this.AssembleFleet(facing, fvec);
            foreach (Ship ship in (List<Ship>)this.Ships)
            {
                ship.GetAI().SetPriorityOrder();
                ship.GetAI().OrderFormationWarpQ(MovePosition + ship.FleetOffset, facing, fvec);
            }
        }

        public void AttackMoveTo(Vector2 MovePosition)
        {
            this.GoalStack.Clear();
            Vector2 fVec = Vector2.Normalize(HelperFunctions.FindVectorToTarget(this.findAveragePosition(), MovePosition));
            this.Position = this.findAveragePosition() + fVec * 3500f;
            this.GoalStack.Push(new Fleet.FleetGoal(this, MovePosition, MathHelper.ToRadians(HelperFunctions.findAngleToTarget(this.findAveragePosition(), MovePosition)), fVec, Fleet.FleetGoalType.AttackMoveTo));
        }

        public void MoveToNow(Vector2 MovePosition, float facing, Vector2 fVec)
        {
            this.Position = MovePosition;
            this.facing = facing;
            this.AssembleFleet(facing, fVec);
            foreach (Ship ship in (List<Ship>)this.Ships)
            {
                ship.GetAI().SetPriorityOrder();
                ship.GetAI().OrderMoveTowardsPosition(MovePosition + ship.FleetOffset, facing, fVec, true);
            }
        }

        private void MoveToQueue(Vector2 MovePosition, float facing, Vector2 fVec)
        {
            this.Position = MovePosition;
            this.facing = facing;
            this.AssembleFleet(facing, fVec);
            foreach (Ship ship in (List<Ship>)this.Ships)
            {
                ship.GetAI().SetPriorityOrder();
                ship.GetAI().OrderMoveTowardsPosition(MovePosition + ship.FleetOffset, facing, fVec, false);
            }
        }

        private void MoveDirectlyNow(Vector2 MovePosition, float facing, Vector2 fVec)
        {
            this.Position = MovePosition;
            this.facing = facing;
            this.AssembleFleet(facing, fVec);
            foreach (Ship ship in (List<Ship>)this.Ships)
            {
                ship.GetAI().SetPriorityOrder();
                ship.GetAI().OrderMoveDirectlyTowardsPosition(MovePosition + ship.FleetOffset, facing, fVec, true);
            }
        }

        private void AutoAssembleFleet(float facing, Vector2 facingVec)
        {
            foreach (List<Fleet.Squad> list in this.AllFlanks)
            {
                foreach (Fleet.Squad squad in list)
                {
                    for (int index = 0; index < squad.Ships.Count; ++index)
                    {
                        float angle1 = MathHelper.ToRadians(Math.Abs(HelperFunctions.findAngleToTarget(Vector2.Zero, squad.Offset)) + MathHelper.ToDegrees(facing));
                        float distance = squad.Offset.Length();
                        //Vector2 vector2_1 = new Vector2();
                        Vector2 distanceUsingRadians1 = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(Vector2.Zero, angle1, distance);
                        Vector2 vector2_2;
                        switch (index)
                        {
                            case 0:
                                vector2_2 = new Vector2();
                                float angle2 = MathHelper.ToRadians(HelperFunctions.findAngleToTarget(Vector2.Zero, new Vector2(0.0f, -500f)) + MathHelper.ToDegrees(facing));
                                vector2_2 = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(this.Position + distanceUsingRadians1, angle2, 500f);
                                squad.Ships[index].FleetOffset = distanceUsingRadians1 + HelperFunctions.findPointFromAngleAndDistanceUsingRadians(Vector2.Zero, angle2, 500f);
                                Vector2 distanceUsingRadians2 = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(Vector2.Zero, MathHelper.ToRadians(HelperFunctions.findAngleToTarget(Vector2.Zero, new Vector2(0.0f, -500f))), 500f);
                                squad.Ships[index].RelativeFleetOffset = squad.Offset + distanceUsingRadians2;
                                break;
                            case 1:
                                vector2_2 = new Vector2();
                                float angle3 = MathHelper.ToRadians(HelperFunctions.findAngleToTarget(Vector2.Zero, new Vector2(-500f, 0.0f)) + MathHelper.ToDegrees(facing));
                                vector2_2 = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(this.Position + distanceUsingRadians1, angle3, 500f);
                                squad.Ships[index].FleetOffset = distanceUsingRadians1 + HelperFunctions.findPointFromAngleAndDistanceUsingRadians(Vector2.Zero, angle3, 500f);
                                Vector2 distanceUsingRadians3 = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(Vector2.Zero, MathHelper.ToRadians(HelperFunctions.findAngleToTarget(Vector2.Zero, new Vector2(-500f, 0.0f))), 500f);
                                squad.Ships[index].RelativeFleetOffset = squad.Offset + distanceUsingRadians3;
                                break;
                            case 2:
                                vector2_2 = new Vector2();
                                float angle4 = MathHelper.ToRadians(HelperFunctions.findAngleToTarget(Vector2.Zero, new Vector2(500f, 0.0f)) + MathHelper.ToDegrees(facing));
                                vector2_2 = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(this.Position + distanceUsingRadians1, angle4, 500f);
                                squad.Ships[index].FleetOffset = distanceUsingRadians1 + HelperFunctions.findPointFromAngleAndDistanceUsingRadians(Vector2.Zero, angle4, 500f);
                                Vector2 distanceUsingRadians4 = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(Vector2.Zero, MathHelper.ToRadians(HelperFunctions.findAngleToTarget(Vector2.Zero, new Vector2(500f, 0.0f))), 500f);
                                squad.Ships[index].RelativeFleetOffset = squad.Offset + distanceUsingRadians4;
                                break;
                            case 3:
                                vector2_2 = new Vector2();
                                float angle5 = MathHelper.ToRadians(HelperFunctions.findAngleToTarget(Vector2.Zero, new Vector2(0.0f, 500f)) + MathHelper.ToDegrees(facing));
                                vector2_2 = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(this.Position + distanceUsingRadians1, angle5, 500f);
                                squad.Ships[index].FleetOffset = distanceUsingRadians1 + HelperFunctions.findPointFromAngleAndDistanceUsingRadians(Vector2.Zero, angle5, 500f);
                                Vector2 distanceUsingRadians5 = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(Vector2.Zero, MathHelper.ToRadians(HelperFunctions.findAngleToTarget(Vector2.Zero, new Vector2(0.0f, 500f))), 500f);
                                squad.Ships[index].RelativeFleetOffset = squad.Offset + distanceUsingRadians5;
                                break;
                        }
                    }
                }
            }
        }

        public void AssignPositions(float facing)
        {
            this.facing = facing;
            //foreach (Ship ship in (List<Ship>)this.Ships)
            for(int i=0;i<((List<Ship>)this.Ships).Count;i++)
            {
                float angle = MathHelper.ToRadians(Math.Abs(HelperFunctions.findAngleToTarget(Vector2.Zero, ((List<Ship>)this.Ships)[i].RelativeFleetOffset)) + MathHelper.ToDegrees(facing));
                float distance = ((List<Ship>)this.Ships)[i].RelativeFleetOffset.Length();
                ((List<Ship>)this.Ships)[i].FleetOffset = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(Vector2.Zero, angle, distance);
            }
        }

        public void AssignDataPositions(float facing)
        {
            this.facing = facing;
            foreach (FleetDataNode fleetDataNode in (List<FleetDataNode>)this.DataNodes)
            {
                float angle = MathHelper.ToRadians(Math.Abs(HelperFunctions.findAngleToTarget(Vector2.Zero, fleetDataNode.FleetOffset)) + MathHelper.ToDegrees(facing));
                float distance = fleetDataNode.FleetOffset.Length();
                fleetDataNode.OrdersOffset = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(Vector2.Zero, angle, distance);
            }
        }

        public void AssembleFleet(float facing, Vector2 facingVec)
        {
            this.facing = facing;
            foreach (Ship ship in (List<Ship>)this.Ships)
            {
                float angle = MathHelper.ToRadians(Math.Abs(HelperFunctions.findAngleToTarget(Vector2.Zero, ship.RelativeFleetOffset)) + MathHelper.ToDegrees(facing));
                float distance = ship.RelativeFleetOffset.Length();
                ship.FleetOffset = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(Vector2.Zero, angle, distance);
            }
        }

        public override void ProjectPos(Vector2 ProjectedPosition, float facing, List<Fleet.Squad> Flank)
        {
            this.ProjectedFacing = facing;
            Vector2 vector2_1 = new Vector2();
            foreach (Fleet.Squad squad in Flank)
                vector2_1.X += squad.Offset.X;
            vector2_1.X = vector2_1.X / (float)Flank.Count;
            int num = 0;
            foreach (Fleet.Squad squad in Flank)
            {
                Vector2 target = new Vector2();
                target = new Vector2(squad.Offset.X - vector2_1.X, 0.0f);
                ++num;
                for (int index = 0; index < squad.Ships.Count; ++index)
                {
                    Vector2 vector2_2 = new Vector2();
                    float angle1 = MathHelper.ToRadians(HelperFunctions.findAngleToTarget(Vector2.Zero, target) + MathHelper.ToDegrees(facing));
                    float distance = target.Length();
                    //Vector2 vector2_3 = new Vector2();
                    Vector2 distanceUsingRadians = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(Vector2.Zero, angle1, distance);
                   // Vector2 vector2_4;
                    switch (index)
                    {
                        case 0:
                           // vector2_4 = new Vector2();
                            float angle2 = MathHelper.ToRadians(HelperFunctions.findAngleToTarget(Vector2.Zero, new Vector2(0.0f, -500f)) + MathHelper.ToDegrees(facing));
                            vector2_2 = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(ProjectedPosition + distanceUsingRadians, angle2, 500f);
                            break;
                        case 1:
                          //  vector2_4 = new Vector2();
                            float angle3 = MathHelper.ToRadians(HelperFunctions.findAngleToTarget(Vector2.Zero, new Vector2(-500f, 0.0f)) + MathHelper.ToDegrees(facing));
                            vector2_2 = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(ProjectedPosition + distanceUsingRadians, angle3, 500f);
                            break;
                        case 2:
                           // vector2_4 = new Vector2();
                            float angle4 = MathHelper.ToRadians(HelperFunctions.findAngleToTarget(Vector2.Zero, new Vector2(500f, 0.0f)) + MathHelper.ToDegrees(facing));
                            vector2_2 = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(ProjectedPosition + distanceUsingRadians, angle4, 500f);
                            break;
                        case 3:
                           // vector2_4 = new Vector2();
                            float angle5 = MathHelper.ToRadians(HelperFunctions.findAngleToTarget(Vector2.Zero, new Vector2(0.0f, 500f)) + MathHelper.ToDegrees(facing));
                            vector2_2 = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(ProjectedPosition + distanceUsingRadians, angle5, 500f);
                            break;
                    }
                    squad.Ships[index].projectedPosition = vector2_2;
                }
            }
        }

        public void MoveFlankToProjectedPos(Vector2 ProjectedPosition, float facing, List<Fleet.Squad> Flank, Vector2 fVec)
        {
            this.ProjectedFacing = facing;
            foreach (Fleet.Squad squad in Flank)
            {
                for (int index = 0; index < squad.Ships.Count; ++index)
                {
                    lock (GlobalStats.WayPointLock)
                        squad.Ships[index].GetAI().OrderThrustTowardsPosition(squad.Ships[index].projectedPosition, facing, fVec, true);
                }
            }
        }

        public void ProtectedMove()
        {
        }

        public override void ProjectPos(Vector2 ProjectedPosition, float facing, Vector2 fVec)
        {
            this.ProjectedFacing = facing;
            foreach (Ship ship in (List<Ship>)this.Ships)
            {
                float angle = MathHelper.ToRadians(Math.Abs(HelperFunctions.findAngleToTarget(Vector2.Zero, ship.RelativeFleetOffset)) + MathHelper.ToDegrees(facing));
                float distance = ship.RelativeFleetOffset.Length();
                ship.projectedPosition = ProjectedPosition + HelperFunctions.findPointFromAngleAndDistanceUsingRadians(Vector2.Zero, angle, distance);
            }
        }

        public Vector2 findAveragePositionORIG()
        {
            Vector2 zero = Vector2.Zero;
            foreach (Ship ship in (List<Ship>)this.Ships)
                zero += ship.Position;
            return zero / (float)this.Ships.Count;
        }
        //added by gremlin. make fleet center not count warpless ships.
        public Vector2 findAveragePosition()
        {
            Vector2 pos = Vector2.Zero;
            float shipcount = 0;
            foreach (Ship ship in this.Ships)
            //Parallel.ForEach(this.Ships, ship =>
            {
                if (!ship.EnginesKnockedOut && ship.IsWarpCapable&&ship.Active && (!ship.Inhibited ||ship.Inhibited && Vector2.Distance(this.Position,ship.Position)<300000)  )
                {
                    pos = pos + ship.Position;
                    shipcount++;
                }
            }
            //if (pos == Vector2.Zero && this.Ships.Count>0) 
            //    pos = this.Ships[0].Position;
            //float count = (float)this.Ships.Where(ship => !ship.EnginesKnockedOut && ship.IsWarpCapable && !ship.Inhibited && ship.Active).Count();
            if (shipcount > 0)
                return pos / shipcount;
            else if (this.Ships.Count >0)
                return this.Ships[0].Position;
            else
                return Vector2.Zero;
        }

        public void TrackEnemies()
        {
            Fleet.quadrantscan quadrantscan1 = new Fleet.quadrantscan();
            Fleet.quadrantscan quadrantscan2 = new Fleet.quadrantscan();
            Fleet.quadrantscan quadrantscan3 = new Fleet.quadrantscan();
            Fleet.quadrantscan quadrantscan4 = new Fleet.quadrantscan();
            quadrantscan1.Strength = this.Owner.GetGSAI().ThreatMatrix.PingRadarStr(this.Position + new Vector2(25000f, -25000f), 25000f, this.Owner);
            quadrantscan1.avgPos = this.Owner.GetGSAI().ThreatMatrix.PingRadarAvgPos(this.Position + new Vector2(25000f, -25000f), 25000f, this.Owner);
            quadrantscan2.Strength = this.Owner.GetGSAI().ThreatMatrix.PingRadarStr(this.Position + new Vector2(25000f, 25000f), 25000f, this.Owner);
            quadrantscan2.avgPos = this.Owner.GetGSAI().ThreatMatrix.PingRadarAvgPos(this.Position + new Vector2(25000f, 25000f), 25000f, this.Owner);
            quadrantscan3.Strength = this.Owner.GetGSAI().ThreatMatrix.PingRadarStr(this.Position + new Vector2(-25000f, -25000f), 25000f, this.Owner);
            quadrantscan3.avgPos = this.Owner.GetGSAI().ThreatMatrix.PingRadarAvgPos(this.Position + new Vector2(-25000f, -25000f), 25000f, this.Owner);
            quadrantscan4.Strength = this.Owner.GetGSAI().ThreatMatrix.PingRadarStr(this.Position + new Vector2(-25000f, 25000f), 25000f, this.Owner);
            quadrantscan4.avgPos = this.Owner.GetGSAI().ThreatMatrix.PingRadarAvgPos(this.Position + new Vector2(-25000f, 25000f), 25000f, this.Owner);
            IOrderedEnumerable<Fleet.quadrantscan> orderedEnumerable = Enumerable.OrderByDescending<Fleet.quadrantscan, float>((IEnumerable<Fleet.quadrantscan>)new List<Fleet.quadrantscan>()
      {
        quadrantscan1,
        quadrantscan2,
        quadrantscan3,
        quadrantscan4
      }, (Func<Fleet.quadrantscan, float>)(q => q.Strength));
            if (Enumerable.Count<Fleet.quadrantscan>((IEnumerable<Fleet.quadrantscan>)orderedEnumerable) <= 0)
                return;
            Fleet.quadrantscan quadrantscan5 = Enumerable.ElementAt<Fleet.quadrantscan>((IEnumerable<Fleet.quadrantscan>)orderedEnumerable, 0);
            if ((double)quadrantscan5.Strength <= 0.0)
                return;
            if ((double)Vector2.Distance(quadrantscan5.avgPos, this.Position) > 1500.0)
            {
                float facing = Math.Abs(MathHelper.ToRadians(HelperFunctions.findAngleToTarget(this.Position, quadrantscan5.avgPos)));
                Vector2 fVec = Vector2.Normalize(quadrantscan5.avgPos - this.Position);
                quadrantscan5.avgPos -= fVec * 5000f;
                bool flag = true;
                foreach (Ship ship in (List<Ship>)this.Ships)
                {
                    if ((double)Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 2500.0)
                    {
                        flag = false;
                        break;
                    }
                }
                if (!this.HasPriorityOrder && flag)
                    this.AttackMoveTo(quadrantscan5.avgPos);
                else
                    this.MoveToNow(quadrantscan5.avgPos, facing, fVec);
                this.InCombat = true;
            }
            if (!this.InCombat || (double)Vector2.Distance(quadrantscan5.avgPos, this.Position) <= 7500.0)
                return;
            this.InCombat = false;
        }

        public void Reset()
        {
            foreach (Ship ship in (List<Ship>)this.Ships)
                ship.fleet = (Fleet)null;
            this.Ships.Clear();
            this.TaskStep = 0;
            this.Task = (MilitaryTask)null;
            this.GoalStack.Clear();
        }

        private void EvaluateTask(float elapsedTime)
        {
            if (this.Ships.Count == 0)
                this.Task.EndTask();
            if (this.Task == null)
                return;
            switch (this.Task.type)
            {
                case MilitaryTask.TaskType.ClearAreaOfEnemies:
                    this.DoClearAreaOfEnemies(this.Task);
                    break;
                case MilitaryTask.TaskType.AssaultPlanet:
                    this.DoAssaultPlanet(this.Task);
                    break;
                case MilitaryTask.TaskType.CorsairRaid:
                    if (this.TaskStep != 0)
                        break;
                    this.Task.TaskTimer -= elapsedTime;
                    if ((double)this.Task.TaskTimer <= 0.0)
                    {
                        Ship ship1 = new Ship();
                        foreach (Ship ship2 in (List<Ship>)this.Owner.GetShips())
                        {
                            if (ship2.Name == "Corsair Asteroid Base")
                            {
                                ship1 = ship2;
                                break;
                            }
                        }
                        if (ship1 != null)
                        {
                            this.AssembleFleet(0.0f, Vector2.One);
                            this.FormationWarpTo(ship1.Position, 0.0f, Vector2.One);
                            this.Task.EndTaskWithMove();
                        }
                        else
                            this.Task.EndTask();
                    }
                    if (this.Ships.Count != 0)
                        break;
                    this.Task.EndTask();
                    break;
                case MilitaryTask.TaskType.CohesiveClearAreaOfEnemies:
                    this.DoCohesiveClearAreaOfEnemies(this.Task);
                    break;
                case MilitaryTask.TaskType.Exploration:
                    this.DoExplorePlanet(this.Task);
                    break;
                case MilitaryTask.TaskType.DefendSystem:
                    this.DoDefendSystem(this.Task);
                    break;
                case MilitaryTask.TaskType.DefendClaim:
                    this.DoClaimDefense(this.Task);
                    break;
                case MilitaryTask.TaskType.DefendPostInvasion:
                    this.DoPostInvasionDefense(this.Task);
                    break;
                case MilitaryTask.TaskType.GlassPlanet:
                    this.DoGlassPlanet(this.Task);
                    break;
            }
        }

        private void DoExplorePlanet(MilitaryTask Task)
        {
            bool flag1 = true;
            foreach (Building building in Task.GetTargetPlanet().BuildingList)
            {
                if (building.EventTriggerUID != "")
                {
                    flag1 = false;
                    break;
                }
            }
            bool flag2 = false;
            foreach (Ship ship in (List<Ship>)this.Ships)
            {
                if (ship.TroopList.Count > 0)
                    flag2 = true;
            }
            foreach (PlanetGridSquare planetGridSquare in Task.GetTargetPlanet().TilesList)
            {
                if (planetGridSquare.TroopsHere.Count > 0 && planetGridSquare.TroopsHere[0].GetOwner() == this.Owner)
                {
                    flag2 = true;
                    break;
                }
            }
            if (flag1 || !flag2 || Task.GetTargetPlanet().Owner != null)
            {
                Task.EndTask();
            }
            else
            {
                switch (this.TaskStep)
                {
                    case 0:
                        List<Planet> list1 = new List<Planet>();
                        foreach (Planet planet in this.Owner.GetPlanets())
                        {
                            if (planet.HasShipyard)
                                list1.Add(planet);
                        }
                        IOrderedEnumerable<Planet> orderedEnumerable1 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)list1, (Func<Planet, float>)(planet => Vector2.Distance(Task.AO, planet.Position)));
                        if (Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable1) <= 0)
                            break;
                        Vector2 fVec = Vector2.Normalize(Task.AO - Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable1).Position);
                        Vector2 vector2 = Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable1).Position;
                        this.MoveToNow(vector2, Math.Abs(MathHelper.ToRadians(HelperFunctions.findAngleToTarget(vector2, Task.AO))), fVec);
                        foreach (Ship ship in (List<Ship>)this.Ships)
                            ship.GetAI().HasPriorityOrder = true;
                        this.TaskStep = 1;
                        break;
                    case 1:
                        bool flag3 = true;
                        bool flag4 = false;
                        foreach (Ship ship in (List<Ship>)this.Ships)
                        {
                            if (!ship.disabled && ship.Active)
                            {
                                if ((double)Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 5000.0)
                                    flag3 = false;
                                if (ship.InCombat)
                                    flag4 = true;
                                if (!flag3)
                                    break;
                            }
                        }
                        if (!flag3 && !flag4)
                            break;
                        this.TaskStep = 2;
                        Vector2 MovePosition = Task.GetTargetPlanet().Position + Vector2.Normalize(this.findAveragePosition() - Task.GetTargetPlanet().Position) * 50000f;
                        this.Position = MovePosition;
                        this.FormationWarpTo(MovePosition, MathHelper.ToRadians(HelperFunctions.findAngleToTarget(this.findAveragePosition(), Task.AO)), Vector2.Normalize(Task.AO - this.findAveragePosition()));
                        break;
                    case 2:
                        bool flag5 = true;
                        foreach (Ship ship in (List<Ship>)this.Ships)
                        {
                            ship.GetAI().HasPriorityOrder = false;
                            if (!ship.disabled && ship.Active)
                            {
                                if ((double)Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 5000.0)
                                    flag5 = false;
                                if (!flag5)
                                    break;
                            }
                        }
                        if (!flag5)
                            break;
                        foreach (Ship ship in (List<Ship>)this.Ships)
                        {
                            ship.GetAI().State = AIState.HoldPosition;
                            if (ship.Role == "troop")
                                ship.GetAI().HoldPosition();
                        }
                        this.InterceptorDict.Clear();
                        this.TaskStep = 3;
                        break;
                    case 3:
                        this.EnemyClumpsDict.Clear();
                        List<Ship> list2 = new List<Ship>();
                        List<GameplayObject> nearby1 = UniverseScreen.ShipSpatialManager.GetNearby(this.Position);
                        for (int index1 = 0; index1 < nearby1.Count; ++index1)
                        {
                            Ship ship1 = nearby1[index1] as Ship;
                            if (ship1 != null)
                            {
                                ship1.GetAI().HasPriorityOrder = false;
                                if (ship1.loyalty != this.Owner && (ship1.loyalty.isFaction || this.Owner.GetRelations()[ship1.loyalty].AtWar) && (!list2.Contains(ship1) && (double)Vector2.Distance(ship1.Center, Task.AO) < (double)Task.AORadius && !this.EnemyClumpsDict.ContainsKey(ship1.Center)))
                                {
                                    this.EnemyClumpsDict.Add(ship1.Center, new List<Ship>());
                                    this.EnemyClumpsDict[ship1.Center].Add(ship1);
                                    list2.Add(ship1);
                                    List<GameplayObject> nearby2 = UniverseScreen.ShipSpatialManager.GetNearby(this.Position);
                                    for (int index2 = 0; index2 < nearby2.Count; ++index2)
                                    {
                                        Ship ship2 = nearby2[index2] as Ship;
                                        if (ship2 != null && ship2.loyalty != this.Owner && (ship2.loyalty == ship1.loyalty && (double)Vector2.Distance(ship1.Center, ship2.Center) < 10000.0) && !list2.Contains(ship2))
                                            this.EnemyClumpsDict[ship1.Center].Add(ship2);
                                    }
                                }
                            }
                        }
                        if (this.EnemyClumpsDict.Count == 0)
                        {
                            this.TaskStep = 5;
                            break;
                        }
                        else
                        {
                            List<Vector2> list3 = new List<Vector2>();
                            foreach (KeyValuePair<Vector2, List<Ship>> keyValuePair in this.EnemyClumpsDict)
                                list3.Add(keyValuePair.Key);
                            IOrderedEnumerable<Vector2> orderedEnumerable2 = Enumerable.OrderBy<Vector2, float>((IEnumerable<Vector2>)list3, (Func<Vector2, float>)(clumpPos => Vector2.Distance(this.findAveragePosition(), clumpPos)));
                            List<Ship> list4 = new List<Ship>();
                            foreach (Ship toAttack in this.EnemyClumpsDict[Enumerable.First<Vector2>((IEnumerable<Vector2>)orderedEnumerable2)])
                            {
                                float num = 0.0f;
                                foreach (Ship ship in (List<Ship>)this.Ships)
                                {
                                    if (!list4.Contains(ship) && ((double)num == 0.0 || (double)num < (double)toAttack.GetStrength()))
                                    {
                                        ship.GetAI().Intercepting = true;
                                        ship.GetAI().OrderAttackSpecificTarget(toAttack);
                                        list4.Add(ship);
                                        num += ship.GetStrength();
                                    }
                                }
                            }
                            List<Ship> list5 = new List<Ship>();
                            foreach (Ship ship in (List<Ship>)this.Ships)
                            {
                                if (!list4.Contains(ship))
                                    list5.Add(ship);
                            }
                            foreach (Ship ship in list5)
                                ship.GetAI().OrderAttackSpecificTarget(list4[0].GetAI().Target as Ship);
                            this.TaskStep = 4;
                            if (this.InterceptorDict.Count != 0)
                                break;
                            this.TaskStep = 4;
                            break;
                        }
                    case 4:
                        float num1 = 0.0f;
                        float num2 = 0.0f;
                        float num3 = 0.0f;
                        float num4 = 0.0f;
                        foreach (Ship ship in (List<Ship>)this.Ships)
                        {
                            num1 += ship.Ordinance;
                            num2 += ship.OrdinanceMax;
                            foreach (Weapon weapon in ship.Weapons)
                            {
                                if ((double)weapon.OrdinanceRequiredToFire > 0.0)
                                    num3 = weapon.DamageAmount / weapon.fireDelay;
                                if ((double)weapon.PowerRequiredToFire > 0.0)
                                    num4 = weapon.DamageAmount / weapon.fireDelay;
                            }
                        }
                        float num5 = num3 + num4;
                        if ((double)num3 >= 0.5 * (double)num5 && (double)num1 <= 0.100000001490116 * (double)num2)
                        {
                            this.TaskStep = 5;
                            break;
                        }
                        else
                        {
                            bool flag6 = false;
                            foreach (Ship ship in (List<Ship>)this.Ships)
                            {
                                if (!ship.InCombat)
                                {
                                    flag6 = true;
                                    break;
                                }
                            }
                            if (!flag6)
                                break;
                            this.TaskStep = 3;
                            break;
                        }
                    case 5:
                        foreach (Ship ship in (List<Ship>)this.Ships)
                        {
                            ship.GetAI().Intercepting = true;
                            ship.GetAI().OrderLandAllTroops(Task.GetTargetPlanet());
                        }
                        this.Position = Task.GetTargetPlanet().Position;
                        this.AssembleFleet(this.facing, Vector2.Normalize(this.Position - this.findAveragePosition()));
                        break;
                }
            }
        }

        private void DoAssaultPlanet(MilitaryTask Task)
        {
            if (Task.GetTargetPlanet().Owner == this.Owner || Task.GetTargetPlanet().Owner == null || Task.GetTargetPlanet().Owner != null && !this.Owner.GetRelations()[Task.GetTargetPlanet().Owner].AtWar)
            {
                if (Task.GetTargetPlanet().Owner == this.Owner)
                {
                    MilitaryTask militaryTask = new MilitaryTask();
                    militaryTask.AO = Task.GetTargetPlanet().Position;
                    militaryTask.AORadius = 50000f;
                    militaryTask.WhichFleet = Task.WhichFleet;
                    militaryTask.SetEmpire(this.Owner);
                    militaryTask.type = MilitaryTask.TaskType.DefendPostInvasion;
                    this.Owner.GetGSAI().TaskList.QueuePendingRemoval(Task);
                    this.Task = militaryTask;
                    lock (GlobalStats.TaskLocker)
                        this.Owner.GetGSAI().TaskList.Add(Task);
                }
                else
                    Task.EndTask();
            }
            else
            {
                float num1 = 0.0f;
                foreach (Ship ship in (List<Ship>)this.Ships)
                    num1 += ship.GetStrength();
                if ((double)num1 == 0.0)
                    Task.EndTask();
                int num2 = 0;
                int num3 = 0;
                foreach (Ship ship in (List<Ship>)this.Ships)
                {
                    if ((double)ship.GetStrength() > 0.0)
                        ++num3;
                    num2 += ship.TroopList.Count;
                }
                if (num2 == 0)
                {
                    foreach (Troop troop in Task.GetTargetPlanet().TroopsHere)
                    {
                        if (troop.GetOwner() == this.Owner)
                            ++num2;
                    }
                }
                if (num2 == 0 || num3 == 0)
                {
                    if (num3 == 0)
                        Task.IsCoreFleetTask = false;
                    Task.EndTask();
                    this.Task = (MilitaryTask)null;
                    this.TaskStep = 0;
                }
                else
                {
                    switch (this.TaskStep)
                    {
                        case 0:
                            List<Planet> list1 = new List<Planet>();
                            foreach (Planet planet in this.Owner.GetPlanets())
                            {
                                if (planet.HasShipyard)
                                    list1.Add(planet);
                            }
                            IOrderedEnumerable<Planet> orderedEnumerable1 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)list1, (Func<Planet, float>)(planet => Vector2.Distance(Task.AO, planet.Position)));
                            if (Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable1) > 0)
                            {
                                Vector2 fVec = Vector2.Normalize(Task.AO - Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable1).Position);
                                Vector2 vector2 = Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable1).Position;
                                this.MoveToNow(vector2, Math.Abs(MathHelper.ToRadians(HelperFunctions.findAngleToTarget(vector2, Task.AO))), fVec);
                                foreach (Ship ship in (List<Ship>)this.Ships)
                                    ship.GetAI().HasPriorityOrder = true;
                                this.TaskStep = 1;
                                break;
                            }
                            else
                            {
                                Task.EndTask();
                                break;
                            }
                        case 1:
                            bool flag1 = true;
                            foreach (Ship ship in (List<Ship>)this.Ships)
                            {
                                if (!ship.disabled && ship.Active)
                                {
                                    if ((double)Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 5000.0)
                                        flag1 = false;
                                    int num4 = ship.InCombat ? 1 : 0;
                                    if (!flag1)
                                        break;
                                }
                            }
                            if (!flag1)
                                break;
                            this.TaskStep = 2;
                            Vector2 MovePosition = Task.GetTargetPlanet().Position + Vector2.Normalize(this.findAveragePosition() - Task.GetTargetPlanet().Position) * 125000f;
                            this.Position = MovePosition;
                            this.FormationWarpTo(MovePosition, MathHelper.ToRadians(HelperFunctions.findAngleToTarget(this.findAveragePosition(), Task.AO)), Vector2.Normalize(Task.AO - this.findAveragePosition()));
                            break;
                        case 2:
                            bool flag2 = true;
                            foreach (Ship ship in (List<Ship>)this.Ships)
                            {
                                if (!ship.disabled && ship.Active)
                                {
                                    if ((double)Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 25000.0)
                                        flag2 = false;
                                    if (!flag2)
                                        break;
                                }
                            }
                            if (!flag2)
                                break;
                            foreach (Ship ship in (List<Ship>)this.Ships)
                            {
                                ship.GetAI().HasPriorityOrder = false;
                                ship.GetAI().State = AIState.HoldPosition;
                                if (ship.BombBays.Count > 0)
                                    ship.GetAI().OrderBombardPlanet(Task.GetTargetPlanet());
                                else if (ship.Role == "troop")
                                    ship.GetAI().HoldPosition();
                            }
                            this.InterceptorDict.Clear();
                            this.TaskStep = 3;
                            break;
                        case 3:
                            float num5 = 0.0f;
                            float num6 = 0.0f;
                            float num7 = 0.0f;
                            float num8 = 0.0f;
                            foreach (Ship ship in (List<Ship>)this.Ships)
                            {
                                num5 += ship.Ordinance;
                                num6 += ship.OrdinanceMax;
                                foreach (Weapon weapon in ship.Weapons)
                                {
                                    if ((double)weapon.OrdinanceRequiredToFire > 0.0)
                                        num7 = weapon.DamageAmount / weapon.fireDelay;
                                    if ((double)weapon.PowerRequiredToFire > 0.0)
                                        num8 = weapon.DamageAmount / weapon.fireDelay;
                                }
                            }
                            float num9 = num7 + num8;
                            if ((double)num7 >= 0.5 * (double)num9 && (double)num5 <= 0.100000001490116 * (double)num6)
                            {
                                this.TaskStep = 5;
                                break;
                            }
                            else
                            {
                                foreach (Ship key in (List<Ship>)Task.GetTargetPlanet().system.ShipList)
                                {
                                    if (key.loyalty != this.Owner && (key.loyalty.isFaction || this.Owner.GetRelations()[key.loyalty].AtWar) && ((double)Vector2.Distance(key.Center, Task.GetTargetPlanet().Position) < 15000.0 && !this.InterceptorDict.ContainsKey(key)))
                                        this.InterceptorDict.Add(key, new List<Ship>());
                                }
                                List<Ship> list2 = new List<Ship>();
                                foreach (KeyValuePair<Ship, List<Ship>> keyValuePair in this.InterceptorDict)
                                {
                                    List<Ship> list3 = new List<Ship>();
                                    if ((double)Vector2.Distance(keyValuePair.Key.Center, Task.GetTargetPlanet().Position) > 20000.0 || !keyValuePair.Key.Active)
                                    {
                                        list2.Add(keyValuePair.Key);
                                        foreach (Ship ship in keyValuePair.Value)
                                        {
                                            lock (ship)
                                            {
                                                ship.GetAI().OrderQueue.Clear();
                                                ship.GetAI().Intercepting = false;
                                                ship.GetAI().OrderOrbitPlanet(Task.GetTargetPlanet());
                                                ship.GetAI().State = AIState.AwaitingOrders;
                                                ship.GetAI().Intercepting = false;
                                            }
                                        }
                                    }
                                    foreach (Ship ship in keyValuePair.Value)
                                    {
                                        if (!ship.Active)
                                            list3.Add(ship);
                                    }
                                    foreach (Ship ship in list3)
                                        keyValuePair.Value.Remove(ship);
                                }
                                foreach (Ship key in list2)
                                    this.InterceptorDict.Remove(key);
                                foreach (KeyValuePair<Ship, List<Ship>> keyValuePair1 in this.InterceptorDict)
                                {
                                    List<Ship> list3 = new List<Ship>();
                                    foreach (Ship ship in (List<Ship>)this.Ships)
                                    {
                                        if (ship.Role != "troop")
                                            list3.Add(ship);
                                    }
                                    List<Ship> list4 = new List<Ship>();
                                    foreach (KeyValuePair<Ship, List<Ship>> keyValuePair2 in this.InterceptorDict)
                                    {
                                        list4.Add(keyValuePair2.Key);
                                        foreach (Ship ship in keyValuePair2.Value)
                                            list3.Remove(ship);
                                    }
                                    foreach (Ship toAttack in (IEnumerable<Ship>)Enumerable.OrderByDescending<Ship, float>((IEnumerable<Ship>)list4, (Func<Ship, float>)(ship => ship.GetStrength())))
                                    {
                                        IOrderedEnumerable<Ship> orderedEnumerable2 = Enumerable.OrderByDescending<Ship, float>((IEnumerable<Ship>)list3, (Func<Ship, float>)(ship => ship.GetStrength()));
                                        float num4 = 0.0f;
                                        foreach (Ship ship in (IEnumerable<Ship>)orderedEnumerable2)
                                        {
                                            if ((double)num4 != 0.0)
                                            {
                                                if ((double)num4 >= (double)toAttack.GetStrength() * 1.5)
                                                    break;
                                            }
                                            ship.GetAI().Intercepting = true;
                                            list3.Remove(ship);
                                            ship.GetAI().OrderAttackSpecificTarget(toAttack);
                                            this.InterceptorDict[toAttack].Add(ship);
                                            num4 += ship.GetStrength();
                                        }
                                    }
                                }
                                if (this.InterceptorDict.Count == 0 || (double)this.Owner.GetGSAI().ThreatMatrix.PingRadarStr(Task.GetTargetPlanet().Position, 25000f, this.Owner) < 500.0)
                                    this.TaskStep = 4;
                                lock (GlobalStats.TaskLocker)
                                {
                                    using (List<MilitaryTask>.Enumerator resource_0 = this.Owner.GetGSAI().TaskList.GetEnumerator())
                                    {
                                        while (resource_0.MoveNext())
                                        {
                                            MilitaryTask local_43 = resource_0.Current;
                                            if (local_43.WaitForCommand && local_43.GetTargetPlanet() != null && local_43.GetTargetPlanet() == Task.GetTargetPlanet())
                                                local_43.WaitForCommand = false;
                                        }
                                        break;
                                    }
                                }
                            }
                        case 4:
                            int num10 = 0;
                            foreach (Ship ship in (List<Ship>)this.Ships)
                            {
                                if (ship.BombBays.Count > 0 && (double)ship.Ordinance / (double)ship.OrdinanceMax > 0.200000002980232)
                                {
                                    num10 += ship.BombBays.Count;
                                    ship.GetAI().OrderBombardPlanet(Task.GetTargetPlanet());
                                }
                            }
                            float num11 = 0.0f;
                            float num12 = 0.0f;
                            float num13 = 0.0f;
                            float num14 = 0.0f;
                            foreach (Ship ship in (List<Ship>)this.Ships)
                            {
                                num11 += ship.Ordinance;
                                num12 += ship.OrdinanceMax;
                                foreach (Weapon weapon in ship.Weapons)
                                {
                                    if ((double)weapon.OrdinanceRequiredToFire > 0.0)
                                        num13 = weapon.DamageAmount / weapon.fireDelay;
                                    if ((double)weapon.PowerRequiredToFire > 0.0)
                                        num14 = weapon.DamageAmount / weapon.fireDelay;
                                }
                            }
                            float num15 = num13 + num14;
                            if ((double)num13 >= 0.5 * (double)num15 && (double)num11 <= 0.100000001490116 * (double)num12)
                            {
                                this.TaskStep = 5;
                                break;
                            }
                            else
                            {
                                bool flag3 = true;
                                float groundStrOfPlanet = this.GetGroundStrOfPlanet(Task.GetTargetPlanet());
                                float num4 = 0.0f;
                                foreach (Ship ship in (List<Ship>)this.Ships)
                                {
                                    foreach (Troop troop in ship.TroopList)
                                        num4 += (float)troop.Strength;
                                }
                                if ((double)num4 > (double)groundStrOfPlanet || num10 < 6)
                                {
                                    flag3 = true;
                                }
                                else
                                {
                                    int num16 = 0;
                                    foreach (Ship ship in (List<Ship>)this.Ships)
                                    {
                                        if (ship.BombBays.Count > 0)
                                        {
                                            ++num16;
                                            if ((double)ship.Ordinance / (double)ship.OrdinanceMax > 0.200000002980232)
                                            {
                                                flag3 = false;
                                                break;
                                            }
                                        }
                                    }
                                    if (num16 == 0)
                                        flag3 = true;
                                }
                                if (flag3)
                                {
                                    foreach (Ship ship in (List<Ship>)this.Ships)
                                    {
                                        if (ship.BombBays.Count > 0)
                                        {
                                            ship.GetAI().State = AIState.AwaitingOrders;
                                            ship.GetAI().OrderQueue.Clear();
                                        }
                                    }
                                    this.Position = Task.GetTargetPlanet().Position;
                                    this.AssembleFleet(this.facing, Vector2.Normalize(this.Position - this.findAveragePosition()));
                                    using (List<Ship>.Enumerator enumerator = this.Ships.GetEnumerator())
                                    {
                                        while (enumerator.MoveNext())
                                            enumerator.Current.GetAI().OrderLandAllTroops(Task.GetTargetPlanet());
                                        break;
                                    }
                                }
                                else
                                {
                                    using (List<Ship>.Enumerator enumerator = this.Ships.GetEnumerator())
                                    {
                                        while (enumerator.MoveNext())
                                        {
                                            Ship current = enumerator.Current;
                                            if (current.BombBays.Count > 0)
                                                current.GetAI().OrderBombardPlanet(Task.GetTargetPlanet());
                                        }
                                        break;
                                    }
                                }
                            }
                        case 5:
                            List<Planet> list5 = new List<Planet>();
                            foreach (Planet planet in this.Owner.GetPlanets())
                            {
                                if (planet.HasShipyard)
                                    list5.Add(planet);
                            }
                            IOrderedEnumerable<Planet> orderedEnumerable3 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)list5, (Func<Planet, float>)(p => Vector2.Distance(this.Position, p.Position)));
                            if (Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable3) > 0)
                            {
                                this.Position = Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable3).Position;
                                foreach (Ship ship in (List<Ship>)this.Ships)
                                    ship.GetAI().OrderResupply(Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable3), true);
                                this.TaskStep = 6;
                                break;
                            }
                            else
                            {
                                Task.EndTask();
                                break;
                            }
                        case 6:
                            float num17 = 0.0f;
                            float num18 = 0.0f;
                            foreach (Ship ship in (List<Ship>)this.Ships)
                            {
                                ship.GetAI().HasPriorityOrder = true;
                                num17 += ship.Ordinance;
                                num18 += ship.OrdinanceMax;
                            }
                            if ((double)num17 < (double)num18 * 0.899999976158142)
                                break;
                            this.TaskStep = 0;
                            break;
                    }
                }
            }
        }

        private float GetGroundStrOfPlanet(Planet p)
        {
            float num = 0.0f;
            foreach (PlanetGridSquare planetGridSquare in p.TilesList)
            {
                if (planetGridSquare.TroopsHere.Count > 0)
                    num += (float)planetGridSquare.TroopsHere[0].Strength;
                else if (planetGridSquare.building != null && planetGridSquare.building.CombatStrength > 0)
                    num += (float)planetGridSquare.building.CombatStrength;
            }
            return num;
        }

        private void DoPostInvasionDefense(MilitaryTask Task)
        {
            --this.defenseTurns;
            if (this.defenseTurns <= 0)
            {
                Task.EndTask();
            }
            else
            {
                switch (this.TaskStep)
                {
                    case -1:
                        bool flag1 = true;
                        foreach (Ship ship in (List<Ship>)this.Ships)
                        {
                            if (!ship.disabled && ship.Active)
                            {
                                if ((double)Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 5000.0)
                                    flag1 = false;
                                int num = ship.InCombat ? 1 : 0;
                                if (!flag1)
                                    break;
                            }
                        }
                        if (!flag1)
                            break;
                        this.TaskStep = 2;
                        this.FormationWarpTo(Task.AO, MathHelper.ToRadians(HelperFunctions.findAngleToTarget(this.findAveragePosition(), Task.AO)), Vector2.Normalize(Task.AO - this.findAveragePosition()));
                        using (List<Ship>.Enumerator enumerator = this.Ships.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                                enumerator.Current.GetAI().HasPriorityOrder = true;
                            break;
                        }
                    case 0:
                        List<Planet> list1 = new List<Planet>();
                        foreach (Planet planet in this.Owner.GetPlanets())
                        {
                            if (planet.HasShipyard)
                                list1.Add(planet);
                        }
                        IOrderedEnumerable<Planet> orderedEnumerable1 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)list1, (Func<Planet, float>)(planet => Vector2.Distance(Task.AO, planet.Position)));
                        if (Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable1) <= 0)
                            break;
                        Vector2 fVec = Vector2.Normalize(Task.AO - Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable1).Position);
                        Vector2 vector2 = Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable1).Position;
                        this.MoveToNow(vector2, Math.Abs(MathHelper.ToRadians(HelperFunctions.findAngleToTarget(vector2, Task.AO))), fVec);
                        this.TaskStep = 1;
                        break;
                    case 1:
                        bool flag2 = true;
                        foreach (Ship ship in (List<Ship>)this.Ships)
                        {
                            if (!ship.disabled && ship.Active)
                            {
                                if ((double)Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 5000.0)
                                    flag2 = false;
                                int num = ship.InCombat ? 1 : 0;
                                if (!flag2)
                                    break;
                            }
                        }
                        if (!flag2)
                            break;
                        this.TaskStep = 2;
                        this.FormationWarpTo(Task.AO, MathHelper.ToRadians(HelperFunctions.findAngleToTarget(this.findAveragePosition(), Task.AO)), Vector2.Normalize(Task.AO - this.findAveragePosition()));
                        using (List<Ship>.Enumerator enumerator = this.Ships.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                                enumerator.Current.GetAI().HasPriorityOrder = true;
                            break;
                        }
                    case 2:
                        bool flag3 = false;
                        if ((double)Vector2.Distance(this.findAveragePosition(), Task.AO) < 15000.0)
                        {
                            foreach (Ship ship in (List<Ship>)this.Ships)
                            {
                                lock (ship)
                                {
                                    if (ship.InCombat)
                                    {
                                        flag3 = true;
                                        ship.HyperspaceReturn();
                                        ship.GetAI().OrderQueue.Clear();
                                        break;
                                    }
                                }
                            }
                        }
                        if (!flag3 && (double)Vector2.Distance(this.findAveragePosition(), Task.AO) >= 5000.0)
                            break;
                        this.TaskStep = 3;
                        break;
                    case 3:
                        this.EnemyClumpsDict.Clear();
                        List<Ship> list2 = new List<Ship>();
                        List<GameplayObject> nearby1 = UniverseScreen.ShipSpatialManager.GetNearby(this.Position);
                        for (int index1 = 0; index1 < nearby1.Count; ++index1)
                        {
                            Ship ship1 = nearby1[index1] as Ship;
                            if (ship1 != null && ship1.loyalty != this.Owner && (ship1.loyalty.isFaction || this.Owner.GetRelations()[ship1.loyalty].AtWar || this.Owner.isFaction) && (!list2.Contains(ship1) && (double)Vector2.Distance(ship1.Center, Task.AO) < (double)Task.AORadius && !this.EnemyClumpsDict.ContainsKey(ship1.Center)))
                            {
                                this.EnemyClumpsDict.Add(ship1.Center, new List<Ship>());
                                this.EnemyClumpsDict[ship1.Center].Add(ship1);
                                list2.Add(ship1);
                                List<GameplayObject> nearby2 = UniverseScreen.ShipSpatialManager.GetNearby(this.Position);
                                for (int index2 = 0; index2 < nearby2.Count; ++index2)
                                {
                                    Ship ship2 = nearby2[index2] as Ship;
                                    if (ship2 != null && ship2.loyalty != this.Owner && (ship2.loyalty == ship1.loyalty && (double)Vector2.Distance(ship1.Center, ship2.Center) < 10000.0) && !list2.Contains(ship2))
                                        this.EnemyClumpsDict[ship1.Center].Add(ship2);
                                }
                            }
                        }
                        if (this.EnemyClumpsDict.Count == 0)
                        {
                            if ((double)Vector2.Distance(this.findAveragePosition(), Task.AO) <= 10000.0)
                                break;
                            this.FormationWarpTo(Task.AO, 0.0f, new Vector2(0.0f, -1f));
                            break;
                        }
                        else
                        {
                            List<Vector2> list3 = new List<Vector2>();
                            foreach (KeyValuePair<Vector2, List<Ship>> keyValuePair in this.EnemyClumpsDict)
                                list3.Add(keyValuePair.Key);
                            IOrderedEnumerable<Vector2> orderedEnumerable2 = Enumerable.OrderBy<Vector2, float>((IEnumerable<Vector2>)list3, (Func<Vector2, float>)(clumpPos => Vector2.Distance(this.findAveragePosition(), clumpPos)));
                            List<Ship> list4 = new List<Ship>();
                            foreach (Ship toAttack in (IEnumerable<Ship>)Enumerable.OrderByDescending<Ship, int>((IEnumerable<Ship>)this.EnemyClumpsDict[Enumerable.First<Vector2>((IEnumerable<Vector2>)orderedEnumerable2)], (Func<Ship, int>)(ship => ship.Size)))
                            {
                                float num = 0.0f;
                                foreach (Ship ship in (IEnumerable<Ship>)Enumerable.OrderByDescending<Ship, int>((IEnumerable<Ship>)this.Ships, (Func<Ship, int>)(ship => ship.Size)))
                                {
                                    if (!list4.Contains(ship) && ((double)num == 0.0 || (double)num < (double)toAttack.GetStrength()))
                                    {
                                        ship.GetAI().OrderAttackSpecificTarget(toAttack);
                                        ship.GetAI().Intercepting = true;
                                        list4.Add(ship);
                                        num += ship.GetStrength();
                                    }
                                }
                            }
                            List<Ship> list5 = new List<Ship>();
                            foreach (Ship ship in (List<Ship>)this.Ships)
                            {
                                if (!list4.Contains(ship))
                                    list5.Add(ship);
                            }
                            foreach (Ship ship in list5)
                            {
                                ship.GetAI().OrderAttackSpecificTarget(list4[0].GetAI().Target as Ship);
                                ship.GetAI().Intercepting = true;
                            }
                            this.TaskStep = 4;
                            break;
                        }
                    case 4:
                        float num1 = 0.0f;
                        float num2 = 0.0f;
                        float num3 = 0.0f;
                        float num4 = 0.0f;
                        foreach (Ship ship in (List<Ship>)this.Ships)
                        {
                            num1 += ship.Ordinance;
                            num2 += ship.OrdinanceMax;
                            foreach (Weapon weapon in ship.Weapons)
                            {
                                if (weapon.BombTroopDamage_Max <= 0)
                                {
                                    if ((double)weapon.OrdinanceRequiredToFire > 0.0)
                                        num3 = weapon.DamageAmount / weapon.fireDelay;
                                    if ((double)weapon.PowerRequiredToFire > 0.0)
                                        num4 = weapon.DamageAmount / weapon.fireDelay;
                                }
                            }
                        }
                        float num5 = num3 + num4;
                        if ((double)num3 >= 0.649999976158142 * (double)num5 && (double)num1 <= 0.100000001490116 * (double)num2)
                        {
                            this.TaskStep = 5;
                            break;
                        }
                        else
                        {
                            bool flag4 = false;
                            foreach (Ship ship in (List<Ship>)this.Ships)
                            {
                                if (!ship.InCombat)
                                {
                                    flag4 = true;
                                    break;
                                }
                            }
                            if (!flag4)
                                break;
                            this.TaskStep = 3;
                            break;
                        }
                    case 5:
                        List<Planet> list6 = new List<Planet>();
                        foreach (Planet planet in this.Owner.GetPlanets())
                        {
                            if (planet.HasShipyard)
                                list6.Add(planet);
                        }
                        IOrderedEnumerable<Planet> orderedEnumerable3 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)list6, (Func<Planet, float>)(p => Vector2.Distance(this.Position, p.Position)));
                        if (Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable3) <= 0)
                            break;
                        this.Position = Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable3).Position;
                        foreach (Ship ship in (List<Ship>)this.Ships)
                            ship.GetAI().OrderResupply(Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable3), true);
                        this.TaskStep = 6;
                        break;
                    case 6:
                        float num6 = 0.0f;
                        float num7 = 0.0f;
                        foreach (Ship ship in (List<Ship>)this.Ships)
                        {
                            ship.GetAI().HasPriorityOrder = true;
                            num6 += ship.Ordinance;
                            num7 += ship.OrdinanceMax;
                        }
                        if ((double)num6 != (double)num7)
                            break;
                        this.TaskStep = 0;
                        break;
                }
            }
        }

        private void DoDefendSystem(MilitaryTask Task)
        {
            switch (this.TaskStep)
            {
                case -1:
                    bool flag1 = true;
                    foreach (Ship ship in (List<Ship>)this.Ships)
                    {
                        if (!ship.disabled && ship.Active)
                        {
                            if ((double)Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 5000.0)
                                flag1 = false;
                            int num = ship.InCombat ? 1 : 0;
                            if (!flag1)
                                break;
                        }
                    }
                    if (!flag1)
                        break;
                    this.TaskStep = 2;
                    this.FormationWarpTo(Task.AO, MathHelper.ToRadians(HelperFunctions.findAngleToTarget(this.findAveragePosition(), Task.AO)), Vector2.Normalize(Task.AO - this.findAveragePosition()));
                    using (List<Ship>.Enumerator enumerator = this.Ships.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                            enumerator.Current.GetAI().HasPriorityOrder = true;
                        break;
                    }
                case 0:
                    List<Planet> list1 = new List<Planet>();
                    foreach (Planet planet in this.Owner.GetPlanets())
                    {
                        if (planet.HasShipyard)
                            list1.Add(planet);
                    }
                    IOrderedEnumerable<Planet> orderedEnumerable1 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)list1, (Func<Planet, float>)(planet => Vector2.Distance(Task.AO, planet.Position)));
                    if (Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable1) <= 0)
                        break;
                    Vector2 fVec = Vector2.Normalize(Task.AO - Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable1).Position);
                    Vector2 vector2 = Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable1).Position;
                    this.MoveToNow(vector2, Math.Abs(MathHelper.ToRadians(HelperFunctions.findAngleToTarget(vector2, Task.AO))), fVec);
                    this.TaskStep = 1;
                    break;
                case 1:
                    bool flag2 = true;
                    foreach (Ship ship in (List<Ship>)this.Ships)
                    {
                        if (!ship.disabled && ship.Active)
                        {
                            if ((double)Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 5000.0)
                                flag2 = false;
                            int num = ship.InCombat ? 1 : 0;
                            if (!flag2)
                                break;
                        }
                    }
                    if (!flag2)
                        break;
                    this.TaskStep = 2;
                    this.FormationWarpTo(Task.AO, MathHelper.ToRadians(HelperFunctions.findAngleToTarget(this.findAveragePosition(), Task.AO)), Vector2.Normalize(Task.AO - this.findAveragePosition()));
                    using (List<Ship>.Enumerator enumerator = this.Ships.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                            enumerator.Current.GetAI().HasPriorityOrder = true;
                        break;
                    }
                case 2:
                    bool flag3 = false;
                    if ((double)Vector2.Distance(this.findAveragePosition(), Task.AO) < 15000.0)
                    {
                        foreach (Ship ship in (List<Ship>)this.Ships)
                        {
                            lock (ship)
                            {
                                if (ship.InCombat)
                                {
                                    flag3 = true;
                                    ship.HyperspaceReturn();
                                    ship.GetAI().OrderQueue.Clear();
                                    break;
                                }
                            }
                        }
                    }
                    if (!flag3 && (double)Vector2.Distance(this.findAveragePosition(), Task.AO) >= 5000.0)
                        break;
                    this.TaskStep = 3;
                    break;
                case 3:
                    this.EnemyClumpsDict.Clear();
                    List<Ship> list2 = new List<Ship>();
                    List<GameplayObject> nearby1 = UniverseScreen.ShipSpatialManager.GetNearby(this.Position);
                    for (int index1 = 0; index1 < nearby1.Count; ++index1)
                    {
                        Ship ship1 = nearby1[index1] as Ship;
                        if (ship1 != null && ship1.loyalty != this.Owner && (ship1.loyalty.isFaction || this.Owner.GetRelations()[ship1.loyalty].AtWar || this.Owner.isFaction) && (!list2.Contains(ship1) && (double)Vector2.Distance(ship1.Center, Task.AO) < (double)Task.AORadius && !this.EnemyClumpsDict.ContainsKey(ship1.Center)))
                        {
                            this.EnemyClumpsDict.Add(ship1.Center, new List<Ship>());
                            this.EnemyClumpsDict[ship1.Center].Add(ship1);
                            list2.Add(ship1);
                            List<GameplayObject> nearby2 = UniverseScreen.ShipSpatialManager.GetNearby(this.Position);
                            for (int index2 = 0; index2 < nearby2.Count; ++index2)
                            {
                                Ship ship2 = nearby2[index2] as Ship;
                                if (ship2 != null && ship2.loyalty != this.Owner && (ship2.loyalty == ship1.loyalty && (double)Vector2.Distance(ship1.Center, ship2.Center) < 10000.0) && !list2.Contains(ship2))
                                    this.EnemyClumpsDict[ship1.Center].Add(ship2);
                            }
                        }
                    }
                    if (this.EnemyClumpsDict.Count == 0)
                    {
                        if ((double)Vector2.Distance(this.findAveragePosition(), Task.AO) <= 10000.0)
                            break;
                        this.FormationWarpTo(Task.AO, 0.0f, new Vector2(0.0f, -1f));
                        break;
                    }
                    else
                    {
                        List<Vector2> list3 = new List<Vector2>();
                        foreach (KeyValuePair<Vector2, List<Ship>> keyValuePair in this.EnemyClumpsDict)
                            list3.Add(keyValuePair.Key);
                        IOrderedEnumerable<Vector2> orderedEnumerable2 = Enumerable.OrderBy<Vector2, float>((IEnumerable<Vector2>)list3, (Func<Vector2, float>)(clumpPos => Vector2.Distance(this.findAveragePosition(), clumpPos)));
                        List<Ship> list4 = new List<Ship>();
                        foreach (Ship toAttack in (IEnumerable<Ship>)Enumerable.OrderByDescending<Ship, int>((IEnumerable<Ship>)this.EnemyClumpsDict[Enumerable.First<Vector2>((IEnumerable<Vector2>)orderedEnumerable2)], (Func<Ship, int>)(ship => ship.Size)))
                        {
                            float num = 0.0f;
                            foreach (Ship ship in (IEnumerable<Ship>)Enumerable.OrderByDescending<Ship, int>((IEnumerable<Ship>)this.Ships, (Func<Ship, int>)(ship => ship.Size)))
                            {
                                if (!list4.Contains(ship) && ((double)num == 0.0 || (double)num < (double)toAttack.GetStrength()))
                                {
                                    ship.GetAI().OrderAttackSpecificTarget(toAttack);
                                    ship.GetAI().Intercepting = true;
                                    list4.Add(ship);
                                    num += ship.GetStrength();
                                }
                            }
                        }
                        List<Ship> list5 = new List<Ship>();
                        foreach (Ship ship in (List<Ship>)this.Ships)
                        {
                            if (!list4.Contains(ship))
                                list5.Add(ship);
                        }
                        foreach (Ship ship in list5)
                        {
                            ship.GetAI().OrderAttackSpecificTarget(list4[0].GetAI().Target as Ship);
                            ship.GetAI().Intercepting = true;
                        }
                        this.TaskStep = 4;
                        break;
                    }
                case 4:
                    float num1 = 0.0f;
                    float num2 = 0.0f;
                    float num3 = 0.0f;
                    float num4 = 0.0f;
                    foreach (Ship ship in (List<Ship>)this.Ships)
                    {
                        num1 += ship.Ordinance;
                        num2 += ship.OrdinanceMax;
                        foreach (Weapon weapon in ship.Weapons)
                        {
                            if ((double)weapon.OrdinanceRequiredToFire > 0.0)
                                num3 = weapon.DamageAmount / weapon.fireDelay;
                            if ((double)weapon.PowerRequiredToFire > 0.0)
                                num4 = weapon.DamageAmount / weapon.fireDelay;
                        }
                    }
                    float num5 = num3 + num4;
                    if ((double)num3 >= 0.5 * (double)num5 && (double)num1 <= 0.100000001490116 * (double)num2)
                    {
                        this.TaskStep = 5;
                        break;
                    }
                    else
                    {
                        bool flag4 = false;
                        foreach (Ship ship in (List<Ship>)this.Ships)
                        {
                            if (!ship.InCombat)
                            {
                                flag4 = true;
                                break;
                            }
                        }
                        if (!flag4)
                            break;
                        this.TaskStep = 3;
                        break;
                    }
                case 5:
                    List<Planet> list6 = new List<Planet>();
                    foreach (Planet planet in this.Owner.GetPlanets())
                    {
                        if (planet.HasShipyard)
                            list6.Add(planet);
                    }
                    IOrderedEnumerable<Planet> orderedEnumerable3 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)list6, (Func<Planet, float>)(p => Vector2.Distance(this.Position, p.Position)));
                    if (Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable3) <= 0)
                        break;
                    this.Position = Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable3).Position;
                    foreach (Ship ship in (List<Ship>)this.Ships)
                        ship.GetAI().OrderResupply(Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable3), true);
                    this.TaskStep = 6;
                    break;
                case 6:
                    float num6 = 0.0f;
                    float num7 = 0.0f;
                    foreach (Ship ship in (List<Ship>)this.Ships)
                    {
                        ship.GetAI().HasPriorityOrder = true;
                        num6 += ship.Ordinance;
                        num7 += ship.OrdinanceMax;
                    }
                    if ((double)num6 != (double)num7)
                        break;
                    this.TaskStep = 0;
                    break;
            }
        }

        private void DoClaimDefense(MilitaryTask Task)
        {
            switch (this.TaskStep)
            {
                case 0:
                    List<Planet> list1 = new List<Planet>();
                    foreach (Planet planet in this.Owner.GetPlanets())
                    {
                        if (planet.HasShipyard)
                            list1.Add(planet);
                    }
                    IOrderedEnumerable<Planet> orderedEnumerable1 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)list1, (Func<Planet, float>)(planet => Vector2.Distance(Task.AO, planet.Position)));
                    if (Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable1) <= 0)
                        break;
                    Vector2 fVec = Vector2.Normalize(Task.GetTargetPlanet().Position - Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable1).Position);
                    Vector2 vector2 = Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable1).Position;
                    this.MoveToNow(vector2, Math.Abs(MathHelper.ToRadians(HelperFunctions.findAngleToTarget(vector2, Task.GetTargetPlanet().Position))), fVec);
                    this.TaskStep = 1;
                    break;
                case 1:
                    bool flag1 = true;
                    foreach (Ship ship in (List<Ship>)this.Ships)
                    {
                        if (!ship.disabled && ship.Active)
                        {
                            if ((double)Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 5000.0)
                                flag1 = false;
                            int num = ship.InCombat ? 1 : 0;
                            if (!flag1)
                                break;
                        }
                    }
                    if (!flag1)
                        break;
                    this.TaskStep = 2;
                    this.FormationWarpTo(Task.GetTargetPlanet().Position, MathHelper.ToRadians(HelperFunctions.findAngleToTarget(this.findAveragePosition(), Task.GetTargetPlanet().Position)), Vector2.Normalize(Task.GetTargetPlanet().Position - this.findAveragePosition()));
                    using (List<Ship>.Enumerator enumerator = this.Ships.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                            enumerator.Current.GetAI().HasPriorityOrder = true;
                        break;
                    }
                case 2:
                    bool flag2 = false;
                    if ((double)Vector2.Distance(this.findAveragePosition(), Task.GetTargetPlanet().Position) < 15000.0)
                    {
                        foreach (Ship ship in (List<Ship>)this.Ships)
                        {
                            lock (ship)
                            {
                                if (ship.InCombat)
                                {
                                    flag2 = true;
                                    ship.HyperspaceReturn();
                                    ship.GetAI().OrderQueue.Clear();
                                    break;
                                }
                            }
                        }
                    }
                    if (!flag2 && (double)Vector2.Distance(this.findAveragePosition(), Task.GetTargetPlanet().Position) >= 5000.0)
                        break;
                    this.TaskStep = 3;
                    break;
                case 3:
                    this.EnemyClumpsDict.Clear();
                    List<Ship> list2 = new List<Ship>();
                    List<GameplayObject> nearby1 = UniverseScreen.ShipSpatialManager.GetNearby(this.Position);
                    for (int index1 = 0; index1 < nearby1.Count; ++index1)
                    {
                        Ship ship1 = nearby1[index1] as Ship;
                        if (ship1 != null)
                        {
                            ship1.GetAI().HasPriorityOrder = false;
                            if (ship1.loyalty != this.Owner && (ship1.loyalty.isFaction || this.Owner.GetRelations()[ship1.loyalty].AtWar || ship1.isColonyShip && !this.Owner.GetRelations()[ship1.loyalty].Treaty_NAPact) && (!list2.Contains(ship1) && (double)Vector2.Distance(ship1.Center, Task.GetTargetPlanet().Position) < (double)Task.AORadius && !this.EnemyClumpsDict.ContainsKey(ship1.Center)))
                            {
                                this.EnemyClumpsDict.Add(ship1.Center, new List<Ship>());
                                this.EnemyClumpsDict[ship1.Center].Add(ship1);
                                list2.Add(ship1);
                                List<GameplayObject> nearby2 = UniverseScreen.ShipSpatialManager.GetNearby(this.Position);
                                for (int index2 = 0; index2 < nearby2.Count; ++index2)
                                {
                                    Ship ship2 = nearby2[index2] as Ship;
                                    if (ship2 != null && ship2.loyalty != this.Owner && (ship2.loyalty == ship1.loyalty && (double)Vector2.Distance(ship1.Center, ship2.Center) < 10000.0) && !list2.Contains(ship2))
                                        this.EnemyClumpsDict[ship1.Center].Add(ship2);
                                }
                            }
                        }
                    }
                    if (this.EnemyClumpsDict.Count == 0)
                    {
                        using (List<Ship>.Enumerator enumerator = this.Ships.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                Ship current = enumerator.Current;
                                if (!(current.GetAI().State == AIState.Orbit ))
                                    current.GetAI().OrderOrbitPlanet(Task.GetTargetPlanet());
                                else if (current.GetAI().State == AIState.Orbit && (current.GetAI().OrbitTarget == null || current.GetAI().OrbitTarget != null && current.GetAI().OrbitTarget != Task.GetTargetPlanet()))
                                    current.GetAI().OrderOrbitPlanet(Task.GetTargetPlanet());
                            }
                            break;
                        }
                    }
                    else
                    {
                        List<Vector2> list3 = new List<Vector2>();
                        foreach (KeyValuePair<Vector2, List<Ship>> keyValuePair in this.EnemyClumpsDict)
                            list3.Add(keyValuePair.Key);
                        IOrderedEnumerable<Vector2> orderedEnumerable2 = Enumerable.OrderBy<Vector2, float>((IEnumerable<Vector2>)list3, (Func<Vector2, float>)(clumpPos => Vector2.Distance(this.findAveragePosition(), clumpPos)));
                        List<Ship> list4 = new List<Ship>();
                        foreach (Ship toAttack in this.EnemyClumpsDict[Enumerable.First<Vector2>((IEnumerable<Vector2>)orderedEnumerable2)])
                        {
                            float num = 0.0f;
                            foreach (Ship ship in (List<Ship>)this.Ships)
                            {
                                if (!list4.Contains(ship) && ((double)num == 0.0 || (double)num < (double)toAttack.GetStrength()))
                                {
                                    ship.GetAI().Intercepting = true;
                                    ship.GetAI().OrderAttackSpecificTarget(toAttack);
                                    list4.Add(ship);
                                    num += ship.GetStrength();
                                }
                            }
                        }
                        List<Ship> list5 = new List<Ship>();
                        foreach (Ship ship in (List<Ship>)this.Ships)
                        {
                            if (!list4.Contains(ship))
                                list5.Add(ship);
                        }
                        foreach (Ship ship in list5)
                        {
                            ship.GetAI().Intercepting = true;
                            ship.GetAI().OrderAttackSpecificTarget(list4[0].GetAI().Target as Ship);
                        }
                        this.TaskStep = 4;
                        break;
                    }
                case 4:
                    float num1 = 0.0f;
                    float num2 = 0.0f;
                    float num3 = 0.0f;
                    float num4 = 0.0f;
                    foreach (Ship ship in (List<Ship>)this.Ships)
                    {
                        num1 += ship.Ordinance;
                        num2 += ship.OrdinanceMax;
                        foreach (Weapon weapon in ship.Weapons)
                        {
                            if ((double)weapon.OrdinanceRequiredToFire > 0.0)
                                num3 = weapon.DamageAmount / weapon.fireDelay;
                            if ((double)weapon.PowerRequiredToFire > 0.0)
                                num4 = weapon.DamageAmount / weapon.fireDelay;
                        }
                    }
                    float num5 = num3 + num4;
                    if ((double)num3 >= 0.5 * (double)num5 && (double)num1 <= 0.100000001490116 * (double)num2)
                    {
                        this.TaskStep = 5;
                        break;
                    }
                    else
                    {
                        bool flag3 = false;
                        foreach (Ship ship in (List<Ship>)this.Ships)
                        {
                            if (!ship.InCombat)
                            {
                                flag3 = true;
                                break;
                            }
                        }
                        if (!flag3)
                            break;
                        this.TaskStep = 3;
                        break;
                    }
                case 5:
                    List<Planet> list6 = new List<Planet>();
                    foreach (Planet planet in this.Owner.GetPlanets())
                    {
                        if (planet.HasShipyard)
                            list6.Add(planet);
                    }
                    IOrderedEnumerable<Planet> orderedEnumerable3 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)list6, (Func<Planet, float>)(p => Vector2.Distance(this.Position, p.Position)));
                    if (Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable3) <= 0)
                        break;
                    this.Position = Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable3).Position;
                    foreach (Ship ship in (List<Ship>)this.Ships)
                        ship.GetAI().OrderResupply(Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable3), true);
                    this.TaskStep = 6;
                    break;
                case 6:
                    float num6 = 0.0f;
                    float num7 = 0.0f;
                    foreach (Ship ship in (List<Ship>)this.Ships)
                    {
                        ship.GetAI().HasPriorityOrder = true;
                        num6 += ship.Ordinance;
                        num7 += ship.OrdinanceMax;
                    }
                    if ((double)num6 != (double)num7)
                        break;
                    this.TaskStep = 0;
                    break;
            }
        }

        private float GetClumpStrength(List<Ship> clumpships)
        {
            float num = 0.0f;
            foreach (Ship ship in clumpships)
                num += ship.GetStrength();
            return num;
        }

        private void DoCohesiveClearAreaOfEnemies(MilitaryTask Task)
        {
            switch (this.TaskStep)
            {
                case 0:
                    this.TaskStep = 1;
                    this.DoCohesiveClearAreaOfEnemies(Task);
                    break;
                case 1:
                    List<ThreatMatrix.Pin> list1 = new List<ThreatMatrix.Pin>();
                    Dictionary<ThreatMatrix.Pin, float> dictionary1 = new Dictionary<ThreatMatrix.Pin, float>();
                    foreach (KeyValuePair<Guid, ThreatMatrix.Pin> keyValuePair1 in this.Owner.GetGSAI().ThreatMatrix.Pins)
                    {
                        if (!(keyValuePair1.Value.EmpireName == this.Owner.data.Traits.Name) && (EmpireManager.GetEmpireByName(keyValuePair1.Value.EmpireName).isFaction || this.Owner.GetRelations()[EmpireManager.GetEmpireByName(keyValuePair1.Value.EmpireName)].AtWar) && (!list1.Contains(keyValuePair1.Value) && (double)Vector2.Distance(keyValuePair1.Value.Position, Task.AO) < (double)Task.AORadius))
                        {
                            dictionary1.Add(keyValuePair1.Value, keyValuePair1.Value.Strength);
                            list1.Add(keyValuePair1.Value);
                            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> keyValuePair2 in this.Owner.GetGSAI().ThreatMatrix.Pins)
                            {
                                if (!(keyValuePair2.Value.EmpireName == this.Owner.data.Traits.Name) && keyValuePair2.Value.EmpireName == keyValuePair1.Value.EmpireName && ((double)Vector2.Distance(keyValuePair1.Value.Position, keyValuePair2.Value.Position) < 150000.0 && !list1.Contains(keyValuePair2.Value)))
                                {
                                    Dictionary<ThreatMatrix.Pin, float> dictionary2;
                                    ThreatMatrix.Pin index;
                                    (dictionary2 = dictionary1)[index = keyValuePair1.Value] = dictionary2[index] + keyValuePair2.Value.Strength;
                                }
                            }
                        }
                    }
                    float strength = this.GetStrength();
                    this.targetPosition = Vector2.Zero;
                    foreach (KeyValuePair<ThreatMatrix.Pin, float> keyValuePair in dictionary1)
                    {
                        if ((double)strength > (double)keyValuePair.Value * 1.35000002384186 && (double)keyValuePair.Value > 750.0)
                            this.targetPosition = keyValuePair.Key.Position;
                    }
                    if (this.targetPosition != Vector2.Zero)
                    {
                        Vector2 fvec = Vector2.Normalize(Task.AO - this.targetPosition);
                        this.FormationWarpTo(this.targetPosition, Math.Abs(MathHelper.ToRadians(HelperFunctions.findAngleToTarget(this.targetPosition, Task.AO))), fvec);
                        this.TaskStep = 2;
                        break;
                    }
                    else
                    {
                        this.Task.EndTask();
                        break;
                    }
                case 2:
                    if ((double)this.Owner.GetGSAI().ThreatMatrix.PingRadarStr(this.targetPosition, 20000f, this.Owner) == 0.0)
                    {
                        this.TaskStep = 1;
                        break;
                    }
                    else
                    {
                        if ((double)Vector2.Distance(this.targetPosition, this.findAveragePosition()) >= 25000.0)
                            break;
                        this.TaskStep = 3;
                        break;
                    }
                case 3:
                    this.EnemyClumpsDict.Clear();
                    List<Ship> list2 = new List<Ship>();
                    Vector2 averagePosition = this.findAveragePosition();
                    List<GameplayObject> nearby1 = UniverseScreen.ShipSpatialManager.GetNearby(this.Position);
                    for (int index1 = 0; index1 < nearby1.Count; ++index1)
                    {
                        Ship ship1 = nearby1[index1] as Ship;
                        if (ship1 != null && ship1.loyalty != this.Owner && (ship1.loyalty.isFaction || this.Owner.GetRelations()[ship1.loyalty].AtWar) && (!list2.Contains(ship1) && (double)Vector2.Distance(ship1.Center, averagePosition) < 50000.0 && !this.EnemyClumpsDict.ContainsKey(ship1.Center)))
                        {
                            this.EnemyClumpsDict.Add(ship1.Center, new List<Ship>());
                            this.EnemyClumpsDict[ship1.Center].Add(ship1);
                            list2.Add(ship1);
                            List<GameplayObject> nearby2 = UniverseScreen.ShipSpatialManager.GetNearby(this.Position);
                            for (int index2 = 0; index2 < nearby2.Count; ++index2)
                            {
                                Ship ship2 = nearby2[index2] as Ship;
                                if (ship2 != null && ship2.loyalty != this.Owner && (ship2.loyalty == ship1.loyalty && (double)Vector2.Distance(ship1.Center, ship2.Center) < 10000.0) && !list2.Contains(ship2))
                                    this.EnemyClumpsDict[ship1.Center].Add(ship2);
                            }
                        }
                    }
                    if (this.EnemyClumpsDict.Count == 0)
                    {
                        Task.EndTask();
                        break;
                    }
                    else
                    {
                        List<Vector2> list3 = new List<Vector2>();
                        foreach (KeyValuePair<Vector2, List<Ship>> keyValuePair in this.EnemyClumpsDict)
                            list3.Add(keyValuePair.Key);
                        IOrderedEnumerable<Vector2> orderedEnumerable = Enumerable.OrderBy<Vector2, float>((IEnumerable<Vector2>)list3, (Func<Vector2, float>)(clumpPos => Vector2.Distance(this.findAveragePosition(), clumpPos)));
                        List<Ship> list4 = new List<Ship>();
                        foreach (Ship toAttack in this.EnemyClumpsDict[Enumerable.First<Vector2>((IEnumerable<Vector2>)orderedEnumerable)])
                        {
                            float num = 0.0f;
                            foreach (Ship ship in (List<Ship>)this.Ships)
                            {
                                if (!list4.Contains(ship) && ((double)num == 0.0 || (double)num < (double)toAttack.GetStrength()))
                                {
                                    ship.GetAI().Intercepting = true;
                                    ship.GetAI().OrderAttackSpecificTarget(toAttack);
                                    list4.Add(ship);
                                    num += ship.GetStrength();
                                }
                            }
                        }
                        List<Ship> list5 = new List<Ship>();
                        foreach (Ship ship in (List<Ship>)this.Ships)
                        {
                            if (!list4.Contains(ship))
                                list5.Add(ship);
                        }
                        foreach (Ship ship in list5)
                        {
                            ship.GetAI().Intercepting = true;
                            ship.GetAI().OrderAttackSpecificTarget(list4[0].GetAI().Target as Ship);
                        }
                        this.TaskStep = 4;
                        break;
                    }
                case 4:
                    float num1 = 0.0f;
                    float num2 = 0.0f;
                    float num3 = 0.0f;
                    float num4 = 0.0f;
                    foreach (Ship ship in (List<Ship>)this.Ships)
                    {
                        num1 += ship.Ordinance;
                        num2 += ship.OrdinanceMax;
                        foreach (Weapon weapon in ship.Weapons)
                        {
                            if ((double)weapon.OrdinanceRequiredToFire > 0.0)
                                num3 = weapon.DamageAmount / weapon.fireDelay;
                            if ((double)weapon.PowerRequiredToFire > 0.0)
                                num4 = weapon.DamageAmount / weapon.fireDelay;
                        }
                    }
                    float num5 = num3 + num4;
                    if ((double)num3 >= 0.5 * (double)num5 && (double)num1 <= 0.100000001490116 * (double)num2)
                    {
                        this.TaskStep = 5;
                        break;
                    }
                    else
                    {
                        bool flag = false;
                        foreach (Ship ship in (List<Ship>)this.Ships)
                        {
                            if (!ship.InCombat)
                            {
                                flag = true;
                                break;
                            }
                        }
                        if (!flag)
                            break;
                        this.TaskStep = 3;
                        break;
                    }
                case 5:
                    foreach (Ship ship in (List<Ship>)this.Ships)
                        ship.GetAI().OrderResupplyNearest();
                    this.TaskStep = 6;
                    break;
                case 6:
                    float num6 = 0.0f;
                    float num7 = 0.0f;
                    foreach (Ship ship in (List<Ship>)this.Ships)
                    {
                        if (ship.GetAI().State != AIState.Resupply)
                        {
                            Task.EndTask();
                            return;
                        }
                        else
                        {
                            ship.GetAI().HasPriorityOrder = true;
                            num6 += ship.Ordinance;
                            num7 += ship.OrdinanceMax;
                        }
                    }
                    if ((double)num6 != (double)num7)
                        break;
                    this.TaskStep = 1;
                    break;
            }
        }

        private void DoGlassPlanet(MilitaryTask Task)
        {
            if (Task.GetTargetPlanet().Owner == this.Owner || Task.GetTargetPlanet().Owner == null)
                Task.EndTask();
            else if (Task.GetTargetPlanet().Owner != null & Task.GetTargetPlanet().Owner != this.Owner && !Task.GetTargetPlanet().Owner.GetRelations()[this.Owner].AtWar)
            {
                Task.EndTask();
            }
            else
            {
                switch (this.TaskStep)
                {
                    case 0:
                        List<Planet> list1 = new List<Planet>();
                        foreach (Planet planet in this.Owner.GetPlanets())
                        {
                            if (planet.HasShipyard)
                                list1.Add(planet);
                        }
                        IOrderedEnumerable<Planet> orderedEnumerable1 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)list1, (Func<Planet, float>)(planet => Vector2.Distance(Task.AO, planet.Position)));
                        if (Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable1) <= 0)
                            break;
                        Vector2 fVec = Vector2.Normalize(Task.AO - Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable1).Position);
                        Vector2 vector2 = Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable1).Position;
                        this.MoveToNow(vector2, Math.Abs(MathHelper.ToRadians(HelperFunctions.findAngleToTarget(vector2, Task.AO))), fVec);
                        this.TaskStep = 1;
                        break;
                    case 1:
                        bool flag = true;
                        foreach (Ship ship in (List<Ship>)this.Ships)
                        {
                            if (!ship.disabled && ship.Active)
                            {
                                if ((double)Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 15000.0)
                                    flag = false;
                                int num = ship.InCombat ? 1 : 0;
                                if (!flag)
                                    break;
                            }
                        }
                        if (!flag)
                            break;
                        Vector2 MovePosition = Task.GetTargetPlanet().Position + Vector2.Normalize(this.findAveragePosition() - Task.GetTargetPlanet().Position) * 150000f;
                        this.Position = MovePosition;
                        this.FormationWarpTo(MovePosition, MathHelper.ToRadians(HelperFunctions.findAngleToTarget(this.findAveragePosition(), Task.AO)), Vector2.Normalize(Task.AO - this.findAveragePosition()));
                        foreach (Ship ship in (List<Ship>)this.Ships)
                            ship.GetAI().HasPriorityOrder = true;
                        this.TaskStep = 2;
                        break;
                    case 2:
                        if (Task.WaitForCommand && (double)this.Owner.GetGSAI().ThreatMatrix.PingRadarStr(Task.GetTargetPlanet().Position, 30000f, this.Owner) > 250.0)
                            break;
                        foreach (Ship ship in (List<Ship>)this.Ships)
                            ship.GetAI().OrderBombardPlanet(Task.GetTargetPlanet());
                        this.TaskStep = 4;
                        break;
                    case 4:
                        float num1 = 0.0f;
                        float num2 = 0.0f;
                        float num3 = 0.0f;
                        float num4 = 0.0f;
                        foreach (Ship ship in (List<Ship>)this.Ships)
                        {
                            num1 += ship.Ordinance;
                            num2 += ship.OrdinanceMax;
                            foreach (Weapon weapon in ship.Weapons)
                            {
                                if ((double)weapon.OrdinanceRequiredToFire > 0.0)
                                    num3 = weapon.DamageAmount / weapon.fireDelay;
                                if ((double)weapon.PowerRequiredToFire > 0.0)
                                    num4 = weapon.DamageAmount / weapon.fireDelay;
                            }
                        }
                        float num5 = num3 + num4;
                        if ((double)num3 >= 0.5 * (double)num5 && (double)num1 <= 0.100000001490116 * (double)num2)
                        {
                            this.TaskStep = 5;
                            break;
                        }
                        else
                        {
                            this.TaskStep = 2;
                            break;
                        }
                    case 5:
                        List<Planet> list2 = new List<Planet>();
                        foreach (Planet planet in this.Owner.GetPlanets())
                        {
                            if (planet.HasShipyard)
                                list2.Add(planet);
                        }
                        IOrderedEnumerable<Planet> orderedEnumerable2 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)list2, (Func<Planet, float>)(p => Vector2.Distance(this.Position, p.Position)));
                        if (Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable2) <= 0)
                            break;
                        this.Position = Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable2).Position;
                        foreach (Ship ship in (List<Ship>)this.Ships)
                            ship.GetAI().OrderResupply(Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable2), true);
                        this.TaskStep = 6;
                        break;
                    case 6:
                        float num6 = 0.0f;
                        float num7 = 0.0f;
                        foreach (Ship ship in (List<Ship>)this.Ships)
                        {
                            if (ship.GetAI().State != AIState.Resupply)
                            {
                                this.TaskStep = 5;
                                return;
                            }
                            else
                            {
                                ship.GetAI().HasPriorityOrder = true;
                                num6 += ship.Ordinance;
                                num7 += ship.OrdinanceMax;
                            }
                        }
                        if ((double)num6 != (double)num7)
                            break;
                        this.TaskStep = 0;
                        break;
                }
            }
        }

        private void DoClearAreaOfEnemies(MilitaryTask Task)
        {
            switch (this.TaskStep)
            {
                case 0:
                    List<Planet> list1 = new List<Planet>();
                    foreach (Planet planet in this.Owner.GetPlanets())
                    {
                        if (planet.HasShipyard)
                            list1.Add(planet);
                    }
                    IOrderedEnumerable<Planet> orderedEnumerable1 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)list1, (Func<Planet, float>)(planet => Vector2.Distance(Task.AO, planet.Position)));
                    if (Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable1) <= 0)
                        break;
                    Vector2 fVec = Vector2.Normalize(Task.AO - Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable1).Position);
                    Vector2 vector2 = Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable1).Position;
                    this.MoveToNow(vector2, Math.Abs(MathHelper.ToRadians(HelperFunctions.findAngleToTarget(vector2, Task.AO))), fVec);
                    this.TaskStep = 1;
                    break;
                case 1:
                    bool flag1 = true;
                    foreach (Ship ship in (List<Ship>)this.Ships)
                    {
                        if (!ship.disabled && ship.Active)
                        {
                            if ((double)Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 5000.0)
                                flag1 = false;
                            int num = ship.InCombat ? 1 : 0;
                            if (!flag1)
                                break;
                        }
                    }
                    if (!flag1)
                        break;
                    this.TaskStep = 2;
                    this.FormationWarpTo(Task.AO, MathHelper.ToRadians(HelperFunctions.findAngleToTarget(this.findAveragePosition(), Task.AO)), Vector2.Normalize(Task.AO - this.findAveragePosition()));
                    using (List<Ship>.Enumerator enumerator = this.Ships.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                            enumerator.Current.GetAI().HasPriorityOrder = true;
                        break;
                    }
                case 2:
                    float num1 = 0.0f;
                    float num2 = 0.0f;
                    float num3 = 0.0f;
                    float num4 = 0.0f;
                    foreach (Ship ship in (List<Ship>)this.Ships)
                    {
                        num1 += ship.Ordinance;
                        num2 += ship.OrdinanceMax;
                        foreach (Weapon weapon in ship.Weapons)
                        {
                            if ((double)weapon.OrdinanceRequiredToFire > 0.0)
                                num3 = weapon.DamageAmount / weapon.fireDelay;
                            if ((double)weapon.PowerRequiredToFire > 0.0)
                                num4 = weapon.DamageAmount / weapon.fireDelay;
                        }
                    }
                    float num5 = num3 + num4;
                    if ((double)num3 >= 0.5 * (double)num5 && (double)num1 <= 0.100000001490116 * (double)num2)
                    {
                        this.TaskStep = 5;
                        break;
                    }
                    else
                    {
                        bool flag2 = false;
                        if ((double)Vector2.Distance(this.findAveragePosition(), Task.AO) < 15000.0)
                        {
                            foreach (Ship ship in (List<Ship>)this.Ships)
                            {
                                lock (ship)
                                {
                                    if (ship.InCombat)
                                    {
                                        flag2 = true;
                                        ship.HyperspaceReturn();
                                        ship.GetAI().OrderQueue.Clear();
                                        break;
                                    }
                                }
                            }
                        }
                        if (!flag2 && (double)Vector2.Distance(this.findAveragePosition(), Task.AO) >= 10000.0)
                            break;
                        this.TaskStep = 3;
                        break;
                    }
                case 3:
                    this.EnemyClumpsDict.Clear();
                    List<Ship> list2 = new List<Ship>();
                    List<GameplayObject> nearby1 = UniverseScreen.ShipSpatialManager.GetNearby((GameplayObject)this.Ships[0]);
                    for (int index1 = 0; index1 < nearby1.Count; ++index1)
                    {
                        Ship ship1 = nearby1[index1] as Ship;
                        if (ship1 != null && ship1.loyalty != this.Owner && (ship1.loyalty.isFaction || this.Owner.GetRelations()[ship1.loyalty].AtWar) && (!list2.Contains(ship1) && (double)Vector2.Distance(ship1.Center, Task.AO) < (double)Task.AORadius && !this.EnemyClumpsDict.ContainsKey(ship1.Center)))
                        {
                            this.EnemyClumpsDict.Add(ship1.Center, new List<Ship>());
                            this.EnemyClumpsDict[ship1.Center].Add(ship1);
                            list2.Add(ship1);
                            List<GameplayObject> nearby2 = UniverseScreen.ShipSpatialManager.GetNearby((GameplayObject)this.Ships[0]);
                            for (int index2 = 0; index2 < nearby2.Count; ++index2)
                            {
                                Ship ship2 = nearby2[index2] as Ship;
                                if (ship2 != null && ship2.loyalty != this.Owner && (ship2.loyalty == ship1.loyalty && (double)Vector2.Distance(ship1.Center, ship2.Center) < 10000.0) && !list2.Contains(ship2))
                                    this.EnemyClumpsDict[ship1.Center].Add(ship2);
                            }
                        }
                    }
                    if (this.EnemyClumpsDict.Count == 0 || (double)Vector2.Distance(this.findAveragePosition(), Task.AO) > 25000.0)
                    {
                        Vector2 enemyWithinRadius = this.Owner.GetGSAI().ThreatMatrix.GetPositionOfNearestEnemyWithinRadius(this.Position, Task.AORadius, this.Owner);
                        if (enemyWithinRadius == Vector2.Zero)
                        {
                            Task.EndTask();
                            break;
                        }
                        else
                        {
                            this.MoveDirectlyNow(enemyWithinRadius, MathHelper.ToRadians(HelperFunctions.findAngleToTarget(this.findAveragePosition(), enemyWithinRadius)), Vector2.Normalize(enemyWithinRadius - this.Position));
                            this.TaskStep = 2;
                            break;
                        }
                    }
                    else
                    {
                        List<Vector2> list3 = new List<Vector2>();
                        foreach (KeyValuePair<Vector2, List<Ship>> keyValuePair in this.EnemyClumpsDict)
                            list3.Add(keyValuePair.Key);
                        IOrderedEnumerable<Vector2> orderedEnumerable2 = Enumerable.OrderBy<Vector2, float>((IEnumerable<Vector2>)list3, (Func<Vector2, float>)(clumpPos => Vector2.Distance(this.findAveragePosition(), clumpPos)));
                        List<Ship> list4 = new List<Ship>();
                        foreach (Ship toAttack in this.EnemyClumpsDict[Enumerable.First<Vector2>((IEnumerable<Vector2>)orderedEnumerable2)])
                        {
                            float num6 = 0.0f;
                            foreach (Ship ship in (List<Ship>)this.Ships)
                            {
                                if (!list4.Contains(ship) && ((double)num6 == 0.0 || (double)num6 < (double)toAttack.GetStrength()))
                                {
                                    ship.GetAI().Intercepting = true;
                                    ship.GetAI().OrderAttackSpecificTarget(toAttack);
                                    list4.Add(ship);
                                    num6 += ship.GetStrength();
                                }
                            }
                        }
                        List<Ship> list5 = new List<Ship>();
                        foreach (Ship ship in (List<Ship>)this.Ships)
                        {
                            if (!list4.Contains(ship))
                                list5.Add(ship);
                        }
                        foreach (Ship ship in list5)
                        {
                            ship.GetAI().Intercepting = true;
                            ship.GetAI().OrderAttackSpecificTarget(list4[0].GetAI().Target as Ship);
                        }
                        this.TaskStep = 4;
                        break;
                    }
                case 4:
                    float num7 = 0.0f;
                    float num8 = 0.0f;
                    float num9 = 0.0f;
                    float num10 = 0.0f;
                    foreach (Ship ship in (List<Ship>)this.Ships)
                    {
                        num7 += ship.Ordinance;
                        num8 += ship.OrdinanceMax;
                        foreach (Weapon weapon in ship.Weapons)
                        {
                            if ((double)weapon.OrdinanceRequiredToFire > 0.0)
                                num9 = weapon.DamageAmount / weapon.fireDelay;
                            if ((double)weapon.PowerRequiredToFire > 0.0)
                                num10 = weapon.DamageAmount / weapon.fireDelay;
                        }
                    }
                    float num11 = num9 + num10;
                    if ((double)num9 >= 0.5 * (double)num11 && (double)num7 <= 0.100000001490116 * (double)num8)
                    {
                        this.TaskStep = 5;
                        break;
                    }
                    else
                    {
                        bool flag2 = false;
                        foreach (Ship ship in (List<Ship>)this.Ships)
                        {
                            if (!ship.InCombat)
                            {
                                flag2 = true;
                                break;
                            }
                        }
                        if (!flag2)
                            break;
                        this.TaskStep = 3;
                        break;
                    }
                case 5:
                    List<Planet> list6 = new List<Planet>();
                    foreach (Planet planet in this.Owner.GetPlanets())
                    {
                        if (planet.HasShipyard)
                            list6.Add(planet);
                    }
                    IOrderedEnumerable<Planet> orderedEnumerable3 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)list6, (Func<Planet, float>)(p => Vector2.Distance(this.Position, p.Position)));
                    if (Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable3) <= 0)
                        break;
                    this.Position = Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable3).Position;
                    foreach (Ship ship in (List<Ship>)this.Ships)
                        ship.GetAI().OrderResupply(Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable3), true);
                    this.TaskStep = 6;
                    break;
                case 6:
                    float num12 = 0.0f;
                    float num13 = 0.0f;
                    foreach (Ship ship in (List<Ship>)this.Ships)
                    {
                        if (ship.GetAI().State != AIState.Resupply)
                        {
                            this.TaskStep = 5;
                            return;
                        }
                        else
                        {
                            ship.GetAI().HasPriorityOrder = true;
                            num12 += ship.Ordinance;
                            num13 += ship.OrdinanceMax;
                        }
                    }
                    if ((double)num12 != (double)num13)
                        break;
                    this.TaskStep = 0;
                    break;
            }
        }

        public float GetStrength()
        {
            float num = 0.0f;
            foreach (Ship ship in (List<Ship>)this.Ships)
            {
                if (ship.Active)
                    num += ship.GetStrength();
            }
            return num;
        }

        public void UpdateAI(float elapsedTime, int which)
        {
            if (this.Task != null)
            {
                this.EvaluateTask(elapsedTime);
            }
            else
            {
                if (EmpireManager.GetEmpireByName(Fleet.screen.PlayerLoyalty) == this.Owner || this.IsCoreFleet || this.Ships.Count <= 0)
                    return;
                foreach (Ship s in (List<Ship>)this.Owner.GetFleetsDict()[which].Ships)
                {
                    s.GetAI().OrderQueue.Clear();
                    s.GetAI().State = AIState.AwaitingOrders;
                    s.fleet = (Fleet)null;
                    s.InCombatTimer = 0.0f;
                    s.InCombat = false;
                    s.HyperspaceReturn();
                    s.isSpooling = false;
                    if (s.Role == "troop")
                        s.GetAI().OrderRebaseToNearest();
                    else
                        this.Owner.ForcePoolAdd(s);
                }
                this.Owner.GetGSAI().UsedFleets.Remove(which);
                this.Reset();
            }
        }

        public void Update(float elapsedTime)
        {
            List<Ship> list = new List<Ship>();
            foreach (Ship ship in (List<Ship>)this.Ships)
            {
                if (!ship.Active)
                    list.Add(ship);
            }
            foreach (Ship ship in list)
            {
                ship.fleet = (Fleet)null;
                this.Ships.Remove(ship);
            }
            if (this.Ships.Count <= 0 || this.GoalStack.Count <= 0)
                return;
            this.GoalStack.Peek().Evaluate(elapsedTime);
        }

        public enum FleetCombatStatus
        {
            Maintain,
            Loose,
            Free,
        }

        public class Squad
        {
            public FleetDataNode MasterDataNode = new FleetDataNode();
            public BatchRemovalCollection<FleetDataNode> DataNodes = new BatchRemovalCollection<FleetDataNode>();
            public BatchRemovalCollection<Ship> Ships = new BatchRemovalCollection<Ship>();
            public Fleet Fleet;
            public Vector2 Offset;
            public Fleet.FleetCombatStatus FleetCombatStatus;
        }

        private struct quadrantscan
        {
            public Vector2 avgPos;
            public float Strength;
        }

        public enum FleetGoalType
        {
            AttackMoveTo,
            MoveTo,
        }

        public class FleetGoal
        {
            public Fleet.FleetGoalType type = Fleet.FleetGoalType.MoveTo;
            public Vector2 Velocity = new Vector2();
            public Vector2 MovePosition = new Vector2();
            public Vector2 PositionLast = new Vector2();
            public Vector2 FinalFacingVector = new Vector2();
            public SolarSystem sysToAttack;
            private Fleet fleet;
            public float FinalFacing;

            public FleetGoal(SolarSystem toAttack, Fleet fleet, Fleet.FleetGoalType t)
            {
                this.fleet = fleet;
                this.sysToAttack = toAttack;
                this.type = t;
            }

            public FleetGoal(Fleet fleet, Vector2 MovePosition, float facing, Vector2 fVec, Fleet.FleetGoalType t)
            {
                this.type = t;
                this.fleet = fleet;
                this.FinalFacingVector = fVec;
                this.FinalFacing = facing;
                this.MovePosition = MovePosition;
            }

            public void Evaluate(float elapsedTime)
            {
                switch (this.type)
                {
                    case Fleet.FleetGoalType.AttackMoveTo:
                        this.DoAttackMove(elapsedTime);
                        break;
                    case Fleet.FleetGoalType.MoveTo:
                        this.DoMove(elapsedTime);
                        break;
                }
            }

            private void DoAttackMove(float elapsedTime)
            {
                this.fleet.Position += Vector2.Normalize(HelperFunctions.FindVectorToTarget(this.fleet.Position, this.MovePosition)) * this.fleet.speed * elapsedTime;
                this.fleet.AssembleFleet(this.FinalFacing, this.FinalFacingVector);
                if ((double)Vector2.Distance(this.fleet.Position, this.MovePosition) >= 100.0)
                    return;
                this.fleet.Position = this.MovePosition;
                this.fleet.GoalStack.Pop();
            }

            private void DoMove(float elapsedTime)
            {
                Vector2 vector2 = Vector2.Normalize(HelperFunctions.FindVectorToTarget(this.fleet.Position, this.MovePosition));
                float num1 = 0.0f;
                int num2 = 0;
                foreach (Ship ship in (List<Ship>)this.fleet.Ships)
                {
                    if (ship.FleetCombatStatus != Fleet.FleetCombatStatus.Free && !ship.EnginesKnockedOut)
                    {
                        float num3 = Vector2.Distance(this.fleet.Position + ship.FleetOffset, ship.Center);
                        num1 += num3;
                        ++num2;
                    }
                }
                float num4 = num1 / (float)num2;
                this.fleet.Position += vector2 * (this.fleet.speed + 75f) * elapsedTime;
                this.fleet.AssembleFleet(this.FinalFacing, this.FinalFacingVector);
                if ((double)Vector2.Distance(this.fleet.Position, this.MovePosition) >= 100.0)
                    return;
                this.fleet.Position = this.MovePosition;
                this.fleet.GoalStack.Pop();
            }
        }
    }
}
