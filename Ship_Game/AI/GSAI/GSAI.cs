using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
    [System.Runtime.InteropServices.Guid("2CC355DF-EA7A-49C8-8940-00AA0713EFE3")]
    public sealed partial class GSAI : IDisposable
    {
        public string EmpireName;
        private int numberOfShipGoals = 6;

        private int numberTroopGoals = 2;
        private Empire empire;

        public BatchRemovalCollection<Goal> Goals = new BatchRemovalCollection<Goal>();

        public ThreatMatrix ThreatMatrix = new ThreatMatrix();

        public DefensiveCoordinator DefensiveCoordinator;

        private BatchRemovalCollection<SolarSystem> MarkedForExploration = new BatchRemovalCollection<SolarSystem>();

        public Array<AO> AreasOfOperations = new Array<AO>();

        public Array<int> UsedFleets = new Array<int>();

        public BatchRemovalCollection<MilitaryTask> TaskList = new BatchRemovalCollection<MilitaryTask>();

        private Map<Ship, Array<Ship>> InterceptorDict = new Map<Ship, Array<Ship>>();

        public Array<MilitaryTask> TasksToAdd = new Array<MilitaryTask>();

        public int num_current_invasion_tasks;

        public float FreighterUpkeep = 0f;
        public float PlatformUpkeep = 0f;
        public float StationUpkeep = 0f;

        public float toughnuts = 0;
        //public float SSPBudget = 0;
        public float DefStr;

        //private int ThirdDemand = 75;

        float minimumWarpRange = GlobalStats.MinimumWarpRange;
        //SizeLimiter

        public int recyclepool =0;
        private float buildCapacity = 0;

        //added by gremlin: Smart Ship research
        
        //string BestCombatHull = "";
        //string BestSupportShip = "";
        private OffensiveForcePoolManager OffensiveForcePoolManager;

        public GSAI(Empire e)
        {
            this.EmpireName = e.data.Traits.Name;
            this.empire = e;
            this.DefensiveCoordinator = new DefensiveCoordinator(e);
            OffensiveForcePoolManager = new OffensiveForcePoolManager(e);
            if (this.empire.data.EconomicPersonality != null)
            {
                this.numberOfShipGoals = this.numberOfShipGoals + this.empire.data.EconomicPersonality.ShipGoalsPlus;
            }
           // this.desired_ColonyGoals = (int)this.empire.data.difficulty;
     
        }

        public Array<Planet> GetKnownPlanets()
        {
            Array<Planet> KnownPlanets = new Array<Planet>();
            foreach (SolarSystem s in UniverseScreen.SolarSystemList)
            {
                if (!s.ExploredDict[empire]) continue;
                KnownPlanets.AddRange(s.PlanetList);
            }
            return KnownPlanets;
        }
        //added by gremlin ExplorationTarget
        public SolarSystem AssignExplorationTarget(Ship queryingShip)
        {
            Array<SolarSystem> Potentials = new Array<SolarSystem>();
            foreach (SolarSystem s in UniverseScreen.SolarSystemList)
            {
                if (s.ExploredDict[this.empire])
                {
                    continue;
                }
                Potentials.Add(s);
            }
            
            using (MarkedForExploration.AcquireReadLock())
            foreach (SolarSystem s in this.MarkedForExploration)
            {
                Potentials.Remove(s);
            }
            IOrderedEnumerable<SolarSystem> sortedList =
                from system in Potentials
                orderby Vector2.Distance(this.empire.GetWeightedCenter(), system.Position)
                select system;
            if (sortedList.Count<SolarSystem>() <= 0)
            {
                queryingShip.AI.OrderQueue.Clear();
                return null;
            }
            //SolarSystem nearesttoScout = sortedList.OrderBy(furthest => Vector2.Distance(queryingShip.Center, furthest.Position)).FirstOrDefault();
            SolarSystem nearesttoHome = sortedList.OrderBy(furthest => Vector2.Distance(this.empire.GetWeightedCenter(), furthest.Position)).FirstOrDefault(); ;
            //SolarSystem potentialTarget = null;
            foreach (SolarSystem nearest in sortedList)
            {
                if (nearest.CombatInSystem) continue;
                float distanceToScout = Vector2.Distance(queryingShip.Center, nearest.Position);
                float distanceToEarth = Vector2.Distance(this.empire.GetWeightedCenter(), nearest.Position);

                if (distanceToScout > distanceToEarth + 50000f)
                {
                    continue;
                }
                nearesttoHome = nearest;
                break;

            }
            this.MarkedForExploration.Add(nearesttoHome);
            return nearesttoHome;
        }
        public void RemoveShipFromForce(Ship ship, AO ao = null)
        {
            if (ship == null) return;
            empire.ForcePoolRemove(ship);
            ao?.RemoveShip(ship);
            DefensiveCoordinator.Remove(ship);

        }
        public void AssignShipToForce(Ship toAdd)
        {            
            if (toAdd.fleet != null ||empire.GetShipsFromOffensePools().Contains(toAdd) )//|| empire.GetForcePool().Contains(toAdd))
            {
                Log.Error("ship in {0}", toAdd.fleet?.Name ?? "force Pool");
            }
            int numWars = empire.AtWarCount;
            
            float baseDefensePct = 0.1f;
            baseDefensePct = baseDefensePct + 0.15f * (float)numWars;
            if(toAdd .hasAssaultTransporter || toAdd.BombCount >0 || toAdd.HasTroopBay || toAdd.BaseStrength ==0 || toAdd.WarpThrust <= 0 || toAdd.GetStrength() < toAdd.BaseStrength || !toAdd.BaseCanWarp && !empire.GetForcePool().Contains(toAdd))
            {
                empire.GetForcePool().Add(toAdd);
                return;
            }

            if (baseDefensePct > 0.35f)
            {
                baseDefensePct = 0.35f;
            }
            
            bool needDef = empire.currentMilitaryStrength * baseDefensePct - DefStr >0 && DefensiveCoordinator.DefenseDeficit >0;

            if (needDef)   //
            {
                DefensiveCoordinator.AddShip(toAdd);
                return;
            }
            AO area = AreasOfOperations.FindMinFiltered(ao => !ao.AOFull, ao => toAdd.Position.SqDist(ao.Position));
            if (!area?.AddShip(toAdd) ?? false )
                empire.GetForcePool().Add(toAdd);

        }

        private void AssessTeritorialConflicts(float weight)
        {

            weight *= .1f;
            foreach (SystemCommander CheckBorders in this.DefensiveCoordinator.DefenseDict.Values)
            {
                
                if (CheckBorders.RankImportance > 5)
                {
                    foreach (SolarSystem closeenemies in CheckBorders.System.FiveClosestSystems)
                    {
                        foreach (Empire enemy in closeenemies.OwnerList)
                        {
#if PERF
                            if (enemy.isPlayer)
                                continue;
#endif
                            if (enemy.isFaction)
                                continue;
                            Relationship check = null;

                            if (this.empire.TryGetRelations(enemy, out check))
                            {
                                if (!check.Known || check.Treaty_Alliance)
                                    continue;
                                
                                    weight *= (this.empire.currentMilitaryStrength + closeenemies.GetActualStrengthPresent(enemy)) / (this.empire.currentMilitaryStrength +1);
                                
                                if (check.Treaty_OpenBorders)
                                {
                                    weight *= .5f;
                                }
                                if (check.Treaty_NAPact)
                                    weight *= .5f;
                               if (enemy.isPlayer)
                                weight *= ((int)Empire.Universe.GameDifficulty+1);

                                if (check.Anger_TerritorialConflict > 0)
                                    check.Anger_TerritorialConflict += (check.Anger_TerritorialConflict + CheckBorders.RankImportance * weight) / (check.Anger_TerritorialConflict);
                                else
                                    check.Anger_TerritorialConflict += CheckBorders.RankImportance * weight;

                            }
                        }
                        
                    }

                }
            }
        }
   


        //added by gremlin aggruthmanager

        //added by gremlin CunningAgent


    
        //added by gremlin deveks HonPacManager

        public void FactionUpdate()
        {
            string name = this.empire.data.Traits.Name;
            switch (name)
            {
                case "The Remnant":
                    {
                        bool HasPlanets = false; // this.empire.GetPlanets().Count > 0;
                        foreach(Planet planet in this.empire.GetPlanets())
                        {
                            HasPlanets = true;
                            
                            foreach(QueueItem item in planet.ConstructionQueue)
                            {
                            
                                {
                                    item.Cost = 0;                                                                        
                                }
                            

                            }
                            planet.ApplyProductiontoQueue(1, 0);
                            
                        }
                        foreach (Ship assimilate in this.empire.GetShips())
                        {
                            if (assimilate.shipData.ShipStyle != "Remnant" && assimilate.shipData.ShipStyle != null)
                            {
                                if (HasPlanets)
                                {
                                    if (assimilate.GetStrength() <= 0)
                                    {

                                        Planet target = null;
                                        if (assimilate.System!= null)
                                        {
                                            target = assimilate.System.PlanetList.Where(owner => owner.Owner != this.empire && owner.Owner != null).FirstOrDefault();

                                        }
                                        if (target != null)
                                        {
                                            assimilate.shipData.Role = ShipData.RoleName.troop;
                                            assimilate.TroopList.Add(ResourceManager.CreateTroop("Remnant Defender", assimilate.loyalty));

                                            if (assimilate.GetStrength() <= 0)
                                            {
                                                assimilate.isColonyShip = true;

                                                // @todo this looks like FindMinFiltered
                                                Planet capture = Empire.Universe.PlanetsDict.Values
                                                    .Where(potentials => potentials.Owner == null && potentials.habitable)
                                                    .OrderBy(potentials => Vector2.Distance(assimilate.Center, potentials.Position))
                                                    .FirstOrDefault();
                                                if (capture != null)
                                                    assimilate.AI.OrderColonization(capture);
                                            }

                                        }
                                    }
                                    else 
                                    {
                                        if (assimilate.Size < 50)
                                            assimilate.AI.OrderRefitTo("Heavy Drone");
                                        else if (assimilate.Size < 100)
                                            assimilate.AI.OrderRefitTo("Remnant Slaver");
                                        else if (assimilate.Size >= 100)
                                            assimilate.AI.OrderRefitTo("Remnant Exterminator");
                                    }
                                }
                                else
                                {
                                    if (assimilate.GetStrength() <= 0)
                                    {


                                        assimilate.isColonyShip = true;


                                        Planet capture = Empire.Universe.PlanetsDict.Values
                                            .Where(potentials => potentials.Owner == null && potentials.habitable)
                                            .OrderBy(potentials => Vector2.Distance(assimilate.Center, potentials.Position))
                                            .FirstOrDefault();
                                        if (capture != null)
                                            assimilate.AI.OrderColonization(capture);



                                    }

                                }


                            }
                            else
                            {
                                Planet target = null;
                                if (assimilate.System!= null && assimilate.AI.State == AIState.AwaitingOrders)
                                {
                                    target = assimilate.System.PlanetList.Where(owner => owner.Owner != this.empire && owner.Owner != null).FirstOrDefault();
                                    if (target !=null && (assimilate.HasTroopBay || assimilate.hasAssaultTransporter))
                                        if (assimilate.TroopList.Count > assimilate.GetHangars().Count)
                                            assimilate.AI.OrderAssaultPlanet(target);
                                }
                                
                            }
                        }
                    }
                    break;
                case "Corsairs":
                    {
                        bool AttackingSomeone = false;
                        //lock (GlobalStats.TaskLocker)
                        {
                            this.TaskList.ForEach(task =>//foreach (MilitaryTask task in this.TaskList)
                            {
                                if (task.type != MilitaryTask.TaskType.CorsairRaid)
                                {
                                    return;
                                }
                                AttackingSomeone = true;
                            }, false, false, false);
                        }
                        if (!AttackingSomeone)
                        {
                            foreach (KeyValuePair<Empire, Relationship> r in this.empire.AllRelations)
                            {
                                if (!r.Value.AtWar || r.Key.GetPlanets().Count <= 0 || this.empire.GetShips().Count <= 0)
                                {
                                    continue;
                                }
                                Vector2 center = new Vector2();
                                foreach (Ship ship in this.empire.GetShips())
                                {
                                    center = center + ship.Center;
                                }
                                center = center / (float)this.empire.GetShips().Count;
                                IOrderedEnumerable<Planet> sortedList =
                                    from planet in r.Key.GetPlanets()
                                    orderby Vector2.Distance(planet.Position, center)
                                    select planet;
                                MilitaryTask task = new MilitaryTask(this.empire);
                                task.SetTargetPlanet(sortedList.First<Planet>());
                                task.TaskTimer = 300f;
                                task.type = MilitaryTask.TaskType.CorsairRaid;
                              //  lock (GlobalStats.TaskLocker)
                                {
                                    this.TaskList.Add(task);
                                }
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
            
            
            
            //lock (GlobalStats.TaskLocker)
            {
                this.TaskList.ForEach(task =>//foreach (MilitaryTask task in this.TaskList)
                {
                    if (task.type != MilitaryTask.TaskType.Exploration)
                    {
                        task.Evaluate(this.empire);
                    }
                    else
                    {
                        task.EndTask();
                    }
                }, false, false, false);
            }
        }

        private Vector2 FindAveragePosition(Empire e)
        {
            Vector2 AvgPos = new Vector2();
            foreach (Planet p in e.GetPlanets())
            {
                AvgPos = AvgPos + p.Position;
            }
            if (e.GetPlanets().Count <= 0)
            {
                return Vector2.Zero;
            }
            Vector2 count = AvgPos / (float)e.GetPlanets().Count;
            AvgPos = count;
            return count;
        }

        private string GetAnAssaultShip()
        {
            Array<Ship> PotentialShips = new Array<Ship>();
            foreach (string shipsWeCanBuild in this.empire.ShipsWeCanBuild)
            {
                if (ResourceManager.ShipsDict[shipsWeCanBuild].TroopList.Count <= 0)
                {
                    continue;
                }
                PotentialShips.Add(ResourceManager.ShipsDict[shipsWeCanBuild]);
            }
            if (PotentialShips.Count > 0)
            {
                IOrderedEnumerable<Ship> sortedList = 
                    from ship in PotentialShips
                    orderby ship.TroopList.Count descending
                    select ship;
                if (sortedList.Count<Ship>() > 0)
                {
                    return sortedList.First<Ship>().Name;
                }
            }
            return "";
        }


        //Added by McShooterz: used for AI to get defensive structures to build around planets
        public string GetDefenceSatellite()
        {
            Array<Ship> PotentialSatellites = new Array<Ship>();
            foreach (string platform in this.empire.structuresWeCanBuild)
            {
                Ship orbitalDefense = ResourceManager.ShipsDict[platform];
                if (platform != "Subspace Projector" && orbitalDefense.shipData.Role == ShipData.RoleName.platform && orbitalDefense.BaseStrength > 0)
                    PotentialSatellites.Add(orbitalDefense);
            }
            if (PotentialSatellites.Count() == 0)
                return "";
            int index = RandomMath.InRange(PotentialSatellites.Count());
            return PotentialSatellites[index].Name;
        }
        public string GetStarBase()
        {
            Array<Ship> PotentialSatellites = new Array<Ship>();
            foreach (string platform in this.empire.structuresWeCanBuild)
            {
                Ship orbitalDefense = ResourceManager.ShipsDict[platform];
                if (orbitalDefense.shipData.Role == ShipData.RoleName.station && (orbitalDefense.shipData.IsOrbitalDefense || !orbitalDefense.shipData.IsShipyard))
                {
                    //if (orbitalDefense.BaseStrength == 0 && orbitalDefense.GetStrength() == 0)
                    //    continue;
                    PotentialSatellites.Add(orbitalDefense);
                }
            }
            if (PotentialSatellites.Count() == 0)
                return "";
            int index = RandomMath.InRange((int)(PotentialSatellites.Count()*.5f));
            return PotentialSatellites.OrderByDescending(tech=> tech.shipData.TechScore).ThenByDescending(stre=>stre.shipData.BaseStrength).Skip(index).FirstOrDefault().Name;
        }

        private float GetDistance(Empire e)
        {
            if (e.GetPlanets().Count == 0 || this.empire.GetPlanets().Count == 0)
            {
                return 0f;
            }
            Vector2 AvgPos = new Vector2();
            foreach (Planet p in e.GetPlanets())
            {
                AvgPos = AvgPos + p.Position;
            }
            AvgPos = AvgPos / (float)e.GetPlanets().Count;
            Vector2 Ouravg = new Vector2();
            foreach (Planet p in this.empire.GetPlanets())
            {
                Ouravg = Ouravg + p.Position;
            }
            Ouravg = Ouravg / (float)this.empire.GetPlanets().Count;
            return Vector2.Distance(AvgPos, Ouravg);
        }

        public float GetDistanceFromOurAO(Planet p)
        {
            IOrderedEnumerable<AO> sortedList = 
                from area in this.AreasOfOperations
                orderby Vector2.Distance(p.Position, area.Position)
                select area;
            if (sortedList.Count<AO>() == 0)
            {
                return 0f;
            }
            return Vector2.Distance(p.Position, sortedList.First<AO>().Position);
        }
        private float GetDistanceFromOurAO(Vector2 p)
        {
            IOrderedEnumerable<AO> sortedList =
                from area in this.AreasOfOperations
                orderby Vector2.Distance(p, area.Position)
                select area;
            if (sortedList.Count<AO>() == 0)
            {
                return 0f;
            }
            return Vector2.Distance(p, sortedList.First<AO>().Position);
        }

        public void InitialzeAOsFromSave(UniverseData data)
        {
            foreach (AO area in AreasOfOperations)
            {
                area.InitFromSave(data, empire);                
            }
        }

        //addedby gremlin manageAOs
        public void ManageAOs()
        {
            //Vector2 empireCenter =this.empire.GetWeightedCenter();
            
            Array<AO> aOs = new Array<AO>();
            float empireStr = this.empire.currentMilitaryStrength; // / (this.AreasOfOperations.Count*2.5f+1);
            foreach (AO areasOfOperation in this.AreasOfOperations)
            {
                
                if (areasOfOperation.GetPlanet().Owner != empire)
                {
                    aOs.Add(areasOfOperation);
                    continue;
                }                
                areasOfOperation.ThreatLevel = (int)ThreatMatrix.PingRadarStrengthLargestCluster(areasOfOperation.Position, areasOfOperation.Radius, empire);

                int min = (int)(areasOfOperation.GetOffensiveForcePool().Sum(str => str.BaseStrength) *.5f);
                if (areasOfOperation.ThreatLevel < min)
                    areasOfOperation.ThreatLevel = min;
                
            }
            foreach (AO aO1 in aOs)
            {
                this.AreasOfOperations.Remove(aO1);
            }
            Array<Planet> planets = new Array<Planet>();
            foreach (Planet planet1 in this.empire.GetPlanets())
            {
                if (planet1.GetMaxProductionPotential() <= 5f || !planet1.HasShipyard)
                {
                    continue;
                }
                bool flag = false;
                foreach (AO areasOfOperation1 in this.AreasOfOperations)
                {
                    if (areasOfOperation1.GetPlanet() != planet1)
                    {
                        continue;
                    }
                    flag = true;
                    break;
                }
                if (flag)
                {
                    continue;
                }
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
                foreach (SolarSystem system in planet2.system.FiveClosestSystems)
                {
                    //if (system.OwnerList.Contains(this.empire))
                    //    continue;
                    if (aoSize < Vector2.Distance(planet2.Position, system.Position))
                        aoSize = Vector2.Distance(planet2.Position, system.Position);
                }
                float aomax = Empire.Universe.Size.X * .2f;
                if (aoSize > aomax)
                    aoSize = aomax;
                bool flag1 = true;
                foreach (AO areasOfOperation2 in this.AreasOfOperations)
                {

                    if (Vector2.Distance(areasOfOperation2.GetPlanet().Position, planet2.Position) >= aoSize)
                        continue;
                    flag1 = false;
                    break;
                }
                if (!flag1)
                {
                    continue;
                }

                AO aO2 = new AO(planet2, aoSize);
                this.AreasOfOperations.Add(aO2);
            }
        }


        //added by gremlin Economic planner

        public void RunEventChecker(KeyValuePair<Empire, Relationship> Them)
        {
            if (empire == Empire.Universe.PlayerEmpire || empire.isFaction || !Them.Value.Known)
                return;

            Array<Planet> OurTargetPlanets = new Array<Planet>();
            Array<Planet> TheirTargetPlanets = new Array<Planet>();
            foreach (Goal g in this.Goals)
            {
                if (g.type == GoalType.Colonize)
                    OurTargetPlanets.Add(g.GetMarkedPlanet());
            }
            foreach (Goal g in Them.Key.GetGSAI().Goals)
            {
                if (g.type == GoalType.Colonize)
                    TheirTargetPlanets.Add(g.GetMarkedPlanet());
            }
            SolarSystem sharedSystem = null;
            Them.Key.GetShips().ForEach(ship => //foreach (Ship ship in Them.Key.GetShips())
            {
                if (ship.AI.State != AIState.Colonize || ship.AI.ColonizeTarget == null)
                {
                    return;
                }
                TheirTargetPlanets.Add(ship.AI.ColonizeTarget);
            }, false, false, false);

            foreach (Planet p in OurTargetPlanets)
            {
                bool matchFound = false;
                foreach (Planet other in TheirTargetPlanets)
                {
                    if (p == null || other == null || p.system != other.system)
                    {
                        continue;
                    }
                    sharedSystem = p.system;
                    matchFound = true;
                    break;
                }
                if (matchFound)
                    break;
            }

            if (sharedSystem != null && !Them.Value.AtWar && !Them.Value.WarnedSystemsList.Contains(sharedSystem.guid))
            {
                bool TheyAreThereAlready = false;
                foreach (Planet p in sharedSystem.PlanetList)
                {
                    if (p.Owner == null || p.Owner != Empire.Universe.PlayerEmpire)
                    {
                        continue;
                    }
                    TheyAreThereAlready = true;
                }
                if (!TheyAreThereAlready)
                {
                    if (Them.Key == Empire.Universe.PlayerEmpire)
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "Claim System", sharedSystem));
                    }
                    Them.Value.WarnedSystemsList.Add(sharedSystem.guid);
                }
            }
        }


        private void RunManagers()
        {
            if (this.empire.data.IsRebelFaction || this.empire.data.Defeated)
            {
                return;
            }
            if (!this.empire.isPlayer)
            {
                OffensiveForcePoolManager.ManageAOs();
                foreach (AO ao in this.AreasOfOperations)
                {
                    ao.Update();
                }
            }
            //this.UpdateThreatMatrix();
            if (!this.empire.isFaction && !this.empire.MinorRace)
            {
                if (!this.empire.isPlayer || this.empire.AutoColonize)
                    this.RunExpansionPlanner();
                if(!this.empire.isPlayer || this.empire.AutoBuild)
                    this.RunInfrastructurePlanner();
            }
            this.DefensiveCoordinator.ManageForcePool();
            if (!this.empire.isPlayer)
            {
                this.RunEconomicPlanner();
                this.RunDiplomaticPlanner();
                if(this.empire.isFaction)
                {

                }
                if (!this.empire.MinorRace)
                {
                    
                    this.RunMilitaryPlanner();
                    this.RunResearchPlanner();
                    this.RunAgentManager();
                    this.RunWarPlanner();
                }
            }
            //Added by McShooterz: automating research
            else
            {
                if (this.empire.AutoResearch)
                    this.RunResearchPlanner();
                if (this.empire.data.AutoTaxes)
                    this.RunEconomicPlanner();
            }
        }

    
        //added by gremlin deveksmod military planner


        private float randomizer(float priority, float bonus)
        {
            float index=0;
            index += RandomMath.RandomBetween(0, (priority + bonus));
            index += RandomMath.RandomBetween(0, (priority + bonus));
            index += RandomMath.RandomBetween(0, (priority + bonus));
            return index ;
        }

        public class wieght
        {
            public int strength;
            public ShipModule module;
            public int count;
            
        }
        int hullScaler = 1;

        //public void HullChecker(Ship ship, ShipData currentHull, float moneyNeeded)
        //{
        //    foreach (KeyValuePair<string, Ship> wecanbuildit in ResourceManager.ShipsDict.OrderBy(techcost => techcost.Value.shipData != null ? techcost.Value.shipData.TechScore : 0)) // techcost.Value.BaseStrength)) //techcost.Value.shipData != null ? techcost.Value.shipData.techsNeeded.Count : 0))// // shipData !=null? techcost.Value.shipData.TechScore:0))   //Value.shipData.techsNeeded.Count:0))
        //    {

        //        bool test;

        //        ship = wecanbuildit.Value;
        //        if (ship.shipData == null)
        //            continue;
        //        if (ship.BaseStrength < 1f)
        //            continue;
        //        Ship_Game.ShipRole roles;
        //        if (!ResourceManager.ShipRoles.TryGetValue(ship.shipData.Role, out roles)
        //            || roles == null || roles.Protected
        //            || ship.shipData.Role == ShipData.RoleName.freighter
        //            || ship.shipData.Role == ShipData.RoleName.platform
        //            || ship.shipData.Role == ShipData.RoleName.station
        //            )
        //            continue;

        //        test = false;

        //        if (ship.shipData.ShipStyle != this.empire.data.Traits.ShipType
        //            && (this.empire.GetHDict().TryGetValue(ship.shipData.Hull, out test) && test == true) == false)
        //            continue;
        //        test = false;
        //        if (this.empire.ShipsWeCanBuild.Contains(wecanbuildit.Key))//this.empire.GetHDict()[ship.shipData.Hull] || 
        //            continue;
        //        int techCost = ship.shipData.techsNeeded.Count;
        //        if (techCost == 0)
        //            continue;

        //        int trueTechcost = techCost;
        //        if (currentHull != null && ship.shipData.Hull == currentHull.Hull)
        //        {
        //            techCost -= 1;
        //        }


        //        {

        //            bool hangarflag = false;
        //            bool bombFlag = false;
        //            foreach (ModuleSlot hangar in ship.ModuleSlotList)
        //            {

        //                if (hangar.module.ModuleType == ShipModuleType.Hangar)
        //                {
        //                    hangarflag = true;

        //                }
        //                if (hangar.module.ModuleType == ShipModuleType.Bomb)
        //                    bombFlag = true;
        //                if (bombFlag && hangarflag)
        //                    break;
        //            }
        //            techCost -= bombFlag ? 1 : 0;
        //            techCost -= hangarflag ? 1 : 0;
        //        }

        //        if (ship.Name == this.BestCombatShip)
        //            techCost -= 1;
        //        {
        //            moneyNeeded = ship.GetMaintCost(this.empire);// *5;
        //            bool GeneralTechBlock = false;
        //            foreach (string shipTech in ship.shipData.techsNeeded)
        //            {

        //               // if (!test && !unlockedTech.Contains(shipTech))
        //                {
        //                    test = true;
        //                }

        //                {
        //                    TechEntry generalTech;

        //                    if (this.empire.TechnologyDict.TryGetValue(shipTech, out generalTech))
        //                    {
        //                        Technology hull = generalTech.GetTech();
        //                        if (generalTech.Unlocked)
        //                        {
        //                            techCost--;
        //                            trueTechcost--;
        //                            continue;
        //                        }
        //                        else if ((hull.RaceRestrictions.Count > 0 && hull.RaceRestrictions.Where(restrict => restrict.ShipType == this.empire.data.Traits.ShipType).Count() == 0)
        //                            || (hull.RaceRestrictions.Count == 0 && hull.Secret && !hull.Discovered))
        //                        {
        //                            test = false;
        //                            break;
        //                        }
        //                        else if (generalTech.GetTech().TechnologyType == TechnologyType.ShipGeneral)
        //                        {
        //                            if (!GeneralTechBlock)
        //                            {
        //                                techCost--;
        //                                GeneralTechBlock = true;
        //                            }
        //                            else
        //                                GeneralTechBlock = false;

        //                            continue;
        //                        }

        //                        {


        //                            if (this.empire.data.ShipBudget * .3f < ship.GetMaintCost(this.empire))
        //                            {
        //                                test = false;
        //                                break;
        //                                //techCost += 100;
        //                            }
        //                            if (hull.Cost / (this.empire.Research + 1) > 500)
        //                            {
        //                                techCost += 2; //(int)(hull.Cost / (150 * (this.empire.Research + 1)));
        //                            }
        //                            else if (hull.Cost / (this.empire.Research + 1) > 150)
        //                                techCost += 1;


        //                        }
        //                        if (!test)
        //                            continue;
        //                    }
        //                    else
        //                        test = false;
        //                }
        //            }
        //            if (!test)
        //            {
        //                if (ship.Name == this.BestCombatShip)
        //                    this.BestCombatShip = string.Empty;
        //                ship = null;
        //                continue;
        //            }
        //           // useableTech.AddRange(ship.shipData.techsNeeded);
        //            if (trueTechcost == 0)
        //            {
        //                Log.Info("skipped: " + ship.Name);
        //                if (ship.Name == this.BestCombatShip)
        //                    this.BestCombatShip = string.Empty;
        //                ship = null;
        //                continue;
        //            }
        //            //if (BestShip == null
        //            //    || (BestShip.BaseStrength < ship.BaseStrength
        //            //    && BestShipTechCost >= techCost
        //            //    && BestShip.shipData.techsNeeded.Except(this.empire.ShipTechs).Count() > techCost
        //            //    )) //(string.IsNullOrEmpty(BestShip) ||(bestShipStrength < ship.BaseStrength &&  BestShipTechCost >= techCost)) //BestShipTechCost == 0 ||
        //            //{
        //            //    bestShipStrength = ship.BaseStrength;
        //            //    BestShip = wecanbuildit.Value;
        //            //    BestShipTechCost = techCost;
        //            //    //Log.Info("Choosing Best Ship Tech: " + this.empire.data.Traits.ShipType + " -Ship: " + BestShip + " -TechCount: " + trueTechcost +"/"+techCost);
        //            //}

        //        }
        //    }
        //}


        public void TriggerRefit()
        {

            bool TechCompare(int[] original, int[] newTech)
            {
                bool compare(int o, int n) => o > 0 && o > n;
                
                for (int x = 0; x < 4; x++)                
                    if (!compare(original[x], newTech[x])) return false;
                
                return true;
            }

            int upgrades = 0;
            var offPool =empire.GetShipsFromOffensePools();
            for (int i = offPool.Count - 1; i >= 0; i--)
            {
                Ship ship = offPool[i];
                if (upgrades < 5)
                {
                    int techScore = ship.GetTechScore(out int[] origTechs);
                    string name = "";

                    foreach (string shipName in empire.ShipsWeCanBuild)
                    {
                        Ship newTemplate = ResourceManager.GetShipTemplate(shipName);
                        if (newTemplate.GetShipData().Hull != ship.GetShipData().Hull)                                                        
                            continue;
                        int newScore =newTemplate.GetTechScore(out int[] newTech);

                        if(newScore <= techScore || !TechCompare(origTechs, newTech)) continue;                        

                        name      = shipName;
                        techScore = newScore;
                        origTechs = newTech;
                    }
                    if (!string.IsNullOrEmpty(name))
                    {
                        ship.AI.OrderRefitTo(name);
                        ++upgrades;
                    }                    
                }
                else
                    break;
            }
        }

        public void Update()
        {		    
            DefStr = this.DefensiveCoordinator.GetForcePoolStrength();
            if (!this.empire.isFaction)
            {
                this.RunManagers();
            }
            foreach (Goal g in this.Goals)
            //Parallel.ForEach(this.Goals, g =>
            {
                g.Evaluate();
            }//);
            this.Goals.ApplyPendingRemovals();
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~GSAI() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            TaskList?.Dispose(ref TaskList);
            DefensiveCoordinator?.Dispose(ref DefensiveCoordinator);
            Goals?.Dispose(ref Goals);
        }
    }
}