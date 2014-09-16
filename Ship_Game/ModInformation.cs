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

        //added by Gremlin
        public string Version;

        //added by McShooterz
        public bool useAlternateTech;
        public bool useHullBonuses;
        public bool useWeaponModifiers;
        public bool removeRemnantStory;
        public bool useCombatRepair;
        public bool clearVanillaTechs;

        public bool useWarpCurve;
        public float curveFactor;
        public float MaxWarp;
        public float WarpSpeedMultiplier;

        //added by The Doctor
        public bool customMilTraitTechs;
        public bool customRemnantElements;
        public bool enableECM;
        public bool useDestroyers;
        public bool useDrones;
        public bool expandedWeaponCats;
        public bool overrideSecretsTree;
        public bool usePlanetaryProjection;
        public bool useProportionalUpkeep;
        
        

        public float ShipyardBonus;
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
        
        public int RemnantTechCount;
        

		public ModInformation()
		{
		}
	}
}