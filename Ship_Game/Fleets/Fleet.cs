using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI.Tasks;
using Ship_Game.Debug;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using Ship_Game.Fleets.FleetTactics;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.AI.CombatTactics;

namespace Ship_Game.Fleets
{
    public sealed class Fleet : ShipGroup
    {
        public readonly Array<FleetDataNode> DataNodes = new Array<FleetDataNode>();
        public Guid Guid = Guid.NewGuid();
        public string Name = "";

        readonly Array<Ship> CenterShips = new Array<Ship>();
        readonly Array<Ship> LeftShips = new Array<Ship>();
        readonly Array<Ship> RightShips = new Array<Ship>();
        readonly Array<Ship> RearShips = new Array<Ship>();
        readonly Array<Ship> ScreenShips = new Array<Ship>();
        public Array<Squad> CenterFlank = new Array<Squad>();
        public Array<Squad> LeftFlank = new Array<Squad>();
        public Array<Squad> RightFlank = new Array<Squad>();
        public Array<Squad> ScreenFlank = new Array<Squad>();
        public Array<Squad> RearFlank = new Array<Squad>();
        public readonly Array<Array<Squad>> AllFlanks = new Array<Array<Squad>>();

        int DefenseTurns = 50;
        public MilitaryTask FleetTask;
        MilitaryTask CoreFleetSubTask;
        public FleetCombatStatus Fcs;
        public CombatStatus TaskCombatStatus = CombatStatus.InCombat;

        public int FleetIconIndex;
        public SubTexture Icon => ResourceManager.FleetIcon(FleetIconIndex);

        public int TaskStep;
        public bool IsCoreFleet;

        Array<Ship> AllButRearShips => Ships.Except(RearShips).ToArrayList();
        public bool HasRepair { get; private set; }  //fbedard: ships in fleet with repair capability will not return for repair.
        public bool HasOrdnanceSupplyShuttles { get; private set; } // FB: fleets with supply bays will be able to resupply ships
        public bool ReadyForWarp { get; private set; }

        public bool InFormationWarp { get; private set; }

        public override string ToString()
            => $"{Owner.Name} {Name} ships={Ships.Count} pos={FinalPosition} guid={Guid} id={FleetTask?.WhichFleet ?? -1}";

        public void ClearFleetTask() => FleetTask = null;
        public Fleet()
        {
            FleetIconIndex = RandomMath.IntBetween(1, 10);
            SetCommandShip(null);
        }

        public void SetNameByFleetIndex(int index)
        {
            string suffix = "th";
            switch (index % 10)
            {
                case 1: suffix = "st"; break;
                case 2: suffix = "nd"; break;
                case 3: suffix = "rd"; break;
            }
            Name = index + suffix + " fleet";
        }

        public override void AddShip(Ship newShip)
        {
            if (newShip == null) // Added ship should never be null
            {
                Log.Error($"Ship Was Null for {Name}");
                return;
            }
            if (newShip.loyalty != Owner)
                Log.Warning("ship loyalty incorrect");
            // This is finding a logic bug: Ship is already in a fleet or this fleet already contains the ship.
            // This should likely be two different checks. There is also the possibilty that the ship is in another
            // Fleet ship list.
            if (newShip.fleet != null || Ships.ContainsRef(newShip))
            {
                Log.Warning($"{newShip}: \n already in fleet:\n{newShip.fleet}\nthis fleet:\n{this}");
                return; // recover
            }

            if (newShip.IsPlatformOrStation)
                return;

            UpdateOurFleetShip(newShip);
            SortIntoFlanks(newShip, CommandShip?.DesignRole ?? ShipData.RoleName.capital);
            AssignPositionTo(newShip);
            AddShipToNodes(newShip);
        }

        void UpdateOurFleetShip(Ship ship)
        {
            HasRepair = HasRepair || ship.hasRepairBeam || ship.HasRepairModule && ship.Ordinance > 0;

            HasOrdnanceSupplyShuttles = HasOrdnanceSupplyShuttles ||
                                        ship.Carrier.HasSupplyBays && ship.Ordinance >= 100;
        }

        public void AddExistingShip(Ship ship, FleetDataNode node)
        {
            node.Ship = ship;
            base.AddShip(ship);
            ship.fleet = this;
            ship.AI.FleetNode = node;
        }

        void AddShipToNodes(Ship shipToAdd)
        {
            base.AddShip(shipToAdd);
            shipToAdd.fleet = this;
            AddShipToDataNode(shipToAdd);
        }

        void ClearFlankList()
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
            AllFlanks.Clear();
        }

        void ResetFlankLists()
        {
            ClearFlankList();
            if (Ships.IsEmpty)
            {
                Log.Error($"Fleet ships was empty! Fleet: {Name}");
                return;
            }

            var mainShipList = new Array<Ship>(Ships);
            var largestShip = CommandShip ?? mainShipList.FindMax(ship => (int)ship.DesignRole);

            for (int i = mainShipList.Count - 1; i >= 0; i--)
            {
                Ship ship = mainShipList[i];
                SortIntoFlanks(ship, largestShip.DesignRole);
            }
        }

        void SortIntoFlanks(Ship ship, ShipData.RoleName largest)
        {
            int leftCount = LeftShips.Count;
            var roleType = ship.DesignRoleType;
            if (roleType != ShipData.RoleType.Warship || ship.DesignRole == ShipData.RoleName.carrier)
            {
                RearShips.AddUniqueRef(ship);
            }
            else if (CommandShip == ship || ship.DesignRole > ShipData.RoleName.fighter 
                                         && largest == ship.DesignRole )
            {
                CenterShips.AddUniqueRef(ship);
            }
            else if (CenterShips.Count - 2 <= ScreenShips.Count)
            {
                CenterShips.AddUniqueRef(ship);
            }
            else if (ScreenShips.Count <= leftCount)
            {
                ScreenShips.AddUniqueRef(ship);
            }
            else if (leftCount <= RightShips.Count)
            {
                LeftShips.AddUniqueRef(ship);
            }
            else
            {
                RightShips.AddUniqueRef(ship);
            }
        }

        enum FlankType
        {
            Left,
            Right
        }

        void FlankToCenterOffset(Array<Squad> flank, FlankType flankType)
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

        void LeftFlankToCenterOffset() => FlankToCenterOffset(LeftFlank, FlankType.Left);
        void RightFlankToCenterOffset() => FlankToCenterOffset(RightFlank, FlankType.Right);

        public void AutoArrange()
        {
            ResetFlankLists(); // set up center, left, right, screen, rear...
            SetSpeed();

            CenterFlank = SortSquadBySize(CenterShips);
            LeftFlank = SortSquadBySpeed(LeftShips);
            RightFlank = SortSquadBySpeed(RightShips);
            ScreenFlank = SortSquadBySpeed(ScreenShips);
            RearFlank = SortSquadBySpeed(RearShips);
            AllFlanks.Add(CenterFlank);
            AllFlanks.Add(LeftFlank);
            AllFlanks.Add(RightFlank);
            AllFlanks.Add(ScreenFlank);
            AllFlanks.Add(RearFlank);
            FinalPosition = AveragePosition();

            ArrangeSquad(CenterFlank, Vector2.Zero);
            ArrangeSquad(ScreenFlank, new Vector2(0.0f, -2500f));
            ArrangeSquad(RearFlank, new Vector2(0.0f, 2500f));

            LeftFlankToCenterOffset();
            RightFlankToCenterOffset();

            for (int x = 0; x < Ships.Count; x++)
            {
                Ship s = Ships[x];
                AddShipToDataNode(s);
            }
            AutoAssembleFleet(0.0f);

            for (int i = 0; i < Ships.Count; i++)
            {
                Ship s = Ships[i];
                if (s.InCombat)
                    continue;

                s.AI.OrderAllStop();
                s.AI.OrderThrustTowardsPosition(FinalPosition + s.FleetOffset, FinalDirection, false);
            }
        }

