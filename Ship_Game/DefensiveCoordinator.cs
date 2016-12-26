using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
namespace Ship_Game
{
	public sealed class DefensiveCoordinator: IDisposable
	{
		private Empire us;

		public Dictionary<SolarSystem, SystemCommander> DefenseDict = new Dictionary<SolarSystem, SystemCommander>();

		public BatchRemovalCollection<Ship> DefensiveForcePool = new BatchRemovalCollection<Ship>();
        public float defenseDeficit = 0;

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;
        public float EmpireTroopRatio;
        public float UniverseWants;
		public DefensiveCoordinator(Empire e)
		{
			this.us = e;
		}

		public float GetForcePoolStrengthORIG()
		{
			float str = 0f;
			foreach (Ship ship in this.DefensiveForcePool)
			{
				if (!ship.Active)
				{
					continue;
				}
				str = str + ship.GetStrength();
			}
			return str;
		}
        //added by gremlin parallel forcepool
        public float GetForcePoolStrength()
        {


            int strength = 0;
            //Parallel.ForEach(this.DefensiveForcePool, ship =>
            //{
            //    int shipStr = (int)ship.GetStrength();
            //    Interlocked.Add(ref strength, shipStr);

            //    //safeadd  //SafeAddFloat(ref Strength, shipStr);       ßInterlocked
            //});
            

            foreach(Ship ship in this.DefensiveForcePool)
            {
                if(!ship.Active || ship.dying)
                {
                    continue;
                }
                strength += (int)ship.GetStrength();
            }
            return (float)strength;

        }

        public float GetDefensiveThreatFromPlanets(Array<Planet> planets)
        {
            if (this.DefenseDict.Count == 0)
                return 0;
            HashSet<SystemCommander> scoms = new HashSet<SystemCommander>();
            foreach(Planet planet in planets)
            {
                SystemCommander temp =null;
                if(this.DefenseDict.TryGetValue(planet.system,out temp))
                {
                    scoms.Add(temp);
                }

            }
            return scoms.Average(defense => defense.RankImportance);
        }

		public float GetPctOfForces(SolarSystem system)
		{
			return this.DefenseDict[system].GetOurStrength() / this.GetForcePoolStrength();
		}

		public float GetPctOfValue(SolarSystem system)
		{
			return this.DefenseDict[system].PercentageOfValue;
		}

        public void remove(Ship ship)
        {
            Ship removed = null;
            KeyValuePair<Guid,Ship> item;
            this.DefensiveForcePool.Remove(ship);
            foreach (KeyValuePair<SolarSystem, SystemCommander> entry in this.DefenseDict)
            {
                 item=entry.Value.ShipsDict.FirstOrDefault(kvp=> kvp.Value == ship);

                 if (item.Value == ship && entry.Value.ShipsDict.TryRemove(item.Key, out removed))
                {
                    item.Value.GetAI().SystemToDefend = null;
    
                    break;
                }

                
            }
        }

