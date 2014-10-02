using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class SolarSystemData
	{
		public string Name;

		public string SunPath = "star_yellow";

		public List<SolarSystemData.Ring> RingList = new List<SolarSystemData.Ring>();

		public SolarSystemData()
		{
		}

		public struct Ring
		{
			public string Planet;

			public string SpecialDescription;

			public int WhichPlanet;

			public string Asteroids;

			public string HasRings;

			public bool HomePlanet;

            public float planetScale;

			public string Owner;

			public string Station;

            public List<Moon> Moons;

			public List<string> BuildingList;

            public List<string> Guardians;

            public float MaxPopDefined;
		}

        public struct Moon
        {
            public int WhichMoon;
            public float MoonScale;
        }
	}
}