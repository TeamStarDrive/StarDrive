using System.Collections.Generic;
using Ship_Game.Gameplay;

namespace Ship_Game
{
	public sealed class Outcome
	{
		private Planet _selectedPlanet;

		public bool BeginArmageddon;

		public int Chance;

		private Artifact _grantedArtifact;

		public Array<string> TroopsToSpawn;

		public Array<string> FriendlyShipsToSpawn;

		public Array<string> RemnantShipsToSpawn;

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

		public Array<string> TroopsGranted;

		public float FoodProductionBonus;

		public float IndustryBonus;

		public float ScienceBonus;

		public bool SelectRandomPlanet;

		public string SpawnBuildingOnPlanet;

		public string SpawnFleetInOrbitOfPlanet;

        public bool OnlyTriggerOnce;

        public bool AlreadyTriggered;

	    public Artifact GetArtifact()
		{
			return this._grantedArtifact;
		}

		public Planet GetPlanet()
		{
			return this._selectedPlanet;
		}

		public void SetArtifact(Artifact art)
		{
			this._grantedArtifact = art;
		}

		public void SetPlanet(Planet p)
		{
			this._selectedPlanet = p;
		}

	    private void FlatGrants(Empire triggerEmpire)
	    {
            triggerEmpire.Money += MoneyGranted;
            triggerEmpire.data.Traits.ResearchMod += ScienceBonus;
            triggerEmpire.data.Traits.ProductionMod += IndustryBonus;
        }

	    private void TechGrants(Empire triggerer)
	    {
            if (SecretTechDiscovered != null)
            {
                if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.overrideSecretsTree)
                {
                    triggerer.GetTDict()[SecretTechDiscovered].Discovered = true;
                }
                else
                {
                    triggerer.GetTDict()["Secret"].Discovered = true;
                    triggerer.GetTDict()[SecretTechDiscovered].Discovered = true;
                }
            }
            if (UnlockTech != null)
            {
                if (!triggerer.GetTDict()[UnlockTech].Unlocked)
                {
                    triggerer.UnlockTech(UnlockTech);
                }
                else
                {
                    WeHadIt = true;
                }
            }
        }

	    private void ShipGrants(Empire triggerer ,Planet p)
	    {
            foreach (string ship in FriendlyShipsToSpawn)
            {
                triggerer.ForcePoolAdd(ResourceManager.CreateShipAt(ship, triggerer, p, true));
            }
            foreach (string ship in RemnantShipsToSpawn)
            {
                Ship tomake = ResourceManager.CreateShipAt(ship, EmpireManager.Remnants, p, true);
                tomake.GetAI().DefaultAIState = AIState.Exterminate;
            }
        }

	    private void BuildingActions(Planet p, PlanetGridSquare eventLocation)
	    {
            if (RemoveTrigger)
            {
                p.BuildingList.Remove(eventLocation.building);
                eventLocation.building = null;
            }
            if (!string.IsNullOrEmpty(ReplaceWith))
            {
                eventLocation.building = ResourceManager.GetBuilding(ReplaceWith);
                p.BuildingList.Add(eventLocation.building);
            }
        }

	    private bool SetRandomPlanet()
	    {
	        if (!SelectRandomPlanet) return false;
            Array<Planet> potentials = new Array<Planet>();
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
                SetPlanet(potentials[RandomMath.InRange(potentials.Count)]);
                return true;
            }
            return false;	        
	    }
	    private void TroopActions(Empire triggerer, Planet p, PlanetGridSquare eventLocation)
	    {
            if (TroopsGranted != null)
            {
                foreach (string troopname in TroopsGranted)
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
            if (TroopsToSpawn != null)
            {
                foreach (string troopname in TroopsToSpawn)
                {
                    Troop t = ResourceManager.CreateTroop(ResourceManager.TroopsDict[troopname],
                        EmpireManager.Unknown);
                    t.SetOwner(EmpireManager.Unknown);
                    if (p.AssignTroopToNearestAvailableTile(t, eventLocation))
                    {
                        continue;
                    }
                    p.AssignTroopToTile(t);
                }
            }
        }

	    public bool InValidOutcome(Empire triggerer)
	    {
	        return this.OnlyTriggerOnce && this.AlreadyTriggered && triggerer.isPlayer;

	    }
	    public void CheckOutComes(Planet p,  PlanetGridSquare eventLocation, Empire triggerer, EventPopup popup)
	    {
            //artifact setup
            if (GrantArtifact)
	        {
                //Find all available artifacts
                Array<Artifact> potentials = new Array<Artifact>();
                foreach (var kv in ResourceManager.ArtifactsDict)
                {
                    if (kv.Value.Discovered)
                    {
                        continue;
                    }
                    potentials.Add(kv.Value);
                }
                //if no artifact is available just give them money 
                if (potentials.Count <= 0)
                {
                    MoneyGranted = 500;
                }
                else
                {
                    //choose a random available artifact and process it. 
                    Artifact chosenArtifact = potentials[RandomMath.InRange(potentials.Count)];
                    triggerer.data.OwnedArtifacts.Add(chosenArtifact);
                    ResourceManager.ArtifactsDict[chosenArtifact.Name].Discovered = true;
                    SetArtifact(chosenArtifact);
                    chosenArtifact.CheckGrantArtifact(triggerer, this, popup);
                }                
	        }
            //Generic grants
	        FlatGrants(triggerer);
            TechGrants(triggerer);
	        ShipGrants(triggerer, p);
	        if (BeginArmageddon)
	        {
	            GlobalStats.RemnantArmageddon = true;
	        }
            //planet triggered events
	        if (p != null)
	        {
	            BuildingActions(p, eventLocation);
	            TroopActions(triggerer, p, eventLocation);
                return;	            
	        }

	        //events that trigger on other planets
	        if(!SetRandomPlanet()) return;
	        p = this._selectedPlanet;
                        
            if (eventLocation == null)
	        {
                eventLocation = p.TilesList[17];
	        }

            BuildingActions(p, eventLocation);
            TroopActions(triggerer, p, eventLocation);
	    }
	}
}