using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI {
    public sealed partial class EmpireAI
    {
        private void RunDiplomaticPlanner()
        {
            if (OwnerEmpire.isPlayer)
                return;
            string name = OwnerEmpire.data.DiplomaticPersonality.Name;
            if (name != null)
            {
                switch (name)
                {
                    case "Pacifist":
                        DoPacifistRelations();
                        break;
                    case "Aggressive":
                        DoAggressiveRelations();
                        break;
                    case "Honorable":
                        DoHonorableRelations();
                        break;
                    case "Xenophobic":
                        DoXenophobicRelations();
                        break;
                    case "Ruthless":
                        DoRuthlessRelations();
                        break;
                    case "Cunning":
                        DoCunningRelations();
                        break;
                }
            }
            foreach (KeyValuePair<Empire, Relationship> relationship in OwnerEmpire.AllRelations)
            {
                if (!relationship.Key.isFaction && !OwnerEmpire.isFaction && !relationship.Key.data.Defeated)
                    RunEventChecker(relationship);
            }
        }

        private void DoPacifistRelations()
        {
            AssessTeritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 50f);
            foreach (KeyValuePair<Empire, Relationship> Relationship in OwnerEmpire.AllRelations)
            {
                if (Relationship.Value.Known && !Relationship.Key.isFaction && !Relationship.Key.data.Defeated)
                {
                    float usedTrust = 0.0f;
                    foreach (TrustEntry trustEntry in Relationship.Value.TrustEntries)
                        usedTrust += trustEntry.TrustCost;
                    switch (Relationship.Value.Posture)
                    {
                        case Posture.Friendly:
                            if (Relationship.Value.TurnsKnown > SecondDemand && !Relationship.Value.Treaty_Trade &&
                                (!Relationship.Value.HaveRejected_TRADE &&
                                 Relationship.Value.Trust - usedTrust >
                                 OwnerEmpire.data.DiplomaticPersonality.Trade) &&
                                (!Relationship.Value.Treaty_Trade &&
                                 Relationship.Value.turnsSinceLastContact > SecondDemand &&
                                 !Relationship.Value.HaveRejected_TRADE))
                            {
                                Offer offer1 = new Offer();
                                offer1.TradeTreaty = true;
                                offer1.AcceptDL = "Trade Accepted";
                                offer1.RejectDL = "Trade Rejected";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify = new Ref<bool>(() => r.HaveRejected_TRADE,
                                    x => r.HaveRejected_TRADE = x);
                                Offer offer2 = new Offer();
                                offer2.TradeTreaty = true;
                                if (Relationship.Key == Empire.Universe.PlayerEmpire)
                                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                                        Empire.Universe.PlayerEmpire, "Offer Trade", offer2, offer1));
                                else
                                    Relationship.Key.GetEmpireAI()
                                        .AnalyzeOffer(offer2, offer1, OwnerEmpire, Offer.Attitude.Respectful);
                            }
                            AssessAngerPacifist(Relationship, Posture.Friendly, usedTrust);
                            if (Relationship.Value.TurnsAbove95 > 100 &&
                                Relationship.Value.turnsSinceLastContact > 10 &&
                                (!Relationship.Value.Treaty_Alliance && Relationship.Value.Treaty_Trade) &&
                                (Relationship.Value.Treaty_NAPact && !Relationship.Value.HaveRejected_Alliance &&
                                 Relationship.Value.TotalAnger < 20.0))
                            {
                                Offer offer1 = new Offer();
                                offer1.Alliance = true;
                                offer1.AcceptDL = "ALLIANCE_ACCEPTED";
                                offer1.RejectDL = "ALLIANCE_REJECTED";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify = new Ref<bool>(() => r.HaveRejected_Alliance,
                                    x =>
                                    {
                                        r.HaveRejected_Alliance = x;
                                        SetAlliance(!r.HaveRejected_Alliance);
                                    });
                                Offer offer2 = new Offer();
                                if (Relationship.Key == Empire.Universe.PlayerEmpire)
                                {
                                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(
                                        Empire.Universe, OwnerEmpire, Empire.Universe.PlayerEmpire, "OFFER_ALLIANCE", offer2,
                                        offer1));
                                    continue;
                                }

                                Relationship.Key.GetEmpireAI()
                                    .AnalyzeOffer(offer2, offer1, OwnerEmpire, Offer.Attitude.Respectful);
                                continue;
                            }
                            else
                                continue;
                        case Posture.Neutral:
                            if (Relationship.Value.TurnsKnown == FirstDemand && !Relationship.Value.Treaty_NAPact)
                            {
                                Offer offer1 = new Offer();
                                offer1.NAPact = true;
                                offer1.AcceptDL = "NAPact Accepted";
                                offer1.RejectDL = "NAPact Rejected";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify =
                                    new Ref<bool>(() => r.HaveRejectedNapact, x => r.HaveRejectedNapact = x);
                                Relationship.Value.turnsSinceLastContact = 0;
                                Offer offer2 = new Offer();
                                offer2.NAPact = true;
                                if (Relationship.Key == Empire.Universe.PlayerEmpire)
                                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                                        Empire.Universe.PlayerEmpire, "Offer NAPact", offer2, offer1));
                                else
                                    Relationship.Key.GetEmpireAI()
                                        .AnalyzeOffer(offer2, offer1, OwnerEmpire, Offer.Attitude.Respectful);
                            }
                            if (Relationship.Value.TurnsKnown > FirstDemand && Relationship.Value.Treaty_NAPact)
                                Relationship.Value.Posture = Posture.Friendly;
                            else if (Relationship.Value.TurnsKnown > FirstDemand &&
                                     Relationship.Value.HaveRejectedNapact)
                                Relationship.Value.Posture = Posture.Neutral;
                            AssessAngerPacifist(Relationship, Posture.Neutral, usedTrust);
                            if (Relationship.Value.Trust > 50f && Relationship.Value.TotalAnger < 10)
                            {
                                Relationship.Value.Posture = Posture.Friendly;
                                continue;
                            }
                            else
                                continue;
                        case Posture.Hostile:
                            if (Relationship.Value.ActiveWar != null)
                            {
                                Array<Empire> list = new Array<Empire>();
                                foreach (KeyValuePair<Empire, Relationship> keyValuePair in OwnerEmpire.AllRelations)
                                {
                                    if (keyValuePair.Value.Treaty_Alliance &&
                                        keyValuePair.Key.GetRelations(Relationship.Key).Known &&
                                        !keyValuePair.Key.GetRelations(Relationship.Key).AtWar)
                                        list.Add(keyValuePair.Key);
                                }
                                foreach (Empire Ally in list)
                                {
                                    if (!Relationship.Value.ActiveWar.AlliesCalled.Contains(Ally.data.Traits.Name) &&
                                        OwnerEmpire.GetRelations(Ally).turnsSinceLastContact > 10)
                                    {
                                        CallAllyToWar(Ally, Relationship.Key);
                                        Relationship.Value.ActiveWar.AlliesCalled.Add(Ally.data.Traits.Name);
                                    }
                                }
                                if (Relationship.Value.ActiveWar.TurnsAtWar % 100.0 == 0f)
                                {
                                    switch (Relationship.Value.ActiveWar.WarType)
                                    {
                                        case WarType.BorderConflict:
                                            if ((Relationship.Value.Anger_FromShipsInOurBorders +
                                                 Relationship.Value.Anger_TerritorialConflict) >
                                                OwnerEmpire.data.DiplomaticPersonality.Territorialism)
                                                return;
                                            switch (Relationship.Value.ActiveWar.GetBorderConflictState())
                                            {
                                                case WarState.WinningSlightly:
                                                    OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    OfferPeace(Relationship, "OFFERPEACE_WINNINGBC");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    OfferPeace(Relationship, "OFFERPEACE_LOSINGBC");
                                                    continue;
                                                default:
                                                    continue;
                                            }
                                        case WarType.ImperialistWar:
                                            switch (Relationship.Value.ActiveWar.GetWarScoreState())
                                            {
                                                case WarState.WinningSlightly:
                                                    OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    OfferPeace(Relationship, "OFFERPEACE_FAIR_WINNING");
                                                    continue;
                                                case WarState.EvenlyMatched:
                                                    OfferPeace(Relationship, "OFFERPEACE_EVENLY_MATCHED");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    OfferPeace(Relationship, "OFFERPEACE_PLEADING");
                                                    continue;
                                                default:
                                                    continue;
                                            }
                                        case WarType.DefensiveWar:
                                            switch (Relationship.Value.ActiveWar.GetBorderConflictState())
                                            {
                                                case WarState.WinningSlightly:
                                                    OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    OfferPeace(Relationship, "OFFERPEACE_FAIR_WINNING");
                                                    continue;
                                                case WarState.EvenlyMatched:
                                                    OfferPeace(Relationship, "OFFERPEACE_EVENLY_MATCHED");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    OfferPeace(Relationship, "OFFERPEACE_PLEADING");
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
                                continue;
                        default:
                            continue;
                    }
                }
            }
        }

        private void DoRuthlessRelations()
        {
            AssessTeritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 5f);
            int numberofWars = 0;
            Array<Empire> PotentialTargets = new Array<Empire>();
            foreach (KeyValuePair<Empire, Relationship> Relationship in OwnerEmpire.AllRelations)
            {
                if (!Relationship.Value.AtWar || Relationship.Key.data.Defeated)
                {
                    continue;
                }
                numberofWars += (int) Relationship.Key.currentMilitaryStrength * 2; //++;
            }
            //Label0:
            foreach (KeyValuePair<Empire, Relationship> Relationship in OwnerEmpire.AllRelations)
            {
                if (!Relationship.Value.Known || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
                {
                    continue;
                }
                if (Relationship.Key.data.DiplomaticPersonality != null && !Relationship.Value.HaveRejected_TRADE &&
                    !Relationship.Value.Treaty_Trade && !Relationship.Value.AtWar &&
                    (Relationship.Key.data.DiplomaticPersonality.Name != "Aggressive" ||
                     Relationship.Key.data.DiplomaticPersonality.Name != "Ruthless"))
                {
                    Offer NAPactOffer = new Offer
                    {
                        TradeTreaty = true,
                        AcceptDL = "Trade Accepted",
                        RejectDL = "Trade Rejected"
                    };
                    Relationship value = Relationship.Value;
                    NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_TRADE,
                        x => value.HaveRejected_TRADE = x);
                    Offer OurOffer = new Offer
                    {
                        TradeTreaty = true
                    };
                    Relationship.Key.GetEmpireAI()
                        .AnalyzeOffer(OurOffer, NAPactOffer, OwnerEmpire, Offer.Attitude.Respectful);
                }
                float usedTrust = 0f;
                foreach (TrustEntry te in Relationship.Value.TrustEntries)
                {
                    usedTrust = usedTrust + te.TrustCost;
                }
                AssessAngerAggressive(Relationship, Relationship.Value.Posture, usedTrust);
                Relationship.Value.Posture = Posture.Hostile;
                if (!Relationship.Value.Known || Relationship.Value.AtWar)
                {
                    continue;
                }
                Relationship.Value.Posture = Posture.Hostile;
                if (Relationship.Key == Empire.Universe.PlayerEmpire && Relationship.Value.Threat <= -15f &&
                    !Relationship.Value.HaveInsulted_Military && Relationship.Value.TurnsKnown > FirstDemand)
                {
                    Relationship.Value.HaveInsulted_Military = true;
                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                        Empire.Universe.PlayerEmpire, "Insult Military"));
                }
                if (Relationship.Value.Threat > 0f || Relationship.Value.TurnsKnown <= SecondDemand ||
                    Relationship.Value.Treaty_Alliance)
                {
                    if (Relationship.Value.Threat > -45f || numberofWars > OwnerEmpire.currentMilitaryStrength) //!= 0)
                    {
                        continue;
                    }
                    PotentialTargets.Add(Relationship.Key);
                }
                else
                {
                    int i = 0;
                    while (i < 5)
                    {
                        if (i >= DesiredPlanets.Length)
                        {
                            //goto Label0;    //this tried to restart the loop it's in => bad mojo
                            break;
                        }
                        if (DesiredPlanets[i].Owner != Relationship.Key)
                        {
                            i++;
                        }
                        else
                        {
                            PotentialTargets.Add(Relationship.Key);
                            //goto Label0;
                            break;
                        }
                    }                    

                }
            }
            if (PotentialTargets.Count > 0 && numberofWars <= OwnerEmpire.currentMilitaryStrength) //1)
            {
                IOrderedEnumerable<Empire> sortedList =
                    from target in PotentialTargets
                    orderby Vector2.Distance(OwnerEmpire.GetWeightedCenter(), target.GetWeightedCenter())
                    select target;
                bool foundwar = false;
                foreach (Empire e in PotentialTargets)
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
            AssessTeritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 10f);
            foreach (KeyValuePair<Empire, Relationship> Relationship in OwnerEmpire.AllRelations)
            {
                if (!Relationship.Value.Known || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
                {
                    continue;
                }
                float usedTrust = 0f;
                foreach (TrustEntry te in Relationship.Value.TrustEntries)
                {
                    usedTrust = usedTrust + te.TrustCost;
                }
                AssessDiplomaticAnger(Relationship);
                switch (Relationship.Value.Posture)
                {
                    case Posture.Friendly:
                    {
                        if (Relationship.Value.TurnsKnown <= SecondDemand ||
                            Relationship.Value.Trust - usedTrust <= OwnerEmpire.data.DiplomaticPersonality.Trade ||
                            Relationship.Value.Treaty_Trade || Relationship.Value.HaveRejected_TRADE ||
                            Relationship.Value.turnsSinceLastContact <= SecondDemand ||
                            Relationship.Value.HaveRejected_TRADE)
                        {
                            continue;
                        }
                        Offer NAPactOffer = new Offer
                        {
                            TradeTreaty = true,
                            AcceptDL = "Trade Accepted",
                            RejectDL = "Trade Rejected"
                        };
                        Relationship value = Relationship.Value;
                        NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_TRADE,
                            x => value.HaveRejected_TRADE = x);
                        Offer OurOffer = new Offer
                        {
                            TradeTreaty = true
                        };
                        if (Relationship.Key != Empire.Universe.PlayerEmpire)
                        {
                            Relationship.Key.GetEmpireAI()
                                .AnalyzeOffer(OurOffer, NAPactOffer, OwnerEmpire, Offer.Attitude.Respectful);
                            continue;
                        }

                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                            Empire.Universe.PlayerEmpire, "Offer Trade", new Offer(), NAPactOffer));
                        continue;
                    }
                    case Posture.Neutral:
                    {
                        if (Relationship.Value.TurnsKnown >= FirstDemand && !Relationship.Value.Treaty_NAPact &&
                            !Relationship.Value.HaveRejectedDemandTech && !Relationship.Value.XenoDemandedTech)
                        {
                            Array<TechEntry> potentialDemands = TradableTechs(Relationship.Key);
                            if (potentialDemands.Count > 0)
                            {
                                int Random = (int) RandomMath.RandomBetween(0f, potentialDemands.Count + 0.75f);
                                if (Random > potentialDemands.Count - 1)
                                {
                                    Random = potentialDemands.Count - 1;
                                }
                                TechEntry TechToDemand = potentialDemands[Random];
                                Offer DemandTech = new Offer();
                                DemandTech.TechnologiesOffered.Add(TechToDemand.UID);
                                Relationship.Value.XenoDemandedTech = true;
                                Offer TheirDemand = new Offer
                                {
                                    AcceptDL = "Xeno Demand Tech Accepted",
                                    RejectDL = "Xeno Demand Tech Rejected"
                                };
                                Relationship relationship = Relationship.Value;
                                TheirDemand.ValueToModify = new Ref<bool>(() => relationship.HaveRejectedDemandTech,
                                    x => relationship.HaveRejectedDemandTech = x);
                                Relationship.Value.turnsSinceLastContact = 0;
                                if (Relationship.Key != Empire.Universe.PlayerEmpire)
                                {
                                    Relationship.Key.GetEmpireAI()
                                        .AnalyzeOffer(DemandTech, TheirDemand, OwnerEmpire, Offer.Attitude.Threaten);
                                }
                                else
                                {
                                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                                        Empire.Universe.PlayerEmpire, "Xeno Demand Tech", DemandTech, TheirDemand));
                                }
                            }
                        }
                        if (!Relationship.Value.HaveRejectedDemandTech)
                        {
                            continue;
                        }
                        Relationship.Value.Posture = Posture.Hostile;
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
            foreach (TechEntry tech in OwnerEmpire.TechsAvailableForTrade(them))
            {
                if (them.GetTechEntry(tech).CanBeTakenFrom(OwnerEmpire))
                    tradableTechs.Add(tech);
            }

            return tradableTechs;
        }

        private void DoAggressiveRelations()
        {
            int numberofWars = 0;
            Array<Empire> PotentialTargets = new Array<Empire>();
            foreach (KeyValuePair<Empire, Relationship> Relationship in OwnerEmpire.AllRelations)
            {
                if (Relationship.Key.data.Defeated || !Relationship.Value.AtWar && !Relationship.Value.PreparingForWar)
                {
                    continue;
                }
                //numberofWars++;
                numberofWars += (int) Relationship.Key.currentMilitaryStrength;
            }
            AssessTeritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 10f);
            foreach (KeyValuePair<Empire, Relationship> Relationship in OwnerEmpire.AllRelations)
            {
                if (!Relationship.Value.Known || Relationship.Value.AtWar || Relationship.Key.isFaction ||
                    Relationship.Key.data.Defeated)
                {
                    continue;
                }

                if (Relationship.Key.data.DiplomaticPersonality != null && !Relationship.Value.HaveRejected_TRADE &&
                    !Relationship.Value.Treaty_Trade && !Relationship.Value.AtWar &&
                    (Relationship.Key.data.DiplomaticPersonality.Name != "Aggressive" ||
                     Relationship.Key.data.DiplomaticPersonality.Name != "Ruthless"))
                {
                    Offer NAPactOffer = new Offer
                    {
                        TradeTreaty = true,
                        AcceptDL = "Trade Accepted",
                        RejectDL = "Trade Rejected"
                    };
                    Relationship value = Relationship.Value;
                    NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_TRADE,
                        x => value.HaveRejected_TRADE = x);
                    Offer OurOffer = new Offer
                    {
                        TradeTreaty = true
                    };
                    Relationship.Key.GetEmpireAI()
                        .AnalyzeOffer(OurOffer, NAPactOffer, OwnerEmpire, Offer.Attitude.Respectful);
                }
                float usedTrust = 0f;
                foreach (TrustEntry te in Relationship.Value.TrustEntries)
                {
                    usedTrust = usedTrust + te.TrustCost;
                }
                Relationship.Value.Posture = Posture.Neutral;
                if (Relationship.Value.Threat <= 0f)
                {
                    if (!Relationship.Value.HaveInsulted_Military && Relationship.Value.TurnsKnown > FirstDemand)
                    {
                        Relationship.Value.HaveInsulted_Military = true;
                        if (Relationship.Key == Empire.Universe.PlayerEmpire)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                                Empire.Universe.PlayerEmpire, "Insult Military"));
                        }
                    }
                    Relationship.Value.Posture = Posture.Hostile;
                }
                else if (Relationship.Value.Threat > 25f && Relationship.Value.TurnsKnown > FirstDemand)
                {
                    if (!Relationship.Value.HaveComplimented_Military && Relationship.Value.HaveInsulted_Military &&
                        Relationship.Value.TurnsKnown > FirstDemand &&
                        Relationship.Key == Empire.Universe.PlayerEmpire)
                    {
                        Relationship.Value.HaveComplimented_Military = true;
                        if (!Relationship.Value.HaveInsulted_Military ||
                            Relationship.Value.TurnsKnown <= SecondDemand)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                                Empire.Universe.PlayerEmpire, "Compliment Military"));
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                                Empire.Universe.PlayerEmpire, "Compliment Military Better"));
                        }
                    }
                    Relationship.Value.Posture = Posture.Friendly;
                }
                switch (Relationship.Value.Posture)
                {
                    case Posture.Friendly:
                    {
                        if (Relationship.Value.TurnsKnown > SecondDemand &&
                            Relationship.Value.Trust - usedTrust >
                            OwnerEmpire.data.DiplomaticPersonality.Trade &&
                            !Relationship.Value.HaveRejected_TRADE && !Relationship.Value.Treaty_Trade)
                        {
                            Offer NAPactOffer = new Offer
                            {
                                TradeTreaty = true,
                                AcceptDL = "Trade Accepted",
                                RejectDL = "Trade Rejected"
                            };
                            Relationship relationship = Relationship.Value;
                            NAPactOffer.ValueToModify = new Ref<bool>(() => relationship.HaveRejected_TRADE,
                                x => relationship.HaveRejected_TRADE = x);
                            Offer OurOffer = new Offer
                            {
                                TradeTreaty = true
                            };
                            if (Relationship.Key != Empire.Universe.PlayerEmpire)
                            {
                                Relationship.Key.GetEmpireAI()
                                    .AnalyzeOffer(OurOffer, NAPactOffer, OwnerEmpire, Offer.Attitude.Respectful);
                            }
                            else
                            {
                                Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                                    Empire.Universe.PlayerEmpire, "Offer Trade", OurOffer, NAPactOffer));
                            }
                        }
                        AssessAngerAggressive(Relationship, Relationship.Value.Posture, usedTrust);
                        if (Relationship.Value.TurnsAbove95 <= 100 || Relationship.Value.turnsSinceLastContact <= 10 ||
                            Relationship.Value.Treaty_Alliance || !Relationship.Value.Treaty_Trade ||
                            !Relationship.Value.Treaty_NAPact || Relationship.Value.HaveRejected_Alliance ||
                            Relationship.Value.TotalAnger >= 20f)
                        {
                            continue;
                        }
                        Offer OfferAlliance = new Offer
                        {
                            Alliance = true,
                            AcceptDL = "ALLIANCE_ACCEPTED",
                            RejectDL = "ALLIANCE_REJECTED"
                        };
                        Relationship value1 = Relationship.Value;
                        OfferAlliance.ValueToModify = new Ref<bool>(() => value1.HaveRejected_Alliance, x =>
                        {
                            value1.HaveRejected_Alliance = x;
                            SetAlliance(!value1.HaveRejected_Alliance);
                        });
                        Offer OurOffer0 = new Offer();
                        if (Relationship.Key != Empire.Universe.PlayerEmpire)
                        {
                            Relationship.Key.GetEmpireAI()
                                .AnalyzeOffer(OurOffer0, OfferAlliance, OwnerEmpire, Offer.Attitude.Respectful);
                            continue;
                        }

                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                            Empire.Universe.PlayerEmpire, "OFFER_ALLIANCE", OurOffer0, OfferAlliance));
                        continue;
                    }
                    case Posture.Neutral:
                    {
                        AssessAngerAggressive(Relationship, Relationship.Value.Posture, usedTrust);
                        continue;
                    }
                    case Posture.Hostile:
                    {
                        if (Relationship.Value.Threat < -15f && Relationship.Value.TurnsKnown > SecondDemand &&
                            !Relationship.Value.Treaty_Alliance)
                        {
                            if (Relationship.Value.TotalAnger < 75f)
                            {
                                int i = 0;
                                while (i < 5)
                                {
                                    if (i >= DesiredPlanets.Length)
                                    {
                                        break;
                                    }
                                    if (DesiredPlanets[i].Owner != Relationship.Key)
                                    {
                                        i++;
                                    }
                                    else
                                    {
                                        PotentialTargets.Add(Relationship.Key);
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                PotentialTargets.Add(Relationship.Key);
                            }
                        }
                        else if (Relationship.Value.Threat <= -45f && Relationship.Value.TotalAnger > 20f)
                        {
                            PotentialTargets.Add(Relationship.Key);
                        }
                        //Label0:
                        AssessAngerAggressive(Relationship, Relationship.Value.Posture, usedTrust);
                        continue;
                    }
                    default:
                    {
                        continue; //this doesn't actually do anything, since it's at the end of the loop anyways
                    }
                }
            }
            if (PotentialTargets.Count > 0 && numberofWars * 2 < OwnerEmpire.currentMilitaryStrength) //<= 1)
            {
                Empire ToAttack = PotentialTargets.First();
                OwnerEmpire.GetRelations(ToAttack).PreparingForWar = true;
            }
        }

        private void DoCunningRelations()
        {
            DoHonorableRelations();
        }

        private void DoHonorableRelations()
        {
            AssessTeritorialConflicts(OwnerEmpire.data.DiplomaticPersonality.Territorialism / 10f);
            foreach (KeyValuePair<Empire, Relationship> Relationship in OwnerEmpire.AllRelations)
            {
                if (Relationship.Value.Known && !Relationship.Key.isFaction && !Relationship.Key.data.Defeated)
                {
                    switch (Relationship.Value.Posture)
                    {
                        case Posture.Friendly:
                            float usedTrust1 = 0.0f;
                            foreach (TrustEntry trustEntry in Relationship.Value.TrustEntries)
                                usedTrust1 += trustEntry.TrustCost;
                            if (Relationship.Value.TurnsKnown > SecondDemand &&
                                Relationship.Value.Trust - (double) usedTrust1 >
                                OwnerEmpire.data.DiplomaticPersonality.Trade &&
                                (Relationship.Value.turnsSinceLastContact > SecondDemand &&
                                 !Relationship.Value.Treaty_Trade) && !Relationship.Value.HaveRejected_TRADE)
                            {
                                Offer offer1 = new Offer();
                                offer1.TradeTreaty = true;
                                offer1.AcceptDL = "Trade Accepted";
                                offer1.RejectDL = "Trade Rejected";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify =
                                    new Ref<bool>(() => r.HaveRejected_TRADE, x => r.HaveRejected_TRADE = x);
                                Offer offer2 = new Offer();
                                offer2.TradeTreaty = true;
                                if (Relationship.Key == Empire.Universe.PlayerEmpire)
                                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                                        Empire.Universe.PlayerEmpire, "Offer Trade", offer2, offer1));
                                else
                                    Relationship.Key.GetEmpireAI()
                                        .AnalyzeOffer(offer2, offer1, OwnerEmpire, Offer.Attitude.Respectful);
                            }
                            AssessAngerPacifist(Relationship, Posture.Friendly, usedTrust1);
                            if (Relationship.Value.TurnsAbove95 > 100 &&
                                Relationship.Value.turnsSinceLastContact > 10 &&
                                (!Relationship.Value.Treaty_Alliance && Relationship.Value.Treaty_Trade) &&
                                (Relationship.Value.Treaty_NAPact && !Relationship.Value.HaveRejected_Alliance &&
                                 Relationship.Value.TotalAnger < 20.0))
                            {
                                Offer offer1 = new Offer();
                                offer1.Alliance = true;
                                offer1.AcceptDL = "ALLIANCE_ACCEPTED";
                                offer1.RejectDL = "ALLIANCE_REJECTED";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify = new Ref<bool>(() => r.HaveRejected_Alliance,
                                    x =>
                                    {
                                        r.HaveRejected_Alliance = x;
                                        SetAlliance(!r.HaveRejected_Alliance);
                                    });
                                Offer offer2 = new Offer();
                                if (Relationship.Key == Empire.Universe.PlayerEmpire)
                                {
                                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(
                                        Empire.Universe, OwnerEmpire, Empire.Universe.PlayerEmpire, "OFFER_ALLIANCE", offer2,
                                        offer1));
                                    continue;
                                }

                                Relationship.Key.GetEmpireAI()
                                    .AnalyzeOffer(offer2, offer1, OwnerEmpire, Offer.Attitude.Respectful);
                                continue;
                            }
                            else
                                continue;
                        case Posture.Neutral:
                            if (Relationship.Value.TurnsKnown == FirstDemand && !Relationship.Value.Treaty_NAPact)
                            {
                                Offer offer1 = new Offer();
                                offer1.NAPact = true;
                                offer1.AcceptDL = "NAPact Accepted";
                                offer1.RejectDL = "NAPact Rejected";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify = new Ref<bool>(() => r.HaveRejectedNapact,
                                    x => r.HaveRejectedNapact = x);
                                Relationship.Value.turnsSinceLastContact = 0;
                                Offer offer2 = new Offer();
                                offer2.NAPact = true;
                                if (Relationship.Key == Empire.Universe.PlayerEmpire)
                                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(
                                        Empire.Universe, OwnerEmpire, Empire.Universe.PlayerEmpire, "Offer NAPact", offer2,
                                        offer1));
                                else
                                    Relationship.Key.GetEmpireAI()
                                        .AnalyzeOffer(offer2, offer1, OwnerEmpire, Offer.Attitude.Respectful);
                            }
                            if (Relationship.Value.TurnsKnown > FirstDemand && Relationship.Value.Treaty_NAPact)
                                Relationship.Value.Posture = Posture.Friendly;
                            else if (Relationship.Value.TurnsKnown > FirstDemand &&
                                     Relationship.Value.HaveRejectedNapact)
                                Relationship.Value.Posture = Posture.Neutral;
                            float usedTrust2 = 0.0f;
                            foreach (TrustEntry trustEntry in Relationship.Value.TrustEntries)
                                usedTrust2 += trustEntry.TrustCost;
                            AssessAngerPacifist(Relationship, Posture.Neutral, usedTrust2);
                            continue;
                        case Posture.Hostile:
                            if (Relationship.Value.ActiveWar != null)
                            {
                                Array<Empire> list = new Array<Empire>();
                                foreach (KeyValuePair<Empire, Relationship> keyValuePair in OwnerEmpire.AllRelations)
                                {
                                    if (keyValuePair.Value.Treaty_Alliance &&
                                        keyValuePair.Key.GetRelations(Relationship.Key).Known &&
                                        !keyValuePair.Key.GetRelations(Relationship.Key).AtWar)
                                        list.Add(keyValuePair.Key);
                                }
                                foreach (Empire Ally in list)
                                {
                                    if (!Relationship.Value.ActiveWar.AlliesCalled.Contains(Ally.data.Traits.Name) &&
                                        OwnerEmpire.GetRelations(Ally).turnsSinceLastContact > 10)
                                    {
                                        CallAllyToWar(Ally, Relationship.Key);
                                        Relationship.Value.ActiveWar.AlliesCalled.Add(Ally.data.Traits.Name);
                                    }
                                }
                                if (Relationship.Value.ActiveWar.TurnsAtWar % 100.0 == 0.0)
                                {
                                    switch (Relationship.Value.ActiveWar.WarType)
                                    {
                                        case WarType.BorderConflict:
                                            if (Relationship.Value.Anger_FromShipsInOurBorders +
                                                Relationship.Value.Anger_TerritorialConflict >
                                                (double) OwnerEmpire.data.DiplomaticPersonality.Territorialism)
                                                return;
                                            switch (Relationship.Value.ActiveWar.GetBorderConflictState())
                                            {
                                                case WarState.WinningSlightly:
                                                    OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    OfferPeace(Relationship, "OFFERPEACE_WINNINGBC");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    OfferPeace(Relationship, "OFFERPEACE_LOSINGBC");
                                                    continue;
                                                default:
                                                    continue;
                                            }
                                        case WarType.ImperialistWar:
                                            switch (Relationship.Value.ActiveWar.GetWarScoreState())
                                            {
                                                case WarState.WinningSlightly:
                                                    OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    OfferPeace(Relationship, "OFFERPEACE_FAIR_WINNING");
                                                    continue;
                                                case WarState.EvenlyMatched:
                                                    OfferPeace(Relationship, "OFFERPEACE_EVENLY_MATCHED");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    OfferPeace(Relationship, "OFFERPEACE_PLEADING");
                                                    continue;
                                                default:
                                                    continue;
                                            }
                                        case WarType.DefensiveWar:
                                            switch (Relationship.Value.ActiveWar.GetBorderConflictState())
                                            {
                                                case WarState.WinningSlightly:
                                                    OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    OfferPeace(Relationship, "OFFERPEACE_FAIR_WINNING");
                                                    continue;
                                                case WarState.EvenlyMatched:
                                                    OfferPeace(Relationship, "OFFERPEACE_EVENLY_MATCHED");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    OfferPeace(Relationship, "OFFERPEACE_PLEADING");
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
                                AssessAngerPacifist(Relationship, Posture.Hostile, 100f);
                                continue;
                            }
                        default:
                            continue;
                    }
                }
            }
        }

        private void AssessTeritorialConflicts(float weight)
        {

            weight *= .1f;
            foreach (SystemCommander CheckBorders in DefensiveCoordinator.DefenseDict.Values)
            {
                if (CheckBorders.RankImportance > 5)
                {
                    foreach (SolarSystem closeenemies in CheckBorders.System.FiveClosestSystems)
                    {
                        foreach (Empire enemy in closeenemies.OwnerList)
                        {

                            if (enemy.isPlayer)
                                continue;

                            if (enemy.isFaction)
                                continue;
                            Relationship check = null;

                            if (OwnerEmpire.TryGetRelations(enemy, out check))
                            {
                                if (!check.Known || check.Treaty_Alliance)
                                    continue;

                                weight *= (OwnerEmpire.currentMilitaryStrength + closeenemies.GetActualStrengthPresent(enemy)) / (OwnerEmpire.currentMilitaryStrength + 1);

                                if (check.Treaty_OpenBorders)
                                {
                                    weight *= .5f;
                                }
                                if (check.Treaty_NAPact)
                                    weight *= .5f;
                                if (enemy.isPlayer)
                                    weight *= ((int)CurrentGame.Difficulty + 1);

                                if (check.Anger_TerritorialConflict > 0)
                                    check.Anger_TerritorialConflict += (check.Anger_TerritorialConflict + CheckBorders.RankImportance * weight) / (check.Anger_TerritorialConflict);
                                else
                                    check.Anger_TerritorialConflict += CheckBorders.RankImportance * weight;

                            }
                        }

                    }

                }
            }
        }


    }
}