using Newtonsoft.Json;
using Ship_Game.Ships;
using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public sealed class RacialTrait
    {
        public enum NameOfTrait
        {
            None,
            Cybernetic,
            Militaristic,
            NonCybernetic
        }
        
        [Serialize(0)] public string Name;
        [Serialize(1)] public int TraitName;
        [Serialize(2)] public string VideoPath = "";
        [Serialize(3)] public string ShipType = "";
        [Serialize(4)] public string Singular;
        [Serialize(5)] public string Plural;
        [Serialize(6)] public bool Pack;
        [Serialize(7)] public float SpyMultiplier;
        [Serialize(8)] public string Adj1;
        [Serialize(9)] public string Adj2;
        [Serialize(10)] public string HomeSystemName;
        [Serialize(11)] public string HomeworldName;
        [Serialize(12)] public int FlagIndex;
        [Serialize(13)] public float R;
        [Serialize(14)] public float G;
        [Serialize(15)] public float B;
        [Serialize(16)] public int Excludes;
        [Serialize(17)] public int Description;
        [Serialize(18)] public string Category;
        [Serialize(19)] public int Cost;
        [Serialize(20)] public float ConsumptionModifier;
        [Serialize(21)] public float ReproductionMod;
        [Serialize(22)] public float PopGrowthMax;
        [Serialize(23)] public float PopGrowthMin;
        [Serialize(24)] public float DiplomacyMod;
        [Serialize(25)] public float GenericMaxPopMod;
        [Serialize(26)] public int Blind;
        [Serialize(27)] public int BonusExplored;
        [Serialize(28)] public int Militaristic;
        [Serialize(29)] public float HomeworldSizeMod;
        [Serialize(30)] public int Prewarp;
        [Serialize(31)] public int Prototype;
        [Serialize(32)] public float Spiritual;
        [Serialize(33)] public float HomeworldFertMod;
        [Serialize(34)] public float HomeworldRichMod;
        [Serialize(35)] public float DodgeMod;
        [Serialize(36)] public float EnergyDamageMod;
        [Serialize(37)] public float ResearchMod;
        [Serialize(38)] public float Mercantile;
        [Serialize(39)] public int Miners;
        [Serialize(40)] public float ProductionMod;
        [Serialize(41)] public float MaintMod; // ex: -0.25
        [Serialize(42)] public float InBordersSpeedBonus = 0.5f;
        [Serialize(43)] public float TaxMod; // bonus tax modifier
        [Serialize(44)] public float ShipCostMod;
        [Serialize(45)] public float ModHpModifier;
        [Serialize(46)] public int SmallSize;
        [Serialize(47)] public int HugeSize;
        [Serialize(48)] public float PassengerModifier = 1f;
        [Serialize(49)] public float PassengerBonus;
        [Serialize(50)] public bool Assimilators;
        [Serialize(51)] public float GroundCombatModifier;
        [Serialize(52)] public float RepairMod;
        [Serialize(53)] public int Cybernetic;

        //Trait Booleans
        [Serialize(54)] public bool PhysicalTraitAlluring;
        [Serialize(55)] public bool PhysicalTraitRepulsive;
        [Serialize(56)] public bool PhysicalTraitEagleEyed;
        [Serialize(57)] public bool PhysicalTraitBlind;
        [Serialize(58)] public bool PhysicalTraitEfficientMetabolism;
        [Serialize(59)] public bool PhysicalTraitGluttonous;
        [Serialize(60)] public bool PhysicalTraitFertile;
        [Serialize(61)] public bool PhysicalTraitLessFertile;
        [Serialize(62)] public bool PhysicalTraitSmart;
        [Serialize(63)] public bool PhysicalTraitDumb;
        [Serialize(64)] public bool PhysicalTraitReflexes;
        [Serialize(65)] public bool PhysicalTraitPonderous;
        [Serialize(66)] public bool PhysicalTraitSavage;
        [Serialize(67)] public bool PhysicalTraitTimid;
        [Serialize(68)] public bool SociologicalTraitEfficient;
        [Serialize(69)] public bool SociologicalTraitWasteful;
        [Serialize(70)] public bool SociologicalTraitIndustrious;
        [Serialize(71)] public bool SociologicalTraitLazy;
        [Serialize(72)] public bool SociologicalTraitMercantile;
        [Serialize(73)] public bool SociologicalTraitMeticulous;
        [Serialize(74)] public bool SociologicalTraitCorrupt;
        [Serialize(75)] public bool SociologicalTraitSkilledEngineers;
        [Serialize(76)] public bool SociologicalTraitHaphazardEngineers;

        [Serialize(77)] public bool HistoryTraitAstronomers;
        [Serialize(78)] public bool HistoryTraitCybernetic;
        [Serialize(79)] public bool HistoryTraitManifestDestiny;
        [Serialize(80)] public bool HistoryTraitMilitaristic;
        [Serialize(81)] public bool HistoryTraitNavalTraditions;
        [Serialize(82)] public bool HistoryTraitPackMentality;
        [Serialize(83)] public bool HistoryTraitPrototypeFlagship;
        [Serialize(84)] public bool HistoryTraitSpiritual;
        [Serialize(85)] public bool HistoryTraitPollutedHomeWorld;
        [Serialize(86)] public bool HistoryTraitIndustrializedHomeWorld;
        [Serialize(87)] public bool HistoryTraitDuplicitous;
        [Serialize(88)] public bool HistoryTraitHonest;
        [Serialize(89)] public bool HistoryTraitHugeHomeWorld;
        [Serialize(90)] public bool HistoryTraitSmallHomeWorld;

        // Pointless variables
        [Serialize(91)] public int Aquatic;
        [Serialize(92)] public int Burrowers;
        [Serialize(93)] public float SpyModifier;

        [Serialize(94)] public float ResearchTaxMultiplier = 1;
        [Serialize(95)] public int PreferredEnvDescription;
        [Serialize(96)] public bool TaxGoods;
        [Serialize(97)] public bool SmartMissiles;

        public float HomeworldSizeMultiplier => 1f + HomeworldSizeMod;
        public float MaintMultiplier => 1f + MaintMod; // Ex: 1.25

        public RacialTrait GetClone()
        {
            return (RacialTrait)MemberwiseClone();
        }

        [XmlIgnore][JsonIgnore] public bool IsCybernetic => Cybernetic > 0;
        [XmlIgnore][JsonIgnore] public bool IsOrganic    => Cybernetic < 1;

        [XmlIgnore][JsonIgnore] public SubTexture Icon => 
            ResourceManager.TextureOrDefault("Portraits/"+VideoPath, "Portraits/Unknown");

        [XmlIgnore][JsonIgnore] public SubTexture FlagIcon => ResourceManager.Flag(FlagIndex);

        [XmlIgnore][JsonIgnore] public Color Color
        {
            get => new Color((byte)R, (byte)G, (byte)B, 255);
            set
            {
                R = value.R;
                G = value.G;
                B = value.B;
            }
        }

        public void TechUnlocks(TechEntry techEntry, Empire empire)
        {
            if (!techEntry.Discovered) return;

            if (techEntry.IsUnlockedAtGameStart(empire))
                techEntry.ForceFullyResearched();
        }

        public float ResearchMultiplierForTech(TechEntry tech, Empire empire)
        {
            if (IsCybernetic && tech.UnlocksFoodBuilding)
                return 0.5f;
            return 1;
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
                    Cybernetic = 1;
                if (trait.RepairMod > 0)          
                    RepairMod = trait.RepairMod;
                if (HistoryTraitManifestDestiny && trait.PassengerBonus > 0)
                {
                    PassengerBonus = trait.PassengerBonus;
                }
                if (HistoryTraitManifestDestiny &&  trait.PassengerModifier > 1)
                {
                    PassengerModifier = trait.PassengerModifier;
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
                Militaristic = 1;

            if (HistoryTraitPackMentality)
                Pack = true;

            if (HistoryTraitPrototypeFlagship)
                Prototype = 1;
        }

        public void ApplyTraitToShip(Ship ship)
        {
            if (Pack) ship.UpdatePackDamageModifier();
        }
    }
}
