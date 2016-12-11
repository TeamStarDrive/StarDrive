using System;
using System.Collections.Generic;
using Ship_Game.Gameplay;

namespace Ship_Game
{
	public sealed class Outcome
	{
		private Planet SelectedPlanet;

		public bool BeginArmageddon;

		public int Chance;

		private Artifact grantedArtifact;

		public List<string> TroopsToSpawn;

		public List<string> FriendlyShipsToSpawn;

		public List<string> RemnantShipsToSpawn;

		public bool UnlockSecretBranch;

		public string SecretTechDiscovered;

		public string TitleText;

		public string UnlockTech;

		public bool WeHadIt;

		public bool GrantArtifact;

		public bool RemoveTrigger;

		public string ReplaceWith = "";

		public string DescriptionText;

		public int MoneyGranted;

		public List<string> TroopsGranted;

		public float FoodProductionBonus;

		public float IndustryBonus;

		public float ScienceBonus;

		public bool SelectRandomPlanet;

		public string SpawnBuildingOnPlanet;

		public string SpawnFleetInOrbitOfPlanet;

        public bool onlyTriggerOnce;

        public bool alreadyTriggered;

		public Outcome()
		{
		}

		public Artifact GetArtifact()
		{
			return this.grantedArtifact;
		}

		public Planet GetPlanet()
		{
			return this.SelectedPlanet;
		}

		public void SetArtifact(Artifact art)
		{
			this.grantedArtifact = art;
		}

		public void SetPlanet(Planet p)
		{
			this.SelectedPlanet = p;
		}


        public void CheckOutComes(Planet p, Outcome triggeredOutcome, PlanetGridSquare eventLocation, Empire triggerer)
        {
            if (triggeredOutcome == null)
            {
                return;
            }
            if (triggeredOutcome.GrantArtifact)
            {
                Artifact.CheckGrantArtifact(triggerer, triggeredOutcome);
            }            
            triggerer.Money += triggeredOutcome.MoneyGranted;
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
                List<Planet> potentials = new List<Planet>();
                foreach (SolarSystem s in UniverseScreen.SolarSystemList)
                {
                    foreach (Planet rp in s.PlanetList)
                    {
                        if (!rp.habitable || rp.Owner != null)
                        {
                            continue;
                        }
                        potentials.Add(rp);
                    }
                }
                if (potentials.Count > 0)
                {
                    triggeredOutcome.SetPlanet(potentials[RandomMath.InRange(potentials.Count)]);
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

                if (assignedtile != null && triggeredOutcome.GetPlanet() != null &&
                    triggeredOutcome.TroopsToSpawn != null)
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