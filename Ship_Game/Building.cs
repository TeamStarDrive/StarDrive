using System.Xml.Serialization;
using Ship_Game.Gameplay;
using Newtonsoft.Json;

namespace Ship_Game
{
    public sealed class Building
    {
        [Serialize(0)] public string Name;
        [Serialize(1)] public bool IsSensor;
        [Serialize(2)] public bool NoRandomSpawn;
        [Serialize(3)] public bool AllowShipBuilding;
        [Serialize(4)] public int NameTranslationIndex;
        [Serialize(5)] public int DescriptionIndex;
        [Serialize(6)] public int ShortDescriptionIndex;
        [Serialize(7)] public string ResourceCreated;
        [Serialize(8)] public string ResourceConsumed;
        [Serialize(9)] public float ConsumptionPerTurn;
        [Serialize(10)] public float OutputPerTurn;
        [Serialize(11)] public string CommodityRequired;
        [Serialize(12)] public CommodityBonusType CommodityBonusType;
        [Serialize(13)] public float CommodityBonusAmount;
        [Serialize(14)] public bool IsCommodity;
        [Serialize(15)] public bool WinsGame;
        [Serialize(16)] public bool BuildOnlyOnce;
        [Serialize(17)] public string EventOnBuild;
        [Serialize(18)] public string EventTriggerUID = "";
        [Serialize(19)] public bool EventWasTriggered;
        [Serialize(20)] public bool CanBuildAnywhere;
        [Serialize(21)] public float PlusTerraformPoints;
        [Serialize(22)] public int Strength = 5;
        [Serialize(23)] public float PlusProdPerRichness;
        [Serialize(24)] public float PlanetaryShieldStrengthAdded;
        [Serialize(25)] public float PlusFlatPopulation;
        [Serialize(26)] public float MinusFertilityOnBuild;
        [Serialize(27)] public string Icon;
        [Serialize(28)] public bool Scrappable ;
        [Serialize(29)] public bool Unique = true;
        [Serialize(30)] public bool isWeapon;
        [Serialize(31)] public string Weapon = "";
        [Serialize(33)] public float WeaponTimer;
        [Serialize(34)] public float AttackTimer;
        [Serialize(35)] public int AvailableAttackActions = 1;
        [Serialize(36)] public int CombatStrength;
        [Serialize(37)] public int SoftAttack;
        [Serialize(38)] public int HardAttack;
        [Serialize(39)] public int Defense;
        [Serialize(40)] public float PlusTaxPercentage;
        [Serialize(41)] public bool AllowInfantry;
        [Serialize(42)] public float Maintenance;
        [Serialize(43)] public float Cost;
        [Serialize(44)] public int StorageAdded;
        [Serialize(45)] public float PlusResearchPerColonist;
        [Serialize(46)] public string ExcludesPlanetType = "";
        [Serialize(47)] public float PlusFlatResearchAmount;
        [Serialize(48)] public float CreditsPerColonist;
        [Serialize(49)] public float PlusFlatFoodAmount;
        [Serialize(50)] public float PlusFoodPerColonist;
        [Serialize(51)] public float MaxPopIncrease;
        [Serialize(52)] public float PlusProdPerColonist;
        [Serialize(53)] public float PlusFlatProductionAmount;
        [Serialize(54)] public float SensorRange;
        [Serialize(55)] public bool IsProjector;
        [Serialize(56)] public float ProjectorRange;
        [Serialize(57)] public float ShipRepair;
        [Serialize(58)] public BuildingCategory Category = BuildingCategory.General;
        [Serialize(59)] public bool IsPlayerAdded = false;

        [XmlIgnore][JsonIgnore] public Weapon theWeapon;

        public void SetPlanet(Planet p)
        {
            p.BuildingList.Add(this);
            this.AssignBuildingToTile(p);
        }

        public Building Clone()
        {
            Building b = (Building)MemberwiseClone();
            b.theWeapon = null;
            return b;
        }

        public bool ProducesProduction => PlusFlatProductionAmount >0 || PlusProdPerColonist >0 || PlusProdPerRichness >0;
        public bool ProducesFood => PlusFlatFoodAmount >0 || PlusFoodPerColonist > 0;

        private static float Production(Planet planet, float flatBonus, float perColonistBonus, float adjust = 1)
        {
            float food = flatBonus;
            return food + perColonistBonus * (planet.Population / 1000f) * adjust;
        }

