using System;

namespace Ship_Game
{
	public class RacialTrait
	{
		public string Name;

		public int TraitName;

		public string VideoPath = "";

		public string ShipType = "Pollops";

		public string Singular;

		public string Plural;

		public bool Pack;

		public float SpyMultiplier;

		public string Adj1;

		public string Adj2;

		public string HomeSystemName;

		public string HomeworldName;

		public int FlagIndex;

		public float R;

		public float G;

		public float B;

		public int Excludes;

		public int Description;

		public string RaceDescription;

		public string Category;

		public int Cost;

		public float ConsumptionModifier;

		public float ReproductionMod;

		public float PopGrowthMax;

		public float PopGrowthMin;

		public float DiplomacyMod;

		public float GenericMaxPopMod;

		public int Aquatic;

		public int Burrowers;

		public int Blind;

		public int BonusExplored;

		public int Militaristic;

		public float HomeworldSizeMod;

		public int Prewarp;

		public int Prototype;

		public float Spiritual;

		public float HomeworldFertMod;

		public float HomeworldRichMod;

		public float DodgeMod;

		public float EnergyDamageMod;

		public float ResearchMod;

		public float Mercantile;

		public int Miners;

		public float ProductionMod;

		public float MaintMod;

		public float InBordersSpeedBonus = 0.5f;

		public float TaxMod;

		public float ShipCostMod;

		public float ModHpModifier;

		public int SmallSize;

		public int HugeSize;

		public int PassengerModifier = 1;

		public int PassengerBonus;

		public bool Assimilators;

		public float GroundCombatModifier;

		public float RepairMod;

		public int Cybernetic;

        //Trait Booleans
        public Boolean PhysicalTraitAlluring;
        public Boolean PhysicalTraitRepulsive;

        public Boolean PhysicalTraitEagleEyed;
        public Boolean PhysicalTraitBlind;

        public Boolean PhysicalTraitEfficientMetabolism;
        public Boolean PhysicalTraitGluttonous;

        public Boolean PhysicalTraitFertile;
        public Boolean PhysicalTraitLessFertile;

        public Boolean PhysicalTraitSmart;
        public Boolean PhysicalTraitDumb;

        public Boolean PhysicalTraitReflexes;
        public Boolean PhysicalTraitPonderous;

        public Boolean PhysicalTraitSavage;
        public Boolean PhysicalTraitTimid;

        public Boolean SociologicalTraitEfficient;
        public Boolean SociologicalTraitWasteful;

        public Boolean SociologicalTraitIndustrious;
        public Boolean SociologicalTraitLazy;

        public Boolean SociologicalTraitMercantile;

        public Boolean SociologicalTraitMeticulous;
        public Boolean SociologicalTraitCorrupt;

        public Boolean SociologicalTraitSkilledEngineers;
        public Boolean SociologicalTraitHaphazardEngineers;

        public Boolean HistoryTraitAstronomers;
        public Boolean HistoryTraitCybernetic;
        public Boolean HistoryTraitManifestDestiny;
        public Boolean HistoryTraitMilitaristic;
        public Boolean HistoryTraitNavalTraditions;
        public Boolean HistoryTraitPackMentality;
        public Boolean HistoryTraitPrototypeFlagship;
        public Boolean HistoryTraitSpiritual;
        public Boolean HistoryTraitPollutedHomeWorld;
        public Boolean HistoryTraitIndustrializedHomeWorld;

        public Boolean HistoryTraitDuplicitous;
        public Boolean HistoryTraitHonest;

        public Boolean HistoryTraitHugeHomeWorld;
        public Boolean HistoryTraitSmallHomeWorld;

        //Pointless variables
        public int Aquatic;
        public int Burrowers;

		public RacialTrait()
		{
		}
	}
}