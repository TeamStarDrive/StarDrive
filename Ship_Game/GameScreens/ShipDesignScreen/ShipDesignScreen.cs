using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.UI;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game
{
    public sealed partial class ShipDesignScreen
    {
        private Matrix View;
        private Matrix Projection;
        public Camera2D Camera;
        public Array<ToggleButton> CombatStatusButtons = new Array<ToggleButton>();
        public bool Debug;
        public ShipData ActiveHull;
        public EmpireUIOverlay EmpireUI;
        //private Menu1 ModuleSelectionMenu;
        private SceneObject shipSO;
        private Vector3 CameraPosition = new Vector3(0f, 0f, 1300f);
        public Array<SlotStruct> Slots = new Array<SlotStruct>();
        private Vector2 Offset;
        private CombatState CombatState = CombatState.AttackRuns;
        private bool ShipSaved = true;
        private Array<ShipData> AvailableHulls = new Array<ShipData>();
        private UIButton ToggleOverlayButton;
        private UIButton SaveButton;
        private UIButton LoadButton;
        public ModuleSelection ModSel;
        private Submenu StatsSub;
        private Menu1 ShipStats;
        private bool LowRes;
        private float LowestX;
        private float HighestX;
        private GenericButton ArcsButton;
        private CloseButton Close;
        private float OriginalZ;
        private Rectangle SearchBar;
        private Rectangle BottomSep;
        private ScrollList HullSL;
        private WeaponScrollList WeaponSL;
        private Rectangle HullSelectionRect;
        private Submenu HullSelectionSub;
        private Rectangle BlackBar;
        private Rectangle SideBar;

        public ShipModule HighlightedModule;
        private Vector2 CameraVelocity              = Vector2.Zero;
        private Vector2 StartDragPos                = new Vector2();
        private ShipData Changeto;
        private string ScreenToLaunch;
        private bool ShowAllArcs;
        private ShipModule HoveredModule;
        private float TransitionZoom                = 1f;
        private SlotModOperation Operation;
        public ShipModule ActiveModule;
        private ActiveModuleState ActiveModState;
        private Selector selector;
        public bool ToggleOverlay                   = true;
        private Vector2 starfieldPos                = Vector2.Zero;
        private CategoryDropDown CategoryList;
        private Rectangle DropdownRect;
        private Vector2 ClassifCursor;
        public Stack<DesignAction> DesignStack      = new Stack<DesignAction>();
        private string LastActiveUID                = "";                                      //Gretman - To Make the Ctrl-Z much more responsive
        private Vector2 LastDesignActionPos         = Vector2.Zero;
        private Vector2 CoBoxCursor;
        private UICheckBox CarrierOnlyBox;
        private bool Fml                            = false;
        private bool Fmlevenmore                    = false;
        public bool CarrierOnly;
        private ShipData.Category LoadCategory;
        private HashSet<string> Techs               = new HashSet<string>();
        private readonly Texture2D TopBar132        = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px");
        private readonly Texture2D TopBar132Hover   = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px_hover");
        private readonly Texture2D TopBar132Pressed = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px_pressed");
        private readonly Texture2D TopBar68         = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_68px");
        private readonly Texture2D TopBar68Hover    = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_68px_hover");
        private readonly Texture2D TopBar68Pressed  = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_pressed"];
        private ShipData.RoleName Role;
        private Rectangle DesignRoleRect;


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
                    ActiveModule.XSIZE = moduleTemplate.XSIZE;
                    ActiveModule.YSIZE = moduleTemplate.YSIZE;
                    ActiveModState = ActiveModuleState.Normal;
                    return;
                }
                case ActiveModuleState.Left:
                {
                    ActiveModule.XSIZE = y; // @todo Why are these swapped? Please comment.
                    ActiveModule.YSIZE = x; // These are swapped because if the module is facing left or right, then the length is now the height, and vice versa
                    ActiveModState = ActiveModuleState.Left;
                    ActiveModule.Facing = 270f;
                    return;
                }
                case ActiveModuleState.Right:
                {
                    ActiveModule.XSIZE = y; // @todo Why are these swapped? Please comment.
                    ActiveModule.YSIZE = x; // These are swapped because if the module is facing left or right, then the length is now the height, and vice versa
                    ActiveModState = ActiveModuleState.Right;
                    ActiveModule.Facing = 90f;
                    return;
                }
                case ActiveModuleState.Rear:
                {
                    ActiveModule.XSIZE = moduleTemplate.XSIZE;
                    ActiveModule.YSIZE = moduleTemplate.YSIZE;
                    ActiveModState = ActiveModuleState.Rear;
                    ActiveModule.Facing = 180f;
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
                if (ss.CheckedConduits
                    || ss == slot
                    || ss.Module == null
                    || !slot.IsNeighbourTo(ss)
                    || ss.Module.ModuleType != ShipModuleType.PowerConduit)
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
        private void ClearDestinationSlots(SlotStruct slot)
        {
            for (int y = 0; y < ActiveModule.YSIZE; y++)
            {
                for (int x = 0; x < ActiveModule.XSIZE; x++)
                {
                    if (!FindStructFromOffset(slot, x, y, out SlotStruct slot2))
                        continue;
                    if (slot2.Module != null || slot2.Parent != null) 
                    {
                        ClearParentSlot(slot2.Parent ?? slot2); 
                    }
                    
                    slot2.ModuleUID = null;
                    slot2.Tex       = null;
                    slot2.Module    = null;
                    slot2.Parent    = slot;
                    slot2.State     = ActiveModuleState.Normal;
                }
            }
        }

        // @todo This is all broken. Redo everything.
        private void ClearParentSlot(SlotStruct parent, bool addToAlteredSlots = true)
        {
            //actually supposed to clear ALL slots of a module, not just the parent
            if (addToAlteredSlots && DesignStack.Count > 0)
            {
                DesignStack.Peek().AlteredSlots.Add(new SlotStruct(parent));
            }
            if (parent.Module != null)
            {
                for (int y = 0; y < parent.Module.YSIZE; ++y)
                {
                    for (int x = 0; x < parent.Module.XSIZE; ++x)
                    {
                        if (!FindStructFromOffset(parent, x, y, out SlotStruct slot2))
                            continue;
                        slot2.Clear();
                    }
                }
            }
            parent.Clear();
        }

        private void ClearSlot(SlotStruct slot, bool addToAlteredSlots = true)
        {   
            //this is the clearslot function actually used atm
            //only called from installmodule atm, not from manual module removal
            if (slot.Module != null || slot.Parent != null)
            {
                ClearParentSlot(slot.Parent ?? slot, addToAlteredSlots);
            }
            else
            {
                //this requires not being a child slot and not containing a module
                //only empty parent slots can trigger this
                //why would we want to clear an empty slot?
                //might be used on initial load instead of a proper slot constructor
                slot.Clear();
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

        private void DebugAlterSlot(Vector2 slotPos, SlotModOperation op)
        {
            ModuleSlotData toRemove = FindModuleSlotAtPos(slotPos);
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

        protected override void Destroy()
        {
            HullSL?.Dispose(ref HullSL);
            ModSel?.Dispose();
            base.Destroy();
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
                            maint *= (int)Size / 50f;
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
            switch (ship.Role) {
                case ShipData.RoleName.fighter:
                case ShipData.RoleName.scout:
                    maint = fCost * GlobalStats.ActiveModInfo.UpkeepFighter;
                    break;
                case ShipData.RoleName.corvette:
                case ShipData.RoleName.gunboat:
                    maint = fCost * GlobalStats.ActiveModInfo.UpkeepCorvette;
                    break;
                case ShipData.RoleName.frigate:
                case ShipData.RoleName.destroyer:
                    maint = fCost * GlobalStats.ActiveModInfo.UpkeepFrigate;
                    break;
                case ShipData.RoleName.cruiser:
                    maint = fCost * GlobalStats.ActiveModInfo.UpkeepCruiser;
                    break;
                case ShipData.RoleName.carrier:
                    maint = fCost * GlobalStats.ActiveModInfo.UpkeepCarrier;
                    break;
                case ShipData.RoleName.capital:
                    maint = fCost * GlobalStats.ActiveModInfo.UpkeepCapital;
                    break;
                case ShipData.RoleName.freighter:
                    maint = fCost * GlobalStats.ActiveModInfo.UpkeepFreighter;
                    break;
                case ShipData.RoleName.platform:
                    maint = fCost * GlobalStats.ActiveModInfo.UpkeepPlatform;
                    break;
                case ShipData.RoleName.station:
                    maint = fCost * GlobalStats.ActiveModInfo.UpkeepStation;
                    break;
                default:
                    if (ship.Role == ShipData.RoleName.drone && GlobalStats.ActiveModInfo.useDrones)
                        maint = fCost * GlobalStats.ActiveModInfo.UpkeepDrone;
                    else
                        maint = fCost * GlobalStats.ActiveModInfo.UpkeepBaseline;
                    break;
            }
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
            if (stat < 10000f)
                return stat.ToString("#");
            float single = stat / 1000f;
            if (single < 100)
                return string.Concat(single.ToString("#.##"), "k");
            if(single < 1000)
                return string.Concat(single.ToString("#.#"), "k");
            return string.Concat(single.ToString("#"), "k");
        }

        private string GetConduitGraphic(SlotStruct ss)
        {
            var conduit = new Ship.ConduitGraphic();
            foreach (SlotStruct slot in Slots)
                if (slot.Module?.ModuleType == ShipModuleType.PowerConduit)
                    conduit.Add(slot.PQ.X - ss.PQ.X, slot.PQ.Y - ss.PQ.Y);
            return conduit.GetGraphic();
        }


        public bool SlotStructFits(SlotStruct slot, ShipModule activeModule = null, bool rotated = false)
        {
            activeModule = activeModule ?? ActiveModule;
            int numFreeSlots = 0;
            int sx = slot.PQ.X, sy = slot.PQ.Y;
            int xSize = rotated ? activeModule.YSIZE : activeModule.XSIZE;
            int ySize = rotated ? activeModule.XSIZE : activeModule.YSIZE;
            for (int x = 0; x < xSize; ++x) 
            {
                for (int y = 0; y < ySize; ++y)
                {
                    for (int i = 0; i < Slots.Count; ++i)
                    {
                        SlotStruct ss = Slots[i];
                        if (ss.ShowValid && ss.PQ.Y == sy + (16 * y) && ss.PQ.X == sx + (16 * x))
                        {
                            ++numFreeSlots;
                        }
                    }
                }
            }
            return numFreeSlots == (activeModule.XSIZE * activeModule.YSIZE);
        }

        public ShipModule CreateDesignModule(string uid)
        {
            return ShipModule.CreateNoParent(uid, EmpireManager.Player, ActiveHull);
        }
        
        private void InstallModule(SlotStruct slot)
        {
            if (!SlotStructFits(slot))
            {
                PlayNegativeSound();
                return;
            }

            var designAction = new DesignAction
            {
                clickedSS = new SlotStruct
                {
                    PQ            = slot.PQ,
                    Restrictions  = slot.Restrictions,
                    Facing        = slot.Module?.Facing ?? 0.0f,
                    ModuleUID     = slot.ModuleUID,
                    Module        = slot.Module,
                    Tex           = slot.Tex,
                    SlotReference = slot.SlotReference,
                    State         = slot.State,
                }
            };
            DesignStack.Push(designAction);
            ClearSlot(slot);
            ClearDestinationSlots(slot);
            ChangeModuleState(ActiveModState);
            slot.ModuleUID            = ActiveModule.UID;
            slot.Module               = CreateDesignModule(ActiveModule.UID);
            slot.Module.XSIZE         = ActiveModule.XSIZE;
            slot.Module.YSIZE         = ActiveModule.YSIZE;
            slot.Module.XMLPosition   = ActiveModule.XMLPosition;
            slot.State                = ActiveModState;
            slot.Module.hangarShipUID = ActiveModule.hangarShipUID;
            slot.Module.Facing        = ActiveModule.Facing;
            slot.Tex                  = ActiveModule.ModuleTexture;
            slot.Module.SetAttributesNoParent();

            RecalculatePower();
            ShipSaved = false;
            ActiveModule = CreateDesignModule(ActiveModule.UID);
            ChangeModuleState(ActiveModState);
        }

        private void InstallModuleFromLoad(SlotStruct slot)
        {
            if (SlotStructFits(slot))
            {
                ActiveModuleState activeModuleState = slot.State;
                ClearSlot(slot);
                ClearDestinationSlots(slot);
                slot.ModuleUID     = ActiveModule.UID;
                slot.Module        = ActiveModule; 
                slot.State         = activeModuleState;
                slot.Module.Facing = slot.Facing;
                slot.Tex           = ActiveModule.ModuleTexture;
                slot.Module.SetAttributesNoParent();
                RecalculatePower();
            }
            else PlayNegativeSound();
        }

        private void InstallModuleNoStack(SlotStruct slot)
        {
            if (!SlotStructFits(slot))
            {
                PlayNegativeSound();
                return;
            }

            ClearSlotNoStack(slot);
            ClearDestinationSlots(slot);
            slot.ModuleUID            = ActiveModule.UID;
            slot.Module               = ActiveModule;
            slot.State                = ActiveModState;
            slot.Module.hangarShipUID = ActiveModule.hangarShipUID;
            slot.Module.Facing        = ActiveModule.Facing;
            slot.Tex                  = ActiveModule.ModuleTexture;
            slot.Module.SetAttributesNoParent();

            RecalculatePower();
            ShipSaved = false;
            if (ActiveModule.ModuleType != ShipModuleType.Hangar)
            {
                ActiveModule = CreateDesignModule(ActiveModule.UID);
            }

            //grabs a fresh copy of the same module type to cursor 
            ChangeModuleState(ActiveModState);
            //set rotation for new module at cursor
        }

        public void PlayNegativeSound() => GameAudio.PlaySfxAsync("UI_Misc20");

        private void RecalculatePower()
        {
            // reset everything
            foreach (SlotStruct slot in Slots)
            {
                slot.Powered = false;
                slot.CheckedConduits = false;
                if (slot.Module != null)
                    slot.Module.Powered = false;
            }

            foreach (SlotStruct slotStruct in Slots)
            {
                //System.Diagnostics.Debug.Assert(slotStruct.parent != null, "parent is null");                   
                if (slotStruct.Module != null && slotStruct.Module.ModuleType == ShipModuleType.PowerPlant)
                {
                    foreach (SlotStruct slot in Slots)
                    {
                        if (slot.Module != null && slot.Module.ModuleType == ShipModuleType.PowerConduit && slot.IsNeighbourTo(slotStruct))
                            CheckAndPowerConduit(slot);
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
                            foreach (SlotStruct slot in Slots)
                            {
                                if (slot.Module != null && slot.Module.ModuleType == ShipModuleType.PowerConduit && slot.IsNeighbourTo(slotStruct))
                                    CheckAndPowerConduit(slot);
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
            if (mod == null) return;
            GameAudio.PlaySfxAsync("smallservo");
            mod.SetAttributesNoParent();
            ActiveModule = mod;
            foreach (SlotStruct s in Slots)                                    
                s.SetValidity(ActiveModule);
            
            HighlightedModule = null;
            HoveredModule = null;
        }        

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            float desiredZ = MathHelper.SmoothStep(Camera.Zoom, TransitionZoom, 0.2f);
            Camera.Zoom = desiredZ;
            if (Camera.Zoom < 0.3f)
            {
                Camera.Zoom = 0.3f;
            }
            if (Camera.Zoom > 2.65f)
            {
                Camera.Zoom = 2.65f;
            }

            var modules = new Array<ShipModule>();
            for (int x = 0; x < Slots.Count; x++)
            {
                SlotStruct slot = Slots[x];
                if (slot?.Module == null) continue;
                modules.Add(slot.Module);
            }

            var role = Ship.GetDesignRole(modules.ToArray(), ActiveHull.Role, ActiveHull.Role, ActiveHull.ModuleSlots.Length, null);
            var designRoleRect = DesignRoleRect;
            SpriteFont roleFont = Fonts.Arial12;
            if (role != Role)
            {
                ShipData.CreateDesignRoleToolTip(role, roleFont, designRoleRect, true);
            }
            Role = role;
            CameraPosition.Z = OriginalZ / Camera.Zoom;
            Vector3 camPos = CameraPosition * new Vector3(-1f, 1f, 1f);
            View = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(180f.ToRadians())) 
                 * Matrix.CreateRotationX(0f.ToRadians())) 
                 * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

 


        public enum ActiveModuleState
        {
            Normal,
            Left,
            Right,
            Rear
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
        

    }
}