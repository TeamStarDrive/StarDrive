// Type: Ship_Game.Gameplay.Fleet
// Assembly: StarDrive, Version=1.0.9.0, Culture=neutral, PublicKeyToken=null
// MVID: C34284EE-F947-460F-BF1D-3C6685B19387
// Assembly location: E:\Games\Steam\steamapps\common\StarDrive\oStarDrive.exe

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
    public sealed class Fleet : ShipGroup
    {
        public BatchRemovalCollection<FleetDataNode> DataNodes = new BatchRemovalCollection<FleetDataNode>();
        public Guid Guid = Guid.NewGuid();
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
        private Map<Vector2, Ship[]> EnemyClumpsDict = new Map<Vector2, Ship[]>();
        private Map<Ship, Array<Ship>> InterceptorDict = new Map<Ship, Array<Ship>>();
        private int DefenseTurns = 50;
        private Vector2 TargetPosition = Vector2.Zero;
        public MilitaryTask FleetTask;
        public FleetCombatStatus Fcs;
        public Empire Owner;
        public Vector2 Position;
        public float Facing;
        public float Speed;
        public int FleetIconIndex;
        public static UniverseScreen Screen;
        public int TaskStep;
        public bool IsCoreFleet;
        [XmlIgnore][JsonIgnore] public Vector2 StoredFleetPosition;
        [XmlIgnore][JsonIgnore] public float StoredFleetDistancetoMove;

        public bool HasRepair;  //fbedard: ships in fleet with repair capability will not return for repair.

        public override string ToString() => $"Fleet {Name} size={Ships.Count} pos={Position} guid={Guid}";

        //This file refactored by Gretman

        public Fleet()
        {
            FleetIconIndex = RandomMath.IntBetween(1, 10);
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

        public Stack<FleetGoal> GetStack()
        {
            return this.GoalStack;
        }

        public void AddShip(Ship shiptoadd, bool updateOnly = false)
        {
            this.HasRepair = HasRepair || shiptoadd.hasRepairBeam || (shiptoadd.HasRepairModule && shiptoadd.Ordinance > 0);        
            if (updateOnly) return;
            if (shiptoadd.fleet != null || Ships.Contains(shiptoadd))
                Log.Error("ship already in a fleet");
            if (shiptoadd.shipData.Role == ShipData.RoleName.station || shiptoadd.IsPlatform)
                return;
            this.Ships.Add(shiptoadd);
            shiptoadd.fleet = this;            
            this.SetSpeed();
            this.AssignPositions(this.Facing);
            //shiptoadd.GetAI().FleetNode = figure out how to set the ships datanode
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
            foreach (Ship ship in mainShipList)
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
            IOrderedEnumerable<Ship> remainingShips = mainShipList.OrderByDescending(ship => ship.GetStrength() + ship.Size);
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

            this.Position = this.FindAveragePosition();

            ArrangeSquad(this.CenterFlank, Vector2.Zero);
            ArrangeSquad(this.ScreenFlank, new Vector2(0.0f, -2500f));
            ArrangeSquad(this.RearFlank, new Vector2(0.0f, 2500f));

            for (int index = 0; index < this.LeftFlank.Count; ++index)
            {
                this.LeftFlank[index].Offset = new Vector2(-this.CenterFlank.Count * 1400 - (this.LeftFlank.Count == 1 ? 1400 : index * 1400), 0.0f);
            }

            for (int index = 0; index < this.RightFlank.Count; ++index)
            {
                this.RightFlank[index].Offset = new Vector2(this.CenterFlank.Count * 1400 + (this.RightFlank.Count == 1 ? 1400 : index * 1400), 0.0f);
            }

            this.AutoAssembleFleet(0.0f);
            foreach (Ship s in this.Ships)
            {
                if (!s.InCombat)
                {
                    lock (s.AI.WayPointLocker)
                        s.AI.OrderThrustTowardsPosition(this.Position + s.FleetOffset, this.Facing, new Vector2(0.0f, -1f), true);
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
            if (sizeOverSpeed) { orderedShips = allShips.OrderByDescending(ship => ship.Size);  }
            else               { orderedShips = allShips.OrderByDescending(ship => ship.speed); }

            Squad squad = new Squad();
            squad.Fleet = this;
            for (int index = 0; index < orderedShips.Count(); ++index)
            {
                if (squad.Ships.Count < 4)
                    squad.Ships.Add(orderedShips.ElementAt(index));
                if (squad.Ships.Count == 4 || index == orderedShips.Count() - 1)
                {
                    destSquad.Add(squad);
                    squad = new Squad {Fleet = this};
                }
            }
        }

        private void ArrangeSquad(Array<Squad> squad, Vector2 squadOffset)
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

        public void MoveToDirectly(Vector2 movePosition, float facing, Vector2 fVec)
        {
            this.Position = this.FindAveragePosition();
            this.GoalStack.Clear();
            this.MoveDirectlyNow(movePosition, facing, fVec);
        }

        public void FormationWarpTo(Vector2 movePosition, float facing, Vector2 fvec, bool queueOrder = false)
        {
            this.GoalStack.Clear();
            this.Position = movePosition;
            this.Facing = facing;
            this.AssembleFleet(facing, fvec);
            foreach (Ship ship in this.Ships)
            {
                ship.AI.SetPriorityOrder();
                if (queueOrder) ship.AI.OrderFormationWarpQ(movePosition + ship.FleetOffset, facing, fvec);
                else            ship.AI.OrderFormationWarp(movePosition + ship.FleetOffset, facing, fvec);
            }
        }

        public void AttackMoveTo(Vector2 movePosition)
        {
            this.GoalStack.Clear();
            Vector2 fVec = this.FindAveragePosition().FindVectorToTarget(movePosition);
            this.Position = this.FindAveragePosition() + fVec * 3500f;
            this.GoalStack.Push(new FleetGoal(this, movePosition, FindAveragePosition().RadiansToTarget(movePosition), fVec, FleetGoalType.AttackMoveTo));
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

        private void MoveDirectlyNow(Vector2 movePosition, float facing, Vector2 fVec)
        {
            this.Position = movePosition;
            this.Facing = facing;
            this.AssembleFleet(facing, fVec);
            foreach (Ship ship in this.Ships)
            {
                //Prevent fleets with no tasks from and are near their distination from being dumb.
                if (this.Owner.isPlayer || ship.AI.State == AIState.AwaitingOrders || ship.AI.State == AIState.AwaitingOffenseOrders)
                {
                    ship.AI.SetPriorityOrder();
                    ship.AI.OrderMoveDirectlyTowardsPosition(movePosition + ship.FleetOffset, facing, fVec, true);
                }
            }
        }

        private void AutoAssembleFleet(float facing)
        {
            foreach (Array<Squad> list in this.AllFlanks)
            {
                foreach (Squad squad in list)
                {
                    for (int index = 0; index < squad.Ships.Count; ++index)
                    {
                        float radiansAngle;
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
                        Vector2 distanceUsingRadians = Vector2.Zero.PointFromRadians((squad.Offset.ToRadians() + facing), squad.Offset.Length());
                        squad.Ships[index].FleetOffset = distanceUsingRadians + Vector2.Zero.PointFromRadians(radiansAngle + facing, 500f);
                        distanceUsingRadians = Vector2.Zero.PointFromRadians(radiansAngle, 500f);
                        squad.Ships[index].RelativeFleetOffset = squad.Offset + distanceUsingRadians;
                    }
                }
            }
        }

        public void AssignPositions(float facing)
        {
            this.Facing = facing;
            foreach (Ship ship in Ships)
            {
                float angle      = ship.RelativeFleetOffset.ToRadians() + facing;
                float distance   = ship.RelativeFleetOffset.Length();
                ship.FleetOffset = Vector2.Zero.PointFromRadians(angle, distance);
            }
        }

        public void AssembleFleet(float facing, Vector2 facingVec)
        {
            this.Facing = facing;
            foreach (Ship ship in this.Ships)
            {
                if (ship.AI.State == AIState.AwaitingOrders || this.IsCoreFleet)
                {
                    float angle = ship.RelativeFleetOffset.ToRadians() + facing;
                    float distance = ship.RelativeFleetOffset.Length();
                    ship.FleetOffset = Vector2.Zero.PointFromRadians(angle, distance);
                }
            }
        }

        public override void ProjectPos(Vector2 projectedPosition, float facing, Vector2 fVec)
        {
            this.ProjectedFacing = facing;
            foreach (Ship ship in Ships)
            {
                float angle = ship.RelativeFleetOffset.ToRadians() + facing;
                float distance = ship.RelativeFleetOffset.Length();
                ship.projectedPosition = projectedPosition + Vector2.Zero.PointFromRadians(angle, distance);
            }
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
            float sum = (float)distances.Sum(distance => Math.Pow(distance -avgdistance, 2));
            float stddev = (float)Math.Sqrt((sum) / (distances.Count  - 1));
            this.StoredFleetDistancetoMove = distances.Where(distance => distance <= avgdistance + stddev).Average();
        }

        public void Reset()
        {
            while (Ships.Count > 0) {
                Ships.PopLast().fleet = null;
            }
            TaskStep  = 0;
            FleetTask = null;
            GoalStack.Clear();
        }

        private void EvaluateTask(float elapsedTime)
        {
            if (Ships.Count == 0)
                FleetTask.EndTask();
            if (FleetTask == null)
                return;
            switch (FleetTask.type)
            {
                case MilitaryTask.TaskType.ClearAreaOfEnemies:         DoClearAreaOfEnemies(FleetTask); break;
                case MilitaryTask.TaskType.AssaultPlanet:              DoAssaultPlanet(FleetTask);      break;
                case MilitaryTask.TaskType.CorsairRaid:                DoCorsairRaid(elapsedTime); break;
                case MilitaryTask.TaskType.CohesiveClearAreaOfEnemies: DoCohesiveClearAreaOfEnemies(FleetTask); break;
                case MilitaryTask.TaskType.Exploration:                DoExplorePlanet(FleetTask); break;
                case MilitaryTask.TaskType.DefendSystem:               DoDefendSystem(FleetTask); break;
                case MilitaryTask.TaskType.DefendClaim:                DoClaimDefense(FleetTask); break;
                case MilitaryTask.TaskType.DefendPostInvasion:         DoPostInvasionDefense(FleetTask); break;
                case MilitaryTask.TaskType.GlassPlanet:                DoGlassPlanet(FleetTask); break;
            }
            this.Owner.GetGSAI().TaskList.ApplyPendingRemovals();
        }

        private void DoCorsairRaid(float elapsedTime)
        {
            if (this.TaskStep != 0)
                return;
            this.FleetTask.TaskTimer -= elapsedTime;
            if (this.FleetTask.TaskTimer <= 0.0)
            {
                bool found = false;
                for (int index = 0; index < Owner.GetShips().Count; index++)
                {
                    Ship ship = Owner.GetShips()[index];
                    if (ship.Name != "Corsair Asteroid Base") continue;
                    found = true;
                    this.AssembleFleet(0.0f, Vector2.One);
                    this.FormationWarpTo(ship.Position, 0.0f, Vector2.One);
                    this.FleetTask.EndTaskWithMove();
                    break;
                }
                if (!found)                
                    this.FleetTask.EndTask();
            }
            if (this.Ships.Count == 0)
                this.FleetTask.EndTask();
        }

        private void DoExplorePlanet(MilitaryTask task) //Mer Gretman Left off here
        {
            Log.Info("DoExplorePlanet called!  " + this.Owner.PortraitName);
            bool eventBuildingFound = false;
            foreach (Building building in task.GetTargetPlanet().BuildingList)
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
                    foreach (PlanetGridSquare planetGridSquare in task.GetTargetPlanet().TilesList)
                    {
                        if (planetGridSquare.TroopsHere.Count > 0 && planetGridSquare.TroopsHere[0].GetOwner() == this.Owner)
                        {
                            weHaveTroops = true;
                            break;
                        }
                    }
                }
            }

            if (eventBuildingFound || !weHaveTroops || task.GetTargetPlanet().Owner != null)
            {
                task.EndTask();
            }
            else
            {
                switch (this.TaskStep)
                {
                    case 0:                        
                        Vector2 nearestShipyard = Owner.RallyPoints.FindMin(planet => Vector2.Distance(task.AO, planet.Position)).Position;                        
                        Vector2 fVec = Vector2.Normalize(task.AO - nearestShipyard);
                        this.MoveToNow(nearestShipyard, nearestShipyard.RadiansToTarget(task.AO), fVec);
                        for (int index = 0; index < Ships.Count; index++)
                        {
                            Ship ship = Ships[index];
                            ship.AI.HasPriorityOrder = true;
                        }
                        this.TaskStep = 1;
                        break;
                    case 1:
                        bool fleetGathered = true;
                        bool fleetNotInCombat = true;
                        for (int index = 0; index < this.Ships.Count; index++)
                        {
                            Ship ship = this.Ships[index];
                            if (!ship.disabled && ship.hasCommand && ship.Active)
                            {
                                if (ship.Center.OutsideRadius(this.Position + ship.FleetOffset, 5000))
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
                        Vector2 movePosition = task.GetTargetPlanet().Position + Vector2.Normalize(FindAveragePosition() - task.GetTargetPlanet().Position) * 50000f;
                        Position = movePosition;
                        FormationWarpTo(movePosition, FindAveragePosition().RadiansToTarget(task.AO), Vector2.Normalize(task.AO - FindAveragePosition()));
                        break;
                    case 2:
                        fleetGathered = IsFleetAssembled(5000,out bool endTask);
                        
                        if (!fleetGathered)
                            break;
                        for (int index = 0; index < this.Ships.Count; index++)
                        {
                            Ship ship = Ships[index];
                            ship.AI.State = AIState.HoldPosition;
                            if (ship.shipData.Role == ShipData.RoleName.troop)
                                ship.AI.HoldPosition();
                        }
                        this.InterceptorDict.Clear();
                        this.TaskStep = 3;
                        break;
                    case 3:
                        this.EnemyClumpsDict = Owner.GetGSAI().ThreatMatrix.PingRadarShipClustersByVector(Position, 150000, 10000, this.Owner);

                        if (this.EnemyClumpsDict.Count == 0)
                        {
                            this.TaskStep = 5;
                            break;
                        }
                        else
                        {
                            Array<Vector2> list3 = new Array<Vector2>();
                            foreach (var keyValuePair in this.EnemyClumpsDict)
                                list3.Add(keyValuePair.Key);
                            IOrderedEnumerable<Vector2> orderedEnumerable2 = list3.OrderBy(clumpPos => Vector2.Distance(this.FindAveragePosition(), clumpPos));
                            Array<Ship> list4 = new Array<Ship>();
                            foreach (Ship toAttack in this.EnemyClumpsDict[orderedEnumerable2.First()])
                            {
                                float num = 0.0f;
                                foreach (Ship ship in this.Ships)
                                {
                                    if (!list4.Contains(ship) && (num < 1 || num < toAttack.GetStrength()))
                                    {
                                        ship.AI.Intercepting = true;
                                        ship.AI.OrderAttackSpecificTarget(toAttack);
                                        list4.Add(ship);
                                        num += ship.GetStrength();
                                    }
                                }
                            }
                            Array<Ship> list5 = new Array<Ship>();
                            foreach (Ship ship in this.Ships)
                            {
                                if (!list4.Contains(ship))
                                    list5.Add(ship);
                            }
                            foreach (Ship ship in list5)
                                ship.AI.OrderAttackSpecificTarget(list4[0].AI.Target as Ship);
                            this.TaskStep = 4;
                            if (this.InterceptorDict.Count != 0)
                                break;
                            this.TaskStep = 4;
                            break;
                        }
                    case 4:
                        if (IsFleetSupplied())
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
                        foreach (Ship ship in this.Ships)
                        {
                            ship.AI.Intercepting = true;
                            ship.AI.OrderLandAllTroops(task.GetTargetPlanet());
                        }
                        this.Position = task.GetTargetPlanet().Position;
                        this.AssembleFleet(this.Facing, Vector2.Normalize(this.Position - this.FindAveragePosition()));
                        break;
                }
            }
        }

        private void DoAssaultPlanet(MilitaryTask task)
        {
            if (!Owner.IsEmpireAttackable(task.GetTargetPlanet().Owner))
            {
                if (task.GetTargetPlanet().Owner == Owner || task.GetTargetPlanet().AnyOfOurTroops(Owner))
                {
                    MilitaryTask militaryTask = new MilitaryTask
                    {
                        AO = task.GetTargetPlanet().Position,
                        AORadius = 50000f,
                        WhichFleet = task.WhichFleet
                    };
                    militaryTask.SetEmpire(Owner);
                    militaryTask.type = MilitaryTask.TaskType.DefendPostInvasion;
                    Owner.GetGSAI().TaskList.QueuePendingRemoval(task);
                    FleetTask = militaryTask;                 
                    Owner.GetGSAI().TaskList.Add(task);
                }
                else
                    task.EndTask();
            }
            else
            {                                                
                int assaultShips = CountShipsWithStrength(out int availableTroops);                
                if (availableTroops == 0)                
                    availableTroops += task.GetTargetPlanet().CountEmpireTroops(Owner);                    
                
                if (availableTroops == 0 || assaultShips == 0)
                {
                    if (assaultShips == 0)
                        task.IsCoreFleetTask = false;
                    task.EndTask();
                    FleetTask = null;
                    TaskStep = 0;
                }
                else
                {
                    switch (this.TaskStep)
                    {
                        case -1:
                        case 0:
                            Planet goaltarget = Owner.RallyPoints.FindMin(distance => distance.Position.SqDist(FleetTask.AO));
                            
                            if(goaltarget == null)
                            {
                                task.EndTask();
                                break;
                            }
                            
                            Vector2 fVec = Vector2.Normalize(task.AO - goaltarget.Position);
                            Vector2 vector2 = goaltarget.Position;
                            MoveToNow(vector2, vector2.RadiansToTarget(task.AO), fVec);        
                            TaskStep = 1;
                            break;

                        case 1:                            
                            
                            bool nearFleet = IsFleetAssembled(5000, out bool endTask);
                            
                            if (endTask) 
                                task.EndTask();

                            if (nearFleet)
                            {
                                TaskStep = 2;
                                Vector2 movePosition = task.GetTargetPlanet().Position + Vector2.Normalize(FindAveragePosition() - task.GetTargetPlanet().Position) * 125000f;
                                Position = movePosition;
                                FormationWarpTo(movePosition, FindAveragePosition().RadiansToTarget(task.AO), Vector2.Normalize(task.AO - FindAveragePosition()));
                            }
                            break;
                        case 2:                            
                            if (!IsFleetAssembled(25000, out endTask))
                                break;
                            using (Ships.AcquireReadLock())
                            {
                                foreach (Ship ship in Ships)                                
                                        ship.AI.HoldPosition();
                                
                            }
                            InterceptorDict.Clear();
                            TaskStep = 3;
                            Position = task.GetTargetPlanet().Position;
                            AssembleFleet(Facing, Vector2.Normalize(Position - FindAveragePosition()));
                            break;
                        case 3:
                            if (!IsFleetSupplied())
                            {
                                TaskStep = 5;
                                break;
                            }
                            BombPlanet(0, task);
                            
                                                          //TODO: Indiction logic.   this doesnt work. 
                                foreach (Ship key in Owner.GetGSAI().ThreatMatrix.PingRadarShip(task.GetTargetPlanet().Position,10000,Owner))
                                {
                                    if (Owner.IsEmpireAttackable(key.loyalty,key)                                        
                                        //&& (key.Center.InRadius(task.GetTargetPlanet().Position, 15000 )
                                        && !InterceptorDict.ContainsKey(key))
                                        InterceptorDict.Add(key, new Array<Ship>());
                                }
                                Array<Ship> list2 = new Array<Ship>();
                                foreach (var kv in this.InterceptorDict)
                                {
                                    Array<Ship> list3 = new Array<Ship>();
                                    if (!kv.Key.Active || kv.Key.Center.OutsideRadius(task.GetTargetPlanet().Position, 20000))
                                        continue;                                    
                                    list2.Add(kv.Key);
                                    foreach (Ship ship in kv.Value)
                                    {
                                        if(!ship.AI.Intercepting )
                                        lock (ship)
                                        {
                                            ship.AI.OrderQueue.Clear();                                            
                                            ship.AI.OrderOrbitPlanet(task.GetTargetPlanet());
                                            ship.AI.State = AIState.AwaitingOrders;
                                            ship.AI.Intercepting = false;
                                        }
                                    }
                                    
                                    foreach (Ship ship in kv.Value)
                                    {
                                        if (!ship.Active)
                                            list3.Add(ship);
                                    }
                                    foreach (Ship ship in list3)
                                        kv.Value.Remove(ship);
                                }
                                foreach (Ship key in list2)
                                    this.InterceptorDict.Remove(key);
                                //foreach (var keyValuePair1 in this.InterceptorDict)
                                {
                                    Array<Ship> list3 = new Array<Ship>();
                                    foreach (Ship ship in Ships)
                                    {
                                        if (ship.shipData.Role != ShipData.RoleName.troop)
                                            list3.Add(ship);
                                    }
                                    Array<Ship> list4 = new Array<Ship>();
                                    foreach (var kv in InterceptorDict)
                                    {
                                        list4.Add(kv.Key);
                                        foreach (Ship ship in kv.Value)
                                            list3.Remove(ship);
                                    }
                                    foreach (Ship toAttack in list4.OrderByDescending(ship => ship.GetStrength()))
                                    {
                                        IOrderedEnumerable<Ship> orderedEnumerable2 = list3.OrderByDescending(ship => ship.GetStrength());
                                        float num4 = 0.0f;
                                        foreach (Ship ship in orderedEnumerable2)
                                        {                                         
                                            if (num4 > 0)
                                            {
                                                if (num4 >= toAttack.GetStrength() * 1.5f)
                                                    break;
                                            }
                                            ship.AI.hasPriorityTarget = true;
                                            ship.AI.Intercepting = true;
                                            list3.Remove(ship);
                                            ship.AI.OrderAttackSpecificTarget(toAttack);
                                            InterceptorDict[toAttack].Add(ship);
                                            num4 += ship.GetStrength();
                                        }                                        
                                    }
                                }
                                if (InterceptorDict.Count == 0 
                                    || this.Owner.GetGSAI().ThreatMatrix.PingRadarStr(task.GetTargetPlanet().Position, 25000f, this.Owner) < 500)
                                    this.TaskStep = 4;

                                using (Owner.GetGSAI().TaskList.AcquireReadLock())
                                    foreach(MilitaryTask militaryTask in Owner.GetGSAI().TaskList)
                                    {
                                        if (militaryTask.WaitForCommand && militaryTask.GetTargetPlanet() != null 
                                            && militaryTask.GetTargetPlanet() == task.GetTargetPlanet())
                                            militaryTask.WaitForCommand = false;
                                    }
                             
                            break;
                        case 4:                            
                            float theirGroundStrength = GetGroundStrOfPlanet(task.GetTargetPlanet());
                            float ourGroundStrength = FleetTask.GetTargetPlanet().GetGroundStrength(Owner);
                            if (!IsFleetSupplied())
                            {
                                TaskStep = 5;
                                break;
                            }

                            if (!IsInvading(theirGroundStrength, ourGroundStrength, task) && BombPlanet(ourGroundStrength, task) == 0)
                                task.EndTask();
                            else
                                TaskStep = 3;
                                break;
                            
                        case 5:
                            Planet rallyPoint = Owner.RallyPoints.FindMin(planet => Position.SqDist(planet.Position));                                                        
                            foreach (Ship ship in this.Ships)
                                ship.AI.OrderResupply(rallyPoint, true);
                            this.TaskStep = 6;
                            break;
                                                        
                        case 6:
                            float num17 = 0.0f;
                            float num18 = 0.0f;
                            foreach (Ship ship in this.Ships)
                            {
                                ship.AI.HasPriorityOrder = true;
                                num17 += ship.Ordinance;
                                num18 += ship.OrdinanceMax;
                            }
                            if (num18 >0 && num17 < num18 * 0.89f)
                                break;
                            task.Step = 0;
                            break;
                    }
                }
            }
        }

        private int BombPlanet(float ourGroundStrength, MilitaryTask task , int freeSpacesNeeded =int.MaxValue)
        {

            bool doBombs = !(ourGroundStrength > 0 && freeSpacesNeeded >= task.GetTargetPlanet().GetGroundLandingSpots());
            int bombs = 0;            
            
            for (int index = 0; index < Ships.Count; index++)
            {
                Ship ship = Ships[index];
                if (!ship.Active) continue;
                int shipbombs = ship.BombCount;
                if (shipbombs == 0) continue;
                bombs += shipbombs;                
                if(doBombs)
                    ship.AI.OrderBombardPlanet(task.GetTargetPlanet());                    
            }
            
            return bombs;
        }

        private bool IsInvading(float thierGroundStrength, float ourGroundStrength, MilitaryTask task, int LandingspotsNeeded =5)
        {            
            int freeLandingSpots = task.GetTargetPlanet().GetGroundLandingSpots();
            if (freeLandingSpots == 0) return false;

            float planetAssaultStrength = 0.0f;
            foreach (Ship ship in Ships)            
                planetAssaultStrength += ship.PlanetAssaultStrength;
            
            planetAssaultStrength += ourGroundStrength;
            if (planetAssaultStrength < thierGroundStrength) return false;
            if ( freeLandingSpots < LandingspotsNeeded ) return false;
        
            for (int index = 0; index < Ships.Count; index++)
            {
                Ship ship = Ships[index];
                if (!ship.Active || ship.AI.State != AIState.Bombard) continue;
                ship.AI.State = AIState.AwaitingOrders;
                ship.AI.OrderQueue.Clear();
            }               
            
            foreach (Ship ship in Ships)
            {
                if (!ship.AI.HasPriorityOrder && ship.ReadyPlanetAssaulttTroops > 0)
                {
                    ship.AI.OrderLandAllTroops(task.GetTargetPlanet());
                    ship.AI.HasPriorityOrder = true;
                }

            }
            return true;

        }
        private bool IsFleetSupplied()
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

        private float GetGroundStrOfPlanet(Planet p)
        {
            return p.GetGroundStrengthOther(this.Owner);
        }

        private void DoPostInvasionDefense(MilitaryTask task)
        {
            --this.DefenseTurns;
            if (this.DefenseTurns <= 0)
            {
                task.EndTask();
            }
            else
            {
                switch (this.TaskStep)
                {
                    case -1:
                        bool flag1 = true;
                        foreach (Ship ship in this.Ships)
                        {
                            if (!ship.disabled && ship.hasCommand && ship.Active)
                            {
                                if (Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 5000.0)
                                    flag1 = false;
                                int num = ship.InCombat ? 1 : 0;
                                if (!flag1)
                                    break;
                            }
                        }
                        if (!flag1)
                            break;
                        TaskStep = 2;
                        FormationWarpTo(task.AO, FindAveragePosition().RadiansToTarget(task.AO), Vector2.Normalize(task.AO - FindAveragePosition()));

                        foreach (Ship ship in Ships)
                            ship.AI.HasPriorityOrder = true;
                        break;
                    case 0:
                        Array<Planet> list1 = new Array<Planet>();
                        foreach (Planet planet in this.Owner.GetPlanets())
                        {
                            if (planet.HasShipyard)
                                list1.Add(planet);
                        }
                        IOrderedEnumerable<Planet> orderedEnumerable1 = list1.OrderBy(planet => Vector2.Distance(task.AO, planet.Position));
                        if (orderedEnumerable1.Count() <= 0)
                            break;
                        Vector2 fVec = Vector2.Normalize(task.AO - orderedEnumerable1.First().Position);
                        Vector2 vector2 = orderedEnumerable1.First().Position;
                        this.MoveToNow(vector2, vector2.RadiansToTarget(task.AO), fVec);
                        this.TaskStep = 1;
                        break;
                    case 1:
                        bool flag2 = true;
                        for (int index = 0; index < this.Ships.Count; index++)
                        {
                            Ship ship = this.Ships[index];
                            if (!ship.disabled && ship.hasCommand && ship.Active)
                            {
                                if (Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 5000.0)
                                    flag2 = false;
                                if (!flag2)
                                    break;
                            }
                        }
                        if (!flag2)
                            break;
                        TaskStep = 2;
                        FormationWarpTo(task.AO, FindAveragePosition().RadiansToTarget(task.AO), Vector2.Normalize(task.AO - FindAveragePosition()));

                        foreach (Ship ship in Ships)
                            ship.AI.HasPriorityOrder = true;

                        break;
                    case 2:
                        bool flag3 = false;
                        if (Vector2.Distance(this.FindAveragePosition(), task.AO) < 15000.0)
                        {
                            foreach (Ship ship in this.Ships)
                            {
                                lock (ship)
                                {
                                    if (ship.InCombat)
                                    {
                                        flag3 = true;
                                        ship.HyperspaceReturn();
                                        ship.AI.OrderQueue.Clear();
                                        break;
                                    }
                                }
                            }
                        }
                        if (!flag3 && Vector2.Distance(this.FindAveragePosition(), task.AO) >= 5000.0)
                            break;
                        this.TaskStep = 3;
                        break;
                    case 3:
                        this.EnemyClumpsDict = Owner.GetGSAI().ThreatMatrix.PingRadarShipClustersByVector(Position, 150000, 10000, this.Owner);
                        if (this.EnemyClumpsDict.Count == 0)
                        {
                            if (Vector2.Distance(this.FindAveragePosition(), task.AO) <= 10000.0)
                                break;
                            this.FormationWarpTo(task.AO, 0.0f, new Vector2(0.0f, -1f));
                            break;
                        }
                        else
                        {
                            Array<Vector2> list3 = new Array<Vector2>();
                            foreach (var keyValuePair in this.EnemyClumpsDict)
                                list3.Add(keyValuePair.Key);
                            IOrderedEnumerable<Vector2> orderedEnumerable2 = list3.OrderBy(clumpPos => Vector2.Distance(this.FindAveragePosition(), clumpPos));
                            Array<Ship> list4 = new Array<Ship>();
                            foreach (Ship toAttack in this.EnemyClumpsDict[orderedEnumerable2.First()].OrderByDescending(ship => ship.Size))
                            {
                                float num = 0.0f;
                                foreach (Ship ship in this.Ships.OrderByDescending(ship => ship.Size))
                                {
                                    if (!list4.Contains(ship) && (num == 0.0 || num < (double)toAttack.GetStrength()))
                                    {
                                        ship.AI.OrderAttackSpecificTarget(toAttack);
                                        ship.AI.Intercepting = true;
                                        list4.Add(ship);
                                        num += ship.GetStrength();
                                    }
                                }
                            }
                            Array<Ship> list5 = new Array<Ship>();
                            foreach (Ship ship in this.Ships)
                            {
                                if (!list4.Contains(ship))
                                    list5.Add(ship);
                            }
                            foreach (Ship ship in list5)
                            {
                                ship.AI.OrderAttackSpecificTarget(list4[0].AI.Target as Ship);
                                ship.AI.Intercepting = true;
                            }
                            this.TaskStep = 4;
                            break;
                        }
                    case 4:
                        if (IsFleetSupplied())
                        {
                            this.TaskStep = 5;
                            break;
                        }
                        else
                        {
                            bool flag4 = false;
                            foreach (Ship ship in this.Ships)
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
                        IOrderedEnumerable<Planet> orderedEnumerable3 = list6.OrderBy(p => Vector2.Distance(this.Position, p.Position));
                        if (orderedEnumerable3.Count() <= 0)
                            break;
                        this.Position = orderedEnumerable3.First().Position;
                        foreach (Ship ship in this.Ships)
                            ship.AI.OrderResupply(orderedEnumerable3.First(), true);
                        this.TaskStep = 6;
                        break;
                    case 6:
                        float num6 = 0.0f;
                        float num7 = 0.0f;
                        foreach (Ship ship in this.Ships)
                        {
                            ship.AI.HasPriorityOrder = true;
                            num6 += ship.Ordinance;
                            num7 += ship.OrdinanceMax;
                        }
                        if (num6 != (double)num7)
                            break;
                        this.TaskStep = 0;
                        break;
                }
            }
        }

        private void DoDefendSystem(MilitaryTask task)
        {
            switch (this.TaskStep)
            {
                case -1:
                    bool flag1 = true;
                    foreach (Ship ship in this.Ships)
                    {
                        if (!ship.disabled && ship.hasCommand && ship.Active)
                        {
                            if (Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 5000.0)
                                flag1 = false;
                            int num = ship.InCombat ? 1 : 0;
                            if (!flag1)
                                break;
                        }
                    }
                    if (!flag1)
                        break;
                    this.TaskStep = 2;
                    this.FormationWarpTo(task.AO, FindAveragePosition().RadiansToTarget(task.AO), Vector2.Normalize(task.AO - this.FindAveragePosition()));
                    foreach (Ship ship in Ships)
                        ship.AI.HasPriorityOrder = true;
                    break;
                case 0:
                    Array<Planet> list1 = new Array<Planet>();
                    foreach (Planet planet in this.Owner.GetPlanets())
                    {
                        if (planet.HasShipyard)
                            list1.Add(planet);
                    }
                    IOrderedEnumerable<Planet> orderedEnumerable1 = list1.OrderBy(planet => Vector2.Distance(task.AO, planet.Position));
                    if (orderedEnumerable1.Count() <= 0)
                        break;
                    Vector2 fVec = Vector2.Normalize(task.AO - orderedEnumerable1.First().Position);
                    Vector2 vector2 = orderedEnumerable1.First().Position;
                    this.MoveToNow(vector2, vector2.RadiansToTarget(task.AO), fVec);
                    this.TaskStep = 1;
                    break;
                case 1:
                    bool flag2 = true;
                    foreach (Ship ship in this.Ships)
                    {
                        if (!ship.disabled && ship.hasCommand && ship.Active)
                        {
                            if (Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 5000.0)
                                flag2 = false;
                            int num = ship.InCombat ? 1 : 0;
                            if (!flag2)
                                break;
                        }
                    }
                    if (!flag2)
                        break;
                    this.TaskStep = 2;
                    this.FormationWarpTo(task.AO, FindAveragePosition().RadiansToTarget(task.AO), Vector2.Normalize(task.AO - this.FindAveragePosition()));
                    foreach (Ship ship in Ships)
                        ship.AI.HasPriorityOrder = true;
                    break;
                case 2:
                    bool flag3 = false;
                    if (Vector2.Distance(this.FindAveragePosition(), task.AO) < 15000.0)
                    {
                        foreach (Ship ship in this.Ships)
                        {
                            lock (ship)
                            {
                                if (ship.InCombat)
                                {
                                    flag3 = true;
                                    ship.HyperspaceReturn();
                                    ship.AI.OrderQueue.Clear();
                                    break;
                                }
                            }
                        }
                    }
                    if (!flag3 && Vector2.Distance(this.FindAveragePosition(), task.AO) >= 5000.0)
                        break;
                    this.TaskStep = 3;
                    break;
                case 3:
                    this.EnemyClumpsDict = Owner.GetGSAI().ThreatMatrix.PingRadarShipClustersByVector(Position, 150000, 10000, this.Owner);

                    if (this.EnemyClumpsDict.Count == 0)
                    {
                        if (Vector2.Distance(this.FindAveragePosition(), task.AO) <= 10000.0)
                            break;
                        this.FormationWarpTo(task.AO, 0.0f, new Vector2(0.0f, -1f));
                        break;
                    }
                    else
                    {
                        Array<Vector2> list3 = new Array<Vector2>();
                        foreach (var keyValuePair in this.EnemyClumpsDict)
                            list3.Add(keyValuePair.Key);
                        IOrderedEnumerable<Vector2> orderedEnumerable2 = list3.OrderBy(clumpPos => Vector2.Distance(this.FindAveragePosition(), clumpPos));
                        Array<Ship> list4 = new Array<Ship>();
                        foreach (Ship toAttack in this.EnemyClumpsDict[orderedEnumerable2.First()].OrderByDescending(ship => ship.Size))
                        {
                            float num = 0.0f;
                            foreach (Ship ship in this.Ships.OrderByDescending(ship => ship.Size))
                            {
                                if (!list4.Contains(ship) && (num == 0.0 || num < (double)toAttack.GetStrength()))
                                {
                                    ship.AI.OrderAttackSpecificTarget(toAttack);
                                    ship.AI.Intercepting = true;
                                    list4.Add(ship);
                                    num += ship.GetStrength();
                                }
                            }
                        }
                        Array<Ship> list5 = new Array<Ship>();
                        foreach (Ship ship in this.Ships)
                        {
                            if (!list4.Contains(ship))
                                list5.Add(ship);
                        }
                        foreach (Ship ship in list5)
                        {
                            ship.AI.OrderAttackSpecificTarget(list4[0].AI.Target as Ship);
                            ship.AI.Intercepting = true;
                        }
                        this.TaskStep = 4;
                        break;
                    }
                case 4:
                    if (IsFleetSupplied())
                    {
                        this.TaskStep = 5;
                        break;
                    }
                    else
                    {
                        bool flag4 = false;
                        foreach (Ship ship in this.Ships)
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
                    IOrderedEnumerable<Planet> orderedEnumerable3 = list6.OrderBy(p => Vector2.Distance(this.Position, p.Position));
                    if (orderedEnumerable3.Count() <= 0)
                        break;
                    this.Position = orderedEnumerable3.First().Position;
                    foreach (Ship ship in this.Ships)
                        ship.AI.OrderResupply(orderedEnumerable3.First(), true);
                    this.TaskStep = 6;
                    break;
                case 6:
                    float num6 = 0.0f;
                    float num7 = 0.0f;
                    foreach (Ship ship in this.Ships)
                    {
                        ship.AI.HasPriorityOrder = true;
                        num6 += ship.Ordinance;
                        num7 += ship.OrdinanceMax;
                    }
                    if (num6 != (double)num7)
                        break;
                    this.TaskStep = 0;
                    break;
            }
        }

        private void DoClaimDefense(MilitaryTask task)
        {
            switch (TaskStep)
            {
                case 0:
                    Planet rallyPoint = Owner.RallyPoints.FindMin(planet => planet.Position.SqDist(task.AO));
                    if (rallyPoint == null) return;
                    Position = rallyPoint.Position;
                    Vector2 fVec = Vector2.Normalize(task.GetTargetPlanet().Position - Position);
                    MoveToNow(Position, Position.RadiansToTarget(task.GetTargetPlanet().Position), fVec);
                    TaskStep = 1;
                    break;
                case 1:
                    if (!IsFleetAssembled(5000, out bool endtask))
                        break;                    
                    TaskStep = 2;
                    Position = task.GetTargetPlanet().Position;
                    FormationWarpTo(Position, FindAveragePosition().RadiansToTarget(Position), Vector2.Normalize(Position - FindAveragePosition()));
                    foreach (Ship ship in Ships)
                        ship.AI.HasPriorityOrder = true;
                    break;
                case 2:
                    if (IsFleetAssembled(15000f, out bool incombat) && incombat)
                    {
                        foreach (Ship ship in Ships)
                        {
                            if (ship.InCombat)
                            {                                   
                                ship.HyperspaceReturn();
                                ship.AI.OrderQueue.Clear();
                                break;
                            }
                        }
                    }
                    if (!incombat && FindAveragePosition().OutsideRadius(task.GetTargetPlanet().Position, 5000f))
                        break;
                    TaskStep = 3;
                    break;
                case 3:                    
                    EnemyClumpsDict = Owner.GetGSAI().ThreatMatrix.PingRadarShipClustersByVector(FleetTask.GetTargetPlanet().Position, 150000,10000,Owner);
                    
                    if (EnemyClumpsDict.Count == 0)
                    {
                        foreach (Ship ship in Ships)
                        {
                            var ai = ship.AI;
                            var target = task.GetTargetPlanet();
                            if (!ship.InCombat)
                            {
                                if (ai.State != AIState.Orbit || ai.OrbitTarget == null || ai.OrbitTarget != target)
                                    ai.OrderOrbitPlanet(target);
                            }
                            else // if (current.GetAI().TargetQueue.Count == 0)
                            {
                                ai.OrderMoveDirectlyTowardsPosition(target.ParentSystem.Position, 0, Vector2.Zero, true);
                                ai.HasPriorityOrder = true;
                            }
                        }
                        break;
                    }
                    else
                    {
                        Array<Vector2> list3 = new Array<Vector2>();
                        foreach (var keyValuePair in this.EnemyClumpsDict)
                            list3.Add(keyValuePair.Key);
                        IOrderedEnumerable<Vector2> orderedEnumerable2 = list3.OrderBy(clumpPos => Vector2.Distance(this.FindAveragePosition(), clumpPos));
                        Array<Ship> list4 = new Array<Ship>();
                        foreach (Ship toAttack in this.EnemyClumpsDict[orderedEnumerable2.First()])
                        {
                            float num = 0.0f;
                            foreach (Ship ship in this.Ships)
                            {
                                if (!list4.Contains(ship) && (num == 0.0f || num < (double)toAttack.GetStrength()))
                                {
                                    ship.AI.Intercepting = true;
                                    ship.AI.OrderAttackSpecificTarget(toAttack);
                                    list4.Add(ship);
                                    num += ship.GetStrength();
                                }
                            }
                        }

                        Ship[] uniqueShips = Ships.UniqueGameObjects();
                        foreach (Ship ship in uniqueShips)
                        {
                            ship.AI.Intercepting = true;
                            ship.AI.OrderAttackSpecificTarget(list4[0].AI.Target as Ship);
                        }
                        TaskStep = 4;
                        break;
                    }
                case 4:
                    if (!IsFleetAssembled(150000, out bool combat, FleetTask.GetTargetPlanet().ParentSystem.Position))
                    {
                        foreach (Ship ship in this.Ships)
                        {
                            if (ship.Center.OutsideRadius(FleetTask.GetTargetPlanet().ParentSystem.Position, 150000f))
                            {

                                ship.AI.OrderQueue.Clear();
                                ship.AI.OrderMoveDirectlyTowardsPosition(this.FleetTask.GetTargetPlanet().ParentSystem.Position, 0, Vector2.Zero, true);
                                //ship.GetAI().HasPriorityOrder = true;

                            }
                        }
                    }
                    if(!IsFleetSupplied())                    
                    {
                        this.TaskStep = 5;
                        break;
                    }
                    else
                    {
                        if (combat) break;
                        this.TaskStep = 3;
                        break;                        
                    }
                case 5:

                    rallyPoint = Owner.RallyPoints.FindMin(planet => Position.SqDist(planet.Position));
                    this.Position = rallyPoint.Position;
                    foreach (Ship ship in this.Ships)
                        ship.AI.OrderResupply(rallyPoint, true);
                    this.TaskStep = 6;
                    break;
                case 6:
                    float num6 = 0.0f;
                    float num7 = 0.0f;
                    foreach (Ship ship in this.Ships)
                    {
                        ship.AI.HasPriorityOrder = true;
                        num6 += ship.Ordinance;
                        num7 += ship.OrdinanceMax;
                    }
                    if (num6 < num7)
                        break;
                    this.TaskStep = 0;
                    break;
            }
        }

        private void DoCohesiveClearAreaOfEnemies(MilitaryTask task)
        {
            switch (this.TaskStep)
            {
                case 0:
                    //this.TaskStep = 1;
                    //this.DoCohesiveClearAreaOfEnemies(task);
                    //break;
                case 1:
                   // Array<ThreatMatrix.Pin> list1 = new Array<ThreatMatrix.Pin>();
                    Map<Vector2, float> threatDict = this.Owner.GetGSAI().ThreatMatrix.PingRadarStrengthClusters(this.FleetTask.AO, this.FleetTask.AORadius, 10000f, this.Owner);
                    float strength = this.GetStrength();                    

                    //TODO: add this to threat dictionary. find max in strength
                    var targetSpot = new KeyValuePair<Vector2, float>(Vector2.Zero,float.MaxValue);
                    float distance = float.MaxValue;
                    foreach (var kv in threatDict)
                    {
                        float tempDis = FindAveragePosition().SqDist(kv.Key) ;
                        if (kv.Value < strength && tempDis <  distance)
                        {
                            targetSpot = kv;
                            distance = tempDis;
                        }
                    }

                    if (targetSpot.Value < strength)
                    {
                        TargetPosition = targetSpot.Key;
                        Vector2 fvec = Vector2.Normalize(task.AO - this.TargetPosition);
                        this.FormationWarpTo(this.TargetPosition, TargetPosition.RadiansToTarget(task.AO), fvec);
                        this.TaskStep = 2;

                    }
                    else                    
                        this.FleetTask.EndTask();
                    

                    break;
                    
                    
                case 2:

                    if (this.Owner.GetGSAI().ThreatMatrix.PingRadarStr(this.TargetPosition, 150000, this.Owner) <1)
                    {
                        this.TaskStep = 1;
                        break;
                    }
                    else
                    {
                        
                        if (Vector2.Distance(this.TargetPosition, this.FindAveragePosition()) > 25000)
                            break;
                        this.TaskStep = 3;
                        break;
                    }
                case 3:
                    this.EnemyClumpsDict = this.Owner.GetGSAI().ThreatMatrix.PingRadarShipClustersByVector(this.Position, 150000, 10000, this.Owner);
                   
                    if (this.EnemyClumpsDict.Count == 0)
                    {
                        task.EndTask();
                        break;
                    }
                    else
                    {
                        Array<Vector2> list3 = new Array<Vector2>();
                        foreach (var keyValuePair in this.EnemyClumpsDict)
                            list3.Add(keyValuePair.Key);
                        IOrderedEnumerable<Vector2> orderedEnumerable = list3.OrderBy(clumpPos => Vector2.Distance(this.FindAveragePosition(), clumpPos));
                        Array<Ship> list4 = new Array<Ship>();
                        foreach (Ship toAttack in this.EnemyClumpsDict[orderedEnumerable.First()])
                        {
                            float num = 0.0f;
                            foreach (Ship ship in this.Ships)
                            {
                                if (!list4.Contains(ship) && (num == 0 || num < toAttack.GetStrength()))
                                {
                                    ship.AI.Intercepting = true;
                                    ship.AI.OrderAttackSpecificTarget(toAttack);
                                    list4.Add(ship);
                                    num += ship.GetStrength();
                                }
                            }
                        }
                        Array<Ship> list5 = new Array<Ship>();
                        foreach (Ship ship in this.Ships)
                        {
                            if (!list4.Contains(ship))
                                list5.Add(ship);
                        }
                        foreach (Ship ship in list5)
                        {
                            ship.AI.Intercepting = true;
                            ship.AI.OrderAttackSpecificTarget(list4[0].AI.Target as Ship);
                        }
                        
                        this.TaskStep = 4;
                        break;
                    }
                case 4:
                    if (IsFleetSupplied())
                    {
                        this.TaskStep = 5;
                        break;
                    }

                    bool allInCombat = true;
                    foreach (Ship ship in this.Ships)
                    {
                        if (ship.AI.BadGuysNear && !ship.InDeepSpace)
                        {
                            allInCombat = false;
                            break;
                        }
                    }
                    if (this.Owner.GetGSAI().ThreatMatrix.PingRadarStr(this.Position, 150000, this.Owner) > 0)
                    {
                        
                        if(!allInCombat )
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
                    foreach (Ship ship in this.Ships)
                        ship.AI.OrderResupplyNearest(true);
                    this.TaskStep = 6;
                    break;
                case 6:
                    float num6 = 0.0f;
                    float num7 = 0.0f;
                    foreach (Ship ship in this.Ships)
                    {
                        if (ship.AI.State != AIState.Resupply)
                        {
                            task.EndTask();
                            return;
                        }
                        ship.AI.HasPriorityOrder = true;
                        num6 += ship.Ordinance;
                        num7 += ship.OrdinanceMax;
                    }
                    if (num6 != num7)
                        break;
                    this.TaskStep = 1;
                    break;
            }
        }

        private void DoGlassPlanet(MilitaryTask task)
        {
            if (task.GetTargetPlanet().Owner == this.Owner || task.GetTargetPlanet().Owner == null)
                task.EndTask();
            else if (task.GetTargetPlanet().Owner != null & task.GetTargetPlanet().Owner != this.Owner && !task.GetTargetPlanet().Owner.GetRelations(this.Owner).AtWar)
            {
                task.EndTask();
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
                        IOrderedEnumerable<Planet> orderedEnumerable1 = list1.OrderBy(planet => Vector2.Distance(task.AO, planet.Position));
                        if (!orderedEnumerable1.Any())
                            break;
                        Vector2 fVec = Vector2.Normalize(task.AO - orderedEnumerable1.First().Position);
                        Vector2 vector2 = orderedEnumerable1.First().Position;
                        this.MoveToNow(vector2, vector2.RadiansToTarget(task.AO), fVec);
                        this.TaskStep = 1;
                        break;
                    case 1:
                        bool flag = true;
                        foreach (Ship ship in this.Ships)
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
                        Vector2 movePosition = task.GetTargetPlanet().Position + Vector2.Normalize(this.FindAveragePosition() - task.GetTargetPlanet().Position) * 150000f;
                        this.Position = movePosition;
                        this.FormationWarpTo(movePosition, FindAveragePosition().RadiansToTarget(task.AO), Vector2.Normalize(task.AO - this.FindAveragePosition()));
                        foreach (Ship ship in this.Ships)
                            ship.AI.HasPriorityOrder = true;
                        this.TaskStep = 2;
                        break;
                    case 2:
                        if (task.WaitForCommand && this.Owner.GetGSAI().ThreatMatrix.PingRadarStr(task.GetTargetPlanet().Position, 30000f, this.Owner) > 250.0)
                            break;
                        foreach (Ship ship in this.Ships)
                            ship.AI.OrderBombardPlanet(task.GetTargetPlanet());
                        this.TaskStep = 4;
                        break;
                    case 4:
                        if (IsFleetSupplied())
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
                        IOrderedEnumerable<Planet> orderedEnumerable2 = list2.OrderBy(p => Vector2.Distance(this.Position, p.Position));
                        if (!orderedEnumerable2.Any())
                            break;
                        this.Position = orderedEnumerable2.First().Position;
                        foreach (Ship ship in this.Ships)
                            ship.AI.OrderResupply(orderedEnumerable2.First(), true);
                        this.TaskStep = 6;
                        break;
                    case 6:
                        float num6 = 0.0f;
                        float num7 = 0.0f;
                        foreach (Ship ship in this.Ships)
                        {
                            if (ship.AI.State != AIState.Resupply)
                            {
                                this.TaskStep = 5;
                                return;
                            }
                            ship.AI.HasPriorityOrder = true;
                            num6 += ship.Ordinance;
                            num7 += ship.OrdinanceMax;
                        }
                        if (num6 != num7)
                            break;
                        this.TaskStep = 0;
                        break;
                }
            }
        }

        private void DoClearAreaOfEnemies(MilitaryTask task)
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
                    IOrderedEnumerable<Planet> orderedEnumerable1 = list1.OrderBy(planet => Vector2.Distance(task.AO, planet.Position));
                    if (!orderedEnumerable1.Any())
                        break;
                    Vector2 fVec = Vector2.Normalize(task.AO - orderedEnumerable1.First().Position);
                    Vector2 vector2 = orderedEnumerable1.First().Position;
                    this.MoveToNow(vector2, vector2.RadiansToTarget(task.AO), fVec);
                    this.TaskStep = 1;
                    break;
                case 1:
                    bool flag1 = true;
                    foreach (Ship ship in this.Ships)
                    {
                        if (!ship.disabled && ship.hasCommand && ship.Active)
                        {
                            if (Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 5000.0)
                                flag1 = false;

                            if (!flag1)
                            {
                                if (ship.InDeepSpace && ship.engineState != Ship.MoveState.Warp && ship.speed ==0 && ship.InCombatTimer <15)
                                    this.FleetTask.EndTask();
                                break;
                            }
                        }
                    }
                    if (!flag1)
                        break;
                    TaskStep = 2;
                    FormationWarpTo(task.AO, FindAveragePosition().RadiansToTarget(task.AO), Vector2.Normalize(task.AO - FindAveragePosition()));
                    foreach (Ship ship in Ships)
                        ship.AI.HasPriorityOrder = true;
                    break;
                case 2:
                    if (IsFleetSupplied())
                    {
                        this.TaskStep = 5;
                        break;
                    }
                    else
                    {
                        bool flag2 = false;
                        if (Vector2.Distance(this.FindAveragePosition(), task.AO) < 15000.0)
                        {
                            foreach (Ship ship in this.Ships)
                            {
                                lock (ship)
                                {
                                    if (ship.InCombat)
                                    {
                                        flag2 = true;
                                        ship.HyperspaceReturn();
                                        ship.AI.OrderQueue.Clear();
                                        break;
                                    }
                                }
                            }
                        }
                        if (!flag2 && Vector2.Distance(this.FindAveragePosition(), task.AO) >= 10000.0)
                            break;
                        this.TaskStep = 3;
                        break;
                    }
                case 3:
                    this.EnemyClumpsDict = Owner.GetGSAI().ThreatMatrix.PingRadarShipClustersByVector(Ships[0].Center, 150000, 10000, this.Owner);
                    //Array<Ship> list2 = new Array<Ship>();
                    //Array<GameplayObject> nearby1 = UniverseScreen.ShipSpatialManager.GetNearby((GameplayObject)this.Ships[0]);
                    //for (int index1 = 0; index1 < nearby1.Count; ++index1)
                    //{
                    //    Ship ship1 = nearby1[index1] as Ship;
                    //    if (ship1 != null && ship1.loyalty != this.Owner && (ship1.loyalty.isFaction || this.Owner.GetRelations(ship1.loyalty).AtWar) && (!list2.Contains(ship1) && (double)Vector2.Distance(ship1.Center, FleetTask.AO) < (double)FleetTask.AORadius && !this.EnemyClumpsDict.ContainsKey(ship1.Center)))
                    //    {
                    //        this.EnemyClumpsDict.Add(ship1.Center, new Array<Ship>());
                    //        this.EnemyClumpsDict[ship1.Center].Add(ship1);
                    //        list2.Add(ship1);
                    //        Array<GameplayObject> nearby2 = UniverseScreen.ShipSpatialManager.GetNearby((GameplayObject)this.Ships[0]);
                    //        for (int index2 = 0; index2 < nearby2.Count; ++index2)
                    //        {
                    //            Ship ship2 = nearby2[index2] as Ship;
                    //            if (ship2 != null && ship2.loyalty != this.Owner && (ship2.loyalty == ship1.loyalty && (double)Vector2.Distance(ship1.Center, ship2.Center) < 10000.0) && !list2.Contains(ship2))
                    //                this.EnemyClumpsDict[ship1.Center].Add(ship2);
                    //        }
                    //    }
                    //}
                    if (this.EnemyClumpsDict.Count == 0 || Vector2.Distance(this.FindAveragePosition(), task.AO) > 25000.0)
                    {
                        Vector2 enemyWithinRadius = this.Owner.GetGSAI().ThreatMatrix.GetPositionOfNearestEnemyWithinRadius(this.Position, task.AORadius, this.Owner);
                        if (enemyWithinRadius == Vector2.Zero)
                        {
                            task.EndTask();
                            break;
                        }
                        this.MoveDirectlyNow(enemyWithinRadius, FindAveragePosition().RadiansToTarget(enemyWithinRadius), Vector2.Normalize(enemyWithinRadius - this.Position));
                        this.TaskStep = 2;
                        break;
                    }
                    else
                    {
                        Array<Vector2> list3 = new Array<Vector2>();
                        foreach (var keyValuePair in this.EnemyClumpsDict)
                            list3.Add(keyValuePair.Key);
                        IOrderedEnumerable<Vector2> orderedEnumerable2 = list3.OrderBy(clumpPos => Vector2.Distance(this.FindAveragePosition(), clumpPos));
                        Array<Ship> list4 = new Array<Ship>();
                        foreach (Ship toAttack in this.EnemyClumpsDict[orderedEnumerable2.First()])
                        {
                            float num6 = 0.0f;
                            foreach (Ship ship in this.Ships)
                            {
                                if (!list4.Contains(ship) && (num6 == 0.0 || num6 < (double)toAttack.GetStrength()))
                                {
                                    ship.AI.Intercepting = true;
                                    ship.AI.OrderAttackSpecificTarget(toAttack);
                                    list4.Add(ship);
                                    num6 += ship.GetStrength();
                                }
                            }
                        }
                        Array<Ship> list5 = new Array<Ship>();
                        foreach (Ship ship in this.Ships)
                        {
                            if (!list4.Contains(ship))
                                list5.Add(ship);
                        }
                        foreach (Ship ship in list5)
                        {
                            ship.AI.Intercepting = true;
                            ship.AI.OrderAttackSpecificTarget(list4[0].AI.Target as Ship);
                        }
                        this.TaskStep = 4;
                        break;
                    }
                case 4:
                    if (IsFleetSupplied())
                    {
                        this.TaskStep = 5;
                        break;
                    }
                    else
                    {
                        bool flag2 = false;
                        foreach (Ship ship in this.Ships)
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
                    IOrderedEnumerable<Planet> orderedEnumerable3 = list6.OrderBy(p => Vector2.Distance(this.Position, p.Position));
                    if (orderedEnumerable3.Count() <= 0)
                        break;
                    this.Position = orderedEnumerable3.First().Position;
                    foreach (Ship ship in this.Ships)
                        ship.AI.OrderResupply(orderedEnumerable3.First(), true);
                    this.TaskStep = 6;
                    break;
                case 6:
                    float num12 = 0.0f;
                    float num13 = 0.0f;
                    foreach (Ship ship in this.Ships)
                    {
                        if (ship.AI.State != AIState.Resupply)
                        {
                            this.TaskStep = 5;
                            return;
                        }
                        ship.AI.HasPriorityOrder = true;
                        num12 += ship.Ordinance;
                        num13 += ship.OrdinanceMax;
                    }
                    if (num12 != (double)num13)
                        break;
                    this.TaskStep = 0;
                    break;
            }
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

        public int CountShipsWithStrength(bool any =false)
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

        public int CountShipsWithStrength(out int troops )
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
        public bool IsFleetAssembled(float radius,  out bool endTask, Vector2 position = default(Vector2))
        {
            if (position == default(Vector2)) position = Position;
            endTask = false;
            bool assembled = true;
            //using (Ships.AcquireReadLock())
            {
                for (int index = 0; index < Ships.Count; index++)
                {
                    Ship ship = Ships[index];
                    if (ship.disabled || !ship.hasCommand || !ship.Active)
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

        public void UpdateAI(float elapsedTime, int which)
        {
            if (FleetTask != null)
            {
                EvaluateTask(elapsedTime);
            }
            else
            {
                if (EmpireManager.Player == Owner || IsCoreFleet )
                    return;
                Owner.GetGSAI().UsedFleets.Remove(which);                
                for (int i = 0; i < Ships.Count; ++i)
                {
                    Ship s = Ships[i];
                    RemoveShipAt(s, i--);

                    s.AI.OrderQueue.Clear();
                    s.AI.State = AIState.AwaitingOrders;
                    s.HyperspaceReturn();
                    s.isSpooling = false;
                    if (s.shipData.Role == ShipData.RoleName.troop)
                        s.AI.OrderRebaseToNearest();
                    else
                        Owner.ForcePoolAdd(s);
                }
                Reset();
            }
        }
        private void RemoveFromAllSquads(Ship ship)
        {
            
            foreach (FleetDataNode fleetDataNode in DataNodes)
            {
                if (fleetDataNode.Ship == ship)
                    fleetDataNode.Ship = (Ship)null;
            }
            foreach (var list in AllFlanks)
            {
                foreach (Squad squad in list)
                {
                    if (squad.Ships.Contains(ship))
                        squad.Ships.QueuePendingRemoval(ship);
                    foreach (FleetDataNode fleetDataNode in squad.DataNodes)
                    {
                        if (fleetDataNode.Ship == ship)
                            fleetDataNode.Ship = (Ship)null;
                    }
                }
            }
        }
        private void RemoveShipAt(Ship ship, int index)
        {
            ship.fleet = null;
            RemoveFromAllSquads(ship);
            Ships.RemoveAtSwapLast(index);
        }

        public bool RemoveShip(Ship ship)
        {
            if (ship.Active && ship.fleet != this)
                Log.Error("{0} : not equal {1}", ship.fleet.Name, Name);
            ship.fleet = null;
            RemoveFromAllSquads(ship);
            if (!Ships.Remove(ship) && ship.Active)
            {
                Log.Error("Ship is not in this fleet");
                return false;
            }
            return true;
        }
        
        public void Update(float elapsedTime)
        {
            HasRepair = false;

            for (int index = Ships.Count - 1; index >= 0; index--)
            {
                Ship ship = Ships[index];
                if (!ship.Active)
                    RemoveShip(ship);
                else AddShip(ship, true);
            }

            Ships.ApplyPendingRemovals();

            if (Ships.Count <= 0 || GoalStack.Count <= 0)
                return;
            GoalStack.Peek().Evaluate(elapsedTime);
        }

        public enum FleetCombatStatus
        {
            Maintain,
            Loose,
            Free
        }

        public sealed class Squad : IDisposable
        {
            public FleetDataNode MasterDataNode = new FleetDataNode();
            public BatchRemovalCollection<FleetDataNode> DataNodes = new BatchRemovalCollection<FleetDataNode>();
            public BatchRemovalCollection<Ship> Ships = new BatchRemovalCollection<Ship>();
            public Fleet Fleet;
            public Vector2 Offset;

            public FleetCombatStatus FleetCombatStatus;

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            ~Squad() { Dispose(false); }

            private void Dispose(bool disposing)
            {
                DataNodes?.Dispose(ref DataNodes);
                Ships?.Dispose(ref Ships);
            }
        }

        public enum FleetGoalType
        {
            AttackMoveTo,
            MoveTo
        }

        public class FleetGoal
        {
            public FleetGoalType Type;
            public Vector2 Velocity = new Vector2();
            public Vector2 MovePosition;
            public Vector2 PositionLast = new Vector2();
            public Vector2 FinalFacingVector;
            public SolarSystem SysToAttack;
            private readonly Fleet Fleet;
            public float FinalFacing;

            public FleetGoal(Fleet fleet, Vector2 movePosition, float facing, Vector2 fVec, FleetGoalType t)
            {
                Type              = t;
                Fleet             = fleet;
                FinalFacingVector = fVec;
                FinalFacing       = facing;
                MovePosition      = movePosition;
            }

            public void Evaluate(float elapsedTime)
            {
                switch (Type)
                {
                    case FleetGoalType.AttackMoveTo:
                        DoAttackMove(elapsedTime);
                        break;
                    case FleetGoalType.MoveTo:
                        DoMove(elapsedTime);
                        break;
                }
            }

            private void DoAttackMove(float elapsedTime)
            {
                Fleet.Position += Fleet.Position.FindVectorToTarget(MovePosition) * Fleet.Speed * elapsedTime;
                Fleet.AssembleFleet(FinalFacing, FinalFacingVector);
                if (Vector2.Distance(Fleet.Position, MovePosition) >= 100.0)
                    return;
                Fleet.Position = MovePosition;
                Fleet.GoalStack.Pop();
            }

            private void DoMove(float elapsedTime)
            {
                Vector2 vector2 = Fleet.Position.FindVectorToTarget(MovePosition);
                float num1 = 0.0f;
                int num2 = 0;
                foreach (Ship ship in Fleet.Ships)
                {
                    if (ship.FleetCombatStatus != FleetCombatStatus.Free && !ship.EnginesKnockedOut)
                    {
                        float num3 = Vector2.Distance(Fleet.Position + ship.FleetOffset, ship.Center);
                        num1 += num3;
                        ++num2;
                    }
                }
                Fleet.Position += vector2 * (Fleet.Speed + 75f) * elapsedTime;
                Fleet.AssembleFleet(FinalFacing, FinalFacingVector);
                if (Vector2.Distance(Fleet.Position, MovePosition) >= 100.0)
                    return;
                Fleet.Position = MovePosition;
                Fleet.GoalStack.Pop();
            }
        }

        protected override void Destroy()
        {
            DataNodes?.Dispose(ref DataNodes);
            GoalStack       = null;
            CenterShips     = null;
            LeftShips       = null;
            RightShips      = null;
            RearShips       = null;
            ScreenShips     = null;
            CenterFlank     = null;
            LeftFlank       = null;
            RightFlank      = null;
            ScreenFlank     = null;
            RearFlank       = null;
            AllFlanks       = null;
            EnemyClumpsDict = null;
            InterceptorDict = null;
            FleetTask       = null;
            base.Destroy();
        }
        public static string GetDefaultFleetNames(int index)
        {
            switch (index)
            {
                case 1:
                    return "First";
                    
                case 2:
                    return "Second";
                    
                case 3:
                    return "Third";
                    
                case 4:
                    return "Fourth";
                    
                case 5:
                    return "Fifth";
                    
                case 6:
                    return "Sixth";
                    
                case 7:
                    return "Seventh";
                    
                case 8:
                    return "Eigth";
                    
                case 9:
                    return "Ninth";
                    
            }
            return "";
        }
    }
}
