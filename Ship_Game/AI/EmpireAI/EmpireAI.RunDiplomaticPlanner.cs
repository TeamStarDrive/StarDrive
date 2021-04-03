using System.Linq;
using Ship_Game.Gameplay;

namespace Ship_Game.AI 
{
    public sealed partial class EmpireAI
    {
        Empire Player => Empire.Universe.PlayerEmpire;

        private void RunDiplomaticPlanner()
        {
            if (OwnerEmpire.isPlayer)
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

        void DoConservativeRelations()
        {
            float territorialDiv = OwnerEmpire.Personality == PersonalityType.Pacifist ? 50 : 10;
            AssessTerritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / territorialDiv);
            foreach ((Empire them, Relationship rel) in OwnerEmpire.AllRelations)
            {
                if (!DoNotInteract(rel, them))
                    rel.DoConservative(OwnerEmpire, them);
            }
        }

        void DoRuthlessRelations()
        {
            AssessTerritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 5f);
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
            AssessTerritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 10f);
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

        public bool TradableTechs(Empire them, out Array<TechEntry> tradableTechs)
        {
            tradableTechs = new Array<TechEntry>();
            var available = OwnerEmpire.TechsAvailableForTrade(them);
            foreach (TechEntry tech in available)
            {
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
            if (potentialTargets.Count > 0 && TotalEnemiesStrength() * 1.5f < OwnerEmpire.OffensiveStrength)
            {
                Empire closest = potentialTargets.Sorted(e => e.WeightedCenter.Distance(OwnerEmpire.WeightedCenter)).First();
                Relationship usToThem = OwnerEmpire.GetRelations(closest);
                if (usToThem.ActiveWar != null && usToThem.ActiveWar.WarType == WarType.DefensiveWar)
                {
                    usToThem.ActiveWar.WarTheaters.AddCaptureAll();
                    return;
                }

                if (closest.CurrentMilitaryStrength * 1.5f < OwnerEmpire.OffensiveStrength)
                {
                    OwnerEmpire.ResetPreparingForWar();
                    usToThem.PreparingForWar     = true;
                    usToThem.PreparingForWarType = WarType.ImperialistWar;
                }
            }
        }

        void PrepareToAttackXenophobic(Array<Empire> potentialTargets)
        {
            if (potentialTargets.Count > 0 && TotalEnemiesStrength() < OwnerEmpire.OffensiveStrength)
            {
                Empire closest = potentialTargets.Sorted(e => e.WeightedCenter.Distance(OwnerEmpire.WeightedCenter)).First();
                Relationship usToThem = OwnerEmpire.GetRelations(closest);
                if (usToThem.ActiveWar != null && usToThem.ActiveWar.WarType == WarType.DefensiveWar)
                {
                    usToThem.ActiveWar.WarTheaters.AddCaptureAll();
                    return;
                }

                if (closest.CurrentMilitaryStrength * 2f < OwnerEmpire.OffensiveStrength)
                {
                    usToThem.PreparingForWar     = true;
                    usToThem.PreparingForWarType = WarType.GenocidalWar;
                }
            }
        }

        void PrepareToAttackWeakest(Array<Empire> potentialTargets)
        {
            if (potentialTargets.Count > 0 && TotalEnemiesStrength() < OwnerEmpire.OffensiveStrength)
            {
                Empire weakest        = potentialTargets.Sorted(e => e.CurrentMilitaryStrength).First();
                Relationship usToThem = OwnerEmpire.GetRelations(weakest);
                if (usToThem.ActiveWar != null && usToThem.ActiveWar.WarType == WarType.DefensiveWar)
                {
                    usToThem.ActiveWar.WarTheaters.AddCaptureAll();
                    return;
                }

                OwnerEmpire.ResetPreparingForWar();
                usToThem.PreparingForWar     = true;
                usToThem.PreparingForWarType = WarType.ImperialistWar;
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

                        if (usToThem.Anger_TerritorialConflict > 0)
                            usToThem.AddAngerTerritorialConflict((usToThem.Anger_TerritorialConflict + borders.RankImportance * modifiedWeight) / usToThem.Anger_TerritorialConflict);
                        else
                            usToThem.AddAngerTerritorialConflict(borders.RankImportance * modifiedWeight);
                    }
                }
            }
        }

        float GetModifiedTerritorialWeight(float weight, Relationship usToThem, Empire others, SolarSystem system)
        {
            float modifiedWeight = weight;
            float ourStr         = OwnerEmpire.CurrentMilitaryStrength.LowerBound(1);
            modifiedWeight      *= (ourStr + system.GetActualStrengthPresent(others)) / ourStr;

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