// Type: Ship_Game.Gameplay.Fleet
// Assembly: StarDrive, Version=1.0.9.0, Culture=neutral, PublicKeyToken=null
// MVID: C34284EE-F947-460F-BF1D-3C6685B19387
// Assembly location: E:\Games\Steam\steamapps\common\StarDrive\oStarDrive.exe

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI.Tasks;
using Ship_Game.Debug;
using Ship_Game.Ships;
using System;
using System.Linq;

namespace Ship_Game.AI
{
    public sealed partial class Fleet : ShipGroup
    {
        public BatchRemovalCollection<FleetDataNode> DataNodes = new BatchRemovalCollection<FleetDataNode>();
        public Guid Guid = Guid.NewGuid();
        public string Name = "";

        private Array<Ship> CenterShips = new Array<Ship>();
        private Array<Ship> LeftShips = new Array<Ship>();
        private Array<Ship> RightShips = new Array<Ship>();
        private Array<Ship> RearShips = new Array<Ship>();
        private Array<Ship> ScreenShips = new Array<Ship>();
        public Array<Squad> CenterFlank = new Array<Squad>();
        public Array<Squad> LeftFlank = new Array<Squad>();
        public Array<Squad> RightFlank = new Array<Squad>();
        public Array<Squad> ScreenFlank = new Array<Squad>();
        public Array<Squad> RearFlank = new Array<Squad>();
        public Array<Array<Squad>> AllFlanks = new Array<Array<Squad>>();

        private Map<Vector2, Ship[]> EnemyClumpsDict = new Map<Vector2, Ship[]>();
        private Map<Ship, Array<Ship>> InterceptorDict = new Map<Ship, Array<Ship>>();
        private int DefenseTurns = 50;
        private Vector2 TargetPosition = Vector2.Zero;
        public MilitaryTask FleetTask;
        private MilitaryTask CoreFleetSubTask;
        public FleetCombatStatus Fcs;


        public int FleetIconIndex;
        public static UniverseScreen Screen;
        public int TaskStep;
        public bool IsCoreFleet;

        private Array<Ship> AllButRearShips => Ships.Except(RearShips).ToArrayList();
        public bool HasRepair { get; private set; }  //fbedard: ships in fleet with repair capability will not return for repair.
        public bool HasOrdnanceSupplyShuttles { get; private set; } // FB: fleets with supply bays will be able to resupply ships
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
            //Finding a bug. Added ship should never be null
            if (shiptoadd == null)
            {
                Log.WarningWithCallStack($"Ship Was Null for {Name}");
                return;
            }
            using (Ships.AcquireWriteLock())
            {
                if (shiptoadd.IsPlatformOrStation) return;

                FleetShipAddsRepair(shiptoadd);
                FleetShipAddsOrdnanceSupplyShuttles(shiptoadd);

                if (updateOnly && Ships.Contains(shiptoadd)) return;

                //This is finding a logic bug: Ship is already in a fleet or this fleet already contains the ship.
                //This should likely be two different checks. There is also the possibilty that the ship is in another
                //Fleet ship list.
                if (shiptoadd.fleet != null || Ships.Contains(shiptoadd))
                {
                    Log.Warning("ship already in a fleet");
                    return; // recover
                }

                AddShipToNodes(shiptoadd);
                AssignPositions(Facing);
            }

        }

        private void FleetShipAddsRepair(Ship ship)
        {
            HasRepair = HasRepair || ship.hasRepairBeam || (ship.HasRepairModule && ship.Ordinance > 0);
        }

        private void FleetShipAddsOrdnanceSupplyShuttles( Ship ship)
        {
            HasOrdnanceSupplyShuttles = HasOrdnanceSupplyShuttles || (ship.Carrier.HasSupplyBays && ship.Ordinance >= 100);
        }

        public void AddExistingShip(Ship ship) => AddShipToNodes(ship);

        private void AddShipToNodes(Ship shiptoadd)
        {
            base.AddShip(shiptoadd);
            shiptoadd.fleet = this;
            SetSpeed();
            AddShipToDataNode(shiptoadd);
        }


        public int CountCombatSquads => CenterFlank.Count + LeftFlank.Count + RightFlank.Count + ScreenFlank.Count;

        private void ClearFlankList()
        {
            CenterShips.Clear();
            LeftShips.Clear();
            RightShips.Clear();
            ScreenShips.Clear();
            RearShips.Clear();
            CenterFlank.Clear();
            LeftFlank.Clear();
            RightFlank.Clear();
            ScreenFlank.Clear();
            RearFlank.Clear();
        }

