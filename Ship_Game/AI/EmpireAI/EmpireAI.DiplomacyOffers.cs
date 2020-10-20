using System.Linq;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        void AcceptNAPact(Empire us, Empire them,  Offer.Attitude attitude)
        {
            us.SignTreatyWith(them, TreatyType.NonAggression);

            float cost = 0f;
            if (Empire.Universe.PlayerEmpire != us)
            {
                switch (us.Personality)
                {
                    case PersonalityType.Pacifist: 
                    case PersonalityType.Cunning:    cost = 0f;  break;
                    case PersonalityType.Xenophobic: cost = 15f; break;
                    case PersonalityType.Aggressive: cost = 35f; break;
                    case PersonalityType.Honorable:  cost = 5f;  break;
                    case PersonalityType.Ruthless:   cost = 50f; break;
                }
            }

            Relationship usToThem = us.GetRelations(them);
            usToThem.AddTrustEntry(attitude, TrustEntryType.Treaty, cost);
        }

        void AcceptTradeTreaty(Empire us, Empire them, Offer.Attitude attitude)
        {
            us.SignTreatyWith(them, TreatyType.Trade);

            Relationship usToThem = us.GetRelations(them);
            usToThem.AddTrustEntry(attitude, TrustEntryType.Treaty, 0.1f);
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

        bool AllianceAccepted(Offer theirOffer, Offer ourOffer, Relationship usToThem, Empire them, out string text)
        {
            text = "";
            if (!theirOffer.Alliance)
                return false;

            bool allianceGood = false;
            if (!theirOffer.IsBlank() || !ourOffer.IsBlank())
            {
                text = "OFFER_ALLIANCE_TOO_COMPLICATED";
            }
            else if (usToThem.Trust < 90f || usToThem.TotalAnger >= 20f || usToThem.TurnsKnown <= 100)
            {
                text = "AI_ALLIANCE_REJECT";
            }
            else
            {
                usToThem.SetAlliance(true, OwnerEmpire, them);
                text = "AI_ALLIANCE_ACCEPT";
                allianceGood = true;
            }

            return allianceGood;
        }

        bool PeaceAccepted(Offer theirOffer, Offer ourOffer, Empire them,
                           Offer.Attitude attitude, out string text)
        {
            text = "";
            if (!theirOffer.PeaceTreaty)
                return false;

            bool isPeace = false;
            PeaceAnswer answer = AnalyzePeaceOffer(theirOffer, ourOffer, them, attitude);
            if (answer.Peace)
            {
                AcceptOffer(ourOffer, theirOffer, OwnerEmpire, them, attitude);
                isPeace = true;
            }

            text = answer.Answer;
            return isPeace;
        }

        public string AnalyzeOffer(Offer theirOffer, Offer ourOffer, Empire them, Offer.Attitude attitude)
        {
            Empire us = OwnerEmpire;
            Relationship usToThem = us.GetRelations(them);
            Relationship themToUs = them.GetRelations(us);

            if (AllianceAccepted(theirOffer, ourOffer, usToThem, them, out string allianceText))
                return allianceText;

            if (PeaceAccepted(theirOffer, ourOffer, them, attitude, out string peaceText))
                return peaceText;

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
            if (theirOffer.OpenBorders) valueToUs   += 0.01f;
            if (ourOffer.NAPact)        valueToThem += 10f;
            if (theirOffer.NAPact)      valueToUs   += 10f;
            if (ourOffer.TradeTreaty)   valueToThem += them.EstimateNetIncomeAtTaxRate(0.5f) < 5 ? 20f : 10f;
            if (theirOffer.TradeTreaty) valueToUs   += OwnerEmpire.EstimateNetIncomeAtTaxRate(0.5f) < 5 ? 20f : 10f;

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
            valueToUs += them.data.Traits.DiplomacyMod * valueToUs;
            if (valueToThem.AlmostZero() && valueToUs > 0f)
            {
                usToThem.ImproveRelations(valueToUs, valueToUs);
                AcceptOffer(ourOffer, theirOffer, us, them, attitude);
                return "OfferResponse_Accept_Gift";
            }
            valueToUs -= valueToUs * usToThem.Anger_DiplomaticConflict / 100f;
            OfferQuality offerQuality = ProcessQuality(valueToUs, valueToThem, out float offerDifferential);
            switch (attitude)
            {
                case Offer.Attitude.Pleading:
                    if (totalTrustRequiredFromUs > usToThem.Trust)
                    {
                        if (offerQuality != OfferQuality.Great)
                            return "OfferResponse_InsufficientTrust";

                        usToThem.ImproveRelations(valueToUs - valueToThem, valueToUs - valueToThem);
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
                            usToThem.ImproveRelations(valueToUs - valueToThem, valueToUs - valueToThem);
                            AcceptOffer(ourOffer, theirOffer, us, them, attitude);
                            return "OfferResponse_Accept_Fair_Pleading";
                        case OfferQuality.Good:
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
                                usToThem.ImproveRelations(valueToUs - valueToThem, valueToUs - valueToThem);
                                AcceptOffer(ourOffer, theirOffer, us, them, attitude);
                                return "OfferResponse_Accept_Fair";
                            case OfferQuality.Good:
                                usToThem.ImproveRelations(valueToUs - valueToThem, valueToUs - valueToThem);
                                AcceptOffer(ourOffer, theirOffer, us, them, attitude);
                                return "OfferResponse_Accept_Good";
                            case OfferQuality.Great:
                                usToThem.ImproveRelations(valueToUs - valueToThem, valueToUs - valueToThem);
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
                            return "OfferResponse_InsufficientTrust";
                        case OfferQuality.Good:
                            if (themToUs.turnsSinceLastContact >= themToUs.TechTradeTurns) // So it wont be exploited by the player
                                usToThem.ImproveRelations(2f.UpperBound(valueToUs), 5);

                            return "OfferResponse_InsufficientTrust";
                        case OfferQuality.Great:
                            usToThem.ImproveRelations(valueToUs - valueToThem, valueToUs);
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
            Empire us = OwnerEmpire;
            Relationship usToThem = us.GetRelations(them);
            DTrait personality = us.data.DiplomaticPersonality;
            float valueToUs    = theirOffer.ArtifactsOffered.Count * 15f;
            float valueToThem  = ourOffer.ArtifactsOffered.Count * 15f;

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

            if (personality.Name.NotEmpty())
            {
                WarType warType = usToThem.ActiveWar.WarType;
                WarState warState = WarState.NotApplicable;
                switch (warType)
                {
                    case WarType.BorderConflict: warState = usToThem.ActiveWar.GetBorderConflictState(planetsToUs); break;
                    case WarType.ImperialistWar: warState = usToThem.ActiveWar.GetWarScoreState();                  break;
                    case WarType.DefensiveWar:   warState = usToThem.ActiveWar.GetWarScoreState();                  break;
                }

                switch (us.Personality)
                {
                    case PersonalityType.Pacifist:
                    case PersonalityType.Honorable when warType == WarType.DefensiveWar:
                        AddToValue(warState, 10, 5, 5, 10, ref valueToUs, ref valueToThem); break;
                    case PersonalityType.Honorable:
                        AddToValue(warState, 15, 8, 8, 15, ref valueToUs, ref valueToThem); break;
                    case PersonalityType.Xenophobic when warType == WarType.DefensiveWar:
                        AddToValue(warState, 10, 5, 5, 10, ref valueToUs, ref valueToThem); break;
                    case PersonalityType.Xenophobic:
                        AddToValue(warState, 15, 8, 8, 15, ref valueToUs, ref valueToThem); break;
                    case PersonalityType.Aggressive:
                        AddToValue(warState, 10, 5, 75, 200, ref valueToUs, ref valueToThem); break;
                    case PersonalityType.Ruthless:
                        AddToValue(warState, 5, 1, 120, 300, ref valueToUs, ref valueToThem); break;
                    case PersonalityType.Cunning:
                        AddToValue(warState, 10, 5, 5, 10, ref valueToUs, ref valueToThem); break;
                }
            }

            valueToUs += valueToUs * them.data.Traits.DiplomacyMod; // TODO FB - need to be smarter here
            OfferQuality offerQuality = ProcessQuality(valueToUs, valueToThem, out _);
            PeaceAnswer response      = ProcessPeace("REJECT_OFFER_PEACE_POOROFFER"); // Default response is reject
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

        void AddToValue(WarState warState, float losingBadly, float losingSlightly, float winningSlightly, float dominating, 
            ref float valueToUs, ref float valueToThem)
        {
            switch (warState)
            {
                case WarState.LosingBadly:     valueToUs   += losingBadly;       break;
                case WarState.LosingSlightly:  valueToUs   += losingSlightly;    break;
                case WarState.WinningSlightly: valueToThem += winningSlightly;   break;
                case WarState.Dominating:      valueToThem += dominating;        break;
            }
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