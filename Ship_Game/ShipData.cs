using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class ShipData
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

        public RoleName Role = RoleName.fighter;

		public List<ShipToolScreen.ThrusterZone> ThrusterList;

		public string ModelPath;

		public AIState DefaultAIState;

        // The Doctor: intending to use this for 'Civilian', 'Recon', 'Fighter', 'Bomber' etc.
        public Category ShipCategory = Category.Unclassified;

        public RoleName HullRole
        {
            get
            {
                ShipData role = null;
                if (ResourceManager.HullsDict.TryGetValue(this.Hull, out role))
                {
                    return role.Role;
                }
                return this.Role;

            }
        }


        // The Doctor: intending to use this as a user-toggled flag which tells the AI not to build a design as a stand-alone vessel from a planet; only for use in a hangar
        public bool CarrierShip = false;
        public float BaseStrength;
        public bool BaseCanWarp;
		public List<ModuleSlotData> ModuleSlotList = new List<ModuleSlotData>();
        public bool hullUnlockable = false;
        public bool allModulesUnlocakable = true;
        public bool unLockable = false;
        //public HashSet<string> EmpiresThatCanUseThis = new HashSet<string>();
        public HashSet<string> techsNeeded = new HashSet<string>();
        public int TechScore = 0;
        //public Dictionary<string, HashSet<string>> EmpiresThatCanUseThis = new Dictionary<string, HashSet<string>>();
        private static string[] RoleArray = {"disabled","platform","station","construction","supply","freighter","troop","fighter","scout","gunboat","drone","corvette","frigate","destroyer","cruiser","carrier","capital","prototype"};
        private static string[] CategoryArray = {"Unclassified","Civilian","Recon","Combat", "Bomber", "Fighter", "Kamikaze"};

		public ShipData()
		{
		}
        

        public ShipData HullData
        {
            get 
            { 
                ShipData hull =null;
                ResourceManager.HullsDict.TryGetValue(this.Hull, out hull);
                return hull; 
            }
            
        }
        
		public ShipData GetClone()
		{
			return (ShipData)this.MemberwiseClone();
		}

        public string GetRole()
		{
            return RoleArray[(int)Role];
		}

        public string GetCategory()
        {
            return CategoryArray[(int)ShipCategory];
        }

        public enum Category
        {
            Unclassified,
            Civilian,
            Recon,
            Combat,
            Bomber,
            Fighter,            
            Kamikaze
        }

        public enum RoleName
        {
            disabled,
            platform,
            station,
            construction,
            supply,
            freighter,
            troop,
            fighter,
            scout,
            gunboat,
            drone,
            corvette,
            frigate,
            destroyer,
            cruiser,
            carrier,
            capital,
            prototype
        }
	}
}