        public void ManageForcePool()
        {

            #region Figure defensive importance
            foreach (Planet p in Ship.universeScreen.PlanetsDict.Values)  //this.us.GetPlanets())
            //Parallel.ForEach(Ship.universeScreen.PlanetsDict.Values, p =>
           {
               if (p.Owner != us && !p.EventsOnBuildings() && !p.TroopsHereAreEnemies(this.us))
               {
                   p.TroopsHere.ApplyPendingRemovals();
                   foreach (Troop troop in p.TroopsHere.Where(loyalty => loyalty.GetOwner() == this.us))
                   {
                       p.TroopsHere.QueuePendingRemoval(troop);
                       troop.Launch();
                   }
                   p.TroopsHere.ApplyPendingRemovals();
               }
           }//);
        
            foreach (Planet p in this.us.GetPlanets())
            {
                if (p == null || p.system == null || this.DefenseDict.ContainsKey(p.system))
                {
                    continue;
                }
                this.DefenseDict.Add(p.system, new SystemCommander(this.us, p.system));
            }

            Array<SolarSystem> Keystoremove = new Array<SolarSystem>();
            foreach (KeyValuePair<SolarSystem, SystemCommander> entry in this.DefenseDict)
            {
                if (entry.Key.OwnerList.Contains(this.us))
                {
                    entry.Value.updatePlanetTracker();
                    continue;
                }
                Keystoremove.Add(entry.Key);

            }
            foreach (SolarSystem key in Keystoremove)
            {
                foreach (KeyValuePair<Guid, Ship> entry in this.DefenseDict[key].ShipsDict)
                {
                    if (entry.Value.System== entry.Value.GetAI().SystemToDefend)
                        entry.Value.GetAI().SystemToDefend = null;
                }
                this.DefenseDict[key].ShipsDict.Clear();
                this.DefenseDict.Remove(key);
            }
            float TotalValue = 0f;
            Array<SolarSystem> systems = new Array<SolarSystem>();
            foreach (KeyValuePair<SolarSystem, SystemCommander> entry in this.DefenseDict)
            {
                systems.Add(entry.Key);
                entry.Value.ValueToUs = 0f;
                entry.Value.IdealShipStrength = 0f;
                entry.Value.PercentageOfValue = 0f;
                foreach (Planet p in entry.Key.PlanetList)
                {
                    if (p.Owner != null && p.Owner == this.us)
                    {
                        float cummulator = 0;
                        SystemCommander value = entry.Value;
                        cummulator += p.Population / 10000f;
                        SystemCommander valueToUs = entry.Value;
                        cummulator += (p.MaxPopulation / 10000f);// - p.Population / 10000f);
                        SystemCommander systemCommander = entry.Value;
                        cummulator += p.Fertility;
                        SystemCommander value1 = entry.Value;
                        //added by gremlin commodities increase defense desire
                        cummulator += p.MineralRichness;
                        cummulator += p.BuildingList.Where(commodity => commodity.IsCommodity).Count();
                        //value.ValueToUs += p.CombatTimer >0 ? p.CombatTimer :0;
                        cummulator += p.developmentLevel;
                        cummulator += p.GovBuildings ? 1 : 0;
                        //cummulator += entry.Value.system.DangerTimer > 0 ? 5 : 0;
                        cummulator += entry.Value.system.combatTimer > 0 ? 5 : 0;  //fbedard: DangerTimer is in relation to the player only !

                        if (this.us.data.Traits.Cybernetic > 0)
                        {
                            cummulator += p.MineralRichness;
                        }
                        cummulator += p.HasShipyard ? 5 : 0;
                        entry.Value.ValueToUs = cummulator;
                        entry.Value.planetTracker[p].value = cummulator;

                    }
                    foreach (Planet other in entry.Key.PlanetList)
                    {
                        if (other == p || other.Owner == null || other.Owner == this.us)
                        {
                            continue;
                        }
                        if (this.us.GetRelations(other.Owner).Trust < 50f)
                        {
                            SystemCommander valueToUs1 = entry.Value;
                            valueToUs1.ValueToUs = valueToUs1.ValueToUs + 2.5f;
                        }
                        if (this.us.GetRelations(other.Owner).Trust < 10f)
                        {
                            SystemCommander systemCommander1 = entry.Value;
                            systemCommander1.ValueToUs = systemCommander1.ValueToUs + 2.5f;
                        }
                        if (this.us.GetRelations(other.Owner).TotalAnger > 2.5f)
                        {
                            SystemCommander value2 = entry.Value;
                            value2.ValueToUs = value2.ValueToUs + 2.5f;
                        }
                        if (this.us.GetRelations(other.Owner).TotalAnger <= 30f)
                        {
                            continue;
                        }
                        SystemCommander valueToUs2 = entry.Value;
                        valueToUs2.ValueToUs = valueToUs2.ValueToUs + 2.5f;
                    }
                }
                foreach (SolarSystem fiveClosestSystem in entry.Key.FiveClosestSystems)                
                {
                    bool flag = false; ;
                    foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.us.AllRelations)
                    {
                        if (!flag && fiveClosestSystem.OwnerList.Count == 0)
                        {
                            flag = true;
                        }
                        if (!flag && fiveClosestSystem.OwnerList.Contains(this.us))
                            flag = false;

                        if (!fiveClosestSystem.OwnerList.Contains(Relationship.Key))
                        {


                            continue;
                        }

                        if (Relationship.Value.AtWar)
                        {
                            entry.Value.ValueToUs += 5f;
                            flag = true;
                            continue;
                        }
                        if (!Relationship.Value.Treaty_OpenBorders)
                        {
                            entry.Value.ValueToUs += 1f;
                            flag = true;
                        }

                    }
                    if (!flag & entry.Value.ValueToUs > 5)
                        entry.Value.ValueToUs -= 2.5f;
                }

            }
            int ranker = 0;
            int split = (int)(this.DefenseDict.Count * .10f);
            int splitStore = split;
            IOrderedEnumerable<KeyValuePair<SolarSystem, SystemCommander>> SComs = this.DefenseDict.OrderBy(value => value.Value.PercentageOfValue).ThenBy(devlev => devlev.Value.SystemDevelopmentlevel);
            foreach (KeyValuePair<SolarSystem, SystemCommander> SCCom in SComs)
            {

                split--;
                if (split <= 0)
                {
                    ranker++;
                    split = splitStore;
                    if (ranker > 10)
                        ranker = 10;
                }
                SCCom.Value.RankImportance = ranker;
            }
            foreach (KeyValuePair<SolarSystem, SystemCommander> SCCom in SComs)
            {
                SCCom.Value.RankImportance = 10 * (SCCom.Value.RankImportance / ranker);
                TotalValue = TotalValue + SCCom.Value.ValueToUs;
            }
            #endregion
            #region Manage Ships
            //IOrderedEnumerable<SolarSystem> sortedList =
            //        from system in systems
            //        orderby system.GetPredictedEnemyPresence(60f, this.us) descending
            //        select system;                    
            int StrToAssign = (int)this.GetForcePoolStrength();
            float StartingStr = StrToAssign;
            //float minimumStrength = 0;
            Parallel.ForEach(SComs, dd=>
                {
                    SolarSystem solarSystem  = dd.Key;
                    {
                        float Predicted = solarSystem.GetPredictedEnemyPresence(120f, this.us);
                        if (Predicted <= 0f)
                        {
                            dd.Value.IdealShipStrength = 0f;
                        }
                        else
                        {
                            dd.Value.IdealShipStrength = Predicted * (dd.Value.RankImportance / 10);
                            float min = (float)Math.Pow((double)dd.Value.ValueToUs,3) * dd.Value.RankImportance ;
                            dd.Value.IdealShipStrength += min;

                            Interlocked.Add(ref  StrToAssign, -(int)dd.Value.IdealShipStrength);

                        }
                    }
                });
            
