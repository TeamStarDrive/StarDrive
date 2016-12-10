using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
    public sealed class ExplorationEvent
    {
        public string Name;

        public List<Outcome> PotentialOutcomes;

        public void TriggerOutcome(Empire triggerer, Outcome triggeredOutcome)
        {
            CheckOutComes(null, triggeredOutcome, null, triggerer);           
        }

        public void TriggerPlanetEvent(Planet p, Empire triggerer, PlanetGridSquare eventLocation, Empire playerEmpire, UniverseScreen screen)
        {
            int ranMax = 0;
            int ranMin = 0;
            foreach (Outcome outcome in this.PotentialOutcomes)
            {
                if (outcome.onlyTriggerOnce && outcome.alreadyTriggered && triggerer.isPlayer)
                {
                    continue;
                }
                else
                {
                    ranMax += outcome.Chance;
                }
            }

            int random = (int) RandomMath.RandomBetween(ranMin, ranMax);

            Outcome triggeredOutcome = null;
            int cursor = 0;
            foreach (Outcome outcome in this.PotentialOutcomes)
            {
                if (outcome.onlyTriggerOnce && outcome.alreadyTriggered && triggerer.isPlayer) continue;
                cursor = cursor + outcome.Chance;
                if (random > cursor) continue;
                triggeredOutcome = outcome;
                if (triggerer.isPlayer) outcome.alreadyTriggered = true;
                break;
            }
            CheckOutComes(p, triggeredOutcome, eventLocation, triggerer);
            if (triggerer == playerEmpire)
            {
                screen.ScreenManager.AddScreen(new EventPopup(screen, playerEmpire, this, triggeredOutcome));
                AudioManager.PlayCue("sd_notify_alert");
            }
        }

        private void CheckGrantArtifact(Empire triggerer, Outcome triggeredOutcome)
        {
            if (!triggeredOutcome.GrantArtifact) return;
            List<Artifact> potentials = new List<Artifact>();
            foreach (KeyValuePair<string, Artifact> artifact in ResourceManager.ArtifactsDict)
            {
                if (artifact.Value.Discovered)
                {
                    continue;
                }
                potentials.Add(artifact.Value);
            }
            if (potentials.Count <= 0)
            {
                triggeredOutcome.MoneyGranted = 500;
            }
            else
            {
                int ranart = (int) RandomMath.RandomBetween(0f, (float) potentials.Count + 0.8f);
                if (ranart > potentials.Count - 1)
                {
                    ranart = potentials.Count - 1;
                }
                triggerer.data.OwnedArtifacts.Add(potentials[ranart]);
                ResourceManager.ArtifactsDict[potentials[ranart].Name].Discovered = true;
                triggeredOutcome.SetArtifact(potentials[ranart]);
                if (triggeredOutcome.GetArtifact().DiplomacyMod > 0f)
                {
                    triggerer.data.Traits.DiplomacyMod += (triggeredOutcome.GetArtifact().DiplomacyMod +
                                                           triggeredOutcome.GetArtifact().DiplomacyMod *
                                                           triggerer.data.Traits.Spiritual);
                }
                if (triggeredOutcome.GetArtifact().FertilityMod > 0f)
                {
                    triggerer.data.EmpireFertilityBonus += triggeredOutcome.GetArtifact().FertilityMod;
                    foreach (Planet planet in triggerer.GetPlanets())
                    {
                        Planet fertility = planet;
                        fertility.Fertility = fertility.Fertility +
                                              (triggeredOutcome.GetArtifact().FertilityMod +
                                               triggeredOutcome.GetArtifact().FertilityMod *
                                               triggerer.data.Traits.Spiritual);
                    }
                }
                if (triggeredOutcome.GetArtifact().GroundCombatMod > 0f)
                {
                    triggerer.data.Traits.GroundCombatModifier += (triggeredOutcome.GetArtifact().GroundCombatMod +
                                                                   triggeredOutcome.GetArtifact().GroundCombatMod *
                                                                   triggerer.data.Traits.Spiritual);
                }
                if (triggeredOutcome.GetArtifact().ModuleHPMod > 0f)
                {
                    triggerer.data.Traits.ModHpModifier += (triggeredOutcome.GetArtifact().ModuleHPMod +
                                                            triggeredOutcome.GetArtifact().ModuleHPMod *
                                                            triggerer.data.Traits.Spiritual);
                    triggerer.RecalculateMaxHP = true;
                    //So existing ships will benefit from changes to ModHpModifier -Gretman
                }
                if (triggeredOutcome.GetArtifact().PlusFlatMoney > 0f)
                {
                    triggerer.data.FlatMoneyBonus += (triggeredOutcome.GetArtifact().PlusFlatMoney +
                                                      triggeredOutcome.GetArtifact().PlusFlatMoney *
                                                      triggerer.data.Traits.Spiritual);
                }
                if (triggeredOutcome.GetArtifact().ProductionMod > 0f)
                {
                    triggerer.data.Traits.ProductionMod += (triggeredOutcome.GetArtifact().ProductionMod +
                                                            triggeredOutcome.GetArtifact().ProductionMod *
                                                            triggerer.data.Traits.Spiritual);
                }
                if (triggeredOutcome.GetArtifact().ReproductionMod > 0f)
                {
                    triggerer.data.Traits.ReproductionMod += (triggeredOutcome.GetArtifact().ReproductionMod +
                                                              triggeredOutcome.GetArtifact().ReproductionMod *
                                                              triggerer.data.Traits.Spiritual);
                }
                if (triggeredOutcome.GetArtifact().ResearchMod > 0f)
                {
                    triggerer.data.Traits.ResearchMod += (triggeredOutcome.GetArtifact().ResearchMod +
                                                          triggeredOutcome.GetArtifact().ResearchMod *
                                                          triggerer.data.Traits.Spiritual);
                }
                if (triggeredOutcome.GetArtifact().SensorMod > 0f)
                {
                    triggerer.data.SensorModifier += (triggeredOutcome.GetArtifact().SensorMod +
                                                      triggeredOutcome.GetArtifact().SensorMod *
                                                      triggerer.data.Traits.Spiritual);
                }
                if (triggeredOutcome.GetArtifact().ShieldPenBonus > 0f)
                {
                    triggerer.data.ShieldPenBonusChance += (triggeredOutcome.GetArtifact().ShieldPenBonus +
                                                            triggeredOutcome.GetArtifact().ShieldPenBonus *
                                                            triggerer.data.Traits.Spiritual);
                }
            }
        }

        private void CheckOutComes(Planet p, Outcome triggeredOutcome, PlanetGridSquare eventLocation, Empire triggerer)
        {
            if (triggeredOutcome == null)
            {
                return;
            }
            CheckGrantArtifact(triggerer, triggeredOutcome);
            triggerer.Money += (float)triggeredOutcome.MoneyGranted;
            triggerer.data.Traits.ResearchMod += triggeredOutcome.ScienceBonus;
            triggerer.data.Traits.ProductionMod += triggeredOutcome.IndustryBonus;

            if (triggeredOutcome.SecretTechDiscovered != null)
            {
                if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.overrideSecretsTree)
                {
                    triggerer.GetTDict()[triggeredOutcome.SecretTechDiscovered].Discovered = true;
                }
                else
                {
                    triggerer.GetTDict()["Secret"].Discovered = true;
                    triggerer.GetTDict()[triggeredOutcome.SecretTechDiscovered].Discovered = true;
                }
            }
            if (triggeredOutcome.BeginArmageddon)
            {
                GlobalStats.RemnantArmageddon = true;
            }
            foreach (string ship in triggeredOutcome.FriendlyShipsToSpawn)
            {
                triggerer.ForcePoolAdd(ResourceManager.CreateShipAt(ship, triggerer, p, true));
            }
            foreach (string ship in triggeredOutcome.RemnantShipsToSpawn)
            {
                Ship tomake = ResourceManager.CreateShipAt(ship, EmpireManager.GetEmpireByName("The Remnant"), p, true);
                tomake.GetAI().DefaultAIState = AIState.Exterminate;
            }
            if (triggeredOutcome.UnlockTech != null)
            {
                if (!triggerer.GetTDict()[triggeredOutcome.UnlockTech].Unlocked)
                {
                    triggerer.UnlockTech(triggeredOutcome.UnlockTech);
                }
                else
                {
                    triggeredOutcome.WeHadIt = true;
                }
            }
            if (triggeredOutcome.RemoveTrigger)
            {
                p.BuildingList.Remove(eventLocation.building);
                eventLocation.building = null;
            }
            if (!string.IsNullOrEmpty(triggeredOutcome.ReplaceWith))
            {
                eventLocation.building = ResourceManager.GetBuilding(triggeredOutcome.ReplaceWith);
                p.BuildingList.Add(eventLocation.building);
            }
            if (triggeredOutcome.TroopsGranted != null)
            {
                foreach (string troopname in triggeredOutcome.TroopsGranted)
                {
                    Troop t = ResourceManager.CreateTroop(ResourceManager.TroopsDict[troopname], triggerer);
                    t.SetOwner(triggerer);
                    if (p.AssignTroopToNearestAvailableTile(t, eventLocation))
                    {
                        continue;
                    }
                    p.AssignTroopToTile(t);
                }
            }
            if (triggeredOutcome.TroopsToSpawn != null)
            {
                foreach (string troopname in triggeredOutcome.TroopsToSpawn)
                {
                    Troop t = ResourceManager.CreateTroop(ResourceManager.TroopsDict[troopname],
                        EmpireManager.GetEmpireByName("Unknown"));
                    t.SetOwner(EmpireManager.GetEmpireByName("Unknown"));
                    if (p.AssignTroopToNearestAvailableTile(t, eventLocation))
                    {
                        continue;
                    }
                    p.AssignTroopToTile(t);
                }
            }
            if (triggeredOutcome.SelectRandomPlanet)
            {
                PlanetGridSquare assignedtile = null;
                List<Planet> Potentials = new List<Planet>();
                foreach (SolarSystem s in UniverseScreen.SolarSystemList)
                {
                    foreach (Planet rp in s.PlanetList)
                    {
                        if (!rp.habitable || rp.Owner != null)
                        {
                            continue;
                        }
                        Potentials.Add(rp);
                    }
                }
                if (Potentials.Count > 0)
                {
                    triggeredOutcome.SetPlanet(Potentials[RandomMath.InRange(Potentials.Count)]);
                }
                if (triggeredOutcome.GetPlanet() != null)
                {
                    assignedtile = triggeredOutcome.GetPlanet().TilesList[17];
                    if (triggeredOutcome.SpawnBuildingOnPlanet != null)
                    {
                        Building b = ResourceManager.GetBuilding(triggeredOutcome.SpawnBuildingOnPlanet);
                        triggeredOutcome.GetPlanet().AssignBuildingToSpecificTile(b, assignedtile);
                    }
                }

                if (assignedtile != null && triggeredOutcome.GetPlanet() != null && triggeredOutcome.TroopsToSpawn != null)
                {
                    foreach (string troopname in triggeredOutcome.TroopsToSpawn)
                    {
                        Troop t = ResourceManager.CreateTroop(ResourceManager.TroopsDict[troopname],
                            EmpireManager.GetEmpireByName("Unknown"));
                        t.SetOwner(EmpireManager.GetEmpireByName("The Remnant"));
                        if (triggeredOutcome.GetPlanet().AssignTroopToNearestAvailableTile(t, assignedtile))
                        {
                            continue;
                        }
                        triggeredOutcome.GetPlanet().AssignTroopToTile(t);
                    }
                }
            }

        }
    }
}