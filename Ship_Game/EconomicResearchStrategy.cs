using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class EconomicResearchStrategy
	{
		public string Name;

		public List<EconomicResearchStrategy.Tech> TechPath = new List<EconomicResearchStrategy.Tech>();

		public EconomicResearchStrategy()
		{
		}

		public struct Tech
		{
			public string id;
		}
	}
}