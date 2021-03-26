using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class Artifact
    {
        [Serialize(0)] public bool Discovered;
        [Serialize(1)] public string Name;
        [Serialize(2)] public string Description;
        [Serialize(3)] public int NameIndex;
        [Serialize(4)] public int DescriptionIndex;
        [Serialize(5)] public float ReproductionMod;
        [Serialize(6)] public float ShieldPenBonus;
        [Serialize(7)] public float FertilityMod;
        [Serialize(8)] public float ProductionMod;
        [Serialize(9)] public float GroundCombatMod;
        [Serialize(10)] public float ResearchMod;
        [Serialize(11)] public float PlusFlatMoney;
        [Serialize(12)] public float DiplomacyMod; // OnGoing effect which is tied to OngoingDiplomacyMod in empire data.
        [Serialize(13)] public float SensorMod;
        [Serialize(14)] public float ModuleHPMod;


        bool TrySetArtifactEffect(ref float outModifier, float inModifier, RacialTrait traits,
                                  string text, EventPopup popup, bool percent = true)
        {
            if (inModifier <= 0f)
                return false;

            outModifier += inModifier + inModifier * traits.Spiritual;
            popup?.AddArtifactEffect(new EventPopup.ArtifactEffect(text, outModifier, percent));
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
                //apply artifact bonus.
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

                if (TrySetArtifactEffect(ref triggerer.data.Traits.ModHpModifier,
                    ModuleHPMod,
                    triggerer.data.Traits, "Empire-wide Ship Module Hitpoint Bonus: ", popup))
                    EmpireShipBonuses.RefreshBonuses(triggerer); // RedFox: This will refresh all empire module stats

                TrySetArtifactEffect(ref triggerer.data.FlatMoneyBonus,
                    PlusFlatMoney,
                    triggerer.data.Traits, "Credits per Turn Bonus: ", popup);

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
                EmpireShipBonuses.RefreshBonuses(triggerer);
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