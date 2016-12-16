using Ship_Game.Gameplay;
using MsgPack.Serialization;

namespace Ship_Game
{
	public sealed class Building
	{
        [MessagePackMember(0)] public string Name;
        [MessagePackMember(1)] public bool IsSensor;
        [MessagePackMember(2)] public bool NoRandomSpawn;
		[MessagePackMember(3)] public bool AllowShipBuilding;
		[MessagePackMember(4)] public int NameTranslationIndex;
		[MessagePackMember(5)] public int DescriptionIndex;
		[MessagePackMember(6)] public int ShortDescriptionIndex;
		[MessagePackMember(7)] public string ResourceCreated;
		[MessagePackMember(8)] public string ResourceConsumed;
		[MessagePackMember(9)] public float ConsumptionPerTurn;
		[MessagePackMember(10)] public float OutputPerTurn;
		[MessagePackMember(11)] public string CommodityRequired;
		[MessagePackMember(12)] public CommodityBonusType CommodityBonusType;
		[MessagePackMember(13)] public float CommodityBonusAmount;
		[MessagePackMember(14)] public bool IsCommodity;
		[MessagePackMember(15)] public bool WinsGame;
		[MessagePackMember(16)] public bool BuildOnlyOnce;
		[MessagePackMember(17)] public string EventOnBuild;
		[MessagePackMember(18)] public string EventTriggerUID = "";
		[MessagePackMember(19)] public bool EventWasTriggered;
		[MessagePackMember(20)] public bool CanBuildAnywhere;
		[MessagePackMember(21)] public float PlusTerraformPoints;
		[MessagePackMember(22)] public int Strength = 5;
		[MessagePackMember(23)] public float PlusProdPerRichness;
		[MessagePackMember(24)] public float PlanetaryShieldStrengthAdded;
		[MessagePackMember(25)] public float PlusFlatPopulation;
		[MessagePackMember(26)] public float MinusFertilityOnBuild;
		[MessagePackMember(27)] public string Icon;
		[MessagePackMember(28)] public bool Scrappable = true;
		[MessagePackMember(29)] public bool Unique = true;
		[MessagePackMember(30)] public bool isWeapon;
		[MessagePackMember(31)] public string Weapon = "";
		[MessagePackMember(33)] public float WeaponTimer;
		[MessagePackMember(34)] public float AttackTimer;
		[MessagePackMember(35)] public int AvailableAttackActions = 1;
		[MessagePackMember(36)] public int CombatStrength;
		[MessagePackMember(37)] public int SoftAttack;
		[MessagePackMember(38)] public int HardAttack;
		[MessagePackMember(39)] public int Defense;
		[MessagePackMember(40)] public float PlusTaxPercentage;
		[MessagePackMember(41)] public bool AllowInfantry;
		[MessagePackMember(42)] public float Maintenance;
		[MessagePackMember(43)] public float Cost;
		[MessagePackMember(44)] public int StorageAdded;
		[MessagePackMember(45)] public float PlusResearchPerColonist;
		[MessagePackMember(46)] public string ExcludesPlanetType = "";
		[MessagePackMember(47)] public float PlusFlatResearchAmount;
		[MessagePackMember(48)] public float CreditsPerColonist;
		[MessagePackMember(49)] public float PlusFlatFoodAmount;
		[MessagePackMember(50)] public float PlusFoodPerColonist;
		[MessagePackMember(51)] public float MaxPopIncrease;
		[MessagePackMember(52)] public float PlusProdPerColonist;
		[MessagePackMember(53)] public float PlusFlatProductionAmount;
        [MessagePackMember(54)] public float SensorRange;
        [MessagePackMember(55)] public bool IsProjector;
        [MessagePackMember(56)] public float ProjectorRange;
        [MessagePackMember(57)] public float ShipRepair;
        [MessagePackMember(58)] public BuildingCategory Category = BuildingCategory.General;
        [MessagePackMember(59)] public bool IsPlayerAdded = false;

        [MessagePackIgnore] public Weapon theWeapon;

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
	}
}