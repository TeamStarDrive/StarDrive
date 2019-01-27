using System;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Ship_Game
{
    public sealed class WeaponTagModifier
    {
        [Serialize(0)] public float Speed;
        [Serialize(1)] public float Range;
        [Serialize(2)] public float Rate;
        [Serialize(3)] public float Turn;
        [Serialize(4)] public float Damage;
        [Serialize(5)] public float ExplosionRadius;
        [Serialize(6)] public float ShieldDamage;
        [Serialize(7)] public float ArmorDamage;
        [Serialize(8)] public float ShieldPenetration;
        [Serialize(9)] public float HitPoints;
        [Serialize(10)] public float ArmourPenetration;
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
    }

    public sealed class EmpireData : IEmpireData
    {
        public WeaponTagModifier GetWeaponTag(string weaponTag)
        {
            WeaponTags.TryGetValue(weaponTag, out WeaponTagModifier tag);
            if (tag != null) return tag;

            //@HACK issue. if a module has not weapon type defined it will not have a tag. 
            //this appears to crash here with no tags. So its not absolutely a fatal error i dont think.
            //its just a baddly defined weapon. BUT! from here we cant give a good error message.
            //We dont know the weapon and neither does the caller. So how do we make this give a good error message?
            tag = new WeaponTagModifier();
            Log.Warning("Selected Module Weapon had no weapon type defined.");
            return tag;
        }

        [Serialize(0)] public SerializableDictionary<string, WeaponTagModifier> WeaponTags = new SerializableDictionary<string, WeaponTagModifier>();
        [Serialize(1)] public string WarpStart;
        [Serialize(2)] public string WarpEnd;
        [Serialize(3)] public Difficulty difficulty;
        [Serialize(4)] public bool ModRace;
        [Serialize(5)] public string CurrentAutoFreighter = "";
        [Serialize(6)] public string CurrentAutoColony    = "";
        [Serialize(7)] public string CurrentAutoScout     = "";
        [Serialize(8)] public string CurrentConstructor   = "";
        [Serialize(9)] public string DiplomacyDialogPath;
        [Serialize(10)] public DTrait DiplomaticPersonality;
        [Serialize(11)] public ETrait EconomicPersonality;
        [Serialize(12)] public float TaxRate = 0.25f; // player modified tax rate
        [Serialize(13)] public Array<string> ExcludedDTraits = new Array<string>();
        [Serialize(14)] public Array<string> ExcludedETraits = new Array<string>();
        [Serialize(15)] public BatchRemovalCollection<Agent> AgentList = new BatchRemovalCollection<Agent>();
        [Serialize(16)] public string AbsorbedBy;
        [Serialize(17)] public string StartingShip;
        [Serialize(18)] public string StartingScout;
        [Serialize(19)] public string PrototypeShip;
        [Serialize(20)] public string DefaultColonyShip;
        [Serialize(21)] public string DefaultSmallTransport;
        [Serialize(22)] public string DefaultTroopShip;
        [Serialize(23)] public string DefaultConstructor;
        [Serialize(24)] public string DefaultShipyard = "Shipyard";
        [Serialize(25)] public bool Defeated;
        [Serialize(26)] public bool HasSecretTech;
        [Serialize(27)] public bool RebellionLaunched;
        [Serialize(28)] public float MilitaryScoreTotal;
        [Serialize(29)] public int ScoreAverage;
        [Serialize(30)] public string MusicCue;
        [Serialize(31)] public Array<string> ResearchQueue = new Array<string>();
        [Serialize(32)] public BatchRemovalCollection<Mole> MoleList = new BatchRemovalCollection<Mole>();
        [Serialize(33)] public float CounterIntelligenceBudget;
        [Serialize(34)] public string PortraitName;
        [Serialize(35)] public string RebelSing;
        [Serialize(36)] public string RebelPlur;
        [Serialize(37)] public int TroopNameIndex;
        [Serialize(38)] public int TroopDescriptionIndex;
        [Serialize(39)] public string RebelName;
        [Serialize(40)] public bool IsRebelFaction;
        [Serialize(41)] public RacialTrait Traits { get; set; }
        [Serialize(42)] public byte Faction;
        [Serialize(43)] public bool MinorRace; // @todo This is deprecated
        [Serialize(44)] public short TurnsBelowZero;
        [Serialize(45)] public bool Privatization;
        [Serialize(46)] public float CivMaintMod = 1f;
        [Serialize(47)] public float FuelCellModifier;
        [Serialize(48)] public float FlatMoneyBonus;
        [Serialize(49)] public float EmpireWideProductionPercentageModifier = 1f; // @todo wtf??
        [Serialize(50)] public float FTLModifier        = 35f;
        [Serialize(51)] public float MassModifier       = 1f;
        [Serialize(52)] public float ArmourMassModifier = 1f;
        [Serialize(53)] public float SubLightModifier   = 1f;
        [Serialize(54)] public float EmpireFertilityBonus;
        [Serialize(55)] public float SensorModifier     = 1f;
        [Serialize(56)] public float OrdnanceEffectivenessBonus;
        [Serialize(57)] public int ArmorPiercingBonus;
        [Serialize(58)] public float SpoolTimeModifier        = 1.0f;
        [Serialize(59)] public float ExplosiveRadiusReduction = 0f;
        [Serialize(60)] public float ShieldPenBonusChance;
        [Serialize(61)] public float SpyModifier;
        [Serialize(62)] public float DefensiveSpyBonus;
        [Serialize(63)] public float OffensiveSpyBonus;
        [Serialize(64)] public float FTLPowerDrainModifier = 2f;
        [Serialize(65)] public Array<Artifact> OwnedArtifacts = new Array<Artifact>();
        [Serialize(66)] public int BonusFighterLevels;
        [Serialize(67)] public float MissileDodgeChance;
        [Serialize(68)] public float MissileHPModifier = 1f;
        [Serialize(69)] public bool Inhibitors;
        [Serialize(70)] public float BaseReproductiveRate = 0.01f;

        //Added by McShooterz: power bonus
        [Serialize(71)] public float PowerFlowMod   = 0f;
        [Serialize(72)] public float ShieldPowerMod = 0f;
        [Serialize(73)] public float ExperienceMod  = 0f;

        //economy
        [Serialize(74)] public float SSPBudget     = 0;
        [Serialize(75)] public float SpyBudget     = 0;
        [Serialize(76)] public float ShipBudget    = 0;
        [Serialize(77)] public float ColonyBudget  = 0;
        [Serialize(78)] public float DefenseBudget = 0;

        //unlock at start
        [Serialize(79)] public Array<string> unlockBuilding = new Array<string>();
        [Serialize(80)] public Array<string> unlockShips    = new Array<string>();

        //designsWeHave our techTree has techs for.
        //sortsaves
        [Serialize(81)] public SortButton PLSort = new SortButton();
        [Serialize(82)] public SortButton ESSort = new SortButton();
        [Serialize(83)] public SortButton SLSort = new SortButton();

        //techTimers
        [Serialize(84)] public short TechDelayTime    = 4;
        [Serialize(85)] public bool  SpyMute          = false;
        [Serialize(86)] public bool  SpyMissionRepeat = false;
        [Serialize(87)] public float treasuryGoal     = 0.20f;
        [Serialize(88)] public bool  AutoTaxes        = false;
        [Serialize(89)] public float BorderTolerance  = 40f;
        [Serialize(90)] public int   BaseShipLevel    = 0;
        [Serialize(91)] public float PlayerTaxGoal    = .2f;

        //FB: default assault and supply shuttles - it is not mandatory since we have a default boarding / supply shuttles in the game
        [Serialize(92)] public string DefaultAssaultShuttle;
        [Serialize(93)] public string DefaultSupplyShuttle;

        [XmlIgnore][JsonIgnore] public string Name => Traits.Name;

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
            // @todo Mapping these by string is a bad idea. Consider using an Enum
            WeaponTags.Add("Kinetic",   new WeaponTagModifier());
            WeaponTags.Add("Energy",    new WeaponTagModifier());
            WeaponTags.Add("Beam",      new WeaponTagModifier());
            WeaponTags.Add("Hybrid",    new WeaponTagModifier());
            WeaponTags.Add("Railgun",   new WeaponTagModifier());
            WeaponTags.Add("Missile",   new WeaponTagModifier());
            WeaponTags.Add("Explosive", new WeaponTagModifier());
            WeaponTags.Add("Guided",    new WeaponTagModifier());
            WeaponTags.Add("Intercept", new WeaponTagModifier());
            WeaponTags.Add("PD",        new WeaponTagModifier());
            WeaponTags.Add("Spacebomb", new WeaponTagModifier());
            WeaponTags.Add("BioWeapon", new WeaponTagModifier());
            WeaponTags.Add("Drone",     new WeaponTagModifier());
            WeaponTags.Add("Torpedo",   new WeaponTagModifier());
            WeaponTags.Add("Subspace",  new WeaponTagModifier());
            WeaponTags.Add("Warp",      new WeaponTagModifier());
            //added by McShooterz: added missing tags
            WeaponTags.Add("Cannon",    new WeaponTagModifier());
            WeaponTags.Add("Bomb",      new WeaponTagModifier());
            //added by The Doctor: New tags
            WeaponTags.Add("Array",     new WeaponTagModifier());
            WeaponTags.Add("Flak",      new WeaponTagModifier());
            WeaponTags.Add("Tractor",   new WeaponTagModifier());
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

            // @todo This is so borked, I don't even...
            //       Reset stuff to defaults:
            data.OwnedArtifacts.Clear();
            data.MoleList.Clear();
            data.ResearchQueue.Clear();
            data.AgentList.Clear();

            data.CounterIntelligenceBudget = 0.0f;
            data.FlatMoneyBonus  = 0.0f;
            data.TaxRate = 0.25f;
            data.TurnsBelowZero = 0;

            if (data.DefaultTroopShip.IsEmpty())
            {
                data.DefaultTroopShip = data.PortraitName + " " + "Troop";
            }

            return data;
        }
    }
} 