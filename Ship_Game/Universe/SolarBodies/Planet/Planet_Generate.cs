using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data;

namespace Ship_Game
{
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
        public void InitNewMinorPlanet(PlanetType type)
        {
            GenerateNewFromPlanetType(type);
            AddEventsAndCommodities();
        }

        static PlanetType ChooseType(SunZone sunZone)
        {
            for (int x = 0; x < 5; x++)
            {
                PlanetType type = ResourceManager.RandomPlanet();
                if (type.Zone == sunZone 
                || (type.Zone == SunZone.Any && sunZone == SunZone.Near)
                || (x > 2 && type.Zone == SunZone.Any))
                    return type;
            }
            return ResourceManager.RandomPlanet();
        }

        public void RestorePlanetTypeFromSave(int planetId)
        {
            // we revert to random just in case people unload mods
            Type = ResourceManager.PlanetOrRandom(planetId);
        }

        void GenerateNewFromPlanetType(PlanetType type)
        {
            Type = type;
            MaxPopBase = type.MaxPop.Generate();
            Fertility  = type.Fertility.Generate().Clamped(type.MinFertility, 100.0f);
            MaxFertility = Fertility;
            Zone         = type.Zone;
            TilesList.Clear();

            if (Habitable)
            {
                float richness = RandomMath.RandomBetween(0.0f, 100f);
                if      (richness >= 92.5f) MineralRichness = RandomMath.RandomBetween(2.00f, 2.50f);
                else if (richness >= 85.0f) MineralRichness = RandomMath.RandomBetween(1.50f, 2.00f);
                else if (richness >= 25.0f) MineralRichness = RandomMath.RandomBetween(0.75f, 1.50f);
                else if (richness >= 12.5f) MineralRichness = RandomMath.RandomBetween(0.25f, 0.75f);
                else if (richness < 12.5f)  MineralRichness = RandomMath.RandomBetween(0.10f, 0.25f);
            }
            else MineralRichness = 0.0f;
        }

        public void GenerateNewHomeWorld(PlanetType type)
        {
            Type = type;
            Fertility = type.Fertility.Generate().Clamped(type.MinFertility, 100.0f);
            MaxFertility = Fertility;
            Zone         = SunZone.Any;

            TilesList.Clear();
            for (int x = 0; x < 7; ++x)
            {
                for (int y = 0; y < 5; ++y)
                {
                    // HomeWorld always gets 75% habitable chance per tile
                    bool habitableTile = RandomMath.RandomBetween(0f, 100f) < 75f;
                    TilesList.Add(new PlanetGridSquare(x, y, null, habitableTile));
                }
            }
        }

        // Refactored by Fat Bastard && RedFox
        void Terraform(PlanetCategory newCategory, bool recalculateTileHabitation = false)
        {
            Type = ResourceManager.RandomPlanet(newCategory);

            // reduce the habitable tile chance slightly from base values:
            Range chance = Type.HabitableTileChance;
            chance.Min = Math.Max(chance.Min - 10, 10);
            chance.Max = Math.Max(chance.Max - 10, chance.Min);
            float habitableChance = chance.Generate();

            // also reduce base value of maxPop, we don't want super-planets
            Range maxPop = Type.MaxPop;
            const float maxAllowed = 10000f;
            // rescale max value to 10000
            if (maxPop.Max > maxAllowed)
            {
                float rescale = maxAllowed / maxPop.Max;
                maxPop.Min *= rescale;
                maxPop.Max *= rescale;
            }
            MaxPopBase = maxPop.Generate();

            foreach (PlanetGridSquare pgs in TilesList)
            {
                if (!recalculateTileHabitation && pgs.Habitable && !pgs.Biosphere) 
                    continue;

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
                        if ((int)RandomMath.RandomBetween(0.0f, 100f) < habitableChance)
                        {
                            pgs.Habitable = true;
                            pgs.Biosphere = false;
                        }
                        else
                            pgs.Habitable = pgs.Biosphere;
                        continue;
                    default:
                        continue;
                }
            }
            UpdateDescription();
            CreatePlanetSceneObject(Empire.Universe);
        }

        protected void AddEventsAndCommodities()
        {
            if (Habitable)
            {
                float habitableChance = GlobalStats.ActiveModInfo?.ChanceForCategory(Category)
                                     ?? Type.HabitableTileChance.Generate();
                SetTileHabitability(habitableChance);
            }
            foreach (RandomItem item in ResourceManager.RandomItemsList)
            {
                (float chance, float maxInstance) = item.ChanceAndMaxInstance(Category);
                SpawnRandomItem(item, chance, maxInstance);
            }
            AddTileEvents();
        }
    }
}