            //foreach (SolarSystem solarSystem in sortedList)
            //{
            //    float Predicted = solarSystem.GetPredictedEnemyPresence(120f, this.us);
            //    if (Predicted <= 0f)
            //    {
            //        this.DefenseDict[solarSystem].IdealShipStrength = 0f;
            //    }
            //    else
            //    {
            //        this.DefenseDict[solarSystem].IdealShipStrength = Predicted * (this.DefenseDict[solarSystem].RankImportance /10);
            //        StrToAssign = StrToAssign - Predicted;
            //    }
            //}
            this.defenseDeficit = StrToAssign * -1;
            if (StrToAssign < 0f)
            {
                StrToAssign = 0;
            }

            foreach (KeyValuePair<SolarSystem, SystemCommander> entry in this.DefenseDict)
            {
                entry.Value.PercentageOfValue = entry.Value.ValueToUs / TotalValue;
                float min = StrToAssign * entry.Value.PercentageOfValue; // (entry.Value.RankImportance / (10 * this.DefenseDict.Count));
                if (entry.Value.IdealShipStrength < min)
                    entry.Value.IdealShipStrength = min;
                
            }
            
            Dictionary<Guid, Ship> AssignedShips = new Dictionary<Guid, Ship>();
            Array<Ship> ShipsAvailableForAssignment = new Array<Ship>();
            //Remove excess force:
            foreach (KeyValuePair<SolarSystem, SystemCommander> defenseDict in this.DefenseDict)
            {

                if (defenseDict.Value.GetOurStrength() <= defenseDict.Value.IdealShipStrength )
                {
                    continue;
                }

                var ships = defenseDict.Value.GetShipList();
                ships.Sort((x,y) => x.GetStrength().CompareTo(y.GetStrength()));
                foreach (Ship current in ships)
                {
                    defenseDict.Value.ShipsDict.TryRemove(current.guid, out Ship remove);
                    ShipsAvailableForAssignment.Add(current);
                    current.GetAI().SystemToDefend = null;
                    //current.GetAI().State = AIState.AwaitingOrders;

                    if (defenseDict.Value.GetOurStrength() <= defenseDict.Value.IdealShipStrength)
                        break;
                }
            }
            //Add available force to pool:
            foreach (Ship defensiveForcePool in this.DefensiveForcePool)
            {
                if (!(defensiveForcePool.GetAI().HasPriorityOrder || defensiveForcePool.GetAI().State == AIState.Resupply )
                    && defensiveForcePool.loyalty == this.us)
                {
                    //if (defensiveForcePool.GetAI().SystemToDefend == defensiveForcePool.GetSystem()
                    //    || defensiveForcePool.GetAI().State == AIState.SystemDefender
                    //    )
                    //{
                    //    continue;
                    //}                    
                    ShipsAvailableForAssignment.Add(defensiveForcePool);
                }
                else
                {
                    this.DefensiveForcePool.QueuePendingRemoval(defensiveForcePool);
                }
            }
            //Assign available force:
            foreach (KeyValuePair<SolarSystem, SystemCommander> entry in this.DefenseDict)
            {
                if (entry.Key == null)
                {
                    continue;
                }
                entry.Value.AssignTargets();
            }
            //IOrderedEnumerable<SolarSystem> valueSortedList =
            //    from system in systems
            //    orderby this.DefenseDict[system].IdealShipStrength - this.DefenseDict[system].GetOurStrength() descending
            //    select system;
            if (ShipsAvailableForAssignment.Count > 0)
            {
                foreach (KeyValuePair<SolarSystem, SystemCommander> SCCOMS in SComs.OrderByDescending(descending => descending.Value.RankImportance))
                {
                    SolarSystem solarSystem1 = SCCOMS.Key;
                    if (StartingStr < 0f)
                    {
                        break;
                    }
                    //IOrderedEnumerable<Ship> distanceSorted =
                        //from ship in ShipsAvailableForAssignment
                        //orderby Vector2.Distance(ship.Center, solarSystem1.Position)
                        //select ship;
                    foreach (Ship ship1 in ShipsAvailableForAssignment.OrderBy(ship => Vector2.Distance(ship.Center, solarSystem1.Position)))
                    {
                        if (ship1.GetAI().State == AIState.Resupply || (ship1.GetAI().State == AIState.SystemDefender && ship1.GetAI().SystemToDefend != null) )
                        {
                            continue;
                        }
                        if (ship1.Active)
                        {
                            if (AssignedShips.ContainsKey(ship1.guid))
                            {
                                continue;
                            }
                            //var t = this.DefenseDict[solarSystem1].ShipsDict;
                            if (StartingStr <= 0f || this.StrengthOf(SCCOMS.Value.ShipsDict) >= SCCOMS.Value.IdealShipStrength)
                            {

                                break;
                            }
                            AssignedShips.Add(ship1.guid, ship1);
                            if (SCCOMS.Value.ShipsDict.ContainsKey(ship1.guid))
                            {
                                continue;
                            }
                            SCCOMS.Value.ShipsDict.TryAdd(ship1.guid, ship1);
                            StartingStr = StartingStr - ship1.GetStrength();
     
                            ship1.GetAI().OrderSystemDefense(solarSystem1);
                        }
                        else
                        {
                            this.DefensiveForcePool.QueuePendingRemoval(ship1);
                        }
                    }
                }
            }
            this.DefensiveForcePool.ApplyPendingRemovals();


