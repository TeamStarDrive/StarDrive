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

        [XmlIgnore][JsonIgnore] public Weapon theWeapon;

        public void SetPlanet(Planet p)
		{
			p.BuildingList.Add(this);
			p.AssignBuildingToTile(this);
		}

		public Building Clone()
		{
		    Building b = (Building)MemberwiseClone();
            b.theWeapon = null;
            return b;
		}

        public bool ProducesProduction => PlusFlatProductionAmount >0 || PlusProdPerColonist >0 || PlusProdPerRichness >0;
        public bool ProducesFood => PlusFlatFoodAmount >0 || PlusFoodPerColonist > 0;
	}
}