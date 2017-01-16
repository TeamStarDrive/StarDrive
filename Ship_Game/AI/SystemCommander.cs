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
        public bool IsEnoughStrength => GetOurStrength() >= IdealShipStrength;
        public float PercentageOfValue;
        public float incomingThreatTime;
        public float SystemDevelopmentlevel;
        public float RankImportance;
        public int TroopCount = 0;
        public int TroopsWanted => IdealTroopCount - TroopCount;
		public ConcurrentDictionary<Guid, Ship> ShipsDict = new ConcurrentDictionary<Guid, Ship>();

		public Map<Ship, Array<Ship>> EnemyClumpsDict = new Map<Ship, Array<Ship>>();

		private Empire us;
        //public ReaderWriterLockSlim 
        public Map<Planet, PlanetTracker> planetTracker = new Map<Planet, PlanetTracker>();

		public SystemCommander(Empire e, SolarSystem system)
		{
			this.System = system;
			this.us = e;
		}
        public float UpdateSystemValue(Empire owner)
        {
            Empire Us = owner;
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
            if (ShipsDict.TryRemove(shipToRemove.guid, out Ship ship))
            {
                ship.GetAI().SystemToDefend = null;
                return true;
            }
            return false;
        }
        private void Clear()
        {
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
            this.EnemyClumpsDict.Clear();
            HashSet<Ship> ShipsAlreadyConsidered = new HashSet<Ship>();
            foreach (KeyValuePair<Guid, Ship> entry in this.ShipsDict)
            {
                Ship ship = entry.Value;


                if (ship == null || ship.GetAI().BadGuysNear || ship.GetAI().SystemToDefend == this.System)
                {
                    continue;
                }
                ship.GetAI().Target = null;
                ship.GetAI().hasPriorityTarget = false;
                ship.GetAI().Intercepting = false;
                ship.GetAI().SystemToDefend = null;
            }
            for (int i = 0; i < this.System.ShipList.Count; i++)
            {
                Ship ship = this.System.ShipList[i];
                if (ship != null && ship.loyalty != this.us && (ship.loyalty.isFaction || this.us.GetRelations(ship.loyalty).AtWar) && !ShipsAlreadyConsidered.Contains(ship) && !this.EnemyClumpsDict.ContainsKey(ship))
                {
                    this.EnemyClumpsDict.Add(ship, new Array<Ship>());
                    this.EnemyClumpsDict[ship].Add(ship);
                    ShipsAlreadyConsidered.Add(ship);
                    for (int j = 0; j < this.System.ShipList.Count; j++)
                    {
                        Ship otherShip = this.System.ShipList[j];
                        if (otherShip.loyalty != this.us && otherShip.loyalty == ship.loyalty && Vector2.Distance(ship.Center, otherShip.Center) < 15000f && !ShipsAlreadyConsidered.Contains(otherShip))
                        {
                            this.EnemyClumpsDict[ship].Add(otherShip);
                        }
                    }
                }
            }
            if (this.EnemyClumpsDict.Count != 0)
            {
                Array<Ship> ClumpsList = new Array<Ship>();
                foreach (KeyValuePair<Ship, Array<Ship>> entry in this.EnemyClumpsDict)
                {
                    ClumpsList.Add(entry.Key);
                }
                IOrderedEnumerable<Ship> distanceSorted =
                    from clumpPos in ClumpsList
                    orderby Vector2.Distance(this.System.Position, clumpPos.Center)
                    select clumpPos;
                HashSet<Ship> AssignedShips = new HashSet<Ship>();
                foreach (Ship enemy in this.EnemyClumpsDict[distanceSorted.First<Ship>()])
                {
                    float AssignedStr = 0f;
                    foreach (KeyValuePair<Guid, Ship> friendly in this.ShipsDict)
                    {
                        if (!friendly.Value.InCombat && friendly.Value.System==this.System)
                        {
                            if (AssignedShips.Contains(friendly.Value) || AssignedStr != 0f && AssignedStr >= enemy.GetStrength() || friendly.Value.GetAI().State == AIState.Resupply)
                            {
                                continue;
                            }
                            friendly.Value.GetAI().Intercepting = true;
                            friendly.Value.GetAI().OrderAttackSpecificTarget(enemy);
                            AssignedShips.Add(friendly.Value);
                            AssignedStr = AssignedStr + friendly.Value.GetStrength();
                        }
                        else
                        {
                            if (AssignedShips.Contains(friendly.Value))
                            {
                                continue;
                            }
                            AssignedShips.Add(friendly.Value);
                        }
                    }
                }
                Array<Ship> UnassignedShips = new Array<Ship>();
                foreach (KeyValuePair<Guid, Ship> ship in this.ShipsDict)
                {
                    if (AssignedShips.Contains(ship.Value))
                    {
                        continue;
                    }
                    UnassignedShips.Add(ship.Value);
                }
                foreach (Ship ship in UnassignedShips)
                {
                    if (ship.GetAI().State == AIState.Resupply ||ship.System!=this.System)
                    {
                        continue;
                    }
                    ship.GetAI().Intercepting = true;
                    ship.GetAI().OrderAttackSpecificTarget(AssignedShips.First().GetAI().Target as Ship);
                }
            }
            else
            {
                foreach (KeyValuePair<Guid, Ship> ship in this.ShipsDict)
                {
                    if (ship.Value.GetAI().State == AIState.Resupply )
                    {
                        continue; 
                    }
                    ship.Value.GetAI().OrderSystemDefense(this.System);
                }
            }
        }

		public float GetOurStrength()
		{
			float str = 0f;
			foreach (KeyValuePair<Guid, Ship> ship in this.ShipsDict)
			{
				str = str + ship.Value.BaseStrength;//.GetStrength();
			}
            
			return str;
		}

        public IEnumerable<Ship> GetShipList() => ShipsDict.Values;
        public void CalculateTroopNeeds(Empire Us)
        {
            int mintroopLevel = (int)(Ship.universeScreen.GameDifficulty + 1) * 2;            
            int totalCurrentTroops = 0;
            //foreach (KeyValuePair<SolarSystem, SystemCommander> entry in DefenseDict)
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
                totalCurrentTroops += currentTroops;

                TroopStrengthNeeded = IdealTroopCount - currentTroops;

                
            }

        }
        public void UpdatePlanetTracker()
        {
            var planetsHere = System.PlanetList.Where(planet => planet.Owner == us).ToArray();
            foreach(Planet planet in  planetsHere)
            {
                PlanetTracker currentValue = null;
                if(!planetTracker.TryGetValue(planet, out currentValue))
                {
                    PlanetTracker newEntry = new PlanetTracker(planet);
                    

                    planetTracker.Add(planet, newEntry);
                    continue;
                }
                if(currentValue.planet.Owner != this.us)
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
            this.planet = toTrack;

        }
       
    }

}