using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
	public sealed class DefensiveCoordinator: IDisposable
	{
		private readonly Empire Us;
		public Map<SolarSystem, SystemCommander> DefenseDict = new Map<SolarSystem, SystemCommander>();
		public BatchRemovalCollection<Ship> DefensiveForcePool = new BatchRemovalCollection<Ship>();
        public float DefenseDeficit;        
        public float EmpireTroopRatio;
        public float UniverseWants;
        public float GetPctOfForces(SolarSystem system) => DefenseDict[system].GetOurStrength() / GetForcePoolStrength();
        public float GetPctOfValue(SolarSystem system) => DefenseDict[system].PercentageOfValue;
        private int TotalValue;
        public DefensiveCoordinator(Empire e)
		{
            Us = e;
		}
        public void AddShip(Ship ship)
        {
            ship.GetAI().SystemToDefend = null;
            ship.GetAI().SystemToDefendGuid = Guid.Empty;
            ship.GetAI().HasPriorityOrder = false;
            ship.GetAI().State = AIState.SystemDefender;
            DefenseDeficit -= ship.GetStrength();
        }
        //added by gremlin parallel forcepool
        public float GetForcePoolStrength()
        {            
            float strength = 0;
            for (int index = 0; index < DefensiveForcePool.Count; index++)
            {
                Ship ship = DefensiveForcePool[index];
                if (!ship.Active || ship.dying) continue;
                strength +=  ship.GetStrength();
            }
            return strength;
        }

	    public float GetDefensiveThreatFromPlanets(Array<Planet> planets)
	    {
	        if (DefenseDict.Count == 0) return 0;
            int count = 0;
            float str = 0;            
	        for (var index = 0; index < planets.Count; index++)
	        {
	            Planet planet = planets[index];	      
                if (!DefenseDict.TryGetValue(planet.system, out SystemCommander scom)) continue;
                count++;
                str += scom.RankImportance;	                
	        }
            return str / count;
        }
        public Planet AssignIdleShips(Ship ship)
        {
            return DefenseDict[ship.GetAI().SystemToDefend].AssignIdleDuties(ship);
        }
        public void Remove(Ship ship, bool queueRemoval = false)
        {
            SolarSystem systoDefend = ship.GetAI().SystemToDefend;
            if (queueRemoval)
                DefensiveForcePool.QueuePendingRemoval(ship);            
            else if (!DefensiveForcePool.Remove(ship))
            {
                if(ship.Active && systoDefend != null)
                    Log.Info(color: ConsoleColor.Yellow, text: "DefensiveCoordinator: Remove : Not in DefensePool");
            }
            else if (ship.Active && systoDefend == null)
                Log.Info(color: ConsoleColor.Yellow, text: "DefensiveCoordinator: Remove : SystemToDefend Was Null");


            bool found = false;
            if (systoDefend != null) //check for specific system commander
            {
                if (!DefenseDict[systoDefend].RemoveShip(ship))
                {
                    if (ship.Active)
                        Log.Info(color: ConsoleColor.Yellow, text: "SystemCommander: Remove : Not in SystemCommander");
                }
                else found = true;
                // return; // when sysdefense is safe enable. 
            }
            
            foreach (var kv in DefenseDict) //double check for ship in any other sys commanders
            {
                if (!kv.Value.RemoveShip(ship) ) continue;
                if (!found)
                    found = true;
                else Log.Info(color: ConsoleColor.Yellow, text: "SystemCommander: Remove : Ship Was in Multiple SystemCommanders");

            }
            if (!found && ship.Active)
            {
                Log.Info(color: ConsoleColor.Yellow,
                    text:
                    systoDefend == null
                        ? "SystemCommander: Remove : SystemToDefend Was Null"
                        : "SystemCommander: Remove : Ship Not Found in Any");
            }
            
            
        }
        private void CalculateSystemImportance()
        {
            foreach (Planet p in Ship.universeScreen.PlanetsDict.Values) //@TODO move this to planet. this is removing troops without any safety
            {
                if (p.Owner != Us && !p.EventsOnBuildings() && !p.TroopsHereAreEnemies(Us))
                {
                    p.TroopsHere.ApplyPendingRemovals();
                    foreach (Troop troop in p.TroopsHere.Where(loyalty => loyalty.GetOwner() == Us))
                    {
                        p.TroopsHere.QueuePendingRemoval(troop);
                        troop.Launch();
                    }
                    p.TroopsHere.ApplyPendingRemovals();
                }
                else if (p.Owner == Us) //This should stay here.
                {
                    if (p.system == null || DefenseDict.ContainsKey(p.system)) continue;
                    DefenseDict.Add(p.system, new SystemCommander(Us, p.system));
                }
            }
            TotalValue = 0;
            
            foreach (var kv in DefenseDict.ToArray())
            {
                if (kv.Key.OwnerList.Contains(Us))
                {
                    kv.Value.UpdatePlanetTracker();
                    continue;
                }
                kv.Value.Dispose();
                DefenseDict.Remove(kv.Key);                
            }

            foreach (var kv in DefenseDict)
                kv.Value.UpdateSystemValue();
       
            int ranker = 0;
            int split = DefenseDict.Count / 10;
            int splitStore = split;
            //@Complex Double orderBy is not simple.
            var sComs = DefenseDict.OrderBy(value => value.Value.PercentageOfValue).ThenBy(devlev => devlev.Value.SystemDevelopmentlevel);
            foreach (var kv in sComs)
            {
                split--;
                if (split <= 0)
                {
                    ranker++;
                    split = splitStore;
                    if (ranker > 10)
                        ranker = 10;
                }
                kv.Value.RankImportance = ranker;
            }
            foreach (var kv in sComs)
            {
                kv.Value.RankImportance = (int)(10 * (kv.Value.RankImportance / ranker));
                TotalValue += (int)kv.Value.ValueToUs;
                kv.Value.CalculateShipneeds();
                kv.Value.CalculateTroopNeeds();
            }
        }
        private void ManageShips()
        {
            var sComs = DefenseDict.OrderByDescending(rank => rank.Value.RankImportance);
            int strToAssign = (int)GetForcePoolStrength();
            float startingStr = strToAssign;
            foreach (var kv in sComs)
            {                
                strToAssign -= kv.Value.IdealShipStrength;                
            }
            DefenseDeficit = strToAssign * -1;
            if (strToAssign < 0f) strToAssign = 0;

            foreach (var kv in DefenseDict)
            {
                kv.Value.PercentageOfValue = kv.Value.ValueToUs / TotalValue;
                int min = (int)(strToAssign * kv.Value.PercentageOfValue);
                if (kv.Value.IdealShipStrength < min) kv.Value.IdealShipStrength = min;
            }

            Map<Guid, Ship> assignedShips = new Map<Guid, Ship>();
            Array<Ship> shipsAvailableForAssignment = new Array<Ship>();
            //Remove excess force:
            foreach (var kv in DefenseDict)
            {
                if (!kv.Value.IsEnoughShipStrength) continue;

                Ship[] ships = kv.Value.GetShipList.ToArray();
                Array.Sort(ships, (x, y) => x.GetStrength().CompareTo(y.GetStrength()));
                foreach (Ship current in ships)
                {
                    kv.Value.RemoveShip(current);
                    shipsAvailableForAssignment.Add(current);

                    if (!kv.Value.IsEnoughShipStrength)
                        break;
                }
            }
            //Add available force to pool:            
            for(int x = 0; x< DefensiveForcePool.Count;x++)
            {
                Ship ship = DefensiveForcePool[x];
                if (ship.Active && !(ship.GetAI().HasPriorityOrder || ship.GetAI().State == AIState.Resupply)
                    && ship.loyalty == Us && ship.GetAI().SystemToDefend == null)
                {
                    shipsAvailableForAssignment.Add(ship);
                }
                else Remove(ship);
            }
            //Assign available force:
            foreach (var kv in DefenseDict)
            {
                kv.Value?.AssignTargets();
            }

            if (shipsAvailableForAssignment.Count > 0)
            {
                foreach (var kv in sComs.OrderByDescending(descending => descending.Value.RankImportance))
                {
                    if (startingStr < 0f) break;

                    foreach (Ship ship in shipsAvailableForAssignment
                        .OrderBy(ship => ship.Center.SqDist(kv.Key.Position)))
                    {
                        if (ship.GetAI().State == AIState.Resupply
                            || (ship.GetAI().State == AIState.SystemDefender
                            && ship.GetAI().SystemToDefend != null))
                            continue;
                        if (!ship.Active)
                        {
                            Remove(ship);                            
                            continue;
                        }

                        if (assignedShips.ContainsKey(ship.guid)) continue;
                        if (startingStr <= 0f || kv.Value.IsEnoughShipStrength) break;

                        assignedShips.Add(ship.guid, ship);
                        if (kv.Value.ShipsDict.ContainsKey(ship.guid)) continue;

                        kv.Value.ShipsDict.Add(ship.guid, ship);
                        startingStr = startingStr - ship.GetStrength();
                        ship.GetAI().OrderSystemDefense(kv.Key);
                    }
                }
            }
            DefensiveForcePool.ApplyPendingRemovals();

        }
        private void ManageTroops()
        {
            if (Us.isPlayer)
            {
                bool flag = false;
                foreach (Planet planet in Us.GetPlanets())
                {
                    if (planet.colonyType != Planet.ColonyType.Military)
                        continue;
                    flag = true;
                    break;
                }
                if (!flag)
                    return;
            }

            float totalTroopStrength = 0f;
            var troopShips = Us.GetTroopShips(ref totalTroopStrength);
            int totalTroopWanted = 0;
            int totalCurrentTroops = 0;
            foreach (var kv in DefenseDict)
            {
                // find max number of troops for system.
                
                int currentTroops = kv.Value.TroopCount;
                for (int i = 0; i < troopShips.Count; i++)
                {
                    Ship troop = troopShips[i];

                    if (troop == null || troop.TroopList.Count <= 0)
                    {
                        troopShips.Remove(troop);
                        continue;
                    }

                    ArtificialIntelligence troopAI = troop.GetAI();
                    if (troopAI == null)
                    {
                        troopShips.Remove(troop);
                        continue;
                    }
                    if (troopAI.State == AIState.Rebase
                        && troopAI.OrderQueue.NotEmpty
                        && troopAI.OrderQueue.Any(goal => goal.TargetPlanet != null && kv.Key == goal.TargetPlanet.system))
                    {
                        currentTroops++;
                        kv.Value.TroopStrengthNeeded--;
                        troopShips.Remove(troop);
                    }
                }
                kv.Value.TroopCount = currentTroops;
                totalCurrentTroops += currentTroops;
                totalTroopWanted += kv.Value.TroopsWanted;
            }
            UniverseWants = totalCurrentTroops / (float)totalTroopWanted;


            for (int x = 0; x < troopShips.Count; x++)
            {
                Ship troopShip = troopShips[x];
                var sortedSystems =
                from sComs in DefenseDict.Values
                orderby sComs.TroopStrengthNeeded / sComs.IdealTroopCount descending
                orderby (int)(Vector2.Distance(sComs.System.Position, troopShip.Center) / (UniverseData.UniverseWidth / 5f))
                orderby sComs.ValueToUs descending
                select sComs.System;
                foreach (SolarSystem solarSystem2 in sortedSystems)
                {

                    if (solarSystem2.PlanetList.Count <= 0) continue;

                    SystemCommander defenseSystem = DefenseDict[solarSystem2];

                    if (defenseSystem.TroopStrengthNeeded <= 0) continue;

                    defenseSystem.TroopStrengthNeeded--;
                    troopShips.Remove(troopShip);

                    Planet target = null;
                    foreach (Planet lowTroops in solarSystem2.PlanetList)
                    {
                        if (lowTroops.Owner != troopShip.loyalty) continue;

                        if (target == null || lowTroops.TroopsHere.Count < target.TroopsHere.Count)
                            target = lowTroops;
                    }
                    if (target == null) continue;
                    troopShip.GetAI().OrderRebase(target, true);
                }
            }
            EmpireTroopRatio = UniverseWants;
            if (UniverseWants > .8f) return;

            foreach (var kv in DefenseDict)
                foreach (Planet p in kv.Key.PlanetList)
                {
                    if (Us.isPlayer && p.colonyType != Planet.ColonyType.Military) continue;
                    float devratio = (p.developmentLevel + 1) / (kv.Value.SystemDevelopmentlevel + 1);
                    if (!kv.Key.CombatInSystem
                        && p.GetDefendingTroopCount() > kv.Value.IdealTroopCount * devratio)
                    {
                        Troop l = p.TroopsHere.FirstOrDefault(loyalty => loyalty.GetOwner() == Us);
                        l?.Launch();
                    }
                }
        }
        public void ManageForcePool()
        {            
            CalculateSystemImportance();
            ManageShips();
            ManageTroops();
        }        

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }
        ~DefensiveCoordinator() { Destroy(); }

        private void Destroy()
        {
            DefensiveForcePool?.Dispose(ref DefensiveForcePool);
            if (DefenseDict != null)
                foreach (var kv in DefenseDict)
                {
                    kv.Value?.Dispose();
                }
            DefenseDict = null;
        }
    }
}