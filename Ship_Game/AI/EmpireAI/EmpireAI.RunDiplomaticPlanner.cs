using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.DiplomacyScreen;

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

        void RequestHelpFromAllies(Relationship usToEnemy, Empire enemy)
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
                    && OwnerEmpire.GetRelations(ally).turnsSinceLastContact > FirstDemand)
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

                switch (relations.Posture)
                {
                    case Posture.Friendly:
                        relations.OfferTrade(OwnerEmpire);
                        // Todo Trade Tech
                        relations.OfferNonAggression(OwnerEmpire);
                        relations.OfferNonAggression(OwnerEmpire);
                        relations.OfferOpenBorders(OwnerEmpire);
                        relations.OfferAlliance(OwnerEmpire);
                        ChangeToNeutralIfPossible(relations, them);
                        break;
                    case Posture.Neutral:
                        relations.AssessDiplomaticAnger(OwnerEmpire);
                        relations.OfferTrade(OwnerEmpire);
                        relations.OfferNonAggression(OwnerEmpire);
                        ChangeToFriendlyIfPossible(relations, them);
                        ChangeToHostileIfPossible(relations, them);
                        break;
                    case Posture.Hostile when relations.ActiveWar != null:
                        relations.RequestPeace(OwnerEmpire);
                        RequestHelpFromAllies(relations, them);
                        break;
                    case Posture.Hostile:
                        relations.AssessDiplomaticAnger(OwnerEmpire);
                        ChangeToNeutralIfPossible(relations, them);
                        break;
                }
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

                switch (relations.Posture)
                {
                    case Posture.Friendly:
                        relations.OfferTrade(OwnerEmpire);
                        relations.OfferNonAggression(OwnerEmpire);
                        // todo Trade Tech
                        ChangeToNeutralIfPossible(relations, them);
                        break;
                    case Posture.Neutral:
                        relations.AssessDiplomaticAnger(OwnerEmpire);
                        if (!them.IsRuthless)
                            relations.OfferTrade(OwnerEmpire);

                        ChangeToFriendlyIfPossible(relations, them);
                        ChangeToHostileIfPossible(relations, them);
                        relations.ReferToMilitary(OwnerEmpire, threatForInsult: -20, compliment: false);
                        break;
                    case Posture.Hostile when relations.ActiveWar != null:
                        RequestHelpFromAllies(relations, them);
                        break;
                    case
                        Posture.Hostile:
                        relations.ReferToMilitary(OwnerEmpire, threatForInsult: -15, compliment: false);
                        relations.AssessDiplomaticAnger(OwnerEmpire);
                        ChangeToNeutralIfPossible(relations, them);
                        break;
                }

                if (!relations.AtWar && TheyArePotentialTargetRuthless(relations, them))
                    potentialTargets.Add(them);
            }

            PrepareToAttackClosest(potentialTargets);
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

                relations.AssessDiplomaticAnger(OwnerEmpire);
                switch (relations.Posture)
                {
                    case Posture.Friendly:
                        relations.OfferTrade(OwnerEmpire);
                        ChangeToNeutralIfPossible(relations, them);
                        break;
                    case Posture.Neutral:
                        DemandTech(relations, them);
                        ChangeToFriendlyIfPossible(relations, them);
                        ChangeToHostileIfPossible(relations, them);
                        //if (relations.HaveRejectedDemandTech)
                        //    relations.ChangeToHostile();

                        break; ;
                    case Posture.Hostile when relations.ActiveWar != null:
                        relations.RequestPeace(OwnerEmpire, onlyBadly: true);
                        break;
                    case Posture.Hostile:
                        ChangeToNeutralIfPossible(relations, them);
                        break;
                }
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
                if (!relationship.Key.data.Defeated 
                    && (relationship.Value.AtWar || relationship.Value.PreparingForWar))
                {
                    enemyStr += (int)relationship.Key.CurrentMilitaryStrength;
                }
            }

            return enemyStr;
        }

        bool TheyArePotentialTargetRuthless(Relationship relations, Empire them)
        {
            if (relations.Threat < 0f
                && relations.TurnsKnown > SecondDemand
                && !relations.Treaty_Alliance)
            {
                return false;
            }

            if (relations.Threat < -40f)
                return true;

            if (ExpansionAI.DesiredPlanets.Any(p => p.Owner == them))
                return true;

            return false;
        }

        bool TheyArePotentialTargetAggressive(Relationship relations, Empire them)
        {
            if (relations.ActiveWar != null)
                return false;

            if (relations.Threat < -15f && relations.TurnsKnown > SecondDemand && !relations.Treaty_Alliance)
            {
                if (relations.TotalAnger > 75f || ExpansionAI.DesiredPlanets.Any(p => p.Owner == them))
                    return true;
            }
            else if (relations.Threat <= -45f && relations.TotalAnger > 20f)
            {
                return true;
            }

            return false;
        }

        void PrepareToAttackClosest(Array<Empire> potentialTargets)
        {
            if (potentialTargets.Count > 0 && TotalEnemiesStrength() < OwnerEmpire.CurrentMilitaryStrength)
            {
                Empire closest = potentialTargets.Sorted(e => e.GetWeightedCenter().Distance(OwnerEmpire.GetWeightedCenter())).First();
                OwnerEmpire.GetRelations(closest).PreparingForWar     = true;
                OwnerEmpire.GetRelations(closest).PreparingForWarType = WarType.ImperialistWar;
            }
        }

        void PrepareToAttackWeakest(Array<Empire> potentialTargets)
        {
            if (potentialTargets.Count > 0 && TotalEnemiesStrength() * 2 < OwnerEmpire.CurrentMilitaryStrength)
            {
                Empire weakest = potentialTargets.Sorted(e => e.CurrentMilitaryStrength).First();
                OwnerEmpire.GetRelations(weakest).PreparingForWar     = true;
                OwnerEmpire.GetRelations(weakest).PreparingForWarType = WarType.ImperialistWar;
            }
        }

        void DemandTech(Relationship relations, Empire them)
        {
            if (relations.TurnsKnown < FirstDemand
                || relations.Treaty_NAPact
                || relations.HaveRejectedDemandTech
                || relations.XenoDemandedTech)
            {
                return;
            }

            Array<TechEntry> potentialDemands = them.GetEmpireAI().TradableTechs(OwnerEmpire);
            if (potentialDemands.Count == 0) 
                return;

            TechEntry techToDemand = potentialDemands.RandItem();
            Offer demandTech       = new Offer();

            demandTech.TechnologiesOffered.AddUnique(techToDemand.UID);
            relations.XenoDemandedTech = true;
            Offer theirDemand = new Offer
            {
                AcceptDL      = "Xeno Demand Tech Accepted",
                RejectDL      = "Xeno Demand Tech Rejected",
                ValueToModify = new Ref<bool>(() => relations.HaveRejectedDemandTech,
                                               x => relations.HaveRejectedDemandTech = x)
            };

            if (them == Player)
                DiplomacyScreen.Show(OwnerEmpire, "Xeno Demand Tech", demandTech, theirDemand);
            else
                them.GetEmpireAI().AnalyzeOffer(theirDemand, demandTech, OwnerEmpire, Offer.Attitude.Threaten);

            relations.turnsSinceLastContact = 0;
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

                relations.AssessDiplomaticAnger(OwnerEmpire);
                relations.ReferToMilitary(OwnerEmpire, threatForInsult: 0);
                switch (relations.Posture)
                {
                    case Posture.Friendly:
                        relations.OfferTrade(OwnerEmpire);
                        relations.OfferNonAggression(OwnerEmpire);
                        relations.OfferOpenBorders(OwnerEmpire);
                        relations.OfferAlliance(OwnerEmpire);
                        ChangeToNeutralIfPossible(relations, them);
                        break;
                    case Posture.Neutral:
                        relations.OfferTrade(OwnerEmpire);
                        ChangeToFriendlyIfPossible(relations, them);
                        ChangeToHostileIfPossible(relations, them);
                        break;
                    case Posture.Hostile when relations.ActiveWar != null:
                        relations.RequestPeace(OwnerEmpire, onlyBadly: true);
                        RequestHelpFromAllies(relations, them);
                        break;
                    case Posture.Hostile:
                        if (TheyArePotentialTargetAggressive(relations, them))
                            potentialTargets.Add(them);

                        break;
                }
            }

            PrepareToAttackWeakest(potentialTargets);
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