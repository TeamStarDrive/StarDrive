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
        public short StartingCost;
        public byte ArmoredBonus;
        public byte SensorBonus;
        public byte SpeedBonus;
        public byte CargoBonus;

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