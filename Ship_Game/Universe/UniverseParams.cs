using SDGraphics;
using Ship_Game.Data.Serialization;
using static Ship_Game.RaceDesignScreen;

namespace Ship_Game.Universe;

[StarDataType]
public class UniverseParams
{
    // this is only used during first time universe generation and shouldn't be serialized
    public EmpireData PlayerData;

    // Universe Generator parameters:
    [StarData(DefaultValue=GameDifficulty.Normal)]
    public GameDifficulty Difficulty = GameDifficulty.Normal;
    
    [StarData(DefaultValue=StarsAbundance.Normal)]
    public StarsAbundance StarsCount = StarsAbundance.Normal;
    
    [StarData(DefaultValue=GalSize.Medium)]
    public GalSize GalaxySize = GalSize.Medium;
    
    [StarData(DefaultValue=ExtraRemnantPresence.Normal)]
    public ExtraRemnantPresence ExtraRemnant = ExtraRemnantPresence.Normal;
    
    [StarData] public int NumSystems;
    [StarData] public int NumOpponents;
    [StarData] public int RacialTraitPoints;
    [StarData] public GameMode Mode = GameMode.Sandbox;
    [StarData(DefaultValue=1f)] public float Pace = 1f;
    [StarData(DefaultValue=1f)] public float StarsModifier = 1f;

    // Universe customization parameters:
    [StarData] public float MinAcceptableShipWarpRange;
    [StarData] public int TurnTimer; // seconds between Empire turns, every turn advances stardate by 0.1
    [StarData] public bool PreventFederations;
    [StarData] public bool EliminationMode;
    [StarData] public float CustomMineralDecay;
    [StarData] public float VolcanicActivity;
    [StarData] public float ShipMaintenanceMultiplier;
    [StarData] public bool AIUsesPlayerDesigns;
    [StarData] public bool UseUpkeepByHullSize;
    [StarData] public float StartingPlanetRichnessBonus;

    // in-system FTL modifier is the BASE FTL modifier when ships are inside solar systems
    const float DefaultInSystemFTLModifier = 1f;

    [StarData(DefaultValue=DefaultInSystemFTLModifier)]
    public float FTLModifier = DefaultInSystemFTLModifier;
    
    // if within enemy projector range, then this BASE FTL modifier is used
    const float DefaultEnemyFTLModifier = 0.5f;

    [StarData(DefaultValue=DefaultEnemyFTLModifier)]
    public float EnemyFTLModifier = DefaultEnemyFTLModifier;


    // configured gravity wells for this game, if 0, then gravity wells are disabled
    [StarData] public float GravityWellRange;
    [StarData] public int ExtraPlanets;

    // persistent toggle flags for different checkboxes
    [StarData(DefaultValue=true)] public bool PlanetsScreenHideInhospitable = true;
    [StarData(DefaultValue=true)] public bool DisableInhibitionWarning = true;
    [StarData(DefaultValue=false)] public bool EnableStarvationWarning = false;
    [StarData(DefaultValue=true)] public bool AllowPlayerInterTrade  = true;
    [StarData] public bool SuppressOnBuildNotifications;
    [StarData] public bool PlanetScreenHideOwned;
    [StarData] public bool ShipListFilterPlayerShipsOnly;
    [StarData] public bool ShipListFilterInFleetsOnly;
    [StarData] public bool ShipListFilterNotInFleets;
    [StarData] public bool CordrazinePlanetCaptured;
    [StarData] public bool DisableVolcanoWarning;
    [StarData(DefaultValue=true)] public bool ShowAllDesigns = true;
    [StarData] public bool FilterOldModules;

    [StarData] public bool DisableRemnantStory;
    [StarData] public bool DisableAlternateAITraits;
    [StarData] public bool DisablePirates;
    [StarData] public bool FixedPlayerCreditCharge;
    [StarData] public bool DisableResearchStations;
    [StarData] public bool DisableMiningOps;

    public bool DebugDisableShipLaunch; // Only for testing

    public UniverseParams()
    {
        // initialize defaults from Settings
        var s = GlobalStats.Defaults;

        NumOpponents = s.DefaultNumOpponents.UpperBound(ResourceManager.MajorRaces.Count - 1);
        RacialTraitPoints = s.TraitPoints;
        MinAcceptableShipWarpRange = s.MinAcceptableShipWarpRange;
        TurnTimer = s.TurnTimer;
        CustomMineralDecay = s.CustomMineralDecay;
        VolcanicActivity = s.VolcanicActivity;
        ShipMaintenanceMultiplier = s.ShipMaintenanceMultiplier;
        AIUsesPlayerDesigns = s.AIUsesPlayerDesigns;
        UseUpkeepByHullSize = s.UseUpkeepByHullSize;
        StartingPlanetRichnessBonus = s.StartingPlanetRichnessBonus;
        GravityWellRange = s.GravityWellRange;
        DisableRemnantStory = s.DisableRemnantStory;
    }

    [StarDataDeserialized]
    public void OnDeserialized()
    {
        // BUGFIX: if FTL modifiers become 0, then reset the defaults,
        //         because if they are 0, the game would break
        if (FTLModifier == 0f) FTLModifier = DefaultInSystemFTLModifier;
        if (EnemyFTLModifier == 0f) EnemyFTLModifier = DefaultEnemyFTLModifier;
    }
}
