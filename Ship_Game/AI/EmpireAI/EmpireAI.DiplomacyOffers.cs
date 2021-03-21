using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        void AcceptNAPact(Empire us, Empire them,  Offer.Attitude attitude)
        {
            us.SignTreatyWith(them, TreatyType.NonAggression);
            Relationship usToThem = us.GetRelations(them);
            usToThem.AddTrustEntry(attitude, TrustEntryType.Treaty, OwnerEmpire.PersonalityModifiers.TrustCostNaPact);
        }

        void AcceptTradeTreaty(Empire us, Empire them, Offer.Attitude attitude)
        {
            us.SignTreatyWith(them, TreatyType.Trade);
            Relationship usToThem = us.GetRelations(them);
            usToThem.AddTrustEntry(attitude, TrustEntryType.Treaty, OwnerEmpire.PersonalityModifiers.TrustCostTradePact);
        }

        void AcceptBorderTreaty(Empire us, Empire them, Offer.Attitude attitude)
        {
            us.SignTreatyWith(them, TreatyType.OpenBorders);
            Relationship usToThem = us.GetRelations(them);
            usToThem.AddTrustEntry(attitude, TrustEntryType.Treaty, 5f);
        }

        void GiveTechs(Empire us, Empire them, Array<string> techs, Offer.Attitude attitude)
        {
            Relationship usToThem = us.GetRelations(them);
            foreach (string tech in techs)
            {
                them.UnlockTech(tech, TechUnlockType.Diplomacy, us);
                usToThem.NumTechsWeGave += 1;
                Log.Info(System.ConsoleColor.White, $"{us.Name} gave {tech} to {them.Name}");
                if (Empire.Universe.PlayerEmpire != us)
                {
                    float cost = ResourceManager.Tech(tech).DiplomaticValueTo(us);
                    usToThem.AddTrustEntry(attitude, TrustEntryType.Technology, cost, turnTimer:40);
                }
            }
        }

        void GiveArtifacts(Empire us, Empire them, Array<string> artifacts, Offer.Attitude attitude)
        {
            foreach (string art in artifacts)
            {
                Artifact toGive = ResourceManager.ArtifactsDict[art];
                foreach (Artifact artifact in us.data.OwnedArtifacts)
                {
                    if (artifact.Name == art)
                    {
                        toGive = artifact;
                    }
                }
                us.RemoveArtifact(toGive);
                them.AddArtifact(toGive);
            }
        }

        void GiveColonies(Empire us, Empire them, Array<string> colonies, Offer.Attitude attitude)
        {
            Relationship usToThem = us.GetRelations(them);
            foreach (string planetName in colonies)
            {
                var toRemove = new Array<Planet>();
                var troopShips = new Array<Ship>();

                Planet p = us.FindPlanet(planetName);
                if (p != null)
                {
                    // remove our troops from this planet
                    foreach (PlanetGridSquare pgs in p.TilesList)
                    {
                        if (pgs.TroopsAreOnTile && pgs.LockOnOurTroop(us, out Troop troop))
                        {
                            troop.SetPlanet(p); // FB - this is for making sure there is a host planet for the troops? strange
                            troopShips.Add(troop.Launch(ignoreMovement: true));
                        }
                    }

                    foreach (Ship orbital in p.OrbitalStations)
                        orbital.ChangeLoyalty(them, notification: false);

                    toRemove.Add(p);
                    p.Owner = them;
                    them.AddPlanet(p);

                    p.ParentSystem.OwnerList.Clear();
                    foreach (Planet pl in p.ParentSystem.PlanetList)
                    {
                        if (pl.Owner != null && !p.ParentSystem.OwnerList.Contains(pl.Owner))
                            p.ParentSystem.OwnerList.Add(pl.Owner);
                    }

                    if (!them.isPlayer)
                    {
                        p.colonyType = them.AssessColonyNeeds(p);
                    }
                    if (!us.isPlayer)
                    {
                        float cost = p.ColonyWorthTo(us);
                        usToThem.AddTrustEntry(attitude, TrustEntryType.Colony, cost, turnTimer:40);
                    }
                }

                foreach (Planet planetToRemove in toRemove)
                    us.RemovePlanet(planetToRemove);
                foreach (Ship ship in troopShips)
                    ship.AI.OrderRebaseToNearest();
            }
        }

        void ProcessOffer(Empire us, Empire them, Offer ourOffer, Offer.Attitude attitude)
        {
            if (ourOffer.NAPact)
                AcceptNAPact(them, us, attitude);

            if (ourOffer.TradeTreaty)
                AcceptTradeTreaty(them, us, attitude);

            if (ourOffer.OpenBorders) // if we offer BorderTreaty, then THEY need to accept it
                AcceptBorderTreaty(them, us, attitude);

            GiveTechs(us, them, ourOffer.TechnologiesOffered, attitude);
            GiveArtifacts(us, them, ourOffer.ArtifactsOffered, attitude);
            GiveColonies(us, them, ourOffer.ColoniesOffered, attitude);
        }

        void AcceptPeaceTreaty(Empire us, Empire them, Offer.Attitude attitude)
        {
            Relationship rel = us.GetRelations(them);
            rel.AtWar = false;
            rel.PreparingForWar = false;
            rel.ActiveWar.EndStarDate = Empire.Universe.StarDate;
            rel.WarHistory.Add(rel.ActiveWar);

            DTrait persona = us.data.DiplomaticPersonality;
            if (!us.isPlayer && persona != null)
            {
                rel.ChangeToNeutral();
                float borderAnger  = rel.Anger_FromShipsInOurBorders - persona.Territorialism / 3f;
                float territoryAnger = rel.Anger_TerritorialConflict - persona.Territorialism / 3f;
                if (borderAnger > 0)    rel.AddAngerShipsInOurBorders(-borderAnger);
                if (territoryAnger > 0) rel.AddAngerTerritorialConflict(-territoryAnger);
            }

            rel.ResetAngerMilitaryConflict();
            rel.WarnedAboutShips = false;
            rel.WarnedAboutColonizing = false;
            rel.HaveRejectedDemandTech = false;
            rel.HaveRejected_OpenBorders = false;
            rel.HaveRejected_TRADE = false;
            rel.HasDefenseFleet = false;
            if (rel.DefenseFleet != -1)
                us.GetFleetsDict()[rel.DefenseFleet].FleetTask.EndTask();

            us.GetEmpireAI().RemoveMilitaryTasksTargeting(them);
            rel.ActiveWar = null;
        }

        public void AcceptOffer(Offer ourOffer, Offer theirOffer,
                                Empire us, Empire them, Offer.Attitude attitude)
        {
            if (ourOffer.PeaceTreaty || theirOffer.PeaceTreaty)
            {
                Relationship rel = OwnerEmpire.GetRelations(them);
                bool neededPeace = them.isPlayer  // player asked peace since they is in a real bad state
                                     && rel.ActiveWar.GetWarScoreState() == WarState.Dominating
                                     && them.TotalPopBillion < OwnerEmpire.TotalPopBillion / (int)(CurrentGame.Difficulty + 1);

                if (!neededPeace && them.TheyAreAlliedWithOurEnemies(OwnerEmpire, out Array<Empire> empiresAlliedWithThem))
                    CheckAIEmpiresResponse(OwnerEmpire, empiresAlliedWithThem, them, true);

                us.SignTreatyWith(them, TreatyType.Peace);
                AcceptPeaceTreaty(us, them, attitude);
                AcceptPeaceTreaty(them, us, attitude);

                if (us.isPlayer || them.isPlayer ||
                    (EmpireManager.Player.IsKnown(us) &&
                     EmpireManager.Player.IsKnown(them)))
                {
                    Empire.Universe.NotificationManager.AddPeaceTreatyEnteredNotification(us, them);
                }
            }

            ProcessOffer(us, them, ourOffer, attitude);
            ProcessOffer(them, us, theirOffer, attitude);

            Empire.UpdateBilateralRelations(us, them);
        }

        string ProcessAlliance(Offer theirOffer, Offer ourOffer, Relationship usToThem, Empire them)
        {
            string answer;
            if (!theirOffer.IsBlank() || !ourOffer.IsBlank())
            {
                answer = "OFFER_ALLIANCE_TOO_COMPLICATED";
            }
            else if (usToThem.Trust < 90f || usToThem.TotalAnger >= 20f || usToThem.TurnsKnown <= 100)
            {
                answer = "AI_ALLIANCE_REJECT";
            }
            else if (WeCanAllyWithThem(them, usToThem, out answer))
            {
                usToThem.SetAlliance(true, OwnerEmpire, them);
            }

            return answer;
        }

        bool WeCanAllyWithThem(Empire them, Relationship usToThem, out string answer)
        {
            bool allowAlliance     = true;
            answer                 = "AI_ALLIANCE_ACCEPT";
            const string rejection = "AI_ALLIANCE_REJECT_ALLIED_WITH_ENEMY";
            if (OwnerEmpire.TheyAreAlliedWithOurEnemies(them, out Array<Empire> empiresAlliedWithThem))
            {
                usToThem.AddAngerDiplomaticConflict(OwnerEmpire.PersonalityModifiers.AddAngerAlliedWithEnemy);
                if (!OwnerEmpire.IsPacifist || !OwnerEmpire.IsCunning) // Only pacifist and cunning will ally
                {
                    allowAlliance   = false;
                    answer          = rejection;
                    usToThem.Trust -= 25;
                }
                else
                {
                    usToThem.Trust -= 5;
                }
            }

            CheckAIEmpiresResponse(them, empiresAlliedWithThem, OwnerEmpire, allowAlliance);
            return allowAlliance;
        }

        string ProcessPeace(Offer theirOffer, Offer ourOffer, Empire them,
                           Offer.Attitude attitude)
        {
            PeaceAnswer answer = AnalyzePeaceOffer(theirOffer, ourOffer, them, attitude);
            Relationship rel   = OwnerEmpire.GetRelations(them);
            bool neededPeace   = them.isPlayer  // player asked peace since they is in a real bad state
                                 && rel.ActiveWar.GetWarScoreState() == WarState.Dominating
                                 && them.TotalPopBillion < OwnerEmpire.TotalPopBillion / (int)(CurrentGame.Difficulty + 1);

            if (answer.Peace)
                AcceptOffer(ourOffer, theirOffer, OwnerEmpire, them, attitude);

            if (!neededPeace && OwnerEmpire.TheyAreAlliedWithOurEnemies(them, out Array<Empire> empiresAlliedWithThem))
                CheckAIEmpiresResponse(them, empiresAlliedWithThem, OwnerEmpire, answer.Peace);

            return answer.Answer;
        }

        public void CheckAIEmpiresResponse(Empire them, Array<Empire> otherEmpires, Empire empireSignedWith, bool treatySigned)
        {
            foreach (Empire e in otherEmpires)
            {
                if (!e.isPlayer)
                    e.RespondToPlayerThirdPartyTreatiesWithEnemies(them, empireSignedWith, treatySigned);
            }
        }

        public string AnalyzeOffer(Offer theirOffer, Offer ourOffer, Empire them, Offer.Attitude attitude)
        {
            Empire us = OwnerEmpire;
            Relationship usToThem = us.GetRelations(them);
            Relationship themToUs = them.GetRelations(us);

            if (theirOffer.Alliance)
                return ProcessAlliance(theirOffer, ourOffer, usToThem, them);

            if (theirOffer.PeaceTreaty)
                return ProcessPeace(theirOffer, ourOffer, them, attitude);

            float totalTrustRequiredFromUs = 0f;
            DTrait dt = us.data.DiplomaticPersonality;
            if (ourOffer.TradeTreaty)
            {
                totalTrustRequiredFromUs += dt.Trade;
            }
            if (ourOffer.OpenBorders)
            {
                totalTrustRequiredFromUs += (dt.NAPact + 7.5f);
            }
            if (ourOffer.NAPact)
            {
                totalTrustRequiredFromUs += dt.NAPact;
                int numWars = 0;
                foreach ((Empire relThem, Relationship rel)  in us.AllRelations)
                {
                    if (rel.AtWar && !relThem.isFaction)
                        ++numWars;
                }
                if (numWars > 0 && !usToThem.AtWar)
                {
                    totalTrustRequiredFromUs -= dt.NAPact;
                }
                else if (usToThem.Threat >= 20f)
                {
                    totalTrustRequiredFromUs -= dt.NAPact;
                }
            }

            float valueToThem = 0f;
            float valueToUs   = 0f;

            foreach (string tech in theirOffer.TechnologiesOffered)
            {
                valueToUs += ResourceManager.Tech(tech).DiplomaticValueTo(us, 0.02f);
            }

            if (ourOffer.TechnologiesOffered.Count > 0)
            {
                foreach (string tech in ourOffer.TechnologiesOffered)
                {
                    float value = ResourceManager.Tech(tech).DiplomaticValueTo(them, 0.02f);
                    valueToThem += value;
                    totalTrustRequiredFromUs += value;
                }

                // if value for them is higher, reduce a little trust needed
                totalTrustRequiredFromUs -= ((valueToThem - valueToUs) / 2).UpperBound(0);
            }

            if (ourOffer.OpenBorders)   valueToThem += 5f;
            if (theirOffer.OpenBorders) valueToUs   += them.isPlayer ? 2f : 5f;
            if (ourOffer.NAPact)        valueToThem += 10f;
            if (theirOffer.NAPact)      valueToUs   += 10f;
            if (ourOffer.TradeTreaty)   valueToThem += them.EstimateNetIncomeAtTaxRate(0.5f) < 5 ? 15f : 12f;
            if (theirOffer.TradeTreaty) valueToUs   += OwnerEmpire.EstimateNetIncomeAtTaxRate(0.5f) < 5 ? 15f : 12f;

            valueToThem += ourOffer.ArtifactsOffered.Count * 15f;
            valueToUs   += theirOffer.ArtifactsOffered.Count * 15f;

            if (us.GetPlanets().Count - ourOffer.ColoniesOffered.Count + theirOffer.ColoniesOffered.Count < 1)
            {
                // todo not sure this is needed, better check the colony value
                usToThem.DamageRelationship(us, them, "Insulted", 25f, null);
                return "OfferResponse_Reject_Insulting";
            }
            foreach (string planetName in ourOffer.ColoniesOffered)
            {
                foreach (Planet p in us.GetPlanets())
                {
                    if (p.Name != planetName)
                        continue;

                    float worth = p.ColonyWorthTo(us);
                    foreach (Building b in p.BuildingList)
                        if (b.IsCapital)
                            worth += 200f;
                    float multiplier = 1.25f * p.ParentSystem.PlanetList.Count(other => other.Owner == p.Owner);
                    worth *= multiplier;
                    valueToThem += worth;
                }
            }
            foreach (string planetName in theirOffer.ColoniesOffered)
            {
                foreach (Planet p in them.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }
                    float worth = p.ColonyWorthTo(us);
                    int multiplier = 1 + p.ParentSystem.PlanetList.Count(other => other.Owner == p.Owner);
                    worth *= multiplier;
                    valueToUs += worth;
                }
            }
            if (!theirOffer.TradeTreaty && !theirOffer.NAPact || them.isPlayer)
                valueToUs += them.data.Traits.DiplomacyMod * valueToUs;

            if (valueToThem.AlmostZero() && valueToUs > 0f)
            {
                usToThem.ImproveRelations(valueToUs, valueToUs);
                AcceptOffer(ourOffer, theirOffer, us, them, attitude);
                ourOffer.AcceptDL = "OfferResponse_Accept_Gift";
                return "OfferResponse_Accept_Gift";
            }

            float angerMultiplier = them.isPlayer ? usToThem.TotalAnger / 100 : usToThem.Anger_DiplomaticConflict / 100;
            valueToUs -= valueToUs * angerMultiplier;
            valueToUs += 1 * them.data.OngoingDiplomaticModifier;
            OfferQuality offerQuality = ProcessQuality(valueToUs, valueToThem, out float offerDifferential);
            bool canImproveRelations  = themToUs.turnsSinceLastContact >= themToUs.SecondDemand; // So it wont be exploited by the player
            switch (attitude)
            {
                case Offer.Attitude.Pleading:
                    if (totalTrustRequiredFromUs > usToThem.Trust)
                    {
                        if (offerQuality != OfferQuality.Great)
                            return "OfferResponse_InsufficientTrust";

                        if (canImproveRelations)
                            usToThem.ImproveRelations(4f.UpperBound(valueToUs), 8);

                        AcceptOffer(ourOffer, theirOffer, us, them, attitude);
                        return "OfferResponse_AcceptGreatOffer_LowTrust";
                    }

                    switch (offerQuality)
                    {
                        case OfferQuality.Insulting:
                            usToThem.DamageRelationship(us, them, "Insulted", valueToThem - valueToUs, null);
                            return "OfferResponse_Reject_Insulting";
                        case OfferQuality.Poor:
                            return "OfferResponse_Reject_PoorOffer_EnoughTrust";
                        case OfferQuality.Fair:
                            if (canImproveRelations)
                                usToThem.ImproveRelations(valueToUs - valueToThem, valueToUs - valueToThem);

                            AcceptOffer(ourOffer, theirOffer, us, them, attitude);
                            return "OfferResponse_Accept_Fair_Pleading";
                        case OfferQuality.Good:
                            if (canImproveRelations)
                                usToThem.ImproveRelations(valueToUs - valueToThem, valueToUs - valueToThem);

                            AcceptOffer(ourOffer, theirOffer, us, them, attitude);
                            return "OfferResponse_Accept_Good";
                        case OfferQuality.Great:
                            usToThem.ImproveRelations(valueToUs - valueToThem, valueToUs - valueToThem);
                            AcceptOffer(ourOffer, theirOffer, us, them, attitude);
                            return "OfferResponse_Accept_Great";
                    }

                    break;
                case Offer.Attitude.Respectful:
                    if (totalTrustRequiredFromUs + usToThem.TrustUsed <= usToThem.Trust)
                    {
                        switch (offerQuality)
                        {
                            case OfferQuality.Insulting:
                                usToThem.DamageRelationship(us, them, "Insulted", valueToThem - valueToUs, null);

                                return "OfferResponse_Reject_Insulting";
                            case OfferQuality.Poor:
                                return "OfferResponse_Reject_PoorOffer_EnoughTrust";
                            case OfferQuality.Fair:
                                if (canImproveRelations)
                                    usToThem.ImproveRelations(2f.UpperBound(valueToUs), 4f);

                                AcceptOffer(ourOffer, theirOffer, us, them, attitude);
                                return "OfferResponse_Accept_Fair";
                            case OfferQuality.Good:
                                if (canImproveRelations)
                                    usToThem.ImproveRelations(3f.UpperBound(valueToUs), 6f);

                                AcceptOffer(ourOffer, theirOffer, us, them, attitude);
                                return "OfferResponse_Accept_Good";
                            case OfferQuality.Great:
                                if (canImproveRelations)
                                    usToThem.ImproveRelations(4f.UpperBound(valueToUs), 8f);

                                AcceptOffer(ourOffer, theirOffer, us, them, attitude);
                                return "OfferResponse_Accept_Great";
                        }
                    }

                    switch (offerQuality)
                    {
                        case OfferQuality.Insulting:
                            usToThem.DamageRelationship(us, them, "Insulted", valueToThem - valueToUs, null);
                            return "OfferResponse_Reject_Insulting";
                        case OfferQuality.Poor:
                            return "OfferResponse_Reject_PoorOffer_LowTrust";
                        case OfferQuality.Fair:
                            if (canImproveRelations)
                                usToThem.ImproveRelations(2f.UpperBound(valueToUs), 4);

                            return "OfferResponse_InsufficientTrust";
                        case OfferQuality.Good:
                            if (canImproveRelations)
                                usToThem.ImproveRelations(3f.UpperBound(valueToUs), 6);

                            return "OfferResponse_InsufficientTrust";
                        case OfferQuality.Great:
                            if (canImproveRelations)
                                usToThem.ImproveRelations(4f.UpperBound(valueToUs), 8);

                            AcceptOffer(ourOffer, theirOffer, us, them, attitude);
                            return "OfferResponse_AcceptGreatOffer_LowTrust";
                    }

                    break;
                case Offer.Attitude.Threaten:
                    if (dt.Name == "Ruthless")
                        return "OfferResponse_InsufficientFear";

                    usToThem.DamageRelationship(us, them, "Insulted", valueToThem - valueToUs, null);

                    if (offerQuality == OfferQuality.Great)
                    {
                        AcceptOffer(ourOffer, theirOffer, us, them, attitude);
                        return "OfferResponse_AcceptGreatOffer_LowTrust";
                    }

                    // Lower quality because of threatening attitude
                    offerQuality = offerDifferential < 0.95f ? OfferQuality.Poor : OfferQuality.Fair;

                    if (usToThem.Threat <= valueToThem || usToThem.FearUsed + valueToThem >= usToThem.Threat)
                    {
                        return "OfferResponse_InsufficientFear";
                    }

                    switch (offerQuality)
                    {
                        case OfferQuality.Poor:
                            AcceptOffer(ourOffer, theirOffer, us, them, attitude);
                            return "OfferResponse_Accept_Bad_Threatening";
                        case OfferQuality.Fair:
                            AcceptOffer(ourOffer, theirOffer, us, them, attitude);
                            return "OfferResponse_Accept_Fair_Threatening";
                    }

                    break;
            }

            return "";
        }

        PeaceAnswer AnalyzePeaceOffer(Offer theirOffer, Offer ourOffer, Empire them, Offer.Attitude attitude)
        {
            WarState state;
            Empire us             = OwnerEmpire;
            Relationship usToThem = us.GetRelations(them);
            float valueToUs       = 10 + theirOffer.ArtifactsOffered.Count * 15f; // default value is 10
            float valueToThem     = 10 + ourOffer.ArtifactsOffered.Count * 15f; // default value is 10


            if (usToThem.ActiveWar != null)
            {
                if (Empire.Universe.StarDate - usToThem.ActiveWar.StartDate < 100)
                    return ProcessPeace("REJECT_OFFER_PEACE_UNWILLING_BC");
            }

            foreach (string tech in ourOffer.TechnologiesOffered)
                valueToThem += ResourceManager.Tech(tech).DiplomaticValueTo(us);

            foreach (string tech in theirOffer.TechnologiesOffered)
                valueToUs += ResourceManager.Tech(tech).DiplomaticValueTo(us);

            foreach (string planetName in ourOffer.ColoniesOffered)
            {
                foreach (Planet p in us.GetPlanets())
                {
                    if (p.Name == planetName)
                        valueToThem += p.ColonyWorthTo(us);
                }
            }
            Array<Planet> planetsToUs = new Array<Planet>();
            foreach (string planetName in theirOffer.ColoniesOffered)
            {
                foreach (Planet p in them.GetPlanets())
                {
                    if (p.Name != planetName)
                        continue;
                    planetsToUs.Add(p);
                    float worth = p.ColonyWorthTo(us);
                    foreach (Building b in p.BuildingList)
                        if (b.IsCapital)
                            worth += 100000f; // basically, don't let AI give away their capital too easily
                    valueToUs += worth;
                }
            }

            valueToUs += valueToUs * them.data.Traits.DiplomacyMod; // TODO FB - need to be smarter here
            valueToUs *= us.AlliancesValueMultiplierThirdParty(them, out bool reject);

            float ourWarsGrade      = us.GetAverageWarGrade();
            float ourPeaceThreshold = us.PersonalityModifiers.WarGradeThresholdForPeace;
            valueToUs *= ourPeaceThreshold / ourWarsGrade; // If we are losing in our wars, this will increase the value of their offer

            OfferQuality offerQuality = ProcessQuality(valueToUs, valueToThem, out _);
            PeaceAnswer response      = ProcessPeace("REJECT_OFFER_PEACE_POOROFFER"); // Default response is reject
            if (reject)
            {
                response = ProcessPeace("REJECT_OFFER_PEACE_ALLIED_WITH_ENEMY");
                return response;
            }

            switch (usToThem.ActiveWar.WarType)
            {
                case WarType.BorderConflict:
                    state = usToThem.ActiveWar.GetBorderConflictState(planetsToUs);
                    switch (state)
                    {
                        case WarState.EvenlyMatched:
                        case WarState.WinningSlightly:
                        case WarState.LosingSlightly:
                            switch (offerQuality)
                            {
                                case OfferQuality.Fair when usToThem.ActiveWar.StartingNumContestedSystems > 0:
                                case OfferQuality.Good when usToThem.ActiveWar.StartingNumContestedSystems > 0:
                                    response = ProcessPeace("REJECT_OFFER_PEACE_UNWILLING_BC");
                                    break;
                                case OfferQuality.Fair:
                                case OfferQuality.Good:
                                case OfferQuality.Great:
                                    response = ProcessPeace("ACCEPT_OFFER_PEACE", true);
                                    break;
                            }

                            break;
                        case WarState.Dominating when offerQuality >= OfferQuality.Good:
                            response = ProcessPeace("ACCEPT_OFFER_PEACE", true);
                            break;
                        case WarState.ColdWar when offerQuality < OfferQuality.Great:
                            response = ProcessPeace("REJECT_OFFER_PEACE_UNWILLING_BC");
                            break;
                        case WarState.ColdWar: // Great offer for Cold war
                            response = ProcessPeace("ACCEPT_PEACE_COLDWAR", true);
                            break;
                        case WarState.LosingBadly: response = ProcessLosingBadly(offerQuality); 
                            break;
                    }

                    break;
                case WarType.DefensiveWar:
                case WarType.ImperialistWar:
                    state = usToThem.ActiveWar.GetWarScoreState();
                    switch (state)
                    {
                        case WarState.EvenlyMatched:
                        case WarState.LosingSlightly:
                        case WarState.WinningSlightly:
                            if (offerQuality >= OfferQuality.Fair)
                                response = ProcessPeace("ACCEPT_OFFER_PEACE", true);

                            break;
                        case WarState.Dominating when offerQuality >= OfferQuality.Good:
                            response = ProcessPeace("ACCEPT_OFFER_PEACE", true);
                            break;
                        case WarState.ColdWar: response = ProcessColdWar(offerQuality); 
                            break;
                        case WarState.LosingBadly: response = ProcessLosingBadly(offerQuality); 
                            break;
                    }

                    break;
            }

            return response; // Genocidal , Skirmish and NotApplicable are refused by default
        }

        PeaceAnswer ProcessColdWar(OfferQuality offerQuality)
        {
            string personality = OwnerEmpire.data.DiplomaticPersonality.Name;

            if (personality.NotEmpty() && personality == "Pacifist" && offerQuality >= OfferQuality.Fair)
                return ProcessPeace("ACCEPT_OFFER_PEACE", true);

            if (offerQuality == OfferQuality.Great)
                return ProcessPeace("ACCEPT_PEACE_COLDWAR", true);
            return ProcessPeace("REJECT_PEACE_RUTHLESS");
        }

        PeaceAnswer ProcessLosingBadly(OfferQuality offerQuality)
        {
            switch (offerQuality)
            {
                case OfferQuality.Fair:
                case OfferQuality.Good:
                case OfferQuality.Great: return ProcessPeace("ACCEPT_OFFER_PEACE", true);
                case OfferQuality.Poor:  return ProcessPeace("ACCEPT_OFFER_PEACE_RELUCTANT", true);
                default:                 return ProcessPeace("REJECT_OFFER_PEACE_POOROFFER"); // Insulting
            }
        }

        PeaceAnswer ProcessPeace(string answer, bool isPeace = false)
        {
            PeaceAnswer response = new PeaceAnswer
            {
                Peace  = isPeace,
                Answer = answer
            };

            return response;
        }

        public struct PeaceAnswer
        {
            public string Answer;
            public bool Peace;
        }

        OfferQuality ProcessQuality(float valueToUs, float valueToThem, out float offerDiff)
        {
            offerDiff = valueToUs / valueToThem.LowerBound(0.01f);

            if (offerDiff.AlmostEqual(1) && valueToUs > 0)
                return OfferQuality.Fair;

            if (offerDiff > 1.45f) return OfferQuality.Great;
            if (offerDiff > 1.1f)  return OfferQuality.Good;
            if (offerDiff > 0.9f)  return OfferQuality.Fair;
            if (offerDiff > 0.65f) return OfferQuality.Poor;

            return OfferQuality.Insulting;
        }

        enum OfferQuality
        {
            Insulting,
            Poor,
            Fair,
            Good,
            Great
        }
    }
}