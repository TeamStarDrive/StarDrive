﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.Data;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.NewGame;
using static System.Net.Mime.MediaTypeNames;

namespace Ship_Game
{
    public partial class RaceDesignScreen
    {
        class RaceTextBuilder
        {
            readonly string PluralText, SingularText;
            readonly StringBuilder Sb = new StringBuilder();
            public override string ToString() => Sb.ToString();
            public RaceTextBuilder(string plural, string singular)
            {
                PluralText = plural;
                SingularText = singular;
            }
            public RaceTextBuilder Add(GameText text)      { Sb.Append(Localizer.Token(text)); return this; }
            public RaceTextBuilder Add(string text)        { Sb.Append(text); return this; }
            public RaceTextBuilder Plural(GameText text)   { Sb.Append(PluralText).Append(Localizer.Token(text)); return this; }
            public RaceTextBuilder Plural(string text)     { Sb.Append(PluralText).Append(text); return this; }
            public RaceTextBuilder Singular(GameText text) { Sb.Append(SingularText).Append(Localizer.Token(text)); return this; }
        }

        protected void DoRaceDescription()
        {
            CreateRaceSummary();
            RacialTrait race = RaceSummary;

            var b = new RaceTextBuilder(Plural, Singular);

            b.Add(RaceName).Add(GameText.IsAnEmpireComprisedOf).Plural(". ");
            b.Plural(race.IsOrganic ? GameText.AreARaceOf : GameText.AreACyberneticRaceOf);
            b.Add(race.Aquatic <= 0 ? GameText.TerrestrialBeingsWith : GameText.AquaticBeingsWith);
          
            var growth = race.IsOrganic ? (Hi:GameText.AHighReproductiveRateAnd, Avg:GameText.AnAverageReproductiveRateAnd, Lo:GameText.ALowReproductiveRateAnd)
                                        : (Hi:GameText.SwiftManufacturingSpeedsAnd, Avg:GameText.AverageManufacturingTimesAnd, Lo:GameText.LengthyManufacturingTimesAnd);
            if      (race.PopGrowthMin > 0f) b.Add(growth.Hi);
            else if (race.PopGrowthMin < 0f || race.PopGrowthMax < 0f) b.Add(growth.Lo);
            else b.Add(growth.Avg);

            var consumption = race.IsOrganic ? (Hi:GameText.AGluttonousApproachToLife, Avg:GameText.UnremarkableDietaryNeeds, Lo:GameText.ARatherEfficientMetabolism) 
                                             : (Hi:GameText.ARavenousAppetiteForParts, Avg:GameText.ModerateMaterialRequirements, Lo:GameText.MinimalMaterialAndComponentRequirements);
            if      (race.ConsumptionModifier > 0f) b.Add(consumption.Hi);
            else if (race.ConsumptionModifier < 0f) b.Add(consumption.Lo);
            else                                    b.Add(consumption.Avg);


            var combat = race.IsOrganic ? (Majestic:(GameText.Physically, GameText.PossessANonthreateningCountenanceThat),  // Physically, HUMANS possess a non-threatening countenance..... However, they are exceedingly charismatic and naturally draw others towards them
                                           Grotesque:(GameText.Physically, GameText.PossessANonthreateningCountenanceThat2), // Physically, HUMANS possess a non-threatening countenance..... However, they are offensively mannered with many odiferous
                                           Tolerable:(GameText.Physically, GameText.HaveNoParticularStrengthsOr)) // Physically, HUMANS have no particular strengths or weaknesses
                                        : (Majestic:(GameText.Physically, GameText.SeemToHaveMadeGreat),  // Physically, HUMANS seem to have made great efforts to mechanically improve upon the grace and beauty of their bodies
                                           Grotesque:(GameText.InRejectingTheirOrganicForms, GameText.HaveDevelopedAColdDisdain), // In rejecting their organic forms,HUMANS have developed a cold disdain for organic life forms and are particularly uncharismatic
                                           Tolerable:(GameText.AlthoughTheyHaveReplacedTheir, GameText.HaveNotExtensivelyImprovedOn));// Although they have replaced their organic bodies with cybernetics, HUMANS have not extensively improved on their original organic structure
            
            if (race.GroundCombatModifier > 0f)
                combat = race.IsOrganic ? (Majestic:(GameText.Physically, GameText.AreBothBeautifulAndTerrifying),  // Physically, HUMANS are both beautiful and terrifying, with an enigmatic sort of lethal majesty
                                           Grotesque:(GameText.Physically, GameText.AppearBothLethalAndGrotesque), // Physically, HUMANS appear both lethal and grotesque, as they tend to project a sense of instinctive
                                           Tolerable:(GameText.Physically, GameText.AppearSomewhatBestialWithObvious)) // Physically, HUMANS appear somewhat bestial, with obvious signs of their great strength
                                        : (Majestic:(GameText.InRejectingTheirOrganicForms, GameText.HaveBecomeIncredibleMachinesOf),  // In rejecting their organic forms, HUMANS have become incredible machines of grace and lethality, with a sleek, deadly, and beautiful appearance
                                           Grotesque:(GameText.InRejectingTheirOrganicForms, GameText.HaveBecomeConfusingTechnologicalMonstrosities), // In rejecting their organic forms, HUMANS have become confusing technological monstrosities with deadly-looking appendages and a decidedly hostile appearance
                                           Tolerable:(GameText.InRejectingTheirOrganicForms, GameText.LookLikeMachinesDesignedFor));// In rejecting their organic forms, HUMANS look like machines designed for war without any particular focus put on appearance
            
            if (race.GroundCombatModifier < 0f)
                combat = race.IsOrganic ? (Majestic:(GameText.Physically,GameText.AreCreaturesOfFragileBeauty),  // Physically, HUMANS are creatures of fragile beauty, with a delicate and ephemeral stature
                                           Grotesque:(GameText.Physically,GameText.AppearGnarledAndWeakWith), // Physically, HUMANS appear gnarled and weak, with a countenance both hideous and scrawny
                                           Tolerable:(GameText.Physically,GameText.SeemToHaveEvolvedWithout)) // Physically, HUMANS seem to have evolved without much need for speed or strength and as a result are somewhat frail
                                        : (Majestic:(GameText.Physically,GameText.ComeOffAsFriendlyLittle),  // Physically, HUMANS come off as friendly little robots that could only be described as cute, but fragile
                                           Grotesque:(GameText.Physically,GameText.AreSpindlyInsectlikeMachinesThat), // Physically, HUMANS are spindly, insect-like machines that click and hiss in menacing ways. They look like they could be crunched under a boot
                                           Tolerable:(GameText.Physically,GameText.LookLikeComputersWithWheels));// Physically, HUMANS look like computers with wheels and treads attached, and do not seem armed or armored in any way

            if      (race.DiplomacyMod > 0f)  b.Add(combat.Majestic.Item1).Plural(combat.Majestic.Item2);
            else if (race.DiplomacyMod < 0f)  b.Add(combat.Grotesque.Item1).Plural(combat.Grotesque.Item2);
            else                              b.Add(combat.Tolerable.Item1).Plural(combat.Tolerable.Item2);


            if (race.GroundCombatModifier < 0f || race.DiplomacyMod <= 0f) // if bad stats before, consider these prefixes:
            {
                if (race.ResearchMod > 0f) b.Add(GameText.However); // "However, "
                if (race.ResearchMod < 0f) b.Add(GameText.ToMakeMattersWorse); // "To make matters worse, "
            }
            else if (race.GroundCombatModifier > 0f || race.DiplomacyMod > 0f) // if good stats before, consider these prefixes:
            {
                if (race.ResearchMod > 0f) b.Add(GameText.Furthermore); // "Furthermore, "
                if (race.ResearchMod < 0f) b.Add(GameText.However); // "However, "
            }
            if      (race.ResearchMod > 0f) b.Plural(GameText.AreExtremelyIntelligentForA2); // | HUMANS | are extremely intelligent for a starfaring race and have a natural curiosity about the universe
            else if (race.ResearchMod < 0f) b.Plural(GameText.AreSomewhatDumbForA); // | HUMANS | are somewhat dumb for a starfaring race and have difficulty being creative
            else                            b.Plural(GameText.PossessAnAverageIntelligenceFor); // | HUMANS | possess an average intelligence for a starfaring race

            b.Add("\n \n");
            if (race.TaxMod > 0f)
            {
                b.Singular(GameText.GovernmentIsEfficientAndEffective); // | HUMAN | government is efficient and effective (HAH! AS IF!)
                if      (race.MaintMod < 0f) b.Add(GameText.WhichReflectsTheMethodicalAnd).Plural(". "); // , which reflects the methodical and practical nature of | HUMANS |.
                else if (race.MaintMod > 0f) b.Add(GameText.ButThisIsCounterbalancedBy); // but this is counterbalanced by a wasteful, consumer-oriented society
                else                         b.Add(GameText.AndItsCitizensAreRelatively); // and its citizens are relatively intelligent about resource allocation
            }
            else if (race.TaxMod < 0f)
            {
                b.Add(GameText.While).Singular(GameText.GovernmentCanBeSomewhatCorrupt); // While | HUMAN | government can be somewhat corrupt
                if      (race.MaintMod < 0f) b.Add(GameText.ItsPrivateSectorMakesUp); // , its private sector makes up for the government's wastefulness with its efficiency. 
                else if (race.MaintMod > 0f) b.Add(GameText.ThisCorruptionPalesInComparison); // , this corruption pales in comparison to recklessness of such a wasteful, consumer-oriented society.
                else                         b.Add(", ").Plural(GameText.AreNeverthelessAPracticalRace); // , | HUMANS | are nevertheless a practical race when it comes to resource allocation.
            }
            else
            {
                b.Singular(GameText.GovernmentIsFunctionalAndStable); // | HUMAN | government is functional and stable
                if      (race.MaintMod < 0f) b.Add(GameText.EvenSo).Singular(GameText.SocietyAtLargeIsVery); // Even so, | HUMAN | society at large is very environmentally concious and practical about resource allocation.
                else if (race.MaintMod > 0f) b.Plural(GameText.AlsoTendToBeA); // | HUMANS | also tend to be a wasteful people who are neglectful of their investments
            }

            if (race.ProductionMod > 0f)
            {
                b.Singular(GameText.WorkEthicIsStrong); // | HUMAN | work ethic is strong
                if      (race.ModHpModifier > 0f) b.Add(GameText.AndTheirEngineersAndShipwrights); // and their engineers and shipwrights produce components of superior quality
                else if (race.ModHpModifier < 0f) b.Add(GameText.YetTheirEngineeringSkillsAre); // yet their engineering skills are poor, producing goods and ships of somewhat inferior quality
                else                              b.Add(GameText.AndTheirEngineersAndShipwrights2); // and their engineers and shipwrights are generally competent
            }
            else if (race.ProductionMod >= 0f)
            {
                b.Plural(GameText.AreNeitherParticularlyIndustriousNor); // | HUMANS | are neither particularly industrious nor particularly lazy
                if      (race.ModHpModifier > 0f) b.Add(GameText.ButTheirEngineersAndShipwrights2); // but their engineers and shipwrights are highly skilled and produce components of superior quality
                else if (race.ModHpModifier < 0f) b.Add(GameText.OverallTheirEngineeringSkillsAre); // . Overall, their engineering skills are somwhat poor compared to peer races and tend to produce ships of inferior quality. 
                else                              b.Add(GameText.AndTheirEngineersAndShipwrights3); // and their engineers and shipwrights are generally competent
            }
            else
            {
                b.Singular(GameText.WorkersAreGenerallyQuiteLazy); // | HUMAN | workers are generally quite lazy
                if      (race.ModHpModifier > 0f) b.Add(GameText.YetTheirEngineersAndShipwrights); // yet their engineers and shipwrights are highly skilled, producing components of superior quality
                else if (race.ModHpModifier < 0f) b.Add(GameText.AndTheirEngineeringSkillsAre); // and their engineering skills are poor, producing goods and ships of somewhat inferior quality
                else                              b.Add(GameText.ButTheirEngineersAndShipwrights); // but their engineers and shipwrights are generally competent
            }

            if      (race.SpyMultiplier > 0f) b.Plural(GameText.PossessASomewhatDuplicitousNature); // | HUMANS | possess a somewhat duplicitous nature and as a result make excellent spies
            else if (race.SpyMultiplier < 0f) b.Plural(GameText.LiveVeryOpenAndHonest); // | HUMANS | live very open and honest lives, and as a result have a poor understanding of the concept of espionage

            if (race.Spiritual > 0f) b.Add(GameText.FinallyTheyAreADeeply);

            b.Add("\n \n").Add(GameText.The).Singular(GameText.HomeworldOf).Add(HomeWorldName); // The | HUMAN | homeworld of | EARTH |
            if      (race.HomeworldSizeMod > 0f) b.Add(GameText.IsVeryLargeForA); // is very large for a terran planet
            else if (race.HomeworldSizeMod < 0f) b.Add(GameText.IsVerySmallForA); // is very small for a terran planet
            else                                 b.Add(GameText.IsOfAnAverageSize); // is of an average size for a terran planet

            if (race.HomeworldFertMod < 0f) b.Add(GameText.ThePlanetIsExtremelyPolluted); // The planet is extremely polluted from centuries of environmental abuses
            if (race.BonusExplored > 0) b.Add(GameText.HistoricallyThisRaceHasLooked); // Historically, this race has looked to the stars for many centuries

            if (race.Militaristic > 0)
            {
                b.Add(GameText.AStrongHistoryOfMilitary); // A strong history of military conflict has made the military a central part of this race's culture.
                if (race.ShipCostMod < 0f) b.Add(GameText.Furthermore2).Singular(GameText.NavalTraditionIsVeryStrong); // Furthermore, | HUMAN | naval tradition is very strong,
            }
            else if (race.ShipCostMod < 0f)
            {
                b.Plural(GameText.AreNaturalSailorsAndShipwrights); // | HUMANS | are natural sailors and shipwrights
            }
            DescriptionTextList.SetLines(b.ToString(), DescriptionTextFont, Color.White);
        }

    
        // Sets the empire data externally, checks for fields that are default so don't overwrite
        public void SetCustomEmpireData(RacialTrait traits)
        {
            RaceArchetypeListItem archetype = ChooseRaceList.AllEntries.Find(
                                                (r) => r.EmpireData.Traits.ShipType == traits.ShipType);
            if (archetype == null)
            {
                Log.Error($"Failed to find Archetype: {traits.ShipType}");
                return;
            }

            ChooseRaceList.OnItemClicked(archetype); // click on the archetype faction
            SetRacialTraits(traits);
            EnvMenu.UpdatePreferences(RaceSummary);
        }

