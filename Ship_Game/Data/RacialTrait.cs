using Newtonsoft.Json;
using Ship_Game.Ships;
using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Data.Serialization;

namespace Ship_Game
{
    [StarDataType]
    public sealed class RacialTrait
    {
        public enum NameOfTrait
        {
            None,
            Cybernetic,
            Militaristic,
            NonCybernetic
        }
        
        [StarData] public string Name;
        [StarData] public int TraitName;
        [XmlIgnore][JsonIgnore] public LocalizedText LocalizedName => new LocalizedText(TraitName);
        [StarData] public string VideoPath = "";
        [StarData] public string ShipType = "";
        [StarData] public string Singular;
        [StarData] public string Plural;
        [StarData] public bool Pack;
        [StarData] public float SpyMultiplier;
        [StarData] public string Adj1;
        [StarData] public string Adj2;
        [StarData] public string HomeSystemName;
        [StarData] public string HomeworldName;
        [StarData] public int FlagIndex;
        [StarData] public float R;
        [StarData] public float G;
        [StarData] public float B;
        [StarData] public int Excludes;
        [StarData] public int Description;
        [StarData] public string Category;
        [StarData] public int Cost;
        [StarData] public float ConsumptionModifier;
        [StarData] public float ReproductionMod;
        [StarData] public float PopGrowthMax;
        [StarData] public float PopGrowthMin;
        [StarData] public float DiplomacyMod; // Initial Trust only
        [StarData] public float GenericMaxPopMod;
        [StarData] public int Blind;
        [StarData] public int BonusExplored;
        [StarData] public int Militaristic;
        [StarData] public float HomeworldSizeMod;
        [StarData] public int Prewarp;
        [StarData] public int Prototype;
        [StarData] public float Spiritual;
        [StarData] public float HomeworldFertMod;
        [StarData] public float HomeworldRichMod;
        [StarData] public float DodgeMod;
        [StarData] public float EnergyDamageMod;
        [StarData] public float ResearchMod;
        [StarData] public float Mercantile;
        [StarData] public int Miners;
        [StarData] public float ProductionMod;
        [StarData] public float MaintMod; // ex: -0.25
        [StarData] public float InBordersSpeedBonus = 0.5f;
        [StarData] public float TaxMod; // bonus tax modifier
        [StarData] public float ShipCostMod;
        [StarData] public float ModHpModifier;
        [StarData] public int SmallSize;
        [StarData] public int HugeSize;
        [StarData] public float PassengerModifier = 1f;
        [StarData] public float PassengerBonus;
        [StarData] public bool Assimilators;
        [StarData] public float GroundCombatModifier;
        [StarData] public float RepairMod;
        [StarData] public int Cybernetic;

        //Trait Booleans
        [StarData] public bool PhysicalTraitAlluring;
        [StarData] public bool PhysicalTraitRepulsive;
        [StarData] public bool PhysicalTraitEagleEyed;
        [StarData] public bool PhysicalTraitBlind;
        [StarData] public bool PhysicalTraitEfficientMetabolism;
        [StarData] public bool PhysicalTraitGluttonous;
        [StarData] public bool PhysicalTraitFertile;
        [StarData] public bool PhysicalTraitLessFertile;
        [StarData] public bool PhysicalTraitSmart;
        [StarData] public bool PhysicalTraitDumb;
        [StarData] public bool PhysicalTraitReflexes;
        [StarData] public bool PhysicalTraitPonderous;
        [StarData] public bool PhysicalTraitSavage;
        [StarData] public bool PhysicalTraitTimid;
        [StarData] public bool SociologicalTraitEfficient;
        [StarData] public bool SociologicalTraitWasteful;
        [StarData] public bool SociologicalTraitIndustrious;
        [StarData] public bool SociologicalTraitLazy;
        [StarData] public bool SociologicalTraitMercantile;
        [StarData] public bool SociologicalTraitMeticulous;
        [StarData] public bool SociologicalTraitCorrupt;
        [StarData] public bool SociologicalTraitSkilledEngineers;
        [StarData] public bool SociologicalTraitHaphazardEngineers;

        [StarData] public bool HistoryTraitAstronomers;
        [StarData] public bool HistoryTraitCybernetic;
        [StarData] public bool HistoryTraitManifestDestiny;
        [StarData] public bool HistoryTraitMilitaristic;
        [StarData] public bool HistoryTraitNavalTraditions;
        [StarData] public bool HistoryTraitPackMentality;
        [StarData] public bool HistoryTraitPrototypeFlagship;
        [StarData] public bool HistoryTraitSpiritual;
        [StarData] public bool HistoryTraitPollutedHomeWorld;
        [StarData] public bool HistoryTraitIndustrializedHomeWorld;
        [StarData] public bool HistoryTraitDuplicitous;
        [StarData] public bool HistoryTraitHonest;
        [StarData] public bool HistoryTraitHugeHomeWorld;
        [StarData] public bool HistoryTraitSmallHomeWorld;

        // Pointless variables
        [StarData] public int Aquatic;
        [StarData] public int Burrowers;
        [StarData] public float SpyModifier;

        [StarData] public float ResearchTaxMultiplier = 1;
        [StarData] public bool TaxGoods;
        [StarData] public bool SmartMissiles;
        [StarData] public int TerraformingLevel;  // FB from 0 to 3
        [StarData] public float EnemyPlanetInhibitionPercentCounter;  // FB - from 0 to 0.75

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
