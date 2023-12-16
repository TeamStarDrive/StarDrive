using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI.Tasks;
using Ship_Game.Debug;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using SDGraphics;
using SDUtils;
using Ship_Game.Fleets.FleetTactics;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Vector2 = SDGraphics.Vector2;
using Ship_Game.Universe;

namespace Ship_Game.Fleets
{
    [StarDataType]
    public sealed class Fleet : ShipGroup
    {
        [StarData] public readonly Array<FleetDataNode> DataNodes = new();
        
        // unique object id assigned by the universe
        [StarData] public readonly int Id;
        
        // fleet key [1..9] when assigned to the Empire
        [StarData] public int Key;
        
        [StarData] public string Name = "";
        public ShipAI.TargetParameterTotals TotalFleetAttributes;
        public ShipAI.TargetParameterTotals AverageFleetAttributes;

        [StarData] readonly Array<Ship> CenterShips  = new();
        [StarData] readonly Array<Ship> LeftShips    = new();
        [StarData] readonly Array<Ship> RightShips   = new();
        [StarData] readonly Array<Ship> RearShips    = new();
        [StarData] readonly Array<Ship> ScreenShips  = new();
        [StarData] public Array<Squad> CenterFlank   = new();
        [StarData] public Array<Squad> LeftFlank     = new();
        [StarData] public Array<Squad> RightFlank    = new();
        [StarData] public Array<Squad> ScreenFlank   = new();
        [StarData] public Array<Squad> RearFlank     = new();
        [StarData] public readonly Array<Array<Squad>> AllFlanks = new();

        int DefenseTurns = 50;
        [StarData] public MilitaryTask FleetTask;
        [StarData] public CombatStatus TaskCombatStatus = CombatStatus.InCombat;

        [StarData] public int FleetIconIndex;
        public SubTexture Icon => ResourceManager.FleetIcon(FleetIconIndex);

        [StarData] public int TaskStep;
        [StarData] public bool IsCoreFleet;
        [StarData] public bool AutoRequisition { get; private set; }

        Ship[] AllButRearShips => Ships.Except(RearShips);
        [StarData] public bool HasRepair { get; private set; }  //fbedard: ships in fleet with repair capability will not return for repair.
        [StarData] public bool HasOrdnanceSupplyShuttles { get; private set; } // FB: fleets with supply bays will be able to resupply ships
        [StarData] public bool ReadyForWarp { get; private set; }

        [StarData] public bool InFormationMove { get; private set; }

        public override string ToString()
            => $"{Owner.Name} {Name} ships={Ships.Count} pos={FinalPosition} ID={Id} task={FleetTask?.Type}";

        [StarDataConstructor] Fleet() {}

        public Fleet(UniverseState us, Empire owner)
        {
            Id = us.CreateId();
            Owner = owner;
            FleetIconIndex = owner.Random.Int(1, 30);
            SetCommandShip(null);
        }

        // Depends on Ships being finalized
        [StarDataDeserialized(typeof(Ship))]
        void OnDeserialized()
        {
            AssignPositions(FinalDirection);
            UpdateSpeedLimit();
        }

        public void AddShips(IReadOnlyList<Ship> ships)
        {
            for (int i = 0; i < ships.Count; i++)
                if (ships[i].CanBeAddedToFleets())
                    AddShip(ships[i]);
        }

        /// @return TRUE if ship was added to the Fleet,
        /// FALSE if ship cannot be assigned to a fleet (already in fleet)
        public override bool AddShip(Ship newShip)
        {
            if (newShip == null) // Added ship should never be null
            {
                Log.Error($"Ship Was Null for {Name}");
                return false;
            }
            if (newShip.Loyalty != Owner)
                Log.Warning("ship loyalty incorrect");

            // This is finding a logic bug: Ship is already in a fleet or this fleet already contains the ship.
            // This should likely be two different checks. There is also the possibility that the ship is in another
            // Fleet ship list.
            if (newShip.Fleet != null || !base.AddShip(newShip))
            {
                if (newShip.Fleet != this)
                {
                    Log.Warning($"{newShip}: \n already in another fleet:\n{newShip.Fleet}\nthis fleet:\n{this}");
                    return false; // this ship is not added to the fleet
                }
                Log.Warning($"{newShip}: \n Added to fleet it was already part of:\n{newShip.Fleet}");
                return true;
            }

            UpdateOurFleetShip(newShip);

            SortIntoFlanks(newShip, TotalFleetAttributes.GetAveragedValues());
            AddShipToNodes(newShip);
            AssignPositionTo(newShip);
            return true;
        }

        void UpdateOurFleetShip(Ship ship)
        {
            HasRepair = HasRepair || ship.HasRepairBeam || ship.HasRepairModule && ship.Ordinance > 0;

            HasOrdnanceSupplyShuttles = HasOrdnanceSupplyShuttles ||
                                        ship.Carrier.HasSupplyBays && ship.Ordinance >= 100;
        }

        public void AddExistingShip(Ship ship, FleetDataNode node)
        {
            node.Ship = ship;
            base.AddShip(ship);
            ship.Fleet = this;
            ship.AI.FleetNode = node;
            ship.FleetOffset = node.RelativeFleetOffset;
            ship.RelativeFleetOffset = node.RelativeFleetOffset;

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

        bool AddShipToNodes(Ship shipToAdd)
        {
            shipToAdd.Fleet = this;
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

            if (roleType != RoleType.Warship)
            {
                RearShips.AddUniqueRef(ship);
            }
            else if (CommandShip == ship)
            {
                CenterShips.AddUniqueRef(ship);
            }
            else if (ship.DesignRole == RoleName.carrier)
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
            if (flank.IsEmpty)
                return;

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
            UpdateSpeedLimit();

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
                if (!s.OnHighAlert)
                {
                    s.AI.OrderAllStop();
                    s.AI.OrderThrustTowardsPosition(GetFinalPos(s), FinalDirection, false);
                }
            }
        }

        public void OrderAbortMove()
        {
            FinalPosition = AveragePos;
            for (int i = 0; i < Ships.Count; i++)
            {
                Ship s = Ships[i];
                if (s.InCombat)
                    continue;

                s.AI.OrderAllStop();
                s.AI.OrderThrustTowardsPosition(GetFinalPos(s), FinalDirection, false);
            }
        }

        void SetAIDefaultTactics()
        {
            if (Owner.isPlayer) return;

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
            FleetDataNode node = DataNodes.Find(n => n.Ship == ship || n.Ship == null && n.ShipName == ship.Name && n.Goal == null);
            bool nodeFound = node != null;

            if (node == null)
            {
                node = new() { CombatState = ship.AI.CombatState };
                DataNodes.Add(node);
            }

            node.Ship = ship;
            node.ShipName = ship.Name;
            node.OrdersRadius = node.OrdersRadius < 2 ? ship.AI.GetSensorRadius() : node.OrdersRadius;
            ship.AI.FleetNode = node;
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
                    case SquadSortType.Defense: return (int)(ship.ArmorMax + ship.ShieldMax);
                    case SquadSortType.Utility: return ship.DesignRole == RoleName.support || ship.DesignRoleType == RoleType.Troop ? 1 : 0;
                    default: return 0;
                }
            }