            #endregion
            #region Manage Troops
            if (this.us.isPlayer)
            {
                bool flag = false;

                foreach(Planet planet in this.us.GetPlanets())
                {
                    if(planet.colonyType == Planet.ColonyType.Military)
                        flag=true;
                }
                if(!flag)
                return;
            }
            BatchRemovalCollection<Ship> TroopShips = new BatchRemovalCollection<Ship>();
            BatchRemovalCollection<Troop> GroundTroops = new BatchRemovalCollection<Troop>();
            foreach (Planet p in this.us.GetPlanets())
            {
                for (int i = 0; i < p.TroopsHere.Count; i++)
                {
                    if (p.TroopsHere[i].Strength > 0 && p.TroopsHere[i].GetOwner() == this.us)//&& !p.RecentCombat && p.ParentSystem.combatTimer <=0)
                    {
                        GroundTroops.Add(p.TroopsHere[i]);
                    }
                }
            }
            foreach (Ship ship2 in this.us.GetShips())
            {
                if (ship2.shipData.Role != ShipData.RoleName.troop || ship2.fleet != null || ship2.Mothership != null || ship2.GetAI().HasPriorityOrder) //|| ship2.GetAI().State != AIState.AwaitingOrders)
                {
                    continue;
                }
                TroopShips.Add(ship2);

            }
            float TotalTroopStrength = 0f;
            foreach (Troop t in GroundTroops)
            {
                TotalTroopStrength = TotalTroopStrength + t.Strength;
            }
            foreach (Ship ship3 in TroopShips)
            {
                for (int i = 0; i < ship3.TroopList.Count; i++)
                {
                    if (ship3.TroopList[i].GetOwner() == us)
                    {
                        TotalTroopStrength += ship3.TroopList[i].Strength;
                    }
                }
            }
            int mintroopLevel = (int)(Ship.universeScreen.GameDifficulty + 1) * 2;
            int totalTroopWanted = 0;
            int totalCurrentTroops = 0;
            foreach (KeyValuePair<SolarSystem, SystemCommander> entry in DefenseDict)
            {
                // find max number of troops for system.
                var planets = entry.Key.PlanetList.Where(planet => planet.Owner == us).ToArray();
                int planetCount = planets.Length;
                int developmentlevel = planets.Sum(development => development.developmentLevel);
                entry.Value.SystemDevelopmentlevel = developmentlevel;
                int maxtroops = entry.Key.PlanetList.Where(planet => planet.Owner == us).Sum(planet => planet.GetPotentialGroundTroops());
                entry.Value.IdealTroopStr = (mintroopLevel + entry.Value.RankImportance) * planetCount;

                if (entry.Value.IdealTroopStr > maxtroops)
                    entry.Value.IdealTroopStr = maxtroops;
                totalTroopWanted += (int)entry.Value.IdealTroopStr;
                int currentTroops = entry.Key.PlanetList.Where(planet => planet.Owner == us).Sum(planet => planet.GetDefendingTroopCount());
                totalCurrentTroops += currentTroops;

                entry.Value.TroopStrengthNeeded = entry.Value.IdealTroopStr - currentTroops;
                GroundTroops.ApplyPendingRemovals();

                for (int i = 0; i < TroopShips.Count; i++)
                {
                    Ship troop = TroopShips[i];

                    if (troop == null || troop.TroopList.Count <= 0)
                    {
                        TroopShips.QueuePendingRemoval(troop);
                        continue;
                    }

                    ArtificialIntelligence troopAI = troop.GetAI();
                    if (troopAI == null)
                    {
                        TroopShips.QueuePendingRemoval(troop);
                        continue;
                    }
                    if (troopAI.State == AIState.Rebase
                        && troopAI.OrderQueue.Count > 0
                        && troopAI.OrderQueue.Any(goal => goal.TargetPlanet != null && entry.Key == goal.TargetPlanet.system))
                    {
                        currentTroops++;
                        entry.Value.TroopStrengthNeeded--;
                        TroopShips.QueuePendingRemoval(troop);
                    }

                    if (entry.Value.TroopStrengthNeeded < 0)
                    {

                    }
                }
                TroopShips.ApplyPendingRemovals();
            }
            this.UniverseWants = totalCurrentTroops / (float)totalTroopWanted;
            //Planet tempPlanet = null;          //Not referenced in code, removing to save memory
            //int TroopsSent = 0;          //Not referenced in code, removing to save memory
            foreach (Ship ship4 in TroopShips)
            {


                IOrderedEnumerable<SolarSystem> sortedSystems =
                    from system in systems
                    //where !system.CombatInSystem 
                    orderby this.DefenseDict[system].TroopStrengthNeeded / this.DefenseDict[system].IdealTroopStr descending
                    orderby (int)(Vector2.Distance(system.Position, ship4.Center) / (UniverseData.UniverseWidth / 5f))
                    orderby this.DefenseDict[system].ValueToUs descending

                    select system;
                foreach (SolarSystem solarSystem2 in sortedSystems)
                {

                    if (solarSystem2.PlanetList.Count <= 0)
                    {
                        continue;
                    }
                    SystemCommander defenseSystem = this.DefenseDict[solarSystem2];

                    if (defenseSystem.TroopStrengthNeeded <= 0)
                        continue;
                    defenseSystem.TroopStrengthNeeded--;
                    TroopShips.QueuePendingRemoval(ship4);


                    //send troops to the first planet in the system with the lowest troop count.
                    //Planet target = solarSystem2.PlanetList.Where(planet => planet.Owner == ship4.loyalty)
                    //.OrderBy(planet => planet.GetDefendingTroopCount() < defenseSystem.IdealTroopStr / solarSystem2.PlanetList.Count * (planet.developmentLevel / defenseSystem.SystemDevelopmentlevel))
                    //.FirstOrDefault();
                    Planet target = null;
                    foreach(Planet lowTroops in solarSystem2.PlanetList )
                    {
                        if (lowTroops.Owner != ship4.loyalty)
                            continue;
                        if (target==null || lowTroops.TroopsHere.Count < target.TroopsHere.Count)
                            target = lowTroops;
                    }
                    if (target == null)
                        continue;
                    //if (target != tempPlanet)
                    //{
                    //    tempPlanet = target;
                    //    TroopsSent = 0;
                    //}

                    ship4.GetAI().OrderRebase(target, true);
                    //TroopShips.QueuePendingRemoval(ship4);


                }
            }
            TroopShips.ApplyPendingRemovals();
            //foreach (Ship Scraptroop in TroopShips)
            //{
            //    Scraptroop.GetAI().OrderScrapShip();
            //}

