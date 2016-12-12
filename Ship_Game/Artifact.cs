using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using System.Xml.Serialization;
using static Ship_Game.EventPopup;
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

        private bool TrySetArtifactEffect(ref float outModifier, float inModifier, RacialTrait traits, string text, EventPopup popup)
        {
            if (inModifier <= 0f)
                return false;
            outModifier += inModifier + inModifier * traits.Spiritual;
            if (popup != null)
            {
                var drawpackage = new DrawPackage(text, Fonts.Arial12Bold, inModifier, Color.White, "%");
                popup.DrawPaackages[Packagetypes.Artifact].Add(drawpackage);
            }
            return true;            
        }

        public void CheckGrantArtifact(Empire triggerer, Outcome triggeredOutcome, EventPopup popup)
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
                if (TrySetArtifactEffect(ref bonus, FertilityMod,
                    triggerer.data.Traits, "Fertility Bonus to all Owned Colonies: ",popup))
                {
                    triggerer.data.EmpireFertilityBonus += triggeredOutcome.GetArtifact().FertilityMod;
                    foreach (Planet planet in triggerer.GetPlanets())
                    {
                        planet.Fertility += bonus;
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
                    triggerer.RecalculateMaxHP = true;
                //So existing ships will benefit from changes to ModHpModifier -Gretman

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
            }
        }
    }
}