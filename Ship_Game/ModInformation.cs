using System;
using Microsoft.Xna.Framework;
using Ship_Game.GameScreens.MainMenu;

namespace Ship_Game
{
    public sealed class ModInformation
    {
        public string ModName         = "";
        public string CustomMenuMusic = "";
        public string ModDescription  = "";
        public bool DisableDefaultRaces;
        public string PortraitPath = "";
        public string URL          = "";
        public string Author       = "";

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
        public bool UseProportionalUpkeep;
        public bool reconDropDown;
        public bool ColoniserMenu;

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
        public bool UseResearchableShipTechs; // Use short term researchable techs with no best ship

        // Doctor: Planet generation: % chance of each tile on this planet type being habitable. Default values as vanilla.
        public int BarrenHab = 0;
        public int IceHab    = 15;
        public int OceanHab  = 50;
        public int SteppeHab = 67;
        public int SwampHab  = 67;
        public int TerranHab = 75;

        // Research costs will be increased based on map size to balance the increased capacity of larger maps
        public bool ChangeResearchCostBasedOnSize;
        public int CostBasedOnSizeThreshold = 2500;  // Allow tuning the change up/down

        public int DefaultNumOpponents = 7; // Default AIs to start on default settings
        public float HangarCombatShipCostMultiplier = 1;

        public int ChanceForCategory(PlanetCategory category)
        {
            switch (category)
            {
                default:
                case PlanetCategory.Other: return BarrenHab;
                case PlanetCategory.Volcanic: return BarrenHab;
                case PlanetCategory.GasGiant: return BarrenHab;
                case PlanetCategory.Barren: return BarrenHab;
                case PlanetCategory.Desert: return OceanHab;
                case PlanetCategory.Tundra: return OceanHab;
                case PlanetCategory.Oceanic: return OceanHab;
                case PlanetCategory.Steppe: return SteppeHab;
                case PlanetCategory.Terran: return TerranHab;
                case PlanetCategory.Ice:    return IceHab;
                case PlanetCategory.Swamp:  return SwampHab;
            }
        }
    }
}