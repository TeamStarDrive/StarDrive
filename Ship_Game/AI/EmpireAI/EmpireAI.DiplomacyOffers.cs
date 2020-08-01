using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Collections.Generic;

namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        public void AcceptOffer(Offer theirOffer, Offer ourOffer, Empire us, Empire them)
        {
            if (theirOffer.PeaceTreaty)
            {
                Relationship usToThem          = OwnerEmpire.GetRelations(them);
                usToThem.AtWar                 = false;
                usToThem.PreparingForWar       = false;
                usToThem.ActiveWar.EndStarDate = Empire.Universe.StarDate;
                usToThem.WarHistory.Add(usToThem.ActiveWar);
                DTrait ourPersonality = OwnerEmpire.data.DiplomaticPersonality;
                if (ourPersonality != null)
                {
                    usToThem.ChangeToNeutral();
                    float borderAngerToReduce = usToThem.Anger_FromShipsInOurBorders - ourPersonality.Territorialism / 3f;
                    if (borderAngerToReduce > 0)
                        usToThem.AddAngerShipsInOurBorders(-borderAngerToReduce);

                    float territoryAngerToReduce = usToThem.Anger_TerritorialConflict - ourPersonality.Territorialism / 3f;
                    if (territoryAngerToReduce > 0)
                        usToThem.AddAngerTerritorialConflict(-borderAngerToReduce);
                }
                usToThem.ResetAngerMilitaryConflict();
                usToThem.WarnedAboutShips = false;
                usToThem.WarnedAboutColonizing = false;
                usToThem.HaveRejectedDemandTech = false;
                usToThem.HaveRejected_OpenBorders = false;
                usToThem.HaveRejected_TRADE = false;
                usToThem.HasDefenseFleet = false;
                if (usToThem.DefenseFleet != -1)
                    OwnerEmpire.GetFleetsDict()[usToThem.DefenseFleet].FleetTask.EndTask();

                RemoveMilitaryTasksTargeting(them);

                usToThem.ActiveWar = null;
                Relationship themToUs = them.GetRelations(OwnerEmpire);
                themToUs.AtWar = false;
                themToUs.PreparingForWar = false;
                themToUs.ActiveWar.EndStarDate = Empire.Universe.StarDate;
                themToUs.WarHistory.Add(themToUs.ActiveWar);
                themToUs.ChangeToNeutral();
                if (EmpireManager.Player != them)
                {
                    float borderAngerToReduce = themToUs.Anger_FromShipsInOurBorders - them.data.DiplomaticPersonality.Territorialism / 3f;
                    if (borderAngerToReduce > 0)
                        themToUs.AddAngerShipsInOurBorders(-borderAngerToReduce);

                    float territoryAngerToReduce = themToUs.Anger_TerritorialConflict - them.data.DiplomaticPersonality.Territorialism / 3f;
                    if (territoryAngerToReduce > 0)
                        themToUs.AddAngerTerritorialConflict(-territoryAngerToReduce);

                    themToUs.ResetAngerMilitaryConflict();
                    themToUs.WarnedAboutShips = false;
                    themToUs.WarnedAboutColonizing = false;
                    themToUs.HaveRejectedDemandTech = false;
                    themToUs.HaveRejected_OpenBorders = false;
                    themToUs.HaveRejected_TRADE = false;
                    if (themToUs.DefenseFleet != -1)
                    {
                        them.GetFleetsDict()[themToUs.DefenseFleet].FleetTask.EndTask();
                    }

                    them.GetEmpireAI().RemoveMilitaryTasksTargeting(OwnerEmpire);
                }
                themToUs.ActiveWar = null;
                if (them == Empire.Universe.PlayerEmpire || OwnerEmpire == Empire.Universe.PlayerEmpire)
                {
                    Empire.Universe.NotificationManager.AddPeaceTreatyEnteredNotification(OwnerEmpire, them);
                }
                else if (Empire.Universe.PlayerEmpire.GetRelations(them).Known &&
                         Empire.Universe.PlayerEmpire.GetRelations(OwnerEmpire).Known)
                {
                    Empire.Universe.NotificationManager.AddPeaceTreatyEnteredNotification(OwnerEmpire, them);
                }
            }
            if (theirOffer.NAPact)
            {
                us.SignTreatyWith(them, TreatyType.NonAggression);
                TrustEntry te = new TrustEntry();
                if (Empire.Universe.PlayerEmpire != us)
                {
                    switch (us.Personality)
                    {
                        case PersonalityType.Pacifist: 
                        case PersonalityType.Cunning:    te.TrustCost = 0f;  break;
                        case PersonalityType.Xenophobic: te.TrustCost = 15f; break;
                        case PersonalityType.Aggressive: te.TrustCost = 35f; break;
                        case PersonalityType.Honorable:  te.TrustCost = 5f;  break;
                        case PersonalityType.Ruthless:   te.TrustCost = 50f; break;
                    }
                }
                te.Type = TrustEntryType.Treaty;
                us.GetRelations(them).TrustEntries.Add(te);
            }
            if (ourOffer.NAPact)
            {
                them.SignTreatyWith(us, TreatyType.NonAggression);
                if (Empire.Universe.PlayerEmpire != them)
                {
                    TrustEntry te = new TrustEntry();
                    switch (them.Personality)
                    {
                        case PersonalityType.Pacifist:
                        case PersonalityType.Cunning:    te.TrustCost = 0f;  break;
                        case PersonalityType.Xenophobic: te.TrustCost = 15f; break;
                        case PersonalityType.Aggressive: te.TrustCost = 35f; break;
                        case PersonalityType.Honorable:  te.TrustCost = 5f;  break;
                        case PersonalityType.Ruthless:   te.TrustCost = 50f; break;
                    }

                    te.Type = TrustEntryType.Treaty;
                    them.GetRelations(us).TrustEntries.Add(te);
                }
            }
            if (theirOffer.TradeTreaty)
            {
                us.SignTreatyWith(them, TreatyType.Trade);
                TrustEntry te = new TrustEntry
                {
                    TrustCost = 0.1f,
                    Type = TrustEntryType.Treaty
                };
                us.GetRelations(them).TrustEntries.Add(te);
            }
            if (ourOffer.TradeTreaty)
            {
                them.SignTreatyWith(us, TreatyType.Trade);
                TrustEntry te = new TrustEntry
                {
                    TrustCost = 0.1f,
                    Type = TrustEntryType.Treaty
                };
                them.GetRelations(us).TrustEntries.Add(te);
            }
            if (theirOffer.OpenBorders)
            {
                us.SignTreatyWith(them, TreatyType.OpenBorders);
                TrustEntry te = new TrustEntry
                {
                    TrustCost = 5f,
                    Type = TrustEntryType.Treaty
                };
                us.GetRelations(them).TrustEntries.Add(te);
            }
            if (ourOffer.OpenBorders)
            {
                them.SignTreatyWith(us, TreatyType.OpenBorders);
                TrustEntry te = new TrustEntry
                {
                    TrustCost = 5f,
                    Type = TrustEntryType.Treaty
                };
                them.GetRelations(us).TrustEntries.Add(te);
            }
            foreach (string tech in ourOffer.TechnologiesOffered)
            {
                //Added by McShooterz:
                //Them.UnlockTech(tech);
                them.AcquireTech(tech, us, TechUnlockType.Diplomacy);
                if (Empire.Universe.PlayerEmpire == us)
                {
                    continue;
                }
                TrustEntry te = new TrustEntry
                {
                    TrustCost = ResourceManager.Tech(tech).DiplomaticValueTo(us),
                    TurnTimer = 40,
                    Type = TrustEntryType.Technology
                };
                us.GetRelations(them).TrustEntries.Add(te);
            }
            foreach (string tech in theirOffer.TechnologiesOffered)
            {
                //Added by McShooterz:
                us.AcquireTech(tech, them, TechUnlockType.Diplomacy);
                if (Empire.Universe.PlayerEmpire == them)
                {
                    continue;
                }
                TrustEntry te = new TrustEntry
                {
                    TrustCost = ResourceManager.Tech(tech).DiplomaticValueTo(them),
                    Type = TrustEntryType.Treaty
                };
                them.GetRelations(us).TrustEntries.Add(te);
            }
            foreach (string art in ourOffer.ArtifactsOffered)
            {
                Artifact toGive = ResourceManager.ArtifactsDict[art];
                foreach (Artifact artifact in us.data.OwnedArtifacts)
                {
                    if (artifact.Name != art)
                    {
                        continue;
                    }
                    toGive = artifact;
                }
                us.RemoveArtifact(toGive);
                them.AddArtifact(toGive);
            }
            foreach (string art in theirOffer.ArtifactsOffered)
            {
                Artifact toGive = ResourceManager.ArtifactsDict[art];
                foreach (Artifact artifact in them.data.OwnedArtifacts)
                {
                    if (artifact.Name != art)
                    {
                        continue;
                    }
                    toGive = artifact;
                }
                them.RemoveArtifact(toGive);
                us.AddArtifact(toGive);
            }
            foreach (string planetName in ourOffer.ColoniesOffered)
            {
                Array<Planet> toRemove = new Array<Planet>();
                Array<Ship> troopShips = new Array<Ship>();
                foreach (Planet p in us.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }

                    // remove our troops from this planet
                    foreach (PlanetGridSquare pgs in p.TilesList)
                    {

                        if (pgs.TroopsAreOnTile && pgs.LockOnOurTroop(us, out Troop troop))
                        {
                            troop.SetPlanet(p); // FB - this is for making sure there is a host planet for the troops? strange
                            troopShips.Add(troop.Launch(ignoreMovement: true));
                        }
                    }
                    toRemove.Add(p);
                    p.Owner = them;
                    them.AddPlanet(p);
                    if (them != EmpireManager.Player)
                    {
                        p.colonyType = them.AssessColonyNeeds(p);
                    }
                    p.ParentSystem.OwnerList.Clear();
                    foreach (Planet pl in p.ParentSystem.PlanetList)
                    {
                        if (pl.Owner == null || p.ParentSystem.OwnerList.Contains(pl.Owner))
                        {
                            continue;
                        }
                        p.ParentSystem.OwnerList.Add(pl.Owner);
                    }
                    var te = new TrustEntry
                    {
                        TrustCost = p.ColonyWorthTo(us),
                        TurnTimer = 40,
                        Type = TrustEntryType.Technology
                    };
                    us.GetRelations(them).TrustEntries.Add(te);
                }
                foreach (Planet p in toRemove)
                {
                    us.RemovePlanet(p);
                }
                foreach (Ship ship in troopShips)
                {
                    ship.AI.OrderRebaseToNearest();
                }
            }
            foreach (string planetName in theirOffer.ColoniesOffered)
            {
                Array<Planet> toRemove = new Array<Planet>();
                Array<Ship> troopShips = new Array<Ship>();
                foreach (Planet p in them.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }
                    toRemove.Add(p);
                    p.Owner = us;
                    us.AddPlanet(p);
                    p.ParentSystem.OwnerList.Clear();
                    foreach (Planet pl in p.ParentSystem.PlanetList)
                    {
                        if (pl.Owner == null || p.ParentSystem.OwnerList.Contains(pl.Owner))
                        {
                            continue;
                        }
                        p.ParentSystem.OwnerList.Add(pl.Owner);
                    }

                    // remove troops which are not ours from the planet
                    foreach (PlanetGridSquare pgs in p.TilesList)
                    {
                        if (pgs.TroopsAreOnTile && pgs.LockOnEnemyTroop(us, out Troop troop))
                        {
                            troop.SetPlanet(p); // FB - this is for making sure there is a host planet for the troops? strange
                            troopShips.Add(troop.Launch(ignoreMovement: true));
                        }
                    }
                    if (Empire.Universe.PlayerEmpire != them)
                    {
                        var te = new TrustEntry
                        {
                            TrustCost = p.ColonyWorthTo(us),
                            TurnTimer = 40,
                            Type = TrustEntryType.Technology
                        };
                        them.GetRelations(us).TrustEntries.Add(te);
                    }
                    if (us == EmpireManager.Player)
                    {
                        continue;
                    }
                    p.colonyType = us.AssessColonyNeeds(p);
                }
                foreach (Planet p in toRemove)
                {
                    them.RemovePlanet(p);
                }
                foreach (Ship ship in troopShips)
                {
                    ship.AI.OrderRebaseToNearest();
                }
            }
        }

        public void AcceptThreat(Offer theirOffer, Offer ourOffer, Empire us, Empire them)
        {
            if (theirOffer.PeaceTreaty)
            {
                Relationship usToThem = OwnerEmpire.GetRelations(them);
                usToThem.AtWar = false;
                usToThem.PreparingForWar = false;
                usToThem.ActiveWar.EndStarDate = Empire.Universe.StarDate;
                usToThem.WarHistory.Add(usToThem.ActiveWar);
                usToThem.ActiveWar = null;
                usToThem.ChangeToNeutral();
                float borderAngerToReduce = usToThem.Anger_FromShipsInOurBorders - us.data.DiplomaticPersonality.Territorialism / 3f;
                if (borderAngerToReduce > 0)
                    usToThem.AddAngerShipsInOurBorders(-borderAngerToReduce);

                float territoryAngerToReduce = usToThem.Anger_TerritorialConflict - us.data.DiplomaticPersonality.Territorialism / 3f;
                if (territoryAngerToReduce > 0)
                    usToThem.AddAngerTerritorialConflict(-territoryAngerToReduce);

                usToThem.ResetAngerMilitaryConflict();
                usToThem.WarnedAboutShips = false;
                usToThem.WarnedAboutColonizing = false;
                usToThem.HaveRejectedDemandTech = false;
                usToThem.HaveRejected_OpenBorders = false;
                usToThem.HaveRejected_TRADE = false;
                usToThem.HasDefenseFleet = false;
                if (usToThem.DefenseFleet != -1)
                {
                    OwnerEmpire.GetFleetsDict()[usToThem.DefenseFleet].FleetTask.EndTask();
                }

                RemoveMilitaryTasksTargeting(them);
                Relationship themToUs = them.GetRelations(OwnerEmpire);
                themToUs.AtWar = false;
                themToUs.PreparingForWar = false;
                themToUs.ActiveWar.EndStarDate = Empire.Universe.StarDate;
                themToUs.WarHistory.Add(themToUs.ActiveWar);
                themToUs.ActiveWar = null;
                if (EmpireManager.Player != them)
                {
                    float theirBorderAngerToReduce = themToUs.Anger_FromShipsInOurBorders - them.data.DiplomaticPersonality.Territorialism / 3f;
                    if (theirBorderAngerToReduce > 0)
                        themToUs.AddAngerShipsInOurBorders(-theirBorderAngerToReduce);

                    float theirTerritoryAngerToReduce = themToUs.Anger_TerritorialConflict - them.data.DiplomaticPersonality.Territorialism / 3f;
                    if (theirTerritoryAngerToReduce > 0)
                        themToUs.AddAngerTerritorialConflict(-theirTerritoryAngerToReduce);
                    
                    themToUs.ResetAngerMilitaryConflict();
                    themToUs.WarnedAboutShips = false;
                    themToUs.WarnedAboutColonizing = false;
                    themToUs.HaveRejectedDemandTech = false;
                    themToUs.HaveRejected_OpenBorders = false;
                    themToUs.HaveRejected_TRADE = false;
                    if (themToUs.DefenseFleet != -1)
                    {
                        them.GetFleetsDict()[themToUs.DefenseFleet].FleetTask.EndTask();
                    }
                    them.GetEmpireAI().RemoveMilitaryTasksTargeting(OwnerEmpire);
                }
            }
            if (theirOffer.NAPact)
            {
                us.SignTreatyWith(them, TreatyType.NonAggression);
                FearEntry te = new FearEntry();
                if (Empire.Universe.PlayerEmpire != us)
                {
                    switch (us.Personality)
                    {
                        case PersonalityType.Pacifist:
                        case PersonalityType.Cunning:    te.FearCost = 0f;  break;
                        case PersonalityType.Xenophobic: te.FearCost = 15f; break;
                        case PersonalityType.Aggressive: te.FearCost = 35f; break;
                        case PersonalityType.Honorable:  te.FearCost = 5f;  break;
                        case PersonalityType.Ruthless:   te.FearCost = 50f; break;
                    }
                }
                us.GetRelations(them).FearEntries.Add(te);
            }
            if (ourOffer.NAPact)
            {
                them.SignTreatyWith(us, TreatyType.NonAggression);
                if (!them.isPlayer)
                {
                    FearEntry te = new FearEntry();
                    switch (them.Personality)
                    {
                        case PersonalityType.Pacifist:
                        case PersonalityType.Cunning:    te.FearCost = 0f;  break;
                        case PersonalityType.Xenophobic: te.FearCost = 15f; break;
                        case PersonalityType.Aggressive: te.FearCost = 35f; break;
                        case PersonalityType.Honorable:  te.FearCost = 5f;  break;
                        case PersonalityType.Ruthless:   te.FearCost = 50f; break;
                    }

                    them.GetRelations(us).FearEntries.Add(te);
                }
            }
            if (theirOffer.TradeTreaty)
            {
                us.SignTreatyWith(them, TreatyType.Trade);
                FearEntry te = new FearEntry
                {
                    FearCost = 5f
                };
                us.GetRelations(them).FearEntries.Add(te);
            }
            if (ourOffer.TradeTreaty)
            {
                them.SignTreatyWith(us, TreatyType.Trade);
                FearEntry te = new FearEntry
                {
                    FearCost = 0.1f
                };
                them.GetRelations(us).FearEntries.Add(te);
            }
            if (theirOffer.OpenBorders)
            {
                us.SignTreatyWith(them, TreatyType.OpenBorders);
                FearEntry te = new FearEntry
                {
                    FearCost = 5f
                };
                us.GetRelations(them).FearEntries.Add(te);
            }
            if (ourOffer.OpenBorders)
            {
                them.SignTreatyWith(us, TreatyType.OpenBorders);
                FearEntry te = new FearEntry
                {
                    FearCost = 5f
                };
                them.GetRelations(us).FearEntries.Add(te);
            }
            foreach (string tech in ourOffer.TechnologiesOffered)
            {
                them.UnlockTech(tech, TechUnlockType.Diplomacy, us);
                if (Empire.Universe.PlayerEmpire == us)
                {
                    continue;
                }
                FearEntry te = new FearEntry
                {
                    FearCost = ResourceManager.Tech(tech).DiplomaticValueTo(us),
                    TurnTimer = 40
                };
                us.GetRelations(them).FearEntries.Add(te);
            }
            foreach (string tech in theirOffer.TechnologiesOffered)
            {
                us.UnlockTech(tech, TechUnlockType.Diplomacy,them);
                if (Empire.Universe.PlayerEmpire == them)
                {
                    continue;
                }
                FearEntry te = new FearEntry
                {
                    FearCost = ResourceManager.Tech(tech).DiplomaticValueTo(them)
                };
                them.GetRelations(us).FearEntries.Add(te);
            }
            foreach (string art in ourOffer.ArtifactsOffered)
            {
                Artifact toGive = ResourceManager.ArtifactsDict[art];
                foreach (Artifact artifact in us.data.OwnedArtifacts)
                {
                    if (artifact.Name != art)
                    {
                        continue;
                    }
                    toGive = artifact;
                }
                us.RemoveArtifact(toGive);
                them.AddArtifact(toGive);
            }
            foreach (string art in theirOffer.ArtifactsOffered)
            {
                Artifact toGive = ResourceManager.ArtifactsDict[art];
                foreach (Artifact artifact in them.data.OwnedArtifacts)
                {
                    if (artifact.Name != art)
                    {
                        continue;
                    }
                    toGive = artifact;
                }
                them.RemoveArtifact(toGive);
                us.AddArtifact(toGive);
            }
            foreach (string planetName in ourOffer.ColoniesOffered)
            {
                Array<Planet> toRemove = new Array<Planet>();
                Array<Ship> troopShips = new Array<Ship>();
                foreach (Planet p in us.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }

                    // remove our troops from the planet
                    foreach (PlanetGridSquare pgs in p.TilesList)
                    {
                        if (pgs.TroopsAreOnTile && pgs.LockOnOurTroop(us, out Troop troop))
                        {
                            troop.SetPlanet(p); // FB - this is for making sure there is a host planet for the troops? strange
                            troopShips.Add(troop.Launch(ignoreMovement: true));
                        }
                    }
                    toRemove.Add(p);
                    p.Owner = them;
                    them.AddPlanet(p);
                    p.ParentSystem.OwnerList.Clear();
                    foreach (Planet pl in p.ParentSystem.PlanetList)
                    {
                        if (pl.Owner == null || p.ParentSystem.OwnerList.Contains(pl.Owner))
                        {
                            continue;
                        }
                        p.ParentSystem.OwnerList.Add(pl.Owner);
                    }
                    var te = new FearEntry
                    {
                        FearCost = p.ColonyWorthTo(us),
                        TurnTimer = 40
                    };
                    us.GetRelations(them).FearEntries.Add(te);
                }
                foreach (Planet p in toRemove)
                {
                    us.RemovePlanet(p);
                }
                foreach (Ship ship in troopShips)
                {
                    ship.AI.OrderRebaseToNearest();
                }
            }
            foreach (string planetName in theirOffer.ColoniesOffered)
            {
                Array<Planet> toRemove = new Array<Planet>();
                Array<Ship> troopShips = new Array<Ship>();
                foreach (Planet p in them.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }
                    toRemove.Add(p);
                    p.Owner = us;
                    us.AddPlanet(p);
                    p.ParentSystem.OwnerList.Clear();
                    foreach (Planet pl in p.ParentSystem.PlanetList)
                    {
                        if (pl.Owner == null || p.ParentSystem.OwnerList.Contains(pl.Owner))
                        {
                            continue;
                        }
                        p.ParentSystem.OwnerList.Add(pl.Owner);
                    }

                    // remove troops which are not ours from the planet
                    foreach (PlanetGridSquare pgs in p.TilesList)
                    {
                        if (pgs.TroopsAreOnTile && pgs.LockOnEnemyTroop(us, out Troop troop))
                        {
                            troop.SetPlanet(p); // FB - this is for making sure there is a host planet for the troops? strange
                            troopShips.Add(troop.Launch(ignoreMovement: true));
                        }
                    }
                    if (Empire.Universe.PlayerEmpire == them)
                    {
                        continue;
                    }
                    var te = new FearEntry
                    {
                        FearCost = p.ColonyWorthTo(them),
                        TurnTimer = 40
                    };
                    them.GetRelations(us).FearEntries.Add(te);
                }
                foreach (Planet p in toRemove)
                {
                    them.RemovePlanet(p);
                }
                foreach (Ship ship in troopShips)
                {
                    ship.AI.OrderRebaseToNearest();
                }
            }
            us.GetRelations(them).UpdateRelationship(us, them);
        }

        bool AllianceAccepted(Offer theirOffer, Offer ourOffer, Relationship usToThem, Relationship themToUs, Empire them, out string text)
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

        bool PeaceAccepted(Offer theirOffer, Offer ourOffer, Empire them, Offer.Attitude attitude, out string text)
        {
            text = "";
            if (!theirOffer.PeaceTreaty)
                return false;

            bool isPeace       = false;
            PeaceAnswer answer = AnalyzePeaceOffer(theirOffer, ourOffer, them, attitude);
            if (answer.Peace)
            {
                AcceptOffer(theirOffer, ourOffer, OwnerEmpire, them);
                OwnerEmpire.SignTreatyWith(them, TreatyType.Peace);
                isPeace = true;
            }

            text = answer.Answer;
            return isPeace;

        }

        public string AnalyzeOffer(Offer theirOffer, Offer ourOffer, Empire them, Offer.Attitude attitude)
        {
            Empire us             = OwnerEmpire;
            Relationship usToThem = us.GetRelations(them);
            Relationship themToUs = them.GetRelations(us);

            if (AllianceAccepted(theirOffer, ourOffer, usToThem, themToUs, them, out string allianceText))
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
                foreach (KeyValuePair<Empire, Relationship> relationship in us.AllRelations)
                {
                    if (relationship.Key.isFaction || !relationship.Value.AtWar)
                    {
                        continue;
                    }
                    numWars++;
                }
                if (numWars > 0 && !us.GetRelations(them).AtWar)
                {
                    totalTrustRequiredFromUs -= dt.NAPact;
                }
                else if (us.GetRelations(them).Threat >= 20f)
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

            valueToThem += ourOffer.ArtifactsOffered.Count() * 15f;
            valueToUs   += theirOffer.ArtifactsOffered.Count() * 15f;

            if (us.GetPlanets().Count - ourOffer.ColoniesOffered.Count + theirOffer.ColoniesOffered.Count < 1)
            {
                // todo not sure this is needed, better check the colony value
                us.GetRelations(them).DamageRelationship(us, them, "Insulted", 25f, null);
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
                AcceptOffer(theirOffer, ourOffer, us, them);
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
                        AcceptOffer(theirOffer, ourOffer, us, them);
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
                            AcceptOffer(theirOffer, ourOffer, us, them);
                            return "OfferResponse_Accept_Fair_Pleading";
                        case OfferQuality.Good:
                            usToThem.ImproveRelations(valueToUs - valueToThem, valueToUs - valueToThem);
                            AcceptOffer(theirOffer, ourOffer, us, them);
                            return "OfferResponse_Accept_Good";
                        case OfferQuality.Great:
                            usToThem.ImproveRelations(valueToUs - valueToThem, valueToUs - valueToThem);
                            AcceptOffer(theirOffer, ourOffer, us, them);
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
                                AcceptOffer(theirOffer, ourOffer, us, them);
                                return "OfferResponse_Accept_Fair";
                            case OfferQuality.Good:
                                usToThem.ImproveRelations(valueToUs - valueToThem, valueToUs - valueToThem);
                                AcceptOffer(theirOffer, ourOffer, us, them);
                                return "OfferResponse_Accept_Good";
                            case OfferQuality.Great:
                                usToThem.ImproveRelations(valueToUs - valueToThem, valueToUs - valueToThem);
                                AcceptOffer(theirOffer, ourOffer, us, them);
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
                            AcceptOffer(theirOffer, ourOffer, us, them);
                            return "OfferResponse_AcceptGreatOffer_LowTrust";
                    }

                    break;
                case Offer.Attitude.Threaten:
                    if (dt.Name == "Ruthless")
                        return "OfferResponse_InsufficientFear";

                    usToThem.DamageRelationship(us, them, "Insulted", valueToThem - valueToUs, null);

                    if (offerQuality == OfferQuality.Great)
                    {
                        AcceptThreat(theirOffer, ourOffer, us, them);
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
                            AcceptThreat(theirOffer, ourOffer, us, them);
                            return "OfferResponse_Accept_Bad_Threatening";
                        case OfferQuality.Fair:
                            AcceptThreat(theirOffer, ourOffer, us, them);
                            return "OfferResponse_Accept_Fair_Threatening";
                    }

                    break;
            }

            return "";
        }

        PeaceAnswer AnalyzePeaceOffer(Offer theirOffer, Offer ourOffer, Empire them, Offer.Attitude attitude)
        {
            WarState state;
            Empire us          = OwnerEmpire;
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
                WarType warType = us.GetRelations(them).ActiveWar.WarType;
                WarState warState = WarState.NotApplicable;
                switch (warType)
                {
                    case WarType.BorderConflict: warState = us.GetRelations(them).ActiveWar.GetBorderConflictState(planetsToUs); break;
                    case WarType.ImperialistWar: warState = us.GetRelations(them).ActiveWar.GetWarScoreState();                  break;
                    case WarType.DefensiveWar:   warState = us.GetRelations(them).ActiveWar.GetWarScoreState();                  break;
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
            switch (us.GetRelations(them).ActiveWar.WarType)
            {
                case WarType.BorderConflict:
                    state = us.GetRelations(them).ActiveWar.GetBorderConflictState(planetsToUs);
                    switch (state)
                    {
                        case WarState.EvenlyMatched:
                        case WarState.WinningSlightly:
                        case WarState.LosingSlightly:
                            switch (offerQuality)
                            {
                                case OfferQuality.Fair when us.GetRelations(them).ActiveWar.StartingNumContestedSystems > 0:
                                case OfferQuality.Good when us.GetRelations(them).ActiveWar.StartingNumContestedSystems > 0:
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
                    state = us.GetRelations(them).ActiveWar.GetWarScoreState();
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