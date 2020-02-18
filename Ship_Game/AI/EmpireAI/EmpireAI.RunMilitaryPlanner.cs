using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using Ship_Game.AI.Tasks;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    using static ShipBuilder;

    public sealed partial class EmpireAI
    {
        readonly Array<MilitaryTask> TaskList = new Array<MilitaryTask>();
        readonly Array<MilitaryTask> TasksToAdd = new Array<MilitaryTask>();
        readonly Array<MilitaryTask> TasksToRemove = new Array<MilitaryTask>();

        void RunMilitaryPlanner()
        {
            if (OwnerEmpire.isPlayer)
                return;

            RunGroundPlanner();
            NumberOfShipGoals = 3;
            int goalsInConstruction = SearchForGoals(GoalType.BuildOffensiveShips).Count;
            if (goalsInConstruction <= NumberOfShipGoals)
                BuildWarShips(goalsInConstruction);

            Goals.ApplyPendingRemovals();

            // this where the global AI attack stuff happens.
            Toughnuts = 0;
            var olderWar = OwnerEmpire.GetOldestWar();

            foreach (MilitaryTask task in TasksSortedByPriority())
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

        void RemoveMilitaryTasksTargeting(Empire empire)
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

        public MilitaryTask[] GetMilitaryTasksTargeting(Empire empire)
        {
            return TaskList.Filter(task => task.TargetPlanet?.Owner == empire);
        }

        public MilitaryTask[] GetClaimTasks()
        {
            return TaskList.Filter(task => task.type == MilitaryTask.TaskType.DefendClaim
                                        && task.TargetPlanet != null);
        }

        public bool HasTaskOfType(MilitaryTask.TaskType type)
        {
            for (int i = TaskList.Count - 1; i >= 0; --i)
                if (TaskList[i].type == type)
                    return true;
            return false;
        }

        public bool IsAssaultingSystem(SolarSystem system)
                    => AnyTaskTargeting(MilitaryTask.TaskType.AssaultPlanet, system);

        public bool IsClaimingSystem(SolarSystem system)
                    => AnyTaskTargeting(MilitaryTask.TaskType.DefendClaim, system);

        public bool IsGlassingSystem(SolarSystem system)
                    => AnyTaskTargeting(MilitaryTask.TaskType.GlassPlanet, system);

        public bool IsAssaultingPlanet(Planet planet)
                    => AnyTaskTargeting(MilitaryTask.TaskType.AssaultPlanet, planet);

        public bool AnyTaskTargeting(MilitaryTask.TaskType taskType, SolarSystem solarSystem)
                    => TaskList.Any(t => t.type == taskType && t.TargetPlanet.ParentSystem == solarSystem);

        public bool AnyTaskTargeting(MilitaryTask.TaskType taskType, Planet planet)
                    => TaskList.Any(t => t.type == taskType && t.TargetPlanet == planet);

        public MilitaryTask[] TasksSortedByPriority() => TaskList.Sorted(true, TaskPriority);
        int TaskPriority(MilitaryTask task)
        {
            // sort by tasktype.
            // sort by enemy strength
            // and the date the war started.
            // the task priority number may be very high

            int initialPriority;

            switch (task.type)
            {
                case MilitaryTask.TaskType.AssaultPlanet             : initialPriority = 1; break;
                case MilitaryTask.TaskType.GlassPlanet               : initialPriority = 2; break;
                case MilitaryTask.TaskType.Resupply                  : initialPriority = 5; break;
                case MilitaryTask.TaskType.CorsairRaid               : initialPriority = 6; break;
                case MilitaryTask.TaskType.Exploration               : initialPriority = int.MinValue; break;
                case MilitaryTask.TaskType.DefendSystem              : initialPriority = 8; break;
                case MilitaryTask.TaskType.DefendClaim               : initialPriority = 7; break;
                case MilitaryTask.TaskType.DefendPostInvasion        : initialPriority = 10; break;
                default                                              : initialPriority = 20;
                    if (task.IsCoreFleetTask) initialPriority = int.MaxValue;
                    break;
            }

            int strengthMod          = (int)(Math.Max(task.MinimumTaskForceStrength, task.EnemyStrength));
            int targetEmpirePriority = 1;

            if ((task.type == MilitaryTask.TaskType.AssaultPlanet 
                 || task.type == MilitaryTask.TaskType.GlassPlanet)
                 && task.TargetPlanet.Owner != null)
            {
                var targetRelation   = OwnerEmpire.GetRelations(task.TargetPlanet.Owner);
                targetEmpirePriority = initialPriority * -(int)(targetRelation?.ActiveWar?.StartDate ?? 1);
            }
            return initialPriority + (strengthMod * targetEmpirePriority);
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
        }

        public void ReadFromSave(SavedGame.GSAISAVE aiSave)
        {
            TaskList.Clear();
            TaskList.AddRange(aiSave.MilitaryTaskList);
        }

        public void SendExplorationFleet(Planet p)
        {
            var militaryTask = new MilitaryTask
            {
                AO = p.Center,
                AORadius = 50000f,
                type = MilitaryTask.TaskType.Exploration
            };
            militaryTask.SetTargetPlanet(p);
            militaryTask.SetEmpire(OwnerEmpire);
            AddPendingTask(militaryTask);
        }

        void BuildWarShips(int goalsInConstruction)
        {
            int shipCountLimit = GlobalStats.ShipCountLimit;
            var buildRatios = new RoleBuildInfo(BuildCapacity, this, OwnerEmpire.data.TaxRate < 0.15f);
            //
            while (goalsInConstruction < NumberOfShipGoals && !buildRatios.OverBudget)
            {
                string s = GetAShip(buildRatios);
                if (string.IsNullOrEmpty(s))
                    break;

                if (Recyclepool > 0)
                    Recyclepool--;

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

            public bool OverBudget { get; private set; }

            public RoleBuildInfo(float capacity, EmpireAI eAI, bool ignoreDebt)
            {
                EmpireAI = eAI;
                foreach (RoleCounts.CombatRole role in Enum.GetValues(typeof(RoleCounts.CombatRole)))
                {
                    if (role != RoleCounts.CombatRole.Disabled)
                        ShipCounts.Add(role, new RoleCounts(role, OwnerEmpire));
                }
                PopulateRoleCountWithActiveShips(OwnerEmpire, ShipCounts);
                PopulateRoleCountWithBuildableShips(OwnerEmpire, ShipCounts);
                MaintenanceOfShipsUnderConstruction(eAI, ShipCounts);

                float totalMaintenance =0;

                var ratios = new FleetRatios(OwnerEmpire);
                foreach (var kv in ShipCounts)
                {
                    kv.Value.CalculateBasicCounts(ratios);
                    TotalFleetMaintenanceMin += kv.Value.FleetRatioMaintenance;
                    totalMaintenance += kv.Value.CurrentMaintenance;
                }
                foreach (var kv in ShipCounts)
                {
                    kv.Value.CalculateDesiredShips(ratios, capacity, TotalFleetMaintenanceMin);
                    if (!ignoreDebt)
                        kv.Value.ScrapAsNeeded(OwnerEmpire);
                }
                OverBudget = capacity < totalMaintenance;
            }

            public void PopulateRoleCountWithActiveShips(Empire empire, Map<RoleCounts.CombatRole, RoleCounts> currentShips)
            {
                foreach (var ship in empire.GetShips())
                {
                    if (ship != null && ship.Active && ship.Mothership == null && ship.AI.State != AIState.Scrap)
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
                    if (combatRole == RoleCounts.CombatRole.Disabled) continue;
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
                public CombatRole Role { get;}
                readonly Array<Ship> BuildableShips = new Array<Ship>();
                readonly Array<Ship> CurrentShips   = new Array<Ship>();

                public RoleCounts(CombatRole role, Empire empire)
                {
                    Role = role;
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
                        PerUnitMaintenanceMax = BuildableShips.Max(ship => ship.GetMaintCost(ship.loyalty));
                    float minimum = CombatRoleToRatioMin(ratio);
                    FleetRatioMaintenance = PerUnitMaintenanceMax * minimum;
                }

                public void CalculateDesiredShips(FleetRatios ratio, float buildCapacity, float totalFleetMaintenance)
                {
                    float minimum = CombatRoleToRatioMin(ratio);
                    if (minimum.AlmostZero())
                        return;
                    CalculateBuildCapacity(buildCapacity, minimum, totalFleetMaintenance);
                    DesiredCount = (int)(RoleBuildBudget / PerUnitMaintenanceMax.ClampMin(0.001f)); // MinimumMaintenance));
                    if (Role < CombatRole.Frigate)
                        DesiredCount = Math.Min(50, DesiredCount);
                }

                private void CalculateBuildCapacity(float totalCapacity, float wantedMin, float totalFleetMaintenance)
                {
                    if (wantedMin < .01f) return;
                    //float maintenanceRatio = FleetRatioMaintenance / totalFleetMaintenance;
                    float buildCapFleetMultiplier = totalCapacity / totalFleetMaintenance;
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

                public void AddToCurrentShipCount(int value) => CurrentCount += value;

                public float BuildPriority()
                {
                    if (CurrentCount >= DesiredCount && CurrentMaintenance >= RoleBuildBudget)
                        return 0;
                    return CurrentMaintenance.ClampMin(.00001f) / RoleBuildBudget;
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
                                        (!ship.fleet?.IsCoreFleet ?? true)
                                        && ship.AI.State != AIState.Scrap
                                        && ship.AI.State != AIState.Scuttle
                                        && ship.AI.State != AIState.Resupply
                                        && ship.Mothership == null && ship.Active
                                        && ship.GetMaintCost(empire) > 0)
                        { 
                            CurrentCount--;
                            CurrentMaintenance -= ship.GetMaintCost();
                            ship.AI.OrderScrapShip();
                            if (CurrentCount <= DesiredCount + 1
                                && CurrentMaintenance <= RoleBuildBudget + PerUnitMaintenanceMax)
                                break;
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
                        case ShipData.RoleName.troopShip:
                        case ShipData.RoleName.prototype:
                        case ShipData.RoleName.scout:    return CombatRole.Disabled;
                        case ShipData.RoleName.support:  return CombatRole.Support;
                        case ShipData.RoleName.bomber:   return CombatRole.Bomber;
                        case ShipData.RoleName.carrier:  return CombatRole.Carrier;
                        case ShipData.RoleName.fighter:  return CombatRole.Fighter;
                        case ShipData.RoleName.gunboat:  return CombatRole.Corvette;
                        case ShipData.RoleName.drone:    return CombatRole.Fighter;
                        case ShipData.RoleName.corvette: return CombatRole.Corvette;
                        case ShipData.RoleName.frigate:  return CombatRole.Frigate;
                        case ShipData.RoleName.destroyer:return CombatRole.Frigate;
                        case ShipData.RoleName.cruiser:  return CombatRole.Cruiser;
                        case ShipData.RoleName.capital:  return CombatRole.Capital;
                        default:                         return CombatRole.Disabled;
                    }
                }
            }
        }

        //fbedard: Build a ship with a random role

        private string GetAShip(RoleBuildInfo buildRatios)
        {

            //Find ship to build

            Map<ShipData.RoleName, float> pickRoles = buildRatios.CreateBuildPriorities();

            foreach (var kv in pickRoles.OrderBy(val => val.Value))
            {
                string buildThis = PickFromCandidates(kv.Key, OwnerEmpire);
                if (buildThis.IsEmpty())
                    continue;
                buildRatios.IncrementShipCount(kv.Key);
                return buildThis;
            }

            return null;  //Find nothing to build !
        }
    }
}