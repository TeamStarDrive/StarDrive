using Ship_Game.Ships;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Data.Serialization;
using Ship_Game.Data;
using System;
using SDUtils;

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
        [StarData] public float EnergyDamageMod;
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

        // FB - Environment
        [StarData] public PlanetCategory PreferredEnv;
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
        [StarData] public float EnemyPlanetInhibitionPercentCounter;  // unlocked by tech, FB - from 0 to 0.75
        [StarData] public bool Assimilators;
        [StarData] public float PassengerModifier = 1f;

        [StarData] public Array<string> TraitOptions;

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
        public void LoadTraitConstraints()
        {
            var traits = ResourceManager.RaceTraits;
            if (traits.TraitList == null)
                return;

            foreach (RacialTraitOption trait in traits.TraitList)
            {
                DiplomacyMod += trait.DiplomacyMod;
                EnergyDamageMod += trait.EnergyDamageMod;
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
                HomeworldFertMod = trait.HomeworldFertMod;
                HomeworldFertMod += trait.HomeworldFertMod;
                HomeworldRichMod += trait.HomeworldRichMod;
                Militaristic += trait.Militaristic;
                Pack += trait.Pack;
                Aquatic += trait.Aquatic;
                Prototype += trait.Prototype;
                EnvTerran *= trait.EnvTerranMultiplier > 0 ? trait.EnvTerranMultiplier : 1;
                EnvOceanic *= trait.EnvOceanicMultiplier > 0 ? trait.EnvOceanicMultiplier : 1;
                EnvSteppe *= trait.EnvSteppeMultiplier > 0 ? trait.EnvSteppeMultiplier : 1;
                EnvTundra *= trait.EnvTundraMultiplier > 0 ? trait.EnvTundraMultiplier : 1;
                EnvSwamp *= trait.EnvSwampMultiplier > 0 ? trait.EnvSwampMultiplier : 1;
                EnvDesert *= trait.EnvDesertMultiplier > 0 ? trait.EnvDesertMultiplier: 1;
                EnvIce *= trait.EnvIceMultiplier > 0 ? trait.EnvIceMultiplier : 1;
                EnvBarren *= trait.EnvBarrenMultiplier > 0 ? trait.EnvBarrenMultiplier : 1;
                EnvVolcanic *= trait.EnvVolcanicMultiplier > 0 ? trait.EnvVolcanicMultiplier : 1;
                if (trait.PreferredEnv != PlanetCategory.Terran)
                    PreferredEnv = trait.PreferredEnv;
            }
        }

        public void ApplyTraitToShip(Ship ship)
        {
            if (Pack > 0) ship.UpdatePackDamageModifier();
        }
    }
}
