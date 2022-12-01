using Ship_Game.Data.Serialization;

namespace Ship_Game.Universe;

// TODO: Use these directly, instead of updating GlobalStats which should remain readonly !
[StarDataType]
public class UniverseParams
{
    
    [StarData(DefaultValue=ExtraRemnantPresence.Normal)]
    public ExtraRemnantPresence ExtraRemnant = ExtraRemnantPresence.Normal;

    [StarData] public float MinAcceptableShipWarpRange;
    [StarData] public int TurnTimer;
    [StarData] public bool PreventFederations;
    [StarData] public bool EliminationMode;
    [StarData] public float CustomMineralDecay;
    [StarData] public float VolcanicActivity;
    [StarData] public float ShipMaintenanceMultiplier;
    [StarData] public bool AIUsesPlayerDesigns;
    [StarData] public bool UseUpkeepByHullSize;
    [StarData] public float StartingPlanetRichness;

    [StarData(DefaultValue=1f)] public float FTLModifier = 1f; // in-system FTL modifier
    [StarData(DefaultValue=1f)] public float EnemyFTLModifier = 1f; // in-system FTL modifier for enemies

    // configured gravity wells for this game, if 0, then gravity wells are disabled
    [StarData] public float GravityWellRange;
    [StarData] public int ExtraPlanets;

    // persistent toggle flags for different checkboxes
    [StarData(DefaultValue=true)] public bool FTLInNeutralSystems = true;
    [StarData(DefaultValue=true)] public bool PlanetsScreenHideInhospitable = true;
    [StarData(DefaultValue=true)] public bool DisableInhibitionWarning = true;
    [StarData] public bool SuppressOnBuildNotifications;
    [StarData] public bool PlanetScreenHideOwned;
    [StarData] public bool ShipListFilterPlayerShipsOnly;
    [StarData] public bool ShipListFilterInFleetsOnly;
    [StarData] public bool ShipListFilterNotInFleets;
    [StarData] public bool CordrazinePlanetCaptured;
    [StarData] public bool DisableVolcanoWarning;
    [StarData] public bool ShowAllDesigns;
    [StarData] public bool FilterOldModules;

    [StarData] public bool DisableRemnantStory;
    [StarData] public bool DisablePirates;
    [StarData] public bool FixedPlayerCreditCharge;


    public UniverseParams()
    {
        // initialize defaults from Settings
        var s = GlobalStats.Settings;
        MinAcceptableShipWarpRange = s.MinAcceptableShipWarpRange;
        TurnTimer = s.TurnTimer;
        GravityWellRange = s.GravityWellRange;
        CustomMineralDecay = s.CustomMineralDecay;
        VolcanicActivity = s.VolcanicActivity;
        ShipMaintenanceMultiplier = s.ShipMaintenanceMultiplier;
        AIUsesPlayerDesigns = s.AIUsesPlayerDesigns;
        UseUpkeepByHullSize = s.UseUpkeepByHullSize;
        StartingPlanetRichness = s.StartingPlanetRichness;
        GravityWellRange = s.GravityWellRange;
        DisableRemnantStory = s.DisableRemnantStory;
    }

    public void UpdateGlobalStats()
    {
        var s = GlobalStats.Settings;
        s.GravityWellRange = GravityWellRange;
        s.CustomMineralDecay = CustomMineralDecay;
        s.MinAcceptableShipWarpRange = MinAcceptableShipWarpRange;
        s.ShipMaintenanceMultiplier = ShipMaintenanceMultiplier;
        s.AIUsesPlayerDesigns = AIUsesPlayerDesigns;
        s.UseUpkeepByHullSize = UseUpkeepByHullSize;
    }
}