        public float CreditsProduced(Planet planet)
        {
            return Production(planet, 0, CreditsPerColonist);            
        }
        public float FoodProduced(Planet planet)
        {
            if (planet.Owner.data.Traits.Cybernetic < 1)
                return Production(planet, PlusFlatFoodAmount, PlusFoodPerColonist, planet.Fertility);
            return ProductionProduced(planet);
        }
        public float ProductionProduced(Planet planet)
        {
            return Production(planet, PlusFlatProductionAmount, PlusProdPerColonist, planet.MineralRichness);
        }
        public float ResearchProduced(Planet planet)
        {
            return Production(planet, PlusFlatResearchAmount, PlusResearchPerColonist);
        }

        public bool AssignBuildingToTile(SolarSystemBody solarSystemBody  = null)
        {
            if (AssignBuildingToRandomTile(solarSystemBody, true) != null)
                return true;
            PlanetGridSquare targetPGS;
            if (!string.IsNullOrEmpty(this.EventTriggerUID))
            {
                targetPGS = AssignBuildingToRandomTile(solarSystemBody);
                if (targetPGS != null)                
                    return targetPGS.Habitable = true;                    
                
            }
            if (this.Name == "Outpost" || !string.IsNullOrEmpty(this.EventTriggerUID))
            {
                targetPGS = AssignBuildingToRandomTile(solarSystemBody);
                if (targetPGS != null)
                    return targetPGS.Habitable = true;
            }
            if (this.Name == "Biospheres")
                return AssignBuildingToRandomTile(solarSystemBody) != null;                    
            return false;            
        }

        public PlanetGridSquare AssignBuildingToRandomTile(SolarSystemBody solarSystemBody, bool habitable = false)
        {
            PlanetGridSquare[] list;
            list = !habitable ? solarSystemBody.TilesList.FilterBy(planetGridSquare => planetGridSquare.building == null) 
                : solarSystemBody.TilesList.FilterBy(planetGridSquare => planetGridSquare.building == null && planetGridSquare.Habitable);
            if (list.Length == 0)
                return null;

            int index = RandomMath.InRange(list.Length - 1);
            var targetPGS = solarSystemBody.TilesList.Find(pgs => pgs == list[index]);
            targetPGS.building = this;
            return targetPGS;

        }

        public void AssignBuildingToSpecificTile(PlanetGridSquare pgs, Array<Building> BuildingList)
        {
            if (pgs.building != null)
                BuildingList.Remove(pgs.building);
            pgs.building = this;
            BuildingList.Add(this);
        }

        public bool AssignBuildingToTileOnColonize(Planet planet)
        {
            if (this.AssignBuildingToRandomTile(planet, habitable: true) != null) return true;
            return this.AssignBuildingToRandomTile(planet) != null;
        }

        public bool AssignBuildingToTile(QueueItem qi, Planet planet)
        {
            Array<PlanetGridSquare> list = new Array<PlanetGridSquare>();
            if (this.Name == "Biospheres") 
                return false;
            foreach (PlanetGridSquare planetGridSquare in planet.TilesList)
            {
                bool flag = true;
                foreach (QueueItem queueItem in planet.ConstructionQueue)
                {
                    if (queueItem.pgs == planetGridSquare)
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag && planetGridSquare.Habitable && planetGridSquare.building == null)
                    list.Add(planetGridSquare);
            }
            if (list.Count > 0)
            {
                int index = (int)RandomMath.RandomBetween(0.0f, list.Count);
                PlanetGridSquare planetGridSquare1 = list[index];
                foreach (PlanetGridSquare planetGridSquare2 in planet.TilesList)
                {
                    if (planetGridSquare2 == planetGridSquare1)
                    {
                        planetGridSquare2.QItem = qi;
                        qi.pgs = planetGridSquare2;
                        return true;
                    }
                }
            }
            else if (this.CanBuildAnywhere)
            {
                PlanetGridSquare planetGridSquare1 = planet.TilesList[(int)RandomMath.RandomBetween(0.0f, planet.TilesList.Count)];
                foreach (PlanetGridSquare planetGridSquare2 in planet.TilesList)
                {
                    if (planetGridSquare2 == planetGridSquare1)
                    {
                        planetGridSquare2.QItem = qi;
                        qi.pgs = planetGridSquare2;
                        return true;
                    }
                }
            }
            return false;
        }

        public void ScrapBuilding(Planet planet)
        {
            Building building1 = null;
            if (MinusFertilityOnBuild < 0)
                planet.Fertility += MinusFertilityOnBuild;

            foreach (Building building2 in planet.BuildingList)
            {
                if (this == building2)
                    building1 = building2;
            }
            planet.BuildingList.Remove(building1);
            planet.ProductionHere += ResourceManager.BuildingsDict[this.Name].Cost / 2f;
            foreach (PlanetGridSquare planetGridSquare in planet.TilesList)
            {
                if (planetGridSquare.building != null && planetGridSquare.building == building1)
                    planetGridSquare.building = null;
            }
        }
    }
}