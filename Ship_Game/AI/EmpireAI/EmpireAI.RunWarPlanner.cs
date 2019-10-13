using Microsoft.Xna.Framework;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
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
                        ally.GetEmpireAI().DeclareWarOnViaCall(enemy, WarType.ImperialistWar);
                        return;
                    }

                    Relationship ourRelationToAlly = OwnerEmpire.GetRelations(ally);

                    float anger = 30f;
                    if (OwnerEmpire.data.DiplomaticPersonality != null &&
                        OwnerEmpire.data.DiplomaticPersonality.Name == "Honorable")
                    {
                        anger = 60f;
                        offer.RejectDL  = "HelpUS_War_No_BreakAlliance";
                        ally.GetRelations(OwnerEmpire).Treaty_Alliance = false;
                        ourRelationToAlly.Treaty_Alliance    = false;
                        ourRelationToAlly.Treaty_OpenBorders = false;
                        ourRelationToAlly.Treaty_NAPact      = false;
                    }

                    ourRelationToAlly.Trust -= anger;
                    ourRelationToAlly.Anger_DiplomaticConflict += anger;
                })
            };

            if (ally == Empire.Universe.PlayerEmpire)
            {
                DiplomacyScreen.Show(OwnerEmpire, dialogue, ourOffer, offer, enemy);
            }
        }

        public void DeclareWarFromEvent(Empire them, WarType wt)
        {
            Relationship ourRelationToThem = OwnerEmpire.GetRelations(them);
            ourRelationToThem.AtWar     = true;
            ourRelationToThem.Posture   = Posture.Hostile;
            ourRelationToThem.ActiveWar = new War(OwnerEmpire, them, Empire.Universe.StarDate)
            {
                WarType = wt
            };
            if (ourRelationToThem.Trust > 0f)
            {
                ourRelationToThem.Trust = 0f;
            }
            ourRelationToThem.Treaty_OpenBorders = false;
            ourRelationToThem.Treaty_NAPact      = false;
            ourRelationToThem.Treaty_Trade       = false;
            ourRelationToThem.Treaty_Alliance    = false;
            ourRelationToThem.Treaty_Peace       = false;
            them.GetEmpireAI().GetWarDeclaredOnUs(OwnerEmpire, wt);
        }

        public void DeclareWarOn(Empire them, WarType wt)
        {
            Relationship ourRelations = OwnerEmpire.GetRelations(them);
            Relationship theirRelationToUs = them.GetRelations(OwnerEmpire);
            ourRelations.PreparingForWar = false;
            if (OwnerEmpire.isFaction || OwnerEmpire.data.Defeated || (them.data.Defeated || them.isFaction))
                return;

            ourRelations.FedQuest = null;
            if (OwnerEmpire == Empire.Universe.PlayerEmpire && ourRelations.Treaty_NAPact)
            {
                ourRelations.Treaty_NAPact = false;
                foreach (KeyValuePair<Empire, Relationship> kv in OwnerEmpire.AllRelations)
                {
                    if (kv.Key != them)
                    {
                        Relationship otherRelationToUs = kv.Key.GetRelations(OwnerEmpire);
                        otherRelationToUs.Trust                    -= 50f;
                        otherRelationToUs.Anger_DiplomaticConflict += 20f;
                        otherRelationToUs.UpdateRelationship(kv.Key, OwnerEmpire);
                    }
                }
                theirRelationToUs.Trust                    -= 50f;
                theirRelationToUs.Anger_DiplomaticConflict += 50f;
                theirRelationToUs.UpdateRelationship(them, OwnerEmpire);
            }

            // AI is declaring war on player
            if (them == Empire.Universe.PlayerEmpire && !ourRelations.AtWar)
            {
                AIDeclaresWarOnPlayer(them, wt, ourRelations);
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

            ourRelations.AtWar   = true;
            ourRelations.Posture = Posture.Hostile;
            ourRelations.ActiveWar = new War(OwnerEmpire, them, Empire.Universe.StarDate) {WarType = wt};
            if (ourRelations.Trust > 0f)
                ourRelations.Trust          = 0.0f;
            ourRelations.Treaty_OpenBorders = false;
            ourRelations.Treaty_NAPact      = false;
            ourRelations.Treaty_Trade       = false;
            ourRelations.Treaty_Alliance    = false;
            ourRelations.Treaty_Peace       = false;
            them.GetEmpireAI().GetWarDeclaredOnUs(OwnerEmpire, wt);
        }

        void AIDeclaresWarOnPlayer(Empire player, WarType wt, Relationship ourRelations)
        {
            switch (wt)
            {
                case WarType.BorderConflict:
                    if (ourRelations.contestedSystemGuid != Guid.Empty)
                    {
                        DiplomacyScreen.Show(OwnerEmpire, player, "Declare War BC TarSys", ourRelations.GetContestedSystem());
                        break;
                    }
                    else
                    {
                        DiplomacyScreen.Show(OwnerEmpire, player, "Declare War BC", ourRelations.GetContestedSystem());
                        break;
                    }
                case WarType.ImperialistWar:
                    if (ourRelations.Treaty_NAPact)
                    {
                        DiplomacyScreen.Show(OwnerEmpire, player, "Declare War Imperialism Break NA");
                        foreach (var kv in OwnerEmpire.AllRelations)
                        {
                            if (kv.Key != player)
                            {
                                kv.Value.Trust -= 50f;
                                kv.Value.Anger_DiplomaticConflict += 20f;
                            }
                        }

                        break;
                    }
                    else
                    {
                        DiplomacyScreen.Show(OwnerEmpire, player, "Declare War Imperialism");
                        break;
                    }
                case WarType.DefensiveWar:
                    if (!ourRelations.Treaty_NAPact)
                    {
                        DiplomacyScreen.Show(OwnerEmpire, player, "Declare War Defense");
                        ourRelations.Anger_DiplomaticConflict += 25f;
                        ourRelations.Trust -= 25f;
                        break;
                    }
                    else if (ourRelations.Treaty_NAPact)
                    {
                        DiplomacyScreen.Show(OwnerEmpire, player, "Declare War Defense BrokenNA");
                        ourRelations.Treaty_NAPact = false;
                        foreach (var kv in OwnerEmpire.AllRelations)
                        {
                            if (kv.Key != player)
                            {
                                kv.Value.Trust -= 50f;
                                kv.Value.Anger_DiplomaticConflict += 20f;
                            }
                        }

                        ourRelations.Trust -= 50f;
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
                        Relationship r = OwnerEmpire.GetRelations(them);
                        if (r.contestedSystemGuid == Guid.Empty)
                        {
                            DiplomacyScreen.Show(OwnerEmpire, them, "Declare War BC");
                        }
                        else
                        {
                            DiplomacyScreen.Show(OwnerEmpire, them, "Declare War BC Tarsys", r.GetContestedSystem());
                        }
                        break;
                    }
                    case WarType.ImperialistWar:
                    {
                        if (OwnerEmpire.GetRelations(them).Treaty_NAPact)
                        {
                            DiplomacyScreen.Show(OwnerEmpire, them, "Declare War Imperialism Break NA");
                        }
                        else
                        {
                            DiplomacyScreen.Show(OwnerEmpire, them, "Declare War Imperialism");
                        }
                        break;
                    }
                    case WarType.DefensiveWar:
                    {
                        if (OwnerEmpire.GetRelations(them).Treaty_NAPact)
                        {
                            if (OwnerEmpire.GetRelations(them).Treaty_NAPact)
                            {
                                DiplomacyScreen.Show(OwnerEmpire, them, "Declare War Defense BrokenNA");
                                OwnerEmpire.GetRelations(them).Treaty_NAPact = false;
                                OwnerEmpire.GetRelations(them).Trust -= 50f;
                                OwnerEmpire.GetRelations(them).Anger_DiplomaticConflict += 50f;
                            }
                        }
                        else
                        {
                            DiplomacyScreen.Show(OwnerEmpire, them, "Declare War Defense");
                            OwnerEmpire.GetRelations(them).Anger_DiplomaticConflict += 25f;
                            OwnerEmpire.GetRelations(them).Trust -= 25f;
                        }
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
            them.GetEmpireAI().GetWarDeclaredOnUs(OwnerEmpire, wt);
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
                            if (!ship.IsPlatformOrStation)
                            {
                                continue;
                            }
                            foundhome = true;
                            foreach (Ship fship in OwnerEmpire.GetFleetsDict()[task.WhichFleet].Ships)
                            {
                                fship.AI.ClearOrders();
                                fship.DoEscort(ship);
                            }
                            break;
                        }
                        if (!foundhome)
                        {
                            foreach (Ship ship in OwnerEmpire.GetFleetsDict()[task.WhichFleet].Ships)
                            {
                                ship.AI.ClearOrders();
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

        private void FightDefaultWar(KeyValuePair<Empire, Relationship> r, int warWeight)
        {
            foreach (MilitaryTask militaryTask in TaskList)
            {
                if (militaryTask.type == MilitaryTask.TaskType.AssaultPlanet)
                {
                    warWeight--;
                }
                if (warWeight < 0)
                    return;
            }

            WarTargets(r, warWeight);
        }

        private void WarTargets(KeyValuePair<Empire, Relationship> r, int warWeight)
        {
            var warType = r.Value.ActiveWar?.WarType ?? r.Value.PreparingForWarType;
            switch (warType)
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
            var ourOffer = new Offer { PeaceTreaty = true };
            if (relationship.Key == Empire.Universe.PlayerEmpire)
            {
                DiplomacyScreen.Show(OwnerEmpire, dialogue, ourOffer, offerPeace);
            }
            else
            {
                relationship.Key.GetEmpireAI()
                    .AnalyzeOffer(ourOffer, offerPeace, OwnerEmpire, Offer.Attitude.Respectful);
            }
        }

        private IEnumerable<KeyValuePair<Empire, Relationship>> EmpireAttackWeights()
        {
            return OwnerEmpire.AllRelations.OrderByDescending(anger =>
            {
                if (!anger.Value.Known) return 0;
                float angerMod = anger.Key.GetWeightedCenter().Distance(OwnerEmpire.GetWeightedCenter());
                angerMod = (Empire.Universe.UniverseSize * 2 - angerMod) / (Empire.Universe.UniverseSize * 2);
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
            }).ToArray(); 
        }

        private void RunWarPlanner()
        {
            if (OwnerEmpire.isPlayer)
                return;

            int warWeight = (int)Math.Ceiling(1 +
                              5 * (OwnerEmpire.ResearchStrategy.MilitaryRatio 
                                   + OwnerEmpire.ResearchStrategy.ExpansionRatio));
            var weightedTargets = EmpireAttackWeights();
            foreach (var kv in weightedTargets)
            {
                if (warWeight <= 0) break;
                if (!kv.Value.Known) continue;
                if (kv.Key.data.Defeated) continue;
                if (!OwnerEmpire.IsEmpireAttackable(kv.Key)) continue;
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
                    FightDefaultWar(kv, warWeight);
                    continue;
                }

                if (!kv.Value.PreparingForWar) continue;
                WarTargets(kv, warWeight);
                return;
            }
        }

        private void AssignTargets(KeyValuePair<Empire, Relationship> kv, float warWeight, WarType warType = WarType.BorderConflict)
        {
            Array<SolarSystem> solarSystems = new Array<SolarSystem>();
            Array<Planet> planets = new Array<Planet>();
            if (warWeight <= 0) return;
            Planet[] planetTargetPriority = PlanetTargetPriority(kv.Key, warType);

            // so what this is trying to do here is take the best system and attack
            // all planets in that system to try and completely flip the system. 

            for (int index = 0; index < planetTargetPriority.Length; ++index)
            {
                Planet p = planetTargetPriority[index];

                if (!solarSystems.Contains(p.ParentSystem))
                    solarSystems.Add(p.ParentSystem);
                if (warWeight-- <= 0) break;
            }

            for (int i = 0; i < solarSystems.Count; i++)
            {
                var system = solarSystems[i];
                for (int x = 0; x < planetTargetPriority.Length; x++)
                {
                    var planet = planetTargetPriority[x];
                    if (planet.ParentSystem == system)
                        planets.Add(planet);
                }
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

        private Planet[] PlanetTargetPriority(Empire empire, WarType warType)
        {
            switch (warType)
            {
                case WarType.BorderConflict:
                {
                        var planets = new Array<Planet>();
                    var targetSystems = OwnerEmpire.GetBorderSystems(empire);
                        foreach (var s in targetSystems)
                            foreach (var p in s.PlanetList)
                            {
                                if (p.Owner == empire)
                                    planets.Add(p);
                            }
                        return planets.ToArray();
                }
            }
            
            return empire.GetPlanets().OrderBy(insystem => !insystem.ParentSystem.OwnerList.Contains(OwnerEmpire))
                .ThenBy(planet => GetDistanceFromOurAO(planet) / 150000f)
                .ThenByDescending(planet => empire.GetEmpireAI()
                                      .DefensiveCoordinator.DefenseDict
                                      .TryGetValue(planet.ParentSystem, out SystemCommander scom)
                                      ? scom.PlanetTracker[planet].Value
                                      : 0).ToArray();
        }
    }
}