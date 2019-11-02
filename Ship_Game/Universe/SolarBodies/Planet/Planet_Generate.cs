using System;
using Microsoft.Xna.Framework;
using Ship_Game.Ships;

namespace Ship_Game
{
    using static RandomMath;
    public partial class Planet
    {

        public string LocalizedCategory
        {
            get
            {
                switch (Category)
                {
                    case PlanetCategory.Terran:   return Localizer.Token(1447);
                    case PlanetCategory.Barren:   return Localizer.Token(1448);
                    case PlanetCategory.GasGiant: return Localizer.Token(1449);
                    case PlanetCategory.Volcanic: return Localizer.Token(1450);
                    case PlanetCategory.Tundra:   return Localizer.Token(1451);
                    case PlanetCategory.Desert:   return Localizer.Token(1452);
                    case PlanetCategory.Steppe:   return Localizer.Token(1453);
                    case PlanetCategory.Swamp:    return Localizer.Token(1454);
                    case PlanetCategory.Ice:      return Localizer.Token(1455);
                    case PlanetCategory.Oceanic:  return Localizer.Token(1456);
                    default: return "";
                }
            }
        }

        public string LocalizedRichness => $"{LocalizedCategory} {RichnessText}";

        public string CategoryName
        {
            get
            {
                if (Category == PlanetCategory.GasGiant)
                    return "Gas Giant";
                return Category.ToString();
            }
        }

        public string PlanetTileId
        {
            get
            {
                if (Type.PlanetTile.NotEmpty())
                    return Type.PlanetTile;
                return Category.ToString();

            }
        }

        // this applies to any randomly generated planet
        // which is newly created and is not a HomeWorld
        public void InitNewMinorPlanet(PlanetType type, float scale, float preDefinedPop = 0)
        {
            GenerateNewFromPlanetType(type, scale, preDefinedPop);
            AddEventsAndCommodities();
        }

        // FB - this is a more comprehensive method of choosing planet type.
        // It gets the planet category by weights based on sun zone and then
        // randomize relevant planet types from the chose category
        // this reduces chances of terran planets and its configurable via SunZoneData.yaml
        static PlanetType ChooseTypeByWeight(SunZone sunZone)
        {
            PlanetCategory chosenCategory = ResourceManager.RandomPlanetCategoryFor(sunZone);
            return ResourceManager.RandomPlanet(chosenCategory);
        }

        public void RestorePlanetTypeFromSave(int planetId)
        {
            // we revert to random just in case people unload mods
            Type = ResourceManager.PlanetOrRandom(planetId);
        }

        public PlanetGridSquare FindTileUnderMouse(Vector2 mousePos)
            => TilesList.Find(pgs => pgs.ClickRect.HitTest(mousePos));

        void GenerateNewFromPlanetType(PlanetType type, float scale, float preDefinedPop = 0)
        {
            TilesList.Clear();
            Type = type;
            Scale = scale;

            if (Habitable)
            {
                float richness = RandomBetween(0.0f, 100f);
                if      (richness >= 92.5f) MineralRichness = RandomBetween(2.00f, 2.50f);
                else if (richness >= 85.0f) MineralRichness = RandomBetween(1.50f, 2.00f);
                else if (richness >= 25.0f) MineralRichness = RandomBetween(0.75f, 1.50f);
                else if (richness >= 12.5f) MineralRichness = RandomBetween(0.25f, 0.75f);
                else if (richness < 12.5f)  MineralRichness = RandomBetween(0.10f, 0.25f);

                float habitableChance = GlobalStats.ActiveModInfo?.ChanceForCategory(Category)
                                        ?? Type.HabitableTileChance.Generate();

                SetTileHabitability(habitableChance, out int numHabitableTiles);
                if (preDefinedPop > 0)
                    BasePopPerTile = (int)(preDefinedPop * 1000 / numHabitableTiles);
                else
                    BasePopPerTile = (int)(type.PopPerTile.Generate() * scale);

                BaseFertility    = type.BaseFertility.Generate().Clamped(type.MinBaseFertility, 100.0f);
                BaseMaxFertility = BaseFertility;
            }
            else
                MineralRichness = 0.0f;
        }