            //TroopShips.ApplyPendingRemovals();
            //Troop management is horked.
            // Since it doesnt keep track troop needs per planet the troops can not decide which planet to defend and so constantly launch and land.
            // so for now i am disabling the launch code when there are too many troops.
            // Troops will still rebase after they sit idle from fleet activity. 

            //float want = 0;          //Not referenced in code, removing to save memory
            //float ideal = 0;          //Not referenced in code, removing to save memory
            this.EmpireTroopRatio = UniverseWants;
            if (UniverseWants < .8f)
            {

                foreach (KeyValuePair<SolarSystem, SystemCommander> defenseSystem in this.DefenseDict)
                {
                    foreach (Planet p in defenseSystem.Key.PlanetList)
                    {
                        if (this.us.isPlayer && p.colonyType != Planet.ColonyType.Military)
                            continue;
                        float devratio = (float)(p.developmentLevel + 1) / (defenseSystem.Value.SystemDevelopmentlevel + 1);
                        if (!defenseSystem.Key.CombatInSystem
                            && p.GetDefendingTroopCount() > defenseSystem.Value.IdealTroopStr * devratio)// + (int)Ship.universeScreen.GameDifficulty)
                        {
                           
                            Troop l = p.TroopsHere.FirstOrDefault(loyalty => loyalty.GetOwner() == this.us);
                            l?.Launch();
                        }
                    }
                }
            }

        }
            #endregion
		
