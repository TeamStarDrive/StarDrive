using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class EmpireData : IDisposable
	{
		public SerializableDictionary<string, WeaponTagModifier> WeaponTags = new SerializableDictionary<string, WeaponTagModifier>();

		public string WarpStart;

		public string WarpEnd;

		public Difficulty difficulty;

		public bool ModRace;

		public string CurrentAutoFreighter = "";

		public string CurrentAutoColony = "";

		public string CurrentAutoScout = "";

        public string CurrentConstructor = "";

		public string DiplomacyDialogPath;

		public DTrait DiplomaticPersonality;

		public ETrait EconomicPersonality;

		public float TaxRate = 0.25f;

		public List<string> ExcludedDTraits = new List<string>();

		public List<string> ExcludedETraits = new List<string>();

		public BatchRemovalCollection<Agent> AgentList = new BatchRemovalCollection<Agent>();

		public string AbsorbedBy;

		public string StartingShip;

		public string StartingScout;

		public string PrototypeShip;

		public string DefaultColonyShip;

		public string DefaultSmallTransport;

        public string DefaultTroopShip;

        public string DefaultConstructor;

        public string DefaultShipyard = "Shipyard";

		public bool Defeated;

		public bool HasSecretTech;

		public bool RebellionLaunched;

		public float MilitaryScoreTotal;

		public int ScoreAverage;

		public string MusicCue;

		public List<string> ResearchQueue = new List<string>();

		public BatchRemovalCollection<Mole> MoleList = new BatchRemovalCollection<Mole>();

		public float CounterIntelligenceBudget;

		public string PortraitName;

		public string RebelSing;

		public string RebelPlur;

		public int TroopNameIndex;

		public int TroopDescriptionIndex;

		public string RebelName;

		public bool IsRebelFaction;

		public RacialTrait Traits;

		public byte Faction;

        public bool MinorRace;

		public short TurnsBelowZero;

		public bool Privatization;

        public float CivMaintMod = 1f;

		public float FuelCellModifier;

		public float FlatMoneyBonus;

		public float EmpireWideProductionPercentageModifier = 1f;

		public float FTLModifier = 35f;

		public float MassModifier = 1f;

        public float ArmourMassModifier = 1f;

		public float SubLightModifier = 1f;

		public float EmpireFertilityBonus;

		public float SensorModifier = 1f;

		public float OrdnanceEffectivenessBonus;

		public int ArmorPiercingBonus;

        public float SpoolTimeModifier = 1.0f;

		public float ExplosiveRadiusReduction = 0f;

		public float ShieldPenBonusChance;

		public float SpyModifier;

		public float DefensiveSpyBonus;

		public float OffensiveSpyBonus;

		public float FTLPowerDrainModifier = 2f;

		public List<Artifact> OwnedArtifacts = new List<Artifact>();

		public int BonusFighterLevels;

		public float MissileDodgeChance;

		public float MissileHPModifier = 1f;

		public bool Inhibitors;

		public float BaseReproductiveRate = 0.01f;

        //Added by McShooterz: power bonus
        public float PowerFlowMod = 0f;
        public float ShieldPowerMod = 0f;
        public float ExperienceMod = 0f;
        
        //economy
        public float SSPBudget = 0;
        public float SpyBudget = 0;
        public float ShipBudget = 0;
        public float ColonyBudget = 0;
        public float DefenseBudget = 0;

        //unlock at start
        public List<string> unlockBuilding = new List<string>();
        public List<string> unlockShips = new List<string>();

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;

        //designsWeHave our techTree has techs for.
        //sortsaves
        public SortButton PLSort = new SortButton();
        public SortButton ESSort = new SortButton();
        public SortButton SLSort = new SortButton();

        //techTimers
        public short TechDelayTime=4;

        public bool SpyMute = false;
        public bool SpyMissionRepeat = false;
        public float treasuryGoal = .20f;
        public bool AutoTaxes = false;
        //autosave in save games.        

        private int autosavefreq =0 ;

        public int AutoSaveFreq
        {
            get {
                if (this.autosavefreq == 0)
                    return GlobalStats.AutoSaveFreq;
                else
                    return autosavefreq; 
            }
            set { autosavefreq = value; }
        }

		public EmpireData()
		{
			this.WeaponTags.Add("Kinetic", new WeaponTagModifier());
			this.WeaponTags.Add("Energy", new WeaponTagModifier());
			this.WeaponTags.Add("Beam", new WeaponTagModifier());
			this.WeaponTags.Add("Hybrid", new WeaponTagModifier());
			this.WeaponTags.Add("Railgun", new WeaponTagModifier());
			this.WeaponTags.Add("Missile", new WeaponTagModifier());
			this.WeaponTags.Add("Explosive", new WeaponTagModifier());
			this.WeaponTags.Add("Guided", new WeaponTagModifier());
			this.WeaponTags.Add("Intercept", new WeaponTagModifier());
			this.WeaponTags.Add("PD", new WeaponTagModifier());
			this.WeaponTags.Add("Spacebomb", new WeaponTagModifier());
			this.WeaponTags.Add("BioWeapon", new WeaponTagModifier());
			this.WeaponTags.Add("Drone", new WeaponTagModifier());
			this.WeaponTags.Add("Torpedo", new WeaponTagModifier());
			this.WeaponTags.Add("Subspace", new WeaponTagModifier());
			this.WeaponTags.Add("Warp", new WeaponTagModifier());
            //added by McShooterz: added missing tags
            this.WeaponTags.Add("Cannon", new WeaponTagModifier());
            this.WeaponTags.Add("Bomb", new WeaponTagModifier());
            //added by The Doctor: New tags
            this.WeaponTags.Add("Array", new WeaponTagModifier());
            this.WeaponTags.Add("Flak", new WeaponTagModifier());
            this.WeaponTags.Add("Tractor", new WeaponTagModifier());

		}

		public EmpireData GetClone()
		{
			return (EmpireData)this.MemberwiseClone();
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~EmpireData() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.AgentList != null)
                        this.AgentList.Dispose();
                    if (this.MoleList != null)
                        this.MoleList.Dispose();

                }
                this.AgentList = null;
                this.MoleList = null;
                this.disposed = true;
            }
        }
	}
} 