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


        public void CheckGrantArtifact(Empire triggerer, Outcome triggeredOutcome)
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
                


                //apply artifact bonus.
                float bonus = 0;
                if (TrySetArtifactEffect(ref bonus, ref triggeredOutcome.GetArtifact().FertilityMod,
                            triggerer.data.Traits))
                {
                    triggerer.data.EmpireFertilityBonus += triggeredOutcome.GetArtifact().FertilityMod;
                    foreach (Planet planet in triggerer.GetPlanets())
                    {
                        planet.Fertility += bonus;                        
                    }
                }
                TrySetArtifactEffect(ref triggerer.data.Traits.DiplomacyMod,
                    ref triggeredOutcome.GetArtifact().DiplomacyMod,
                    triggerer.data.Traits);

                TrySetArtifactEffect(ref triggerer.data.Traits.GroundCombatModifier,
                    ref triggeredOutcome.GetArtifact().GroundCombatMod,
                    triggerer.data.Traits);

                if (TrySetArtifactEffect(ref triggerer.data.Traits.ModHpModifier,
                    ref triggeredOutcome.GetArtifact().ModuleHPMod,
                    triggerer.data.Traits))
                    triggerer.RecalculateMaxHP = true;
                //So existing ships will benefit from changes to ModHpModifier -Gretman

                TrySetArtifactEffect(ref triggerer.data.FlatMoneyBonus,
                    ref triggeredOutcome.GetArtifact().PlusFlatMoney,
                    triggerer.data.Traits);

                TrySetArtifactEffect(ref triggerer.data.Traits.ProductionMod,
                    ref triggeredOutcome.GetArtifact().ProductionMod,
                    triggerer.data.Traits);

                TrySetArtifactEffect(ref triggerer.data.Traits.ReproductionMod,
                    ref triggeredOutcome.GetArtifact().ReproductionMod,
                    triggerer.data.Traits);

                TrySetArtifactEffect(ref triggerer.data.Traits.ResearchMod,
                    ref triggeredOutcome.GetArtifact().ResearchMod,
                    triggerer.data.Traits);

                TrySetArtifactEffect(ref triggerer.data.SensorModifier,
                    ref triggeredOutcome.GetArtifact().SensorMod,
                    triggerer.data.Traits);

                TrySetArtifactEffect(ref triggerer.data.ShieldPenBonusChance,
                    ref triggeredOutcome.GetArtifact().ShieldPenBonus,
                    triggerer.data.Traits);


            }
        }
    }
}