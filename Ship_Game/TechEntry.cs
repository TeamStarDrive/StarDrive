using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class TechEntry
	{
		public string UID;
		public float Progress;
		public bool Discovered;
		public bool Unlocked;
        public int  Level;

        public float GetTechCost()
        {
            return GetTech().Cost * (float)Math.Max(1, Math.Pow( 2.0, Level));
        }

        public string AcquiredFrom = "";
        //added by gremlin
        public bool shipDesignsCanuseThis = true;
        public float maxOffensiveValueFromthis = 0;

		public TechEntry()
		{
		}

		public Technology GetTech()
		{
			return ResourceManager.TechTree[UID];
		}
	}
}