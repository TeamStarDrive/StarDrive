using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Ships;
using Ship_Game.Universe;

namespace Ship_Game
{
    public sealed class Outcome
    {
        private Planet SelectedPlanet; 
        public int Chance;
        private Artifact GrantedArtifact;
        public Array<string> TroopsToSpawn;
        public Array<string> FriendlyShipsToSpawn;
        public Array<string> PirateShipsToSpawn;
        public Array<string> RemnantShipsToSpawn;
        public string SecretTechDiscovered;
        public string TitleText;
        public string UnlockTech;
        public bool WeHadIt;
        public bool GrantArtifact;
        public bool RemoveTrigger = true;
        public string ReplaceWith = "";
        public string DescriptionText;
        public int MoneyGranted;
        public Array<string> TroopsGranted;
        public float IndustryBonus;
        public float ScienceBonus;
        public bool SelectRandomPlanet;
        public bool OnlyTriggerOnce;
        public bool AlreadyTriggered;
        public int NumTilesToMakeHabitable;
        public int NumTilesToMakeUnhabitable;
        public float ChangeBaseFertility;
        public float ChangeBaseMaxFertility;
        public float ChangeRichness;

        // Text to show on the confirmation button
        public string ConfirmText;

        // relative path to Image asset to be used while displaying the event
        public string Image;

        public string LocalizedTitle => Localizer.Token(TitleText);
        public string LocalizedDescr => Localizer.Token(DescriptionText);

        public Outcome()
        {
        }

        public Artifact GetArtifact()
        {
            return GrantedArtifact;
        }

        public Planet GetPlanet()
        {
            return SelectedPlanet;
        }

        public void SetArtifact(Artifact art)
        {
            GrantedArtifact = art;
        }

        public void SetPlanet(Planet p)
        {
            SelectedPlanet = p;
        }

        private void FlatGrants(Empire triggerEmpire)
        {
            triggerEmpire.AddMoney(MoneyGranted);
            triggerEmpire.data.Traits.ResearchMod += ScienceBonus;
            triggerEmpire.data.Traits.ProductionMod += IndustryBonus;
        }

        private void TechGrants(Empire triggeredBy)
        {
            if (SecretTechDiscovered != null)
            {
                triggeredBy.SetEmpireTechDiscovered(SecretTechDiscovered);
            }
            if (UnlockTech != null)
            {
                TechEntry tech = triggeredBy.GetTechEntry(UnlockTech);
                if (tech.CanBeResearched)
                {
                    //triggeredBy.UnlockTech(tech, TechUnlockType.Event); // FB making secret tech need research instead
                    tech.SetDiscovered(triggeredBy);
                }
                else
                {
                    WeHadIt = true;
                }
            }
        }

        void PlanetGrants(Planet p, PlanetGridSquare eventTile)
        {
            if (p == null)
                return;

            MakeTilesHabitable(p, eventTile);
            MakeTilesUnhabitable(p, eventTile);
            p.AddBaseFertility(ChangeBaseFertility);
            p.AddMaxBaseFertility(ChangeBaseMaxFertility);
            p.MineralRichness = (p.MineralRichness + ChangeRichness).LowerBound(0);
        }

        void MakeTilesHabitable(Planet p, PlanetGridSquare eventTile)
        {
            for (int i = 0; i < NumTilesToMakeHabitable; i++)
            {
                PlanetGridSquare tile = p.Random.RandItemFiltered(p.TilesList, t => !t.Habitable && t != eventTile);
                if (tile == null)
                    break;
                p.MakeTileHabitable(tile);
            }
        }

        void MakeTilesUnhabitable(Planet p, PlanetGridSquare eventTile)
        {
            for (int i = 0; i < NumTilesToMakeUnhabitable; i++)
            {
                PlanetGridSquare tile = p.Random.RandItemFiltered(p.TilesList, t => t.Habitable && !t.Biosphere && t != eventTile);
                if (tile == null)
                    break;

                if (p.Owner == p.Universe.Player && tile.BuildingOnTile && !tile.VolcanoHere)
                    p.Universe.Notifications.AddBuildingDestroyed(p, tile.Building, GameText.WasDestroyedInAnExploration);

                p.DestroyTile(tile);
            }
        }

