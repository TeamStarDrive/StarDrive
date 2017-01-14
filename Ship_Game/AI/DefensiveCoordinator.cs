using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
	public sealed class DefensiveCoordinator: IDisposable
	{
		private Empire Us;
		public Map<SolarSystem, SystemCommander> DefenseDict = new Map<SolarSystem, SystemCommander>();
		public BatchRemovalCollection<Ship> DefensiveForcePool = new BatchRemovalCollection<Ship>();
        public float DefenseDeficit;        
        public float EmpireTroopRatio;
        public float UniverseWants;
		public DefensiveCoordinator(Empire e)
		{
            Us = e;
		}

        //added by gremlin parallel forcepool
        public float GetForcePoolStrength()
        {            
            int strength = 0;

            for (var index = 0; index < DefensiveForcePool.Count; index++)
            {
                Ship ship = DefensiveForcePool[index];
                if (!ship.Active || ship.dying) continue;
                strength += (int) ship.GetStrength();
            }
            return strength;
        }

	    public float GetDefensiveThreatFromPlanets(Array<Planet> planets)
	    {
	        if (DefenseDict.Count == 0) return 0;
            float total = 0f;
            int count = 0;            
	        for (var index = 0; index < planets.Count; index++)
	        {
	            Planet planet = planets[index];
                if (DefenseDict.TryGetValue(planet.system, out SystemCommander temp))
                {
                    total += temp.RankImportance;
                    count++;
                    
                }
	        }
            return total / count;
        }

		public float GetPctOfForces(SolarSystem system)
		{
			return DefenseDict[system].GetOurStrength() / GetForcePoolStrength();
		}

		public float GetPctOfValue(SolarSystem system)
		{
			return DefenseDict[system].PercentageOfValue;
		}

        public void Remove(Ship ship)
        {
            this.DefensiveForcePool.Remove(ship);
            foreach (KeyValuePair<SolarSystem, SystemCommander> entry in DefenseDict)
            {
                var item = entry.Value.ShipsDict.FirstOrDefault(kvp => kvp.Value == ship);                
                if (item.Value == ship && entry.Value.ShipsDict.TryRemove(item.Key, value: out Ship removed))
                {
                    item.Value.GetAI().SystemToDefend = null;
                    break;
                }
            }
        }

        public void ManageForcePool()
        {

            #region Figure defensive importance
            foreach (Planet p in Ship.universeScreen.PlanetsDict.Values)
            {
                if (p.Owner != Us && !p.EventsOnBuildings() && !p.TroopsHereAreEnemies(Us))
                {
                    p.TroopsHere.ApplyPendingRemovals();
                    foreach (Troop troop in p.TroopsHere)
                    {
                        if (troop.GetOwner() != Us) continue;
                        p.TroopsHere.QueuePendingRemoval(troop);
                        troop.Launch();
                    }
                    p.TroopsHere.ApplyPendingRemovals();
                }
            }
        
            foreach (Planet p in Us.GetPlanets())
            {
                if (p?.system == null || DefenseDict.ContainsKey(p.system)) continue;

                DefenseDict.Add(p.system, new SystemCommander(Us, p.system));
            }

            Array<SolarSystem> Keystoremove = new Array<SolarSystem>();
            foreach (KeyValuePair<SolarSystem, SystemCommander> entry in DefenseDict)
            {
                if (entry.Key.OwnerList.Contains(Us))
                {
                    entry.Value.updatePlanetTracker();
                    continue;
                }
                Keystoremove.Add(entry.Key);

            }
            foreach (SolarSystem key in Keystoremove)
            {
                foreach (var kv in DefenseDict[key].ShipsDict)
                {
                    if (kv.Value.System == kv.Value.GetAI().SystemToDefend)
                        kv.Value.GetAI().SystemToDefend = null;
                }
                DefenseDict[key].ShipsDict.Clear();
                DefenseDict.Remove(key);
            }
            float TotalValue = 0f;
            Array<SolarSystem> systems = new Array<SolarSystem>();
            foreach (KeyValuePair<SolarSystem, SystemCommander> entry in DefenseDict)
            {
                systems.Add(entry.Key);
                entry.Value.ValueToUs = 0f;
                entry.Value.IdealShipStrength = 0f;
                entry.Value.PercentageOfValue = 0f;
                foreach (Planet p in entry.Key.PlanetList)
                {
                    if (p.Owner != null && p.Owner == this.Us)
                    {
                        float cummulator = 0;
                        cummulator += p.Population / 10000f;                        
                        cummulator += (p.MaxPopulation / 10000f);                        
                        cummulator += p.Fertility;
                        cummulator += p.MineralRichness;
                        cummulator += p.BuildingList.Where(commodity => commodity.IsCommodity).Count();                        
                        cummulator += p.developmentLevel;
                        cummulator += p.GovBuildings ? 1 : 0;                        
                        cummulator += entry.Value.system.combatTimer > 0 ? 5 : 0;  //fbedard: DangerTimer is in relation to the player only !

                        if (Us.data.Traits.Cybernetic > 0) cummulator += p.MineralRichness;

                        cummulator += p.HasShipyard ? 5 : 0;
                        entry.Value.ValueToUs = cummulator;
                        entry.Value.planetTracker[p].value = cummulator;

                    }
                    foreach (Planet other in entry.Key.PlanetList)
                    {
                        if (other?.Owner == null || other.Owner == Us) continue;

                        var relation = Us.GetRelations(other.Owner);

                        if (relation.Trust < 50f)       entry.Value.ValueToUs += 2.5f;                        
                        if (relation.Trust < 10f)       entry.Value.ValueToUs += 2.5f;
                        if (relation.TotalAnger > 2.5f) entry.Value.ValueToUs += 2.5f;                        
                        if (relation.TotalAnger <= 30f) continue;                        
                        entry.Value.ValueToUs += 2.5f;
                        
                    }
                }
                foreach (SolarSystem fiveClosestSystem in entry.Key.FiveClosestSystems)                
                {
                    bool flag = false; 
                    foreach (var kv in Us.AllRelations)
                    {
                        if (!flag && fiveClosestSystem.OwnerList.Count == 0) flag = true;
                        
                        if (!flag && fiveClosestSystem.OwnerList.Contains(Us)) flag = false;
                        
                        if (!fiveClosestSystem.OwnerList.Contains(kv.Key)) continue;
                        
                        if (kv.Value.AtWar)
                        {
                            entry.Value.ValueToUs += 5f;
                            flag = true;
                            continue;
                        }
                        if (!kv.Value.Treaty_OpenBorders)
                        {
                            entry.Value.ValueToUs += 1f;
                            flag = true;
                        }

                    }
                    if (!flag & entry.Value.ValueToUs > 5) entry.Value.ValueToUs -= 2.5f;

                }

            }
            int ranker = 0;
            int split = (int)(this.DefenseDict.Count * .10f);
            int splitStore = split;
            var SComs = DefenseDict.OrderBy(value => value.Value.PercentageOfValue).ThenBy(devlev => devlev.Value.SystemDevelopmentlevel);
            foreach (var kv in SComs)
            {
                split--;
                if (split <= 0)
                {
                    ranker++;
                    split = splitStore;
                    if (ranker > 10) ranker = 10;

                }
                kv.Value.RankImportance = ranker;
            }
            foreach (var kv in SComs)
            {
                kv.Value.RankImportance = 10 * (kv.Value.RankImportance / ranker);
                TotalValue = TotalValue + kv.Value.ValueToUs;
            }
            #endregion
            #region Manage Ships                   
            int StrToAssign = (int)GetForcePoolStrength();
            float StartingStr = StrToAssign;
            foreach (var dd in SComs)
            {
                SolarSystem solarSystem = dd.Key;
                {
                    float Predicted = solarSystem.GetPredictedEnemyPresence(120f, this.Us);
                    if (Predicted <= 0f)
                    {
                        dd.Value.IdealShipStrength = 0f;
                    }
                    else
                    {
                        dd.Value.IdealShipStrength = Predicted * (dd.Value.RankImportance / 10);
                        float min = (float)Math.Pow((double)dd.Value.ValueToUs, 3) * dd.Value.RankImportance;
                        dd.Value.IdealShipStrength += min;

                        Interlocked.Add(ref StrToAssign, -(int)dd.Value.IdealShipStrength);

                    }
                }
            }
            
            DefenseDeficit = StrToAssign * -1;
            if (StrToAssign < 0f) StrToAssign = 0;
            
            foreach (KeyValuePair<SolarSystem, SystemCommander> entry in DefenseDict)
            {
                entry.Value.PercentageOfValue = entry.Value.ValueToUs / TotalValue;
                float min = StrToAssign * entry.Value.PercentageOfValue; 
                if (entry.Value.IdealShipStrength < min) entry.Value.IdealShipStrength = min;

            }
            
            Map<Guid, Ship> AssignedShips = new Map<Guid, Ship>();
            Array<Ship> ShipsAvailableForAssignment = new Array<Ship>();
            //Remove excess force:
            foreach (KeyValuePair<SolarSystem, SystemCommander> defenseDict in this.DefenseDict)
            {

                if (defenseDict.Value.GetOurStrength() <= defenseDict.Value.IdealShipStrength ) continue;
                
                var ships = defenseDict.Value.GetShipList();
                ships.Sort((x,y) => x.GetStrength().CompareTo(y.GetStrength()));
                foreach (Ship current in ships)
                {
                    defenseDict.Value.ShipsDict.TryRemove(current.guid, out Ship remove);
                    ShipsAvailableForAssignment.Add(current);
                    current.GetAI().SystemToDefend = null;                

                    if (defenseDict.Value.GetOurStrength() <= defenseDict.Value.IdealShipStrength) break;

                }
            }
            //Add available force to pool:
            foreach (Ship defensiveForcePool in this.DefensiveForcePool)
            {
                if (!(defensiveForcePool.GetAI().HasPriorityOrder || defensiveForcePool.GetAI().State == AIState.Resupply )
                    && defensiveForcePool.loyalty == Us)
                    ShipsAvailableForAssignment.Add(defensiveForcePool);

                else DefensiveForcePool.QueuePendingRemoval(defensiveForcePool);
                
            }
            //Assign available force:
            foreach (var kv in this.DefenseDict)
            {
                if (kv.Key == null) continue;
                
                kv.Value.AssignTargets();
            }
            if (ShipsAvailableForAssignment.Count > 0)
            {
                foreach (KeyValuePair<SolarSystem, SystemCommander> SCCOMS in SComs.OrderByDescending(descending => descending.Value.RankImportance))
                {
                    SolarSystem solarSystem1 = SCCOMS.Key;
                    if (StartingStr < 0f) break;                    
                
                    foreach (Ship ship1 in ShipsAvailableForAssignment.OrderBy(ship => Vector2.Distance(ship.Center, solarSystem1.Position)))
                    {
                        if (ship1.GetAI().State == AIState.Resupply || (ship1.GetAI().State == AIState.SystemDefender && ship1.GetAI().SystemToDefend != null) )                        
                            continue;
                        
                        if (ship1.Active)
                        {
                            if (AssignedShips.ContainsKey(ship1.guid)) continue;
                            
                            if (StartingStr <= 0f || StrengthOf(SCCOMS.Value.ShipsDict) >= SCCOMS.Value.IdealShipStrength) break;
                            
                            AssignedShips.Add(ship1.guid, ship1);

                            if (SCCOMS.Value.ShipsDict.ContainsKey(ship1.guid)) continue;
                            
                            SCCOMS.Value.ShipsDict.TryAdd(ship1.guid, ship1);
                            StartingStr = StartingStr - ship1.GetStrength();
     
                            ship1.GetAI().OrderSystemDefense(solarSystem1);
                        }
                        else                        
                            DefensiveForcePool.QueuePendingRemoval(ship1);
                        
                    }
                }
            }
            this.DefensiveForcePool.ApplyPendingRemovals();


            #endregion
            #region Manage Troops
            if (Us.isPlayer)
            {
                bool haveMilGov = false;
                foreach (Planet planet in Us.GetPlanets())
                {
                    if (planet.colonyType != Planet.ColonyType.Military)
                        continue;

                    haveMilGov = true;
                    break;                        
                }
                if (!haveMilGov) return;
            }
            BatchRemovalCollection<Ship> TroopShips = new BatchRemovalCollection<Ship>();
            BatchRemovalCollection<Troop> GroundTroops = new BatchRemovalCollection<Troop>();

            foreach (Planet p in this.Us.GetPlanets())            
                for (int i = 0; i < p.TroopsHere.Count; i++)                
                    if (p.TroopsHere[i].Strength > 0 && p.TroopsHere[i].GetOwner() == Us)                   
                        GroundTroops.Add(p.TroopsHere[i]);


            foreach (Ship ship2 in this.Us.GetShips())
            {
                if (ship2.shipData.Role != ShipData.RoleName.troop || ship2.fleet != null || ship2.Mothership != null || ship2.GetAI().HasPriorityOrder)
                    continue;

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
                    if (ship3.TroopList[i].GetOwner() == Us)
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
                var planets = entry.Key.PlanetList.Where(planet => planet.Owner == Us).ToArray();
                int planetCount = planets.Length;
                int developmentlevel = planets.Sum(development => development.developmentLevel);
                entry.Value.SystemDevelopmentlevel = developmentlevel;
                int maxtroops = entry.Key.PlanetList.Where(planet => planet.Owner == Us).Sum(planet => planet.GetPotentialGroundTroops());
                entry.Value.IdealTroopStr = (mintroopLevel + entry.Value.RankImportance) * planetCount;

                if (entry.Value.IdealTroopStr > maxtroops)
                    entry.Value.IdealTroopStr = maxtroops;
                totalTroopWanted += (int)entry.Value.IdealTroopStr;
                int currentTroops = entry.Key.PlanetList.Where(planet => planet.Owner == Us).Sum(planet => planet.GetDefendingTroopCount());
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
                        && troopAI.OrderQueue.NotEmpty
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
                        if (this.Us.isPlayer && p.colonyType != Planet.ColonyType.Military)
                            continue;
                        float devratio = (float)(p.developmentLevel + 1) / (defenseSystem.Value.SystemDevelopmentlevel + 1);
                        if (!defenseSystem.Key.CombatInSystem
                            && p.GetDefendingTroopCount() > defenseSystem.Value.IdealTroopStr * devratio)// + (int)Ship.universeScreen.GameDifficulty)
                        {
                           
                            Troop l = p.TroopsHere.FirstOrDefault(loyalty => loyalty.GetOwner() == this.Us);
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
                    bases => bases.BaseStrength > 0 && bases.loyalty != Us && 
                    (bases.loyalty.isFaction || Us.GetRelations(bases.loyalty).AtWar || 
                    !Us.GetRelations(bases.loyalty).Treaty_OpenBorders)).ToArray() 
                : Us.FindShipsInOurBorders().Where(bases=> bases.BaseStrength >0).ToArray();
            


            if (incomingShips.Length == 0)
            {
                
                return;
            }

            Array<Ship> ShipsAlreadyConsidered = new Array<Ship>();
            var rangePartitioner = Partitioner.Create(0, incomingShips.Length);
            System.Threading.Tasks.Parallel.ForEach(rangePartitioner, (range, loopState) =>
            {
                    
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    //for (int i = 0; i < incomingShips.Count; i++)
                    {
                        //Ship ship = this.system.ShipList[i];
                        Ship ship = incomingShips[i];

                        if (ship != null && ship.loyalty != this.Us
                            && (ship.loyalty.isFaction || this.Us.GetRelations(ship.loyalty).AtWar || !this.Us.GetRelations(ship.loyalty).Treaty_OpenBorders)
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
                                if (otherShip.loyalty != this.Us && otherShip.loyalty == ship.loyalty && Vector2.Distance(ship.Center, otherShip.Center) < 15000f
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
            DefensiveForcePool?.Dispose(ref DefensiveForcePool);
        }
	}
}