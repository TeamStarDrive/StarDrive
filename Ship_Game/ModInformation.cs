using System;

namespace Ship_Game
{
	public class ModInformation
	{
		public string ModName = "";

		public string CustomMenuMusic = "";

		public string ModDescription = "";

		public bool DisableDefaultRaces;

		public string PortraitPath = "";

		public string ModImagePath_1920x1280 = "";

        //added by McShooterz
        public bool useRacialTech;
        public bool useAlternateTech;
        public bool useHullBonuses;
        public bool useWeaponModifiers;
        public bool removeRemnantStory;
        public bool useCombatRepair;

        //added by The Doctor
        public bool useDestroyers;
        public bool useDrones;
        public bool enableECM;
        public bool useProportionalUpkeep;
        public bool usePlanetaryProjection;
        public float UpkeepBaseline;
        public float UpkeepFighter;
        public float UpkeepCorvette;
        public float UpkeepFrigate;
        public float UpkeepCruiser;
        public float UpkeepCarrier;
        public float UpkeepCapital;
        public float UpkeepFreighter;
        public float UpkeepPlatform;
        public float UpkeepStation;
        public float UpkeepDrone;
        

		public ModInformation()
		{
		}
	}
}