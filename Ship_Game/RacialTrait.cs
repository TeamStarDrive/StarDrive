using System;

namespace Ship_Game
{
	public sealed class RacialTrait
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
        public bool PhysicalTraitAlluring;
        public bool PhysicalTraitRepulsive;

        public bool PhysicalTraitEagleEyed;
        public bool PhysicalTraitBlind;

        public bool PhysicalTraitEfficientMetabolism;
        public bool PhysicalTraitGluttonous;

        public bool PhysicalTraitFertile;
        public bool PhysicalTraitLessFertile;

        public bool PhysicalTraitSmart;
        public bool PhysicalTraitDumb;

        public bool PhysicalTraitReflexes;
        public bool PhysicalTraitPonderous;

        public bool PhysicalTraitSavage;
        public bool PhysicalTraitTimid;

        public bool SociologicalTraitEfficient;
        public bool SociologicalTraitWasteful;

        public bool SociologicalTraitIndustrious;
        public bool SociologicalTraitLazy;

        public bool SociologicalTraitMercantile;

        public bool SociologicalTraitMeticulous;
        public bool SociologicalTraitCorrupt;

        public bool SociologicalTraitSkilledEngineers;
        public bool SociologicalTraitHaphazardEngineers;

        public bool HistoryTraitAstronomers;
        public bool HistoryTraitCybernetic;
        public bool HistoryTraitManifestDestiny;
        public bool HistoryTraitMilitaristic;
        public bool HistoryTraitNavalTraditions;
        public bool HistoryTraitPackMentality;
        public bool HistoryTraitPrototypeFlagship;
        public bool HistoryTraitSpiritual;
        public bool HistoryTraitPollutedHomeWorld;
        public bool HistoryTraitIndustrializedHomeWorld;

        public bool HistoryTraitDuplicitous;
        public bool HistoryTraitHonest;

        public bool HistoryTraitHugeHomeWorld;
        public bool HistoryTraitSmallHomeWorld;

        //Pointless variables
        public int Aquatic;
        public int Burrowers;

		public RacialTrait()
		{
		}

        public RacialTrait GetClone()
        {
            return (RacialTrait)MemberwiseClone();
        }

        //Added by McShooterz: set old values from new bools
        public void LoadTraitConstraints()
        {
            var traits = ResourceManager.RaceTraits;
            if (traits.TraitList == null)
                return;
            foreach (RacialTrait trait in traits.TraitList)
            {
                if (PhysicalTraitAlluring && trait.DiplomacyMod > 0 
                    || PhysicalTraitRepulsive && trait.DiplomacyMod < 0)
                {
                    DiplomacyMod = trait.DiplomacyMod;
                }
                if (PhysicalTraitEagleEyed && trait.EnergyDamageMod > 0 
                    || PhysicalTraitBlind && trait.EnergyDamageMod < 0)
                {
                    EnergyDamageMod = trait.EnergyDamageMod;
                }
                if (PhysicalTraitEfficientMetabolism && trait.ConsumptionModifier < 0 
                    || PhysicalTraitGluttonous && trait.ConsumptionModifier > 0)
                {
                    ConsumptionModifier = trait.ConsumptionModifier;
                }
                if (PhysicalTraitFertile && trait.PopGrowthMin > 0)
                {
                    PopGrowthMin = trait.PopGrowthMin;
                }
                else if (PhysicalTraitLessFertile && trait.PopGrowthMax > 0)
                {
                    PopGrowthMax = trait.PopGrowthMax;
                }
                if (PhysicalTraitSmart && trait.ResearchMod > 0 || PhysicalTraitDumb && trait.ResearchMod < 0)
                {
                    ResearchMod = trait.ResearchMod;
                }
                if (PhysicalTraitReflexes && trait.DodgeMod > 0 || PhysicalTraitPonderous && trait.DodgeMod < 0)
                {
                    DodgeMod = trait.DodgeMod;
                }
                if ((PhysicalTraitSavage && trait.GroundCombatModifier > 0) || (PhysicalTraitTimid && trait.GroundCombatModifier < 0))
                {
                    GroundCombatModifier = trait.GroundCombatModifier;
                }
                if ((SociologicalTraitEfficient && trait.MaintMod < 0) || (SociologicalTraitWasteful && trait.MaintMod > 0))
                {
                    MaintMod = trait.MaintMod;
                }
                if ((SociologicalTraitIndustrious && trait.ProductionMod > 0) || (SociologicalTraitLazy && trait.ProductionMod < 0))
                {
                    ProductionMod = trait.ProductionMod;
                }
                if ((SociologicalTraitMeticulous && trait.TaxMod > 0) || (SociologicalTraitCorrupt && trait.TaxMod < 0))
                {
                    TaxMod = trait.TaxMod;
                }
                if ((SociologicalTraitSkilledEngineers && trait.ModHpModifier > 0) || (SociologicalTraitHaphazardEngineers && trait.ModHpModifier < 0))
                {
                    ModHpModifier = trait.ModHpModifier;
                }
                if (SociologicalTraitMercantile && trait.Mercantile > 0)
                {
                    Mercantile = trait.Mercantile;
                }
                if ((HistoryTraitDuplicitous && trait.SpyMultiplier > 0) || (HistoryTraitHonest && trait.SpyMultiplier < 0))
                {
                    SpyMultiplier = trait.SpyMultiplier;
                }
                if ((HistoryTraitHugeHomeWorld && trait.HomeworldSizeMod > 0) || (HistoryTraitSmallHomeWorld && trait.HomeworldSizeMod < 0))
                {
                    HomeworldSizeMod = trait.HomeworldSizeMod;
                }
                if (HistoryTraitAstronomers && trait.BonusExplored > 0)
                {
                    BonusExplored = trait.BonusExplored;
                }
                if (HistoryTraitCybernetic)
                {
                    Cybernetic = 1;
                    if (trait.RepairMod > 0)
                        RepairMod = trait.RepairMod;
                }
                if (HistoryTraitManifestDestiny && trait.PassengerBonus > 0)
                {
                    PassengerBonus = trait.PassengerBonus;
                }
                if (HistoryTraitNavalTraditions && trait.ShipCostMod < 0)
                {
                    ShipCostMod = trait.ShipCostMod;
                }
                if (HistoryTraitSpiritual && trait.Spiritual > 0)
                {
                    Spiritual = trait.Spiritual;
                }
                if (HistoryTraitPollutedHomeWorld && trait.HomeworldFertMod < 0 && trait.HomeworldRichMod == 0)
                {
                    HomeworldFertMod -= trait.HomeworldFertMod;
                }
                if (HistoryTraitIndustrializedHomeWorld && trait.HomeworldFertMod < 0 && trait.HomeworldRichMod > 0)
                {
                    HomeworldFertMod -= trait.HomeworldFertMod;
                    HomeworldRichMod = trait.HomeworldRichMod;
                }
            }
            if (HistoryTraitMilitaristic)
            {
                Militaristic = 1;
            }
            if (HistoryTraitPackMentality)
            {
                Pack = true;
            }
            if (HistoryTraitPrototypeFlagship)
            {
                Prototype = 1;
            }
        }
	}
}