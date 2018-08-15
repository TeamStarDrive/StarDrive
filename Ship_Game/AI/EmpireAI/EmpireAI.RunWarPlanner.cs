using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI {
    public sealed partial class EmpireAI
    {
        public void CallAllyToWar(Empire ally, Empire enemy)
        {
            var offer = new Offer
            {
                AcceptDL = "HelpUS_War_Yes",
                RejectDL = "HelpUS_War_No"
            };
            const string dialogue = "HelpUS_War";
            var ourOffer = new Offer
            {
                ValueToModify = new Ref<bool>(() => ally.GetRelations(enemy).AtWar, x =>
                {
                    if (x)
                    {
                        ally.GetGSAI().DeclareWarOnViaCall(enemy, WarType.ImperialistWar);
                        return;
                    }
                    float amount = 30f;
                    if (OwnerEmpire.data.DiplomaticPersonality != null &&
                        OwnerEmpire.data.DiplomaticPersonality.Name == "Honorable")
                    {
                        amount                                            = 60f;
                        offer.RejectDL                                    = "HelpUS_War_No_BreakAlliance";
                        OwnerEmpire.GetRelations(ally).Treaty_Alliance    = false;
                        ally.GetRelations(OwnerEmpire).Treaty_Alliance    = false;
                        OwnerEmpire.GetRelations(ally).Treaty_OpenBorders = false;
                        OwnerEmpire.GetRelations(ally).Treaty_NAPact      = false;
                    }
                    Relationship item                                = OwnerEmpire.GetRelations(ally);
                    item.Trust                                       = item.Trust - amount;
                    Relationship angerDiplomaticConflict             = OwnerEmpire.GetRelations(ally);
                    angerDiplomaticConflict.Anger_DiplomaticConflict =
                        angerDiplomaticConflict.Anger_DiplomaticConflict + amount;
                })
            };
            if (ally == Empire.Universe.PlayerEmpire)
            {
                Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                    Empire.Universe.PlayerEmpire, dialogue, ourOffer, offer, enemy));
            }
        }

        public void DeclareWarFromEvent(Empire them, WarType wt)
        {
            OwnerEmpire.GetRelations(them).AtWar     = true;
            OwnerEmpire.GetRelations(them).Posture   = Posture.Hostile;
            OwnerEmpire.GetRelations(them).ActiveWar = new War(OwnerEmpire, them, Empire.Universe.StarDate)
            {
                WarType = wt
            };
            if (OwnerEmpire.GetRelations(them).Trust > 0f)
            {
                OwnerEmpire.GetRelations(them).Trust = 0f;
            }
            OwnerEmpire.GetRelations(them).Treaty_OpenBorders = false;
            OwnerEmpire.GetRelations(them).Treaty_NAPact      = false;
            OwnerEmpire.GetRelations(them).Treaty_Trade       = false;
            OwnerEmpire.GetRelations(them).Treaty_Alliance    = false;
            OwnerEmpire.GetRelations(them).Treaty_Peace       = false;
            them.GetGSAI().GetWarDeclaredOnUs(OwnerEmpire, wt);
        }

        public void DeclareWarOn(Empire them, WarType wt)
        {
            Relationship ourRelations = OwnerEmpire.GetRelations(them);
            Relationship theirRelations = them.GetRelations(OwnerEmpire );
            ourRelations.PreparingForWar = false;
            if (OwnerEmpire.isFaction || OwnerEmpire.data.Defeated || (them.data.Defeated || them.isFaction))
                return;
            ourRelations.FedQuest = null;
            if (OwnerEmpire == Empire.Universe.PlayerEmpire && ourRelations.Treaty_NAPact)
            {
                ourRelations.Treaty_NAPact = false;
                foreach (var kv in OwnerEmpire.AllRelations)
                {
                    if (kv.Key != them)
                    {
                        kv.Key.GetRelations(OwnerEmpire).Trust                    -= 50f;
                        kv.Key.GetRelations(OwnerEmpire).Anger_DiplomaticConflict += 20f;
                        kv.Key.GetRelations(OwnerEmpire).UpdateRelationship(kv.Key, OwnerEmpire);
                    }
                }
                theirRelations.Trust                    -= 50f;
                theirRelations.Anger_DiplomaticConflict += 50f;
                theirRelations.UpdateRelationship(them, OwnerEmpire);
            }
            if (them == Empire.Universe.PlayerEmpire && !ourRelations.AtWar)
            {
                switch (wt)
                {
                    case WarType.BorderConflict:
                        if (ourRelations.contestedSystemGuid != Guid.Empty)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                                "Declare War BC TarSys", ourRelations.GetContestedSystem()));
                            break;
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                                "Declare War BC"));
                            break;
                        }
                    case WarType.ImperialistWar:
                        if (ourRelations.Treaty_NAPact)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                                "Declare War Imperialism Break NA"));
                            using (var enumerator = OwnerEmpire.AllRelations.GetEnumerator())
                            {
                                while (enumerator.MoveNext())
                                {
                                    var kv = enumerator.Current;
                                    if (kv.Key != them)
                                    {
                                        kv.Value.Trust                    -= 50f;
                                        kv.Value.Anger_DiplomaticConflict += 20f;
                                    }
                                }
                                break;
                            }
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                                "Declare War Imperialism"));
                            break;
                        }
                    case WarType.DefensiveWar:
                        if (!ourRelations.Treaty_NAPact)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                                "Declare War Defense"));
                            ourRelations.Anger_DiplomaticConflict += 25f;
                            ourRelations.Trust                    -= 25f;
                            break;
                        }
                        else if (ourRelations.Treaty_NAPact)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                                "Declare War Defense BrokenNA"));
                            ourRelations.Treaty_NAPact = false;
                            foreach (var kv in OwnerEmpire.AllRelations)
                            {
                                if (kv.Key != them)
                                {
                                    kv.Value.Trust                    -= 50f;
                                    kv.Value.Anger_DiplomaticConflict += 20f;
                                }
                            }
                            ourRelations.Trust                    -= 50f;
                            ourRelations.Anger_DiplomaticConflict += 50f;
                            break;
                        }
                        else
                            break;
                    case WarType.GenocidalWar:
                        break;
                    case WarType.SkirmishWar:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(wt), wt, null);
                }
            }
            if (them == Empire.Universe.PlayerEmpire || OwnerEmpire == Empire.Universe.PlayerEmpire)
                Empire.Universe.NotificationManager.AddWarDeclaredNotification(OwnerEmpire, them);
            else if (Empire.Universe.PlayerEmpire.GetRelations(them).Known &&
                     Empire.Universe.PlayerEmpire.GetRelations(OwnerEmpire).Known)
                Empire.Universe.NotificationManager.AddWarDeclaredNotification(OwnerEmpire, them);
            ourRelations.AtWar             = true;
            ourRelations.Posture           = Posture.Hostile;
            ourRelations.ActiveWar = new War(OwnerEmpire, them, Empire.Universe.StarDate) {WarType = wt};
            if (ourRelations.Trust > 0f)
                ourRelations.Trust          = 0.0f;
            ourRelations.Treaty_OpenBorders = false;
            ourRelations.Treaty_NAPact      = false;
            ourRelations.Treaty_Trade       = false;
            ourRelations.Treaty_Alliance    = false;
            ourRelations.Treaty_Peace       = false;
            them.GetGSAI().GetWarDeclaredOnUs(OwnerEmpire, wt);
        }

        public void DeclareWarOnViaCall(Empire them, WarType wt)
        {
            OwnerEmpire.GetRelations(them).PreparingForWar = false;
            if (OwnerEmpire.isFaction || OwnerEmpire.data.Defeated || them.data.Defeated || them.isFaction)
            {
                return;
            }
            OwnerEmpire.GetRelations(them).FedQuest = null;
            if (OwnerEmpire == Empire.Universe.PlayerEmpire && OwnerEmpire.GetRelations(them).Treaty_NAPact)
            {
                OwnerEmpire.GetRelations(them).Treaty_NAPact     = false;
                Relationship item                                = them.GetRelations(OwnerEmpire);
                item.Trust                                       = item.Trust - 50f;
                Relationship angerDiplomaticConflict             = them.GetRelations(OwnerEmpire);
                angerDiplomaticConflict.Anger_DiplomaticConflict =
                    angerDiplomaticConflict.Anger_DiplomaticConflict + 50f;
                them.GetRelations(OwnerEmpire).UpdateRelationship(them, OwnerEmpire);
            }
            if (them == Empire.Universe.PlayerEmpire && !OwnerEmpire.GetRelations(them).AtWar)
            {
                switch (wt)
                {
                    case WarType.BorderConflict:
                    {
                        if (OwnerEmpire.GetRelations(them).contestedSystemGuid == Guid.Empty)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                                "Declare War BC"));
                            break;
                        }
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                            "Declare War BC Tarsys", OwnerEmpire.GetRelations(them).GetContestedSystem()));
                        break;
                    }
                    case WarType.ImperialistWar:
                    {
                        if (!OwnerEmpire.GetRelations(them).Treaty_NAPact)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                                "Declare War Imperialism"));
                            break;
                        }
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                            "Declare War Imperialism Break NA"));
                        break;
                    }
                    case WarType.DefensiveWar:
                    {
                        if (OwnerEmpire.GetRelations(them).Treaty_NAPact)
                        {
                            if (!OwnerEmpire.GetRelations(them).Treaty_NAPact)
                            {
                                break;
                            }
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                                "Declare War Defense BrokenNA"));
                            OwnerEmpire.GetRelations(them).Treaty_NAPact = false;
                            Relationship trust                           = OwnerEmpire.GetRelations(them);
                            trust.Trust                                  = trust.Trust - 50f;
                            Relationship relationship                    = OwnerEmpire.GetRelations(them);
                            relationship.Anger_DiplomaticConflict        = relationship.Anger_DiplomaticConflict + 50f;
                            break;
                        }

                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                            "Declare War Defense"));
                        Relationship item1             = OwnerEmpire.GetRelations(them);
                        item1.Anger_DiplomaticConflict = item1.Anger_DiplomaticConflict + 25f;
                        Relationship trust1            = OwnerEmpire.GetRelations(them);
                        trust1.Trust                   = trust1.Trust - 25f;
                        break;
                    }
                    case WarType.GenocidalWar:
                        break;
                    case WarType.SkirmishWar:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(wt), wt, null);
                }
            }
            if (them == Empire.Universe.PlayerEmpire || OwnerEmpire == Empire.Universe.PlayerEmpire)
            {
                Empire.Universe.NotificationManager.AddWarDeclaredNotification(OwnerEmpire, them);
            }
            else if (Empire.Universe.PlayerEmpire.GetRelations(them).Known &&
                     Empire.Universe.PlayerEmpire.GetRelations(OwnerEmpire).Known)
            {
                Empire.Universe.NotificationManager.AddWarDeclaredNotification(OwnerEmpire, them);
            }
            OwnerEmpire.GetRelations(them).AtWar     = true;
            OwnerEmpire.GetRelations(them).Posture   = Posture.Hostile;
            OwnerEmpire.GetRelations(them).ActiveWar = new War(OwnerEmpire, them, Empire.Universe.StarDate)
            {
                WarType = wt
            };
            if (OwnerEmpire.GetRelations(them).Trust > 0f)
            {
                OwnerEmpire.GetRelations(them).Trust = 0f;
            }
            OwnerEmpire.GetRelations(them).Treaty_OpenBorders = false;
            OwnerEmpire.GetRelations(them).Treaty_NAPact      = false;
            OwnerEmpire.GetRelations(them).Treaty_Trade       = false;
            OwnerEmpire.GetRelations(them).Treaty_Alliance    = false;
            OwnerEmpire.GetRelations(them).Treaty_Peace       = false;
            them.GetGSAI().GetWarDeclaredOnUs(OwnerEmpire, wt);
        }

        public void EndWarFromEvent(Empire them)
        {
            OwnerEmpire.GetRelations(them).AtWar = false;
            them.GetRelations(OwnerEmpire).AtWar = false;
            //lock (GlobalStats.TaskLocker)
            {
                TaskList.ForEach(task => //foreach (MilitaryTask task in TaskList)
                {
                    if (OwnerEmpire.GetFleetsDict().ContainsKey(task.WhichFleet) &&
                        OwnerEmpire.data.Traits.Name == "Corsairs")
                    {
                        bool foundhome = false;
                        foreach (Ship ship in OwnerEmpire.GetShips())
                        {
                            if (ship.shipData.Role != ShipData.RoleName.station &&
                                ship.shipData.Role != ShipData.RoleName.platform)
                            {
                                continue;
                            }
                            foundhome = true;
                            foreach (Ship fship in OwnerEmpire.GetFleetsDict()[task.WhichFleet].Ships)
                            {
                                fship.AI.OrderQueue.Clear();
                                fship.DoEscort(ship);
                            }
                            break;
                        }
                        if (!foundhome)
                        {
                            foreach (Ship ship in OwnerEmpire.GetFleetsDict()[task.WhichFleet].Ships)
                            {
                                ship.AI.OrderQueue.Clear();
                                ship.AI.State = AIState.AwaitingOrders;
                            }
                        }
                    }
                    task.EndTaskWithMove();
                }, false, false);
            }
        }

        // ReSharper disable once UnusedMember.Local 
        // Lets think about using this
        private void FightBrutalWar(KeyValuePair<Empire, Relationship> r)
        {
            var invasionTargets = new Array<Planet>();
            Vector2 ownerCenter = OwnerEmpire.GetWeightedCenter();
            foreach (Planet p in OwnerEmpire.GetPlanets())
            {
                foreach (Planet toCheck in p.ParentSystem.PlanetList)
                {
                    if (toCheck.Owner == null || toCheck.Owner == OwnerEmpire || !toCheck.Owner.isFaction &&
                        !OwnerEmpire.GetRelations(toCheck.Owner).AtWar)
                    {
                        continue;
                    }
                    invasionTargets.Add(toCheck);
                }
            }
            if (invasionTargets.Count > 0)
            {
               
                Planet target = invasionTargets.FindMin(distance=> distance.Center.SqDist(ownerCenter));
                bool ok = true;

                using (TaskList.AcquireReadLock())
                {
                    foreach (MilitaryTask task in TaskList)
                    {
                        if (task.TargetPlanet != target)
                        {
                            continue;
                        }
                        ok = false;
                        break;
                    }
                }
                if (ok)
                {
                    var invadeTask = new MilitaryTask(target, OwnerEmpire);
                    {
                        TaskList.Add(invadeTask);
                        //if (r.Key.isFaction) return;
                    }
                }
            }
            var planetsWeAreInvading = new Array<Planet>();
            {
                TaskList.ForEach(task =>
                {
                    if (task.type != MilitaryTask.TaskType.AssaultPlanet || task.TargetPlanet.Owner == null ||
                        task.TargetPlanet.Owner != r.Key)
                    {
                        return;
                    }
                    planetsWeAreInvading.Add(task.TargetPlanet);
                }, false, false);
            }
            if (planetsWeAreInvading.Count < 3 && OwnerEmpire.GetPlanets().Count > 0)
            {
                Vector2 vector2 = FindAveragePosition(OwnerEmpire);
                FindAveragePosition(r.Key);
                IOrderedEnumerable<Planet> sortedList =
                    from planet in r.Key.GetPlanets()
                    orderby Vector2.Distance(vector2, planet.Center)
                    select planet;
                foreach (Planet p in sortedList)
                {
                    if (planetsWeAreInvading.Contains(p))
                    {
                        continue;
                    }
                    if (planetsWeAreInvading.Count >= 3)
                    {
                        break;
                    }
                    planetsWeAreInvading.Add(p);
                    var invade = new MilitaryTask(p, OwnerEmpire);
                    {
                        TaskList.Add(invade);
                    }
                }
            }
        }

        private void FightDefaultWar(KeyValuePair<Empire, Relationship> r)
        {
            float warWeight = 1 + OwnerEmpire.getResStrat().ExpansionPriority +
                              OwnerEmpire.getResStrat().MilitaryPriority;
            foreach (MilitaryTask militaryTask in TaskList)
            {
                if (militaryTask.type == MilitaryTask.TaskType.AssaultPlanet)
                {
                    warWeight--;
                }
                if (warWeight < 0)
                    return;
            }
            switch (r.Value.ActiveWar.WarType)
            {
                case WarType.BorderConflict:
                    AssignTargets(r, warWeight);
                    break;
                case WarType.ImperialistWar:
                    AssignTargets(r, warWeight);
                    break;
                case WarType.GenocidalWar:
                    break;
                case WarType.DefensiveWar:
                    break;
                case WarType.SkirmishWar:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void GetWarDeclaredOnUs(Empire warDeclarant, WarType wt)
        {
            Relationship relations = OwnerEmpire.GetRelations(warDeclarant);
            relations.AtWar        = true;
            relations.FedQuest     = null;
            relations.Posture      = Posture.Hostile;
            relations.ActiveWar    = new War(OwnerEmpire, warDeclarant, Empire.Universe.StarDate)
            {
                WarType = wt
            };
            if (Empire.Universe.PlayerEmpire != OwnerEmpire)
            {
                if (OwnerEmpire.data.DiplomaticPersonality.Name == "Pacifist")
                {
                    relations.ActiveWar.WarType = relations.ActiveWar.StartingNumContestedSystems <= 0
                        ? WarType.DefensiveWar
                        : WarType.BorderConflict;
                }
            }
            if (relations.Trust > 0f)
                relations.Trust          = 0f;
            relations.Treaty_Alliance    = false;
            relations.Treaty_NAPact      = false;
            relations.Treaty_OpenBorders = false;
            relations.Treaty_Trade       = false;
            relations.Treaty_Peace       = false;
        }

        public void OfferPeace(KeyValuePair<Empire, Relationship> relationship, string whichPeace)
        {
            var offerPeace = new Offer
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
                var ourOffer = new Offer {PeaceTreaty = true};
                relationship.Key.GetGSAI().AnalyzeOffer(ourOffer, offerPeace, OwnerEmpire, Offer.Attitude.Respectful);
                return;
            }
            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                Empire.Universe.PlayerEmpire, dialogue, new Offer(), offerPeace));
        }

        private IEnumerable<KeyValuePair<Empire, Relationship>> EmpireAttackWeights()
        {
            return OwnerEmpire.AllRelations.OrderByDescending(anger =>
                {
                    if (!anger.Value.Known) return 0;
                    float angerMod = anger.Key.GetWeightedCenter().Distance(OwnerEmpire.GetWeightedCenter());
                    angerMod = (Empire.Universe.UniverseSize - angerMod) / UniverseData.UniverseWidth;
                    if (anger.Value.AtWar)
                        angerMod *= 100;                    
                    angerMod += anger.Key.GetPlanets().Any(p => IsInOurAOs(p.Center)) ? 1 : 0;
                    if (anger.Value.Treaty_Trade)
                        angerMod *= .5f;
                    if (anger.Value.Treaty_Alliance)
                        angerMod *= .5f;
                    foreach (var s in OwnerEmpire.GetOwnedSystems())
                    {
                        if (s.OwnerList.Contains(anger.Key))
                        {
                            angerMod *= 2;
                            break;
                        }
                    }
                    //switching to godSight. 
                    float killableMod = 1 + (int)OwnerEmpire.currentMilitaryStrength / (anger.Key.currentMilitaryStrength + 1);//    (ThreatMatrix.StrengthOfEmpire(anger.Key) +1);
                    return (anger.Value.TotalAnger + 1) * angerMod * killableMod;
                }
            ).ToArray(); 
            
        }

        private void RunWarPlanner()
        {
            if (OwnerEmpire.isPlayer)
                return;

            float warWeight = 1 +
                              OwnerEmpire.getResStrat().MilitaryPriority;
            var weightedTargets = EmpireAttackWeights();
            foreach (var kv in weightedTargets)
            {
                if (!kv.Value.Known) continue;
                if (kv.Key.data.Defeated) continue;
                if (!OwnerEmpire.IsEmpireAttackable(kv.Key)) continue;                
                if (!(warWeight > 0)) continue;
                if (kv.Key.isFaction)
                {
                    foreach (var planet in kv.Key.GetPlanets())
                    {
                        if (!planet.ParentSystem.OwnerList.Contains(OwnerEmpire) && !IsInOurAOs(planet.Center)) continue;
                        FightBrutalWar(kv);
                        //kv.Value.AtWar = false;
                        break;
                    }
                    continue;
                }
                warWeight--;
                if (kv.Value.AtWar)
                {
                    FightDefaultWar(kv);
                    return;
                }
                
                if (!kv.Value.PreparingForWar) continue;

                switch (kv.Value.PreparingForWarType)
                {
                    case WarType.BorderConflict:
                        AssignTargets(kv, warWeight);
                        break;
                    case WarType.ImperialistWar:
                        AssignTargets(kv, warWeight);                        
                        break;
                    case WarType.GenocidalWar:
                        break;
                    case WarType.DefensiveWar:
                        break;
                    case WarType.SkirmishWar:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                return;
            }
        }

        private void AssignTargets(KeyValuePair<Empire, Relationship> kv, float warWeight, WarType warType = WarType.BorderConflict)
        {
            Array<SolarSystem> solarSystems = new Array<SolarSystem>();
            Array<Planet> planets = new Array<Planet>();            
            Planet[] planetTargetPriority = PlanetTargetPriority(kv.Key);

            for (int index = 0; index < planetTargetPriority.Length; ++index)
            {
                Planet p =
                    planetTargetPriority[index];
                if (solarSystems.Count > warWeight)
                    break;

                if (!solarSystems.Contains(p.ParentSystem))
                {
                    solarSystems.Add(p.ParentSystem);
                }

                planets.Add(p);
            }

            foreach (Planet planet in planets)
            {
                bool assault = true;
                TaskList.ForEach(task =>
                {
                    if (task.TargetPlanet == planet &&
                        task.type == MilitaryTask.TaskType.AssaultPlanet)
                    {
                        assault = false;
                    }
                }, false, false);
                if (assault)
                {
                    var invasionTask = new MilitaryTask(planet, OwnerEmpire);                   
                    TaskList.Add(invasionTask);

                }
            }
        }

        private Planet[] PlanetTargetPriority(Empire empire)
        {
            return empire.GetPlanets().OrderBy(insystem => !insystem.ParentSystem.OwnerList.Contains(OwnerEmpire))
                .ThenBy(planet => GetDistanceFromOurAO(planet) / 150000f)
                .ThenByDescending(planet => empire.GetGSAI()
                                      .DefensiveCoordinator.DefenseDict
                                      .TryGetValue(planet.ParentSystem, out SystemCommander scom)
                                      ? scom.PlanetTracker[planet].Value
                                      : 0).ToArray();
        }
    }
}