        void AddShipToDataNode(Ship ship)
        {
            FleetDataNode node = DataNodes.Find(n => n.Ship == ship);

            if (node == null)
            {
                node = new FleetDataNode
                {
                    FleetOffset = ship.RelativeFleetOffset,
                    OrdersOffset = ship.RelativeFleetOffset,
                    CombatState = ship.AI.CombatState
                };
                DataNodes.Add(node);
            }

            node.Ship = ship;
            node.ShipName = ship.Name;
            node.OrdersRadius = node.OrdersRadius < 2 ? ship.AI.GetSensorRadius() : node.OrdersRadius;
            ship.AI.FleetNode = node;
            ship.AI.CombatState = node.CombatState;
        }

        enum SquadSortType
        {
            Size,
            Speed
        }

        Array<Squad> SortSquadBySpeed(Array<Ship> allShips) => SortSquad(allShips, SquadSortType.Speed);
        Array<Squad> SortSquadBySize(Array<Ship> allShips) => SortSquad(allShips, SquadSortType.Size);

        Array<Squad> SortSquad(Array<Ship> allShips, SquadSortType sort)
        {
            var destSquad = new Array<Squad>();
            if (allShips.IsEmpty)
                return destSquad;

            int SortValue(Ship ship)
            {
                switch (sort)
                {
                    case SquadSortType.Size: return ship.SurfaceArea;
                    case SquadSortType.Speed: return (int)ship.MaxSTLSpeed;
                    default: return 0;
                }
            }

            allShips.Sort((a, b) =>
            {
                int aValue = SortValue(a);
                int bValue = SortValue(b);

                int order = bValue - aValue;
                if (order != 0) return order;
                return b.guid.CompareTo(a.guid);
            });

            var squad = new Squad { Fleet = this };

            for (int x = 0; x < allShips.Count; ++x)
            {
                if (squad.Ships.Count < 4)
                    squad.Ships.Add(allShips[x]);

                if (squad.Ships.Count != 4 && x != allShips.Count - 1)
                    continue;

                destSquad.Add(squad);
                squad = new Squad { Fleet = this };
            }
            return destSquad;
        }

        static void ArrangeSquad(Array<Squad> squad, Vector2 squadOffset)
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

        void AutoAssembleFleet(float facing)
        {
            for (int flank = 0; flank < AllFlanks.Count; flank++)
            {
                Array<Squad> squads = AllFlanks[flank];
                foreach (Squad squad in squads)
                {
                    for (int index = 0; index < squad.Ships.Count; ++index)
                    {
                        Ship ship = squad.Ships[index];
                        float radiansAngle;
                        switch (index)
                        {
                            default:
                            case 0: radiansAngle = RadMath.RadiansUp; break;
                            case 1: radiansAngle = RadMath.RadiansLeft; break;
                            case 2: radiansAngle = RadMath.RadiansRight; break;
                            case 3: radiansAngle = RadMath.RadiansDown; break;
                        }

                        Vector2 offset = (facing + squad.Offset.ToRadians()).RadiansToDirection() * squad.Offset.Length();
                        ship.FleetOffset = offset + (facing + radiansAngle).RadiansToDirection() * 500f;
                        ship.RelativeFleetOffset = squad.Offset + radiansAngle.RadiansToDirection() * 500f;
                    }
                }
            }
        }

        public void AssembleFleet2(Vector2 finalPosition, Vector2 finalDirection)
            => AssembleFleet(finalPosition, finalDirection, IsCoreFleet);

        public void Reset()
        {
            while (Ships.Count > 0)
            {
                var ship = Ships.PopLast();
                RemoveShip(ship);
            }
            TaskStep = 0;
            FleetTask = null;
            ClearFleetGoals();
        }

        void EvaluateTask(FixedSimTime timeStep)
        {
            if (Ships.Count == 0)
                FleetTask.EndTask();
            if (FleetTask == null)
                return;
            if (Empire.Universe.SelectedFleet == this)
                Empire.Universe.DebugWin?.DrawCircle(DebugModes.AO, FinalPosition, FleetTask.AORadius, Color.AntiqueWhite);

            TaskCombatStatus = FleetInAreaInCombat(FleetTask.AO, FleetTask.AORadius);

            switch (FleetTask.type)
            {
                case MilitaryTask.TaskType.ClearAreaOfEnemies:         DoClearAreaOfEnemies(FleetTask);         break;
                case MilitaryTask.TaskType.AssaultPlanet:              DoAssaultPlanet(FleetTask);              break;
                case MilitaryTask.TaskType.CohesiveClearAreaOfEnemies: DoCohesiveClearAreaOfEnemies(FleetTask); break;
                case MilitaryTask.TaskType.Exploration:                DoExplorePlanet(FleetTask);              break;
                case MilitaryTask.TaskType.DefendSystem:               DoDefendSystem(FleetTask);               break;
                case MilitaryTask.TaskType.DefendClaim:                DoClaimDefense(FleetTask);               break;
                case MilitaryTask.TaskType.DefendPostInvasion:         DoPostInvasionDefense(FleetTask);        break;
                case MilitaryTask.TaskType.GlassPlanet:                DoGlassPlanet(FleetTask);                break;
                case MilitaryTask.TaskType.AssaultPirateBase:          DoAssaultPirateBase(FleetTask);          break;
                case MilitaryTask.TaskType.RemnantEngagement:          DoRemnantEngagement(FleetTask);          break;
                case MilitaryTask.TaskType.DefendVsRemnants:           DoDefendVsRemnant(FleetTask);            break;
            }
        }

        void DoExplorePlanet(MilitaryTask task)
        {
            bool eventBuildingFound = task.TargetPlanet.EventsOnBuildings();

            if (EndInvalidTask(!StillInvasionEffective(task)) || !StillCombatEffective(task))                                      return;
            if (EndInvalidTask(!eventBuildingFound || task.TargetPlanet.Owner != null&& task.TargetPlanet.Owner != Owner)) return;

            switch (TaskStep)
            {
                case 0:
                    FleetTaskGatherAtRally(task);
                    TaskStep = 1;
                    break;
                case 1:
                    if (!HasArrivedAtRallySafely()) break;
                    GatherAtAO(task, distanceFromAO: task.TargetPlanet.GravityWellRadius);
                    TaskStep = 2;
                    break;
                case 2:
                    if (ArrivedAtCombatRally(FinalPosition))
                    {
                        TaskStep = 3;
                        var combatOffset = task.AO.OffsetTowards(AveragePosition(), task.TargetPlanet.GravityWellRadius);
                        EscortingToPlanet(combatOffset, false);
                    }
                    break;
                case 3:
                    var planetMoveStatus = FleetMoveStatus(task.TargetPlanet.GravityWellRadius, FinalPosition);
                    
                    if (!planetMoveStatus.HasFlag(MoveStatus.MajorityAssembled))
                    {
                        if (planetMoveStatus.HasFlag(MoveStatus.AssembledInCombat))
                        {
                            ClearPriorityOrderForShipsInAO(Ships, FinalPosition, task.TargetPlanet.GravityWellRadius);
                        }
                        break;
                    }
                    var planetGuard = task.AO.OffsetTowards(AveragePosition(), 500);
                    EngageCombatToPlanet(planetGuard, true);
                    TaskStep = 4;
                    break;

                case 4:
                    EndInvalidTask(StatusOfPlanetAssault(task) == Status.Critical);
                    break;
            }
        }

        bool ClearPriorityOrderForShipsInAO(Array<Ship> ships, Vector2 ao, float radius)
        {
            bool clearedOrder = false;
            for (int i = 0; i < ships.Count; i++)
            {
                Ship ship = ships[i];
                if (ship.IsSpoolingOrInWarp) continue;

                if (ship.AI.HasPriorityOrder && ship.AI.State != AIState.AssaultPlanet 
                                             && ship.AI.State != AIState.Bombard
                                             && ship.AI.Target != null
                                             && ship.InRadius(ao, radius) 
                                             && ship.AI.Target.InRadius(ao, radius))
                {
                    ship.AI.ClearPriorityOrderAndTarget();
                    clearedOrder = true;
                }
            }

            return clearedOrder;
        }