        private void ResetFlankLists()
        {
            ClearFlankList();

            var mainShipList = new Array<Ship>(Ships);
            var largestShip = mainShipList.FindMax(ship => (int)(ship.DesignRole));
            ShipData.RoleName largestCombat = largestShip.DesignRole;

            for (int i = mainShipList.Count - 1; i >= 0; i--)
            {
                Ship ship = mainShipList[i];

                if (ship.DesignRole >= ShipData.RoleName.fighter && ship.DesignRole == largestCombat)
                {
                    ScreenShips.Add(ship);
                    mainShipList.RemoveSwapLast(ship);
                }
                else if (ship.DesignRole            == ShipData.RoleName.troop ||
                         ship.DesignRole            == ShipData.RoleName.freighter ||
                         ship.shipData.ShipCategory == ShipData.Category.Civilian ||
                         ship.DesignRole            == ShipData.RoleName.troopShip
                )
                {
                    RearShips.Add(ship);
                    mainShipList.RemoveSwapLast(ship);
                }
                else if (ship.DesignRole < ShipData.RoleName.fighter)
                {
                    CenterShips.Add(ship);
                    mainShipList.RemoveSwapLast(ship);
                }
                else
                {
                    int leftOver = mainShipList.Count;
                    if (leftOver % 2 == 0)
                        RightShips.Add(ship);
                    else
                        LeftShips.Add(ship);
                    mainShipList.RemoveSwapLast(ship);
                }
            }

            int totalShips = CenterShips.Count;
            foreach (Ship ship in mainShipList.OrderByDescending(ship => ship.GetStrength() + ship.SurfaceArea))
            {
                if (totalShips < 4) CenterShips.Add(ship);
                else if (totalShips < 8) LeftShips.Add(ship);
                else if (totalShips < 12) RightShips.Add(ship);
                else if (totalShips < 16) ScreenShips.Add(ship);
                else if (totalShips < 20 && RearShips.Count == 0) RearShips.Add(ship);

                ++totalShips;
                if (totalShips != 16) continue;
                //so far as i can tell this has zero effect.
                ship.FleetCombatStatus = FleetCombatStatus.Maintain;
                totalShips = 0;
            }

        }
        private enum FlankType
        {
            Left,
            Right
        }

        private void FlankToCenterOffset(Array<Squad> flank, FlankType flankType)
        {
            if (flank.IsEmpty) return;
            int centerSquadCount = Math.Max(1, CenterFlank.Count);
            for (int x = 0; x < flank.Count; x++)
            {
                Squad squad = flank[x];
                var offset = centerSquadCount * 1400 + x * 1400;
                if (flankType == FlankType.Left)
                    offset *= -1;
                squad.Offset = new Vector2(offset, 0f);
            }
        }

        private void LeftFlankToCenterOffset() => FlankToCenterOffset(LeftFlank, FlankType.Left);
        private void RightFlankToCenterOffset() => FlankToCenterOffset(RightFlank, FlankType.Right);

        public void AutoArrange()
        {
            ResetFlankLists();

            SetSpeed();

            CenterFlank = SortSquadBySize(CenterShips);
            LeftFlank   = SortSquadBySpeed(LeftShips);
            RightFlank  = SortSquadBySpeed(RightShips);
            ScreenFlank = SortSquadBySpeed(ScreenShips);
            RearFlank   = SortSquadBySpeed(RearShips);
            AllFlanks.Add(CenterFlank);
            AllFlanks.Add(LeftFlank);
            AllFlanks.Add(RightFlank);
            AllFlanks.Add(ScreenFlank);
            AllFlanks.Add(RearFlank);
            Position = FindAveragePosition();

            ArrangeSquad(CenterFlank, Vector2.Zero);
            ArrangeSquad(ScreenFlank, new Vector2(0.0f, -2500f));
            ArrangeSquad(RearFlank  , new Vector2(0.0f, 2500f));

            LeftFlankToCenterOffset();
            RightFlankToCenterOffset();


            for (int x = 0; x < Ships.Count; x++)
            {
                Ship s = Ships[x];
                AddShipToDataNode(s);
            }
            AutoAssembleFleet(0.0f);

            for (int x = 0; x < Ships.Count; x++)
            {
                Ship s = Ships[x];
                if (s.InCombat)
                    continue;
                s.AI.OrderQueue.Clear();
                s.AI.WayPoints.Clear();
                s.AI.State = AIState.AwaitingOrders;
                s.AI.OrderAllStop();
                s.AI.OrderThrustTowardsPosition(Position + s.FleetOffset, Facing, new Vector2(0.0f, -1f), false);
            }
        }

        private void AddShipToDataNode(Ship ship)
        {
            FleetDataNode fleetDataNode = DataNodes.Find(newship => newship.Ship == ship) ??
                                          DataNodes.Find(newship => newship.Ship == null && newship.ShipName == ship.Name);
            if (fleetDataNode == null)
            {
                fleetDataNode = new FleetDataNode
                {
                    FleetOffset  = ship.RelativeFleetOffset,
                    OrdersOffset = ship.RelativeFleetOffset
                };

                DataNodes.Add(fleetDataNode);
            }
            ship.RelativeFleetOffset = fleetDataNode.FleetOffset;

            fleetDataNode.Ship         = ship;
            fleetDataNode.ShipName     = ship.Name;
            fleetDataNode.OrdersRadius = fleetDataNode.OrdersRadius < 2 ? ship.AI.GetSensorRadius() : fleetDataNode.OrdersRadius;
            ship.AI.FleetNode          = fleetDataNode;
        }

        private enum SquadSortType
        {
            Size,
            Speed
        }

        private Array<Squad> SortSquadBySpeed(Array<Ship> allShips) => SortSquad(allShips, SquadSortType.Speed);
        private Array<Squad> SortSquadBySize(Array<Ship> allShips) => SortSquad(allShips, SquadSortType.Size);

        private Array<Squad> SortSquad(Array<Ship> allShips, SquadSortType sort)
        {
            var destSquad = new Array<Squad>();
            if (allShips.IsEmpty) return destSquad;
            allShips.Sort(ship =>
            {
                switch (sort)
                {
                    case SquadSortType.Size:
                        return -ship.SurfaceArea;
                    case SquadSortType.Speed:
                        return -ship.Speed;
                    default:
                        return 0;
                }
            });

            Squad squad = new Squad { Fleet = this };
            destSquad.Add(squad);
            for (int x = 0; x < allShips.Count; ++x)
            {
                if (squad.Ships.Count < 4)
                    squad.Ships.Add(allShips[x]);
                if (squad.Ships.Count != 4 && x != allShips.Count - 1) continue;

                squad = new Squad { Fleet = this };
                destSquad.Add(squad);
            }
            return destSquad;
        }

