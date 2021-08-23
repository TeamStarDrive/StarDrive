using System.Linq;
using Ship_Game.Gameplay;

namespace Ship_Game.AI 
{
    public sealed partial class EmpireAI
    {
        Empire Player => Empire.Universe.PlayerEmpire;

        private void RunDiplomaticPlanner()
        {
            if (OwnerEmpire.isPlayer || OwnerEmpire.GetAverageWarGrade() < 2 && TryMergeOrSurrender())
                return;

            switch (OwnerEmpire.Personality)
            {
                case PersonalityType.Cunning:
                case PersonalityType.Honorable:
                case PersonalityType.Pacifist:   DoConservativeRelations(); break;
                case PersonalityType.Aggressive: DoAggressiveRelations();   break;
                case PersonalityType.Xenophobic: DoXenophobicRelations();   break;
                case PersonalityType.Ruthless:   DoRuthlessRelations();     break;
            }

            foreach ((Empire them, Relationship rel) in OwnerEmpire.AllRelations)
            {
                if (!them.isFaction && !OwnerEmpire.isFaction && !them.data.Defeated)
                    CheckColonizationClaims(them, rel);
            }
        }

        bool TryMergeOrSurrender()
        {
            float ratio = OwnerEmpire.PersonalityModifiers.PopRatioBeforeMerge;
            var enemies =  EmpireManager.MajorEmpiresAtWarWith(OwnerEmpire)
                .Filter(e => e.TotalPopBillion * ratio > OwnerEmpire.TotalPopBillion 
                             || e.OffensiveStrength * ratio > OwnerEmpire.CurrentMilitaryStrength);

            if (enemies.Length > 0)
            {
                Empire biggest = enemies.FindMax(e => e.TotalPopBillion);
                OwnerEmpire.TryMergeOrSurrender(biggest);
            }

            return false;
        }

        void DoConservativeRelations()
        {
            float territorialDiv = OwnerEmpire.IsPacifist ? 50 : 10;
            AssessTerritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / territorialDiv);
            foreach ((Empire them, Relationship rel) in OwnerEmpire.AllRelations)
            {
                if (!DoNotInteract(rel, them))
                    rel.DoConservative(OwnerEmpire, them);
            }
        }

        void DoRuthlessRelations()
        {
            AssessTerritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 10f);
            var potentialTargets = new Array<Empire>();

            foreach ((Empire them, Relationship rel) in OwnerEmpire.AllRelations)
            {
                if (DoNotInteract(rel, them))
                    continue;

                rel.DoRuthless(OwnerEmpire, them, out bool theyArePotentialTargets);
                if (theyArePotentialTargets)
                    potentialTargets.Add(them);
            }