        public void GeneratePlanetFromSystemData(SolarSystemData.Ring ringData, PlanetType type, float scale)
        {
            if (ringData.UniqueHabitat)
            {
                UniqueHab = true;
                UniqueHabPercent = ringData.UniqueHabPC;
            }

            InitNewMinorPlanet(type, scale, ringData.MaxPopDefined);

            if (ringData.Owner.NotEmpty())
            {
                Owner = EmpireManager.GetEmpireByName(ringData.Owner);
                Owner.AddPlanet(this);
                InitializeWorkerDistribution(Owner);
                Population = MaxPopulation;
                MineralRichness = 1f;
                colonyType = ColonyType.Core;
                SetBaseFertility(2f, 2f);
            }
        }

        public void GenerateNewHomeWorld(Empire owner, float preDefinedPop = 0)
        {
            Owner         = owner;
            Owner.Capital = this;
            Scale         = 1 * Owner.data.Traits.HomeworldSizeMultiplier; // base max pop is affected by scale
            Owner.AddPlanet(this);

            CreateHomeWorldEnvironment();
            SetTileHabitability(0, out _); // Create the homeworld's tiles without making them habitable yet
            SetHomeworldTiles();

            if (Owner.isPlayer)
                colonyType = ColonyType.Colony;

            CreateHomeWorldFertilityAndRichness();
            int numHabitableTiles = TilesList.Count(t => t.Habitable);
            CreateHomeWorldPopulation(preDefinedPop, numHabitableTiles);
            InitializeWorkerDistribution(Owner);
            FoodHere     = 100f;
            ProdHere     = 100f;
            HasSpacePort = true;
            if (!ParentSystem.OwnerList.Contains(Owner))
                ParentSystem.OwnerList.Add(Owner);

            CreateHomeWorldBuildings();
        }

        private void SetTileHabitability(float tileChance, out int numHabitableTiles)
        {
            numHabitableTiles = 0;

            TilesList.Clear();
            for (int x = 0; x < TileMaxX; ++x)
            {
                for (int y = 0; y < TileMaxY; ++y)
                {
                    bool habitableTile = RollDice(tileChance);
                    TilesList.Add(new PlanetGridSquare(x, y, null, habitableTile));
                    if (habitableTile)
                        ++numHabitableTiles;
                }
            }
        }

        private void SetHomeworldTiles()
        {
            for (int i = 0; i < 28; ++i)
            {
                PlanetGridSquare tile = RandItem(TilesList.Filter(t => !t.Habitable));
                tile.Habitable = true;
            }
        }

        private void CreateHomeWorldPopulation(float preDefinedPop, int numHabitableTiles)
        {
            // Homeworld Pop is always 14 (or if defined else in the xml) multiplied by scale (homeworld size mod)
            float envMultiplier = 1 / Owner.RacialEnvModifer(Owner.data.PreferredEnv);
            float maxPop        = preDefinedPop > 0 ? preDefinedPop * 1000 : 14000;
            BasePopPerTile      = (int)(maxPop * envMultiplier / numHabitableTiles) * Scale;
            UpdateMaxPopulation();
            Population          = MaxPopulation;
        }

        private void CreateHomeWorldFertilityAndRichness()
        {
            // Set the base fertility so it always corresponds to preferred env plus any modifiers from traits
            float baseMaxFertility = (2 + Owner.data.Traits.HomeworldFertMod) / Owner.RacialEnvModifer(Owner.data.PreferredEnv);
            SetBaseFertilityMinMax(baseMaxFertility);

            MineralRichness = 1f + Owner.data.Traits.HomeworldRichMod;
        }

        private void CreateHomeWorldEnvironment()
        {
            PlanetCategory preferred = Owner.data.PreferredEnv == PlanetCategory.Other ? PlanetCategory.Terran
                                                                                       : Owner.data.PreferredEnv;

            Type = ResourceManager.RandomPlanet(preferred);
            Zone = SunZone.Any;
        }

        private void CreateHomeWorldBuildings()
        {
            ResourceManager.CreateBuilding(Building.CapitalId).SetPlanet(this);
            ResourceManager.CreateBuilding(Building.SpacePortId).SetPlanet(this);
        }

        private void ApplyTerraforming() // Added by Fat Bastard
        {
            if (TerraformToAdd.LessOrEqual(0) || Owner == null)
            {
                TerraformPoints = 0;
                return; // No Terraformers or No owner (Terraformers cannot continue working)
            }

            // Remove negative effect buildings
            if (ScrapNegativeEnvBuilding())
                return;

            // First, make un-habitable tiles habitable
            if (TerraformTiles()) 
                return;

            // Then, if all tiles are habitable, proceed to Planet Terraform
            if (TerraformPlanet())
                return;

            // Then, remove any existing biospheres from the new heaven
            TerraformBioSpheres();
        }

