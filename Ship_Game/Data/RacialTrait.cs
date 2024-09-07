using Ship_Game.Ships;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Data.Serialization;
using Ship_Game.Data;
using System;
using SDUtils;
using Ship_Game.Utils;

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

        // Race trait info, set in Races/MyRace.xml
        [StarData] public string Name;
        [StarData] public string VideoPath = "";
        [StarData] public string ShipType = "";
        [StarData] public string Singular;
        [StarData] public string Plural;

        [StarData] public string Adj1;
        [StarData] public string Adj2;
        [StarData] public string HomeSystemName;
        [StarData] public string HomeworldName;
        [StarData] public int FlagIndex;
        [StarData] public float R;
        [StarData] public float G;
        [StarData] public float B;

        // RacialTraits.xml
        [StarData] public int TraitIndex;
        [StarData] public string TraitName;
        [XmlIgnore] public LocalizedText LocalizedName => new(TraitIndex);
        [StarData] public string Category;
        [StarData] public int Cost;
        [StarData] public int Description;
        [StarData] public int Excludes;

        // individual trait effects defined in RacialTraits.xml
        [StarData] public float SpyMultiplier;
        [StarData] public float ConsumptionModifier;
        [StarData] public float ReproductionMod;
        [StarData] public float PopGrowthMax;
        [StarData] public float PopGrowthMin;
        [StarData] public float DiplomacyMod; // Initial Trust only
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
        [StarData] public float TargetingModifier;
        [StarData] public float ResearchMod;
        [StarData] public float Mercantile;
        [StarData] public float ProductionMod;
        [StarData] public float MaintMod; // ex: -0.25
        [StarData] public float ShipMaintMultiplier = 1; // ex: 0.75
        [StarData] public float InBordersSpeedBonus = 0.5f;
        [StarData] public float TaxMod; // bonus tax modifier
        [StarData] public float ShipCostMod;
        [StarData] public float ModHpModifier;
        [StarData] public float PassengerBonus;
        [StarData] public float GroundCombatModifier;
        [StarData] public float RepairMod;
        [StarData] public int Cybernetic;
        [StarData] public float SpyModifier;
        [StarData] public int Pack;
        [StarData] public int Aquatic;
        [StarData] public float ExploreDistanceMultiplier = 1;
        [StarData] public float CreditsPerKilledSlot;
        [StarData] public float PenaltyPerKilledSlot;
        [StarData] public float ConstructionRateMultiplier = 1; // for constructors
        [StarData] public float BuilderShipConstructionMultiplier = 1; // for builder ships
        [StarData] public int ExtraStartingScouts;
        [StarData] public float ResearchBenefitFromAlliance;

        // FB - Environment
        [StarData] public PlanetCategory PreferredEnv = PlanetCategory.Terran;
        [StarData] public float EnvTerran = 1;
        [StarData] public float EnvOceanic = 1;
        [StarData] public float EnvSteppe = 1;
        [StarData] public float EnvTundra = 1;
        [StarData] public float EnvSwamp = 1;
        [StarData] public float EnvDesert = 1;
        [StarData] public float EnvIce = 1;
        [StarData] public float EnvBarren = 1;
        [StarData] public float EnvVolcanic = 1;

        [StarData] public float ResearchTaxMultiplier = 1; // set by difficulty
        [StarData] public bool TaxGoods; // unlocked by tech
        [StarData] public bool SmartMissiles; // unlocked by tech
        [StarData] public int TerraformingLevel;  // unlocked by tech, FB from 0 to 3
        [StarData] public int DysonSwarmMax = 1;  // unlocked by tech
        [StarData] public float EnemyPlanetInhibitionPercentCounter;  // unlocked by tech, FB - from 0 to 0.75
        [StarData] public bool Assimilators;
        [StarData] public float PassengerModifier = 1f;

        [StarData] public Array<TraitSet> TraitSets = new();
        public Array<string> PlayerTraitOptions => TraitSets.Count > 0 ? TraitSets[0].TraitOptions : new Array<string>();

        // MISC wrappers

        public float HomeworldSizeMultiplier => 1f + HomeworldSizeMod;
        public float MaintMultiplier => 1f + MaintMod; // Ex: 1.25


        public RacialTrait GetClone()
        {
            return (RacialTrait)MemberwiseClone();
        }

        [XmlIgnore] public bool IsCybernetic => Cybernetic > 0;
        [XmlIgnore] public bool IsOrganic    => Cybernetic < 1;

        [XmlIgnore] public SubTexture Icon => 
            ResourceManager.TextureOrDefault("Portraits/"+VideoPath, "Portraits/Unknown");

        [XmlIgnore] public SubTexture FlagIcon => ResourceManager.Flag(FlagIndex);

        [XmlIgnore] public Color Color
        {
            get => new Color((byte)R, (byte)G, (byte)B, 255);
            set
            {
                R = value.R;
                G = value.G;
                B = value.B;
            }
        }

        public void UnlockAtGameStart(TechEntry techEntry, Empire empire)
        {
            if (techEntry.Discovered && techEntry.IsUnlockedAtGameStart(empire))
                techEntry.ForceFullyResearched();
        }

        public float ResearchMultiplierForTech(TechEntry tech, Empire empire)
        {
            if (IsCybernetic && tech.UnlocksFoodBuilding)
                return 0.5f;
            return 1;
        }

        //Added by McShooterz: set old values from new bools
        public void LoadTraitConstraints(bool isPlayer, RandomBase random, string raceName, bool disableAlternateTraits, out string selectedTraits)
        {
            selectedTraits = string.Empty;
            if (TraitSets.Count == 0)
                return;

            Array<string> traitOptions = isPlayer || disableAlternateTraits 
                ? PlayerTraitOptions 
                : random.Item(TraitSets).TraitOptions;

            foreach (string trait in traitOptions)
                selectedTraits += $"{trait}, ";

            if (Log.HasDebugger)
                Log.Info($"Selected traits for {raceName}: {selectedTraits}");


            var traits = ResourceManager.RaceTraits;
            if (traits.TraitList == null)
                return;

            foreach (RacialTraitOption trait in traits.TraitList)
            {
                if (!traitOptions.Contains(trait.TraitName))
                    continue;

                DiplomacyMod += trait.DiplomacyMod;
                TargetingModifier += trait.TargetingModifier;
                ConsumptionModifier += trait.ConsumptionModifier;
                PopGrowthMin += trait.PopGrowthMin;
                PopGrowthMax += trait.PopGrowthMax;
                ResearchMod += trait.ResearchMod;
                DodgeMod += trait.DodgeMod;
                GroundCombatModifier += trait.GroundCombatModifier;
                MaintMod += trait.MaintMod;
                ProductionMod += trait.ProductionMod;
                TaxMod += trait.TaxMod;
                ModHpModifier += trait.ModHpModifier;
                Mercantile += trait.Mercantile;
                SpyMultiplier += trait.SpyMultiplier;
                HomeworldSizeMod += trait.HomeworldSizeMod;
                BonusExplored += trait.BonusExplored;
                Cybernetic += trait.Cybernetic;
                RepairMod += trait.RepairMod;
                PassengerBonus += trait.PassengerBonus;
                ShipCostMod += trait.ShipCostMod;
                Spiritual += trait.Spiritual;
                HomeworldFertMod += trait.HomeworldFertMod;
                HomeworldRichMod += trait.HomeworldRichMod;
                Militaristic += trait.Militaristic;
                Pack += trait.Pack;
                Aquatic += trait.Aquatic;
                CreditsPerKilledSlot += trait.CreditsPerKilledSlot;
                PenaltyPerKilledSlot += trait.PenaltyPerKilledSlot;
                ExtraStartingScouts += trait.ExtraStartingScouts;
                ResearchBenefitFromAlliance += trait.ResearchBenefitFromAlliance;

                ExploreDistanceMultiplier *= trait.ExploreDistanceMultiplier;
                ConstructionRateMultiplier *= trait.ConstructionRateMultiplier;
                BuilderShipConstructionMultiplier *= trait.BuilderShipConstructionMultiplier;

                Prototype += trait.Prototype;
                EnvTerran *= trait.EnvTerranMultiplier;
                EnvOceanic *= trait.EnvOceanicMultiplier;
                EnvSteppe *= trait.EnvSteppeMultiplier;
                EnvTundra *= trait.EnvTundraMultiplier;
                EnvSwamp *= trait.EnvSwampMultiplier;
                EnvDesert *= trait.EnvDesertMultiplier;
                EnvIce *= trait.EnvIceMultiplier;
                EnvBarren *= trait.EnvBarrenMultiplier;
                EnvVolcanic *= trait.EnvVolcanicMultiplier;

                if (trait.PreferredEnv != PlanetCategory.Terran)
                    PreferredEnv = trait.PreferredEnv;
            }
        }

        public void ApplyTraitToShip(Ship ship)
        {
            if (Pack > 0) ship.UpdatePackDamageModifier();
        }

    }

    [StarDataType]
    public class TraitSet
    {
        [StarData] public Array<string> TraitOptions;
    }

}