            PrepareToAttackClosest(potentialTargets);
        }

        void DoAggressiveRelations()
        {
            var potentialTargets = new Array<Empire>();

            AssessTerritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 10f);
            foreach ((Empire them, Relationship rel) in OwnerEmpire.AllRelations)
            {
                if (DoNotInteract(rel, them))
                    continue;

                rel.DoAggressive(OwnerEmpire, them, out bool theyArePotentialTargets);
                if (theyArePotentialTargets)
                    potentialTargets.Add(them);
            }

            PrepareToAttackWeakest(potentialTargets);
        }

        void DoXenophobicRelations()
        {
            var potentialTargets = new Array<Empire>();
            AssessTerritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 7f);
            foreach ((Empire them, Relationship rel) in OwnerEmpire.AllRelations)
            {
                if (DoNotInteract(rel, them))
                    continue;

                rel.DoXenophobic(OwnerEmpire, them, out bool theyArePotentialTargets);
                if (theyArePotentialTargets)
                    potentialTargets.Add(them);
            }

            PrepareToAttackXenophobic(potentialTargets);
        }

        public bool TradableTechs(Empire them, out Array<TechEntry> tradableTechs, bool forceAllTechs = false)
        {
            // Do not allow trading tech if no trade treaty, if the owner is the player, allow them 
            // to offer techs as part of peace request or other stuff
            tradableTechs = new Array<TechEntry>();
            if (!forceAllTechs && !OwnerEmpire.IsTradeTreaty(them) && !OwnerEmpire.isPlayer)
                return false; 

            var available = OwnerEmpire.TechsAvailableForTrade(them);
            foreach (TechEntry tech in available)
            {
                if (!forceAllTechs && tech.IsMilitary() && !OwnerEmpire.IsAlliedWith(them))
                    continue; // Need Alliance in order to trade military tech

                if (tech.TheyCanUseThis(OwnerEmpire, them))
                    tradableTechs.Add(tech);
            }

            return tradableTechs.Count > 0;
        }

        bool DoNotInteract(Relationship relations, Empire them)
        {
            return !relations.Known
                   || them.isFaction
                   || them.data.Defeated
                   || GlobalStats.RestrictAIPlayerInteraction && Player == them;
        }

        int TotalEnemiesStrength()
        {
            int enemyStr = 0;
            foreach ((Empire them, Relationship rel) in OwnerEmpire.AllRelations)
            {
                if (!them.isFaction
                    &&!them.data.Defeated 
                    && (rel.AtWar || rel.PreparingForWar))
                {
                    enemyStr += (int)them.CurrentMilitaryStrength;
                }
            }

            return enemyStr;
        }

        void PrepareToAttackClosest(Array<Empire> potentialTargets)
        {
            if (!OwnerEmpire.IsAtWarWithMajorEmpire
                && potentialTargets.Count > 0 
                && TotalEnemiesStrength() * 1.5f < OwnerEmpire.OffensiveStrength)
            {
                Empire closest = potentialTargets.Sorted(e => e.WeightedCenter.Distance(OwnerEmpire.WeightedCenter)).First();
                Relationship usToThem = OwnerEmpire.GetRelations(closest);
                if (closest.CurrentMilitaryStrength * 1.5f < OwnerEmpire.OffensiveStrength)
                    usToThem.PrepareForWar(WarType.ImperialistWar, OwnerEmpire);
            }
        }

        void PrepareToAttackXenophobic(Array<Empire> potentialTargets)
        {
            if (!OwnerEmpire.IsAtWarWithMajorEmpire
                && potentialTargets.Count > 0 
                && TotalEnemiesStrength() < OwnerEmpire.OffensiveStrength)
            {
                Empire closest = potentialTargets.Sorted(e => e.WeightedCenter.Distance(OwnerEmpire.WeightedCenter)).First();
                Relationship usToThem = OwnerEmpire.GetRelations(closest);
                if (closest.CurrentMilitaryStrength * 2f < OwnerEmpire.OffensiveStrength)
                    usToThem.PrepareForWar(WarType.GenocidalWar, OwnerEmpire);
            }
        }

        void PrepareToAttackWeakest(Array<Empire> potentialTargets)
        {
            if (!OwnerEmpire.IsAtWarWithMajorEmpire
                && potentialTargets.Count > 0 
                && TotalEnemiesStrength() < OwnerEmpire.OffensiveStrength)
            {
                Empire weakest        = potentialTargets.Sorted(e => e.CurrentMilitaryStrength).First();
                Relationship usToThem = OwnerEmpire.GetRelations(weakest);
                usToThem.PrepareForWar(WarType.ImperialistWar, OwnerEmpire);
            }
        }

        void AssessTerritorialConflicts(float weight)
        {
            weight *= 0.1f;
            foreach (SystemCommander borders in DefensiveCoordinator.DefenseDict.Values.Filter(sysCom => sysCom.RankImportance > 5))
            {
                foreach (SolarSystem closeSystem in borders.System.FiveClosestSystems)
                {
                    foreach (Empire others in closeSystem.OwnerList)
                    {
                        if (others == OwnerEmpire
                            || others.isFaction
                            || !OwnerEmpire.GetRelations(others, out Relationship usToThem)
                            || !usToThem.Known
                            || usToThem.Treaty_Alliance
                            || usToThem.Treaty_OpenBorders
                            || usToThem.Treaty_Peace)
                        {
                            continue;
                        }

                        float modifiedWeight = GetModifiedTerritorialWeight(weight, usToThem, others, closeSystem);

                        float angerConflict = usToThem.Anger_TerritorialConflict.LowerBound(1);
                        usToThem.AddAngerTerritorialConflict(borders.RankImportance * modifiedWeight / angerConflict);
                    }
                }
            }
        }

        float GetModifiedTerritorialWeight(float weight, Relationship usToThem, Empire others, SolarSystem system)
        {
            float modifiedWeight = weight;
            float ourStr         = OwnerEmpire.CurrentMilitaryStrength.LowerBound(1);
            modifiedWeight      *= (system.GetActualStrengthPresent(others) / ourStr).UpperBound(1f);

            if (usToThem.Treaty_Trade)
                modifiedWeight *= 0.5f;

            if (usToThem.Treaty_NAPact)
                modifiedWeight *= 0.5f;

            if (others.isPlayer)
                modifiedWeight *= OwnerEmpire.DifficultyModifiers.DiploWeightVsPlayer;

            return modifiedWeight;
        }
    }
}