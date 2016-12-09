using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using static Ship_Game.ResourceManager;
using static Ship_Game.GlobalStats;

namespace Ship_Game
{
	public sealed class ExplorationEvent
	{
		public string Name;

		public List<Outcome> PotentialOutcomes;

	    public void TriggerOutcome(Empire Triggerer, Outcome triggeredOutcome)
		{
			if (triggeredOutcome.SecretTechDiscovered != null)
			{
				if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.overrideSecretsTree)
                {
                    Triggerer.GetTDict()[triggeredOutcome.SecretTechDiscovered].Discovered = true;
                }
                else
                {
                    Triggerer.GetTDict()["Secret"].Discovered = true;
                    Triggerer.GetTDict()[triggeredOutcome.SecretTechDiscovered].Discovered = true;
                }
			}
			if (triggeredOutcome.BeginArmageddon)
			{
                RemnantArmageddon = true;
			}
			if (triggeredOutcome.GrantArtifact)
			{
				var potentials = new List<Artifact>();
				foreach (var artifact in ArtifactsDict)
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
					int ranart = (int)RandomMath.RandomBetween(0f, (float)potentials.Count + 0.8f);
					if (ranart > potentials.Count - 1)
					{
						ranart = potentials.Count - 1;
					}
					Triggerer.data.OwnedArtifacts.Add(potentials[ranart]);
					ArtifactsDict[potentials[ranart].Name].Discovered = true;
					triggeredOutcome.SetArtifact(potentials[ranart]);
					if (triggeredOutcome.GetArtifact().DiplomacyMod > 0f)
					{
						RacialTrait traits = Triggerer.data.Traits;
						traits.DiplomacyMod = traits.DiplomacyMod + (triggeredOutcome.GetArtifact().DiplomacyMod + triggeredOutcome.GetArtifact().DiplomacyMod * Triggerer.data.Traits.Spiritual);
					}
					if (triggeredOutcome.GetArtifact().FertilityMod > 0f)
					{
						EmpireData triggerer = Triggerer.data;
						triggerer.EmpireFertilityBonus = triggerer.EmpireFertilityBonus + triggeredOutcome.GetArtifact().FertilityMod;
						foreach (Planet planet in Triggerer.GetPlanets())
						{
							Planet fertility = planet;
							fertility.Fertility = fertility.Fertility + (triggeredOutcome.GetArtifact().FertilityMod + triggeredOutcome.GetArtifact().FertilityMod * Triggerer.data.Traits.Spiritual);
						}
					}
					if (triggeredOutcome.GetArtifact().GroundCombatMod > 0f)
					{
						RacialTrait groundCombatModifier = Triggerer.data.Traits;
						groundCombatModifier.GroundCombatModifier = groundCombatModifier.GroundCombatModifier + (triggeredOutcome.GetArtifact().GroundCombatMod + triggeredOutcome.GetArtifact().GroundCombatMod * Triggerer.data.Traits.Spiritual);
					}
					if (triggeredOutcome.GetArtifact().ModuleHPMod > 0f)
					{
						RacialTrait modHpModifier = Triggerer.data.Traits;
						modHpModifier.ModHpModifier = modHpModifier.ModHpModifier + (triggeredOutcome.GetArtifact().ModuleHPMod + triggeredOutcome.GetArtifact().ModuleHPMod * Triggerer.data.Traits.Spiritual);
                        Triggerer.RecalculateMaxHP = true;       //So existing ships will benefit from changes to ModHpModifier -Gretman
                    }
					if (triggeredOutcome.GetArtifact().PlusFlatMoney > 0f)
					{
						EmpireData flatMoneyBonus = Triggerer.data;
						flatMoneyBonus.FlatMoneyBonus = flatMoneyBonus.FlatMoneyBonus + (triggeredOutcome.GetArtifact().PlusFlatMoney + triggeredOutcome.GetArtifact().PlusFlatMoney * Triggerer.data.Traits.Spiritual);
					}
					if (triggeredOutcome.GetArtifact().ProductionMod > 0f)
					{
						RacialTrait productionMod = Triggerer.data.Traits;
						productionMod.ProductionMod = productionMod.ProductionMod + (triggeredOutcome.GetArtifact().ProductionMod + triggeredOutcome.GetArtifact().ProductionMod * Triggerer.data.Traits.Spiritual);
					}
					if (triggeredOutcome.GetArtifact().ReproductionMod > 0f)
					{
						RacialTrait reproductionMod = Triggerer.data.Traits;
						reproductionMod.ReproductionMod = reproductionMod.ReproductionMod + (triggeredOutcome.GetArtifact().ReproductionMod + triggeredOutcome.GetArtifact().ReproductionMod * Triggerer.data.Traits.Spiritual);
					}
					if (triggeredOutcome.GetArtifact().ResearchMod > 0f)
					{
						RacialTrait researchMod = Triggerer.data.Traits;
						researchMod.ResearchMod = researchMod.ResearchMod + (triggeredOutcome.GetArtifact().ResearchMod + triggeredOutcome.GetArtifact().ResearchMod * Triggerer.data.Traits.Spiritual);
					}
					if (triggeredOutcome.GetArtifact().SensorMod > 0f)
					{
						EmpireData sensorModifier = Triggerer.data;
						sensorModifier.SensorModifier = sensorModifier.SensorModifier + (triggeredOutcome.GetArtifact().SensorMod + triggeredOutcome.GetArtifact().SensorMod * Triggerer.data.Traits.Spiritual);
					}
					if (triggeredOutcome.GetArtifact().ShieldPenBonus > 0f)
					{
						EmpireData shieldPenBonusChance = Triggerer.data;
						shieldPenBonusChance.ShieldPenBonusChance = shieldPenBonusChance.ShieldPenBonusChance + (triggeredOutcome.GetArtifact().ShieldPenBonus + triggeredOutcome.GetArtifact().ShieldPenBonus * Triggerer.data.Traits.Spiritual);
					}
				}
			}
			if (triggeredOutcome.UnlockTech != null)
			{
				if (!Triggerer.GetTDict()[triggeredOutcome.UnlockTech].Unlocked)
				{
					Triggerer.UnlockTech(triggeredOutcome.UnlockTech);
				}
				else
				{
					triggeredOutcome.WeHadIt = true;
				}
			}

