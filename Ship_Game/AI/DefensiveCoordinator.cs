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
		private readonly Empire _us;
		public Map<SolarSystem, SystemCommander> DefenseDict = new Map<SolarSystem, SystemCommander>();
		public BatchRemovalCollection<Ship> DefensiveForcePool = new BatchRemovalCollection<Ship>();
        public float DefenseDeficit;        
        private bool _disposed;
        public float EmpireTroopRatio;
        public float UniverseWants;
		public DefensiveCoordinator(Empire e)
		{
            _us = e;
		}
        
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

            var scoms = new HashSet<SystemCommander>();
	        for (var index = 0; index < planets.Count; index++)
	        {
	            Planet planet = planets[index];
	            if (DefenseDict.TryGetValue(planet.system, out SystemCommander temp))
	                scoms.Add(temp);
	        }
	        return scoms.Average(defense => defense.RankImportance);
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
                
#pragma warning disable 168
                if (item.Value == ship && entry.Value.ShipsDict.TryRemove(item.Key, value: out Ship removed))
#pragma warning restore 168
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
                if (p.Owner != _us && !p.EventsOnBuildings() && !p.TroopsHereAreEnemies(_us))
                {
                    p.TroopsHere.ApplyPendingRemovals();
                    foreach (Troop troop in p.TroopsHere.Where(loyalty => loyalty.GetOwner() == _us))
                    {
                        p.TroopsHere.QueuePendingRemoval(troop);
                        troop.Launch();
                    }
                    p.TroopsHere.ApplyPendingRemovals();
                }
            }

            foreach (Planet p in _us.GetPlanets())
            {
                if (p?.system == null || DefenseDict.ContainsKey(p.system)) continue;                
                DefenseDict.Add(p.system, new SystemCommander(_us, p.system));
            }

            Array<SolarSystem> keystoremove = new Array<SolarSystem>();
            foreach (KeyValuePair<SolarSystem, SystemCommander> entry in DefenseDict)
            {
                if (entry.Key.OwnerList.Contains(_us))
                {
                    entry.Value.updatePlanetTracker();
                    continue;
                }
                keystoremove.Add(entry.Key);

            }
            foreach (SolarSystem key in keystoremove)
            {
                foreach (var entry in DefenseDict[key].ShipsDict)
                {
                    if (entry.Value.System== entry.Value.GetAI().SystemToDefend)
                        entry.Value.GetAI().SystemToDefend = null;
                }
                DefenseDict[key].ShipsDict.Clear();
                DefenseDict.Remove(key);
            }
            float totalValue = 0f;
            var systems = new Array<SolarSystem>();
            foreach (var entry in DefenseDict)
            {
                systems.Add(entry.Key);
                entry.Value.ValueToUs = 0f;
                entry.Value.IdealShipStrength = 0f;
                entry.Value.PercentageOfValue = 0f;
                foreach (Planet p in entry.Key.PlanetList)
                {
                    if (p.Owner != null && p.Owner == _us)
                    {
                        float cummulator = 0;
                        cummulator += p.Population / 10000f;
                        cummulator += (p.MaxPopulation / 10000f);
                        cummulator += p.Fertility;
                        cummulator += p.MineralRichness;
                        cummulator += p.BuildingList.Count(commodity => commodity.IsCommodity);
                        cummulator += p.developmentLevel;
                        cummulator += p.GovBuildings ? 1 : 0;
                        cummulator += _us.GetGSAI().ThreatMatrix.PingRadarStr(p.Position,100000,_us) > 0 ? 5 : 0;  //fbedard: DangerTimer is in relation to the player only !

                        if (_us.data.Traits.Cybernetic > 0) cummulator += p.MineralRichness;
                        cummulator += p.HasShipyard ? 5 : 0;
                        entry.Value.ValueToUs = cummulator;
                        entry.Value.planetTracker[p].value = cummulator;

                    }
                    foreach (Planet other in entry.Key.PlanetList)
                    {
                        if (other == p || other.Owner == null || other.Owner == _us) continue;                      
                        if (_us.GetRelations(other.Owner).Trust < 50f) entry.Value.ValueToUs += 2.5f;
                        if (_us.GetRelations(other.Owner).Trust < 10f) entry.Value.ValueToUs += 2.5f;
                        if (_us.GetRelations(other.Owner).TotalAnger > 2.5f) entry.Value.ValueToUs += 2.5f;
                        if (this._us.GetRelations(other.Owner).TotalAnger <= 30f) continue;
                        entry.Value.ValueToUs += 2.5f;
                    }
                }
                foreach (SolarSystem fiveClosestSystem in entry.Key.FiveClosestSystems)
                {
                    float enstr = _us.GetGSAI().ThreatMatrix.PingRadarStr(fiveClosestSystem.Position, 150000, _us);
                 
                    if (enstr < 1 & entry.Value.ValueToUs > 5)
                        entry.Value.ValueToUs -= 2.5f;
                    else entry.Value.ValueToUs += 1f;
                }
            }
            int ranker = 0;
            int split = (int)(DefenseDict.Count * .10f);
            int splitStore = split;
            var sComs = DefenseDict.OrderBy(value => value.Value.PercentageOfValue).ThenBy(devlev => devlev.Value.SystemDevelopmentlevel);
            foreach (var scCom in sComs)
            {

                split--;
                if (split <= 0)
                {
                    ranker++;
                    split = splitStore;
                    if (ranker > 10)
                        ranker = 10;
                }
                scCom.Value.RankImportance = ranker;
            }
            foreach (var scCom in sComs)
            {
                scCom.Value.RankImportance = 10 * (scCom.Value.RankImportance / ranker);
                totalValue = totalValue + scCom.Value.ValueToUs;
            }
            #endregion
            #region Manage Ships                
            int strToAssign = (int)GetForcePoolStrength();
            float startingStr = strToAssign;            
            Parallel.ForEach(sComs, dd=>
                {
                    SolarSystem solarSystem  = dd.Key;
                    {
                        float predicted = solarSystem.GetPredictedEnemyPresence(120f, _us);
                        if (predicted <= 0f)
                        {
                            dd.Value.IdealShipStrength = 0f;
                        }
                        else
                        {
                            dd.Value.IdealShipStrength = predicted * (dd.Value.RankImportance / 10);
                            float min = (float)Math.Pow(dd.Value.ValueToUs,3) * dd.Value.RankImportance ;
                            dd.Value.IdealShipStrength += min;

                            // ReSharper disable once AccessToModifiedClosure
                            Interlocked.Add(ref  strToAssign, -(int)dd.Value.IdealShipStrength);

                        }
                    }
                });
            
            DefenseDeficit = strToAssign * -1;
            if (strToAssign < 0f) strToAssign = 0;

            foreach (var entry in DefenseDict)
            {
                entry.Value.PercentageOfValue = entry.Value.ValueToUs / totalValue;
                float min = strToAssign * entry.Value.PercentageOfValue; 
                if (entry.Value.IdealShipStrength < min)
                    entry.Value.IdealShipStrength = min;                
            }
            
            Map<Guid, Ship> assignedShips = new Map<Guid, Ship>();
            Array<Ship> shipsAvailableForAssignment = new Array<Ship>();
            //Remove excess force:
            foreach (var defenseDict in this.DefenseDict)
            {

                if (defenseDict.Value.GetOurStrength() <= defenseDict.Value.IdealShipStrength ) continue;

                var ships = defenseDict.Value.GetShipList();
                ships.Sort((x,y) => x.GetStrength().CompareTo(y.GetStrength()));
                foreach (Ship current in ships)
                {
#pragma warning disable 168
                    defenseDict.Value.ShipsDict.TryRemove(current.guid, out Ship remove);
#pragma warning restore 168
                    shipsAvailableForAssignment.Add(current);
                    current.GetAI().SystemToDefend = null;
                    if (defenseDict.Value.GetOurStrength() <= defenseDict.Value.IdealShipStrength)
                        break;
                }
            }
            //Add available force to pool:
            foreach (Ship defensiveForcePool in this.DefensiveForcePool)
            {
                if (!(defensiveForcePool.GetAI().HasPriorityOrder ||
                      defensiveForcePool.GetAI().State == AIState.Resupply)
                    && defensiveForcePool.loyalty == _us)

                    shipsAvailableForAssignment.Add(defensiveForcePool);

                else DefensiveForcePool.QueuePendingRemoval(defensiveForcePool);

            }
            //Assign available force:
            foreach (var entry in DefenseDict)
            {
                if (entry.Key == null) continue;
                
                entry.Value.AssignTargets();
            }
            if (shipsAvailableForAssignment.Count > 0)
            {
                foreach (var sccoms in sComs.OrderByDescending(descending => descending.Value.RankImportance))
                {
                    SolarSystem solarSystem1 = sccoms.Key;
                    if (startingStr < 0f) break;

                    foreach (
                        Ship ship1 in
                        shipsAvailableForAssignment.OrderBy(ship => ship.Center.SqDist(solarSystem1.Position))
                    )
                    {
                        if (ship1.GetAI().State == AIState.Resupply ||
                            (ship1.GetAI().State == AIState.SystemDefender && ship1.GetAI().SystemToDefend != null))
                            continue;
                        
                        if (ship1.Active)
                        {
                            if (assignedShips.ContainsKey(ship1.guid)) continue;

                            //var t = this.DefenseDict[solarSystem1].ShipsDict;
                            if (startingStr <= 0f ||
                                this.StrengthOf(sccoms.Value.ShipsDict) >= sccoms.Value.IdealShipStrength) break;

                            assignedShips.Add(ship1.guid, ship1);
                            if (sccoms.Value.ShipsDict.ContainsKey(ship1.guid)) continue;

                            sccoms.Value.ShipsDict.TryAdd(ship1.guid, ship1);
                            startingStr = startingStr - ship1.GetStrength();
                            ship1.GetAI().OrderSystemDefense(solarSystem1);
                        }
                        else DefensiveForcePool.QueuePendingRemoval(ship1);

                    }
                }
            }
            DefensiveForcePool.ApplyPendingRemovals();


            #endregion
            #region Manage Troops
            if (_us.isPlayer)
            {
                bool flag = false;
                
                foreach(Planet planet in this._us.GetPlanets())
                {
                    if(planet.colonyType == Planet.ColonyType.Military)
                        flag=true;
                }
                if(!flag)
                return;
            }
            var troopShips = new BatchRemovalCollection<Ship>();
            var groundTroops = new BatchRemovalCollection<Troop>();
            foreach (Planet p in this._us.GetPlanets())
            {
                for (int i = 0; i < p.TroopsHere.Count; i++)
                {
                    if (p.TroopsHere[i].Strength > 0 && p.TroopsHere[i].GetOwner() == _us)
                        groundTroops.Add(p.TroopsHere[i]);                    
                }
            }
            foreach (Ship ship2 in _us.GetShips())
            {
                if (ship2.shipData.Role != ShipData.RoleName.troop || ship2.fleet != null || ship2.Mothership != null || ship2.GetAI().HasPriorityOrder)                 
                    continue;
                
                troopShips.Add(ship2);
            }
            float totalTroopStrength = 0f;
            foreach (Troop t in groundTroops)            
                totalTroopStrength = totalTroopStrength + t.Strength;
            
            foreach (Ship ship3 in troopShips)
            {
                for (int i = 0; i < ship3.TroopList.Count; i++)
                {
                    if (ship3.TroopList[i].GetOwner() == _us)                    
                        totalTroopStrength += ship3.TroopList[i].Strength;                    
                }
            }
            int mintroopLevel = (int)(Ship.universeScreen.GameDifficulty + 1) * 2;
            int totalTroopWanted = 0;
            int totalCurrentTroops = 0;
            foreach (var entry in DefenseDict)
            {
                // find max number of troops for system.
                var planets = entry.Key.PlanetList.Where(planet => planet.Owner == _us).ToArray();
                int planetCount = planets.Length;
                int developmentlevel = planets.Sum(development => development.developmentLevel);
                entry.Value.SystemDevelopmentlevel = developmentlevel;
                int maxtroops = entry.Key.PlanetList.Where(planet => planet.Owner == _us).Sum(planet => planet.GetPotentialGroundTroops());
                entry.Value.IdealTroopStr = (mintroopLevel + entry.Value.RankImportance) * planetCount;

                if (entry.Value.IdealTroopStr > maxtroops)
                    entry.Value.IdealTroopStr = maxtroops;
                totalTroopWanted += (int)entry.Value.IdealTroopStr;
                int currentTroops = entry.Key.PlanetList.Where(planet => planet.Owner == _us).Sum(planet => planet.GetDefendingTroopCount());
                totalCurrentTroops += currentTroops;

                entry.Value.TroopStrengthNeeded = entry.Value.IdealTroopStr - currentTroops;
                groundTroops.ApplyPendingRemovals();

                for (int i = 0; i < troopShips.Count; i++)
                {
                    Ship troop = troopShips[i];

                    if (troop == null || troop.TroopList.Count <= 0)
                    {
                        troopShips.QueuePendingRemoval(troop);
                        continue;
                    }

                    ArtificialIntelligence troopAI = troop.GetAI();
                    if (troopAI == null)
                    {
                        troopShips.QueuePendingRemoval(troop);
                        continue;
                    }
                    if (troopAI.State == AIState.Rebase
                        && troopAI.OrderQueue.Count > 0
                        && troopAI.OrderQueue.Any(goal => goal.TargetPlanet != null && entry.Key == goal.TargetPlanet.system))
                    {
                        currentTroops++;
                        entry.Value.TroopStrengthNeeded--;
                        troopShips.QueuePendingRemoval(troop);
                    }
                }
                troopShips.ApplyPendingRemovals();
            }
            UniverseWants = totalCurrentTroops / (float)totalTroopWanted;
            foreach (Ship ship4 in troopShips)
            {


                IOrderedEnumerable<SolarSystem> sortedSystems =
                    from system in systems
                    //where !system.CombatInSystem 
                    orderby DefenseDict[system].TroopStrengthNeeded / this.DefenseDict[system].IdealTroopStr descending
                    orderby (int)(Vector2.Distance(system.Position, ship4.Center) / (UniverseData.UniverseWidth / 5f))
                    orderby DefenseDict[system].ValueToUs descending

                    select system;
                foreach (SolarSystem solarSystem2 in sortedSystems)
                {

                    if (solarSystem2.PlanetList.Count <= 0) continue;

                    SystemCommander defenseSystem = DefenseDict[solarSystem2];

                    if (defenseSystem.TroopStrengthNeeded <= 0)
                        continue;
                    defenseSystem.TroopStrengthNeeded--;
                    troopShips.QueuePendingRemoval(ship4);


                    //send troops to the first planet in the system with the lowest troop count.
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

                    ship4.GetAI().OrderRebase(target, true);

                }
            }
            troopShips.ApplyPendingRemovals();
            
            EmpireTroopRatio = UniverseWants;
            if (UniverseWants < .8f)
            {

                foreach (KeyValuePair<SolarSystem, SystemCommander> defenseSystem in this.DefenseDict)
                {
                    foreach (Planet p in defenseSystem.Key.PlanetList)
                    {
                        if (_us.isPlayer && p.colonyType != Planet.ColonyType.Military)
                            continue;
                        float devratio = (p.developmentLevel + 1) / (defenseSystem.Value.SystemDevelopmentlevel + 1);
                        if (!defenseSystem.Key.CombatInSystem
                            && p.GetDefendingTroopCount() > defenseSystem.Value.IdealTroopStr * devratio)
                        {
                            Troop l = p.TroopsHere.FirstOrDefault(loyalty => loyalty.GetOwner() == _us);
                            l?.Launch();
                        }
                    }
                }
            }

        }
            #endregion
		
        public ConcurrentDictionary<Ship, Array<Ship>> EnemyClumpsDict = new ConcurrentDictionary<Ship, Array<Ship>>();
        
		private float StrengthOf(ConcurrentDictionary<Guid, Ship> dict)
		{
			float str = 0f;
			foreach (var entry in dict)
			    str = str + entry.Value.GetStrength();
			
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
            if (!_disposed)
            {
                if (disposing)
                {
                        DefensiveForcePool?.Dispose();
                }
                this.DefensiveForcePool = null;
                this._disposed = true;
            }
        }
	}
}