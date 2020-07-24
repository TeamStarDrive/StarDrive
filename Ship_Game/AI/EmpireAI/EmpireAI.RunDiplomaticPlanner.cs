using System.Collections.Generic;
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

            foreach (KeyValuePair<Empire, Relationship> relationship in OwnerEmpire.AllRelations)
            {
                if (!relationship.Key.isFaction && !OwnerEmpire.isFaction && !relationship.Key.data.Defeated)
                    RunEventChecker(relationship);
            }
        }

        public void RequestHelpFromAllies(Relationship usToEnemy, Empire enemy, int contactThreshold)
        {
            if (usToEnemy.ActiveWar == null) // They Accepted Peace
                return;

            var allies = new Array<Empire>();
            foreach (KeyValuePair<Empire, Relationship> kv in OwnerEmpire.AllRelations)
            {
                if (kv.Value.Treaty_Alliance 
                    && kv.Key.GetRelations(enemy).Known
                    && !kv.Key.GetRelations(enemy).AtWar)
                {
                    allies.Add(kv.Key);
                }
            }
            foreach (Empire ally in allies)
            {
                if (!usToEnemy.ActiveWar.AlliesCalled.Contains(ally.data.Traits.Name)
                    && OwnerEmpire.GetRelations(ally).turnsSinceLastContact > contactThreshold)
                {
                    CallAllyToWar(ally, enemy);
                    usToEnemy.ActiveWar.AlliesCalled.Add(ally.data.Traits.Name);
                }
            }
        }

        void DoConservativeRelations()
        {
            float territorialDiv = OwnerEmpire.Personality == PersonalityType.Pacifist ? 50 : 10;
            AssessTerritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / territorialDiv);
            foreach (KeyValuePair<Empire, Relationship> kv in OwnerEmpire.AllRelations)
            {
                Relationship relations = kv.Value;
                Empire them            = kv.Key;
                if (DoNotInteract(relations, them))
                    continue;

                relations.DoConservative(OwnerEmpire, them);
            }
        }

        void DoRuthlessRelations()
        {
            AssessTerritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 5f);
            Array<Empire> potentialTargets = new Array<Empire>();

            foreach (KeyValuePair<Empire, Relationship> kv in OwnerEmpire.AllRelations)
            {
                Relationship relations = kv.Value;
                Empire them            = kv.Key;
                if (DoNotInteract(relations, them))
                    continue;

                relations.DoRuthless(OwnerEmpire, them, out bool theyArePotentialTargets);
                if (theyArePotentialTargets)
                    potentialTargets.Add(them);
            }

            PrepareToAttackClosest(potentialTargets);
        }

        void DoAggressiveRelations()
        {
            Array<Empire> potentialTargets = new Array<Empire>();

            AssessTerritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 10f);
            foreach (KeyValuePair<Empire, Relationship> kv in OwnerEmpire.AllRelations)
            {
                Relationship relations = kv.Value;
                Empire them            = kv.Key;
                if (DoNotInteract(relations, them))
                    continue;

                relations.DoAggressive(OwnerEmpire, them, out bool theyArePotentialTargets);
                if (theyArePotentialTargets)
                    potentialTargets.Add(them);
            }

            PrepareToAttackWeakest(potentialTargets);
        }

        void DoXenophobicRelations()
        {
            AssessTerritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 10f);
            foreach (KeyValuePair<Empire, Relationship> kv in OwnerEmpire.AllRelations)
            {
                Relationship relations = kv.Value;
                Empire them            = kv.Key;
                if (DoNotInteract(relations, them))
                    continue;

                relations.DoXenophobic(OwnerEmpire);
            }
        }

        public Array<TechEntry> TradableTechs(Empire them)
        {
            var tradableTechs = new Array<TechEntry>();
            var available     = OwnerEmpire.TechsAvailableForTrade(them);
            foreach (TechEntry tech in available)
            {
                if (tech.TheyCanUseThis(OwnerEmpire, them))
                    tradableTechs.Add(tech);
            }

            return tradableTechs;
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
            foreach (KeyValuePair<Empire, Relationship> relationship in OwnerEmpire.AllRelations)
            {
                if (!relationship.Key.isFaction
                    &&!relationship.Key.data.Defeated 
                    && (relationship.Value.AtWar || relationship.Value.PreparingForWar))
                {
                    enemyStr += (int)relationship.Key.CurrentMilitaryStrength;
                }
            }

            return enemyStr;
        }

        void PrepareToAttackClosest(Array<Empire> potentialTargets)
        {
            if (potentialTargets.Count > 0 && TotalEnemiesStrength() * 1.5f < OwnerEmpire.CurrentMilitaryStrength)
            {
                Empire closest = potentialTargets.Sorted(e => e.GetWeightedCenter().Distance(OwnerEmpire.GetWeightedCenter())).First();
                Relationship usToThem = OwnerEmpire.GetRelations(closest);
                if (usToThem.ActiveWar != null && usToThem.ActiveWar.WarType == WarType.DefensiveWar)
                {
                    usToThem.ActiveWar.WarTheaters.AddCaptureAll();
                    return;
                }

                DeclareWarOn(closest, WarType.ImperialistWar);

            }
        }

        void PrepareToAttackWeakest(Array<Empire> potentialTargets)
        {
            if (potentialTargets.Count > 0 && TotalEnemiesStrength() < OwnerEmpire.CurrentMilitaryStrength)
            {
                Empire weakest       = potentialTargets.Sorted(e => e.CurrentMilitaryStrength).First();
                Relationship usToThem = OwnerEmpire.GetRelations(weakest);
                if (usToThem.ActiveWar != null && usToThem.ActiveWar.WarType == WarType.DefensiveWar)
                {
                    usToThem.ActiveWar.WarTheaters.AddCaptureAll();
                    return;
                }

                DeclareWarOn(weakest, WarType.ImperialistWar);
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
                            || !OwnerEmpire.TryGetRelations(others, out Relationship usToThem)
                            || !usToThem.Known
                            || usToThem.Treaty_Alliance)
                        {
                            continue;
                        }

                        float modifiedWeight = GetModifiedTerritorialWeight(weight, usToThem, others, closeSystem);

                        if (usToThem.Anger_TerritorialConflict > 0)
                            usToThem.Anger_TerritorialConflict += (usToThem.Anger_TerritorialConflict + borders.RankImportance * modifiedWeight) / usToThem.Anger_TerritorialConflict;
                        else
                            usToThem.Anger_TerritorialConflict += borders.RankImportance * modifiedWeight;
                    }
                }
            }
        }

        float GetModifiedTerritorialWeight(float weight, Relationship usToThem, Empire others, SolarSystem system)
        {
            float modifiedWeight = weight;
            float ourStr         = OwnerEmpire.CurrentMilitaryStrength.LowerBound(1);
            modifiedWeight      *= (ourStr + system.GetActualStrengthPresent(others)) / ourStr;

            if (usToThem.Treaty_OpenBorders)
                modifiedWeight *= 0.5f;

            if (usToThem.Treaty_NAPact)
                modifiedWeight *= 0.5f;

            if (others.isPlayer)
                modifiedWeight *= OwnerEmpire.DifficultyModifiers.DiploWeightVsPlayer;

            return modifiedWeight;
        }
    }
}