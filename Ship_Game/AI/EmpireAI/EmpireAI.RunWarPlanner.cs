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
                ourRelations.Trust = 0.0f;
            ourRelations.Treaty_OpenBorders = false;
            ourRelations.Treaty_NAPact      = false;
            ourRelations.Treaty_Trade       = false;
            ourRelations.Treaty_Alliance    = false;
            ourRelations.Treaty_Peace       = false;
            them.GetEmpireAI().GetWarDeclaredOnUs(OwnerEmpire, wt);
        }

        void AIDeclaresWarOnPlayer(Empire player, WarType warType, Relationship aiRelationToPlayer)
        {
            switch (warType)
            {
                case WarType.BorderConflict:
                    if (aiRelationToPlayer.GetContestedSystem(out SolarSystem contested))
                    {
                        DiplomacyScreen.Show(OwnerEmpire, player, "Declare War BC TarSys", contested);
                    }
                    else
                    {
                        DiplomacyScreen.Show(OwnerEmpire, player, "Declare War BC");
                    }
                    break;
                case WarType.ImperialistWar:
                    if (aiRelationToPlayer.Treaty_NAPact)
                    {
                        DiplomacyScreen.Show(OwnerEmpire, player, "Declare War Imperialism Break NA");
                        foreach (KeyValuePair<Empire, Relationship> kv in OwnerEmpire.AllRelations)
                        {
                            if (kv.Key != player)
                            {
                                kv.Value.Trust -= 50f;
                                kv.Value.Anger_DiplomaticConflict += 20f;
                            }
                        }
                    }
                    else
                    {
                        DiplomacyScreen.Show(OwnerEmpire, player, "Declare War Imperialism");
                    }
                    break;
                case WarType.DefensiveWar:
                    if (aiRelationToPlayer.Treaty_NAPact)
                    {
                        DiplomacyScreen.Show(OwnerEmpire, player, "Declare War Defense BrokenNA");
                        aiRelationToPlayer.Treaty_NAPact = false;
                        aiRelationToPlayer.Trust -= 50f;
                        aiRelationToPlayer.Anger_DiplomaticConflict += 50f;
                        foreach (KeyValuePair<Empire, Relationship> kv in OwnerEmpire.AllRelations)
                        {
                            if (kv.Key != player)
                            {
                                kv.Value.Trust -= 50f;
                                kv.Value.Anger_DiplomaticConflict += 20f;
                            }
                        }
                    }
                    else
                    {
                        DiplomacyScreen.Show(OwnerEmpire, player, "Declare War Defense");
                        aiRelationToPlayer.Anger_DiplomaticConflict += 25f;
                        aiRelationToPlayer.Trust -= 25f;
                    }
                    break;
                case WarType.GenocidalWar:
                    break;
                case WarType.SkirmishWar:
                    break;
            }
        }

        public void DeclareWarOnViaCall(Empire them, WarType wt)
        {
            Relationship ourRelationToThem = OwnerEmpire.GetRelations(them);
            ourRelationToThem.PreparingForWar = false;
            if (OwnerEmpire.isFaction || OwnerEmpire.data.Defeated || them.data.Defeated || them.isFaction)
                return;

            ourRelationToThem.FedQuest = null;
            if (OwnerEmpire == Empire.Universe.PlayerEmpire && ourRelationToThem.Treaty_NAPact)
            {
                ourRelationToThem.Treaty_NAPact     = false;
                Relationship item                                = them.GetRelations(OwnerEmpire);
                item.Trust                                       = item.Trust - 50f;
                Relationship angerDiplomaticConflict             = them.GetRelations(OwnerEmpire);
                angerDiplomaticConflict.Anger_DiplomaticConflict =
                    angerDiplomaticConflict.Anger_DiplomaticConflict + 50f;
                them.GetRelations(OwnerEmpire).UpdateRelationship(them, OwnerEmpire);
            }

            if (them == Empire.Universe.PlayerEmpire && !ourRelationToThem.AtWar)
            {
                AIDeclaresWarOnPlayer(them, wt, ourRelationToThem);
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

        public void EndWarFromEvent(Empire them)
        {
            OwnerEmpire.GetRelations(them).AtWar = false;
            them.GetRelations(OwnerEmpire).AtWar = false;

            foreach (MilitaryTask task in TaskList.AtomicCopy())
            {
                if (OwnerEmpire.GetFleetsDict().ContainsKey(task.WhichFleet) &&
                    OwnerEmpire.data.Traits.Name == "Corsairs")
                {
                    bool foundhome = false;
                    foreach (Ship ship in OwnerEmpire.GetShips())
                    {
                        if (ship.IsPlatformOrStation)
                        {
                            foundhome = true;
                            foreach (Ship fship in OwnerEmpire.GetFleetsDict()[task.WhichFleet].Ships)
                            {
                                fship.AI.ClearOrders();
                                fship.DoEscort(ship);
                            }
                            break;
                        }
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
            }
        }

        void FightBrutalWar(KeyValuePair<Empire, Relationship> r)
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
                Planet target = invasionTargets.FindMin(distance => distance.Center.SqDist(ownerCenter));
                TryAssaultPlanet(target);
            }

            var planetsWeAreInvading = new HashSet<Planet>();

            using (TaskList.AcquireReadLock())
            {
                foreach (MilitaryTask task in TaskList)
                {
                    if (task.type == MilitaryTask.TaskType.AssaultPlanet && task.TargetPlanet.Owner != null && task.TargetPlanet.Owner == r.Key)
                        planetsWeAreInvading.Add(task.TargetPlanet);
                }
            }

            if (planetsWeAreInvading.Count < 3 && OwnerEmpire.GetPlanets().Count > 0)
            {
                Vector2 empireCenter = FindAveragePosition(OwnerEmpire);
                Planet[] planetsByDistance = r.Key.GetPlanets().Sorted(p => empireCenter.SqDist(p.Center));
                foreach (Planet p in planetsByDistance)
                {
                    if (planetsWeAreInvading.Count >= 3)
                        break;

                    if (planetsWeAreInvading.Add(p)) // true: planet doesn't exist yet
                        TaskList.Add(new MilitaryTask(p, OwnerEmpire));
                }
            }
        }

        private void FightDefaultWar(KeyValuePair<Empire, Relationship> r, int warWeight)
        {
            foreach (MilitaryTask militaryTask in TaskList)
            {
                if (militaryTask.type == MilitaryTask.TaskType.AssaultPlanet)
                    warWeight--;
                if (warWeight < 0)
                    return;
            }

            WarTargets(r, warWeight);
        }

        void WarTargets(KeyValuePair<Empire, Relationship> r, int warWeight)
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
            relations.AtWar     = true;
            relations.FedQuest  = null;
            relations.Posture   = Posture.Hostile;
            relations.ActiveWar = new War(OwnerEmpire, warDeclarant, Empire.Universe.StarDate)
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

        IEnumerable<KeyValuePair<Empire, Relationship>> EmpireAttackWeights()
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

            int warWeight = (int)Math.Ceiling(1 + 5 * (OwnerEmpire.Research.Strategy.MilitaryRatio + OwnerEmpire.Research.Strategy.ExpansionRatio));
            var weightedTargets = EmpireAttackWeights();
            foreach (KeyValuePair<Empire, Relationship> kv in weightedTargets)
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

        public bool IsAlreadyAssaultingPlanet(Planet planet)
        {
            using (TaskList.AcquireReadLock())
                return TaskList.Any(task => task.TargetPlanet == planet 
                                         && task.type == MilitaryTask.TaskType.AssaultPlanet);
        }

        void TryAssaultPlanet(Planet planet)
        {
            if (!IsAlreadyAssaultingPlanet(planet))
                TaskList.Add(new MilitaryTask(planet, OwnerEmpire));
        }

        void AssignTargets(KeyValuePair<Empire, Relationship> kv, float warWeight, WarType warType = WarType.BorderConflict)
        {
            if (warWeight <= 0)
                return;

            Planet[] priorityTargets = PlanetTargetPriority(kv.Key, warType);
            if (priorityTargets.Length == 0)
                return;

            // so what this is trying to do here is take the best system and attack
            // all planets in that system to try and completely flip the system.

            SolarSystem targetSystem = priorityTargets[0].ParentSystem;
            Planet[] targetPlanetsInSystem = priorityTargets.Filter(p => p.ParentSystem == targetSystem);

            foreach (Planet planet in targetPlanetsInSystem)
            {
                TryAssaultPlanet(planet);
                if (warWeight-- <= 0)
                    break;
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
            
            return empire.GetPlanets()
                .OrderBy(insystem => !insystem.ParentSystem.OwnerList.Contains(OwnerEmpire))
                .ThenBy(planet => GetDistanceFromOurAO(planet) / 150000f)
                .ThenByDescending(planet =>
                    empire.GetEmpireAI().DefensiveCoordinator.DefenseDict.TryGetValue(planet.ParentSystem, out SystemCommander scom)
                    ? scom.PlanetValues[planet].Value : 0)
                .ToArray();
        }
    }
}