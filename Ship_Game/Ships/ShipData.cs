using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using SgMotion;
using SgMotion.Controllers;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
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
    public sealed class ShipData
    {
        public string Name; // ex: "Dodaving", just an arbitrary name
        public string ModName; // null if vanilla, else mod name eg "Combined Arms"
        public string ShipStyle; // "Terran"
        public string Hull; // ID of the hull, ex: "Cordrazine/Dodaving"
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

        // Constant: total number of slots in this ShipData Template
        [XmlIgnore] [JsonIgnore] public int SurfaceArea;
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
        [XmlIgnore] [JsonIgnore] public float Radius { get; private set; }
        [XmlIgnore] [JsonIgnore] public float ModelZ { get; private set; }

        public ShipData()
        {
        }

        // Make a COPY from a `hull` template
        // This is used in ShipDesignScreen and ShipToolScreen
        public ShipData(ShipData hull)
        {
            Name = hull.Name;
            CombatState = hull.CombatState;
            MechanicalBoardingDefense = hull.MechanicalBoardingDefense;

            InitCommonState(hull);

            ModuleSlots = new ModuleSlotData[hull.ModuleSlots.Length];
            for (int i = 0; i < hull.ModuleSlots.Length; ++i)
            {
                ModuleSlotData slot = hull.ModuleSlots[i];
                ModuleSlots[i] = new ModuleSlotData
                {
                    Position     = slot.Position,
                    Restrictions = slot.Restrictions,
                    Facing       = slot.Facing,
                    ModuleUID    = slot.ModuleUID,
                    Orientation  = slot.Orientation,
                    SlotOptions  = slot.SlotOptions
                };
            }

            UpdateBaseHull();
        }

        // Make ShipData from an actual ship
        // This is used during Serialization (Save)
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
            Radius = hull.Radius;
            ModelZ = hull.ModelZ;
        }

        void FixMissingFields()
        {
            if (ShipStyle.IsEmpty())
                ShipStyle = BaseHull.ShipStyle;

            if (IconPath.IsEmpty())
                IconPath = BaseHull.IconPath;
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

            SurfaceArea = BaseHull.SurfaceArea;

            // edge case if Hull lookup fails
            if (SurfaceArea == 0)
            {
                SurfaceArea = GetSurfaceArea();
            }
            #if DEBUG
            else if (SurfaceArea != GetSurfaceArea())
            {
                Log.Warning(ConsoleColor.Red, $"ShipData {Hull} '{Name}' SurfaceArea mismatch: hull {SurfaceArea} != calculated {GetSurfaceArea()}");
            }
            #endif

            FixMissingFields();
        }

        public override string ToString() { return Name; }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct CThrusterZone
        {
            public readonly float X, Y, Z, Scale;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct CModuleSlot
        {
            public readonly float PosX, PosY, Health, ShieldPower, Facing;
            public readonly CStrView InstalledModuleUID;
            public readonly CStrView HangarshipGuid;
            public readonly CStrView State;
            public readonly CStrView Restrictions;
            public readonly CStrView SlotOptions;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        unsafe struct CShipDataParser
        {
            public readonly CStrView Name;
            public readonly CStrView Hull;
            public readonly CStrView ShipStyle;
            public readonly CStrView EventOnDeath;
            public readonly CStrView SelectionGraphic;
            public readonly CStrView IconPath;
            public readonly CStrView ModelPath;
            public readonly CStrView DefaultAIState;
            public readonly CStrView Role;
            public readonly CStrView CombatState;
            public readonly CStrView ShipCategory;
            public readonly CStrView HangarDesignation;
            public readonly CStrView ModName;

            public readonly int TechScore;
            public readonly float BaseStrength;
            public readonly float FixedUpkeep;
            public readonly float MechanicalBoardingDefense;
            public readonly byte Experience;
            public readonly byte Level;
            public readonly short FixedCost;
            public readonly byte Animated;
            public readonly byte HasFixedCost;
            public readonly byte HasFixedUpkeep;
            public readonly byte IsShipyard;
            public readonly byte CarrierShip;
            public readonly byte BaseCanWarp;
            public readonly byte IsOrbitalDefense;
            public readonly byte HullUnlockable;
            public readonly byte UnLockable;
            public readonly byte AllModulesUnlockable;

            public readonly CThrusterZone* Thrusters;
            public readonly int ThrustersLen;
            public readonly CModuleSlot* ModuleSlots;
            public readonly int ModuleSlotsLen;
            public readonly CStrView* Techs;
            public readonly int TechsLen;

            public readonly CStrView ErrorMessage;
        }

        [DllImport("SDNative.dll")]
        static extern unsafe CShipDataParser* CreateShipDataParser(
            [MarshalAs(UnmanagedType.LPWStr)] string filename);

        [DllImport("SDNative.dll")]
        static extern unsafe void DisposeShipDataParser(CShipDataParser* parser);

        // Added by RedFox - manual parsing of ShipData, because this is the slowest part
        // in loading, the brunt work is offloaded to C++ and then copied back into C#
        public static unsafe ShipData Parse(FileInfo info, bool isEmptyHull)
        {
            CShipDataParser* s = null;
            try
            {
                s = CreateShipDataParser(info.FullName); // @note This will never throw
                if (!s->ErrorMessage.Empty)
                {
                    Log.Error($"Ship Load error in {info.FullName} : {s->ErrorMessage.AsString}");
                    throw new InvalidDataException(s->ErrorMessage.AsString);
                }

                // if this design belongs to a specific Mod, then make sure current ModName matches
                string modName = s->ModName.AsString;
                if (modName.NotEmpty() && modName != GlobalStats.ModName)
                    return null; // ignore this design

                var ship = new ShipData
                {
                    Animated       = s->Animated != 0,
                    ShipStyle      = s->ShipStyle.AsInternedOrNull,
                    EventOnDeath   = s->EventOnDeath.AsInternedOrNull,
                    experience     = s->Experience,
                    Level          = s->Level,
                    Name           = s->Name.AsString,
                    ModName        = modName,
                    HasFixedCost   = s->HasFixedCost != 0,
                    FixedCost      = s->FixedCost,
                    HasFixedUpkeep = s->HasFixedUpkeep != 0,
                    FixedUpkeep    = s->FixedUpkeep,
                    IsShipyard     = s->IsShipyard != 0,
                    IconPath       = s->IconPath.AsString,
                    Hull           = s->Hull.AsString,
                    ModelPath      = s->ModelPath.AsString,
                    CarrierShip    = s->CarrierShip != 0,
                    BaseStrength   = s->BaseStrength,
                    HullUnlockable = s->HullUnlockable != 0,
                    UnLockable     = s->UnLockable != 0,
                    TechScore      = s->TechScore,
                    IsOrbitalDefense          = s->IsOrbitalDefense != 0,
                    SelectionGraphic          = s->SelectionGraphic.AsString,
                    AllModulesUnlockable     = s->AllModulesUnlockable != 0,
                    MechanicalBoardingDefense = s->MechanicalBoardingDefense
                };
                Enum.TryParse(s->Role.AsString,              out ship.Role);
                Enum.TryParse(s->CombatState.AsString,       out ship.CombatState);
                Enum.TryParse(s->ShipCategory.AsString,      out ship.ShipCategory);
                Enum.TryParse(s->HangarDesignation.AsString, out ship.HangarDesignation);
                Enum.TryParse(s->DefaultAIState.AsString,    out ship.DefaultAIState);

                // @todo Remove SDNative.ModuleSlot conversion
                // @todo Optimize CModuleSlot -- we don't need string data for everything
                //       GUID should be byte[16]
                //       Orientation should be int
                //       
                ship.ModuleSlots = new ModuleSlotData[s->ModuleSlotsLen];
                for (int i = 0; i < s->ModuleSlotsLen; ++i)
                {
                    CModuleSlot* msd = &s->ModuleSlots[i];
                    var slot = new ModuleSlotData();
                    slot.Position = new Vector2(msd->PosX, msd->PosY);
                    // @note Interning the strings saves us roughly 70MB of RAM across all UID-s
                    slot.ModuleUID = msd->InstalledModuleUID.AsInternedOrNull; // must be interned
                    slot.Health      = msd->Health;
                    slot.ShieldPower = msd->ShieldPower;
                    slot.Facing      = msd->Facing;
                    Enum.TryParse(msd->Restrictions.AsString, out slot.Restrictions);
                    slot.Orientation = msd->State.AsInterned;
                    // slot options can be:
                    // "NotApplicable", "Ftr-Plasma Tentacle", "Vulcan Scout", ... etc.
                    // It's a general purpose "whatever" sink, however it's used very frequently
                    slot.SlotOptions = msd->SlotOptions.AsInterned;
                    ship.ModuleSlots[i] = slot;
                }

                ship.ThrusterList = new ThrusterZone[s->ThrustersLen];
                for (int i = 0; i < s->ThrustersLen; ++i)
                {
                    CThrusterZone* zone = &s->Thrusters[i];
                    ship.ThrusterList[i] = new ThrusterZone
                    {
                        Position = new Vector3(zone->X, zone->Y, zone->Z),
                        Scale = zone->Scale
                    };
                }

                // @todo Remove conversion to HashSet
                ship.TechsNeeded = new HashSet<string>();
                for (int i = 0; i < s->TechsLen; ++i)
                    ship.TechsNeeded.Add(s->Techs[i].AsInterned);

                // This is a Hull definition from Content/Hulls/
                if (isEmptyHull)
                {
                    // make sure to calculate the surface area correctly
                    ship.SurfaceArea = ship.GetSurfaceArea();

                    ship.ShipStyle = info.Directory?.Name ?? "";
                    ship.Hull      = ship.ShipStyle + "/" + ship.Hull;

                    // Note: carrier role as written in the hull file was changed to battleship, since now carriers are a design role
                    // originally, carriers are battleships. The naming was poorly thought on 15b, or not fixed later.
                    ship.Role = ship.Role == RoleName.carrier ? RoleName.battleship : ship.Role;

                    // Set the BaseHull here to avoid invalid hull lookup
                    ship.BaseHull = ship; // Hull definition references itself as the base
                }

                ship.UpdateBaseHull();
                return ship;
            }
            catch (Exception e)
            {
                Log.ErrorDialog(e, $"Failed to parse ShipData '{info.FullName}'", 0);
                throw;
            }
            finally
            {
                DisposeShipDataParser(s);
            }
        }

        static bool HasLegacyDummySlots(ModuleSlotData[] slots)
        {
            for (int i = 0; i < slots.Length; ++i)
                if (slots[i].IsDummy)
                    return true;
            return false;
        }

        static bool IsAllDummySlots(ModuleSlotData[] slots)
        {
            for (int i = 0; i < slots.Length; ++i)
                if (!slots[i].IsDummy)
                    return false;
            return true;
        }

        bool DetectOverlappingModules()
        {
            for (int i = 0; i < ModuleSlots.Length; ++i)
            {
                ModuleSlotData a = ModuleSlots[i];
                ShipModule ma = a.ModuleOrNull;
                if (ma == null)
                    continue;
                var ra = new Rectangle((int)a.Position.X, (int)a.Position.Y, ma.XSIZE * 16, ma.YSIZE * 16);
                for (int j = i + 1; j < ModuleSlots.Length; ++j)
                {
                    ModuleSlotData b = ModuleSlots[j];
                    ShipModule mb = b.ModuleOrNull;
                    if (mb == null)
                        continue;
                    var rb = new Rectangle((int)b.Position.X, (int)b.Position.Y, mb.XSIZE * 16, mb.YSIZE * 16);
                    if (ra.GetIntersectingRect(rb, out Rectangle intersection))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        int GetSurfaceArea()
        {
            // Legacy Hulls and Templates can have "Dummy" modules, in which case SurfaceArea == slots.Length
            bool hasLegacyDummySlots = HasLegacyDummySlots(ModuleSlots);
            if (hasLegacyDummySlots)
            {
                if (IsAllDummySlots(ModuleSlots))
                    return ModuleSlots.Length;
            }

            #if DEBUG
            if (DetectOverlappingModules())
                Log.Warning($"ShipData '{Name}' overlapping modules!");
            #endif

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
                    BaseHull.Radius = shipSO.WorldBoundingSphere.Radius;
                    BaseHull.ModelZ = BaseHull.Volume.Z;
                }
            }
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

        public struct ThrusterZone
        {
            public Vector3 Position;
            [XmlElement(ElementName = "scale")]
            public float Scale;
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