            Triggerer.Money += triggeredOutcome.MoneyGranted;
            Triggerer.data.Traits.ResearchMod += triggeredOutcome.ScienceBonus;
            Triggerer.data.Traits.ProductionMod += triggeredOutcome.IndustryBonus;
			PlanetGridSquare assignedtile = null;
			if (triggeredOutcome.SelectRandomPlanet)
			{
				var potentials = new List<Planet>();
				foreach (SolarSystem s in UniverseScreen.SolarSystemList)
				{
				    potentials.AddRange(s.PlanetList.Where(p => p.habitable && p.Owner == null));
				}
				if (potentials.Count > 0)
				{
					triggeredOutcome.SetPlanet(potentials[HelperFunctions.GetRandomIndex(potentials.Count)]);
				}
				if (triggeredOutcome.GetPlanet() != null)
				{
					assignedtile = triggeredOutcome.GetPlanet().TilesList[17];
					if (triggeredOutcome.SpawnBuildingOnPlanet != null)
					{
						Building b = GetBuilding(triggeredOutcome.SpawnBuildingOnPlanet);
						triggeredOutcome.GetPlanet().AssignBuildingToSpecificTile(b, assignedtile);
					}
				}
			}
			if (assignedtile != null && triggeredOutcome.GetPlanet() != null && triggeredOutcome.TroopsToSpawn != null)
			{
                SpawnTroops(triggeredOutcome.GetPlanet(), triggeredOutcome.TroopsToSpawn, assignedtile, EmpireManager.GetEmpireByName("The Remnant"));
            }
		}

		public void TriggerPlanetEvent(Planet p, Empire Triggerer, PlanetGridSquare eventLocation, Empire playerEmpire, UniverseScreen screen)
		{
            int ranMax = 0;
            int ranMin = 0;
            foreach (Outcome outcome in this.PotentialOutcomes)
            {
                if (!(outcome.onlyTriggerOnce && outcome.alreadyTriggered && Triggerer.isPlayer))
                {
                    ranMax += outcome.Chance;
                }                
            }

			int random = (int)RandomMath.RandomBetween(ranMin, ranMax);

		    Outcome triggeredOutcome = null;
			int cursor = 0;
			foreach (Outcome outcome in this.PotentialOutcomes)
			{
			    if (outcome.onlyTriggerOnce && outcome.alreadyTriggered && Triggerer.isPlayer)
			        continue;

                cursor = cursor + outcome.Chance;
                if (random > cursor)
                {
                    continue;
                }
                triggeredOutcome = outcome;
                if (Triggerer.isPlayer)
                {
                    outcome.alreadyTriggered = true;
                }
                break;
                
			}
			if (triggeredOutcome != null)
			{
				if (triggeredOutcome.GrantArtifact)
				{
					List<Artifact> potentials = (from artifact in ArtifactsDict where !artifact.Value.Discovered select artifact.Value).ToList();
				    if (potentials.Count <= 0)
					{
						triggeredOutcome.MoneyGranted = 500;
					}
					else
					{
						int ranart = (int)RandomMath.RandomBetween(0f, potentials.Count + 0.8f);
						if (ranart > potentials.Count - 1)
						{
							ranart = potentials.Count - 1;
						}
						Triggerer.data.OwnedArtifacts.Add(potentials[ranart]);
						ArtifactsDict[potentials[ranart].Name].Discovered = true;
						triggeredOutcome.SetArtifact(potentials[ranart]);
						if (triggeredOutcome.GetArtifact().DiplomacyMod > 0f)
						{
                            Triggerer.data.Traits.DiplomacyMod += (triggeredOutcome.GetArtifact().DiplomacyMod + triggeredOutcome.GetArtifact().DiplomacyMod * Triggerer.data.Traits.Spiritual);
						}
						if (triggeredOutcome.GetArtifact().FertilityMod > 0f)
						{
                            Triggerer.data.EmpireFertilityBonus = Triggerer.data.EmpireFertilityBonus + triggeredOutcome.GetArtifact().FertilityMod;
							foreach (Planet planet in Triggerer.GetPlanets())
							{
                                planet.Fertility += (triggeredOutcome.GetArtifact().FertilityMod + triggeredOutcome.GetArtifact().FertilityMod * Triggerer.data.Traits.Spiritual);
							}
						}
						if (triggeredOutcome.GetArtifact().GroundCombatMod > 0f)
						{
                            Triggerer.data.Traits.GroundCombatModifier += (triggeredOutcome.GetArtifact().GroundCombatMod + triggeredOutcome.GetArtifact().GroundCombatMod * Triggerer.data.Traits.Spiritual);
						}
						if (triggeredOutcome.GetArtifact().ModuleHPMod > 0f)
						{
                            Triggerer.data.Traits.ModHpModifier += (triggeredOutcome.GetArtifact().ModuleHPMod + triggeredOutcome.GetArtifact().ModuleHPMod * Triggerer.data.Traits.Spiritual);
                            Triggerer.RecalculateMaxHP = true;       //So existing ships will benefit from changes to ModHpModifier -Gretman
                        }
						if (triggeredOutcome.GetArtifact().PlusFlatMoney > 0f)
						{
                            Triggerer.data.FlatMoneyBonus += (triggeredOutcome.GetArtifact().PlusFlatMoney + triggeredOutcome.GetArtifact().PlusFlatMoney * Triggerer.data.Traits.Spiritual);
						}
						if (triggeredOutcome.GetArtifact().ProductionMod > 0f)
						{
                            Triggerer.data.Traits.ProductionMod += (triggeredOutcome.GetArtifact().ProductionMod + triggeredOutcome.GetArtifact().ProductionMod * Triggerer.data.Traits.Spiritual);
						}
						if (triggeredOutcome.GetArtifact().ReproductionMod > 0f)
						{
                            Triggerer.data.Traits.ReproductionMod += (triggeredOutcome.GetArtifact().ReproductionMod + triggeredOutcome.GetArtifact().ReproductionMod * Triggerer.data.Traits.Spiritual);
						}
						if (triggeredOutcome.GetArtifact().ResearchMod > 0f)
						{
                            Triggerer.data.Traits.ResearchMod += (triggeredOutcome.GetArtifact().ResearchMod + triggeredOutcome.GetArtifact().ResearchMod * Triggerer.data.Traits.Spiritual);
						}
						if (triggeredOutcome.GetArtifact().SensorMod > 0f)
						{
                            Triggerer.data.SensorModifier += (triggeredOutcome.GetArtifact().SensorMod + triggeredOutcome.GetArtifact().SensorMod * Triggerer.data.Traits.Spiritual);
						}
						if (triggeredOutcome.GetArtifact().ShieldPenBonus > 0f)
						{
                            Triggerer.data.ShieldPenBonusChance += (triggeredOutcome.GetArtifact().ShieldPenBonus + triggeredOutcome.GetArtifact().ShieldPenBonus * Triggerer.data.Traits.Spiritual);
						}
					}
				}
				if (triggeredOutcome.BeginArmageddon)
				{
					GlobalStats.RemnantArmageddon = true;
				}
				foreach (string ship in triggeredOutcome.FriendlyShipsToSpawn)
				{
					Triggerer.ForcePoolAdd(CreateShipAt(ship, Triggerer, p, true));
				}
				foreach (string ship in triggeredOutcome.RemnantShipsToSpawn)
				{
					Ship tomake = CreateShipAt(ship, EmpireManager.GetEmpireByName("The Remnant"), p, true);
					tomake.GetAI().DefaultAIState = AIState.Exterminate;
				}
				if (triggeredOutcome.UnlockTech != null)
				{
					if (!Triggerer.GetTDict()[triggeredOutcome.UnlockTech].Unlocked)
					{
						Triggerer.UnlockTech(triggeredOutcome.UnlockTech);
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
					eventLocation.building = GetBuilding(triggeredOutcome.ReplaceWith);
					p.BuildingList.Add(eventLocation.building);
				}				
                Triggerer.Money += triggeredOutcome.MoneyGranted;
                Triggerer.data.Traits.ResearchMod += triggeredOutcome.ScienceBonus;
                Triggerer.data.Traits.ProductionMod += triggeredOutcome.IndustryBonus;
				if (triggeredOutcome.TroopsGranted != null)
				{					
                    SpawnTroops(p, triggeredOutcome.TroopsGranted, eventLocation,Triggerer);
                }
				if (triggeredOutcome.TroopsToSpawn != null)
				{
				    SpawnTroops(p,triggeredOutcome.TroopsToSpawn, eventLocation, EmpireManager.GetEmpireByName("Unknown"));

				}
			}
			if (Triggerer == playerEmpire)
			{
				screen.ScreenManager.AddScreen(new EventPopup(screen, playerEmpire, this, triggeredOutcome));
				AudioManager.PlayCue("sd_notify_alert");
			}
		}

	    private void SpawnTroops(Planet p, List<string> troopsToSpawn, PlanetGridSquare tile, Empire empire)
	    {
            foreach (string troopname in troopsToSpawn)
            {
                Troop t = CreateTroop(TroopsDict[troopname], empire);
                if (p.AssignTroopToNearestAvailableTile(t, tile))
                {
                    continue;
                }
                p.AssignTroopToTile(t);
            }
        }
	}
}