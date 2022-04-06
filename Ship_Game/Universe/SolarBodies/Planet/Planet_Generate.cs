using Microsoft.Xna.Framework;
using Ship_Game.Universe.SolarBodies;

namespace Ship_Game
{
    using static RandomMath;
    public partial class Planet
    {
        // This is true if the empire can build capital on this world
        // There can be several home worlds if federating, allowing multiple capitals
        // But if a planet is taken in combat, it will not be a homeworld anymore
        // If an empire loses a homeworld, it can designate a new one and build a new capital
        // And the IsHomeworld for this planet will be set
        // If the original Capital of the race is taken by this race, the Capital will be
        // moved from its current planet to the original one (If the planet is not a capital by origin.
        // The `Planet Capital` var is used for this)
        public bool IsHomeworld { get; private set; } 

        public void SetHomeworld(bool value)
        {
            IsHomeworld = value;
        }

        public static string TextCategory(PlanetCategory category)
        {
            switch (category)
            {
                case PlanetCategory.Terran:   return Localizer.Token(GameText.Terran);
                case PlanetCategory.Barren:   return Localizer.Token(GameText.Barren);
                case PlanetCategory.GasGiant: return Localizer.Token(GameText.GasGiant);
                case PlanetCategory.Volcanic: return Localizer.Token(GameText.Volcanic);
                case PlanetCategory.Tundra:   return Localizer.Token(GameText.Tundra);
                case PlanetCategory.Desert:   return Localizer.Token(GameText.Desert);
                case PlanetCategory.Steppe:   return Localizer.Token(GameText.Steppe);
                case PlanetCategory.Swamp:    return Localizer.Token(GameText.Swamp);
                case PlanetCategory.Ice:      return Localizer.Token(GameText.Ice);
                case PlanetCategory.Oceanic:  return Localizer.Token(GameText.Oceanic);
                default: return "";
            }
        }

        public string LocalizedCategory => TextCategory(Category);

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
            return ResourceManager.Planets.RandomPlanet(chosenCategory);
        }

        public PlanetGridSquare FindTileUnderMouse(Vector2 mousePos)
            => TilesList.Find(pgs => pgs.ClickRect.HitTest(mousePos));

        // preDefinedPop is in millions
        public void GenerateNewFromPlanetType(PlanetType type, float scale, float preDefinedPop = 0)
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

                float habitableChance = Type.HabitableTileChance.Generate();

                SetTileHabitability(habitableChance, out int numHabitableTiles);
                if (preDefinedPop > 0)
                    BasePopPerTile = (int)(preDefinedPop * 1000 / numHabitableTiles);
                else
                    BasePopPerTile = ((int)(type.PopPerTile.Generate() * scale)).RoundUpToMultipleOf(10);

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
            Scale         = 1 * Owner.data.Traits.HomeworldSizeMultiplier; // base max pop is affected by scale
            IsHomeworld   = true;
            Owner.SetCapital(this);
            Owner.AddPlanet(this);

            CreateHomeWorldEnvironment();
            SetTileHabitability(0, out _); // Create the homeworld's tiles without making them habitable yet
            SetHomeworldTiles();
            ResetGarrisonSize();

            if (OwnerIsPlayer)
                colonyType = ColonyType.Colony;

            CreateHomeWorldFertilityAndRichness();
            int numHabitableTiles = TilesList.Count(t => t.Habitable);
            CreateHomeWorldPopulation(preDefinedPop, numHabitableTiles);
            InitializeWorkerDistribution(Owner);
            HasSpacePort = true;
            if (!ParentSystem.OwnerList.Contains(Owner))
                ParentSystem.OwnerList.Add(Owner);

