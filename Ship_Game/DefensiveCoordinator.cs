using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;

namespace Ship_Game
{
	public class DefensiveCoordinator
	{
		private Empire us;

		public Dictionary<SolarSystem, SystemCommander> DefenseDict = new Dictionary<SolarSystem, SystemCommander>();

		public BatchRemovalCollection<Ship> DefensiveForcePool = new BatchRemovalCollection<Ship>();

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
            Parallel.ForEach(this.DefensiveForcePool, ship =>
            {
                int shipStr = (int)ship.GetStrength();
                Interlocked.Add(ref strength, shipStr);

                //safeadd  //SafeAddFloat(ref Strength, shipStr);       ßInterlocked
            });
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
						value.ValueToUs = value.ValueToUs + p.Population / 1000f;
						SystemCommander valueToUs = entry.Value;
						valueToUs.ValueToUs = valueToUs.ValueToUs + (p.MaxPopulation / 1000f - p.Population / 1000f);
						SystemCommander systemCommander = entry.Value;
						systemCommander.ValueToUs = systemCommander.ValueToUs + p.Fertility;
						SystemCommander value1 = entry.Value;
						value1.ValueToUs = value1.ValueToUs + p.MineralRichness;
                        if(this.us.data.Traits.Cybernetic >0)
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
					foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.us.GetRelations())
					{
						if (!Relationship.Value.AtWar || !fiveClosestSystem.OwnerList.Contains(Relationship.Key))
						{
							continue;
						}
						SystemCommander systemCommander2 = entry.Value;
						systemCommander2.ValueToUs = systemCommander2.ValueToUs + 10f;
					}
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
					if (defensiveForcePool.GetAI().SystemToDefend != null)
					{
						continue;
					}
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
				if (entry.Key == null)
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
				if (!(ship2.Role == "troop") || ship2.fleet != null || ship2.GetAI().State != AIState.AwaitingOrders)
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
					if (ship3.TroopList[i].GetOwner() == this.us)
					{
						TotalTroopStrength = TotalTroopStrength + (float)ship3.TroopList[i].Strength;
					}
				}
			}
			foreach (KeyValuePair<SolarSystem, SystemCommander> entry in this.DefenseDict)
			{
				entry.Value.IdealTroopStr = entry.Value.PercentageOfValue * TotalTroopStrength;
				entry.Value.TroopStrengthNeeded = entry.Value.PercentageOfValue * TotalTroopStrength;
				foreach (Planet p in entry.Key.PlanetList)
				{
					if (p.Owner != this.us )
					{
						continue;
					}
					foreach (Troop t in p.TroopsHere)
					{
						if (t.GetOwner() != this.us || entry.Value.TroopStrengthNeeded - (float)t.Strength < 0f)
						{
							continue;
						}
						SystemCommander troopStrengthNeeded = entry.Value;
						troopStrengthNeeded.TroopStrengthNeeded = troopStrengthNeeded.TroopStrengthNeeded - (float)t.Strength;
						GroundTroops.QueuePendingRemoval(t);
					}
				}
				GroundTroops.ApplyPendingRemovals();
				foreach (Ship troopship in TroopShips)
				{
					if (troopship.GetAI().OrderQueue.Count <= 0 || troopship.GetAI().OrderQueue.Last.Value.TargetPlanet == null || troopship.GetAI().OrderQueue.Last.Value.TargetPlanet.system != entry.Key)
					{
						continue;
					}
					for (int i = 0; i < troopship.TroopList.Count; i++)
					{
						SystemCommander troopStrengthNeeded1 = entry.Value;
						troopStrengthNeeded1.TroopStrengthNeeded = troopStrengthNeeded1.TroopStrengthNeeded - (float)troopship.TroopList[i].Strength;
						TroopShips.QueuePendingRemoval(troopship);
					}
				}
				TroopShips.ApplyPendingRemovals();
			}
			foreach (Ship ship4 in TroopShips)
			{
                //added by gremlin troop defense fix?
                if (ship4.TroopList.Count == 0 || ship4.GetAI().State != AIState.AwaitingOrders )
				{
					continue;
				}
				IOrderedEnumerable<SolarSystem> sortedSystems = 
					from system in systems
					orderby Vector2.Distance(system.Position, ship4.Center)
					select system;
				foreach (SolarSystem solarSystem2 in sortedSystems)
				{
					if ((float)ship4.TroopList[0].Strength >= this.DefenseDict[solarSystem2].TroopStrengthNeeded)
					{
						continue;
					}
					SystemCommander item = this.DefenseDict[solarSystem2];
					item.TroopStrengthNeeded = item.TroopStrengthNeeded - (float)ship4.TroopList[0].Strength;
					if (solarSystem2.PlanetList.Count <= 0)
					{
						continue;
					}
					List<Planet> Potentials = new List<Planet>();
					foreach (Planet p in solarSystem2.PlanetList)
					{
						if (p.Owner == null || p.Owner != this.us)
						{
							continue;
						}
						Potentials.Add(p);
					}
					if (Potentials.Count <= 0)
					{
						continue;
					}
					int Ran = (int)RandomMath.RandomBetween(0f, (float)Potentials.Count + 0.85f);
					if (Ran > Potentials.Count - 1)
					{
						Ran = Potentials.Count - 1;
					}
					ship4.GetAI().OrderRebase(Potentials[Ran], true);
				}
			}
			foreach (Troop troop in GroundTroops)
			{
                if (troop.GetPlanet() == null || troop.GetPlanet().CombatTimer > 0 || troop.GetPlanet().ParentSystem.combatTimer>0)
				{
					continue;
				}
				IOrderedEnumerable<SolarSystem> sortedSystems = 
					from system in systems
                    orderby (int)(Vector2.Distance(system.Position, troop.GetPlanet().Position) / (UniverseData.UniverseWidth /5f))
                    orderby system.combatTimer descending
                    
					select system;
				foreach (SolarSystem solarSystem3 in sortedSystems)
				{
                    //added by gremlin Dont take troops from system that have combat. and prevent troop loop
                    if (solarSystem3.combatTimer > 0 || (float)troop.Strength < this.DefenseDict[solarSystem3].TroopStrengthNeeded + (float)troop.Strength)
					{
						continue;
					}
					Ship troopship = troop.Launch();
					if (troopship == null)
					{
						continue;
					}
					SystemCommander item1 = this.DefenseDict[solarSystem3];
                    //added by gremlin... Instead of lowering needed strength when removing a strength. increase it.
					item1.TroopStrengthNeeded = item1.TroopStrengthNeeded + (float)troop.Strength;
					if (solarSystem3.PlanetList.Count <= 0)
					{
						continue;
					}
					List<Planet> Potentials = new List<Planet>();
					foreach (Planet p in solarSystem3.PlanetList)
					{
						if (p.Owner == null || p.Owner != this.us)
						{
							continue;
						}
						Potentials.Add(p);
					}
					if (Potentials.Count <= 0)
					{
						continue;
					}
					int Ran = (int)RandomMath.RandomBetween(0f, (float)Potentials.Count + 0.85f);
					if (Ran > Potentials.Count - 1)
					{
						Ran = Potentials.Count - 1;
					}
					troopship.GetAI().OrderRebase(Potentials[Ran], true);
				}
			}
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