            allShips.Sort((a, b) =>
            {
                int aValue = SortValue(a);
                int bValue = SortValue(b);

                int order = bValue - aValue;
                if (order != 0) return order;
                return b.Id.CompareTo(a.Id);
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
            int upOrDown = flank == FlankType.Screen ? -1 : 1;
            float spacer = 1000 * upOrDown;
            Vector2 squadOffset = new(0,spacer);
            int row = 0;
            int columns = flank == FlankType.Screen ? 9 : 5;
            int rowMax = 1 +(squads.Count / columns).LowerBound(columns);
            float tallestSquad = 0;
            Vector2 previousSizeLeft = Vector2.Zero;
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
                    squad.SetOffSets(new(previousSizeLeft.X, squadOffset.Y + height));
                    previousSizeLeft = squad.GetSquadSize();
                    previousSizeRight = -squad.GetSquadSize();
                }
                else if (index % 2 == 1)
                {
                    int dir = squad.Ships.Count >= 3 ? 2 : -1;
                    float width = dir == -1 ? 0 : squad.Ships[dir].GridWidth * 16;
                    squad.SetOffSets(new(previousSizeLeft.X + width, squadOffset.Y));
                    previousSizeLeft = squad.GetSquadSize();
                }
                else
                {
                    int dir = squad.Ships.Count >= 2 ? 1 : -1;
                    float width = dir == -1 ? 0 : squad.Ships[dir].GridWidth * 16;
                    squad.SetOffSets(new(previousSizeRight.X - width, squadOffset.Y));
                    previousSizeRight = -squad.GetSquadSize();
                }

                tallestSquad = Math.Max(tallestSquad, squad.GetSquadSize().Y);
                row++;
                
                if (row > rowMax)
                {
                    row = 0;
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

        void EvaluateTask(MilitaryTask task)
        {
            if (Owner.Universe.Screen.SelectedFleet == this)
                Owner.Universe.DebugWin?.DrawCircle(DebugModes.Tasks, FinalPosition, task.AORadius, Color.AntiqueWhite);

            if (task.RallyPlanet == null)
                task.GetRallyPlanet(AveragePosition(force: true));

            TaskCombatStatus = FleetInAreaInCombat(task.AO, task.AORadius);

            switch (task.Type)
            {
                case MilitaryTask.TaskType.StrikeForce:
                case MilitaryTask.TaskType.ReclaimPlanet:
                case MilitaryTask.TaskType.AssaultPlanet:        DoAssaultPlanet(task);        break;
                case MilitaryTask.TaskType.ClearAreaOfEnemies:   DoClearAreaOfEnemies(task);   break;
                case MilitaryTask.TaskType.Exploration:          DoExplorePlanet(task);        break;
                case MilitaryTask.TaskType.DefendClaim:          DoClaimDefense(task);         break;
                case MilitaryTask.TaskType.DefendPostInvasion:   DoPostInvasionDefense(task);  break;
                case MilitaryTask.TaskType.GlassPlanet:          DoGlassPlanet(task);          break;
                case MilitaryTask.TaskType.AssaultPirateBase:    DoAssaultPirateBase(task);    break;
                case MilitaryTask.TaskType.RemnantEngagement:    DoRemnantEngagement(task);    break;
                case MilitaryTask.TaskType.DefendVsRemnants:     DoDefendVsRemnant(task);      break;
                case MilitaryTask.TaskType.GuardBeforeColonize:  DoPreColonizationGuard(task); break;
                case MilitaryTask.TaskType.StageFleet:           DoStagingFleet(task);         break;
                case MilitaryTask.TaskType.InhibitorInvestigate: DoDeepSpaceInvestigate(task); break;
                case MilitaryTask.TaskType.RemnantPortalDefense: DoRemnantPortalDefense(task); break;
            }
        }

        void DoExplorePlanet(MilitaryTask task)
        {
            Planet targetPlanet = task.TargetPlanet;
            bool eventBuildingFound = targetPlanet.EventsOnTiles();
            task.TargetEmpire ??= Owner.AI.ThreatMatrix.GetStrongestHostileAt(targetPlanet.System);

            if (EndInvalidTask(!eventBuildingFound
                               || targetPlanet.Owner != null && !Owner.IsAtWarWith(targetPlanet.Owner)
                               || !MajorityTroopShipsAreInWell(targetPlanet) && (!StillInvasionEffective(task) || !StillCombatEffective(task))))
            {
                return;
            }

            switch (TaskStep)
            {
                case 0:
                    if (ShouldGatherAtRallyFirst(task))
                    {
                        FleetTaskGatherAtRally(task);
                        TaskStep = 1;
                    }
                    else
                    {
                        AddFleetProjectorGoal();
                        TaskStep = 2;
                    }

                    break;
                case 1:
                    if (!HasArrivedAtRallySafely() || Ships.Any(s => s?.System == task.RallyPlanet.System && s?.InCombat == true))
                        break;

                    AddFleetProjectorGoal();
                    TaskStep = 2;
                    break;
                case 2:
                    if (FleetProjectorGoalIsStillBuildingConstructor(task.TargetPlanet.System))
                        break;

                    GatherAtAO(task, distanceFromAO: targetPlanet.GravityWellRadius);
                    TaskStep = 3;
                    break;
                case 3:
                    if (ArrivedAtCombatRally(FinalPosition))
                    {
                        TaskStep = 4;
                        var combatOffset = task.AO.OffsetTowards(AveragePosition(), targetPlanet.GravityWellRadius);
                        EscortingToPlanet(combatOffset, MoveOrder.Regular);
                    }
                    break;
                case 4:
                    var planetMoveStatus = FleetMoveStatus(targetPlanet.GravityWellRadius, FinalPosition);

                    if (!planetMoveStatus.IsSet(MoveStatus.MajorityAssembled))
                    {
                        if (planetMoveStatus.IsSet(MoveStatus.AssembledInCombat))
                        {
                            ClearPriorityOrderForShipsInAO(Ships, FinalPosition, targetPlanet.GravityWellRadius);
                        }
                        break;
                    }
                    var planetGuard = task.AO.OffsetTowards(AveragePosition(), 500);
                    EngageCombatToPlanet(planetGuard, MoveOrder.Aggressive);
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
                    if (!DoOrbitTaskArea(task) || !AttackEnemyStrengthClumpsInAO(task))
                        EndInvalidTask(--DefenseTurns <= 0 
                            && !Owner.IsSystemUnderThreatForUs(task.TargetPlanet.System));
                    break;
            }
        }

        void RemoveTroopShips()
        {
            for (int i = Ships.Count -1; i >= 0; i--)
            {
                Ship ship = Ships[i];
                if (ship.DesignRole == RoleName.troop)
                {
                    RemoveShip(ship, returnToEmpireAI: true, clearOrders: true);
                }
            }
        }

        public static void CreatePostInvasionFromCurrentTask(Fleet fleet, MilitaryTask task, Empire owner, string name)
        {
            fleet.RemoveTroopShips();
            task.FlagFleetNeededForAnotherTask();
            fleet.TaskStep = 0;
            var postInvasion = MilitaryTask.CreatePostInvasion(task.TargetPlanet, task.Fleet, owner);
            fleet.Name = name;
            fleet.FleetTask = postInvasion;
            owner.AI.QueueForRemoval(task);
            owner.AI.AddPendingTask(postInvasion);
        }

        // Note - the task type of the reclaim fleet is Assault Planet
        public static void CreateReclaimFromCurrentTask(Fleet fleet, MilitaryTask task, Empire owner)
        {
            task.FlagFleetNeededForAnotherTask();
            fleet.TaskStep = 0;
            var reclaim = MilitaryTask.CreateReclaimTask(owner, task.TargetPlanet, task.Fleet);
            fleet.Name = "Reclaim Fleet " + fleet.Key;
            fleet.FleetTask  = reclaim;
            owner.AI.QueueForRemoval(task);
            owner.AI.AddPendingTask(reclaim);
        }

        public static void CreateStrikeFromCurrentTask(Fleet fleet, MilitaryTask task, Empire owner, Goal goal)
        {
            task.FlagFleetNeededForAnotherTask();
            fleet.TaskStep = 2;
            var strikeFleet = new MilitaryTask(task.TargetPlanet, owner, fleet, MilitaryTaskImportance.Normal)
            {
                Goal = goal,
                Type = MilitaryTask.TaskType.StrikeForce,
                NeedEvaluation = false,
            };

            fleet.Name = "Strike Fleet " + fleet.Key;
            fleet.FleetTask = strikeFleet;
            owner.AI.QueueForRemoval(task);
            owner.AI.AddPendingTask(strikeFleet);
        }

        bool ShouldGatherAtRallyFirst(MilitaryTask task)
        {
            Vector2 enemySystemPos = task.TargetSystem?.Position ?? task.TargetPlanet.System.Position;
            Vector2 rallySystemPos = task.RallyPlanet.System.Position;
            Vector2 fleetPos       = AveragePosition(force: true);

            return fleetPos.SqDist(rallySystemPos) < fleetPos.SqDist(enemySystemPos) * 2;
        }

        void DoStagingFleet(MilitaryTask task)
        {
            switch (TaskStep)
            {
                case 0:
                    FleetTaskGatherAtRally(task); // rally is closest planet to fleet average pos now
                    TaskStep = 1;
                    break;
                case 1:
                    if (!HasArrivedAtRallySafely())
                        break;

                    task.GetRallyPlanet(task.TargetPlanet.Position);
                    FleetTaskGatherAtRally(task); // move the fleet to the closest system to target planet
                    TaskStep = 2; 
                    break;
                case 2:
                    if (!HasArrivedAtRallySafely())
                        break;

                    AddFleetProjectorGoal();
                    TaskStep = 3; // Wait for PrepareForWar.cs goal to handle this fleet
                    break;
            }
        }

        void DoAssaultPlanet(MilitaryTask task)
        {
            if (!Owner.IsEmpireAttackable(task.TargetPlanet.Owner))
                TaskStep = 8;
            else
                task.TargetEmpire = task.TargetPlanet.Owner;

            switch (TaskStep)
            {
                case 0:
                    if (AveragePos.InRadius(task.TargetPlanet.System.Position, task.TargetPlanet.System.Radius * 2))
                    {
                        TaskStep = 5;
                        break;
                    }

                    if (ShouldGatherAtRallyFirst(task))
                    {
                        FleetTaskGatherAtRally(task);
                        TaskStep = 1;
                    }
                    else
                    {
                        AddFleetProjectorGoal();
                        TaskStep = 2;
                    }

                    break;
                case 1:
                    if (!HasArrivedAtRallySafely())
                        break;

                    AddFleetProjectorGoal();
                    TaskStep = 2; 
                    break;
                case 2: 
                    if (FleetProjectorGoalIsStillBuildingConstructor(task.TargetPlanet.System))
                        break;

                    SetOrdersRadius(Ships, 5000);
                    GatherAtAO(task, distanceFromAO: task.TargetPlanet.System.Radius * 2);
                    TaskStep = 3;
                    break;
                case 3:
                    if (ShipsUnderAttackInAo(RearShips, task.TargetPlanet.Position,
                        task.TargetPlanet.System.Radius, out Ship shipBeingTargeted))
                    {
                        EngageCombatToPlanet(shipBeingTargeted.Position, MoveOrder.Aggressive);
                        ClearPriorityOrderForShipsInAO(Ships, shipBeingTargeted.Position, shipBeingTargeted.SensorRange);
                        TaskStep = 6;
                        break;
                    }
                    if (!ArrivedAtCombatRally(FinalPosition))
                        break;
                    
                    TaskStep = 4; // Note - Reclaim fleets (from clear area) are set to this step number when created
                    SetOrdersRadius(Ships, 5000f);
                    break;
                case 4:
                    MoveStatus combatRally = FleetMoveStatus(GetRelativeSize().Length() * 2 , FinalPosition);
                    if (!combatRally.IsSet(MoveStatus.MajorityAssembled))
                    {
                        if (combatRally.IsSet(MoveStatus.AssembledInCombat))
                            ClearPriorityOrderForShipsInAO(Ships, FinalPosition, GetRelativeSize().Length() / 2);

                        if (combatRally.IsSet(MoveStatus.Dispersed))
                        {
                            GatherAtAO(task, distanceFromAO: 30000);
                            TaskStep = 3;
                        }

                        break;
                    }

                    TaskStep = 5;
                    break;
                case 5:
                    Vector2 combatOffset  = task.AO.OffsetTowards(AveragePosition(), task.TargetPlanet.GravityWellRadius);
                    MoveStatus inPosition = FleetMoveStatus(task.TargetPlanet.GravityWellRadius, combatOffset);
                    if (!inPosition.IsSet(MoveStatus.MajorityAssembled))
                    {
                        if (inPosition.IsSet(MoveStatus.AssembledInCombat))
                            ClearPriorityOrderForShipsInAO(Ships, combatOffset, GetRelativeSize().Length());
                    }

                    Vector2 resetPos = task.AO.OffsetTowards(AveragePosition(), 1500);
                    EngageCombatToPlanet(resetPos, MoveOrder.Aggressive);
                    TaskStep = 6;
                    break;
                case 6:
                    RearShipsToCombat(MoveOrder.Aggressive);
                    switch (StatusOfPlanetAssault(task))
                    {
                        case Status.NotApplicable: TaskStep = 5; break;
                        case Status.Good:          TaskStep = 7; break;
                        case Status.Critical:      TaskStep = 8; break;
                    }

                    break;
                case 7:
                    TaskStep = ShipsOffMission(task) ? 5 : 6;
                    break;
                case 8:
                    if (TryGetNewTargetPlanet(task, out Planet newTarget)
                        && task.GetMoreTroops(newTarget, out Array<Ship> troopShips))
                    {
                        AddShips(troopShips);
                        AutoArrange();
                        FinalPosition = task.TargetPlanet.Position;
                        task.AO       = task.TargetPlanet.Position;
                        bool inSystem = AveragePos.InRadius(newTarget.System.Position, newTarget.System.Radius);
                        if (inSystem)
                        {
                            GatherAtAO(task, distanceFromAO: 30000);
                            if (CanInvadeNow(newTarget, task))
                            {
                                TaskStep = 6;
                                EscortingToPlanet(newTarget.Position, MoveOrder.Aggressive);
                            }
                            else
                            {
                                TaskStep = 3;
                            }
                        }
                        else
                        {
                            AddFleetProjectorGoal();
                            TaskStep = 2;
                        }

                        task.SetTargetPlanet(newTarget);
                        task.AO = newTarget.Position;
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
                && CommandShip?.System == task.TargetPlanet.System
                && Owner.Universe.Remnants.AnyActiveFleetsTargetingSystem(task.TargetPlanet.System);

            EndInvalidTask(remnantsTargeting 
                           || !MajorityTroopShipsAreInWell(task.TargetPlanet) && (!invasionEffective || !combatEffective));
        }

        void DoDeepSpaceInvestigate(MilitaryTask task)
        {
            switch (TaskStep)
            {
                case 0:
                    FleetTaskGatherAtRally(task);
                    TaskStep = 1;
                    break;
                case 1:
                    if (HasArrivedAtRallySafely())
                    {
                        GatherAtAO(task, distanceFromAO: Owner.GetProjectorRadius());
                        TaskStep= 2;
                    }
                    break;
                case 2:
                    if (ArrivedAtCombatRally(FinalPosition))
                    {
                        FinalPosition = task.AO;
                        FleetMoveToPosition(FinalPosition, 0, MoveOrder.Aggressive);
                        TaskStep= 3;
                        TaskStep = 3;
                    }
                    break;
                case 3:
                    if (ArrivedAtCombatRally(FinalPosition))
                    {
                        CancelFleetMoveInArea(task.AO, task.AORadius * 2);
                        AttackEnemyStrengthClumpsInAO(task);
                        TaskStep = 4;
                    }
                    break;
                case 4:
                    float enemyStr = Owner.Threats.GetHostileStrengthAt(task.AO, 30_000);
                    task.TargetEmpire = Owner.Threats.GetStrongestHostileAt(task.AO, 30_000);
                    if (EndInvalidTask(!CanTakeThisFight(enemyStr, task)))
                        return;

                    if (enemyStr == 0)
                        task.EndTask();

                    break;
            }
        }

        bool ShipsUnderAttackInAo(Array<Ship> ships, Vector2 ao, float radius, out Ship shipBeingTargeted)
        {
            shipBeingTargeted = null;
            for (int i = 0; i < ships.Count; i++)
            {
                Ship ship = ships[i];
                if (ship.Position.InRadius(ao, radius) && ship.IsBeingTargeted(out _))
                {
                    shipBeingTargeted = ship;
                    return true;
                }
            }

            return false;
        }

        bool CanInvadeNow(Planet p, MilitaryTask task)
        {
            if (!StillCombatEffective(task) || !TryGetTroopShipsInArea(p.Position, p.System.Radius, out Ship[] troopShips))
                return false;

            float troopStr  = troopShips.Sum(s => s.GetOurTroopStrength(s.TroopCount));
            float groundStr = p.GetGroundStrength(task.TargetEmpire) * Owner.DifficultyModifiers.EnemyTroopStrength;
            return troopStr > groundStr;
        }

        bool TryGetTroopShipsInArea(Vector2 center, float radius, out Ship[] troopShips)
        {
            troopShips = Ships.Filter(s => s.Position.InRadius(center, radius) && s.IsTroopShip);
            return troopShips.Length > 0;
        }

        bool TryGetNewTargetPlanet(MilitaryTask task, out Planet newTarget)
        {
            Planet currentTarget = task.TargetPlanet;
            newTarget            = null;

            if (currentTarget.Owner != null && Owner.IsAtWarWith(currentTarget.Owner))
            {
                newTarget = currentTarget; // Invasion or bombing was not effective, retry
                return true; 
            }

            var currentSystem    = currentTarget.System;
            newTarget            = currentSystem.PlanetList.Find(p => Owner.IsAtWarWith(p.Owner));

            if (newTarget != null)
                return true;

            if (task.Type == MilitaryTask.TaskType.ReclaimPlanet)
                return false; // No targets found in system for reclaim fleets

            newTarget =  task.Type == MilitaryTask.TaskType.StrikeForce 
                ? TryGetNewTargetPlanetStrike(currentSystem, task.TargetEmpire) 
                : TryGetNewTargetPlanetInvasion(currentSystem);

            return newTarget != null;
        }

        Planet TryGetNewTargetPlanetInvasion(SolarSystem system)
        {
            var potentialSystems = system.FiveClosestSystems.Filter(s => s.PlanetList.Any(p => p.Owner?.IsAtWarWith(Owner) == true));
            if (potentialSystems.Length == 0)
                return null;

            SolarSystem potentialSystem = potentialSystems.FindMin(s => Owner.KnownEnemyStrengthIn(s));
            return potentialSystem.PlanetList.Find(p => Owner.IsAtWarWith(p.Owner));
        }

        Planet TryGetNewTargetPlanetStrike(SolarSystem system, Empire enemy)
        {
            var planets = enemy?.GetPlanets() ?? new Array<Planet>();
            return planets.Count == 0 ? null : planets.FindMin(p => p.Position.Distance(system.Position));
        }

        void DoClaimDefense(MilitaryTask task)
        {
            if (EndInvalidTask(task.TargetPlanet.Owner != null 
                               && !task.TargetPlanet.Owner.IsFaction 
                               && !task.TargetPlanet.Owner.data.IsRebelFaction
                               || !CanTakeThisFight(task.EnemyStrength, task)))
            {
                return;
            }

            task.AO = task.TargetPlanet.Position;
            switch (TaskStep)
            {
                case 0:
                    if (ShouldGatherAtRallyFirst(task))
                    {
                        FleetTaskGatherAtRally(task);
                        TaskStep = 1;
                    }
                    else
                    {
                        AddFleetProjectorGoal();
                        TaskStep = 2;
                    }

                    break;
                case 1:
                    if (!HasArrivedAtRallySafely()
                        || Ships.Any(s => s?.System == task.RallyPlanet.System && s?.InCombat == true))
                    {
                        break;
                    }

                    AddFleetProjectorGoal();
                    TaskStep = 2;
                    break;
                case 2:
                    if (FleetProjectorGoalIsStillBuildingConstructor(task.TargetPlanet.System))
                        break;

                    GatherAtAO(task, FleetTask.TargetPlanet.System.Radius);
                    TaskStep = 3;
                    break;
                case 3:
                    if (!ArrivedAtCombatRally(FinalPosition))
                        break;

                    TaskStep = 4;
                    CancelFleetMoveInArea(task.AO, task.AORadius * 2);
                    break;
                case 4:
                    CombatMoveToAO(task, FleetTask.TargetPlanet.GravityWellRadius * 1.5f);
                    TaskStep = 5;
                    break;
                case 5:
                    if (!ArrivedAtCombatRally(FinalPosition))
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
                    if (FleetInAreaInCombat(AveragePos, 50000) == CombatStatus.InCombat)
                        break;

                    GatherAtAO(task, target.System.Radius*2);
                    if (TryCalcEtaToPlanet(task, target.Owner, out float eta))
                        Owner.Remnants.InitTargetEmpireDefenseActions(target, eta, GetStrength());

                    TaskStep = 2;
                    break;
                case 2:
                    if (!ArrivedAtCombatRally(FinalPosition))
                        break;

                    TaskStep = 3;
                    CancelFleetMoveInArea(task.AO, task.AORadius * 2);
                    break;
                case 3:
                    FleetMoveToPosition(task.AO, target.GravityWellRadius * 1.5f, MoveOrder.Regular);
                    TaskStep = 4;
                    break;
                case 4:
                    if (!ArrivedAtCombatRally(FinalPosition))
                    {
                        StartBombing(FleetTask.TargetPlanet);
                        break;
                    }

                    TaskStep = 5;
                    CancelFleetMoveInArea(task.AO, task.AORadius * 2);
                    break;
                case 5:
                    if (FleetInAreaInCombat(task.AO, task.AORadius) == CombatStatus.InCombat)
                    {
                        StartBombing(FleetTask.TargetPlanet);
                        AttackEnemyStrengthClumpsInAO(task);
                        if (target.Owner == null)
                            TaskStep = 7;
                        break;
                    }

                    OrderFleetOrbit(target, clearOrders:true);
                    TaskStep = 6;
                    break;
                case 6:
                    if (StartBombing(FleetTask.TargetPlanet))
                        break;

                    TaskStep = 7;
                    break;
                case 7:
                    OrderFleetOrbit(target, clearOrders:true);
                    break; // Change in task step is done from Remnant goals
                case 8: // Go back to portal, this step is set from the Remnant goal
                    ClearOrders();
                    task.SetTargetPlanet(null);
                    AutoArrange();
                    GatherAtAO(task, 500);
                    TaskStep = 9;  // Tasks steps below 9 are a signal that the remnant fleet still on target (GetRemnantEngagementsGoalsFor)
                    break;
                case 9:
                    if (!ArrivedAtCombatRally(FinalPosition, radiusMultiplier: 10))
                        break;

                    TaskStep = 10; // Goal will wait for fleet to be in this task to disband it.
                    break;
            }
        }

        void DoRemnantPortalDefense(MilitaryTask task)
        {
            switch (TaskStep)
            {
                case 1:
                    if (task.TargetShip == null)
                        CombatMoveToAO(task, 10_000);
                    else if (FleetInAreaInCombat(AveragePos, 100_000) != CombatStatus.InCombat)
                        FleetMoveToPosition(task.TargetShip.Position, 20_000, MoveOrder.Aggressive);

                    TaskStep = 2;
                    break;
                case 2:
                    AttackEnemyStrengthClumpsInAO(task);
                    break;
                case 3:
                    CombatMoveToAO(task, 10_000);
                    TaskStep = 4;
                    break;
                case 4:
                    if (!ArrivedAtCombatRally(FinalPosition))
                        break;
                    TaskStep = 5;
                    break;
                case 5:
                    Owner.Remnants.DisbandDefenseFleet(this);
                    FleetTask.EndTask();
                    break;

            }
        }

        bool TryCalcEtaToPlanet(MilitaryTask task, Empire targetEmpire, out float starDateEta)
        {
            starDateEta = 0;
            if (task.TargetPlanet == null)
                return false;

            if (AveragePosition().InRadius(task.TargetPlanet.System.Position, task.TargetPlanet.System.Radius))
            {
                if (targetEmpire?.isPlayer == true)
                    return false; // The Fleet is already there

                starDateEta = Owner.Universe.StarDate;
                return true; // AI might retaliate even if its the same system
            }

            float distanceToPlanet = AveragePosition().Distance(task.TargetPlanet.Position);
            float slowestWarpSpeed = Ships.Min(s => s.MaxFTLSpeed).LowerBound(1000);
            float secondsToTarget = distanceToPlanet / slowestWarpSpeed;
            float turnsToTarget = secondsToTarget / Owner.Universe.P.TurnTimer;
            starDateEta = (Owner.Universe.StarDate + turnsToTarget / 10).RoundToFractionOf10();

            return starDateEta.Greater(0);
        }

        void DoPreColonizationGuard(MilitaryTask task)
        {
            if (EndInvalidTask(task.TargetPlanet.Owner != null
                || Owner.KnownEnemyStrengthIn(task.TargetPlanet.System)
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
                    if (!ArrivedAtCombatRally(task.AO, radiusMultiplier: 10))
                        break;

                    TaskStep = 3;
                    break;
            }

            OrderFleetOrbit(task.TargetPlanet, clearOrders:true);
        }

        void DoDefendVsRemnant(MilitaryTask task)
        {
            // Should we abort the fight against remnants taskforce?
            if (EndInvalidTask(!CanTakeThisFight(task.EnemyStrength, task)))
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
                    if (!ArrivedAtCombatRally(task.AO, radiusMultiplier: 0.5f))
                        break;

                    TaskStep = 2; // Defend till death (or until the DefenseVsRemnant goal redirects or cancels us)!
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

            switch (TaskStep)
            {
                case 0:
                    FleetTaskGatherAtRally(task);
                    TaskStep = 1;
                    break;
                case 1:
                    if (!HasArrivedAtRallySafely(checkRadiusMultiplier: 5)
                        || Ships.Any(s => s?.System == task.RallyPlanet.System && s?.InCombat == true))
                    {
                        break;
                    }

                    FinalPosition = task.AO.OffsetTowards(task.AO, 200000);
                    FleetMoveToPosition(FinalPosition, 0, MoveOrder.Regular);
                    TaskStep = 2;
                    break;
                case 2:
                    if (!ArrivedAtCombatRally(FinalPosition))
                        break;

                    FinalPosition = task.AO;
                    FleetMoveToPosition(FinalPosition, 0, MoveOrder.Aggressive);
                    TaskStep = 3;
                    break;
                case 3:
                    if (!ArrivedAtCombatRally(FinalPosition))
                        break;

                    CancelFleetMoveInArea(task.AO, task.AORadius * 2);
                    TaskStep = 4;
                    break;
                case 4:
                    if (!AttackEnemyStrengthClumpsInAO(task))
                        TaskStep = 5;

                    break;
                case 5:
                    ClearOrders();
                    FleetTask?.EndTask();
                    break;
            }
        }

        void DoGlassPlanet(MilitaryTask task)
        {
            if (task.TargetPlanet.Owner == null)
            {
                task.IncreaseColonyLostValueByBombing();
                TaskStep = 6;
            }
            else if (!Owner.IsEmpireAttackable(task.TargetPlanet.Owner))
            {
                TaskStep = 6;
            }
            else
            {
                task.TargetEmpire = task.TargetPlanet.Owner;
            }

            task.AO = task.TargetPlanet.Position;
            switch (TaskStep)
            {
                case 0:
                    if (ShouldGatherAtRallyFirst(task))
                    {
                        FleetTaskGatherAtRally(task);
                        TaskStep = 1;
                    }
                    else
                    {
                        AddFleetProjectorGoal();
                        TaskStep = 2;
                    }

                    break;
                case 1:
                    MoveStatus moveStatus = FleetMoveStatus(task.RallyPlanet.System.Radius);
                    if (moveStatus.IsSet(MoveStatus.MajorityAssembled) && !task.RallyPlanet.System.HostileForcesPresent(Owner))
                    {
                        AddFleetProjectorGoal();
                        TaskStep = 2;
                    }

                    break;
                case 2:
                    if (FleetProjectorGoalIsStillBuildingConstructor(task.TargetPlanet.System))
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
                    EngageCombatToPlanet(task.TargetPlanet.Position, MoveOrder.Aggressive);
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
                        FinalPosition = task.TargetPlanet.Position;
                        task.AO       = task.TargetPlanet.Position;
                        bool inSystem = AveragePos.InRadius(newTarget.System.Position, newTarget.System.Radius);
                        if (inSystem)
                        {
                            TaskStep = 3;
                            GatherAtAO(task, distanceFromAO: 30000);
                        }
                        else
                        {
                            AddFleetProjectorGoal();
                            TaskStep = 2;
                        }

                        task.SetTargetPlanet(newTarget);
                        task.AO = newTarget.Position;
                    }
                    else
                    {
                        CreatePostInvasionFromCurrentTask(this, task, Owner, "Post Invasion Defense");
                    }

                    return;
            }

            bool remnantsTargeting = !Owner.WeAreRemnants
                && CommandShip?.System == task.TargetPlanet.System
                && Owner.Universe.Remnants.AnyActiveFleetsTargetingSystem(task.TargetPlanet.System);

            if (EndInvalidTask(task.TargetPlanet.Owner == null || remnantsTargeting || !StillCombatEffective(task)))
                return;

            bool bombOk = Ships.Select(s => s.Bomb60SecStatus()).Any(bt => bt != Status.NotApplicable && bt != Status.Critical);
            if (!bombOk)
                EndInvalidTask(true);
        }

        void DoClearAreaOfEnemies(MilitaryTask task)
        {
            if (task.TargetEmpire == null && FleetTask.TargetSystem != null)
                task.TargetEmpire = Owner.AI.ThreatMatrix.GetStrongestHostileAt(FleetTask.TargetSystem);

            float enemyStrength = Owner.AI.ThreatMatrix.GetHostileStrengthAt(task.AO, task.AORadius);
            bool threatIncoming = Owner.IsSystemUnderThreatForUs(FleetTask.TargetSystem)
                || Owner.IsSystemUnderThreatForAllies(FleetTask.TargetSystem);

            if (threatIncoming && EndInvalidTask(!CanTakeThisFight(enemyStrength*0.5f, task))) 
                return; // If no threat is incoming, stay put to clear remaining lone ships

            float aoRadius = task.TargetSystem?.Radius * 2 ?? task.RallyPlanet.System.Radius * 2;

            switch (TaskStep)
            {
                // Find a rally planet closest to fleel average pos and move there first
                case 0:
                    if (AveragePos.InRadius(task.TargetSystem?.Position ?? task.AO, Owner.GetProjectorRadius()))
                    {
                        GatherAtAO(task, distanceFromAO: 30000);
                        TaskStep = 2;
                    }
                    else if (ShouldGatherAtRallyFirst(task))
                    {
                        FleetTaskGatherAtRally(task);
                        TaskStep = 1;
                    }
                    else
                    {
                        GatherAtAO(task, distanceFromAO: aoRadius);
                        TaskStep = 2;
                    }
                    break;
                case 1:
                    MoveStatus moveStatus = FleetMoveStatus(task.RallyPlanet.System.Radius);
                    if (moveStatus.IsSet(MoveStatus.MajorityAssembled) && !task.RallyPlanet.System.HostileForcesPresent(Owner))
                    {
                        GatherAtAO(task, distanceFromAO: aoRadius);
                        TaskStep = 2;
                    }
                    break;
                case 2:
                    if (!ArrivedAtCombatRally(FinalPosition))
                    {
                        ClearPriorityOrderForShipsInAO(Ships, task.AO, aoRadius);
                        break;
                    }

                    TaskStep = 3;
                    break;
                case 3:
                    ClearPriorityOrderForShipsInAO(Ships, task.AO, aoRadius);
                    if (AttackEnemyStrengthClumpsInAO(task, Ships))
                        break;

                    TaskStep = 4;
                    break;
                case 4:
                    if (task.TargetPlanet == null)
                        task.SetTargetPlanet(task.TargetSystem.PlanetList.FindMax(p => p.ColonyBaseValue(Owner) + p.ColonyPotentialValue(Owner)));

                    if (task.TargetPlanet != null)
                        DoOrbitTaskArea(task);
                    else
                        DoCombatMoveToTaskArea(task, true);

                    if (threatIncoming)
                    {
                        if (enemyStrength < 1)
                            TaskStep = 6; // search and destroy the threat, which is parked somewhere, doing nothing
                    }
                    else if (enemyStrength < 1) // No threats and no enemies
                    {
                        TaskStep = 5;
                    }
                    else
                    {
                        TaskStep = 3; // Attack in system again
                    }
                    break;
                case 5:
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
                            task.AO = task.TargetPlanet.Position;
                            AddShips(troopShips);
                            AutoArrange();
                            CreateReclaimFromCurrentTask(this, task, Owner);
                            AddFleetProjectorGoal();
                            GatherAtAO(task, distanceFromAO: 20000);
                            TaskStep = 4; // This sets the step for the reclaim fleet (assault planet).
                        }
                        else
                        {
                            EndInvalidTask(true);
                        }
                    }

                    break;
                case 6:
                    var threat = Owner.SystemsWithThreat.Find(t => !t.ThreatTimedOut && t.TargetSystem == FleetTask.TargetSystem);
                    if (threat?.NearestFleet == null)
                    {
                        TaskStep = 5;
                        break;
                    }

                    Vector2 enemyFleetPos = threat.NearestFleet.FinalPosition;
                    task.AO = enemyFleetPos;
                    GatherAtAO(task, distanceFromAO: 20000);
                    TaskStep = 2;
                    break;
            }
        }

        bool EndInvalidTask(bool condition)
        {
            if (!condition) 
                return false;

            FleetTask?.EndTask();
            FleetTask = null;
            TaskStep  = 0;
            return true;
        }

        void OrderFleetOrbit(Planet planet, bool clearOrders)
        {
            for (int i = 0; i < Ships.Count; i++)
            {
                Ship ship = Ships[i];
                ship.OrderToOrbit(planet, clearOrders);
                ship.AI.SetPriorityOrder(false);
            }
        }

        void AddFleetProjectorGoal()
        {
            Planet targetPlanet = FleetTask?.TargetPlanet;
            if (targetPlanet != null && !targetPlanet.System.HasPlanetsOwnedBy(Owner))
                Owner.AI.AddGoalAndEvaluate(new DeployFleetProjector(this, FleetTask.TargetPlanet, Owner));
        }

        bool FleetProjectorGoalIsStillBuildingConstructor(SolarSystem targetSystem)
        {
            if (targetSystem.IsExclusivelyOwnedBy(Owner))
                return false; // no need for projector goal

            var deployGoal = Owner.AI.FindGoal(g => g.Type == GoalType.DeployFleetProjector
                                                    && g is FleetGoal fg && fg.Fleet == this);

            return deployGoal != null && deployGoal.FinishedShip == null;
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
                if (ship.CanTakeFleetOrders && !ship.Position.OutsideRadius(pos, radius) &&
                    ship.AI.State == AIState.FormationMoveTo)
                {
                    ship.AI.ChangeAIState(AIState.AwaitingOrders);
                    ship.AI.ClearPriorityOrderAndTarget();
                }
            }
        }

        public void ClearOrders()
        {
            for (int i = 0; i < Ships.Count; i++)
            {
                Ship ship = Ships[i];
                ship.AI.CombatState = ship.ShipData.DefaultCombatState;
                ship.AI.ClearOrders();
            }
        }

        void FleetTaskGatherAtRally(MilitaryTask task)
        {
            Planet planet       = task.RallyPlanet;
            Vector2 movePoint   = planet.Position;
            Vector2 finalFacing = movePoint.DirectionToTarget(task.AO);
            MoveTo(movePoint, finalFacing);
        }

        bool HasArrivedAtRallySafely(float checkRadiusMultiplier = 2)
        {
            float fleetRadius = GetRelativeSize().Length() * checkRadiusMultiplier;
            MoveStatus status = FleetMoveStatus(fleetRadius);

            // if the command ship is stuck, unstuck it. Since the fleet average position is the command ship's position
            if (CommandShip != null 
                && !CommandShip.Position.InRadius(FinalPosition, fleetRadius) 
                && CommandShip.AI.State is AIState.AwaitingOrders or AIState.HoldPosition)
            {
                MoveOrder order = CommandShip.IsInhibitedByUnfriendlyGravityWell ? MoveOrder.Aggressive : MoveOrder.Regular;
                CommandShip.AI.OrderMoveTo(FinalPosition, FinalDirection, order);
            }

            if (status.IsSet(MoveStatus.MajorityAssembled))
                return true;

            if (!status.IsSet(MoveStatus.Assembled))
                return false;

            if (EndInvalidTask(status.IsSet(MoveStatus.AssembledInCombat)))
                return false;

            return !status.IsSet(MoveStatus.Dispersed);
        }

        // Note - distance from AO is important to IncomingThreatDetector 
        // Fleet final position is checked against the system radius * 2~
        // Beyond that, the threat wont be registered.
        void GatherAtAO(MilitaryTask task, float distanceFromAO)
        {
            FleetMoveToPosition(task.AO, distanceFromAO, MoveOrder.Regular);
        }

        void CombatMoveToAO(MilitaryTask task, float distanceFromAO)
        {
            FleetMoveToPosition(task.AO, distanceFromAO, MoveOrder.Aggressive);
        }

        void FleetMoveToPosition(Vector2 position, float offsetToAO, MoveOrder order)
        {
            Vector2 averagePos = AveragePosition();
            Vector2 finalPos = position.OffsetTowards(averagePos, offsetToAO);
            Vector2 finalDir = averagePos.DirectionToTarget(position);
            MoveTo(finalPos, finalDir, order);
        }

        bool ArrivedAtCombatRally(Vector2 position, float radiusMultiplier = 2f)
        {
            float radius = GetRelativeSize().Length() * radiusMultiplier;
            radius = Math.Max(1000, radius);
            MoveStatus status = FleetMoveStatus(radius, position);

            if (status.IsSet(MoveStatus.AssembledInCombat))
                ClearPriorityOrderForShipsInAO(Ships, position, radius);
            
            if (status.IsSet(MoveStatus.Assembled))
            {
                float fleetRadius = radius * 0.5f;
                float size = 1;
                if (radius < fleetRadius)
                    size = radius / fleetRadius;
                
                for (int i = 0; i < Ships.Count; i++)
                {
                    var ship = Ships[i];
                    if (ship.IsSpoolingOrInWarp || ship.InCombat || ship.AI.State != AIState.AwaitingOrders)
                        continue;
                    if (ship.InRadius(position, radius)) continue;
                    Vector2 movePos = position + ship.AI.FleetNode.RelativeFleetOffset / size;
                    ship.AI.OrderMoveTo(movePos, position.DirectionToTarget(FleetTask.AO), AIState.AwaitingOrders, MoveOrder.Aggressive);
                }
            }

            return status.IsSet(MoveStatus.MajorityAssembled);
        }

        Ship[] AvailableShips => AllButRearShips.Filter(ship => !ship.AI.HasPriorityOrder);

        bool AttackEnemyStrengthClumpsInAO(MilitaryTask task)
        {
            return AttackEnemyStrengthClumpsInAO(task, AvailableShips);
        }

        bool AttackEnemyStrengthClumpsInAO(MilitaryTask task, IReadOnlyList<Ship> ships)
        {
            if (ships.Count == 0)
                return false;

            ThreatCluster[] clusters = Owner.AI.ThreatMatrix.FindHostileClustersByDist(task.AO, task.AORadius);
            if (clusters.Length == 0)
                return false;

            var availableShips = new Array<Ship>(ships);
            while (availableShips.Count > 0)
            {
                foreach (ThreatCluster c in clusters)
                {
                    if (availableShips.Count == 0)
                        break;

                    float attackStr = 0.0f;
                    for (int i = availableShips.Count - 1; i >= 0; --i)
                    {
                        Ship ship = availableShips[i];
                        if (ship.AI.HasPriorityOrder
                            || ship.InCombat
                            || ship.AI.State == AIState.AssaultPlanet
                            || ship.AI.State == AIState.Bombard)
                        {
                        }
                        else
                        {
                            ship.AI.OrderAttackMoveTo(c.Position);
                            attackStr += ship.GetStrength();
                        }

                        availableShips.RemoveAtSwapLast(i);
                    }
                }
            }

            foreach (Ship needEscort in RearShips)
            {
                if (availableShips.TryPopLast(out Ship ship))
                    ship.DoEscort(needEscort);
                else
                    break; // no more ships for escort duties
            }

            // other ships should move 
            foreach (Ship ship in availableShips)
                ship.AI.OrderMoveTo(task.AO, FinalPosition.DirectionToTarget(task.AO));

            return true;
        }

        bool MoveFleetToNearestCluster(MilitaryTask task)
        {
            ThreatCluster[] clusters = Owner.AI.ThreatMatrix.FindHostileClusters(task.AO, task.AORadius);
            if (clusters.Length != 0)
            {
                float totalStrength = clusters.Sum(c => c.Strength);

                // if we are stronger than them?
                // TODO: For AI, use difficulty-based ratios, on EASY, AI should engage even if they are going to lose
                //                                            on HARD, the AI should be more careful and engage with superiority
                if (totalStrength < GetStrength())
                {
                    ThreatCluster strongest = clusters.FindMax(c => c.Strength);
                    FleetMoveToPosition(strongest.Position, 7500, MoveOrder.Aggressive);
                    return true;
                }
            }
            return false;
        }

        bool ShipsOffMission(MilitaryTask task)
        {
            return AllButRearShips.Any(ship => ship.CanTakeFleetOrders &&
                                               !ship.AI.HasPriorityOrder &&
                                               !ship.InCombat &&
                                               ship.Position.OutsideRadius(task.AO, task.AORadius * 1.5f));
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
            return Ships.Any(ship => ship.Position.InRadius(task.TargetPlanet.Position, invasionSafeZone));
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
            bool bombing = StartBombing(task.TargetPlanet);
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
            => Owner.Universe?.DebugWin?.DebugLogText($"{task.Type}: ({Owner.Name}) Planet: {task.TargetPlanet?.Name ?? "None"} {text}", DebugModes.Normal);

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
            float enemyStrength = Owner.AI.ThreatMatrix.GetHostileStrengthAt(task.AO, task.AORadius);
            if (CanTakeThisFight(enemyStrength, task))
                return true;

            DebugInfo(task, $"Enemy Strength too high. Them: {enemyStrength} Us: {GetStrength()}");
            return false;
        }


        // Most of out troop ships are committed (they are in te planet's gravity well)
        bool MajorityTroopShipsAreInWell(Planet p)
        {
            int numTroopShips    = 0;
            int troopShipsInWell = 0;
            for (int i = 0; i < Ships.Count; i++)
            {
                Ship ship = Ships[i];
                if (ship.IsTroopShip && ship.System == p.System)
                {
                    numTroopShips += 1;
                    // Checking in radius to make sure the well belongs to the correct planet
                    // Checking inhibition as well since some tech can affect grav well, so in radius is not enough.
                    if (ship.InRadius(p.Position, p.GravityWellRadius) && ship.IsInhibitedByUnfriendlyGravityWell)
                        troopShipsInWell += 1;
                }
            }

            return troopShipsInWell / (float)numTroopShips.LowerBound(1) > 0.75f;
        }

        bool StillInvasionEffective(MilitaryTask task)
        {
            bool troopsOnPlanet = task.TargetPlanet.AnyOfOurTroops(Owner);
            bool invasionTroops = Ships.Any(troops => troops.DesignRole == RoleName.troop || troops.Carrier.AnyAssaultOpsAvailable) && GetStrength() > 0;
            bool stillMissionEffective = troopsOnPlanet || invasionTroops;
            if (!stillMissionEffective)
                DebugInfo(task, " No Troops on Planet and No Ships.");
            return stillMissionEffective;
        }

        void InvadeTactics(IEnumerable<Ship> flankShips, InvasionTactics type, Vector2 moveTo, MoveOrder order)
        {
            foreach (Ship ship in flankShips)
            {
                ShipAI ai = ship.AI;
                ai.CombatState = ship.ShipData.DefaultCombatState;
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
                    case InvasionTactics.Rear when !ai.HasPriorityOrder:
                    case InvasionTactics.Screen:
                    case InvasionTactics.MainBattleGroup:
                    case InvasionTactics.FlankGuard:
                        TacticalMove(ship, moveTo, fleetSizeRatio, order);
                        break;
                    case InvasionTactics.Wait:
                        ai.OrderHoldPosition();
                        break;
                }
            }
        }