        void DoPostInvasionDefense(MilitaryTask task)
        {
            if (EndInvalidTask(--DefenseTurns <= 0))
                return;

            switch (TaskStep)
            {
                case 0:
                    SetPostInvasionFleetCombat();
                    DefenseTurns = 50;
                    TaskStep = 1;
                    break;
                case 1:
                    if (!DoOrbitTaskArea(task))
                    {
                        AttackEnemyStrengthClumpsInAO(task);
                    }
                    break;
            }
        }

        public static void CreatePostInvasionFromCurrentTask(Fleet fleet, MilitaryTask task, Empire owner)
        {
            fleet.TaskStep   = 0;
            var postInvasion = MilitaryTask.CreatePostInvasion(task.TargetPlanet, task.WhichFleet, owner);
            owner.GetEmpireAI().QueueForRemoval(task);
            fleet.FleetTask  = postInvasion;
            owner.GetEmpireAI().AddPendingTask(postInvasion);
        }

        bool TryOrderPostAssaultFleet(MilitaryTask task, int minimumTaskStep)
        {
            if (TaskStep < minimumTaskStep ||
                (task.TargetPlanet.Owner != Owner &&
                 !task.TargetPlanet.AnyOfOurTroops(Owner))) return false;
            CreatePostInvasionFromCurrentTask(this, task, Owner);

           for (int x = 0; x < Ships.Count; ++x)
           {
               var ship = Ships[x];
               if (ship.Carrier.AnyAssaultOpsAvailable)
                   RemoveShip(ship);
           }
           return true;
        }

        bool TryOrderPostBombFleet(MilitaryTask task, int minimumTaskStep)
        {
            if (TaskStep < minimumTaskStep || task.TargetPlanet.Owner != null ) return false;
            CreatePostInvasionFromCurrentTask(this, task, Owner);

           for (int x = 0; x < Ships.Count; ++x)
           {
               var ship = Ships[x];
               if (ship.Carrier.AnyAssaultOpsAvailable)
                   RemoveShip(ship);
           }
           return true;
        }


        void DoAssaultPlanet(MilitaryTask task)
        {
            if (!Owner.IsEmpireAttackable(task.TargetPlanet.Owner))
            {
                if (!TryOrderPostAssaultFleet(task, 2))
                {
                    Log.Info($"Invasion ({Owner.Name}) planet ({task.TargetPlanet}) Not attackable");
                    task.EndTask();
                    
                }
                return;
            }

            if (EndInvalidTask(!StillInvasionEffective(task) || !StillCombatEffective(task))) return;

            switch (TaskStep)
            {
                case 0:
                    FleetTaskGatherAtRally(task);
                    TaskStep = 1;
                    break;
                case 1:
                    if (!HasArrivedAtRallySafely(GetRelativeSize().Length()))
                        break;
                    GatherAtAO(task, distanceFromAO: Owner.GetProjectorRadius() * 1.5f);
                    TaskStep = 2;
                    SetOrdersRadius(Ships, 2000f);
                    break;
                case 2:
                    MoveStatus combatRally = FleetMoveStatus(0, FinalPosition);
                    if (!combatRally.HasFlag(MoveStatus.MajorityAssembled))
                    {
                        if (combatRally.HasFlag(MoveStatus.AssembledInCombat))
                        {
                            ClearPriorityOrderForShipsInAO(Ships, FinalPosition, GetRelativeSize().Length() / 2);
                        }
                        break;
                    }
                    TaskStep = 3;
                    break;
                case 3:
                    Vector2 combatOffset = task.AO.OffsetTowards(AveragePosition(), task.TargetPlanet.GravityWellRadius);
                    EscortingToPlanet(combatOffset, false);
                    TaskStep = 4; ;
                    break;
                case 4:
                    combatOffset   = task.AO.OffsetTowards(AveragePosition(), task.TargetPlanet.GravityWellRadius);
                    MoveStatus inPosition = FleetMoveStatus(task.TargetPlanet.GravityWellRadius, combatOffset);
                    if (!inPosition.HasFlag(MoveStatus.MajorityAssembled))
                    {
                        if(inPosition.HasFlag(MoveStatus.AssembledInCombat))
                        {
                            ClearPriorityOrderForShipsInAO(Ships, combatOffset, GetRelativeSize().Length());
                        }
                        break;
                    }
                    RearShipsToCombat(combatOffset, false);
                    Vector2 resetPosition = task.AO.OffsetTowards(AveragePosition(), 1500);
                    EngageCombatToPlanet(resetPosition, true);
                    TaskStep = 5;
                    break;

                case 5:
                    switch (StatusOfPlanetAssault(task))
                    {
                        case Status.NotApplicable: TaskStep = 4; break;
                        case Status.Good:          TaskStep = 6; break;
                        case Status.Critical:
                            {
                                EndInvalidTask(true);
                                break;
                            }
                    }
                    break;
                case 6:
                    if (ShipsOffMission(task))
                    {
                        Vector2 returnToCombat = task.AO.OffsetTowards(AveragePosition(), 500);
                        EngageCombatToPlanet(returnToCombat, true);
                    }
                    TaskStep = 5;
                    break;
            }
        }

