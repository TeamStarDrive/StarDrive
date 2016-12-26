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
using System.Xml.Serialization;

namespace Ship_Game.Gameplay
{
    public sealed class Fleet : ShipGroup
    {
        public BatchRemovalCollection<FleetDataNode> DataNodes = new BatchRemovalCollection<FleetDataNode>();
        public Guid guid = Guid.NewGuid();
        public string Name = "";
        private Stack<FleetGoal> GoalStack = new Stack<FleetGoal>();
        private Array<Ship> CenterShips = new Array<Ship>();
        private Array<Ship> LeftShips   = new Array<Ship>();
        private Array<Ship> RightShips  = new Array<Ship>();
        private Array<Ship> RearShips   = new Array<Ship>();
        private Array<Ship> ScreenShips = new Array<Ship>();
        public Array<Squad> CenterFlank = new Array<Squad>();
        public Array<Squad> LeftFlank   = new Array<Squad>();
        public Array<Squad> RightFlank  = new Array<Squad>();
        public Array<Squad> ScreenFlank = new Array<Squad>();
        public Array<Squad> RearFlank   = new Array<Squad>();
        public Array<Array<Squad>> AllFlanks = new Array<Array<Squad>>();
        public Vector2 GoalMovePosition = new Vector2();
        private Map<Vector2, Array<Ship>> EnemyClumpsDict = new Map<Vector2, Array<Ship>>();
        private Map<Ship, Array<Ship>> InterceptorDict = new Map<Ship, Array<Ship>>();
        private int defenseTurns = 50;
        private Vector2 targetPosition = Vector2.Zero;
        public MilitaryTask Task;
        public FleetCombatStatus fcs;
        public Empire Owner;
        public Vector2 Position;
        public float facing;
        public float speed;
        public int FleetIconIndex;
        //private bool HasPriorityOrder;
        //private bool InCombat;
        public static UniverseScreen screen;
        public int TaskStep;
        public bool IsCoreFleet;
        [XmlIgnore]
        public Vector2 StoredFleetPosition;
        [XmlIgnore]
        public float StoredFleetDistancetoMove;
        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;
        public bool HasRepair;  //fbedard: ships in fleet with repair capability will not return for repair.

        //This file refactored by Gretman

        public Fleet()
        {
            this.FleetIconIndex = RandomMath.IntBetween(1, 10);
        }

        public void SetNameByFleetIndex(int index)
        {
            string suffix = "th";
            switch (index % 10) {
                case 1: suffix = "st"; break;
                case 2: suffix = "nd"; break;
                case 3: suffix = "rd"; break;
            }
            Name = index + suffix + " fleet";
        }

        public Stack<Fleet.FleetGoal> GetStack()
        {
            return this.GoalStack;
        }

        public void AddShip(Ship shiptoadd)
        {
            if (shiptoadd.shipData.Role == ShipData.RoleName.station || shiptoadd.IsPlatform)
                return;
            this.Ships.Add(shiptoadd);
            shiptoadd.fleet = this;
            this.SetSpeed();
            this.AssignPositions(this.facing);
        }

        public void SetSpeed()
        {
            using (Ships.AcquireReadLock())
            {
                if (this.Ships.Count == 0) return;
                float slowestship = this.Ships[0].speed;
                for (int loop = 0; loop < this.Ships.Count; loop++)     //Modified this so speed of a fleet is only set in one place -Gretman
                {
                    if (this.Ships[loop].Inhibited || this.Ships[loop].EnginesKnockedOut || !this.Ships[loop].Active) continue;
                    if (this.Ships[loop].speed < slowestship) slowestship = this.Ships[loop].speed;
                }
                if (slowestship < 200) slowestship = 200;
                this.speed = slowestship;
            }
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

            BatchRemovalCollection<Ship> mainShipList = new BatchRemovalCollection<Ship>();
            mainShipList.AddRange(this.Ships);
            foreach (Ship ship in (Array<Ship>)mainShipList)
            {
                if (ship.shipData.Role == ShipData.RoleName.scout || ship.shipData.ShipCategory == ShipData.Category.Recon)
                {
                    this.ScreenShips.Add(ship);
                    mainShipList.QueuePendingRemoval(ship);
                }
                else if (ship.shipData.Role == ShipData.RoleName.troop || ship.shipData.Role == ShipData.RoleName.freighter || ship.shipData.ShipCategory == ShipData.Category.Civilian)
                {
                    this.RearShips.Add(ship);
                    mainShipList.QueuePendingRemoval(ship);
                }
                else if (ship.shipData.Role > ShipData.RoleName.cruiser)
                {
                    this.CenterShips.Add(ship);
                    mainShipList.QueuePendingRemoval(ship);
                }
            }
            mainShipList.ApplyPendingRemovals();

            this.SetSpeed();
            IOrderedEnumerable<Ship> remainingShips = Enumerable.OrderByDescending(mainShipList, ship => ship.GetStrength() + ship.Size);
            int totalShips = this.CenterShips.Count;
            foreach (Ship ship in remainingShips)
            {
                if      (totalShips < 4) this.CenterShips.Add(ship);
                else if (totalShips < 8) this.LeftShips.Add(ship);
                else if (totalShips < 12) this.RightShips.Add(ship);
                else if (totalShips < 16) this.ScreenShips.Add(ship);
                else if (totalShips < 20 && this.RearShips.Count == 0) this.RearShips.Add(ship);

                ++totalShips;
                if (totalShips == 16)
                {
                    ship.FleetCombatStatus = FleetCombatStatus.Maintain;
                    totalShips = 0;
                }
            }

            SortSquad(this.CenterShips, this.CenterFlank, true);
            SortSquad(this.LeftShips, this.LeftFlank);
            SortSquad(this.RightShips, this.RightFlank);
            SortSquad(this.ScreenShips, this.ScreenFlank);
            SortSquad(this.RearShips, this.RearFlank);

            this.Position = this.findAveragePosition();

            ArrangeSquad(this.CenterFlank, Vector2.Zero);
            ArrangeSquad(this.ScreenFlank, new Vector2(0.0f, -2500f));
            ArrangeSquad(this.RearFlank, new Vector2(0.0f, 2500f));

            for (int index = 0; index < this.LeftFlank.Count; ++index)
            {
                this.LeftFlank[index].Offset = new Vector2((float)(-this.CenterFlank.Count * 1400 - (this.LeftFlank.Count == 1 ? 1400 : index * 1400)), 0.0f);
            }

            for (int index = 0; index < this.RightFlank.Count; ++index)
            {
                this.RightFlank[index].Offset = new Vector2((float)(this.CenterFlank.Count * 1400 + (this.RightFlank.Count == 1 ? 1400 : index * 1400)), 0.0f);
            }

            this.AutoAssembleFleet(0.0f, new Vector2(0.0f, -1f));
            foreach (Ship s in (Array<Ship>)this.Ships)
            {
                if (!s.InCombat)
                {
                    lock (s.GetAI().WayPointLocker)
                        s.GetAI().OrderThrustTowardsPosition(this.Position + s.FleetOffset, this.facing, new Vector2(0.0f, -1f), true);
                }
                FleetDataNode fleetDataNode = new FleetDataNode();
                fleetDataNode.Ship = s;
                fleetDataNode.ShipName = s.Name;
                fleetDataNode.FleetOffset = s.RelativeFleetOffset;
                fleetDataNode.OrdersOffset = s.RelativeFleetOffset;
                this.DataNodes.Add(fleetDataNode);
            }
        }

        private void SortSquad(Array<Ship> allShips, Array<Squad> destSquad, bool sizeOverSpeed = false)
        {
            IOrderedEnumerable<Ship> orderedShips;      //If true, sort by size instead of speed
            if (sizeOverSpeed) { orderedShips = Enumerable.OrderByDescending(allShips, ship => ship.Size);  }
            else               { orderedShips = Enumerable.OrderByDescending(allShips, ship => ship.speed); }

            Fleet.Squad squad = new Fleet.Squad();
            squad.Fleet = this;
            for (int index = 0; index < orderedShips.Count(); ++index)
            {
                if (squad.Ships.Count < 4)
                    squad.Ships.Add(orderedShips.ElementAt(index));
                if (squad.Ships.Count == 4 || index == orderedShips.Count() - 1)
                {
                    destSquad.Add(squad);
                    squad = new Fleet.Squad();
                    squad.Fleet = this;
                }
            }
        }

        private void ArrangeSquad(Array<Fleet.Squad> squad, Vector2 squadOffset)
        {
            int leftSide = 0;
            int rightSide = 0;
            for (int index = 0; index < squad.Count; ++index)
            {
                if (index == 0)
                    squad[index].Offset = squadOffset;
                else if (index % 2 == 1)
                {
                    ++leftSide;
                    squad[index].Offset = new Vector2(leftSide * (-1400 + squadOffset.X), squadOffset.Y);
                }
                else
                {
                    ++rightSide;
                    squad[index].Offset = new Vector2(rightSide * (1400 + squadOffset.X), squadOffset.Y);
                }
            }
        }

