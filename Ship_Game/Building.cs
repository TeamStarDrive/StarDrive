using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

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
        [Serialize(61)] public int DefenseShipsCapacity;
        [Serialize(62)] public ShipData.RoleName DefenseShipsRole;

        // XML Ignore because we load these from XML templates
        [XmlIgnore][JsonIgnore] public Weapon TheWeapon { get; private set; }
        [XmlIgnore][JsonIgnore] public float Offense { get; private set; }
        [XmlIgnore] [JsonIgnore] public int CurrentNumDefenseShips { get; private set; } 

        [XmlIgnore][JsonIgnore] public float ActualCost => Cost * CurrentGame.Pace;

        public override string ToString()
            => string.Format("BID:{0} Name:{1} ActualCost:{2} +Tax:{3}  Short:{4}", 
                             BID, Name, ActualCost, PlusTaxPercentage, ShortDescrText);

        [XmlIgnore][JsonIgnore] public string TranslatedName => Localizer.Token(NameTranslationIndex);
        [XmlIgnore][JsonIgnore] public string DescriptionText => Localizer.Token(DescriptionIndex);
        [XmlIgnore][JsonIgnore] public string ShortDescrText => Localizer.Token(ShortDescriptionIndex);

        // Each Building templates has a unique ID: 
        [XmlIgnore][JsonIgnore] public int BID { get; private set; }
        public void AssignBuildingId(int bid) => BID = bid;

        public static int CapitalId, OutpostId, BiospheresId, SpacePortId, TerraformerId;
        [XmlIgnore][JsonIgnore] public bool IsCapital => BID == CapitalId;
        [XmlIgnore][JsonIgnore] public bool IsOutpost => BID == OutpostId;
        [XmlIgnore][JsonIgnore] public bool IsCapitalOrOutpost => BID == CapitalId || BID == OutpostId;
        [XmlIgnore][JsonIgnore] public bool IsBiospheres => BID == BiospheresId;
        [XmlIgnore][JsonIgnore] public bool IsSpacePort  => BID == SpacePortId;
        [XmlIgnore][JsonIgnore] public bool IsTerraformer => BID == TerraformerId;

        // these appear in Hardcore Ruleset
        public static int FissionablesId, MineFissionablesId, FuelRefineryId;

        public void SetPlanet(Planet p)
        {
            p.BuildingList.Add(this);
            AssignBuildingToTile(p);
        }

        public Building Clone()
        {
            var b = (Building)MemberwiseClone();
            b.TheWeapon = null;
            b.CurrentNumDefenseShips = b.DefenseShipsCapacity;
            return b;
        }

        public void CreateWeapon()
        {
            if (!isWeapon)
                return;

            TheWeapon = ResourceManager.CreateWeapon(Weapon);
            Offense = TheWeapon.CalculateWeaponOffense() * 3; //360 degree angle
        }
        
        public float StrengthMax => ResourceManager.GetBuildingTemplate(BID).Strength;

        public void UpdateCurrentDefenseShips(int num)
        {
            if (DefenseShipsCapacity > 0)
                CurrentNumDefenseShips = (CurrentNumDefenseShips + num).Clamped(0,DefenseShipsCapacity);
        }

        public float ActualMaintenance(Planet p) => Maintenance + Maintenance * p.Owner.data.Traits.MaintMod;
        
        public bool ProducesProduction => PlusFlatProductionAmount > 0 || PlusProdPerColonist > 0 || PlusProdPerRichness > 0;
        public bool ProducesFood       => PlusFlatFoodAmount > 0 || PlusFoodPerColonist > 0;
        public bool ProducesPopulation => PlusFlatPopulation > 0;
        public bool IsMilitary         => CombatStrength > 0&& !IsCapitalOrOutpost && MaxPopIncrease.AlmostZero(); // FB - pop relevant because of CA

        static float Production(Planet planet, float flatBonus, float perColonistBonus, float adjust = 1)
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

        public bool AssignBuildingToTile(Planet solarSystemBody = null)
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
            if (IsOutpost || !string.IsNullOrEmpty(EventTriggerUID))
            {
                targetPGS = AssignBuildingToRandomTile(solarSystemBody);
                if (targetPGS != null)
                    return targetPGS.Habitable = true;
            }
            if (IsBiospheres)
                return AssignBuildingToRandomTile(solarSystemBody) != null;                    
            return false;            
        }

        public PlanetGridSquare AssignBuildingToRandomTile(Planet planet, bool mustBeHabitableTile = false)
        {
            PlanetGridSquare[] list = mustBeHabitableTile 
                ? planet.TilesList.Filter(pgs => pgs.building == null && pgs.Habitable) 
                : planet.TilesList.Filter(pgs => pgs.building == null);
            if (list.Length == 0)
                return null;
            PlanetGridSquare target = RandomMath.RandItem(list);
            target.building = this;
            return target;
        }

        public bool AssignBuildingToTileOnColonize(Planet planet)
        {
            if (AssignBuildingToRandomTile(planet, mustBeHabitableTile: true) != null) return true;
            return AssignBuildingToRandomTile(planet) != null;
        }

        public bool AssignBuildingToTile(Building b, ref PlanetGridSquare where, Planet planet)
        {
            // only validate the location
            if (where != null)
                return where.CanBuildHere(b);
            PlanetGridSquare[] freeSpots = planet.TilesList.Filter(pgs => pgs.CanBuildHere(b));
            if (freeSpots.Length > 0)
                where = RandomMath.RandItem(freeSpots);
            return where != null;
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
            planet.ProdHere += ActualCost / 2f;
            foreach (PlanetGridSquare planetGridSquare in planet.TilesList)
            {
                if (planetGridSquare.building != null && planetGridSquare.building == building1)
                    planetGridSquare.building = null;
            }
        }
    }
}