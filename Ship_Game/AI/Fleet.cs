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
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.AI
{
    public sealed partial class Fleet : ShipGroup
    {
        public BatchRemovalCollection<FleetDataNode> DataNodes = new BatchRemovalCollection<FleetDataNode>();
        public Guid Guid = Guid.NewGuid();
        public string Name = "";
        
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
        
        private Map<Vector2, Ship[]> EnemyClumpsDict   = new Map<Vector2, Ship[]>();
        private Map<Ship, Array<Ship>> InterceptorDict = new Map<Ship, Array<Ship>>();
        private int DefenseTurns                       = 50;
        private Vector2 TargetPosition                 = Vector2.Zero;
        public Tasks.MilitaryTask FleetTask;
        public FleetCombatStatus Fcs;

        
        public int FleetIconIndex;
        public static UniverseScreen Screen;
        public int TaskStep;
        public bool IsCoreFleet;


        public bool HasRepair;  //fbedard: ships in fleet with repair capability will not return for repair.
        public bool ReadyForWarp { get; private set; }
        public override string ToString() => $"Fleet {Name} size={Ships.Count} pos={Position} guid={Guid}";

        //This file refactored by Gretman

        public Fleet()
        {
            FleetIconIndex = RandomMath.IntBetween(1, 10);
            InitializeGoalStack();
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

        public override void AddShip(Ship ship) => AddShip(ship, false);
        
        public void AddShip(Ship shiptoadd, bool updateOnly)
        {
            this.HasRepair = HasRepair || shiptoadd.hasRepairBeam || (shiptoadd.HasRepairModule && shiptoadd.Ordinance > 0);        
            if (updateOnly && Ships.Contains(shiptoadd)) return;
            if (shiptoadd.fleet != null || Ships.Contains(shiptoadd))
            {
                Log.Warning("ship already in a fleet");
                return; // recover
            }
            if (shiptoadd.shipData.Role == ShipData.RoleName.station || shiptoadd.IsPlatform)
                return;
            base.AddShip(shiptoadd);            
            shiptoadd.fleet = this;            
            SetSpeed();
            AssignPositions(Facing);
            AddShipToDataNode(shiptoadd);
            //shiptoadd.GetAI().FleetNode = figure out how to set the ships datanode
        }

        public int CountCombatSquads => CenterFlank.Count + LeftFlank.Count + RightFlank.Count + ScreenFlank.Count;

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
            for (int i = 0; i < mainShipList.Count; i++)
            {
                Ship ship = mainShipList[i];
                
                if (ship.DesignRole == ShipData.RoleName.fighter ||
                    ship.shipData.ShipCategory == ShipData.Category.Recon)
                {
                    this.ScreenShips.Add(ship);
                    mainShipList.QueuePendingRemoval(ship);
                }
                else if (ship.shipData.Role == ShipData.RoleName.troop ||
                         ship.shipData.Role == ShipData.RoleName.freighter ||
                         ship.shipData.ShipCategory == ShipData.Category.Civilian)
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
                AddShipToDataNode(s);

            }
        }

        private void AddShipToDataNode(Ship ship)
        {
            FleetDataNode fleetDataNode = DataNodes.Find(newship => newship.Ship == ship) ?? new FleetDataNode();
            fleetDataNode.Ship = ship;
            fleetDataNode.ShipName = ship.Name;
            fleetDataNode.FleetOffset = ship.RelativeFleetOffset;
            fleetDataNode.OrdersOffset = ship.RelativeFleetOffset;
            DataNodes.Add(fleetDataNode);
            ship.AI.FleetNode = fleetDataNode;
        }

        private void SortSquad(Array<Ship> allShips, Array<Squad> destSquad, bool sizeOverSpeed = false)
        {
            IOrderedEnumerable<Ship> orderedShips;      //If true, sort by size instead of speed
            if (sizeOverSpeed) { orderedShips = allShips.OrderByDescending(ship => ship.Size);  }
            else               { orderedShips = allShips.OrderByDescending(ship => ship.Speed); }

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

        public void AssembleFleet(float facing, Vector2 facingVec) => AssembleFleet(facing, facingVec, IsCoreFleet);

        public void Reset()
        {
            while (Ships.Count > 0) {
                var ship = Ships.PopLast();
                ship.ClearFleet();
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
                case Tasks.MilitaryTask.TaskType.ClearAreaOfEnemies:         DoClearAreaOfEnemies(FleetTask); break;
                case Tasks.MilitaryTask.TaskType.AssaultPlanet:              DoAssaultPlanet(FleetTask);      break;
                case Tasks.MilitaryTask.TaskType.CorsairRaid:                DoCorsairRaid(elapsedTime); break;
                case Tasks.MilitaryTask.TaskType.CohesiveClearAreaOfEnemies: DoCohesiveClearAreaOfEnemies(FleetTask); break;
                case Tasks.MilitaryTask.TaskType.Exploration:                DoExplorePlanet(FleetTask); break;
                case Tasks.MilitaryTask.TaskType.DefendSystem:               DoDefendSystem(FleetTask); break;
                case Tasks.MilitaryTask.TaskType.DefendClaim:                DoClaimDefense(FleetTask); break;
                case Tasks.MilitaryTask.TaskType.DefendPostInvasion:         DoPostInvasionDefense(FleetTask); break;
                case Tasks.MilitaryTask.TaskType.GlassPlanet:                DoGlassPlanet(FleetTask); break;
            }
            this.Owner.GetGSAI().TaskList.ApplyPendingRemovals();
        }

        private bool IsInFormationWarp()
        {
            foreach (Ship ship in Ships)
            {
                if (ship.AI.State != AIState.FormationWarp ) continue;
                return false;                
            }
            return true;
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

        private void DoExplorePlanet(Tasks.MilitaryTask task) //Mer Gretman Left off here
        {
            Log.Info("DoExplorePlanet called!  " + this.Owner.PortraitName);
            bool eventBuildingFound = true;
            foreach (Building building in task.GetTargetPlanet().BuildingList)
            {
                if (string.IsNullOrEmpty(building.EventTriggerUID)) continue;
                
                    eventBuildingFound = false;
                    break;
                
            }

            bool weHaveTroops = false;
            if (!eventBuildingFound)    //No need to do this part if a task ending scenario has already been found -Gretman
            {
                

                using (this.Ships.AcquireReadLock())
                    foreach (Ship ship in this.Ships)
                    {
                        if (ship.TroopList.Count > 0 || ship.DesignRole == ShipData.RoleName.troop)
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
                        Vector2 nearestShipyard = Owner.RallyPoints.FindMin(planet => Vector2.Distance(task.AO, planet.Center)).Center;                        
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
                            if (!ship.EMPdisabled && ship.hasCommand && ship.Active)
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
                        Vector2 movePosition = task.GetTargetPlanet().Center + Vector2.Normalize(FindAveragePosition() - task.GetTargetPlanet().Center) * 50000f;
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
                        this.Position = task.GetTargetPlanet().Center;
                        this.AssembleFleet(this.Facing, Vector2.Normalize(this.Position - this.FindAveragePosition()));
                        break;
                }
            }
        }

        private void DoAssaultPlanet(Tasks.MilitaryTask task)
        {
            if (!Owner.IsEmpireAttackable(task.GetTargetPlanet().Owner))
            {
                if (task.GetTargetPlanet().Owner == Owner || task.GetTargetPlanet().AnyOfOurTroops(Owner))
                {
                    Tasks.MilitaryTask militaryTask = new Tasks.MilitaryTask
                    {
                        AO = task.GetTargetPlanet().Center,
                        AORadius = 50000f,
                        WhichFleet = task.WhichFleet
                    };
                    militaryTask.SetEmpire(Owner);
                    militaryTask.type = Tasks.MilitaryTask.TaskType.DefendPostInvasion;
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
                var target = task.GetTargetPlanet();
                if (availableTroops == 0)                
                    availableTroops += target.AnyOfOurTroops(Owner) ? 1 : 0;   
                
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
                            Planet goaltarget = Owner.RallyPoints.FindMin(distance => distance.Center.SqDist(FleetTask.AO));

                            if (goaltarget == null)
                            {
                                task.EndTask();
                                break;
                            }

                            Vector2 fVec = Vector2.Normalize(task.AO - goaltarget.Center);
                            Vector2 vector2 = goaltarget.Center;
                            MoveToNow(vector2, vector2.RadiansToTarget(task.AO), fVec);
                            TaskStep = 1;
                            break;

                        case 1:

                            bool nearFleet = IsFleetAssembled(5000, out bool endTask);

                            if (endTask)
                            {
                                task.EndTask();
                                break;
                            }

                            if (nearFleet)
                            {
                                TaskStep = 2;
                                Vector2 movePosition = task.GetTargetPlanet().Center + Vector2.Normalize(FindAveragePosition() - task.GetTargetPlanet().Center) * 125000f;
                                Position = movePosition;
                                FormationWarpTo(movePosition, FindAveragePosition().RadiansToTarget(task.AO), Vector2.Normalize(task.AO - FindAveragePosition()));
                            }
                            break;
                        case 2:
                            //float targetStr = Owner.GetGSAI().ThreatMatrix.PingRadarStr(task.GetTargetPlanet().Center, task.AORadius, Owner);
                            //float fleetStr = GetStrength();
                            //if (Owner.GetGSAI().ThreatMatrix.PingRadarStr(task.GetTargetPlanet().Center, task.AORadius, Owner) > GetStrength())
                            //    task.EndTask();
                            if (!IsFleetAssembled(25000, out endTask))
                            {
                                if (endTask) TaskStep = 1;
                                break;
                            }
                            using (Ships.AcquireReadLock())
                            {
                                foreach (Ship ship in Ships)
                                    ship.AI.HoldPosition();

                            }
                            InterceptorDict.Clear();
                            TaskStep = 3;
                            Position = task.GetTargetPlanet().Center;
                            AssembleFleet(Facing, Vector2.Normalize(Position - FindAveragePosition()));
                            break;
                        case 3:                            
                            //float fleetStrength = GetStrength();

                            if (!IsFleetSupplied())
                            {
                                TaskStep = 5;
                                break;
                            }
                            Planet targetPlanet = task.GetTargetPlanet();
                            if (targetPlanet.GetGroundLandingSpots() < RearShips.Count)
                                BombPlanet(0, task);

                            //TODO: Indiction logic.   this doesnt work. 
                            
                               if (FleetTaskAttackAllEnemiesInAO(targetPlanet.Center, targetPlanet.GravityWellRadius *3, targetPlanet.GravityWellRadius / CountCombatSquads))
                               {
                                   TaskStep = 4;
                                break;
                               }

                            float targetStrength = Owner.GetGSAI().ThreatMatrix.PingRadarStr(task.GetTargetPlanet().Center, task.AORadius, Owner);
                            if (targetStrength > 500 && targetStrength > GetStrength() * 2)
                            {
                                bool near = false;
                                foreach (var ship in RearShips)
                                {
                                    if (FleetTask.AO.InRadius(ship.Center, FleetTask.AORadius))
                                    {
                                        near = true;
                                        break;
                                    }
                                }
                                if (!near)
                                    task.EndTask();
                            }
                            //using (Owner.GetGSAI().TaskList.AcquireReadLock())
                            //    foreach (MilitaryTask militaryTask in Owner.GetGSAI().TaskList)
                            //    {
                            //        if (militaryTask.WaitForCommand && militaryTask.GetTargetPlanet() != null
                            //            && militaryTask.GetTargetPlanet() == task.GetTargetPlanet())
                            //            militaryTask.WaitForCommand = false;
                            //    }

                            break;
                        case 4:                    
                            float theirGroundStrength = GetGroundStrOfPlanet(task.GetTargetPlanet());
                            float ourGroundStrength = FleetTask.GetTargetPlanet().GetGroundStrength(Owner);                         

                            if (!IsInvading(theirGroundStrength, ourGroundStrength, task) && BombPlanet(ourGroundStrength, task) == 0)
                                task.EndTask();
                            else
                                TaskStep = 3;
                            if (!IsFleetSupplied())                            
                                TaskStep = 5;                                
                            
                            break;

                        case 5:
                            Planet rallyPoint = Owner.RallyPoints.FindMin(planet => Position.SqDist(planet.Center));
                            foreach (Ship ship in this.Ships)
                                ship.AI.OrderResupply(rallyPoint, true);
                            TaskStep = 3;
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
                            if (num18 > 0 && num17 < num18 * 0.89f)
                                break;
                            task.Step = 0;
                            break;
                    }
                }
            }
        }

        private bool FleetTaskAttackAllEnemiesInAO(Vector2 center, float radius, float granularity, float minimumStrength = 500)
        {

            EnemyClumpsDict = Owner.GetGSAI().ThreatMatrix.PingRadarShipClustersByVector(center, radius, granularity, Owner);
            bool shipsinAO = false;
            foreach (var ship in Ships)
            {
                shipsinAO = FleetTask.AO.InRadius(ship.Center, FleetTask.AORadius);
                if (shipsinAO) break;
            }
            if (shipsinAO) // || (EnemyClumpsDict?.Count ?? 0) == 0)
            {
                foreach (Ship ship in Ships)
                {
                    if (ship.AI.State == AIState.Bombard) continue;
                    if (RearShips.Contains(ship)) continue;
                    if (ship.AI.EscortTarget != null) continue;
                    ship.AI.Intercepting = false;
                    ship.AI.CombatState = ship.shipData.CombatState;
                    ship.AI.OrderMoveTowardsPosition(center + ship.FleetOffset, ship.Center.Facing(center), false, null);                    
                }
                foreach (Ship rearShip in RearShips)
                    foreach (Ship ship in Ships)
                    {
                        if (ship.AI.State == AIState.Bombard) continue;
                        if (RearShips.Contains(ship)) continue;
                        if (ship.AI.EscortTarget != null) continue;
                        ship.AI.Intercepting = false;
                        ship.AI.CombatState = ship.shipData.CombatState;
                        {
                            ship.DoEscort(rearShip);
                            break;
                        }
                    }
                return true;
            }
            
           
            //Array<Vector2> clumpCenter = new Array<Vector2>();
            //foreach (var keyValuePair in EnemyClumpsDict)
            //    clumpCenter.Add(keyValuePair.Key);
            //IOrderedEnumerable<Vector2> orderedEnumerable2 = clumpCenter.OrderBy(clumpPos => FindAveragePosition().SqDist(clumpPos));

            Array<Ship> available = new Array<Ship>();
            int keysCount = EnemyClumpsDict.Count;
            using (Ships.AcquireReadLock())
            {
                bool noAttackShips = true;
                foreach (Ship ship in Ships)
                {
                    ship.AI.CombatState = ship.shipData.CombatState;
                    if (RearShips.Contains(ship)) continue;
                    noAttackShips = false;
                    if (ship.AI.State == AIState.Bombard) continue;
                    if (ship.Center.InRadius(center, radius)) continue;
                    
                    available.Add(ship);
                    ship.AI.Intercepting = false;                                    
                }
                if (noAttackShips) return false;
                //foreach (Ship[] ships in EnemyClumpsDict.Values)
                //{
                //    if (minimumStrength < 1)
                //        break;
                //    minimumStrength -= ships.Sum(str => str.GetStrength());
                //}
                //if (minimumStrength > 0) return true;

                bool allGroupsCovered = false;
                Array<Ship> assignedShips = new Array<Ship>();
                foreach (Ship[] ships in EnemyClumpsDict.Values) // [orderedEnumerable2.First()])}
                {
                    Ship clumpCenter = null;
                    for (int x = 0; x < ships.Length; x++ )
                    {
                        if (ships[x].Center.InRadius(center, radius))
                        {
                            clumpCenter = ships[x];
                            break;
                        }
                    }
                    if (clumpCenter == null) continue;
                    allGroupsCovered = false;
                    float strength = ships.Sum(str => str.GetStrength());

                    Ship main = null;
                    for (int i = 0; i < available.Count; i++)
                    {                        
                        Ship ship = available[i];
                        if (assignedShips.Contains(ship)) continue;
                        if (main == null)
                        {
                            ship.AI.Intercepting = true;
                            ship.AI.OrderAttackSpecificTarget(clumpCenter);
                            main = ship;
                        }
                        else
                        {
                            ship.AI.HasPriorityTarget = false;
                            ship.AI.Intercepting = false;
                            ship.DoEscort(main);
                            ship.AI.FleetNode.AssistWeight = .75f;
                            ship.AI.FleetNode.DefenderWeight = .75f;
                        }
                        assignedShips.Add(ship);
                        strength -= ship.GetStrength();                        
                        if (strength < 0)
                        {
                            allGroupsCovered = true;
                            break;
                        }
                    }

                }
                foreach(Ship ship in available)
                {
                    if (ship.AI.Intercepting || ship.Center.InRadius(center, radius) 
                                             || ship.AI.HasPriorityOrder || ship.AI.EscortTarget != null)
                        continue;
                    ship.AI.OrderMoveTowardsPosition(center, 0, false, FleetTask.GetTargetPlanet());
                    //ship.AI.OrderMoveDirectlyTowardsPosition(center, 0, Vector2.Zero, true);
                }

                foreach (var ship in RearShips)
                {
                    if (ship.AI.State == AIState.AssaultPlanet) continue;
                    if (ship.AI.HasPriorityOrder) continue;
                    if (ship.DesignRole == ShipData.RoleName.troop) continue;
                    ship.AI.OrderMoveTowardsPosition(center, 0, false, FleetTask.GetTargetPlanet());//  (center, 0, Vector2.Zero, true);
                }


                return true; //allGroupsCovered;
            }
        }

        private int BombPlanet(float ourGroundStrength, Tasks.MilitaryTask task , int freeSpacesNeeded =int.MaxValue)
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

        private bool IsInvading(float thierGroundStrength, float ourGroundStrength, Tasks.MilitaryTask task, int LandingspotsNeeded =5)
        {            
            int freeLandingSpots = task.GetTargetPlanet().GetGroundLandingSpots();
            if (freeLandingSpots < 1)
                return false;

            float planetAssaultStrength = 0.0f;
            foreach (Ship ship in Ships)            
                planetAssaultStrength += ship.PlanetAssaultStrength;
            
            planetAssaultStrength += ourGroundStrength;
            if (planetAssaultStrength < thierGroundStrength) return false;
            if ( freeLandingSpots < LandingspotsNeeded ) return false;
            //if (ourGroundStrength < 1)
            //    return true;
            if (ourGroundStrength > 1)
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

        private float GetGroundStrOfPlanet(Planet p) => p.GetGroundStrengthOther(Owner);        

        private void DoPostInvasionDefense(Tasks.MilitaryTask task)
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
                            if (!ship.EMPdisabled && ship.hasCommand && ship.Active)
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
                        IOrderedEnumerable<Planet> orderedEnumerable1 = list1.OrderBy(planet => Vector2.Distance(task.AO, planet.Center));
                        if (orderedEnumerable1.Count() <= 0)
                            break;
                        Vector2 fVec = Vector2.Normalize(task.AO - orderedEnumerable1.First().Center);
                        Vector2 vector2 = orderedEnumerable1.First().Center;
                        this.MoveToNow(vector2, vector2.RadiansToTarget(task.AO), fVec);
                        this.TaskStep = 1;
                        break;
                    case 1:
                        bool flag2 = true;
                        for (int index = 0; index < this.Ships.Count; index++)
                        {
                            Ship ship = this.Ships[index];
                            if (!ship.EMPdisabled && ship.hasCommand && ship.Active)
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
                        IOrderedEnumerable<Planet> orderedEnumerable3 = list6.OrderBy(p => Vector2.Distance(this.Position, p.Center));
                        if (orderedEnumerable3.Count() <= 0)
                            break;
                        this.Position = orderedEnumerable3.First().Center;
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

        private void DoDefendSystem(Tasks.MilitaryTask task)
        {
            switch (this.TaskStep)
            {
                case -1:
                    bool flag1 = true;
                    foreach (Ship ship in this.Ships)
                    {
                        if (!ship.EMPdisabled && ship.hasCommand && ship.Active)
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
                    IOrderedEnumerable<Planet> orderedEnumerable1 = list1.OrderBy(planet => Vector2.Distance(task.AO, planet.Center));
                    if (orderedEnumerable1.Count() <= 0)
                        break;
                    Vector2 fVec = Vector2.Normalize(task.AO - orderedEnumerable1.First().Center);
                    Vector2 vector2 = orderedEnumerable1.First().Center;
                    this.MoveToNow(vector2, vector2.RadiansToTarget(task.AO), fVec);
                    this.TaskStep = 1;
                    break;
                case 1:
                    bool flag2 = true;
                    foreach (Ship ship in this.Ships)
                    {
                        if (!ship.EMPdisabled && ship.hasCommand && ship.Active)
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
                    IOrderedEnumerable<Planet> orderedEnumerable3 = list6.OrderBy(p => Vector2.Distance(this.Position, p.Center));
                    if (orderedEnumerable3.Count() <= 0)
                        break;
                    this.Position = orderedEnumerable3.First().Center;
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

        private void DoClaimDefense(Tasks.MilitaryTask task)
        {
            switch (TaskStep)
            {
                case 0:
                    Planet rallyPoint = Owner.RallyPoints.FindMin(planet => planet.Center.SqDist(task.AO));
                    if (rallyPoint == null) return;
                    Position = rallyPoint.Center;
                    Vector2 fVec = Vector2.Normalize(task.GetTargetPlanet().Center - Position);
                    MoveToNow(Position, Position.RadiansToTarget(task.GetTargetPlanet().Center), fVec);
                    TaskStep = 1;
                    break;
                case 1:
                    if (!IsFleetAssembled(5000, out bool endtask))
                        break;                    
                    TaskStep = 2;
                    Position = task.GetTargetPlanet().Center;
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
                    if (!incombat && FindAveragePosition().OutsideRadius(task.GetTargetPlanet().Center, 5000f))
                        break;
                    TaskStep = 3;
                    break;
                case 3:                    
                    EnemyClumpsDict = Owner.GetGSAI().ThreatMatrix.PingRadarShipClustersByVector(FleetTask.GetTargetPlanet().Center, 150000,10000,Owner);
                    
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
                                if (!list4.Contains(ship) && (num <1 || num < (double)toAttack.GetStrength()))
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

                    rallyPoint = Owner.RallyPoints.FindMin(planet => Position.SqDist(planet.Center));
                    this.Position = rallyPoint.Center;
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

        private void DoCohesiveClearAreaOfEnemies(Tasks.MilitaryTask task)
        {
            switch (this.TaskStep)
            {
                case 0:
                    //this.TaskStep = 1;
                    //this.DoCohesiveClearAreaOfEnemies(task);
                    //break;
                case 1:
                    
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
                    
                    if (this.Owner.GetGSAI().ThreatMatrix.PingRadarStr(TargetPosition, 75000, Owner) <1)
                    {
                        this.TaskStep = 1;
                        break;
                    }
                    else
                    {
                        
                        if (Vector2.Distance(this.TargetPosition, this.FindAveragePosition()) > 10000
                        && IsInFormationWarp()
                            )
                            break;
                        this.TaskStep = 3;
                        break;
                    }
                case 3:
                    this.EnemyClumpsDict = this.Owner.GetGSAI().ThreatMatrix.PingRadarShipClustersByVector
                        (this.Position, 150000, 10000, this.Owner, truePosition: true);
                   
                    if (this.EnemyClumpsDict.Count == 0)
                    {
                        task.Step = 1;
                        break;
                    }
                    else
                    {
                        Vector2[] list3 = EnemyClumpsDict.Keys.ToArray();
                        //foreach (var keyValuePair in this.EnemyClumpsDict)
                        //    list3.Add(keyValuePair.Key);
                        var orderedEnumerable = list3.OrderBy(clumpPos => this.FindAveragePosition().SqDist(clumpPos)).FirstOrDefault();
                        Array<Ship> list4 = new Array<Ship>();
                        
                        foreach (Ship toAttack in this.EnemyClumpsDict[orderedEnumerable])
                        {
                            Ship flag = null;
                            float num = 0.0f;
                            foreach (Ship ship in this.Ships)
                            {
                                if (!list4.Contains(ship) && (num < 1 || num < toAttack.GetStrength()))
                                {
                                    ship.AI.CombatState = ship.shipData.CombatState;
                                    ship.AI.Intercepting = false;
                                    if (flag == null)
                                    {
                                        ship.AI.Intercepting = true;
                                        ship.AI.OrderAttackSpecificTarget(toAttack);                                        
                                        list4.Add(ship);
                                        num += ship.GetStrength();
                                        flag = ship;
                                    }
                                    else
                                    {
                                        ship.DoEscort(flag);                                        

                                    }
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
                    if (!IsFleetSupplied())
                    {
                        this.TaskStep = 5;
                        break;
                    }

                    bool allInCombat = true;
                    foreach (Ship ship in this.Ships)
                    {
                        if (!ship.AI.BadGuysNear )
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
                    if (!IsFleetSupplied(wantedSupplyRatio: .9f))
                        break;
                    this.TaskStep = 1;
                    break;
            }
        }

        private void DoGlassPlanet(Tasks.MilitaryTask task)
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
                        IOrderedEnumerable<Planet> orderedEnumerable1 = list1.OrderBy(planet => Vector2.Distance(task.AO, planet.Center));
                        if (!orderedEnumerable1.Any())
                            break;
                        Vector2 fVec = Vector2.Normalize(task.AO - orderedEnumerable1.First().Center);
                        Vector2 vector2 = orderedEnumerable1.First().Center;
                        this.MoveToNow(vector2, vector2.RadiansToTarget(task.AO), fVec);
                        this.TaskStep = 1;
                        break;
                    case 1:

                        int step = MoveToPositionIfAssembled(task, task.AO, 15000f, 150000f);
                        if (step == -1)
                            task.EndTask();
                        TaskStep += step;                        
                        break;
                    case 2:
                        if (task.WaitForCommand && this.Owner.GetGSAI().ThreatMatrix.PingRadarStr(task.GetTargetPlanet().Center, 30000f, this.Owner) > 250.0)
                            break;
                        foreach (Ship ship in this.Ships)
                            ship.AI.OrderBombardPlanet(task.GetTargetPlanet());
                        this.TaskStep = 4;
                        break;
                    case 4:
                        if (!IsFleetSupplied())
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
                        IOrderedEnumerable<Planet> orderedEnumerable2 = list2.OrderBy(p => Vector2.Distance(this.Position, p.Center));
                        if (!orderedEnumerable2.Any())
                            break;
                        this.Position = orderedEnumerable2.First().Center;
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
                        if ((int)num6 != (int)num7)
                            break;
                        this.TaskStep = 0;
                        break;
                }
            }
        }

        private void DoClearAreaOfEnemies(Tasks.MilitaryTask task)
        {
            switch (TaskStep)
            {
                case 0:
                    Array<Planet> list1 = new Array<Planet>();
                    foreach (Planet planet in this.Owner.GetPlanets())
                    {
                        if (planet.HasShipyard)
                            list1.Add(planet);
                    }
                    IOrderedEnumerable<Planet> orderedEnumerable1 = list1.OrderBy(planet => Vector2.Distance(task.AO, planet.Center));
                    if (!orderedEnumerable1.Any())
                        break;
                    Vector2 fVec = Vector2.Normalize(task.AO - orderedEnumerable1.First().Center);
                    Vector2 vector2 = orderedEnumerable1.First().Center;
                    this.MoveToNow(vector2, vector2.RadiansToTarget(task.AO), fVec);
                    this.TaskStep = 1;
                    break;
                case 1:
                    int step = MoveToPositionIfAssembled(task, task.AO, 5000f, 7500f);
                    if (step == -1)
                        task.EndTask();
                    TaskStep += step;
     
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
                    if (!IsFleetSupplied())
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
                    IOrderedEnumerable<Planet> orderedEnumerable3 = list6.OrderBy(p => Vector2.Distance(this.Position, p.Center));
                    if (orderedEnumerable3.Count() <= 0)
                        break;
                    this.Position = orderedEnumerable3.First().Center;
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

        private int MoveToPositionIfAssembled(MilitaryTask task, Vector2 position, float assemblyRadius = 5000f, float moveToWithin = 7500f )
        {
            bool nearFleet = IsFleetAssembled(assemblyRadius, out bool endTask);

            if (endTask)
                return -1;

            if (nearFleet)
            {                
                Vector2 movePosition = position + Vector2.Normalize(FindAveragePosition() - position) * moveToWithin;
                Position = movePosition;
                FormationWarpTo(movePosition, FindAveragePosition().RadiansToTarget(position),
                    Vector2.Normalize(position - FindAveragePosition()));
                return 1;
            }
            return 0;
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
                {
                    if (!IsCoreFleet) return;
                    foreach (Ship ship in Ships)
                    {
                        ship.AI.HasPriorityTarget = false;
                        ship.AI.Intercepting = false;
                    }
                    return;
                }
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

            if (DataNodes != null)
                using (DataNodes.AcquireWriteLock())
                    foreach (FleetDataNode fleetDataNode in DataNodes)
                    {
                        if (fleetDataNode.Ship == ship)
                            fleetDataNode.Ship = null;
                    }
            if (AllFlanks == null) return;
            foreach (var list in AllFlanks)
            {
                foreach (Squad squad in list)
                {
                    if (squad.Ships.Contains(ship))
                        squad.Ships.QueuePendingRemoval(ship);
                    if (squad.DataNodes == null) continue;
                    using (squad.DataNodes.AcquireWriteLock())
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
            if (ship == null) return false;
            if (ship.Active && ship.fleet != this)
                Log.Error("{0} : not equal {1}", ship.fleet?.Name, Name);
            if (ship.AI.State != AIState.AwaitingOrders && ship.Active)
                Log.Info("WTF");
            ship.fleet = null;
            RemoveFromAllSquads(ship);
            if (Ships == null) return true;
            Log.Info("Ship removed");
            if (Ships.Remove(ship) || !ship.Active) return true;
            Log.Info("Ship is not in this fleet");
            return false;
        }

        
        public void Update(float elapsedTime)
        {
            HasRepair = false;
            ReadyForWarp = true;
            for (int index = Ships.Count - 1; index >= 0; index--)
            {
                Ship ship = Ships[index];
                if (!ship.Active)
                {
                    RemoveShip(ship);
                    continue;
                }
                AddShip(ship, true);
                ReadyForWarp = ReadyForWarp && ship.ShipReadyForWarp();                
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

        protected override void Destroy()
        {
            if (Ships != null)
                for (int x = Ships.Count - 1; x >= 0; x--)
                {
                    var ship = Ships[x];
                    RemoveShip(ship);
                }

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