        public void MoveToDirectly(Vector2 MovePosition, float facing, Vector2 fVec)
        {
            this.Position = this.findAveragePosition();
            this.GoalStack.Clear();
            this.MoveDirectlyNow(MovePosition, facing, fVec);
        }

        public void FormationWarpTo(Vector2 MovePosition, float facing, Vector2 fvec)
        {
            this.GoalStack.Clear();
            this.Position = MovePosition;
            this.facing = facing;
            this.AssembleFleet(facing, fvec);
            foreach (Ship ship in (Array<Ship>)this.Ships)
            {
                ship.GetAI().SetPriorityOrder();
                ship.GetAI().OrderFormationWarpQ(MovePosition + ship.FleetOffset, facing, fvec);
            }
        }

        public void AttackMoveTo(Vector2 movePosition)
        {
            this.GoalStack.Clear();
            Vector2 fVec = this.findAveragePosition().FindVectorToTarget(movePosition);
            this.Position = this.findAveragePosition() + fVec * 3500f;
            this.GoalStack.Push(new FleetGoal(this, movePosition, findAveragePosition().RadiansToTarget(movePosition), fVec, FleetGoalType.AttackMoveTo));
        }

        public void MoveToNow(Vector2 MovePosition, float facing, Vector2 fVec)
        {
            this.Position = MovePosition;
            this.facing = facing;
            this.AssembleFleet(facing, fVec);
            foreach (Ship ship in (Array<Ship>)this.Ships)
            {
                ship.GetAI().SetPriorityOrder();
                ship.GetAI().OrderMoveTowardsPosition(MovePosition + ship.FleetOffset, facing, fVec, true, null);
            }
        }

        private void MoveDirectlyNow(Vector2 MovePosition, float facing, Vector2 fVec)
        {
            this.Position = MovePosition;
            this.facing = facing;
            this.AssembleFleet(facing, fVec);
            foreach (Ship ship in (Array<Ship>)this.Ships)
            {
                //Prevent fleets with no tasks from and are near their distination from being dumb.
                if (this.Owner.isPlayer || ship.GetAI().State == AIState.AwaitingOrders || ship.GetAI().State == AIState.AwaitingOffenseOrders)
                {
                    ship.GetAI().SetPriorityOrder();
                    ship.GetAI().OrderMoveDirectlyTowardsPosition(MovePosition + ship.FleetOffset, facing, fVec, true);
                }
            }
        }

        private void AutoAssembleFleet(float facing, Vector2 facingVec)
        {
            foreach (Array<Fleet.Squad> list in this.AllFlanks)
            {
                foreach (Fleet.Squad squad in list)
                {
                    for (int index = 0; index < squad.Ships.Count; ++index)
                    {
                        float radiansAngle = 0;
                        switch (index)
                        {
                            case 0:
                                radiansAngle = new Vector2(0.0f, -500f).ToRadians();
                                break;
                            case 1:
                                radiansAngle = new Vector2(-500f, 0.0f).ToRadians();
                                break;
                            case 2:
                                radiansAngle = new Vector2(500f, 0.0f).ToRadians();
                                break;
                            case 3:
                                radiansAngle = new Vector2(0.0f, 500f).ToRadians();
                                break;
                            default:
                                radiansAngle = new Vector2(0.0f, 0.0f).ToRadians();
                                break;
                        }
                        Vector2 distanceUsingRadians = MathExt.PointFromRadians(Vector2.Zero, (squad.Offset.ToRadians() + facing), squad.Offset.Length());
                        squad.Ships[index].FleetOffset = distanceUsingRadians + MathExt.PointFromRadians(Vector2.Zero, radiansAngle + facing, 500f);
                        distanceUsingRadians = MathExt.PointFromRadians(Vector2.Zero, radiansAngle, 500f);
                        squad.Ships[index].RelativeFleetOffset = squad.Offset + distanceUsingRadians;
                    }
                }
            }
        }

        public void AssignPositions(float facing)
        {
            this.facing = facing;
            foreach (Ship ship in Ships)
            {
                float angle      = ship.RelativeFleetOffset.ToRadians() + facing;
                float distance   = ship.RelativeFleetOffset.Length();
                ship.FleetOffset = MathExt.PointFromRadians(Vector2.Zero, angle, distance);
            }
        }

        public void AssembleFleet(float facing, Vector2 facingVec)
        {
            this.facing = facing;
            foreach (Ship ship in this.Ships)
            {
                if (ship.GetAI().State == AIState.AwaitingOrders || this.IsCoreFleet)
                {
                    float angle = ship.RelativeFleetOffset.ToRadians() + facing;
                    float distance = ship.RelativeFleetOffset.Length();
                    ship.FleetOffset = MathExt.PointFromRadians(Vector2.Zero, angle, distance);
                }
            }
        }

        public override void ProjectPos(Vector2 ProjectedPosition, float facing, Vector2 fVec)
        {
            this.ProjectedFacing = facing;
            foreach (Ship ship in Ships)
            {
                float angle = ship.RelativeFleetOffset.ToRadians() + facing;
                float distance = ship.RelativeFleetOffset.Length();
                ship.projectedPosition = ProjectedPosition + MathExt.PointFromRadians(Vector2.Zero, angle, distance);
            }
        }

        public Vector2 findAveragePosition()
        {
            if (StoredFleetPosition == Vector2.Zero)
                StoredFleetPosition = findAveragePositionset();
            else if (Ships.Count != 0)
                StoredFleetPosition = Ships[0].Center;
            return StoredFleetPosition;
        }
        
        public Vector2 findAveragePositionset()
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
            float sum = (float)distances.Sum(distance => Math.Pow(distance -avgdistance, 2));
            float stddev = (float)Math.Sqrt((sum) / (distances.Count  - 1));
            this.StoredFleetDistancetoMove = distances.Where(distance => distance <= avgdistance + stddev).Average();
        }

        public void Reset()
        {
            using (Ships.AcquireWriteLock())
            {
                foreach (Ship ship in Ships)
                    ship.fleet = null;
                Ships.Clear();
            }
            TaskStep = 0;
            Task = null;
            GoalStack.Clear();
        }

        private void EvaluateTask(float elapsedTime)
        {
            if (Ships.Count == 0)
                Task.EndTask();
            if (Task == null)
                return;
            switch (Task.type)
            {
                case MilitaryTask.TaskType.ClearAreaOfEnemies:         DoClearAreaOfEnemies(Task); break;
                case MilitaryTask.TaskType.AssaultPlanet:              DoAssaultPlanet(Task);      break;
                case MilitaryTask.TaskType.CorsairRaid:                DoCorsairRaid(elapsedTime); break;
                case MilitaryTask.TaskType.CohesiveClearAreaOfEnemies: DoCohesiveClearAreaOfEnemies(Task); break;
                case MilitaryTask.TaskType.Exploration:                DoExplorePlanet(Task); break;
                case MilitaryTask.TaskType.DefendSystem:               DoDefendSystem(Task); break;
                case MilitaryTask.TaskType.DefendClaim:                DoClaimDefense(Task); break;
                case MilitaryTask.TaskType.DefendPostInvasion:         DoPostInvasionDefense(Task); break;
                case MilitaryTask.TaskType.GlassPlanet:                DoGlassPlanet(Task); break;
            }
        }

        private void DoCorsairRaid(float elapsedTime)
        {
            if (this.TaskStep != 0)
                return;
            this.Task.TaskTimer -= elapsedTime;
            if (this.Task.TaskTimer <= 0.0)
            {
                Ship HomeBase = new Ship();
                foreach (Ship ship in Owner.GetShips())
                {
                    if (ship.Name == "Corsair Asteroid Base")
                    {
                        HomeBase = ship;
                        break;
                    }
                }
                if (HomeBase != null)
                {
                    this.AssembleFleet(0.0f, Vector2.One);
                    this.FormationWarpTo(HomeBase.Position, 0.0f, Vector2.One);
                    this.Task.EndTaskWithMove();
                }
                else
                    this.Task.EndTask();
            }
            if (this.Ships.Count == 0)
                this.Task.EndTask();
        }

