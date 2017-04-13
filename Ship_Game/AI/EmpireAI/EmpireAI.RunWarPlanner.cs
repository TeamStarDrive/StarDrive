using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI {
    public sealed partial class GSAI
    {
        public void CallAllyToWar(Empire Ally, Empire Enemy)
        {
            Offer offer = new Offer()
            {
                AcceptDL = "HelpUS_War_Yes",
                RejectDL = "HelpUS_War_No"
            };
            string dialogue = "HelpUS_War";
            Offer OurOffer = new Offer()
            {
                ValueToModify = new Ref<bool>(() => Ally.GetRelations(Enemy).AtWar, (bool x) =>
                {
                    if (x)
                    {
                        Ally.GetGSAI().DeclareWarOnViaCall(Enemy, WarType.ImperialistWar);
                        return;
                    }
                    float Amount = 30f;
                    if (this.empire.data.DiplomaticPersonality != null &&
                        this.empire.data.DiplomaticPersonality.Name == "Honorable")
                    {
                        Amount = 60f;
                        offer.RejectDL = "HelpUS_War_No_BreakAlliance";
                        this.empire.GetRelations(Ally).Treaty_Alliance = false;
                        Ally.GetRelations(this.empire).Treaty_Alliance = false;
                        this.empire.GetRelations(Ally).Treaty_OpenBorders = false;
                        this.empire.GetRelations(Ally).Treaty_NAPact = false;
                    }
                    Relationship item = this.empire.GetRelations(Ally);
                    item.Trust = item.Trust - Amount;
                    Relationship angerDiplomaticConflict = this.empire.GetRelations(Ally);
                    angerDiplomaticConflict.Anger_DiplomaticConflict =
                        angerDiplomaticConflict.Anger_DiplomaticConflict + Amount;
                })
            };
            if (Ally == Empire.Universe.PlayerEmpire)
            {
                Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire,
                    Empire.Universe.PlayerEmpire, dialogue, OurOffer, offer, Enemy));
            }
        }

        public void DeclareWarFromEvent(Empire them, WarType wt)
        {
            empire.GetRelations(them).AtWar = true;
            empire.GetRelations(them).Posture = Posture.Hostile;
            empire.GetRelations(them).ActiveWar = new War(this.empire, them, Empire.Universe.StarDate)
            {
                WarType = wt
            };
            if (empire.GetRelations(them).Trust > 0f)
            {
                empire.GetRelations(them).Trust = 0f;
            }
            empire.GetRelations(them).Treaty_OpenBorders = false;
            empire.GetRelations(them).Treaty_NAPact = false;
            empire.GetRelations(them).Treaty_Trade = false;
            empire.GetRelations(them).Treaty_Alliance = false;
            empire.GetRelations(them).Treaty_Peace = false;
            them.GetGSAI().GetWarDeclaredOnUs(this.empire, wt);
        }

        public void DeclareWarOn(Empire them, WarType wt)
        {
            empire.GetRelations(them).PreparingForWar = false;
            if (this.empire.isFaction || this.empire.data.Defeated || (them.data.Defeated || them.isFaction))
                return;
            empire.GetRelations(them).FedQuest = (FederationQuest) null;
            if (this.empire == Empire.Universe.PlayerEmpire && empire.GetRelations(them).Treaty_NAPact)
            {
                empire.GetRelations(them).Treaty_NAPact = false;
                foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.empire.AllRelations)
                {
                    if (keyValuePair.Key != them)
                    {
                        keyValuePair.Key.GetRelations(this.empire).Trust -= 50f;
                        keyValuePair.Key.GetRelations(this.empire).Anger_DiplomaticConflict += 20f;
                        keyValuePair.Key.GetRelations(this.empire).UpdateRelationship(keyValuePair.Key, this.empire);
                    }
                }
                them.GetRelations(this.empire).Trust -= 50f;
                them.GetRelations(this.empire).Anger_DiplomaticConflict += 50f;
                them.GetRelations(this.empire).UpdateRelationship(them, this.empire);
            }
            if (them == Empire.Universe.PlayerEmpire && !empire.GetRelations(them).AtWar)
            {
                switch (wt)
                {
                    case WarType.BorderConflict:
                        if (empire.GetRelations(them).contestedSystemGuid != Guid.Empty)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them,
                                "Declare War BC TarSys", empire.GetRelations(them).GetContestedSystem()));
                            break;
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them,
                                "Declare War BC"));
                            break;
                        }
                    case WarType.ImperialistWar:
                        if (empire.GetRelations(them).Treaty_NAPact)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them,
                                "Declare War Imperialism Break NA"));
                            using (var enumerator = this.empire.AllRelations.GetEnumerator())
                            {
                                while (enumerator.MoveNext())
                                {
                                    KeyValuePair<Empire, Relationship> current = enumerator.Current;
                                    if (current.Key != them)
                                    {
                                        current.Value.Trust -= 50f;
                                        current.Value.Anger_DiplomaticConflict += 20f;
                                    }
                                }
                                break;
                            }
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them,
                                "Declare War Imperialism"));
                            break;
                        }
                    case WarType.DefensiveWar:
                        if (!empire.GetRelations(them).Treaty_NAPact)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them,
                                "Declare War Defense"));
                            empire.GetRelations(them).Anger_DiplomaticConflict += 25f;
                            empire.GetRelations(them).Trust -= 25f;
                            break;
                        }
                        else if (empire.GetRelations(them).Treaty_NAPact)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them,
                                "Declare War Defense BrokenNA"));
                            empire.GetRelations(them).Treaty_NAPact = false;
                            foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.empire.AllRelations)
                            {
                                if (keyValuePair.Key != them)
                                {
                                    keyValuePair.Value.Trust -= 50f;
                                    keyValuePair.Value.Anger_DiplomaticConflict += 20f;
                                }
                            }
                            empire.GetRelations(them).Trust -= 50f;
                            empire.GetRelations(them).Anger_DiplomaticConflict += 50f;
                            break;
                        }
                        else
                            break;
                }
            }
            if (them == Empire.Universe.PlayerEmpire || this.empire == Empire.Universe.PlayerEmpire)
                Empire.Universe.NotificationManager.AddWarDeclaredNotification(this.empire, them);
            else if (Empire.Universe.PlayerEmpire.GetRelations(them).Known &&
                     Empire.Universe.PlayerEmpire.GetRelations(this.empire).Known)
                Empire.Universe.NotificationManager.AddWarDeclaredNotification(this.empire, them);
            empire.GetRelations(them).AtWar = true;
            empire.GetRelations(them).Posture = Posture.Hostile;
            empire.GetRelations(them).ActiveWar = new War(this.empire, them, Empire.Universe.StarDate);
            empire.GetRelations(them).ActiveWar.WarType = wt;
            if (empire.GetRelations(them).Trust > 0f)
                empire.GetRelations(them).Trust = 0.0f;
            empire.GetRelations(them).Treaty_OpenBorders = false;
            empire.GetRelations(them).Treaty_NAPact = false;
            empire.GetRelations(them).Treaty_Trade = false;
            empire.GetRelations(them).Treaty_Alliance = false;
            empire.GetRelations(them).Treaty_Peace = false;
            them.GetGSAI().GetWarDeclaredOnUs(this.empire, wt);
        }

        public void DeclareWarOnViaCall(Empire them, WarType wt)
        {
            empire.GetRelations(them).PreparingForWar = false;
            if (this.empire.isFaction || this.empire.data.Defeated || them.data.Defeated || them.isFaction)
            {
                return;
            }
            empire.GetRelations(them).FedQuest = null;
            if (this.empire == Empire.Universe.PlayerEmpire && empire.GetRelations(them).Treaty_NAPact)
            {
                empire.GetRelations(them).Treaty_NAPact = false;
                Relationship item = them.GetRelations(this.empire);
                item.Trust = item.Trust - 50f;
                Relationship angerDiplomaticConflict = them.GetRelations(this.empire);
                angerDiplomaticConflict.Anger_DiplomaticConflict =
                    angerDiplomaticConflict.Anger_DiplomaticConflict + 50f;
                them.GetRelations(this.empire).UpdateRelationship(them, this.empire);
            }
            if (them == Empire.Universe.PlayerEmpire && !empire.GetRelations(them).AtWar)
            {
                switch (wt)
                {
                    case WarType.BorderConflict:
                    {
                        if (empire.GetRelations(them).contestedSystemGuid == Guid.Empty)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them,
                                "Declare War BC"));
                            break;
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them,
                                "Declare War BC Tarsys", empire.GetRelations(them).GetContestedSystem()));
                            break;
                        }
                    }
                    case WarType.ImperialistWar:
                    {
                        if (!empire.GetRelations(them).Treaty_NAPact)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them,
                                "Declare War Imperialism"));
                            break;
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them,
                                "Declare War Imperialism Break NA"));
                            break;
                        }
                    }
                    case WarType.DefensiveWar:
                    {
                        if (empire.GetRelations(them).Treaty_NAPact)
                        {
                            if (!empire.GetRelations(them).Treaty_NAPact)
                            {
                                break;
                            }
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them,
                                "Declare War Defense BrokenNA"));
                            empire.GetRelations(them).Treaty_NAPact = false;
                            Relationship trust = empire.GetRelations(them);
                            trust.Trust = trust.Trust - 50f;
                            Relationship relationship = empire.GetRelations(them);
                            relationship.Anger_DiplomaticConflict = relationship.Anger_DiplomaticConflict + 50f;
                            break;
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them,
                                "Declare War Defense"));
                            Relationship item1 = empire.GetRelations(them);
                            item1.Anger_DiplomaticConflict = item1.Anger_DiplomaticConflict + 25f;
                            Relationship trust1 = empire.GetRelations(them);
                            trust1.Trust = trust1.Trust - 25f;
                            break;
                        }
                    }
                }
            }
            if (them == Empire.Universe.PlayerEmpire || this.empire == Empire.Universe.PlayerEmpire)
            {
                Empire.Universe.NotificationManager.AddWarDeclaredNotification(this.empire, them);
            }
            else if (Empire.Universe.PlayerEmpire.GetRelations(them).Known &&
                     Empire.Universe.PlayerEmpire.GetRelations(this.empire).Known)
            {
                Empire.Universe.NotificationManager.AddWarDeclaredNotification(this.empire, them);
            }
            empire.GetRelations(them).AtWar = true;
            empire.GetRelations(them).Posture = Posture.Hostile;
            empire.GetRelations(them).ActiveWar = new War(this.empire, them, Empire.Universe.StarDate)
            {
                WarType = wt
            };
            if (empire.GetRelations(them).Trust > 0f)
            {
                empire.GetRelations(them).Trust = 0f;
            }
            empire.GetRelations(them).Treaty_OpenBorders = false;
            empire.GetRelations(them).Treaty_NAPact = false;
            empire.GetRelations(them).Treaty_Trade = false;
            empire.GetRelations(them).Treaty_Alliance = false;
            empire.GetRelations(them).Treaty_Peace = false;
            them.GetGSAI().GetWarDeclaredOnUs(this.empire, wt);
        }

        public void EndWarFromEvent(Empire them)
        {
            this.empire.GetRelations(them).AtWar = false;
            them.GetRelations(this.empire).AtWar = false;
            //lock (GlobalStats.TaskLocker)
            {
                this.TaskList.ForEach(task => //foreach (MilitaryTask task in this.TaskList)
                {
                    if (this.empire.GetFleetsDict().ContainsKey(task.WhichFleet) &&
                        this.empire.data.Traits.Name == "Corsairs")
                    {
                        bool foundhome = false;
                        foreach (Ship ship in this.empire.GetShips())
                        {
                            if (!(ship.shipData.Role == ShipData.RoleName.station) &&
                                !(ship.shipData.Role == ShipData.RoleName.platform))
                            {
                                continue;
                            }
                            foundhome = true;
                            foreach (Ship fship in empire.GetFleetsDict()[task.WhichFleet].Ships)
                            {
                                fship.AI.OrderQueue.Clear();
                                fship.DoEscort(ship);
                            }
                            break;
                        }
                        if (!foundhome)
                        {
                            foreach (Ship ship in this.empire.GetFleetsDict()[task.WhichFleet].Ships)
                            {
                                ship.AI.OrderQueue.Clear();
                                ship.AI.State = AIState.AwaitingOrders;
                            }
                        }
                    }
                    task.EndTaskWithMove();
                }, false, false, false);
            }
        }

        private void FightBrutalWar(KeyValuePair<Empire, Relationship> r)
        {
            Array<Planet> InvasionTargets = new Array<Planet>();
            foreach (Planet p in this.empire.GetPlanets())
            {
                foreach (Planet toCheck in p.system.PlanetList)
                {
                    if (toCheck.Owner == null || toCheck.Owner == this.empire || !toCheck.Owner.isFaction &&
                        !this.empire.GetRelations(toCheck.Owner).AtWar)
                    {
                        continue;
                    }
                    InvasionTargets.Add(toCheck);
                }
            }
            if (InvasionTargets.Count > 0)
            {
                Planet target = InvasionTargets[0];
                bool OK = true;

                using (TaskList.AcquireReadLock())
                {
                    foreach (MilitaryTask task in this.TaskList)
                    {
                        if (task.GetTargetPlanet() != target)
                        {
                            continue;
                        }
                        OK = false;
                        break;
                    }
                }
                if (OK)
                {
                    MilitaryTask InvadeTask = new MilitaryTask(target, this.empire);
                    //lock (GlobalStats.TaskLocker)
                    {
                        this.TaskList.Add(InvadeTask);
                    }
                }
            }
            Array<Planet> PlanetsWeAreInvading = new Array<Planet>();
            //lock (GlobalStats.TaskLocker)
            {
                this.TaskList.ForEach(task => //foreach (MilitaryTask task in this.TaskList)
                {
                    if (task.type != MilitaryTask.TaskType.AssaultPlanet || task.GetTargetPlanet().Owner == null ||
                        task.GetTargetPlanet().Owner != r.Key)
                    {
                        return;
                    }
                    PlanetsWeAreInvading.Add(task.GetTargetPlanet());
                }, false, false, false);
            }
            if (PlanetsWeAreInvading.Count < 3 && this.empire.GetPlanets().Count > 0)
            {
                Vector2 vector2 = this.FindAveragePosition(this.empire);
                this.FindAveragePosition(r.Key);
                IOrderedEnumerable<Planet> sortedList =
                    from planet in r.Key.GetPlanets()
                    orderby Vector2.Distance(vector2, planet.Position)
                    select planet;
                foreach (Planet p in sortedList)
                {
                    if (PlanetsWeAreInvading.Contains(p))
                    {
                        continue;
                    }
                    if (PlanetsWeAreInvading.Count >= 3)
                    {
                        break;
                    }
                    PlanetsWeAreInvading.Add(p);
                    MilitaryTask invade = new MilitaryTask(p, this.empire);
                    //lock (GlobalStats.TaskLocker)
                    {
                        this.TaskList.Add(invade);
                    }
                }
            }
        }

        private void FightDefaultWar(KeyValuePair<Empire, Relationship> r)
        {
            float warWeight = 1 + this.empire.getResStrat().ExpansionPriority +
                              this.empire.getResStrat().MilitaryPriority;
            foreach (MilitaryTask item_0 in (Array<MilitaryTask>) this.TaskList)
            {
                if (item_0.type == MilitaryTask.TaskType.AssaultPlanet)
                {
                    warWeight--;
                }
                if (warWeight < 0)
                    return;
            }
            Array<SolarSystem> s;
            SystemCommander scom;
            switch (r.Value.ActiveWar.WarType)
            {
                case WarType.BorderConflict:
                    Array<Planet> list1 = new Array<Planet>();
                    IOrderedEnumerable<Planet> orderedEnumerable1 = Enumerable.OrderBy(r.Key.GetPlanets(),
                        (Func<Planet, float>) (planet => this.GetDistanceFromOurAO(planet) / 150000 +
                                                         (r.Key.GetGSAI()
                                                             .DefensiveCoordinator.DefenseDict
                                                             .TryGetValue(planet.ParentSystem, out scom)
                                                             ? scom.RankImportance
                                                             : 0)));
                    int x = (int) UniverseData.UniverseWidth;
                    s = new Array<SolarSystem>();

                    for (int index = 0; index < Enumerable.Count(orderedEnumerable1); ++index)
                    {
                        Planet p = Enumerable.ElementAt(orderedEnumerable1, index);
                        if (s.Count > warWeight)
                            break;

                        if (!s.Contains(p.ParentSystem))
                        {
                            s.Add(p.ParentSystem);
                        }
                        //if(s.Count >2)
                        //    break;
                        list1.Add(p);
                    }
                    foreach (Planet planet in list1)
                    {
                        bool canAddTask = true;

                        using (TaskList.AcquireReadLock())
                        {
                            foreach (MilitaryTask task in TaskList)
                            {
                                if (task.GetTargetPlanet() == planet &&
                                    task.type == MilitaryTask.TaskType.AssaultPlanet)
                                {
                                    canAddTask = false;
                                    break;
                                }
                            }
                        }
                        if (canAddTask)
                        {
                            TaskList.Add(new MilitaryTask(planet, empire));
                        }
                    }
                    break;
                case WarType.ImperialistWar:
                    Array<Planet> list2 = new Array<Planet>();
                    IOrderedEnumerable<Planet> orderedEnumerable2 = Enumerable.OrderBy<Planet, float>(
                        (IEnumerable<Planet>) r.Key.GetPlanets(),
                        (Func<Planet, float>) (planet => this.GetDistanceFromOurAO(planet) / 150000 +
                                                         (r.Key.GetGSAI()
                                                             .DefensiveCoordinator.DefenseDict
                                                             .TryGetValue(planet.ParentSystem, out scom)
                                                             ? scom.RankImportance
                                                             : 0)));
                    s = new Array<SolarSystem>();
                    for (int index = 0;
                        index < Enumerable.Count<Planet>((IEnumerable<Planet>) orderedEnumerable2);
                        ++index)
                    {
                        Planet p = Enumerable.ElementAt<Planet>((IEnumerable<Planet>) orderedEnumerable2, index);
                        if (s.Count > warWeight)
                            break;

                        if (!s.Contains(p.ParentSystem))
                        {
                            s.Add(p.ParentSystem);
                        }
                        //if (s.Count > 2)
                        //    break;
                        list2.Add(p);
                    }
                    foreach (Planet planet in list2)
                    {
                        bool flag = true;
                        bool claim = false;
                        bool claimPressent = false;
                        if (!s.Contains(planet.ParentSystem))
                            continue;
                        using (TaskList.AcquireReadLock())
                        {
                            foreach (MilitaryTask item_1 in (Array<MilitaryTask>) this.TaskList)
                            {
                                if (item_1.GetTargetPlanet() == planet)
                                {
                                    if (item_1.type == MilitaryTask.TaskType.AssaultPlanet)
                                        flag = false;
                                    if (item_1.type == MilitaryTask.TaskType.DefendClaim)
                                    {
                                        claim = true;
                                        if (item_1.Step == 2)
                                            claimPressent = true;
                                    }
                                }
                            }
                        }
                        if (flag && claimPressent)
                        {
                            TaskList.Add(new MilitaryTask(planet, this.empire));
                        }
                        if (!claim)
                        {
                            MilitaryTask task = new MilitaryTask()
                            {
                                AO = planet.Position
                            };
                            task.SetEmpire(this.empire);
                            task.AORadius = 75000f;
                            task.SetTargetPlanet(planet);
                            task.TargetPlanetGuid = planet.guid;
                            task.type = MilitaryTask.TaskType.DefendClaim;
                            TaskList.Add(task);
                        }
                    }
                    break;
            }
        }

        public void GetWarDeclaredOnUs(Empire warDeclarant, WarType wt)
        {
            var relations = empire.GetRelations(warDeclarant);
            relations.AtWar = true;
            relations.FedQuest = null;
            relations.Posture = Posture.Hostile;
            relations.ActiveWar = new War(empire, warDeclarant, Empire.Universe.StarDate)
            {
                WarType = wt
            };
            if (Empire.Universe.PlayerEmpire != empire)
            {
                if (empire.data.DiplomaticPersonality.Name == "Pacifist")
                {
                    relations.ActiveWar.WarType = relations.ActiveWar.StartingNumContestedSystems <= 0
                        ? WarType.DefensiveWar
                        : WarType.BorderConflict;
                }
            }
            if (relations.Trust > 0f)
                relations.Trust = 0f;
            relations.Treaty_Alliance = false;
            relations.Treaty_NAPact = false;
            relations.Treaty_OpenBorders = false;
            relations.Treaty_Trade = false;
            relations.Treaty_Peace = false;
        }

        public void OfferPeace(KeyValuePair<Empire, Relationship> relationship, string whichPeace)
        {
            Offer offerPeace = new Offer()
            {
                PeaceTreaty = true,
                AcceptDL = "OFFERPEACE_ACCEPTED",
                RejectDL = "OFFERPEACE_REJECTED"
            };
            Relationship value = relationship.Value;
            offerPeace.ValueToModify = new Ref<bool>(() => false, x => value.SetImperialistWar());
            string dialogue = whichPeace;
            if (relationship.Key != Empire.Universe.PlayerEmpire)
            {
                Offer ourOffer = new Offer {PeaceTreaty = true};
                relationship.Key.GetGSAI().AnalyzeOffer(ourOffer, offerPeace, empire, Offer.Attitude.Respectful);
                return;
            }
            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire,
                Empire.Universe.PlayerEmpire, dialogue, new Offer(), offerPeace));
        }

        private void RunWarPlanner()
        {
            float warWeight = 1 + this.empire.getResStrat().ExpansionPriority +
                              this.empire.getResStrat().MilitaryPriority;

            foreach (KeyValuePair<Empire, Relationship> r in this.empire.AllRelations.OrderByDescending(anger =>
                {
                    float angerMod = Vector2.Distance(anger.Key.GetWeightedCenter(), this.empire.GetWeightedCenter());
                    angerMod = (Empire.Universe.Size.X - angerMod) / UniverseData.UniverseWidth;
                    if (anger.Value.AtWar)
                        angerMod *= 100;
                    return anger.Value.TotalAnger * angerMod;
                }
            ))
            {
                if (warWeight > 0)

                    if (r.Key.isFaction)
                    {
                        r.Value.AtWar = false;
                    }
                    else
                    {
                        warWeight--;
                        SystemCommander scom;
                        if (r.Value.PreparingForWar)
                        {
                            Array<SolarSystem> s;
                            switch (r.Value.PreparingForWarType)
                            {
                                case WarType.BorderConflict:
                                    Array<Planet> list1 = new Array<Planet>();
                                    s = new Array<SolarSystem>();

                                    IOrderedEnumerable<Planet> orderedEnumerable1 = Enumerable.OrderBy<Planet, float>(
                                        (IEnumerable<Planet>) r.Key.GetPlanets(),
                                        (Func<Planet, float>) (planet => this.GetDistanceFromOurAO(planet) / 150000 +
                                                                         (r.Key.GetGSAI()
                                                                             .DefensiveCoordinator.DefenseDict
                                                                             .TryGetValue(planet.ParentSystem, out scom)
                                                                             ? scom.RankImportance
                                                                             : 0)));
                                    for (int index = 0;
                                        index < Enumerable.Count<Planet>((IEnumerable<Planet>) orderedEnumerable1);
                                        ++index)
                                    {
                                        Planet p =
                                            Enumerable.ElementAt<Planet>((IEnumerable<Planet>) orderedEnumerable1,
                                                index);
                                        if (s.Count > warWeight)
                                            break;

                                        if (!s.Contains(p.ParentSystem))
                                        {
                                            s.Add(p.ParentSystem);
                                        }

                                        list1.Add(p);

                                        //list1.Add(Enumerable.ElementAt<Planet>((IEnumerable<Planet>)orderedEnumerable1, index));
                                        //if (index == 2)
                                        //    break;
                                    }
                                    foreach (Planet planet in list1)
                                    {
                                        bool assault = true;
                                        bool claim = false;
                                        bool claimPresent = false;
                                        //this.TaskList.thisLock.EnterReadLock();
                                        {
                                            //foreach (MilitaryTask item_0 in (Array<MilitaryTask>)this.TaskList)
                                            this.TaskList.ForEach(item_0 =>
                                            {
                                                //if (!assault)
                                                //    return;
                                                if (item_0.GetTargetPlanet() == planet &&
                                                    item_0.type == MilitaryTask.TaskType.AssaultPlanet)
                                                {
                                                    assault = false;
                                                }
                                                if (item_0.GetTargetPlanet() == planet &&
                                                    item_0.type == MilitaryTask.TaskType.DefendClaim)
                                                {
                                                    if (item_0.Step == 2)
                                                        claimPresent = true;
                                                    //if (s.Contains(current.ParentSystem))
                                                    //    s.Remove(current.ParentSystem);
                                                    claim = true;
                                                }
                                            }, false, false, false);
                                        }
                                        //this.TaskList.thisLock.ExitReadLock();
                                        if (assault && claimPresent)
                                        {
                                            TaskList.Add(new MilitaryTask(planet, empire));
                                        }
                                        if (!claim)
                                        {
                                            MilitaryTask task = new MilitaryTask()
                                            {
                                                AO = planet.Position
                                            };
                                            task.SetEmpire(this.empire);
                                            task.AORadius = 75000f;
                                            task.SetTargetPlanet(planet);
                                            task.TargetPlanetGuid = planet.guid;
                                            task.type = MilitaryTask.TaskType.DefendClaim;
                                            TaskList.Add(task);
                                        }
                                    }
                                    break;
                                case WarType.ImperialistWar:
                                    Array<Planet> list2 = new Array<Planet>();
                                    s = new Array<SolarSystem>();
                                    IOrderedEnumerable<Planet> orderedEnumerable2 = r.Key.GetPlanets()
                                        .OrderBy(
                                            (planet => GetDistanceFromOurAO(planet) / 150000 +
                                                       (r.Key.GetGSAI()
                                                           .DefensiveCoordinator.DefenseDict
                                                           .TryGetValue(planet.ParentSystem, out scom)
                                                           ? scom.RankImportance
                                                           : 0)));
                                    for (int index = 0; index < Enumerable.Count(orderedEnumerable2); ++index)
                                    {
                                        Planet p = Enumerable.ElementAt(orderedEnumerable2, index);
                                        if (s.Count > warWeight)
                                            break;

                                        if (!s.Contains(p.ParentSystem))
                                        {
                                            s.Add(p.ParentSystem);
                                        }
                                        //if (s.Count > 2)
                                        //    break;
                                        list2.Add(p);
                                    }
                                    foreach (Planet planet in list2)
                                    {
                                        bool flag = true;
                                        bool claim = false;
                                        bool claimPresent = false;
                                        //this.TaskList.thisLock.EnterReadLock();
                                        {
                                            // foreach (MilitaryTask item_1 in (Array<MilitaryTask>)this.TaskList)
                                            this.TaskList.ForEach(item_1 =>
                                            {
                                                if (!flag && claim)
                                                    return;
                                                if (item_1.GetTargetPlanet() == planet &&
                                                    item_1.type == MilitaryTask.TaskType.AssaultPlanet)
                                                {
                                                    flag = false;
                                                }
                                                if (item_1.GetTargetPlanet() == planet &&
                                                    item_1.type == MilitaryTask.TaskType.DefendClaim)
                                                {
                                                    if (item_1.Step == 2)
                                                        claimPresent = true;

                                                    claim = true;
                                                }
                                            }, false, false, false);
                                        }
                                        //  this.TaskList.thisLock.ExitReadLock();
                                        if (flag && claimPresent)
                                        {
                                            TaskList.Add(new MilitaryTask(planet, empire));
                                        }
                                        if (!claim)
                                        {
                                            // @todo This is repeated everywhere. Might cut down a lot of code by creating a function

                                            //public MilitaryTask(Vector2 location, float radius, Array<Goal> GoalsToHold, Empire Owner)
                                            MilitaryTask task = new MilitaryTask()
                                            {
                                                AO = planet.Position
                                            };
                                            task.SetEmpire(this.empire);
                                            task.AORadius = 75000f;
                                            task.SetTargetPlanet(planet);
                                            task.TargetPlanetGuid = planet.guid;
                                            task.type = MilitaryTask.TaskType.DefendClaim;
                                            task.EnemyStrength = 0;
                                            //lock (GlobalStats.TaskLocker)
                                            {
                                                TaskList.Add(task);
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                        if (r.Value.AtWar)
                        {
                            // int num = (int)this.empire.data.difficulty;
                            this.FightDefaultWar(r);
                        }
                    }
                //warWeight--;
            }
        }
    }
}