        void DoDefendSystem(MilitaryTask task)
        {
            // this is currently unused. the system needs to be created with a defensive fleet.
            // no defensive fleets are created during the game. yet...
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
                    if (ShipsOffMission(task))
                        TaskStep = 3;
                    break;
            }
        }

        void DoClaimDefense(MilitaryTask task)
        {
            if (EndInvalidTask(task.TargetPlanet.Owner != null && !task.TargetPlanet.Owner.isFaction
                               || !CanTakeThisFight(task.EnemyStrength)))
            {
                return;
            }

            task.AO = task.TargetPlanet.Center;
            switch (TaskStep)
            {
                case 0:
                    FleetTaskGatherAtRally(task);
                    TaskStep = 1;
                    break;
                case 1:
                    if (!HasArrivedAtRallySafely())
                        break;

                    AddFleetProjectorGoal();
                    TaskStep = 2;
                    break;
                case 2:
                    if (FleetProjectorGoalInProgress())
                        break;

                    GatherAtAO(task, FleetTask.TargetPlanet.ParentSystem.Radius);
                    TaskStep = 3;
                    break;
                case 3:
                    if (!ArrivedAtCombatRally(FinalPosition, GetRelativeSize().Length() / 2))
                        break;

                    TaskStep = 4;
                    CancelFleetMoveInArea(task.AO, task.AORadius * 2);
                    break;
                case 4:
                    CombatMoveToAO(task, FleetTask.TargetPlanet.GravityWellRadius * 1.5f);
                    TaskStep = 5;
                    break;
                case 5:
                    if (!ArrivedAtCombatRally(FinalPosition, GetRelativeSize().Length() / 2))
                        break;

                    TaskStep = 6;
                    CancelFleetMoveInArea(task.AO, task.AORadius * 2);
                    break;
                case 6:
                     AttackEnemyStrengthClumpsInAO(task);
                    TaskStep = 7;
                    break;
                case 7:
                    if (!DoOrbitTaskArea(task, excludeInvade: true))
                        AttackEnemyStrengthClumpsInAO(task);

                    OrderShipsToInvade(Ships, task, false);
                    break;
            }
        }

        void DoRemnantEngagement(MilitaryTask task)
        {
            Planet target = FleetTask.TargetPlanet;
            switch (TaskStep)
            {
                case 1:
                    GatherAtAO(task, target.ParentSystem.Radius);
                    if (TryCalcEtaToPlanet(task, target.Owner, out float eta))
                        Owner.Remnants.InitTargetEmpireDefenseActions(target, eta, GetStrength());

                    TaskStep = 2;
                    break;
                case 2:
                    if (!ArrivedAtCombatRally(FinalPosition, GetRelativeSize().Length() * 2))
                        break;

                    TaskStep = 3;
                    CancelFleetMoveInArea(task.AO, task.AORadius * 2);
                    break;
                case 3:
                    FleetMoveToPosition(task.AO, target.GravityWellRadius * 1.5f, false);
                    TaskStep = 4;
                    break;
                case 4:
                    if (!ArrivedAtCombatRally(FinalPosition, GetRelativeSize().Length() * 2))
                    {
                        BombPlanet(FleetTask);
                        break;
                    }

                    TaskStep = 5;
                    CancelFleetMoveInArea(task.AO, task.AORadius * 2);
                    break;
                case 5:
                    if (FleetInAreaInCombat(task.AO, task.AORadius) == CombatStatus.InCombat)
                    {
                        BombPlanet(FleetTask);
                        AttackEnemyStrengthClumpsInAO(task);
                        if (target.Owner == null)
                            TaskStep = 7;
                        break;
                    }

                    OrderFleetOrbit(target);
                    TaskStep = 6;
                    break;
                case 6:
                    if (BombPlanet(FleetTask))
                        break;

                    TaskStep = 7;
                    break;
                case 7:
                    OrderFleetOrbit(target);
                    break; // Change in task step is done from Remnant goals
                case 8: // Go back to portal, this step is set from the Remnant goal
                    GatherAtAO(task, 500);
                    TaskStep = 9;
                    break;
                case 9:
                    if (!ArrivedAtCombatRally(FinalPosition, 50000))
                        break;

                    TaskStep = 10;
                    break;
            }
        }

        bool TryCalcEtaToPlanet(MilitaryTask task, Empire targetEmpire, out float starDateEta)
        {
            starDateEta = 0;
            if (task.TargetPlanet == null)
                return false;

            if (AveragePosition().InRadius(task.TargetPlanet.ParentSystem.Position, task.TargetPlanet.ParentSystem.Radius))
            {
                if (targetEmpire?.isPlayer == true)
                    return false; // The Fleet is already there

                starDateEta = Empire.Universe.StarDate;
                return true; // AI might retaliate even if its the same system
            }

            float distanceToPlanet = AveragePosition().Distance(task.TargetPlanet.Center);
            float slowestWarpSpeed = Ships.Min(s => s.MaxFTLSpeed).LowerBound(1000);
            float secondsToTarget  = distanceToPlanet / slowestWarpSpeed;
            float turnsToTarget    = secondsToTarget / GlobalStats.TurnTimer;
            starDateEta            = (Empire.Universe.StarDate + turnsToTarget / 10).RoundToFractionOf10();

            return starDateEta.Greater(0);
        }

        void DoDefendVsRemnant(MilitaryTask task)
        {
            if (EndInvalidTask(task.TargetPlanet.Owner != Owner ||
                               !EmpireManager.Remnants.Remnants.Goals
                                   .Any(g => g.Fleet?.FleetTask?.TargetPlanet == task.TargetPlanet)))
            {
                ClearOrders();
                return;
            }

            switch (TaskStep)
            {
                case 0:
                    FleetTaskGatherAtRally(task);
                    TaskStep = 1;
                    break;
                case 1:
                    if (!HasArrivedAtRallySafely(task.RallyPlanet.ParentSystem.Radius))
                        break;

                    GatherAtAO(task, 3000);
                    TaskStep = 2;
                    break;
                case 2:
                    if (!ArrivedAtCombatRally(task.AO, GetRelativeSize().Length() / 2))
                        break;

                    TaskStep = 3;
                    CancelFleetMoveInArea(task.AO, task.AORadius * 2);
                    break;
                case 3:
                    if (!CanTakeThisFight(task.EnemyStrength))
                        FleetTask?.EndTask();

                    break;
            }
        }

        void DoAssaultPirateBase(MilitaryTask task)
        {
            if (EndInvalidTask(task.TargetShip == null || !task.TargetShip.Active))
            {
                ClearOrders();
                return;
            }

            task.AO = task.TargetShip.Center;
            switch (TaskStep)
            {
                case 0:
                    FleetTaskGatherAtRally(task);
                    TaskStep = 1;
                    break;
                case 1:
                    if (!HasArrivedAtRallySafely(task.RallyPlanet.ParentSystem.Radius))
                        break;

                    GatherAtAO(task, 3000);
                    TaskStep = 2;
                    break;
                case 2:
                    if (!ArrivedAtCombatRally(task.AO, GetRelativeSize().Length() / 2))
                        break;
                    TaskStep = 3;
                    CancelFleetMoveInArea(task.AO, task.AORadius * 2);
                    break;
                case 3:
                    if (!AttackEnemyStrengthClumpsInAO(task))
                        TaskStep = 4;
                    else if (!CanTakeThisFight(task.EnemyStrength))
                        FleetTask?.EndTask();
                    break;
                case 4:
                    ClearOrders();
                    FleetTask?.EndTask();
                    break;
            }
        }

        void DoCohesiveClearAreaOfEnemies(MilitaryTask task)
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
                    TaskStep = 2;
                    break;
                case 2:
                    if (!ArrivedAtCombatRally(CoreFleetSubTask.AO)) break;
                    TaskStep = 3;
                    break;
                case 3:
                    if (!AttackEnemyStrengthClumpsInAO(CoreFleetSubTask))
                    {
                        TaskStep = 1;
                        CoreFleetSubTask = null;
                        break;
                    }
                    CancelFleetMoveInArea(task.AO, task.AORadius);
                    TaskStep = 4;
                    break;
                case 4:
                    if (ShipsOffMission(CoreFleetSubTask))
                        TaskStep = 3;
                    break;
            }
        }

        void DoGlassPlanet(MilitaryTask task)
        {
            bool endTask  = task.TargetPlanet.Owner == Owner || task.TargetPlanet.Owner?.IsAtWarWith(Owner) == false;
            endTask      |= task.TargetPlanet.Owner == null && task.TargetPlanet.GetGroundStrengthOther(Owner) < 1;
            endTask      |= !Ships.Select(s => s.Bomb60SecStatus()).Any(bt=> bt != Status.NotApplicable && bt != Status.Critical);
            if (endTask)
            {
                EndInvalidTask(!TryOrderPostBombFleet(task, 3));
                return;
            }
            
            task.AO = task.TargetPlanet.Center;
            switch (TaskStep)
            {
                case 0:
                    FleetTaskGatherAtRally(task);
                    TaskStep = 1;
                    break;
                case 1:
                    MoveStatus moveStatus = FleetMoveStatus(task.RallyPlanet.ParentSystem.Radius);
                    if (moveStatus.HasFlag(MoveStatus.MajorityAssembled))
                    {
                        GatherAtAO(task, 400000);
                        TaskStep = 2;
                        break;
                    }
                    else if (moveStatus.HasFlag(MoveStatus.AssembledInCombat))
                    {
                        task.Step = 5;
                    }
                    break;
                case 2:
                    if (!ArrivedAtCombatRally(FinalPosition))
                        break;
                    TaskStep = 3;
                    break;
                case 3:
                    EngageCombatToPlanet(task.TargetPlanet.Center, true);
                    StartBombing(task.TargetPlanet);
                    TaskStep = 4;
                    break;
                case 4:
                    if (ShipsOffMission(task))
                        TaskStep = 3;
                    break;
                case 5:
                    var currentSystem = task.RallyPlanet.ParentSystem;
                    if (currentSystem.OwnerList.Any(e=> Owner.IsEmpireHostile(e))) 
                    {
                        var newTarget = currentSystem.PlanetList.Find(p => Owner.IsEmpireHostile(p.Owner));
                        EngageCombatToPlanet(newTarget.Center, true);
                        StartBombing(newTarget);
                        FinalPosition = task.RallyPlanet.Center;
                    }
                    task.Step = 1;
                    break;
            }
        }

        void DoClearAreaOfEnemies(MilitaryTask task)
        {
            float enemyStrength = Owner.GetEmpireAI().ThreatMatrix.PingHostileStr(task.AO, task.AORadius, Owner);
            if (EndInvalidTask((enemyStrength < 1 && TaskStep > 5)|| !CanTakeThisFight(enemyStrength))) return;

            switch (TaskStep)
            {
                case 0:
                    GatherAtAO(task, distanceFromAO: Owner.GetProjectorRadius());
                    TaskStep++;
                    break;
                case 1:
                    if (!ArrivedAtCombatRally(FinalPosition))
                        break;
                    TaskStep++;
                    break;
                case 2:
                    if (AttackEnemyStrengthClumpsInAO(task, Ships))
                    {
                        TaskStep++;
                    }
                    else if (task.TargetPlanet != null)
                    {
                        if (DoOrbitTaskArea(task))
                            TaskStep++;
                    }
                    else
                    {
                        DoCombatMoveToTaskArea(task, true);
                        TaskStep++;
                    }
                    break;
                default:
                    if (TaskStep++ > 6) TaskStep = 2;
                    break;
            }
        }

        bool EndInvalidTask(bool condition)
        {
            if (!condition) return false;
            FleetTask.EndTask();
            FleetTask = null;
            TaskStep = 0;
            return true;
        }

        void OrderFleetOrbit(Planet planet)
        {
            for (int i = 0; i < Ships.Count; i++)
            {
                Ship ship = Ships[i];
                ship.OrderToOrbit(planet);
            }
        }

        void AddFleetProjectorGoal()
        {
            if (FleetTask?.TargetPlanet == null)
                return;

            Goal goal = new DeployFleetProjector(this, FleetTask.TargetPlanet, Owner);
            Owner.GetEmpireAI().AddGoal(goal);
        }

        bool FleetProjectorGoalInProgress()
        {
            var goals = Owner.GetEmpireAI().SearchForGoals(GoalType.DeployFleetProjector).Filter(g => g.Fleet == this);
            if (goals.Length == 1)
            {
                Goal deployGoal = goals[0];
                if (deployGoal.FinishedShip == null)
                    return true;
            }

            return false;

        }

        /// @return true if order successful. Fails when enemies near.
        bool DoOrbitTaskArea(MilitaryTask task, bool excludeInvade = false)
        {
            TaskCombatStatus = FleetInAreaInCombat(task.AO, task.AORadius);

            if (TaskCombatStatus < CombatStatus.ClearSpace)
                return false;

            DoOrbitAreaRestricted(task.TargetPlanet, task.AO, task.AORadius, excludeInvade);
            return true;
        }

        bool DoCombatMoveToTaskArea(MilitaryTask task, bool excludeInvade = false)
        {
            TaskCombatStatus = FleetInAreaInCombat(task.AO, task.AORadius);

            if (TaskCombatStatus < CombatStatus.ClearSpace)
                return false;

            if (ArrivedAtCombatRally(task.AO))
                return true;
            CombatMoveToAO(task, 0);

            return false;
        }

        void CancelFleetMoveInArea(Vector2 pos, float radius)
        {
            foreach (Ship ship in Ships)
            {
                if (ship.CanTakeFleetOrders && !ship.Center.OutsideRadius(pos, radius) &&
                    ship.AI.State == AIState.FormationWarp)
                {
                    ship.AI.State = AIState.AwaitingOrders;
                    ship.AI.ClearPriorityOrderAndTarget();
                }
            }
        }

        void ClearOrders()
        {
            for (int i = 0; i < Ships.Count; i++)
            {
                Ship ship = Ships[i];
                ship.AI.CombatState = ship.shipData.CombatState;
                ship.AI.ClearOrders();
            }
        }

        void SetPriorityOrderToShipsIf(Array<Ship> ships, Func<Ship, bool> condition, bool clearOtherOrders = false)
        {
            for (int i = 0; i < ships.Count; ++i)
            {
                Ship ship = Ships[i];
                if (condition(ship))
                    ship.AI.SetPriorityOrder(clearOtherOrders);
            }
        }

        void SetAllShipsPriorityOrder() => SetPriorityOrderToShipsIf(Ships,s=> s.CanTakeFleetOrders);

        void FleetTaskGatherAtRally(MilitaryTask task)
        {
            Planet planet       = task.RallyPlanet;
            Vector2 movePoint   = planet.Center;
            Vector2 finalFacing = movePoint.DirectionToTarget(task.AO);

            MoveToNow(movePoint, finalFacing, false);
        }

        bool HasArrivedAtRallySafely(float fleetRadius = 0)
        {
            MoveStatus status = MoveStatus.None;

            status = FleetMoveStatus(fleetRadius);

            if (FleetTask?.TargetPlanet?.ParentSystem.Position.InRadius(FinalPosition, 500000) == true)
            {
                if (status.HasFlag(MoveStatus.MajorityAssembled))
                {
                    return true;
                }
            }

            if (!status.HasFlag(MoveStatus.Assembled))
                return false;
            
            if (EndInvalidTask(status.HasFlag(MoveStatus.AssembledInCombat)))
                return false;

            return !status.HasFlag(MoveStatus.Dispersed);
        }

        void GatherAtAO(MilitaryTask task, float distanceFromAO)
        {
            FleetMoveToPosition(task.AO, distanceFromAO, false);
        }

        void CombatMoveToAO(MilitaryTask task, float distanceFromAO) => FleetMoveToPosition(task.AO, distanceFromAO, true);

        void FleetMoveToPosition(Vector2 position, float offsetToAO, bool combatMove)
        {
            FinalPosition = position.OffsetTowards(AveragePosition(), offsetToAO);
            FormationWarpTo(FinalPosition
                , AveragePosition().DirectionToTarget(position)
                , queueOrder: false
                , offensiveMove: combatMove);
        }

        void HoldFleetPosition()
        {
            for (int index = 0; index < Ships.Count; index++)
            {
                Ship ship = Ships[index];
                ship.AI.State = AIState.HoldPosition;
                if (ship.shipData.Role == ShipData.RoleName.troop)
                    ship.AI.HoldPosition();
            }
        }

        bool ArrivedAtOffsetRally(MilitaryTask task)
        {
            if (FleetMoveStatus(5000f).HasFlag(MoveStatus.Dispersed))
                return false;

            HoldFleetPosition();
            return true;
        }

        bool ArrivedAtCombatRally(Vector2 position, float radius = 0)
        {
            radius = radius.AlmostZero() ? GetRelativeSize().Length() / 1.5f : radius;
            MoveStatus status = FleetMoveStatus(radius, position);

            if (status.HasFlag(MoveStatus.MajorityAssembled)) return true;
            if (status.HasFlag(MoveStatus.AssembledInCombat))
            {
                ClearPriorityOrderForShipsInAO(Ships, position, radius);
            }
            if (status.HasFlag(MoveStatus.Assembled))
            {
                float fleetRadius = GetRelativeSize().Length() / 2;
                float size = 1;
                if (radius < fleetRadius)
                {
                    size = radius / fleetRadius;
                }
                for (int i = 0; i < Ships.Count; i++)
                {
                    var ship = Ships[i];
                    if (ship.IsSpoolingOrInWarp || ship.InCombat || ship.AI.State != AIState.AwaitingOrders) 
                        continue;
                    if (ship.InRadius(position, radius)) continue;
                    Vector2 movePos = position + ship.AI.FleetNode.FleetOffset / size;
                    ship.AI.OrderMoveTo(movePos, position.DirectionToTarget(FleetTask.AO)
                        , true, AIState.AwaitingOrders, null, true);
                }
            }
            return false;
        }

        Ship[] AvailableShips => AllButRearShips.Filter(ship => !ship.AI.HasPriorityOrder);

        bool AttackEnemyStrengthClumpsInAO(MilitaryTask task) => AttackEnemyStrengthClumpsInAO(task, AvailableShips);

        bool AttackEnemyStrengthClumpsInAO(MilitaryTask task, IEnumerable<Ship> ships)
        {
            Map<Vector2, float> enemyClumpsDict = Owner.GetEmpireAI().ThreatMatrix
                .PingRadarStrengthClusters(task.AO, task.AORadius, 2500, Owner);

            if (enemyClumpsDict.Count == 0)
                return false;

            var availableShips = new Array<Ship>(ships);
            while(availableShips.Count > 0)
            {
                foreach (var kv in enemyClumpsDict.OrderBy(dis => dis.Key.SqDist(task.AO)))
                {
                    if (availableShips.Count == 0)
                        break;

                    float attackStr = 0.0f;
                    for (int i = availableShips.Count - 1; i >= 0; --i)
                    {
                        //if (attackStr > kv.Value * 3)
                        //    break;

                        Ship ship = availableShips[i];
                        if (ship.AI.HasPriorityOrder 
                            || ship.InCombat 
                            || ship.AI.State == AIState.AssaultPlanet 
                            || ship.AI.State == AIState.Bombard)
                        {
                            availableShips.RemoveAtSwapLast(i);
                            continue;
                        }
                        Vector2 vFacing = ship.Center.DirectionToTarget(kv.Key);
                        ship.AI.OrderMoveTo(kv.Key, vFacing, true, AIState.MoveTo, offensiveMove: true);
                        ship.ForceCombatTimer();

                        availableShips.RemoveAtSwapLast(i);
                        attackStr += ship.GetStrength();
                    }
                }
            } 

            foreach (Ship needEscort in RearShips)
            {
                if (availableShips.IsEmpty) break;
                Ship ship = availableShips.PopLast();
                ship.DoEscort(needEscort);
            }

            foreach (Ship ship in availableShips)
                ship.AI.OrderMoveDirectlyTo(task.AO, FinalPosition.DirectionToTarget(task.AO), true, AIState.MoveTo);

            return true;
        }

        bool MoveFleetToNearestCluster(MilitaryTask task)
        {
            var strengthCluster = new ThreatMatrix.StrengthCluster
            {
                Empire      = Owner,
                Granularity = 5000f,
                Position    = task.AO,
                Radius      = task.AORadius
            };

            strengthCluster = Owner.GetEmpireAI().ThreatMatrix.FindLargestStrengthClusterLimited(strengthCluster, GetStrength(), AveragePosition());
            if (strengthCluster.Strength <= 0) return false;
            CoreFleetSubTask = new MilitaryTask
            {
                AO = strengthCluster.Position,
                AORadius = strengthCluster.Granularity
            };
            GatherAtAO(CoreFleetSubTask, 7500);
            return true;
        }

        bool ShipsOffMission(MilitaryTask task)
        {
            return AllButRearShips.Any(ship => ship.CanTakeFleetOrders &&
                                               !ship.AI.HasPriorityOrder &&
                                               !ship.InCombat &&
                                               ship.Center.OutsideRadius(task.AO, task.AORadius * 1.5f));
        }

        void SetOrdersRadius(Array<Ship> ships, float ordersRadius)
        {
            for (int i = 0; i < ships.Count; i++)
            {
                Ship ship = ships[i];
                ship.AI.FleetNode.OrdersRadius = ordersRadius;
            }
        }
        
        bool ReadyToInvade(MilitaryTask task)
        {
            float invasionSafeZone = (task.TargetPlanet.GravityWellRadius);
            return Ships.Any(ship => ship.Center.InRadius(task.TargetPlanet.Center, invasionSafeZone));
        }

        /// <summary>
        /// Status of planetary assault.
        /// <para>NotApplicable if waiting for invasion to start</para>
        /// Good if invasion inProgress.
        /// <para></para>
        /// Critical if invasion should fail
        /// </summary>
        Status StatusOfPlanetAssault(MilitaryTask task)
        {
            bool bombing              = BombPlanet(task);
            bool readyToInvade        = ReadyToInvade(task);

            if (readyToInvade)
            {
                bool invading = OrderShipsToInvade(RearShips, task, bombing);

                if (bombing || invading) 
                    return Status.Good;
                return Status.Critical;
            }
            return Status.NotApplicable;
        }

        void DebugInfo(MilitaryTask task, string text)
            => Empire.Universe?.DebugWin?.DebugLogText($"{task.type}: ({Owner.Name}) Planet: {task.TargetPlanet?.Name ?? "None"} {text}", DebugModes.Normal);

        // @return TRUE if we can take this fight, potentially, maybe...
        public bool CanTakeThisFight(float enemyFleetStrength)
        {
            float ourStrengthThreshold = GetStrength() * 2;
            return enemyFleetStrength < ourStrengthThreshold;
        }

        bool StillCombatEffective(MilitaryTask task)
        {
            float enemyStrength = Owner.GetEmpireAI().ThreatMatrix.PingHostileStr(task.AO, task.AORadius, Owner);
            if (CanTakeThisFight(enemyStrength))
                return true;

            DebugInfo(task, $"Enemy Strength too high. Them: {enemyStrength} Us: {GetStrength()}");
            return false;
        }

        bool StillInvasionEffective(MilitaryTask task)
        {
            bool troopsOnPlanet = task.TargetPlanet.AnyOfOurTroops(Owner);
            bool invasionTroops = Ships.Any(troops => troops.Carrier.AnyAssaultOpsAvailable) && GetStrength() > 0;
            bool stillMissionEffective = troopsOnPlanet || invasionTroops;
            if (!stillMissionEffective)
                DebugInfo(task, " No Troops on Planet and No Ships.");
            return stillMissionEffective;
        }

        void InvadeTactics(IEnumerable<Ship> flankShips, InvasionTactics type, Vector2 moveTo, bool combatMove)
        {
            foreach (Ship ship in flankShips)
            {
                ShipAI ai = ship.AI;
                ai.CombatState = ship.shipData.CombatState;
                if (!ship.CanTakeFleetOrders)
                    continue;

                ai.CancelIntercept();
                ai.ClearOrders();

                float fleetSizeRatio = GetRelativeSize().Length();
                if (fleetSizeRatio > FleetTask.AORadius)
                    fleetSizeRatio /= FleetTask.AORadius;
                else fleetSizeRatio = 1;

                switch (type)
                {
                    case InvasionTactics.Screen:
                        {
                            TacticalMove(ship, moveTo, fleetSizeRatio, combatMove, SpeedLimit);
                            break;
                        }

                    case InvasionTactics.Rear:
                        if (!ai.HasPriorityOrder)
                        {
                            TacticalMove(ship, moveTo, fleetSizeRatio, combatMove, SpeedLimit * 0.5f);
                        }
                        break;

                    case InvasionTactics.MainBattleGroup:
                        {
                            TacticalMove(ship, moveTo, fleetSizeRatio, combatMove, SpeedLimit * 0.75f);
                            break;
                        }
                    case InvasionTactics.FlankGuard:
                        {
                            TacticalMove(ship, moveTo, fleetSizeRatio, combatMove, SpeedLimit *.05f);
                            break;
                        }
                    case InvasionTactics.Wait:
                        ai.HoldPosition();
                        break;
                }
            }
        }

        void TacticalMove(Ship ship, Vector2 moveTo, float fleetSizeRatio, bool combatMove, float speedLimit)
        {
            var ai              = ship.AI;
            Vector2 offset      = ship.FleetOffset / fleetSizeRatio;
            Vector2 fleetMoveTo = moveTo + offset;
            FinalDirection      = fleetMoveTo.DirectionToTarget(FleetTask.AO);

            ai.OrderMoveDirectlyTo(fleetMoveTo, FinalDirection, true, ai.State, speedLimit, combatMove);
        }

        private enum InvasionTactics
        {
            /// <summary>
            /// Screen ships should engage combat targets attempting to screen the fleet from targets between the fleet and its objective. 
            /// </summary>
            Screen,
            /// <summary>
            /// The MBG should have the bigger damage dealing ships including carriers and captials. 
            /// </summary>
            MainBattleGroup,
            /// <summary>
            /// Flank Guard should protect sides of the main battle group and provide fire support. 
            /// </summary>
            FlankGuard,
            /// <summary>
            /// Rear ships should be reserve and protected ships. Troop Transports and other utility ships. 
            /// </summary>
            Rear,
            /// <summary>
            /// Wait should tell the ships to hold position for further orders. 
            /// </summary>
            Wait
        }

        void EscortingToPlanet(Vector2 position, bool combatMove)
        {
            FinalPosition = position;
            FinalDirection = FinalPosition.DirectionToTarget(FinalPosition);

            InvadeTactics(ScreenShips, InvasionTactics.Screen, FinalPosition, combatMove);
            InvadeTactics(CenterShips, InvasionTactics.MainBattleGroup, FinalPosition, combatMove);

            InvadeTactics(RearShips, InvasionTactics.Wait, FinalPosition, combatMove);
            
            InvadeTactics(RightShips, InvasionTactics.FlankGuard, FinalPosition, combatMove);
            InvadeTactics(LeftShips, InvasionTactics.FlankGuard, FinalPosition, combatMove);
        }

        void EngageCombatToPlanet(Vector2 position, bool combatMove)
        {
            FinalPosition = position;
            FinalDirection = FinalPosition.DirectionToTarget(FinalPosition);

            InvadeTactics(ScreenShips, InvasionTactics.Screen, FinalPosition, combatMove);
            InvadeTactics(CenterShips, InvasionTactics.MainBattleGroup, FinalPosition, combatMove);
            InvadeTactics(RightShips, InvasionTactics.FlankGuard, FinalPosition, combatMove);
            InvadeTactics(LeftShips, InvasionTactics.FlankGuard, FinalPosition, combatMove);
        }

        void RearShipsToCombat(Vector2 position, bool combatMove)
        {
            var notBombersOrTroops = new Array<Ship>();
            foreach(var ship in RearShips)
            {
                if (ship.DesignRoleType == ShipData.RoleType.Troop) continue;
                if (ship.DesignRole == ShipData.RoleName.bomber) continue;
                notBombersOrTroops.Add(ship);

            }

            InvadeTactics(notBombersOrTroops, InvasionTactics.Screen, FinalPosition, combatMove);
        }

        bool StartBombing(Planet planet)
        {
            if (planet.Owner == null)
                return false; // colony was destroyed

            bool anyShipsBombing = false;
            Ship[] ships = Ships.Filter(ship => ship.HasBombs 
                           && ship.Supply.ShipStatusWithPendingResupply(SupplyType.Rearm) >= Status.Critical);
            
            for (int x = 0; x < ships.Length; x++)
            {
                Ship ship = ships[x];
                if (ship.HasBombs && !ship.AI.HasPriorityOrder && ship.AI.State != AIState.Bombard)
                {
                    ship.AI.OrderBombardPlanet(planet);
                    ship.AI.SetPriorityOrder(true);
                }
                anyShipsBombing |= ship.AI.State == AIState.Bombard;
            }

            return anyShipsBombing;
        }

        /// <summary>
        /// @return TRUE if any ships are bombing planet
        /// Bombing is done if possible.
        /// </summary>
        bool BombPlanet(MilitaryTask task)
        {
            return StartBombing(task.TargetPlanet);
        }

        /// <summary>
        /// Sends any capable ships to invade task planet. Returns true if succesful. 
        /// <para></para>
        /// Invasion start success depends on the number of landing spots on the planet and the strength comparison
        /// between invasion forces and planet defenders. 
        /// </summary>
        bool OrderShipsToInvade(IEnumerable<Ship> ships, MilitaryTask task, bool targetBeingBombed)
        {
            float planetAssaultStrength = 0.0f;
            int shipsInvading           = 0;
            float theirGroundStrength   = GetGroundStrOfPlanet(task.TargetPlanet);
            float ourGroundStrength     = FleetTask.TargetPlanet.GetGroundStrength(Owner);
            var invasionShips           = ships.ToArray();

            // collect current invasion stats from all ships in fleet. 
            for (int i = 0; i < Ships.Count; i++)
            {
                Ship ship              = Ships[i];
                planetAssaultStrength += ship.Carrier.PlanetAssaultStrength; 

                if (ship.AI.State == AIState.AssaultPlanet) shipsInvading++;
            }

            planetAssaultStrength += ourGroundStrength;

            // we need at least 1 spot open. if we have bombers one should be there soon. 
            // else figure the base number by our strength ratio. if we have twice the strength then 
            // 2 landing spots might do the job. 
            float landingSpotRatio = theirGroundStrength / planetAssaultStrength.LowerBound(1);
            int landingSpotsNeeded = targetBeingBombed ? 0 : (int)(1 * landingSpotRatio).Clamped(1, 10);

            int freeLandingSpots = task.TargetPlanet.GetFreeTiles(Owner);

            // If we arent bombing and no troops are fighting or on their way
            // and we dont have enough strength to open a few tiles with brute force troops.
            // give up. 
            if (!targetBeingBombed && ourGroundStrength < 1 && shipsInvading < 1 && freeLandingSpots < landingSpotsNeeded)
                return false;

            int numberOfShipsToSend = freeLandingSpots - shipsInvading;

            for (int x = 0; x < invasionShips.Length && numberOfShipsToSend > 0; x++)
            {
                Ship ship = invasionShips[x];
                if (ship.DesignRoleType == ShipData.RoleType.Troop && ship.AI.State != AIState.AssaultPlanet)
                {
                    ship.AI.ClearOrders();
                    ship.AI.OrderLandAllTroops(task.TargetPlanet);
                    numberOfShipsToSend--;
                    shipsInvading++;
                }
            }

            return shipsInvading > 0;
        }

        float GetGroundStrOfPlanet(Planet p) => p.GetGroundStrengthOther(Owner);

        void SetPostInvasionFleetCombat()
        {
            foreach (FleetDataNode node in DataNodes)
            {
                node.OrdersRadius = FleetTask.AORadius;
                node.AssistWeight = 1;
                node.DPSWeight = -1;
            }
        }

        void PostInvasionStayInAO()
        {
            foreach (Ship ship in Ships)
            {
                if (ship.Center.SqDist(FleetTask.AO) > ship.AI.FleetNode.OrdersRadius)
                    ship.AI.OrderThrustTowardsPosition(FleetTask.AO + ship.FleetOffset, 60f.AngleToDirection(), true);
            }
        }

        bool PostInvasionAnyShipsOutOfAO(MilitaryTask task) =>
            Ships.Any(ship => task.AO.OutsideRadius(ship.Center, ship.AI.FleetNode.OrdersRadius));

        public void UpdateAI(FixedSimTime timeStep, int which)
        {
            if (FleetTask != null)
            {
                EvaluateTask(timeStep);
            }
            else // no fleet task
            {
                if (EmpireManager.Player == Owner || IsCoreFleet)
                {
                    if (!IsCoreFleet) return;
                    foreach (Ship ship in Ships)
                    {
                        ship.AI.CancelIntercept();
                    }
                    return;
                }
                Owner.GetEmpireAI().UsedFleets.Remove(which);

                Reset();
            }
        }

        void RemoveFromAllSquads(Ship ship)
        {
            for (int i = 0; i < DataNodes.Count; ++i)
            {
                FleetDataNode node = DataNodes[i];
                if (node.Ship == ship)
                    node.Ship = null;
            }

            foreach (Array<Squad> flank in AllFlanks)
            {
                foreach (Squad squad in flank)
                {
                    squad.Ships.RemoveRef(ship);
                    for (int nodeId = 0; nodeId < squad.DataNodes.Count; ++nodeId)
                    {
                        FleetDataNode node = squad.DataNodes[nodeId];
                        if (node.Ship == ship)
                        {
                            node.Ship = null;
                            // dont know which one it's in... so this this dumb.
                            // this will be fixed later when flank stuff is refactored.
                            ScreenShips.RemoveRef(ship);
                            CenterShips.RemoveRef(ship);
                            LeftShips.RemoveRef(ship);
                            RightShips.RemoveRef(ship);
                            RearShips.RemoveRef(ship);
                        }
                    }
                }
            }
        }

        public bool RemoveShip(Ship ship)
        {
            if (ship == null)
            {
                Log.Error($"Attempted to remove a null ship from Fleet {Name}");
                return false;
            }

            if (ship.Active && ship.fleet != this)
            {
                Log.Warning($"{ship.fleet?.Name ?? "No Fleet"} : not equal {Name}");
            }

            if (ship.AI.State != AIState.AwaitingOrders && ship.Active)
                Empire.Universe.DebugWin?.DebugLogText($"Fleet RemoveShip: Ship not awaiting orders and removed from fleet State: {ship.AI.State}", DebugModes.Normal);

            RemoveFromAllSquads(ship);

            // Todo - this if block is strange. It removes the ship before and then checks if its active.
            // If it is active , it adds the ship again. It does not seem right.
            if (ship.fleet == this && ship.Active)
            {
                ship.fleet = null;
                ship.AI.ClearOrders();
                ship.loyalty.AddShipNextFrame(ship);
                ship.HyperspaceReturn();
            }
            else 
            {
                if (ship.Active)
                    Log.Warning($"Ship was not part of this fleet: {this} ---- Ship: {ship} ");
            }
            return Ships.Remove(ship);
        }

        // @return The desired formation pos for this ship
        public Vector2 GetFormationPos(Ship ship) => AveragePosition() + ship.FleetOffset - AverageOffsetFromZero;

        // @return The Final destination position for this ship
        public Vector2 GetFinalPos(Ship ship) => FinalPosition + ship.FleetOffset;

        public float FormationWarpSpeed(Ship ship)
        {
            // this is the desired position inside the fleet formation
            Vector2 desiredFormationPos = GetFormationPos(ship);
            Vector2 desiredFinalPos = GetFinalPos(ship);

            float distToFinalPos = ship.Center.Distance(desiredFinalPos);
            float distFromFormation = ship.Center.Distance(desiredFormationPos);
            float distFromFormationToFinal = desiredFormationPos.Distance(desiredFinalPos);
            float shipSpeed = SpeedLimit;


            // Outside of fleet formation
            if (distToFinalPos > distFromFormationToFinal + ship.CurrentVelocity + 75f)
            {
                shipSpeed = ship.VelocityMaximum;
            }
            else
            // FINAL APPROACH
            if (distToFinalPos < ship.FleetOffset.Length()
                // NON FINAL: we are much further from the formation
                || distFromFormation > distToFinalPos)
            {
                shipSpeed = SpeedLimit * 2;
            }
            // formation is behind us? We are going way too fast
            else if (distFromFormationToFinal > distToFinalPos + 75f)
            {
                // SLOW DOWN MAN! but never slower than 50% of fleet speed
                shipSpeed = Math.Max(SpeedLimit - distFromFormation, SpeedLimit * 0.5f);
            }
            // CLOSER TO FORMATION: we are too far from desired position
            else if (distFromFormation > SpeedLimit)
            {
                // hurry up! set a really high speed
                // but at least fleet speed, not less in case we get really close
                shipSpeed = Math.Max(distFromFormation - SpeedLimit, SpeedLimit);
            }
            // getting close to our formation pos
            else if (distFromFormation < SpeedLimit * 0.5f)
            {
                // we are in formation, CRUISING SPEED
                shipSpeed = SpeedLimit;
            }
            return shipSpeed;
        }

        public bool FindShipNode(Ship ship, out FleetDataNode node)
        {
            node = null;
            foreach (FleetDataNode n in DataNodes)
            {
                if (n.Ship == ship)
                {
                    node = n;
                    break;
                }
            }

            return node != null;
        }

        public bool GoalGuidExists(Guid guid)
        {
            return DataNodes.Any(n => n.GoalGUID == guid);
        }

        public bool FindNodeWithGoalGuid(Guid guid, out FleetDataNode node)
        {
            node = null;
            foreach (FleetDataNode n in DataNodes)
            {
                if (n.GoalGUID == guid)
                {
                    node = n;
                    break;
                }
            }

            return node != null;
        }

        public void AssignGoalGuid(FleetDataNode node, Guid goalGuid)
        {
            node.GoalGUID = goalGuid;
        }

        public void RemoveGoalGuid(FleetDataNode node)
        {
            if (node != null)
                AssignGoalGuid(node, Guid.Empty);
        }

        public void RemoveGoalGuid(Guid guid)
        {
            if (FindNodeWithGoalGuid(guid, out FleetDataNode node))
                AssignGoalGuid(node, Guid.Empty);
        }

        public void AssignShipName(FleetDataNode node, string name)
        {
            node.ShipName = name;
        }

        public void Update(FixedSimTime timeStep)
        {
            InFormationWarp   = false;
            HasRepair         = false;
            bool readyForWarp = true;
            Ship commandShip  = null;

            if (Ships.Count == 0) return;
            if (CommandShip != null && !CommandShip.CanTakeFleetMoveOrders())
                SetCommandShip(null);

            for (int i = Ships.Count - 1; i >= 0; --i)
            {
                Ship ship = Ships[i];
                if (!ship.Active)
                {
                    RemoveShip(ship);
                    continue;
                }
                if (ship.fleet != this)
                {
                    RemoveShip(ship);
                    Log.Warning("Fleet Update. Ship in fleet was not assigned to this fleet");
                    continue;
                }

                if (CommandShip == null && ship.CanTakeFleetMoveOrders())
                {
                    if ((commandShip?.SurfaceArea ?? 0) < ship.SurfaceArea)
                        commandShip = ship;
                }

                Empire.Universe.DebugWin?.DrawCircle(DebugModes.PathFinder, FinalPosition, 7500, Color.Yellow);

                // if combat in move position do not move in formation. 
                if ( !IsAssembling && ship.AI.HasPriorityOrder && ship.engineState == Ship.MoveState.Sublight
                                   && ship.AI.State == AIState.FormationWarp)
                {
                    if (CombatStatusOfShipInArea(ship, FinalPosition, 7500) != CombatStatus.ClearSpace)
                    {
                        ClearPriorityOrderIfSubLight(ship);
                    }
                }

                if (!InFormationWarp)
                    InFormationWarp = ship.AI.State == AIState.FormationWarp;

                UpdateOurFleetShip(ship);


                if (readyForWarp)
                    readyForWarp = ship.ShipEngines.ReadyForFormationWarp > Status.Poor;

                // once in warp clear assembling flag. 
                if (ship.engineState == Ship.MoveState.Warp) IsAssembling = false;
            }

            if (Ships.Count > 0 && HasFleetGoal)
                GoalStack.Peek().Evaluate(timeStep);

            if (commandShip != null)
                SetCommandShip(commandShip);
            ReadyForWarp = readyForWarp;
        }

        public void OffensiveTactic()
        {
            var fleetTactics = new CombatPreferences();
            fleetTactics.SetTacticDefense(ScreenShips);
            fleetTactics.SetTacticIntercept(LeftShips);
            fleetTactics.SetTacticIntercept(RightShips);
            fleetTactics.SetTacticIntercept(RearShips);
            fleetTactics.SetTacticAttack(CenterShips, CommandShip?.SensorRange ?? 150000);
        }

        public void DefensiveTactic()
        {
            var fleetTactics = new CombatPreferences();
            fleetTactics.SetTacticDefense(ScreenShips);
            fleetTactics.SetTacticIntercept(LeftShips);
            fleetTactics.SetTacticIntercept(RightShips);
            fleetTactics.SetTacticDefense(RearShips);
            fleetTactics.SetTacticDefense(CenterShips, CommandShip?.SensorRange ?? 150000);
        }

        public enum FleetCombatStatus
        {
            Maintain,
            Loose,
            Free
        }

        public sealed class Squad
        {
            public FleetDataNode MasterDataNode = new FleetDataNode();
            public Array<FleetDataNode> DataNodes = new Array<FleetDataNode>();
            public Array<Ship> Ships = new Array<Ship>();
            public Fleet Fleet;
            public Vector2 Offset;
        }

        public enum FleetGoalType
        {
            AttackMoveTo,
            MoveTo
        }

        public static string GetDefaultFleetNames(int index)
        {
            switch (index)
            {
                case 1: return "First";
                case 2: return "Second";
                case 3: return "Third";
                case 4: return "Fourth";
                case 5: return "Fifth";
                case 6: return "Sixth";
                case 7: return "Seventh";
                case 8: return "Eight";
                case 9: return "Ninth";
            }
            return "";
        }
    }
}
