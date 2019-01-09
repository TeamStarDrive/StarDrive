using System.Xml.Serialization;
using Newtonsoft.Json;
using Ship_Game.Gameplay;

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
        [Serialize(28)] public bool Scrappable = true;
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
        [Serialize(60)] public int InvadeInjurePoints;

        [XmlIgnore][JsonIgnore] public Weapon TheWeapon { get; private set; }
        [XmlIgnore][JsonIgnore] public float Offense { get; private set; }

        public void SetPlanet(Planet p)
        {
            p.BuildingList.Add(this);
            AssignBuildingToTile(p);
        }

        public Building Clone()
        {
            var b = (Building)MemberwiseClone();
            b.TheWeapon = null;
            return b;
        }

        public void CreateWeapon()
        {
            if (!isWeapon)
                return;

            TheWeapon = ResourceManager.CreateWeapon(Weapon);
            Offense = TheWeapon.CalculateWeaponOffense() * 3; //360 degree angle
        }
        
        public float StrengthMax
        {
            get
            {
                Building template = ResourceManager.GetBuildingTemplate(Name);
                return template.Strength;
            }
        }

        public float ActualMaintenance(Planet p) => Maintenance + Maintenance * p.Owner.data.Traits.MaintMod;
        
        public bool ProducesProduction => PlusFlatProductionAmount > 0 || PlusProdPerColonist > 0 || PlusProdPerRichness > 0;
        public bool ProducesFood => PlusFlatFoodAmount > 0 || PlusFoodPerColonist > 0;
        public bool ProducesPopulation => PlusFlatPopulation > 0;

        private static float Production(Planet planet, float flatBonus, float perColonistBonus, float adjust = 1)
        {
            return flatBonus + perColonistBonus * planet.PopulationBillion * adjust;
        }

        public float CreditsProduced(Planet planet)
        {
            return Production(planet, 0, CreditsPerColonist);            
        }
        public float FoodProduced(Planet planet)
        {
            if (planet.NonCybernetic)
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
            if (!string.IsNullOrEmpty(EventTriggerUID))
            {
                targetPGS = AssignBuildingToRandomTile(solarSystemBody);
                if (targetPGS != null)                
                    return targetPGS.Habitable = true;                    
                
            }
            if (Name == "Outpost" || !string.IsNullOrEmpty(EventTriggerUID))
            {
                targetPGS = AssignBuildingToRandomTile(solarSystemBody);
                if (targetPGS != null)
                    return targetPGS.Habitable = true;
            }
            if (Name == "Biospheres")
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
            if (AssignBuildingToRandomTile(planet, habitable: true) != null) return true;
            return AssignBuildingToRandomTile(planet) != null;
        }

        public bool AssignBuildingToTile(QueueItem qi, Planet planet)
        {
            Array<PlanetGridSquare> list = new Array<PlanetGridSquare>();
            if (Name == "Biospheres") //biospheres are handled specifically, later in the calling function
                return false;

            foreach (PlanetGridSquare planetGridSquare in planet.TilesList) //Check all planet tiles
            {
                if (!planetGridSquare.Habitable || planetGridSquare.building != null) continue;

                bool spotClaimed = false;
                foreach (QueueItem queueItem in planet.ConstructionQueue)   //Check the queue to see if this grid square is already claimed
                {
                    if (queueItem.pgs == planetGridSquare)                  //Isn't the building assigned to the tile already? Why check the queue over and over?
                    {
                        spotClaimed = true;
                        break;
                    }
                }
                if (!spotClaimed) list.Add(planetGridSquare);   //Add to list of usable tiles 
            }

            if (list.Count > 0)
            {
                int index = (int)RandomMath.RandomBetween(0.0f, list.Count);
                PlanetGridSquare planetGridSquare1 = list[index];
                foreach (PlanetGridSquare planetGridSquare2 in planet.TilesList)    //What the shit is this doing? Is it seriously itterating the foreach until it finds the random tile picked above?!
                {                                                                   //There has to be a better way to translate from <list> back to <planet.TilesList>...
                    if (planetGridSquare2 == planetGridSquare1)
                    {
                        planetGridSquare2.QItem = qi;
                        qi.pgs = planetGridSquare2;
                        return true;
                    }
                }
            }
            else if (CanBuildAnywhere)     //I would like buildings that CanBuildAnywhere to prefer uninhabitable tiles
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
                planet.ChangeMaxFertility(MinusFertilityOnBuild);

            foreach (Building building2 in planet.BuildingList)
            {
                if (this == building2)
                    building1 = building2;
            }
            planet.BuildingList.Remove(building1);
            planet.ProdHere += ResourceManager.BuildingsDict[Name].Cost / 2f;
            foreach (PlanetGridSquare planetGridSquare in planet.TilesList)
            {
                if (planetGridSquare.building != null && planetGridSquare.building == building1)
                    planetGridSquare.building = null;
            }
        }
    }
}