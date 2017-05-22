using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SgMotion;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game
{
    public sealed partial class ShipDesignScreen : GameScreen
    {
        private Matrix WorldMatrix = Matrix.Identity;
        private Matrix View;
        private Matrix Projection;
        public Camera2d Camera;
        public Array<ToggleButton> CombatStatusButtons = new Array<ToggleButton>();
        public bool Debug;
        public ShipData ActiveHull;
        public EmpireUIOverlay EmpireUI;
        private Menu1 ModuleSelectionMenu;
        private Model ActiveModel;
        private SceneObject shipSO;
        private Vector3 cameraPosition = new Vector3(0f, 0f, 1300f);
        public Array<SlotStruct> Slots = new Array<SlotStruct>();
        private Vector2 offset;
        private CombatState CombatState = CombatState.AttackRuns;
        private bool ShipSaved = true;
        private Array<ShipData> AvailableHulls = new Array<ShipData>();
        private UIButton ToggleOverlayButton;
        private UIButton SaveButton;
        private UIButton LoadButton;
        private Submenu ModSel;
        private Submenu StatsSub;
        private Menu1 ShipStats;
        private Menu1 activeModWindow;
        private Submenu ActiveModSubMenu;
        private WeaponScrollList WeaponSl;
        private bool Reset = true;
        private Submenu ChooseFighterSub;
        private ScrollList ChooseFighterSL;
        private bool LowRes;
        private float LowestX;
        private float HighestX;
        private GenericButton ArcsButton;
        private CloseButton Close;
        private float OriginalZ;
        private Rectangle Choosefighterrect;
        private Rectangle SearchBar;
        private Rectangle BottomSep;
        private ScrollList HullSL;
        private Rectangle HullSelectionRect;
        private Submenu HullSelectionSub;
        private Rectangle BlackBar;
        private Rectangle SideBar;
        private Vector2 SelectedCatTextPos;
        private SkinnableButton wpn;
        private SkinnableButton pwr;
        private SkinnableButton def;
        private SkinnableButton spc;
        private Rectangle ModuleSelectionArea = new Rectangle();
        private readonly Array<ModuleCatButton> ModuleCatButtons = new Array<ModuleCatButton>();
        private readonly Array<ModuleButton> ModuleButtons = new Array<ModuleButton>();
        private Rectangle UpArrow;
        private Rectangle DownArrow;
        private MouseState MouseStateCurrent;
        private MouseState MouseStatePrevious;
        private ShipModule HighlightedModule;
        private Vector2 CameraVelocity = Vector2.Zero;
        private Vector2 StartDragPos = new Vector2();
        private ShipData Changeto;
        private string ScreenToLaunch;
        private bool ShowAllArcs;
        private ShipModule HoveredModule;
        private float TransitionZoom = 1f;
        private SlotModOperation Operation;
        private int HullIndex;
        private ShipModule ActiveModule;
        private ShipModule ActiveHangarModule;
        private ActiveModuleState ActiveModState;
        private Selector selector;
        public bool ToggleOverlay = true;
        private Vector2 starfieldPos = Vector2.Zero;
        private int ScrollPosition;
        private CategoryDropDown CategoryList;
        private Rectangle DropdownRect;
        private Vector2 ClassifCursor;
        public Stack<DesignAction> DesignStack = new Stack<DesignAction>();
        private string LastActiveUID = "";                                      //Gretman - To Make the Ctrl-Z much more responsive
        private Vector2 LastDesignActionPos = Vector2.Zero;
        private Vector2 CoBoxCursor;
        private Checkbox CarrierOnlyBox;
        private bool Fml = false;
        private bool Fmlevenmore = false;
        public bool CarrierOnly;
        private ShipData.Category LoadCategory;
        public string HangarShipUIDLast = "Undefined";
        private float HoldTimer = .50f;
        private HashSet<string> Techs = new HashSet<string>();
        private readonly Texture2D TopBar132 = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px");
        private readonly Texture2D TopBar132Hover = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px_hover");
        private readonly Texture2D TopBar132Pressed = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px_pressed");
        private readonly Texture2D TopBar68 = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_68px");
        private readonly Texture2D TopBar68Hover = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_68px_hover");
        private readonly Texture2D TopBar68Pressed = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_pressed"];        


#if SHIPYARD
        short TotalI, TotalO, TotalE, TotalIO, TotalIE, TotalOE, TotalIOE = 0;        //For Gretman's debug shipyard
#endif


        public ShipDesignScreen(GameScreen parent, EmpireUIOverlay EmpireUI) : base(parent)
        {
            this.EmpireUI         = EmpireUI;
            base.TransitionOnTime = TimeSpan.FromSeconds(2);
#if SHIPYARD
            Debug = true;
#endif
        }

        private void AddToTechList(HashSet<string> techlist)
        {
            foreach (string tech in techlist)
                this.Techs.Add(tech);
        }


        private void ChangeModuleState(ActiveModuleState state)
        {
            if (ActiveModule == null)
                return;
            ShipModule moduleTemplate = ResourceManager.GetModuleTemplate(ActiveModule.UID);
            int x = moduleTemplate.XSIZE;
            int y = moduleTemplate.YSIZE;
            switch (state)
            {
                case ActiveModuleState.Normal:
                {
                    this.ActiveModule.XSIZE = moduleTemplate.XSIZE;
                    this.ActiveModule.YSIZE = moduleTemplate.YSIZE;
                    this.ActiveModState = ActiveModuleState.Normal;
                    return;
                }
                case ActiveModuleState.Left:
                {
                    this.ActiveModule.XSIZE = y; // @todo Why are these swapped? Please comment.
                    this.ActiveModule.YSIZE = x; // These are swapped because if the module is facing left or right, then the length is now the height, and vice versa
                    this.ActiveModState = ActiveModuleState.Left;
                    this.ActiveModule.Facing = 270f;
                    return;
                }
                case ActiveModuleState.Right:
                {
                    this.ActiveModule.XSIZE = y; // @todo Why are these swapped? Please comment.
                    this.ActiveModule.YSIZE = x; // These are swapped because if the module is facing left or right, then the length is now the height, and vice versa
                    this.ActiveModState = ActiveModuleState.Right;
                    this.ActiveModule.Facing = 90f;
                    return;
                }
                case ActiveModuleState.Rear:
                {
                    this.ActiveModule.XSIZE = moduleTemplate.XSIZE;
                    this.ActiveModule.YSIZE = moduleTemplate.YSIZE;
                    this.ActiveModState = ActiveModuleState.Rear;
                    this.ActiveModule.Facing = 180f;
                    return;
                }
                default:
                {
                    return;
                }
            }
        }

        private void CheckAndPowerConduit(SlotStruct slot)
        {
            slot.Module.Powered = true;
            slot.CheckedConduits = true;
            foreach (SlotStruct ss in Slots)
            {
                if (ss == slot || Math.Abs(slot.PQ.X - ss.PQ.X) / 16 + Math.Abs(slot.PQ.Y - ss.PQ.Y) / 16 != 1 || ss.Module == null || ss.Module.ModuleType != ShipModuleType.PowerConduit || ss.CheckedConduits)
                    continue;
                CheckAndPowerConduit(ss);
            }
        }

        private bool FindStructFromOffset(SlotStruct offsetBase, int x, int y, out SlotStruct found)
        {
            found = null;
            if (x == 0 && y == 0)
                return false; // ignore self, {0,0} is offsetBase

            int sx = offsetBase.PQ.X + 16 * x;
            int sy = offsetBase.PQ.Y + 16 * y;
            for (int i = 0; i < Slots.Count; ++i)
            {
                SlotStruct s = Slots[i];
                if (s.PQ.X == sx && s.PQ.Y == sy)
                {
                    found = s;
                    return true;
                }
            }
            return false;
        }

        // @todo This is all broken. Redo everything.
        private void ClearDestinationSlots(SlotStruct slot, bool addToAlteredSlots = true)
        {
            for (int y = 0; y < ActiveModule.YSIZE; y++)
            {
                for (int x = 0; x < ActiveModule.XSIZE; x++)
                {
                    if (!FindStructFromOffset(slot, x, y, out SlotStruct slot2))
                        continue;

                    if (slot2.Module != null)
                        ClearParentSlot(slot2, addToAlteredSlots);

                    slot2.ModuleUID = null;
                    slot2.Tex       = null;
                    slot2.Module    = null;
                    slot2.Parent    = slot;
                    slot2.State     = ActiveModuleState.Normal;
                }
            }
        }

        private void ClearDestinationSlotsNoStack(SlotStruct slot) => ClearDestinationSlots(slot, false);

        // @todo This is all broken. Redo everything.
        private void ClearParentSlot(SlotStruct parent, bool addToAlteredSlots = true)
        {
            //actually supposed to clear ALL slots of a module, not just the parent
            if (addToAlteredSlots && DesignStack.Count > 0)
            {
                SlotStruct slot1 = new SlotStruct()
                {
                    PQ            = parent.PQ,
                    Restrictions  = parent.Restrictions,
                    Facing        = parent.Facing,
                    ModuleUID     = parent.ModuleUID,
                    Module        = parent.Module,
                    State         = parent.State,
                    SlotReference = parent.SlotReference
                };
                DesignStack.Peek().AlteredSlots.Add(slot1);
            }

            for (int y = 0; y < parent.Module.YSIZE; ++y)
            {
                for (int x = 0; x < parent.Module.XSIZE; ++x)
                {
                    if (!FindStructFromOffset(parent, x, y, out SlotStruct slot2))
                        continue;
                    slot2.ModuleUID = null;
                    slot2.Tex       = null;
                    slot2.Module    = null;
                    slot2.Parent    = null;
                    slot2.State     = ActiveModuleState.Normal;
                }
            }
            //clear parent slot
            parent.ModuleUID = null;
            parent.Tex       = null;
            parent.Module    = null;
            parent.Parent    = null;
            parent.State     = ActiveModuleState.Normal;
        }
        private void ClearParentSlotNoStack(SlotStruct parent) => ClearParentSlot(parent, false);   //Unused

        private void ClearSlot(SlotStruct slot, bool addToAlteredSlots = true)
        {   //this is the clearslot function actually used atm
            //only called from installmodule atm, not from manual module removal
            if (slot.Module != null)
            {
                ClearParentSlot(slot, addToAlteredSlots);
            }
            else
            {   //this requires not being a child slot and not containing a module
                //only empty parent slots can trigger this
                //why would we want to clear an empty slot?
                //might be used on initial load instead of a proper slot constructor
                slot.ModuleUID = null;
                slot.Tex       = null;
                slot.Parent    = null;
                slot.Module    = null;
                slot.State     = ActiveModuleState.Normal;
            }
        }
        private void ClearSlotNoStack(SlotStruct slot) => ClearSlot(slot, false);

        private ModuleSlotData FindModuleSlotAtPos(Vector2 slotPos)
        {
            ModuleSlotData[] slots = ActiveHull.ModuleSlots;
            for (int i = 0; i < slots.Length; ++i)
                if (slots[i].Position == slotPos)
                    return slots[i];
            return null;
        }

        private void DebugAlterSlot(Vector2 SlotPos, SlotModOperation op)
        {
            ModuleSlotData toRemove = FindModuleSlotAtPos(SlotPos);
            if (toRemove == null)
                return;

            switch (op)
            {
                default:
                case SlotModOperation.Normal: return;
                case SlotModOperation.Delete: ActiveHull.ModuleSlots.Remove(toRemove, out ActiveHull.ModuleSlots); break;
                case SlotModOperation.I:      toRemove.Restrictions = Restrictions.I;  break;
                case SlotModOperation.O:      toRemove.Restrictions = Restrictions.O;  break;
                case SlotModOperation.E:      toRemove.Restrictions = Restrictions.E;  break;
                case SlotModOperation.IO:     toRemove.Restrictions = Restrictions.IO; break;
                case SlotModOperation.IE:     toRemove.Restrictions = Restrictions.IE; break;
                case SlotModOperation.OE:     toRemove.Restrictions = Restrictions.OE; break;
                case SlotModOperation.IOE:    toRemove.Restrictions = Restrictions.IOE; break;
            }
            ChangeHull(ActiveHull);
        }

        protected override void Dispose(bool disposing)
        {
            HullSL?.Dispose(ref HullSL);
            WeaponSl?.Dispose(ref WeaponSl);
            ChooseFighterSL?.Dispose(ref ChooseFighterSL);
            base.Dispose(disposing);
        }

        private float GetMaintCostShipyard(ShipData ship, float Size, Empire empire)
        {
            float maint = 0f;
            float maintModReduction = 1;

            //Get Maintenance of ship role
            bool foundMaint = false;
            if (ResourceManager.ShipRoles.ContainsKey(ship.Role))
            {
                for (int i = 0; i < ResourceManager.ShipRoles[ship.Role].RaceList.Count; i++)
                {
                    if (ResourceManager.ShipRoles[ship.Role].RaceList[i].ShipType == empire.data.Traits.ShipType)
                    {
                        maint = ResourceManager.ShipRoles[ship.Role].RaceList[i].Upkeep;
                        foundMaint = true;
                        break;
                    }
                }
                if (!foundMaint)
                    maint = ResourceManager.ShipRoles[ship.Role].Upkeep;
            }
            else
                return 0f;

            //Modify Maintenance by freighter size
            if (ship.Role == ShipData.RoleName.freighter)
            {
                switch ((int)Size / 50)
                {
                    case 0:
                        {
                            break;
                        }

                    case 1:
                        {
                            maint *= 1.5f;
                            break;
                        }

                    case 2:
                    case 3:
                    case 4:
                        {
                            maint *= 2f;
                            break;
                        }
                    default:
                        {
                            maint *= (int)Size / 50;
                            break;
                        }
                }
            }

            if ((ship.Role == ShipData.RoleName.freighter || ship.Role == ShipData.RoleName.platform) && empire.data.CivMaintMod != 1.0)
            {
                maint *= empire.data.CivMaintMod;
            }

            //Apply Privatization
            if ((ship.Role == ShipData.RoleName.freighter || ship.Role == ShipData.RoleName.platform) && empire.data.Privatization)
            {
                maint *= 0.5f;
            }

            //Subspace Projectors do not get any more modifiers
            if (ship.Name == "Subspace Projector")
            {
                return maint;
            }

            //Maintenance fluctuator
            //string configvalue1 = ConfigurationManager.AppSettings["countoffiles"];
            float OptionIncreaseShipMaintenance = GlobalStats.ShipMaintenanceMulti;
            if (OptionIncreaseShipMaintenance > 1)
            {
                maintModReduction = OptionIncreaseShipMaintenance;
                maint *= maintModReduction;
            }
            return maint;
        }

        private float GetMaintCostShipyardProportional(ShipData ship, float fCost, Empire empire)
        {
            float maint = 0f;

            // Calculate maintenance by proportion of ship cost, Duh.
            if (ship.Role == ShipData.RoleName.fighter || ship.Role == ShipData.RoleName.scout)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepFighter;
            else if (ship.Role == ShipData.RoleName.corvette || ship.Role == ShipData.RoleName.gunboat)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepCorvette;
            else if (ship.Role == ShipData.RoleName.frigate || ship.Role == ShipData.RoleName.destroyer)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepFrigate;
            else if (ship.Role == ShipData.RoleName.cruiser)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepCruiser;
            else if (ship.Role == ShipData.RoleName.carrier)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepCarrier;
            else if (ship.Role == ShipData.RoleName.capital)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepCapital;
            else if (ship.Role == ShipData.RoleName.freighter)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepFreighter;
            else if (ship.Role == ShipData.RoleName.platform)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepPlatform;
            else if (ship.Role == ShipData.RoleName.station)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepStation;
            else if (ship.Role == ShipData.RoleName.drone && GlobalStats.ActiveModInfo.useDrones)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepDrone;
            else
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepBaseline;
            if (maint == 0f && GlobalStats.ActiveModInfo.UpkeepBaseline > 0)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepBaseline;
            else if (maint == 0f && GlobalStats.ActiveModInfo.UpkeepBaseline == 0)
                maint = fCost * 0.004f;


            // Modifiers below here  

            if ((ship.Role == ShipData.RoleName.freighter || ship.Role == ShipData.RoleName.platform) && empire != null && !empire.isFaction && empire.data.CivMaintMod != 1.0)
            {
                maint *= empire.data.CivMaintMod;
            }

            if ((ship.Role == ShipData.RoleName.freighter || ship.Role == ShipData.RoleName.platform) && empire != null && !empire.isFaction && empire.data.Privatization)
            {
                maint *= 0.5f;
            }

            if (GlobalStats.ShipMaintenanceMulti > 1)
            {
                maint *= GlobalStats.ShipMaintenanceMulti;
            }
            return maint;

        }


        private string GetNumberString(float stat)
        {
            if (stat < 1000f)
                return stat.ToString("#.#");
            else if (stat < 10000f)
                return stat.ToString("#");
            float single = stat / 1000f;
            if (single < 100)
                return string.Concat(single.ToString("#.##"), "k");
            if(single < 1000)
                return string.Concat(single.ToString("#.#"), "k");
            return string.Concat(single.ToString("#"), "k");
        }

        //Mer - Gretman left off here

        private string GetConduitGraphic(SlotStruct ss)
        {
            bool right  = false;
            bool left   = false;
            bool up     = false;
            bool down   = false;
            int numNear = 0;
            foreach (SlotStruct slot in this.Slots)
            {
                if (slot.Module == null || slot.Module.ModuleType != ShipModuleType.PowerConduit || slot == ss)
                {
                    continue;
                }
                int totalDistanceX = Math.Abs(slot.PQ.X - ss.PQ.X) / 16;
                int totalDistanceY = Math.Abs(slot.PQ.Y - ss.PQ.Y) / 16;
                if (totalDistanceX == 1 && totalDistanceY == 0)
                {
                    if (slot.PQ.X <= ss.PQ.X)
                    {
                        right = true;
                    }
                    else
                    {
                        left = true;
                    }
                }
                if (totalDistanceY != 1 || totalDistanceX != 0)
                {
                    continue;
                }
                if (slot.PQ.Y <= ss.PQ.Y)
                {
                    down = true;
                }
                else
                {
                    up = true;
                }
            }
            if (left)
            {
                numNear++;
            }
            if (right)
            {
                numNear++;
            }
            if (up)
            {
                numNear++;
            }
            if (down)
            {
                numNear++;
            }
            if (numNear <= 1)
            {
                if (up)
                {
                    return "conduit_powerpoint_up";
                }
                if (down)
                {
                    return "conduit_powerpoint_down";
                }
                if (left)
                {
                    return "conduit_powerpoint_left";
                }
                if (right)
                {
                    return "conduit_powerpoint_right";
                }
                return "conduit_intersection";
            }
            if (numNear != 3)
            {
                if (numNear == 4)
                {
                    return "conduit_intersection";
                }
                if (numNear == 2)
                {
                    if (left && up)
                    {
                        return "conduit_corner_TL";
                    }
                    if (left && down)
                    {
                        return "conduit_corner_BL";
                    }
                    if (right && up)
                    {
                        return "conduit_corner_TR";
                    }
                    if (right && down)
                    {
                        return "conduit_corner_BR";
                    }
                    if (up && down)
                    {
                        return "conduit_straight_vertical";
                    }
                    if (left && right)
                    {
                        return "conduit_straight_horizontal";
                    }
                }
            }
            else
            {
                if (up && down && left)
                {
                    return "conduit_tsection_right";
                }
                if (up && down && right)
                {
                    return "conduit_tsection_left";
                }
                if (left && right && down)
                {
                    return "conduit_tsection_up";
                }
                if (left && right && up)
                {
                    return "conduit_tsection_down";
                }
            }
            return "";
        }


        private bool SlotStructFits(SlotStruct slot)
        {
            int numFreeSlots = 0;
            int sx = slot.PQ.X, sy = slot.PQ.Y;
            for (int y = 0; y < ActiveModule.YSIZE; ++y)
            {
                for (int x = 0; x < ActiveModule.XSIZE; ++x)
                {
                    for (int i = 0; i < Slots.Count; ++i)
                    {
                        SlotStruct ss = Slots[i];
                        if (ss.ShowValid && ss.PQ.Y == (sy + 16 * y) && ss.PQ.X == (sx + 16 * x))
                        {
                            if (ss.Module == null && ss.Parent == null)
                                ++numFreeSlots;
                        }
                    }
                }
            }
            return numFreeSlots == (ActiveModule.XSIZE * ActiveModule.YSIZE);
        }
        
        private void InstallModule(SlotStruct slot)
        {
            if (SlotStructFits(slot))
            {
                DesignAction designAction = new DesignAction
                {
                    clickedSS = new SlotStruct
                    {
                        PQ = slot.PQ,
                        Restrictions = slot.Restrictions,
                        Facing = slot.Module?.Facing ?? 0.0f,
                        ModuleUID = slot.ModuleUID,
                        Module = slot.Module,
                        Tex = slot.Tex,
                        SlotReference = slot.SlotReference,
                        State = slot.State
                    }
                };
                DesignStack.Push(designAction);
                ClearSlot(slot);
                ClearDestinationSlots(slot);

                slot.ModuleUID            = ActiveModule.UID;
                slot.Module               = ShipModule.CreateNoParent(ActiveModule.UID);
                slot.Module.XSIZE         = ActiveModule.XSIZE;
                slot.Module.YSIZE         = ActiveModule.YSIZE;
                slot.Module.XMLPosition   = ActiveModule.XMLPosition;
                slot.State                = ActiveModState;
                slot.Module.hangarShipUID = ActiveModule.hangarShipUID;
                slot.Module.Facing        = ActiveModule.Facing;
                slot.Tex = ResourceManager.Texture(ActiveModule.IconTexturePath);
                slot.Module.SetAttributesNoParent();

                RecalculatePower();
                ShipSaved = false;
                if (ActiveModule.ModuleType != ShipModuleType.Hangar)
                {
                    ActiveModule = ShipModule.CreateNoParent(ActiveModule.UID);
                }
                ChangeModuleState(ActiveModState);
            }
            else PlayNegativeSound();
        }

        private void InstallModuleFromLoad(SlotStruct slot)
        {
            if (SlotStructFits(slot))
            {
                ActiveModuleState activeModuleState = slot.State;
                ClearSlot(slot);
                ClearDestinationSlotsNoStack(slot);
                slot.ModuleUID = ActiveModule.UID;
                slot.Module    = ActiveModule; 
                slot.State     = activeModuleState;
                slot.Module.Facing = slot.Facing;
                slot.Tex = ResourceManager.TextureDict[ActiveModule.IconTexturePath];
                slot.Module.SetAttributesNoParent();

                RecalculatePower();
            }
            else PlayNegativeSound();
        }

        private void InstallModuleNoStack(SlotStruct slot)
        {
            if (SlotStructFits(slot))
            {
                ClearSlotNoStack(slot);
                ClearDestinationSlotsNoStack(slot);
                slot.ModuleUID            = ActiveModule.UID;
                slot.Module               = ActiveModule;
                slot.State                = ActiveModState;
                slot.Module.hangarShipUID = ActiveModule.hangarShipUID;
                slot.Module.Facing        = ActiveModule.Facing;
                slot.Tex = ResourceManager.TextureDict[ResourceManager.GetModuleTemplate(ActiveModule.UID).IconTexturePath];
                slot.Module.SetAttributesNoParent();

                RecalculatePower();
                ShipSaved = false;
                if (ActiveModule.ModuleType != ShipModuleType.Hangar)
                {
                    ActiveModule = ShipModule.CreateNoParent(ActiveModule.UID);
                }
                //grabs a fresh copy of the same module type to cursor 
                ChangeModuleState(ActiveModState);
                //set rotation for new module at cursor
            }
            else PlayNegativeSound();
        }

        public void PlayNegativeSound() => GameAudio.PlaySfxAsync("UI_Misc20");

        private void RecalculatePower()
        {
            foreach (SlotStruct slotStruct in this.Slots)
            {
                slotStruct.Powered = false;
                slotStruct.CheckedConduits = false;
                if (slotStruct.Module != null)
                    slotStruct.Module.Powered = false;
            }
            foreach (SlotStruct slotStruct in this.Slots)
            {
                //System.Diagnostics.Debug.Assert(slotStruct.parent != null, "parent is null");                   
                if (slotStruct.Module != null && slotStruct.Module.ModuleType == ShipModuleType.PowerPlant)
                {
                    foreach (SlotStruct slot in this.Slots)
                    {
                        if (slot.Module != null && slot.Module.ModuleType == ShipModuleType.PowerConduit && (Math.Abs(slot.PQ.X - slotStruct.PQ.X) / 16 + Math.Abs(slot.PQ.Y - slotStruct.PQ.Y) / 16 == 1 && slot.Module != null))
                            this.CheckAndPowerConduit(slot);
                    }
                }                
                else if (slotStruct.Parent != null)               
                {
                    //System.Diagnostics.Debug.Assert(slotStruct.parent.module != null, "parent is fine, module is null");
                    if (slotStruct.Parent.Module != null)
                    {
                        //System.Diagnostics.Debug.Assert(slotStruct.parent.module.ModuleType != null, "parent is fine, module is fine, moduletype is null");
                        if (slotStruct.Parent.Module.ModuleType == ShipModuleType.PowerPlant)
                        {
                            foreach (SlotStruct slot in this.Slots)
                            {
                                if (slot.Module != null && slot.Module.ModuleType == ShipModuleType.PowerConduit && (Math.Abs(slot.PQ.X - slotStruct.PQ.X) / 16 + Math.Abs(slot.PQ.Y - slotStruct.PQ.Y) / 16 == 1 && slot.Module != null))
                                    this.CheckAndPowerConduit(slot);
                            }
                        }
                    }
                }
            }
            foreach (SlotStruct slotStruct1 in Slots)
            {
                if (slotStruct1.Module != null && slotStruct1.Module.PowerRadius > 0 && (slotStruct1.Module.ModuleType != ShipModuleType.PowerConduit || slotStruct1.Module.Powered))
                {
                    foreach (SlotStruct slotStruct2 in Slots)
                    {
                        if (Math.Abs(slotStruct1.PQ.X - slotStruct2.PQ.X) / 16 + Math.Abs(slotStruct1.PQ.Y - slotStruct2.PQ.Y) / 16 <= (int)slotStruct1.Module.PowerRadius)
                            slotStruct2.Powered = true;
                    }
                    if (slotStruct1.Module.XSIZE <= 1 && slotStruct1.Module.YSIZE <= 1)
                        continue;

                    for (int y = 0; y < slotStruct1.Module.YSIZE; ++y)
                    {
                        for (int x = 0; x < slotStruct1.Module.XSIZE; ++x)
                        {
                            if (x == 0 && y == 0) continue;
                            foreach (SlotStruct slotStruct2 in Slots)
                            {
                                if (slotStruct2.PQ.Y == slotStruct1.PQ.Y + 16 * y && slotStruct2.PQ.X == slotStruct1.PQ.X + 16 * x)
                                {
                                    foreach (SlotStruct slotStruct3 in Slots)
                                    {
                                        if (Math.Abs(slotStruct2.PQ.X - slotStruct3.PQ.X) / 16 + Math.Abs(slotStruct2.PQ.Y - slotStruct3.PQ.Y) / 16 <= (int)slotStruct1.Module.PowerRadius)
                                            slotStruct3.Powered = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            foreach (SlotStruct slotStruct in this.Slots)
            {
                if (slotStruct.Powered)
                {
                    if (slotStruct.Module != null && slotStruct.Module.ModuleType != ShipModuleType.PowerConduit)
                        slotStruct.Module.Powered = true;
                    if (slotStruct.Parent != null && slotStruct.Parent.Module != null)
                        slotStruct.Parent.Module.Powered = true;                    
                }
                if (!slotStruct.Powered && slotStruct.Module != null && slotStruct.Module.IndirectPower)
                        slotStruct.Module.Powered = true;
            }
        }

        public void SetActiveModule(ShipModule mod)
        {

            GameAudio.PlaySfxAsync("smallservo");
            mod.SetAttributesNoParent();
            this.ActiveModule = mod;
            this.ResetModuleState();
            foreach (SlotStruct s in this.Slots)                                    
                s.SetValidity(ActiveModule);
            
            if (this.ActiveHangarModule != this.ActiveModule && this.ActiveModule.ModuleType == ShipModuleType.Hangar)
            {
                this.ActiveHangarModule = this.ActiveModule;
                this.ChooseFighterSL.Entries.Clear();
                this.ChooseFighterSL.Copied.Clear();
                foreach (string shipname in EmpireManager.Player.ShipsWeCanBuild)
                {
                    if (!this.ActiveModule.PermittedHangarRoles.Contains(Ship_Game.ResourceManager.ShipsDict[shipname].shipData.GetRole()) || Ship_Game.ResourceManager.ShipsDict[shipname].Size >= this.ActiveModule.MaximumHangarShipSize)
                    {
                        continue;
                    }
                    this.ChooseFighterSL.AddItem(Ship_Game.ResourceManager.ShipsDict[shipname]);
                }
                if (this.HangarShipUIDLast != "Undefined" && this.ActiveModule.PermittedHangarRoles.Contains(Ship_Game.ResourceManager.ShipsDict[HangarShipUIDLast].shipData.GetRole()) && this.ActiveModule.MaximumHangarShipSize >= Ship_Game.ResourceManager.ShipsDict[HangarShipUIDLast].Size)
                {
                    this.ActiveModule.hangarShipUID = this.HangarShipUIDLast;
                }
                else if (this.ChooseFighterSL.Entries.Count > 0)
                {
                    this.ActiveModule.hangarShipUID = (this.ChooseFighterSL.Entries[0].item as Ship).Name;
                }
            }
            this.HighlightedModule = null;
            this.HoveredModule = null;
            this.ResetModuleState();
        }

        public void UpdateHangarOptions(ShipModule mod)
        {
            if (this.ActiveHangarModule != mod &&  mod.ModuleType == ShipModuleType.Hangar)
            {
                this.ActiveHangarModule = mod;
                this.ChooseFighterSL.Entries.Clear();
                this.ChooseFighterSL.Copied.Clear();
                foreach (string shipname in EmpireManager.Player.ShipsWeCanBuild)
                {
                    if (!mod.PermittedHangarRoles.Contains(ResourceManager.ShipsDict[shipname].shipData.GetRole()) || ResourceManager.ShipsDict[shipname].Size >= mod.MaximumHangarShipSize)
                    {
                        continue;
                    }
                    ChooseFighterSL.AddItem(ResourceManager.ShipsDict[shipname]);
                }
            }
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            float DesiredZ = MathHelper.SmoothStep(this.Camera.Zoom, this.TransitionZoom, 0.2f);
            this.Camera.Zoom = DesiredZ;
            if (this.Camera.Zoom < 0.3f)
            {
                this.Camera.Zoom = 0.3f;
            }
            if (this.Camera.Zoom > 2.65f)
            {
                this.Camera.Zoom = 2.65f;
            }

                this.cameraPosition.Z = this.OriginalZ / this.Camera.Zoom;
            Vector3 camPos = this.cameraPosition * new Vector3(-1f, 1f, 1f);
            this.View = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(180f.ToRadians())) * Matrix.CreateRotationX(0f.ToRadians())) * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        //Added by McShooterz: modifies weapon stats to reflect weapon tag bonuses
        private float ModifiedWeaponStat(Weapon weapon, string stat)
        {
            float value=0;

            switch (stat)
            {
                case "damage":
                    value = weapon.DamageAmount;
                    break;
                case "range":
                    value = weapon.Range;
                    break;
                case "speed":
                    value = weapon.ProjectileSpeed;
                    break;
                case "firedelay":
                    value = weapon.fireDelay;
                    break;
                case "armor":
                    value = weapon.EffectVsArmor;
                    break;
                case "shield":
                    value = weapon.EffectVSShields;
                    break;
            }

            if (weapon.Tag_Missile)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Missile"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Missile"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Missile"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Missile"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Missile"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Missile"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Energy)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Energy"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Energy"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Energy"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Energy"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Energy"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Energy"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Torpedo)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Torpedo"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Torpedo"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Torpedo"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Torpedo"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Torpedo"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Torpedo"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Kinetic)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Kinetic"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Kinetic"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Kinetic"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Kinetic"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Kinetic"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Kinetic"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Hybrid)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Hybrid"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Hybrid"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Hybrid"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Hybrid"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Hybrid"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Hybrid"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Railgun)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Railgun"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Railgun"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Railgun"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Railgun"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Railgun"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Railgun"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Explosive)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Explosive"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Explosive"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Explosive"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Explosive"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Explosive"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Explosive"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Guided)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Guided"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Guided"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Guided"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Guided"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Guided"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Guided"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Intercept)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Intercept"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Intercept"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Intercept"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Intercept"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Intercept"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Intercept"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_PD)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["PD"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["PD"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["PD"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["PD"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["PD"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["PD"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_SpaceBomb)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Spacebomb"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Spacebomb"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Spacebomb"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Spacebomb"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Spacebomb"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Spacebomb"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_BioWeapon)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["BioWeapon"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["BioWeapon"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["BioWeapon"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["BioWeapon"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["BioWeapon"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["BioWeapon"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Drone)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Drone"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Drone"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Drone"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Drone"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Drone"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Drone"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Subspace)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Subspace"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Subspace"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Subspace"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Subspace"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Subspace"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Subspace"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Warp)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Warp"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Warp"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Warp"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Warp"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Warp"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Warp"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Cannon)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Cannon"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Cannon"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Cannon"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Cannon"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Cannon"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Cannon"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Beam)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Beam"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Beam"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Beam"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Beam"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Beam"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Beam"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Bomb)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Bomb"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Bomb"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Bomb"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Bomb"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Bomb"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Bomb"].ShieldDamage;
                        break;
                }
            }
            return value;
        }

        public enum ActiveModuleState
        {
            Normal,
            Left,
            Right,
            Rear
        }

        private enum Colors     //Unused
        {
            Black,
            Red,
            Blue,
            Orange,
            Yellow,
            Green
        }

        private struct ModuleCatButton
        {
            public Rectangle mRect;

            public string Category;
        }

        private enum SlotModOperation
        {
            Delete,
            I,
            O,
            E,
            IO,
            IE,
            OE,
            IOE,
            Normal

        }
        private class WeaponScrollList: ScrollList
        {
            private readonly ShipDesignScreen Screen;
            public WeaponScrollList(Submenu weaponList, ShipDesignScreen shipDesignScreen) : base(weaponList)
            {            
                Screen = shipDesignScreen;
            }
            public override bool HandleInput(InputState input)
            {                
                Vector2 mousePos = input.CursorPosition;
                for (int index = indexAtTop;
                    index < Copied.Count
                    && index < indexAtTop + entriesToDisplay;
                    ++index)
                {
                    Entry e = Copied[index];
                    if (e.item is ModuleHeader moduleHeader)
                    {
                        if (moduleHeader.HandleInput(input, e))
                            return true;
                    }
                    else if (e.clickRect.HitTest(mousePos))
                    {                        
                        e.clickRectHover = 1;
                        if (!input.InGameSelect) continue;
                        Screen.SetActiveModule(ShipModule.CreateNoParent(((ShipModule) e.item).UID));
                        Screen.ResetModuleState();
                        return true;
                    }
                    else
                        e.clickRectHover = 0;
                }
                return base.HandleInput(input);

            }
            public void DrawModules()
            {
                if (!Screen.ModSel.Tabs[3].Selected) return;
                if (!Screen.Reset)
                {
                    Screen.DrawList();
                    return;
                }
                Entries.Clear();
                var moduleCategories = new Array<string>();
                foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
                {
                    if (!EmpireManager.Player.IsModuleUnlocked(module.Key) || module.Value.UID == "Dummy")
                    {
                        continue;
                    }
                    module.Value.ModuleType.ToString();
                    ShipModule tmp = ShipModule.CreateNoParent(module.Key);
                    tmp.SetAttributesNoParent();

                    if (Screen.RestrictedModCheck(Screen.ActiveHull.Role, tmp)) continue;

                    if ((tmp.ModuleType == ShipModuleType.Troop || tmp.ModuleType == ShipModuleType.Colony ||
                         tmp.ModuleType == ShipModuleType.Command || tmp.ModuleType == ShipModuleType.Storage ||
                         tmp.ModuleType == ShipModuleType.Hangar || tmp.ModuleType == ShipModuleType.Sensors ||
                         tmp.ModuleType == ShipModuleType.Special || tmp.ModuleType == ShipModuleType.Transporter ||
                         tmp.ModuleType == ShipModuleType.Ordnance ||
                         tmp.ModuleType == ShipModuleType.Construction) &&
                        !moduleCategories.Contains(tmp.ModuleType.ToString()))
                    {
                        moduleCategories.Add(tmp.ModuleType.ToString());
                        ModuleHeader type = new ModuleHeader(tmp.ModuleType.ToString(), 240f);
                        AddItem(type);
                    }
                    tmp = null;
                }
                foreach (Entry e in Entries)
                {
                    foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
                    {
                        if (!EmpireManager.Player.GetMDict()[module.Key] || module.Value.UID == "Dummy")
                        {
                            continue;
                        }
                        ShipModule tmp = ShipModule.CreateNoParent(module.Key);
                        tmp.SetAttributesNoParent();

                        if (Screen.RestrictedModCheck(Screen.ActiveHull.Role, tmp)) continue;

                        if ((tmp.ModuleType == ShipModuleType.Troop || tmp.ModuleType == ShipModuleType.Colony ||
                             tmp.ModuleType == ShipModuleType.Command || tmp.ModuleType == ShipModuleType.Storage ||
                             tmp.ModuleType == ShipModuleType.Hangar || tmp.ModuleType == ShipModuleType.Sensors ||
                             tmp.ModuleType == ShipModuleType.Special ||
                             tmp.ModuleType == ShipModuleType.Transporter ||
                             tmp.ModuleType == ShipModuleType.Ordnance ||
                             tmp.ModuleType == ShipModuleType.Construction) &&
                            ((ModuleHeader) e.item).Text == tmp.ModuleType.ToString())
                        {
                            e.AddItem(module.Value);
                        }
                        tmp = null;
                    }
                }
                Screen.Reset = false;
            }
        }
    }
}