using System;
using System.Linq;
using System.Xml.Serialization;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    [StarDataType]
    public sealed class WeaponTagModifier
    {
        [StarData] public float Speed;           // % bonus
        [StarData] public float Range;           // % bonus
        [StarData] public float Rate;            // % bonus
        [StarData] public float Turn;            // % bonus
        [StarData] public float Damage;          // % bonus
        [StarData] public float ExplosionRadius; // % bonus
        [StarData] public float ShieldDamage;    // % bonus
        [StarData] public float ArmorDamage;        // FLAT bonus
        [StarData] public float ShieldPenetration;  // FLAT bonus
        [StarData] public float HitPoints;          // % bonus
        [StarData] public float ArmourPenetration; // FLAT bonus
    }

    // @todo Find a better place for this enum
    public enum WeaponStat
    {
        Damage, Range, Speed, FireDelay, Armor, Shield
    }

    [StarDataType]
    public sealed class DTrait
    {
        [StarData] public string Name;
        [StarData] public string Description;
        [StarData] public int Territorialism;
        [StarData] public float Opportunism;
        [StarData] public int NAPact;
        [StarData] public int Alliance;
        [StarData] public int Trade;
        [StarData] public int Trustworthiness;
        [StarData] public float NaturalRelChange;
        [StarData] public float AngerDissipation;
        [StarData] public float WeaknessDecline;
        [StarData] public float PowerIncrease;
        [StarData] public float TrustGainedAtPeace;

        [XmlIgnore]
        public bool IsTrusting => Trustworthiness >= 80;
        [XmlIgnore]
        public bool Careless   => Trustworthiness <= 60;
        [XmlIgnore]
        
        public PersonalityType TraitName
        {
            get
            {
                Enum.TryParse(Name, out PersonalityType traitType);
                return traitType;
            }
        }

        public DTrait() // For Player Empire Only
        {
            Name = "None";
        }
    }

    public enum PersonalityType
    {
        None, // For player, avoid null checks
        Cunning,
        Ruthless,
        Aggressive,
        Honorable,
        Xenophobic,
        Pacifist
    }

    /// <summary>
    /// This class looks pretty useless. I think we need another class for "mood" or find the mood of the empire buried in the code.
    /// mostly only the name or type is used. the logic is a little confusing. 
    /// This has an effect on diplomacy but all that code is in the diplomacy code. i feel it should be pulled out and placed here.
    /// in the diplomacy code there is a lot of this "TrustCost = (Them.EconomicPersonality.Name == "Technologists"
    /// this makes new econmic personality files basically... Useless. 
    /// except that it contains the econ strat which has useful values.
    /// 
    /// </summary>
    [StarDataType]
    public sealed class ETrait
    {
        [StarData] public string Name;
        [StarData] public string Description;
        [StarData] public string EconomicResearchStrategy;
        [StarData] public int ColonyGoalsPlus;
        [StarData] public int ShipGoalsPlus;
    }

    // Read-Only interface of EmpireData
    public interface IEmpireData
    {
        string Name { get; }
        string ArchetypeName { get; }
        bool IsCybernetic { get; }
        bool IsFactionOrMinorRace { get; }
        RacialTrait Traits { get; }
        EmpireData CreateInstance(bool copyTraits = true);

        // RaceDesignScreen:
        string ShipType { get; }
        string VideoPath { get; }
        string Singular { get; }
        string Plural { get; }
        string HomeSystemName { get; }
        string HomeWorldName { get; }
        string Adj1 { get; }
        string Adj2 { get; }

        string WarpStart { get; }
        string WarpEnd { get; }
    }

    [StarDataType]
    public sealed class EmpireData : IEmpireData
    {
        [StarData] public Map<WeaponTag, WeaponTagModifier> WeaponTags = new();
        [StarData] public string WarpStart { get; set; }
        [StarData] public string WarpEnd { get; set; }
        [StarData] public string CurrentAutoFreighter = "";
        [StarData] public string CurrentAutoColony    = "";
        [StarData] public string CurrentAutoScout     = "";
        [StarData] public string CurrentConstructor   = "";
        [StarData] public string CurrentMiningStation = "";
        [StarData] public string CurrentResearchStation = "";
        [StarData] public string DiplomacyDialogPath;
        [StarData] public DTrait DiplomaticPersonality;
        [StarData] public ETrait EconomicPersonality;

        float TaxRateValue = 0.25f;

        // player modified tax rate
        [StarData]
        public float TaxRate
        {
            get => TaxRateValue;
            set => TaxRateValue = value.NaNChecked(0.25f, "EmpireData.TaxRate");
        }

        [StarData] public Array<string> PersonalityTraitsWeights = new();
        [StarData] public Array<string> EconomicTraitsWeights = new();
        [StarData] public Array<Agent> AgentList = new();
        [StarData] public string AbsorbedBy;
        [StarData] public string StartingShip;
        [StarData] public string StartingScout;
        [StarData] public string PrototypeShip;
        [StarData] public string DefaultColonyShip;
        [StarData] public string DefaultSmallTransport;
        [StarData] public string DefaultTroopShip;
        [StarData] public string DefaultConstructor;
        [StarData] public string DefaultShipyard = "Shipyard";
        [StarData] public bool RebellionLaunched;
        [StarData] public string MusicCue;
        [StarData] public Array<string> ResearchQueue = new();
        [StarData] public Array<Mole> MoleList = new();
        [StarData] public float CounterIntelligenceBudget;

        // NOTE: This is currently the main unique identifier?
        [StarData] public string PortraitName;
        [StarData] public string RebelSing;
        [StarData] public string RebelPlur;
        [StarData] public int TroopNameIndex;
        [StarData] public int TroopDescriptionIndex;
        [XmlIgnore] public LocalizedText TroopName => new(TroopNameIndex);
        [XmlIgnore] public LocalizedText TroopDescription => new(TroopDescriptionIndex);
        [StarData] public string RebelName;
        [StarData] public bool IsRebelFaction;
        [StarData] public RacialTrait Traits { get; set; }
        [StarData] public byte Faction;
        [StarData] public bool MinorRace; // @todo This is deprecated
        [StarData] public short TurnsBelowZero;
        [StarData] public bool Privatization;
        [StarData] public float CivMaintMod = 1f; // x100%
        [StarData] public float FuelCellModifier; // +100%
        [StarData] public float FlatMoneyBonus;
        [StarData] public float FTLModifier        = 35f;
        [StarData] public float MassModifier       = 1f;
        [StarData] public float ArmourMassModifier = 1f;
        [StarData] public float SubLightModifier   = 1f;
        [StarData] public float EmpireFertilityBonus;
        [StarData] public float SensorModifier     = 1f;
        [StarData] public float OrdnanceEffectivenessBonus;
        [StarData] public int ArmorPiercingBonus;
        [StarData] public float SpoolTimeModifier        = 1f;
        [StarData] public float ExplosiveRadiusReduction = 0f;
        [StarData] public float ShieldPenBonusChance;
        [StarData] public float SpyModifier;
        [StarData] public float DefensiveSpyBonus;
        [StarData] public float OffensiveSpyBonus;
        [StarData] public float FTLPowerDrainModifier = 2f;
        [StarData] public Array<Artifact> OwnedArtifacts = new();
        [StarData] public int BonusFighterLevels;
        [StarData] public float MissileDodgeChance;
        [StarData] public float MissileHPModifier = 1f;
        [StarData] public bool Inhibitors;
        [StarData] public float BaseReproductiveRate = 0.01f;
        [StarData] public float ExoticStorageMultiplier = 1;
        [StarData] public float MiningSpeedMultiplier = 1;
        [StarData] public float RefiningRatioMultiplier = 1;

        // Added by McShooterz: power bonus
        [StarData] public float PowerFlowMod   = 0f;
        [StarData] public float ShieldPowerMod = 0f;
        [StarData] public float ExperienceMod  = 0f;

        // unlock at start
        [StarData] public Array<string> unlockBuilding = new();
        [StarData] public Array<string> unlockShips    = new();

        // designsWeHave our techTree has techs for.
        // sortsaves
        public SortButton PLSort = new();
        public SortButton ESSort = new();
        public SortButton SLSort = new();

        // techTimers
        [StarData] public short TechDelayTime    = 0;
        [StarData] public bool  SpyMute          = false;
        [StarData] public bool  SpyMissionRepeat = false;
        [StarData] public float treasuryGoal     = 0.2f;
        [StarData] public float BorderTolerance  = 40f;
        [StarData] public int   BaseShipLevel    = 0;

        //FB: default assault and supply shuttles - it is not mandatory since we have a default boarding / supply shuttles in the game
        [StarData] public string DefaultAssaultShuttle;
        [StarData] public string DefaultSupplyShuttle;
        [StarData] public string DefaultMiningShip;

        [StarData] public string DefaultResearchStation;
        [StarData] public string DefaultMiningStation;

        // FB - Thruster Colors
        [StarData] public byte ThrustColor0R;
        [StarData] public byte ThrustColor0G;
        [StarData] public byte ThrustColor0B;
        [StarData] public byte ThrustColor1R;
        [StarData] public byte ThrustColor1G;
        [StarData] public byte ThrustColor1B;



        // FB - Minimum Troop Level
        [StarData] public int MinimumTroopLevel;

        // FB - For Pirates
        [StarData] public string PirateSlaverBasic;
        [StarData] public string PirateSlaverImproved;
        [StarData] public string PirateSlaverAdvanced;
        [StarData] public string PirateFighterBasic;
        [StarData] public string PirateFighterImproved;
        [StarData] public string PirateFighterAdvanced;
        [StarData] public string PirateFrigateBasic;
        [StarData] public string PirateFrigateImproved;
        [StarData] public string PirateFrigateAdvanced;
        [StarData] public string PirateBaseBasic;
        [StarData] public string PirateBaseImproved;
        [StarData] public string PirateBaseAdvanced;
        [StarData] public string PirateStationBasic;
        [StarData] public string PirateStationImproved;
        [StarData] public string PirateStationAdvanced;
        [StarData] public string PirateFlagShip;
        [StarData] public bool IsPirateFaction;
        [StarData] public int PiratePaymentPeriodTurns = 100; 
        [StarData] public int MinimumColoniesForStartPayment = 3;
        [StarData] public float MilitaryScoreAverage;

        // FB - For Remnants
        [StarData] public bool IsRemnantFaction;
        [StarData] public string RemnantFighter;
        [StarData] public string RemnantCorvette;
        [StarData] public string RemnantBeamCorvette;
        [StarData] public string RemnantBattleCorvette;
        [StarData] public string RemnantSupportSmall;
        [StarData] public string RemnantCarrier;
        [StarData] public string RemnantAssimilator;
        [StarData] public string RemnantTorpedoCruiser;
        [StarData] public string RemnantMotherShip;
        [StarData] public string RemnantExterminator;
        [StarData] public string RemnantPortal;
        [StarData] public string RemnantBomber;
        [StarData] public string RemnantInhibitor;
        [StarData] public string RemnantBattleship;
        [StarData] public string RemnantFrigate;
        [StarData] public string RemnantBomberLight;
        [StarData] public string RemnantBomberMedium;
        [StarData] public string RemnantCruiser;
        [StarData] public string RemnantBehemoth;

        [StarData] public string SpacePortModel;
        [StarData] public float BombEnvironmentDamageMultiplier = 1;
        [StarData] public float OngoingDiplomaticModifier;
        [StarData] public int[] RoleLevels = new int[Enum.GetNames(typeof(RoleName)).Length];

        [XmlIgnore] public string Name => Traits.Name;
        [XmlIgnore] public string ArchetypeName => PortraitName;

        [XmlIgnore] public SubTexture PortraitTex
            => ResourceManager.Texture("Portraits/" + PortraitName);

        public override string ToString() => $"Name: '{Name}' ShipType: {ShipType}";

        [XmlIgnore]
        public string ScoutShip => CurrentAutoScout.NotEmpty() ? CurrentAutoScout
                                 : StartingScout.NotEmpty()    ? StartingScout
                                 : "Unarmed Scout";

        [XmlIgnore]
        public string FreighterShip => CurrentAutoFreighter.NotEmpty()  ? CurrentAutoFreighter
                                     : DefaultSmallTransport.NotEmpty() ? DefaultSmallTransport
                                     : "Small Transport";
        
        [XmlIgnore]
        public string ColonyShip => CurrentAutoColony.NotEmpty() ? CurrentAutoColony
                                  : DefaultColonyShip.NotEmpty() ? DefaultColonyShip
                                  : "Colony Ship";
                
        [XmlIgnore]
        public string ConstructorShip => CurrentConstructor.NotEmpty() ? CurrentConstructor
                                       : DefaultConstructor.NotEmpty() ? DefaultConstructor
                                       : "Terran Constructor";

        [XmlIgnore]
        public string ResearchStation => CurrentResearchStation.NotEmpty() ? CurrentResearchStation
                                       : DefaultResearchStation.NotEmpty() ? DefaultResearchStation
                                       : "Basic Research Station";

        [XmlIgnore]
        public string MiningStation => CurrentMiningStation.NotEmpty() ? CurrentMiningStation
                                       : DefaultMiningStation.NotEmpty() ? DefaultMiningStation
                                       : "Basic Mining Station";

        [XmlIgnore]
        public bool IsCybernetic => Traits.Cybernetic > 0;
        [XmlIgnore]
        public bool IsFaction => Faction > 0;
        [XmlIgnore]
        public bool IsFactionOrMinorRace => Faction > 0 || MinorRace;

        [XmlIgnore]  public float EnvPerfTerran  => Traits.EnvTerran;
        [XmlIgnore]  public float EnvPerfOceanic => Traits.EnvOceanic;
        [XmlIgnore]  public float EnvPerfSteppe  => Traits.EnvSteppe;
        [XmlIgnore]  public float EnvPerfTundra  => Traits.EnvTundra;
        [XmlIgnore]  public float EnvPerfSwamp   => Traits.EnvSwamp;
        [XmlIgnore]  public float EnvPerfDesert  => Traits.EnvDesert;
        [XmlIgnore]  public float EnvPerfIce     => Traits.EnvIce;
        [XmlIgnore]  public float EnvPerfBarren  => Traits.EnvBarren;
        [XmlIgnore]  public PlanetCategory PreferredEnvPlanet => Traits.PreferredEnv;

        [XmlIgnore]  public string ShipType  => Traits.ShipType;
        [XmlIgnore]  public string VideoPath => Traits.VideoPath;
        [XmlIgnore]  public string Singular => Traits.Singular;
        [XmlIgnore]  public string Plural   => Traits.Plural;
        [XmlIgnore]  public string HomeSystemName => Traits.HomeSystemName;
        [XmlIgnore]  public string HomeWorldName  => Traits.HomeworldName;
        [XmlIgnore]  public string Adj1 => Traits.Adj1;
        [XmlIgnore]  public string Adj2 => Traits.Adj2;

        public EmpireData()
        {
            for (int i = 0; i < WeaponTemplate.TagValues.Length; ++i)
                WeaponTags.Add(WeaponTemplate.TagValues[i], new WeaponTagModifier());
        }

        public EmpireData GetClone()
        {
            return (EmpireData)MemberwiseClone();
        }

        EmpireData IEmpireData.CreateInstance(bool copyTraits)
        {
            var data = (EmpireData)MemberwiseClone();
            if (copyTraits)
            {
                data.Traits = Traits.GetClone();
            }

            // Reset stuff to defaults:
            data.OwnedArtifacts = new();
            data.ResearchQueue = new();
            data.AgentList = new();
            data.MoleList = new();

            data.CounterIntelligenceBudget = 0.0f;
            data.FlatMoneyBonus = 0.0f;
            data.TurnsBelowZero = 0;
            data.TaxRate = 0.25f;

            if (data.DefaultTroopShip.IsEmpty())
            {
                data.DefaultTroopShip = data.PortraitName + " Troop";
            }
            return data;
        }

        public float GetStatBonusForWeaponTag(WeaponStat stat, WeaponTag weaponTag)
        {
            if (!WeaponTags.TryGetValue(weaponTag, out WeaponTagModifier tag))
            {
                Log.Error($"Empire '{Name}' has no WeaponTag '{weaponTag}' entry!");
                return 0f;
            }
            switch (stat)
            {
                case WeaponStat.Damage:    return tag.Damage;
                case WeaponStat.Range:     return tag.Range;
                case WeaponStat.Speed:     return tag.Speed;
                case WeaponStat.FireDelay: return tag.Rate;
                case WeaponStat.Armor:     return tag.ArmorDamage;
                case WeaponStat.Shield:    return tag.ShieldDamage;
                default: return 0f;
            }
        }

        void SetTechModifierDefaults()
        {
            // get the original Archetype and create an instance with original defaults
            IEmpireData race = ResourceManager.AllRaces.First(r => r.ArchetypeName == ArchetypeName);
            EmpireData template = race.CreateInstance(false);

            CivMaintMod = template.CivMaintMod;
            FuelCellModifier = template.FuelCellModifier;
            FTLModifier = template.FTLModifier;
            MassModifier = template.MassModifier;
            ArmourMassModifier = template.ArmourMassModifier;
            SubLightModifier = template.SubLightModifier;

            //EmpireFertilityBonus = 0f; // only modified by artifacts
            SensorModifier = template.SensorModifier;
            OrdnanceEffectivenessBonus = template.OrdnanceEffectivenessBonus;
            ArmorPiercingBonus = template.ArmorPiercingBonus;
            SpoolTimeModifier = template.SpoolTimeModifier;
            ExplosiveRadiusReduction = template.ExplosiveRadiusReduction;
            ShieldPenBonusChance = template.ShieldPenBonusChance;
            SpyModifier = template.SpyModifier;
            DefensiveSpyBonus = template.DefensiveSpyBonus;
            OffensiveSpyBonus = template.OffensiveSpyBonus;
            FTLPowerDrainModifier = template.FTLPowerDrainModifier;
            BonusFighterLevels = template.BonusFighterLevels;
            MissileDodgeChance = template.MissileDodgeChance;
            MissileHPModifier = template.MissileHPModifier;
            BaseReproductiveRate = template.BaseReproductiveRate;

            PowerFlowMod = template.PowerFlowMod;
            ShieldPowerMod = template.ShieldPowerMod;
            ExperienceMod = template.ExperienceMod;

            BombEnvironmentDamageMultiplier = template.BombEnvironmentDamageMultiplier;
            OngoingDiplomaticModifier = template.OngoingDiplomaticModifier;
        }

        public void ResetAllBonusModifiers(Empire empire)
        {
            // reset all general modifiers
            SetTechModifierDefaults();

            // reset all weapon bonuses, race-specific bonuses
            // will be set by InitEmpireUnlocks()
            foreach (WeaponTag tag in WeaponTags.Keys.ToArr())
                WeaponTags[tag] = new WeaponTagModifier();

            // refresh all cached hull bonuses
            EmpireHullBonuses.RefreshBonuses(empire);
        }
    }
} 