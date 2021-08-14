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

namespace Ship_Game.Ships.Legacy
{
    // @note
    // ShipData templates from root content and save designs is loaded using SDNative
    // Game Save/Load uses a separate serializer which used to be XML, but now is Json
    // @note Saving ShipData designs is still done in XML -- should change that!
    //       However, we will have to support XML for a long time to have backwards compat.
    public sealed partial class LegacyShipData
    {
        public string Name; // ex: "Dodaving", just an arbitrary name
        public string Hull; // ID of the hull, ex: "Cordrazine/Dodaving"
        public string ModName = ""; // "" if vanilla, else mod name eg "Combined Arms"
        public string ShipStyle; // "Terran"
        public string Description; // "Early Rocket fighter, great against unshielded foes, but die easily"
        public string IconPath; // "ShipIcons/shuttle"
        public string ModelPath; // "Model/Ships/Terran/Shuttle/ship08"
        
        public byte Level;
        public byte experience;

        public string EventOnDeath;
        public string SelectionGraphic = "";

        public float MechanicalBoardingDefense;
        public float FixedUpkeep;
        public int FixedCost;
        public bool Animated;
        public bool IsShipyard;
        public bool IsOrbitalDefense;
        // The Doctor: intending to use this as a user-toggled
        // flag which tells the AI not to build a design as a stand-alone vessel
        // from a planet; only for use in a hangar
        public bool CarrierShip;

        public CombatState CombatState;
        public RoleName Role = RoleName.fighter;
        public Category ShipCategory = Category.Unclassified;
        public HangarOptions HangarDesignation = HangarOptions.General;
        public AIState DefaultAIState;

        public ThrusterZone[] ThrusterList;

        [XmlIgnore] [JsonIgnore] public LegacyShipGridInfo GridInfo;

        [XmlIgnore] [JsonIgnore] public float BaseStrength;
        [XmlArray(ElementName = "ModuleSlotList")] public LegacyModuleSlotData[] ModuleSlots;
        [XmlIgnore] [JsonIgnore] public bool UnLockable;
        [XmlIgnore] [JsonIgnore] public bool HullUnlockable;
        [XmlIgnore] [JsonIgnore] public bool AllModulesUnlockable = true;
        [XmlArray(ElementName = "techsNeeded")] public HashSet<string> TechsNeeded = new HashSet<string>();
        [XmlIgnore] [JsonIgnore] public int TechScore;

        static readonly string[] RoleArray     = typeof(RoleName).GetEnumNames();
        static readonly string[] CategoryArray = typeof(Category).GetEnumNames();
        [XmlIgnore] [JsonIgnore] public RoleName HullRole => BaseHull.Role;

        // BaseHull is the template layout of the ship hull design
        [XmlIgnore] [JsonIgnore] public LegacyShipData BaseHull { get; internal set; }

        // Model path of the template hull layout
        [XmlIgnore] [JsonIgnore] public string HullModel => BaseHull.ModelPath;

        [XmlIgnore] [JsonIgnore] public bool IsHull { get; private set; }

        public FileInfo Source;

        public LegacyShipData()
        {
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
            GridInfo = new LegacyShipGridInfo(Hull, ModuleSlots, IsHull, BaseHull);
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

        static Map<string, LegacyShipData> HullsDict;
        static object HullsLock = new object();

        public void UpdateBaseHull()
        {
            if (BaseHull == null)
            {
                lock (HullsLock)
                {
                    if (HullsDict == null)
                        HullsDict = ResourceManager.LoadLegacyShipHulls();
                }

                if (Hull.NotEmpty())
                {
                    if (!HullsDict.TryGetValue(Hull, out LegacyShipData hull))
                    {
                        string maybeHull = ShipStyle + "/" + Hull;
                        if (HullsDict.TryGetValue(maybeHull, out hull))
                        {
                            Hull = maybeHull;
                        }
                    }
                    BaseHull = hull;
                }

                if (BaseHull == null)
                {
                    Log.Warning(ConsoleColor.Red, $"ShipData {Hull} '{Name}' cannot find hull: {Hull}");
                    BaseHull = this;
                    if (Hull.IsEmpty())
                        Hull = ShipStyle + "/" + Name;
                }
            }

            FixMissingFields();
        }

        public override string ToString() { return Name; }

        public static LegacyShipData Parse(string filePath, bool isHullDefinition)
        {
            var file = new FileInfo(filePath);
            return Parse(file, isHullDefinition);
        }

        public static LegacyShipData Parse(FileInfo info, bool isHullDefinition)
        {
            try
            {
                return ParseXML(info, isHullDefinition);
            }
            catch (Exception e)
            {
                Log.Error(e, $"Failed to parse LegacyShipData '{info.FullName}'", 0);
            }
            return null;
        }

        public static bool IsAllDummySlots(LegacyModuleSlotData[] slots)
        {
            for (int i = 0; i < slots.Length; ++i)
                if (!slots[i].IsDummy)
                    return false;
            return true;
        }

        public LegacyShipData GetClone()
        {
            return (LegacyShipData)MemberwiseClone();
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
    }
}