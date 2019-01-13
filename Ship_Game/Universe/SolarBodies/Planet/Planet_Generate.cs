using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public void SetPlanetAttributes(bool setType = true)
        {
            HasEarthLikeClouds = false;
            float richness = RandomMath.RandomBetween(0.0f, 100f);
            if      (richness >= 92.5f) MineralRichness = RandomMath.RandomBetween(2.00f, 2.50f);
            else if (richness >= 85.0f) MineralRichness = RandomMath.RandomBetween(1.50f, 2.00f);
            else if (richness >= 25.0f) MineralRichness = RandomMath.RandomBetween(0.75f, 1.50f);
            else if (richness >= 12.5f) MineralRichness = RandomMath.RandomBetween(0.25f, 0.75f);
            else if (richness < 12.5f)  MineralRichness = RandomMath.RandomBetween(0.10f, 0.25f);

            if (setType) ApplyPlanetType();
            if (!Habitable) MineralRichness = 0.0f;

            AddEventsAndCommodities();
        }
        
        public void LoadAttributes()
        {
            switch (PlanetType)
            {
                case 1:
                    Category = PlanetCategory.Terran;
                    PlanetComposition = Localizer.Token(1700);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    break;
                case 2:
                    Category = PlanetCategory.GasGiant;
                    PlanetComposition = Localizer.Token(1701);
                    break;
                case 3:
                    Category = PlanetCategory.Barren;
                    PlanetComposition = Localizer.Token(1702);
                    Habitable = true;
                    break;
                case 4:
                    Category = PlanetCategory.Barren;
                    PlanetComposition = Localizer.Token(1702);
                    Habitable = true;
                    break;
                case 5:
                    Category = PlanetCategory.Barren;
                    PlanetComposition = Localizer.Token(1703);
                    Habitable = true;
                    break;
                case 6:
                    Category = PlanetCategory.GasGiant;
                    PlanetComposition = Localizer.Token(1701);
                    break;
                case 7:
                    Category = PlanetCategory.Barren;
                    PlanetComposition = Localizer.Token(1704);
                    Habitable = true;
                    break;
                case 8:
                    Category = PlanetCategory.Barren;
                    PlanetComposition = Localizer.Token(1703);
                    Habitable = true;
                    break;
                case 9:
                    Category = PlanetCategory.Volcanic;
                    PlanetComposition = Localizer.Token(1705);
                    break;
                case 10:
                    Category = PlanetCategory.GasGiant;
                    PlanetComposition = Localizer.Token(1706);
                    break;
                case 11:
                    Category = PlanetCategory.Tundra;
                    PlanetComposition = Localizer.Token(1707);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    break;
                case 12:
                    Category = PlanetCategory.GasGiant;
                    Habitable = false;
                    PlanetComposition = Localizer.Token(1708);
                    break;
                case 13:
                    Category = PlanetCategory.Terran;
                    PlanetComposition = Localizer.Token(1709);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    break;
                case 14:
                    Category = PlanetCategory.Desert;
                    PlanetComposition = Localizer.Token(1710);
                    Habitable = true;
                    break;
                case 15:
                    Category = PlanetCategory.GasGiant;
                    PlanetComposition = Localizer.Token(1711);
                    PlanetType = 26;
                    break;
                case 16:
                    Category = PlanetCategory.Barren;
                    PlanetComposition = Localizer.Token(1712);
                    Habitable = true;
                    break;
                case 17:
                    Category = PlanetCategory.Ice;
                    PlanetComposition = Localizer.Token(1713);
                    Habitable = true;
                    break;
                case 18:
                    Category = PlanetCategory.Steppe;
                    PlanetComposition = Localizer.Token(1714);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    break;
                case 19:
                    Category = PlanetCategory.Swamp;
                    PlanetComposition = "";
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    break;
                case 20:
                    Category = PlanetCategory.GasGiant;
                    PlanetComposition = Localizer.Token(1711);
                    break;
                case 21:
                    Category = PlanetCategory.Oceanic;
                    PlanetComposition = Localizer.Token(1716);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    break;
                case 22:
                    Category = PlanetCategory.Terran;
                    PlanetComposition = Localizer.Token(1717);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    break;
                case 23:
                    Category = PlanetCategory.Volcanic;
                    PlanetComposition = Localizer.Token(1718);
                    break;
                case 24:
                    Category = PlanetCategory.Barren;
                    PlanetComposition = Localizer.Token(1719);
                    Habitable = true;
                    break;
                case 25:
                    Category = PlanetCategory.Terran;
                    PlanetComposition = Localizer.Token(1720);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    break;
                case 26:
                    Category = PlanetCategory.GasGiant;
                    PlanetComposition = Localizer.Token(1711);
                    break;
                case 27:
                    Category = PlanetCategory.Terran;
                    PlanetComposition = Localizer.Token(1721);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    break;
                case 29:
                    Category = PlanetCategory.Terran;
                    PlanetComposition = Localizer.Token(1722);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    break;
            }
        }

        void GenerateType(SunZone sunZone)
        {
            for (int x = 0; x < 5; x++)
            {
                Category = PlanetCategory.Other;
                PlanetComposition = "";
                HasEarthLikeClouds = false;
                Habitable = false;
                MaxPopBase = 0;
                Fertility = 0;
                PlanetType = RandomMath.IntBetween(1, 24);
                TilesList.Clear();
                ApplyPlanetType();
                if (Zone == sunZone || (Zone == SunZone.Any && sunZone == SunZone.Near))
                    break;
                if (x > 2 && Zone == SunZone.Any)
                    break;
            }
        }

        void ApplyPlanetType()
        {
            HabitalTileChance = 20;
            switch (PlanetType)
            {
                case 1:
                    Category = PlanetCategory.Terran;
                    PlanetComposition = Localizer.Token(1700);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    MaxPopBase = (int)RandomMath.RandomBetween(4000f, 8000f);
                    Fertility = RandomMath.RandomBetween(0.8f, 1.5f);
                    Zone = SunZone.Habital;
                    HabitalTileChance = 20;
                    break;
                case 2:
                    Category = PlanetCategory.GasGiant;
                    PlanetComposition = Localizer.Token(1701);
                    Zone = SunZone.Far;
                    break;
                case 3:
                    Category = PlanetCategory.Barren;
                    PlanetComposition = Localizer.Token(1702);
                    Habitable = true;
                    MaxPopBase = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Zone = SunZone.Any;
                    break;
                case 4:
                    Category = PlanetCategory.Barren;
                    PlanetComposition = Localizer.Token(1703);
                    Habitable = true;
                    MaxPopBase = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Zone = SunZone.Any;
                    break;
                case 5:
                    Category = PlanetCategory.Barren;
                    PlanetComposition = Localizer.Token(1704);
                    MaxPopBase = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    Zone = SunZone.Any;
                    break;
                case 6:
                    Category = PlanetCategory.GasGiant;
                    PlanetComposition = Localizer.Token(1701);
                    Zone = SunZone.Far;
                    break;
                case 7:
                    Category = PlanetCategory.Barren;
                    PlanetComposition = Localizer.Token(1704);
                    MaxPopBase = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    Zone = SunZone.Any;
                    break;
                case 8:
                    Category = PlanetCategory.Barren;
                    PlanetComposition = Localizer.Token(1703);
                    MaxPopBase = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    Zone = SunZone.Any;
                    break;
                case 9:
                    Category = PlanetCategory.Volcanic;
                    PlanetComposition = Localizer.Token(1705);
                    Zone = SunZone.Any;
                    break;
                case 10:
                    Category = PlanetCategory.GasGiant;
                    PlanetComposition = Localizer.Token(1706);
                    Zone = SunZone.Far;
                    break;
                case 11:
                    Category = PlanetCategory.Tundra;
                    PlanetComposition = Localizer.Token(1707);
                    MaxPopBase = (int)RandomMath.RandomBetween(4000f, 8000f);
                    Fertility = RandomMath.AvgRandomBetween(0.5f, 1f);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    Zone = SunZone.Far;
                    HabitalTileChance = 20;
                    break;
                case 12:
                    Category = PlanetCategory.GasGiant;
                    Habitable = false;
                    PlanetComposition = Localizer.Token(1708);
                    Zone = SunZone.Far;
                    break;
                case 13:
                    Category = PlanetCategory.Terran;
                    PlanetComposition = Localizer.Token(1709);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopBase = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.AvgRandomBetween(1f, 3f);
                    Zone = SunZone.Habital;
                    HabitalTileChance = 75;
                    break;
                case 14:
                    Category = PlanetCategory.Desert;
                    PlanetComposition = Localizer.Token(1710);
                    HabitalTileChance = RandomMath.AvgRandomBetween(10f, 45f);
                    MaxPopBase = (int)HabitalTileChance * 100;
                    Fertility = RandomMath.AvgRandomBetween(-2f, 2f);
                    Fertility = Fertility < .8f ? .8f : Fertility;
                    Habitable = true;
                    Zone = SunZone.Near;
                    break;
                case 15:
                    Category = PlanetCategory.GasGiant;
                    PlanetComposition = Localizer.Token(1711);
                    PlanetType = 26;
                    Zone = SunZone.Far;
                    break;
                case 16:
                    Category = PlanetCategory.Barren;
                    PlanetComposition = Localizer.Token(1712);
                    MaxPopBase = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    Zone = SunZone.Any;
                    break;
                case 17:
                    Category = PlanetCategory.Ice;
                    PlanetComposition = Localizer.Token(1713);
                    MaxPopBase = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    Zone = SunZone.VeryFar;
                    break;
                case 18:
                    Category = PlanetCategory.Steppe;
                    PlanetComposition = Localizer.Token(1714);
                    HasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(15f, 45f);
                    MaxPopBase = (int)HabitalTileChance * 200;
                    Fertility = RandomMath.AvgRandomBetween(0f, 2f);
                    Fertility = Fertility < .8f ? .8f : Fertility;
                    Habitable = true;
                    Zone = SunZone.Habital;
                    break;
                case 19:
                    Category = PlanetCategory.Swamp;
                    Habitable = true;
                    PlanetComposition = Localizer.Token(1715);
                    HabitalTileChance = RandomMath.AvgRandomBetween(15f, 45f);
                    MaxPopBase = HabitalTileChance * 200;
                    Fertility = RandomMath.AvgRandomBetween(-2f, 3f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    HasEarthLikeClouds = true;
                    Zone = SunZone.Near;
                    break;
                case 20:
                    Category = PlanetCategory.GasGiant;
                    PlanetComposition = Localizer.Token(1711);
                    Zone = SunZone.Far;
                    break;
                case 21:
                    Category = PlanetCategory.Oceanic;
                    PlanetComposition = Localizer.Token(1716);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(15f, 45f);
                    MaxPopBase = HabitalTileChance * 100 + 1500;
                    Fertility = RandomMath.AvgRandomBetween(-3f, 5f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    Zone = SunZone.Habital;
                    break;
                case 22:
                    Category = PlanetCategory.Terran;
                    PlanetComposition = Localizer.Token(1717);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(60f, 90f);
                    MaxPopBase = HabitalTileChance * 200f;
                    Fertility = RandomMath.AvgRandomBetween(0f, 3f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    Zone = SunZone.Habital;
                    break;
                case 23:
                    Category = PlanetCategory.Volcanic;
                    PlanetComposition = Localizer.Token(1718);
                    Zone = SunZone.Near;
                    break;
                case 24:
                    Category = PlanetCategory.Barren;
                    PlanetComposition = Localizer.Token(1719);
                    Habitable = true;
                    MaxPopBase = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Zone = SunZone.Any;
                    break;
                case 25:
                    Category = PlanetCategory.Terran;
                    PlanetComposition = Localizer.Token(1720);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(60f, 90f);
                    MaxPopBase = HabitalTileChance * 200f;
                    Fertility = RandomMath.AvgRandomBetween(-.50f, 3f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    Zone = SunZone.Habital;
                    break;
                case 26:
                    Category = PlanetCategory.GasGiant;
                    PlanetComposition = Localizer.Token(1711);
                    Zone = SunZone.Far;
                    break;
                case 27:
                    Category = PlanetCategory.Terran;
                    PlanetComposition = Localizer.Token(1721);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(60f, 90f);
                    MaxPopBase = HabitalTileChance * 200f;
                    Fertility = RandomMath.AvgRandomBetween(-50f, 3f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    Zone = SunZone.Habital;
                    break;
                case 29:
                    Category = PlanetCategory.Terran;
                    PlanetComposition = Localizer.Token(1722);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    HabitalTileChance = RandomMath.AvgRandomBetween(50f, 80f);
                    MaxPopBase = HabitalTileChance * 150f;
                    Fertility = RandomMath.AvgRandomBetween(-50f, 3f);
                    Fertility = Fertility < 1 ? 1 : Fertility;
                    Zone = SunZone.Habital;
                    break;
            }
            MaxFertility = Fertility;
        }

        

        public void SetPlanetAttributes(float mrich)
        {
            if (mrich >= 87.5f)     MineralRichness = 2.5f;
            else if (mrich >= 75f)  MineralRichness = 1.5f;
            else if (mrich >= 25.0) MineralRichness = 1f;
            else if (mrich >= 12.5) MineralRichness = 0.5f;
            else if (mrich < 12.5)  MineralRichness = 0.1f;

            TilesList.Clear();
            switch (PlanetType)
            {
                case 1:
                    Category = PlanetCategory.Terran;
                    PlanetComposition = Localizer.Token(1700);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    MaxPopBase = (int)RandomMath.RandomBetween(4000f, 8000f);
                    Fertility = RandomMath.RandomBetween(0.5f, 2f);
                    HabitalTileChance = 20;
                    break;
                case 2:
                    Category = PlanetCategory.GasGiant;
                    PlanetComposition = Localizer.Token(1701);
                    break;
                case 3:
                    Category = PlanetCategory.Barren;
                    PlanetComposition = Localizer.Token(1702);
                    Habitable = true;
                    MaxPopBase = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    break;
                case 4:
                    Category = PlanetCategory.Barren;
                    PlanetComposition = Localizer.Token(1702);
                    Habitable = true;
                    MaxPopBase = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    break;
                case 5:
                    Category = PlanetCategory.Barren;
                    PlanetComposition = Localizer.Token(1703);
                    MaxPopBase = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    break;
                case 6:
                    Category = PlanetCategory.GasGiant;
                    PlanetComposition = Localizer.Token(1701);
                    break;
                case 7:
                    Category = PlanetCategory.Barren;
                    PlanetComposition = Localizer.Token(1704);
                    MaxPopBase = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    break;
                case 8:
                    Category = PlanetCategory.Barren;
                    PlanetComposition = Localizer.Token(1703);
                    MaxPopBase = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    break;
                case 9:
                    Category = PlanetCategory.Volcanic;
                    PlanetComposition = Localizer.Token(1705);
                    break;
                case 10:
                    Category = PlanetCategory.GasGiant;
                    PlanetComposition = Localizer.Token(1706);
                    break;
                case 11:
                    Category = PlanetCategory.Tundra;
                    PlanetComposition = Localizer.Token(1707);
                    MaxPopBase = (int)RandomMath.RandomBetween(4000f, 8000f);
                    Fertility = RandomMath.RandomBetween(0.5f, 0.9f);
                    HasEarthLikeClouds = true;
                    Habitable = true;
                    HabitalTileChance = 20;
                    break;
                case 12:
                    Category = PlanetCategory.GasGiant;
                    Habitable = false;
                    PlanetComposition = Localizer.Token(1708);
                    break;
                case 13:
                    Category = PlanetCategory.Terran;
                    PlanetComposition = Localizer.Token(1709);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopBase = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.RandomBetween(0.8f, 3f);
                    HabitalTileChance = 75;
                    break;
                case 14:
                    Category = PlanetCategory.Desert;
                    PlanetComposition = Localizer.Token(1710);
                    MaxPopBase = (int)RandomMath.RandomBetween(1000f, 3000f);
                    Fertility = RandomMath.RandomBetween(0.2f, 1.8f);
                    Habitable = true;
                    HabitalTileChance = 20;
                    break;
                case 15:
                    Category = PlanetCategory.GasGiant;
                    PlanetComposition = Localizer.Token(1711);
                    PlanetType = 26;
                    break;
                case 16:
                    Category = PlanetCategory.Barren;
                    PlanetComposition = Localizer.Token(1712);
                    MaxPopBase = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    break;
                case 17:
                    Category = PlanetCategory.Ice;
                    PlanetComposition = Localizer.Token(1713);
                    MaxPopBase = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    Habitable = true;
                    HabitalTileChance = 10;
                    break;
                case 18:
                    Category = PlanetCategory.Steppe;
                    PlanetComposition = Localizer.Token(1714);
                    Fertility = RandomMath.RandomBetween(0.4f, 1.4f);
                    HasEarthLikeClouds = true;
                    MaxPopBase = (int)RandomMath.RandomBetween(2000f, 4000f);
                    Habitable = true;
                    HabitalTileChance = 50;
                    break;
                case 19:
                    Category = PlanetCategory.Swamp;
                    Habitable = true;
                    PlanetComposition = Localizer.Token(1712);
                    MaxPopBase = (int)RandomMath.RandomBetween(1000f, 3000f);
                    Fertility = RandomMath.RandomBetween(1f, 5f);
                    HasEarthLikeClouds = true;
                    HabitalTileChance = 20;
                    break;
                case 20:
                    Category = PlanetCategory.GasGiant;
                    PlanetComposition = Localizer.Token(1711);
                    break;
                case 21:
                    Category = PlanetCategory.Oceanic;
                    PlanetComposition = Localizer.Token(1716);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopBase = (int)RandomMath.RandomBetween(3000f, 6000f);
                    Fertility = RandomMath.RandomBetween(2f, 5f);
                    HabitalTileChance = 20;
                    break;
                case 22:
                    Category = PlanetCategory.Terran;
                    PlanetComposition = Localizer.Token(1717);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopBase = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.RandomBetween(1f, 3f);
                    HabitalTileChance = 75;
                    break;
                case 23:
                    Category = PlanetCategory.Volcanic;
                    PlanetComposition = Localizer.Token(1718);
                    break;
                case 24:
                    Category = PlanetCategory.Barren;
                    PlanetComposition = Localizer.Token(1719);
                    Habitable = true;
                    MaxPopBase = (int)RandomMath.RandomBetween(100f, 500f);
                    Fertility = 0.0f;
                    break;
                case 25:
                    Category = PlanetCategory.Terran;
                    PlanetComposition = Localizer.Token(1720);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopBase = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.RandomBetween(1f, 2f);
                    HabitalTileChance = 90;
                    break;
                case 26:
                    Category = PlanetCategory.GasGiant;
                    PlanetComposition = Localizer.Token(1711);
                    break;
                case 27:
                    Category = PlanetCategory.Terran;
                    PlanetComposition = Localizer.Token(1721);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopBase = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.RandomBetween(1f, 3f);
                    HabitalTileChance = 60;
                    break;
                case 29:
                    Category = PlanetCategory.Terran;
                    PlanetComposition = Localizer.Token(1722);
                    Habitable = true;
                    HasEarthLikeClouds = true;
                    MaxPopBase = (int)RandomMath.RandomBetween(12000f, 20000f);
                    Fertility = RandomMath.RandomBetween(1f, 3f);
                    HabitalTileChance = 50;
                    break;
            }
            MaxFertility = Fertility;

            if (!Habitable) MineralRichness = 0.0f;
            else
            {
                if (Fertility <= 0 && Owner.Capital == null)
                    return;
                float chance = Owner.Capital == null ? HabitalTileChance : 75; // homeworlds always get 75% habitable chance per tile
                for (int x = 0; x < 7; ++x)
                {
                    for (int y = 0; y < 5; ++y)
                    {
                        bool habitableTile =  (int)RandomMath.RandomBetween(0.0f, 100f) < chance;
                        TilesList.Add(new PlanetGridSquare(x, y, null, habitableTile));
                    }
                }
            }
        }

        void Terraform(bool recalculateTileHabitation = false) // Refactored by Fat Bastard
        {
            switch (PlanetType)
            {
                case 7:
                    Category = PlanetCategory.Barren;
                    PlanetComposition = Localizer.Token(1704);
                    MaxPopBase = (int)RandomMath.RandomBetween(0.0f, 500f);
                    HabitalTileChance = 10;
                    break;
                case 9:
                    Category = PlanetCategory.Volcanic;
                    PlanetComposition = Localizer.Token(1705);
                    MaxPopBase = (int)RandomMath.RandomBetween(0.0f, 200f);
                    HabitalTileChance = 10;
                    break;
                case 11:
                    Category = PlanetCategory.Tundra;
                    PlanetComposition = Localizer.Token(1724);
                    MaxPopBase = (int)RandomMath.RandomBetween(4000f, 8000f);
                    break;
                case 14:
                    Category = PlanetCategory.Desert;
                    PlanetComposition = Localizer.Token(1725);
                    MaxPopBase = (int)RandomMath.RandomBetween(1000f, 3000f);
                    HabitalTileChance = RandomMath.AvgRandomBetween(10f, 40f);
                    break;
                case 17:
                    Category = PlanetCategory.Ice;
                    PlanetComposition = Localizer.Token(1713);
                    MaxPopBase = (int)RandomMath.RandomBetween(100f, 500f);
                    HabitalTileChance = RandomMath.AvgRandomBetween(15f, 30f);
                    break;
                case 18:
                    Category = PlanetCategory.Steppe;
                    PlanetComposition = Localizer.Token(1726);
                    MaxPopBase = (int)RandomMath.RandomBetween(2000f, 4000f);
                    HabitalTileChance = RandomMath.AvgRandomBetween(20f, 45f);
                    break;
                case 19:
                    Category = PlanetCategory.Swamp;
                    PlanetComposition = Localizer.Token(1727);
                    MaxPopBase = (int)RandomMath.RandomBetween(1000f, 3000f);
                    HabitalTileChance = RandomMath.AvgRandomBetween(15f, 50f);
                    break;
                case 21:
                    Category = PlanetCategory.Oceanic;
                    PlanetComposition = Localizer.Token(1728);
                    MaxPopBase = (int)RandomMath.RandomBetween(3000f, 6000f);
                    HabitalTileChance = RandomMath.AvgRandomBetween(25f, 55f);
                    break;
                case 22:
                    Category = PlanetCategory.Terran;
                    PlanetComposition = Localizer.Token(1717);
                    MaxPopBase = (int)RandomMath.RandomBetween(6000f, 10000f);
                    HabitalTileChance = RandomMath.AvgRandomBetween(50f, 70f);
                    break;
            }
            HasEarthLikeClouds = true;
            Habitable = true;
            foreach (PlanetGridSquare planetGridSquare in TilesList)
            {
                if (!recalculateTileHabitation && planetGridSquare.Habitable && !planetGridSquare.Biosphere) 
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
                            planetGridSquare.Habitable = true;
                            planetGridSquare.Biosphere = false;
                        }
                        else
                            planetGridSquare.Habitable = planetGridSquare.Biosphere;

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
