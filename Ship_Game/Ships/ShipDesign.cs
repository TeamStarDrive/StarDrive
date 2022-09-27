using Ship_Game.AI;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using SDGraphics;
using SDUtils;
using Ship_Game.Data;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Ships
{
    /// <summary>
    /// This describes an unique Ship Design, such as `Vulcan Scout` and serves as a template
    /// for spawning new ships.
    ///
    /// This class is Serialized/Deserialized using a custom text-based format
    /// </summary>
    [StarDataType]
    public sealed partial class ShipDesign : IShipDesign
    {
        // Current version of ShipData files
        // If we introduce incompatibilities we need to convert old to new
        const int Version = 1;
        [StarData] public string Name { get; set; } // ex: "Dodaving", just an arbitrary name
        [StarData] public string Hull { get; set; }  // ID of the hull, ex: "Cordrazine/Dodaving"
        [StarData] public string ModName { get; set; } = ""; // "" if vanilla, else mod name eg "Combined Arms"
        [StarData] public string ShipStyle { get; set; } // "Terran"
        [StarData] public string Description { get; set; } // "Early Rocket fighter, great against unshielded foes, but die easily"
        [StarData] public string IconPath { get; set; } // "ShipIcons/shuttle"
        
        // Role expressed by this ShipDesign's modules, such as `Carrier`
        // This is saved in Shipyard, or can be updated via --fix-roles
        [StarData] public RoleName Role { get; private set; } = RoleName.fighter;

        [StarData] public string EventOnDeath { get; set; }
        [StarData] public string SelectionGraphic { get; set; } = "";

        [StarData] public float FixedUpkeep { get; set; }
        [StarData] public int FixedCost { get; set; }
        [StarData] public bool IsShipyard { get; set; }
        [StarData] public bool IsOrbitalDefense { get; set; }
        [StarData] public bool IsCarrierOnly { get; set; } // this ship is restricted to Carriers only

        [StarData] public ShipCategory ShipCategory { get; set; } = ShipCategory.Unclassified;
        [StarData] public HangarOptions HangarDesignation { get; set; } = HangarOptions.General;
        [StarData] public CombatState DefaultCombatState { get; set; }

        public ModuleGridFlyweight Grid { get; private set; }
        [StarData] public ShipGridInfo GridInfo { get; set; }

        // All the slots of the ShipDesign
        // NOTE: This is loaded on-demand when a new ship is being initialized
        [StarData] DesignSlot[] DesignSlots;

        // Complete list of all the unique module UID-s found in this design
        public string[] UniqueModuleUIDs { get; private set; } = Empty<string>.Array;

        // Maps each DesignSlot to `UniqueModuleUIDs`
        public ushort[] SlotModuleUIDMapping { get; private set; } = Empty<ushort>.Array;

        public bool Unlockable { get; set; } = true; // unlocked=true by default
        public HashSet<string> TechsNeeded { get; set; } = new();

        // BaseHull is the template layout of the ship hull design
        public ShipHull BaseHull { get; private set; }
        public HullBonus Bonuses { get; private set; }
        public FileInfo Source { get; set; }

        [StarData] public bool IsPlayerDesign { get; set; }
        [StarData] public bool IsReadonlyDesign { get; set; }
        public bool Deleted { get; set; }
        public bool IsFromSave { get; set; }

        public bool IsValidForCurrentMod => ModName.IsEmpty() || ModName == GlobalStats.ModName;

        // You should always use this `Icon` property, because of bugs with `IconPath` initialization
        // when a ShipData is copied. @todo Fix ShipData copying
        public SubTexture Icon => ResourceManager.Texture(IconPath);

        public override string ToString() { return Name; }

        public ShipDesign()
        {
        }

        // Create a new empty ShipData from a ShipHull
        public ShipDesign(ShipHull hull, string newName)
        {
            Name = newName;
            Hull = hull.HullName;
            ModName = GlobalStats.ModName;
            ShipStyle = hull.Style;
            Description = hull.Description;
            IconPath = hull.IconPath;
            GridInfo = new ShipGridInfo(hull);

            Role = hull.Role;
            SelectionGraphic = hull.SelectIcon;

            IsShipyard       = hull.IsShipyard;
            IsOrbitalDefense = hull.IsOrbitalDefense;
            TechsNeeded = new HashSet<string>(hull.TechsNeeded);
            BaseHull = hull;
            Bonuses  = hull.Bonuses;

            Unlockable = hull.Unlockable;
            DesignSlots = Empty<DesignSlot>.Array;

            InitializeCommonStats(BaseHull, DesignSlots);
        }

        // called when this object has been fully deserialized
        [StarDataDeserialized]
        void OnDeserialized()
        {
            if (!ResourceManager.Hull(Hull, out ShipHull hull)) // If the hull is invalid, then ship loading fails!
                return;

            if (!IsValidForCurrentMod || !hull.IsValidForCurrentMod)
            {
                Role = RoleName.disabled;
                return; // this design doesn't need to be parsed
            }

            BaseHull = hull;
            Bonuses = hull.Bonuses;
            SetDesignSlots(DesignSlots, updateRole:false);
        }

        // Deep clone of this ShipDesign
        // Feel free to edit the cloned design
        public ShipDesign GetClone(string newName)
        {
            var clone = (ShipDesign)MemberwiseClone();
            clone.TechsNeeded = new HashSet<string>(TechsNeeded);
            clone.DesignSlots = new DesignSlot[DesignSlots.Length];
            for (int i = 0; i < DesignSlots.Length; ++i)
                clone.DesignSlots[i] = new DesignSlot(DesignSlots[i]);
            if (newName.NotEmpty())
                clone.Name = newName;
            return clone;
        }

        // Marks the this design as Deleted and performs
        // aggressive cleanup of ShipDesign to assist the Garbage Collector
        // Which is not always able to clean up everything due to dangling references
        public void Dispose()
        {
            Deleted = true;
            DesignSlots = Empty<DesignSlot>.Array;
            Hangars = Empty<ShipModule>.Array;
            AllFighterHangars = Empty<ShipModule>.Array;
            Weapons = Empty<Weapon>.Array;
        }

        // Sets the new design slots and calculates Unique Module UIDs
        public void SetDesignSlots(DesignSlot[] slots, bool updateRole = true)
        {
            var moduleUIDs = new HashSet<string>();
            for (int i = 0; i < slots.Length; ++i)
                moduleUIDs.Add(slots[i].ModuleUID);

            DesignSlots = slots;
            SetModuleUIDs(moduleUIDs.ToArr());

            // TODO: this violates proper software design, @see RoleData and fix this
            if (updateRole)
                Role = HullRole; // make sure to reset ship role before recalculating it
            InitializeCommonStats(BaseHull, slots, updateRole);
        }

        void SetModuleUIDs(string[] moduleUIDs)
        {
            UniqueModuleUIDs = moduleUIDs;

            var UIDtoModuleUIDsIdx = new Map<string, int>(); // Module UID => UniqueModuleUIDs Index

            if (moduleUIDs.Length != 0 && DesignSlots.Length == 0)
                Log.Error("SetModuleUIDs failed: DesignSlots was empty");

            // [i] maps to => UniqueModuleUIDs Index
            var slotModuleUIDMapping = new ushort[DesignSlots.Length];
            for (int i = 0, count = 0; i < DesignSlots.Length; ++i)
            {
                string uid = DesignSlots[i].ModuleUID;
                if (UIDtoModuleUIDsIdx.TryGetValue(uid, out int moduleUIDIdx))
                {
                    slotModuleUIDMapping[i] = (ushort)moduleUIDIdx;
                }
                else
                {
                    slotModuleUIDMapping[i] = (ushort)count;
                    UIDtoModuleUIDsIdx.Add(uid, count);
                    ++count;
                }
            }
            SlotModuleUIDMapping = slotModuleUIDMapping;
        }

        public DesignSlot[] GetOrLoadDesignSlots()
        {
            if (DesignSlots == null && Source != null)
            {
                DesignSlots = LoadDesignSlots(Source, UniqueModuleUIDs);
            }
            return DesignSlots;
        }

        public static ShipDesign Parse(string filePath)
        {
            var file = new FileInfo(filePath);
            return Parse(file);
        }

        public static ShipDesign Parse(FileInfo info)
        {
            try
            {
                return ParseDesign(info);
            }
            catch (Exception e)
            {
                Log.Error(e, $"Failed to parse ShipData '{info.FullName}'", 0);
            }
            return null;
        }

        public bool AreModulesEqual(ShipDesign savedDesign)
        {
            var saved = savedDesign.GetOrLoadDesignSlots();
            var ours = GetOrLoadDesignSlots();
            if (ours.Length != saved.Length)
                return false;

            for (int i = 0; i < saved.Length; ++i)
                if (ours[i].ModuleUID != saved[i].ModuleUID) // it is enough to test only module UID-s
                    return false;
            return true;
        }

        public void LoadModel(out SceneObject shipSO, GameContentManager content)
        {
            BaseHull.LoadModel(out shipSO, content);
        }

        public static RoleType ShipRoleToRoleType(RoleName role)
        {
            switch (role)
            {
                case RoleName.disabled:  return RoleType.NotApplicable;
                case RoleName.ssp:
                case RoleName.construction:
                case RoleName.shipyard:  return RoleType.EmpireSupport;
                case RoleName.colony:
                case RoleName.scout:
                case RoleName.freighter: return RoleType.Civilian;
                case RoleName.platform:
                case RoleName.station:   return RoleType.Orbital;
                case RoleName.supply:    return RoleType.NotApplicable;
                case RoleName.support:
                case RoleName.carrier:
                case RoleName.bomber:    return RoleType.WarSupport;
                case RoleName.troop:
                case RoleName.troopShip: return RoleType.Troop;
                case RoleName.fighter:
                case RoleName.gunboat:
                case RoleName.drone:
                case RoleName.corvette:
                case RoleName.frigate:
                case RoleName.destroyer:
                case RoleName.cruiser:
                case RoleName.battleship:
                case RoleName.capital:
                case RoleName.prototype: return RoleType.Warship;
                default:
                    return RoleType.NotApplicable;
            }
        }
    }
}
