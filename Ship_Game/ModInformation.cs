namespace Ship_Game
{
    public sealed class ModInformation
    {
        public bool DisableDefaultRaces;
        public string ModName         = "";
        public string CustomMenuMusic = "";
        public string ModDescription  = "";
        public string PortraitPath    = "";
        public string URL             = "";
        public string Author          = "";

        //added by Gremlin
        public string Version;
        public string BitbucketAPIString;
        public string DownLoadSite;
        public float GlobalExplosionVisualIncreaser     = 1f;
        public float GlobalShipExplosionVisualIncreaser = 1f;
        public int MaxOpponents = 7;

        //added by McShooterz
        public bool UseHullBonuses;
        public bool RemoveRemnantStory;
        public bool UseCombatRepair;
        public bool ClearVanillaTechs;
        public bool ClearVanillaWeapons;

        //added by The Doctor
        public bool enableECM;
        public bool useDestroyers;
        public bool usePlanetaryProjection;
        public bool UseUpkeepByHullSize;
        public bool reconDropDown;

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
        public float SpaceportScale = 0.8f;

        // added by Fat Bastard
        public bool EnableShipTechLineFocusing; // Use short term researchable techs with no best ship
        public bool DisableShipPicker; // Disable the ship picker and use all techs that can be researched based on ship designs
        public string DefaultEventDrone; // In case an event building has defense drones and drones are not researched

        // How tougher are remnant designs in the mod. This affects starting fleet multipliers and also increases with difficulty. Vanilla is 2
        public float RemnantDesignStrMultiplier; 

        public string SupportedBlackBoxVersions;

        // Research costs will be increased based on map size to balance the increased capacity of larger maps
        public bool ChangeResearchCostBasedOnSize;
        public int CostBasedOnSizeThreshold = 2500;  // Allow tuning the change up/down

        public int DefaultNumOpponents = 7; // Default AIs to start on default settings
        public float HangarCombatShipCostMultiplier = 1;
        public bool DisplayEnvPerfInRaceDesign;

        // TRUE by default, but if set false, no vanilla designs will be loaded
        // from StarDrive/Content/ShipDesigns and the mod is responsible to provide all required designs
        public bool UseVanillaShips = true;
    }
}