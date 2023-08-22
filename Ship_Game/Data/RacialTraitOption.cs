using Ship_Game.Data.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Ship_Game.Data
{
    public sealed class RacialTraitOption
    {
        // RacialTraits.xml
        public int TraitIndex;
        public string TraitName;
        public string Category;
        public int Cost;
        public int Description;
        public int Excludes;

        [XmlIgnore] public LocalizedText LocalizedName => new(TraitIndex);

        // individual trait effects defined in RacialTraits.xml
        public float SpyMultiplier;
        public float ConsumptionModifier;
        public float ReproductionMod;
        public float PopGrowthMax;
        public float PopGrowthMin;
        public float DiplomacyMod; // Initial Trust only
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
        public float ProductionMod;
        public float MaintMod; // ex: -0.25
        public float ShipMaintMultiplier; // ex: 0.75
        public float InBordersSpeedBonus;
        public float TaxMod; // bonus tax modifier
        public float ShipCostMod;
        public float ModHpModifier;
        public float PassengerBonus;
        public float GroundCombatModifier;
        public float RepairMod;
        public int Cybernetic;
        public float SpyModifier;
        public int Pack;
    }
}
