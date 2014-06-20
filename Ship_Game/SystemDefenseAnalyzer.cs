using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class SystemDefenseAnalyzer
	{
		public SolarSystem system;

		public float dValue;

		public List<PlanetDefenseAnalyzer> pdList;

		public float DesiredForceStrength;

		public float ActualForceStrength;

		public List<Ship> ShipsDefendingHere = new List<Ship>();

		public float ThreatLevel;

		public SystemDefenseAnalyzer()
		{
		}
	}
}