using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
	public sealed class SystemCommander
	{
		public SolarSystem System;
		public float ValueToUs;
		public int IdealTroopCount;
		public float TroopStrengthNeeded;
		public int IdealShipStrength;
        public bool IsEnoughShipStrength => GetOurStrength() >= IdealShipStrength;
        public bool IsEnoughTroopStrength => IdealTroopCount >= TroopCount;
        public float PercentageOfValue;
        public float SystemDevelopmentlevel;
        public float RankImportance;
        public int TroopCount = 0;
        public int TroopsWanted => IdealTroopCount - TroopCount;
		public Map<Guid, Ship> ShipsDict = new Map<Guid, Ship>();
		public Map<Ship, Array<Ship>> EnemyClumpsDict = new Map<Ship, Array<Ship>>();
		private readonly Empire Us;
        public Map<Planet, PlanetTracker> planetTracker = new Map<Planet, PlanetTracker>();

		public SystemCommander(Empire e, SolarSystem system)
		{
			System = system;
			Us = e;
		}
        public float UpdateSystemValue()
        {            
            ValueToUs = 0f;
            IdealShipStrength = 0;
            PercentageOfValue = 0f;
            foreach (Planet p in System.PlanetList)
            {
                if (p.Owner != null && p.Owner == Us)
                {
                    float cummulator = 0;
                    cummulator += p.Population / 10000f;
                    cummulator += (p.MaxPopulation / 10000f);
                    cummulator += p.Fertility;
                    cummulator += p.MineralRichness;
                    cummulator += p.CommoditiesPresent.Count;
                    cummulator += p.developmentLevel;
                    cummulator += p.GovBuildings ? 1 : 0;
                    cummulator += System.combatTimer > 0 ? 5 : 0;  //fbedard: DangerTimer is in relation to the player only !
                    cummulator += p.HasShipyard ? 5 : 0;

                    ValueToUs = cummulator;
                    planetTracker[p].value = cummulator;

                    if (Us.data.Traits.Cybernetic > 0) cummulator += p.MineralRichness;
                }
                foreach (Planet other in System.PlanetList)
                {
                    if (other == p || other.Owner == null || other.Owner == Us)
                        continue;
                    Relationship them = Us.GetRelations(other.Owner);
                    if (them.Trust < 50f) ValueToUs += 2.5f;
                    if (them.Trust < 10f) ValueToUs += 2.5f;
                    if (them.TotalAnger > 2.5f) ValueToUs += 2.5f;
                    if (them.TotalAnger <= 30f) continue;
                    ValueToUs += 2.5f;

                }
            }
            foreach (SolarSystem fiveClosestSystem in System.FiveClosestSystems)
            {
                bool noEnemies = true;
                if (!fiveClosestSystem.ExploredDict[Us]) continue;
                foreach (Empire e in fiveClosestSystem.OwnerList)
                {
                    if (e == Us) continue;
                    Relationship rel = Us.GetRelations(e);
                    if (!rel.Known) continue;
                    if (rel.AtWar) ValueToUs += 5f;
                    else if (rel.Treaty_OpenBorders) ValueToUs += 1f;
                    noEnemies = false;
                }
                if (noEnemies)
                {
                    ValueToUs *= 2;
                    ValueToUs /= 3;
                }
            }
            return ValueToUs;
        }
        public bool RemoveShip(Ship shipToRemove)
        {
            shipToRemove.GetAI().SystemToDefend = null;                        
            if (ShipsDict.Remove(shipToRemove.guid))
                return true;            
            return false;
        }
        private void Clear()
        {
            if (ShipsDict == null) return;
            foreach (Ship ship in ShipsDict.Values)
                ship.GetAI().SystemToDefend = null;
            ShipsDict.Clear();
        }
        public Planet AssignIdleDuties(Ship ship)
        {
            Array<PlanetTracker> planets = planetTracker.Values.ToArrayList();
            Planet p = planets.FindMax(value => value.value).planet;            
            return p ;
            
        }
        public void AssignTargets()
        {
            EnemyClumpsDict.Clear();
            HashSet<Ship> shipsAlreadyConsidered = new HashSet<Ship>();
            foreach (var kv in ShipsDict)
            {
                Ship ship = kv.Value;
                
                if (ship == null || ship.GetAI().BadGuysNear 
                    || ship.GetAI().SystemToDefend == System)                
                    continue;
                
                ship.GetAI().Target = null;
                ship.GetAI().hasPriorityTarget = false;
                ship.GetAI().Intercepting = false;
                ship.GetAI().SystemToDefend = null;
            }
            for (int i = 0; i < System.ShipList.Count; i++)
            {
                Ship ship = System.ShipList[i];
                if (ship != null && Us.IsEmpireAttackable(ship.loyalty,ship)
                    &&!shipsAlreadyConsidered.Contains(ship) 
                    && !EnemyClumpsDict.ContainsKey(ship))
                {
                    EnemyClumpsDict.Add(ship, new Array<Ship>());
                    EnemyClumpsDict[ship].Add(ship);
                    shipsAlreadyConsidered.Add(ship);
                    for (int j = 0; j < System.ShipList.Count; j++)
                    {
                        Ship otherShip = System.ShipList[j];
                        if (otherShip.loyalty != Us && otherShip.loyalty == ship.loyalty 
                            && ship.Center.InRadius(otherShip.Center, 15000f )
                            && !shipsAlreadyConsidered.Contains(otherShip))
                        
                            EnemyClumpsDict[ship].Add(otherShip);                        
                    }
                }
            }
            if (EnemyClumpsDict.Count != 0)
            {
                int i = 0;
                var clumpsList = new Ship[EnemyClumpsDict.Count];
                foreach (KeyValuePair<Ship, Array<Ship>> kv in EnemyClumpsDict)
                    clumpsList[i++] = kv.Key;

                Ship closest = clumpsList.FindMin(ship => System.Position.SqDist(ship.Center));

                var assignedShips = new Array<Ship>();
                foreach (Ship enemy in EnemyClumpsDict[closest])
                {
                    float assignedStr = 0f;
                    foreach (var kv in ShipsDict)
                    {
                        if (!kv.Value.InCombat && kv.Value.System == System)
                        {
                            if (assignedShips.Contains(kv.Value) || assignedStr > 0f && assignedStr >= enemy.GetStrength() || kv.Value.GetAI().State == AIState.Resupply)
                            {
                                continue;
                            }
                            kv.Value.GetAI().Intercepting = true;
                            kv.Value.GetAI().OrderAttackSpecificTarget(enemy);
                            assignedShips.Add(kv.Value);
                            assignedStr = assignedStr + kv.Value.GetStrength();
                        }
                        else
                        {
                            if (assignedShips.Contains(kv.Value))
                            {
                                continue;
                            }
                            assignedShips.Add(kv.Value);
                        }
                    }
                }
                foreach (var kv in ShipsDict)
                {
                    Ship ship = kv.Value;
                    if (!assignedShips.Contains(ship))
                        continue;
                    if (ship.GetAI().State == AIState.Resupply || ship.System != System)
                        continue;

                    ship.GetAI().Intercepting = true;
                    ship.GetAI().OrderAttackSpecificTarget(assignedShips.First.GetAI().Target as Ship);
                }
            }
            else
            {
                foreach (var kv in ShipsDict)
                {
                    if (kv.Value.GetAI().State == AIState.Resupply ) continue;
   
                    kv.Value.GetAI().OrderSystemDefense(System);
                }
            }
        }

		public float GetOurStrength()
		{
			float str = 0f;
            foreach (var kv in ShipsDict)
                str = str + kv.Value.GetStrength();
            
			return str;
		}

        public IEnumerable<Ship> GetShipList() => ShipsDict.Values;
        public void CalculateTroopNeeds()
        {
            int mintroopLevel = (int)(Ship.universeScreen.GameDifficulty + 1) * 2;
            TroopCount = 0;
            {
                // find max number of troops for system.
                var planets = System.PlanetList.Where(planet => planet.Owner == Us).ToArray();
                int planetCount = planets.Length;
                int developmentlevel = planets.Sum(development => development.developmentLevel);
                SystemDevelopmentlevel = developmentlevel;
                int maxtroops = System.PlanetList.Where(planet => planet.Owner == Us).Sum(planet => planet.GetPotentialGroundTroops());
                IdealTroopCount = (mintroopLevel + (int)RankImportance) * planetCount;

                if (IdealTroopCount > maxtroops)
                    IdealTroopCount = maxtroops;
                int currentTroops = System.PlanetList.Where(planet => planet.Owner == Us).Sum(planet => planet.GetDefendingTroopCount());
                TroopCount += currentTroops;

                TroopStrengthNeeded = IdealTroopCount - currentTroops;
            }

        }
        public void CalculateShipneeds()
        {
            float predicted = Us.GetGSAI().ThreatMatrix.PingRadarStr(System.Position, 150000 * 2, Us);
            int min = (int)(Math.Pow(ValueToUs, 3) * RankImportance);
            foreach (var system in System.FiveClosestSystems)
            {
                predicted += Us.GetGSAI().ThreatMatrix.PingRadarStr(system.Position, 150000 * 2, Us);
            }
            if (predicted <= 0f) IdealShipStrength = min;
            else
            {
                IdealShipStrength = (int)(predicted * RankImportance / 10);
                
                IdealShipStrength += min;
            }
        }
        public void UpdatePlanetTracker()
        {
            var planetsHere = System.PlanetList.Where(planet => planet.Owner == Us).ToArray();
            foreach(Planet planet in  planetsHere)
            {
                if (!planetTracker.TryGetValue(planet, out PlanetTracker currentValue))
                {
                    PlanetTracker newEntry = new PlanetTracker(planet);
                    planetTracker.Add(planet, newEntry);
                    continue;
                }
                if (currentValue.planet.Owner != Us)
                {
                    planetTracker.Remove(currentValue.planet);
                }
            }
        }
        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }
        ~SystemCommander() { Destroy(); }

        private void Destroy()
        {
            Clear();            
            ShipsDict = null;
            EnemyClumpsDict?.Clear();
            EnemyClumpsDict = null;
            planetTracker?.Clear();
            planetTracker = null;
            System = null;          
        }
    }
    public class PlanetTracker
    {
        public float value;
        public int troopsWanted;
        public int troopsHere ;
        public Planet planet;
        public PlanetTracker(Planet toTrack)
        {
            planet = toTrack;

        }
       
    }

}