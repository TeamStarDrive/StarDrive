using System;
using System.Collections.Generic;

namespace Ship_Game
{
    //Unused in game that i can see --gremlin
	public class MilitaryResearchStrategy
	{
		public string Name;

		public List<MilitaryResearchStrategy.Tech> HullPath = new List<MilitaryResearchStrategy.Tech>();

		public List<MilitaryResearchStrategy.Tech> DefensePath = new List<MilitaryResearchStrategy.Tech>();

		public List<MilitaryResearchStrategy.Tech> KineticPath = new List<MilitaryResearchStrategy.Tech>();

		public List<MilitaryResearchStrategy.Tech> EnergyPath = new List<MilitaryResearchStrategy.Tech>();

		public List<MilitaryResearchStrategy.Tech> BeamPath = new List<MilitaryResearchStrategy.Tech>();

		public List<MilitaryResearchStrategy.Tech> MissilePath = new List<MilitaryResearchStrategy.Tech>();

		public MilitaryResearchStrategy()
		{
		}

		public struct Tech
		{
			public string id;
		}
	}
}