        public bool TilesToTerraform      => TilesList.Any(t => !t.Habitable && !t.Biosphere);
        public bool BioSpheresToTerraform => TilesList.Any(t => t.Biosphere);

        private bool TerraformTiles()
        {

            if (!TilesToTerraform)
                return false; // no tiles need terraforming

            TerraformPoints += TerraformToAdd * 5; // Terraforming a tile is faster than the whole planet
            if (TerraformPoints.GreaterOrEqual(1))
                CompleteTileTerraforming(TilesList.Filter(t => !t.Habitable && !t.Biosphere));

            return true;
        }

        private bool TerraformPlanet()
        {
            if (Category == Owner.data.PreferredEnv && BaseMaxFertility.GreaterOrEqual(TerraformTargetFertility))
                return false;

            TerraformPoints += TerraformToAdd;
            if (TerraformPoints.GreaterOrEqual(1))
            {
                CompletePlanetTerraform();
                return false;
            }

            return true;
        }

        private bool ScrapNegativeEnvBuilding()
        {
            if (Owner.IsCybernetic)
                return false; // Cybernetic races do not care for environmental harmful facilities. 

            var negativeEnvBuildings = BuildingList.Filter(b => b.MaxFertilityOnBuild < 0 && !b.IsBiospheres);
            if (negativeEnvBuildings.Length > 0)
            {
                ScrapBuilding(negativeEnvBuildings[0]);
                return true;
            }

            return false;
        }

        private void TerraformBioSpheres()
        {
            if (!BioSpheresToTerraform)
            {
                RemoveTerraformers();
                if (Owner.isPlayer) // Notify player that the planet was terraformed
                    Empire.Universe.NotificationManager.AddRandomEventNotification(
                        Name + " " + Localizer.Token(1971), Type.IconPath, "SnapToPlanet", this);
                return;
            }

            TerraformPoints += TerraformToAdd * 10; // Terraforming Biospheres is much faster than the whole planet
            if (TerraformPoints.GreaterOrEqual(1))
                CompleteTileTerraforming(TilesList.Filter(t => t.Biosphere));
        }

        private void CompleteTileTerraforming(PlanetGridSquare[] possibleTiles)
        {
            if (possibleTiles.Length > 0)
            {
                PlanetGridSquare tile = RandItem(possibleTiles);
                MakeTileHabitable(tile);
            }

            UpdateTerraformPoints(0); // Start terraforming a new tile
        }

        private void CompletePlanetTerraform()
        {
            Terraform(Owner.data.PreferredEnv);
            UpdateTerraformPoints(0);
            if (Owner.NonCybernetic)
            {
                float fertilityAfterTerraform = Fertility; // setting the fertility to what the empire saw before terraform. It will slowly rise.
                BaseMaxFertility              = Math.Max(TerraformTargetFertility, BaseMaxFertility);
                BaseFertility                 = fertilityAfterTerraform;
            }
            else
                BaseMaxFertility = 0; // cybernetic races will reduce fertility to 0 on their terraformed worlds, they dont need it.

            string messageText = Localizer.Token(1920);
            if (!BioSpheresToTerraform) 
            {
                RemoveTerraformers();
                messageText = Localizer.Token(1971);
            }

            if (Owner.isPlayer) // Notify player that the planet was terraformed
                Empire.Universe.NotificationManager.AddRandomEventNotification(
                    Name + " " + messageText, Type.IconPath, "SnapToPlanet", this);
        }

        private void RemoveTerraformers()
        {
            foreach (PlanetGridSquare tile in TilesList)
            {
                if (tile.building?.PlusTerraformPoints > 0)
                    ScrapBuilding(tile.building);
            }
        }

        public void UpdateTerraformPoints(float points)
        {
            TerraformPoints = points;
        }

        private void UpdateOrbitalsMaint()
        {
            OrbitalsMaintenance = 0;
            foreach (Ship orbital in OrbitalStations.Values)
            {
                OrbitalsMaintenance += orbital.GetMaintCost(Owner);
            }
        }

        public void MakeTileHabitable(PlanetGridSquare tile)
        {
            if (tile.Biosphere)
                ClearBioSpheresFromList(tile);

            tile.Habitable = true;
            UpdateMaxPopulation();
        }

