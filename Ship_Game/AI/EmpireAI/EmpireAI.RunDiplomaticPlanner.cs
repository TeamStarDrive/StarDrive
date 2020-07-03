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
                || availableTrust <= OwnerEmpire.data.DiplomaticPersonality.NAPact)
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

        void OfferPeace(Relationship usToThem, Empire them) // ToDo  what about surrender and federation?
        {
            if ((usToThem.ActiveWar.TurnsAtWar % 100).NotZero( )) 
                return;

            switch (usToThem.ActiveWar.WarType)
            {
                case WarType.BorderConflict:
                    if (usToThem.Anger_FromShipsInOurBorders +
                        usToThem.Anger_TerritorialConflict >
                        OwnerEmpire.data.DiplomaticPersonality.Territorialism)
                    {
                        return;
                    }

                    switch (usToThem.ActiveWar.GetBorderConflictState())
                    {
                        case WarState.LosingSlightly:
                        case WarState.LosingBadly:     OfferPeace(usToThem, them, "OFFERPEACE_LOSINGBC");  break;
                        case WarState.WinningSlightly: OfferPeace(usToThem, them, "OFFERPEACE_FAIR");      break;
                        case WarState.Dominating:      OfferPeace(usToThem, them, "OFFERPEACE_WINNINGBC"); break;
                    }

                    break;
                case WarType.ImperialistWar:
                    switch (usToThem.ActiveWar.GetWarScoreState())
                    {
                        case WarState.LosingSlightly:
                        case WarState.LosingBadly:     OfferPeace(usToThem, them, "OFFERPEACE_PLEADING");       break;
                        case WarState.WinningSlightly: OfferPeace(usToThem, them, "OFFERPEACE_FAIR");           break;
                        case WarState.Dominating:      OfferPeace(usToThem, them, "OFFERPEACE_FAIR_WINNING");   break;
                        case WarState.EvenlyMatched:   OfferPeace(usToThem, them, "OFFERPEACE_EVENLY_MATCHED"); break;
                    }

                    break;
                case WarType.DefensiveWar:
                    switch (usToThem.ActiveWar.GetBorderConflictState())
                    {
                        case WarState.LosingSlightly:
                        case WarState.LosingBadly:     OfferPeace(usToThem, them, "OFFERPEACE_PLEADING");       break;
                        case WarState.WinningSlightly: OfferPeace(usToThem, them, "OFFERPEACE_FAIR");           break;
                        case WarState.Dominating:      OfferPeace(usToThem, them, "OFFERPEACE_FAIR_WINNING");   break;
                        case WarState.EvenlyMatched:   OfferPeace(usToThem, them, "OFFERPEACE_EVENLY_MATCHED"); break;
                    }

                    break;
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

                float usedTrust = relations.TrustEntries.Sum(te => te.TrustCost);

                switch (relations.Posture)
                {
                    case Posture.Friendly:
                        AssessAngerPacifist(kv, relations.Posture, usedTrust); // TODO refactor anger
                        OfferTrade(relations, them, relations.Trust - usedTrust);
                        OfferAlliance(relations, them);
                        break;
                    case Posture.Neutral:
                        AssessAngerPacifist(kv, relations.Posture, usedTrust);
                        OfferNonAggression(relations, them, relations.Trust - usedTrust);
                        if (relations.TurnsKnown > FirstDemand && relations.Treaty_NAPact)
                            relations.ChangeToFriendly();

                        if (relations.Trust > 50f && relations.TotalAnger < 10)
                            relations.ChangeToFriendly();

                        break;
                    case Posture.Hostile when relations.ActiveWar != null:
                        OfferPeace(relations, them);
                        RequestHelpFromAllies(relations, them);
                        break;
                    case Posture.Hostile:  // todo  review if this is needed.
                        AssessAngerPacifist(kv, Posture.Hostile, 100f);
                        break;
                }
            }
        }

        private void DoRuthlessRelations()
        {
            AssessTerritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 5f);
            int numberofWars = 0;
            Array<Empire> potentialTargets = new Array<Empire>();
            foreach (KeyValuePair<Empire, Relationship> relationship in OwnerEmpire.AllRelations)
            {
                if (!relationship.Value.AtWar || relationship.Key.data.Defeated)
                    continue;

                numberofWars += (int) relationship.Key.CurrentMilitaryStrength * 2; //++;
            }
            //Label0:
            foreach (KeyValuePair<Empire, Relationship> kv in OwnerEmpire.AllRelations)
            {
                Relationship relations = kv.Value;
                Empire them            = kv.Key;
                if (!relations.Known || them.isFaction || them.data.Defeated)
                    continue;

                if (them.data.DiplomaticPersonality != null && !relations.HaveRejected_TRADE &&
                    !relations.Treaty_Trade && !relations.AtWar &&
                    (!them.IsAggressive || !them.IsRuthless))
                {
                    Offer NAPactOffer = new Offer
                    {
                        TradeTreaty = true,
                        AcceptDL = "Trade Accepted",
                        RejectDL = "Trade Rejected"
                    };
                    Relationship value = relations;
                    NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_TRADE,
                        x => value.HaveRejected_TRADE = x);
                    Offer OurOffer = new Offer
                    {
                        TradeTreaty = true
                    };
                    them.GetEmpireAI()
                        .AnalyzeOffer(OurOffer, NAPactOffer, OwnerEmpire, Offer.Attitude.Respectful);
                }
                float usedTrust = relations.TrustEntries.Sum(te => te.TrustCost);
                AssessAngerAggressive(kv, relations.Posture, usedTrust);
                relations.Posture = Posture.Hostile;
                if (!relations.Known || relations.AtWar)
                {
                    continue;
                }
                relations.Posture = Posture.Hostile;
                if (them == Empire.Universe.PlayerEmpire && relations.Threat <= -15f &&
                    !relations.HaveInsulted_Military && relations.TurnsKnown > FirstDemand)
                {
                    relations.HaveInsulted_Military = true;
                    DiplomacyScreen.Show(OwnerEmpire, "Insult Military");
                }
                if (relations.Threat > 0f || relations.TurnsKnown <= SecondDemand ||
                    relations.Treaty_Alliance)
                {
                    if (relations.Threat > -45f || numberofWars > OwnerEmpire.CurrentMilitaryStrength) //!= 0)
                    {
                        continue;
                    }
                    potentialTargets.Add(them);
                }
                else
                {
                    int i = 0;
                    while (i < 5)
                    {
                        if (i >= ExpansionAI.DesiredPlanets.Length)
                        {
                            //goto Label0;    //this tried to restart the loop it's in => bad mojo
                            break;
                        }
                        if (ExpansionAI.DesiredPlanets[i].Owner != them)
                        {
                            i++;
                        }
                        else
                        {
                            potentialTargets.Add(them);
                            //goto Label0;
                            break;
                        }
                    }                    

                }
            }
            if (potentialTargets.Count > 0 && numberofWars <= OwnerEmpire.CurrentMilitaryStrength) //1)
            {
                IOrderedEnumerable<Empire> sortedList =
                    from target in potentialTargets
                    orderby Vector2.Distance(OwnerEmpire.GetWeightedCenter(), target.GetWeightedCenter())
                    select target;
                bool foundwar = false;
                foreach (Empire e in potentialTargets)
                {
                    Empire ToAttack = e;
                    if (OwnerEmpire.GetRelations(e).Treaty_NAPact)
                    {
                        continue;
                    }
                    OwnerEmpire.GetRelations(ToAttack).PreparingForWar = true;
                    foundwar = true;
                }
                if (!foundwar)
                {
                    Empire ToAttack = sortedList.First();
                    OwnerEmpire.GetRelations(ToAttack).PreparingForWar = true;
                }
            }
        }

        private void DoXenophobicRelations()
        {
            AssessTerritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 10f);
            foreach (KeyValuePair<Empire, Relationship> kv in OwnerEmpire.AllRelations)
            {
                Relationship relations = kv.Value;
                Empire them            = kv.Key;
                if (!relations.Known || them.isFaction || them.data.Defeated)
                    continue;

                float usedTrust = relations.TrustEntries.Sum(te => te.TrustCost);
                AssessDiplomaticAnger(kv);
                switch (relations.Posture)
                {
                    case Posture.Friendly:
                    {
                        if (relations.TurnsKnown <= SecondDemand ||
                            relations.Trust - usedTrust <= OwnerEmpire.data.DiplomaticPersonality.Trade ||
                            relations.Treaty_Trade || relations.HaveRejected_TRADE ||
                            relations.turnsSinceLastContact <= SecondDemand ||
                            relations.HaveRejected_TRADE)
                        {
                            continue;
                        }
                        Offer NAPactOffer = new Offer
                        {
                            TradeTreaty = true,
                            AcceptDL = "Trade Accepted",
                            RejectDL = "Trade Rejected"
                        };
                        Relationship value = relations;
                        NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_TRADE,
                            x => value.HaveRejected_TRADE = x);
                        Offer OurOffer = new Offer
                        {
                            TradeTreaty = true
                        };
                        if (them == Empire.Universe.PlayerEmpire)
                        {
                            DiplomacyScreen.Show(OwnerEmpire, "Offer Trade", OurOffer, NAPactOffer);
                        }
                        else
                        {
                            them.GetEmpireAI()
                                .AnalyzeOffer(OurOffer, NAPactOffer, OwnerEmpire, Offer.Attitude.Respectful);
                        }
                        continue;
                    }
                    case Posture.Neutral:
                    {
                        if (relations.TurnsKnown >= FirstDemand && !relations.Treaty_NAPact &&
                            !relations.HaveRejectedDemandTech && !relations.XenoDemandedTech)
                        {
                                var empire = them;
                            Array<TechEntry> potentialDemands = empire.GetEmpireAI().TradableTechs(OwnerEmpire);
                            if (potentialDemands.Count > 0)
                            {
                                int Random = (int) RandomMath.RandomBetween(0f, potentialDemands.Count + 0.75f);
                                if (Random > potentialDemands.Count - 1)
                                {
                                    Random = potentialDemands.Count - 1;
                                }
                                TechEntry TechToDemand = potentialDemands[Random];
                                Offer DemandTech = new Offer();
                                DemandTech.TechnologiesOffered.AddUnique(TechToDemand.UID);
                                relations.XenoDemandedTech = true;
                                Offer TheirDemand = new Offer
                                {
                                    AcceptDL = "Xeno Demand Tech Accepted",
                                    RejectDL = "Xeno Demand Tech Rejected"
                                };
                                Relationship relationship = relations;
                                TheirDemand.ValueToModify = new Ref<bool>(() => relationship.HaveRejectedDemandTech,
                                    x => relationship.HaveRejectedDemandTech = x);
                                relations.turnsSinceLastContact = 0;

                                if (them == Empire.Universe.PlayerEmpire)
                                {
                                    DiplomacyScreen.Show(OwnerEmpire, "Xeno Demand Tech", DemandTech, TheirDemand);
                                }
                                else
                                {
                                    them.GetEmpireAI()
                                        .AnalyzeOffer(DemandTech, TheirDemand, OwnerEmpire, Offer.Attitude.Threaten);
                                }
                            }
                        }
                        if (!relations.HaveRejectedDemandTech)
                        {
                            continue;
                        }
                        relations.Posture = Posture.Hostile;
                        continue;
                    }
                    default:
                    {
                        continue;
                    }
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

        void ReferToMilitary(Relationship usToThem, Empire them)
        {
            if (usToThem.Threat <= 0f)
            {
                if (!usToThem.HaveInsulted_Military && usToThem.TurnsKnown > FirstDemand)
                {
                    usToThem.HaveInsulted_Military = true;
                    if (them == Empire.Universe.PlayerEmpire)
                        DiplomacyScreen.Show(OwnerEmpire, "Insult Military");
                }
                usToThem.Posture = Posture.Hostile;
            }
            else if (usToThem.Threat > 25f && usToThem.TurnsKnown > FirstDemand)
            {
                if (!usToThem.HaveComplimented_Military && usToThem.HaveInsulted_Military &&
                    usToThem.TurnsKnown > FirstDemand &&
                    them == Empire.Universe.PlayerEmpire)
                {
                    usToThem.HaveComplimented_Military = true;
                    if (!usToThem.HaveInsulted_Military || usToThem.TurnsKnown <= SecondDemand)
                        DiplomacyScreen.Show(OwnerEmpire, "Compliment Military");
                    else
                        DiplomacyScreen.Show(OwnerEmpire, "Compliment Military Better");
                }

                usToThem.Posture = Posture.Friendly;
            }
        }

        bool TheyArePotentialTarget(Relationship relations, Empire them)
        {
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

        void PrepareToAttack(Array<Empire> potentialTargets)
        {
            if (potentialTargets.Count > 0 && TotalEnemiesStrength() * 2 < OwnerEmpire.CurrentMilitaryStrength)
            {
                Empire weakest = potentialTargets.Sorted(e => e.CurrentMilitaryStrength).First();
                OwnerEmpire.GetRelations(weakest).PreparingForWar = true;
            }
        }

        private void DoAggressiveRelations()
        {
            Array<Empire> potentialTargets = new Array<Empire>();

            AssessTerritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 10f);
            foreach (KeyValuePair<Empire, Relationship> kv in OwnerEmpire.AllRelations)
            {
                Relationship relations = kv.Value;
                Empire them            = kv.Key;
                if (relations.AtWar || DoNotInteract(relations, them))
                    continue;

                float usedTrust = relations.TrustEntries.Sum(te => te.TrustCost);
                if (!them.IsAggressive)
                    OfferTrade(relations, them, relations.Trust - usedTrust); // trade in friendly might not be reached

                relations.ChangeToNeutral();
                ReferToMilitary(relations, them);
                switch (relations.Posture)
                {
                    case Posture.Friendly:
                        AssessAngerAggressive(kv, relations.Posture, usedTrust); // ToDo - review this
                        OfferTrade(relations, them, relations.Trust - usedTrust);
                        OfferAlliance(relations, them);
                        break;
                    case Posture.Neutral:
                        AssessAngerAggressive(kv, relations.Posture, usedTrust);
                        break;
                    case Posture.Hostile:
                        AssessAngerAggressive(kv, relations.Posture, usedTrust);
                        if (TheyArePotentialTarget(relations, them))
                            potentialTargets.Add(them);

                        break;
                }
            }

            PrepareToAttack(potentialTargets);
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

                float usedTrust = relations.TrustEntries.Sum(te => te.TrustCost);
                switch (relations.Posture)
                {
                    case Posture.Friendly:
                        AssessAngerPacifist(kv, Posture.Friendly, usedTrust);
                        OfferTrade(relations, them, relations.Trust - usedTrust);
                        OfferAlliance(relations, them);
                        break;
                    case Posture.Neutral:
                        AssessAngerPacifist(kv, Posture.Neutral, usedTrust);
                        OfferNonAggression(relations, them, relations.Trust - usedTrust);
                        if (relations.TurnsKnown > FirstDemand && relations.Treaty_NAPact)
                            relations.ChangeToFriendly();

                        break;
                    case Posture.Hostile when relations.ActiveWar != null:
                        OfferPeace(relations, them);
                        RequestHelpFromAllies(relations, them);
                        break;

                    case Posture.Hostile:
                        AssessAngerPacifist(kv, Posture.Hostile, 100f);
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