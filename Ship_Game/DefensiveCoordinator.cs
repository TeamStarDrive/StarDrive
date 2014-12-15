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
	public class DefensiveCoordinator
	{
		private Empire us;

		public Dictionary<SolarSystem, SystemCommander> DefenseDict = new Dictionary<SolarSystem, SystemCommander>();

		public BatchRemovalCollection<Ship> DefensiveForcePool = new BatchRemovalCollection<Ship>();
        public float defenseDeficit = 0;

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
                strength += (int)ship.GetStrength();
            }
            return (float)strength;

        }

		public float GetPctOfForces(SolarSystem system)
		{
			return this.DefenseDict[system].GetOurStrength() / this.GetForcePoolStrength();
		}

		public float GetPctOfValue(SolarSystem system)
		{
			return this.DefenseDict[system].PercentageOfValue;
		}

        public void ManageForcePool()
        {
            foreach (Planet p in this.us.GetPlanets())
            {
                if (p.Owner == null && p.BuildingList.Count == 0 && p.TroopsHere.Count > 0 && p.GetGroundStrengthOther(this.us) > 0)
                {
                    foreach (Troop troop in p.TroopsHere.Where(loyalty => loyalty.GetOwner() == this.us))
                    {
                        troop.Launch();
                    }
                }

                if (p == null || p.system == null || this.DefenseDict.ContainsKey(p.system))
                {
                    continue;
                }
                this.DefenseDict.Add(p.system, new SystemCommander(this.us, p.system));
            }
            List<SolarSystem> Keystoremove = new List<SolarSystem>();
            foreach (KeyValuePair<SolarSystem, SystemCommander> entry in this.DefenseDict)
            {
                if (entry.Key.OwnerList.Contains(this.us))
                {
                    continue;
                }
                Keystoremove.Add(entry.Key);
            }
            foreach (SolarSystem key in Keystoremove)
            {
                foreach (KeyValuePair<Guid, Ship> entry in this.DefenseDict[key].ShipsDict)
                {
                    if (entry.Value.GetSystem() == entry.Value.GetAI().SystemToDefend)
                    entry.Value.GetAI().SystemToDefend = null;
                }
                this.DefenseDict[key].ShipsDict.Clear();
                this.DefenseDict.Remove(key);
            }
            float TotalValue = 0f;
            List<SolarSystem> systems = new List<SolarSystem>();
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
                        SystemCommander value = entry.Value;
                        value.ValueToUs = value.ValueToUs + p.Population / 10000f;
                        SystemCommander valueToUs = entry.Value;
                        valueToUs.ValueToUs = valueToUs.ValueToUs + (p.MaxPopulation / 10000f);// - p.Population / 10000f);
                        SystemCommander systemCommander = entry.Value;
                        systemCommander.ValueToUs = systemCommander.ValueToUs + p.Fertility;
                        SystemCommander value1 = entry.Value;
                        //added by gremlin commodities increase defense desire
                        value1.ValueToUs = value1.ValueToUs + p.MineralRichness;
                        value.ValueToUs += p.BuildingList.Where(commodity => commodity.IsCommodity).Count();
                        if (this.us.data.Traits.Cybernetic > 0)
                        {
                            value1.ValueToUs += p.MineralRichness;
                        }
                    }
                    foreach (Planet other in entry.Key.PlanetList)
                    {
                        if (other == p || other.Owner == null || other.Owner == this.us)
                        {
                            continue;
                        }
                        if (this.us.GetRelations()[other.Owner].Trust < 50f)
                        {
                            SystemCommander valueToUs1 = entry.Value;
                            valueToUs1.ValueToUs = valueToUs1.ValueToUs + 2.5f;
                        }
                        if (this.us.GetRelations()[other.Owner].Trust < 10f)
                        {
                            SystemCommander systemCommander1 = entry.Value;
                            systemCommander1.ValueToUs = systemCommander1.ValueToUs + 2.5f;
                        }
                        if (this.us.GetRelations()[other.Owner].TotalAnger > 2.5f)
                        {
                            SystemCommander value2 = entry.Value;
                            value2.ValueToUs = value2.ValueToUs + 2.5f;
                        }
                        if (this.us.GetRelations()[other.Owner].TotalAnger <= 30f)
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
                    foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.us.GetRelations())
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
                            entry.Value.ValueToUs += 10f;
                            flag = true;
                            continue;
                        }
                        if (!Relationship.Value.Treaty_OpenBorders)
                        {
                            entry.Value.ValueToUs += 5f;
                            flag = true;
                        }

                    }
                    if (!flag& entry.Value.ValueToUs>5)
                        entry.Value.ValueToUs -= 2.5f;
                }

            }
            IOrderedEnumerable<SolarSystem> sortedList =
                from system in systems
                orderby system.GetPredictedEnemyPresence(60f, this.us) descending
                select system;
            float StrToAssign = this.GetForcePoolStrength();
            float StartingStr = StrToAssign;
            foreach (SolarSystem solarSystem in sortedList)
            {
                float Predicted = solarSystem.GetPredictedEnemyPresence(120f, this.us);
                if (Predicted <= 0f)
                {
                    this.DefenseDict[solarSystem].IdealShipStrength = 0f;
                }
                else
                {
                    this.DefenseDict[solarSystem].IdealShipStrength = Predicted;
                    StrToAssign = StrToAssign - Predicted;
                }
            }
            if (StrToAssign < 0f)
            {
                StrToAssign = 0f;
            }
            foreach (KeyValuePair<SolarSystem, SystemCommander> entry in this.DefenseDict)
            {
                TotalValue = TotalValue + entry.Value.ValueToUs;
            }
            foreach (KeyValuePair<SolarSystem, SystemCommander> entry in this.DefenseDict)
            {
                entry.Value.PercentageOfValue = entry.Value.ValueToUs / TotalValue;
                SystemCommander idealShipStrength = entry.Value;
                idealShipStrength.IdealShipStrength = idealShipStrength.IdealShipStrength + entry.Value.PercentageOfValue * StrToAssign;
            }
            Dictionary<Guid, Ship> AssignedShips = new Dictionary<Guid, Ship>();
            List<Ship> ShipsAvailableForAssignment = new List<Ship>();
            foreach (KeyValuePair<SolarSystem, SystemCommander> defenseDict in this.DefenseDict)
            {
                if (this.DefenseDict[defenseDict.Key].GetOurStrength() <= this.DefenseDict[defenseDict.Key].IdealShipStrength + this.DefenseDict[defenseDict.Key].IdealShipStrength * 0.1f)
                {
                    continue;
                }
                IOrderedEnumerable<Ship> strsorted =
                    from ship in this.DefenseDict[defenseDict.Key].GetShipList()
                    orderby ship.GetStrength()
                    select ship;
                using (IEnumerator<Ship> enumerator = strsorted.GetEnumerator())
                {
                    do
                    {
                        if (!enumerator.MoveNext())
                        {
                            break;
                        }
                        Ship current = enumerator.Current;
                        this.DefenseDict[defenseDict.Key].ShipsDict.Remove(current.guid);
                        ShipsAvailableForAssignment.Add(current);
                    }
                    while (this.DefenseDict[defenseDict.Key].GetOurStrength() >= this.DefenseDict[defenseDict.Key].IdealShipStrength + this.DefenseDict[defenseDict.Key].IdealShipStrength * 0.1f);
                }
            }
            foreach (Ship defensiveForcePool in this.DefensiveForcePool)
            {
                if ((!defensiveForcePool.GetAI().HasPriorityOrder || defensiveForcePool.GetAI().State == AIState.Resupply) && defensiveForcePool.loyalty == this.us)
                {
                    //if (defensiveForcePool.GetAI().SystemToDefend != null)
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
            IOrderedEnumerable<SolarSystem> valueSortedList =
                from system in systems
                orderby this.DefenseDict[system].IdealShipStrength - this.DefenseDict[system].GetOurStrength() descending
                select system;
            if (ShipsAvailableForAssignment.Count > 0)
            {
                foreach (SolarSystem solarSystem1 in valueSortedList)
                {
                    if (StartingStr < 0f)
                    {
                        break;
                    }
                    IOrderedEnumerable<Ship> distanceSorted =
                        from ship in ShipsAvailableForAssignment
                        orderby Vector2.Distance(ship.Center, solarSystem1.Position)
                        select ship;
                    foreach (Ship ship1 in distanceSorted)
                    {
                        if (ship1.GetAI().State == AIState.Resupply)
                        {
                            continue;
                        }
                        if (ship1.Active)
                        {
                            if (AssignedShips.ContainsKey(ship1.guid))
                            {
                                continue;
                            }
                            if (StartingStr <= 0f || this.StrengthOf(this.DefenseDict[solarSystem1].ShipsDict) >= this.DefenseDict[solarSystem1].IdealShipStrength)
                            {
                                break;
                            }
                            AssignedShips.Add(ship1.guid, ship1);
                            if (this.DefenseDict[solarSystem1].ShipsDict.ContainsKey(ship1.guid))
                            {
                                continue;
                            }
                            this.DefenseDict[solarSystem1].ShipsDict.Add(ship1.guid, ship1);
                            StartingStr = StartingStr - ship1.GetStrength();
                            if (ship1.InCombat || ship1.GetAI().State == AIState.Resupply)
                            {
                                continue;
                            }
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
            foreach (KeyValuePair<SolarSystem, SystemCommander> entry in this.DefenseDict)
            {
                if (entry.Key == null )
                {
                    continue;
                }
                entry.Value.AssignTargets();
            }
			if (this.us == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
			{
				return;
			}
			BatchRemovalCollection<Ship> TroopShips = new BatchRemovalCollection<Ship>();
			BatchRemovalCollection<Troop> GroundTroops = new BatchRemovalCollection<Troop>();
			foreach (Planet p in this.us.GetPlanets())
			{
				for (int i = 0; i < p.TroopsHere.Count; i++)
				{
					if (p.TroopsHere[i].Strength > 0 && p.TroopsHere[i].GetOwner() == this.us )//&& !p.RecentCombat && p.ParentSystem.combatTimer <=0)
					{
						GroundTroops.Add(p.TroopsHere[i]);
					}
				}
			}
			foreach (Ship ship2 in this.us.GetShips())
			{
				if (ship2.Role != "troop" || ship2.fleet != null ) //|| ship2.GetAI().State != AIState.AwaitingOrders)
				{
					continue;
				}
				TroopShips.Add(ship2);

			}
			float TotalTroopStrength = 0f;
			foreach (Troop t in GroundTroops)
			{
				
                TotalTroopStrength = TotalTroopStrength + (float)t.Strength;
			}
			foreach (Ship ship3 in TroopShips)
			{
				
                for (int i = 0; i < ship3.TroopList.Count; i++)
				{
					
                    if (ship3.TroopList[i].GetOwner() == this.us )
					{
						TotalTroopStrength = TotalTroopStrength + (float)ship3.TroopList[i].Strength;
					}
				}
			}
            int maxtroops = 0;
            int currentTroops = 0;
            List<Planet> planets = new List<Planet>();
            int planetCount = 0;
            int developmentlevel = 0;
			foreach (KeyValuePair<SolarSystem, SystemCommander> entry in this.DefenseDict)
			{
                    //entry.Value.ValueToUs ; // entry.Key.PlanetList.Where(planet => planet.Owner == this.us).Sum(planet => planet.GetPotentialGroundTroops(this.us) / (6 - planet.developmentLevel));
                 maxtroops =entry.Key.PlanetList.Where(planet => planet.Owner == this.us).Sum(planet => planet.GetPotentialGroundTroops(this.us)) ;
                 currentTroops = entry.Key.PlanetList.Where(planet => planet.Owner == this.us).Sum(planet => planet.TroopsHere.Where(troop => troop.GetOwner() == this.us).Count());
                //int planetCount = entry.Key.PlanetList.Where(planet => planet.Owner == this.us).Count();
                 planets = entry.Key.PlanetList.Where(planet => planet.Owner == this.us).ToList();
                 planetCount = planets.Count;
                 developmentlevel = planets.Sum(development => development.developmentLevel);

                //float currentTroopStrength = entry.Key.PlanetList.Where(planet => planet.Owner == this.us).Sum(planet => planet.TroopsHere.Where(troop=> troop.GetOwner()==this.us).Sum(troop=>troop.Strength));
                
                //if (entry.Value.IdealTroopStr >= maxtroops-30)
                //    entry.Value.IdealTroopStr = maxtroops -30;
                    //entry.Value.PercentageOfValue * TotalTroopStrength;              
                
                //entry.Value.TroopStrengthNeeded = entry.Value.ValueToUs * 40;
                
                //if (entry.Value.TroopStrengthNeeded >= maxtroops - 120)
                //    entry.Value.TroopStrengthNeeded = maxtroops - 120;
                    //entry.Key.PlanetList.Where(planet => planet.Owner == this.us).Sum(planet => planet.GetPotentialGroundTroops(this.us) / (6 - planet.developmentLevel)) *10; //entry.Value.PercentageOfValue * TotalTroopStrength;
                entry.Value.IdealTroopStr = developmentlevel *(1+(int)Empire.universeScreen.GameDifficulty);
                if (entry.Value.IdealTroopStr > maxtroops)
                    entry.Value.IdealTroopStr = maxtroops;
                entry.Value.TroopStrengthNeeded = entry.Value.IdealTroopStr - currentTroops;
                //foreach (Planet p in entry.Key.PlanetList)
                //{
                //    if (p.Owner != this.us)
                //    {
                //        continue;
                //    }
                //    foreach (Troop t in p.TroopsHere)
                //    {
                //        if (t.GetOwner() != this.us || entry.Value.TroopStrengthNeeded - (float)t.Strength < 0f)
                //        {
                //            continue;
                //        }

                //        entry.Value.TroopStrengthNeeded = entry.Value.TroopStrengthNeeded - (float)t.Strength;
                //        GroundTroops.QueuePendingRemoval(t);
                //    }

                //}
                GroundTroops.ApplyPendingRemovals();
                //foreach (Ship troopship in TroopShips)
                //{
                //    if (troopship.GetAI().OrderQueue.Count <= 0 || troopship.GetAI().OrderQueue.Last.Value.TargetPlanet == null || troopship.GetAI().OrderQueue.Last.Value.TargetPlanet.system != entry.Key)
              
                //    {
                //        continue;
                //    }
                //    for (int i = 0; i < troopship.TroopList.Count; i++)
                //    {
                //        SystemCommander troopStrengthNeeded1 = entry.Value;
                //        troopStrengthNeeded1.TroopStrengthNeeded = troopStrengthNeeded1.TroopStrengthNeeded - (float)troopship.TroopList[i].Strength;
                //        TroopShips.QueuePendingRemoval(troopship);
                //    }
                //}
				
                for(int i=0;i < TroopShips.Count;i++)
                {
                    Ship troop = TroopShips[i];

                    if (troop == null || troop.TroopList.Count <=0)
                    {
                        TroopShips.QueuePendingRemoval(troop);
                        continue;
                    }
                        
                    ArtificialIntelligence  troopAI = troop.GetAI();
                    if (troopAI == null)
                    {
                        TroopShips.QueuePendingRemoval(troop);
                        continue;
                    }
                    if(troopAI.OrderQueue.Count >0 && troopAI.OrderQueue.Where(goal=> entry.Key.PlanetList.Contains(goal.TargetPlanet)).FirstOrDefault() !=null)
                    {
                        currentTroops++;
                        entry.Value.TroopStrengthNeeded--;
                        TroopShips.QueuePendingRemoval(troop);
                    }

                }
                TroopShips.ApplyPendingRemovals();
			}
			foreach (Ship ship4 in TroopShips)
			{

				IOrderedEnumerable<SolarSystem> sortedSystems = 
					from system in systems
                    orderby this.DefenseDict[system].ValueToUs descending
                    orderby Vector2.Distance(system.Position, ship4.Center)
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

                    Planet target = solarSystem2.PlanetList.OrderBy(planet => planet.TroopsHere.Where(troops => troops.GetOwner() == this.us).Count()).First();
                    ship4.GetAI().OrderRebase(target, true);
					
                    //List<Planet> Potentials = new List<Planet>();
                    //foreach (Planet p in solarSystem2.PlanetList)
                    //{
                    //    if (p.Owner == null || p.Owner != this.us)
                    //    {
                    //        continue;
                    //    }
                    //    Potentials.Add(p);
                    //}
                    //if (Potentials.Count <= 0)
                    //{
                    //    continue;
                    //}
                    //int Ran = (int)RandomMath.RandomBetween(0f, (float)Potentials.Count + 0.85f);
                    //if (Ran > Potentials.Count - 1)
                    //{
                    //    Ran = Potentials.Count - 1;
                    //}
                    //ship4.GetAI().OrderRebase(Potentials[Ran], true);
				}
			}
         


			foreach (Troop troop in GroundTroops)
			{
                if (troop == null || troop.GetPlanet() == null)
                    continue;
                Planet current = troop.GetPlanet();
                int troopshere = current.TroopsHere.Where(us=> us.GetOwner() == this.us).Count();
                SystemCommander defenseSystem = this.DefenseDict[current.ParentSystem];
                bool inneed = troopshere < defenseSystem.IdealTroopStr;
                Ship troopship = null;
                if (current == null || current.CombatTimer > 0 || current.ParentSystem.combatTimer>0
                   )
                    
				{
	
                    continue;
				}
                if (!inneed && troopshere > defenseSystem.IdealTroopStr)
                {
                    troopship = troop.Launch();
                }
                else
                    continue;
                if (troopship == null)
                {
                    continue;
                }
                IOrderedEnumerable<SolarSystem> sortedSystems = 
					from system in systems
                    orderby this.DefenseDict[system].ValueToUs descending
                    orderby (int)(Vector2.Distance(system.Position, current.Position) / (UniverseData.UniverseWidth / 5f))
                    orderby system.combatTimer descending
                    
					select system;
				foreach (SolarSystem solarSystem3 in sortedSystems)
				{
                    //added by gremlin Dont take troops from system that have combat. and prevent troop loop
                    if ( this.DefenseDict[solarSystem3].TroopStrengthNeeded <=0)
					{
						continue;
					}

                    if (solarSystem3.PlanetList.Count <= 0)
                    {
                        continue;


					
					}
                    SystemCommander item1 = this.DefenseDict[solarSystem3];                    
                    item1.TroopStrengthNeeded--;
                    Planet target = solarSystem3.PlanetList.OrderBy(planet => planet.TroopsHere.Where(troops => troops.GetOwner() == this.us).Count()).First();
                    troopship.GetAI().OrderRebase(target, true);
                    List<Planet> Potentials = new List<Planet>();
                    //foreach (Planet p in solarSystem3.PlanetList)
                    //{
                    //    if (p.Owner == null || p.Owner != this.us || (current != null && p == current) || p.TroopsHere.Count*10>= this.DefenseDict[p.ParentSystem].IdealTroopStr)
                    //    {
                    //        continue;
                    //    }
                    //    Potentials.Add(p);
                    //}
                    //if (Potentials.Count <= 0)
                    //{
                    //    continue;
                    //}
                    //int Ran = (int)RandomMath.RandomBetween(0f, (float)Potentials.Count + 0.85f);
                    //if (Ran > Potentials.Count - 1)
                    //{
                    //    Ran = Potentials.Count - 1;
                    //}
                    //troopship.GetAI().OrderRebase(Potentials[Ran], true);
				}
			}
		}
        public ConcurrentDictionary<Ship, List<Ship>> EnemyClumpsDict = new ConcurrentDictionary<Ship, List<Ship>>();

        public void refreshclumps()
        {
            this.EnemyClumpsDict.Clear();
     
            //List<Ship> ShipsAlreadyConsidered = new List<Ship>();
            



  
            List<Ship> incomingShips = new List<Ship>();
            if (Empire.universeScreen.GameDifficulty > UniverseData.GameDifficulty.Hard)
                incomingShips = Empire.universeScreen.MasterShipList.AsParallel().Where(bases => bases.BaseStrength > 0 && bases.loyalty != this.us && (bases.loyalty.isFaction || this.us.GetRelations()[bases.loyalty].AtWar || !this.us.GetRelations()[bases.loyalty].Treaty_OpenBorders)).ToList();
            else
            {
                incomingShips = us.GetShipsInOurBorders().Where(bases=> bases.BaseStrength >0).ToList();
            }
            


            if (incomingShips.Count == 0)
            {
                
                return;
            }
            var source = incomingShips.ToArray();

            List<Ship> ShipsAlreadyConsidered = new List<Ship>();
            var   rangePartitioner = Partitioner.Create(0, source.Length);
            Parallel.ForEach(rangePartitioner, (range, loopState) =>
                {
                    
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        //for (int i = 0; i < incomingShips.Count; i++)
                        {
                            //Ship ship = this.system.ShipList[i];
                            Ship ship = incomingShips[i];

                            if (ship != null && ship.loyalty != this.us
                                && (ship.loyalty.isFaction || this.us.GetRelations()[ship.loyalty].AtWar || !this.us.GetRelations()[ship.loyalty].Treaty_OpenBorders)
                                && !ShipsAlreadyConsidered.Contains(ship) && !this.EnemyClumpsDict.ContainsKey(ship))
                            {
                                //lock(this.EnemyClumpsDict)
                                this.EnemyClumpsDict.TryAdd(ship, new List<Ship>());
                                this.EnemyClumpsDict[ship].Add(ship);
                                lock(ShipsAlreadyConsidered)
                                ShipsAlreadyConsidered.Add(ship);
                                //for (int j = 0; j < this.system.ShipList.Count; j++)
                                //                             var source = Empire.universeScreen.MasterShipList.ToArray();
                                //     var rangePartitioner = Partitioner.Create(0, source.Length);

                                //     //Parallel.For(0, Empire.universeScreen.MasterShipList.Count, i =>  
                                //     Parallel.ForEach(rangePartitioner, (range, loopState) =>
                                //{
                                //    for (int i = range.Item1; i < range.Item2; i++)


                                //for (int j = 0; j < incomingShips.Count; j++)
                                for (int j = range.Item1; j < range.Item2; j++)
                                //Parallel.ForEach(rangePartitioner, (range, loopState) =>
                                {
                                    //for (int j = range.Item1; j < range.Item2; j++)
                                    {
                                        Ship otherShip = source[j]; //incomingShips[j];
                                        if (otherShip.loyalty != this.us && otherShip.loyalty == ship.loyalty && Vector2.Distance(ship.Center, otherShip.Center) < 15000f
                                            && !ShipsAlreadyConsidered.Contains(otherShip))
                                        {
                                            this.EnemyClumpsDict[ship].Add(otherShip);
                                        }
                                    }
                                }//);
                            }

                        }
                    }
                });
  
            
        }
		private float StrengthOf(Dictionary<Guid, Ship> dict)
		{
			float str = 0f;
			foreach (KeyValuePair<Guid, Ship> entry in dict)
			{
				str = str + entry.Value.GetStrength();
			}
			return str;
		}
	}
}