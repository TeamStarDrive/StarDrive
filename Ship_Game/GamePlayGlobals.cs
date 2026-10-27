using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;
using System.IO;

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
    // core game settings
    [StarData] public int MaxOpponents = 7;
    [StarData] public int DefaultNumOpponents = 5; // Default AIs to start on default settings
    [StarData] public int TurnTimer = 5; // default time in seconds for a single turn
    [StarData] public int TraitPoints = 8; // How many points each race has to spend on racial traits


    // GamePlay modifiers
    // How easy ships are to destroy. Ships which have active internal slots below this ratio, will Die()
    [StarData] public float ShipDestroyThreshold = 0.5f;
    // Percent of the deaths of military ships (frigate and above) and untethered stations that leave a derelict, with no death blast, before the caps below lower it; with salvage ops on a roll by its hull decides whether it drifts, and with them off every one drifts
    [StarData] public float DerelictChance = 20;
    // Most drifting derelicts at once, seen or not, as a multiple of the galaxy's star systems from 0.1 to 10, a value outside it used as the nearer end with a warning in the log; the cap is rounded up, at least 1. Past it a seen derelict death takes the place of the oldest drifting one not seen, or shows the normal explosion, still with no death blast. With salvage ops off the derelict chance falls with their share of it, to none at the limit
    [StarData] public float DriftingDerelictCapMultiplier = 1f;
    // Most salvageable derelicts at once, as a multiple of the galaxy's star systems from 0.1 to 10, a value outside it used as the nearer end with a warning in the log; the cap is rounded up, at least 1. With salvage ops on the derelict chance falls with their share of it, to none at the limit
    [StarData] public float SalvageableDerelictCapMultiplier = 1f;
    // Production a salvage platform strips from its derelict in a turn, while its hold has room, before the Salvage Speed research or pirates' level raise it; above 0, as a recovery works at twice it
    [StarData] public float SalvageSpeed = 12;
    // Share of what a tech has left to research that a derelict's whole production advances it by, each load paying its part
    [StarData] public float SalvageTechShare = 0.2f;
    // A recovery's work: this times the share of health the ship had lost at its death times its slots, at the production pace, times 1 minus the race's engineers; above 0. A platform works it at twice its empire's salvage speed
    [StarData] public float SalvageRecoveryWorkFactor = 1.1f;
    // Turns a salvage platform breaks into its derelict before a recovery's work starts, whatever the ship's size and the production pace; 0 or more
    [StarData] public int SalvageRecoveryBreakInTurns = 10;
    // How tougher are remnant designs in the mod. This affects starting fleet multipliers and also increases with difficulty. Vanilla is 2
    [StarData] public float RemnantDesignStrMultiplier;
    public float RemnantDesignStr => RemnantDesignStrMultiplier > 0.01f ? RemnantDesignStrMultiplier : 2f;
    [StarData] public int CostBasedOnSizeThreshold = 2500;  // Allow tuning the change up/down
    [StarData] public float HangarCombatShipCostMultiplier = 1;
    [StarData] public float ResearchStationProductionPerResearch = 2f; // Production consumed per 1 Research point
    [StarData] public float MiningStationFoodPerOneRefining = 1; // Food consumed per 1 refining point

    // required empire pop ratio before expansion is considered
    [StarData] public float RequiredExpansionPopRatio = 0.2f;
    [StarData] public float ShipyardBonus;
    [StarData] public float CustomMineralDecay = 1;
    [StarData] public float VolcanicActivity = 1;
    // sets the default gravity well range, 0 means disabled
    [StarData] public float GravityWellRange = 8000;
    // base richness for empire capitals
    [StarData] public float StartingPlanetRichnessBonus = 1;
    [StarData] public float ShipMaintenanceMultiplier = 1;
    // How much rushing costs in percentage of production cost
    [StarData] public float RushCostPercentage = 1;
    // minimum ship warp range which is accepted as good
    [StarData] public float MinAcceptableShipWarpRange = 600000;

    // base amount of ship repair per SECOND from planetary buildings
    [StarData] public float BaseShipyardRepair = 100;
    // repair rate multiplier when a ship or planet is in combat
    [StarData] public float InCombatRepairModifier = 0.5f;
    // +bonus based on colony level, 0.1 would be +10% increase per level
    [StarData] public float BonusRepairPerColonyLevel = 0.1f;
    // multiplier of EMP damage removed from shipyard repair when orbiting a colony
    [StarData] public float BonusColonyEMPRecovery = 5.0f;
    // base repair rate from ship command/engineering modules per SECOND
    [StarData] public float SelfRepairMultiplier = 1f;  
    // +bonus rate based on crew level, 0.2 would be +20% increase per each crew level
    [StarData] public float BonusRepairPerCrewLevel = 0.2f;
    // repair rate modifier for command/engineering self repair when in combat
    [StarData] public float InCombatSelfRepairModifier = 0.2f;
    // Shield power multiply
    [StarData] public float ShieldPowerMultiplier = 1f;
    // Projectile Hit Points Multiplier
    [StarData] public float ProjectileHitpointsMultiplier = 1f;
    // Construction Ship Discount for Orbitals build cost
    [StarData] public float ConstructionShipOrbitalDiscount = 100f;
    // How much construction the construction module can process per turn 
    [StarData] public int ConstructionModuleBuildRate = 100;
    // How much construction Builder ships add when they reach the contructor
    [StarData] public int BuilderShipConstructionAdded = 100;
    // How much extra research the empire gets from allies
    [StarData] public float ResearchBenefitFromAlliance = 0.1f;
    // How much exotic resource storage an empire has per resource based on all planet normal storage
    [StarData] public float ExoticRatioStorage = 0.1f;

    // feature flags
    [StarData] public bool UseCombatRepair;
    [StarData] public bool EnableECM;
    [StarData] public bool UseDestroyers;
    [StarData] public bool ReconDropDown;
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
    // salvageable derelicts and salvage stations; off, the Disable Salvage Ops rule option is locked on
    [StarData] public bool EnableSalvageOps;
    [StarData] public bool AIUsesPlayerDesigns = true; // Can AI use player designs? This will make the AI stronger.
    // changes how upkeep is calculated, default:false means upkeep depends on ship cost
    // setting this to true means upkeep depends on number of hull design slots
    [StarData] public bool UseUpkeepByHullSize;
    [StarData] public bool EnableRandomizedAIFleetSizes;

    // visual modifiers
    [StarData] public float SpaceportScale = 0.5f;
    [StarData] public float ExplosionVisualIncreaser = 1f;
    [StarData] public float ShipExplosionVisualIncreaser = 1f;
    [StarData] public float ModuleDamageVisualIntensity = 1f;


    // misc settings
    [StarData] public string CustomMenuMusic;
    [StarData] public bool EnableHullEditor;
    // In case an event building has defense drones and drones are not researched
    [StarData] public string DefaultEventDrone;
    [StarData] public string ResearchRootUIDToDisplay;


    // Urls for accessing auto-updater, should be changed for mods, if unused, set to ""
    [StarData] public string URL;
    [StarData] public string DownloadSite;


    // Mod information, should be null for vanilla
    [StarData] public ModInformation Mod;

    [StarDataConstructor]
    public GamePlayGlobals()
    {
        // A little bit of magic, if GlobalStats.DefaultSettings is not null,
        // then pre-initialize all fields from that
        if (GlobalStats.VanillaDefaults != null)
        {
            foreach (var field in typeof(GamePlayGlobals).GetFields())
                field.SetValue(this, field.GetValue(GlobalStats.VanillaDefaults));
            EnableSalvageOps = false; // a mod turns it on itself, once it has salvage designs
        }
    }

    // Deserializes Globals.yaml
    public static GamePlayGlobals Deserialize(FileInfo globalsFile)
    {
        var settings = YamlParser.DeserializeOne<GamePlayGlobals>(globalsFile);
        if (settings.Mod != null)
        {
            string currentDir = Directory.GetCurrentDirectory();
            // "Mods/ExampleMod/"
            settings.Mod.Path = globalsFile.Directory!.FullName;
            settings.Mod.Path = settings.Mod.Path.Replace(currentDir, "").Trim('\\');
            settings.Mod.Path = settings.Mod.Path.Replace('\\', '/') + '/';
        }
        return settings;
    }
}