        private static void ArrangeSquad(Array<Squad> squad, Vector2 squadOffset)
        {
            int leftSide  = 0;
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
            for (int i = 0; i < AllFlanks.Count; i++)
            {
                Array<Squad> list = AllFlanks[i];
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

                        Vector2 distanceUsingRadians =
                            Vector2.Zero.PointFromRadians((squad.Offset.ToRadians() + facing), squad.Offset.Length());
                        squad.Ships[index].FleetOffset =
                            distanceUsingRadians + Vector2.Zero.PointFromRadians(radiansAngle + facing, 500f);

                        distanceUsingRadians                   = Vector2.Zero.PointFromRadians(radiansAngle, 500f);
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
            if (Empire.Universe.SelectedFleet == this)
                Empire.Universe.DebugWin?.DrawCircle(DebugModes.AO, Position, FleetTask.AORadius, Color.AntiqueWhite);
            switch (FleetTask.type)
            {
                case MilitaryTask.TaskType.ClearAreaOfEnemies:         DoClearAreaOfEnemies(FleetTask); break;
                case MilitaryTask.TaskType.AssaultPlanet:              DoAssaultPlanet(FleetTask); break;
                case MilitaryTask.TaskType.CorsairRaid:                DoCorsairRaid(elapsedTime); break;
                case MilitaryTask.TaskType.CohesiveClearAreaOfEnemies: DoCohesiveClearAreaOfEnemies(FleetTask); break;
                case MilitaryTask.TaskType.Exploration:                DoExplorePlanet(FleetTask); break;
                case MilitaryTask.TaskType.DefendSystem:               DoDefendSystem(FleetTask); break;
                case MilitaryTask.TaskType.DefendClaim:                DoClaimDefense(FleetTask); break;
                case MilitaryTask.TaskType.DefendPostInvasion:         DoPostInvasionDefense(FleetTask); break;
                case MilitaryTask.TaskType.GlassPlanet:                DoGlassPlanet(FleetTask); break;
            }
            Owner.GetEmpireAI().TaskList.ApplyPendingRemovals();
        }

        private void DoExplorePlanet(MilitaryTask task) //Mer Gretman Left off here
        {
            bool eventBuildingFound = task.TargetPlanet.EventsOnBuildings();

            if (EndInvalidTask(!StillMissionEffective(task)) || !StillCombatEffective(task))
            {
                task.IsCoreFleetTask = false;
                FleetTask = null;
                TaskStep = 0;
                return;
            }

             if (EndInvalidTask(eventBuildingFound || task.TargetPlanet.Owner != null
                                                   && task.TargetPlanet.Owner != Owner))
                return;

            switch (TaskStep)
            {
                case 0:
                    FleetTaskGatherAtRally(task);
                    TaskStep = 1;
                    break;
                case 1:
                    if (!HasArrivedAtRallySafely(5000)) break;
                    GatherAtAO(task, distanceFromAO: 50000f);
                    TaskStep = 2;
                    break;
                case 2:
                    if (ArrivedAtOffsetRally(task))
                        TaskStep = 3;
                    break;
                case 3:
                    EscortingToPlanet(task);
                    TaskStep = 4;
                    break;

                case 4:
                    WaitingForPlanetAssault(task);
                    if (EndInvalidTask(!IsFleetSupplied())) break;
                    if (ShipsOffMission(task))
                        TaskStep = 3;
                    break;
                case 5:
                    for (int i = 0; i < Ships.Count; i++)
                    {
                        Ships[i].AI.OrderLandAllTroops(task.TargetPlanet);
                    }
                    Position = task.TargetPlanet.Center;
                    AssembleFleet(Facing, FindAveragePosition().DirectionToTarget(Position));
                    break;
            }
        }
        private void DoPostInvasionDefense(MilitaryTask task)
        {
            if (EndInvalidTask(--DefenseTurns <= 0))
                return;

            switch (TaskStep)
            {
                case 0:
                    if (EndInvalidTask(FleetTask.TargetPlanet == null))
                        break;

                    SetPostInvasionFleetCombat();

                    TaskStep = 1;
                    break;
                case 1:

                    if (EndInvalidTask(!IsFleetSupplied()))
                        break;
                    PostInvasionStayInAO();
                    TaskStep = 2;
                    break;
                case 2:
                    if (PostInvastionAnyShipsOutOfAO(task))
                    {
                        TaskStep = 1;
                        break;
                    }
                    if (Ships.Any(ship => ship.InCombat))
                        break;
                    AssembleFleet(1, Vector2.Zero);
                    break;
            }
        }
        private void DoAssaultPlanet(MilitaryTask task)
        {
            if (!Owner.IsEmpireAttackable(task.TargetPlanet.Owner))
            {
                if (task.TargetPlanet.Owner == Owner || task.TargetPlanet.AnyOfOurTroops(Owner))
                {
                    var militaryTask = MilitaryTask.CreatePostInvasion(task.TargetPlanet.Center, task.WhichFleet, Owner);
                    Owner.GetEmpireAI().RemoveFromTaskList(task);
                    FleetTask = militaryTask;
                    Owner.GetEmpireAI().AddToTaskList(militaryTask);
                    for (int x =0; x < Ships.Count; ++x)
                    {
                        var ship = Ships[x];
                        if (ship.Carrier.AnyPlanetAssaultAvailable)
                            RemoveShip(ship);
                    }

                }
                else
                {
                    Log.Info($"Invasion ({Owner.Name}) planet ({task.TargetPlanet}) Not attackable");
                    task.EndTask();
                }
                return;
            }

            if (EndInvalidTask(!StillMissionEffective(task)) | !StillCombatEffective(task))
            {
                task.IsCoreFleetTask = false;
                FleetTask = null;
                TaskStep = 0;
                return;
            }

            switch (TaskStep)
            {
                case 0:
                    FleetTaskGatherAtRally(task);
                    SetRestrictedCombatWeights(5000);
                    TaskStep = 1;
                    break;
                case 1:
                    if (!HasArrivedAtRallySafely(5000))
                        break;

                    GatherAtAO(task, distanceFromAO: Owner.ProjectorRadius * 1.1f);
                    TaskStep = 2;
                    break;
                case 2:
                    if (!ArrivedAtOffsetRally(task)) break;
                    TaskStep = 3;

                    Position = task.TargetPlanet.Center;
                    AssembleFleet(Facing, Vector2.Normalize(Position - FindAveragePosition()));
                    break;
                case 3:
                    EscortingToPlanet(task);
                    TaskStep = 4;
                    break;
                case 4:
                    WaitingForPlanetAssault(task);
                    if (ShipsOffMission(task))
                        TaskStep = 3;
                    if (!IsFleetSupplied())
                        TaskStep = 5;
                    break;

                case 5:
                    SendFleetToResupply();
                    TaskStep = 3;
                    break;
            }
        }
        private void DoCorsairRaid(float elapsedTime)
        {
            if (TaskStep != 0)
                return;

            FleetTask.TaskTimer -= elapsedTime;
            Ship station = Owner.GetShips().Find(ship => ship.Name == "Corsair Asteroid Base");
            if (FleetTask.TaskTimer > 0.0)
            {
                EndInvalidTask(Ships.Count == 0);
                return;
            }
            if (EndInvalidTask(station == null)) return;

            AssembleFleet(0.0f, Vector2.One);
            // ReSharper disable once PossibleNullReferenceException station should never be null here
            FormationWarpTo(station.Position, 0.0f, Vector2.One);
            FleetTask.EndTaskWithMove();
        }
        private void DoDefendSystem(MilitaryTask task)
        {
            //this is currently unused. the system needs to be created with a defensive fleet.
            //no defensive fleets are created during the game. yet...
            switch (TaskStep)
            {
                case -1:
                    FleetTaskGatherAtRally(task);
                    SetAllShipsPriorityOrder();
                    break;
                case 0:
                    FleetTaskGatherAtRally(task);
                    TaskStep = 1;
                    break;
                case 1:
                    if (!ArrivedAtOffsetRally(task))
                        break;
                    GatherAtAO(task, distanceFromAO: 125000f);
                    TaskStep = 2;
                    SetAllShipsPriorityOrder();
                    break;
                case 2:
                    if (ArrivedAtOffsetRally(task))
                        TaskStep = 3;
                    break;
                case 3:
                    if (!AttackEnemyStrengthClumpsInAO(task))
                        DoOrbitTaskArea(task);

                    TaskStep = 4;
                    break;
                case 4:
                    if (EndInvalidTask(task.TargetPlanet.Owner != null))
                        break;
                    if (!IsFleetSupplied())
                        TaskStep = 5;

                    if (ShipsOffMission(task))
                        TaskStep = 3;
                    break;
                case 5:
                    SendFleetToResupply();
                    TaskStep = 6;
                    break;

                case 6:
                    if (!IsFleetSupplied())
                        break;
                    TaskStep = 3;
                    break;
            }
        }

        private void DoClaimDefense(MilitaryTask task)
        {
            if (EndInvalidTask(task.TargetPlanet.Owner != null))
                return;
            task.AO = task.TargetPlanet.Center;
            switch (TaskStep)
            {
                case 0:
                    SetLooseCombatWeights();
                    FleetTaskGatherAtRally(task);
                    TaskStep = 1;
                    break;
                case 1:
                    if (!HasArrivedAtRallySafely(5000)) break;
                    GatherAtAO(task, 10000);
                    TaskStep = 2;
                    break;
                case 2:
                    if (!ArrivedAtCombatRally(task))
                        break;
                    TaskStep = 3;
                    CancelFleetMoveInArea(task.AO, task.AORadius * 2);
                    break;
                case 3:
                    if (!DoOrbitTaskArea(task))
                        AttackEnemyStrengthClumpsInAO(task);

                    TaskStep = 4;
                    break;

                case 4:
                    if (!IsFleetSupplied())
                        TaskStep = 5;
                    //if (Ships.Any(s => s.Speed < 1))
                    //    Log.Error("");
                    if (ShipsOffMission(task))
                        TaskStep = 3;
                    break;

                case 5:
                    SendFleetToResupply();
                    TaskStep = 6;
                    break;

                case 6:
                    if (!IsFleetSupplied())
                        break;
                    TaskStep = 3;
                    break;
            }
        }

        private void DoCohesiveClearAreaOfEnemies(MilitaryTask task)
        {
            if (CoreFleetSubTask == null) TaskStep = 1;

            switch (TaskStep)
            {
                case 1:
                    if (EndInvalidTask(!MoveFleetToNearestCluster(task)))
                    {
                        CoreFleetSubTask = null;
                        break;
                    }

                    SetRestrictedCombatWeights(task.AORadius);
                    TaskStep = 2;
                    break;
                case 2:
                    if (!ArrivedAtCombatRally(CoreFleetSubTask)) break;
                    TaskStep = 3;
                    break;

                case 3:

                    if (!AttackEnemyStrengthClumpsInAO(CoreFleetSubTask))
                    {
                        TaskStep = 1;
                        CoreFleetSubTask = null;
                        break;
                    }
                    CancelFleetMoveInArea(task.AO, task.AORadius * 2);
                    TaskStep = 4;
                    break;
                case 4:
                    if (!IsFleetSupplied())
                    {
                        TaskStep = 5;
                        break;
                    }
                    if (ShipsOffMission(CoreFleetSubTask))
                        TaskStep = 3;
                    for (int x = 0; x < Ships.Count; x++)
                    {
                        var ship = Ships[x];
                        if (ship.AI.BadGuysNear)
                            ship.AI.HasPriorityOrder = false;
                    }
                    break;

                case 5:
                    SendFleetToResupply();
                    TaskStep =4;
                    break;
                case 6:
                    IsFleetSupplied(wantedSupplyRatio: .9f);

                    break;
            }
        }

        private void DoGlassPlanet(MilitaryTask task)
        {
            if (task.TargetPlanet.Owner == Owner || task.TargetPlanet.Owner == null)
                task.EndTask();
            else if (task.TargetPlanet.Owner != null & task.TargetPlanet.Owner != Owner && !task.TargetPlanet.Owner.GetRelations(Owner).AtWar)
            {
                task.EndTask();
            }
            else
            {
                switch (TaskStep)
                {
                    case 0:
                        Array<Planet> list1 = new Array<Planet>();
                        foreach (Planet planet in Owner.GetPlanets())
                        {
                            if (planet.HasSpacePort)
                                list1.Add(planet);
                        }
                        IOrderedEnumerable<Planet> orderedEnumerable1 = list1.OrderBy(planet => Vector2.Distance(task.AO, planet.Center));
                        if (!orderedEnumerable1.Any())
                            break;
                        Vector2 fVec = Vector2.Normalize(task.AO - orderedEnumerable1.First().Center);
                        Vector2 vector2 = orderedEnumerable1.First().Center;
                        MoveToNow(vector2, vector2.RadiansToTarget(task.AO), fVec);
                        TaskStep = 1;
                        break;
                    case 1:

                        int step = MoveToPositionIfAssembled(task, task.AO, 15000f, 150000f);
                        if (step == -1)
                            task.EndTask();
                        TaskStep += step;
                        break;
                    case 2:
                        if (task.WaitForCommand && Owner.GetEmpireAI().ThreatMatrix.PingRadarStr(task.TargetPlanet.Center, 30000f, Owner) > 250.0)
                            break;
                        foreach (Ship ship in Ships)
                            ship.AI.OrderBombardPlanet(task.TargetPlanet);
                        TaskStep = 4;
                        break;
                    case 4:
                        if (!IsFleetSupplied())
                        {
                            TaskStep = 5;
                            break;
                        }
                        else
                        {
                            TaskStep = 2;
                            break;
                        }
                    case 5:
                        Array<Planet> list2 = new Array<Planet>();
                        foreach (Planet planet in Owner.GetPlanets())
                        {
                            if (planet.HasSpacePort)
                                list2.Add(planet);
                        }
                        IOrderedEnumerable<Planet> orderedEnumerable2 = list2.OrderBy(p => Vector2.Distance(Position, p.Center));
                        if (!orderedEnumerable2.Any())
                            break;
                        Position = orderedEnumerable2.First().Center;
                        foreach (Ship ship in Ships)
                            ship.AI.OrderResupply(orderedEnumerable2.First(), true);
                        TaskStep = 6;
                        break;
                    case 6:
                        float num6 = 0.0f;
                        float num7 = 0.0f;
                        foreach (Ship ship in Ships)
                        {
                            if (ship.AI.State != AIState.Resupply)
                            {
                                TaskStep = 5;
                                return;
                            }
                            ship.AI.HasPriorityOrder = true;
                            num6 += ship.Ordinance;
                            num7 += ship.OrdinanceMax;
                        }
                        if ((int)num6 != (int)num7)
                            break;
                        TaskStep = 0;
                        break;
                }
            }
        }

        private void DoClearAreaOfEnemies(MilitaryTask task)
        {
            if (EndInvalidTask(!StillCombatEffective(task)))
            {
                FleetTask = null;
                TaskStep = 0;
                return;
            }
            switch (TaskStep)
            {
                case 0:
                    FleetTaskGatherAtRally(task);
                    SetRestrictedCombatWeights(task.AORadius);
                    TaskStep = 1;
                    break;
                case 1:
                    if (!HasArrivedAtRallySafely(5000))
                        break;

                    GatherAtAO(task, distanceFromAO: 10000f);
                    TaskStep = 2;
                    break;
                case 2:
                    if (!ArrivedAtCombatRally(task)) break;
                    TaskStep = 3;

                    Position = task.AO;
                    AssembleFleet(Facing, Vector2.Normalize(Position - FindAveragePosition()));
                    break;
                case 3:
                    if (!AttackEnemyStrengthClumpsInAO(task))
                        DoOrbitTaskArea(task);
                    else
                        CancelFleetMoveInArea(task.AO, task.AORadius * 2);
                    TaskStep = 4;
                    break;
                case 4:
                    if (EndInvalidTask(!Owner.IsEmpireAttackable(task.TargetPlanet.Owner)))
                        break;
                    if (ShipsOffMission(task))
                        TaskStep = 3;
                    if (!IsFleetSupplied())
                        TaskStep = 5;
                    break;
                case 5:
                    SendFleetToResupply();
                    TaskStep = 6;
                    break;
                case 6:
                    if (!IsFleetSupplied(wantedSupplyRatio: .9f))
                        break;
                    TaskStep = 4;
                    break;
            }
        }

        private bool EndInvalidTask(bool condition)
        {
            if (!condition) return false;
            FleetTask.EndTask();
            return true;
        }

        public CombatStatus InCombatAtAO => FleetInAreaInCombat(FleetTask.AO, FleetTask.AORadius);
        /// <summary>
        /// Return true if order successfull. Fails when enemies near.
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        private bool DoOrbitTaskArea(MilitaryTask task)
        {
            CombatStatus status = FleetInAreaInCombat(task.AO, task.AORadius);

            if (status < CombatStatus.ClearSpace) return false;

            DoOrbitAreaRestricted(task.TargetPlanet, task.AO, task.AORadius);
            return true;
        }

        private void CancelFleetMoveInArea(Vector2 pos, float radius )
        {
            foreach(var ship in Ships)
            {
                if (ship.Center.OutsideRadius(pos, radius)) continue;
                if (ship.AI.State != AIState.FormationWarp) continue;
                ship.AI.State = AIState.AwaitingOrders;
                ship.AI.ClearPriorityOrder();
            }
        }

        private void SetPriorityOrderTo(Array<Ship> ships)
        {
            for (int i = 0; i < ships.Count; ++i)
            {
                Ship ship = Ships[i];
                ship.AI.SetPriorityOrder(true);
            }
        }
        private void SetAllShipsPriorityOrder() => SetPriorityOrderTo(Ships);

        private void FleetTaskGatherAtRally(MilitaryTask task)
        {
            Planet planet           = Owner.FindNearestRallyPoint(task.AO);
            Vector2 movePoint       = planet.Center;
            Vector2 finalFacing     = movePoint.DirectionToTarget(task.AO);

            SetAllShipsPriorityOrder();
            MoveToNow(movePoint, movePoint.RadiansToTarget(task.AO), finalFacing);
        }

        private bool HasArrivedAtRallySafely(float distanceFromPosition)
        {
            MoveStatus status = IsFleetAssembled(distanceFromPosition);
            if (status == MoveStatus.Dispersed)
                return false;
            bool invalid = status == MoveStatus.InCombat;
            if (invalid)
            {
                DebugInfo(FleetTask, $"Fleet in Combat");
            }
            return !EndInvalidTask(invalid);
        }

        private void GatherAtAO(MilitaryTask task, float distanceFromAO)
        {
            Vector2 movePosition = task.AO.OffSetTo(FindAveragePosition(), distanceFromAO);
            Position             = movePosition;
            float facing         = FindAveragePosition().RadiansToTarget(task.AO);
            Vector2 fleetFacing  = FindAveragePosition().DirectionToTarget(task.AO);

            FormationWarpTo(movePosition, facing, fleetFacing);
        }

        private void HoldFleetPosition()
        {
            for (int index = 0; index < Ships.Count; index++)
            {
                Ship ship = Ships[index];
                ship.AI.State = AIState.HoldPosition;
                if (ship.shipData.Role == ShipData.RoleName.troop)
                    ship.AI.HoldPosition();
            }
        }

        private bool ArrivedAtOffsetRally(MilitaryTask task)
        {
            if (IsFleetAssembled(5000f) != MoveStatus.Assembled)
                return false;

            HoldFleetPosition();

            InterceptorDict.Clear();
            return true;
        }

        private bool ArrivedAtCombatRally(MilitaryTask task)
        {
            return IsFleetAssembled(5000f, task.AO) != MoveStatus.Dispersed;
        }

        private Ship[] AvailableShips => AllButRearShips.Filter(ship => !ship.AI.HasPriorityOrder);

        private bool AttackEnemyStrengthClumpsInAO(MilitaryTask task)
        {
            Map<Vector2, float> enemyClumpsDict = Owner.GetEmpireAI().ThreatMatrix
                .PingRadarStrengthClusters(task.AO, task.AORadius, 2500, Owner);

            if (enemyClumpsDict.Count == 0)
                return false;


            var availableShips = new Array<Ship>(AvailableShips);

            foreach (var kv in enemyClumpsDict.OrderBy(dis => dis.Key.SqDist(task.AO)))
            {
                if (availableShips.Count == 0) break;
                float attackStr = 0.0f;
                for (int x = availableShips.Count - 1; x >= 0; x--)
                {
                    if (attackStr > kv.Value * 3) break;

                    Ship ship       = availableShips[x];
                    if (ship.AI.HasPriorityOrder || ship.InCombat)
                    {
                        availableShips.RemoveAtSwapLast(x);
                        continue;
                    }
                    Vector2 vFacing = ship.Center.DirectionToTarget(kv.Key);
                    float facing    = ship.Center.RadiansToTarget(kv.Key);
                    ship.AI.OrderMoveTowardsPosition(kv.Key, facing, true, null);
                    ship.ForceCombatTimer();

                    availableShips.RemoveAtSwapLast(x);
                    attackStr += ship.GetStrength();
                }
            }
            foreach (Ship needEscort in RearShips)
            {
                if (availableShips.IsEmpty) break;
                Ship ship = availableShips.PopLast();
                ship.DoEscort(needEscort);

            }

            foreach (Ship ship in availableShips)
                ship.AI.OrderMoveDirectlyTowardsPosition(task.AO, 0, true);

            return true;
        }
        private bool MoveFleetToNearestCluster(MilitaryTask task)
        {
            ThreatMatrix.StrengthCluster strengthCluster = new ThreatMatrix.StrengthCluster
            {
                Empire = Owner,
                Granularity = 5000f,
                Postition = task.AO,
                Radius = task.AORadius
            };

            strengthCluster = Owner.GetEmpireAI().ThreatMatrix.FindLargestStengthClusterLimited(strengthCluster, GetStrength(), FindAveragePosition());
            if (strengthCluster.Strength <= 0) return false;
            CoreFleetSubTask = new MilitaryTask
            {
                AO = strengthCluster.Postition,
                AORadius = strengthCluster.Granularity
            };
            GatherAtAO(CoreFleetSubTask, 7500);
            return true;
        }
        private bool ShipsOffMission(MilitaryTask task)
        {
            return AllButRearShips.Any(ship => !ship.AI.HasPriorityOrder
                                     && (!ship.InCombat
                                         && ship.Center.OutsideRadius(task.AO, task.AORadius * 1.5f)));
        }

        private void SetRestrictedCombatWeights(float ordersRadius)
        {
            for (int x = 0; x < Ships.Count; x++)
            {
                var ship = Ships[x];

                ship.AI.FleetNode.AssistWeight   = 1f;
                ship.AI.FleetNode.DefenderWeight = 1f;
                ship.AI.FleetNode.OrdersRadius   = ordersRadius;
            }
        }
        private void SetLooseCombatWeights()
        {
            for (int x = 0; x < Ships.Count; x++)
            {
                var ship = Ships[x];

                ship.AI.FleetNode.AssistWeight = 1f;
                ship.AI.FleetNode.DefenderWeight = 1f;
                ship.AI.FleetNode.OrdersRadius = ship.SensorRange;
            }
        }


        private void WaitingForPlanetAssault(MilitaryTask task)
        {
            float theirGroundStrength = GetGroundStrOfPlanet(task.TargetPlanet);
            float ourGroundStrength   = FleetTask.TargetPlanet.GetGroundStrength(Owner);
            bool invading             = IsInvading(theirGroundStrength, ourGroundStrength, task);
            bool bombing              = BombPlanet(ourGroundStrength, task);
            if(!bombing && !invading)
                EndInvalidTask(true);
        }

        private void SendFleetToResupply()
        {
            Planet rallyPoint = Owner.RallyShipYardNearestTo(FindAveragePosition());
            if (rallyPoint == null) return;
            for (int x = 0; x < Ships.Count; x++)
            {
                Ship ship = Ships[x];
                if (ship.AI.HasPriorityOrder) continue;
                ship.AI.OrderResupply(rallyPoint, true);
            }
        }

        private void DebugInfo(MilitaryTask task, string text)
            => Empire.Universe?.DebugWin?.DebugLogText($"{task.type}: ({Owner.Name}) Planet: {task.TargetPlanet?.Name ?? "None"} {text}", DebugModes.Normal);

        private bool StillCombatEffective(MilitaryTask task)
        {
            float targetStrength =
                Owner.GetEmpireAI().ThreatMatrix.PingRadarStr(task.AO, task.AORadius, Owner);
            float fleetStrengthThreshold = GetStrength() * 2;
            if (!(targetStrength >= fleetStrengthThreshold))
                return true;
            DebugInfo(task, $"Enemy Strength too high. Them: {targetStrength} Us: {fleetStrengthThreshold}");
            return false;
        }

        private bool StillMissionEffective(MilitaryTask task)
        {
            bool troopsOnPlanet        = task.TargetPlanet.AnyOfOurTroops(Owner);
            bool noShips               = Ships.Any(troops => troops.Carrier.AnyPlanetAssaultAvailable);
            bool stillMissionEffective = troopsOnPlanet || noShips;
            if (!stillMissionEffective)
                DebugInfo(task, $" No Troops on Planet and No Ships.");
            return stillMissionEffective;
        }

        private void InvadeTactics(Array<Ship> flankShips, string type, Vector2 moveTo)
        {
            foreach (Ship ship in flankShips)
            {
                ShipAI ai = ship.AI;
                ai.CombatState = ship.shipData.CombatState;
                if (!ship.Center.OutsideRadius(FleetTask.TargetPlanet.Center, FleetTask.AORadius))
                    continue;

                ai.CancelIntercept();
                ai.FleetNode.AssistWeight = 1f;
                ai.FleetNode.DefenderWeight = 1f;
                ai.FleetNode.OrdersRadius = ship.maxWeaponsRange;
                switch (type) {
                    case "screen":
                        if (!ship.InCombat)
                            ai.OrderMoveDirectlyTowardsPosition(moveTo + ship.FleetOffset, 1, false);
                        break;
                    case "rear":
                        if (!ai.HasPriorityOrder)
                            ai.OrderMoveDirectlyTowardsPosition(moveTo + ship.FleetOffset, Facing, Vector2.Zero, false, Speed * 0.75f);
                        break;
                    case "center":
                        if (!ship.InCombat || (ai.State != AIState.Bombard && ship.DesignRole != ShipData.RoleName.bomber))
                            ai.OrderMoveDirectlyTowardsPosition(moveTo + ship.FleetOffset, 1, false);
                        break;
                    case "side":
                        if (!ship.InCombat)
                            ai.OrderMoveDirectlyTowardsPosition(moveTo + ship.FleetOffset, 1, false);
                        break;
                }
            }
        }

        private bool EscortingToPlanet(MilitaryTask task)
        {
            var center = task.TargetPlanet.Center;
            AttackEnemyStrengthClumpsInAO(task);
            InvadeTactics(ScreenShips, "screen", center);
            InvadeTactics(CenterShips, "center", center);
            InvadeTactics(RearShips, "rear", center);
            InvadeTactics(RightShips, "side", center);
            InvadeTactics(LeftShips, "side", center);

            return !task.TargetPlanet.AnyOfOurTroops(Owner) || Ships.Any(bombers => bombers.AI.State == AIState.Bombard);
        }

        private bool StartBombPlanet(MilitaryTask task) => StartStopBombing(true, task);
        private bool StopBombPlanet(MilitaryTask task) => StartStopBombing(false, task);
        private bool StartStopBombing(bool doBombing, MilitaryTask task)
        {
            var bombers = Ships.Filter(ship => ship.BombBays.Count > 0);
            foreach (var ship in bombers)
            {
                if (doBombing)
                {
                    if (ship.AI.HasPriorityOrder || ship.AI.State == AIState.Bombard) continue;
                    ship.AI.OrderBombardPlanet(task.TargetPlanet);
                }
                else if (ship.AI.State == AIState.Bombard)
                    ship.AI.ClearOrdersNext = true;
            }
            return bombers.Length > 0;
        }

        private bool BombPlanet(float ourGroundStrength, MilitaryTask task , int freeSpacesNeeded = 5)
        {
            bool doBombs = !(ourGroundStrength > 0 && freeSpacesNeeded >= task.TargetPlanet.GetGroundLandingSpots());
            return StartStopBombing(doBombs, task);
        }

        private bool IsInvading(float theirGroundStrength, float ourGroundStrength, MilitaryTask task, int LandingspotsNeeded =5)
        {
            int freeLandingSpots = task.TargetPlanet.GetGroundLandingSpots();
            if (freeLandingSpots < 1)
                return false;
            float planetAssaultStrength = 0.0f;
            foreach (Ship ship in RearShips)
                planetAssaultStrength += ship.Carrier.PlanetAssaultStrength;

            planetAssaultStrength += ourGroundStrength;
            if (planetAssaultStrength < theirGroundStrength *.75f)
            {
                DebugInfo(task, $"Fail insuffcient forces. us: {planetAssaultStrength} them:{theirGroundStrength}");
                return false;
            }
            if (freeLandingSpots < LandingspotsNeeded)
            {
                DebugInfo(task,$"fail insuffcient landing space. planetHas: {freeLandingSpots} Needed: {LandingspotsNeeded}");
                return false;
            }

            if (ourGroundStrength < 1)
                StopBombPlanet(task);

            if (Ships.Find(ship=> ship.Center.InRadius(task.AO,task.AORadius)) != null)
            OrderShipsToInvade(RearShips, task);
            return true;
        }

        private void OrderShipsToInvade(Array<Ship> ships, MilitaryTask task)
        {
            foreach (Ship ship in ships)
            {
                ship.AI.OrderLandAllTroops(task.TargetPlanet);
                ship.AI.SetPriorityOrder(false);
            }
        }

        private float GetGroundStrOfPlanet(Planet p) => p.GetGroundStrengthOther(Owner);

        private void SetPostInvasionFleetCombat()
        {
            foreach (var node in DataNodes)
            {
                node.OrdersRadius = FleetTask.AORadius;
                node.AssistWeight = 1;
                node.DPSWeight = -1;
            }
        }

        private void PostInvasionStayInAO()
        {
            foreach (var ship in Ships)
            {

                if (ship.Center.SqDist(FleetTask.AO) > ship.AI.FleetNode.OrdersRadius)
                    ship.AI.OrderThrustTowardsPosition(FleetTask.AO + ship.FleetOffset, 1f, Vector2.Zero, true);
            }
        }

        private bool PostInvastionAnyShipsOutOfAO(MilitaryTask task) =>
            Ships.Any(ship => task.AO.OutsideRadius(ship.Center, ship.AI.FleetNode.OrdersRadius));

        private int MoveToPositionIfAssembled(MilitaryTask task, Vector2 position, float assemblyRadius = 5000f, float moveToWithin = 7500f )
        {
            MoveStatus nearFleet = IsFleetAssembled(assemblyRadius, task.AO);

            if (nearFleet == MoveStatus.InCombat)
                return -1;

            if (nearFleet == MoveStatus.Assembled)
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
                        ship.AI.CancelIntercept();
                    }
                    return;
                }
                Owner.GetEmpireAI().UsedFleets.Remove(which);
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
                                fleetDataNode.Ship = null;
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
                Log.Warning($"{ship.fleet?.Name ?? "No Fleet"} : not equal {Name}");
            if (ship.AI.State != AIState.AwaitingOrders && ship.Active)
                Empire.Universe.DebugWin?.DebugLogText($"Fleet RemoveShip: Ship not awaiting orders and removed from fleet State: {ship.AI.State}", DebugModes.Normal);
            ship.fleet = null;
            RemoveFromAllSquads(ship);
            if (Ships.Remove(ship) || !ship.Active) return true;
            Empire.Universe.DebugWin?.DebugLogText("Fleet RemoveShip: Ship is not in this fleet", DebugModes.Normal);
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
                if (ship.AI.State == AIState.FormationWarp)
                {
                    SetCombatMoveAtPositon(ship, Position, 7500);
                   
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
