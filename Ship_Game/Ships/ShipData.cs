using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Ship_Game.Data;

namespace Ship_Game.Ships
{
    // @note
    // ShipData templates from root content and save designs is loaded using SDNative
    // Game Save/Load uses a separate serializer which used to be XML, but now is Json
    // @note Saving ShipData designs is still done in XML -- should change that!
    //       However, we will have to support XML for a long time to have backwards compat.
    public sealed partial class ShipData
    {
        // Current version of ShipData files
        // If we introduce incompatibilities we need to convert old to new
        const int Version = 1;

        public bool ThisClassMustNotBeAutoSerializedByDotNet =>
            throw new InvalidOperationException(
                $"BUG! ShipData must not be automatically serialized! Add [XmlIgnore][JsonIgnore] to `public ShipData XXX;` PROPERTIES/FIELDS. {this}");

        public string Name; // ex: "Dodaving", just an arbitrary name
        public string Hull; // ID of the hull, ex: "Cordrazine/Dodaving"
        public string ModName = ""; // "" if vanilla, else mod name eg "Combined Arms"
        public string ShipStyle; // "Terran"
        public string Description; // "Early Rocket fighter, great against unshielded foes, but die easily"
        public string IconPath; // "ShipIcons/shuttle"
        
        public string EventOnDeath;
        public string SelectionGraphic = "";

        public float FixedUpkeep;
        public int FixedCost;
        public bool IsShipyard;
        public bool IsOrbitalDefense;
        // The Doctor: intending to use this as a user-toggled
        // flag which tells the AI not to build a design as a stand-alone vessel
        // from a planet; only for use in a hangar
        public bool CarrierShip;

        public RoleName Role = RoleName.fighter;
        public Category ShipCategory = Category.Unclassified;
        public HangarOptions HangarDesignation = HangarOptions.General;
        public AIState DefaultAIState;
        public CombatState DefaultCombatState;

        public ShipGridInfo GridInfo;
        public DesignSlot[] ModuleSlots;
        public bool Unlockable = true; // unlocked=true by default
        public HashSet<string> TechsNeeded = new HashSet<string>();

        static readonly string[] RoleArray     = typeof(RoleName).GetEnumNames();
        static readonly string[] CategoryArray = typeof(Category).GetEnumNames();
        public RoleName HullRole => BaseHull.Role;
        public ShipRole ShipRole => ResourceManager.ShipRoles[Role];

        // BaseHull is the template layout of the ship hull design
        public ShipHull BaseHull { get; private set; }
        public HullBonus Bonuses { get; }
        public FileInfo Source { get; private set; }

        public bool IsValidForCurrentMod => ModName.IsEmpty() || ModName == GlobalStats.ModName;

        // You should always use this `Icon` property, because of bugs with `IconPath` initialization
        // when a ShipData is copied. @todo Fix ShipData copying
        public SubTexture Icon => ResourceManager.Texture(IconPath);

        public ShipData()
        {
        }

        // Create a new empty ShipData from a ShipHull
        public ShipData(ShipHull hull)
        {
            Name = hull.HullName;
            Hull = hull.HullName;
            ModName = hull.ModName;
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
            ModuleSlots = Array.Empty<DesignSlot>();
        }

        public override string ToString() { return Name; }

        public static ShipData Parse(string filePath)
        {
            var file = new FileInfo(filePath);
            return Parse(file);
        }

        public static ShipData Parse(FileInfo info)
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
            if (ModuleSlots.Length != saved.Length)
                return false;

            for (int i = 0; i < saved.Length; ++i)
                if (ModuleSlots[i].ModuleUID != saved[i].ModuleUID) // it is enough to test only module UID-s
                    return false;
            return true;
        }

        public static ShipData FromSave(ModuleSaveData[] saved, ShipData template)
        {
            // savedModules are different, grab the existing template's defaults but apply the new ship's modules
            // this is pretty inefficient but it's currently the only way to handle obsolete designs without crashing
            // TODO: implement obsolete ships and ship versioning
            ShipData data = template.GetClone();
            
            data.ModuleSlots = new DesignSlot[saved.Length];
            for (int i = 0; i < saved.Length; ++i)
                data.ModuleSlots[i] = saved[i].ToDesignSlot();

            return data;
        }

        public static ShipData FromSave(ModuleSaveData[] saved, SavedGame.ShipSaveData save, ShipHull hull)
        {
            var data = new ShipData(hull);

            data.Name = save.Name;
            data.ModName = GlobalStats.ModName;

            data.ModuleSlots = new DesignSlot[saved.Length];
            for (int i = 0; i < saved.Length; ++i)
                data.ModuleSlots[i] = saved[i].ToDesignSlot();

            return data;
        }

        public ShipData GetClone()
        {
            return (ShipData)MemberwiseClone();
        }

        public string GetRole()
        {
            return RoleArray[(int)Role -1];
        }

        public static string GetRole(RoleName role)
        {
            int roleNum = (int)role - 1;
            return RoleArray[roleNum];
        }

        public string GetCategory()
        {
            return CategoryArray[(int)ShipCategory];
        }

        public void LoadModel(out SceneObject shipSO, GameContentManager content)
        {
            BaseHull.LoadModel(out shipSO, content);
        }
        
        public struct ThrusterZone
        {
            public Vector3 Position;
            [XmlElement(ElementName = "scale")]
            public float Scale;
        }

        public enum Category
        {
            Unclassified,
            Civilian,
            Recon,
            Conservative,
            Neutral,
            Reckless,
            Kamikaze
        }

        public enum HangarOptions
        {
            General,
            AntiShip,
            Interceptor
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
