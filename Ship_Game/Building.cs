using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class Building
	{
		public string Name;

		public bool NoRandomSpawn;

		public bool AllowShipBuilding;

		public int NameTranslationIndex;

		public int DescriptionIndex;

		public int ShortDescriptionIndex;

		public string ResourceCreated;

		public string ResourceConsumed;

		public float ConsumptionPerTurn;

		public float OutputPerTurn;

		public string CommodityRequired;

		public Ship_Game.CommodityBonusType CommodityBonusType;

		public float CommodityBonusAmount;

		public bool IsCommodity;

		public bool WinsGame;

		public bool BuildOnlyOnce;

		public string EventOnBuild;

		public string EventTriggerUID = "";

		public bool EventWasTriggered;

		public bool CanBuildAnywhere;

		public float PlusTerraformPoints;

		public int Strength = 5;

		public float PlusProdPerRichness;

		public float PlanetaryShieldStrengthAdded;

		public float PlusFlatPopulation;

		public float MinusFertilityOnBuild;

		public string Icon;

		public bool Scrappable = true;

		public bool Unique = true;

		public bool isWeapon;

		public string Weapon = "";

		public Ship_Game.Gameplay.Weapon theWeapon;

		public float WeaponTimer;

		public float AttackTimer;

		public int AvailableAttackActions = 1;

		public int CombatStrength;

		public int SoftAttack;

		public int HardAttack;

		public int Defense;

		public float PlusTaxPercentage;

		public bool AllowInfantry;

		public float Maintenance;

		public float Cost;

		public int StorageAdded;

		public float PlusResearchPerColonist;

		public string ExcludesPlanetType = "";

		public float PlusFlatResearchAmount;

		public float CreditsPerColonist;

		public float PlusFlatFoodAmount;

		public float PlusFoodPerColonist;

		public float MaxPopIncrease;

		public float PlusProdPerColonist;

		public float PlusFlatProductionAmount;

        public float SensorRange;

		public Building()
		{
		}

		public void SetPlanet(Planet p)
		{
			p.BuildingList.Add(this);
			p.AssignBuildingToTile(this);
		}

		public void Update()
		{
		}
	}
}