using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Ship_Game.Data;
using Ship_Game.Data.Mesh;

namespace Ship_Game.Ships
{
    // @note
    // ShipData templates from root content and save designs is loaded using SDNative
    // Game Save/Load uses a separate serializer which used to be XML, but now is Json
    // @note Saving ShipData designs is still done in XML -- should change that!
    //       However, we will have to support XML for a long time to have backwards compat.
    public sealed partial class ShipData
    {
        public static bool UseNewShipDataLoaders = false;
        public static bool GenerateNewHullFiles = false; // only need to do this once
        public static bool GenerateNewDesignFiles = false; // only need to do this once
        const int CurrentVersion = 1;

        public bool ThisClassMustNotBeAutoSerializedByDotNet =>
            throw new InvalidOperationException(
                $"BUG! ShipData must not be automatically serialized! Add [XmlIgnore][JsonIgnore] to `public ShipData XXX;` PROPERTIES/FIELDS. {this}");

        public string Name; // ex: "Dodaving", just an arbitrary name
        public string Hull; // ID of the hull, ex: "Cordrazine/Dodaving"
        public string ModName = ""; // "" if vanilla, else mod name eg "Combined Arms"
        public string ShipStyle; // "Terran"
        public string Description; // "Early Rocket fighter, great against unshielded foes, but die easily"
        public string IconPath; // "ShipIcons/shuttle"
        public string ModelPath; // "Model/Ships/Terran/Shuttle/ship08"
        
        public string EventOnDeath;
        public string SelectionGraphic = "";

        public float FixedUpkeep;
        public int FixedCost;
        public bool Animated;
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

        public ThrusterZone[] ThrusterList;

        public ShipGridInfo GridInfo;

        public DesignSlot[] ModuleSlots;
        public bool UnLockable;
        public bool HullUnlockable;
        public bool AllModulesUnlockable = true;
        public HashSet<string> TechsNeeded = new HashSet<string>();

        static readonly string[] RoleArray     = typeof(RoleName).GetEnumNames();
        static readonly string[] CategoryArray = typeof(Category).GetEnumNames();
        public RoleName HullRole => BaseHull.Role;
        public ShipRole ShipRole => ResourceManager.ShipRoles[Role];

        // BaseHull is the template layout of the ship hull design
        public ShipHull BaseHull { get; internal set; }
        public HullBonus Bonuses { get; private set; }

        // Model path of the template hull layout
        public string HullModel => BaseHull.ModelPath;

        public bool IsValidForCurrentMod
            => ModName.IsEmpty() || ModName == GlobalStats.ModName;

        // You should always use this `Icon` property, because of bugs with `IconPath` initialization
        // when a ShipData is copied. @todo Fix ShipData copying
        public SubTexture Icon => ResourceManager.Texture(IconPath);
        public Vector3 Volume { get; private set; }
        public float ModelZ { get; private set; }

        public ShipData()
        {
        }

        // Make a DEEP COPY from a `hull` template
        // This is used in ShipDesignScreen's DesignModuleGrid
        public ShipData(ShipData design)
        {
            Name = design.Name;
            GridInfo = design.GridInfo;

            InitCommonState(design);
            UpdateBaseHull();

            ModuleSlots = null; // these must be initialized by DesignModuleGrid.cs
        }

        void InitCommonState(ShipData hull)
        {
            Hull              = hull.Hull;
            Role              = hull.Role;
            Animated          = hull.Animated;
            IconPath          = hull.IconPath;
            IsShipyard        = hull.IsShipyard;
            IsOrbitalDefense  = hull.IsOrbitalDefense;
            ModelPath         = hull.HullModel;
            ShipStyle         = hull.ShipStyle;
            ThrusterList      = hull.ThrusterList;
            ShipCategory      = hull.ShipCategory;
            HangarDesignation = hull.HangarDesignation;
            CarrierShip       = hull.CarrierShip;
            TechsNeeded       = hull.TechsNeeded;
            BaseHull          = hull.BaseHull;
            Bonuses           = hull.Bonuses;

            UnLockable = hull.UnLockable;
            HullUnlockable = hull.HullUnlockable;
            AllModulesUnlockable = hull.AllModulesUnlockable;

            Volume = hull.Volume;
            ModelZ = hull.ModelZ;
        }

        void FixMissingFields()
        {
            if (ShipStyle.IsEmpty())
                ShipStyle = BaseHull.Style;

            if (IconPath.IsEmpty())
                IconPath = BaseHull.IconPath;
        }

        public void UpdateGridInfo()
        {
            GridInfo = new ShipGridInfo(ModuleSlots);
        }

        public void UpdateBaseHull()
        {
            if (BaseHull == null)
            {
                if (Hull.NotEmpty() && ResourceManager.Hull(Hull, out ShipHull hull))
                {
                    BaseHull = hull;
                }
                else
                {
                    Log.Warning(ConsoleColor.Red, $"ShipData '{Name}' cannot find hull: {Hull}");
                    if (Hull.IsEmpty())
                        Hull = ShipStyle + "/" + Name;
                    if (ResourceManager.Hull(Hull, out hull))
                        BaseHull = hull;
                    else
                        throw new Exception($"ShipData '{Name}' has no viable hull named '{Hull}'");
                }
            }

            Bonuses = BaseHull.Bonuses;

            FixMissingFields();
        }

        public override string ToString() { return Name; }

        public static ShipData Parse(FileInfo info, bool isHullDefinition)
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

        public static bool IsAllDummySlots(ModuleSlotData[] slots)
        {
            for (int i = 0; i < slots.Length; ++i)
                if (!slots[i].IsDummy)
                    return false;
            return true;
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

        public enum RoleName
        {
            disabled = 1,
            shipyard,
            ssp,
            platform,
            station,
            construction,
            colony,
            supply,
            freighter,
            troop,
            troopShip, // Design role
            support,   // Design role
            bomber,    // Design role
            carrier,   // Design role
            fighter,
            scout,
            gunboat,
            drone,
            corvette,
            frigate,
            destroyer,
            cruiser,
            battleship,
            capital, 
            prototype
        }
        public enum RoleType
        {
            Civilian,
            Orbital,
            EmpireSupport,
            Warship,
            WarSupport,
            Troop,
            NotApplicable
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
