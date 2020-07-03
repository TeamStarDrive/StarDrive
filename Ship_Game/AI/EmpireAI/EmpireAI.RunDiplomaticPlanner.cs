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

        private void DoPacifistRelations()
        {
            AssessTerritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 50f);
            foreach (KeyValuePair<Empire, Relationship> kv in OwnerEmpire.AllRelations)
            {
                Relationship relations = kv.Value;
                Empire them            = kv.Key;
                if (!relations.Known || them.isFaction || them.data.Defeated) 
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
                        break; ;
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

        private void DoAggressiveRelations()
        {
            int numberOfWars = 0;
            Array<Empire> potentialTargets = new Array<Empire>();
            foreach (KeyValuePair<Empire, Relationship> relationship in OwnerEmpire.AllRelations)
            {
                if (relationship.Key.data.Defeated || !relationship.Value.AtWar && !relationship.Value.PreparingForWar)
                {
                    continue;
                }
                //numberofWars++;
                numberOfWars += (int) relationship.Key.CurrentMilitaryStrength;
            }
            AssessTerritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 10f);
            foreach (KeyValuePair<Empire, Relationship> kv in OwnerEmpire.AllRelations)
            {
                Relationship relations = kv.Value;
                Empire them            = kv.Key;
                if (!relations.Known || relations.AtWar || them.isFaction || them.data.Defeated)
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
                float usedTrust = 0f;
                foreach (TrustEntry te in relations.TrustEntries)
                {
                    usedTrust = usedTrust + te.TrustCost;
                }
                relations.Posture = Posture.Neutral;
                if (relations.Threat <= 0f)
                {
                    if (!relations.HaveInsulted_Military && relations.TurnsKnown > FirstDemand)
                    {
                        relations.HaveInsulted_Military = true;
                        if (them == Empire.Universe.PlayerEmpire)
                        {
                            DiplomacyScreen.Show(OwnerEmpire, "Insult Military");
                        }
                    }
                    relations.Posture = Posture.Hostile;
                }
                else if (relations.Threat > 25f && relations.TurnsKnown > FirstDemand)
                {
                    if (!relations.HaveComplimented_Military && relations.HaveInsulted_Military &&
                        relations.TurnsKnown > FirstDemand &&
                        them == Empire.Universe.PlayerEmpire)
                    {
                        relations.HaveComplimented_Military = true;
                        if (!relations.HaveInsulted_Military ||
                            relations.TurnsKnown <= SecondDemand)
                        {
                            DiplomacyScreen.Show(OwnerEmpire, "Compliment Military");
                        }
                        else
                        {
                            DiplomacyScreen.Show(OwnerEmpire, "Compliment Military Better");
                        }
                    }
                    relations.Posture = Posture.Friendly;
                }
                switch (relations.Posture)
                {
                    case Posture.Friendly:
                    {
                        if (relations.TurnsKnown > SecondDemand &&
                            relations.Trust - usedTrust >
                            OwnerEmpire.data.DiplomaticPersonality.Trade &&
                            !relations.HaveRejected_TRADE && !relations.Treaty_Trade)
                        {
                            Offer NAPactOffer = new Offer
                            {
                                TradeTreaty = true,
                                AcceptDL = "Trade Accepted",
                                RejectDL = "Trade Rejected"
                            };
                            Relationship relationship = relations;
                            NAPactOffer.ValueToModify = new Ref<bool>(() => relationship.HaveRejected_TRADE,
                                x => relationship.HaveRejected_TRADE = x);
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
                        }
                        AssessAngerAggressive(kv, relations.Posture, usedTrust);
                        if (relations.TurnsAbove95 <= 100 || relations.turnsSinceLastContact <= 10 ||
                            relations.Treaty_Alliance || !relations.Treaty_Trade ||
                            !relations.Treaty_NAPact || relations.HaveRejected_Alliance ||
                            relations.TotalAnger >= 20f)
                        {
                            continue;
                        }
                        Offer OfferAlliance = new Offer
                        {
                            Alliance = true,
                            AcceptDL = "ALLIANCE_ACCEPTED",
                            RejectDL = "ALLIANCE_REJECTED"
                        };
                        Relationship value1 = relations;
                        OfferAlliance.ValueToModify = new Ref<bool>(() => value1.HaveRejected_Alliance, x =>
                        {
                            value1.HaveRejected_Alliance = x;
                            SetAlliance(!value1.HaveRejected_Alliance);
                        });
                        Offer OurOffer0 = new Offer();
                        if (them == Empire.Universe.PlayerEmpire)
                        {
                            DiplomacyScreen.Show(OwnerEmpire, "OFFER_ALLIANCE", OurOffer0, OfferAlliance);
                        }
                        else
                        {
                            them.GetEmpireAI()
                                .AnalyzeOffer(OurOffer0, OfferAlliance, OwnerEmpire, Offer.Attitude.Respectful);
                        }

                        continue;
                    }
                    case Posture.Neutral:
                    {
                        AssessAngerAggressive(kv, relations.Posture, usedTrust);
                        continue;
                    }
                    case Posture.Hostile:
                    {
                        if (relations.Threat < -15f && relations.TurnsKnown > SecondDemand &&
                            !relations.Treaty_Alliance)
                        {
                            if (relations.TotalAnger < 75f)
                            {
                                int i = 0;
                                while (i < 5)
                                {
                                    if (i >= ExpansionAI.DesiredPlanets.Length)
                                    {
                                        break;
                                    }
                                    if (ExpansionAI.DesiredPlanets[i].Owner != them)
                                    {
                                        i++;
                                    }
                                    else
                                    {
                                        potentialTargets.Add(them);
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                potentialTargets.Add(them);
                            }
                        }
                        else if (relations.Threat <= -45f && relations.TotalAnger > 20f)
                        {
                            potentialTargets.Add(them);
                        }
                        //Label0:
                        AssessAngerAggressive(kv, relations.Posture, usedTrust);
                        continue;
                    }
                    default:
                    {
                        continue; //this doesn't actually do anything, since it's at the end of the loop anyways
                    }
                }
            }
            if (potentialTargets.Count > 0 && numberOfWars * 2 < OwnerEmpire.CurrentMilitaryStrength) //<= 1)
            {
                Empire ToAttack = potentialTargets.First();
                OwnerEmpire.GetRelations(ToAttack).PreparingForWar = true;
            }
        }

        private void DoHonorableRelations()
        {
            AssessTerritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 10f);
            foreach (KeyValuePair<Empire, Relationship> kv in OwnerEmpire.AllRelations)
            {
                Relationship relations = kv.Value;
                Empire them            = kv.Key;

                if (!relations.Known || them.isFaction || them.data.Defeated) 
                    continue;

                switch (relations.Posture)
                {
                    case Posture.Friendly:
                        float usedTrust1 = 0.0f;
                        foreach (TrustEntry trustEntry in relations.TrustEntries)
                            usedTrust1 += trustEntry.TrustCost;
                        if (relations.TurnsKnown > SecondDemand &&
                            relations.Trust - (double) usedTrust1 >
                            OwnerEmpire.data.DiplomaticPersonality.Trade &&
                            (relations.turnsSinceLastContact > SecondDemand &&
                             !relations.Treaty_Trade) && !relations.HaveRejected_TRADE)
                        {
                            Offer offer1 = new Offer();
                            offer1.TradeTreaty = true;
                            offer1.AcceptDL = "Trade Accepted";
                            offer1.RejectDL = "Trade Rejected";
                            Relationship r = relations;
                            offer1.ValueToModify =
                                new Ref<bool>(() => r.HaveRejected_TRADE, x => r.HaveRejected_TRADE = x);
                            Offer offer2 = new Offer();
                            offer2.TradeTreaty = true;
                            if (them == Empire.Universe.PlayerEmpire)
                            {
                                DiplomacyScreen.Show(OwnerEmpire, "Offer Trade", offer2, offer1);
                            }
                            else
                            {
                                them.GetEmpireAI()
                                    .AnalyzeOffer(offer2, offer1, OwnerEmpire, Offer.Attitude.Respectful);
                            }
                        }
                        AssessAngerPacifist(kv, Posture.Friendly, usedTrust1);
                        if (relations.TurnsAbove95 > 100 &&
                            relations.turnsSinceLastContact > 10 &&
                            (!relations.Treaty_Alliance && relations.Treaty_Trade) &&
                            (relations.Treaty_NAPact && !relations.HaveRejected_Alliance &&
                             relations.TotalAnger < 20.0))
                        {
                            Offer offer1 = new Offer();
                            offer1.Alliance = true;
                            offer1.AcceptDL = "ALLIANCE_ACCEPTED";
                            offer1.RejectDL = "ALLIANCE_REJECTED";
                            Relationship r = relations;
                            offer1.ValueToModify = new Ref<bool>(() => r.HaveRejected_Alliance,
                                x =>
                                {
                                    r.HaveRejected_Alliance = x;
                                    SetAlliance(!r.HaveRejected_Alliance);
                                });
                            Offer offer2 = new Offer();
                            if (them == Empire.Universe.PlayerEmpire)
                            {
                                DiplomacyScreen.Show(OwnerEmpire, "OFFER_ALLIANCE", offer2, offer1);
                            }
                            else
                            {
                                them.GetEmpireAI()
                                    .AnalyzeOffer(offer2, offer1, OwnerEmpire, Offer.Attitude.Respectful);
                            }
                            continue;
                        }
                        else
                            continue;
                    case Posture.Neutral:
                        if (relations.TurnsKnown == FirstDemand && !relations.Treaty_NAPact)
                        {
                            Offer offer1 = new Offer();
                            offer1.NAPact = true;
                            offer1.AcceptDL = "NAPact Accepted";
                            offer1.RejectDL = "NAPact Rejected";
                            Relationship r = relations;
                            offer1.ValueToModify = new Ref<bool>(() => r.HaveRejectedNapact,
                                x => r.HaveRejectedNapact = x);
                            relations.turnsSinceLastContact = 0;
                            Offer offer2 = new Offer();
                            offer2.NAPact = true;
                            if (them == Empire.Universe.PlayerEmpire)
                            {
                                DiplomacyScreen.Show(OwnerEmpire, "Offer NAPact", offer2, offer1);
                            }
                            else
                            {
                                them.GetEmpireAI()
                                    .AnalyzeOffer(offer2, offer1, OwnerEmpire, Offer.Attitude.Respectful);
                            }
                        }
                        if (relations.TurnsKnown > FirstDemand && relations.Treaty_NAPact)
                            relations.Posture = Posture.Friendly;
                        else if (relations.TurnsKnown > FirstDemand &&
                                 relations.HaveRejectedNapact)
                            relations.Posture = Posture.Neutral;
                        float usedTrust2 = 0.0f;
                        foreach (TrustEntry trustEntry in relations.TrustEntries)
                            usedTrust2 += trustEntry.TrustCost;
                        AssessAngerPacifist(kv, Posture.Neutral, usedTrust2);
                        continue;
                    case Posture.Hostile:
                        if (relations.ActiveWar != null)
                        {
                            Array<Empire> list = new Array<Empire>();
                            foreach (KeyValuePair<Empire, Relationship> keyValuePair in OwnerEmpire.AllRelations)
                            {
                                if (keyValuePair.Value.Treaty_Alliance &&
                                    keyValuePair.Key.GetRelations(them).Known &&
                                    !keyValuePair.Key.GetRelations(them).AtWar)
                                    list.Add(keyValuePair.Key);
                            }
                            foreach (Empire Ally in list)
                            {
                                if (!relations.ActiveWar.AlliesCalled.Contains(Ally.data.Traits.Name) &&
                                    OwnerEmpire.GetRelations(Ally).turnsSinceLastContact > 10)
                                {
                                    CallAllyToWar(Ally, them);
                                    relations.ActiveWar.AlliesCalled.Add(Ally.data.Traits.Name);
                                }
                            }
                            if (GlobalStats.RestrictAIPlayerInteraction && Empire.Universe.PlayerEmpire == them)
                                return;
                            if (relations.ActiveWar.TurnsAtWar % 100.0 == 0.0)
                            {
                                switch (relations.ActiveWar.WarType)
                                {
                                    case WarType.BorderConflict:
                                        if (relations.Anger_FromShipsInOurBorders +
                                            relations.Anger_TerritorialConflict >
                                            OwnerEmpire.data.DiplomaticPersonality.Territorialism)
                                            return;
                                        switch (relations.ActiveWar.GetBorderConflictState())
                                        {
                                            case WarState.WinningSlightly:
                                                OfferPeace(relations, them, "OFFERPEACE_FAIR");
                                                continue;
                                            case WarState.Dominating:
                                                OfferPeace(relations, them, "OFFERPEACE_WINNINGBC");
                                                continue;
                                            case WarState.LosingSlightly:
                                            case WarState.LosingBadly:
                                                OfferPeace(relations, them, "OFFERPEACE_LOSINGBC");
                                                continue;
                                            default:
                                                continue;
                                        }
                                    case WarType.ImperialistWar:
                                        switch (relations.ActiveWar.GetWarScoreState())
                                        {
                                            case WarState.WinningSlightly:
                                                OfferPeace(relations, them, "OFFERPEACE_FAIR");
                                                continue;
                                            case WarState.Dominating:
                                                OfferPeace(relations, them, "OFFERPEACE_FAIR_WINNING");
                                                continue;
                                            case WarState.EvenlyMatched:
                                                OfferPeace(relations, them, "OFFERPEACE_EVENLY_MATCHED");
                                                continue;
                                            case WarState.LosingSlightly:
                                            case WarState.LosingBadly:
                                                OfferPeace(relations, them, "OFFERPEACE_PLEADING");
                                                continue;
                                            default:
                                                continue;
                                        }
                                    case WarType.DefensiveWar:
                                        switch (relations.ActiveWar.GetBorderConflictState())
                                        {
                                            case WarState.WinningSlightly:
                                                OfferPeace(relations, them, "OFFERPEACE_FAIR");
                                                continue;
                                            case WarState.Dominating:
                                                OfferPeace(relations, them, "OFFERPEACE_FAIR_WINNING");
                                                continue;
                                            case WarState.EvenlyMatched:
                                                OfferPeace(relations, them, "OFFERPEACE_EVENLY_MATCHED");
                                                continue;
                                            case WarState.LosingSlightly:
                                            case WarState.LosingBadly:
                                                OfferPeace(relations, them, "OFFERPEACE_PLEADING");
                                                continue;
                                            default:
                                                continue;
                                        }
                                    default:
                                        continue;
                                }
                            }

                            continue;
                        }
                        else
                        {
                            AssessAngerPacifist(kv, Posture.Hostile, 100f);
                            continue;
                        }
                    default:
                        continue;
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