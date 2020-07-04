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
                case PersonalityType.Honorable:  DoHonorableRelations();  break;
                case PersonalityType.Pacifist:   DoPacifistRelations();   break;
                case PersonalityType.Aggressive: DoAggressiveRelations(); break;
                case PersonalityType.Xenophobic: DoXenophobicRelations(); break;
                case PersonalityType.Ruthless:   DoRuthlessRelations();   break;
            }

            foreach (KeyValuePair<Empire, Relationship> relationship in OwnerEmpire.AllRelations)
            {
                if (!relationship.Key.isFaction && !OwnerEmpire.isFaction && !relationship.Key.data.Defeated)
                    RunEventChecker(relationship);
            }
        }

        void OfferTrade(Relationship usToThem, Empire them, float availableTrust)
        {
            if (usToThem.TurnsKnown < SecondDemand
                || usToThem.Treaty_Trade
                || usToThem.HaveRejected_TRADE
                || availableTrust <= OwnerEmpire.data.DiplomaticPersonality.Trade
                || usToThem.turnsSinceLastContact < SecondDemand)
            {
                return;
            }

            Offer offer1 = new Offer
            {
                TradeTreaty   = true,
                AcceptDL      = "Trade Accepted",
                RejectDL      = "Trade Rejected",
                ValueToModify = new Ref<bool>(() => usToThem.HaveRejected_TRADE,
                                               x => usToThem.HaveRejected_TRADE = x)
            };

            Offer offer2 = new Offer { TradeTreaty = true };
            if (them == Player)
                DiplomacyScreen.Show(OwnerEmpire, "Offer Trade", offer2, offer1);
            else
                them.GetEmpireAI().AnalyzeOffer(offer2, offer1, OwnerEmpire, Offer.Attitude.Respectful);

            usToThem.turnsSinceLastContact = 0;
        }

        void OfferOpenBorders(Relationship usToThem, Empire them, float availableTrust)
        {
            if (usToThem.turnsSinceLastContact < SecondDemand 
                || usToThem.HaveRejected_OpenBorders
                || !usToThem.Treaty_NAPact
                || !usToThem.Treaty_Trade
                || usToThem.Treaty_OpenBorders
                || availableTrust < OwnerEmpire.data.DiplomaticPersonality.Territorialism / 2f)
            {
                return;
            }

            float territorialism = OwnerEmpire.data.DiplomaticPersonality.Territorialism;
            if (usToThem.Trust < 20f
                || usToThem.Anger_TerritorialConflict + usToThem.Anger_FromShipsInOurBorders < 0.75f * territorialism
                || availableTrust < territorialism / 2f)
            {
                return;
            }

            bool friendlyOpen = usToThem.Trust > 50f;
            Offer openBordersOffer = new Offer
            {
                OpenBorders   = true,
                AcceptDL      = "Open Borders Accepted",
                RejectDL      = friendlyOpen ? "Open Borders Friends Rejected" : "Open Borders Rejected",
                ValueToModify = new Ref<bool>(() => usToThem.HaveRejected_OpenBorders,
                                               x => usToThem.HaveRejected_OpenBorders = x)
            };

            Offer ourOffer = new Offer { OpenBorders = true };
            if (them.isPlayer)
                DiplomacyScreen.Show(OwnerEmpire, friendlyOpen ? "Offer Open Borders Friends" : "Offer Open Borders", ourOffer, openBordersOffer);
            else
                them.GetEmpireAI().AnalyzeOffer(ourOffer, openBordersOffer, OwnerEmpire, Offer.Attitude.Pleading);

            usToThem.turnsSinceLastContact = 0;
        }

        void OfferAlliance(Relationship usToThem, Empire them)
        {
            if (usToThem.TurnsAbove95 < 100
                || usToThem.turnsSinceLastContact < FirstDemand
                || usToThem.Treaty_Alliance
                || !usToThem.Treaty_Trade
                || !usToThem.Treaty_NAPact
                || usToThem.HaveRejected_Alliance
                || usToThem.TotalAnger >= 20)
            {
                return;
            }

            Offer offer1 = new Offer
            {
                Alliance      = true,
                AcceptDL      = "ALLIANCE_ACCEPTED",
                RejectDL      = "ALLIANCE_REJECTED",
                ValueToModify = new Ref<bool>(() => usToThem.HaveRejected_Alliance,
                    x =>
                    {
                        usToThem.HaveRejected_Alliance = x;
                        SetAlliance(!usToThem.HaveRejected_Alliance);
                    })
            };

            Offer offer2 = new Offer();
            if (them == Empire.Universe.PlayerEmpire)
                DiplomacyScreen.Show(OwnerEmpire, "OFFER_ALLIANCE", offer2, offer1);
            else
                them.GetEmpireAI().AnalyzeOffer(offer2, offer1, OwnerEmpire, Offer.Attitude.Respectful);

            usToThem.turnsSinceLastContact = 0;
        }

        void OfferNonAggression(Relationship usToThem, Empire them, float availableTrust)
        {
            if (usToThem.TurnsKnown < FirstDemand
                || usToThem.Treaty_NAPact
                || availableTrust <= OwnerEmpire.data.DiplomaticPersonality.NAPact
                || usToThem.HaveRejectedNapact
                || usToThem.turnsSinceLastContact < SecondDemand)
            {
                return;
            }

            Offer offer1 = new Offer
            {
                NAPact        = true,
                AcceptDL      = "NAPact Accepted",
                RejectDL      = "NAPact Rejected",
                ValueToModify = new Ref<bool>(() => usToThem.HaveRejectedNapact,
                                               x => usToThem.HaveRejectedNapact = x)
            };

            Offer offer2 = new Offer { NAPact = true };
            if (them == Empire.Universe.PlayerEmpire)
                DiplomacyScreen.Show(OwnerEmpire, "Offer NAPact", offer2, offer1);
            else
                them.GetEmpireAI().AnalyzeOffer(offer2, offer1, OwnerEmpire, Offer.Attitude.Respectful);

            usToThem.turnsSinceLastContact = 0;
        }

        void OfferPeace(Relationship usToThem, Empire them, bool onlyBadly = false) 
        {
            if ((usToThem.ActiveWar.TurnsAtWar % 100).NotZero( )) 
                return;

            WarState warState = WarState.NotApplicable;
            switch (usToThem.ActiveWar.WarType)
            {
                case WarType.BorderConflict:
                    if (usToThem.Anger_FromShipsInOurBorders +
                        usToThem.Anger_TerritorialConflict >
                        OwnerEmpire.data.DiplomaticPersonality.Territorialism)
                    {
                        return;
                    }

                    warState = usToThem.ActiveWar.GetBorderConflictState();
                    if (CheckLosingBadly())
                        break;

                    switch (warState)
                    {
                        case WarState.LosingSlightly:
                        case WarState.LosingBadly:     OfferPeace(usToThem, them, "OFFERPEACE_LOSINGBC");  break;
                        case WarState.WinningSlightly: OfferPeace(usToThem, them, "OFFERPEACE_FAIR");      break;
                        case WarState.Dominating:      OfferPeace(usToThem, them, "OFFERPEACE_WINNINGBC"); break;
                    }

                    break;
                case WarType.ImperialistWar:
                    warState = usToThem.ActiveWar.GetWarScoreState();
                    if (CheckLosingBadly())
                        break;

                    switch (warState)
                    {
                        case WarState.LosingSlightly:
                        case WarState.LosingBadly:     OfferPeace(usToThem, them, "OFFERPEACE_PLEADING");       break;
                        case WarState.WinningSlightly: OfferPeace(usToThem, them, "OFFERPEACE_FAIR");           break;
                        case WarState.Dominating:      OfferPeace(usToThem, them, "OFFERPEACE_FAIR_WINNING");   break;
                        case WarState.EvenlyMatched:   OfferPeace(usToThem, them, "OFFERPEACE_EVENLY_MATCHED"); break;
                    }

                    break;
                case WarType.DefensiveWar:
                    warState = usToThem.ActiveWar.GetBorderConflictState();
                    if (CheckLosingBadly())
                        break;

                    switch (warState)
                    {
                        case WarState.LosingSlightly:
                        case WarState.LosingBadly:     OfferPeace(usToThem, them, "OFFERPEACE_PLEADING");       break;
                        case WarState.WinningSlightly: OfferPeace(usToThem, them, "OFFERPEACE_FAIR");           break;
                        case WarState.Dominating:      OfferPeace(usToThem, them, "OFFERPEACE_FAIR_WINNING");   break;
                        case WarState.EvenlyMatched:   OfferPeace(usToThem, them, "OFFERPEACE_EVENLY_MATCHED"); break;
                    }

                    break;
            }

            usToThem.turnsSinceLastContact = 0;

            bool CheckLosingBadly()
            {
                if (!onlyBadly) 
                    return false;

                if (warState == WarState.LosingBadly)
                    OfferPeace(usToThem, them, "OFFERPEACE_PLEADING");

                return true;
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

        void DoPacifistRelations()
        {
            AssessTerritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 50f);
            foreach (KeyValuePair<Empire, Relationship> kv in OwnerEmpire.AllRelations)
            {
                Relationship relations = kv.Value;
                Empire them            = kv.Key;
                if (DoNotInteract(relations, them))
                    continue;

                switch (relations.Posture)
                {
                    case Posture.Friendly:
                        OfferTrade(relations, them, relations.Trust - relations.TrustUsed);
                        OfferNonAggression(relations, them, relations.Trust - relations.TrustUsed);
                        OfferOpenBorders(relations, them, relations.Trust - relations.TrustUsed);
                        OfferAlliance(relations, them);
                        break;
                    case Posture.Neutral:
                        AssessDiplomaticAnger(relations, them);
                        OfferNonAggression(relations, them, relations.Trust - relations.TrustUsed);
                        if (relations.TurnsKnown > FirstDemand && relations.Treaty_NAPact)
                            relations.ChangeToFriendly();

                        if (relations.Trust > 50f && relations.TotalAnger < 10)
                            relations.ChangeToFriendly();

                        break;
                    case Posture.Hostile when relations.ActiveWar != null:
                        OfferPeace(relations, them);
                        RequestHelpFromAllies(relations, them);
                        break;
                    case Posture.Hostile:
                        AssessDiplomaticAnger(relations, them);
                        break;
                }
            }
        }

        private void DoRuthlessRelations()
        {
            AssessTerritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 5f);
            Array<Empire> potentialTargets = new Array<Empire>();

            foreach (KeyValuePair<Empire, Relationship> kv in OwnerEmpire.AllRelations)
            {
                Relationship relations = kv.Value;
                Empire them            = kv.Key;
                if (DoNotInteract(relations, them))
                    continue;

                if (!them.IsRuthless)
                    OfferTrade(relations, them, relations.Trust - relations.TrustUsed);

                AssessDiplomaticAnger(relations, them);
                relations.ChangeToHostile();
                if (relations.AtWar)
                    continue;

                ReferToMilitary(relations, them, threatForInsult: -15, compliment: false);

                if (TheyArePotentialTargetRuthless(relations, them))
                    potentialTargets.Add(them);
            }

            PrepareToAttackClosest(potentialTargets);
        }

        private void DoXenophobicRelations()
        {
            AssessTerritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 10f);
            foreach (KeyValuePair<Empire, Relationship> kv in OwnerEmpire.AllRelations)
            {
                Relationship relations = kv.Value;
                Empire them            = kv.Key;
                if (DoNotInteract(relations, them))
                    continue;

                AssessDiplomaticAnger(relations, them);
                switch (relations.Posture)
                {
                    case Posture.Friendly:
                        OfferTrade(relations, them, relations.Trust - relations.TrustUsed);
                        break;
                    case Posture.Neutral:
                        DemandTech(relations, them);
                        if (relations.HaveRejectedDemandTech)
                            relations.ChangeToHostile();

                        break; ;
                    case Posture.Hostile when relations.ActiveWar != null:
                        OfferPeace(relations, them, onlyBadly: true);
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

        void ReferToMilitary(Relationship usToThem, Empire them, float threatForInsult, bool compliment = true)
        {
            if (usToThem.Threat <= threatForInsult)
            {
                if (!usToThem.HaveInsulted_Military && usToThem.TurnsKnown > FirstDemand)
                {
                    usToThem.HaveInsulted_Military = true;
                    if (them.isPlayer)
                        DiplomacyScreen.Show(OwnerEmpire, "Insult Military");
                }

                if (OwnerEmpire.IsAggressive)
                    usToThem.ChangeToHostile();
            }
            else if (compliment && usToThem.Threat > 25f && usToThem.TurnsKnown > FirstDemand)
            {
                if (!usToThem.HaveComplimented_Military && usToThem.HaveInsulted_Military &&
                    usToThem.TurnsKnown > FirstDemand && them.isPlayer)
                {
                    usToThem.HaveComplimented_Military = true;
                    if (!usToThem.HaveInsulted_Military || usToThem.TurnsKnown <= SecondDemand)
                        DiplomacyScreen.Show(OwnerEmpire, "Compliment Military");
                    else
                        DiplomacyScreen.Show(OwnerEmpire, "Compliment Military Better");
                }

                if (OwnerEmpire.IsAggressive)
                    usToThem.ChangeToFriendly();
            }
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

        private void DoAggressiveRelations()
        {
            Array<Empire> potentialTargets = new Array<Empire>();

            AssessTerritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 10f);
            foreach (KeyValuePair<Empire, Relationship> kv in OwnerEmpire.AllRelations)
            {
                Relationship relations = kv.Value;
                Empire them            = kv.Key;
                if (DoNotInteract(relations, them))
                    continue;

                relations.ChangeToNeutral();
                ReferToMilitary(relations, them, threatForInsult: 0);
                switch (relations.Posture)
                {
                    case Posture.Friendly:
                        OfferTrade(relations, them, relations.Trust - relations.TrustUsed);
                        OfferNonAggression(relations, them, relations.Trust - relations.TrustUsed);
                        OfferOpenBorders(relations, them, relations.Trust - relations.TrustUsed);
                        OfferAlliance(relations, them);
                        break;
                    case Posture.Neutral:
                        AssessDiplomaticAnger(relations, them);
                        break;
                    case Posture.Hostile when relations.ActiveWar != null:
                        OfferPeace(relations, them, onlyBadly: true);
                        RequestHelpFromAllies(relations, them);
                        break;
                    case Posture.Hostile:
                        AssessDiplomaticAnger(relations, them);
                        if (TheyArePotentialTargetAggressive(relations, them))
                            potentialTargets.Add(them);

                        break;
                }
            }

            PrepareToAttackWeakest(potentialTargets);
        }

        private void DoHonorableRelations()
        {
            AssessTerritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 10f);
            foreach (KeyValuePair<Empire, Relationship> kv in OwnerEmpire.AllRelations)
            {
                Relationship relations = kv.Value;
                Empire them            = kv.Key;

                if (DoNotInteract(relations, them))
                    continue;

                switch (relations.Posture)
                {
                    case Posture.Friendly:
                        OfferTrade(relations, them, relations.Trust - relations.TrustUsed);
                        OfferNonAggression(relations, them, relations.Trust - relations.TrustUsed);
                        OfferOpenBorders(relations, them, relations.Trust - relations.TrustUsed);
                        OfferAlliance(relations, them);
                        break;
                    case Posture.Neutral:
                        AssessDiplomaticAnger(relations, them);
                        OfferNonAggression(relations, them, relations.Trust - relations.TrustUsed);
                        if (relations.TurnsKnown > FirstDemand && relations.Treaty_NAPact)
                            relations.ChangeToFriendly();

                        break;
                    case Posture.Hostile when relations.ActiveWar != null:
                        OfferPeace(relations, them);
                        RequestHelpFromAllies(relations, them);
                        break;

                    case Posture.Hostile:
                        AssessDiplomaticAnger(relations, them);
                        break;
                }
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