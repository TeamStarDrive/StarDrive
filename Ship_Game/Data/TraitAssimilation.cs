using System;
using System.Reflection;
using SDGraphics;
using SDUtils;
using Ship_Game.Ships;

namespace Ship_Game.Data
{
    // an Assimilator empire that invades a big enough colony takes on the former owner's traits that have no downside
    public static class TraitAssimilation
    {
        sealed class Effect
        {
            public readonly string Field;
            readonly bool LowerIsBetter;
            readonly Func<RacialTraitOption, float> Of;
            readonly Action<Empire, float> AddTo;

            public Effect(string field, bool lowerIsBetter, Func<RacialTraitOption, float> of, Action<Empire, float> addTo)
            {
                Field = field;
                LowerIsBetter = lowerIsBetter;
                Of = of;
                AddTo = addTo;
            }

            // negative when the trait makes this value worse
            public float Benefit(RacialTraitOption trait) => LowerIsBetter ? -Of(trait) : Of(trait);

            public void AddBenefit(Empire empire, float benefit) => AddTo(empire, LowerIsBetter ? -benefit : benefit);
        }

        static readonly Effect[] Effects =
        {
            new(nameof(RacialTraitOption.ConsumptionModifier), true, t => t.ConsumptionModifier, (e, v) => e.data.Traits.ConsumptionModifier += v),
            new(nameof(RacialTraitOption.PopGrowthMax), true, t => t.PopGrowthMax, (e, v) => e.data.Traits.PopGrowthMax += v),
            new(nameof(RacialTraitOption.MaintMod), true, t => t.MaintMod, (e, v) => e.data.Traits.MaintMod += v),
            new(nameof(RacialTraitOption.ShipCostMod), true, t => t.ShipCostMod, (e, v) => e.data.Traits.ShipCostMod += v),
            new(nameof(RacialTraitOption.DiplomacyMod), false, t => t.DiplomacyMod, (e, v) => e.data.Traits.DiplomacyMod += v),
            new(nameof(RacialTraitOption.DodgeMod), false, t => t.DodgeMod, (e, v) => e.data.Traits.DodgeMod += v),
            new(nameof(RacialTraitOption.TargetingModifier), false, t => t.TargetingModifier, (e, v) => e.data.Traits.TargetingModifier += v),
            new(nameof(RacialTraitOption.GroundCombatModifier), false, t => t.GroundCombatModifier, (e, v) => e.data.Traits.GroundCombatModifier += v),
            new(nameof(RacialTraitOption.Mercantile), false, t => t.Mercantile, (e, v) => e.data.Traits.Mercantile += v),
            new(nameof(RacialTraitOption.RepairMod), false, t => t.RepairMod, (e, v) => e.data.Traits.RepairMod += v),
            new(nameof(RacialTraitOption.PopGrowthMin), false, t => t.PopGrowthMin, (e, v) => e.data.Traits.PopGrowthMin += v),
            new(nameof(RacialTraitOption.Spiritual), false, t => t.Spiritual, (e, v) => e.SetSpiritual(e.data.Traits.Spiritual + v)),
            new(nameof(RacialTraitOption.ProductionMod), false, t => t.ProductionMod, (e, v) => e.data.Traits.ProductionMod += v * AIScale(e, e.DifficultyModifiers.ProductionMod)),
            new(nameof(RacialTraitOption.ResearchMod), false, t => t.ResearchMod, (e, v) => e.data.Traits.ResearchMod += v * AIScale(e, e.DifficultyModifiers.ResearchMod)),
            new(nameof(RacialTraitOption.ModHpModifier), false, t => t.ModHpModifier, (e, v) => e.data.Traits.ModHpModifier += v),
            new(nameof(RacialTraitOption.TaxMod), false, t => t.TaxMod, (e, v) => e.data.Traits.TaxMod += v * AIScale(e, e.DifficultyModifiers.TaxMod)),
            new(nameof(RacialTraitOption.SpyMultiplier), false, t => t.SpyMultiplier, (e, v) => { e.data.Traits.SpyMultiplier += v; e.data.SpyModifier += v; }),
            new(nameof(RacialTraitOption.PassengerBonus), false, t => t.PassengerBonus, (e, v) => e.data.Traits.PassengerModifier += v),
            new(nameof(RacialTraitOption.CreditsPerKilledSlot), false, t => t.CreditsPerKilledSlot, (e, v) => e.data.Traits.CreditsPerKilledSlot += v),
            new(nameof(RacialTraitOption.ResearchBenefitFromAlliance), false, t => t.ResearchBenefitFromAlliance, (e, v) => e.data.Traits.ResearchBenefitFromAlliance += v),
            new(nameof(RacialTraitOption.ConstructionRateMultiplier), false, t => t.ConstructionRateMultiplier - 1f, (e, v) => e.data.Traits.ConstructionRateMultiplier += v),
            new(nameof(RacialTraitOption.BuilderShipConstructionMultiplier), false, t => t.BuilderShipConstructionMultiplier - 1f, (e, v) => e.data.Traits.BuilderShipConstructionMultiplier += v),
            new(nameof(RacialTraitOption.ExploreDistanceMultiplier), false, t => t.ExploreDistanceMultiplier - 1f, (e, v) => e.data.Traits.ExploreDistanceMultiplier += v),
            new(nameof(RacialTraitOption.Militaristic), false, t => t.Militaristic, (e, v) => e.data.Traits.Militaristic += (int)Math.Round(v)),
        };

        // effects that only shape the homeworld or the start of the game: they neither keep a trait out nor count for it
        static readonly string[] StartOnly =
        {
            nameof(RacialTraitOption.BonusExplored), nameof(RacialTraitOption.ExtraStartingScouts), nameof(RacialTraitOption.Prototype),
            nameof(RacialTraitOption.HomeworldSizeMod), nameof(RacialTraitOption.HomeworldFertMod), nameof(RacialTraitOption.HomeworldRichMod),
        };

