using Ship_Game.Data.Serialization;

namespace Ship_Game;

/// <summary>
/// Global gameplay settings
/// This is configurable from Content/Globals.yaml or Mods/MyMod/Globals.yaml
/// It should contain GamePlay or Mod related settings,
/// in contrast with general engine settings
/// </summary>
[StarDataType]
public class GamePlayGlobals
{
    [StarData] public int MaxOpponents = 7;
    [StarData] public int DefaultNumOpponents = 5; // Default AIs to start on default settings
    
    
    // gameplay modifiers
    // How tougher are remnant designs in the mod. This affects starting fleet multipliers and also increases with difficulty. Vanilla is 2
    [StarData] public float RemnantDesignStrMultiplier; 
    [StarData] public int CostBasedOnSizeThreshold = 2500;  // Allow tuning the change up/down
    [StarData] public float HangarCombatShipCostMultiplier = 1;
    [StarData] public float ShipyardBonus;
    [StarData] public float CustomMineralDecay = 1;
    [StarData] public float VolcanicActivity = 1;
    // sets the default gravity well range, 0 means disabled
    [StarData] public float GravityWellRange = 8000;
    // base richness for empire capitals
    [StarData] public float StartingPlanetRichness = 1;
    [StarData] public float ShipMaintenanceMultiplier = 1;
    // How much rushing costs in percentage of production cost
    [StarData] public float RushCostPercentage = 1;
    // minimum ship warp range which is accepted as good
    [StarData] public float MinAcceptableShipWarpRange = 600000;


    // feature flags
    [StarData] public bool UseHullBonuses;
    [StarData] public bool RemoveRemnantStory;
    [StarData] public bool UseCombatRepair;
    [StarData] public bool EnableECM;
    [StarData] public bool UseDestroyers;
    [StarData] public bool UsePlanetaryProjection;
    [StarData] public bool ReconDropDown;
    [StarData] public bool DisplayEnvPerfInRaceDesign;
    // Research costs will be increased based on map size to balance the increased capacity of larger maps
    [StarData] public bool ChangeResearchCostBasedOnSize;
    // Use short term researchable techs with no best ship
    [StarData] public bool EnableShipTechLineFocusing;
    // Disable the ship picker and use all techs that can be researched based on ship designs
    [StarData] public bool DisableShipPicker;
    // for mods that don't require remnant storyline
    [StarData] public bool DisableRemnantStory;
    // for mods that don't require pirates
    [StarData] public bool DisablePirates;
    [StarData] public bool AIUsesPlayerDesigns = true; // Can AI use player designs? This will make the AI stronger.


    // visual modifiers
    [StarData] public float SpaceportScale = 0.5f;
    [StarData] public float ExplosionVisualIncreaser = 1f;
    [StarData] public float ShipExplosionVisualIncreaser = 1f;
    [StarData] public float ModuleDamageVisualIntensity = 1f;


    // misc settings
    [StarData] public string CustomMenuMusic;
    // In case an event building has defense drones and drones are not researched
    [StarData] public string DefaultEventDrone;
    [StarData] public string ResearchRootUIDToDisplay;


    // Urls for accessing auto-updater, should be changed for mods, if unused, set to ""
    [StarData] public string URL;
    [StarData] public string DownloadSite;
    [StarData] public string BitbucketAPIString;


    // Mod information, should be null for vanilla
    [StarData] public ModInformation Mod;

    [StarDataConstructor]
    public GamePlayGlobals()
    {
        // A little bit of magic, if GlobalStats.DefaultSettings is not null,
        // then pre-initialize all fields from that
        if (GlobalStats.DefaultSettings != null)
        {
            foreach (var field in typeof(GamePlayGlobals).GetFields())
                field.SetValue(this, field.GetValue(GlobalStats.DefaultSettings));
        }
    }
}
