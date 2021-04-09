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

namespace Ship_Game.Fleets
{
    public sealed class Fleet : ShipGroup
    {
        public readonly Array<FleetDataNode> DataNodes = new Array<FleetDataNode>();
        public Guid Guid = Guid.NewGuid();
        public string Name = "";
        public ShipAI.TargetParameterTotals TotalFleetAttributes;
        public ShipAI.TargetParameterTotals AverageFleetAttributes;

        readonly Array<Ship> CenterShips  = new Array<Ship>();
        readonly Array<Ship> LeftShips    = new Array<Ship>();
        readonly Array<Ship> RightShips   = new Array<Ship>();
        readonly Array<Ship> RearShips    = new Array<Ship>();
        readonly Array<Ship> ScreenShips  = new Array<Ship>();
        public Array<Squad> CenterFlank   = new Array<Squad>();
        public Array<Squad> LeftFlank     = new Array<Squad>();
        public Array<Squad> RightFlank    = new Array<Squad>();
        public Array<Squad> ScreenFlank   = new Array<Squad>();
        public Array<Squad> RearFlank     = new Array<Squad>();
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
        public bool AutoRequisition { get; private set; }

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

        public Fleet(Array<Ship> ships, Empire empire)
        {
            Owner          = empire;
            FleetIconIndex = RandomMath.IntBetween(1, 10);
            SetCommandShip(null);
            AddShips(ships);
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

        public void AddShips(Array<Ship> ships)
        {
            for (int i = 0; i < ships.Count; i++)
                AddShip(ships[i]);
        }

        public override bool AddShip(Ship newShip)
        {
            if (newShip == null) // Added ship should never be null
            {
                Log.Error($"Ship Was Null for {Name}");
                return false;
            }
            if (newShip.loyalty != Owner)
                Log.Warning("ship loyalty incorrect");

            if (newShip.IsPlatformOrStation)
                return false;

            // This is finding a logic bug: Ship is already in a fleet or this fleet already contains the ship.
            // This should likely be two different checks. There is also the possibilty that the ship is in another
            // Fleet ship list.
            if (newShip.fleet != null || !base.AddShip(newShip))
            {
                Log.Warning($"{newShip}: \n already in fleet:\n{newShip.fleet}\nthis fleet:\n{this}");
                return false; // recover
            }

            UpdateOurFleetShip(newShip);

            SortIntoFlanks(newShip, TotalFleetAttributes.GetAveragedValues());
            AddShipToNodes(newShip);
            AssignPositionTo(newShip);
            return true;
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
            ship.FleetOffset = node.FleetOffset;
            ship.RelativeFleetOffset = node.FleetOffset;

            for (int i = 0; i < AllFlanks.Count; i++)
            {
                Array<Squad> flank = AllFlanks[i];
                for (int x = 0; x < flank.Count; x++)
                {
                    var squad = flank[x];
                    if (!squad.DataNodes.ContainsRef(node)) continue;
                    squad.Ships.AddUniqueRef(ship);
                    foreach (var flankShip in squad.Ships)
                    {
                        if (CenterShips.ContainsRef(flankShip)) { CenterShips.AddUniqueRef(ship); return; }
                        if (ScreenShips.ContainsRef(flankShip)) { ScreenShips.AddUniqueRef(ship); return; }
                        if (RearShips.ContainsRef(flankShip))   { RearShips.AddUniqueRef(ship);   return; }
                        if (RightShips.ContainsRef(flankShip))  { RightShips.AddUniqueRef(ship);  return; }
                        if (LeftShips.ContainsRef(flankShip))   { LeftShips.AddUniqueRef(ship);   return; }
                    }
                }
            }
        }

        public void SetAutoRequisition(bool value)
        {
            AutoRequisition = value;
        }

        bool AddShipToNodes(Ship shipToAdd)
        {
            shipToAdd.fleet = this;
            return AssignExistingOrCreateNewNode(shipToAdd);
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

            var mainShipList = new Array<Ship>();

            foreach (var ship in Ships)
            {
                TotalFleetAttributes.AddTargetValue(ship);
                mainShipList.Add(ship);
            }

            var fleetParameters = TotalFleetAttributes.GetAveragedValues();

            for (int i = mainShipList.Count - 1; i >= 0; i--)
            {
                Ship ship = mainShipList[i];
                SortIntoFlanks(ship, fleetParameters);
            }
        }

        void SortIntoFlanks(Ship ship, ShipAI.TargetParameterTotals fleetAverages)
        {
            int leftCount = LeftShips.Count;
            var roleType = ship.DesignRoleType;

            if (roleType != ShipData.RoleType.Warship)
            {
                RearShips.AddUniqueRef(ship);
            }
            else if (CommandShip == ship)
            {
                CenterShips.AddUniqueRef(ship);
            }
            else if (ship.DesignRole == ShipData.RoleName.carrier)
            {
                if (leftCount <= RightShips.Count)
                {
                    LeftShips.AddUniqueRef(ship);
                }
                else
                {
                    RightShips.AddUniqueRef(ship);
                }
            }
            else if (fleetAverages.ScreenShip(ship))
            {
                ScreenShips.AddUniqueRef(ship);
            }
            else if (fleetAverages.LongRange(ship) )
            {
                CenterShips.AddUniqueRef(ship);
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
            Screen,
            Center,
            Left,
            Right,
            Rear
        }

        public Vector2 GetFlankSize(Array<Squad> flank)
        {
            Vector2 size = Vector2.Zero;
            for (int i = 0; i < flank.Count; i++) 
                size = Vector2.Max(flank[i].GetSquadSize(), size);

            return size;
        }

        void FlankToCenterOffset(Array<Squad> flank, FlankType flankType)
        {
            if (flank.IsEmpty) return;

            Vector2 centerFlankSize = GetFlankSize(CenterFlank);
            int buildDirection = flankType == FlankType.Left ? -1 : 1;
            int shipIndexForWidth = buildDirection == -1 ? 2 : 1;

            float initialX = flankType == FlankType.Left ? -centerFlankSize.X : centerFlankSize.X;
            float initialY = 75;
            int columnMax = 4;
            int column = 1;
            int columnStartIndex = 0;
            //Vector2 position = new Vector2(initialX, initialY);

            for (int x = 0; x < flank.Count; x++)
            {
                Squad squad  = flank[x];

                shipIndexForWidth  = squad.Ships.Count > shipIndexForWidth ? shipIndexForWidth : -1;
                float width         = shipIndexForWidth == -1 || x == columnStartIndex ? 0 : squad.Ships[shipIndexForWidth].GridWidth * 16 * buildDirection;
                float previousWidth = x > 0 ? flank[x - 1].GetSquadSize().X * buildDirection : 0f;
                previousWidth       = x == columnStartIndex ? initialX : previousWidth;

                float height  = x - columnMax > -1 ? flank[x - columnMax].GetSquadSize().Y : initialY;
                var upperShip = squad.Ships.Count > 0 ? squad.Ships[0] : null;
                height       += upperShip?.GridHeight * 16 ?? 0;
                
                Vector2 position = new Vector2(previousWidth + width , height);
                squad.SetOffSets(position);
                column++;
                if (column > columnMax)
                {
                    column = 0;
                    columnStartIndex = x + 1;
                }
            }
        }

        void LeftFlankToCenterOffset() => FlankToCenterOffset(LeftFlank, FlankType.Left);
        void RightFlankToCenterOffset() => FlankToCenterOffset(RightFlank, FlankType.Right);

        static Vector2 GetLargestSquad(Array<Squad> squads)
        {
            if (squads.IsEmpty) return Vector2.Zero;

            Vector2 largest = Vector2.Zero;
            foreach (var squad in squads)
            {
                largest = Vector2.Max(largest, squad.GetSquadSize());
            }
            return largest;
        }

        public void AutoArrange()
        {
            ResetFlankLists(); // set up center, left, right, screen, rear...
            SetSpeed();

            CenterFlank = SortSquadBySpeed(CenterShips);
            LeftFlank   = SortSquadBySpeed(LeftShips);
            RightFlank  = SortSquadBySpeed(RightShips);
            ScreenFlank = SortSquadByDefense(ScreenShips);
            RearFlank   = SortSquadByUtility(RearShips);

            AllFlanks.Add(CenterFlank);
            AllFlanks.Add(LeftFlank);
            AllFlanks.Add(RightFlank);
            AllFlanks.Add(ScreenFlank);
            AllFlanks.Add(RearFlank);

            for (int x = 0; x < Ships.Count; x++)
            {
                Ship s = Ships[x];
                AssignExistingOrCreateNewNode(s);
            }
            
            var centerSize = ArrangeSquad(CenterFlank, Vector2.Zero, FlankType.Center);
            ArrangeSquad(ScreenFlank, centerSize * -1, FlankType.Screen);
            var screenSize = ArrangeSquad(RearFlank, centerSize, FlankType.Rear);

            LeftFlankToCenterOffset();
            RightFlankToCenterOffset();

            FinalPosition = AveragePosition(true);
            SetAllSquadsShipPositions(0.0f);
            SetAIDefaultTactics();
            for (int i = 0; i < Ships.Count; i++)
            {
                Ship s = Ships[i];
                if (s.InCombat)
                    continue;

                s.AI.OrderAllStop();
                s.AI.OrderThrustTowardsPosition(FinalPosition + s.FleetOffset, FinalDirection, false);
            }
        }

        void SetAIDefaultTactics()
        {
            if (Owner.isPlayer == true) return;

            for (int i = 0; i < ScreenFlank.Count; i++)
            {
                Squad squad = ScreenFlank[i];
                squad.SetSquadTactics(s =>
                {
                    if (s.MaxSTLSpeed >= AverageFleetAttributes.Speed)
                        s.AI.CombatState = CombatState.Artillery;
                    else
                        s.AI.CombatState = CombatState.ShortRange;
                });
            }

            for (int i = 0; i < CenterFlank.Count; i++)
            {
                Squad squad = CenterFlank[i];
                squad.SetSquadTactics(s => s.AI.CombatState = CombatState.Artillery);
            }
            SetOrdersRadius(Ships, AverageFleetAttributes.MaxSensorRange);
        }

        bool AssignExistingOrCreateNewNode(Ship ship)
        {
            FleetDataNode node = DataNodes.Find(n => n.Ship == ship || n.Ship == null && n.ShipName == ship.Name && n.GoalGUID == Guid.Empty);
            bool nodeFound = node != null;

            if (node == null)
            {
                var offset = ship.RelativeFleetOffset;// ship.RelativeFleetOffset + GetRelativeSize();
                node = new FleetDataNode
                {
                    
                    FleetOffset  = offset,
                    OrdersOffset = offset,
                    CombatState  = ship.AI.CombatState
            };
                DataNodes.Add(node);
            }

            node.Ship           = ship;
            node.ShipName       = ship.Name;
            node.OrdersRadius   = node.OrdersRadius < 2 ? ship.AI.GetSensorRadius() : node.OrdersRadius;
            ship.AI.FleetNode   = node;
            return nodeFound;
            
        }

        public void RefitNodeName(string oldName, string newName)
        {
            foreach (FleetDataNode node in DataNodes)
            {
                if (node.ShipName == oldName)
                    node.ShipName = newName;
            }
        }

        enum SquadSortType
        {
            Size,
            Speed,
            Defense,
            Utility
        }

        Array<Squad> SortSquadBySpeed(Array<Ship> allShips) => SortSquad(allShips, SquadSortType.Speed);
        Array<Squad> SortSquadBySize(Array<Ship> allShips) => SortSquad(allShips, SquadSortType.Size);
        Array<Squad> SortSquadByDefense(Array<Ship> allShips) => SortSquad(allShips, SquadSortType.Defense);
        Array<Squad> SortSquadByUtility(Array<Ship> allShips) => SortSquad(allShips, SquadSortType.Utility);

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
                    case SquadSortType.Defense: return (int)(ship.armor_max + ship.shield_max);
                    case SquadSortType.Utility: return ship.DesignRole == ShipData.RoleName.support || ship.DesignRoleType == ShipData.RoleType.Troop ? 1 : 0;
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
                {
                    var squadShip = allShips[x];
                    squad.Ships.Add(squadShip);
                    squad.DataNodes.AddUnique(squadShip.AI.FleetNode);
                }

                if (squad.Ships.Count != 4 && x != allShips.Count - 1)
                    continue;

                destSquad.Add(squad);
                squad = new Squad { Fleet = this };
            }
            return destSquad;
        }

