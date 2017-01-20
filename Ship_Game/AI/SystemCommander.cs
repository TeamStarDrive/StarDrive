using System;
using System.Collections.Generic;
using System.Linq;
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
        public int TroopCount;
        public int TroopsWanted => IdealTroopCount - TroopCount;
		public Map<Guid, Ship> ShipsDict = new Map<Guid, Ship>();
		public Map<Ship, Ship[]> EnemyClumpsDict = new Map<Ship, Ship[]>();
		private readonly Empire Us;
        public Map<Planet, PlanetTracker> PlanetTracker = new Map<Planet, PlanetTracker>();
        public IEnumerable<Ship> GetShipList => ShipsDict.Values;

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

                    if (Us.data.Traits.Cybernetic > 0) cummulator += p.MineralRichness;
                    ValueToUs = cummulator;
                    PlanetTracker[p].value = cummulator;
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
            var planets = PlanetTracker.Values;
            PlanetTracker best = null;
            foreach(PlanetTracker pl in planets)
            {
                if (best == null || best.value < pl.value)
                    best = pl;
            }
            return best.Planet;                        
        }
        public void AssignTargets()
        {
            EnemyClumpsDict = Us.GetGSAI().ThreatMatrix.PingRadarShipClustersByShip(System.Position, 150000, 15000, Us);
            
            if (EnemyClumpsDict.Count != 0)
            {
                int i = 0;
                var clumpsList = new Ship[EnemyClumpsDict.Count];
                foreach (var kv in EnemyClumpsDict)
                    clumpsList[i++] = kv.Key;

                Ship closest = clumpsList.FindMin(ship => System.Position.SqDist(ship.Center));
                i = 0;
                var assignedShips = new Ship[ShipsDict.Count];
                foreach (Ship enemy in EnemyClumpsDict[closest])
                {
                    float assignedStr = 0f;
                    foreach (var kv in ShipsDict)
                    {
                        if (!kv.Value.InCombat && kv.Value.System == System)
                        {
                            if ((assignedStr > 0f && assignedStr >= enemy.GetStrength())
                                || kv.Value.GetAI().State == AIState.Resupply
                                ||assignedShips.Contains(kv.Value))
                                continue;

                            kv.Value.GetAI().Intercepting = true;
                            kv.Value.GetAI().OrderAttackSpecificTarget(enemy);
                            assignedShips[i++]=kv.Value;
                            assignedStr += kv.Value.GetStrength();
                        }
                        else
                        {
                            if (assignedShips.Contains(kv.Value)) continue;
                            assignedStr += kv.Value.GetStrength();
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
                    ship.GetAI().OrderAttackSpecificTarget(assignedShips[0]?.GetAI().Target as Ship);
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
                if (!PlanetTracker.TryGetValue(planet, out PlanetTracker currentValue))
                {
                    PlanetTracker newEntry = new PlanetTracker(planet);
                    PlanetTracker.Add(planet, newEntry);
                    continue;
                }
                if (currentValue.Planet.Owner != Us)
                {
                    PlanetTracker.Remove(currentValue.Planet);
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
            PlanetTracker?.Clear();
            PlanetTracker = null;
            System = null;          
        }
    }
    public class PlanetTracker
    {
        public float value;
        public int TroopsWanted;
        public int TroopsHere ;
        public Planet Planet;
        public PlanetTracker(Planet toTrack)
        {
            Planet = toTrack;

        }
       
    }

}