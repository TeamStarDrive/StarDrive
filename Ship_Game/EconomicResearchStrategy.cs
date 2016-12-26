using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class EconomicResearchStrategy
	{
		public string Name;

		public Array<Tech> TechPath = new Array<Tech>();

        public byte MilitaryPriority = 5;
        public byte ExpansionPriority = 5;
        public byte ResearchPriority = 5;
        public byte IndustryPriority = 5;

		public EconomicResearchStrategy()
		{
		}

		public struct Tech
		{
			public string id;
		}
	}
}