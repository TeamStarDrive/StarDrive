using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Ship_Game
{
	public class SystemCommander
	{
		public SolarSystem system;

		public float ValueToUs;

		public float IdealTroopStr;

		public float TroopStrengthNeeded;

		public float IdealShipStrength;

		public float PercentageOfValue;

		public Dictionary<Guid, Ship> ShipsDict = new Dictionary<Guid, Ship>();

		public Dictionary<Ship, List<Ship>> EnemyClumpsDict = new Dictionary<Ship, List<Ship>>();

		private Empire us;

		public SystemCommander(Empire e, SolarSystem system)
		{
			this.system = system;
			this.us = e;
		}

		public void AssignTargets()
		{
			
            this.EnemyClumpsDict.Clear();
            if (this.ShipsDict.Count == 0)
                return;
			List<Ship> ShipsAlreadyConsidered = new List<Ship>();

			foreach (KeyValuePair<Guid, Ship> entry in this.ShipsDict)
			{
				Ship ship = entry.Value;
                if (ship == null || ship.GetAI().Target != null && ship.GetAI().Target.GetSystem() != null
                    && ship.GetAI().Target.GetSystem() == this.system)//(ship.GetAI().Target.GetSystem() == null ||) ship.GetAI().Target == null  ||
				{
					continue;
				}
				ship.GetAI().Target = null;
				ship.GetAI().hasPriorityTarget = false;
				ship.GetAI().Intercepting = false;
				ship.GetAI().SystemToDefend = null;
			}

			//for (int i = 0; i < this.system.ShipList.Count; i++)
            for (int i=0 ; i< this.us.GetShipsInOurBorders().Count;i++)
			{
				//Ship ship = this.system.ShipList[i];
                Ship ship = this.us.GetShipsInOurBorders()[i];
                if (ship != null && ship.loyalty != this.us 
                    && (ship.loyalty.isFaction || this.us.GetRelations()[ship.loyalty].AtWar || !this.us.GetRelations()[ship.loyalty].Treaty_OpenBorders) 
                    && !ShipsAlreadyConsidered.Contains(ship) && !this.EnemyClumpsDict.ContainsKey(ship))
				{
					this.EnemyClumpsDict.Add(ship, new List<Ship>());
					this.EnemyClumpsDict[ship].Add(ship);
					ShipsAlreadyConsidered.Add(ship);
					//for (int j = 0; j < this.system.ShipList.Count; j++)
                    for (int j = 0; j < this.us.GetShipsInOurBorders().Count; j++)
					{
                        Ship otherShip = this.us.GetShipsInOurBorders()[j];
						if (otherShip.loyalty != this.us && otherShip.loyalty == ship.loyalty && Vector2.Distance(ship.Center, otherShip.Center) < 15000f && !ShipsAlreadyConsidered.Contains(otherShip))
						{
							this.EnemyClumpsDict[ship].Add(otherShip);
						}
					}
				}
			}
			if (this.EnemyClumpsDict.Count != 0 && this.ShipsDict.Count !=0)
			{
				List<Ship> ClumpsList = new List<Ship>();
				foreach (KeyValuePair<Ship, List<Ship>> entry in this.EnemyClumpsDict)//.OrderBy(entry => Vector2.Distance(entry.Key.Position,this.system.Position)))
				{
					ClumpsList.Add(entry.Key);
				}
				IOrderedEnumerable<Ship> distanceSorted = 
					from clumpPos in ClumpsList
					orderby Vector2.Distance(this.system.Position, clumpPos.Center)
					select clumpPos;
				List<Ship> AssignedShips = new List<Ship>();
				foreach (Ship enemy in this.EnemyClumpsDict[distanceSorted.First<Ship>()])
				{
					float AssignedStr = 0f;
                    float strMod = 1;
                    strMod += (int)Empire.universeScreen.GameDifficulty *.10f;
                    float enemystrength = this.EnemyClumpsDict[enemy].Sum(str => str.GetStrength()) * strMod;
					foreach (KeyValuePair<Guid, Ship> friendly in this.ShipsDict)
					{
						if (!friendly.Value.InCombat)
						{
                            if (AssignedShips.Contains(friendly.Value) || AssignedStr != 0f && AssignedStr >= enemystrength || friendly.Value.GetAI().State == AIState.Resupply)
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
				List<Ship> UnassignedShips = new List<Ship>();
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
					if (ship.GetAI().State == AIState.Resupply)
					{
						continue;
					}
					ship.GetAI().Intercepting = true;
					ship.GetAI().OrderAttackSpecificTarget(AssignedShips[0].GetAI().Target as Ship);
				}
			}
            //else if(this.us.GetShipsInOurBorders().Count >0)
            else 
            {
                //if (this.us.GetShipsInOurBorders().Count() > 0)
                //{
                //    foreach (Ship ship in this.ShipsDict.Values)
                //    {
                //        if (ship.GetAI().Target != null || ship.GetAI().Intercepting || (ship.GetAI().State != AIState.AwaitingOrders && ship.GetAI().State != AIState.SystemDefender))
                //            continue;
                //        ship.GetAI().OrderAttackSpecificTarget(this.us.GetShipsInOurBorders().OrderBy(distance => Vector2.Distance(ship.Position, distance.Position)).First());
                //    }
                //}  
                //else
				foreach (KeyValuePair<Guid, Ship> ship in this.ShipsDict)
				{
                    if (ship.Value.GetAI().State == AIState.Resupply || ship.Value.InCombat || ship.Value.GetAI().Target!=null)
					{
						continue;
					}
					ship.Value.GetAI().OrderSystemDefense(this.system);
				}
			}
		}

		public float GetOurStrength()
		{
			float str = 0f;
			foreach (KeyValuePair<Guid, Ship> ship in this.ShipsDict)
			{
				str = str + ship.Value.GetStrength();
			}
			return str;
		}

		public List<Ship> GetShipList()
		{
			List<Ship> retlist = new List<Ship>();
			foreach (KeyValuePair<Guid, Ship> ship in this.ShipsDict)
			{
				retlist.Add(ship.Value);
			}
			return retlist;
		}
	}
}