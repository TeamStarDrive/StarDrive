using System;
using System.Collections.Generic;
using MsgPack.Serialization;

namespace Ship_Game
{
    public sealed class WeaponTagModifier
    {
        [MessagePackMember(0)] public float Speed;
        [MessagePackMember(1)] public float Range;
        [MessagePackMember(2)] public float Rate;
        [MessagePackMember(3)] public float Turn;
        [MessagePackMember(4)] public float Damage;
        [MessagePackMember(5)] public float ExplosionRadius;
        [MessagePackMember(6)] public float ShieldDamage;
        [MessagePackMember(7)] public float ArmorDamage;
        [MessagePackMember(8)] public float ShieldPenetration;
        [MessagePackMember(9)] public float HitPoints;
        [MessagePackMember(10)] public float ArmourPenetration;
    }

    public sealed class DTrait
    {
        [MessagePackMember(0)] public string Name;
        [MessagePackMember(1)] public string Description;
        [MessagePackMember(2)] public int Territorialism;
        [MessagePackMember(3)] public float Opportunism;
        [MessagePackMember(4)] public int NAPact;
        [MessagePackMember(5)] public int Alliance;
        [MessagePackMember(6)] public int Trade;
        [MessagePackMember(7)] public int Trustworthiness;
        [MessagePackMember(8)] public float NaturalRelChange;
        [MessagePackMember(9)] public float AngerDissipation;
        [MessagePackMember(10)] public float WeaknessDecline;
        [MessagePackMember(11)] public float PowerIncrease;
        [MessagePackMember(12)] public float TrustGainedAtPeace;
    }

    public sealed class ETrait
    {
        [MessagePackMember(0)] public string Name;
        [MessagePackMember(1)] public string Description;
        [MessagePackMember(2)] public string EconomicResearchStrategy;
        [MessagePackMember(3)] public int ColonyGoalsPlus;
        [MessagePackMember(4)] public int ShipGoalsPlus;
    }

