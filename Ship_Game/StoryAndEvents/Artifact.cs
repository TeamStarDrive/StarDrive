using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Ship_Game.Ships;
using System.Xml.Serialization;
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
        [StarData] public float DiplomacyMod; // OnGoing effect which is tied to OngoingDiplomacyMod in empire data.
        [StarData] public float SensorMod;
        [StarData] public float ModuleHPMod;

        [XmlIgnore][JsonIgnore] public LocalizedText NameText => new LocalizedText(NameIndex); 

        bool TrySetArtifactEffect(ref float outModifier, float inModifier, RacialTrait traits,
                                  string text, EventPopup popup, bool percent = true)
        {
            if (inModifier <= 0f)
                return false;

            outModifier += inModifier + inModifier * traits.Spiritual;
            popup?.AddArtifactEffect(new EventPopup.ArtifactEffect(text, inModifier, percent));
            return true;
        }

        public void CheckGrantArtifact(Empire triggerer, Outcome triggeredOutcome, EventPopup popup)
        {
            Array<Artifact> potentials = new Array<Artifact>();
            foreach (var kv in ResourceManager.ArtifactsDict)
            {
                if (kv.Value.Discovered)
                {
                    continue;
                }
                potentials.Add(kv.Value);
            }
            if (potentials.Count <= 0)
            {
                triggeredOutcome.MoneyGranted = 500;
            }
            else
            {
                // apply artifact bonus.
                // FB - todo, move text to GameText.cs for translation as well.
                float bonus = 0;
                if (TrySetArtifactEffect(ref bonus, FertilityMod,
                    triggerer.data.Traits, "Fertility Bonus to all Owned Colonies: ",popup))
                {
                    triggerer.data.EmpireFertilityBonus += triggeredOutcome.GetArtifact().FertilityMod;
                    foreach (Planet planet in triggerer.GetPlanets())
                    {
                        planet.AddMaxBaseFertility(bonus);
                    }
                }
                TrySetArtifactEffect(ref triggerer.data.Traits.DiplomacyMod,
                    DiplomacyMod,
                    triggerer.data.Traits, "Diplomacy Bonus: ", popup);

                TrySetArtifactEffect(ref triggerer.data.Traits.GroundCombatModifier,
                    GroundCombatMod,
                    triggerer.data.Traits, "Empire-wide Ground Combat Bonus: ", popup);

                TrySetArtifactEffect(ref triggerer.data.Traits.ModHpModifier,
                    ModuleHPMod,
                    triggerer.data.Traits, "Empire-wide Ship Module Hitpoint Bonus: ", popup);

                TrySetArtifactEffect(ref triggerer.data.FlatMoneyBonus,
                    PlusFlatMoney,
                    triggerer.data.Traits, "Credits per Turn Bonus: ", popup, percent: false);

                TrySetArtifactEffect(ref triggerer.data.Traits.ProductionMod,
                    ProductionMod,
                    triggerer.data.Traits, "Empire-wide Production Bonus: ", popup);

                TrySetArtifactEffect(ref triggerer.data.Traits.ReproductionMod,
                    ReproductionMod,
                    triggerer.data.Traits, "Empire-wide Popoulation Growth Bonus: ", popup);

                TrySetArtifactEffect(ref triggerer.data.Traits.ResearchMod,
                    ResearchMod,
                    triggerer.data.Traits, "Empire-wide Research Bonus: ", popup);

                TrySetArtifactEffect(ref triggerer.data.SensorModifier,
                    SensorMod,
                    triggerer.data.Traits, "Empire-wide Sensor Range Bonus: ", popup);

                TrySetArtifactEffect(ref triggerer.data.ShieldPenBonusChance,
                    ShieldPenBonus,
                    triggerer.data.Traits, "Empire-wide Bonus Shield Penetration Chance: ", popup);

                // refresh all bonuses so modules would know their health etc. increased
                EmpireHullBonuses.RefreshBonuses(triggerer);
            }
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