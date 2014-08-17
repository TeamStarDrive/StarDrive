using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class ExplorationEvent
	{
		public string Name;

		public List<Outcome> PotentialOutcomes;

		public ExplorationEvent()
		{
		}

		public void TriggerOutcome(Empire Triggerer, Outcome triggeredOutcome)
		{
			if (triggeredOutcome.SecretTechDiscovered != null)
			{
                if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.overrideSecretsTree)
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
				GlobalStats.RemnantArmageddon = true;
			}
			if (triggeredOutcome.GrantArtifact)
			{
				List<Ship_Game.Artifact> Potentials = new List<Ship_Game.Artifact>();
				foreach (KeyValuePair<string, Ship_Game.Artifact> Artifact in ResourceManager.ArtifactsDict)
				{
					if (Artifact.Value.Discovered)
					{
						continue;
					}
					Potentials.Add(Artifact.Value);
				}
				if (Potentials.Count <= 0)
				{
					triggeredOutcome.MoneyGranted = 500;
				}
				else
				{
					int ranart = (int)RandomMath.RandomBetween(0f, (float)Potentials.Count + 0.8f);
					if (ranart > Potentials.Count - 1)
					{
						ranart = Potentials.Count - 1;
					}
					Triggerer.data.OwnedArtifacts.Add(Potentials[ranart]);
					ResourceManager.ArtifactsDict[Potentials[ranart].Name].Discovered = true;
					triggeredOutcome.SetArtifact(Potentials[ranart]);
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
			Empire money = Triggerer;
			money.Money = money.Money + (float)triggeredOutcome.MoneyGranted;
			RacialTrait racialTrait = Triggerer.data.Traits;
			racialTrait.ResearchMod = racialTrait.ResearchMod + triggeredOutcome.ScienceBonus;
			RacialTrait traits1 = Triggerer.data.Traits;
			traits1.ProductionMod = traits1.ProductionMod + triggeredOutcome.IndustryBonus;
			PlanetGridSquare assignedtile = null;
			if (triggeredOutcome.SelectRandomPlanet)
			{
				List<Planet> Potentials = new List<Planet>();
				foreach (SolarSystem s in UniverseScreen.SolarSystemList)
				{
					foreach (Planet p in s.PlanetList)
					{
						if (!p.habitable || p.Owner != null)
						{
							continue;
						}
						Potentials.Add(p);
					}
				}
				if (Potentials.Count > 0)
				{
					triggeredOutcome.SetPlanet(Potentials[HelperFunctions.GetRandomIndex(Potentials.Count)]);
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
			}
			if (assignedtile != null && triggeredOutcome.GetPlanet() != null && triggeredOutcome.TroopsToSpawn != null)
			{
				foreach (string troopname in triggeredOutcome.TroopsToSpawn)
				{
					Troop t = ResourceManager.CreateTroop(ResourceManager.TroopsDict[troopname], EmpireManager.GetEmpireByName("Unknown"));
					t.SetOwner(EmpireManager.GetEmpireByName("The Remnant"));
					if (triggeredOutcome.GetPlanet().AssignTroopToNearestAvailableTile(t, assignedtile))
					{
						continue;
					}
					triggeredOutcome.GetPlanet().AssignTroopToTile(t);
				}
			}
		}

		public void TriggerPlanetEvent(Planet p, Empire Triggerer, PlanetGridSquare eventLocation, Empire PlayerEmpire, UniverseScreen screen)
		{
			int Random = (int)RandomMath.RandomBetween(0f, 100f);
			Outcome triggeredOutcome = new Outcome();
			int cursor = 0;
			foreach (Outcome outcome in this.PotentialOutcomes)
			{
				cursor = cursor + outcome.Chance;
				if (Random > cursor)
				{
					continue;
				}
				triggeredOutcome = outcome;
				break;
			}
			if (triggeredOutcome != null)
			{
				if (triggeredOutcome.GrantArtifact)
				{
					List<Ship_Game.Artifact> Potentials = new List<Ship_Game.Artifact>();
					foreach (KeyValuePair<string, Ship_Game.Artifact> Artifact in ResourceManager.ArtifactsDict)
					{
						if (Artifact.Value.Discovered)
						{
							continue;
						}
						Potentials.Add(Artifact.Value);
					}
					if (Potentials.Count <= 0)
					{
						triggeredOutcome.MoneyGranted = 500;
					}
					else
					{
						int ranart = (int)RandomMath.RandomBetween(0f, (float)Potentials.Count + 0.8f);
						if (ranart > Potentials.Count - 1)
						{
							ranart = Potentials.Count - 1;
						}
						Triggerer.data.OwnedArtifacts.Add(Potentials[ranart]);
						ResourceManager.ArtifactsDict[Potentials[ranart].Name].Discovered = true;
						triggeredOutcome.SetArtifact(Potentials[ranart]);
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
				if (triggeredOutcome.BeginArmageddon)
				{
					GlobalStats.RemnantArmageddon = true;
				}
				foreach (string ship in triggeredOutcome.FriendlyShipsToSpawn)
				{
					Triggerer.ForcePoolAdd(ResourceManager.CreateShipAt(ship, Triggerer, p, true));
				}
				foreach (string ship in triggeredOutcome.RemnantShipsToSpawn)
				{
					Ship tomake = ResourceManager.CreateShipAt(ship, EmpireManager.GetEmpireByName("The Remnant"), p, true);
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
				if (triggeredOutcome.ReplaceWith != "")
				{
					eventLocation.building = ResourceManager.GetBuilding(triggeredOutcome.ReplaceWith);
					p.BuildingList.Add(eventLocation.building);
				}
				Empire money = Triggerer;
				money.Money = money.Money + (float)triggeredOutcome.MoneyGranted;
				RacialTrait racialTrait = Triggerer.data.Traits;
				racialTrait.ResearchMod = racialTrait.ResearchMod + triggeredOutcome.ScienceBonus;
				RacialTrait traits1 = Triggerer.data.Traits;
				traits1.ProductionMod = traits1.ProductionMod + triggeredOutcome.IndustryBonus;
				if (triggeredOutcome.TroopsGranted != null)
				{
					foreach (string troopname in triggeredOutcome.TroopsGranted)
					{
						Troop t = ResourceManager.CreateTroop(ResourceManager.TroopsDict[troopname], Triggerer);
						t.SetOwner(Triggerer);
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
						Troop t = ResourceManager.CreateTroop(ResourceManager.TroopsDict[troopname], EmpireManager.GetEmpireByName("Unknown"));
						t.SetOwner(EmpireManager.GetEmpireByName("Unknown"));
						if (p.AssignTroopToNearestAvailableTile(t, eventLocation))
						{
							continue;
						}
						p.AssignTroopToTile(t);
					}
				}
			}
			if (Triggerer == PlayerEmpire)
			{
				screen.ScreenManager.AddScreen(new EventPopup(screen, PlayerEmpire, this, triggeredOutcome));
				AudioManager.PlayCue("sd_notify_alert");
			}
		}
	}
}