    public sealed class EmpireData : IDisposable
	{
        [MessagePackMember(0)] public SerializableDictionary<string, WeaponTagModifier> WeaponTags = new SerializableDictionary<string, WeaponTagModifier>();
        [MessagePackMember(1)] public string WarpStart;
		[MessagePackMember(2)] public string WarpEnd;
		[MessagePackMember(3)] public Difficulty difficulty;
		[MessagePackMember(4)] public bool ModRace;
		[MessagePackMember(5)] public string CurrentAutoFreighter = "";
		[MessagePackMember(6)] public string CurrentAutoColony    = "";
		[MessagePackMember(7)] public string CurrentAutoScout     = "";
        [MessagePackMember(8)] public string CurrentConstructor   = "";
		[MessagePackMember(9)] public string DiplomacyDialogPath;
		[MessagePackMember(10)] public DTrait DiplomaticPersonality;
		[MessagePackMember(11)] public ETrait EconomicPersonality;
		[MessagePackMember(12)] public float TaxRate = 0.25f;
		[MessagePackMember(13)] public List<string> ExcludedDTraits = new List<string>();
		[MessagePackMember(14)] public List<string> ExcludedETraits = new List<string>();
		[MessagePackMember(15)] public BatchRemovalCollection<Agent> AgentList = new BatchRemovalCollection<Agent>();
		[MessagePackMember(16)] public string AbsorbedBy;
		[MessagePackMember(17)] public string StartingShip;
		[MessagePackMember(18)] public string StartingScout;
		[MessagePackMember(19)] public string PrototypeShip;
		[MessagePackMember(20)] public string DefaultColonyShip;
		[MessagePackMember(21)] public string DefaultSmallTransport;
        [MessagePackMember(22)] public string DefaultTroopShip;
        [MessagePackMember(23)] public string DefaultConstructor;
        [MessagePackMember(24)] public string DefaultShipyard = "Shipyard";
		[MessagePackMember(25)] public bool Defeated;
		[MessagePackMember(26)] public bool HasSecretTech;
		[MessagePackMember(27)] public bool RebellionLaunched;
		[MessagePackMember(28)] public float MilitaryScoreTotal;
		[MessagePackMember(29)] public int ScoreAverage;
		[MessagePackMember(30)] public string MusicCue;
		[MessagePackMember(31)] public List<string> ResearchQueue = new List<string>();
		[MessagePackMember(32)] public BatchRemovalCollection<Mole> MoleList = new BatchRemovalCollection<Mole>();
		[MessagePackMember(33)] public float CounterIntelligenceBudget;
		[MessagePackMember(34)] public string PortraitName;
		[MessagePackMember(35)] public string RebelSing;
		[MessagePackMember(36)] public string RebelPlur;
		[MessagePackMember(37)] public int TroopNameIndex;
		[MessagePackMember(38)] public int TroopDescriptionIndex;
		[MessagePackMember(39)] public string RebelName;
		[MessagePackMember(40)] public bool IsRebelFaction;
		[MessagePackMember(41)] public RacialTrait Traits;
		[MessagePackMember(42)] public byte Faction;
        [MessagePackMember(43)] public bool MinorRace;
		[MessagePackMember(44)] public short TurnsBelowZero;
		[MessagePackMember(45)] public bool Privatization;
        [MessagePackMember(46)] public float CivMaintMod = 1f;
		[MessagePackMember(47)] public float FuelCellModifier;
		[MessagePackMember(48)] public float FlatMoneyBonus;
		[MessagePackMember(49)] public float EmpireWideProductionPercentageModifier = 1f;
		[MessagePackMember(50)] public float FTLModifier = 35f;
		[MessagePackMember(51)] public float MassModifier = 1f;
        [MessagePackMember(52)] public float ArmourMassModifier = 1f;
		[MessagePackMember(53)] public float SubLightModifier = 1f;
		[MessagePackMember(54)] public float EmpireFertilityBonus;
		[MessagePackMember(55)] public float SensorModifier = 1f;
		[MessagePackMember(56)] public float OrdnanceEffectivenessBonus;
		[MessagePackMember(57)] public int ArmorPiercingBonus;
        [MessagePackMember(58)] public float SpoolTimeModifier = 1.0f;
		[MessagePackMember(59)] public float ExplosiveRadiusReduction = 0f;
		[MessagePackMember(60)] public float ShieldPenBonusChance;
		[MessagePackMember(61)] public float SpyModifier;
		[MessagePackMember(62)] public float DefensiveSpyBonus;
		[MessagePackMember(63)] public float OffensiveSpyBonus;
        [MessagePackMember(64)] public float FTLPowerDrainModifier = 2f;
        [MessagePackMember(65)] public List<Artifact> OwnedArtifacts = new List<Artifact>();
        [MessagePackMember(66)] public int BonusFighterLevels;
        [MessagePackMember(67)] public float MissileDodgeChance;
        [MessagePackMember(68)] public float MissileHPModifier = 1f;
        [MessagePackMember(69)] public bool Inhibitors;
        [MessagePackMember(70)] public float BaseReproductiveRate = 0.01f;

        //Added by McShooterz: power bonus
        [MessagePackMember(71)] public float PowerFlowMod = 0f;
        [MessagePackMember(72)] public float ShieldPowerMod = 0f;
        [MessagePackMember(73)] public float ExperienceMod = 0f;

        //economy
        [MessagePackMember(74)] public float SSPBudget = 0;
        [MessagePackMember(75)] public float SpyBudget = 0;
        [MessagePackMember(76)] public float ShipBudget = 0;
        [MessagePackMember(77)] public float ColonyBudget = 0;
        [MessagePackMember(78)] public float DefenseBudget = 0;

        //unlock at start
        [MessagePackMember(79)] public List<string> unlockBuilding = new List<string>();
        [MessagePackMember(80)] public List<string> unlockShips = new List<string>();

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;

        //designsWeHave our techTree has techs for.
        //sortsaves
        [MessagePackMember(81)] public SortButton PLSort = new SortButton();
        [MessagePackMember(82)] public SortButton ESSort = new SortButton();
        [MessagePackMember(83)] public SortButton SLSort = new SortButton();

        //techTimers
        [MessagePackMember(84)] public short TechDelayTime=4;
        [MessagePackMember(85)] public bool SpyMute = false;
        [MessagePackMember(86)] public bool SpyMissionRepeat = false;
        [MessagePackMember(87)] public float treasuryGoal = .20f;
        [MessagePackMember(88)] public bool AutoTaxes = false;
        //autosave in save games.        

        [MessagePackIgnore]
        public int AutoSaveFreq => GlobalStats.AutoSaveFreq;

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
            if (disposed) return;
            disposed = true;
            if (disposing)
            {
                AgentList?.Dispose();
                MoleList?.Dispose();
            }
            AgentList = null;
            MoleList = null;
        }
	}
} 