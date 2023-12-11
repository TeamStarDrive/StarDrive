using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.ExtensionMethods;
using Ship_Game.Universe.SolarBodies;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game
{
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
        [StarData] public bool IsHomeworld { get; private set; } 

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
                if (PType.PlanetTile.NotEmpty())
                    return PType.PlanetTile;
                return Category.ToString();

            }
        }

        // this applies to any randomly generated planet
        // which is newly created and is not a HomeWorld
        void InitNewMinorPlanet(RandomBase random, PlanetType type, float scale, float preDefinedPop = 0)
        {
            GenerateNewFromPlanetType(random, type, scale, preDefinedPop);
            AddEventsAndCommodities();
        }

        // FB - this is a more comprehensive method of choosing planet type.
        // It gets the planet category by weights based on sun zone and then
        // randomize relevant planet types from the chose category
        // this reduces chances of terran planets and its configurable via SunZoneData.yaml
        static PlanetType ChooseTypeByWeight(SunZone sunZone, RandomBase random)
        {
            PlanetCategory chosenCategory = ResourceManager.RandomPlanetCategoryFor(sunZone, random);
            return ResourceManager.Planets.RandomPlanet(chosenCategory);
        }

        public PlanetGridSquare FindTileUnderMouse(Vector2 mousePos)
            => TilesList.Find(pgs => pgs.ClickRect.HitTest(mousePos));

        // preDefinedPop is in millions
        public void GenerateNewFromPlanetType(RandomBase random, PlanetType type, float scale, float preDefinedPop = 0)
        {
            TilesList.Clear();
            InitPlanetType(type, scale, fromSave: false);

            if (Habitable)
            {
                float richness = random.RollDie(100);
                if      (richness >= 99f) MineralRichness = random.Float(3.00f, 6.00f);
                else if (richness >= 95f) MineralRichness = random.Float(2.00f, 4.00f);
                else if (richness >= 90f) MineralRichness = random.Float(1.50f, 3.00f);
                else if (richness >= 80f) MineralRichness = random.Float(1.25f, 2.50f);
                else if (richness >= 70f) MineralRichness = random.Float(1.00f, 2.00f);
                else if (richness >= 60f) MineralRichness = random.Float(1.00f, 1.75f);
                else if (richness >= 50f) MineralRichness = random.Float(0.75f, 1.50f);
                else if (richness >= 40f) MineralRichness = random.Float(0.75f, 1.25f);
                else if (richness >= 30f) MineralRichness = random.Float(0.75f, 1.00f);
                else if (richness >= 20f) MineralRichness = random.Float(0.50f, 0.75f);
                else if (richness >= 10f) MineralRichness = random.Float(0.25f, 0.50f);
                else                      MineralRichness = random.Float(0.10f, 0.25f);

                float habitableChance = PType.HabitableTileChance.Generate(random);

                SetTileHabitability(random, habitableChance, out int numHabitableTiles);
                if (preDefinedPop > 0)
                    BasePopPerTile = (int)(preDefinedPop * 1000 / numHabitableTiles);
                else
                    BasePopPerTile = ((int)(type.PopPerTile.Generate(random) * scale)).RoundUpToMultipleOf(10);

                BaseFertility    = type.BaseFertility.Generate(random).Clamped(type.MinBaseFertility, 100.0f);
                BaseMaxFertility = BaseFertility;
            }
            else
                MineralRichness = 0.0f;
        }

        void GeneratePlanetFromSystemData(RandomBase random, SolarSystemData.Ring data)
        {
            PlanetType type = ResourceManager.Planets.PlanetOrRandom(data.WhichPlanet);

            float scale;
            if (data.planetScale > 0)
                scale = data.planetScale;
            else
                scale = type.Scale + Random.Float(0.9f, 1.8f);

            if (data.UniqueHabitat)
            {
                UniqueHab = true;
                UniqueHabPercent = data.UniqueHabPC;
            }

            InitNewMinorPlanet(random, type, scale, data.MaxPopDefined);
        }

        public void GenerateNewHomeWorld(RandomBase random, Empire owner, SolarSystemData.Ring data = null)
        {
            PlanetType type = ResourceManager.Planets.RandomPlanet(owner.data.PreferredEnvPlanet);
            float scale = 1f * owner.data.Traits.HomeworldSizeMultiplier; // base max pop is affected by scale

            InitPlanetType(type, scale, fromSave: false);
            SetOwner(owner);
            IsHomeworld = true;
            Owner.SetCapital(this);
            SetTileHabitability(random, 0, out _); // Create the homeworld's tiles without making them habitable yet
            SetHomeworldTiles(random);
            ResetGarrisonSize();
            if (OwnerIsPlayer)
                CType = ColonyType.Colony;

            CreateHomeWorldFertilityAndRichness();
            int numHabitableTiles = TilesList.Count(t => t.Habitable);
            float preDefinedPop = data?.MaxPopDefined ?? 0f;
            CreateHomeWorldPopulation(preDefinedPop, numHabitableTiles);
            InitializeWorkerDistribution(Owner);
            if (!System.OwnerList.Contains(Owner))
                System.OwnerList.Add(Owner);

            UpdateDevelopmentLevel();
            CreateHomeWorldBuildings();
        }

        void SetTileHabitability(RandomBase random, float tileChance, out int numHabitableTiles)
        {
            numHabitableTiles = 0;

            TilesList.Clear();
            for (int y = 0; y < TileMaxY; ++y) // row-major
            {
                for (int x = 0; x < TileMaxX; ++x)
                {
                        bool habitableTile = random.RollDice(tileChance);
                        bool terraformable = !habitableTile && random.RollDice(25) || habitableTile;
                        TilesList.Add(new PlanetGridSquare(this, x, y, null, habitableTile, terraformable));
                        if (habitableTile)
                            ++numHabitableTiles;
                }
            }
        }

        void SetHomeworldTiles(RandomBase random)
        {
            for (int i = 0; i < 28; ++i)
            {
                PlanetGridSquare tile = random.Item(TilesList.Filter(t => !t.Habitable));
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

        void CreateHomeWorldBuildings()
        {
            ResourceManager.CreateBuilding(this, Building.CapitalId).AssignBuildingToTilePlanetCreation(this, out _);
            ResourceManager.CreateBuilding(this, Building.SpacePortId).AssignBuildingToTilePlanetCreation(this, out _);
            FoodHere = Storage.Max * 0.2f;
            ProdHere = Storage.Max * 0.35f;
            AllowInfantry = true; // for initialization only, before we reach planet Update
        }

        void ApplyTerraforming(RandomBase random) // Added by Fat Bastard
        {
            if (!Terraformable)
                return;

            if (TerraformToAdd <= 0 || Owner == null)
            {
                TerraformPoints = 0;
                return; // No Terraformers or No owner (Terraformers cannot continue working)
            }

            // First, remove Volcanoes
            if (TerraformTerrain(random))
                return;

            // Then, make un-habitable terraformable tiles habitable
            if (TerraformTiles(random)) 
                return;

            // Then, if all tiles are habitable and Terraforming Level is 3, proceed to Planet Terraform
            TerraformPlanet();
        }

        public bool HasTilesToTerraform     => TilesList.Any(t => t.CanTerraform);
        public bool BioSpheresToTerraform   => TilesList.Any(t => t.BioCanTerraform);
        public int TerraformerLimit         => TilesList.Count(t => t.CanTerraform)/2 + 2;

        public bool HasTerrainToTerraform => HasBuilding(b => b.CanBeTerraformed);

        bool TerraformTerrain(RandomBase random)
        {
            if (!HasTerrainToTerraform)
                return false;

            TerraformPoints += TerraformToAdd * 4f; // Terraforming Terrain or Volcano is faster than the whole planet
            if (TerraformPoints.GreaterOrEqual(1))
                CompleteTerrainTerraforming(random, TilesList.Filter(t => t.VolcanoHere || t.TerrainCanBeTerraformed));

            return true;
        }

        bool TerraformTiles(RandomBase random)
        {
            if (!HasTilesToTerraform)
                return false; // no tiles need terraforming

            TerraformPoints += TerraformToAdd * 3f; // Terraforming a tile is faster than the whole planet
            if (TerraformPoints.GreaterOrEqual(1))
                CompleteTileTerraforming(random, TilesList.Filter(t => !t.Habitable && t.Terraformable || t.BioCanTerraform));

            return true;
        }

        void TerraformPlanet()
        {
            if (Category == Owner.data.PreferredEnvPlanet && BaseMaxFertility.GreaterOrEqual(TerraformedMaxFertility))
                return;

            if (TerraformPoints.AlmostZero()) // Starting terraform
                SetBaseFertilityTerraform();

            TerraformPoints += TerraformToAdd;

            // Increase MaxBaseFertility if the target MaxBaseFertility is higher than current 
            if (TerraformedMaxFertility.Greater(BaseMaxFertility))
                AddMaxBaseFertility(BaseFertilityTerraformRatio * TerraformToAdd);

            if (TerraformPoints.GreaterOrEqual(1))
                CompletePlanetTerraform();
        }

        void CompleteTerrainTerraforming(RandomBase random, PlanetGridSquare[] possibleTiles)
        {
            if (possibleTiles.Length > 0)
            {
                PlanetGridSquare tile = random.Item(possibleTiles);
                if (tile.VolcanoHere)
                    Volcano.RemoveVolcano(tile, this);
                else
                    DestroyBuildingOn(tile);
            }

            TerraformPoints = 0; // Start terraforming a new tile or remove terraformers if terra level is 1.
            if (!Terraformable)
                RemoveTerraformers();
            else if (TerraformersHere > TerraformerLimit)
                RemoveTerraformers(removeOne: true); // Dynamically remove terraformers
        }

        void CompleteTileTerraforming(RandomBase random, PlanetGridSquare[] possibleTiles)
        {
            if (possibleTiles.Length > 0)
            {
                PlanetGridSquare tile = random.Item(possibleTiles);
                MakeTileHabitable(tile);
            }

            TerraformPoints = 0; // Start terraforming a new tile
            if (!Terraformable)
                RemoveTerraformers();
            else if (TerraformersHere > TerraformerLimit)
                RemoveTerraformers(removeOne: true); // Dynamically remove terraformers
        }

        void CompletePlanetTerraform()
        {
            Terraform(Owner.data.PreferredEnvPlanet);
            TerraformPoints = 0;
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
                CType = Owner.AssessColonyNeeds(this);
        }

        public bool ContainsEventTerraformers => HasBuilding(b => b.IsEventTerraformer);

        // Checks if the owner can terraform the planet or parts of it
        // Commodity terraformers will set the Terraform level to 3.
        public bool Terraformable 
        {
            get
            {
                int terraLevel = ContainsEventTerraformers ? 3 : Owner?.data.Traits.TerraformingLevel ?? 0;
                return terraLevel >= 1 && HasTerrainToTerraform
                    || terraLevel >= 2 && HasTilesToTerraform
                    || terraLevel == 3 && BioSpheresToTerraform
                    || terraLevel == 3 &&
                        (Category != Owner.data.PreferredEnvPlanet || BaseMaxFertility.Less(TerraformedMaxFertility));
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
                Universe.Notifications.AddRandomEventNotification(msg, PType.IconPath, "SnapToPlanet", this);
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
                DestroyBuildingOn(tile);

            tile.Habitable = true;
            tile.Terraformable = false;
            UpdateMaxPopulation();
        }

        // Refactored by Fat Bastard && RedFox
        void Terraform(PlanetCategory newCategory)
        {
            if (Category == newCategory)
                return; // A planet with the same category was Terraformed (probably to increase fertility)

            PType = ResourceManager.Planets.RandomPlanet(newCategory);
            RecreateSceneObject();
            UpdateDescription();
            if (BasePopPerTile <= 200)
                BasePopPerTile = (BasePopPerTile * 2).LowerBound(200);
            UpdateMaxPopulation();
            RemoveTerraformers();
        }

        private void ReCalculateHabitableChances(RandomBase random) // FB - We might need it for planet degrade
        {
            float habitableChance = PType.HabitableTileChance.Generate(random);
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
                        if (!random.RollDice(habitableChance))
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

        void AddEventsAndCommodities()
        {
            if (!Habitable)
                return;

            foreach (RandomItem item in ResourceManager.RandomItemsList)
            {
                (float chance, int minInstance, int maxInstance) = item.ChanceMinMaxInstance(Category);
                SpawnRandomItem(item, chance, minInstance, maxInstance);
            }
            AddTileEvents();
        }

        public void RecreateSceneObject()
        {
            CreatePlanetSceneObject();
        }
    }
}