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
        private void RunMilitaryPlanner()
        {
            if (OwnerEmpire.isPlayer)
                return;
            RunGroundPlanner();
            NumberOfShipGoals = 3;
            int goalsInConstruction = SearchForGoals(GoalType.BuildOffensiveShips).Count;
            if (goalsInConstruction <= NumberOfShipGoals)
                BuildWarShips(goalsInConstruction);

            Goals.ApplyPendingRemovals();

            //this where the global AI attack stuff happenes.
            using (TaskList.AcquireReadLock())
            {
                int toughNutCount = 0;

                foreach (var task in TaskList)
                {
                    if (task.IsToughNut) toughNutCount++;
                    task.Evaluate(OwnerEmpire);
                }
                Toughnuts = toughNutCount;
            }
            TaskList.AddRange(TasksToAdd);
            TasksToAdd.Clear();
            TaskList.ApplyPendingRemovals();
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
            TaskList.Add(militaryTask);
        }

        private void BuildWarShips(int goalsInConstruction)
        {
            int shipCountLimit = GlobalStats.ShipCountLimit;
            RoleBuildInfo buildRatios = new RoleBuildInfo(BuildCapacity, this, OwnerEmpire.data.TaxRate < 0.25f);


            while (goalsInConstruction < NumberOfShipGoals
                   && (Empire.Universe.globalshipCount < shipCountLimit + Recyclepool
                       || OwnerEmpire.empireShipTotal < OwnerEmpire.EmpireShipCountReserve))
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

            Map<RoleCounts.CombatRole, RoleCounts> ShipCounts;
            public float TotalFleetMaintenanceMin { get; private set; }

            public RoleBuildInfo(float capacity, EmpireAI eAI, bool ignoreDebt)
            {
                EmpireAI = eAI;
                ShipCounts = new Map<RoleCounts.CombatRole, RoleCounts>();
                foreach (RoleCounts.CombatRole role in Enum.GetValues(typeof(RoleCounts.CombatRole)))
                {
                    if (role != RoleCounts.CombatRole.Disabled)
                        ShipCounts.Add(role, new RoleCounts(role, OwnerEmpire));
                }
                CurrentShips(OwnerEmpire, ShipCounts);
                BuildableShips(OwnerEmpire, ShipCounts);
                ShipsUnderConstruction(eAI, ShipCounts);

                var ratios = new FleetRatios(OwnerEmpire);
                foreach (var kv in ShipCounts)
                {
                    kv.Value.CalculateBasicCounts(ratios, capacity);
                    TotalFleetMaintenanceMin += kv.Value.FleetRatioMaintenance;
                }
                foreach (var kv in ShipCounts)
                {
                    kv.Value.CalculateDesiredShips(ratios, capacity, TotalFleetMaintenanceMin);
                    if (!ignoreDebt)
                        kv.Value.ScrapAsNeeded(OwnerEmpire);
                }
                
            }

            public void CurrentShips(Empire empire, Map<RoleCounts.CombatRole, RoleCounts> currentShips)
            {
                foreach (var ship in empire.GetShips())
                {
                    if (ship == null || !ship.Active || ship.Mothership != null || ship.AI.State == AIState.Scrap)
                        continue;
                    var combatRole = RoleCounts.ShipRoleToCombatRole(ship.DesignRole);
                    if (currentShips.TryGetValue(combatRole, out RoleCounts roleCounts))
                        roleCounts.AddToCurrentShips(ship);
                }
            }

            public void BuildableShips(Empire empire, Map<RoleCounts.CombatRole, RoleCounts> buildableShips)
            {
                foreach (var shipName in empire.ShipsWeCanBuild)
                {
                    var ship = ResourceManager.GetShipTemplate(shipName);
                    var combatRole = RoleCounts.ShipRoleToCombatRole(ship.DesignRole);
                    if (buildableShips.TryGetValue(combatRole, out RoleCounts roleCounts))
                        roleCounts.AddToBuildableShips(ship);
                }
            }

            public void ShipsUnderConstruction(EmpireAI eAI, Map<RoleCounts.CombatRole, RoleCounts> shipsBuilding)
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
                    {
                        priorities.Add(role, priority);
                    }

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
                public float PerUnitMaintenanceAverage { get; private set; }
                public float FleetRatioMaintenance { get; private set; }
                public float CurrentMaintenance { get; private set; }
                public float CurrentCount { get; private set; }
                public int DesiredCount { get; private set; }
                public float MaintenanceInConstruction { get; private set; }
                public CombatRole Role { get;}
                readonly Array<Ship> BuildableShips;
                readonly Array<Ship> CurrentShips;

                public RoleCounts (CombatRole role, Empire empire)
                {
                    Role = role;
                    PerUnitMaintenanceAverage = 0;

                    CurrentMaintenance = 0;
                    CurrentCount = 0;
                    DesiredCount = 0;
                    MaintenanceInConstruction = 0;
                    BuildableShips = new Array<Ship>();
                    CurrentShips = new Array<Ship>();
                }
                public void CalculateBasicCounts(FleetRatios ratio, float buildCapacity)
                {
                    if (CurrentShips.NotEmpty)
                    {
                        CurrentMaintenance = CurrentShips.Sum(ship => ship.GetMaintCost());
                        CurrentCount += CurrentShips.Count;
                    }
                    CurrentMaintenance += MaintenanceInConstruction;
                    if (BuildableShips.NotEmpty)
                        PerUnitMaintenanceAverage = BuildableShips.Average(ship => ship.GetMaintCost());
                    float minimum = CombatRoleToRatioMin(ratio);
                    FleetRatioMaintenance = PerUnitMaintenanceAverage * minimum;
                }

                public void CalculateDesiredShips(FleetRatios ratio, float buildCapacity, float totalFleetMaintenanceMin)
                {
                    float minimum = CombatRoleToRatioMin(ratio);
                    DesiredCount = ratio.ApplyRatio(totalFleetMaintenanceMin, PerUnitMaintenanceAverage, buildCapacity, minimum);
                }

                private float CombatRoleToRatioMin(FleetRatios ratio)
                {
                    float minimum = 0;
                    switch (Role)
                    {
                        case CombatRole.Disabled:
                            break;
                        case CombatRole.Fighter:
                            minimum = ratio.MinFighters;
                            break;
                        case CombatRole.Corvette:
                            minimum = ratio.MinCorvettes;
                            break;
                        case CombatRole.Frigate:
                            minimum = ratio.MinFrigates;
                            break;
                        case CombatRole.Cruiser:
                            minimum = ratio.MinCruisers;
                            break;
                        case CombatRole.Capital:
                            minimum = ratio.MinCapitals;
                            break;
                        case CombatRole.Carrier:
                            minimum = ratio.MinCarriers;
                            break;
                        case CombatRole.Bomber:
                            minimum = ratio.MinBombers;
                            break;
                        case CombatRole.Support:
                            minimum = ratio.MinSupport;
                            break;
                        case CombatRole.TroopShip:
                            minimum = ratio.MinTroopShip;
                            break;
                        default:
                            minimum = 0;
                            break;
                    }
                    return minimum;
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
                    if (CurrentCount >= DesiredCount)
                        return 0;
                    return CurrentCount.ClampMin(1) / DesiredCount;
                }
                public void ScrapAsNeeded(Empire empire)
                {
                    if (CurrentCount <= DesiredCount + 1)
                        return;

                    foreach (var ship in CurrentShips
                        .Filter(ship => !ship.InCombat &&
                                        (!ship.fleet?.IsCoreFleet ?? true)
                                        && ship.AI.State != AIState.Scrap
                                        && ship.AI.State != AIState.Scuttle
                                        && ship.AI.State != AIState.Resupply
                                        && ship.Mothership == null && ship.Active
                                        && ship.GetMaintCost(empire) > 0)
                        .OrderBy(ship => ship.shipData.TechsNeeded.Count))
                    {
                        CurrentCount--;
                        ship.AI.OrderScrapShip();
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
                            return CombatRole.Disabled;
                        case ShipData.RoleName.support:
                            return CombatRole.Support;
                        case ShipData.RoleName.bomber:
                            return CombatRole.Bomber;
                        case ShipData.RoleName.carrier:
                            return CombatRole.Carrier;
                        case ShipData.RoleName.fighter:
                            return CombatRole.Fighter;
                        case ShipData.RoleName.scout:
                            return CombatRole.Fighter;
                        case ShipData.RoleName.gunboat:
                            return CombatRole.Corvette;
                        case ShipData.RoleName.drone:
                            return CombatRole.Fighter;
                        case ShipData.RoleName.corvette:
                            return CombatRole.Corvette;
                        case ShipData.RoleName.frigate:
                            return CombatRole.Frigate;
                        case ShipData.RoleName.destroyer:
                            return CombatRole.Frigate;
                        case ShipData.RoleName.cruiser:
                            return CombatRole.Cruiser;
                        case ShipData.RoleName.capital:
                            return CombatRole.Capital;
                        default:
                            return CombatRole.Disabled;
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