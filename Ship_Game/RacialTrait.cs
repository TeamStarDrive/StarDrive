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

		public string Category;

		public int Cost;

		public float ConsumptionModifier;

		public float ReproductionMod;

		public float PopGrowthMax;

		public float PopGrowthMin;

		public float DiplomacyMod;

		public float GenericMaxPopMod;

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

        //Added by McShooterz: set old values from new bools
        public void SetValues()
        {
            if (ResourceManager.GetRaceTraits().TraitList == null)
                return;
            foreach (RacialTrait RacialTrait in ResourceManager.GetRaceTraits().TraitList)
            {
                if ((this.PhysicalTraitAlluring && RacialTrait.DiplomacyMod > 0) || (this.PhysicalTraitRepulsive && RacialTrait.DiplomacyMod < 0))
                {
                    this.DiplomacyMod = RacialTrait.DiplomacyMod;
                }
                if ((this.PhysicalTraitEagleEyed && RacialTrait.EnergyDamageMod > 0) || (this.PhysicalTraitBlind && RacialTrait.EnergyDamageMod < 0))
                {
                    this.EnergyDamageMod = RacialTrait.EnergyDamageMod;
                }
                if ((this.PhysicalTraitEfficientMetabolism && RacialTrait.ConsumptionModifier < 0) || (this.PhysicalTraitGluttonous && RacialTrait.ConsumptionModifier > 0))
                {
                    this.ConsumptionModifier = RacialTrait.ConsumptionModifier;
                }
                if (this.PhysicalTraitFertile && RacialTrait.PopGrowthMin > 0)
                {
                    this.PopGrowthMin = RacialTrait.PopGrowthMin;
                }
                else if (this.PhysicalTraitLessFertile && RacialTrait.PopGrowthMax > 0)
                {
                    this.PopGrowthMax = RacialTrait.PopGrowthMax;
                }
                if ((this.PhysicalTraitSmart && RacialTrait.ResearchMod > 0) || (this.PhysicalTraitDumb && RacialTrait.ResearchMod < 0))
                {
                    this.ResearchMod = RacialTrait.ResearchMod;
                }
                if ((this.PhysicalTraitReflexes && RacialTrait.DodgeMod > 0) || (this.PhysicalTraitPonderous && RacialTrait.DodgeMod < 0))
                {
                    this.DodgeMod = RacialTrait.DodgeMod;
                }
                if ((this.PhysicalTraitSavage && RacialTrait.GroundCombatModifier > 0) || (this.PhysicalTraitTimid && RacialTrait.GroundCombatModifier < 0))
                {
                    this.GroundCombatModifier = RacialTrait.GroundCombatModifier;
                }
                if ((this.SociologicalTraitEfficient && RacialTrait.MaintMod < 0) || (this.SociologicalTraitWasteful && RacialTrait.MaintMod > 0))
                {
                    this.MaintMod = RacialTrait.MaintMod;
                }
                if ((this.SociologicalTraitIndustrious && RacialTrait.ProductionMod > 0) || (this.SociologicalTraitLazy && RacialTrait.ProductionMod < 0))
                {
                    this.ProductionMod = RacialTrait.ProductionMod;
                }
                if ((this.SociologicalTraitMeticulous && RacialTrait.TaxMod > 0) || (this.SociologicalTraitCorrupt && RacialTrait.TaxMod < 0))
                {
                    this.TaxMod = RacialTrait.TaxMod;
                }
                if ((this.SociologicalTraitSkilledEngineers && RacialTrait.ModHpModifier > 0) || (this.SociologicalTraitHaphazardEngineers && RacialTrait.ModHpModifier < 0))
                {
                    this.ModHpModifier = RacialTrait.ModHpModifier;
                }
                if (this.SociologicalTraitMercantile && RacialTrait.Mercantile > 0)
                {
                    this.Mercantile = RacialTrait.Mercantile;
                }
                if ((this.HistoryTraitDuplicitous && RacialTrait.SpyMultiplier > 0) || (this.HistoryTraitHonest && RacialTrait.SpyMultiplier < 0))
                {
                    this.SpyMultiplier = RacialTrait.SpyMultiplier;
                }
                if ((this.HistoryTraitHugeHomeWorld && RacialTrait.HomeworldSizeMod > 0) || (this.HistoryTraitSmallHomeWorld && RacialTrait.HomeworldSizeMod < 0))
                {
                    this.HomeworldSizeMod = RacialTrait.HomeworldSizeMod;
                }
                if (this.HistoryTraitAstronomers && RacialTrait.BonusExplored > 0)
                {
                    this.BonusExplored = RacialTrait.BonusExplored;
                }
                if (this.HistoryTraitCybernetic)
                {
                    this.Cybernetic = 1;
                    if (RacialTrait.RepairMod > 0)
                        this.RepairMod = RacialTrait.RepairMod;
                }
                if (this.HistoryTraitManifestDestiny && RacialTrait.PassengerBonus > 0)
                {
                    this.PassengerBonus = RacialTrait.PassengerBonus;
                }
                if (this.HistoryTraitNavalTraditions && RacialTrait.ShipCostMod < 0)
                {
                    this.ShipCostMod = RacialTrait.ShipCostMod;
                }
                if (this.HistoryTraitSpiritual && RacialTrait.Spiritual > 0)
                {
                    this.Spiritual = RacialTrait.Spiritual;
                }
                if (this.HistoryTraitPollutedHomeWorld && RacialTrait.HomeworldFertMod < 0 && RacialTrait.HomeworldRichMod == 0)
                {
                    this.HomeworldFertMod -= RacialTrait.HomeworldFertMod;
                }
                if (this.HistoryTraitIndustrializedHomeWorld && RacialTrait.HomeworldFertMod < 0 && RacialTrait.HomeworldRichMod > 0)
                {
                    this.HomeworldFertMod -= RacialTrait.HomeworldFertMod;
                    this.HomeworldRichMod = RacialTrait.HomeworldRichMod;
                }
            }
            if (this.HistoryTraitMilitaristic)
            {
                this.Militaristic = 1;
            }
            if (this.HistoryTraitPackMentality)
            {
                this.Pack = true;
            }
            if (this.HistoryTraitPrototypeFlagship)
            {
                this.Prototype = 1;
            }
        }
	}
}