        public ConcurrentDictionary<Ship, Array<Ship>> EnemyClumpsDict = new ConcurrentDictionary<Ship, Array<Ship>>();

        public void refreshclumps()
        {
            this.EnemyClumpsDict.Clear();
     
            //Array<Ship> ShipsAlreadyConsidered = new Array<Ship>();
            



  
            Ship[] incomingShips = Empire.Universe.GameDifficulty > UniverseData.GameDifficulty.Hard 
                ? Empire.Universe.MasterShipList.AsParallel().Where(
                    bases => bases.BaseStrength > 0 && bases.loyalty != us && 
                    (bases.loyalty.isFaction || us.GetRelations(bases.loyalty).AtWar || 
                    !us.GetRelations(bases.loyalty).Treaty_OpenBorders)).ToArray() 
                : us.FindShipsInOurBorders().Where(bases=> bases.BaseStrength >0).ToArray();
            


            if (incomingShips.Length == 0)
            {
                
                return;
            }

            Array<Ship> ShipsAlreadyConsidered = new Array<Ship>();
            var rangePartitioner = Partitioner.Create(0, incomingShips.Length);
            Parallel.ForEach(rangePartitioner, (range, loopState) =>
            {
                    
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    //for (int i = 0; i < incomingShips.Count; i++)
                    {
                        //Ship ship = this.system.ShipList[i];
                        Ship ship = incomingShips[i];

                        if (ship != null && ship.loyalty != this.us
                            && (ship.loyalty.isFaction || this.us.GetRelations(ship.loyalty).AtWar || !this.us.GetRelations(ship.loyalty).Treaty_OpenBorders)
                            && !ShipsAlreadyConsidered.Contains(ship) && !this.EnemyClumpsDict.ContainsKey(ship))
                        {
                            //lock(this.EnemyClumpsDict)
                            this.EnemyClumpsDict.TryAdd(ship, new Array<Ship>());
                            this.EnemyClumpsDict[ship].Add(ship);
                            lock(ShipsAlreadyConsidered)
                            ShipsAlreadyConsidered.Add(ship);

                            for (int j = range.Item1; j < range.Item2; j++)
                            {
                                Ship otherShip = incomingShips[j];
                                if (otherShip.loyalty != this.us && otherShip.loyalty == ship.loyalty && Vector2.Distance(ship.Center, otherShip.Center) < 15000f
                                    && !ShipsAlreadyConsidered.Contains(otherShip))
                                {
                                    this.EnemyClumpsDict[ship].Add(otherShip);
                                }
                            }
                        }

                    }
                }
            });
  
            
        }
		private float StrengthOf(ConcurrentDictionary<Guid, Ship> dict)
		{
			float str = 0f;
			foreach (KeyValuePair<Guid, Ship> entry in dict)
			{
				str = str + entry.Value.GetStrength();
			}
			return str;
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DefensiveCoordinator() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.DefensiveForcePool != null)
                        this.DefensiveForcePool.Dispose();

                }
                this.DefensiveForcePool = null;
                this.disposed = true;
            }
        }
	}
}