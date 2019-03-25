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
        public string ShipStyle;
        public string EventOnDeath;
        public byte experience;
        public byte Level;
        public string SelectionGraphic = "";
        public string Name; // ex: "Dodaving", just an arbitrary name
        public string ModName;
        public bool HasFixedCost;
        public short FixedCost;
        public float FixedUpkeep;
        public bool HasFixedUpkeep;
        public bool Animated;
        public bool IsShipyard;
        public bool IsOrbitalDefense;
        public string IconPath;
        public CombatState CombatState = CombatState.AttackRuns;
        public float MechanicalBoardingDefense;

        public string Hull; // ID of the hull, ex: "Cordrazine/Dodaving"
        public RoleName Role = RoleName.fighter;
        public Array<ShipToolScreen.ThrusterZone> ThrusterList;
        public string ModelPath;
        public AIState DefaultAIState;
        // The Doctor: intending to use this for 'Civilian', 'Recon', 'Fighter', 'Bomber' etc.
        public Category ShipCategory = Category.Unclassified;

        public HangarOptions HangarDesignation = HangarOptions.General;
        public ShieldsWarpBehavior ShieldsBehavior = ShieldsWarpBehavior.FullPower;

        // The Doctor: intending to use this as a user-toggled flag which tells the AI not to build a design as a stand-alone vessel from a planet; only for use in a hangar
        public bool CarrierShip;
        [XmlIgnore] [JsonIgnore] public float BaseStrength;
        [XmlIgnore] [JsonIgnore] public bool BaseCanWarp;
        [XmlArray(ElementName = "ModuleSlotList")] public ModuleSlotData[] ModuleSlots;
        [XmlIgnore] [JsonIgnore] public bool HullUnlockable;
        [XmlIgnore] [JsonIgnore] public bool AllModulesUnlocakable = true;
        [XmlIgnore] [JsonIgnore] public bool UnLockable;
        [XmlArray(ElementName = "techsNeeded")] public HashSet<string> TechsNeeded = new HashSet<string>();
        [XmlIgnore] [JsonIgnore] public int TechScore;

        static readonly string[] RoleArray     = typeof(RoleName).GetEnumNames();
        static readonly string[] CategoryArray = typeof(Category).GetEnumNames();
        [XmlIgnore] [JsonIgnore] public RoleName HullRole => BaseHull.Role;

        [XmlIgnore] [JsonIgnore] public ShipRole ShipRole => ResourceManager.ShipRoles[Role];

        // BaseHull is the template layout of the ship hull design
        [XmlIgnore] [JsonIgnore] public ShipData BaseHull { get; internal set; }

        // Model path of the template hull layout
        [XmlIgnore] [JsonIgnore] public string HullModel => BaseHull.ModelPath;

        [XmlIgnore] [JsonIgnore] public bool IsValidForCurrentMod
            => ModName.IsEmpty() || ModName == GlobalStats.ModName;

        // You should always use this `Icon` property, because of bugs with `IconPath` initialization
        // when a ShipData is copied. @todo Fix ShipData copying
        [XmlIgnore] [JsonIgnore] public SubTexture Icon => ResourceManager.Texture(ActualIconPath);
        [XmlIgnore] [JsonIgnore] public string ActualIconPath => IconPath.NotEmpty() ? IconPath : BaseHull.IconPath;
        [XmlIgnore] [JsonIgnore] public float ModelZ { get; private set; }
        [XmlIgnore] [JsonIgnore] public Vector3 Volume { get; private set; }
        [XmlIgnore] [JsonIgnore] public HullBonus Bonuses { get; private set; }

        public void UpdateBaseHull()
        {
            if (BaseHull == null)
                BaseHull = ResourceManager.Hull(Hull, out ShipData hull) ? hull : this;

            if (Bonuses == null)
                Bonuses  = ResourceManager.HullBonuses.TryGetValue(Hull, out HullBonus bonus) ? bonus : HullBonus.Default;
        }

        public override string ToString() { return Name; }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct CThrusterZone
        {
            public readonly float X, Y, Scale;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct CModuleSlot
        {
            public readonly float PosX, PosY, Health, ShieldPower, ShieldUpChance, ShieldPowerBeforeWarp, Facing;
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
            public readonly CStrView ShieldsBehavior;
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
        public static unsafe ShipData Parse(FileInfo info)
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

                var ship = new ShipData
                {
                    Animated       = s->Animated != 0,
                    ShipStyle      = s->ShipStyle.AsInternedOrNull,
                    EventOnDeath   = s->EventOnDeath.AsInternedOrNull,
                    experience     = s->Experience,
                    Level          = s->Level,
                    Name           = s->Name.AsString,
                    ModName        = s->ModName.AsString,
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
                    BaseCanWarp    = s->BaseCanWarp != 0,
                    HullUnlockable = s->HullUnlockable != 0,
                    UnLockable     = s->UnLockable != 0,
                    TechScore      = s->TechScore,
                    IsOrbitalDefense          = s->IsOrbitalDefense != 0,
                    SelectionGraphic          = s->SelectionGraphic.AsString,
                    AllModulesUnlocakable     = s->AllModulesUnlockable != 0,
                    MechanicalBoardingDefense = s->MechanicalBoardingDefense
                };
                Enum.TryParse(s->Role.AsString,              out ship.Role);
                Enum.TryParse(s->CombatState.AsString,       out ship.CombatState);
                Enum.TryParse(s->ShipCategory.AsString,      out ship.ShipCategory);
                Enum.TryParse(s->HangarDesignation.AsString, out ship.HangarDesignation);
                Enum.TryParse(s->ShieldsBehavior.AsString,   out ship.ShieldsBehavior);
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
                    slot.Position              = new Vector2(msd->PosX, msd->PosY);
                    // @note Interning the strings saves us roughly 70MB of RAM across all UID-s
                    slot.InstalledModuleUID    = msd->InstalledModuleUID.AsInternedOrNull; // must be interned
                    slot.HangarshipGuid        = msd->HangarshipGuid.Empty ? Guid.Empty : new Guid(msd->HangarshipGuid.AsString);
                    slot.Health                = msd->Health;
                    slot.ShieldPower           = msd->ShieldPower;
                    slot.ShieldUpChance        = msd->ShieldUpChance;
                    slot.ShieldPowerBeforeWarp = msd->ShieldPowerBeforeWarp;
                    slot.Facing                = msd->Facing;
                    Enum.TryParse(msd->Restrictions.AsString, out slot.Restrictions);
                    slot.Orientation           = msd->State.AsInterned;
                    // slot options can be:
                    // "NotApplicable", "Ftr-Plasma Tentacle", "Vulcan Scout", ... etc.
                    // It's a general purpose "whatever" sink, however it's used very frequently
                    slot.SlotOptions = msd->SlotOptions.AsInterned;
                    ship.ModuleSlots[i] = slot;
                }

                int slotCount = ship.ModuleSlots.Length;

                if (ship.ModuleSlots.Length != slotCount)
                    Log.Warning($"Ship {ship.Name} loaded with errors ");
                // @todo Remove conversion to List
                ship.ThrusterList = new Array<ShipToolScreen.ThrusterZone>(s->ThrustersLen);
                for (int i = 0; i < s->ThrustersLen; ++i)
                {
                    CThrusterZone* zone = &s->Thrusters[i];
                    ship.ThrusterList.Add(new ShipToolScreen.ThrusterZone
                    {
                        Position = new Vector2(zone->X, zone->Y),
                        Scale = zone->Scale
                    });
                }

                // @todo Remove conversion to HashSet
                ship.TechsNeeded = new HashSet<string>();
                for (int i = 0; i < s->TechsLen; ++i)
                    ship.TechsNeeded.Add(s->Techs[i].AsInterned);

                ship.UpdateBaseHull();
                return ship;
            }
            catch (Exception e)
            {
                Log.ErrorDialog(e, $"Failed to parse ShipData '{info.FullName}'");
                throw;
            }
            finally
            {
                DisposeShipDataParser(s);
            }
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

        public void PreLoadModel()
        {
            StaticMesh.PreLoadModel(Empire.Universe?.TransientContent, HullModel, Animated);
        }

        public void LoadModel(out SceneObject shipSO, GameScreen screen)
        {
            shipSO = StaticMesh.GetSceneMesh(screen?.TransientContent, HullModel, Animated);

            if (BaseHull.Volume.X.AlmostEqual(0f))
            {
                BaseHull.Volume = shipSO.GetMeshBoundingBox().Max;
                BaseHull.ModelZ = BaseHull.Volume.Z;
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
            platform,
            station,
            construction,
            colony,
            supply,
            freighter,
            troop,
            troopShip,
            support,
            bomber,
            carrier,
            fighter,
            scout,
            gunboat,
            drone,
            corvette,
            frigate,
            destroyer,
            cruiser,
            capital,
            prototype
        }
    }
}