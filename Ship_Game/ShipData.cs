using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;

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
        public CombatState CombatState = CombatState.AttackRuns;
        public float MechanicalBoardingDefense;
        public string Hull;
        public RoleName Role = RoleName.fighter;
        public List<SDNative.ThrusterZone> ThrusterList;
        public string ModelPath;
        public AIState DefaultAIState;
        // The Doctor: intending to use this for 'Civilian', 'Recon', 'Fighter', 'Bomber' etc.
        public Category ShipCategory = Category.Unclassified;

        // @todo This lookup is expensive and never changes once initialized, find a way to initialize this properly
        public RoleName HullRole => ResourceManager.HullsDict.TryGetValue(Hull, out ShipData role) ? role.Role : Role;
        public ShipData HullData => ResourceManager.HullsDict.TryGetValue(Hull, out ShipData hull) ? hull : null;

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
        private static readonly string[] RoleArray     = typeof(RoleName).GetEnumNames();
        private static readonly string[] CategoryArray = typeof(Category).GetEnumNames();

        public ShipData()
        {
        }

        // Added by RedFox - manual parsing of ShipData, because this is the slowest part in loading
        public static ShipData Parse(FileInfo info)
        {
            var s = new SDNative.ShipDataSerializer();
            if (s.LoadFromFile(info.FullName))
            {
                ShipData ship = new ShipData()
                {
                    Animated       = s.Animated,
                    ShipStyle      = s.ShipStyle,
                    EventOnDeath   = s.EventOnDeath,
                    experience     = s.Experience,
                    Level          = s.Level,
                    Name           = s.Name,
                    HasFixedCost   = s.HasFixedCost,
                    FixedCost      = s.FixedCost,
                    HasFixedUpkeep = s.HasFixedUpkeep,
                    FixedUpkeep    = s.FixedUpkeep,
                    IsShipyard     = s.IsShipyard,
                    IconPath       = s.IconPath,
                    Hull           = s.Hull,
                    ModelPath      = s.ModelPath,
                    CarrierShip    = s.CarrierShip,
                    BaseStrength   = s.BaseStrength,
                    BaseCanWarp    = s.BaseCanWarp,
                    hullUnlockable = s.HullUnlockable,
                    unLockable     = s.UnLockable,
                    TechScore      = s.TechScore,
                    IsOrbitalDefense = s.IsOrbitalDefense,
                    SelectionGraphic = s.SelectionGraphic,
                    allModulesUnlocakable = s.AllModulesUnlocakable,
                    MechanicalBoardingDefense = s.MechanicalBoardingDefense
                };
                Enum.TryParse(s.Role,           out ship.Role);
                Enum.TryParse(s.CombatState,    out ship.CombatState);
                Enum.TryParse(s.ShipCategory,   out ship.ShipCategory);
                Enum.TryParse(s.DefaultAIState, out ship.DefaultAIState);

                // @todo Remove conversion to List
                // @todo Remove SDNative.ModuleSlotData conversion
                var moduleSlots = s.GetModuleSlotList();
                ship.ModuleSlotList = new List<ModuleSlotData>(moduleSlots.Length);
                foreach (SDNative.ModuleSlotData msd in moduleSlots)
                {
                    ModuleSlotData slot = new ModuleSlotData
                    {
                        Position = new Vector2(msd.PositionX, msd.PositionY),
                        InstalledModuleUID = msd.InstalledModuleUID,
                        HangarshipGuid = msd.HangarshipGuid,
                        Health         = msd.Health,
                        Shield_Power   = msd.ShieldPower,
                        facing         = msd.Facing,
                        SlotOptions    = msd.SlotOptions
                    };
                    Enum.TryParse(msd.State, out slot.state);
                    Enum.TryParse(msd.Restrictions, out slot.Restrictions);
                    ship.ModuleSlotList.Add(slot);
                }

                // @todo Remove conversion to List
                ship.ThrusterList = new List<SDNative.ThrusterZone>(s.GetThrusterZones());
                ship.techsNeeded = new HashSet<string>(s.GetTechsNeeded());

                return ship;
            }
            throw new InvalidDataException(s.ErrorMessage);
        }

        public ShipData GetClone()
        {
            return (ShipData)MemberwiseClone();
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