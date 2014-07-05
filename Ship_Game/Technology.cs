using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class Technology
	{
		public string UID;

		public string IconPath;

		public float Cost;

		public bool Secret;

		public bool Discovered;

		public int RootNode;

		public string Name;

		public int NameIndex;

		public int DescriptionIndex;

		public string LongDescription;

		public List<Technology.LeadsToTech> LeadsTo = new List<Technology.LeadsToTech>();

		public List<Technology.UnlockedMod> ModulesUnlocked = new List<Technology.UnlockedMod>();

		public List<Technology.UnlockedBuilding> BuildingsUnlocked = new List<Technology.UnlockedBuilding>();

		public List<Technology.UnlockedBonus> BonusUnlocked = new List<Technology.UnlockedBonus>();

		public List<Technology.UnlockedTroop> TroopsUnlocked = new List<Technology.UnlockedTroop>();

		public List<Technology.UnlockedHull> HullsUnlocked = new List<Technology.UnlockedHull>();

        //added by McShooterz: Racial Tech variables
        public List<Technology.RequiredRace> RaceRestrictions = new List<Technology.RequiredRace>();
        public struct RequiredRace
        {
            public string ShipType;
        }

        //added by McShooterz
        public bool Militaristic;
        public bool unlockFrigates;
        public bool unlockCruisers;
        public bool unlockBattleships;

		public Technology()
		{
		}

		public struct LeadsToTech
		{
			public string UID;
		}

		public class UnlockedBonus
		{
			public string Name;

			public string BonusType;

			public List<string> Tags;

			public float Bonus;

			public string Description;

			public int BonusIndex;

			public int BonusNameIndex;

			public UnlockedBonus()
			{
			}
		}

		public struct UnlockedBuilding
		{
			public string Name;
		}

		public struct UnlockedHull
		{
			public string Name;

			public string ShipType;
		}

		public struct UnlockedMod
		{
			public string ModuleUID;
		}

		public struct UnlockedTroop
		{
			public string Name;

			public string Type;
		}
	}
}