using Ship_Game.AI;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using Ship_Game.Data;

namespace Ship_Game.Ships
{
    /// <summary>
    /// This describes an unique Ship Design, such as `Vulcan Scout` and serves as a template
    /// for spawning new ships.
    ///
    /// This class is Serialized/Deserialized using a custom text-based format
    /// </summary>
    public sealed partial class ShipDesign : IShipDesign
    {
        // Current version of ShipData files
        // If we introduce incompatibilities we need to convert old to new
        const int Version = 1;

        public bool ThisClassMustNotBeAutoSerializedByDotNet =>
            throw new InvalidOperationException(
                $"BUG! ShipData must not be automatically serialized! Add [XmlIgnore][JsonIgnore] to `public ShipData XXX;` PROPERTIES/FIELDS. {this}");

        public string Name { get; set; } // ex: "Dodaving", just an arbitrary name
        public string Hull { get; set; }  // ID of the hull, ex: "Cordrazine/Dodaving"
        public string ModName { get; set; } = ""; // "" if vanilla, else mod name eg "Combined Arms"
        public string ShipStyle { get; set; } // "Terran"
        public string Description { get; set; } // "Early Rocket fighter, great against unshielded foes, but die easily"
        public string IconPath { get; set; } // "ShipIcons/shuttle"
        
        public string EventOnDeath { get; set; }
        public string SelectionGraphic { get; set; } = "";

        public float FixedUpkeep { get; set; }
        public int FixedCost { get; set; }
        public bool IsShipyard { get; set; }
        public bool IsOrbitalDefense { get; set; }
        public bool IsCarrierOnly { get; set; } // this ship is restricted to Carriers only

        public ShipCategory ShipCategory { get; set; } = ShipCategory.Unclassified;
        public HangarOptions HangarDesignation { get; set; } = HangarOptions.General;
        public AIState DefaultAIState { get; set; } = AIState.AwaitingOrders;
        public CombatState DefaultCombatState { get; set; } = CombatState.AttackRuns;

        public ShipGridInfo GridInfo { get; set; }

        // All the slots of the ShipDesign
        // NOTE: This is loaded on-demand when a new ship is being initialized
        DesignSlot[] DesignSlots;

        // Complete list of all the unique module UID-s found in this design
        public string[] UniqueModuleUIDs { get; private set; } = Empty<string>.Array;

        // Maps each DesignSlot to `UniqueModuleUIDs`
        public ushort[] SlotModuleUIDMapping { get; private set; } = Empty<ushort>.Array;

        public bool Unlockable { get; set; } = true; // unlocked=true by default
        public HashSet<string> TechsNeeded { get; set; } = new HashSet<string>();

        // BaseHull is the template layout of the ship hull design
        public ShipHull BaseHull { get; }
        public HullBonus Bonuses { get; }
        public FileInfo Source { get; }

        public bool IsPlayerDesign { get; set; }
        public bool IsReadonlyDesign { get; set; }
        public bool Deleted { get; set; }

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
        public void SetDesignSlots(DesignSlot[] slots)
        {
            var moduleUIDs = new HashSet<string>();
            for (int i = 0; i < slots.Length; ++i)
                moduleUIDs.Add(slots[i].ModuleUID);

            DesignSlots = slots;
            SetModuleUIDs(moduleUIDs.ToArray());

            Role = HullRole; // make sure to reset ship role before recalculating it
            InitializeCommonStats(BaseHull, slots, updateRole:true);
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

        public bool AreModulesEqual(ModuleSaveData[] saved)
        {
            if (DesignSlots.Length != saved.Length)
                return false;

            for (int i = 0; i < saved.Length; ++i)
                if (DesignSlots[i].ModuleUID != saved[i].ModuleUID) // it is enough to test only module UID-s
                    return false;
            return true;
        }

        public static ShipDesign FromSave(ModuleSaveData[] saved, string[] moduleUIDs, IShipDesign template)
        {
            // savedModules are different, grab the existing template's defaults but apply the new ship's modules
            // this is pretty inefficient but it's currently the only way to handle obsolete designs without crashing
            // TODO: implement obsolete ships and ship versioning
            ShipDesign data = template.GetClone(null);

            data.SetModuleUIDs(moduleUIDs);
            data.DesignSlots = new DesignSlot[saved.Length];
            for (int i = 0; i < saved.Length; ++i)
                data.DesignSlots[i] = saved[i].ToDesignSlot();

            return data;
        }

        public static ShipDesign FromSave(ModuleSaveData[] saved, string[] moduleUIDs, SavedGame.ShipSaveData save, ShipHull hull)
        {
            var data = new ShipDesign(hull, save.Name);
            data.ModName = GlobalStats.ModName;

            data.SetModuleUIDs(moduleUIDs);
            data.DesignSlots = new DesignSlot[saved.Length];
            for (int i = 0; i < saved.Length; ++i)
                data.DesignSlots[i] = saved[i].ToDesignSlot();

            data.InitializeCommonStats(hull, data.DesignSlots);
            return data;
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