        static Vector2 ArrangeSquad(Array<Squad> squads, Vector2 size, FlankType flank)
        {
            int upOrDown              = flank == FlankType.Screen ? -1 : 1;
            float spacer              = 1000 * upOrDown;
            Vector2 squadOffset       = new Vector2(0,spacer);
            int row                   = 0;
            int columns               = flank == FlankType.Screen ? 9 : 5;
            int rowMax                = 1 +(squads.Count / columns).LowerBound(columns);
            float tallestSquad        = 0;
            Vector2 previousSizeLeft  = Vector2.Zero;
            Vector2 previousSizeRight = Vector2.Zero;

            for (int index = 0; index < squads.Count; ++index)
            {
                var squad = squads[index];
                if (row == 0)
                {
                    int dir = upOrDown == 1 ? 1 : -1;
                    int wantedIndex = dir == 1 ? 0 : 3;
                    int wantedShip = squad.Ships.Count >= wantedIndex + 1? wantedIndex  : -1;
                    float height = dir == -1 ? 0 : squad.Ships[wantedShip].GridHeight * 16 * dir;
                    squad.SetOffSets(new Vector2(previousSizeLeft.X, squadOffset.Y + height));
                    previousSizeLeft = squad.GetSquadSize();
                    previousSizeRight = -squad.GetSquadSize();
                }
                else if (index % 2 == 1)
                {
                    int dir = squad.Ships.Count >= 3 ? 2 : -1;
                    float width = dir == -1 ? 0 : squad.Ships[dir].GridWidth * 16;
                    squad.SetOffSets(new Vector2(previousSizeLeft.X + width , squadOffset.Y));
                    previousSizeLeft = squad.GetSquadSize();
                }
                else
                {
                    int dir = squad.Ships.Count >= 2 ? 1 : -1;
                    float width = dir == -1 ? 0 : squad.Ships[dir].GridWidth * 16;
                    squad.SetOffSets(new Vector2(previousSizeRight.X - width, squadOffset.Y));
                    previousSizeRight = -squad.GetSquadSize();
                }

                tallestSquad = Math.Max(tallestSquad, squad.GetSquadSize().Y);
                row++;
                
                if (row > rowMax)
                {
                    row =  0;
                    squadOffset.Y = (flank == FlankType.Screen ? -1 : 1) * tallestSquad;
                    previousSizeLeft = previousSizeRight = Vector2.Zero;
                    tallestSquad = 0;
                }
            }
            return GetLargestSquad(squads);
        }

