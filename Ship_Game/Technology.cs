using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class Technology
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

		public List<LeadsToTech> LeadsTo = new List<LeadsToTech>();

		public List<UnlockedMod> ModulesUnlocked = new List<UnlockedMod>();

		public List<UnlockedBuilding> BuildingsUnlocked = new List<UnlockedBuilding>();

		public List<UnlockedBonus> BonusUnlocked = new List<UnlockedBonus>();

		public List<UnlockedTroop> TroopsUnlocked = new List<UnlockedTroop>();

		public List<UnlockedHull> HullsUnlocked = new List<UnlockedHull>();

        public List<TriggeredEvent> EventsTriggered = new List<TriggeredEvent>();

        public List<RevealedTech> TechsRevealed = new List<RevealedTech>();

        //Added by McShooterz to allow for techs with more than one level
        public byte MaxLevel = 1;

        //added by McShooterz: Racial Tech variables
        public List<RequiredRace> RaceRestrictions = new List<RequiredRace>();
        public List<RequiredRace> RaceExclusions = new List<RequiredRace>();
        public struct RequiredRace
        {
            public string ShipType;
        }

        //added by McShooterz: Alternate Tach variables
        public bool Militaristic;
        public bool unlockFrigates;
        public bool unlockCruisers;
        public bool unlockBattleships;
        public bool unlockCorvettes;

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