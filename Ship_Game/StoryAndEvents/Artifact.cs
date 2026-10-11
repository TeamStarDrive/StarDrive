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

        static void AddEffect(EventPopup popup, float modifier, GameText text, bool percent = true)
        {
            if (modifier > 0f)
                popup.AddArtifactEffect(new(Localizer.Token(text), modifier, percent));
        }

        public void ShowEffects(EventPopup popup)
        {
            if (popup == null)
                return;

            AddEffect(popup, FertilityMod, GameText.ArtifactFertilityBonus);
            AddEffect(popup, DiplomacyMod, GameText.ArtifactDiplomacyBonus);
            AddEffect(popup, GroundCombatMod, GameText.ArtifactGroundCombatBonus);
            AddEffect(popup, ModuleHPMod, GameText.ArtifactModuleHitpointBonus);
            AddEffect(popup, PlusFlatMoney, GameText.ArtifactCreditsPerTurnBonus, percent: false);
            AddEffect(popup, ProductionMod, GameText.ArtifactProductionBonus);
            AddEffect(popup, ReproductionMod, GameText.ArtifactPopulationGrowthBonus);
            AddEffect(popup, ResearchMod, GameText.ArtifactResearchBonus);
            AddEffect(popup, SensorMod, GameText.ArtifactSensorRangeBonus);
            AddEffect(popup, ShieldPenBonus, GameText.ArtifactShieldPenetrationBonus);
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