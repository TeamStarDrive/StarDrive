using Ship_Game.Ships;
using System.Xml.Serialization;
using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game
{
    [StarDataType]
    public sealed class Artifact
    {
        [StarData] public bool Discovered;
        [StarData] public string Name;
        [StarData] public string Description;
        [StarData] public int NameIndex;
        [StarData] public int DescriptionIndex;
        [StarData] public float ReproductionMod;
        [StarData] public float ShieldPenBonus;
        [StarData] public float FertilityMod;
        [StarData] public float ProductionMod;
        [StarData] public float GroundCombatMod;
        [StarData] public float ResearchMod;
        [StarData] public float PlusFlatMoney;
        [StarData] public float DiplomacyMod; // added to the holder's racial diplomacy trait
        [StarData] public float SensorMod;
        [StarData] public float ModuleHPMod;

        [XmlIgnore] public LocalizedText NameText => new(NameIndex);
        [XmlIgnore] public LocalizedText DescriptionText => new(DescriptionIndex);

        bool TrySetArtifactEffect(ref float outModifier, float inModifier, RacialTrait traits,
                                  GameText text, EventPopup popup, bool percent = true)
        {
            if (inModifier <= 0f)
                return false;

            outModifier += inModifier + inModifier * traits.Spiritual;
            popup?.AddArtifactEffect(new(Localizer.Token(text), inModifier, percent));
            return true;
        }

        public void GrantBonuses(Empire triggerer, EventPopup popup)
        {
            // apply artifact bonus.
            float bonus = 0;
            if (TrySetArtifactEffect(ref bonus, FertilityMod,
                triggerer.data.Traits, GameText.ArtifactFertilityBonus, popup))
            {
                triggerer.data.EmpireFertilityBonus += bonus;
                foreach (Planet planet in triggerer.GetPlanets())
                {
                    planet.AddMaxBaseFertility(bonus);
                }
            }
            TrySetArtifactEffect(ref triggerer.data.Traits.DiplomacyMod,
                DiplomacyMod,
                triggerer.data.Traits, GameText.ArtifactDiplomacyBonus, popup);

            TrySetArtifactEffect(ref triggerer.data.Traits.GroundCombatModifier,
                GroundCombatMod,
                triggerer.data.Traits, GameText.ArtifactGroundCombatBonus, popup);

            TrySetArtifactEffect(ref triggerer.data.Traits.ModHpModifier,
                ModuleHPMod,
                triggerer.data.Traits, GameText.ArtifactModuleHitpointBonus, popup);

            TrySetArtifactEffect(ref triggerer.data.FlatMoneyBonus,
                PlusFlatMoney,
                triggerer.data.Traits, GameText.ArtifactCreditsPerTurnBonus, popup, percent: false);

            TrySetArtifactEffect(ref triggerer.data.Traits.ProductionMod,
                ProductionMod,
                triggerer.data.Traits, GameText.ArtifactProductionBonus, popup);

            TrySetArtifactEffect(ref triggerer.data.Traits.ReproductionMod,
                ReproductionMod,
                triggerer.data.Traits, GameText.ArtifactPopulationGrowthBonus, popup);

            TrySetArtifactEffect(ref triggerer.data.Traits.ResearchMod,
                ResearchMod,
                triggerer.data.Traits, GameText.ArtifactResearchBonus, popup);

            TrySetArtifactEffect(ref triggerer.data.SensorModifier,
                SensorMod,
                triggerer.data.Traits, GameText.ArtifactSensorRangeBonus, popup);

            TrySetArtifactEffect(ref triggerer.data.ShieldPenBonusChance,
                ShieldPenBonus,
                triggerer.data.Traits, GameText.ArtifactShieldPenetrationBonus, popup);

            // refresh all bonuses so modules would know their health etc. increased
            EmpireHullBonuses.RefreshBonuses(triggerer);
        }
        public float GetGroundCombatBonus(EmpireData data) => ArtifactBonusForEmpire(GroundCombatMod, data.Traits.Spiritual);
        public float GetDiplomacyBonus(EmpireData data)    => ArtifactBonusForEmpire(DiplomacyMod   , data.Traits.Spiritual);
        public float GetFertilityBonus(EmpireData data)    => ArtifactBonusForEmpire(FertilityMod   , data.Traits.Spiritual);
        public float GetModuleHpMod(EmpireData data)       => ArtifactBonusForEmpire(ModuleHPMod    , data.Traits.Spiritual);
        public float GetFlatMoneyBonus(EmpireData data)    => ArtifactBonusForEmpire(PlusFlatMoney  , data.Traits.Spiritual);
        public float GetProductionBonus(EmpireData data)   => ArtifactBonusForEmpire(ProductionMod  , data.Traits.Spiritual);
        public float GetResearchMod(EmpireData data)       => ArtifactBonusForEmpire(ResearchMod    , data.Traits.Spiritual);
        public float GetSensorMod(EmpireData data)         => ArtifactBonusForEmpire(SensorMod      , data.Traits.Spiritual);
        public float GetShieldPenMod(EmpireData data)      => ArtifactBonusForEmpire(ShieldPenBonus , data.Traits.Spiritual);
        public float GetReproductionMod(EmpireData data)   => ArtifactBonusForEmpire(ReproductionMod, data.Traits.Spiritual);

        private float ArtifactBonusForEmpire(float artifactBonus, float empireBonus)
        {
            if (artifactBonus <= 0) return 0;
            return artifactBonus + artifactBonus * empireBonus;
        }
    }
}