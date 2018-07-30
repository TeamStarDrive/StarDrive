using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    [System.Runtime.InteropServices.Guid("2CC355DF-EA7A-49C8-8940-00AA0713EFE3")]
    public sealed partial class EmpireAI : IDisposable
    {

        private int NumberOfShipGoals  = 6;
        private int NumberTroopGoals   = 2;
        private float BuildCapacity;
        private readonly float MinimumWarpRange = GlobalStats.MinimumWarpRange;
        
        private readonly Empire OwnerEmpire;
        private readonly BatchRemovalCollection<SolarSystem> MarkedForExploration = new BatchRemovalCollection<SolarSystem>();
        private readonly OffensiveForcePoolManager OffensiveForcePoolManager;

        public string EmpireName;
        public DefensiveCoordinator DefensiveCoordinator;        
        public BatchRemovalCollection<Goal> Goals            = new BatchRemovalCollection<Goal>();
        public ThreatMatrix ThreatMatrix                     = new ThreatMatrix();        
        public Array<AO> AreasOfOperations                   = new Array<AO>();
        public Array<int> UsedFleets                         = new Array<int>();
        public BatchRemovalCollection<Tasks.MilitaryTask> TaskList = new BatchRemovalCollection<Tasks.MilitaryTask>();
        public Array<Tasks.MilitaryTask> TasksToAdd                = new Array<Tasks.MilitaryTask>();        
        public float FreighterUpkeep                         = 0f;
        public float PlatformUpkeep                          = 0f;
        public float StationUpkeep                           = 0f;
        public float Toughnuts                               = 0;                
        public int Recyclepool                               = 0;
        public float DefStr;        
        public EmpireAI(Empire e)
        {
            EmpireName                                = e.data.Traits.Name;
            OwnerEmpire                               = e;
            DefensiveCoordinator                      = new DefensiveCoordinator(e);
            OffensiveForcePoolManager                 = new OffensiveForcePoolManager(e);
            if (OwnerEmpire.data.EconomicPersonality != null)            
                NumberOfShipGoals                     = NumberOfShipGoals + OwnerEmpire.data.EconomicPersonality.ShipGoalsPlus;
            
        }

        public void AddToTaskList(Tasks.MilitaryTask task) => TaskList.Add(task);

        public void RemoveFromTaskList(Tasks.MilitaryTask task)
        {
            if (task == null)
                Log.Error("Attempting to Remove null task from Empire TaskList");
            TaskList.Remove(task);            
        }
        public bool HasGoal(GoalType type)
        {
            for (int i = 0; i < Goals.Count; ++i)
                if (Goals[i].type == type) return true;
            return false;
        }

        public void AddGoal(Goal goal)
        {
            Goals.Add(goal);
        }

        private void RunManagers()
        {
            if (OwnerEmpire.data.IsRebelFaction || OwnerEmpire.data.Defeated)            
                return;
            
            if (!OwnerEmpire.isPlayer)
            {
                OffensiveForcePoolManager.ManageAOs();
                foreach (AO ao in AreasOfOperations)                
                    ao.Update();
                
            }
            if (!OwnerEmpire.isFaction)
            {
                RunEconomicPlanner();
                DefensiveCoordinator.ManageForcePool();
                RunExpansionPlanner();
                RunInfrastructurePlanner();                
                RunDiplomaticPlanner();
                RunResearchPlanner();
                RunAgentManager();
            }
            RunMilitaryPlanner();
            RunWarPlanner();                        
        }

        public Array<Planet> GetKnownPlanets()
        {
            var knownPlanets = new Array<Planet>();
            foreach (SolarSystem s in UniverseScreen.SolarSystemList)
            {
                if (s.IsExploredBy(OwnerEmpire))
                    knownPlanets.AddRange(s.PlanetList);
            }
            return knownPlanets;
        }
        
        public SolarSystem AssignExplorationTarget(Ship queryingShip)
        {
            var potentials = new Array<SolarSystem>();
            foreach (SolarSystem s in UniverseScreen.SolarSystemList)
            {
                if (!s.IsExploredBy(OwnerEmpire))                
                    potentials.Add(s);
            }
            
            using (MarkedForExploration.AcquireReadLock())
            foreach (SolarSystem s in MarkedForExploration)            
                potentials.Remove(s);
            
            IOrderedEnumerable<SolarSystem> sortedList =
                from system in potentials
                orderby Vector2.Distance(OwnerEmpire.GetWeightedCenter(), system.Position)
                select system;
            if (!sortedList.Any())
            {
                queryingShip.AI.OrderQueue.Clear();
                return null;
            }
            SolarSystem nearesttoHome = sortedList.OrderBy(furthest => Vector2.Distance(OwnerEmpire.GetWeightedCenter(), furthest.Position)).FirstOrDefault();
            foreach (SolarSystem nearest in sortedList)
            {
                if (nearest.CombatInSystem) continue;
                float distanceToScout = Vector2.Distance(queryingShip.Center, nearest.Position);
                float distanceToEarth = Vector2.Distance(OwnerEmpire.GetWeightedCenter(), nearest.Position);

                if (distanceToScout > distanceToEarth + 50000f)                
                    continue;
                
                nearesttoHome = nearest;
                break;

            }
            MarkedForExploration.Add(nearesttoHome);
            return nearesttoHome;
        }
        public void RemoveShipFromForce(Ship ship) => RemoveShipFromForce(ship, null);
        public void RemoveShipFromForce(Ship ship, AO ao)
        {
            if (ship == null) return;
            OwnerEmpire.ForcePoolRemove(ship);
            if (ao == null)
                foreach (var aos in AreasOfOperations)
                    aos.RemoveShip(ship);
            else
                ao.RemoveShip(ship);

            DefensiveCoordinator.Remove(ship);

        }


        public void AssignShipToForce(Ship toAdd)
        {
            toAdd.ClearFleet();
            if (OwnerEmpire.GetShipsFromOffensePools().Contains(toAdd))
            {
                //@TODO fix the cause of having ships already in forcepool when a ship is being added to the force pool
                OwnerEmpire.ForcePoolRemove(toAdd);
                         
            }

            int numWars = OwnerEmpire.AtWarCount;
            
            float baseDefensePct = 0.1f;
            baseDefensePct = baseDefensePct + 0.15f * numWars;
            if((toAdd.DesignRole < ShipData.RoleName.fighter || toAdd.BaseStrength <=0 
                || toAdd.WarpThrust <= 0f || toAdd.GetStrength() < toAdd.BaseStrength || !toAdd.BaseCanWarp )
                && !OwnerEmpire.GetForcePool().Contains(toAdd))
            {
                OwnerEmpire.GetForcePool().Add(toAdd);
                return;
            }

            if (baseDefensePct > 0.35f)            
                baseDefensePct = 0.35f;            
            
            bool needDef = OwnerEmpire.currentMilitaryStrength * baseDefensePct - DefStr >0 && DefensiveCoordinator.DefenseDeficit >0;

            if (needDef)
            {
                DefensiveCoordinator.AddShip(toAdd);
                return;
            }
            //need to rework this better divide the ships. 
            AO area = AreasOfOperations.FindMin(ao => toAdd.Position.SqDist(ao.Center));
            if (!area?.AddShip(toAdd) ?? false )
                OwnerEmpire.GetForcePool().Add(toAdd);
        }

        private Vector2 FindAveragePosition(Empire e)
        {
            Vector2 avgPos = new Vector2();
            foreach (Planet p in e.GetPlanets())            
                avgPos = avgPos + p.Center;
            
            if (e.GetPlanets().Count <= 0)            
                return Vector2.Zero;
            
            Vector2 count = avgPos / e.GetPlanets().Count;
            return count;
        }

        //Added by McShooterz: used for AI to get defensive structures to build around planets
        public string GetDefenceSatellite()
        {
            Array<Ship> potentialSatellites = new Array<Ship>();
            foreach (string platform in OwnerEmpire.structuresWeCanBuild)
            {
                Ship orbitalDefense = ResourceManager.ShipsDict[platform];
                if (platform != "Subspace Projector" && orbitalDefense.shipData.Role == ShipData.RoleName.platform 
                    && orbitalDefense.GetStrength() > 0)
                    potentialSatellites.Add(orbitalDefense);
            }
            if (!potentialSatellites.Any())
                return "";
            int index = RandomMath.InRange(potentialSatellites.Count);
            return potentialSatellites[index].Name;
        }

        public string GetStarBase()
        {
            var potentialSatellites = new Array<Ship>();
            foreach (string platform in OwnerEmpire.structuresWeCanBuild)
            {
                Ship orbitalDefense = ResourceManager.GetShipTemplate(platform);
                if (orbitalDefense.shipData.HullRole == ShipData.RoleName.station && !orbitalDefense.shipData.IsShipyard
                                                                                  && orbitalDefense.GetStrength() > 0)
                    //&& (orbitalDefense.shipData.IsOrbitalDefense))                
                    potentialSatellites.Add(orbitalDefense);                
            }
            if (!potentialSatellites.Any())
                return "";
            int index = RandomMath.InRange((int)(potentialSatellites.Count*.5f));
            return potentialSatellites.OrderByDescending(tech=> tech.shipData.TechScore)
                .ThenByDescending(stre => stre.shipData.BaseStrength).Skip(index).FirstOrDefault()?.Name;
        }
        
        public float GetDistanceFromOurAO(Planet p)
        {
            IOrderedEnumerable<AO> sortedList = 
                from area in AreasOfOperations
                orderby Vector2.Distance(p.Center, area.Center)
                select area;
            if (!sortedList.Any())            
                return 0f;
            
            return Vector2.Distance(p.Center, sortedList.First().Center);
        }
        public AO InOurAOs(Vector2 location)
        {
            return AreasOfOperations.Find(ao => location.InRadius(ao.Center, ao.Radius));
        }
        public bool IsInOurAOs(Vector2 location) => InOurAOs(location) != null;
        
        public void InitialzeAOsFromSave(UniverseData data)
        {
            foreach (AO area in AreasOfOperations)            
                area.InitFromSave(data, OwnerEmpire);                
            
        }
        
        public void ManageAOs()
        {
            Array<AO> aOs = new Array<AO>();
            foreach (AO areasOfOperation in AreasOfOperations)
            {
                
                if (areasOfOperation.GetPlanet().Owner != OwnerEmpire)
                {
                    aOs.Add(areasOfOperation);
                    continue;
                }                
                areasOfOperation.ThreatLevel = (int)ThreatMatrix.PingRadarStrengthLargestCluster(areasOfOperation.Center, areasOfOperation.Radius, OwnerEmpire);

                int taskStrNeeded = (int)(TaskList.FindMax(t => t.InitialEnemyStrength).EnemyStrength * 1.5f);

                int min = (int)(areasOfOperation.GetOffensiveForcePool().Sum(str => str.BaseStrength) *.5f);
                areasOfOperation.ThreatLevel = (int)MathExt.Max3(areasOfOperation.ThreatLevel, min, taskStrNeeded);                      
            }
            foreach (AO aO1 in aOs)
            {
                AreasOfOperations.Remove(aO1);
            }
            Array<Planet> planets = new Array<Planet>();
            foreach (Planet planet1 in OwnerEmpire.GetPlanets())
            {
                if (planet1.GetMaxProductionPotential() <= 5f || !planet1.HasShipyard)                
                    continue;
                
                bool flag = false;
                foreach (AO areasOfOperation1 in AreasOfOperations)
                {
                    if (areasOfOperation1.GetPlanet() != planet1)                    
                        continue;
                    
                    flag = true;
                    break;
                }
                if (flag)                
                    continue;
                
                planets.Add(planet1);
            }
            if (planets.Count == 0)
            {
                return;
            }
            IOrderedEnumerable<Planet> maxProductionPotential =
                from planet in planets
                orderby planet.GetMaxProductionPotential() descending
                select planet;
            
            foreach (Planet planet2 in maxProductionPotential)
            {
                float aoSize = 0;
                foreach (SolarSystem system in planet2.ParentSystem.FiveClosestSystems)
                {
                    if (aoSize < Vector2.Distance(planet2.Center, system.Position))
                        aoSize = Vector2.Distance(planet2.Center, system.Position);
                }
                float aomax = Empire.Universe.UniverseSize * .2f;
                if (aoSize > aomax)
                    aoSize = aomax;
                bool flag1 = true;
                foreach (AO areasOfOperation2 in AreasOfOperations)
                {

                    if (Vector2.Distance(areasOfOperation2.GetPlanet().Center, planet2.Center) >= aoSize)
                        continue;
                    flag1 = false;
                    break;
                }
                if (!flag1)
                {
                    continue;
                }

                AO aO2 = new AO(planet2, aoSize);
                AreasOfOperations.Add(aO2);
            }
        }

        public void RunEventChecker(KeyValuePair<Empire, Relationship> them)
        {
            if (OwnerEmpire == Empire.Universe.PlayerEmpire || OwnerEmpire.isFaction || !them.Value.Known)
                return;

            Array<Planet> ourTargetPlanets = new Array<Planet>();
            Array<Planet> theirTargetPlanets = new Array<Planet>();
            foreach (Goal g in Goals)
            {
                if (g.type == GoalType.Colonize)
                    ourTargetPlanets.Add(g.GetMarkedPlanet());
            }
            foreach (Goal g in them.Key.GetGSAI().Goals)
            {
                if (g.type == GoalType.Colonize)
                    theirTargetPlanets.Add(g.GetMarkedPlanet());
            }
            SolarSystem sharedSystem = null;
            them.Key.GetShips().ForEach(ship =>
            {
                if (ship.AI.State != AIState.Colonize || ship.AI.ColonizeTarget == null)                
                    return;
                
                theirTargetPlanets.Add(ship.AI.ColonizeTarget);
            }, false, false);

            foreach (Planet p in ourTargetPlanets)
            {
                bool matchFound = false;
                foreach (Planet other in theirTargetPlanets)
                {
                    if (p == null || other == null || p.ParentSystem != other.ParentSystem)                    
                        continue;
                    
                    sharedSystem = p.ParentSystem;
                    matchFound = true;
                    break;
                }
                if (matchFound)
                    break;
            }

            if (sharedSystem != null && !them.Value.AtWar && !them.Value.WarnedSystemsList.Contains(sharedSystem.guid))
            {
                bool theyAreThereAlready = false;
                foreach (Planet p in sharedSystem.PlanetList)
                {
                    if (p.Owner == null || p.Owner != Empire.Universe.PlayerEmpire)                    
                        continue;                    
                    theyAreThereAlready = true;
                }
                if (!theyAreThereAlready)
                {
                    if (them.Key == Empire.Universe.PlayerEmpire)                    
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, Empire.Universe.PlayerEmpire, "Claim System", sharedSystem));
                    
                    them.Value.WarnedSystemsList.Add(sharedSystem.guid);
                }
            }
        }

        public void TriggerRefit()
        {

            bool TechCompare(int[] original, int[] newTech)
            {
                bool Compare(int o, int n) => o > 0 && o > n;
                
                for (int x = 0; x < 4; x++)                
                    if (Compare(original[x], newTech[x])) return false;
                
                return true;
            }
            
            var offPool =OwnerEmpire.GetShipsFromOffensePools(onlyAO: true);
            for (int i = offPool.Count - 1; i >= 0; i--)
            {
                Ship ship = offPool[i];
                if (ship.AI.BadGuysNear) continue;
                if (ship.AI.HasPriorityOrder || ship.AI.HasPriorityTarget) continue;


                int techScore = ship.GetTechScore(out int[] origTechs);
                string name = "";
                float newStr = 0;
                foreach (string shipName in OwnerEmpire.ShipsWeCanBuild)
                {
                    Ship newTemplate = ResourceManager.GetShipTemplate(shipName);
                    if (newTemplate.shipData.Hull != ship.shipData.Hull && newTemplate.DesignRole != ship.DesignRole)
                        continue;
                    if (newTemplate.DesignRole != ship.DesignRole) continue;
                    if (newTemplate.GetStrength() <= newStr) continue;
                    if (ship.shipData.TechsNeeded.Except(newTemplate.shipData.TechsNeeded).Any()) continue;

                    int newScore = newTemplate.GetTechScore(out int[] newTech);


                    var newTechs = newTemplate.shipData.TechsNeeded.Except(ship.shipData.TechsNeeded).ToArray();

                    if (newTechs.Length == 0 && (newScore <= techScore || !TechCompare(origTechs, newTech)))
                        continue;

                    name = shipName;
                    newStr = newTemplate.GetStrength();
                    techScore = newScore;
                    origTechs = newTech;
                }
                if (string.IsNullOrEmpty(name)) continue;
                ship.AI.OrderRefitTo(name);
                
            }
        }

        public void Update()
        {		    
            DefStr = DefensiveCoordinator.GetForcePoolStrength();
            if (!OwnerEmpire.isFaction)            
                RunManagers();
            
            foreach (Goal g in Goals)
                g.Evaluate();
            
            Goals.ApplyPendingRemovals();
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~EmpireAI() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            TaskList?.Dispose(ref TaskList);
            DefensiveCoordinator?.Dispose(ref DefensiveCoordinator);
            Goals?.Dispose(ref Goals);
        }
    }
}