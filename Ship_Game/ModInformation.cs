using System;

namespace Ship_Game
{
    public sealed class ModInformation
    {
        public string ModName                = "";
        public string CustomMenuMusic        = "";
        public string ModDescription         = "";
        public bool DisableDefaultRaces;
        public string PortraitPath           = "";
        public string ModImagePath_1920x1280 = "";
        public string URL                    = "";
        public string Author                 = "";

        //added by Gremlin
        public string Version;
        public string BitbucketAPIString;
        public string DownLoadSite;
        public float GlobalExplosionVisualIncreaser     = 1f;
        public float GlobalShipExplosionVisualIncreaser = 1f;
        public int MaxOpponents = 7;

        //added by McShooterz
        public bool useAlternateTech;
        public bool useHullBonuses;
        public bool useWeaponModifiers;
        public bool removeRemnantStory;
        public bool useCombatRepair;
        public bool clearVanillaTechs;
        public bool clearVanillaWeapons;

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
        public bool reconDropDown;
        public bool ColoniserMenu;
        public bool ConstructionModule;

        //added by Fat Bastard
        public bool UseShieldWarpBehavior; // Player can set shield to turn off at warp, but upon exit they will have activation chance
        public bool AiPickShipsByStrength; // The AI will pick ships to build based on their Strength and game difficulty level 
        // instead of techs needed. This allows it to choose the toughest ships to build. This is notmalized by ship total slots
        // so ships with more slots of the same role wont get priority (bigger ships also cost more to build and maintain.
        public bool UseManualScriptedResearch; // Dont filter anyting, just use commands for manual research scripts.

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
        public float Spaceportscale = 0.8f;
        
        public int RemnantTechCount;


        // Doctor: Planet generation: % chance of each tile on this planet type being habitable. Default values as vanilla.
        public int BarrenHab = 0;
        public int IceHab    = 15;
        public int OceanHab  = 50;
        public int SteppeHab = 67;
        public int SwampHab  = 67;
        public int TerranHab = 75;

    }
}