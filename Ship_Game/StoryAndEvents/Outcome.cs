using Ship_Game.AI;
using Ship_Game.Ships;

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
        public Array<string> PirateShipsToSpawn;
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
            return _grantedArtifact;
        }

        public Planet GetPlanet()
        {
            return _selectedPlanet;
        }

        public void SetArtifact(Artifact art)
        {
            _grantedArtifact = art;
        }

        public void SetPlanet(Planet p)
        {
            _selectedPlanet = p;
        }

        private void FlatGrants(Empire triggerEmpire)
        {
            triggerEmpire.AddMoney(MoneyGranted);
            triggerEmpire.data.Traits.ResearchMod += ScienceBonus;
            triggerEmpire.data.Traits.ProductionMod += IndustryBonus;
        }

        private void TechGrants(Empire triggerer)
        {
            if (SecretTechDiscovered != null)
            {
                triggerer.SetEmpireTechDiscovered(SecretTechDiscovered);
            }
            if (UnlockTech != null)
            {
                TechEntry tech = triggerer.GetTechEntry(UnlockTech);
                if (!tech.Unlocked)
                {
                    //triggerer.UnlockTech(tech, TechUnlockType.Event); // FB making secret tech need research instead
                    tech.SetDiscovered(triggerer);
                }
                else
                {
                    WeHadIt = true;
                }
            }
        }

        private void ShipGrants(Empire triggerer ,Planet p)
        {
            foreach (string shipName in FriendlyShipsToSpawn)
            {
                triggerer.Pool.ForcePoolAdd(Ship.CreateShipAt(shipName, triggerer, p, true));
            }
            foreach (string shipName in RemnantShipsToSpawn)
            {
                Ship ship = Ship.CreateShipAt(shipName, EmpireManager.Remnants, p, true);
                ship.AI.DefaultAIState = AIState.Exterminate;
            }

            if (PirateShipsToSpawn.Count == 0 || EmpireManager.PirateFactions.Length == 0)
                return;

            Empire pirates = EmpireManager.PirateFactions.RandItem();
            foreach (string shipName in PirateShipsToSpawn)
            {
                Ship ship = Ship.CreateShipAt(shipName, pirates, p, true);
                ship?.AI.OrderToOrbit(p);
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
                eventLocation.building = ResourceManager.CreateBuilding(ReplaceWith);
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
                    if (!rp.Habitable || rp.Owner != null)
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
                foreach (string troopName in TroopsGranted)
                {
                    Troop t = ResourceManager.CreateTroop(troopName, triggerer);
                    t.SetOwner(triggerer);
                    if (!t.TryLandTroop(p, eventLocation))
                        t.Launch(p);
                }
            }

            if (TroopsToSpawn != null)
            {
                foreach (string troopName in TroopsToSpawn)
                {
                    if (p.GetFreeTiles(EmpireManager.Unknown) == 0 && !p.BumpOutTroop(EmpireManager.Unknown))
                    {
                        Log.Warning($"Could not bump out any troop from {p.Name} after event");
                        return;
                    }

                    Troop t = ResourceManager.CreateTroop(troopName, EmpireManager.Unknown);
                    t.SetOwner(EmpireManager.Unknown);
                    if (!t.TryLandTroop(p, eventLocation))
                    {
                        t.SetOwner(EmpireManager.Remnants);
                        t.Launch(p);
                        Log.Warning($"Troop spawned but could not be landed on {p.Name} after event. Transformed to Remnant.");
                    }
                }
            }
        }

        public bool InValidOutcome(Empire triggerer)
        {
            return OnlyTriggerOnce && AlreadyTriggered && triggerer.isPlayer;
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
            p = _selectedPlanet;

            if (eventLocation == null)
            {
                eventLocation = p.TilesList[17];
            }

            BuildingActions(p, eventLocation);
            TroopActions(triggerer, p, eventLocation);
        }
    }
}