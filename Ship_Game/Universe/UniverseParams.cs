using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using static Ship_Game.RaceDesignScreen;
using Color = Microsoft.Xna.Framework.Color;

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

    public Array<IEmpireData> SelectedOpponents = new();

    // the color and flag picked for a chosen opponent on the Game Setup screen
    public readonly Map<IEmpireData, (Color Color, int Flag)> OpponentLooks = new();

    [StarData] public int NumSystems;
    [StarData] public int NumOpponents;
    [StarData] public int RacialTraitPoints;
    [StarData] public GameMode Mode = GameMode.Sandbox;
    [StarData(DefaultValue=1f)] public float Pace = 1f;
    [StarData(DefaultValue=1f)] public float StarsModifier = 1f;

    // Universe customization parameters:
    [StarData] public int TurnTimer; // seconds between Empire turns, every turn advances stardate by 0.1
    [StarData] public bool PreventFederations;
    [StarData] public bool EliminationMode;
    [StarData] public float CustomMineralDecay;
    [StarData] public float VolcanicActivity;
    [StarData] public float ShipMaintenanceMultiplier;
    [StarData] public bool AIUsesPlayerDesigns;
    [StarData] public bool UseUpkeepByHullSize;
    [StarData] public bool UseLegacyEspionage;
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
    [StarData] public bool DisableCrashSiteWarning;
    [StarData] public bool DisableStationBuiltWarning;
    [StarData] public bool DisableDerelictWarning;
    [StarData] public bool DisableSalvageWarning;
    [StarData] public bool DisableBoardingWarning;
    [StarData] public bool DisableTechLeechWarning;
    [StarData(DefaultValue=true)] public bool PlayerHullColors = true;
    [StarData(DefaultValue=true)] public bool OpponentHullColors = true;
    [StarData] bool SavedWithHullColors; // set as it is saved, so a game saved before the hull color boxes loads with both off
    [StarData] public bool PrioitizeProjectors;
    [StarData(DefaultValue=true)] public bool ShowAllDesigns = true;
    [StarData] public bool FilterOldModules;

    [StarData(NoDefaults = true)] public bool DisableRemnantStory; // always written, as a load starts from the mod's Globals
    [StarData] public bool EnableRandomizedAIFleetSizes;
    [StarData] public bool DisableAlternateAITraits;
    [StarData(NoDefaults = true)] public bool DisablePirates; // always written, as a load starts from the mod's Globals
    [StarData] public bool FixedPlayerCreditCharge;
    [StarData] public bool DisableResearchStations;
    [StarData] public bool DisableMiningOps;
    [StarData] public bool DisableSalvageOps;
    [StarData] bool SavedWithSalvageOps; // set as it is saved, so a game saved before salvage ops existed loads with them off

    // the Globals flag, vanilla's or a mod's, is a hard switch: with it off they stay off whatever the rule option says
    public bool SalvageOps { get => !DisableSalvageOps && GlobalStats.Defaults.EnableSalvageOps; set => DisableSalvageOps = !value; }

    // the Game Extensions boxes, ticked while the feature is on
    public bool Pirates { get => !DisablePirates; set => DisablePirates = !value; }
    public bool RemnantStory { get => !DisableRemnantStory; set => DisableRemnantStory = !value; }
    public bool Infiltration { get => !UseLegacyEspionage; set => UseLegacyEspionage = !value; }
    public bool ResearchStations { get => !DisableResearchStations; set => DisableResearchStations = !value; }
    public bool MiningOps { get => !DisableMiningOps; set => DisableMiningOps = !value; }

    [StarDataSerialize]
    StarDataDynamicField[] OnSerialize()
    {
        SavedWithSalvageOps = true;
        SavedWithHullColors = true;
        return null;
    }

    public bool DebugDisableShipLaunch; // Only for testing

    public UniverseParams()
    {
        // initialize defaults from Settings
        var s = GlobalStats.Defaults;

        NumOpponents = s.DefaultNumOpponents.UpperBound(ResourceManager.MajorRaces.Count - 1);
        RacialTraitPoints = s.TraitPoints;
        TurnTimer = s.TurnTimer;
        CustomMineralDecay = s.CustomMineralDecay;
        VolcanicActivity = s.VolcanicActivity;
        ShipMaintenanceMultiplier = s.ShipMaintenanceMultiplier;
        AIUsesPlayerDesigns = s.AIUsesPlayerDesigns;
        UseUpkeepByHullSize = s.UseUpkeepByHullSize;
        StartingPlanetRichnessBonus = s.StartingPlanetRichnessBonus;
        GravityWellRange = s.GravityWellRange;
        DisableRemnantStory = s.DisableRemnantStory;
        DisablePirates = s.DisablePirates;
        DisableSalvageOps = !s.EnableSalvageOps;
        EnableRandomizedAIFleetSizes = s.EnableRandomizedAIFleetSizes;
    }

    [StarDataDeserialized]
    public void OnDeserialized()
    {
        // BUGFIX: if FTL modifiers become 0, then reset the defaults,
        //         because if they are 0, the game would break
        if (FTLModifier == 0f) FTLModifier = DefaultInSystemFTLModifier;
        if (EnemyFTLModifier == 0f) EnemyFTLModifier = DefaultEnemyFTLModifier;

        // a game saved before salvage ops existed keeps them off
        if (!SavedWithSalvageOps)
            DisableSalvageOps = true;

        if (!SavedWithHullColors)
            PlayerHullColors = OpponentHullColors = false;
    }

    // drops a chosen opponent and the look picked for it, so choosing it again shows its race's own
    public bool DropOpponent(IEmpireData race)
    {
        OpponentLooks.Remove(race);
        return SelectedOpponents.Remove(race);
    }

    // the race to create a chosen opponent from: its own, or a copy wearing the color and flag picked for it
    public IEmpireData OpponentData(IEmpireData race)
    {
        if (!OpponentLooks.TryGetValue(race, out (Color Color, int Flag) look))
            return race;
        EmpireData data = race.CreateInstance();
        data.Traits.Color = look.Color;
        data.Traits.FlagIndex = look.Flag;
        return data;
    }
}
