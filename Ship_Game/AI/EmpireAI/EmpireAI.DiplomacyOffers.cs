using SDGraphics;
using SDUtils;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using static Ship_Game.Offer;

namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        float ArtifactValue => OwnerEmpire.Research.MaxResearchPotential.LowerBound(30)
                               * (1 + OwnerEmpire.data.Traits.Spiritual)
                               * OwnerEmpire.Universe.SettingsResearchModifier.LowerBound(1);

        void AcceptNAPact(Empire us, Empire them,  Attitude attitude, int trustUsageDurationTurns)
        {
            us.SignTreatyWith(them, TreatyType.NonAggression);
            Relationship usToThem = us.GetRelations(them);
            usToThem.AddTrustEntry(attitude, TrustEntryType.Treaty, OwnerEmpire.PersonalityModifiers.TrustCostNaPact, trustUsageDurationTurns);
        }

        void AcceptTradeTreaty(Empire us, Empire them, Attitude attitude, int trustUsageDurationTurns)
        {
            us.SignTreatyWith(them, TreatyType.Trade);
            Relationship usToThem = us.GetRelations(them);
            usToThem.AddTrustEntry(attitude, TrustEntryType.Treaty, OwnerEmpire.PersonalityModifiers.TrustCostTradePact, trustUsageDurationTurns);
        }

        void AcceptBorderTreaty(Empire us, Empire them, Attitude attitude, int trustUsageDurationTurns)
        {
            us.SignTreatyWith(them, TreatyType.OpenBorders);
            Relationship usToThem = us.GetRelations(them);
            usToThem.AddTrustEntry(attitude, TrustEntryType.Treaty, 5f, trustUsageDurationTurns);
        }

        void GiveTechs(Empire us, Empire them, Array<string> techs, Attitude attitude, float offerValueRatioToThem)
        {
            if (techs.Count == 0) 
                return;

            Relationship usToThem = us.GetRelations(them);
            float totalCost = 0;
            foreach (string tech in techs)
            {
                them.UnlockTech(tech, TechUnlockType.Diplomacy, us);
                usToThem.NumTechsWeGave += 1;
                Log.Info(System.ConsoleColor.White, $"{us.Name} gave {tech} to {them.Name}");
                totalCost += ResourceManager.Tech(tech).DiplomaticValueTo(us, them);
            }

            if (!us.isPlayer)
                usToThem.AddTrustEntry(attitude, TrustEntryType.Technology, totalCost, turnTimer: (int)(50 * offerValueRatioToThem));
        }

        void GiveArtifacts(Empire us, Empire them, Array<string> artifacts, Attitude attitude, float offerValueRatioToThem)
        {
            if (artifacts.Count == 0)
                return;

            foreach (string art in artifacts)
            {
                Artifact toGive = ResourceManager.ArtifactsDict[art];
                foreach (Artifact artifact in us.data.OwnedArtifacts)
                {
                    if (artifact.Name == art)
                        toGive = artifact;
                }
                us.RemoveArtifact(toGive);
                them.AddArtifact(toGive);
            }

            if (!us.isPlayer)
            {
                Relationship usToThem = us.GetRelations(them);
                usToThem.AddTrustEntry(attitude, TrustEntryType.Technology, 50 * artifacts.Count, turnTimer: (int)(250 * offerValueRatioToThem));
            }
        }

        void GiveColonies(Empire us, Empire them, Array<string> colonies, Attitude attitude, float offerValueRatioToThem)
        {
            if (colonies.Count == 0)
                return;

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
                            troopShips.Add(troop.Launch(forceLaunch: true));
                        }
                    }

                    foreach (Ship orbital in p.OrbitalStations)
                        orbital.LoyaltyChangeByGift(them);

                    toRemove.Add(p);
                    p.SetOwner(them);

                    p.System.OwnerList.Clear();
                    foreach (Planet pl in p.System.PlanetList)
                    {
                        if (pl.Owner != null && !p.System.OwnerList.Contains(pl.Owner))
                            p.System.OwnerList.Add(pl.Owner);
                    }

                    if (!them.isPlayer)
                    {
                        p.CType = them.AssessColonyNeeds(p);
                    }

                    if (!us.isPlayer)
                    {
                        float ourValue = p.ColonyDiplomaticValueTo(us);
                        float theirValue = p.ColonyDiplomaticValueTo(them);
                        float trust = (ourValue + theirValue) * 0.5f;
                        usToThem.AddTrustEntry(attitude, TrustEntryType.Colony, trust.Clamped(5,100), (int)(250 * offerValueRatioToThem));
                    }
                }

                foreach (Planet planetToRemove in toRemove)
                    us.RemovePlanet(planetToRemove);
                foreach (Ship ship in troopShips)
                    ship.AI.OrderRebaseToNearest();
            }
        }

        void ProcessOffer(Empire us, Empire them, Offer ourOffer, Attitude attitude, float offerValueRatioToThem)
        {
            int treatyTrustEnrtyDurationTurns = (int)(250 * offerValueRatioToThem);
            if (ourOffer.NAPact)
                AcceptNAPact(them, us, attitude, treatyTrustEnrtyDurationTurns);

            if (ourOffer.TradeTreaty)
                AcceptTradeTreaty(them, us, attitude, treatyTrustEnrtyDurationTurns);

            if (ourOffer.OpenBorders) // if we offer BorderTreaty, then THEY need to accept it
                AcceptBorderTreaty(them, us, attitude, treatyTrustEnrtyDurationTurns);

            GiveTechs(us, them, ourOffer.TechnologiesOffered, attitude, offerValueRatioToThem);
            GiveArtifacts(us, them, ourOffer.ArtifactsOffered, attitude, offerValueRatioToThem);
            GiveColonies(us, them, ourOffer.ColoniesOffered, attitude, offerValueRatioToThem);
        }

        void AcceptPeaceTreaty(Empire us, Empire them)
        {
            Relationship rel = us.GetRelations(them);
            rel.AtWar = false;
            rel.CancelPrepareForWar();
            rel.ActiveWar.EndStarDate = us.Universe.StarDate;
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
                us.GetFleet(rel.DefenseFleet).FleetTask.EndTask();

            us.AI.RemoveMilitaryTasksTargeting(them);
            rel.ActiveWar = null;
        }

        public void AcceptOffer(Offer ourOffer, Offer theirOffer, Empire us, Empire them, Attitude attitude, float offerValueRatioToThem)
        {
            if (ourOffer.PeaceTreaty || theirOffer.PeaceTreaty)
            {
                Relationship rel = OwnerEmpire.GetRelations(them);
                bool neededPeace = them.isPlayer  // player asked peace since they is in a real bad state
                                     && rel.ActiveWar.GetWarScoreState() == WarState.Dominating
                                     && them.TotalPopBillion < OwnerEmpire.TotalPopBillion / (int)(us.Universe.P.Difficulty + 1);

                if (!neededPeace && them.TheyAreAlliedWithOurEnemies(OwnerEmpire, out Array<Empire> empiresAlliedWithThem))
                    CheckAIEmpiresResponse(OwnerEmpire, empiresAlliedWithThem, them, true);

                us.SignTreatyWith(them, TreatyType.Peace);
                AcceptPeaceTreaty(us, them);
                AcceptPeaceTreaty(them, us);

                if (us.isPlayer || them.isPlayer ||
                    (us.Universe.Player.IsKnown(us) &&
                     us.Universe.Player.IsKnown(them)))
                {
                    us.Universe.Notifications.AddPeaceTreatyEnteredNotification(us, them);
                }
            }

            ProcessOffer(us, them, ourOffer, attitude, offerValueRatioToThem.Clamped(0.2f, 5));
            ProcessOffer(them, us, theirOffer, attitude, offerValueRatioToThem == 0 ? 5 : 1 / offerValueRatioToThem.Clamped(0.2f, 5));

            Empire.UpdateBilateralRelations(us, them);
        }

        string ProcessAlliance(Offer theirOffer, Offer ourOffer, Relationship usToThem, Empire them)
        {
            string answer;
            if (!theirOffer.IsBlank() || !ourOffer.IsBlank())
            {
                answer = "OFFER_ALLIANCE_TOO_COMPLICATED";
            }
            else if (them.isPlayer 
                && (usToThem.TurnsInOpenBorders < 100 * OwnerEmpire.Universe.P.Pace
                    || usToThem.TurnsAbove95 < OwnerEmpire.PersonalityModifiers.TurnsAbove95AllianceTreshold * them.Universe.P.Pace))
            {
                answer = "TREATY_TOO_SOON_REJECT";
            }
            else if (usToThem.AvailableTrust < OwnerEmpire.data.DiplomaticPersonality.Alliance + usToThem.TotalAnger 
                || usToThem.TurnsKnown <= 100 * them.Universe.P.Pace)
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
                            Attitude attitude)
        {
            PeaceAnswer answer = AnalyzePeaceOffer(theirOffer, ourOffer, them);
            Relationship rel   = OwnerEmpire.GetRelations(them);
            bool neededPeace   = them.isPlayer  // player asked peace since they is in a real bad state
                                 && rel.ActiveWar.GetWarScoreState() == WarState.Dominating
                                 && them.TotalPopBillion < OwnerEmpire.TotalPopBillion / (int)(them.Universe.P.Difficulty + 1);

            if (answer.Peace)
                AcceptOffer(ourOffer, theirOffer, OwnerEmpire, them, attitude, 1);

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

        public string AnalyzeOffer(Offer theirOffer, Offer ourOffer, Empire them, Attitude attitude, bool resetTurnsSinceLastContacted = true)
        {
            Empire us = OwnerEmpire;
            Relationship usToThem = us.GetRelations(them);
            Relationship themToUs = them.GetRelations(us);

            if (theirOffer.Alliance)
                return ProcessAlliance(theirOffer, ourOffer, usToThem, them);

            if (theirOffer.PeaceTreaty)
                return ProcessPeace(theirOffer, ourOffer, them, attitude);


            float treayThreshold = 100 * OwnerEmpire.Universe.P.Pace;
            if (them.isPlayer)
            {
                if (theirOffer.NAPact && usToThem.TurnsKnown < treayThreshold
                    || theirOffer.TradeTreaty && usToThem.TurnsInNap < treayThreshold
                    || theirOffer.OpenBorders && usToThem.Treaty_Trade_TurnsExisted < treayThreshold)
                {
                    return "TREATY_TOO_SOON_REJECT";
                }
            }

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
                foreach (Relationship rel in us.AllRelations)
                {
                    if (rel.AtWar && !rel.Them.IsFaction)
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
                valueToUs += ResourceManager.Tech(tech).DiplomaticValueTo(us, them);
            }

            if (ourOffer.TechnologiesOffered.Count > 0)
            {
                foreach (string tech in ourOffer.TechnologiesOffered)
                {
                    float value = ResourceManager.Tech(tech).DiplomaticValueTo(them, us);
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

            valueToThem += ourOffer.ArtifactsOffered.Count * ArtifactValue;
            valueToUs   += theirOffer.ArtifactsOffered.Count * ArtifactValue;

            foreach (string planetName in ourOffer.ColoniesOffered)
            {
                foreach (Planet p in us.GetPlanets())
                {
                    if (p.Name != planetName)
                        continue;

                    float worth = p.ColonyDiplomaticValueTo(us);
                    worth += p.HasCapital ? 200 : 0;
                    float multiplier = 1 + (p.System.PlanetList.Count(other => other.Owner == us)*0.25f);
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
                    float worth = p.ColonyDiplomaticValueTo(us);
                    float multiplier = 1 + (p.System.PlanetList.Count(other => other.Owner == them)*0.2f);
                    worth *= multiplier;
                    valueToUs += worth;
                }
            }
            if (!theirOffer.TradeTreaty && !theirOffer.NAPact || them.isPlayer)
                valueToUs += them.data.Traits.DiplomacyMod * valueToUs;

            if (valueToThem.AlmostZero() && valueToUs > 0f)
            {
                float modifier = 1 / ((int)us.Universe.P.Difficulty).LowerBound(1);
                usToThem.ImproveRelations(valueToUs.UpperBound(25/ modifier), valueToUs.UpperBound(50 / modifier));
                AcceptOffer(ourOffer, theirOffer, us, them, attitude, 0.2f);
                ourOffer.AcceptDL = "OfferResponse_Accept_Gift";
                return "OfferResponse_Accept_Gift";
            }

            float angerMultiplier = them.isPlayer ? usToThem.TotalAnger / 200 : usToThem.Anger_DiplomaticConflict / 200;
            valueToUs -= valueToUs * angerMultiplier;
            valueToUs += 1 * them.data.OngoingDiplomaticModifier;
            OfferQuality offerQuality = ProcessQuality(valueToUs, valueToThem, out float offerDifferential);
            bool canImproveRelations  = themToUs.turnsSinceLastContact >= themToUs.SecondDemand; // So it wont be exploited by the player
            if (resetTurnsSinceLastContacted)
                themToUs.turnsSinceLastContact = 0;

            float valuetoThemRatio = valueToUs <= 0 ? 2 : valueToThem / valueToUs;
            switch (attitude)
            {
                case Attitude.Pleading:
                    if (totalTrustRequiredFromUs > usToThem.Trust)
                    {
                        if (offerQuality is OfferQuality.Great)
                        {
                            if (canImproveRelations)
                                usToThem.ImproveRelations(4f.UpperBound(valueToUs), 8);

                            AcceptOffer(ourOffer, theirOffer, us, them, attitude, valuetoThemRatio);
                            return "OfferResponse_AcceptGreatOffer_LowTrust";
                        }
                        else
                        {
                            return "OfferResponse_InsufficientTrust";
                        }
                    }

                    float improveRalationsRate = valueToUs - valueToThem;
                    switch (offerQuality)
                    {
                        case OfferQuality.Insulting:
                            usToThem.DamageRelationship(us, them, "Insulted", valueToThem - valueToUs, null);
                            return "OfferResponse_Reject_Insulting";
                        case OfferQuality.Poor:
                            return "OfferResponse_Reject_PoorOffer_EnoughTrust";
                        case OfferQuality.Fair:
                            if (canImproveRelations)
                                usToThem.ImproveRelations(improveRalationsRate, improveRalationsRate);

                            AcceptOffer(ourOffer, theirOffer, us, them, attitude, valuetoThemRatio);
                            return "OfferResponse_Accept_Fair_Pleading";
                        case OfferQuality.Good:
                            if (canImproveRelations)
                                usToThem.ImproveRelations(improveRalationsRate, improveRalationsRate);

                            AcceptOffer(ourOffer, theirOffer, us, them, attitude, valuetoThemRatio);
                            return "OfferResponse_Accept_Good";
                        case OfferQuality.Great:
                            if (!canImproveRelations)
                                improveRalationsRate *= 0.25f;

                            usToThem.ImproveRelations(improveRalationsRate, improveRalationsRate);
                            AcceptOffer(ourOffer, theirOffer, us, them, attitude, valuetoThemRatio);
                            return "OfferResponse_Accept_Great";
                    }

                    break;
                case Attitude.Respectful:
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

                                AcceptOffer(ourOffer, theirOffer, us, them, attitude, valuetoThemRatio);
                                return "OfferResponse_Accept_Fair";
                            case OfferQuality.Good:
                                if (canImproveRelations)
                                    usToThem.ImproveRelations(3f.UpperBound(valueToUs), 6f);

                                AcceptOffer(ourOffer, theirOffer, us, them, attitude, valuetoThemRatio);
                                return "OfferResponse_Accept_Good";
                            case OfferQuality.Great:
                                if (canImproveRelations)
                                    usToThem.ImproveRelations(4f.UpperBound(valueToUs), 8f);

                                AcceptOffer(ourOffer, theirOffer, us, them, attitude, valuetoThemRatio);
                                return "OfferResponse_Accept_Great";
                        }
                    }

                    return "OfferResponse_Reject_PoorOffer_EnoughTrust";
                case Attitude.Threaten:
                    DamageRelationsAllied();
                    if (us.IsRuthless || usToThem.TurnsSinceLastThreathened < usToThem.ThreatenedTurnsThreshold)
                        return "OfferResponse_InsufficientFear";

                    usToThem.TurnsSinceLastThreathened = 0;
                    if (offerQuality == OfferQuality.Great)
                    {
                        AcceptOffer(ourOffer, theirOffer, us, them, attitude, valuetoThemRatio);
                        return "OfferResponse_AcceptGreatOffer_LowTrust";
                    }

                    // Lower quality because of threatening attitude
                    offerQuality = offerDifferential < 0.95f ? OfferQuality.Poor : OfferQuality.Fair;
                    float threat = usToThem.Threat.UpperBound(100); // allied threat
                    if (threat <= valueToThem || usToThem.FearUsed + valueToThem >= threat)
                        return "OfferResponse_InsufficientFear";

                    switch (offerQuality)
                    {
                        case OfferQuality.Poor:
                            AcceptOffer(ourOffer, theirOffer, us, them, attitude, valuetoThemRatio);
                            return "OfferResponse_Accept_Bad_Threatening";
                        case OfferQuality.Fair:
                            AcceptOffer(ourOffer, theirOffer, us, them, attitude, valuetoThemRatio);
                            return "OfferResponse_Accept_Fair_Threatening";
                    }

                    break;
            }

            return "";

            void DamageRelationsAllied()
            {
                usToThem.DamageRelationship(us, them, "Insulted", valueToThem - valueToUs, null);
                foreach (Empire ally in us.Universe.GetAllies(us))
                {
                    if (ally != them && !ally.isPlayer)
                        ally.DamageRelationship(them, "Insulted", (valueToThem - valueToUs)*0.5f, null);
                }
            }
        }

        PeaceAnswer AnalyzePeaceOffer(Offer theirOffer, Offer ourOffer, Empire them)
        {
            WarState state;
            Empire us             = OwnerEmpire;
            Relationship usToThem = us.GetRelations(them);
            float valueToUs       = 10 + theirOffer.ArtifactsOffered.Count * ArtifactValue; // default value is 10
            float valueToThem     = 10 + ourOffer.ArtifactsOffered.Count * ArtifactValue; // default value is 10

            if (usToThem.ActiveWar != null)
            {
                if (us.Universe.StarDate - usToThem.ActiveWar.StartDate < (10 * us.Universe.P.Pace))
                    return ProcessPeace("REJECT_OFFER_PEACE_UNWILLING_BC");
            }

            foreach (string tech in ourOffer.TechnologiesOffered)
                valueToThem += ResourceManager.Tech(tech).DiplomaticValueTo(us, them);

            foreach (string tech in theirOffer.TechnologiesOffered)
                valueToUs += ResourceManager.Tech(tech).DiplomaticValueTo(us, them);

            foreach (string planetName in ourOffer.ColoniesOffered)
            {
                foreach (Planet p in us.GetPlanets())
                {
                    if (p.Name == planetName)
                        valueToThem += p.ColonyDiplomaticValueTo(us);
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
                    float worth = p.ColonyDiplomaticValueTo(us);
                    worth += p.HasCapital ? 100000f : 0; // don't let AI give away their capital too easily
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