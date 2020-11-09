using System;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public sealed class WeaponTagModifier
    {
        [Serialize(0)] public float Speed;           // % bonus
        [Serialize(1)] public float Range;           // % bonus
        [Serialize(2)] public float Rate;            // % bonus
        [Serialize(3)] public float Turn;            // % bonus
        [Serialize(4)] public float Damage;          // % bonus
        [Serialize(5)] public float ExplosionRadius; // % bonus
        [Serialize(6)] public float ShieldDamage;    // % bonus
        [Serialize(7)] public float ArmorDamage;        // FLAT bonus
        [Serialize(8)] public float ShieldPenetration;  // FLAT bonus
        [Serialize(9)] public float HitPoints;          // % bonus
        [Serialize(10)] public float ArmourPenetration; // FLAT bonus
    }

    // @todo Find a better place for this enum
    public enum WeaponStat
    {
        Damage, Range, Speed, FireDelay, Armor, Shield
    }

    public sealed class DTrait
    {
        [Serialize(0)] public string Name;
        [Serialize(1)] public string Description;
        [Serialize(2)] public int Territorialism;
        [Serialize(3)] public float Opportunism;
        [Serialize(4)] public int NAPact;
        [Serialize(5)] public int Alliance;
        [Serialize(6)] public int Trade;
        [Serialize(7)] public int Trustworthiness;
        [Serialize(8)] public float NaturalRelChange;
        [Serialize(9)] public float AngerDissipation;
        [Serialize(10)] public float WeaknessDecline;
        [Serialize(11)] public float PowerIncrease;
        [Serialize(12)] public float TrustGainedAtPeace;

        [XmlIgnore][JsonIgnore]
        public bool IsTrusting => Trustworthiness >= 80;
        [XmlIgnore][JsonIgnore]
        public bool Careless   => Trustworthiness <= 60;
        [XmlIgnore]
        [JsonIgnore]
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
        None, // For player nad avoid null checks
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
    public sealed class ETrait
    {
        [Serialize(0)] public string Name;
        [Serialize(1)] public string Description;
        [Serialize(2)] public string EconomicResearchStrategy;
        [Serialize(3)] public int ColonyGoalsPlus;
        [Serialize(4)] public int ShipGoalsPlus;
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

    public sealed class EmpireData : IEmpireData
    {
        [Serialize(0)] public SerializableDictionary<WeaponTag, WeaponTagModifier> WeaponTags
                        = new SerializableDictionary<WeaponTag, WeaponTagModifier>();
        [Serialize(1)] public string WarpStart { get; set; }
        [Serialize(2)] public string WarpEnd { get; set; }
        [Serialize(3)] public Difficulty difficulty;
        [Serialize(4)] public string CurrentAutoFreighter = "";
        [Serialize(5)] public string CurrentAutoColony    = "";
        [Serialize(6)] public string CurrentAutoScout     = "";
        [Serialize(7)] public string CurrentConstructor   = "";
        [Serialize(8)] public string DiplomacyDialogPath;
        [Serialize(9)] public DTrait DiplomaticPersonality;
        [Serialize(10)] public ETrait EconomicPersonality;

        float TaxRateValue = 0.25f;

        // player modified tax rate
        [Serialize(11)]
        public float TaxRate
        {
            get => TaxRateValue;
            set => TaxRateValue = value.NaNChecked(0.25f, "EmpireData.TaxRate");
        }

        [Serialize(12)] public Array<string> ExcludedDTraits = new Array<string>();
        [Serialize(13)] public Array<string> ExcludedETraits = new Array<string>();
        [Serialize(14)] public BatchRemovalCollection<Agent> AgentList = new BatchRemovalCollection<Agent>();
        [Serialize(15)] public string AbsorbedBy;
        [Serialize(16)] public string StartingShip;
        [Serialize(17)] public string StartingScout;
        [Serialize(18)] public string PrototypeShip;
        [Serialize(19)] public string DefaultColonyShip;
        [Serialize(20)] public string DefaultSmallTransport;
        [Serialize(21)] public string DefaultTroopShip;
        [Serialize(22)] public string DefaultConstructor;
        [Serialize(23)] public string DefaultShipyard = "Shipyard";
        [Serialize(24)] public bool Defeated;
        [Serialize(25)] public bool RebellionLaunched;
        [Serialize(26)] public float MilitaryScoreTotal;
        [Serialize(27)] public int ScoreAverage;
        [Serialize(28)] public string MusicCue;
        [Serialize(29)] public Array<string> ResearchQueue = new Array<string>();
        [Serialize(30)] public BatchRemovalCollection<Mole> MoleList = new BatchRemovalCollection<Mole>();
        [Serialize(31)] public float CounterIntelligenceBudget;

        // NOTE: This is currently the main unique identifier?
        [Serialize(32)] public string PortraitName;
        [Serialize(33)] public string RebelSing;
        [Serialize(34)] public string RebelPlur;
        [Serialize(35)] public int TroopNameIndex;
        [Serialize(36)] public int TroopDescriptionIndex;
        [Serialize(37)] public string RebelName;
        [Serialize(38)] public bool IsRebelFaction;
        [Serialize(39)] public RacialTrait Traits { get; set; }
        [Serialize(40)] public byte Faction;
        [Serialize(41)] public bool MinorRace; // @todo This is deprecated
        [Serialize(42)] public short TurnsBelowZero;
        [Serialize(43)] public bool Privatization;
        [Serialize(44)] public float CivMaintMod = 1f;
        [Serialize(45)] public float FuelCellModifier;
        [Serialize(46)] public float FlatMoneyBonus;
        [Serialize(47)] public float FTLModifier        = 35f;
        [Serialize(48)] public float MassModifier       = 1f;
        [Serialize(49)] public float ArmourMassModifier = 1f;
        [Serialize(50)] public float SubLightModifier   = 1f;
        [Serialize(51)] public float EmpireFertilityBonus;
        [Serialize(52)] public float SensorModifier     = 1f;
        [Serialize(53)] public float OrdnanceEffectivenessBonus;
        [Serialize(54)] public int ArmorPiercingBonus;
        [Serialize(55)] public float SpoolTimeModifier        = 1.0f;
        [Serialize(56)] public float ExplosiveRadiusReduction = 0f;
        [Serialize(57)] public float ShieldPenBonusChance;
        [Serialize(58)] public float SpyModifier;
        [Serialize(59)] public float DefensiveSpyBonus;
        [Serialize(60)] public float OffensiveSpyBonus;
        [Serialize(61)] public float FTLPowerDrainModifier = 2f;
        [Serialize(62)] public Array<Artifact> OwnedArtifacts = new Array<Artifact>();
        [Serialize(63)] public int BonusFighterLevels;
        [Serialize(64)] public float MissileDodgeChance;
        [Serialize(65)] public float MissileHPModifier = 1f;
        [Serialize(66)] public bool Inhibitors;
        [Serialize(67)] public float BaseReproductiveRate = 0.01f;

        // Added by McShooterz: power bonus
        [Serialize(68)] public float PowerFlowMod   = 0f;
        [Serialize(69)] public float ShieldPowerMod = 0f;
        [Serialize(70)] public float ExperienceMod  = 0f;

        // economy
        [Serialize(71)] public float SSPBudget     = 0;
        [Serialize(72)] public float SpyBudget     = 0;
        [Serialize(73)] public float ShipBudget    = 0;
        [Serialize(74)] public float ColonyBudget  = 0;
        [Serialize(75)] public float DefenseBudget = 0;

        // unlock at start
        [Serialize(76)] public Array<string> unlockBuilding = new Array<string>();
        [Serialize(77)] public Array<string> unlockShips    = new Array<string>();

        // designsWeHave our techTree has techs for.
        // sortsaves
        [Serialize(78)] public SortButton PLSort = new SortButton();
        [Serialize(79)] public SortButton ESSort = new SortButton();
        [Serialize(80)] public SortButton SLSort = new SortButton();

        // techTimers
        [Serialize(81)] public short TechDelayTime    = 0;
        [Serialize(82)] public bool  SpyMute          = false;
        [Serialize(83)] public bool  SpyMissionRepeat = false;
        [Serialize(84)] public float treasuryGoal     = 0.20f;
        [Serialize(85)] public bool  AutoTaxes        = false;
        [Serialize(86)] public float BorderTolerance  = 40f;
        [Serialize(87)] public int   BaseShipLevel    = 0;

        //FB: default assault and supply shuttles - it is not mandatory since we have a default boarding / supply shuttles in the game
        [Serialize(88)] public string DefaultAssaultShuttle;
        [Serialize(89)] public string DefaultSupplyShuttle;

        // FB - Thruster Colors
        [Serialize(90)] public byte ThrustColor0R;
        [Serialize(91)] public byte ThrustColor0G;
        [Serialize(92)] public byte ThrustColor0B;
        [Serialize(93)] public byte ThrustColor1R;
        [Serialize(94)] public byte ThrustColor1G;
        [Serialize(95)] public byte ThrustColor1B;

        // FB - Environment
        [Serialize(100)] public float EnvTerran;
        [Serialize(101)] public float EnvOceanic;
        [Serialize(102)] public float EnvSteppe;
        [Serialize(103)] public float EnvTundra;
        [Serialize(104)] public float EnvSwamp;
        [Serialize(105)] public float EnvDesert;
        [Serialize(106)] public float EnvIce;
        [Serialize(107)] public float EnvBarren;
        [Serialize(108)] public PlanetCategory PreferredEnv;

        // FB - Minimum Troop Level
        [Serialize(109)] public int MinimumTroopLevel;

        // FB - For Pirates
        [Serialize(110)] public string PirateSlaverBasic;
        [Serialize(111)] public string PirateSlaverImproved;
        [Serialize(112)] public string PirateSlaverAdvanced;
        [Serialize(113)] public string PirateFighterBasic;
        [Serialize(114)] public string PirateFighterImproved;
        [Serialize(115)] public string PirateFighterAdvanced;
        [Serialize(116)] public string PirateFrigateBasic;
        [Serialize(117)] public string PirateFrigateImproved;
        [Serialize(118)] public string PirateFrigateAdvanced;
        [Serialize(119)] public string PirateBaseBasic;
        [Serialize(120)] public string PirateBaseImproved;
        [Serialize(121)] public string PirateBaseAdvanced;
        [Serialize(122)] public string PirateStationBasic;
        [Serialize(123)] public string PirateStationImproved;
        [Serialize(124)] public string PirateStationAdvanced;
        [Serialize(125)] public string PirateFlagShip;
        [Serialize(126)] public bool IsPirateFaction;
        [Serialize(127)] public int PiratePaymentPeriodTurns = 100; 
        [Serialize(128)] public int MinimumColoniesForStartPayment = 3;
        [Serialize(129)] public Array<float> NormalizedMilitaryScore;

        // FB - For Remnants
        [Serialize(130)] public bool IsRemnantFaction;
        [Serialize(131)] public string RemnantFighter;
        [Serialize(132)] public string RemnantCorvette;
        [Serialize(133)] public string RemnantSupportSmall;
        [Serialize(134)] public string RemnantCarrier;
        [Serialize(135)] public string RemnantAssimilator;
        [Serialize(136)] public string RemnantTorpedoCruiser;
        [Serialize(137)] public string RemnantMotherShip;
        [Serialize(138)] public string RemnantExterminator;
        [Serialize(139)] public string RemnantPortal;
        [Serialize(140)] public string RemnantBomber;
        [Serialize(141)] public string RemnantInhibitor;
        [Serialize(142)] public string RemnantFrigate;
        [Serialize(143)] public string RemnantBomberLight;
        [Serialize(144)] public string RemnantBomberMedium;
        [Serialize(145)] public string RemnantCruiser;

        [XmlIgnore][JsonIgnore] public string Name => Traits.Name;
        [XmlIgnore][JsonIgnore] public string ArchetypeName => PortraitName;

        [XmlIgnore][JsonIgnore] public SubTexture PortraitTex
            => ResourceManager.Texture("Portraits/" + PortraitName);

        public override string ToString() => $"Name: '{Name}' ShipType: {ShipType}";

        [XmlIgnore][JsonIgnore]
        public string ScoutShip => CurrentAutoScout.NotEmpty() ? CurrentAutoScout
                                 : StartingScout.NotEmpty()    ? StartingScout
                                 : "Unarmed Scout";

        [XmlIgnore][JsonIgnore]
        public string FreighterShip => CurrentAutoFreighter.NotEmpty()  ? CurrentAutoFreighter
                                     : DefaultSmallTransport.NotEmpty() ? DefaultSmallTransport
                                     : "Small Transport";
        
        [XmlIgnore][JsonIgnore]
        public string ColonyShip => CurrentAutoColony.NotEmpty() ? CurrentAutoColony
                                  : DefaultColonyShip.NotEmpty() ? DefaultColonyShip
                                  : "Colony Ship";
                
        [XmlIgnore][JsonIgnore]
        public string ConstructorShip => CurrentConstructor.NotEmpty()    ? CurrentConstructor
                                       : DefaultConstructor.NotEmpty()    ? DefaultConstructor
                                       : DefaultSmallTransport.NotEmpty() ? DefaultSmallTransport
                                       : "Small Transport";

        [XmlIgnore][JsonIgnore]
        public bool IsCybernetic => Traits.Cybernetic > 0;
        [XmlIgnore][JsonIgnore]
        public bool IsFaction => Faction > 0;
        [XmlIgnore][JsonIgnore]
        public bool IsFactionOrMinorRace => Faction > 0 || MinorRace;

        public string ShipType  => Traits.ShipType;
        public string VideoPath => Traits.VideoPath;
        public string Singular => Traits.Singular;
        public string Plural   => Traits.Plural;
        public string HomeSystemName => Traits.HomeSystemName;
        public string HomeWorldName  => Traits.HomeworldName;
        public string Adj1 => Traits.Adj1;
        public string Adj2 => Traits.Adj2;

        public EmpireData()
        {
            for (int i = 0; i < Weapon.TagValues.Length; ++i)
                WeaponTags.Add(Weapon.TagValues[i], new WeaponTagModifier());
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
            data.OwnedArtifacts.Clear();
            data.ResearchQueue.Clear();
            data.AgentList.Clear();
            data.MoleList.Clear();

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

        public float NormalizeMilitaryScore(float currentStr)
        {
            int maxItems = 10;
            if (NormalizedMilitaryScore.Count == maxItems)
                NormalizedMilitaryScore.RemoveAt(0);

            NormalizedMilitaryScore.Add(currentStr / 1000);
            return NormalizedMilitaryScore.Sum() / NormalizedMilitaryScore.Count;
        }
    }
} 