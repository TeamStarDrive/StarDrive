using System;
using System.Collections.Generic;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
	public sealed class War
	{
		public WarType WarType;
		public float OurStartingStrength;
		public float TheirStartingStrength;
		public float OurStartingGroundStrength;
		public int OurStartingColonies;
		public float TheirStartingGroundStrength;
		public float StrengthKilled;
		public float StrengthLost;
		public float TroopsKilled;
		public float TroopsLost;
		public int ColoniestWon;
		public int ColoniesLost;
		public Array<string> AlliesCalled = new Array<string>();
		public Array<Guid> ContestedSystemsGUIDs = new Array<Guid>();
		public float TurnsAtWar;
		public float EndStarDate;
		public float StartDate;
		private Empire Us;
		public string UsName;
		public string ThemName;
		private Empire Them;
		public int StartingNumContestedSystems;

		public War()
		{
		}

		public War(Empire us, Empire them, float StarDate)
		{
			StartDate = StarDate;
			Us = us;
			Them = them;
			UsName = us.data.Traits.Name;
			ThemName = them.data.Traits.Name;
			foreach (Ship ship in us.GetShips())
			{
				War ourStartingStrength = this;
				ourStartingStrength.OurStartingStrength = ourStartingStrength.OurStartingStrength + ship.GetStrength();
				foreach (Troop t in ship.TroopList)
				{
					War ourStartingGroundStrength = this;
					ourStartingGroundStrength.OurStartingGroundStrength = ourStartingGroundStrength.OurStartingGroundStrength + t.Strength;
				}
			}
			foreach (Planet p in us.GetPlanets())
			{
				War ourStartingColonies = this;
				ourStartingColonies.OurStartingColonies = ourStartingColonies.OurStartingColonies + 1;
                using (p.TroopsHere.AcquireReadLock())
                    foreach (Troop t in p.TroopsHere)
                    {
                        if (t.GetOwner() != us)
                        {
                            continue;
                        }
                        War war = this;
                        war.OurStartingGroundStrength = war.OurStartingGroundStrength + t.Strength;

                    }
			}
			foreach (Ship ship in them.GetShips())
			{
				War theirStartingStrength = this;
				theirStartingStrength.TheirStartingStrength = theirStartingStrength.TheirStartingStrength + ship.GetStrength();
				foreach (Troop t in ship.TroopList)
				{
					War theirStartingGroundStrength = this;
					theirStartingGroundStrength.TheirStartingGroundStrength = theirStartingGroundStrength.TheirStartingGroundStrength + t.Strength;
				}
			}
			foreach (Planet p in them.GetPlanets())
			{
                using (p.TroopsHere.AcquireReadLock())
                foreach (Troop t in p.TroopsHere)
				{
					if (t.GetOwner() != them)
					{
						continue;
					}
					War theirStartingGroundStrength1 = this;
					theirStartingGroundStrength1.TheirStartingGroundStrength = theirStartingGroundStrength1.TheirStartingGroundStrength + t.Strength;
				}
			}
			foreach (KeyValuePair<Guid, SolarSystem> system in Empire.Universe.SolarSystemDict)
			{
				bool WeAreThere = false;
				bool TheyAreThere = false;
				if (system.Value.OwnerList.Contains(Us))
				{
					WeAreThere = true;
				}
				if (system.Value.OwnerList.Contains(Them))
				{
					TheyAreThere = true;
				}
				if (!WeAreThere || !TheyAreThere)
				{
					continue;
				}
				War startingNumContestedSystems = this;
				startingNumContestedSystems.StartingNumContestedSystems = startingNumContestedSystems.StartingNumContestedSystems + 1;
				ContestedSystemsGUIDs.Add(system.Key);
			}
		}

		public WarState GetBorderConflictState()
		{
			float strengthKilled = StrengthKilled / (StrengthLost + 0.01f);
			if (StartingNumContestedSystems == 0)
			{
				return GetWarScoreState();
			}
			if (GetContestedSystemDifferential() == StartingNumContestedSystems && StartingNumContestedSystems > 0)
			{
				return WarState.EvenlyMatched;
			}
			if (GetContestedSystemDifferential() > 0)
			{
				if (GetContestedSystemDifferential() == StartingNumContestedSystems)
				{
					return WarState.Dominating;
				}
				return WarState.WinningSlightly;
			}
			if (GetContestedSystemDifferential() == -StartingNumContestedSystems)
			{
				return WarState.LosingBadly;
			}
			return WarState.LosingSlightly;
		}

		public WarState GetBorderConflictState(Array<Planet> ColoniesOffered)
		{
			float strengthKilled = StrengthKilled / (StrengthLost + 0.01f);
			if (StartingNumContestedSystems == 0)
			{
				return GetWarScoreState();
			}
			if (GetContestedSystemDifferential(ColoniesOffered) == StartingNumContestedSystems && StartingNumContestedSystems > 0)
			{
				return WarState.EvenlyMatched;
			}
			if (GetContestedSystemDifferential(ColoniesOffered) > 0)
			{
				if (GetContestedSystemDifferential(ColoniesOffered) == StartingNumContestedSystems)
				{
					return WarState.Dominating;
				}
				return WarState.WinningSlightly;
			}
			if (GetContestedSystemDifferential(ColoniesOffered) == -StartingNumContestedSystems)
			{
				return WarState.LosingBadly;
			}
			return WarState.LosingSlightly;
		}

		public int GetContestedSystemDifferential(Array<Planet> ColoniesOffered)
		{
			Array<Guid> guids = ContestedSystemsGUIDs;
			foreach (Planet p in ColoniesOffered)
			{
				if (guids.Contains(p.ParentSystem.guid))
				{
					continue;
				}
				guids.Add(p.ParentSystem.guid);
			}
			int num = 0;
			foreach (Guid guid in guids)
			{
				bool WeAreThere = false;
				bool TheyAreThere = false;
				if (Empire.Universe.SolarSystemDict[guid].OwnerList.Contains(Us))
				{
					WeAreThere = true;
				}
				if (Empire.Universe.SolarSystemDict[guid].OwnerList.Contains(Them))
				{
					TheyAreThere = true;
				}
				if (!WeAreThere || TheyAreThere)
				{
					if (!TheyAreThere || WeAreThere)
					{
						continue;
					}
					num--;
				}
				else
				{
					num++;
				}
			}
			return num;
		}

		public int GetContestedSystemDifferential()
		{
			int num = 0;
			foreach (Guid guid in ContestedSystemsGUIDs)
			{
				bool WeAreThere = false;
				bool TheyAreThere = false;
				if (Empire.Universe.SolarSystemDict[guid].OwnerList.Contains(Us))
				{
					WeAreThere = true;
				}
				if (Empire.Universe.SolarSystemDict[guid].OwnerList.Contains(Them))
				{
					TheyAreThere = true;
				}
				if (!WeAreThere || TheyAreThere)
				{
					if (!TheyAreThere || WeAreThere)
					{
						continue;
					}
					num--;
				}
				else
				{
					num++;
				}
			}
			return num;
		}

		public WarState GetWarScoreState()
		{
			float totalThreatAgainstUs = 0f;
			foreach (KeyValuePair<Empire, Relationship> r in Us.AllRelations)
			{
				if (r.Key.isFaction || r.Key.data.Defeated || !r.Value.AtWar)
				{
					continue;
				}
				totalThreatAgainstUs = totalThreatAgainstUs + r.Key.MilitaryScore;
			}
			if (totalThreatAgainstUs / (Us.MilitaryScore + 0.01f) <= 1f)
			{
				float ColonyPercentage = Us.GetPlanets().Count / (0.01f + OurStartingColonies);
				if (ColonyPercentage > 1.25f)
				{
					return WarState.Dominating;
				}
				if (ColonyPercentage < 0.75f)
				{
					return WarState.LosingSlightly;
				}
				if (ColonyPercentage < 0.5f)
				{
					return WarState.LosingBadly;
				}
				float SpaceWarKD = StrengthKilled / (StrengthLost + 0.01f);
				float troopsKilled = TroopsKilled / (TroopsLost + 0.01f);
				if (SpaceWarKD == 0f)
				{
					return WarState.Dominating;
				}
				if (SpaceWarKD > 1.5f)
				{
					return WarState.Dominating;
				}
				if (SpaceWarKD > 0.75f)
				{
					return WarState.WinningSlightly;
				}
				if (SpaceWarKD > 0.35f)
				{
					return WarState.EvenlyMatched;
				}
				if (SpaceWarKD > 0.15)
				{
					return WarState.LosingSlightly;
				}
				return WarState.LosingBadly;
			}
			float ColonyPercentage0 = Us.GetPlanets().Count / (0.01f + OurStartingColonies);
			if (ColonyPercentage0 < 0.75f)
			{
				return WarState.LosingSlightly;
			}
			if (ColonyPercentage0 < 0.5f)
			{
				return WarState.LosingBadly;
			}
			if (StrengthKilled < 250f && StrengthLost < 250f && Us.GetPlanets().Count == OurStartingColonies)
			{
				return WarState.ColdWar;
			}
			float SpaceWarKD0 = StrengthKilled / (StrengthLost + 0.01f);
			float single = TroopsKilled / (TroopsLost + 0.01f);
			if (SpaceWarKD0 > 2f)
			{
				return WarState.Dominating;
			}
			if (SpaceWarKD0 > 1.15f)
			{
				return WarState.WinningSlightly;
			}
			if (SpaceWarKD0 > 0.85f)
			{
				return WarState.EvenlyMatched;
			}
			if (SpaceWarKD0 > 0.5)
			{
				return WarState.LosingSlightly;
			}
			return WarState.LosingBadly;
		}

		public void SetCombatants(Empire u, Empire t)
		{
			Us = u;
			Them = t;
		}
	}
}