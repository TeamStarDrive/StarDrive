using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class Mole
	{
		public Guid PlanetGuid;

		public Mole()
		{
		}

		public static Mole PlantMole(Empire Owner, Empire Target)
		{
			List<Planet> Potentials = new List<Planet>();
			foreach (Planet p in Target.GetPlanets())
			{
				if (!p.ExploredDict[Owner])
				{
					continue;
				}
				bool GoodPlanet = true;
				foreach (Mole m in Target.data.MoleList)
				{
					if (m.PlanetGuid != p.guid)
					{
						continue;
					}
					GoodPlanet = false;
					break;
				}
				if (!GoodPlanet)
				{
					break;
				}
				Potentials.Add(p);
			}
			if (Potentials.Count == 0)
			{
				Potentials = Target.GetPlanets();
			}
			Mole mole = null;
			if (Potentials.Count > 0)
			{
				int Random = (int)RandomMath.RandomBetween(0f, (float)Potentials.Count + 0.7f);
				if (Random > Potentials.Count - 1)
				{
					Random = Potentials.Count - 1;
				}
				mole = new Mole()
				{
					PlanetGuid = Potentials[Random].guid
				};
				Owner.data.MoleList.Add(mole);
			}
			return mole;
		}
	}
}