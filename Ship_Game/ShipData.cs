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

        public bool HasFixedUpkeep;

        public float FixedUpkeep;

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
        public byte DamageBonus; // % weapon damage increase
        public byte ShieldBonus; // % shield power increase

        // The Doctor: intending to use this for 'Civilian', 'Recon', 'Fighter', 'Bomber' etc.
        public Category ShipCategory = Category.Unclassified;

        // The Doctor: intending to use this as a user-toggled flag which tells the AI not to build a design as a stand-alone vessel from a planet; only for use in a hangar
        public bool CarrierShip = false;
        public float BaseStrength;
        public bool BaseCanWarp;
		public List<ModuleSlotData> ModuleSlotList = new List<ModuleSlotData>();

		public ShipData()
		{
		}

		public ShipData GetClone()
		{
			return (ShipData)this.MemberwiseClone();
		}

        public enum Category
        {
            Unclassified,
            Civilian,
            Recon,
            Fighter,
            Bomber,
        }
	}
}