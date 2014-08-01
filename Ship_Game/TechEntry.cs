using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class TechEntry
	{
		public string UID;

		public float Progress;

		public bool Discovered;

		public bool Unlocked;

        public string AcquiredFrom = "";
        //added by gremlin
        public bool shipDesignsCanuseThis = true;
        public float maxOffensiveValueFromthis = 0;

		public TechEntry()
		{
		}

		public Technology GetTech()
		{
			return ResourceManager.TechTree[this.UID];
		}
	}
}