        void TacticalMove(Ship ship, Vector2 moveTo, float fleetSizeRatio, MoveOrder order)
        {
            Vector2 offset = ship.FleetOffset / fleetSizeRatio;
            Vector2 fleetMoveTo = moveTo + offset;
            FinalDirection = fleetMoveTo.DirectionToTarget(FleetTask.AO);

            ship.AI.OrderMoveTo(fleetMoveTo, FinalDirection, ship.AI.State, order);
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

        void EscortingToPlanet(Vector2 position, MoveOrder order)
        {
            FinalPosition = position;
            FinalDirection = FinalPosition.DirectionToTarget(FinalPosition);

            InvadeTactics(ScreenShips, InvasionTactics.Screen, FinalPosition, order);
            InvadeTactics(CenterShips, InvasionTactics.MainBattleGroup, FinalPosition, order);

            InvadeTactics(RearShips, InvasionTactics.Wait, FinalPosition, order);
            
            InvadeTactics(RightShips, InvasionTactics.FlankGuard, FinalPosition, order);
            InvadeTactics(LeftShips, InvasionTactics.FlankGuard, FinalPosition, order);
        }

        void EngageCombatToPlanet(Vector2 position, MoveOrder order)
        {
            FinalPosition = position;
            FinalDirection = FinalPosition.DirectionToTarget(FinalPosition);

            InvadeTactics(ScreenShips, InvasionTactics.Screen, FinalPosition, order);
            InvadeTactics(CenterShips, InvasionTactics.MainBattleGroup, FinalPosition, order);
            InvadeTactics(RightShips, InvasionTactics.FlankGuard, FinalPosition, order);
            InvadeTactics(LeftShips, InvasionTactics.FlankGuard, FinalPosition, order);
        }

        void RearShipsToCombat(MoveOrder order)
        {
            var notBombersOrTroops = new Array<Ship>();
            foreach(var ship in RearShips)
            {
                if (!ship.IsSingleTroopShip && !ship.IsBomber)
                    notBombersOrTroops.Add(ship);
            }

            InvadeTactics(notBombersOrTroops, InvasionTactics.Screen, FinalPosition, order);
        }
        
        /// <summary>
        /// @return TRUE if any ships are bombing planet
        /// Bombing is done if possible.
        /// </summary>
        bool StartBombing(Planet planet)
        {
            if (planet.Owner == null)
                return false; // colony was destroyed

            bool anyShipsBombing = false;
            Ship[] ships = Ships.Filter(ship => ship.HasBombs 
                           && ship.Supply.ShipStatusWithPendingRearm() >= Status.Critical);
            
            for (int x = 0; x < ships.Length; x++)
            {
                Ship ship = ships[x];
                if (ship.HasBombs && !ship.AI.HasPriorityOrder && ship.AI.State != AIState.Bombard)
                {
                    ship.AI.OrderBombardPlanet(planet, clearOrders:true);
                    ship.AI.SetPriorityOrder(true);
                }
                anyShipsBombing |= ship.AI.State == AIState.Bombard;
            }

            return anyShipsBombing;
        }

        /// <summary>
        /// Sends any capable ships to invade task planet. Returns true if succesful. 
        /// <para></para>
        /// Invasion start success depends on the number of landing spots on the planet and the strength comparison
        /// between invasion forces and planet defenders. 
        /// </summary>
        bool OrderShipsToInvade(IEnumerable<Ship> ships, MilitaryTask task, bool targetBeingBombed)
        {
            int shipsInvading = 0;
            float planetAssaultStrength = 0f;
            float theirGroundStrength = GetGroundStrOfPlanet(task.TargetPlanet);
            float ourGroundStrength = FleetTask.TargetPlanet.GetGroundStrength(Owner);
            var invasionShips = ships.ToArr();

            // collect current invasion stats from all ships in fleet. 
            for (int i = 0; i < Ships.Count; i++)
            {
                Ship ship = Ships[i];
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
                if (ship.IsTroopShip && ship.AI.State != AIState.AssaultPlanet && ship.Carrier.PlanetAssaultStrength > 0)
                {
                    ship.AI.OrderLandAllTroops(task.TargetPlanet, clearOrders:true);
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

        public void UpdateAI(FixedSimTime timeStep)
        {
            MilitaryTask task = FleetTask;
            if (task != null)
            {
                if (Ships.IsEmpty)
                {
                    task.EndTask(); // this will set `FleetTask` to null
                }
                else
                {
                    EvaluateTask(task);
                }
            }
            else // no fleet task
            {
                if (Owner.isPlayer || IsCoreFleet)
                {
                    if (!IsCoreFleet) return;
                    foreach (Ship ship in Ships)
                    {
                        ship.AI.CancelIntercept();
                    }
                    return;
                }
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

        /// <summary>
        /// Removes the ship from this Fleet, performing error checks.
        /// If the ship does not belong to this fleet, its orders are not cleared nor returned to empire AI
        /// </summary>
        /// <param name="returnToEmpireAI">
        /// Whether the ship should be assigned to Empire's ShipPools
        /// for new AI controlled assignments</param>
        /// <param name="clearOrders">Clear any standing orders?</param>
        /// <returns>TRUE if this ship was actually removed from this Fleet</returns>
        public bool RemoveShip(Ship ship, bool returnToEmpireAI, bool clearOrders)
        {
            if (ship == null)
            {
                Log.Error($"Attempted to remove a null ship from Fleet {Name}");
                return false;
            }

            RemoveFromAllSquads(ship);
            bool removed = Ships.RemoveRef(ship);

            if (ship.Fleet == this)
            {
                ship.Fleet = null;
                if (ship.Active) // if ship is not dead, it means it's being transferred
                {
                    if (clearOrders)
                    {
                        ship.AI.ClearOrders();
                        ship.HyperspaceReturn(); // only exit hyperspace if we are clearing orders
                    }
                }
            }
            else
            {
                Log.Warning($"Fleet.RemoveShip: Ship was not part of this fleet: {this} ---- Ship: {ship} ");
            }

            return removed;
        }

        /// <summary>
        /// Removes all ships from their respective fleets.
        /// And optionally returns them to their empire's managed AI pool
        /// </summary>
        public static void RemoveShipsFromFleets(IReadOnlyList<Ship> ships, bool returnToManagedPools, bool clearOrders)
        {
            for (int i = 0; i < ships.Count; ++i)
                ships[i].ClearFleet(returnToManagedPools: returnToManagedPools, clearOrders: clearOrders);
        }

        /// <summary>
        /// Resets this entire fleet by removing all ships and clearing fleet tasks & goals
        /// </summary>
        public void Reset(bool returnShipsToEmpireAI = true, bool clearOrders = true)
        {
            RemoveAllShips(returnShipsToEmpireAI, clearOrders: clearOrders);
            Owner.AI?.RemoveFleetFromGoals(this);
            TaskStep = 0;
            FleetTask = null;
            Key = 0;
        }

        /// <summary>
        /// Removes all ships from this fleet, without resetting fleet goals
        /// </summary>
        public void RemoveAllShips(bool returnShipsToEmpireAI, bool clearOrders)
        {
            while (Ships.Count > 0)
            {
                var ship = Ships.PopLast();
                RemoveShip(ship, returnToEmpireAI: returnShipsToEmpireAI, clearOrders: clearOrders);
            }
        }


        /// <summary>
        /// Gives the suitable formation speed for our Ship
        /// This ensures ships slow down or speed up depending on
        /// distance to their formation position
        /// </summary>
        public (float STLSpeed, float FTLSpeed) GetFormationSpeedFor(Ship ship)
        {
            // this is the desired position inside the fleet formation
            Vector2 desiredFormationPos = GetFormationPos(ship);

            ShipAI.ShipGoal goal = ship.AI.OrderQueue.PeekFirst;
            Vector2 nextWayPoint = goal?.MovePosition ?? FinalPosition;
            float distToWP = ship.Position.Distance(nextWayPoint);
            float distToSquadPos = ship.Position.Distance(desiredFormationPos);
            float distSquadPosToWP = desiredFormationPos.Distance(nextWayPoint);
            float speedSTL = STLSpeedLimit; // if these are 0, there is no cap
            float speedFTL = FTLSpeedLimit;
            float relevantSpeedLimit = ship.IsInWarp ? FTLSpeedLimit : STLSpeedLimit;
            float angleDiff = desiredFormationPos.AngleToTargetWithFacing(ship.Position, ship.RotationDegrees);

            // Outside of fleet formation
            if (distToWP > distSquadPosToWP + ship.CurrentVelocity + 75f)
            {
                (speedSTL, speedFTL) = (ship.MaxSTLSpeed, ship.MaxFTLSpeed);
            }
            // FINAL APPROACH
            else if (distToWP < ship.FleetOffset.Length()
                // NON FINAL: we are much further from the formation
                || distToSquadPos > distToWP)
            {
                (speedSTL, speedFTL) = (STLSpeedLimit*2, FTLSpeedLimit*2);
            }
            // We are close to formation
            else if (distToSquadPos + ship.CurrentVelocity < relevantSpeedLimit*2)
            {
                if (distToSquadPos > relevantSpeedLimit * 0.5)
                {
                    int speedAjustDir = distToWP < distSquadPosToWP ? -1 : 1;
                    speedSTL = (STLSpeedLimit + speedAjustDir * distToSquadPos).LowerBound(50);
                    speedFTL = (FTLSpeedLimit + speedAjustDir * distToSquadPos).LowerBound(Ship.LightSpeedConstant);
                }
            }
            // We are far from formation
            else if (distToWP < distSquadPosToWP) // anglediff will be below 90
            {
                if (distToSquadPos > 100_000 && angleDiff > 25)
                {
                    float limit = (angleDiff+10) * 0.01f;
                    // We are ahead, but at a wide angle that allows us to continue full speed
                    speedSTL = Math.Max(ship.MaxSTLSpeed * limit, STLSpeedLimit * 0.25f);
                    speedFTL = Math.Max(ship.MaxFTLSpeed * limit, FTLSpeedLimit * 0.25f);
                }
                else
                {
                    // SLOW DOWN MAN! but never slower than 25% of fleet speed
                    speedSTL = Math.Max(STLSpeedLimit - distToSquadPos * 2, STLSpeedLimit * 0.25f);
                    speedFTL = Math.Max(FTLSpeedLimit - distToSquadPos * 2, FTLSpeedLimit * 0.25f);
                }
            }

            return (speedSTL, speedFTL);
        }

        public bool FindShipNode(Ship ship, out FleetDataNode node)
        {
            node = DataNodes.Find(d=>d.Ship == ship);
            return node != null;
        }

        public bool GoalExists(Goal goal)
        {
            return DataNodes.Any(n => n.Goal == goal);
        }

        public bool FindNodeWithGoal(Goal goal, out FleetDataNode node)
        {
            return (node = DataNodes.Find(n => n.Goal == goal)) != null;
        }

        public void AssignGoal(FleetDataNode node, Goal goal)
        {
            node.Goal = goal;
        }

        public void RemoveGoalFromNode(FleetDataNode node)
        {
            if (node != null)
                AssignGoal(node, null);
        }

        public void RemoveGoal(Goal goal)
        {
            if (FindNodeWithGoal(goal, out FleetDataNode node))
                AssignGoal(node, null);
        }

        public void AssignShipName(FleetDataNode node, string name)
        {
            node.ShipName = name;
        }

        public void Update(FixedSimTime timeStep)
        {
            InFormationMove = false;
            HasRepair = false;
            if (Ships.Count == 0)
                return;

            bool readyForWarp = true;
            Ship commandShip = null;
            var fleetTotals = new ShipAI.TargetParameterTotals();

            if (CommandShip?.Fleet != this || !CommandShip.CanTakeFleetMoveOrders())
                SetCommandShip(null);

            for (int i = Ships.Count - 1; i >= 0; --i)
            {
                Ship ship = Ships[i];

                // TODO: this is a concurrency issue, something has removed multiple items from the Ships array
                if (ship == null)
                    continue;

                if (!ship.Active)
                {
                    RemoveShip(ship, returnToEmpireAI: false, clearOrders: false);
                    continue;
                }
                if (ship.Fleet != this)
                {
                    RemoveShip(ship, returnToEmpireAI: true, clearOrders: true);
                    Log.Warning("Fleet.Update: Removing invalid ship: ship.Fleet != this");
                    continue;
                }

                if (CommandShip == null && ship.CanTakeFleetOrders)
                {
                    if (commandShip == null || commandShip.SurfaceArea < ship.SurfaceArea)
                        commandShip = ship;
                }

                fleetTotals.AddTargetValue(ship);

                if (!InFormationMove)
                    InFormationMove = ship.AI.State == AIState.FormationMoveTo;

                UpdateOurFleetShip(ship);

                readyForWarp = readyForWarp && ship.ShipEngines.ReadyForFormationWarp == WarpStatus.ReadyToWarp;
            }

            if (commandShip != null)
                SetCommandShip(commandShip);

            ReadyForWarp = readyForWarp;
            TotalFleetAttributes = fleetTotals;
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

        public void AddFleetDataNodes(Array<FleetDataNode> nodes)
        {
            foreach (var node in nodes)
                DataNodes.AddUnique(node);
        }

        [StarDataType]
        public sealed class Squad
        {
            [StarData] public FleetDataNode MasterDataNode = new();
            [StarData] public Array<FleetDataNode> DataNodes = new();
            [StarData] public Array<Ship> Ships = new();
            [StarData] public Fleet Fleet;
            [StarData] public Vector2 Offset; // squad offset within fleet

            public const float SquadSpacing = ShipAI.FlockingSeparation + 100f;

            public float GetShipIndexDirection(int index)
            {
                switch (index)
                {
                    default:
                    case 0: return RadMath.RadiansUp;
                    case 1: return RadMath.RadiansLeft;
                    case 2: return RadMath.RadiansRight;
                    case 3: return RadMath.RadiansDown;
                }
            }

            public Vector2 GetSquadSize()
            {
                float x = 0;
                float y = 0;
                for (int i = 0; i < DataNodes.Count; i++)
                {
                    FleetDataNode n = DataNodes[i];
                    float width  = n.Ship?.GridWidth * 8f ?? 0;
                    float height = n.Ship?.GridHeight * 8f ?? 0;
                    float nodeX  = Math.Abs((n.RelativeFleetOffset - Offset).X);
                    float nodeY  = Math.Abs((n.RelativeFleetOffset - Offset).Y);

                    x = Math.Max(x, nodeX) + width + SquadSpacing;
                    y = Math.Max(y, nodeY) + height + SquadSpacing;
                }
                return new Vector2(x, y);
            }

            public void SetNodeOffsets(float facing = 0.0f)
            {
                int count = DataNodes.Count;
                if (count == 0)
                    return;

                float squadOffsetAngle = Offset.Normalized().ToRadians();
                float squadOffsetLen = Offset.Length();

                for (int i = 0; i < DataNodes.Count; ++i)
                {
                    FleetDataNode n = DataNodes[i];
                    Ship ship = n.Ship;
                    if (ship != null)
                    {
                        float radiansAngle = GetShipIndexDirection(i);
                        Vector2 offset = (facing + squadOffsetAngle).RadiansToDirection() * squadOffsetLen;
                        float spacing = (ship.Radius + SquadSpacing);

                        ship.FleetOffset = offset + (facing + radiansAngle).RadiansToDirection() * spacing;
                        ship.RelativeFleetOffset = Offset + radiansAngle.RadiansToDirection() * spacing;
                        n.RelativeFleetOffset = ship.RelativeFleetOffset; // update the node offset also !
                        n.CombatState = n.Ship.AI.CombatState;
                    }
                }
            }

            void ClearSquadOffsets()
            {
                Offset = Vector2.Zero;
                for (int i = 0; i < DataNodes.Count; ++i)
                {
                    FleetDataNode n = DataNodes[i];
                    n.RelativeFleetOffset = Vector2.Zero;

                    if (n.Ship != null)
                    {
                        n.Ship.FleetOffset = Vector2.Zero;
                        n.Ship.RelativeFleetOffset = Vector2.Zero;
                        n.CombatState  = n.Ship.AI.CombatState;
                        n.OrdersRadius = n.Ship.SensorRange;
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
                    if (ship != null)
                        tactic(ship);
                }
            }
        }

        public static string GetDefaultFleetName(int index)
        {
            return GetFleetPrefix(index) + " Fleet";
        }
        
        static string GetFleetPrefix(int index)
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
                // 10th, 11th, 12th, 13th
                case 10: return "10th";
                case 11: return "11th";
                case 12: return "12th";
                case 13: return "13th";
                default: // 14th, ..., 21st, 22nd, 23rd, 24th, ...
                {
                    string suffix = "th";
                    switch (index % 10)
                    {
                        case 1: suffix = "st"; break;
                        case 2: suffix = "nd"; break;
                        case 3: suffix = "rd"; break;
                    }
                    return index + suffix;
                }
            }
        }
    }
}
