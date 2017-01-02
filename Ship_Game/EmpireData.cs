using System;
using System.Collections.Generic;
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
    }

    public sealed class ETrait
    {
        [Serialize(0)] public string Name;
        [Serialize(1)] public string Description;
        [Serialize(2)] public string EconomicResearchStrategy;
        [Serialize(3)] public int ColonyGoalsPlus;
        [Serialize(4)] public int ShipGoalsPlus;
    }

    public sealed class EmpireData : IDisposable
	{
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
		[Serialize(12)] public float TaxRate = 0.25f;
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
		[Serialize(41)] public RacialTrait Traits;
		[Serialize(42)] public byte Faction;
        [Serialize(43)] public bool MinorRace;
		[Serialize(44)] public short TurnsBelowZero;
		[Serialize(45)] public bool Privatization;
        [Serialize(46)] public float CivMaintMod = 1f;
		[Serialize(47)] public float FuelCellModifier;
		[Serialize(48)] public float FlatMoneyBonus;
		[Serialize(49)] public float EmpireWideProductionPercentageModifier = 1f;
		[Serialize(50)] public float FTLModifier = 35f;
		[Serialize(51)] public float MassModifier = 1f;
        [Serialize(52)] public float ArmourMassModifier = 1f;
		[Serialize(53)] public float SubLightModifier = 1f;
		[Serialize(54)] public float EmpireFertilityBonus;
		[Serialize(55)] public float SensorModifier = 1f;
		[Serialize(56)] public float OrdnanceEffectivenessBonus;
		[Serialize(57)] public int ArmorPiercingBonus;
        [Serialize(58)] public float SpoolTimeModifier = 1.0f;
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
        [Serialize(71)] public float PowerFlowMod = 0f;
        [Serialize(72)] public float ShieldPowerMod = 0f;
        [Serialize(73)] public float ExperienceMod = 0f;

        //economy
        [Serialize(74)] public float SSPBudget = 0;
        [Serialize(75)] public float SpyBudget = 0;
        [Serialize(76)] public float ShipBudget = 0;
        [Serialize(77)] public float ColonyBudget = 0;
        [Serialize(78)] public float DefenseBudget = 0;

        //unlock at start
        [Serialize(79)] public Array<string> unlockBuilding = new Array<string>();
        [Serialize(80)] public Array<string> unlockShips = new Array<string>();

        //designsWeHave our techTree has techs for.
        //sortsaves
        [Serialize(81)] public SortButton PLSort = new SortButton();
        [Serialize(82)] public SortButton ESSort = new SortButton();
        [Serialize(83)] public SortButton SLSort = new SortButton();

        //techTimers
        [Serialize(84)] public short TechDelayTime=4;
        [Serialize(85)] public bool SpyMute = false;
        [Serialize(86)] public bool SpyMissionRepeat = false;
        [Serialize(87)] public float treasuryGoal = .20f;
        [Serialize(88)] public bool AutoTaxes = false;

		public EmpireData()
		{
            // @todo Mapping these by string is a bad idea. Consider using an Enum
			WeaponTags.Add("Kinetic", new WeaponTagModifier());
			WeaponTags.Add("Energy", new WeaponTagModifier());
			WeaponTags.Add("Beam", new WeaponTagModifier());
			WeaponTags.Add("Hybrid", new WeaponTagModifier());
			WeaponTags.Add("Railgun", new WeaponTagModifier());
			WeaponTags.Add("Missile", new WeaponTagModifier());
			WeaponTags.Add("Explosive", new WeaponTagModifier());
			WeaponTags.Add("Guided", new WeaponTagModifier());
			WeaponTags.Add("Intercept", new WeaponTagModifier());
			WeaponTags.Add("PD", new WeaponTagModifier());
			WeaponTags.Add("Spacebomb", new WeaponTagModifier());
			WeaponTags.Add("BioWeapon", new WeaponTagModifier());
			WeaponTags.Add("Drone", new WeaponTagModifier());
			WeaponTags.Add("Torpedo", new WeaponTagModifier());
			WeaponTags.Add("Subspace", new WeaponTagModifier());
			WeaponTags.Add("Warp", new WeaponTagModifier());
            //added by McShooterz: added missing tags
            WeaponTags.Add("Cannon", new WeaponTagModifier());
            WeaponTags.Add("Bomb", new WeaponTagModifier());
            //added by The Doctor: New tags
            WeaponTags.Add("Array", new WeaponTagModifier());
            WeaponTags.Add("Flak", new WeaponTagModifier());
            WeaponTags.Add("Tractor", new WeaponTagModifier());

		}

		public EmpireData GetClone()
		{
			return (EmpireData)MemberwiseClone();
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~EmpireData() { Dispose(false); }

        private void Dispose(bool disposing)
        {

        }
	}
} 