        void SetRacialTraits(RacialTrait traits)
        {
            foreach (TraitEntry traitEntry in AllTraits)
            {
                traitEntry.ExcludedBy = new();
            }

            RaceSummary.ShipType = traits.ShipType;
            Picker.CurrentColor  = traits.Color;
            FlagIndex = traits.FlagIndex;
            RaceName = traits.Name;
            Singular = traits.Singular;
            Plural = traits.Plural;
            HomeSysName = traits.HomeSystemName;
            HomeWorldName = traits.HomeworldName;
            TotalPointsUsed = P.RacialTraitPoints;

            foreach (TraitEntry traitEntry in AllTraits)
            {
                traitEntry.Selected = false;
                RacialTraitOption tEnt = traitEntry.Trait;
                if (traits.PlayerTraitOptions.Contains(tEnt.TraitName))
                {
                    traitEntry.Selected = true;
                    TotalPointsUsed -= tEnt.Cost;
                    SetExclusions(traitEntry);
                }
            }
            DoRaceDescription();
        }

        void UpdateTraits()
        {
            foreach (TraitEntry traitEntry in AllTraits)
                traitEntry.ExcludedBy = new();

            TotalPointsUsed = P.RacialTraitPoints;
            foreach (TraitEntry traitEntry in AllTraits.Filter(t => t.Selected))
            {
                TotalPointsUsed -= traitEntry.Trait.Cost;
                SetExclusions(traitEntry);
            }
        }

