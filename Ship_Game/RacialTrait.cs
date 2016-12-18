using System;
using MsgPack.Serialization;

namespace Ship_Game
{
	public sealed class RacialTrait
	{
		[MessagePackMember(0)] public string Name;
		[MessagePackMember(1)] public int TraitName;
		[MessagePackMember(2)] public string VideoPath = "";
		[MessagePackMember(3)] public string ShipType = "Pollops";
		[MessagePackMember(4)] public string Singular;
		[MessagePackMember(5)] public string Plural;
		[MessagePackMember(6)] public bool Pack;
		[MessagePackMember(7)] public float SpyMultiplier;
		[MessagePackMember(8)] public string Adj1;
		[MessagePackMember(9)] public string Adj2;
		[MessagePackMember(10)] public string HomeSystemName;
		[MessagePackMember(11)] public string HomeworldName;
		[MessagePackMember(12)] public int FlagIndex;
		[MessagePackMember(13)] public float R;
		[MessagePackMember(14)] public float G;
		[MessagePackMember(15)] public float B;
		[MessagePackMember(16)] public int Excludes;
		[MessagePackMember(17)] public int Description;
		[MessagePackMember(18)] public string Category;
		[MessagePackMember(19)] public int Cost;
		[MessagePackMember(20)] public float ConsumptionModifier;
		[MessagePackMember(21)] public float ReproductionMod;
		[MessagePackMember(22)] public float PopGrowthMax;
		[MessagePackMember(23)] public float PopGrowthMin;
		[MessagePackMember(24)] public float DiplomacyMod;
		[MessagePackMember(25)] public float GenericMaxPopMod;
		[MessagePackMember(26)] public int Blind;
		[MessagePackMember(27)] public int BonusExplored;
		[MessagePackMember(28)] public int Militaristic;
		[MessagePackMember(29)] public float HomeworldSizeMod;
		[MessagePackMember(30)] public int Prewarp;
		[MessagePackMember(31)] public int Prototype;
		[MessagePackMember(32)] public float Spiritual;
		[MessagePackMember(33)] public float HomeworldFertMod;
		[MessagePackMember(34)] public float HomeworldRichMod;
		[MessagePackMember(35)] public float DodgeMod;
		[MessagePackMember(36)] public float EnergyDamageMod;
		[MessagePackMember(37)] public float ResearchMod;
		[MessagePackMember(38)] public float Mercantile;
		[MessagePackMember(39)] public int Miners;
		[MessagePackMember(40)] public float ProductionMod;
		[MessagePackMember(41)] public float MaintMod;
		[MessagePackMember(42)] public float InBordersSpeedBonus = 0.5f;
		[MessagePackMember(43)] public float TaxMod;
		[MessagePackMember(44)] public float ShipCostMod;
		[MessagePackMember(45)] public float ModHpModifier;
		[MessagePackMember(46)] public int SmallSize;
		[MessagePackMember(47)] public int HugeSize;
		[MessagePackMember(48)] public int PassengerModifier = 1;
		[MessagePackMember(49)] public int PassengerBonus;
		[MessagePackMember(50)] public bool Assimilators;
		[MessagePackMember(51)] public float GroundCombatModifier;
		[MessagePackMember(52)] public float RepairMod;
		[MessagePackMember(53)] public int Cybernetic;

        //Trait Booleans
        [MessagePackMember(54)] public bool PhysicalTraitAlluring;
        [MessagePackMember(55)] public bool PhysicalTraitRepulsive;
        [MessagePackMember(56)] public bool PhysicalTraitEagleEyed;
        [MessagePackMember(57)] public bool PhysicalTraitBlind;
        [MessagePackMember(58)] public bool PhysicalTraitEfficientMetabolism;
        [MessagePackMember(59)] public bool PhysicalTraitGluttonous;
        [MessagePackMember(60)] public bool PhysicalTraitFertile;
        [MessagePackMember(61)] public bool PhysicalTraitLessFertile;
        [MessagePackMember(62)] public bool PhysicalTraitSmart;
        [MessagePackMember(63)] public bool PhysicalTraitDumb;
        [MessagePackMember(64)] public bool PhysicalTraitReflexes;
        [MessagePackMember(65)] public bool PhysicalTraitPonderous;
        [MessagePackMember(66)] public bool PhysicalTraitSavage;
        [MessagePackMember(67)] public bool PhysicalTraitTimid;
        [MessagePackMember(68)] public bool SociologicalTraitEfficient;
        [MessagePackMember(69)] public bool SociologicalTraitWasteful;
        [MessagePackMember(70)] public bool SociologicalTraitIndustrious;
        [MessagePackMember(71)] public bool SociologicalTraitLazy;
        [MessagePackMember(72)] public bool SociologicalTraitMercantile;
        [MessagePackMember(73)] public bool SociologicalTraitMeticulous;
        [MessagePackMember(74)] public bool SociologicalTraitCorrupt;
        [MessagePackMember(75)] public bool SociologicalTraitSkilledEngineers;
        [MessagePackMember(76)] public bool SociologicalTraitHaphazardEngineers;

        [MessagePackMember(77)] public bool HistoryTraitAstronomers;
        [MessagePackMember(78)] public bool HistoryTraitCybernetic;
        [MessagePackMember(79)] public bool HistoryTraitManifestDestiny;
        [MessagePackMember(80)] public bool HistoryTraitMilitaristic;
        [MessagePackMember(81)] public bool HistoryTraitNavalTraditions;
        [MessagePackMember(82)] public bool HistoryTraitPackMentality;
        [MessagePackMember(83)] public bool HistoryTraitPrototypeFlagship;
        [MessagePackMember(84)] public bool HistoryTraitSpiritual;
        [MessagePackMember(85)] public bool HistoryTraitPollutedHomeWorld;
        [MessagePackMember(86)] public bool HistoryTraitIndustrializedHomeWorld;
        [MessagePackMember(87)] public bool HistoryTraitDuplicitous;
        [MessagePackMember(88)] public bool HistoryTraitHonest;
        [MessagePackMember(89)] public bool HistoryTraitHugeHomeWorld;
        [MessagePackMember(90)] public bool HistoryTraitSmallHomeWorld;

        // Pointless variables
        [MessagePackMember(91)] public int Aquatic;
        [MessagePackMember(92)] public int Burrowers;

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