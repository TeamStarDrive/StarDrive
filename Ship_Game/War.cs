using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class War
	{
		public Ship_Game.WarType WarType;

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

		public List<string> AlliesCalled = new List<string>();

		public List<Guid> ContestedSystemsGUIDs = new List<Guid>();

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
			this.StartDate = StarDate;
			this.Us = us;
			this.Them = them;
			this.UsName = us.data.Traits.Name;
			this.ThemName = them.data.Traits.Name;
			foreach (Ship ship in us.GetShips())
			{
				War ourStartingStrength = this;
				ourStartingStrength.OurStartingStrength = ourStartingStrength.OurStartingStrength + ship.GetStrength();
				foreach (Troop t in ship.TroopList)
				{
					War ourStartingGroundStrength = this;
					ourStartingGroundStrength.OurStartingGroundStrength = ourStartingGroundStrength.OurStartingGroundStrength + (float)t.Strength;
				}
			}
			foreach (Planet p in us.GetPlanets())
			{
				War ourStartingColonies = this;
				ourStartingColonies.OurStartingColonies = ourStartingColonies.OurStartingColonies + 1;
				foreach (Troop t in p.TroopsHere)
				{
					if (t.GetOwner() != us)
					{
						continue;
					}
					War war = this;
					war.OurStartingGroundStrength = war.OurStartingGroundStrength + (float)t.Strength;
				}
			}
			foreach (Ship ship in them.GetShips())
			{
				War theirStartingStrength = this;
				theirStartingStrength.TheirStartingStrength = theirStartingStrength.TheirStartingStrength + ship.GetStrength();
				foreach (Troop t in ship.TroopList)
				{
					War theirStartingGroundStrength = this;
					theirStartingGroundStrength.TheirStartingGroundStrength = theirStartingGroundStrength.TheirStartingGroundStrength + (float)t.Strength;
				}
			}
			foreach (Planet p in them.GetPlanets())
			{
				foreach (Troop t in p.TroopsHere)
				{
					if (t.GetOwner() != them)
					{
						continue;
					}
					War theirStartingGroundStrength1 = this;
					theirStartingGroundStrength1.TheirStartingGroundStrength = theirStartingGroundStrength1.TheirStartingGroundStrength + (float)t.Strength;
				}
			}
			foreach (KeyValuePair<Guid, SolarSystem> system in this.Us.GetUS().SolarSystemDict)
			{
				bool WeAreThere = false;
				bool TheyAreThere = false;
				if (system.Value.OwnerList.Contains(this.Us))
				{
					WeAreThere = true;
				}
				if (system.Value.OwnerList.Contains(this.Them))
				{
					TheyAreThere = true;
				}
				if (!WeAreThere || !TheyAreThere)
				{
					continue;
				}
				War startingNumContestedSystems = this;
				startingNumContestedSystems.StartingNumContestedSystems = startingNumContestedSystems.StartingNumContestedSystems + 1;
				this.ContestedSystemsGUIDs.Add(system.Key);
			}
		}

		public WarState GetBorderConflictState()
		{
			float strengthKilled = this.StrengthKilled / (this.StrengthLost + 0.01f);
			if (this.StartingNumContestedSystems == 0)
			{
				return this.GetWarScoreState();
			}
			if (this.GetContestedSystemDifferential() == this.StartingNumContestedSystems && this.StartingNumContestedSystems > 0)
			{
				return WarState.EvenlyMatched;
			}
			if (this.GetContestedSystemDifferential() > 0)
			{
				if (this.GetContestedSystemDifferential() == this.StartingNumContestedSystems)
				{
					return WarState.Dominating;
				}
				return WarState.WinningSlightly;
			}
			if (this.GetContestedSystemDifferential() == -this.StartingNumContestedSystems)
			{
				return WarState.LosingBadly;
			}
			return WarState.LosingSlightly;
		}

		public WarState GetBorderConflictState(List<Planet> ColoniesOffered)
		{
			float strengthKilled = this.StrengthKilled / (this.StrengthLost + 0.01f);
			if (this.StartingNumContestedSystems == 0)
			{
				return this.GetWarScoreState();
			}
			if (this.GetContestedSystemDifferential(ColoniesOffered) == this.StartingNumContestedSystems && this.StartingNumContestedSystems > 0)
			{
				return WarState.EvenlyMatched;
			}
			if (this.GetContestedSystemDifferential(ColoniesOffered) > 0)
			{
				if (this.GetContestedSystemDifferential(ColoniesOffered) == this.StartingNumContestedSystems)
				{
					return WarState.Dominating;
				}
				return WarState.WinningSlightly;
			}
			if (this.GetContestedSystemDifferential(ColoniesOffered) == -this.StartingNumContestedSystems)
			{
				return WarState.LosingBadly;
			}
			return WarState.LosingSlightly;
		}

		public int GetContestedSystemDifferential(List<Planet> ColoniesOffered)
		{
			List<Guid> guids = this.ContestedSystemsGUIDs;
			foreach (Planet p in ColoniesOffered)
			{
				if (guids.Contains(p.system.guid))
				{
					continue;
				}
				guids.Add(p.system.guid);
			}
			int num = 0;
			foreach (Guid guid in guids)
			{
				bool WeAreThere = false;
				bool TheyAreThere = false;
				if (this.Us.GetUS().SolarSystemDict[guid].OwnerList.Contains(this.Us))
				{
					WeAreThere = true;
				}
				if (this.Them.GetUS().SolarSystemDict[guid].OwnerList.Contains(this.Them))
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
			foreach (Guid guid in this.ContestedSystemsGUIDs)
			{
				bool WeAreThere = false;
				bool TheyAreThere = false;
				if (this.Us.GetUS().SolarSystemDict[guid].OwnerList.Contains(this.Us))
				{
					WeAreThere = true;
				}
				if (this.Them.GetUS().SolarSystemDict[guid].OwnerList.Contains(this.Them))
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
			foreach (KeyValuePair<Empire, Relationship> r in this.Us.GetRelations())
			{
				if (r.Key.isFaction || r.Key.data.Defeated || !r.Value.AtWar)
				{
					continue;
				}
				totalThreatAgainstUs = totalThreatAgainstUs + r.Key.MilitaryScore;
			}
			if (totalThreatAgainstUs / (this.Us.MilitaryScore + 0.01f) <= 1f)
			{
				float ColonyPercentage = (float)this.Us.GetPlanets().Count / (0.01f + (float)this.OurStartingColonies);
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
				float SpaceWarKD = this.StrengthKilled / (this.StrengthLost + 0.01f);
				float troopsKilled = this.TroopsKilled / (this.TroopsLost + 0.01f);
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
				if ((double)SpaceWarKD > 0.15)
				{
					return WarState.LosingSlightly;
				}
				return WarState.LosingBadly;
			}
			float ColonyPercentage0 = (float)this.Us.GetPlanets().Count / (0.01f + (float)this.OurStartingColonies);
			if (ColonyPercentage0 < 0.75f)
			{
				return WarState.LosingSlightly;
			}
			if (ColonyPercentage0 < 0.5f)
			{
				return WarState.LosingBadly;
			}
			if (this.StrengthKilled < 250f && this.StrengthLost < 250f && this.Us.GetPlanets().Count == this.OurStartingColonies)
			{
				return WarState.ColdWar;
			}
			float SpaceWarKD0 = this.StrengthKilled / (this.StrengthLost + 0.01f);
			float single = this.TroopsKilled / (this.TroopsLost + 0.01f);
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
			if ((double)SpaceWarKD0 > 0.5)
			{
				return WarState.LosingSlightly;
			}
			return WarState.LosingBadly;
		}

		public void SetCombatants(Empire u, Empire t)
		{
			this.Us = u;
			this.Them = t;
		}
	}
}