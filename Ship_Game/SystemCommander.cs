using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
        public float incomingThreatTime;

		public Dictionary<Guid, Ship> ShipsDict = new Dictionary<Guid, Ship>();

		public ConcurrentDictionary<Ship, List<Ship>> EnemyClumpsDict = new ConcurrentDictionary<Ship, List<Ship>>();

		private Empire us;

		public SystemCommander(Empire e, SolarSystem system)
		{
			this.system = system;
			this.us = e;
		}

		public void AssignTargets(ConcurrentDictionary<Ship,List<Ship>> EnemyClumpsDict)
		{
            this.EnemyClumpsDict.Clear();
            //this.EnemyClumpsDict = EnemyClumpsDict.Where(home => Vector2.Distance(home.Key.Position, this.system.Position) > 100000).ToDictionary(Ship,List<Ship>);
            
            //foreach(KeyValuePair<Ship,List<Ship>> home in EnemyClumpsDict)
            Parallel.ForEach(EnemyClumpsDict, (home,status) =>
            {
                if (Vector2.Distance(home.Key.Position, this.system.Position) > 100000)
                    return;
                    //continue;
                this.EnemyClumpsDict.TryAdd(home.Key, home.Value);
            });

            
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
				HashSet<Ship> AssignedShips = new HashSet<Ship>();
				foreach (Ship enemy in this.EnemyClumpsDict[distanceSorted.First<Ship>()])
				{
					float AssignedStr = 0f;
                    float strMod = 1;
                    strMod += (int)Empire.universeScreen.GameDifficulty *.10f;
                    float enemystrength = 0;
                    try
                    {
                         enemystrength = this.EnemyClumpsDict[enemy].Sum(str => str.GetStrength()) * strMod;
                    }
                    catch
                    {
                        if (enemy.GetAI() != null)
                            System.Diagnostics.Debug.WriteLine("enemy not in dictionary" + enemy.GetAI().State.ToString());
                        else
                            System.Diagnostics.Debug.WriteLine("enemy AI null not in dictionary: ");

                    }
					foreach (KeyValuePair<Guid, Ship> friendly in this.ShipsDict)
					{
						if (!friendly.Value.InCombat && Vector2.Distance(friendly.Value.Position,enemy.Position) / (friendly.Value.velocityMaximum >0?friendly.Value.velocityMaximum:1) <10)
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
                    
					ship.GetAI().OrderAttackSpecificTarget(AssignedShips.First().GetAI().Target as Ship);
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
				str = str + ship.Value.BaseStrength;//.GetStrength();
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