using System;
using System.Collections.Generic;
using static Ship_Game.EmpireData;

namespace Ship_Game
{
	public sealed class Artifact
	{
		public bool Discovered;

		public string Name;

		public string Description;

		public int NameIndex;

		public int DescriptionIndex;

		public float ReproductionMod;

		public float ShieldPenBonus;

		public float FertilityMod;

		public float ProductionMod;

		public float GroundCombatMod;

		public float ResearchMod;

		public float PlusFlatMoney;

		public float DiplomacyMod;

		public float SensorMod;

		public float ModuleHPMod;

		public Artifact()
		{
		}

        private static bool TrySetArtifactEffect(ref float outModifier, ref float inModifier, RacialTrait traits)
        {
            if (inModifier <= 0f)
                return false;
            outModifier += inModifier + inModifier * traits.Spiritual;
            return true;
        }

        public static void CheckGrantArtifact(Empire triggerer, Outcome triggeredOutcome)
        {
            
            List<Artifact> potentials = new List<Artifact>();
            foreach (KeyValuePair<string, Artifact> artifact in ResourceManager.ArtifactsDict)
            {
                if (artifact.Value.Discovered)
                {
                    continue;
                }
                potentials.Add(artifact.Value);
            }
            if (potentials.Count <= 0)
            {
                triggeredOutcome.MoneyGranted = 500;
            }
            else
            {
                int ranart = (int)RandomMath.RandomBetween(0f, potentials.Count + 0.8f);
                if (ranart > potentials.Count - 1)
                {
                    ranart = potentials.Count - 1;
                }
                triggerer.data.OwnedArtifacts.Add(potentials[ranart]);
                ResourceManager.ArtifactsDict[potentials[ranart].Name].Discovered = true;
                triggeredOutcome.SetArtifact(potentials[ranart]);
                if (triggeredOutcome.GetArtifact().DiplomacyMod > 0f)
                {
                    triggerer.data.Traits.DiplomacyMod += (triggeredOutcome.GetArtifact().DiplomacyMod +
                                                           triggeredOutcome.GetArtifact().DiplomacyMod *
                                                           triggerer.data.Traits.Spiritual);
                }
                if (triggeredOutcome.GetArtifact().FertilityMod > 0f)
                {
                    if(TrySetArtifactEffect())
                    triggerer.data.EmpireFertilityBonus += triggeredOutcome.GetArtifact().FertilityMod;
                    foreach (Planet planet in triggerer.GetPlanets())
                    {
                        Planet fertility = planet;
                        fertility.Fertility = fertility.Fertility +
                                              (triggeredOutcome.GetArtifact().FertilityMod +
                                               triggeredOutcome.GetArtifact().FertilityMod *
                                               triggerer.data.Traits.Spiritual);
                    }
                }
                if (triggeredOutcome.GetArtifact().GroundCombatMod > 0f)
                {
                    triggerer.data.Traits.GroundCombatModifier += (triggeredOutcome.GetArtifact().GroundCombatMod +
                                                                   triggeredOutcome.GetArtifact().GroundCombatMod *
                                                                   triggerer.data.Traits.Spiritual);
                }
                if (triggeredOutcome.GetArtifact().ModuleHPMod > 0f)
                {
                    triggerer.data.Traits.ModHpModifier += (triggeredOutcome.GetArtifact().ModuleHPMod +
                                                            triggeredOutcome.GetArtifact().ModuleHPMod *
                                                            triggerer.data.Traits.Spiritual);
                    triggerer.RecalculateMaxHP = true;
                    //So existing ships will benefit from changes to ModHpModifier -Gretman
                }
                if (triggeredOutcome.GetArtifact().PlusFlatMoney > 0f)
                {
                    triggerer.data.FlatMoneyBonus += (triggeredOutcome.GetArtifact().PlusFlatMoney +
                                                      triggeredOutcome.GetArtifact().PlusFlatMoney *
                                                      triggerer.data.Traits.Spiritual);
                }
                if (triggeredOutcome.GetArtifact().ProductionMod > 0f)
                {
                    triggerer.data.Traits.ProductionMod += (triggeredOutcome.GetArtifact().ProductionMod +
                                                            triggeredOutcome.GetArtifact().ProductionMod *
                                                            triggerer.data.Traits.Spiritual);
                }
                if (triggeredOutcome.GetArtifact().ReproductionMod > 0f)
                {
                    triggerer.data.Traits.ReproductionMod += (triggeredOutcome.GetArtifact().ReproductionMod +
                                                              triggeredOutcome.GetArtifact().ReproductionMod *
                                                              triggerer.data.Traits.Spiritual);
                }
                if (triggeredOutcome.GetArtifact().ResearchMod > 0f)
                {
                    triggerer.data.Traits.ResearchMod += (triggeredOutcome.GetArtifact().ResearchMod +
                                                          triggeredOutcome.GetArtifact().ResearchMod *
                                                          triggerer.data.Traits.Spiritual);
                }
                if (triggeredOutcome.GetArtifact().SensorMod > 0f)
                {
                    triggerer.data.SensorModifier += (triggeredOutcome.GetArtifact().SensorMod +
                                                      triggeredOutcome.GetArtifact().SensorMod *
                                                      triggerer.data.Traits.Spiritual);
                }
                if (triggeredOutcome.GetArtifact().ShieldPenBonus > 0f)
                {
                    triggerer.data.ShieldPenBonusChance += (triggeredOutcome.GetArtifact().ShieldPenBonus +
                                                            triggeredOutcome.GetArtifact().ShieldPenBonus *
                                                            triggerer.data.Traits.Spiritual);
                }
            }
        }
    }
}