        // Refactored by Fat Bastard && RedFox
        private void Terraform(PlanetCategory newCategory, bool improve = true)
        {
            if (Category == newCategory)
                return; // A planet with the same category was Terraformed (probably to increase fertility)

            Type                  = ResourceManager.RandomPlanet(newCategory);
            float newBasePopMax   = Type.PopPerTile.Generate();

            // Dont let the BasePopMax be lower if improving or higher if degrading
            BasePopPerTile = improve ? Math.Max(BasePopPerTile, newBasePopMax) 
                                 : Math.Min(BasePopPerTile, newBasePopMax);

            if (!improve)
                ReCalculateHabitableChances();

            CreatePlanetSceneObject(Empire.Universe);
            UpdateDescription();
            UpdateMaxPopulation();
        }

        private void ReCalculateHabitableChances()
        {
            float habitableChance = Type.HabitableTileChance.Generate();
            foreach (PlanetGridSquare pgs in TilesList)
            {
                if (pgs.Biosphere)
                    continue; // Bio Spheres dont degrade

                switch (Category)
                {
                    case PlanetCategory.Barren:
                    case PlanetCategory.Swamp:
                    case PlanetCategory.Ice:
                    case PlanetCategory.Oceanic:
                    case PlanetCategory.Desert:
                    case PlanetCategory.Steppe:
                    case PlanetCategory.Tundra:
                    case PlanetCategory.Terran:
                        if (!RollDice(habitableChance))
                            DestroyTile(pgs);
                        continue;
                    default:
                        continue;
                }
            }
        }

        protected void AddEventsAndCommodities()
        {
            foreach (RandomItem item in ResourceManager.RandomItemsList)
            {
                (float chance, float maxInstance) = item.ChanceAndMaxInstance(Category);
                SpawnRandomItem(item, chance, maxInstance);
            }
            AddTileEvents();
        }

        float QualityForRemnants()
        {
            float quality = BaseFertility*2 + MineralRichness + MaxPopulationBillionFor(EmpireManager.Remnants);
            //Boost the quality score for planets that are very rich, or very fertile
            if (BaseFertility*2 > 1.6)   ++quality;
            if (MineralRichness > 1.6) ++quality;
            return quality;
        }

        public void GenerateRemnantPresence()
        {
            float quality = QualityForRemnants();
            int d100      = RollDie(100);

            switch (GlobalStats.ExtraRemnantGS) // Added by Gretman, Refactored by FB (including all remnant methods)
            {
                case ExtraRemnantPresence.Rare:       RareRemnantPresence(quality, d100);       break;
                case ExtraRemnantPresence.Normal:     NormalRemnantPresence(quality, d100);     break;
                case ExtraRemnantPresence.More:       MoreRemnantPresence(quality, d100);       break;
                case ExtraRemnantPresence.MuchMore:   MuchMoreRemnantPresence(quality, d100);   break;
                case ExtraRemnantPresence.Everywhere: EverywhereRemnantPresence(quality, d100); break;
            }
        }

        void RareRemnantPresence(float quality, int d100)
        {
            if (quality > 8f && d100 >= 70)
                AddMajorRemnantShips(); // RedFox, changed the rare remnant to Major
        }

        void NormalRemnantPresence(float quality, int d100)
        {
            if (quality > 15f)
            {
                if (d100 >= 10) AddMinorRemnantShips();
                if (d100 >= 40) AddMajorRemnantShips();
                if (d100 >= 45) AddSupportRemnantShips();
                if (d100 >= 95) AddTorpedoRemnantShips();
            }
            else if (quality > 12f)
            {
                if (d100 >= 20) AddMinorRemnantShips();
                if (d100 >= 40) AddMiniRemnantShips();
                if (d100 >= 80) AddMajorRemnantShips();
                if (d100 >= 85) AddSupportRemnantShips();
            }
            else if (quality > 6f)
            {
                if (d100 >= 20) AddMiniRemnantShips();
                if (d100 >= 60) AddMinorRemnantShips();
                if (d100 >= 80) AddMinorRemnantShips();
                if (d100 >= 90) AddSupportRemnantShips();
            }
        }

        void MoreRemnantPresence(float quality, int d100)
        {
            NormalRemnantPresence(quality, d100);
            if (quality >= 12f)
            {
                if (d100 >= 15) AddMinorRemnantShips();
                if (d100 >= 30) AddMajorRemnantShips();
                if (d100 >= 45) AddSupportRemnantShips();
                if (d100 >= 85) AddCarrierRemnantShips();
            }
            else if (quality >= 10f)
            {
                if (d100 >= 35) AddMinorRemnantShips();
                if (d100 >= 65) AddMajorRemnantShips();
                if (d100 >= 85) AddSupportRemnantShips();
            }
            else if (quality >= 8f && d100 >= 50)
                AddMinorRemnantShips();
        }

