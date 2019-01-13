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
        public PlanetCategory Category { get; protected set; }

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

        public string LocalizedRichness => $"{LocalizedCategory} {GetRichness()}";

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
                if (Category == PlanetCategory.Terran) switch (PlanetType)
                {
                    default:case 1: return "Terran";
                    case 13:        return "Terran_2";
                    case 22:        return "Terran_3";
                }
                return CategoryName;
            }
        }

        // this applies to any randomly generated planet
        // which is newly created and is not a HomeWorld
        public void InitNewMinorPlanet(PlanetTypeInfo type)
        {
            GenerateNewFromPlanetType(type);
            AddEventsAndCommodities();
        }

        static PlanetTypeInfo ChooseType(SunZone sunZone)
        {
            for (int x = 0; x < 5; x++)
            {
                PlanetTypeInfo type = ResourceManager.RandomPlanet();
                if (type.Zone == sunZone 
                || (type.Zone == SunZone.Any && sunZone == SunZone.Near)
                || (x > 2 && type.Zone == SunZone.Any))
                    return type;
            }
            return ResourceManager.RandomPlanet();
        }

        public void RestorePlanetTypeFromSave(int planetId)
        {
            PlanetTypeInfo type = ResourceManager.PlanetOrRandom(planetId);
            Type       = type;
            PlanetType = type.Id;
            Category   = type.Category;
            PlanetComposition  = type.Composition.Text;
            HasEarthLikeClouds = type.EarthLike;
            Habitable          = type.Habitable;
        }

        void GenerateNewFromPlanetType(PlanetTypeInfo type)
        {
            Type       = type;
            PlanetType = type.Id;
            Category   = type.Category;
            PlanetComposition  = type.Composition.Text;
            HasEarthLikeClouds = type.EarthLike;
            Habitable          = type.Habitable;
            HabitalTileChance  = (int)type.HabitableTileChance.Generate();
            MaxPopBase = type.MaxPop.Generate();
            Fertility = type.Fertility.Generate().Clamped(type.MinFertility, 100.0f);
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

        public void GenerateNewHomeWorld(PlanetTypeInfo type)
        {
            Type       = type;
            PlanetType = type.Id;
            Category   = type.Category;
            PlanetComposition  = type.Composition.Text;
            HasEarthLikeClouds = type.EarthLike;
            Habitable          = type.Habitable;
            Fertility = type.Fertility.Generate().Clamped(type.MinFertility, 100.0f);
            MaxFertility = Fertility;
            Zone = SunZone.Any;

            // HomeWorld always gets 75% habitable chance per tile
            HabitalTileChance = 75f;
            TilesList.Clear();
            for (int x = 0; x < 7; ++x)
            {
                for (int y = 0; y < 5; ++y)
                {
                    bool habitableTile = RandomMath.RandomBetween(0f, 100f) < HabitalTileChance;
                    TilesList.Add(new PlanetGridSquare(x, y, null, habitableTile));
                }
            }
        }

        // Refactored by Fat Bastard && RedFox
        void Terraform(PlanetCategory newCategory, bool recalculateTileHabitation = false)
        {
            PlanetTypeInfo type = ResourceManager.RandomPlanet(newCategory);
            Type       = type;
            PlanetType = type.Id;
            Category   = type.Category;
            PlanetComposition  = type.Composition.Text;
            HasEarthLikeClouds = type.EarthLike;
            Habitable          = type.Habitable;

            // reduce the habitable tile chance slightly from base values:
            Range chance = type.HabitableTileChance;
            chance.Min = Math.Max(chance.Min - 10, 10);
            chance.Max = Math.Max(chance.Max - 10, chance.Min);
            HabitalTileChance = chance.Generate();

            // also reduce base value of maxPop, we don't want super-planets
            Range maxPop = type.MaxPop;
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
                        if ((int)RandomMath.RandomBetween(0.0f, 100f) < HabitalTileChance)
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
            switch (Category)
            {
                case PlanetCategory.Terran:
                    SetTileHabitability(GlobalStats.ActiveModInfo?.TerranHab ?? HabitalTileChance);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.TerranChance, item.TerranInstanceMax);
                    break;
                case PlanetCategory.Steppe:
                    SetTileHabitability(GlobalStats.ActiveModInfo?.SteppeHab ?? HabitalTileChance);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.SteppeChance, item.SteppeInstanceMax);
                    break;
                case PlanetCategory.Ice:
                    SetTileHabitability(GlobalStats.ActiveModInfo?.IceHab ?? 15);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.IceChance, item.IceInstanceMax);
                    break;
                case PlanetCategory.Barren:
                    SetTileHabitability(GlobalStats.ActiveModInfo?.BarrenHab ?? 0);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.BarrenChance, item.BarrenInstanceMax);
                    break;
                case PlanetCategory.Tundra:
                    SetTileHabitability(GlobalStats.ActiveModInfo?.OceanHab ?? HabitalTileChance);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.TundraChance, item.TundraInstanceMax);
                    break;
                case PlanetCategory.Desert:
                    SetTileHabitability(GlobalStats.ActiveModInfo?.OceanHab ?? HabitalTileChance);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.DesertChance, item.DesertInstanceMax);
                    break;
                case PlanetCategory.Oceanic:
                    SetTileHabitability(GlobalStats.ActiveModInfo?.OceanHab ?? HabitalTileChance);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.OceanicChance, item.OceanicInstanceMax);
                    break;
                case PlanetCategory.Swamp:
                    SetTileHabitability(GlobalStats.ActiveModInfo?.SteppeHab ?? HabitalTileChance);
                    foreach (RandomItem item in ResourceManager.RandomItemsList)
                        SpawnRandomItem(item, item.SwampChance, item.SwampInstanceMax);
                    break;
            }
            AddTileEvents();
        }
    }
}