            CreateHomeWorldBuildings();
        }

        void SetTileHabitability(float tileChance, out int numHabitableTiles)
        {
            numHabitableTiles = 0;

            TilesList.Clear();
            for (int x = 0; x < TileMaxX; ++x)
            {
                for (int y = 0; y < TileMaxY; ++y)
                {
                    bool habitableTile = RollDice(tileChance);
                    bool terraformable = !habitableTile && RollDice(25) || habitableTile;
                    TilesList.Add(new PlanetGridSquare(x, y, null, habitableTile, terraformable));
                    if (habitableTile)
                        ++numHabitableTiles;
                }
            }
        }

        void SetHomeworldTiles()
        {
            for (int i = 0; i < 28; ++i)
            {
                PlanetGridSquare tile = RandItem(TilesList.Filter(t => !t.Habitable));
                tile.Habitable = true;
            }
        }

        void CreateHomeWorldPopulation(float preDefinedPop, int numHabitableTiles)
        {
            // Homeworld Pop is always 14 (or if defined else in the xml) multiplied by scale (homeworld size mod)
            float envMultiplier = 1 / Empire.PreferredEnvModifier(Owner);
            float maxPop        = preDefinedPop > 0 ? preDefinedPop * 1000 : 14000;
            BasePopPerTile      = (int)(maxPop * envMultiplier / numHabitableTiles) * Scale;
            UpdateMaxPopulation();
            Population          = MaxPopulation;
        }

        void CreateHomeWorldFertilityAndRichness()
        {
            // Set the base fertility so it always corresponds to preferred env plus any modifiers from traits
            float baseMaxFertility = (2 + Owner.data.Traits.HomeworldFertMod) / Empire.PreferredEnvModifier(Owner);
            SetBaseFertilityMinMax(baseMaxFertility);

            MineralRichness = 1f + Owner.data.Traits.HomeworldRichMod;
        }

        void CreateHomeWorldEnvironment()
        {
            PlanetCategory preferred = Owner.data.PreferredEnv == PlanetCategory.Other ? PlanetCategory.Terran
                                                                                       : Owner.data.PreferredEnv;

            Type = ResourceManager.Planets.RandomPlanet(preferred);
            Zone = SunZone.Any;
        }

        void CreateHomeWorldBuildings()
        {
            ResourceManager.CreateBuilding(this, Building.CapitalId).AssignBuildingToTilePlanetCreation(this, out _);
            ResourceManager.CreateBuilding(this, Building.SpacePortId).AssignBuildingToTilePlanetCreation(this, out _);
            Storage.Max = BuildingList.Sum(b => b.StorageAdded);
            FoodHere    = Storage.Max;
            ProdHere    = Storage.Max / 2;
            AllowInfantry = true; // for initialization only, before we reach planet Update
        }

        void ApplyTerraforming() // Added by Fat Bastard
        {
            if (!Terraformable)
                return;

            if (TerraformToAdd <= 0 || Owner == null)
            {
                TerraformPoints = 0;
                return; // No Terraformers or No owner (Terraformers cannot continue working)
            }

            // First, remove Volcanoes
            if (TerraformVolcanoes())
                return;

            // Then, make un-habitable terraformable tiles habitable
            if (TerraformTiles()) 
                return;

            // Then, if all tiles are habitable and Terraforming Level is 3, proceed to Planet Terraform
            TerraformPlanet();
        }

        public bool HasTilesToTerraform     => TilesList.Any(t => t.CanTerraform);
        public bool HasVolcanoesToTerraform => TilesList.Any(t => t.VolcanoHere);
        public bool BioSpheresToTerraform   => TilesList.Any(t => t.BioCanTerraform);
        public int TerraformerLimit         => TilesList.Count(t => t.CanTerraform)/2 + 2;

        bool TerraformVolcanoes()
        {
            if (!HasVolcanoesToTerraform)
                return false;

            TerraformPoints += TerraformToAdd * 4f; // Terraforming a Volcano is faster than the whole planet
            if (TerraformPoints.GreaterOrEqual(1))
                CompleteVolcanoTerraforming(TilesList.Filter(t => t.VolcanoHere));

            return true;
        }

        bool TerraformTiles()
        {
            if (!HasTilesToTerraform)
                return false; // no tiles need terraforming

            TerraformPoints += TerraformToAdd * 3f; // Terraforming a tile is faster than the whole planet
            if (TerraformPoints.GreaterOrEqual(1))
                CompleteTileTerraforming(TilesList.Filter(t => !t.Habitable && t.Terraformable || t.BioCanTerraform));

            return true;
        }

        void TerraformPlanet()
        {
            if (Category == Owner.data.PreferredEnv && BaseMaxFertility.GreaterOrEqual(TerraformedMaxFertility))
                return;

            if (TerraformPoints.AlmostZero()) // Starting terraform
                SetBaseFertilityTerraform();

            TerraformPoints += TerraformToAdd;

            // Increase MaxBaseFertility if the target MaxBaseFertility is higher than current 
            if (TerraformedMaxFertility.Greater(BaseMaxFertility))
                AddMaxBaseFertility(BaseFertilityTerraformRatio * TerraformToAdd);

            if (TerraformPoints.GreaterOrEqual(1))
                CompletePlanetTerraform();

            return;
        }

        void CompleteVolcanoTerraforming(PlanetGridSquare[] possibleTiles)
        {
            if (possibleTiles.Length > 0)
            {
                PlanetGridSquare tile = RandItem(possibleTiles);
                Volcano.RemoveVolcano(tile, this);
            }

            UpdateTerraformPoints(0); // Start terraforming a new tile or remove terraformers if terra level is 1.
            if (!Terraformable)
                RemoveTerraformers();
            else if (TerraformersHere > TerraformerLimit)
                RemoveTerraformers(removeOne: true); // Dynamically remove terraformers
        }

        void CompleteTileTerraforming(PlanetGridSquare[] possibleTiles)
        {
            if (possibleTiles.Length > 0)
            {
                PlanetGridSquare tile = RandItem(possibleTiles);
                MakeTileHabitable(tile);
            }

            UpdateTerraformPoints(0); // Start terraforming a new tile
            if (!Terraformable)
                RemoveTerraformers();
            else if (TerraformersHere > TerraformerLimit)
                RemoveTerraformers(removeOne: true); // Dynamically remove terraformers
        }

        void CompletePlanetTerraform()
        {
            Terraform(Owner.data.PreferredEnv);
            UpdateTerraformPoints(0);
            if (TerraformedMaxFertility.Greater(BaseMaxFertility))
            {
                // BaseMaxFertility was lower, so the planet was improved. This is just to stabilize
                // BaseMaxFertility after the gradual increase during terraform
                AddMaxBaseFertility(-BaseMaxFertility + TerraformedMaxFertility);
            }
            else 
            {
                // BaseMaxFertility was higher than target max fertility anyway, so keep it the same,
                // considering racial envs and align Current fertility to MaxFertility
                // The LowerBound is for planets which has high original base fertility before terraforming
                float alignedMaxFertility = TerraformedMaxFertility.LowerBound(BaseMaxFertility*TerraformedMaxFertility);
                float alignedFertility    = BaseFertility * TerraformedMaxFertility;
                SetBaseFertility(alignedFertility, alignedMaxFertility);
            }

            if (!OwnerIsPlayer) // Re-assess colony type after terraform, this might change for the AI
                colonyType = Owner.AssessColonyNeeds(this);
        }

        public bool ContainsEventTerraformers => BuildingList.Any(b => b.IsEventTerraformer);

        // Checks if the owner can terraform the planet or parts of it
        // Commodity terraformers will set the Terraform level to 3.
        public bool Terraformable 
        {
            get
            {
                int terraLevel = ContainsEventTerraformers ? 3 : Owner.data.Traits.TerraformingLevel;
                return terraLevel > 0 && HasVolcanoesToTerraform
                    || terraLevel > 1 && HasTilesToTerraform
                    || terraLevel > 2 && BioSpheresToTerraform
                    || terraLevel > 2 &&
                        (Category != Owner.data.PreferredEnv || BaseMaxFertility.Less(TerraformedMaxFertility));
            }
        }

        // FB - This will give the Natural Max Fertility the planet should have after terraforming is complete
        public float TerraformedMaxFertility
        {
            get
            {
                if (IsCybernetic)
                    return 0;

                float racialEnvMultiplier = 1 / Empire.PreferredEnvModifier(Owner);
                return racialEnvMultiplier;
            }
        }

        void RemoveTerraformers(bool removeOne = false)
        {
            foreach (PlanetGridSquare tile in TilesList)
            {
                if (tile.Building?.PlusTerraformPoints > 0)
                {
                    ScrapBuilding(tile.Building);
                    if (removeOne)
                        return;
                }
            }

            // Notify player that the planet was terraformed
            if (OwnerIsPlayer)
            {
                string msg = $"{Localizer.Token(GameText.TerraformLevel)} {Owner.data.Traits.TerraformingLevel}:\n" +
                             $"{Name} {Localizer.Token(GameText.TerraformingCompletedAndTerraformersWere)}";
                Universe.Notifications.AddRandomEventNotification(msg, Type.IconPath, "SnapToPlanet", this);
            }
        }

        public void UpdateTerraformPoints(float points)
        {
            TerraformPoints = points;
        }

        public void MakeTileHabitable(PlanetGridSquare tile)
        {
            if (tile.Biosphere)
                ClearBioSpheresFromList(tile);

            if (tile.LavaHere)
                RemoveBuildingFromPlanet(tile, true);

            tile.Habitable     = true;
            tile.Terraformable = false;
            UpdateMaxPopulation();
        }

        // Refactored by Fat Bastard && RedFox
        void Terraform(PlanetCategory newCategory)
        {
            if (Category == newCategory)
                return; // A planet with the same category was Terraformed (probably to increase fertility)

            Type = ResourceManager.Planets.RandomPlanet(newCategory);
            RecreateSceneObject();
            UpdateDescription();
            UpdateMaxPopulation();
        }

        private void ReCalculateHabitableChances() // FB - We might need it for planet degrade
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

        private void SetBaseFertilityTerraform()
        {
            float ratio;
            if      (BaseMaxFertility.AlmostZero())        ratio = TerraformedMaxFertility;
            else if (TerraformedMaxFertility.AlmostZero()) ratio = 0;
            else                                           ratio = MaxFertility / TerraformedMaxFertility;

            // The ratio is need to gradually increase BaseMaxFertility as the planet is being terraformed
            BaseFertilityTerraformRatio = ratio;
        }

        protected void AddEventsAndCommodities()
        {
            if (!Habitable)
                return;

            foreach (RandomItem item in ResourceManager.RandomItemsList)
            {
                (float chance, int maxInstance) = item.ChanceAndMaxInstance(Category);
                SpawnRandomItem(item, chance, maxInstance);
            }
            AddTileEvents();
        }

        public void RecreateSceneObject()
        {
            CreatePlanetSceneObject();
        }
    }
}