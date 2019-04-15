using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Ship_Game.Data;

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
        public void InitNewMinorPlanet(PlanetType type)
        {
            GenerateNewFromPlanetType(type);
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

        void GenerateNewFromPlanetType(PlanetType type)
        {
            Type = type;
            MaxPopBase = type.MaxPop.Generate();
            Fertility  = type.Fertility.Generate().Clamped(type.MinFertility, 100.0f);
            MaxFertility = Fertility;
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
            if (Owner.isPlayer)
                colonyType = ColonyType.Colony;
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

        float QualityForRemnants()
        {
            float quality = Fertility + MineralRichness + MaxPopulationBillion;
            //Boost the quality score for planets that are very rich, or very fertile
            if (Fertility > 1.6)       ++quality;
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
            if (quality > 14)
            {
                AddMajorRemnantShips();
                AddSupportRemnantShips();
                if (d100 >= 50) AddMinorRemnantShips();
                if (d100 >= 75) AddTorpedoRemnantShips();
            }
            else if (quality > 10f)
            {
                if (d100 >= 40) AddMajorRemnantShips();
                if (d100 >= 85) AddMajorRemnantShips();
                if (d100 >= 95) AddMajorRemnantShips();
            }
            else if (quality > 6f)
            {
                if (d100 >= 20) AddMiniRemnantShips();
                if (d100 >= 50) AddMinorRemnantShips();
                if (d100 >= 80) AddMinorRemnantShips();
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
            int numXenoFighters = RollDie(5) + 2;
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