        // an AI's production, research and tax traits count scaled by its difficulty
        static float AIScale(Empire e, float difficultyMod) => !e.isPlayer && difficultyMod.NotZero() ? difficultyMod : 1f;

        public static void Assimilate(Empire assimilator, Empire conquered)
        {
            Array<RacialTraitOption> all = ResourceManager.RaceTraits?.TraitList ?? new Array<RacialTraitOption>();
            Array<string> names = TraitNames(assimilator.data.SelectedTraitSet);
            Array<string> theirNames = TraitNames(conquered.data.SelectedTraitSet);
            TakeBestOfTechValues(assimilator.data.Traits, conquered.data.Traits, PassengerShare(all, names), PassengerShare(all, theirNames));

            bool changed = false;
            foreach (string name in theirNames)
            {
                RacialTraitOption trait = Find(all, name);
                if (trait == null || names.Contains(name) || !HasNoDownside(trait))
                    continue;

                bool gained = false;
                foreach (Effect effect in Effects)
                {
                    float gain = effect.Benefit(trait) - BestBenefit(all, names, effect);
                    if (gain > 0f)
                    {
                        effect.AddBenefit(assimilator, gain);
                        gained = true;
                    }
                }
                if (!gained)
                    continue;

                for (int i = names.Count - 1; i >= 0; --i)
                {
                    RacialTraitOption own = Find(all, names[i]);
                    if (own != null && HasNoDownside(own) && IsOutdoneBy(own, trait))
                        names.RemoveAt(i);
                }
                names.Add(name);
                changed = true;
            }

            if (changed)
            {
                assimilator.data.SelectedTraitSet = string.Join(", ", names);
                EmpireHullBonuses.RefreshBonuses(assimilator);
            }
        }

        // values techs give: the better of the two empires', passengers without each race's traits' share
        static void TakeBestOfTechValues(RacialTrait ours, RacialTrait theirs, float ourTraitPassengers, float theirTraitPassengers)
        {
            ours.TerraformingLevel = Math.Max(ours.TerraformingLevel, theirs.TerraformingLevel);
            ours.EnemyPlanetInhibitionPercentCounter = Math.Max(ours.EnemyPlanetInhibitionPercentCounter, theirs.EnemyPlanetInhibitionPercentCounter);
            ours.ShipMaintMultiplier = Math.Min(ours.ShipMaintMultiplier, theirs.ShipMaintMultiplier);

            float ourPassengers = ours.PassengerModifier - ourTraitPassengers;
            float theirPassengers = theirs.PassengerModifier - theirTraitPassengers;
            if (theirPassengers > ourPassengers)
                ours.PassengerModifier += theirPassengers - ourPassengers;
        }

        static float PassengerShare(Array<RacialTraitOption> all, Array<string> names)
        {
            float share = 0f;
            for (int i = 0; i < names.Count; ++i)
                if (Find(all, names[i]) is RacialTraitOption trait)
                    share += trait.PassengerBonus;
            return share;
        }

        static Array<string> TraitNames(string selectedTraits)
        {
            var names = new Array<string>();
            if (selectedTraits.IsEmpty())
                return names;

            foreach (string part in selectedTraits.Split(','))
            {
                string name = part.Trim();
                if (name.Length > 0)
                    names.Add(name);
            }
            return names;
        }

        static RacialTraitOption Find(Array<RacialTraitOption> all, string name)
        {
            for (int i = 0; i < all.Count; ++i)
                if (all[i].TraitName == name)
                    return all[i];
            return null;
        }

        // the most any of these traits does for the value, 0 when none helps it
        static float BestBenefit(Array<RacialTraitOption> all, Array<string> names, Effect effect)
        {
            float best = 0f;
            for (int i = 0; i < names.Count; ++i)
                if (Find(all, names[i]) is RacialTraitOption trait)
                    best = Math.Max(best, effect.Benefit(trait));
            return best;
        }

        static bool IsOutdoneBy(RacialTraitOption own, RacialTraitOption other)
        {
            foreach (Effect effect in Effects)
                if (effect.Benefit(own) > effect.Benefit(other))
                    return false;
            return true;
        }

        // every lasting effect of the trait is one of the values above, and none makes its value worse
        static bool HasNoDownside(RacialTraitOption trait)
        {
            bool helps = false;
            foreach (Effect effect in Effects)
            {
                float benefit = effect.Benefit(trait);
                if (benefit < 0f)
                    return false;
                helps |= benefit > 0f;
            }
            return helps && !HasOtherEffects(trait);
        }

        static readonly RacialTraitOption Defaults = new();
        static readonly string[] NotEffects =
        {
            nameof(RacialTraitOption.TraitIndex), nameof(RacialTraitOption.TraitName), nameof(RacialTraitOption.Category),
            nameof(RacialTraitOption.Cost), nameof(RacialTraitOption.Description), nameof(RacialTraitOption.Excludes),
        };

        static bool HasOtherEffects(RacialTraitOption trait)
        {
            foreach (FieldInfo field in typeof(RacialTraitOption).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (Array.IndexOf(NotEffects, field.Name) >= 0 || Array.IndexOf(StartOnly, field.Name) >= 0 || IsEffect(field.Name))
                    continue;
                if (!Equals(field.GetValue(trait), field.GetValue(Defaults)))
                    return true;
            }
            return false;
        }

        static bool IsEffect(string fieldName)
        {
            foreach (Effect effect in Effects)
                if (effect.Field == fieldName)
                    return true;
            return false;
        }
    }
}
