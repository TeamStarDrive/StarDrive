using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class SolarSystemData
	{
		public string Name;
		public string SunPath = "star_yellow";
		public List<Ring> RingList = new List<Ring>();

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
            //Using a separate boolean to ensure that modders can define an unusual 0-habitability planet (e.g. 0 tile Terran); otherwise would have to disregard 0.
            public bool UniqueHabitat;
            public int UniqueHabPC;
		}

        public struct Moon
        {
            public int WhichMoon;
            public float MoonScale;
        }
	}
}