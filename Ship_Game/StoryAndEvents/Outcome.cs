using Ship_Game.AI;
using Ship_Game.Ships;

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
                if (!tech.Unlocked)
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
                var potentialTiles = p.TilesList.Filter(t => !t.Habitable && t != eventTile);
                if (potentialTiles.Length == 0)
                    break;

                PlanetGridSquare tile = potentialTiles.RandItem();
                p.MakeTileHabitable(tile);
            }
        }

        void MakeTilesUnhabitable(Planet p, PlanetGridSquare eventTile)
        {
            for (int i = 0; i < NumTilesToMakeUnhabitable; i++)
            {
                var potentialTiles = p.TilesList.Filter(t => t.Habitable && !t.Biosphere && t != eventTile);
                if (potentialTiles.Length == 0)
                    break;

                PlanetGridSquare tile = potentialTiles.RandItem();
                if (p.Owner == EmpireManager.Player && tile.BuildingOnTile && !tile.VolcanoHere)
                    p.Universe.NotificationManager.AddBuildingDestroyed(p, tile.Building, Localizer.Token(GameText.WasDestroyedInAnExploration));

                p.DestroyTile(tile);
            }
        }

        private void ShipGrants(Empire triggeredBy, Planet p)
        {
            var universe = p.Universe;
            foreach (string shipName in FriendlyShipsToSpawn)
            {
                Ship.CreateShipAt(universe, shipName, triggeredBy, p, true);
            }

            foreach (string shipName in RemnantShipsToSpawn)
            {
                Ship ship = Ship.CreateShipAt(universe, shipName, EmpireManager.Remnants, p, true);
                ship.AI.DefaultAIState = AIState.Exterminate;
            }

            if (PirateShipsToSpawn.Count == 0 || EmpireManager.PirateFactions.Length == 0)
                return;

            Empire pirates = EmpireManager.PirateFactions.RandItem();
            foreach (string shipName in PirateShipsToSpawn)
            {
                Ship ship = Ship.CreateShipAt(universe, shipName, pirates, p, true);
                ship?.AI.OrderToOrbit(p);
            }
        }

        private void BuildingActions(Planet p, PlanetGridSquare eventLocation)
        {
            if (RemoveTrigger)
            {
                p.DestroyBuildingOn(eventLocation);
            }

            if (!string.IsNullOrEmpty(ReplaceWith))
            {
                eventLocation.PlaceBuilding(ResourceManager.CreateBuilding(p.Universe, ReplaceWith), p);
            }
        }

        private bool SetRandomPlanet(UniverseScreen u)
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
                SetPlanet(potentials[RandomMath.InRange(potentials.Count)]);
                return true;
            }

            return false;
        }

        private void TroopActions(Empire triggeredBy, Planet p, PlanetGridSquare eventLocation)
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
                    if (p.GetFreeTiles(EmpireManager.Unknown) == 0 && !p.BumpOutTroop(EmpireManager.Unknown))
                    {
                        Log.Warning($"Could not bump out any troop from {p.Name} after event");
                        return;
                    }

                    if (!ResourceManager.TryCreateTroop(troopName, EmpireManager.Unknown, out Troop t))
                        continue;

                    if (!t.TryLandTroop(p, eventLocation))
                    {
                        t.SetOwner(EmpireManager.Remnants);
                        t.Launch(p);
                        Log.Warning($"Troop spawned but could not be landed on {p.Name} after event. Transformed to Remnant.");
                    }
                }
            }
        }

        public bool InValidOutcome(Empire triggeredBy)
        {
            return OnlyTriggerOnce && AlreadyTriggered && triggeredBy.isPlayer;
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
                    Artifact chosenArtifact = potentials[RandomMath.InRange(potentials.Count)];
                    triggeredBy.data.OwnedArtifacts.Add(chosenArtifact);
                    ResourceManager.ArtifactsDict[chosenArtifact.Name].Discovered = true;
                    SetArtifact(chosenArtifact);
                    chosenArtifact.CheckGrantArtifact(triggeredBy, this, popup);
                }
            }

            //Generic grants
            FlatGrants(triggeredBy);
            TechGrants(triggeredBy);
            ShipGrants(triggeredBy, p);
            PlanetGrants(p, eventLocation);

            //planet triggered events
            if (p != null)
            {
                BuildingActions(p, eventLocation);
                TroopActions(triggeredBy, p, eventLocation);
            }
            else if (SetRandomPlanet(p.Universe)) //events that trigger on other planets
            {
                p = SelectedPlanet;

                if (eventLocation == null)
                {
                    eventLocation = p.TilesList[17];
                }

                BuildingActions(p, eventLocation);
                TroopActions(triggeredBy, p, eventLocation);
            }
        }
    }
}
