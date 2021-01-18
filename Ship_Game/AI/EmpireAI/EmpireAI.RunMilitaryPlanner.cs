using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using Ship_Game.AI.Tasks;
using Microsoft.Xna.Framework;
using System.Threading.Tasks;
using Ship_Game.AI.StrategyAI.WarGoals;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    using static ShipBuilder;

    public sealed partial class EmpireAI
    {
        readonly Array<MilitaryTask> TaskList      = new Array<MilitaryTask>();
        readonly Array<MilitaryTask> TasksToAdd    = new Array<MilitaryTask>();
        readonly Array<MilitaryTask> TasksToRemove = new Array<MilitaryTask>();

        void RunMilitaryPlanner()
        {
            if (OwnerEmpire.isPlayer)
                return;
            RunGroundPlanner();

            NumberOfShipGoals = 2 + OwnerEmpire.GetBestPortsForShipBuilding()?.Count ?? 0;
            var offensiveGoals  = SearchForGoals(GoalType.BuildOffensiveShips);
            var planetsBuilding = new Array<Planet>();
            foreach (var goal in offensiveGoals) planetsBuilding.AddUnique(goal.PlanetBuildingAt);
            //var effectiveGoals  = offensiveGoals.Count / planetsBuilding.Count.LowerBound(1);
            BuildWarShips(offensiveGoals.Count);

            Goals.ApplyPendingRemovals();

            // Empire Military needs. War has its own task list in the WarTasks class
            Toughnuts = 0;

            var tasks = TaskList.SortedDescending(t=>
            {
                float hard = 0;
                if (t.GetTaskCategory() == MilitaryTask.TaskCategory.Expansion)
                    hard = OwnerEmpire.GetFleetStrEmpireMultiplier(t.TargetEmpire);
                return t.Priority + hard;
            });
            
            foreach (MilitaryTask task in tasks)
            {
                if (!task.QueuedForRemoval)
                {
                    if (task.IsToughNut)
                        Toughnuts++;
                    task.Evaluate(OwnerEmpire);
                }
            }
            ApplyPendingChanges();
        }

        void ApplyPendingChanges()
        {
            foreach (MilitaryTask remove in TasksToRemove)
                TaskList.RemoveRef(remove);
            TasksToRemove.Clear();

            TaskList.AddRange(TasksToAdd);
            TasksToAdd.Clear();
        }

        public void AddPendingTask(MilitaryTask task)
        {
            TasksToAdd.Add(task);
        }

        public void AddPendingTasks(Array<MilitaryTask> tasks)
        {
            TasksToAdd.AddRange(tasks);
        }

        public void EndAllTasks()
        {
            foreach (MilitaryTask task in TaskList.ToArray())
                task.EndTask();
            TasksToAdd.Clear();
            TasksToRemove.Clear();
            TaskList.Clear();
        }

        public void QueueForRemoval(MilitaryTask task)
        {
            if (!task.QueuedForRemoval)
            {
                task.QueuedForRemoval = true;
                TasksToRemove.Add(task);
            }
        }

        public void RemoveMilitaryTasksTargeting(Empire empire)
        {
            for (int i = TaskList.Count - 1; i >= 0; i--)
            {
                MilitaryTask task = TaskList[i];
                if (task.TargetPlanet?.Owner == empire)
                    task.EndTask();
            }

            WarTasks.PurgeAllTasksTargeting(empire);
        }

        public MilitaryTask[] GetAtomicTasksCopy()
        {
            return TaskList.ToArray();
        }

        public MilitaryTask[] GetMilitaryTasksTargeting(Empire empire)
        {
            return TaskList.Filter(task => task.TargetPlanet?.Owner == empire);
        }

        public MilitaryTask[] GetWarTasks()
        {
            return TaskList.Filter(task => task.GetTaskCategory().HasFlag(MilitaryTask.TaskCategory.War));
        }

        public MilitaryTask[] GetWarTasks(Empire targetEmpire)
        {
            return TaskList.Filter(task =>
            {
                if (task.GetTaskCategory().HasFlag(MilitaryTask.TaskCategory.War))
                {
                    if (task.TargetPlanet?.Owner == targetEmpire)
                        return true;
                }
                return false;
            });
        }



        public MilitaryTask[] GetTasksNeedingAFleet()
        {
            return TaskList.Filter(task => task.GetTaskCategory().HasFlag(MilitaryTask.TaskCategory.FleetNeeded));
        }

        public float GetStrengthNeededByTasks(Predicate<MilitaryTask> filter)
        {
            return TaskList.Sum(task => filter(task) ? task.MinimumTaskForceStrength : 0);
        }

        public float GetAvgStrengthNeededByExpansionTasks()
        {
            var tasks = GetExpansionTasks();
            if (tasks.Length == 0) return 0;

            return tasks.Average(task =>  task.WhichFleet >0 ? task.MinimumTaskForceStrength : 0);
        }

        public IReadOnlyList<MilitaryTask> GetTasks() => TaskList;

        public MilitaryTask[] GetClaimTasks()
        {
            return TaskList.Filter(task => task.type == MilitaryTask.TaskType.DefendClaim
                                        && task.TargetPlanet != null);
        }

        public MilitaryTask[] GetClaimTasks(SolarSystem targetSystem)
        {
            return TaskList.Filter(task => task.type == MilitaryTask.TaskType.DefendClaim
                                        && task.TargetPlanet?.ParentSystem == targetSystem);
        }

        public MilitaryTask[] GetDefendVsRemnantTasks()
        {
            return TaskList.Filter(task => task.type == MilitaryTask.TaskType.DefendVsRemnants);
        }

        public Goal[] GetRemnantEngagementGoalsFor(Planet p)
        {
            return Goals.Filter(g => g.type == GoalType.RemnantBalancersEngage
                                        && g.ColonizationTarget == p);
        }

        public MilitaryTask[] GetAssaultPirateTasks()
        {
            return TaskList.Filter(task => task.type == MilitaryTask.TaskType.AssaultPirateBase);
        }

        public MilitaryTask[] GetExpansionTasks()
        {
            return TaskList.Filter(task => task.TargetPlanet != null &&
                (task.type == MilitaryTask.TaskType.DefendClaim || task.type == MilitaryTask.TaskType.Exploration));
        }

        public int GetNumClaimTasks()
        {
            return TaskList.Filter(t => t.GetTaskCategory().HasFlag(MilitaryTask.TaskCategory.Expansion)).Length;
        }

        public bool HasAssaultPirateBaseTask(Ship targetBase, out MilitaryTask militaryTask)
        {
            militaryTask = null;
            var filteredTasks = TaskList.Filter(task => task.type == MilitaryTask.TaskType.AssaultPirateBase
                                                     && task.TargetShip == targetBase);

            if (filteredTasks.Length > 0f)
            {
                militaryTask = filteredTasks.First();
                if (filteredTasks.Length > 1)
                {
                    Log.Warning($"{OwnerEmpire.Name} Assault Pirate Base Tasks: Found more than one task for {militaryTask.TargetShip}. Using the first one.");
                }
            }

            return militaryTask != null;
        }

        public bool GetDefendClaimTaskFor(Planet planet, out MilitaryTask militaryTask)
        {
            militaryTask = null;
            var filteredTasks = TaskList.Filter(task => task.type == MilitaryTask.TaskType.DefendClaim
                                                     && task.TargetPlanet == planet);

            if (filteredTasks.Length > 0f)
            {
                militaryTask = filteredTasks.First();
                if (filteredTasks.Length > 1)
                    Log.Warning($"{OwnerEmpire.Name} Defend Claim Tasks: Found more than one task for {planet.Name}. Using the first one.");
            }

            return militaryTask != null;
        }

        public bool HasTaskOfType(MilitaryTask.TaskType type)
        {
            for (int i = TaskList.Count - 1; i >= 0; --i)
                if (TaskList[i].type == type)
                    return true;
            return false;
        }

        public MilitaryTask GetTaskByGuid(Guid guid) => TaskList.Find(t => t.TaskGuid == guid);

        public bool EndTaskByGuid(Guid guid)
        {
            var task = GetTaskByGuid(guid);
            if (task == null) return false;
            if (!TasksToRemove.Contains(task))
            {
                task.EndTask();
            }
            return true;
        }

        public void WriteToSave(SavedGame.GSAISAVE aiSave)
        {
            ApplyPendingChanges();
            aiSave.MilitaryTaskList = new Array<MilitaryTask>(TaskList);
            foreach (MilitaryTask task in aiSave.MilitaryTaskList)
            {
                if (task.TargetPlanet != null)
                    task.TargetPlanetGuid = task.TargetPlanet.guid;
            }
            aiSave.WarTaskClass = WarTasks;


        }

        public void ReadFromSave(SavedGame.GSAISAVE aiSave)
        {
            TaskList.Clear();
            TaskList.AddRange(aiSave.MilitaryTaskList);
            WarTasks = aiSave.WarTaskClass?? new WarTasks(OwnerEmpire);
        }

        public void TrySendExplorationFleetToCrashSite(Planet p)
        {
            if (TaskList.Filter(t => t.type == MilitaryTask.TaskType.Exploration)
                    .Length < 5 + (int)CurrentGame.Difficulty)
            {
                SendExplorationFleet(p);
            }
        }

        public void SendExplorationFleet(Planet p)
        {
            var task = MilitaryTask.CreateExploration(p, OwnerEmpire);
            AddPendingTask(task);
        }

        void BuildWarShips(int goalsInConstruction)
        {
            var buildRatios = new RoleBuildInfo(BuildCapacity, this, OwnerEmpire.data.TaxRate < 0.15f);
            //
            while (!buildRatios.OverBudget && goalsInConstruction < NumberOfShipGoals)
            {
                string s = GetAShip(buildRatios);
                if (string.IsNullOrEmpty(s))
                    break;

                Goals.Add(new BuildOffensiveShips(s, OwnerEmpire));
                goalsInConstruction++;
            }
        }

        public class RoleBuildInfo
        {
            private readonly EmpireAI EmpireAI;
            private Empire OwnerEmpire => EmpireAI.OwnerEmpire;

            Map<RoleCounts.CombatRole, RoleCounts> ShipCounts = new Map<RoleCounts.CombatRole, RoleCounts>();
            public float TotalFleetMaintenanceMin { get; private set; }
            public float TotalMaintenance { get; private set; }
            public float Capacity { get; private set; }
            public FleetRatios Ratios { get; private set; }
            public bool CanBuildMore(RoleCounts.CombatRole role) => ShipCounts[role].CanBuildMore;
            public float RoleBudget(RoleCounts.CombatRole role) => ShipCounts[role].RoleBuildBudget;
            public float RoleCurrentMaintenance(RoleCounts.CombatRole role) => ShipCounts[role].CurrentMaintenance;
            public float RoleUnitMaintenance(RoleCounts.CombatRole role) => ShipCounts[role].PerUnitMaintenanceMax;
            public float RoleCount(RoleCounts.CombatRole role) => ShipCounts[role].CurrentCount;
            public int  RoleCountDesired(RoleCounts.CombatRole role) => ShipCounts[role].DesiredCount;
            public bool RoleIsScrapping(RoleCounts.CombatRole role) => ShipCounts[role].WeAreScrapping;
            public bool OverBudget => Capacity < TotalMaintenance;

            public RoleBuildInfo(float capacity, EmpireAI eAI, bool ignoreDebt)
            {
                Capacity = capacity.LowerBound(0);
                EmpireAI = eAI;
                foreach (RoleCounts.CombatRole role in Enum.GetValues(typeof(RoleCounts.CombatRole)))
                {
                    if (role != RoleCounts.CombatRole.Disabled)
                        ShipCounts.Add(role, new RoleCounts(role, OwnerEmpire));
                }
                PopulateRoleCountWithActiveShips(OwnerEmpire, ShipCounts);
                PopulateRoleCountWithBuildableShips(OwnerEmpire, ShipCounts);
                MaintenanceOfShipsUnderConstruction(eAI, ShipCounts);

                Ratios = new FleetRatios(OwnerEmpire);
                foreach (var kv in ShipCounts)
                {
                    kv.Value.CalculateBasicCounts(Ratios);
                    TotalFleetMaintenanceMin += kv.Value.FleetRatioMaintenance;
                    TotalMaintenance += kv.Value.CurrentMaintenance;
                }
                foreach (var kv in ShipCounts)
                {
                    kv.Value.CalculateDesiredShips(Ratios, Capacity, TotalFleetMaintenanceMin);
                    if (!ignoreDebt)
                        kv.Value.ScrapAsNeeded(OwnerEmpire);
                }
            }

            public void PopulateRoleCountWithActiveShips(Empire empire, Map<RoleCounts.CombatRole, RoleCounts> currentShips)
            {
                foreach (var ship in empire.GetShips())
                {
                    if (ship != null && ship.Active && ship.CanBeScrapped && ship.AI.State != AIState.Scrap)
                    {
                        var combatRole = RoleCounts.ShipRoleToCombatRole(ship.DesignRole);
                        if (currentShips.TryGetValue(combatRole, out RoleCounts roleCounts))
                            roleCounts.AddToCurrentShips(ship);
                    }
                }
            }

            public void PopulateRoleCountWithBuildableShips(Empire empire, Map<RoleCounts.CombatRole, RoleCounts> buildableShips)
            {
                foreach (var shipName in empire.ShipsWeCanBuild)
                {
                    var ship = ResourceManager.GetShipTemplate(shipName);
                    var combatRole = RoleCounts.ShipRoleToCombatRole(ship.DesignRole);
                    if (buildableShips.TryGetValue(combatRole, out RoleCounts roleCounts))
                        roleCounts.AddToBuildableShips(ship);
                }
            }

            public void MaintenanceOfShipsUnderConstruction(EmpireAI eAI, Map<RoleCounts.CombatRole, RoleCounts> shipsBuilding)
            {
                IReadOnlyList<Goal> goals = eAI.SearchForGoals(GoalType.BuildOffensiveShips);
                foreach (Goal goal in goals)
                {
                    var ship = new BuildOffensiveShips.ShipInfo(goal);
                    var combatRole = RoleCounts.ShipRoleToCombatRole(ship.Role);
                    if (shipsBuilding.TryGetValue(combatRole, out RoleCounts roleCounts))
                    {
                        roleCounts.AddToBuildingCost(ship.Upkeep);
                    }
                }
            }

            public Map<ShipData.RoleName, float> CreateBuildPriorities()
            {
                var priorities = new Map<ShipData.RoleName, float>();
                foreach(ShipData.RoleName role in Enum.GetValues(typeof(ShipData.RoleName)))
                {
                    var combatRole = RoleCounts.ShipRoleToCombatRole(role);
                    if (combatRole == RoleCounts.CombatRole.Disabled) 
                        continue;

                    float priority = ShipCounts[combatRole].BuildPriority();
                    if (priority > 0)
                        priorities.Add(role, priority);
                }
                return priorities;
            }

            public void IncrementShipCount(ShipData.RoleName role)
            {
                var combatRole = RoleCounts.ShipRoleToCombatRole(role);
                if (combatRole == RoleCounts.CombatRole.Disabled)
                {
                    Log.Error($"Invalid Role: {role}");
                    return;
                }
                ShipCounts[combatRole].AddToCurrentShipCount(1);
                Capacity -= ShipCounts[combatRole].PerUnitMaintenanceMax;
            }

            public class RoleCounts
            {
                public float PerUnitMaintenanceMax { get; private set; }
                public float FleetRatioMaintenance { get; private set; }
                public float CurrentMaintenance { get; private set; }
                public float CurrentCount { get; private set; }
                public int DesiredCount { get; private set; }
                public float RoleBuildBudget { get; private set; }
                public float MaintenanceInConstruction { get; private set; }
                public bool CanBuildMore => CurrentMaintenance + PerUnitMaintenanceMax < RoleBuildBudget;
                public CombatRole Role { get; }
                private Empire Empire { get; }
                readonly Array<Ship> BuildableShips = new Array<Ship>();
                readonly Array<Ship> CurrentShips   = new Array<Ship>();
                public bool WeAreScrapping = false;

                public RoleCounts(CombatRole role, Empire empire)
                {
                    Role   = role;
                    Empire = empire;
                }

                public void CalculateBasicCounts(FleetRatios ratio)
                {
                    if (CurrentShips.NotEmpty)
                    {
                        CurrentMaintenance = CurrentShips.Sum(ship => ship.GetMaintCost());
                        CurrentCount += CurrentShips.Count;
                    }

                    CurrentMaintenance += MaintenanceInConstruction;
                    if (BuildableShips.NotEmpty)
                        PerUnitMaintenanceMax = BuildableShips.Max(ship => ship.GetMaintCost(Empire));

                    float minimum = CombatRoleToRatioMin(ratio);
                    FleetRatioMaintenance = PerUnitMaintenanceMax * minimum;
                }

                public void CalculateDesiredShips(FleetRatios ratio, float buildCapacity, float totalFleetMaintenance)
                {
                    float minimum = CombatRoleToRatioMin(ratio);
                    if (minimum.AlmostZero())
                        return;
                    CalculateBuildCapacity(buildCapacity, minimum, totalFleetMaintenance);
                    float buildBudget    = RoleBuildBudget.LowerBound(.001f);
                    float maintenanceMax = PerUnitMaintenanceMax.LowerBound(0.001f);
                    DesiredCount = (int)(buildBudget / maintenanceMax); // MinimumMaintenance));
                    //if (Role < CombatRole.Frigate)
                    //    DesiredCount = Math.Min(50, DesiredCount);
                }

                private void CalculateBuildCapacity(float totalCapacity, float wantedMin, float totalFleetMaintenance)
                {
                    if (wantedMin < .01f) return;
                    //float maintenanceRatio = FleetRatioMaintenance / totalFleetMaintenance;
                    float buildCapFleetMultiplier = totalCapacity / totalFleetMaintenance.LowerBound(1);
                    RoleBuildBudget = PerUnitMaintenanceMax * buildCapFleetMultiplier;
                    RoleBuildBudget *= wantedMin;
                    //RoleBuildBudget = totalCapacity * maintenanceRatio;
                }

                private float CombatRoleToRatioMin(FleetRatios ratio)
                {
                    switch (Role)
                    {
                        case CombatRole.Disabled: return 0;
                        case CombatRole.Fighter:  return ratio.MinFighters;
                        case CombatRole.Corvette: return ratio.MinCorvettes;
                        case CombatRole.Frigate:  return ratio.MinFrigates;
                        case CombatRole.Cruiser:  return ratio.MinCruisers;
                        case CombatRole.Capital:  return ratio.MinCapitals;
                        case CombatRole.Carrier:  return ratio.MinCarriers;
                        case CombatRole.Bomber:   return ratio.MinBombers;
                        case CombatRole.Support:  return ratio.MinSupport;
                        case CombatRole.TroopShip:return ratio.MinTroopShip;
                        default:                  return 0;
                    }
                }

                public void AddToCurrentShips(Ship ship) => CurrentShips.Add(ship);

                public void AddToBuildableShips(Ship ship) => BuildableShips.Add(ship);

                public void AddToBuildingCost(float cost)
                {
                    MaintenanceInConstruction += cost;
                    CurrentCount++;
                }

                public void AddToCurrentShipCount(int value)
                {
                    CurrentCount += value;
                    CurrentMaintenance += PerUnitMaintenanceMax * value;
                }

                public float BuildPriority()
                {
                    if (WeAreScrapping || CurrentMaintenance + PerUnitMaintenanceMax > RoleBuildBudget)
                        return 0;

                    // Higher is more important
                    return RoleBuildBudget / CurrentMaintenance.LowerBound(0.01f);
                }

                public void ScrapAsNeeded(Empire empire)
                {
                    if (CurrentCount <= DesiredCount + 1
                       || CurrentMaintenance <= RoleBuildBudget + PerUnitMaintenanceMax)
                        return;

                    foreach (var ship in CurrentShips
                        .OrderBy(ship => ship.shipData.TechsNeeded.Count))
                    {
                        if(!ship.InCombat &&
                                        (ship.fleet == null)
                                        && ship.AI.State != AIState.Scuttle
                                        && ship.AI.State != AIState.Resupply
                                        && ship.CanBeScrapped && ship.Active
                                        && ship.GetMaintCost(empire) > 0)
                        {
                            if (ship.AI.State != AIState.Scrap)
                            {
                                if (CurrentCount <= DesiredCount + 1
                                    && CurrentMaintenance <= RoleBuildBudget + PerUnitMaintenanceMax)
                                    break;
                                CurrentCount--;
                                CurrentMaintenance -= ship.GetMaintCost();
                                ship.AI.OrderScrapShip();
                                WeAreScrapping = true;
                            }
                            else
                            {
                                WeAreScrapping = true;
                            }
                        }
                    }
                }

                public enum CombatRole
                {
                    Disabled,
                    Fighter,
                    Corvette,
                    Frigate,
                    Cruiser,
                    Capital,
                    Carrier,
                    Bomber,
                    Support,
                    TroopShip
                }

                public static CombatRole ShipRoleToCombatRole(ShipData.RoleName role)
                {
                    switch (role)
                    {
                        case ShipData.RoleName.disabled:
                        case ShipData.RoleName.shipyard:
                        case ShipData.RoleName.ssp:
                        case ShipData.RoleName.platform:
                        case ShipData.RoleName.station:
                        case ShipData.RoleName.construction:
                        case ShipData.RoleName.colony:
                        case ShipData.RoleName.supply:
                        case ShipData.RoleName.freighter:
                        case ShipData.RoleName.troop:
                        case ShipData.RoleName.prototype:
                        case ShipData.RoleName.drone:
                        case ShipData.RoleName.scout:     return CombatRole.Disabled;
                        case ShipData.RoleName.troopShip: return CombatRole.TroopShip;
                        case ShipData.RoleName.support:   return CombatRole.Support;
                        case ShipData.RoleName.bomber:    return CombatRole.Bomber;
                        case ShipData.RoleName.carrier:   return CombatRole.Carrier;
                        case ShipData.RoleName.fighter:   return CombatRole.Fighter;
                        case ShipData.RoleName.gunboat:   return CombatRole.Corvette;
                        case ShipData.RoleName.corvette:  return CombatRole.Corvette;
                        case ShipData.RoleName.frigate:   return CombatRole.Frigate;
                        case ShipData.RoleName.destroyer: return CombatRole.Frigate;
                        case ShipData.RoleName.cruiser:   return CombatRole.Cruiser;
                        case ShipData.RoleName.capital:   return CombatRole.Capital;
                        default:                          return CombatRole.Disabled;
                    }
                }
            }
        }

        // Pick a ship by role priority based on build ratios and maintenance 
        public string GetAShip(RoleBuildInfo buildRatios)
        {
            // Find ship to build
            Map<ShipData.RoleName, float> pickRoles = buildRatios.CreateBuildPriorities();

            foreach (var kv in pickRoles.OrderByDescending(val => val.Value))
            {
                Ship ship = PickFromCandidates(kv.Key, OwnerEmpire);
                if (ship == null)
                    continue;

                buildRatios.IncrementShipCount(kv.Key);
                return ship.Name;
            }

            return null;  // Found nothing to build !
        }
    }
}