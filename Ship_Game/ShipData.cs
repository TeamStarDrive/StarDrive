using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class ShipData
	{
		public bool Animated;

		public string ShipStyle;

		public string EventOnDeath;

		public byte experience;

		public byte Level;

		public string SelectionGraphic = "";

		public string Name;

		public bool HasFixedCost;

		public short FixedCost;

		public bool IsShipyard;

		public bool IsOrbitalDefense;

		public string IconPath;

		public Ship_Game.Gameplay.CombatState CombatState = Ship_Game.Gameplay.CombatState.AttackRuns;

		public float MechanicalBoardingDefense;

		public string Hull;

		public string Role;

		public List<ShipToolScreen.ThrusterZone> ThrusterList;

		public string ModelPath;

		public AIState DefaultAIState;

        //Added by McShooterz: New tags for Hull modifiers
        public short StartingCost; // additional cost to build
        public byte ArmoredBonus; // % damage reduction
        public byte SensorBonus; // % sensor range
        public byte SpeedBonus; // % speed increase
        public byte CargoBonus; // % cargo room
        public byte FireRateBonus; // % fire rate
        public byte RepairBonus; // % repair rate
        public byte CostBonus;  // % cost reduction

		public List<ModuleSlotData> ModuleSlotList = new List<ModuleSlotData>();

		public ShipData()
		{
		}

		public ShipData GetClone()
		{
			return (ShipData)this.MemberwiseClone();
		}
	}
}