        void SetAllSquadsShipPositions(float facing)
        {
            for (int flank = 0; flank < AllFlanks.Count; flank++)
            {
                Array<Squad> squads = AllFlanks[flank];
                for (int i = 0; i < squads.Count; i++)
                {
                    Squad squad = squads[i];
                    squad.SetNodeOffsets(facing);
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

        /// <summary>Only For Specific fake fleet destruction </summary>
        public void UnSafeRemoveShip(Ship ship)
        {
            RemoveFromAllSquads(ship);
            Ships.Remove(ship);
            ship.fleet = null;
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
                case MilitaryTask.TaskType.StrikeForce:
                case MilitaryTask.TaskType.AssaultPlanet:              DoAssaultPlanet(FleetTask);              break;
                case MilitaryTask.TaskType.ClearAreaOfEnemies:         DoClearAreaOfEnemies(FleetTask);         break;
                case MilitaryTask.TaskType.CohesiveClearAreaOfEnemies: DoCohesiveClearAreaOfEnemies(FleetTask); break;
                case MilitaryTask.TaskType.Exploration:                DoExplorePlanet(FleetTask);              break;
                case MilitaryTask.TaskType.DefendSystem:               DoDefendSystem(FleetTask);               break;
                case MilitaryTask.TaskType.DefendClaim:                DoClaimDefense(FleetTask);               break;
                case MilitaryTask.TaskType.DefendPostInvasion:         DoPostInvasionDefense(FleetTask);        break;
                case MilitaryTask.TaskType.GlassPlanet:                DoGlassPlanet(FleetTask);                break;
                case MilitaryTask.TaskType.AssaultPirateBase:          DoAssaultPirateBase(FleetTask);          break;
                case MilitaryTask.TaskType.RemnantEngagement:          DoRemnantEngagement(FleetTask);          break;
                case MilitaryTask.TaskType.DefendVsRemnants:           DoDefendVsRemnant(FleetTask);            break;
                case MilitaryTask.TaskType.GuardBeforeColonize:        DoPreColonizationGuard(FleetTask);       break;
            }
        }

        void DoExplorePlanet(MilitaryTask task)
        {
            Planet targetPlanet     = task.TargetPlanet;
            bool eventBuildingFound = targetPlanet.EventsOnTiles();
            if (task.TargetEmpire == null)
                task.TargetEmpire = Owner.GetEmpireAI().ThreatMatrix.GetDominantEmpireInSystem(targetPlanet.ParentSystem);

            if (EndInvalidTask(!eventBuildingFound
                               || targetPlanet.Owner != null && !Owner.IsAtWarWith(targetPlanet.Owner)
                               || !StillInvasionEffective(task)
                               || !StillCombatEffective(task)))
            {
                return;
            }

            switch (TaskStep)
            {
                case 0:
                    if (!GatherAtRallyFirst(task))
                    {
                        AddFleetProjectorGoal();
                        TaskStep = 2;
                        break;
                    }

                    if (FleetTaskGatherAtRally(task))
                        TaskStep = 1;

                    break;
                case 1:
                    if (!HasArrivedAtRallySafely() || Ships.Any(s => s?.System == task.RallyPlanet.ParentSystem && s?.InCombat == true))
                        break;

                    if (!task.TargetPlanet.ParentSystem.HasPlanetsOwnedBy(Owner))
                        AddFleetProjectorGoal();

                    TaskStep = 2;
                    break;
                case 2:
                    if (FleetProjectorGoalInProgress(task.TargetPlanet.ParentSystem))
                        break;

                    GatherAtAO(task, distanceFromAO: targetPlanet.GravityWellRadius);
                    TaskStep = 3;
                    break;
                case 3:
                    if (ArrivedAtCombatRally(FinalPosition))
                    {
                        TaskStep = 4;
                        var combatOffset = task.AO.OffsetTowards(AveragePosition(), targetPlanet.GravityWellRadius);
                        EscortingToPlanet(combatOffset, false);
                    }
                    break;
                case 4:
                    var planetMoveStatus = FleetMoveStatus(targetPlanet.GravityWellRadius, FinalPosition);

                    if (!planetMoveStatus.HasFlag(MoveStatus.MajorityAssembled))
                    {
                        if (planetMoveStatus.HasFlag(MoveStatus.AssembledInCombat))
                        {
                            ClearPriorityOrderForShipsInAO(Ships, FinalPosition, targetPlanet.GravityWellRadius);
                        }
                        break;
                    }
                    var planetGuard = task.AO.OffsetTowards(AveragePosition(), 500);
                    EngageCombatToPlanet(planetGuard, true);
                    TaskStep = 5;
                    break;

                case 5:
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
            switch (TaskStep)
            {
                case 0:
                    SetPostInvasionFleetCombat();
                    DefenseTurns = Owner.PersonalityModifiers.PostInvasionTurns;
                    TaskStep     = 1;
                    break;
                case 1:
                    if (!DoOrbitTaskArea(task))
                        AttackEnemyStrengthClumpsInAO(task);
                    else
                        EndInvalidTask(--DefenseTurns <= 0 && !Owner.SystemWithThreat.Any(t => t.TargetSystem == task.TargetPlanet.ParentSystem));

                    break;
            }
        }

        public static void CreatePostInvasionFromCurrentTask(Fleet fleet, MilitaryTask task, Empire owner, string name)
        {
            task.FlagFleetNeededForAnotherTask();
            fleet.TaskStep   = 0;
            var postInvasion = MilitaryTask.CreatePostInvasion(task.TargetPlanet, task.WhichFleet, owner);
            fleet.Name       = name;
            fleet.FleetTask  = postInvasion;
            owner.GetEmpireAI().QueueForRemoval(task);
            owner.GetEmpireAI().AddPendingTask(postInvasion);
        }

        public static void CreateReclaimFromCurrentTask(Fleet fleet, MilitaryTask task, Empire owner)
        {
            task.FlagFleetNeededForAnotherTask();
            fleet.TaskStep   = 0;
            var reclaim      = MilitaryTask.CreateReclaimTask(owner, task.TargetPlanet, task.WhichFleet);
            fleet.Name       = "Reclaim Fleet";
            fleet.FleetTask  = reclaim;
            owner.GetEmpireAI().QueueForRemoval(task);
            owner.GetEmpireAI().AddPendingTask(reclaim);
        }

        bool GatherAtRallyFirst(MilitaryTask task)
        {
            Vector2 enemySystemPos = task.TargetPlanet.ParentSystem.Position;
            Vector2 rallySystemPos = task.RallyPlanet.ParentSystem.Position;

            return rallySystemPos.Distance(enemySystemPos) > AveragePos.Distance(rallySystemPos);
        }

        void DoAssaultPlanet(MilitaryTask task)
        {
            if (!Owner.IsEmpireAttackable(task.TargetPlanet.Owner))
                TaskStep = 9;
            else
                task.TargetEmpire = task.TargetPlanet.Owner;

            switch (TaskStep)
            {
                case 0:
                    if (AveragePos.InRadius(task.TargetPlanet.ParentSystem.Position, task.TargetPlanet.ParentSystem.Radius * 2))
                    {
                        TaskStep = 6;
                        break;
                    }

                    if (!GatherAtRallyFirst(task))
                    {
                        AddFleetProjectorGoal();
                        TaskStep = 2;
                        break;
                    }

                    if (FleetTaskGatherAtRally(task))
                        TaskStep = 1;

                    break;
                case 1:
                    if (!HasArrivedAtRallySafely(GetRelativeSize().Length()))
                        break;

                    if (!task.TargetPlanet.ParentSystem.HasPlanetsOwnedBy(Owner))
                        AddFleetProjectorGoal();

                    TaskStep = 2; 
                    break;
                case 2: 
                    if (FleetProjectorGoalInProgress(task.TargetPlanet.ParentSystem))
                        break;

                    SetOrdersRadius(Ships, 5000);
                    GatherAtAO(task, distanceFromAO: Owner.GetProjectorRadius() * 2f);
                    TaskStep = 3;
                    break;
                case 3:
                    if (!ArrivedAtCombatRally(FinalPosition))
                        break;
                    
                    TaskStep = 4; // Note - Reclaim fleets (from clear area) are set to this step number when created
                    SetOrdersRadius(Ships, 5000f);
                    break;
                case 4:
                    MoveStatus combatRally = FleetMoveStatus(0, FinalPosition);
                    if (!combatRally.HasFlag(MoveStatus.MajorityAssembled))
                    {
                        if (combatRally.HasFlag(MoveStatus.AssembledInCombat))
                            ClearPriorityOrderForShipsInAO(Ships, FinalPosition, GetRelativeSize().Length() / 2);

                        if (combatRally.HasFlag(MoveStatus.Dispersed))
                        {
                            GatherAtAO(task, distanceFromAO: 30000);
                            TaskStep = 3;
                        }

                        break;
                    }

                    TaskStep = 5;
                    break;
                case 5:
                    Vector2 combatOffset = task.AO.OffsetTowards(AveragePosition(), task.TargetPlanet.GravityWellRadius);
                    EscortingToPlanet(combatOffset, false);
                    TaskStep = 6;
                    break;
                case 6:
                    combatOffset = task.AO.OffsetTowards(AveragePosition(), task.TargetPlanet.GravityWellRadius);
                    MoveStatus inPosition = FleetMoveStatus(task.TargetPlanet.GravityWellRadius, combatOffset);
                    if (!inPosition.HasFlag(MoveStatus.MajorityAssembled))
                    {
                        if (inPosition.HasFlag(MoveStatus.AssembledInCombat))
                            ClearPriorityOrderForShipsInAO(Ships, combatOffset, GetRelativeSize().Length());
                        else
                            EscortingToPlanet(combatOffset, false);
                    }

                    RearShipsToCombat(combatOffset, false);
                    Vector2 resetPosition = task.AO.OffsetTowards(AveragePosition(), 1500);
                    EngageCombatToPlanet(resetPosition, true);
                    TaskStep = 7;
                    break;

                case 7:
                    switch (StatusOfPlanetAssault(task))
                    {
                        case Status.NotApplicable: TaskStep = 6;         break;
                        case Status.Good:          TaskStep = 8;         break;
                        case Status.Critical:      TaskStep = 9;         return;
                    }

                    break;
                case 8:
                    if (ShipsOffMission(task))
                    {
                        TaskStep = 6;
                        break;
                    }
                    TaskStep = 7;
                    break;
                case 9:
                    if (TryGetNewTargetPlanet(task, out Planet newTarget)
                        && task.GetMoreTroops(newTarget, out Array<Ship> troopShips))
                    {
                        AddShips(troopShips);
                        AutoArrange();
                        FinalPosition = task.TargetPlanet.Center;
                        task.AO       = task.TargetPlanet.Center;
                        bool inSystem = AveragePos.InRadius(newTarget.ParentSystem.Position, newTarget.ParentSystem.Radius);
                        if (inSystem)
                        {
                            TaskStep = 3;
                            GatherAtAO(task, distanceFromAO: 30000);
                        }
                        else
                        {
                            if (!task.TargetPlanet.ParentSystem.HasPlanetsOwnedBy(Owner))
                                AddFleetProjectorGoal();

                            TaskStep = 2;
                        }

                        task.SetTargetPlanet(newTarget);
                        task.AO = newTarget.Center;
                    }
                    else
                    {
                        CreatePostInvasionFromCurrentTask(this, task, Owner, "Post Invasion Defense");
                    }

                    return;
            }

            bool invasionEffective = StillInvasionEffective(task);
            bool combatEffective   = StillCombatEffective(task);
            bool remnantsTargeting = !Owner.WeAreRemnants
                                        && CommandShip?.System == task.TargetPlanet.ParentSystem
                                        && EmpireManager.Remnants.GetFleetsDict().Values.ToArray()
                                           .Any(f => f.FleetTask?.TargetPlanet?.ParentSystem == task.TargetPlanet.ParentSystem);

            EndInvalidTask(!invasionEffective || !combatEffective || remnantsTargeting);
        }

        bool TryGetNewTargetPlanet(MilitaryTask task, out Planet newTarget)
        {
            Planet currentTarget = task.TargetPlanet;
            if (currentTarget.Owner != null && Owner.IsAtWarWith(currentTarget.Owner))
            {
                newTarget = currentTarget; // Invasion or bombing was not effective, retry
                return true; 
            }

            var currentSystem    = currentTarget.ParentSystem;
            newTarget            = currentSystem.PlanetList.Find(p => Owner.IsAtWarWith(p.Owner));

            if (newTarget != null)
                return true;

            newTarget =  task.type == MilitaryTask.TaskType.StrikeForce 
                ? TryGetNewTargetPlanetStrike(currentSystem, task.TargetEmpire) 
                : TryGetNewTargetPlanetInvasion(currentSystem);

            return newTarget != null;
        }

        Planet TryGetNewTargetPlanetInvasion(SolarSystem system)
        {
            var potentialSystems = system.FiveClosestSystems.Filter(s => s.PlanetList.Any(p => p.Owner?.IsAtWarWith(Owner) == true));
            if (potentialSystems.Length == 0)
                return null;

            SolarSystem potentialSystem = potentialSystems.Sorted(s => s.GetKnownStrengthHostileTo(Owner)).First();
            return potentialSystem.PlanetList.Find(p => Owner.IsAtWarWith(p.Owner));
        }

        Planet TryGetNewTargetPlanetStrike(SolarSystem system, Empire enemy)
        {
            var planets = enemy.GetPlanets();
            return planets.Count == 0 ? null : planets.FindMin(p => p.Center.Distance(system.Position));
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
                    if (FleetTaskGatherAtRally(task))
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
            if (EndInvalidTask(task.TargetPlanet.Owner != null 
                               && !task.TargetPlanet.Owner.isFaction 
                               && !task.TargetPlanet.Owner.data.IsRebelFaction
                               || !CanTakeThisFight(task.EnemyStrength, task)))
            {
                return;
            }

            task.AO = task.TargetPlanet.Center;
            switch (TaskStep)
            {
                case 0:
                    if (!GatherAtRallyFirst(task))
                    {
                        AddFleetProjectorGoal();
                        TaskStep = 2;
                        break;
                    }

                    if (FleetTaskGatherAtRally(task))
                        TaskStep = 1;

                    break;
                case 1:
                    if (!HasArrivedAtRallySafely()
                        || Ships.Any(s => s?.System == task.RallyPlanet.ParentSystem && s?.InCombat == true))
                    {
                        break;
                    }

                    if (!task.TargetPlanet.ParentSystem.HasPlanetsOwnedBy(Owner))
                        AddFleetProjectorGoal();

                    TaskStep = 2;
                    break;
                case 2:
                    if (FleetProjectorGoalInProgress(task.TargetPlanet.ParentSystem))
                        break;

                    GatherAtAO(task, FleetTask.TargetPlanet.ParentSystem.Radius);
                    TaskStep = 3;
                    break;
                case 3:
                    if (!ArrivedAtCombatRally(FinalPosition, GetRelativeSize().Length() * 2))
                        break;

                    TaskStep = 4;
                    CancelFleetMoveInArea(task.AO, task.AORadius * 2);
                    break;
                case 4:
                    CombatMoveToAO(task, FleetTask.TargetPlanet.GravityWellRadius * 1.5f);
                    TaskStep = 5;
                    break;
                case 5:
                    if (!ArrivedAtCombatRally(FinalPosition, GetRelativeSize().Length() * 2))
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
                    TaskStep = task.TargetPlanet != null ? 8 : 9; // if we need to capture the planet, go to 8.

                    break;
                case 8:
                    if (StillInvasionEffective(task))
                    {
                        OrderShipsToInvade(Ships, task, false);
                        break;
                    }

                    TaskStep = 9;
                    break;
                case 9: // waiting for colonization goal to issue orders to this fleet
                    if (!DoOrbitTaskArea(task, excludeInvade: true))
                        AttackEnemyStrengthClumpsInAO(task);

                    break;
            }
        }

        void DoRemnantEngagement(MilitaryTask task)
        {
            Planet target = FleetTask.TargetPlanet;
            switch (TaskStep)
            {
                case 1:
                    if (FleetInAreaInCombat(GetAveragePosition(Ships), 50000) == CombatStatus.InCombat)
                        break;

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
                    ClearOrders();
                    GatherAtAO(task, 500);
                    TaskStep = 9;
                    break;
                case 9:
                    if (!ArrivedAtCombatRally(FinalPosition, 50000))
                        break;

                    TaskStep = 10; // Goal will wait for fleet to be in this task to disband it.
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
            float secondsToTarget = distanceToPlanet / slowestWarpSpeed;
            float turnsToTarget = secondsToTarget / GlobalStats.TurnTimer;
            starDateEta = (Empire.Universe.StarDate + turnsToTarget / 10).RoundToFractionOf10();

            return starDateEta.Greater(0);
        }

        void DoPreColonizationGuard(MilitaryTask task)
        {
            if (EndInvalidTask(task.TargetPlanet.Owner != null
                               || !task.TargetPlanet.ParentSystem.HasPlanetsOwnedBy(Owner) && task.TargetPlanet.ParentSystem.OwnerList.Count > 0
                               || Owner.KnownEnemyStrengthIn(task.TargetPlanet.ParentSystem)
                                   > task.MinimumTaskForceStrength / Owner.GetFleetStrEmpireMultiplier(task.TargetEmpire)))
            {
                ClearOrders();
                return;
            }

            switch (TaskStep)
            {
                case 1:
                    GatherAtAO(task, 500);
                    TaskStep = 2;
                    break;
                case 2:
                    if (!ArrivedAtCombatRally(task.AO, 20000))
                        break;

                    TaskStep = 3;
                    break;
            }

            OrderFleetOrbit(task.TargetPlanet);
        }

        void DoDefendVsRemnant(MilitaryTask task)
        {
            if (EndInvalidTask(!CanTakeThisFight(task.EnemyStrength, task) || !Owner.GetEmpireAI().Goals.Any(g => g.Fleet == this)))
            {
                ClearOrders();
                return;
            }
            
            switch (TaskStep)
            {
                case 0:
                    GatherAtAO(task, 3000);
                    TaskStep = 1;
                    break;
                case 1:
                    if (!ArrivedAtCombatRally(task.AO, GetRelativeSize().Length() / 2))
                        break;

                    TaskStep = 2; // Defend till death (or until the DefenseVsRemnant goal redirects us)!
                    CancelFleetMoveInArea(task.AO, task.AORadius * 2);
                    break;
            }
        }

        void DoAssaultPirateBase(MilitaryTask task)
        {
            if (EndInvalidTask(!CanTakeThisFight(task.EnemyStrength, task)))
                return;

            if (EndInvalidTask(task.TargetShip == null || !task.TargetShip.Active)) // Pirate base is dead
            {
                ClearOrders();
                return;
            }

            task.AO = task.TargetShip.Center;
            switch (TaskStep)
            {
                case 0:
                    if (FleetTaskGatherAtRally(task))
                        TaskStep = 1;

                    break;
                case 1:
                    if (!HasArrivedAtRallySafely(task.RallyPlanet.ParentSystem.Radius)
                        || Ships.Any(s => s?.System == task.RallyPlanet.ParentSystem && s?.InCombat == true))
                    {
                        break;
                    }

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
            if (task.TargetPlanet.Owner == null || !Owner.IsEmpireAttackable(task.TargetPlanet.Owner))
                TaskStep = 6;
            else
                task.TargetEmpire = task.TargetPlanet.Owner;

            task.AO = task.TargetPlanet.Center;
            switch (TaskStep)
            {
                case 0:
                    if (!GatherAtRallyFirst(task))
                    {
                        AddFleetProjectorGoal();
                        TaskStep = 2;
                        break;
                    }

                    if (FleetTaskGatherAtRally(task))
                        TaskStep = 1;

                    break;
                case 1:
                    MoveStatus moveStatus = FleetMoveStatus(task.RallyPlanet.ParentSystem.Radius);
                    if (moveStatus.HasFlag(MoveStatus.MajorityAssembled) && !task.RallyPlanet.ParentSystem.HostileForcesPresent(Owner))
                    {
                        if (!task.TargetPlanet.ParentSystem.HasPlanetsOwnedBy(Owner))
                            AddFleetProjectorGoal();
                         
                        TaskStep = 2;
                    }

                    break;
                case 2:
                    if (FleetProjectorGoalInProgress(task.TargetPlanet.ParentSystem))
                        break;

                    GatherAtAO(task, 400000);
                    TaskStep = 3;
                    break;
                case 3:
                    if (!ArrivedAtCombatRally(FinalPosition))
                        break;

                    TaskStep = 4;
                    break;
                case 4:
                    EngageCombatToPlanet(task.TargetPlanet.Center, true);
                    StartBombing(task.TargetPlanet);
                    TaskStep = 5;
                    break;
                case 5:
                    if (ShipsOffMission(task))
                        TaskStep = 4;
                    StartBombing(task.TargetPlanet);
                    break;
                case 6:
                    Owner.DecreaseFleetStrEmpireMultiplier(task.TargetEmpire);
                    if (TryGetNewTargetPlanet(task, out Planet newTarget))
                    {
                        FinalPosition = task.TargetPlanet.Center;
                        task.AO       = task.TargetPlanet.Center;
                        bool inSystem = AveragePos.InRadius(newTarget.ParentSystem.Position, newTarget.ParentSystem.Radius);
                        if (inSystem)
                        {
                            TaskStep = 3;
                            GatherAtAO(task, distanceFromAO: 30000);
                        }
                        else
                        {
                            if (!task.TargetPlanet.ParentSystem.HasPlanetsOwnedBy(Owner))
                                AddFleetProjectorGoal();

                            TaskStep = 2;
                        }

                        task.SetTargetPlanet(newTarget);
                        task.AO = newTarget.Center;
                    }
                    else
                    {
                        CreatePostInvasionFromCurrentTask(this, task, Owner, "Post Invasion Defense");
                    }

                    return;
            }

            bool remnantsTargeting = !Owner.WeAreRemnants
                                        && CommandShip?.System == task.TargetPlanet.ParentSystem
                                        && EmpireManager.Remnants.GetFleetsDict().Values.ToArray()
                                           .Any(f => f.FleetTask?.TargetPlanet?.ParentSystem == task.TargetPlanet.ParentSystem);

            if (EndInvalidTask(task.TargetPlanet.Owner == null || remnantsTargeting || !StillCombatEffective(task)))
                return;

            bool bombOk  = Ships.Select(s => s.Bomb60SecStatus()).Any(bt => bt != Status.NotApplicable && bt != Status.Critical);
            if (!bombOk)
                EndInvalidTask(true);
        }

        void DoClearAreaOfEnemies(MilitaryTask task)
        {
            if (task.TargetEmpire == null && FleetTask.TargetSystem != null)
                task.TargetEmpire = Owner.GetEmpireAI().ThreatMatrix.GetDominantEmpireInSystem(FleetTask.TargetSystem);

            float enemyStrength = Owner.GetEmpireAI().ThreatMatrix.PingHostileStr(task.AO, task.AORadius, Owner);

            if (EndInvalidTask(!CanTakeThisFight(enemyStrength*0.5f, task))) 
                return;

            switch (TaskStep)
            {
                case 0:
                    if (AveragePos.InRadius(task.TargetSystem?.Position ?? task.AO, Owner.GetProjectorRadius()))
                        GatherAtAO(task, distanceFromAO: 30000);
                    else
                        GatherAtAO(task, distanceFromAO: Owner.GetProjectorRadius());

                    TaskStep = 1;
                    break;
                case 1:
                    if (!ArrivedAtCombatRally(FinalPosition))
                    {
                        ClearPriorityOrderForShipsInAO(Ships, task.AO, Owner.GetProjectorRadius());
                        break;
                    }

                    TaskStep = 2;
                    break;
                case 2:
                    ClearPriorityOrderForShipsInAO(Ships, task.AO, Owner.GetProjectorRadius());
                    if (AttackEnemyStrengthClumpsInAO(task, Ships))
                        break;

                    TaskStep = 3;
                    break;
                case 3:
                    if (task.TargetPlanet != null)
                        DoOrbitTaskArea(task);
                    else
                        DoCombatMoveToTaskArea(task, true);

                    bool threatIncoming = Owner.SystemWithThreat.Any(t => !t.ThreatTimedOut && t.TargetSystem == FleetTask.TargetSystem);
                    bool stillThreats = threatIncoming || enemyStrength > 1;
                    if (!stillThreats)
                        TaskStep = 4;

                    break;
                case 4:
                    SolarSystem  system = task.TargetSystem;
                    if (!system.PlanetList.Any(p => p.Owner != null && Owner.IsAtWarWith(p.Owner)))
                    {
                        EndInvalidTask(true);
                    }
                    else
                    {
                        Planet newTarget = system.PlanetList.FindMaxFiltered(p => p.Owner != null
                                                                                  && Owner.IsAtWarWith(p.Owner), p => p.ColonyPotentialValue(Owner));

                        if (task.GetMoreTroops(newTarget, out Array<Ship> troopShips))
                        {
                            task.SetTargetPlanet(newTarget);
                            task.AO = task.TargetPlanet.Center;
                            AddShips(troopShips);
                            AutoArrange();
                            CreateReclaimFromCurrentTask(this, task, Owner);
                            if (!task.TargetPlanet.ParentSystem.HasPlanetsOwnedBy(Owner))
                                AddFleetProjectorGoal();

                            GatherAtAO(task, distanceFromAO: 20000);
                            TaskStep = 4;
                        }
                        else
                        {
                            EndInvalidTask(true);
                        }
                    }

                    break;
            }
        }

        bool EndInvalidTask(bool condition)
        {

            if (!condition) 
                return false;

            FleetTask.EndTask();
            FleetTask = null;
            TaskStep  = 0;
            return true;
        }

        void OrderFleetOrbit(Planet planet)
        {
            for (int i = 0; i < Ships.Count; i++)
            {
                Ship ship = Ships[i];
                ship.OrderToOrbit(planet);
                ship.AI.SetPriorityOrder(false);
            }
        }

        void AddFleetProjectorGoal()
        {
            if (FleetTask?.TargetPlanet == null)
                return;

            Goal goal = new DeployFleetProjector(this, FleetTask.TargetPlanet, Owner);
            Owner.GetEmpireAI().AddGoal(goal);
        }

        bool FleetProjectorGoalInProgress(SolarSystem targetSystem)
        {
            if (targetSystem.IsExclusivelyOwnedBy(Owner))
                return false; // no need for projector goal

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

        public void ClearOrders()
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
                    ship.AI.SetPriorityOrder(true);
            }
        }

        void SetAllShipsPriorityOrder() => SetPriorityOrderToShipsIf(Ships, s => s.CanTakeFleetOrders);

        bool FleetTaskGatherAtRally(MilitaryTask task)
        {
            var ownerSystems = Owner.GetOwnedSystems().Filter(s => AveragePos.InRadius(s.Position, s.Radius));
            if (ownerSystems.Length == 1)
            {
                SolarSystem system = ownerSystems.First();
                if (system.DangerousForcesPresent(Owner) && Ships.Any(s => s.System == system && s.InCombat))
                    return false;
            }
            
            Planet planet       = task.RallyPlanet;
            Vector2 movePoint   = planet.Center;
            Vector2 finalFacing = movePoint.DirectionToTarget(task.AO);

            MoveToNow(movePoint, finalFacing, false);
            return true;
        }

        bool HasArrivedAtRallySafely(float fleetRadius = 0)
        {
            MoveStatus status = MoveStatus.None;

            status = FleetMoveStatus(fleetRadius);

            if (FinalPosition.InRadius(AveragePos, fleetRadius) )
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
            radius = radius.AlmostZero() ? GetRelativeSize().Length() * 1.5f : radius;
            radius = Math.Max(1000, radius);
            MoveStatus status = FleetMoveStatus(radius, position);

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
            return status.HasFlag(MoveStatus.MajorityAssembled);
        }

        Ship[] AvailableShips => AllButRearShips.Filter(ship => !ship.AI.HasPriorityOrder);

        bool AttackEnemyStrengthClumpsInAO(MilitaryTask task) => AttackEnemyStrengthClumpsInAO(task, AvailableShips);

        bool AttackEnemyStrengthClumpsInAO(MilitaryTask task, IEnumerable<Ship> ships)
        {
            var availableShips = new Array<Ship>(ships);
            if (availableShips.Count == 0) return false;

            Map<Vector2, float> enemyClumpsDict = Owner.GetEmpireAI().ThreatMatrix
                .PingRadarStrengthClusters(task.AO, task.AORadius, 10000, Owner);

            if (enemyClumpsDict.Count == 0)
                return false;

            while (availableShips.Count > 0)
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
            if (strengthCluster.Strength <= 0) 
                return false;

            FleetMoveToPosition(strengthCluster.Position, 7500, true);
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
            bool bombing = BombPlanet(task);
            bool readyToInvade = ReadyToInvade(task);

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
        public bool CanTakeThisFight(float enemyFleetStrength, MilitaryTask task, bool debug = false)
        {
            float ourStrengthThreshold = GetStrength() * 2;
            if (enemyFleetStrength < ourStrengthThreshold)
                return true;

            // We cannot win, update fleet multipliers for next time
            if (!debug)
                Owner.IncreaseFleetStrEmpireMultiplier(task.TargetEmpire);

            return false;
        }

        bool StillCombatEffective(MilitaryTask task)
        {
            float enemyStrength = Owner.GetEmpireAI().ThreatMatrix.PingHostileStr(task.AO, task.AORadius, Owner);
            if (CanTakeThisFight(enemyStrength, task))
                return true;

            DebugInfo(task, $"Enemy Strength too high. Them: {enemyStrength} Us: {GetStrength()}");
            return false;
        }

        bool StillInvasionEffective(MilitaryTask task)
        {
            bool troopsOnPlanet = task.TargetPlanet.AnyOfOurTroops(Owner);
            bool invasionTroops = Ships.Any(troops => troops.DesignRole == ShipData.RoleName.troop || troops.Carrier.AnyAssaultOpsAvailable) && GetStrength() > 0;
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

        public bool RemoveShip(Ship ship, bool andSquad = false)
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
        public Vector2 GetFormationPos(Ship ship) => AveragePosition() + ship.FleetOffset; //- AverageOffsetFromZero;

        // @return The Final destination position for this ship
        public Vector2 GetFinalPos(Ship ship)
        {
            if (CommandShip?.InCombat == true && FinalPosition.InRadius(CommandShip.Center, CommandShip.AI.FleetNode.OrdersRadius))
                return CommandShip.Center + ship.FleetOffset;
            
            return FinalPosition + ship.FleetOffset;
        }

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
            node = DataNodes.Find(d=>d.Ship == ship);
            return node != null;
        }

        public bool GoalGuidExists(Guid guid)
        {
            return DataNodes.Any(n => n.GoalGUID == guid);
        }

        public bool FindNodeWithGoalGuid(Guid guid, out FleetDataNode node)
        {
            node = DataNodes.Find(n => n.GoalGUID == guid);
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
            var fleetTotals = new ShipAI.TargetParameterTotals();
            if (Ships.Count == 0) return;
            if (CommandShip?.fleet != this || !CommandShip.CanTakeFleetMoveOrders())
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

                if (CommandShip == null && ship.CanTakeFleetOrders)
                {
                    if ((commandShip?.SurfaceArea ?? 0) < ship.SurfaceArea)
                        commandShip = ship;
                }

                fleetTotals.AddTargetValue(ship);

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

            ReadyForWarp          = readyForWarp;
            TotalFleetAttributes  = fleetTotals;
            AverageFleetAttributes = TotalFleetAttributes.GetAveragedValues();
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

        public void AddFleetDataNodes(Array<FleetDataNode> nodes)
        {
            foreach (var node in nodes)
                DataNodes.AddUnique(node);
        }

        public sealed class Squad
        {
            public FleetDataNode MasterDataNode = new FleetDataNode();
            public Array<FleetDataNode> DataNodes = new Array<FleetDataNode>();
            public Array<Ship> Ships = new Array<Ship>();
            public Fleet Fleet;
            public Vector2 Offset;

            public float GetShipDirectionFromShip(Ship ship)
            {
                return GetShipIndexDirection(Ships.IndexOfRef(ship));
            }

            public float GetShipIndexDirection(int index)
            {
                float radiansAngle = 0;
                switch (index)
                {
                    default:
                    case 0:
                        radiansAngle = RadMath.RadiansUp;
                        break;
                    case 1:
                        radiansAngle = RadMath.RadiansLeft;
                        break;
                    case 2:
                        radiansAngle = RadMath.RadiansRight;
                        break;
                    case 3:
                        radiansAngle = RadMath.RadiansDown;
                        break;
                }
                return radiansAngle;
            }

            public Vector2 GetSquadSize()
            {
                float x = 0;
                float y = 0;

                for (int i = 0; i < DataNodes.Count; i++)
                {
                    var n        = DataNodes[i];
                    float width  = n.Ship?.GridWidth * 8f ?? 0;
                    float height = n.Ship?.GridHeight * 8f ?? 0;
                    Vector2 relativeOffset = Offset - Fleet.AveragePos;
                    float nodeX  = Math.Abs((n.FleetOffset - Offset).X);
                    float nodeY  = Math.Abs((n.FleetOffset - Offset).Y);

                    x = Math.Max(x, nodeX) + width + 75f;
                    y = Math.Max(y, nodeY) + height + 75f;
                    
                }
                return new Vector2(x,y);
            }

            public void SetNodeOffsets(float facing = 0.0f)
            {
                for (int index = 0; index < DataNodes.Count; ++index)
                {
                    var node = DataNodes[index];
                    var ship = node?.Ship;
                    if (ship == null) continue;

                    float radiansAngle = GetShipIndexDirection(index);
                    Vector2 offset = (facing + Offset.ToRadians()).RadiansToDirection() * Offset.Length();

                    node.Ship.FleetOffset         = offset;
                    node.Ship.RelativeFleetOffset = offset;
                    node.CombatState              = node.Ship.AI.CombatState;
                    ship.FleetOffset              = offset + (facing + radiansAngle).RadiansToDirection() * (ship.Radius + 75f);
                    ship.RelativeFleetOffset      = Offset + radiansAngle.RadiansToDirection() * (ship.Radius + 75f);
                }
            }

            void ClearSquadOffsets()
            {
                Offset = Vector2.Zero;
                foreach (var node in DataNodes)
                {
                    node.FleetOffset  = Vector2.Zero;
                    node.OrdersOffset = Vector2.Zero;

                    if (node.Ship != null)
                    {
                        node.Ship.FleetOffset         = Vector2.Zero;
                        node.Ship.RelativeFleetOffset = Vector2.Zero;
                        node.CombatState              = node.Ship.AI.CombatState;
                        node.OrdersRadius             = node.Ship.SensorRange;
                    }
                }
            }

            public void SetOffSets(Vector2 offset)
            {
                ClearSquadOffsets();
                Offset = offset;
                SetNodeOffsets();
            }

            public void SetSquadTactics(Action<Ship> tactic)
            {
                for (int i = 0; i < Ships.Count; i++)
                {
                    var ship = Ships[i];
                    if (ship == null) continue;
                    tactic(ship);
                }
            }
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
