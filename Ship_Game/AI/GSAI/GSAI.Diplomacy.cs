using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI {

    public sealed partial class GSAI
    {
        private int FirstDemand = 20;

        private int SecondDemand = 75;

        private void RunDiplomaticPlanner()
        {
            string name = empire.data.DiplomaticPersonality.Name;
            if (name != null)
            {
                switch (name)
                {
                    case "Pacifist": DoPacifistRelations(); break;
                    case "Aggressive": DoAggressiveRelations(); break;
                    case "Honorable": DoHonorableRelations(); break;
                    case "Xenophobic": DoXenophobicRelations(); break;
                    case "Ruthless": DoRuthlessRelations(); break;
                    case "Cunning": DoCunningRelations(); break;
                }
            }
            foreach (KeyValuePair<Empire, Relationship> relationship in empire.AllRelations)
            {
                if (!relationship.Key.isFaction && !empire.isFaction && !relationship.Key.data.Defeated)
                    RunEventChecker(relationship);
            }
        }

        public void AcceptOffer(Offer ToUs, Offer FromUs, Empire us, Empire Them)
        {
            if (ToUs.PeaceTreaty)
            {
                Relationship relation = empire.GetRelations(Them);
                relation.AtWar = false;
                relation.PreparingForWar = false;
                relation.ActiveWar.EndStarDate = Empire.Universe.StarDate;
                relation.WarHistory.Add(relation.ActiveWar);
                if (empire.data.DiplomaticPersonality != null)
                {
                    relation.Posture = Posture.Neutral;
                    if (relation.Anger_FromShipsInOurBorders >
                        (float) (empire.data.DiplomaticPersonality.Territorialism / 3))
                    {
                        relation.Anger_FromShipsInOurBorders =
                            (float) (empire.data.DiplomaticPersonality.Territorialism / 3);
                    }
                    if (relation.Anger_TerritorialConflict >
                        (float) (empire.data.DiplomaticPersonality.Territorialism / 3))
                    {
                        relation.Anger_TerritorialConflict =
                            (float) (empire.data.DiplomaticPersonality.Territorialism / 3);
                    }
                }
                relation.Anger_MilitaryConflict = 0f;
                relation.WarnedAboutShips = false;
                relation.WarnedAboutColonizing = false;
                relation.HaveRejected_Demand_Tech = false;
                relation.HaveRejected_OpenBorders = false;
                relation.HaveRejected_TRADE = false;
                relation.HasDefenseFleet = false;
                if (relation.DefenseFleet != -1)
                {
                    empire.GetFleetsDict()[relation.DefenseFleet].FleetTask.EndTask();
                }
                using (TaskList.AcquireWriteLock())
                {
                    for (int i = TaskList.Count - 1; i >= 0; i--)
                    {
                        MilitaryTask task = TaskList[i];
                        if (task.GetTargetPlanet() == null || task.GetTargetPlanet().Owner == null ||
                            task.GetTargetPlanet().Owner != Them)
                        {
                            continue;
                        }
                        task.EndTask();
                    }
                }
                relation.ActiveWar = null;
                Relationship relationThem = Them.GetRelations(empire);
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
                    relationThem.HaveRejected_Demand_Tech = false;
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
                                if (task.GetTargetPlanet() == null || task.GetTargetPlanet().Owner == null ||
                                    task.GetTargetPlanet().Owner != empire)
                                {
                                    return;
                                }
                                task.EndTask();
                            }, false, false, false);
                    }
                }
                relationThem.ActiveWar = null;
                if (Them == Empire.Universe.PlayerEmpire || empire == Empire.Universe.PlayerEmpire)
                {
                    Empire.Universe.NotificationManager.AddPeaceTreatyEnteredNotification(empire, Them);
                }
                else if (Empire.Universe.PlayerEmpire.GetRelations(Them).Known &&
                         Empire.Universe.PlayerEmpire.GetRelations(empire).Known)
                {
                    Empire.Universe.NotificationManager.AddPeaceTreatyEnteredNotification(empire, Them);
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
                        if (pgs.TroopsHere.Count <= 0 || pgs.TroopsHere[0].GetOwner() != empire)
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
                    p.system.OwnerList.Clear();
                    foreach (Planet pl in p.system.PlanetList)
                    {
                        if (pl.Owner == null || p.system.OwnerList.Contains(pl.Owner))
                        {
                            continue;
                        }
                        p.system.OwnerList.Add(pl.Owner);
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
                    p.system.OwnerList.Clear();
                    foreach (Planet pl in p.system.PlanetList)
                    {
                        if (pl.Owner == null || p.system.OwnerList.Contains(pl.Owner))
                        {
                            continue;
                        }
                        p.system.OwnerList.Add(pl.Owner);
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
                empire.GetRelations(Them).AtWar = false;
                empire.GetRelations(Them).PreparingForWar = false;
                empire.GetRelations(Them).ActiveWar.EndStarDate = Empire.Universe.StarDate;
                empire.GetRelations(Them).WarHistory.Add(empire.GetRelations(Them).ActiveWar);
                empire.GetRelations(Them).Posture = Posture.Neutral;
                if (empire.GetRelations(Them).Anger_FromShipsInOurBorders >
                    (float) (this.empire.data.DiplomaticPersonality.Territorialism / 3))
                {
                    empire.GetRelations(Them).Anger_FromShipsInOurBorders =
                        (float) (this.empire.data.DiplomaticPersonality.Territorialism / 3);
                }
                if (empire.GetRelations(Them).Anger_TerritorialConflict >
                    (float) (this.empire.data.DiplomaticPersonality.Territorialism / 3))
                {
                    empire.GetRelations(Them).Anger_TerritorialConflict =
                        (float) (this.empire.data.DiplomaticPersonality.Territorialism / 3);
                }
                empire.GetRelations(Them).Anger_MilitaryConflict = 0f;
                empire.GetRelations(Them).WarnedAboutShips = false;
                empire.GetRelations(Them).WarnedAboutColonizing = false;
                empire.GetRelations(Them).HaveRejected_Demand_Tech = false;
                empire.GetRelations(Them).HaveRejected_OpenBorders = false;
                empire.GetRelations(Them).HaveRejected_TRADE = false;
                empire.GetRelations(Them).HasDefenseFleet = false;
                if (empire.GetRelations(Them).DefenseFleet != -1)
                {
                    this.empire.GetFleetsDict()[empire.GetRelations(Them).DefenseFleet].FleetTask.EndTask();
                }
                //lock (GlobalStats.TaskLocker)
                {
                    this.TaskList.ForEach(task => //foreach (MilitaryTask task in this.TaskList)
                    {
                        if (task.GetTargetPlanet() == null || task.GetTargetPlanet().Owner == null ||
                            task.GetTargetPlanet().Owner != Them)
                        {
                            return;
                        }
                        task.EndTask();
                    });
                }
                empire.GetRelations(Them).ActiveWar = null;
                Them.GetRelations(this.empire).AtWar = false;
                Them.GetRelations(this.empire).PreparingForWar = false;
                Them.GetRelations(this.empire).ActiveWar.EndStarDate = Empire.Universe.StarDate;
                Them.GetRelations(this.empire).WarHistory.Add(Them.GetRelations(this.empire).ActiveWar);
                Them.GetRelations(this.empire).Posture = Posture.Neutral;
                if (EmpireManager.Player != Them)
                {
                    if (Them.GetRelations(this.empire).Anger_FromShipsInOurBorders >
                        (float) (Them.data.DiplomaticPersonality.Territorialism / 3))
                    {
                        Them.GetRelations(this.empire).Anger_FromShipsInOurBorders =
                            (float) (Them.data.DiplomaticPersonality.Territorialism / 3);
                    }
                    if (Them.GetRelations(this.empire).Anger_TerritorialConflict >
                        (float) (Them.data.DiplomaticPersonality.Territorialism / 3))
                    {
                        Them.GetRelations(this.empire).Anger_TerritorialConflict =
                            (float) (Them.data.DiplomaticPersonality.Territorialism / 3);
                    }
                    Them.GetRelations(this.empire).Anger_MilitaryConflict = 0f;
                    Them.GetRelations(this.empire).WarnedAboutShips = false;
                    Them.GetRelations(this.empire).WarnedAboutColonizing = false;
                    Them.GetRelations(this.empire).HaveRejected_Demand_Tech = false;
                    Them.GetRelations(this.empire).HaveRejected_OpenBorders = false;
                    Them.GetRelations(this.empire).HaveRejected_TRADE = false;
                    if (Them.GetRelations(this.empire).DefenseFleet != -1)
                    {
                        Them.GetFleetsDict()[Them.GetRelations(this.empire).DefenseFleet].FleetTask.EndTask();
                    }
                    //lock (GlobalStats.TaskLocker)
                    {
                        Them.GetGSAI()
                            .TaskList.ForEach(task => //foreach (MilitaryTask task in Them.GetGSAI().TaskList)
                            {
                                if (task.GetTargetPlanet() == null || task.GetTargetPlanet().Owner == null ||
                                    task.GetTargetPlanet().Owner != this.empire)
                                {
                                    return;
                                }
                                task.EndTask();
                            }, false, false, false);
                    }
                }
                Them.GetRelations(this.empire).ActiveWar = null;
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
                        if (pgs.TroopsHere.Count <= 0 || pgs.TroopsHere[0].GetOwner() != this.empire)
                        {
                            continue;
                        }
                        TroopShips.Add(pgs.TroopsHere[0].Launch());
                    }
                    toRemove.Add(p);
                    p.Owner = Them;
                    Them.AddPlanet(p);
                    p.system.OwnerList.Clear();
                    foreach (Planet pl in p.system.PlanetList)
                    {
                        if (pl.Owner == null || p.system.OwnerList.Contains(pl.Owner))
                        {
                            continue;
                        }
                        p.system.OwnerList.Add(pl.Owner);
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
                    p.system.OwnerList.Clear();
                    foreach (Planet pl in p.system.PlanetList)
                    {
                        if (pl.Owner == null || p.system.OwnerList.Contains(pl.Owner))
                        {
                            continue;
                        }
                        p.system.OwnerList.Add(pl.Owner);
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
                if (empire.GetRelations(them).Trust < 90f || empire.GetRelations(them).TotalAnger >= 20f ||
                    empire.GetRelations(them).TurnsKnown <= 100)
                {
                    return "AI_ALLIANCE_REJECT";
                }
                this.SetAlliance(true, them);
                return "AI_ALLIANCE_ACCEPT";
            }
            if (ToUs.PeaceTreaty)
            {
                GSAI.PeaceAnswer answer = this.AnalyzePeaceOffer(ToUs, FromUs, them, attitude);
                if (!answer.peace)
                {
                    return answer.answer;
                }
                this.AcceptOffer(ToUs, FromUs, this.empire, them);
                empire.GetRelations(them).Treaty_Peace = true;
                empire.GetRelations(them).PeaceTurnsRemaining = 100;
                them.GetRelations(this.empire).Treaty_Peace = true;
                them.GetRelations(this.empire).PeaceTurnsRemaining = 100;
                return answer.answer;
            }
            Empire us = this.empire;
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
                if ((double) this.empire.EstimateIncomeAtTaxRate(0.5f) < 1)
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
                    foreach (Planet other in p.system.PlanetList)
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
                    foreach (Planet other in p.system.PlanetList)
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

        public GSAI.PeaceAnswer AnalyzePeaceOffer(Offer ToUs, Offer FromUs, Empire them, Offer.Attitude attitude)
        {
            WarState state;
            Empire us = this.empire;
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
            GSAI.PeaceAnswer response = new GSAI.PeaceAnswer()
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
                        string name1 = this.empire.data.DiplomaticPersonality.Name;
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
                        string name2 = this.empire.data.DiplomaticPersonality.Name;
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

        private void AssessAngerAggressive(KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship,
            Posture posture, float usedTrust)
        {
            if (posture != Posture.Friendly)
            {
                this.AssessDiplomaticAnger(Relationship);
            }
            else if (Relationship.Value.Treaty_OpenBorders ||
                     !Relationship.Value.Treaty_Trade && !Relationship.Value.Treaty_NAPact ||
                     Relationship.Value.HaveRejected_OpenBorders)
            {
                if (Relationship.Value.HaveRejected_OpenBorders || Relationship.Value.TotalAnger > 50f &&
                    Relationship.Value.Trust < Relationship.Value.TotalAnger)
                {
                    Relationship.Value.Posture = Posture.Neutral;
                    return;
                }
            }
            else if (Relationship.Value.Trust >= 50f)
            {
                if (Relationship.Value.Trust - usedTrust >
                    (float) (this.empire.data.DiplomaticPersonality.Territorialism / 2))
                {
                    Offer NAPactOffer = new Offer()
                    {
                        OpenBorders = true,
                        AcceptDL = "Open Borders Accepted",
                        RejectDL = "Open Borders Friends Rejected"
                    };
                    Ship_Game.Gameplay.Relationship value = Relationship.Value;
                    NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_OpenBorders,
                        (bool x) => value.HaveRejected_OpenBorders = x);
                    Offer OurOffer = new Offer()
                    {
                        OpenBorders = true
                    };
                    if (Relationship.Key != Empire.Universe.PlayerEmpire)
                    {
                        Relationship.Key.GetGSAI()
                            .AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Pleading);
                        return;
                    }
                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire,
                        Empire.Universe.PlayerEmpire, "Offer Open Borders Friends", OurOffer, NAPactOffer));
                    return;
                }
            }
            else if (Relationship.Value.Trust >= 20f &&
                     Relationship.Value.Anger_TerritorialConflict + Relationship.Value.Anger_FromShipsInOurBorders >=
                     0.75f * (float) this.empire.data.DiplomaticPersonality.Territorialism)
            {
                if (Relationship.Value.Trust - usedTrust >
                    (float) (this.empire.data.DiplomaticPersonality.Territorialism / 2))
                {
                    Offer NAPactOffer = new Offer()
                    {
                        OpenBorders = true,
                        AcceptDL = "Open Borders Accepted",
                        RejectDL = "Open Borders Rejected"
                    };
                    Ship_Game.Gameplay.Relationship relationship = Relationship.Value;
                    NAPactOffer.ValueToModify = new Ref<bool>(() => relationship.HaveRejected_OpenBorders,
                        (bool x) => relationship.HaveRejected_OpenBorders = x);
                    Offer OurOffer = new Offer()
                    {
                        OpenBorders = true
                    };
                    if (Relationship.Key != Empire.Universe.PlayerEmpire)
                    {
                        Relationship.Key.GetGSAI()
                            .AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Pleading);
                        return;
                    }
                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire,
                        Empire.Universe.PlayerEmpire, "Offer Open Borders", OurOffer, NAPactOffer));
                    return;
                }
            }
            else if (Relationship.Value.turnsSinceLastContact >= 10 && Relationship.Value.Known &&
                     Relationship.Key == Empire.Universe.PlayerEmpire)
            {
                Ship_Game.Gameplay.Relationship r = Relationship.Value;
                if (r.Anger_FromShipsInOurBorders >
                    (float) (this.empire.data.DiplomaticPersonality.Territorialism / 4) && !r.AtWar &&
                    !r.WarnedAboutShips && r.turnsSinceLastContact > 10)
                {
                    this.ThreatMatrix.ClearBorders();
                    if (!r.WarnedAboutColonizing)
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire,
                            Relationship.Key, "Warning Ships"));
                    }
                    else
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire,
                            Relationship.Key, "Warning Colonized then Ships", r.GetContestedSystem()));
                    }
                    r.WarnedAboutShips = true;
                    return;
                }
            }
        }

        private void AssessAngerPacifist(KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship,
            Posture posture, float usedTrust)
        {
            if (posture != Posture.Friendly)
            {
                this.AssessDiplomaticAnger(Relationship);
            }
            else if (!Relationship.Value.Treaty_OpenBorders &&
                     (Relationship.Value.Treaty_Trade || Relationship.Value.Treaty_NAPact) &&
                     !Relationship.Value.HaveRejected_OpenBorders)
            {
                if (Relationship.Value.Trust >= 50f)
                {
                    if (Relationship.Value.Trust - usedTrust >
                        (float) (this.empire.data.DiplomaticPersonality.Territorialism / 2))
                    {
                        Offer NAPactOffer = new Offer()
                        {
                            OpenBorders = true,
                            AcceptDL = "Open Borders Accepted",
                            RejectDL = "Open Borders Friends Rejected"
                        };
                        Ship_Game.Gameplay.Relationship value = Relationship.Value;
                        NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_OpenBorders,
                            (bool x) => value.HaveRejected_OpenBorders = x);
                        Offer OurOffer = new Offer()
                        {
                            OpenBorders = true
                        };
                        if (Relationship.Key != Empire.Universe.PlayerEmpire)
                        {
                            Relationship.Key.GetGSAI()
                                .AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Pleading);
                            return;
                        }
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire,
                            Empire.Universe.PlayerEmpire, "Offer Open Borders Friends", OurOffer, NAPactOffer));
                        return;
                    }
                }
                else if (Relationship.Value.Trust >= 20f &&
                         Relationship.Value.Anger_TerritorialConflict +
                         Relationship.Value.Anger_FromShipsInOurBorders >=
                         0.75f * (float) this.empire.data.DiplomaticPersonality.Territorialism &&
                         Relationship.Value.Trust - usedTrust >
                         (float) (this.empire.data.DiplomaticPersonality.Territorialism / 2))
                {
                    Offer NAPactOffer = new Offer()
                    {
                        OpenBorders = true,
                        AcceptDL = "Open Borders Accepted",
                        RejectDL = "Open Borders Rejected"
                    };
                    Ship_Game.Gameplay.Relationship relationship = Relationship.Value;
                    NAPactOffer.ValueToModify = new Ref<bool>(() => relationship.HaveRejected_OpenBorders,
                        (bool x) => relationship.HaveRejected_OpenBorders = x);
                    Offer OurOffer = new Offer()
                    {
                        OpenBorders = true
                    };
                    if (Relationship.Key != Empire.Universe.PlayerEmpire)
                    {
                        Relationship.Key.GetGSAI()
                            .AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Pleading);
                        return;
                    }
                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire,
                        Empire.Universe.PlayerEmpire, "Offer Open Borders", OurOffer, NAPactOffer));
                    return;
                }
            }
            else if (Relationship.Value.turnsSinceLastContact >= 10)
            {
                if (Relationship.Value.Known && Relationship.Key == Empire.Universe.PlayerEmpire)
                {
                    Ship_Game.Gameplay.Relationship r = Relationship.Value;
                    if (r.Anger_FromShipsInOurBorders >
                        (float) (this.empire.data.DiplomaticPersonality.Territorialism / 4) && !r.AtWar &&
                        !r.WarnedAboutShips && r.turnsSinceLastContact > 10)
                    {
                        this.ThreatMatrix.ClearBorders();
                        if (!r.WarnedAboutColonizing)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire,
                                Relationship.Key, "Warning Ships"));
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire,
                                Relationship.Key, "Warning Colonized then Ships", r.GetContestedSystem()));
                        }
                        r.WarnedAboutShips = true;
                        return;
                    }
                }
            }
            else if (Relationship.Value.HaveRejected_OpenBorders || Relationship.Value.TotalAnger > 50f &&
                     Relationship.Value.Trust < Relationship.Value.TotalAnger)
            {
                Relationship.Value.Posture = Posture.Neutral;
                return;
            }
        }

        private void AssessDiplomaticAnger(KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship)
        {
            if (Relationship.Value.Known && Relationship.Key == Empire.Universe.PlayerEmpire)
            {
                Ship_Game.Gameplay.Relationship r = Relationship.Value;
                Empire them = Relationship.Key;
                if (r.Anger_MilitaryConflict >= 5 && !r.AtWar)
                {
                    this.DeclareWarOn(them, WarType.DefensiveWar);
                }
                if (r.Anger_FromShipsInOurBorders >
                    (float) (this.empire.data.DiplomaticPersonality.Territorialism / 4) && !r.AtWar &&
                    !r.WarnedAboutShips && !r.Treaty_Peace && !r.Treaty_OpenBorders)
                {
                    if (!r.WarnedAboutColonizing)
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire, them,
                            "Warning Ships"));
                    }
                    else
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire, them,
                            "Warning Colonized then Ships", r.GetContestedSystem()));
                    }
                    r.turnsSinceLastContact = 0;
                    r.WarnedAboutShips = true;
                    return;
                }
                if (r.Threat < 25f &&
                    r.Anger_TerritorialConflict + r.Anger_FromShipsInOurBorders >=
                    (float) this.empire.data.DiplomaticPersonality.Territorialism && !r.AtWar &&
                    !r.Treaty_OpenBorders && !r.Treaty_Peace)
                {
                    r.PreparingForWar = true;
                    r.PreparingForWarType = WarType.BorderConflict;
                    return;
                }
                if (r.PreparingForWar && r.PreparingForWarType == WarType.BorderConflict)
                {
                    r.PreparingForWar = false;
                    return;
                }
            }
            else if (Relationship.Value.Known)
            {
                Ship_Game.Gameplay.Relationship r = Relationship.Value;
                Empire them = Relationship.Key;
                if (r.Anger_MilitaryConflict >= 5 && !r.AtWar && !r.Treaty_Peace)
                {
                    this.DeclareWarOn(them, WarType.DefensiveWar);
                }
                if (r.Anger_TerritorialConflict + r.Anger_FromShipsInOurBorders >=
                    (float) this.empire.data.DiplomaticPersonality.Territorialism && !r.AtWar &&
                    !r.Treaty_OpenBorders && !r.Treaty_Peace)
                {
                    r.PreparingForWar = true;
                    r.PreparingForWarType = WarType.BorderConflict;
                }
                if (r.Anger_FromShipsInOurBorders >
                    (float) (this.empire.data.DiplomaticPersonality.Territorialism / 2) && !r.AtWar &&
                    !r.WarnedAboutShips)
                {
                    r.turnsSinceLastContact = 0;
                    r.WarnedAboutShips = true;
                }
            }
        }

        public SolarSystem AssignExplorationTargetORIG(Ship queryingShip)
        {
            Array<SolarSystem> Potentials = new Array<SolarSystem>();
            foreach (SolarSystem s in UniverseScreen.SolarSystemList)
            {
                if (s.ExploredDict[this.empire])
                {
                    continue;
                }
                Potentials.Add(s);
            }
            foreach (SolarSystem s in this.MarkedForExploration)
            {
                Potentials.Remove(s);
            }
            IOrderedEnumerable<SolarSystem> sortedList =
                from system in Potentials
                orderby Vector2.Distance(this.empire.GetWeightedCenter(), system.Position)
                select system;
            if (sortedList.Count<SolarSystem>() <= 0)
            {
                queryingShip.AI.OrderQueue.Clear();
                return null;
            }
            this.MarkedForExploration.Add(sortedList.First<SolarSystem>());
            return sortedList.First<SolarSystem>();
        }

        private void DoPacifistRelations()
        {
            this.AssessTeritorialConflicts(this.empire.data.DiplomaticPersonality.Territorialism / 50f);
            foreach (KeyValuePair<Empire, Relationship> Relationship in this.empire.AllRelations)
            {
                if (Relationship.Value.Known && !Relationship.Key.isFaction && !Relationship.Key.data.Defeated)
                {
                    float usedTrust = 0.0f;
                    foreach (TrustEntry trustEntry in (Array<TrustEntry>) Relationship.Value.TrustEntries)
                        usedTrust += trustEntry.TrustCost;
                    switch (Relationship.Value.Posture)
                    {
                        case Posture.Friendly:
                            if (Relationship.Value.TurnsKnown > this.SecondDemand && !Relationship.Value.Treaty_Trade &&
                                (!Relationship.Value.HaveRejected_TRADE &&
                                 (double) Relationship.Value.Trust - (double) usedTrust >
                                 (double) this.empire.data.DiplomaticPersonality.Trade) &&
                                (!Relationship.Value.Treaty_Trade &&
                                 Relationship.Value.turnsSinceLastContact > this.SecondDemand &&
                                 !Relationship.Value.HaveRejected_TRADE))
                            {
                                Offer offer1 = new Offer();
                                offer1.TradeTreaty = true;
                                offer1.AcceptDL = "Trade Accepted";
                                offer1.RejectDL = "Trade Rejected";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify = new Ref<bool>((Func<bool>) (() => r.HaveRejected_TRADE),
                                    (Action<bool>) (x => r.HaveRejected_TRADE = x));
                                Offer offer2 = new Offer();
                                offer2.TradeTreaty = true;
                                if (Relationship.Key == Empire.Universe.PlayerEmpire)
                                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire,
                                        Empire.Universe.PlayerEmpire, "Offer Trade", offer2, offer1));
                                else
                                    Relationship.Key.GetGSAI()
                                        .AnalyzeOffer(offer2, offer1, this.empire, Offer.Attitude.Respectful);
                            }
                            this.AssessAngerPacifist(Relationship, Posture.Friendly, usedTrust);
                            if (Relationship.Value.TurnsAbove95 > 100 &&
                                Relationship.Value.turnsSinceLastContact > 10 &&
                                (!Relationship.Value.Treaty_Alliance && Relationship.Value.Treaty_Trade) &&
                                (Relationship.Value.Treaty_NAPact && !Relationship.Value.HaveRejected_Alliance &&
                                 (double) Relationship.Value.TotalAnger < 20.0))
                            {
                                Offer offer1 = new Offer();
                                offer1.Alliance = true;
                                offer1.AcceptDL = "ALLIANCE_ACCEPTED";
                                offer1.RejectDL = "ALLIANCE_REJECTED";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify = new Ref<bool>((Func<bool>) (() => r.HaveRejected_Alliance),
                                    (Action<bool>) (x =>
                                    {
                                        r.HaveRejected_Alliance = x;
                                        this.SetAlliance(!r.HaveRejected_Alliance);
                                    }));
                                Offer offer2 = new Offer();
                                if (Relationship.Key == Empire.Universe.PlayerEmpire)
                                {
                                    Empire.Universe.ScreenManager.AddScreen((GameScreen) new DiplomacyScreen(
                                        Empire.Universe, empire, Empire.Universe.PlayerEmpire, "OFFER_ALLIANCE", offer2,
                                        offer1));
                                    continue;
                                }
                                else
                                {
                                    Relationship.Key.GetGSAI()
                                        .AnalyzeOffer(offer2, offer1, this.empire, Offer.Attitude.Respectful);
                                    continue;
                                }
                            }
                            else
                                continue;
                        case Posture.Neutral:
                            if (Relationship.Value.TurnsKnown == this.FirstDemand && !Relationship.Value.Treaty_NAPact)
                            {
                                Offer offer1 = new Offer();
                                offer1.NAPact = true;
                                offer1.AcceptDL = "NAPact Accepted";
                                offer1.RejectDL = "NAPact Rejected";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify =
                                    new Ref<bool>(() => r.HaveRejected_NAPACT, x => r.HaveRejected_NAPACT = x);
                                Relationship.Value.turnsSinceLastContact = 0;
                                Offer offer2 = new Offer();
                                offer2.NAPact = true;
                                if (Relationship.Key == Empire.Universe.PlayerEmpire)
                                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire,
                                        Empire.Universe.PlayerEmpire, "Offer NAPact", offer2, offer1));
                                else
                                    Relationship.Key.GetGSAI()
                                        .AnalyzeOffer(offer2, offer1, this.empire, Offer.Attitude.Respectful);
                            }
                            if (Relationship.Value.TurnsKnown > this.FirstDemand && Relationship.Value.Treaty_NAPact)
                                Relationship.Value.Posture = Posture.Friendly;
                            else if (Relationship.Value.TurnsKnown > this.FirstDemand &&
                                     Relationship.Value.HaveRejected_NAPACT)
                                Relationship.Value.Posture = Posture.Neutral;
                            this.AssessAngerPacifist(Relationship, Posture.Neutral, usedTrust);
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
                                foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.empire.AllRelations)
                                {
                                    if (keyValuePair.Value.Treaty_Alliance &&
                                        keyValuePair.Key.GetRelations(Relationship.Key).Known &&
                                        !keyValuePair.Key.GetRelations(Relationship.Key).AtWar)
                                        list.Add(keyValuePair.Key);
                                }
                                foreach (Empire Ally in list)
                                {
                                    if (!Relationship.Value.ActiveWar.AlliesCalled.Contains(Ally.data.Traits.Name) &&
                                        this.empire.GetRelations(Ally).turnsSinceLastContact > 10)
                                    {
                                        this.CallAllyToWar(Ally, Relationship.Key);
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
                                                (float) this.empire.data.DiplomaticPersonality.Territorialism)
                                                return;
                                            switch (Relationship.Value.ActiveWar.GetBorderConflictState())
                                            {
                                                case WarState.WinningSlightly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_WINNINGBC");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_LOSINGBC");
                                                    continue;
                                                default:
                                                    continue;
                                            }
                                        case WarType.ImperialistWar:
                                            switch (Relationship.Value.ActiveWar.GetWarScoreState())
                                            {
                                                case WarState.WinningSlightly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR_WINNING");
                                                    continue;
                                                case WarState.EvenlyMatched:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_EVENLY_MATCHED");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_PLEADING");
                                                    continue;
                                                default:
                                                    continue;
                                            }
                                        case WarType.DefensiveWar:
                                            switch (Relationship.Value.ActiveWar.GetBorderConflictState())
                                            {
                                                case WarState.WinningSlightly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR_WINNING");
                                                    continue;
                                                case WarState.EvenlyMatched:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_EVENLY_MATCHED");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_PLEADING");
                                                    continue;
                                                default:
                                                    continue;
                                            }
                                        default:
                                            continue;
                                    }
                                }
                                else
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
            this.AssessTeritorialConflicts(this.empire.data.DiplomaticPersonality.Territorialism / 5f);
            int numberofWars = 0;
            Array<Empire> PotentialTargets = new Array<Empire>();
            foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.AllRelations)
            {
                if (!Relationship.Value.AtWar || Relationship.Key.data.Defeated)
                {
                    continue;
                }
                numberofWars += (int) Relationship.Key.currentMilitaryStrength * 2; //++;
            }
            //Label0:
            foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.AllRelations)
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
                    Offer NAPactOffer = new Offer()
                    {
                        TradeTreaty = true,
                        AcceptDL = "Trade Accepted",
                        RejectDL = "Trade Rejected"
                    };
                    Ship_Game.Gameplay.Relationship value = Relationship.Value;
                    NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_TRADE,
                        (bool x) => value.HaveRejected_TRADE = x);
                    Offer OurOffer = new Offer()
                    {
                        TradeTreaty = true
                    };
                    Relationship.Key.GetGSAI()
                        .AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Respectful);
                }
                float usedTrust = 0f;
                foreach (TrustEntry te in Relationship.Value.TrustEntries)
                {
                    usedTrust = usedTrust + te.TrustCost;
                }
                this.AssessAngerAggressive(Relationship, Relationship.Value.Posture, usedTrust);
                Relationship.Value.Posture = Posture.Hostile;
                if (!Relationship.Value.Known || Relationship.Value.AtWar)
                {
                    continue;
                }
                Relationship.Value.Posture = Posture.Hostile;
                if (Relationship.Key == Empire.Universe.PlayerEmpire && Relationship.Value.Threat <= -15f &&
                    !Relationship.Value.HaveInsulted_Military && Relationship.Value.TurnsKnown > this.FirstDemand)
                {
                    Relationship.Value.HaveInsulted_Military = true;
                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire,
                        Empire.Universe.PlayerEmpire, "Insult Military"));
                }
                if (Relationship.Value.Threat > 0f || Relationship.Value.TurnsKnown <= this.SecondDemand ||
                    Relationship.Value.Treaty_Alliance)
                {
                    if (Relationship.Value.Threat > -45f || numberofWars > this.empire.currentMilitaryStrength) //!= 0)
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
                        if (i >= this.DesiredPlanets.Count)
                        {
                            //goto Label0;    //this tried to restart the loop it's in => bad mojo
                            break;
                        }
                        if (this.DesiredPlanets[i].Owner != Relationship.Key)
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
            if (PotentialTargets.Count > 0 && numberofWars <= this.empire.currentMilitaryStrength) //1)
            {
                IOrderedEnumerable<Empire> sortedList =
                    from target in PotentialTargets
                    orderby Vector2.Distance(this.empire.GetWeightedCenter(), target.GetWeightedCenter())
                    select target;
                bool foundwar = false;
                foreach (Empire e in PotentialTargets)
                {
                    Empire ToAttack = e;
                    if (this.empire.GetRelations(e).Treaty_NAPact)
                    {
                        continue;
                    }
                    this.empire.GetRelations(ToAttack).PreparingForWar = true;
                    foundwar = true;
                }
                if (!foundwar)
                {
                    Empire ToAttack = sortedList.First<Empire>();
                    this.empire.GetRelations(ToAttack).PreparingForWar = true;
                }
            }
        }

        private void DoXenophobicRelations()
        {
            this.AssessTeritorialConflicts(this.empire.data.DiplomaticPersonality.Territorialism / 10f);
            foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in empire.AllRelations)
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
                this.AssessDiplomaticAnger(Relationship);
                switch (Relationship.Value.Posture)
                {
                    case Posture.Friendly:
                    {
                        if (Relationship.Value.TurnsKnown <= SecondDemand ||
                            Relationship.Value.Trust - usedTrust <= empire.data.DiplomaticPersonality.Trade ||
                            Relationship.Value.Treaty_Trade || Relationship.Value.HaveRejected_TRADE ||
                            Relationship.Value.turnsSinceLastContact <= this.SecondDemand ||
                            Relationship.Value.HaveRejected_TRADE)
                        {
                            continue;
                        }
                        Offer NAPactOffer = new Offer()
                        {
                            TradeTreaty = true,
                            AcceptDL = "Trade Accepted",
                            RejectDL = "Trade Rejected"
                        };
                        Ship_Game.Gameplay.Relationship value = Relationship.Value;
                        NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_TRADE,
                            (bool x) => value.HaveRejected_TRADE = x);
                        Offer OurOffer = new Offer()
                        {
                            TradeTreaty = true
                        };
                        if (Relationship.Key != Empire.Universe.PlayerEmpire)
                        {
                            Relationship.Key.GetGSAI()
                                .AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Respectful);
                            continue;
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire,
                                Empire.Universe.PlayerEmpire, "Offer Trade", new Offer(), NAPactOffer));
                            continue;
                        }
                    }
                    case Posture.Neutral:
                    {
                        if (Relationship.Value.TurnsKnown >= this.FirstDemand && !Relationship.Value.Treaty_NAPact &&
                            !Relationship.Value.HaveRejected_Demand_Tech && !Relationship.Value.XenoDemandedTech)
                        {
                            Array<string> PotentialDemands = new Array<string>();
                            foreach (KeyValuePair<string, TechEntry> tech in Relationship.Key.GetTDict())
                            {
                                //Added by McShooterz: prevent root nodes from being demanded, and secret but not discovered
                                if (!tech.Value.Unlocked || this.empire.GetTDict()[tech.Key].Unlocked ||
                                    tech.Value.Tech.RootNode == 1 ||
                                    (tech.Value.Tech.Secret && !tech.Value.Tech.Discovered))
                                {
                                    continue;
                                }
                                PotentialDemands.Add(tech.Key);
                            }
                            if (PotentialDemands.Count > 0)
                            {
                                int Random = (int) RandomMath.RandomBetween(0f, (float) PotentialDemands.Count + 0.75f);
                                if (Random > PotentialDemands.Count - 1)
                                {
                                    Random = PotentialDemands.Count - 1;
                                }
                                string TechToDemand = PotentialDemands[Random];
                                Offer DemandTech = new Offer();
                                DemandTech.TechnologiesOffered.Add(TechToDemand);
                                Relationship.Value.XenoDemandedTech = true;
                                Offer TheirDemand = new Offer()
                                {
                                    AcceptDL = "Xeno Demand Tech Accepted",
                                    RejectDL = "Xeno Demand Tech Rejected"
                                };
                                Ship_Game.Gameplay.Relationship relationship = Relationship.Value;
                                TheirDemand.ValueToModify = new Ref<bool>(() => relationship.HaveRejected_Demand_Tech,
                                    (bool x) => relationship.HaveRejected_Demand_Tech = x);
                                Relationship.Value.turnsSinceLastContact = 0;
                                if (Relationship.Key != Empire.Universe.PlayerEmpire)
                                {
                                    Relationship.Key.GetGSAI()
                                        .AnalyzeOffer(DemandTech, TheirDemand, this.empire, Offer.Attitude.Threaten);
                                }
                                else
                                {
                                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire,
                                        Empire.Universe.PlayerEmpire, "Xeno Demand Tech", DemandTech, TheirDemand));
                                }
                            }
                        }
                        if (!Relationship.Value.HaveRejected_Demand_Tech)
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

        private void DoAggressiveRelations()
        {

            int numberofWars = 0;
            Array<Empire> PotentialTargets = new Array<Empire>();
            foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.AllRelations)
            {
                if (Relationship.Key.data.Defeated || !Relationship.Value.AtWar && !Relationship.Value.PreparingForWar)
                {
                    continue;
                }
                //numberofWars++;
                numberofWars += (int)Relationship.Key.currentMilitaryStrength;
            }
            this.AssessTeritorialConflicts(this.empire.data.DiplomaticPersonality.Territorialism / 10f);
            foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.AllRelations)
            {
                if (!Relationship.Value.Known || Relationship.Value.AtWar || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
                {
                    continue;
                }

                if (Relationship.Key.data.DiplomaticPersonality != null && !Relationship.Value.HaveRejected_TRADE && !Relationship.Value.Treaty_Trade && !Relationship.Value.AtWar && (Relationship.Key.data.DiplomaticPersonality.Name != "Aggressive" || Relationship.Key.data.DiplomaticPersonality.Name != "Ruthless"))
                {
                    Offer NAPactOffer = new Offer()
                    {
                        TradeTreaty = true,
                        AcceptDL = "Trade Accepted",
                        RejectDL = "Trade Rejected"
                    };
                    Ship_Game.Gameplay.Relationship value = Relationship.Value;
                    NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_TRADE, (bool x) => value.HaveRejected_TRADE = x);
                    Offer OurOffer = new Offer()
                    {
                        TradeTreaty = true
                    };
                    Relationship.Key.GetGSAI().AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Respectful);
                }
                float usedTrust = 0f;
                foreach (TrustEntry te in Relationship.Value.TrustEntries)
                {
                    usedTrust = usedTrust + te.TrustCost;
                }
                Relationship.Value.Posture = Posture.Neutral;
                if (Relationship.Value.Threat <= 0f)
                {
                    if (!Relationship.Value.HaveInsulted_Military && Relationship.Value.TurnsKnown > this.FirstDemand)
                    {
                        Relationship.Value.HaveInsulted_Military = true;
                        if (Relationship.Key == Empire.Universe.PlayerEmpire)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "Insult Military"));
                        }
                    }
                    Relationship.Value.Posture = Posture.Hostile;
                }
                else if (Relationship.Value.Threat > 25f && Relationship.Value.TurnsKnown > this.FirstDemand)
                {
                    if (!Relationship.Value.HaveComplimented_Military && Relationship.Value.HaveInsulted_Military && Relationship.Value.TurnsKnown > this.FirstDemand && Relationship.Key == Empire.Universe.PlayerEmpire)
                    {
                        Relationship.Value.HaveComplimented_Military = true;
                        if (!Relationship.Value.HaveInsulted_Military || Relationship.Value.TurnsKnown <= this.SecondDemand)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "Compliment Military"));
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "Compliment Military Better"));
                        }
                    }
                    Relationship.Value.Posture = Posture.Friendly;
                }
                switch (Relationship.Value.Posture)
                {
                    case Posture.Friendly:
                    {
                        if (Relationship.Value.TurnsKnown > this.SecondDemand && Relationship.Value.Trust - usedTrust > (float)this.empire.data.DiplomaticPersonality.Trade && !Relationship.Value.HaveRejected_TRADE && !Relationship.Value.Treaty_Trade)
                        {
                            Offer NAPactOffer = new Offer()
                            {
                                TradeTreaty = true,
                                AcceptDL = "Trade Accepted",
                                RejectDL = "Trade Rejected"
                            };
                            Ship_Game.Gameplay.Relationship relationship = Relationship.Value;
                            NAPactOffer.ValueToModify = new Ref<bool>(() => relationship.HaveRejected_TRADE, (bool x) => relationship.HaveRejected_TRADE = x);
                            Offer OurOffer = new Offer()
                            {
                                TradeTreaty = true
                            };
                            if (Relationship.Key != Empire.Universe.PlayerEmpire)
                            {
                                Relationship.Key.GetGSAI().AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Respectful);
                            }
                            else
                            {
                                Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "Offer Trade", OurOffer, NAPactOffer));
                            }
                        }
                        this.AssessAngerAggressive(Relationship, Relationship.Value.Posture, usedTrust);
                        if (Relationship.Value.TurnsAbove95 <= 100 || Relationship.Value.turnsSinceLastContact <= 10 || Relationship.Value.Treaty_Alliance || !Relationship.Value.Treaty_Trade || !Relationship.Value.Treaty_NAPact || Relationship.Value.HaveRejected_Alliance || Relationship.Value.TotalAnger >= 20f)
                        {
                            continue;
                        }
                        Offer OfferAlliance = new Offer()
                        {
                            Alliance = true,
                            AcceptDL = "ALLIANCE_ACCEPTED",
                            RejectDL = "ALLIANCE_REJECTED"
                        };
                        Ship_Game.Gameplay.Relationship value1 = Relationship.Value;
                        OfferAlliance.ValueToModify = new Ref<bool>(() => value1.HaveRejected_Alliance, (bool x) => {
                            value1.HaveRejected_Alliance = x;
                            this.SetAlliance(!value1.HaveRejected_Alliance);
                        });
                        Offer OurOffer0 = new Offer();
                        if (Relationship.Key != Empire.Universe.PlayerEmpire)
                        {
                            Relationship.Key.GetGSAI().AnalyzeOffer(OurOffer0, OfferAlliance, this.empire, Offer.Attitude.Respectful);
                            continue;
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "OFFER_ALLIANCE", OurOffer0, OfferAlliance));
                            continue;
                        }
                    }
                    case Posture.Neutral:
                    {
                        this.AssessAngerAggressive(Relationship, Relationship.Value.Posture, usedTrust);
                        continue;
                    }
                    case Posture.Hostile:
                    {
                        if (Relationship.Value.Threat < -15f && Relationship.Value.TurnsKnown > this.SecondDemand && !Relationship.Value.Treaty_Alliance)
                        {
                            if (Relationship.Value.TotalAnger < 75f)
                            {
                                int i = 0;
                                while (i < 5)
                                {
                                    if (i >= this.DesiredPlanets.Count)
                                    {
                                        break;
                                    }
                                    if (this.DesiredPlanets[i].Owner != Relationship.Key)
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
                        this.AssessAngerAggressive(Relationship, Relationship.Value.Posture, usedTrust);
                        continue;
                    }
                    default:
                    {
                        continue;   //this doesn't actually do anything, since it's at the end of the loop anyways
                    }
                }
            }
            if (PotentialTargets.Count > 0 && numberofWars * 2 < this.empire.currentMilitaryStrength)//<= 1)
            {
                Empire ToAttack = PotentialTargets.First<Empire>();
                this.empire.GetRelations(ToAttack).PreparingForWar = true;
            }
        }

        private void DoCunningRelations()
        {
            this.DoHonorableRelations();
        }

        private void DoHonorableRelations()
        {
            this.AssessTeritorialConflicts(this.empire.data.DiplomaticPersonality.Territorialism / 10f);
            foreach (KeyValuePair<Empire, Relationship> Relationship in this.empire.AllRelations)
            {
                if (Relationship.Value.Known && !Relationship.Key.isFaction && !Relationship.Key.data.Defeated)
                {
                    switch (Relationship.Value.Posture)
                    {
                        case Posture.Friendly:
                            float usedTrust1 = 0.0f;
                            foreach (TrustEntry trustEntry in (Array<TrustEntry>)Relationship.Value.TrustEntries)
                                usedTrust1 += trustEntry.TrustCost;
                            if (Relationship.Value.TurnsKnown > this.SecondDemand && (double)Relationship.Value.Trust - (double)usedTrust1 > (double)this.empire.data.DiplomaticPersonality.Trade && (Relationship.Value.turnsSinceLastContact > this.SecondDemand && !Relationship.Value.Treaty_Trade) && !Relationship.Value.HaveRejected_TRADE)
                            {
                                Offer offer1 = new Offer();
                                offer1.TradeTreaty = true;
                                offer1.AcceptDL = "Trade Accepted";
                                offer1.RejectDL = "Trade Rejected";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify = new Ref<bool>(() => r.HaveRejected_TRADE, x => r.HaveRejected_TRADE = x);
                                Offer offer2 = new Offer();
                                offer2.TradeTreaty = true;
                                if (Relationship.Key == Empire.Universe.PlayerEmpire)
                                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "Offer Trade", offer2, offer1));
                                else
                                    Relationship.Key.GetGSAI().AnalyzeOffer(offer2, offer1, this.empire, Offer.Attitude.Respectful);
                            }
                            this.AssessAngerPacifist(Relationship, Posture.Friendly, usedTrust1);
                            if (Relationship.Value.TurnsAbove95 > 100 && Relationship.Value.turnsSinceLastContact > 10 && (!Relationship.Value.Treaty_Alliance && Relationship.Value.Treaty_Trade) && (Relationship.Value.Treaty_NAPact && !Relationship.Value.HaveRejected_Alliance && (double)Relationship.Value.TotalAnger < 20.0))
                            {
                                Offer offer1 = new Offer();
                                offer1.Alliance = true;
                                offer1.AcceptDL = "ALLIANCE_ACCEPTED";
                                offer1.RejectDL = "ALLIANCE_REJECTED";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify = new Ref<bool>((Func<bool>)(() => r.HaveRejected_Alliance), (Action<bool>)(x =>
                                {
                                    r.HaveRejected_Alliance = x;
                                    this.SetAlliance(!r.HaveRejected_Alliance);
                                }));
                                Offer offer2 = new Offer();
                                if (Relationship.Key == Empire.Universe.PlayerEmpire)
                                {
                                    Empire.Universe.ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "OFFER_ALLIANCE", offer2, offer1));
                                    continue;
                                }
                                else
                                {
                                    Relationship.Key.GetGSAI().AnalyzeOffer(offer2, offer1, this.empire, Offer.Attitude.Respectful);
                                    continue;
                                }
                            }
                            else
                                continue;
                        case Posture.Neutral:
                            if (Relationship.Value.TurnsKnown == this.FirstDemand && !Relationship.Value.Treaty_NAPact)
                            {
                                Offer offer1 = new Offer();
                                offer1.NAPact = true;
                                offer1.AcceptDL = "NAPact Accepted";
                                offer1.RejectDL = "NAPact Rejected";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify = new Ref<bool>((Func<bool>)(() => r.HaveRejected_NAPACT), (Action<bool>)(x => r.HaveRejected_NAPACT = x));
                                Relationship.Value.turnsSinceLastContact = 0;
                                Offer offer2 = new Offer();
                                offer2.NAPact = true;
                                if (Relationship.Key == Empire.Universe.PlayerEmpire)
                                    Empire.Universe.ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "Offer NAPact", offer2, offer1));
                                else
                                    Relationship.Key.GetGSAI().AnalyzeOffer(offer2, offer1, this.empire, Offer.Attitude.Respectful);
                            }
                            if (Relationship.Value.TurnsKnown > this.FirstDemand && Relationship.Value.Treaty_NAPact)
                                Relationship.Value.Posture = Posture.Friendly;
                            else if (Relationship.Value.TurnsKnown > this.FirstDemand && Relationship.Value.HaveRejected_NAPACT)
                                Relationship.Value.Posture = Posture.Neutral;
                            float usedTrust2 = 0.0f;
                            foreach (TrustEntry trustEntry in (Array<TrustEntry>)Relationship.Value.TrustEntries)
                                usedTrust2 += trustEntry.TrustCost;
                            this.AssessAngerPacifist(Relationship, Posture.Neutral, usedTrust2);
                            continue;
                        case Posture.Hostile:
                            if (Relationship.Value.ActiveWar != null)
                            {
                                Array<Empire> list = new Array<Empire>();
                                foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.empire.AllRelations)
                                {
                                    if (keyValuePair.Value.Treaty_Alliance && keyValuePair.Key.GetRelations(Relationship.Key).Known && !keyValuePair.Key.GetRelations(Relationship.Key).AtWar)
                                        list.Add(keyValuePair.Key);
                                }
                                foreach (Empire Ally in list)
                                {
                                    if (!Relationship.Value.ActiveWar.AlliesCalled.Contains(Ally.data.Traits.Name) && this.empire.GetRelations(Ally).turnsSinceLastContact > 10)
                                    {
                                        this.CallAllyToWar(Ally, Relationship.Key);
                                        Relationship.Value.ActiveWar.AlliesCalled.Add(Ally.data.Traits.Name);
                                    }
                                }
                                if ((double)Relationship.Value.ActiveWar.TurnsAtWar % 100.0 == 0.0)
                                {
                                    switch (Relationship.Value.ActiveWar.WarType)
                                    {
                                        case WarType.BorderConflict:
                                            if ((double)(Relationship.Value.Anger_FromShipsInOurBorders + Relationship.Value.Anger_TerritorialConflict) > (double)this.empire.data.DiplomaticPersonality.Territorialism)
                                                return;
                                            switch (Relationship.Value.ActiveWar.GetBorderConflictState())
                                            {
                                                case WarState.WinningSlightly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_WINNINGBC");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_LOSINGBC");
                                                    continue;
                                                default:
                                                    continue;
                                            }
                                        case WarType.ImperialistWar:
                                            switch (Relationship.Value.ActiveWar.GetWarScoreState())
                                            {
                                                case WarState.WinningSlightly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR_WINNING");
                                                    continue;
                                                case WarState.EvenlyMatched:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_EVENLY_MATCHED");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_PLEADING");
                                                    continue;
                                                default:
                                                    continue;
                                            }
                                        case WarType.DefensiveWar:
                                            switch (Relationship.Value.ActiveWar.GetBorderConflictState())
                                            {
                                                case WarState.WinningSlightly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR_WINNING");
                                                    continue;
                                                case WarState.EvenlyMatched:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_EVENLY_MATCHED");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_PLEADING");
                                                    continue;
                                                default:
                                                    continue;
                                            }
                                        default:
                                            continue;
                                    }
                                }
                                else
                                    continue;
                            }
                            else
                            {
                                this.AssessAngerPacifist(Relationship, Posture.Hostile, 100f);
                                continue;
                            }
                        default:
                            continue;
                    }
                }
            }
        }


        public void SetAlliance(bool ally)
        {
            if (ally)
            {
                this.empire.GetRelations(Empire.Universe.PlayerEmpire).Treaty_Alliance = true;
                this.empire.GetRelations(Empire.Universe.PlayerEmpire).Treaty_OpenBorders = true;
                Empire.Universe.PlayerEmpire.GetRelations(this.empire).Treaty_Alliance = true;
                Empire.Universe.PlayerEmpire.GetRelations(this.empire).Treaty_OpenBorders = true;
                return;
            }
            empire.GetRelations(Empire.Universe.PlayerEmpire).Treaty_Alliance = false;
            empire.GetRelations(Empire.Universe.PlayerEmpire).Treaty_OpenBorders = false;
            Empire.Universe.PlayerEmpire.GetRelations(this.empire).Treaty_Alliance = false;
            Empire.Universe.PlayerEmpire.GetRelations(this.empire).Treaty_OpenBorders = false;
        }

        public void SetAlliance(bool ally, Empire them)
        {
            if (ally)
            {
                this.empire.GetRelations(them).Treaty_Alliance = true;
                this.empire.GetRelations(them).Treaty_OpenBorders = true;
                them.GetRelations(this.empire).Treaty_Alliance = true;
                them.GetRelations(this.empire).Treaty_OpenBorders = true;
                return;
            }
            this.empire.GetRelations(them).Treaty_Alliance = false;
            this.empire.GetRelations(them).Treaty_OpenBorders = false;
            them.GetRelations(this.empire).Treaty_Alliance = false;
            them.GetRelations(this.empire).Treaty_OpenBorders = false;
        }

        public struct PeaceAnswer
        {
            public string answer;
            public bool peace;
        }
    }
}