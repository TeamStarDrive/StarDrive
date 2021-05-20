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
        static bool UseNewShipDataLoaders = false;

        public string Name; // ex: "Dodaving", just an arbitrary name
        public string Hull; // ID of the hull, ex: "Cordrazine/Dodaving"
        public string ModName; // null if vanilla, else mod name eg "Combined Arms"
        public string ShipStyle; // "Terran"
        public string IconPath; // "ShipIcons/shuttle"
        public string ModelPath; // "Model/Ships/Terran/Shuttle/ship08"
        
        public byte Level;
        public byte experience;

        public string EventOnDeath;
        public string SelectionGraphic = "";

        public float MechanicalBoardingDefense;
        public float FixedUpkeep;
        public short FixedCost;
        public bool HasFixedCost;
        public bool HasFixedUpkeep;
        public bool Animated;
        public bool IsShipyard;
        public bool IsOrbitalDefense;
        // The Doctor: intending to use this as a user-toggled flag which tells the AI not to build a design as a stand-alone vessel from a planet; only for use in a hangar
        public bool CarrierShip;

        public CombatState CombatState;
        public RoleName Role = RoleName.fighter;
        public Category ShipCategory = Category.Unclassified;
        public HangarOptions HangarDesignation = HangarOptions.General;
        public AIState DefaultAIState;

        public ThrusterZone[] ThrusterList;

        [XmlIgnore] [JsonIgnore] public ShipGridInfo GridInfo;

        [XmlIgnore] [JsonIgnore] public float BaseStrength;
        [XmlArray(ElementName = "ModuleSlotList")] public ModuleSlotData[] ModuleSlots;
        [XmlIgnore] [JsonIgnore] public bool UnLockable;
        [XmlIgnore] [JsonIgnore] public bool HullUnlockable;
        [XmlIgnore] [JsonIgnore] public bool AllModulesUnlockable = true;
        [XmlArray(ElementName = "techsNeeded")] public HashSet<string> TechsNeeded = new HashSet<string>();
        [XmlIgnore] [JsonIgnore] public int TechScore;

        static readonly string[] RoleArray     = typeof(RoleName).GetEnumNames();
        static readonly string[] CategoryArray = typeof(Category).GetEnumNames();
        [XmlIgnore] [JsonIgnore] public RoleName HullRole => BaseHull.Role;
        [XmlIgnore] [JsonIgnore] public ShipRole ShipRole => ResourceManager.ShipRoles[Role];

        // BaseHull is the template layout of the ship hull design
        [XmlIgnore] [JsonIgnore] public ShipData BaseHull { get; internal set; }
        [XmlIgnore] [JsonIgnore] public HullBonus Bonuses { get; private set; }

        // Model path of the template hull layout
        [XmlIgnore] [JsonIgnore] public string HullModel => BaseHull.ModelPath;

        [XmlIgnore] [JsonIgnore] public bool IsValidForCurrentMod
            => ModName.IsEmpty() || ModName == GlobalStats.ModName;

        // You should always use this `Icon` property, because of bugs with `IconPath` initialization
        // when a ShipData is copied. @todo Fix ShipData copying
        [XmlIgnore] [JsonIgnore] public SubTexture Icon => ResourceManager.Texture(IconPath);
        [XmlIgnore] [JsonIgnore] public Vector3 Volume { get; private set; }
        [XmlIgnore] [JsonIgnore] public float ModelZ { get; private set; }

        public ShipData()
        {
        }

        // Make a DEEP COPY from a `hull` template
        // This is used in ShipDesignScreen
        // It inserts any missing BaseHull slots to make ShipDesigner code work
        public ShipData(ShipData hull)
        {
            Name = hull.Name;
            CombatState = hull.CombatState;
            MechanicalBoardingDefense = hull.MechanicalBoardingDefense;

            InitCommonState(hull);
            UpdateBaseHull();

            // create a map of unique slots
            var slotsMap = new Map<Point, ModuleSlotData>();

            // first fill in the slots from the design
            // which may potentially have 2x2 or 3x3 modules
            // and might skip over some basehull slots
            for (int i = 0; i < hull.ModuleSlots.Length; ++i)
            {
                ModuleSlotData designSlot = hull.ModuleSlots[i].GetStatelessClone();
                slotsMap[designSlot.PosAsPoint] = designSlot;
            }

            // now go through basehull and see if there's any
            // 1x1 slots that weren't inserted
            for (int i = 0; i < hull.BaseHull.ModuleSlots.Length; ++i)
            {
                ModuleSlotData base1x1slot = hull.BaseHull.ModuleSlots[i];
                Point position = base1x1slot.PosAsPoint;
                if (!slotsMap.ContainsKey(position))
                    slotsMap[position] = base1x1slot.GetStatelessClone();
            }

            // take all unique slots and sort them according to ModuleSlotData.Sorter rules
            ModuleSlots = slotsMap.Values.ToArray();
            Array.Sort(ModuleSlots, ModuleSlotData.Sorter);
            UpdateGridInfo();
        }

        // Make ShipData from an actual ship
        // This is used during Saving for ShipSaveData
        public ShipData(Ship ship)
        {
            Name        = ship.Name;
            CombatState = ship.AI.CombatState;
            MechanicalBoardingDefense = ship.MechanicalBoardingDefense;

            BaseStrength = ship.BaseStrength;
            Level        = (byte)ship.Level;
            experience   = (byte)ship.experience;

            InitCommonState(ship.shipData);
            FixMissingFields();
            ModuleSlots = ship.GetModuleSlotDataArray();
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
            TechScore         = hull.TechScore;
            BaseHull          = hull.BaseHull;

            UnLockable = hull.UnLockable;
            HullUnlockable = hull.HullUnlockable;
            AllModulesUnlockable = hull.AllModulesUnlockable;

            Volume = hull.Volume;
            ModelZ = hull.ModelZ;
        }

        void FixMissingFields()
        {
            // edge case if Hull lookup fails
            if (GridInfo.SurfaceArea == 0 && ModuleSlots != null)
                UpdateGridInfo();

            if (ShipStyle.IsEmpty())
                ShipStyle = BaseHull.ShipStyle;

            if (IconPath.IsEmpty())
                IconPath = BaseHull.IconPath;
        }

        public void UpdateGridInfo()
        {
            GridInfo = new ShipGridInfo(ModuleSlots);
        }

        void FinalizeAfterLoad(FileInfo info, bool isHullDefinition)
        {
            // This is a Hull definition from Content/Hulls/
            if (isHullDefinition)
            {
                // make sure to calculate the surface area correctly
                UpdateGridInfo();
                ShipStyle = info.Directory?.Name ?? "";
                Hull      = ShipStyle + "/" + Hull;

                // Note: carrier role as written in the hull file was changed to battleship, since now carriers are a design role
                // originally, carriers are battleships. The naming was poorly thought on 15b, or not fixed later.
                Role = Role == RoleName.carrier ? RoleName.battleship : Role;

                // Set the BaseHull here to avoid invalid hull lookup
                BaseHull = this; // Hull definition references itself as the base
            }

            UpdateBaseHull();
        }

        public void UpdateBaseHull()
        {
            if (BaseHull == null)
            {
                if (Hull.NotEmpty() && ResourceManager.Hull(Hull, out ShipData hull))
                {
                    BaseHull = hull;
                }
                else
                {
                    Log.Warning(ConsoleColor.Red, $"ShipData {Hull} '{Name}' cannot find hull: {Hull}");
                    BaseHull = this;
                    if (Hull.IsEmpty())
                        Hull = ShipStyle + "/" + Name;
                }
            }

            if (Bonuses == null)
            {
                Bonuses = ResourceManager.HullBonuses.TryGetValue(BaseHull.Hull, out HullBonus bonus) ? bonus : HullBonus.Default;
            }

            FixMissingFields();
        }

        public override string ToString() { return Name; }

        public static ShipData Parse(FileInfo info, bool isHullDefinition)
        {
            try
            {
                if (!UseNewShipDataLoaders)
                    return ParseXML(info, isHullDefinition);


                return null;
            }
            catch (Exception e)
            {
                Log.ErrorDialog(e, $"Failed to parse ShipData '{info.FullName}'", 0);
                throw;
            }
        }

        public static bool IsAllDummySlots(ModuleSlotData[] slots)
        {
            for (int i = 0; i < slots.Length; ++i)
                if (!slots[i].IsDummy)
                    return false;
            return true;
        }

        int GetSurfaceArea()
        {
            if (ModuleSlots.Length == BaseHull.ModuleSlots.Length)
            {
                if (IsAllDummySlots(ModuleSlots))
                    return ModuleSlots.Length;
            }

            // New Designs, calculate SurfaceArea by using module size
            int surface = 0;
            for (int i = 0; i < ModuleSlots.Length; ++i)
            {
                ModuleSlotData slot = ModuleSlots[i];
                ShipModule module = slot.ModuleOrNull;
                if (module != null)
                    surface += module.XSIZE * module.YSIZE;
                else if (!slot.IsDummy)
                    Log.Warning($"GetSurfaceArea({Name}) failed to find module: {slot.ModuleUID}");
            }
            return surface;
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
            lock (this)
            {
                shipSO = StaticMesh.GetSceneMesh(content, HullModel, Animated);

                if (BaseHull.Volume.X.AlmostEqual(0f))
                {
                    BaseHull.Volume = shipSO.GetMeshBoundingBox().Max;
                    BaseHull.ModelZ = BaseHull.Volume.Z;
                }
            }
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
                case RoleName.supply:
                case RoleName.support:
                case RoleName.bomber:    return RoleType.WarSupport;
                case RoleName.troop:
                case RoleName.troopShip: return RoleType.Troop;
                case RoleName.carrier:
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