        void MuchMoreRemnantPresence(float quality, int d100)
        {
            MoreRemnantPresence(quality, d100);
            if (quality >= 12f)
            {
                AddMajorRemnantShips();
                if (d100 > 10) AddMinorRemnantShips();
                if (d100 > 20) AddSupportRemnantShips();
                if (d100 > 50) AddCarrierRemnantShips();
                if (d100 > 85) AddTorpedoRemnantShips();
            }
            else if (quality >= 10f)
            {
                if (d100 >= 25) AddMinorRemnantShips();
                if (d100 >= 30) AddSupportRemnantShips();
                if (d100 >= 45) AddMinorRemnantShips();
                if (d100 >= 80) AddMiniRemnantShips();
            }
            else if (quality >= 8f)
            {
                if (d100 >= 25) AddMinorRemnantShips();
                if (d100 >= 50) AddSupportRemnantShips();
                if (d100 >= 75) AddMajorRemnantShips();
            }
            else if (quality >= 6f)
            {
                if (d100 >= 50) AddMinorRemnantShips();
                if (d100 >= 75) AddMiniRemnantShips();
            }
            else if (quality > 4f && d100 >= 50)
                AddMiniRemnantShips();
        }

        void EverywhereRemnantPresence(float quality, int d100)
        {
            MuchMoreRemnantPresence(quality, d100);
            if (quality >= 12f)
            {
                AddMajorRemnantShips();
                AddMinorRemnantShips();
                AddSupportRemnantShips();
                if (d100 >= 50) AddCarrierRemnantShips();
                if (d100 >= 70) AddTorpedoRemnantShips();
                if (d100 >= 90) AddCarrierRemnantShips();
            }
            else if (quality >= 10f)
            {
                AddMajorRemnantShips();
                if (d100 >= 40) AddSupportRemnantShips();
                if (d100 >= 60) AddCarrierRemnantShips();
                if (d100 >= 80) AddTorpedoRemnantShips();
                if (d100 >= 95) AddCarrierRemnantShips();
            }
            else if (quality >= 8f)
            {
                AddMinorRemnantShips();
                if (d100 >= 50) AddSupportRemnantShips();
                if (d100 >= 90) AddCarrierRemnantShips();
            }
            else if (quality >= 6f)
            {
                if (d100 >= 30) AddMinorRemnantShips();
                if (d100 >= 50) AddMiniRemnantShips();
                if (d100 >= 70) AddSupportRemnantShips();
            }
            else if (quality >= 4f)
            {
                if (d100 >= 50) AddMiniRemnantShips();
                if (d100 >= 90) AddMiniRemnantShips();
            }
            if (quality > 2f && d100 > 50)
                AddMiniRemnantShips();
        }

        void AddMajorRemnantShips()
        {
            AddMinorRemnantShips();
            AddRemnantGuardians(2, "Xeno Fighter");
            AddRemnantGuardians(1, "Heavy Drone");
            AddRemnantGuardians(1, "Ancient Assimilator");
        }

        void AddMinorRemnantShips()
        {
            int numXenoFighters = RollDie(5) + 1;
            int numDrones       = RollDie(3);

            AddRemnantGuardians(numXenoFighters, "Xeno Fighter");
            AddRemnantGuardians(numDrones, "Heavy Drone");
        }

        void AddMiniRemnantShips()  //Added by Gretman
        {
            int numXenoFighters = RollDie(3);

            AddRemnantGuardians(numXenoFighters, "Xeno Fighter");
            AddRemnantGuardians(1, "Heavy Drone");
        }

        void AddSupportRemnantShips()  //Added by Gretman
        {
            int numSupportDrones = RollDie(4);
            AddRemnantGuardians(numSupportDrones, "Support Drone");
        }

        void AddCarrierRemnantShips()  //Added by Gretman
        {
            AddRemnantGuardians(1, "Ancient Carrier");
            if (RollDice(20)) // 20% chance for another carrier
                AddRemnantGuardians(1, "Ancient Carrier");
        }

        void AddTorpedoRemnantShips()  //Added by Gretman
        {
            AddRemnantGuardians(1, "Ancient Torpedo Cruiser");
            if (RollDice(10)) // 10% chance for another torpedo cruiser
                AddRemnantGuardians(1, "Ancient Torpedo Cruiser");
        }

        void AddRemnantGuardians(int numShips, string shipName)
        {
            for (int i = 0; i < numShips; ++i)
                Guardians.Add(shipName);
        }
    }
}
