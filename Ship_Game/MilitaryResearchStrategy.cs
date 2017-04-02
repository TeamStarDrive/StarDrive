using System;
using System.Collections.Generic;

namespace Ship_Game
{
    //Unused in game that i can see --gremlin
	public sealed class MilitaryResearchStrategy
	{
		public string Name;

		public Array<MilitaryResearchStrategy.Tech> HullPath = new Array<MilitaryResearchStrategy.Tech>();

		public Array<MilitaryResearchStrategy.Tech> DefensePath = new Array<MilitaryResearchStrategy.Tech>();

		public Array<MilitaryResearchStrategy.Tech> KineticPath = new Array<MilitaryResearchStrategy.Tech>();

		public Array<MilitaryResearchStrategy.Tech> EnergyPath = new Array<MilitaryResearchStrategy.Tech>();

		public Array<MilitaryResearchStrategy.Tech> BeamPath = new Array<MilitaryResearchStrategy.Tech>();

		public Array<MilitaryResearchStrategy.Tech> MissilePath = new Array<MilitaryResearchStrategy.Tech>();

		public MilitaryResearchStrategy()
		{
		}

		public struct Tech
		{
			public string id;
		}
	}
}