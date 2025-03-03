using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.AI.Tasks;
using Ship_Game.AI.StrategyAI.WarGoals;
using Ship_Game.Data.Serialization;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    using static ShipBuilder;

    public sealed partial class EmpireAI
    {
        [StarData] readonly Array<MilitaryTask> TaskList = new();
        readonly Array<MilitaryTask> TasksToAdd    = new();
        readonly Array<MilitaryTask> TasksToRemove = new();

        [StarDataSerialize]
        StarDataDynamicField[] OnSerialize()
        {
            ApplyPendingChanges();
            return null;
        }

        void RunMilitaryPlanner()
        {
            if (OwnerEmpire.isPlayer)
                return;

            RunGroundPlanner();
            int buildPlanets   = OwnerEmpire.GetBestPortsForShipBuilding(portQuality: 1.00f)?.Count ?? 0;
            NumberOfShipGoals  = buildPlanets * OwnerEmpire.DifficultyModifiers.CombatShipGoalsPerPlanet;
            var offensiveGoals = SearchForGoals(GoalType.BuildOffensiveShips);
            BuildWarShips(offensiveGoals.Count);
            PrioritizeTasks();
            int taskEvalLimit   = OwnerEmpire.IsAtWarWithMajorEmpire ? (int)OwnerEmpire.GetAverageWarGrade().LowerBound(3) : 10;
            int taskEvalCounter = 0;

            var tasks = OwnerEmpire.AI
                .GetTasks()
                .Filter(t => !t.QueuedForRemoval)
                .OrderByDescending(t => t.Priority)
                .ThenByDescending(t => t.MinimumTaskForceStrength).ToArr();

            for (int i = tasks.Length - 1; i >= 0; i--)
            {
                MilitaryTask task = tasks[i];
                if (task.Evaluate(OwnerEmpire))
                    taskEvalCounter += 1;

                if (taskEvalCounter == taskEvalLimit)
                    break;
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

        void PrioritizeTasks()
        {
            int numWars = OwnerEmpire.TryGetActiveWars(out Array<War> wars) ? wars.Count : 0;
            for (int i = 0; i < TaskList.Count; i++)
            {
                MilitaryTask task = TaskList[i];
                task.Prioritize(numWars);
            }
        }

        public void AddPendingTask(MilitaryTask task)
        {
            TasksToAdd.Add(task);
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
        }

        public MilitaryTask[] GetAtomicTasksCopy()
        {
            return TaskList.ToArray();
        }

        public MilitaryTask[] GetWarTasks()
        {
            return TaskList.Filter(task => task.IsWarTask);
        }

        public float GetAvgStrengthNeededByExpansionTasks(Empire targetEmpire)
        {
            var tasks = GetExpansionTasks(targetEmpire);
            if (tasks.Length == 0) return 0;

            return tasks.Average(task =>  task.Fleet != null ? task.MinimumTaskForceStrength : 0);
        }

        public IReadOnlyList<MilitaryTask> GetTasks() => TaskList;

        public MilitaryTask[] GetClaimTasks()
        {
            return TaskList.Filter(task => task.Type == MilitaryTask.TaskType.DefendClaim
                                        && task.TargetPlanet != null);
        }

        public MilitaryTask[] GetDefendVsRemnantTasks()
        {
            return TaskList.Filter(task => task.Type == MilitaryTask.TaskType.DefendVsRemnants);
        }

        public Goal[] GetRemnantEngagementGoalsFor(Planet p)
        {
            return FindGoals(g => g.IsRemnantEngageAtPlanet(p) && g is FleetGoal { Fleet.TaskStep: < 9 });
        }
        
        public MilitaryTask[] GetAssaultPirateTasks()
        {
            return TaskList.Filter(task => task.Type == MilitaryTask.TaskType.AssaultPirateBase);
        }

        public MilitaryTask[] GetExpansionTasks(Empire targetEmpire = null)
        {
            return TaskList.Filter(task =>
            {
                if (task.TargetPlanet != null &&
                    (task.Type == MilitaryTask.TaskType.DefendClaim ||
                     task.Type == MilitaryTask.TaskType.Exploration) && 
                     (task.TargetEmpire == targetEmpire || targetEmpire == null)) 
                    return true;
                return false;
            });
        }

        public MilitaryTask[] GetPotentialTasksToCompare() 
        {
            var expansionTasks        = GetExpansionTasks();
            var warTasks              = GetWarTasks();
            Array<MilitaryTask> tasks = new Array<MilitaryTask>();

            tasks.AddRange(expansionTasks);
            tasks.AddRange(warTasks);
            return tasks.ToArray();
        }

        public MilitaryTask[] GetDefendSystemTasks()
        {
            return TaskList.Filter(t => t.Type == MilitaryTask.TaskType.ClearAreaOfEnemies);
        }

        public int GetNumClaimTasks()
        {
            return TaskList.Filter(t => t.GetTaskCategory().IsSet(MilitaryTask.TaskCategory.Expansion)).Length;
        }

        public bool HasAssaultPirateBaseTask(Ship targetBase, out MilitaryTask militaryTask)
        {
            militaryTask = null;
            var filteredTasks = TaskList.Filter(task => task.Type == MilitaryTask.TaskType.AssaultPirateBase
                                                     && task.TargetShip == targetBase);

            if (filteredTasks.Length > 0f)
            {
                militaryTask = filteredTasks.First();
                if (filteredTasks.Length > 1)
                {
                    var duplicatedTask = filteredTasks.Last();
                    duplicatedTask.EndTask();
                    Log.Warning($"{OwnerEmpire.Name} Assault Pirate Base Tasks: Found more than one task for {militaryTask.TargetShip}. Ending the last one.");
                }
            }

            return militaryTask != null;
        }

        public void SendExplorationFleet(Planet p)
        {
            if (!TaskList.Any(t => t.Type == MilitaryTask.TaskType.Exploration && t.TargetPlanet == p))
            {
                var task = MilitaryTask.CreateExploration(p, OwnerEmpire);
                AddPendingTask(task);
            }
        }

        void BuildWarShips(int goalsInConstruction)
        {
            bool shouldIgnoreDebt = OwnerEmpire.TotalWarShipMaintenance < BuildCapacity || CreditRating >= 0.9f;
            var buildRatios       = new RoleBuildInfo(BuildCapacity, this, ignoreDebt: shouldIgnoreDebt);

            while (!buildRatios.OverBudget && goalsInConstruction < NumberOfShipGoals)
            {
                string s = GetAShip(buildRatios);
                if (string.IsNullOrEmpty(s))
                    break;

                AddGoalAndEvaluate(new BuildOffensiveShips(s, OwnerEmpire));
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
                foreach (var ship in empire.OwnedShips)
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
                foreach (IShipDesign design in empire.ShipsWeCanBuild)
                {
                    var combatRole = RoleCounts.ShipRoleToCombatRole(design.Role);
                    if (buildableShips.TryGetValue(combatRole, out RoleCounts roleCounts))
                        roleCounts.AddToBuildableShips(design);
                }
            }

            public void MaintenanceOfShipsUnderConstruction(EmpireAI eAI, Map<RoleCounts.CombatRole, RoleCounts> shipsBuilding)
            {
                IReadOnlyList<Goal> goals = eAI.SearchForGoals(GoalType.BuildOffensiveShips);
                foreach (Goal goal in goals)
                {
                    var ship = new BuildOffensiveShips.ShipInfo(eAI.OwnerEmpire, goal);
                    var combatRole = RoleCounts.ShipRoleToCombatRole(ship.Role);
                    if (shipsBuilding.TryGetValue(combatRole, out RoleCounts roleCounts))
                        roleCounts.AddToBuildingCost(ship.Upkeep);
                }
            }

            public Map<RoleName, float> CreateBuildPriorities()
            {
                var priorities = new Map<RoleName, float>();
                foreach(RoleName role in Enum.GetValues(typeof(RoleName)))
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

            public void IncrementShipCount(RoleName role)
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
                readonly Array<IShipDesign> BuildableShips = new();
                readonly Array<Ship> CurrentShips = new();
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
                        PerUnitMaintenanceMax = BuildableShips.Max(ship => ship.GetMaintenanceCost(Empire));

                    float minimum = CombatRoleToRatioMin(ratio);
                    FleetRatioMaintenance = PerUnitMaintenanceMax * minimum;
                }

                public void CalculateDesiredShips(FleetRatios ratio, float buildCapacity, float totalFleetMaintenance)
                {
                    float minimum = CombatRoleToRatioMin(ratio);
                    // quick check and set. if min is 0 or build is 0 there is no need to figure out that it can build 0
                    if (minimum <= 0 || buildCapacity <= 0)
                    {
                        DesiredCount = 0;
                        return;
                    }

                    CalculateBuildCapacity(buildCapacity, minimum, totalFleetMaintenance);
                    float buildBudget    = RoleBuildBudget;
                    float maintenanceMax = PerUnitMaintenanceMax.LowerBound(0.001f);
                    // if it has any budget to build it will build at least one ship
                    // this will give it a slight over build but it will allow it to always be able
                    // to build ships even on factional budgets.
                    DesiredCount = (int)Math.Ceiling(buildBudget / maintenanceMax);
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
                        case CombatRole.Disabled:   return 0;
                        case CombatRole.Fighter:    return ratio.MinFighters;
                        case CombatRole.Corvette:   return ratio.MinCorvettes;
                        case CombatRole.Frigate:    return ratio.MinFrigates;
                        case CombatRole.Cruiser:    return ratio.MinCruisers;
                        case CombatRole.Capital:    return ratio.MinCapitals;
                        case CombatRole.Carrier:    return ratio.MinCarriers;
                        case CombatRole.Bomber:     return ratio.MinBombers;
                        case CombatRole.Support:    return ratio.MinSupport;
                        case CombatRole.TroopShip:  return ratio.MinTroopShip;
                        case CombatRole.Battleship: return ratio.MinBattleships;

                        default:
                            throw new ArgumentOutOfRangeException($"Missing {Role} in MinRatios");
                    }
                }

                public void AddToCurrentShips(Ship ship) => CurrentShips.Add(ship);

                public void AddToBuildableShips(IShipDesign ship) => BuildableShips.Add(ship);

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
                    {
                        WeAreScrapping = false;
                        return;
                    }

                    WeAreScrapping = true;
                    foreach (var ship in CurrentShips
                        .OrderBy(ship => ship.ShipData.TechsNeeded.Count))
                    {
                        if (ship.OnLowAlert 
                            && ship.Fleet == null
                            && ship.AI.State != AIState.Scuttle
                            && ship.AI.State != AIState.Resupply
                            && ship.CanBeScrapped && ship.Active
                            && ship.GetMaintCost(empire) > 0
                            && ship.AI.State != AIState.Scrap)
                        {
                            CurrentCount--;
                            CurrentMaintenance -= ship.GetMaintCost();
                            ship.AI.OrderScrapShip();
                            break; // screp one of each role at a time
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
                    Battleship,
                    Capital,
                    Carrier,
                    Bomber,
                    Support,
                    TroopShip
                }

                public static CombatRole ShipRoleToCombatRole(RoleName role)
                {
                    switch (role)
                    {
                        case RoleName.disabled:
                        case RoleName.shipyard:
                        case RoleName.ssp:
                        case RoleName.platform:
                        case RoleName.station:
                        case RoleName.construction:
                        case RoleName.colony:
                        case RoleName.supply:
                        case RoleName.freighter:
                        case RoleName.troop:
                        case RoleName.prototype:
                        case RoleName.drone:
                        case RoleName.scout:      return CombatRole.Disabled;
                        case RoleName.troopShip:  return CombatRole.TroopShip;
                        case RoleName.support:    return CombatRole.Support;
                        case RoleName.bomber:     return CombatRole.Bomber;
                        case RoleName.carrier:    return CombatRole.Carrier;
                        case RoleName.fighter:    return CombatRole.Fighter;
                        case RoleName.gunboat:    return CombatRole.Corvette;
                        case RoleName.corvette:   return CombatRole.Corvette;
                        case RoleName.frigate:    return CombatRole.Frigate;
                        case RoleName.destroyer:  return CombatRole.Frigate;
                        case RoleName.cruiser:    return CombatRole.Cruiser;
                        case RoleName.battleship: return CombatRole.Battleship;
                        case RoleName.capital:    return CombatRole.Capital;
                        default:                  return CombatRole.Disabled;
                    }
                }
            }
        }

        // Pick a ship by role priority based on build ratios and maintenance 
        public string GetAShip(RoleBuildInfo buildRatios)
        {
            // Find ship to build
            Map<RoleName, float> pickRoles = buildRatios.CreateBuildPriorities();

            foreach (var kv in pickRoles.OrderByDescending(val => val.Value))
            {
                IShipDesign ship = PickFromCandidates(kv.Key, OwnerEmpire);
                if (ship == null)
                    continue;

                buildRatios.IncrementShipCount(kv.Key);
                return ship.Name;
            }

            return null;  // Found nothing to build !
        }
    }
}