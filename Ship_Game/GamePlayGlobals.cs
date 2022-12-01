using Ship_Game.Data.Binary;
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
    [StarData] public int DefaultNumOpponents = 7; // Default AIs to start on default settings

    [StarData] public float HangarCombatShipCostMultiplier = 1;
    [StarData] public bool DisplayEnvPerfInRaceDesign;
    
    // Research costs will be increased based on map size to balance the increased capacity of larger maps
    [StarData] public bool ChangeResearchCostBasedOnSize;
    [StarData] public int CostBasedOnSizeThreshold = 2500;  // Allow tuning the change up/down
    
    // How tougher are remnant designs in the mod. This affects starting fleet multipliers and also increases with difficulty. Vanilla is 2
    [StarData] public float RemnantDesignStrMultiplier; 

    // added by Fat Bastard
    // Use short term researchable techs with no best ship
    [StarData] public bool EnableShipTechLineFocusing;

    // Disable the ship picker and use all techs that can be researched based on ship designs
    [StarData] public bool DisableShipPicker;

    // In case an event building has defense drones and drones are not researched
    [StarData] public string DefaultEventDrone;

    [StarData] public float ExplosionVisualIncreaser     = 1f;
    [StarData] public float ShipExplosionVisualIncreaser = 1f;
    [StarData] public string CustomMenuMusic;

    [StarData] public bool UseHullBonuses;
    [StarData] public bool RemoveRemnantStory;
    [StarData] public bool UseCombatRepair;
    
    [StarData] public float ShipyardBonus;
    [StarData] public float SpaceportScale = 0.5f;
    
    [StarData] public bool EnableECM;
    [StarData] public bool UseDestroyers;
    [StarData] public bool UsePlanetaryProjection;
    [StarData] public bool ReconDropDown;
    
    [StarData] public string URL;
    [StarData] public string DownloadSite;
    [StarData] public string BitbucketAPIString;

    [StarData] public ModInformation Mod;

    [StarDataConstructor]
    public GamePlayGlobals()
    {
        // A little bit of magic, if GlobalStats.DefaultSettings is not null,
        // then pre-initialize all fields from that
        if (GlobalStats.DefaultSettings != null)
        {
            foreach (var field in typeof(GamePlayGlobals).GetFields())
            {
                object defaultValue = field.GetValue(GlobalStats.DefaultSettings);
                field.SetValue(this, defaultValue);
            }
        }
    }
}
