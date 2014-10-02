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

		public byte RootNode;

        public TechnologyType TechnologyType = TechnologyType.General;

		public int NameIndex;

		public int DescriptionIndex;

		public List<Technology.LeadsToTech> LeadsTo = new List<Technology.LeadsToTech>();

		public List<Technology.UnlockedMod> ModulesUnlocked = new List<Technology.UnlockedMod>();

		public List<Technology.UnlockedBuilding> BuildingsUnlocked = new List<Technology.UnlockedBuilding>();

		public List<Technology.UnlockedBonus> BonusUnlocked = new List<Technology.UnlockedBonus>();

		public List<Technology.UnlockedTroop> TroopsUnlocked = new List<Technology.UnlockedTroop>();

		public List<Technology.UnlockedHull> HullsUnlocked = new List<Technology.UnlockedHull>();

        public List<Technology.TriggeredEvent> EventsTriggered = new List<Technology.TriggeredEvent>();

        public List<Technology.RevealedTech> TechsRevealed = new List<Technology.RevealedTech>();

        //Added by McShooterz to allow for techs with more than one level
        public byte MaxLevel = 1;

        //added by McShooterz: Racial Tech variables
        public List<Technology.RequiredRace> RaceRestrictions = new List<Technology.RequiredRace>();
        public struct RequiredRace
        {
            public string ShipType;
        }

        //added by McShooterz: Alternate Tach variables
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

            public string Type;

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

            public string Type;
		}

		public struct UnlockedHull
		{
			public string Name;

			public string ShipType;
		}

		public struct UnlockedMod
		{
			public string ModuleUID;

            public string Type;
		}

		public struct UnlockedTroop
		{
			public string Name;

			public string Type;
		}

        public struct TriggeredEvent
        {
            public string EventUID;
            public string Type;
            public string CustomMessage;
        }

        public struct RevealedTech
        {
            public string RevUID;
            public string Type;
        }
	}
}