        void SetExclusions(TraitEntry t)
        {
            foreach (TraitEntry ex in AllTraits)
                if (t.Trait.Excludes.Contains(ex.Trait.TraitName))
                    ex.ExcludedBy.Add(t.Trait.LocalizedName.Text);
        }
        
        void CreateRaceSummary()
        {
            RaceSummary = new RacialTrait();

            foreach (TraitEntry t in AllTraits)
            {
                if (!t.Selected)
                    continue;

                RacialTraitOption trait = t.Trait;
                RaceSummary.ConsumptionModifier    += trait.ConsumptionModifier;
                RaceSummary.DiplomacyMod           += trait.DiplomacyMod;
                RaceSummary.TargetingModifier      += trait.TargetingModifier;
                RaceSummary.MaintMod               += trait.MaintMod;
                RaceSummary.ReproductionMod        += trait.ReproductionMod;
                RaceSummary.PopGrowthMax           += trait.PopGrowthMax;
                RaceSummary.PopGrowthMin           += trait.PopGrowthMin;
                RaceSummary.ResearchMod            += trait.ResearchMod;
                RaceSummary.ShipCostMod            += trait.ShipCostMod;
                RaceSummary.TaxMod                 += trait.TaxMod;
                RaceSummary.ProductionMod          += trait.ProductionMod;
                RaceSummary.ModHpModifier          += trait.ModHpModifier;
                RaceSummary.Mercantile             += trait.Mercantile;
                RaceSummary.GroundCombatModifier   += trait.GroundCombatModifier;
                RaceSummary.Cybernetic             += trait.Cybernetic;
                RaceSummary.Blind                  += trait.Blind;
                RaceSummary.DodgeMod               += trait.DodgeMod;
                RaceSummary.HomeworldFertMod       += trait.HomeworldFertMod;
                RaceSummary.HomeworldRichMod       += trait.HomeworldRichMod;
                RaceSummary.HomeworldSizeMod       += trait.HomeworldSizeMod;
                RaceSummary.Militaristic           += trait.Militaristic;
                RaceSummary.BonusExplored          += trait.BonusExplored;
                RaceSummary.Prototype              += trait.Prototype;
                RaceSummary.Spiritual              += trait.Spiritual;
                RaceSummary.SpyMultiplier          += trait.SpyMultiplier;
                RaceSummary.RepairMod              += trait.RepairMod;
                RaceSummary.PassengerModifier      += trait.PassengerBonus;
                RaceSummary.Pack                   += trait.Pack;
                RaceSummary.Aquatic                += trait.Aquatic;
                RaceSummary.CreditsPerKilledSlot   += trait.CreditsPerKilledSlot;
                RaceSummary.PenaltyPerKilledSlot   += trait.PenaltyPerKilledSlot;
                RaceSummary.ExtraStartingScouts    += trait.ExtraStartingScouts;
                RaceSummary.ResearchBenefitFromAlliance += trait.ResearchBenefitFromAlliance;

                RaceSummary.ExploreDistanceMultiplier *= trait.ExploreDistanceMultiplier;
                RaceSummary.ConstructionRateMultiplier *= trait.ConstructionRateMultiplier;
                RaceSummary.BuilderShipConstructionMultiplier *= trait.BuilderShipConstructionMultiplier;

                RaceSummary.EnvTerran   *= trait.EnvTerranMultiplier;
                RaceSummary.EnvOceanic  *= trait.EnvOceanicMultiplier;
                RaceSummary.EnvSteppe   *= trait.EnvSteppeMultiplier;
                RaceSummary.EnvTundra   *= trait.EnvTundraMultiplier;
                RaceSummary.EnvSwamp    *= trait.EnvSwampMultiplier;
                RaceSummary.EnvDesert   *= trait.EnvDesertMultiplier;
                RaceSummary.EnvIce      *= trait.EnvIceMultiplier;
                RaceSummary.EnvBarren   *= trait.EnvBarrenMultiplier;
                RaceSummary.EnvVolcanic *= trait.EnvVolcanicMultiplier;
                if (trait.PreferredEnv != PlanetCategory.Terran)
                    RaceSummary.PreferredEnv = trait.PreferredEnv;
            }
        }
    }
}
