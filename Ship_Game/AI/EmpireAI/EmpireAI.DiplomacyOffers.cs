using System.Collections.Generic;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.AI {
    public sealed partial class EmpireAI
    {
        public void AcceptOffer(Offer ToUs, Offer FromUs, Empire us, Empire Them)
        {
            if (ToUs.PeaceTreaty)
            {
                Relationship relation = OwnerEmpire.GetRelations(Them);
                relation.AtWar = false;
                relation.PreparingForWar = false;
                relation.ActiveWar.EndStarDate = Empire.Universe.StarDate;
                relation.WarHistory.Add(relation.ActiveWar);
                if (OwnerEmpire.data.DiplomaticPersonality != null)
                {
                    relation.Posture = Posture.Neutral;
                    if (relation.Anger_FromShipsInOurBorders >
                        (float) (OwnerEmpire.data.DiplomaticPersonality.Territorialism / 3))
                    {
                        relation.Anger_FromShipsInOurBorders =
                            (float) (OwnerEmpire.data.DiplomaticPersonality.Territorialism / 3);
                    }
                    if (relation.Anger_TerritorialConflict >
                        (float) (OwnerEmpire.data.DiplomaticPersonality.Territorialism / 3))
                    {
                        relation.Anger_TerritorialConflict =
                            (float) (OwnerEmpire.data.DiplomaticPersonality.Territorialism / 3);
                    }
                }
                relation.Anger_MilitaryConflict = 0f;
                relation.WarnedAboutShips = false;
                relation.WarnedAboutColonizing = false;
                relation.HaveRejectedDemandTech = false;
                relation.HaveRejected_OpenBorders = false;
                relation.HaveRejected_TRADE = false;
                relation.HasDefenseFleet = false;
                if (relation.DefenseFleet != -1)
                {
                    OwnerEmpire.GetFleetsDict()[relation.DefenseFleet].FleetTask.EndTask();
                }
                using (TaskList.AcquireWriteLock())
                {
                    for (int i = TaskList.Count - 1; i >= 0; i--)
                    {
                        Tasks.MilitaryTask task = TaskList[i];
                        if (task.TargetPlanet == null || task.TargetPlanet.Owner == null ||
                            task.TargetPlanet.Owner != Them)
                        {
                            continue;
                        }
                        task.EndTask();
                    }
                }
                relation.ActiveWar = null;
                Relationship relationThem = Them.GetRelations(OwnerEmpire);
                relationThem.AtWar = false;
                relationThem.PreparingForWar = false;
                relationThem.ActiveWar.EndStarDate = Empire.Universe.StarDate;
                relationThem.WarHistory.Add(relationThem.ActiveWar);
                relationThem.Posture = Posture.Neutral;
                if (EmpireManager.Player != Them)
                {
                    if (relationThem.Anger_FromShipsInOurBorders >
                        (float) (Them.data.DiplomaticPersonality.Territorialism / 3))
                    {
                        relationThem.Anger_FromShipsInOurBorders =
                            (float) (Them.data.DiplomaticPersonality.Territorialism / 3);
                    }
                    if (relationThem.Anger_TerritorialConflict >
                        (float) (Them.data.DiplomaticPersonality.Territorialism / 3))
                    {
                        relationThem.Anger_TerritorialConflict =
                            (float) (Them.data.DiplomaticPersonality.Territorialism / 3);
                    }
                    relationThem.Anger_MilitaryConflict = 0f;
                    relationThem.WarnedAboutShips = false;
                    relationThem.WarnedAboutColonizing = false;
                    relationThem.HaveRejectedDemandTech = false;
                    relationThem.HaveRejected_OpenBorders = false;
                    relationThem.HaveRejected_TRADE = false;
                    if (relationThem.DefenseFleet != -1)
                    {
                        Them.GetFleetsDict()[relationThem.DefenseFleet].FleetTask.EndTask();
                    }
                    //lock (GlobalStats.TaskLocker)
                    {
                        //foreach (MilitaryTask task in Them.GetGSAI().TaskList)
                        Them.GetGSAI()
                            .TaskList.ForEach(task =>
                            {
                                if (task.TargetPlanet == null || task.TargetPlanet.Owner == null ||
                                    task.TargetPlanet.Owner != OwnerEmpire)
                                {
                                    return;
                                }
                                task.EndTask();
                            }, false, false, false);
                    }
                }
                relationThem.ActiveWar = null;
                if (Them == Empire.Universe.PlayerEmpire || OwnerEmpire == Empire.Universe.PlayerEmpire)
                {
                    Empire.Universe.NotificationManager.AddPeaceTreatyEnteredNotification(OwnerEmpire, Them);
                }
                else if (Empire.Universe.PlayerEmpire.GetRelations(Them).Known &&
                         Empire.Universe.PlayerEmpire.GetRelations(OwnerEmpire).Known)
                {
                    Empire.Universe.NotificationManager.AddPeaceTreatyEnteredNotification(OwnerEmpire, Them);
                }
            }
            if (ToUs.NAPact)
            {
                us.GetRelations(Them).Treaty_NAPact = true;
                TrustEntry te = new TrustEntry();
                if (Empire.Universe.PlayerEmpire != us)
                {
                    string name = us.data.DiplomaticPersonality.Name;
                    string str = name;

                    if (name != null)
                    {
                        if (str == "Pacifist")
                        {
                            te.TrustCost = 0f;
                        }
                        else if (str == "Cunning")
                        {
                            te.TrustCost = 0f;
                        }
                        else if (str == "Xenophobic")
                        {
                            te.TrustCost = 15f;
                        }
                        else if (str == "Aggressive")
                        {
                            te.TrustCost = 35f;
                        }
                        else if (str == "Honorable")
                        {
                            te.TrustCost = 5f;
                        }
                        else if (str == "Ruthless")
                        {
                            te.TrustCost = 50f;
                        }
                    }
                }
                te.Type = TrustEntryType.Treaty;
                us.GetRelations(Them).TrustEntries.Add(te);
            }
            if (FromUs.NAPact)
            {
                Them.GetRelations(us).Treaty_NAPact = true;
                if (Empire.Universe.PlayerEmpire != Them)
                {
                    TrustEntry te = new TrustEntry();
                    string name1 = Them.data.DiplomaticPersonality.Name;
                    string str1 = name1;
                    if (name1 != null)
                    {
                        if (str1 == "Pacifist")
                        {
                            te.TrustCost = 0f;
                        }
                        else if (str1 == "Cunning")
                        {
                            te.TrustCost = 0f;
                        }
                        else if (str1 == "Xenophobic")
                        {
                            te.TrustCost = 15f;
                        }
                        else if (str1 == "Aggressive")
                        {
                            te.TrustCost = 35f;
                        }
                        else if (str1 == "Honorable")
                        {
                            te.TrustCost = 5f;
                        }
                        else if (str1 == "Ruthless")
                        {
                            te.TrustCost = 50f;
                        }
                    }
                    te.Type = TrustEntryType.Treaty;
                    Them.GetRelations(us).TrustEntries.Add(te);
                }
            }
            if (ToUs.TradeTreaty)
            {
                us.GetRelations(Them).Treaty_Trade = true;
                us.GetRelations(Them).Treaty_Trade_TurnsExisted = 0;
                TrustEntry te = new TrustEntry()
                {
                    TrustCost = 0.1f,
                    Type = TrustEntryType.Treaty
                };
                us.GetRelations(Them).TrustEntries.Add(te);
            }
            if (FromUs.TradeTreaty)
            {
                Them.GetRelations(us).Treaty_Trade = true;
                Them.GetRelations(us).Treaty_Trade_TurnsExisted = 0;
                TrustEntry te = new TrustEntry()
                {
                    TrustCost = 0.1f,
                    Type = TrustEntryType.Treaty
                };
                Them.GetRelations(us).TrustEntries.Add(te);
            }
            if (ToUs.OpenBorders)
            {
                us.GetRelations(Them).Treaty_OpenBorders = true;
                TrustEntry te = new TrustEntry()
                {
                    TrustCost = 5f,
                    Type = TrustEntryType.Treaty
                };
                us.GetRelations(Them).TrustEntries.Add(te);
            }
            if (FromUs.OpenBorders)
            {
                Them.GetRelations(us).Treaty_OpenBorders = true;
                TrustEntry te = new TrustEntry()
                {
                    TrustCost = 5f,
                    Type = TrustEntryType.Treaty
                };
                Them.GetRelations(us).TrustEntries.Add(te);
            }
            foreach (string tech in FromUs.TechnologiesOffered)
            {
                //Added by McShooterz:
                //Them.UnlockTech(tech);
                Them.AcquireTech(tech, us);
                if (Empire.Universe.PlayerEmpire == us)
                {
                    continue;
                }
                TrustEntry te = new TrustEntry()
                {
                    TrustCost = (us.data.EconomicPersonality.Name == "Technologists"
                        ? ResourceManager.TechTree[tech].Cost / 100f * 0.25f +
                          ResourceManager.TechTree[tech].Cost / 100f
                        : ResourceManager.TechTree[tech].Cost / 100f),
                    TurnTimer = 40,
                    Type = TrustEntryType.Technology
                };
                us.GetRelations(Them).TrustEntries.Add(te);
            }
            foreach (string tech in ToUs.TechnologiesOffered)
            {
                //Added by McShooterz:
                //us.UnlockTech(tech);
                us.AcquireTech(tech, Them);
                if (Empire.Universe.PlayerEmpire == Them)
                {
                    continue;
                }
                TrustEntry te = new TrustEntry()
                {
                    TrustCost = (Them.data.EconomicPersonality.Name == "Technologists"
                        ? ResourceManager.TechTree[tech].Cost / 100f * 0.25f +
                          ResourceManager.TechTree[tech].Cost / 100f
                        : ResourceManager.TechTree[tech].Cost / 100f),
                    Type = TrustEntryType.Treaty
                };
                Them.GetRelations(us).TrustEntries.Add(te);
            }
            foreach (string Art in FromUs.ArtifactsOffered)
            {
                Artifact toGive = ResourceManager.ArtifactsDict[Art];
                foreach (Artifact arti in us.data.OwnedArtifacts)
                {
                    if (arti.Name != Art)
                    {
                        continue;
                    }
                    toGive = arti;
                }
                us.RemoveArtifact(toGive);
                Them.AddArtifact(toGive);
            }
            foreach (string Art in ToUs.ArtifactsOffered)
            {
                Artifact toGive = ResourceManager.ArtifactsDict[Art];
                foreach (Artifact arti in Them.data.OwnedArtifacts)
                {
                    if (arti.Name != Art)
                    {
                        continue;
                    }
                    toGive = arti;
                }
                Them.RemoveArtifact(toGive);
                us.AddArtifact(toGive);
            }
            foreach (string planetName in FromUs.ColoniesOffered)
            {
                Array<Planet> toRemove = new Array<Planet>();
                Array<Ship> TroopShips = new Array<Ship>();
                foreach (Planet p in us.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }
                    foreach (PlanetGridSquare pgs in p.TilesList)
                    {
                        if (pgs.TroopsHere.Count <= 0 || pgs.TroopsHere[0].GetOwner() != OwnerEmpire)
                        {
                            continue;
                        }
                        pgs.TroopsHere[0].SetPlanet(p);
                        TroopShips.Add(pgs.TroopsHere[0].Launch());
                    }
                    toRemove.Add(p);
                    p.Owner = Them;
                    Them.AddPlanet(p);
                    if (Them != EmpireManager.Player)
                    {
                        p.colonyType = Them.AssessColonyNeeds(p);
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
                    float value = p.Population / 1000f + p.FoodHere / 50f + p.ProductionHere / 50f + p.Fertility +
                                  p.MineralRichness + p.MaxPopulation / 10000f;
                    foreach (Building b in p.BuildingList)
                    {
                        value = value + b.Cost / 50f;
                    }
                    TrustEntry te = new TrustEntry()
                    {
                        TrustCost = (us.data.EconomicPersonality.Name == "Expansionists"
                            ? value + value
                            : value + 0.5f * value),
                        TurnTimer = 40,
                        Type = TrustEntryType.Technology
                    };
                    us.GetRelations(Them).TrustEntries.Add(te);
                }
                foreach (Planet p in toRemove)
                {
                    us.RemovePlanet(p);
                }
                foreach (Ship ship in TroopShips)
                {
                    ship.AI.OrderRebaseToNearest();
                }
            }
            foreach (string planetName in ToUs.ColoniesOffered)
            {
                Array<Planet> toRemove = new Array<Planet>();
                Array<Ship> TroopShips = new Array<Ship>();
                foreach (Planet p in Them.GetPlanets())
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
                    float value = p.Population / 1000f + p.FoodHere / 50f + p.ProductionHere / 50f + p.Fertility +
                                  p.MineralRichness + p.MaxPopulation / 10000f;
                    foreach (Building b in p.BuildingList)
                    {
                        value = value + b.Cost / 50f;
                    }
                    foreach (PlanetGridSquare pgs in p.TilesList)
                    {
                        if (pgs.TroopsHere.Count <= 0 || pgs.TroopsHere[0].GetOwner() != Them)
                        {
                            continue;
                        }
                        pgs.TroopsHere[0].SetPlanet(p);
                        TroopShips.Add(pgs.TroopsHere[0].Launch());
                    }
                    if (Empire.Universe.PlayerEmpire != Them)
                    {
                        TrustEntry te = new TrustEntry()
                        {
                            TrustCost = (Them.data.EconomicPersonality.Name == "Expansionists"
                                ? value + value
                                : value + 0.5f * value),
                            TurnTimer = 40,
                            Type = TrustEntryType.Technology
                        };
                        Them.GetRelations(us).TrustEntries.Add(te);
                    }
                    if (us == EmpireManager.Player)
                    {
                        continue;
                    }
                    p.colonyType = us.AssessColonyNeeds(p);
                }
                foreach (Planet p in toRemove)
                {
                    Them.RemovePlanet(p);
                }
                foreach (Ship ship in TroopShips)
                {
                    ship.AI.OrderRebaseToNearest();
                }
            }
        }

        public void AcceptThreat(Offer ToUs, Offer FromUs, Empire us, Empire Them)
        {
            if (ToUs.PeaceTreaty)
            {
                OwnerEmpire.GetRelations(Them).AtWar = false;
                OwnerEmpire.GetRelations(Them).PreparingForWar = false;
                OwnerEmpire.GetRelations(Them).ActiveWar.EndStarDate = Empire.Universe.StarDate;
                OwnerEmpire.GetRelations(Them).WarHistory.Add(OwnerEmpire.GetRelations(Them).ActiveWar);
                OwnerEmpire.GetRelations(Them).Posture = Posture.Neutral;
                if (OwnerEmpire.GetRelations(Them).Anger_FromShipsInOurBorders >
                    (float) (this.OwnerEmpire.data.DiplomaticPersonality.Territorialism / 3))
                {
                    OwnerEmpire.GetRelations(Them).Anger_FromShipsInOurBorders =
                        (float) (this.OwnerEmpire.data.DiplomaticPersonality.Territorialism / 3);
                }
                if (OwnerEmpire.GetRelations(Them).Anger_TerritorialConflict >
                    (float) (this.OwnerEmpire.data.DiplomaticPersonality.Territorialism / 3))
                {
                    OwnerEmpire.GetRelations(Them).Anger_TerritorialConflict =
                        (float) (this.OwnerEmpire.data.DiplomaticPersonality.Territorialism / 3);
                }
                OwnerEmpire.GetRelations(Them).Anger_MilitaryConflict = 0f;
                OwnerEmpire.GetRelations(Them).WarnedAboutShips = false;
                OwnerEmpire.GetRelations(Them).WarnedAboutColonizing = false;
                OwnerEmpire.GetRelations(Them).HaveRejectedDemandTech = false;
                OwnerEmpire.GetRelations(Them).HaveRejected_OpenBorders = false;
                OwnerEmpire.GetRelations(Them).HaveRejected_TRADE = false;
                OwnerEmpire.GetRelations(Them).HasDefenseFleet = false;
                if (OwnerEmpire.GetRelations(Them).DefenseFleet != -1)
                {
                    this.OwnerEmpire.GetFleetsDict()[OwnerEmpire.GetRelations(Them).DefenseFleet].FleetTask.EndTask();
                }
                //lock (GlobalStats.TaskLocker)
                {
                    this.TaskList.ForEach(task => //foreach (MilitaryTask task in this.TaskList)
                    {
                        if (task.TargetPlanet == null || task.TargetPlanet.Owner == null ||
                            task.TargetPlanet.Owner != Them)
                        {
                            return;
                        }
                        task.EndTask();
                    });
                }
                OwnerEmpire.GetRelations(Them).ActiveWar = null;
                Them.GetRelations(this.OwnerEmpire).AtWar = false;
                Them.GetRelations(this.OwnerEmpire).PreparingForWar = false;
                Them.GetRelations(this.OwnerEmpire).ActiveWar.EndStarDate = Empire.Universe.StarDate;
                Them.GetRelations(this.OwnerEmpire).WarHistory.Add(Them.GetRelations(this.OwnerEmpire).ActiveWar);
                Them.GetRelations(this.OwnerEmpire).Posture = Posture.Neutral;
                if (EmpireManager.Player != Them)
                {
                    if (Them.GetRelations(this.OwnerEmpire).Anger_FromShipsInOurBorders >
                        (float) (Them.data.DiplomaticPersonality.Territorialism / 3))
                    {
                        Them.GetRelations(this.OwnerEmpire).Anger_FromShipsInOurBorders =
                            (float) (Them.data.DiplomaticPersonality.Territorialism / 3);
                    }
                    if (Them.GetRelations(this.OwnerEmpire).Anger_TerritorialConflict >
                        (float) (Them.data.DiplomaticPersonality.Territorialism / 3))
                    {
                        Them.GetRelations(this.OwnerEmpire).Anger_TerritorialConflict =
                            (float) (Them.data.DiplomaticPersonality.Territorialism / 3);
                    }
                    Them.GetRelations(this.OwnerEmpire).Anger_MilitaryConflict = 0f;
                    Them.GetRelations(this.OwnerEmpire).WarnedAboutShips = false;
                    Them.GetRelations(this.OwnerEmpire).WarnedAboutColonizing = false;
                    Them.GetRelations(this.OwnerEmpire).HaveRejectedDemandTech = false;
                    Them.GetRelations(this.OwnerEmpire).HaveRejected_OpenBorders = false;
                    Them.GetRelations(this.OwnerEmpire).HaveRejected_TRADE = false;
                    if (Them.GetRelations(this.OwnerEmpire).DefenseFleet != -1)
                    {
                        Them.GetFleetsDict()[Them.GetRelations(this.OwnerEmpire).DefenseFleet].FleetTask.EndTask();
                    }
                    //lock (GlobalStats.TaskLocker)
                    {
                        Them.GetGSAI()
                            .TaskList.ForEach(task => //foreach (MilitaryTask task in Them.GetGSAI().TaskList)
                            {
                                if (task.TargetPlanet == null || task.TargetPlanet.Owner == null ||
                                    task.TargetPlanet.Owner != this.OwnerEmpire)
                                {
                                    return;
                                }
                                task.EndTask();
                            }, false, false, false);
                    }
                }
                Them.GetRelations(this.OwnerEmpire).ActiveWar = null;
            }
            if (ToUs.NAPact)
            {
                us.GetRelations(Them).Treaty_NAPact = true;
                FearEntry te = new FearEntry();
                if (Empire.Universe.PlayerEmpire != us)
                {
                    string name = us.data.DiplomaticPersonality.Name;
                    string str = name;
                    if (name != null)
                    {
                        if (str == "Pacifist")
                        {
                            te.FearCost = 0f;
                        }
                        else if (str == "Cunning")
                        {
                            te.FearCost = 0f;
                        }
                        else if (str == "Xenophobic")
                        {
                            te.FearCost = 15f;
                        }
                        else if (str == "Aggressive")
                        {
                            te.FearCost = 35f;
                        }
                        else if (str == "Honorable")
                        {
                            te.FearCost = 5f;
                        }
                        else if (str == "Ruthless")
                        {
                            te.FearCost = 50f;
                        }
                    }
                }
                us.GetRelations(Them).FearEntries.Add(te);
            }
            if (FromUs.NAPact)
            {
                Them.GetRelations(us).Treaty_NAPact = true;
                if (Empire.Universe.PlayerEmpire != Them)
                {
                    FearEntry te = new FearEntry();
                    string name1 = Them.data.DiplomaticPersonality.Name;
                    string str1 = name1;
                    if (name1 != null)
                    {
                        if (str1 == "Pacifist")
                        {
                            te.FearCost = 0f;
                        }
                        else if (str1 == "Cunning")
                        {
                            te.FearCost = 0f;
                        }
                        else if (str1 == "Xenophobic")
                        {
                            te.FearCost = 15f;
                        }
                        else if (str1 == "Aggressive")
                        {
                            te.FearCost = 35f;
                        }
                        else if (str1 == "Honorable")
                        {
                            te.FearCost = 5f;
                        }
                        else if (str1 == "Ruthless")
                        {
                            te.FearCost = 50f;
                        }
                    }
                    Them.GetRelations(us).FearEntries.Add(te);
                }
            }
            if (ToUs.TradeTreaty)
            {
                us.GetRelations(Them).Treaty_Trade = true;
                us.GetRelations(Them).Treaty_Trade_TurnsExisted = 0;
                FearEntry te = new FearEntry()
                {
                    FearCost = 5f
                };
                us.GetRelations(Them).FearEntries.Add(te);
            }
            if (FromUs.TradeTreaty)
            {
                Them.GetRelations(us).Treaty_Trade = true;
                Them.GetRelations(us).Treaty_Trade_TurnsExisted = 0;
                FearEntry te = new FearEntry()
                {
                    FearCost = 0.1f
                };
                Them.GetRelations(us).FearEntries.Add(te);
            }
            if (ToUs.OpenBorders)
            {
                us.GetRelations(Them).Treaty_OpenBorders = true;
                FearEntry te = new FearEntry()
                {
                    FearCost = 5f
                };
                us.GetRelations(Them).FearEntries.Add(te);
            }
            if (FromUs.OpenBorders)
            {
                Them.GetRelations(us).Treaty_OpenBorders = true;
                FearEntry te = new FearEntry()
                {
                    FearCost = 5f
                };
                Them.GetRelations(us).FearEntries.Add(te);
            }
            foreach (string tech in FromUs.TechnologiesOffered)
            {
                Them.UnlockTech(tech);
                if (Empire.Universe.PlayerEmpire == us)
                {
                    continue;
                }
                FearEntry te = new FearEntry()
                {
                    FearCost = (us.data.EconomicPersonality.Name == "Technologists"
                        ? ResourceManager.TechTree[tech].Cost / 100f * 0.25f +
                          ResourceManager.TechTree[tech].Cost / 100f
                        : ResourceManager.TechTree[tech].Cost / 100f),
                    TurnTimer = 40
                };
                us.GetRelations(Them).FearEntries.Add(te);
            }
            foreach (string tech in ToUs.TechnologiesOffered)
            {
                us.UnlockTech(tech);
                if (Empire.Universe.PlayerEmpire == Them)
                {
                    continue;
                }
                FearEntry te = new FearEntry()
                {
                    FearCost = (Them.data.EconomicPersonality.Name == "Technologists"
                        ? ResourceManager.TechTree[tech].Cost / 100f * 0.25f +
                          ResourceManager.TechTree[tech].Cost / 100f
                        : ResourceManager.TechTree[tech].Cost / 100f)
                };
                Them.GetRelations(us).FearEntries.Add(te);
            }
            foreach (string Art in FromUs.ArtifactsOffered)
            {
                Artifact toGive = ResourceManager.ArtifactsDict[Art];
                foreach (Artifact arti in us.data.OwnedArtifacts)
                {
                    if (arti.Name != Art)
                    {
                        continue;
                    }
                    toGive = arti;
                }
                us.RemoveArtifact(toGive);
                Them.AddArtifact(toGive);
            }
            foreach (string Art in ToUs.ArtifactsOffered)
            {
                Artifact toGive = ResourceManager.ArtifactsDict[Art];
                foreach (Artifact arti in Them.data.OwnedArtifacts)
                {
                    if (arti.Name != Art)
                    {
                        continue;
                    }
                    toGive = arti;
                }
                Them.RemoveArtifact(toGive);
                us.AddArtifact(toGive);
            }
            foreach (string planetName in FromUs.ColoniesOffered)
            {
                Array<Planet> toRemove = new Array<Planet>();
                Array<Ship> TroopShips = new Array<Ship>();
                foreach (Planet p in us.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }
                    foreach (PlanetGridSquare pgs in p.TilesList)
                    {
                        if (pgs.TroopsHere.Count <= 0 || pgs.TroopsHere[0].GetOwner() != this.OwnerEmpire)
                        {
                            continue;
                        }
                        TroopShips.Add(pgs.TroopsHere[0].Launch());
                    }
                    toRemove.Add(p);
                    p.Owner = Them;
                    Them.AddPlanet(p);
                    p.ParentSystem.OwnerList.Clear();
                    foreach (Planet pl in p.ParentSystem.PlanetList)
                    {
                        if (pl.Owner == null || p.ParentSystem.OwnerList.Contains(pl.Owner))
                        {
                            continue;
                        }
                        p.ParentSystem.OwnerList.Add(pl.Owner);
                    }
                    float value = p.Population / 1000f + p.FoodHere / 50f + p.ProductionHere / 50f + p.Fertility +
                                  p.MineralRichness + p.MaxPopulation / 10000f;
                    foreach (Building b in p.BuildingList)
                    {
                        value = value + b.Cost / 50f;
                    }
                    FearEntry te = new FearEntry();
                    if (value < 15f)
                    {
                        value = 15f;
                    }
                    te.FearCost = (us.data.EconomicPersonality.Name == "Expansionists"
                        ? value + value
                        : value + 0.5f * value);
                    te.TurnTimer = 40;
                    us.GetRelations(Them).FearEntries.Add(te);
                }
                foreach (Planet p in toRemove)
                {
                    us.RemovePlanet(p);
                }
                foreach (Ship ship in TroopShips)
                {
                    ship.AI.OrderRebaseToNearest();
                }
            }
            foreach (string planetName in ToUs.ColoniesOffered)
            {
                Array<Planet> toRemove = new Array<Planet>();
                Array<Ship> TroopShips = new Array<Ship>();
                foreach (Planet p in Them.GetPlanets())
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
                    float value = p.Population / 1000f + p.FoodHere / 50f + p.ProductionHere / 50f + p.Fertility +
                                  p.MineralRichness + p.MaxPopulation / 10000f;
                    foreach (Building b in p.BuildingList)
                    {
                        value = value + b.Cost / 50f;
                    }
                    foreach (PlanetGridSquare pgs in p.TilesList)
                    {
                        if (pgs.TroopsHere.Count <= 0 || pgs.TroopsHere[0].GetOwner() != Them)
                        {
                            continue;
                        }
                        TroopShips.Add(pgs.TroopsHere[0].Launch());
                    }
                    if (Empire.Universe.PlayerEmpire == Them)
                    {
                        continue;
                    }
                    FearEntry te = new FearEntry()
                    {
                        FearCost = (Them.data.EconomicPersonality.Name == "Expansionists"
                            ? value + value
                            : value + 0.5f * value),
                        TurnTimer = 40
                    };
                    Them.GetRelations(us).FearEntries.Add(te);
                }
                foreach (Planet p in toRemove)
                {
                    Them.RemovePlanet(p);
                }
                foreach (Ship ship in TroopShips)
                {
                    ship.AI.OrderRebaseToNearest();
                }
            }
            us.GetRelations(Them).UpdateRelationship(us, Them);
        }

        public string AnalyzeOffer(Offer ToUs, Offer FromUs, Empire them, Offer.Attitude attitude)
        {
            if (ToUs.Alliance)
            {
                if (!ToUs.IsBlank() || !FromUs.IsBlank())
                {
                    return "OFFER_ALLIANCE_TOO_COMPLICATED";
                }
                if (OwnerEmpire.GetRelations(them).Trust < 90f || OwnerEmpire.GetRelations(them).TotalAnger >= 20f ||
                    OwnerEmpire.GetRelations(them).TurnsKnown <= 100)
                {
                    return "AI_ALLIANCE_REJECT";
                }
                this.SetAlliance(true, them);
                return "AI_ALLIANCE_ACCEPT";
            }
            if (ToUs.PeaceTreaty)
            {
                EmpireAI.PeaceAnswer answer = this.AnalyzePeaceOffer(ToUs, FromUs, them, attitude);
                if (!answer.peace)
                {
                    return answer.answer;
                }
                this.AcceptOffer(ToUs, FromUs, this.OwnerEmpire, them);
                OwnerEmpire.GetRelations(them).Treaty_Peace = true;
                OwnerEmpire.GetRelations(them).PeaceTurnsRemaining = 100;
                them.GetRelations(this.OwnerEmpire).Treaty_Peace = true;
                them.GetRelations(this.OwnerEmpire).PeaceTurnsRemaining = 100;
                return answer.answer;
            }
            Empire us = this.OwnerEmpire;
            float TotalTrustRequiredFromUS = 0f;
            DTrait dt = us.data.DiplomaticPersonality;
            if (FromUs.TradeTreaty)
            {
                TotalTrustRequiredFromUS = TotalTrustRequiredFromUS + (float) dt.Trade;
            }
            if (FromUs.OpenBorders)
            {
                TotalTrustRequiredFromUS = TotalTrustRequiredFromUS + ((float) dt.NAPact + 7.5f);
            }
            if (FromUs.NAPact)
            {
                TotalTrustRequiredFromUS = TotalTrustRequiredFromUS + (float) dt.NAPact;
                int numWars = 0;
                foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in us.AllRelations)
                {
                    if (Relationship.Key.isFaction || !Relationship.Value.AtWar)
                    {
                        continue;
                    }
                    numWars++;
                }
                if (numWars > 0 && !us.GetRelations(them).AtWar)
                {
                    TotalTrustRequiredFromUS = TotalTrustRequiredFromUS - (float) dt.NAPact;
                }
                else if (us.GetRelations(them).Threat >= 20f)
                {
                    TotalTrustRequiredFromUS = TotalTrustRequiredFromUS - (float) dt.NAPact;
                }
            }
            foreach (string tech in FromUs.TechnologiesOffered)
            {
                TotalTrustRequiredFromUS = TotalTrustRequiredFromUS + ResourceManager.TechTree[tech].Cost / 50f;
            }
            float ValueFromUs = 0f;
            float ValueToUs = 0f;
            if (FromUs.OpenBorders)
            {
                ValueFromUs = ValueFromUs + 5f;
            }
            if (ToUs.OpenBorders)
            {
                ValueToUs = ValueToUs + 0.01f;
            }
            if (FromUs.NAPact)
            {
                ValueFromUs = ValueFromUs + 5f;
            }
            if (ToUs.NAPact)
            {
                ValueToUs = ValueToUs + 5f;
            }
            if (FromUs.TradeTreaty)
            {
                ValueFromUs = ValueFromUs + 5f;
            }
            if (ToUs.TradeTreaty)
            {
                ValueToUs = ValueToUs + 5f;
                if ((double) this.OwnerEmpire.EstimateIncomeAtTaxRate(0.5f) < 1)
                {
                    ValueToUs = ValueToUs + 20f;
                }
            }
            foreach (string tech in FromUs.TechnologiesOffered)
            {
                ValueFromUs = ValueFromUs + (us.data.EconomicPersonality.Name == "Technologists"
                                  ? ResourceManager.TechTree[tech].Cost / 50f * 0.25f +
                                    ResourceManager.TechTree[tech].Cost / 50f
                                  : ResourceManager.TechTree[tech].Cost / 50f);
            }
            foreach (string artifactsOffered in FromUs.ArtifactsOffered)
            {
                ValueFromUs = ValueFromUs + 15f;
            }
            foreach (string str in ToUs.ArtifactsOffered)
            {
                ValueToUs = ValueToUs + 15f;
            }
            foreach (string tech in ToUs.TechnologiesOffered)
            {
                ValueToUs = ValueToUs + (us.data.EconomicPersonality.Name == "Technologists"
                                ? ResourceManager.TechTree[tech].Cost / 50f * 0.25f +
                                  ResourceManager.TechTree[tech].Cost / 50f
                                : ResourceManager.TechTree[tech].Cost / 50f);
            }
            if (us.GetPlanets().Count - FromUs.ColoniesOffered.Count + ToUs.ColoniesOffered.Count < 1)
            {
                us.GetRelations(them).DamageRelationship(us, them, "Insulted", 25f, null);
                return "OfferResponse_Reject_Insulting";
            }
            foreach (string planetName in FromUs.ColoniesOffered)
            {
                foreach (Planet p in us.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }
                    float value = p.Population / 1000f + p.FoodHere / 25f + p.ProductionHere / 25f + p.Fertility +
                                  p.MineralRichness + p.MaxPopulation / 1000f;
                    foreach (Building b in p.BuildingList)
                    {
                        value = value + b.Cost / 25f;
                        if (b.Name != "Capital City")
                        {
                            continue;
                        }
                        value = value + 100f;
                    }
                    float multiplier = 0f;
                    foreach (Planet other in p.ParentSystem.PlanetList)
                    {
                        if (other.Owner != p.Owner)
                        {
                            continue;
                        }
                        multiplier = multiplier + 1.25f;
                    }
                    value = value * multiplier;
                    if (value < 15f)
                    {
                        value = 15f;
                    }
                    ValueFromUs = ValueFromUs + (us.data.EconomicPersonality.Name == "Expansionists"
                                      ? value + value
                                      : value + 0.5f * value);
                }
            }
            foreach (string planetName in ToUs.ColoniesOffered)
            {
                foreach (Planet p in them.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }
                    float value = p.Population / 1000f + p.FoodHere / 50f + p.ProductionHere / 50f + p.Fertility +
                                  p.MineralRichness + p.MaxPopulation / 2000f;
                    foreach (Building b in p.BuildingList)
                    {
                        value = value + b.Cost / 50f;
                    }
                    int multiplier = 1;
                    foreach (Planet other in p.ParentSystem.PlanetList)
                    {
                        if (other.Owner != p.Owner)
                        {
                            continue;
                        }
                        multiplier++;
                    }
                    value = value * (float) multiplier;
                    ValueToUs = ValueToUs + (us.data.EconomicPersonality.Name == "Expansionists"
                                    ? value * 0.5f + value
                                    : value);
                }
            }
            ValueToUs = ValueToUs + them.data.Traits.DiplomacyMod * ValueToUs;
            if (ValueFromUs == 0f && ValueToUs > 0f)
            {
                us.GetRelations(them).ImproveRelations(ValueToUs, ValueToUs);
                this.AcceptOffer(ToUs, FromUs, us, them);
                return "OfferResponse_Accept_Gift";
            }
            ValueToUs = ValueToUs - ValueToUs * us.GetRelations(them).TotalAnger / 100f;
            float offerdifferential = ValueToUs / (ValueFromUs + 0.01f);
            string OfferQuality = "";
            if (offerdifferential < 0.6f)
            {
                OfferQuality = "Insulting";
            }
            else if (offerdifferential < 0.9f && offerdifferential >= 0.6f)
            {
                OfferQuality = "Poor";
            }
            else if (offerdifferential >= 0.9f && offerdifferential < 1.1f)
            {
                OfferQuality = "Fair";
            }
            else if ((double) offerdifferential >= 1.1 && (double) offerdifferential < 1.45)
            {
                OfferQuality = "Good";
            }
            else if (offerdifferential >= 1.45f)
            {
                OfferQuality = "Great";
            }
            if (ValueToUs == ValueFromUs)
            {
                OfferQuality = "Fair";
            }
            switch (attitude)
            {
                case Offer.Attitude.Pleading:
                {
                    if (TotalTrustRequiredFromUS > us.GetRelations(them).Trust)
                    {
                        if (OfferQuality != "Great")
                        {
                            return "OfferResponse_InsufficientTrust";
                        }
                        us.GetRelations(them).ImproveRelations(ValueToUs - ValueFromUs, ValueToUs - ValueFromUs);
                        this.AcceptOffer(ToUs, FromUs, us, them);
                        return "OfferResponse_AcceptGreatOffer_LowTrust";
                    }
                    if (offerdifferential < 0.6f)
                    {
                        OfferQuality = "Insulting";
                    }
                    else if (offerdifferential < 0.8f && offerdifferential > 0.65f)
                    {
                        OfferQuality = "Poor";
                    }
                    else if (offerdifferential >= 0.8f && offerdifferential < 1.1f)
                    {
                        OfferQuality = "Fair";
                    }
                    else if ((double) offerdifferential >= 1.1 && (double) offerdifferential < 1.45)
                    {
                        OfferQuality = "Good";
                    }
                    else if (offerdifferential >= 1.45f)
                    {
                        OfferQuality = "Great";
                    }
                    if (OfferQuality == "Poor")
                    {
                        return "OfferResponse_Reject_PoorOffer_EnoughTrust";
                    }
                    if (OfferQuality == "Insulting")
                    {
                        us.GetRelations(them).DamageRelationship(us, them, "Insulted", ValueFromUs - ValueToUs, null);
                        return "OfferResponse_Reject_Insulting";
                    }
                    if (OfferQuality == "Fair")
                    {
                        us.GetRelations(them).ImproveRelations(ValueToUs - ValueFromUs, ValueToUs - ValueFromUs);
                        this.AcceptOffer(ToUs, FromUs, us, them);
                        return "OfferResponse_Accept_Fair_Pleading";
                    }
                    if (OfferQuality == "Good")
                    {
                        us.GetRelations(them).ImproveRelations(ValueToUs - ValueFromUs, ValueToUs - ValueFromUs);
                        this.AcceptOffer(ToUs, FromUs, us, them);
                        return "OfferResponse_Accept_Good";
                    }
                    if (OfferQuality != "Great")
                    {
                        break;
                    }
                    us.GetRelations(them).ImproveRelations(ValueToUs - ValueFromUs, ValueToUs - ValueFromUs);
                    this.AcceptOffer(ToUs, FromUs, us, them);
                    return "OfferResponse_Accept_Great";
                }
                case Offer.Attitude.Respectful:
                {
                    if (TotalTrustRequiredFromUS + us.GetRelations(them).TrustUsed <= us.GetRelations(them).Trust)
                    {
                        if (OfferQuality == "Poor")
                        {
                            return "OfferResponse_Reject_PoorOffer_EnoughTrust";
                        }
                        if (OfferQuality == "Insulting")
                        {
                            us.GetRelations(them)
                                .DamageRelationship(us, them, "Insulted", ValueFromUs - ValueToUs, null);
                            return "OfferResponse_Reject_Insulting";
                        }
                        if (OfferQuality == "Fair")
                        {
                            us.GetRelations(them).ImproveRelations(ValueToUs - ValueFromUs, ValueToUs - ValueFromUs);
                            this.AcceptOffer(ToUs, FromUs, us, them);
                            return "OfferResponse_Accept_Fair";
                        }
                        if (OfferQuality == "Good")
                        {
                            us.GetRelations(them).ImproveRelations(ValueToUs - ValueFromUs, ValueToUs - ValueFromUs);
                            this.AcceptOffer(ToUs, FromUs, us, them);
                            return "OfferResponse_Accept_Good";
                        }
                        if (OfferQuality != "Great")
                        {
                            break;
                        }
                        us.GetRelations(them).ImproveRelations(ValueToUs - ValueFromUs, ValueToUs - ValueFromUs);
                        this.AcceptOffer(ToUs, FromUs, us, them);
                        return "OfferResponse_Accept_Great";
                    }
                    else
                    {
                        if (OfferQuality == "Great")
                        {
                            us.GetRelations(them).ImproveRelations(ValueToUs - ValueFromUs, ValueToUs);
                            this.AcceptOffer(ToUs, FromUs, us, them);
                            return "OfferResponse_AcceptGreatOffer_LowTrust";
                        }
                        if (OfferQuality == "Poor")
                        {
                            return "OfferResponse_Reject_PoorOffer_LowTrust";
                        }
                        if (OfferQuality == "Fair" || OfferQuality == "Good")
                        {
                            return "OfferResponse_InsufficientTrust";
                        }
                        if (OfferQuality != "Insulting")
                        {
                            break;
                        }
                        us.GetRelations(them).DamageRelationship(us, them, "Insulted", ValueFromUs - ValueToUs, null);
                        return "OfferResponse_Reject_Insulting";
                    }
                }
                case Offer.Attitude.Threaten:
                {
                    if (dt.Name == "Ruthless")
                    {
                        return "OfferResponse_InsufficientFear";
                    }
                    us.GetRelations(them).DamageRelationship(us, them, "Insulted", ValueFromUs - ValueToUs, null);
                    if (OfferQuality == "Great")
                    {
                        this.AcceptThreat(ToUs, FromUs, us, them);
                        return "OfferResponse_AcceptGreatOffer_LowTrust";
                    }
                    if (offerdifferential < 0.95f)
                    {
                        OfferQuality = "Poor";
                    }
                    else if (offerdifferential >= 0.95f)
                    {
                        OfferQuality = "Fair";
                    }
                    if (us.GetRelations(them).Threat <= ValueFromUs || us.GetRelations(them).FearUsed + ValueFromUs >=
                        us.GetRelations(them).Threat)
                    {
                        return "OfferResponse_InsufficientFear";
                    }
                    if (OfferQuality == "Poor")
                    {
                        this.AcceptThreat(ToUs, FromUs, us, them);
                        return "OfferResponse_Accept_Bad_Threatening";
                    }
                    if (OfferQuality != "Fair")
                    {
                        break;
                    }
                    this.AcceptThreat(ToUs, FromUs, us, them);
                    return "OfferResponse_Accept_Fair_Threatening";
                }
            }
            return "";
        }

        public EmpireAI.PeaceAnswer AnalyzePeaceOffer(Offer ToUs, Offer FromUs, Empire them, Offer.Attitude attitude)
        {
            WarState state;
            Empire us = this.OwnerEmpire;
            DTrait dt = us.data.DiplomaticPersonality;
            float ValueToUs = 0f;
            float ValueFromUs = 0f;
            foreach (string tech in FromUs.TechnologiesOffered)
            {
                ValueFromUs = ValueFromUs + (us.data.EconomicPersonality.Name == "Technologists"
                                  ? ResourceManager.TechTree[tech].Cost / 100f * 0.25f +
                                    ResourceManager.TechTree[tech].Cost / 100f
                                  : ResourceManager.TechTree[tech].Cost / 100f);
            }
            foreach (string artifactsOffered in FromUs.ArtifactsOffered)
            {
                ValueFromUs = ValueFromUs + 15f;
            }
            foreach (string str in ToUs.ArtifactsOffered)
            {
                ValueToUs = ValueToUs + 15f;
            }
            foreach (string tech in ToUs.TechnologiesOffered)
            {
                ValueToUs = ValueToUs + (us.data.EconomicPersonality.Name == "Technologists"
                                ? ResourceManager.TechTree[tech].Cost / 100f * 0.25f +
                                  ResourceManager.TechTree[tech].Cost / 100f
                                : ResourceManager.TechTree[tech].Cost / 100f);
            }
            foreach (string planetName in FromUs.ColoniesOffered)
            {
                foreach (Planet p in us.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }
                    float value = p.Population / 1000f + p.FoodHere / 50f + p.ProductionHere / 50f + p.Fertility +
                                  p.MineralRichness + p.MaxPopulation / 10000f;
                    foreach (Building b in p.BuildingList)
                    {
                        value = value + b.Cost / 50f;
                    }
                    ValueFromUs = ValueFromUs + (us.data.EconomicPersonality.Name == "Expansionists"
                                      ? value + value
                                      : value + 0.5f * value);
                }
            }
            Array<Planet> PlanetsToUs = new Array<Planet>();
            foreach (string planetName in ToUs.ColoniesOffered)
            {
                foreach (Planet p in them.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }
                    PlanetsToUs.Add(p);
                    float value = p.Population / 1000f + p.FoodHere / 50f + p.ProductionHere / 50f + p.Fertility +
                                  p.MineralRichness + p.MaxPopulation / 10000f;
                    foreach (Building b in p.BuildingList)
                    {
                        value = value + b.Cost / 50f;
                        if (b.NameTranslationIndex != 409)
                        {
                            continue;
                        }
                        value = value + 1000000f;
                    }
                    ValueToUs = ValueToUs + (us.data.EconomicPersonality.Name == "Expansionists"
                                    ? value * 0.5f + value
                                    : value);
                }
            }
            string name = dt.Name;
            string str1 = name;
            if (name != null)
            {
                if (str1 == "Pacifist")
                {
                    switch (us.GetRelations(them).ActiveWar.WarType)
                    {
                        case WarType.BorderConflict:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetBorderConflictState(PlanetsToUs))
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 10f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 5f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 10f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.ImperialistWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 10f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 5f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 10f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.DefensiveWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 10f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 5f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 10f;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
                else if (str1 == "Honorable")
                {
                    switch (us.GetRelations(them).ActiveWar.WarType)
                    {
                        case WarType.BorderConflict:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetBorderConflictState(PlanetsToUs))
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 15f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 8f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 8f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 15f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.ImperialistWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 15f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 8f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 8f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 15f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.DefensiveWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 10f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 5f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 10f;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
                else if (str1 == "Cunning")
                {
                    switch (us.GetRelations(them).ActiveWar.WarType)
                    {
                        case WarType.BorderConflict:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetBorderConflictState(PlanetsToUs))
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 10f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 5f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 10f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.ImperialistWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 10f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 5f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 10f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.DefensiveWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 10f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 5f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 10f;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
                else if (str1 == "Xenophobic")
                {
                    switch (us.GetRelations(them).ActiveWar.WarType)
                    {
                        case WarType.BorderConflict:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetBorderConflictState(PlanetsToUs))
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 15f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 8f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 8f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 15f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.ImperialistWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 15f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 8f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 8f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 15f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.DefensiveWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 10f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 5f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 10f;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
                else if (str1 == "Aggressive")
                {
                    switch (us.GetRelations(them).ActiveWar.WarType)
                    {
                        case WarType.BorderConflict:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetBorderConflictState(PlanetsToUs))
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 10f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 75f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 200f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.ImperialistWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 10f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 75f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 200f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.DefensiveWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 10f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 75f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 200f;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
                else if (str1 == "Ruthless")
                {
                    switch (us.GetRelations(them).ActiveWar.WarType)
                    {
                        case WarType.BorderConflict:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetBorderConflictState(PlanetsToUs))
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 1f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 120f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 300f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.ImperialistWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 1f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 120f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 300f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.DefensiveWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 1f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 120f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 300f;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
            }
            ValueToUs = ValueToUs + them.data.Traits.DiplomacyMod * ValueToUs;
            float offerdifferential = ValueToUs / (ValueFromUs + 0.0001f);
            string OfferQuality = "";
            if (offerdifferential < 0.6f)
            {
                OfferQuality = "Insulting";
            }
            else if (offerdifferential < 0.9f && offerdifferential > 0.65f)
            {
                OfferQuality = "Poor";
            }
            else if (offerdifferential >= 0.9f && offerdifferential < 1.1f)
            {
                OfferQuality = "Fair";
            }
            else if ((double) offerdifferential >= 1.1 && (double) offerdifferential < 1.45)
            {
                OfferQuality = "Good";
            }
            else if (offerdifferential >= 1.45f)
            {
                OfferQuality = "Great";
            }
            if (ValueToUs == ValueFromUs && ValueToUs > 0f)
            {
                OfferQuality = "Fair";
            }
            EmpireAI.PeaceAnswer response = new EmpireAI.PeaceAnswer()
            {
                peace = false,
                answer = "REJECT_OFFER_PEACE_POOROFFER"
            };
            switch (us.GetRelations(them).ActiveWar.WarType)
            {
                case WarType.BorderConflict:
                {
                    state = us.GetRelations(them).ActiveWar.GetBorderConflictState(PlanetsToUs);
                    if (state == WarState.WinningSlightly)
                    {
                        if (OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else if ((OfferQuality == "Fair" || OfferQuality == "Good") &&
                                 us.GetRelations(them).ActiveWar.StartingNumContestedSystems > 0)
                        {
                            response.answer = "REJECT_OFFER_PEACE_UNWILLING_BC";
                            return response;
                        }
                        else if (OfferQuality == "Fair" || OfferQuality == "Good")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else
                        {
                            response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                            return response;
                        }
                    }
                    else if (state == WarState.Dominating)
                    {
                        if (OfferQuality == "Good" || OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else
                        {
                            response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                            return response;
                        }
                    }
                    else if (state == WarState.ColdWar)
                    {
                        if (OfferQuality != "Great")
                        {
                            response.answer = "REJECT_OFFER_PEACE_UNWILLING_BC";
                            return response;
                        }
                        else
                        {
                            response.answer = "ACCEPT_PEACE_COLDWAR";
                            response.peace = true;
                            return response;
                        }
                    }
                    else if (state != WarState.EvenlyMatched)
                    {
                        if (state != WarState.LosingSlightly)
                        {
                            if (state != WarState.LosingBadly)
                            {
                                return response;
                            }
                            if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                            {
                                response.answer = "ACCEPT_OFFER_PEACE";
                                response.peace = true;
                                return response;
                            }
                            else if (OfferQuality != "Poor")
                            {
                                response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                                return response;
                            }
                            else
                            {
                                response.answer = "ACCEPT_OFFER_PEACE_RELUCTANT";
                                response.peace = true;
                                return response;
                            }
                        }
                        else if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else
                        {
                            response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                            return response;
                        }
                    }
                    else if (OfferQuality == "Great")
                    {
                        response.answer = "ACCEPT_OFFER_PEACE";
                        response.peace = true;
                        return response;
                    }
                    else if ((OfferQuality == "Fair" || OfferQuality == "Good") &&
                             us.GetRelations(them).ActiveWar.StartingNumContestedSystems > 0)
                    {
                        response.answer = "REJECT_OFFER_PEACE_UNWILLING_BC";
                        return response;
                    }
                    else if (OfferQuality == "Fair" || OfferQuality == "Good")
                    {
                        response.answer = "ACCEPT_OFFER_PEACE";
                        response.peace = true;
                        return response;
                    }
                    else
                    {
                        response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                        return response;
                    }
                }
                case WarType.ImperialistWar:
                {
                    state = us.GetRelations(them).ActiveWar.GetWarScoreState();
                    if (state == WarState.WinningSlightly)
                    {
                        if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else
                        {
                            response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                            return response;
                        }
                    }
                    else if (state == WarState.Dominating)
                    {
                        if (OfferQuality == "Good" || OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else
                        {
                            response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                            return response;
                        }
                    }
                    else if (state == WarState.EvenlyMatched)
                    {
                        if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else
                        {
                            response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                            return response;
                        }
                    }
                    else if (state == WarState.ColdWar)
                    {
                        string name1 = this.OwnerEmpire.data.DiplomaticPersonality.Name;
                        str1 = name1;
                        if (name1 != null && str1 == "Pacifist")
                        {
                            if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                            {
                                response.answer = "ACCEPT_OFFER_PEACE";
                                response.peace = true;
                                return response;
                            }
                            else
                            {
                                response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                                return response;
                            }
                        }
                        else if (OfferQuality != "Great")
                        {
                            response.answer = "REJECT_PEACE_RUTHLESS";
                            return response;
                        }
                        else
                        {
                            response.answer = "ACCEPT_PEACE_COLDWAR";
                            response.peace = true;
                            return response;
                        }
                    }
                    else if (state != WarState.LosingSlightly)
                    {
                        if (state != WarState.LosingBadly)
                        {
                            return response;
                        }
                        if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else if (OfferQuality != "Poor")
                        {
                            response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                            return response;
                        }
                        else
                        {
                            response.answer = "ACCEPT_OFFER_PEACE_RELUCTANT";
                            response.peace = true;
                            return response;
                        }
                    }
                    else if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                    {
                        response.answer = "ACCEPT_OFFER_PEACE";
                        response.peace = true;
                        return response;
                    }
                    else
                    {
                        response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                        return response;
                    }
                }
                case WarType.GenocidalWar:
                {
                    return response;
                }
                case WarType.DefensiveWar:
                {
                    state = us.GetRelations(them).ActiveWar.GetWarScoreState();
                    if (state == WarState.WinningSlightly)
                    {
                        if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else
                        {
                            response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                            return response;
                        }
                    }
                    else if (state == WarState.Dominating)
                    {
                        if (OfferQuality == "Good" || OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else
                        {
                            response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                            return response;
                        }
                    }
                    else if (state == WarState.EvenlyMatched)
                    {
                        if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else
                        {
                            response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                            return response;
                        }
                    }
                    else if (state == WarState.ColdWar)
                    {
                        string name2 = this.OwnerEmpire.data.DiplomaticPersonality.Name;
                        str1 = name2;
                        if (name2 != null && str1 == "Pacifist")
                        {
                            if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                            {
                                response.answer = "ACCEPT_OFFER_PEACE";
                                response.peace = true;
                                return response;
                            }
                            else
                            {
                                response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                                return response;
                            }
                        }
                        else if (OfferQuality == "Good" || OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_PEACE_COLDWAR";
                            response.peace = true;
                            return response;
                        }
                        else
                        {
                            response.answer = "REJECT_PEACE_RUTHLESS";
                            return response;
                        }
                    }
                    else if (state != WarState.LosingSlightly)
                    {
                        if (state != WarState.LosingBadly)
                        {
                            return response;
                        }
                        if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else if (OfferQuality != "Poor")
                        {
                            response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                            return response;
                        }
                        else
                        {
                            response.answer = "ACCEPT_OFFER_PEACE_RELUCTANT";
                            response.peace = true;
                            return response;
                        }
                    }
                    else if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                    {
                        response.answer = "ACCEPT_OFFER_PEACE";
                        response.peace = true;
                        return response;
                    }
                    else
                    {
                        response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                        return response;
                    }
                }
                default:
                {
                    return response;
                }
            }
        }

        public struct PeaceAnswer
        {
            public string answer;
            public bool peace;
        }

        public void SetAlliance(bool ally, Empire them)
        {
            if (ally)
            {
                this.OwnerEmpire.GetRelations(them).Treaty_Alliance = true;
                this.OwnerEmpire.GetRelations(them).Treaty_OpenBorders = true;
                them.GetRelations(this.OwnerEmpire).Treaty_Alliance = true;
                them.GetRelations(this.OwnerEmpire).Treaty_OpenBorders = true;
                return;
            }
            this.OwnerEmpire.GetRelations(them).Treaty_Alliance = false;
            this.OwnerEmpire.GetRelations(them).Treaty_OpenBorders = false;
            them.GetRelations(this.OwnerEmpire).Treaty_Alliance = false;
            them.GetRelations(this.OwnerEmpire).Treaty_OpenBorders = false;
        }

        public void SetAlliance(bool ally)
        {
            if (ally)
            {
                this.OwnerEmpire.GetRelations(Empire.Universe.PlayerEmpire).Treaty_Alliance = true;
                this.OwnerEmpire.GetRelations(Empire.Universe.PlayerEmpire).Treaty_OpenBorders = true;
                Empire.Universe.PlayerEmpire.GetRelations(this.OwnerEmpire).Treaty_Alliance = true;
                Empire.Universe.PlayerEmpire.GetRelations(this.OwnerEmpire).Treaty_OpenBorders = true;
                return;
            }
            OwnerEmpire.GetRelations(Empire.Universe.PlayerEmpire).Treaty_Alliance = false;
            OwnerEmpire.GetRelations(Empire.Universe.PlayerEmpire).Treaty_OpenBorders = false;
            Empire.Universe.PlayerEmpire.GetRelations(this.OwnerEmpire).Treaty_Alliance = false;
            Empire.Universe.PlayerEmpire.GetRelations(this.OwnerEmpire).Treaty_OpenBorders = false;
        }
    }
}