        private void DoExplorePlanet(MilitaryTask Task) //Mer Gretman Left off here
        {
            Log.Info("DoExplorePlanet called!  " + this.Owner.PortraitName);
            bool eventBuildingFound = false;
            foreach (Building building in Task.GetTargetPlanet().BuildingList)
            {
                if (!string.IsNullOrEmpty(building.EventTriggerUID))
                {
                    eventBuildingFound = true;
                    break;
                }
            }

            bool weHaveTroops = false;
            if (!eventBuildingFound)    //No need to do this part if a task ending scenario has already been found -Gretman
            {
                using (this.Ships.AcquireReadLock())
                    foreach (Ship ship in this.Ships)
                    {
                        if (ship.TroopList.Count > 0)
                            weHaveTroops = true;
                    }
                if (!weHaveTroops)
                {
                    foreach (PlanetGridSquare planetGridSquare in Task.GetTargetPlanet().TilesList)
                    {
                        if (planetGridSquare.TroopsHere.Count > 0 && planetGridSquare.TroopsHere[0].GetOwner() == this.Owner)
                        {
                            weHaveTroops = true;
                            break;
                        }
                    }
                }
            }

            if (eventBuildingFound || !weHaveTroops || Task.GetTargetPlanet().Owner != null)
            {
                Task.EndTask();
            }
            else
            {
                switch (this.TaskStep)
                {
                    case 0:
                        Array<Planet> list1 = new Array<Planet>();
                        foreach (Planet planet in this.Owner.GetPlanets())
                        {
                            if (planet.HasShipyard) planetsWithShipyards.Add(planet);
                        }

                        Vector2 nearestShipyard = Vector2.Zero;
                        if (planetsWithShipyards.Count > 0)
                            nearestShipyard = planetsWithShipyards.FindMin(planet => Vector2.Distance(Task.AO, planet.Position)).Position;
                        else
                            break;

                        Vector2 fVec = Vector2.Normalize(Task.AO - nearestShipyard);
                        this.MoveToNow(nearestShipyard, nearestShipyard.RadiansToTarget(Task.AO), fVec);
                        foreach (Ship ship in Ships)
                        {
                            ship.GetAI().HasPriorityOrder = true;
                        }
                        this.TaskStep = 1;
                        break;
                    case 1:
                        bool fleetGathered = true;
                        bool fleetNotInCombat = true;
                        foreach (Ship ship in this.Ships)
                        {
                            if (!ship.disabled && ship.hasCommand && ship.Active)
                            {
                                if ((double)Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 5000.0)
                                    fleetGathered = false;
                                if (ship.InCombat)
                                    fleetNotInCombat = false;
                                if (!fleetGathered)
                                    break;
                            }
                        }
                        if (!fleetGathered && fleetNotInCombat)
                            break;
                        this.TaskStep = 2;
                        Vector2 MovePosition = Task.GetTargetPlanet().Position + Vector2.Normalize(this.findAveragePosition() - Task.GetTargetPlanet().Position) * 50000f;
                        this.Position = MovePosition;
                        this.FormationWarpTo(MovePosition, findAveragePosition().RadiansToTarget(Task.AO), Vector2.Normalize(Task.AO - this.findAveragePosition()));
                        break;
                    case 2:
                        bool fleetGathered2 = true;
                        foreach (Ship ship in (Array<Ship>)this.Ships)
                        {
                            ship.GetAI().HasPriorityOrder = false;
                            if (!ship.disabled && ship.hasCommand && ship.Active)
                            {
                                if ((double)Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 5000.0)
                                {
                                    fleetGathered = false;
                                    break;
                                }
                            }
                        }
                        if (!fleetGathered2)
                            break;
                        foreach (Ship ship in (Array<Ship>)this.Ships)
                        {
                            ship.GetAI().State = AIState.HoldPosition;
                            if (ship.shipData.Role == ShipData.RoleName.troop)
                                ship.GetAI().HoldPosition();
                        }
                        this.InterceptorDict.Clear();
                        this.TaskStep = 3;
                        break;
                    case 3:
                        this.EnemyClumpsDict.Clear();
                        Array<Ship> list2 = new Array<Ship>();
                        Array<GameplayObject> nearby1 = UniverseScreen.ShipSpatialManager.GetNearby(this.Position);
                        for (int index1 = 0; index1 < nearby1.Count; ++index1)
                        {
                            Ship ship1 = nearby1[index1] as Ship;
                            if (ship1 != null)
                            {
                                ship1.GetAI().HasPriorityOrder = false;
                                if (ship1.loyalty != this.Owner && (ship1.loyalty.isFaction || this.Owner.GetRelations(ship1.loyalty).AtWar) && (!list2.Contains(ship1) && (double)Vector2.Distance(ship1.Center, Task.AO) < (double)Task.AORadius && !this.EnemyClumpsDict.ContainsKey(ship1.Center)))
                                {
                                    this.EnemyClumpsDict.Add(ship1.Center, new Array<Ship>());
                                    this.EnemyClumpsDict[ship1.Center].Add(ship1);
                                    list2.Add(ship1);
                                    Array<GameplayObject> nearby2 = UniverseScreen.ShipSpatialManager.GetNearby(this.Position);
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
                            Array<Vector2> list3 = new Array<Vector2>();
                            foreach (KeyValuePair<Vector2, Array<Ship>> keyValuePair in this.EnemyClumpsDict)
                                list3.Add(keyValuePair.Key);
                            IOrderedEnumerable<Vector2> orderedEnumerable2 = Enumerable.OrderBy<Vector2, float>((IEnumerable<Vector2>)list3, (Func<Vector2, float>)(clumpPos => Vector2.Distance(this.findAveragePosition(), clumpPos)));
                            Array<Ship> list4 = new Array<Ship>();
                            foreach (Ship toAttack in this.EnemyClumpsDict[Enumerable.First<Vector2>((IEnumerable<Vector2>)orderedEnumerable2)])
                            {
                                float num = 0.0f;
                                foreach (Ship ship in (Array<Ship>)this.Ships)
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
                            Array<Ship> list5 = new Array<Ship>();
                            foreach (Ship ship in (Array<Ship>)this.Ships)
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
                        if (FleetReadyCheck())
                        {
                            this.TaskStep = 5;
                            break;
                        }
                        else
                        {
                            bool flag6 = false;
                            foreach (Ship ship in this.Ships)
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
                        foreach (Ship ship in (Array<Ship>)this.Ships)
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
            if (Task.GetTargetPlanet().Owner == this.Owner || Task.GetTargetPlanet().Owner == null || !this.Owner.GetRelations(Task.GetTargetPlanet().Owner).AtWar)
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
                    this.Owner.GetGSAI().TaskList.Add(Task);
                }
                else
                    Task.EndTask();
            }
            else
            {
                float totalStrength = 0.0f;
                foreach (Ship ship in (Array<Ship>)this.Ships)
                    totalStrength += ship.GetStrength();
                if (totalStrength == 0.0f)
                    Task.EndTask();
                int availabelTroops = 0;
                int assaultShips = 0;
                foreach (Ship ship in (Array<Ship>)this.Ships)
                {
                    if (ship.GetStrength() > 0.0f)
                        ++assaultShips;
                    availabelTroops += ship.PlanetAssaultCount;                    
                }
                if (availabelTroops == 0)
                {
                    foreach (Troop troop in Task.GetTargetPlanet().TroopsHere)
                    {
                        if (troop.GetOwner() == this.Owner)
                            ++availabelTroops;
                    }
                }
                if (availabelTroops == 0 || assaultShips == 0)
                {
                    if (assaultShips == 0)
                        Task.IsCoreFleetTask = false;
                    Task.EndTask();
                    this.Task = null;
                    this.TaskStep = 0;
                }
                else
                {
                    switch (this.TaskStep)
                    {
                        case -1:
                        case 0:
                            Array<Planet> list1 = new Array<Planet>();
                            //foreach (Planet planet in this.Owner.GetPlanets().OrderBy(combat => combat.ParentSystem.DangerTimer))
                            foreach (Planet planet in this.Owner.GetPlanets()
                                .OrderBy(combat => combat.ParentSystem.combatTimer < 30)
                                .ThenBy(shipyard => shipyard.HasShipyard)
                                .ThenBy(distance=> Vector2.Distance(distance.Position,this.Task.AO)))
                            {
                                bool flag = false;
                                foreach(Planet notsafe in planet.ParentSystem.PlanetList)
                                {
                                    if(notsafe.Owner != null && notsafe.Owner != this.Owner)
                                    {
                                        this.Owner.TryGetRelations(notsafe.Owner, out Relationship test);
                                            if(!test.Treaty_OpenBorders || !test.Treaty_NAPact)
                                            {
                                                flag = true;
                                                break;
                                            }
                                    }
                                    if (flag)
                                        break;
                                }
                                if(!flag)
                                list1.Add(planet);
                            }
                            
                            if (list1.Count > 0)
                            {
                                Planet goaltarget = list1.First();
                                Vector2 fVec = Vector2.Normalize(Task.AO - goaltarget.Position);
                                Vector2 vector2 = goaltarget.Position;
 
                                this.MoveToNow(vector2, vector2.RadiansToTarget(Task.AO), fVec);
        
                                this.TaskStep = 1;
                                break;
                            }
                            else
                            {
                                Task.EndTask();
                                break;
                            }
                        case 1:
                            bool nearFleet = true;
                            bool endTask = false;
                            using (Ships.AcquireReadLock())
                            {
                                foreach (Ship ship in Ships)
                                {
                                    if (ship.disabled || !ship.hasCommand || !ship.Active)
                                        continue;
                                    if (Vector2.Distance(ship.Center, Position + ship.FleetOffset) > 5000)
                                    {
                                        nearFleet = false;
                                        break;
                                    }
                                    if (ship.GetAI().BadGuysNear)
                                    {
                                        endTask = true;
                                        break;
                                    }
                                }
                            }

                            // this needs to be called outside of the read lock, because it'll acquire a write lock to Ships
                            if (endTask) 
                                Task.EndTask();

                            if (nearFleet)
                            {
                                this.TaskStep = 2;
                                Vector2 MovePosition = Task.GetTargetPlanet().Position + Vector2.Normalize(this.findAveragePosition() - Task.GetTargetPlanet().Position) * 125000f;
                                this.Position = MovePosition;
                                this.FormationWarpTo(MovePosition, findAveragePosition().RadiansToTarget(Task.AO), Vector2.Normalize(Task.AO - this.findAveragePosition()));
                            }
                            break;
                        case 2:
                            bool flag2 = true;
                            using (Ships.AcquireReadLock())
                            {
                                foreach (Ship ship in Ships)
                                {
                                    if (!ship.disabled && ship.hasCommand && ship.Active)
                                    {
                                        if (Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 25000)
                                        {
                                            flag2 = false;
                                            break;
                                        }
                                    }
                                }
                                if (!flag2)
                                    break;
                                foreach (Ship ship in Ships)
                                {
                                    ship.GetAI().HasPriorityOrder = false;
                                    ship.GetAI().State = AIState.HoldPosition;
                                    if (ship.BombBays.Count > 0)
                                        ship.GetAI().OrderBombardPlanet(Task.GetTargetPlanet());
                                    else if (ship.shipData.Role == ShipData.RoleName.troop)
                                        ship.GetAI().HoldPosition();
                                }
                            }
                            this.InterceptorDict.Clear();
                            this.TaskStep = 3;
                            break;
                        case 3:
                            if (FleetReadyCheck())
                            {
                                this.TaskStep = 5;
                                break;
                            }
                            else
                            {

                                foreach (Ship key in (Array<Ship>)Task.GetTargetPlanet().system.ShipList)
                                {
                                    if (key.loyalty != this.Owner && (key.loyalty.isFaction || this.Owner.GetRelations(key.loyalty).AtWar) && (Vector2.Distance(key.Center, Task.GetTargetPlanet().Position) < 15000 && !this.InterceptorDict.ContainsKey(key)))
                                        this.InterceptorDict.Add(key, new Array<Ship>());
                                }
                                Array<Ship> list2 = new Array<Ship>();
                                foreach (KeyValuePair<Ship, Array<Ship>> keyValuePair in this.InterceptorDict)
                                {
                                    Array<Ship> list3 = new Array<Ship>();
                                    if (Vector2.Distance(keyValuePair.Key.Center, Task.GetTargetPlanet().Position) > 20000 || !keyValuePair.Key.Active)
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
                                foreach (KeyValuePair<Ship, Array<Ship>> keyValuePair1 in this.InterceptorDict)
                                {
                                    Array<Ship> list3 = new Array<Ship>();
                                    foreach (Ship ship in (Array<Ship>)this.Ships)
                                    {
                                        if (ship.shipData.Role != ShipData.RoleName.troop)
                                            list3.Add(ship);
                                    }
                                    Array<Ship> list4 = new Array<Ship>();
                                    foreach (KeyValuePair<Ship, Array<Ship>> keyValuePair2 in this.InterceptorDict)
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
                                            if (num4 != 0.0f)
                                            {
                                                if (num4 >= toAttack.GetStrength() * 1.5f)
                                                    break;
                                            }
                                            ship.GetAI().hasPriorityTarget = true;
                                            ship.GetAI().Intercepting = true;
                                            list3.Remove(ship);
                                            ship.GetAI().OrderAttackSpecificTarget(toAttack);
                                            this.InterceptorDict[toAttack].Add(ship);
                                            num4 += ship.GetStrength();
                                        }
                                    }
                                }
                                if (this.InterceptorDict.Count == 0 || this.Owner.GetGSAI().ThreatMatrix.PingRadarStr(Task.GetTargetPlanet().Position, 25000f, this.Owner) < 500)
                                    this.TaskStep = 4;

                                using (Owner.GetGSAI().TaskList.AcquireReadLock())
                                using (Array<MilitaryTask>.Enumerator resource_0 = this.Owner.GetGSAI().TaskList.GetEnumerator())
                                {
                                    while (resource_0.MoveNext())
                                    {
                                        MilitaryTask local_43 = resource_0.Current;
                                        if (local_43.WaitForCommand && local_43.GetTargetPlanet() != null && local_43.GetTargetPlanet() == Task.GetTargetPlanet())
                                            local_43.WaitForCommand = false;
                                    }
                                }
                            } 
                            break;
                        case 4:
                            int num10 = 0;
                            float groundStrength = this.GetGroundStrOfPlanet(Task.GetTargetPlanet());
                            float ourGroundStrength = this.Task.GetTargetPlanet().GetGroundStrength(this.Owner);
                            if (groundStrength > 30 && ourGroundStrength == 0 )
                            {
                                foreach (Ship ship in (Array<Ship>)this.Ships)
                                {
                                    if ( ship.BombBays.Count > 0 && ship.Ordinance / ship.OrdinanceMax > 0.2f )
                                    {
                                        num10 += ship.BombBays.Count;
                                        ship.GetAI().OrderBombardPlanet(Task.GetTargetPlanet());
                                    }
                                } 
                            }
                            if (FleetReadyCheck())
                            {
                                this.TaskStep = 5;
                                break;
                            }
                            else
                            {
                                bool flag3 = false;
                                int availableBombs = 0;
                                int availableBombers = 0;
                                float num4 = 0.0f;                                
                                foreach (Ship ship in (Array<Ship>)this.Ships)
                                {
                                    num4 +=ship.PlanetAssaultStrength;
                                }
                                num4 += ourGroundStrength;
                                if (ourGroundStrength >0 || num4 > groundStrength && Task.GetTargetPlanet().GetGroundLandingSpots() >8)
                                {
                                    flag3 = true;
                                }
                                else
                                {                                    
                                    foreach (Ship ship in (Array<Ship>)this.Ships)
                                    {
                                        int bombs = ship.BombCount;
                                        availableBombs += ship.BombCount;
                                        availableBombers += bombs > 0 ? 1 : 0;
                                  
                                    }                                    
                                }
                                if (flag3)
                                {
                                    foreach (Ship ship in (Array<Ship>)this.Ships)
                                    {
                                        if ( ship.GetAI().State == AIState.Bombard && ship.BombBays.Count > 0 )
                                        {
                                            ship.GetAI().State = AIState.AwaitingOrders;
                                            ship.GetAI().OrderQueue.Clear();
                                        }
                                    }
                                    this.Position = Task.GetTargetPlanet().Position;                                    
                                    this.AssembleFleet(this.facing, Vector2.Normalize(this.Position - this.findAveragePosition()));
                                    using (Array<Ship>.Enumerator enumerator = this.Ships.GetEnumerator())
                                    {
                                        while (enumerator.MoveNext())
                                        {
                                            if (!enumerator.Current.GetAI().HasPriorityOrder && enumerator.Current.ReadyPlanetAssaulttTroops > 0)
                                            {
                                                enumerator.Current.GetAI().OrderLandAllTroops(Task.GetTargetPlanet());
                                                enumerator.Current.GetAI().HasPriorityOrder = true;
                                            }
                                        }
                                    }
                                }
                                else if (availableBombs > 0)
                                {
                                    using (Ships.AcquireReadLock())
                                    using (Array<Ship>.Enumerator enumerator = this.Ships.GetEnumerator())
                                    {
                                        while (enumerator.MoveNext())
                                        {
                                            Ship current = enumerator.Current;
                                            if (current.BombBays.Count > 0)
                                            {
                                                current.GetAI().OrderBombardPlanet(Task.GetTargetPlanet());
                                                //enumerator.Current.GetAI().HasPriorityOrder = true;
                                            }
                                        }
                                        
                                    }
                                }
                                else if(availableBombers > 0)
                                {
                                    this.TaskStep = 5;
                                }
                                else
                                {
                                    Task.EndTask();
                                    break;
                                }
                                break;
                            }
                        case 5:
                            Array<Planet> list5 = new Array<Planet>();
                            foreach (Planet planet in this.Owner.GetPlanets())
                            {
                                if (planet.HasShipyard)
                                    list5.Add(planet);
                            }
                            IOrderedEnumerable<Planet> orderedEnumerable3 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)list5, (Func<Planet, float>)(p => Vector2.Distance(this.Position, p.Position)));
                            if (Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable3) > 0)
                            {
                                this.Position = Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable3).Position;
                                foreach (Ship ship in (Array<Ship>)this.Ships)
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
                            foreach (Ship ship in (Array<Ship>)this.Ships)
                            {
                                ship.GetAI().HasPriorityOrder = true;
                                num17 += ship.Ordinance;
                                num18 += ship.OrdinanceMax;
                            }
                            if (num18 >0 && num17 < num18 * 0.89f)
                                break;
                            this.TaskStep = 0;
                            break;
                    }
                }
            }
        }

        private bool FleetReadyCheck()
        {
            float currentAmmo = 0.0f;
            float maxAmmo = 0.0f;
            float ammoDPS = 0.0f;
            float energyDPS = 0.0f;
            foreach (Ship ship in this.Ships)
            {
                currentAmmo += ship.Ordinance;
                maxAmmo += ship.OrdinanceMax;
                foreach (Weapon weapon in ship.Weapons)
                {
                    if (weapon.OrdinanceRequiredToFire > 0.0)
                        ammoDPS = weapon.DamageAmount / weapon.fireDelay;
                    if (weapon.PowerRequiredToFire > 0.0)
                        energyDPS = weapon.DamageAmount / weapon.fireDelay;
                }
            }
            if (ammoDPS >= (ammoDPS + energyDPS) * 0.5f && currentAmmo <= maxAmmo * 0.1f)
            {
                return true;
            }
            return false;
        }

        private float GetGroundStrOfPlanet(Planet p)
        {
            return p.GetGroundStrengthOther(this.Owner);
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
                        foreach (Ship ship in (Array<Ship>)this.Ships)
                        {
                            if (!ship.disabled && ship.hasCommand && ship.Active)
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
                        this.FormationWarpTo(Task.AO, findAveragePosition().RadiansToTarget(Task.AO), Vector2.Normalize(Task.AO - this.findAveragePosition()));
                        using (Array<Ship>.Enumerator enumerator = this.Ships.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                                enumerator.Current.GetAI().HasPriorityOrder = true;
                            break;
                        }
                    case 0:
                        Array<Planet> list1 = new Array<Planet>();
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
                        this.MoveToNow(vector2, vector2.RadiansToTarget(Task.AO), fVec);
                        this.TaskStep = 1;
                        break;
                    case 1:
                        bool flag2 = true;
                        foreach (Ship ship in (Array<Ship>)this.Ships)
                        {
                            if (!ship.disabled && ship.hasCommand && ship.Active)
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
                        this.FormationWarpTo(Task.AO, findAveragePosition().RadiansToTarget(Task.AO), Vector2.Normalize(Task.AO - this.findAveragePosition()));
                        using (Array<Ship>.Enumerator enumerator = this.Ships.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                                enumerator.Current.GetAI().HasPriorityOrder = true;
                            break;
                        }
                    case 2:
                        bool flag3 = false;
                        if ((double)Vector2.Distance(this.findAveragePosition(), Task.AO) < 15000.0)
                        {
                            foreach (Ship ship in (Array<Ship>)this.Ships)
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
                        Array<Ship> list2 = new Array<Ship>();
                        Array<GameplayObject> nearby1 = UniverseScreen.ShipSpatialManager.GetNearby(this.Position);
                        for (int index1 = 0; index1 < nearby1.Count; ++index1)
                        {
                            Ship ship1 = nearby1[index1] as Ship;
                            if (ship1 != null && ship1.loyalty != this.Owner && (ship1.loyalty.isFaction || this.Owner.GetRelations(ship1.loyalty).AtWar || this.Owner.isFaction) && (!list2.Contains(ship1) && (double)Vector2.Distance(ship1.Center, Task.AO) < (double)Task.AORadius && !this.EnemyClumpsDict.ContainsKey(ship1.Center)))
                            {
                                this.EnemyClumpsDict.Add(ship1.Center, new Array<Ship>());
                                this.EnemyClumpsDict[ship1.Center].Add(ship1);
                                list2.Add(ship1);
                                Array<GameplayObject> nearby2 = UniverseScreen.ShipSpatialManager.GetNearby(this.Position);
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
                            Array<Vector2> list3 = new Array<Vector2>();
                            foreach (KeyValuePair<Vector2, Array<Ship>> keyValuePair in this.EnemyClumpsDict)
                                list3.Add(keyValuePair.Key);
                            IOrderedEnumerable<Vector2> orderedEnumerable2 = Enumerable.OrderBy<Vector2, float>((IEnumerable<Vector2>)list3, (Func<Vector2, float>)(clumpPos => Vector2.Distance(this.findAveragePosition(), clumpPos)));
                            Array<Ship> list4 = new Array<Ship>();
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
                            Array<Ship> list5 = new Array<Ship>();
                            foreach (Ship ship in (Array<Ship>)this.Ships)
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
                        if (FleetReadyCheck())
                        {
                            this.TaskStep = 5;
                            break;
                        }
                        else
                        {
                            bool flag4 = false;
                            foreach (Ship ship in (Array<Ship>)this.Ships)
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
                        Array<Planet> list6 = new Array<Planet>();
                        foreach (Planet planet in this.Owner.GetPlanets())
                        {
                            if (planet.HasShipyard)
                                list6.Add(planet);
                        }
                        IOrderedEnumerable<Planet> orderedEnumerable3 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)list6, (Func<Planet, float>)(p => Vector2.Distance(this.Position, p.Position)));
                        if (Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable3) <= 0)
                            break;
                        this.Position = Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable3).Position;
                        foreach (Ship ship in (Array<Ship>)this.Ships)
                            ship.GetAI().OrderResupply(Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable3), true);
                        this.TaskStep = 6;
                        break;
                    case 6:
                        float num6 = 0.0f;
                        float num7 = 0.0f;
                        foreach (Ship ship in (Array<Ship>)this.Ships)
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
                    foreach (Ship ship in (Array<Ship>)this.Ships)
                    {
                        if (!ship.disabled && ship.hasCommand && ship.Active)
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
                    this.FormationWarpTo(Task.AO, findAveragePosition().RadiansToTarget(Task.AO), Vector2.Normalize(Task.AO - this.findAveragePosition()));
                    using (Array<Ship>.Enumerator enumerator = this.Ships.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                            enumerator.Current.GetAI().HasPriorityOrder = true;
                        break;
                    }
                case 0:
                    Array<Planet> list1 = new Array<Planet>();
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
                    this.MoveToNow(vector2, vector2.RadiansToTarget(Task.AO), fVec);
                    this.TaskStep = 1;
                    break;
                case 1:
                    bool flag2 = true;
                    foreach (Ship ship in (Array<Ship>)this.Ships)
                    {
                        if (!ship.disabled && ship.hasCommand && ship.Active)
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
                    this.FormationWarpTo(Task.AO, findAveragePosition().RadiansToTarget(Task.AO), Vector2.Normalize(Task.AO - this.findAveragePosition()));
                    using (Array<Ship>.Enumerator enumerator = this.Ships.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                            enumerator.Current.GetAI().HasPriorityOrder = true;
                        break;
                    }
                case 2:
                    bool flag3 = false;
                    if ((double)Vector2.Distance(this.findAveragePosition(), Task.AO) < 15000.0)
                    {
                        foreach (Ship ship in (Array<Ship>)this.Ships)
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
                    Array<Ship> list2 = new Array<Ship>();
                    Array<GameplayObject> nearby1 = UniverseScreen.ShipSpatialManager.GetNearby(this.Position);
                    for (int index1 = 0; index1 < nearby1.Count; ++index1)
                    {
                        Ship ship1 = nearby1[index1] as Ship;
                        if (ship1 != null && ship1.loyalty != this.Owner && (ship1.loyalty.isFaction || this.Owner.GetRelations(ship1.loyalty).AtWar || this.Owner.isFaction) && (!list2.Contains(ship1) && (double)Vector2.Distance(ship1.Center, Task.AO) < (double)Task.AORadius && !this.EnemyClumpsDict.ContainsKey(ship1.Center)))
                        {
                            this.EnemyClumpsDict.Add(ship1.Center, new Array<Ship>());
                            this.EnemyClumpsDict[ship1.Center].Add(ship1);
                            list2.Add(ship1);
                            Array<GameplayObject> nearby2 = UniverseScreen.ShipSpatialManager.GetNearby(this.Position);
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
                        Array<Vector2> list3 = new Array<Vector2>();
                        foreach (KeyValuePair<Vector2, Array<Ship>> keyValuePair in this.EnemyClumpsDict)
                            list3.Add(keyValuePair.Key);
                        IOrderedEnumerable<Vector2> orderedEnumerable2 = Enumerable.OrderBy<Vector2, float>((IEnumerable<Vector2>)list3, (Func<Vector2, float>)(clumpPos => Vector2.Distance(this.findAveragePosition(), clumpPos)));
                        Array<Ship> list4 = new Array<Ship>();
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
                        Array<Ship> list5 = new Array<Ship>();
                        foreach (Ship ship in (Array<Ship>)this.Ships)
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
                    if (FleetReadyCheck())
                    {
                        this.TaskStep = 5;
                        break;
                    }
                    else
                    {
                        bool flag4 = false;
                        foreach (Ship ship in (Array<Ship>)this.Ships)
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
                    Array<Planet> list6 = new Array<Planet>();
                    foreach (Planet planet in this.Owner.GetPlanets())
                    {
                        if (planet.HasShipyard)
                            list6.Add(planet);
                    }
                    IOrderedEnumerable<Planet> orderedEnumerable3 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)list6, (Func<Planet, float>)(p => Vector2.Distance(this.Position, p.Position)));
                    if (Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable3) <= 0)
                        break;
                    this.Position = Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable3).Position;
                    foreach (Ship ship in (Array<Ship>)this.Ships)
                        ship.GetAI().OrderResupply(Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable3), true);
                    this.TaskStep = 6;
                    break;
                case 6:
                    float num6 = 0.0f;
                    float num7 = 0.0f;
                    foreach (Ship ship in (Array<Ship>)this.Ships)
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
                    Array<Planet> list1 = new Array<Planet>();
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
                    this.MoveToNow(vector2, vector2.RadiansToTarget(Task.GetTargetPlanet().Position), fVec);
                    this.TaskStep = 1;
                    break;
                case 1:
                    bool flag1 = true;
                    foreach (Ship ship in (Array<Ship>)this.Ships)
                    {
                        if (!ship.disabled && ship.hasCommand && ship.Active)
                        {
                            if (Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 5000f)
                                flag1 = false;
                            int num = ship.InCombat ? 1 : 0;
                            if (!flag1)
                                break;
                        }
                    }
                    if (!flag1)
                        break;
                    this.TaskStep = 2;
                    this.FormationWarpTo(Task.GetTargetPlanet().Position, findAveragePosition().RadiansToTarget(Task.GetTargetPlanet().Position), Vector2.Normalize(Task.GetTargetPlanet().Position - this.findAveragePosition()));
                    using (Array<Ship>.Enumerator enumerator = this.Ships.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                            enumerator.Current.GetAI().HasPriorityOrder = true;
                        break;
                    }
                case 2:
                    bool flag2 = false;
                    if (Vector2.Distance(this.findAveragePosition(), Task.GetTargetPlanet().Position) < 15000f)
                    {
                        foreach (Ship ship in (Array<Ship>)this.Ships)
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
                    if (!flag2 && Vector2.Distance(this.findAveragePosition(), Task.GetTargetPlanet().Position) >= 5000f)
                        break;
                    this.TaskStep = 3;
                    break;
                case 3:                    
                    this.EnemyClumpsDict = this.Owner.GetGSAI().ThreatMatrix.PingRadarClusters(this.Task.GetTargetPlanet().Position, 150000,10000,this.Owner);
                    #if false
                        this.EnemyClumpsDict.Clear();
                        Array<Ship> list2 = new Array<Ship>();

                        Array<Ship> nearby1; // = UniverseScreen.ShipSpatialManager.GetNearby(this.Position);
                        nearby1 = this.Owner.GetGSAI().ThreatMatrix.PingRadarShip(this.Task.GetTargetPlanet().Position, 150000f);
                        for (int index1 = 0; index1 < nearby1.Count; ++index1)
                        {
                            Ship ship1 = nearby1[index1];
                            if (ship1 != null)
                            {
                                if (list2.Contains(ship1))
                                    continue;
                                //ship1.GetAI().HasPriorityOrder = false;
                                if (ship1.loyalty != this.Owner
                                    && (ship1.loyalty.isFaction || this.Owner.GetRelations()[ship1.loyalty].AtWar || ship1.isColonyShip && !this.Owner.GetRelations()[ship1.loyalty].Treaty_OpenBorders)
                                    //&& (!list2.Contains(ship1) && (double)Vector2.Distance(ship1.Center, Task.GetTargetPlanet().Position) < (double)Task.AORadius && !this.EnemyClumpsDict.ContainsKey(ship1.Center)))
                                    )

                                {
                                    Array<Ship> areaPing = this.Owner.GetGSAI().ThreatMatrix.PingRadarShip(ship1.Center, 10000f);
                                    if (areaPing.Count > 0)
                                    {
                                        this.EnemyClumpsDict.Add(ship1.Center, new Array<Ship>(areaPing));
                                        //this.EnemyClumpsDict[ship1.Center].Add(ship1);
                                        list2.AddRange(this.EnemyClumpsDict[ship1.Center]);
                                    }
                                    //Array<GameplayObject> nearby2 = UniverseScreen.ShipSpatialManager.GetNearby(this.Position);
                                    //for (int index2 = 0; index2 < nearby2.Count; ++index2)
                                    //{
                                    //    Ship ship2 = nearby2[index2] as Ship;
                                    //    if (ship2 != null && ship2.loyalty != this.Owner && (ship2.loyalty == ship1.loyalty && (double)Vector2.Distance(ship1.Center, ship2.Center) < 10000.0) && !list2.Contains(ship2))
                                    //        this.EnemyClumpsDict[ship1.Center].Add(ship2);
                                    //}
                                }
                            }
                        }
                    #endif
                    if (this.EnemyClumpsDict.Count == 0)
                    {
                        using (Array<Ship>.Enumerator enumerator = this.Ships.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                Ship current = enumerator.Current;
                                if (!current.InCombat)
                                {
                                    if (!(current.GetAI().State == AIState.Orbit))
                                        current.GetAI().OrderOrbitPlanet(Task.GetTargetPlanet());
                                    else if (current.GetAI().State == AIState.Orbit && (current.GetAI().OrbitTarget == null || current.GetAI().OrbitTarget != null && current.GetAI().OrbitTarget != Task.GetTargetPlanet()))
                                        current.GetAI().OrderOrbitPlanet(Task.GetTargetPlanet());
                                }
                                else // if (current.GetAI().TargetQueue.Count == 0)
                                {
                                    current.GetAI().OrderMoveDirectlyTowardsPosition(this.Task.GetTargetPlanet().ParentSystem.Position, 0, Vector2.Zero, true);
                                    current.GetAI().HasPriorityOrder = true;
                                }

                            }
                            break;
                        }
                    }
                    else
                    {
                        Array<Vector2> list3 = new Array<Vector2>();
                        foreach (KeyValuePair<Vector2, Array<Ship>> keyValuePair in this.EnemyClumpsDict)
                            list3.Add(keyValuePair.Key);
                        IOrderedEnumerable<Vector2> orderedEnumerable2 = Enumerable.OrderBy<Vector2, float>((IEnumerable<Vector2>)list3, (Func<Vector2, float>)(clumpPos => Vector2.Distance(this.findAveragePosition(), clumpPos)));
                        Array<Ship> list4 = new Array<Ship>();
                        foreach (Ship toAttack in this.EnemyClumpsDict[Enumerable.First<Vector2>((IEnumerable<Vector2>)orderedEnumerable2)])
                        {
                            float num = 0.0f;
                            foreach (Ship ship in (Array<Ship>)this.Ships)
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
                        Array<Ship> list5 = new Array<Ship>();
                        foreach (Ship ship in (Array<Ship>)this.Ships)
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
                    foreach (Ship ship in (Array<Ship>)this.Ships)
                    {
                        if (Vector2.Distance(ship.Center, this.Task.GetTargetPlanet().ParentSystem.Position) > 150000f)
                        {
                            
                            ship.GetAI().OrderQueue.Clear();
                            ship.GetAI().OrderMoveDirectlyTowardsPosition(this.Task.GetTargetPlanet().ParentSystem.Position, 0, Vector2.Zero, true);
                            //ship.GetAI().HasPriorityOrder = true;

                        }
                        num1 += ship.Ordinance;
                        num2 += ship.OrdinanceMax;
                        foreach (Weapon weapon in ship.Weapons)
                        {
                            if (weapon.OrdinanceRequiredToFire > 0)
                                num3 = weapon.DamageAmount / weapon.fireDelay;
                            if (weapon.PowerRequiredToFire > 0.0)
                                num4 = weapon.DamageAmount / weapon.fireDelay;
                        }
                    }
                    float num5 = num3 + num4;
                    if (num3 >= 0.5 * num5 && num1 <= 0.1 * num2)
                    {
                        this.TaskStep = 5;
                        break;
                    }
                    else
                    {
                        bool flag3 = false;
                        foreach (Ship ship in (Array<Ship>)this.Ships)
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
                    Array<Planet> list6 = new Array<Planet>();
                    foreach (Planet planet in this.Owner.GetPlanets())
                    {
                        if (planet.HasShipyard)
                            list6.Add(planet);
                    }
                    IOrderedEnumerable<Planet> orderedEnumerable3 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)list6, (Func<Planet, float>)(p => Vector2.Distance(this.Position, p.Position)));
                    if (Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable3) <= 0)
                        break;
                    this.Position = Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable3).Position;
                    foreach (Ship ship in (Array<Ship>)this.Ships)
                        ship.GetAI().OrderResupply(Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable3), true);
                    this.TaskStep = 6;
                    break;
                case 6:
                    float num6 = 0.0f;
                    float num7 = 0.0f;
                    foreach (Ship ship in (Array<Ship>)this.Ships)
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

        private void DoCohesiveClearAreaOfEnemies(MilitaryTask Task)
        {
            switch (this.TaskStep)
            {
                case 0:
                    this.TaskStep = 1;
                    this.DoCohesiveClearAreaOfEnemies(Task);
                    break;
                case 1:
                   // Array<ThreatMatrix.Pin> list1 = new Array<ThreatMatrix.Pin>();
                    Map<Vector2, float> threatDict = this.Owner.GetGSAI().ThreatMatrix.PingRadarThreatClusters(this.Task.AO, this.Task.AORadius, 10000f, this.Owner);
                    float strength = this.GetStrength();
                    this.targetPosition = Vector2.Zero;
                    
                    if (threatDict.Count != 0)
                    {
                        KeyValuePair<Vector2, float> targetSpot = threatDict
                            .OrderByDescending(p => p.Value < strength * .9f)
                            .ThenByDescending(p => p.Value).First();
                        if (targetSpot.Value < strength)
                            targetPosition = targetSpot.Key;
                    }
                    if (this.targetPosition != Vector2.Zero)
                    {
                        Vector2 fvec = Vector2.Normalize(Task.AO - this.targetPosition);
                        this.FormationWarpTo(this.targetPosition, targetPosition.RadiansToTarget(Task.AO), fvec);
                        this.TaskStep = 2;
                        break;
                    }
                    else
                    {
                        this.Task.EndTask();
                        break;
                    }
                case 2:

                    if (this.Owner.GetGSAI().ThreatMatrix.PingRadarStr(this.targetPosition, 150000, this.Owner) == 0)
                    {
                        this.TaskStep = 1;
                        break;
                    }
                    else
                    {
                        
                        if (Vector2.Distance(this.targetPosition, this.findAveragePosition()) > 25000)
                            break;
                        this.TaskStep = 3;
                        break;
                    }
                case 3:
                    this.EnemyClumpsDict = this.Owner.GetGSAI().ThreatMatrix.PingRadarClusters(this.Position, 150000, 10000, this.Owner);
                   
                    if (this.EnemyClumpsDict.Count == 0)
                    {
                        Task.EndTask();
                        break;
                    }
                    else
                    {
                        Array<Vector2> list3 = new Array<Vector2>();
                        foreach (KeyValuePair<Vector2, Array<Ship>> keyValuePair in this.EnemyClumpsDict)
                            list3.Add(keyValuePair.Key);
                        IOrderedEnumerable<Vector2> orderedEnumerable = Enumerable.OrderBy<Vector2, float>((IEnumerable<Vector2>)list3, (Func<Vector2, float>)(clumpPos => Vector2.Distance(this.findAveragePosition(), clumpPos)));
                        Array<Ship> list4 = new Array<Ship>();
                        foreach (Ship toAttack in this.EnemyClumpsDict[Enumerable.First<Vector2>((IEnumerable<Vector2>)orderedEnumerable)])
                        {
                            float num = 0.0f;
                            foreach (Ship ship in (Array<Ship>)this.Ships)
                            {
                                if (!list4.Contains(ship) && (num == 0 || num < toAttack.GetStrength()))
                                {
                                    ship.GetAI().Intercepting = true;
                                    ship.GetAI().OrderAttackSpecificTarget(toAttack);
                                    list4.Add(ship);
                                    num += ship.GetStrength();
                                }
                            }
                        }
                        Array<Ship> list5 = new Array<Ship>();
                        foreach (Ship ship in (Array<Ship>)this.Ships)
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
                    if (FleetReadyCheck())
                    {
                        this.TaskStep = 5;
                        break;
                    }

                    bool AllInCombat = true;
                    foreach (Ship ship in (Array<Ship>)this.Ships)
                    {
                        if (ship.GetAI().BadGuysNear && !ship.isInDeepSpace)
                        {
                            AllInCombat = false;
                            break;
                        }
                    }
                    if (this.Owner.GetGSAI().ThreatMatrix.PingRadarStr(this.Position, 150000, this.Owner) > 0)
                    {
                        
                        if(!AllInCombat )
                        {
                            this.TaskStep = 3;
                            break;
                        }
                        this.TaskStep = 4;
                        break;
                    }
                    else
                    {
                        this.TaskStep = 2;
                        break;
                    }
                       
                    
                    
                case 5:
                    foreach (Ship ship in (Array<Ship>)this.Ships)
                        ship.GetAI().OrderResupplyNearest(true);
                    this.TaskStep = 6;
                    break;
                case 6:
                    float num6 = 0.0f;
                    float num7 = 0.0f;
                    foreach (Ship ship in (Array<Ship>)this.Ships)
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
                    if (num6 != num7)
                        break;
                    this.TaskStep = 1;
                    break;
            }
        }

        private void DoGlassPlanet(MilitaryTask Task)
        {
            if (Task.GetTargetPlanet().Owner == this.Owner || Task.GetTargetPlanet().Owner == null)
                Task.EndTask();
            else if (Task.GetTargetPlanet().Owner != null & Task.GetTargetPlanet().Owner != this.Owner && !Task.GetTargetPlanet().Owner.GetRelations(this.Owner).AtWar)
            {
                Task.EndTask();
            }
            else
            {
                switch (this.TaskStep)
                {
                    case 0:
                        Array<Planet> list1 = new Array<Planet>();
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
                        this.MoveToNow(vector2, vector2.RadiansToTarget(Task.AO), fVec);
                        this.TaskStep = 1;
                        break;
                    case 1:
                        bool flag = true;
                        foreach (Ship ship in (Array<Ship>)this.Ships)
                        {
                            if (!ship.disabled && ship.hasCommand && ship.Active)
                            {
                                if (Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 15000.0)
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
                        this.FormationWarpTo(MovePosition, findAveragePosition().RadiansToTarget(Task.AO), Vector2.Normalize(Task.AO - this.findAveragePosition()));
                        foreach (Ship ship in (Array<Ship>)this.Ships)
                            ship.GetAI().HasPriorityOrder = true;
                        this.TaskStep = 2;
                        break;
                    case 2:
                        if (Task.WaitForCommand && (double)this.Owner.GetGSAI().ThreatMatrix.PingRadarStr(Task.GetTargetPlanet().Position, 30000f, this.Owner) > 250.0)
                            break;
                        foreach (Ship ship in (Array<Ship>)this.Ships)
                            ship.GetAI().OrderBombardPlanet(Task.GetTargetPlanet());
                        this.TaskStep = 4;
                        break;
                    case 4:
                        if (FleetReadyCheck())
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
                        Array<Planet> list2 = new Array<Planet>();
                        foreach (Planet planet in this.Owner.GetPlanets())
                        {
                            if (planet.HasShipyard)
                                list2.Add(planet);
                        }
                        IOrderedEnumerable<Planet> orderedEnumerable2 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)list2, (Func<Planet, float>)(p => Vector2.Distance(this.Position, p.Position)));
                        if (Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable2) <= 0)
                            break;
                        this.Position = Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable2).Position;
                        foreach (Ship ship in (Array<Ship>)this.Ships)
                            ship.GetAI().OrderResupply(Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable2), true);
                        this.TaskStep = 6;
                        break;
                    case 6:
                        float num6 = 0.0f;
                        float num7 = 0.0f;
                        foreach (Ship ship in (Array<Ship>)this.Ships)
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
                        if (num6 != num7)
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
                    Array<Planet> list1 = new Array<Planet>();
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
                    this.MoveToNow(vector2, vector2.RadiansToTarget(Task.AO), fVec);
                    this.TaskStep = 1;
                    break;
                case 1:
                    bool flag1 = true;
                    foreach (Ship ship in (Array<Ship>)this.Ships)
                    {
                        if (!ship.disabled && ship.hasCommand && ship.Active)
                        {
                            if (Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 5000.0)
                                flag1 = false;

                            if (!flag1)
                            {
                                if (ship.isInDeepSpace && ship.engineState != Ship.MoveState.Warp && ship.speed ==0 && ship.InCombatTimer <15)
                                    this.Task.EndTask();
                                break;
                            }
                        }
                    }
                    if (!flag1)
                        break;
                    this.TaskStep = 2;
                    this.FormationWarpTo(Task.AO, findAveragePosition().RadiansToTarget(Task.AO), Vector2.Normalize(Task.AO - this.findAveragePosition()));
                    using (Array<Ship>.Enumerator enumerator = this.Ships.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                            enumerator.Current.GetAI().HasPriorityOrder = true;
                        break;
                    }
                case 2:
                    if (FleetReadyCheck())
                    {
                        this.TaskStep = 5;
                        break;
                    }
                    else
                    {
                        bool flag2 = false;
                        if (Vector2.Distance(this.findAveragePosition(), Task.AO) < 15000.0)
                        {
                            foreach (Ship ship in (Array<Ship>)this.Ships)
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
                        if (!flag2 && Vector2.Distance(this.findAveragePosition(), Task.AO) >= 10000.0)
                            break;
                        this.TaskStep = 3;
                        break;
                    }
                case 3:
                    this.EnemyClumpsDict.Clear();
                    Array<Ship> list2 = new Array<Ship>();
                    Array<GameplayObject> nearby1 = UniverseScreen.ShipSpatialManager.GetNearby((GameplayObject)this.Ships[0]);
                    for (int index1 = 0; index1 < nearby1.Count; ++index1)
                    {
                        Ship ship1 = nearby1[index1] as Ship;
                        if (ship1 != null && ship1.loyalty != this.Owner && (ship1.loyalty.isFaction || this.Owner.GetRelations(ship1.loyalty).AtWar) && (!list2.Contains(ship1) && (double)Vector2.Distance(ship1.Center, Task.AO) < (double)Task.AORadius && !this.EnemyClumpsDict.ContainsKey(ship1.Center)))
                        {
                            this.EnemyClumpsDict.Add(ship1.Center, new Array<Ship>());
                            this.EnemyClumpsDict[ship1.Center].Add(ship1);
                            list2.Add(ship1);
                            Array<GameplayObject> nearby2 = UniverseScreen.ShipSpatialManager.GetNearby((GameplayObject)this.Ships[0]);
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
                            this.MoveDirectlyNow(enemyWithinRadius, findAveragePosition().RadiansToTarget(enemyWithinRadius), Vector2.Normalize(enemyWithinRadius - this.Position));
                            this.TaskStep = 2;
                            break;
                        }
                    }
                    else
                    {
                        Array<Vector2> list3 = new Array<Vector2>();
                        foreach (KeyValuePair<Vector2, Array<Ship>> keyValuePair in this.EnemyClumpsDict)
                            list3.Add(keyValuePair.Key);
                        IOrderedEnumerable<Vector2> orderedEnumerable2 = Enumerable.OrderBy<Vector2, float>((IEnumerable<Vector2>)list3, (Func<Vector2, float>)(clumpPos => Vector2.Distance(this.findAveragePosition(), clumpPos)));
                        Array<Ship> list4 = new Array<Ship>();
                        foreach (Ship toAttack in this.EnemyClumpsDict[Enumerable.First<Vector2>((IEnumerable<Vector2>)orderedEnumerable2)])
                        {
                            float num6 = 0.0f;
                            foreach (Ship ship in (Array<Ship>)this.Ships)
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
                        Array<Ship> list5 = new Array<Ship>();
                        foreach (Ship ship in (Array<Ship>)this.Ships)
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
                    if (FleetReadyCheck())
                    {
                        this.TaskStep = 5;
                        break;
                    }
                    else
                    {
                        bool flag2 = false;
                        foreach (Ship ship in (Array<Ship>)this.Ships)
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
                    Array<Planet> list6 = new Array<Planet>();
                    foreach (Planet planet in this.Owner.GetPlanets())
                    {
                        if (planet.HasShipyard)
                            list6.Add(planet);
                    }
                    IOrderedEnumerable<Planet> orderedEnumerable3 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)list6, (Func<Planet, float>)(p => Vector2.Distance(this.Position, p.Position)));
                    if (Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable3) <= 0)
                        break;
                    this.Position = Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable3).Position;
                    foreach (Ship ship in (Array<Ship>)this.Ships)
                        ship.GetAI().OrderResupply(Enumerable.First<Planet>((IEnumerable<Planet>)orderedEnumerable3), true);
                    this.TaskStep = 6;
                    break;
                case 6:
                    float num12 = 0.0f;
                    float num13 = 0.0f;
                    foreach (Ship ship in (Array<Ship>)this.Ships)
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
            foreach (Ship ship in (Array<Ship>)this.Ships)
            {
                if (ship.Active)
                    num += ship.GetStrength();
            }
            return num;
        }

        public void UpdateAI(float elapsedTime, int which)
        {
            if (Task != null)
            {
                EvaluateTask(elapsedTime);
            }
            else
            {
                if (EmpireManager.Player == Owner || IsCoreFleet || Ships.Count <= 0)
                    return;
                foreach (Ship s in Owner.GetFleetsDict()[which].Ships)
                {                    
                    s.GetAI().OrderQueue.Clear();
                    s.GetAI().State = AIState.AwaitingOrders;
                    s.fleet = null;
                    s.HyperspaceReturn();
                    s.isSpooling = false;
                    if (s.shipData.Role == ShipData.RoleName.troop)
                        s.GetAI().OrderRebaseToNearest();
                    else
                        Owner.ForcePoolAdd(s);
                }
                Owner.GetGSAI().UsedFleets.Remove(which);
                Reset();
            }
        }

        public void Update(float elapsedTime)
        {
            Array<Ship> list = new Array<Ship>();
            this.HasRepair = false;

            foreach(Ship ship in this.Ships)
            {
                if (!ship.Active)
                {
                    ship.fleet = (Fleet)null;
                    this.Ships.QueuePendingRemoval(ship);
                }
                else
                               if (ship.hasRepairBeam || (ship.HasRepairModule && ship.Ordinance > 0))
                    this.HasRepair = true;
            }
            //this.Ships.ForEach(ship =>
            //{
              
            //});

            this.Ships.ApplyPendingRemovals();
            //foreach (Ship ship in list)
            //{
            //    ship.fleet = (Fleet)null;
            //    this.Ships.Remove(ship);
            //}
            //this.Ships.thisLock.ExitWriteLock();
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

        public sealed class Squad : IDisposable
        {
            public FleetDataNode MasterDataNode = new FleetDataNode();
            public BatchRemovalCollection<FleetDataNode> DataNodes = new BatchRemovalCollection<FleetDataNode>();
            public BatchRemovalCollection<Ship> Ships = new BatchRemovalCollection<Ship>();
            public Fleet Fleet;
            public Vector2 Offset;
            //adding for thread safe Dispose because class uses unmanaged resources 
            private bool disposed;

            public Fleet.FleetCombatStatus FleetCombatStatus;

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            ~Squad() { Dispose(false); }

            private void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        if (this.DataNodes != null)
                            this.DataNodes.Dispose();
                        if (this.Ships != null)
                            this.Ships.Dispose();

                    }
                    this.DataNodes = null;
                    this.Ships = null;
                    this.disposed = true;
                }
            }
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
                this.fleet.Position += this.fleet.Position.FindVectorToTarget(this.MovePosition) * this.fleet.speed * elapsedTime;
                this.fleet.AssembleFleet(this.FinalFacing, this.FinalFacingVector);
                if ((double)Vector2.Distance(this.fleet.Position, this.MovePosition) >= 100.0)
                    return;
                this.fleet.Position = this.MovePosition;
                this.fleet.GoalStack.Pop();
            }

            private void DoMove(float elapsedTime)
            {
                Vector2 vector2 = this.fleet.Position.FindVectorToTarget(this.MovePosition);
                float num1 = 0.0f;
                int num2 = 0;
                foreach (Ship ship in (Array<Ship>)this.fleet.Ships)
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

        ~Fleet() { Dispose(false); }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.DataNodes != null)
                        this.DataNodes.Dispose();

                }
                this.DataNodes = null;
                this.disposed = true;
                base.Dispose(disposing);
                
            }
        }
    }
}