        void ShipGrants(Planet p, Empire triggeredBy)
        {
            p = p ?? triggeredBy.Capital;
            if (p == null)
            {
                Log.Error("ShipGrants failed: no planet");
                return;
            }

            var universe = triggeredBy.Universe;
            foreach (string shipName in FriendlyShipsToSpawn)
            {
                Ship.CreateShipNearPlanet(universe, shipName, triggeredBy, p, doOrbit: true);
            }

            foreach (string shipName in RemnantShipsToSpawn)
            {
                Ship ship = Ship.CreateShipNearPlanet(universe, shipName, p.Universe.Remnants, p, doOrbit: true);
                ship.AI.DefaultAIState = AIState.Exterminate;
            }

            if (PirateShipsToSpawn.Count == 0 || p.Universe.PirateFactions.Length == 0)
            {
                Empire pirates = triggeredBy.Random.RandItem(p.Universe.PirateFactions);
                foreach (string shipName in PirateShipsToSpawn)
                {
                    Ship.CreateShipNearPlanet(universe, shipName, pirates, p, doOrbit: true);
                }
            }
        }

        void BuildingActions(Planet p, PlanetGridSquare eventLocation)
        {
            if (p == null || eventLocation == null)
                return;

            if (RemoveTrigger)
            {
                p.DestroyBuildingOn(eventLocation);
            }

            if (!string.IsNullOrEmpty(ReplaceWith))
            {
                eventLocation.PlaceBuilding(ResourceManager.CreateBuilding(p, ReplaceWith), p);
            }
        }

        bool SetRandomPlanet(UniverseState u)
        {
            if (!SelectRandomPlanet) return false;
            Array<Planet> potentials = new Array<Planet>();
            foreach (Planet rp in u.Planets)
            {
                if (rp.Habitable && rp.Owner == null)
                {
                    potentials.Add(rp);
                }
            }
            if (potentials.Count > 0)
            {
                SetPlanet(u.Random.RandItem(potentials));
                return true;
            }

            return false;
        }

        void TroopActions(Empire triggeredBy, Planet p, PlanetGridSquare eventLocation)
        {
            if (TroopsGranted != null)
            {
                foreach (string troopName in TroopsGranted)
                {
                    if (ResourceManager.TryCreateTroop(troopName, triggeredBy, out Troop t) &&
                        !t.TryLandTroop(p, eventLocation))
                    {
                        t.Launch(p);
                    }
                }
            }

            if (TroopsToSpawn != null)
            {
                foreach (string troopName in TroopsToSpawn)
                {
                    if (p.GetFreeTiles(p.Universe.Unknown) == 0 && !p.BumpOutTroop(p.Universe.Unknown))
                    {
                        Log.Warning($"Could not bump out any troop from {p.Name} after event");
                        return;
                    }

                    if (!ResourceManager.TryCreateTroop(troopName, p.Universe.Unknown, out Troop t))
                        continue;

                    if (!t.TryLandTroop(p, eventLocation))
                    {
                        t.SetOwner(p.Universe.Remnants);
                        t.Launch(p);
                        Log.Warning($"Troop spawned but could not be landed on {p.Name} after event. Transformed to Remnant.");
                    }
                }
            }
        }

        public void CheckOutComes(Planet p, PlanetGridSquare eventLocation, Empire triggeredBy, EventPopup popup)
        {
            //artifact setup
            if (GrantArtifact)
            {
                //Find all available artifacts
                Array<Artifact> potentials = new Array<Artifact>();
                foreach (var kv in ResourceManager.ArtifactsDict)
                {
                    if (!kv.Value.Discovered)
                    {
                        potentials.Add(kv.Value);
                    }
                }
                //if no artifact is available just give them money
                if (potentials.Count <= 0)
                {
                    MoneyGranted = 500;
                }
                else
                {
                    //choose a random available artifact and process it.
                    Artifact chosenArtifact = triggeredBy.Random.RandItem(potentials);
                    triggeredBy.data.OwnedArtifacts.Add(chosenArtifact);
                    ResourceManager.ArtifactsDict[chosenArtifact.Name].Discovered = true;
                    SetArtifact(chosenArtifact);
                    chosenArtifact.CheckGrantArtifact(triggeredBy, this, popup);
                }
            }

            //Generic grants
            FlatGrants(triggeredBy);
            TechGrants(triggeredBy);
            ShipGrants(p, triggeredBy);
            PlanetGrants(p, eventLocation);

            //planet triggered events
            if (p != null)
            {
                BuildingActions(p, eventLocation);
                TroopActions(triggeredBy, p, eventLocation);
            }
            else if (SetRandomPlanet(triggeredBy.Universe)) //events that trigger on other planets
            {
                p = SelectedPlanet;

                if (eventLocation == null)
                {
                    eventLocation = p.TilesList[p.TilesList.Count / 2];
                }

                BuildingActions(p, eventLocation);
                TroopActions(triggeredBy, p, eventLocation);
            }
        }
    }
}
