using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.UI;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game
{
    public sealed partial class ShipDesignScreen : GameScreen
    {
        private Matrix View;
        private Matrix Projection;
        public Camera2D Camera;
        public Array<ToggleButton> CombatStatusButtons = new Array<ToggleButton>();
        public ShipData ActiveHull;
        public EmpireUIOverlay EmpireUI;
        //private Menu1 ModuleSelectionMenu;
        private SceneObject shipSO;
        private Vector3 CameraPosition = new Vector3(0f, 0f, 1300f);
        public Array<SlotStruct> Slots = new Array<SlotStruct>();
        private Vector2 Offset;
        private CombatState CombatState = CombatState.AttackRuns;
        private readonly Array<ShipData> AvailableHulls = new Array<ShipData>();
        private UIButton ToggleOverlayButton;
        private UIButton SaveButton;
        private UIButton LoadButton;
        public ModuleSelection ModSel;
        private Submenu StatsSub;
        private Menu1 ShipStats;
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
        private Vector2 CameraVelocity;
        private Vector2 StartDragPos;
        private ShipData Changeto;
        private string ScreenToLaunch;
        private ShipModule HoveredModule;
        private float TransitionZoom = 1f;
        private SlotModOperation Operation;
        public ShipModule ActiveModule;
        private ActiveModuleState ActiveModState;
        private Selector selector;
        private CategoryDropDown CategoryList;
        private Rectangle DropdownRect;
        private Vector2 ClassifCursor;
        public Stack<DesignAction> DesignStack = new Stack<DesignAction>();
        private string LastActiveUID           = ""; // Gretman - To Make the Ctrl-Z much more responsive
        private Vector2 LastDesignActionPos    = Vector2.Zero;
        private Vector2 CoBoxCursor;
        private UICheckBox CarrierOnlyBox;
        private bool ShowAllArcs;
        private bool Fml;
        private bool Fmlevenmore;
        public bool CarrierOnly;
        public bool ToggleOverlay = true;
        private bool ShipSaved = true;
        private bool LowRes;
        public bool Debug;
        private ShipData.Category LoadCategory;
        private ShipData.RoleName Role;
        private Rectangle DesignRoleRect;


#if SHIPYARD
        short TotalI, TotalO, TotalE, TotalIO, TotalIE, TotalOE, TotalIOE = 0;        //For Gretman's debug shipyard
#endif


        public ShipDesignScreen(GameScreen parent, EmpireUIOverlay empireUi) : base(parent)
        {
            EmpireUI         = empireUi;
            TransitionOnTime = TimeSpan.FromSeconds(2);
#if SHIPYARD
            Debug = true;
#endif
        }

        private void ChangeModuleState(ActiveModuleState state)
        {
            if (ActiveModule == null)
                return;
            ActiveModState = state;
            ShipModule template = ResourceManager.GetModuleTemplate(ActiveModule.UID);
            ActiveModule.ApplyModuleOrientation(template.XSIZE, template.YSIZE, state);
        }

        private bool FindStructFromOffset(SlotStruct offsetBase, int dx, int dy, out SlotStruct found)
        {
            found = null;
            if (dx == 0 && dy == 0)
                return false; // ignore self, {0,0} is offsetBase

            var pos = new Point(offsetBase.PQ.X + dx*16, offsetBase.PQ.Y + dy*16);
            return ModuleGrid.Get(pos, out found);
        }

        // @todo This is all broken. Redo everything.
        private void ClearDestinationSlots(SlotStruct slot, ShipModule newModule)
        {
            for (int y = 0; y < newModule.YSIZE; y++)
            {
                for (int x = 0; x < newModule.XSIZE; x++)
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
                        if (FindStructFromOffset(parent, x, y, out SlotStruct slot2))
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

        private static float GetMaintCostShipyard(ShipData ship, int size, Empire empire)
        {
            float maint = Ship.GetShipRoleMaintenance(ship.ShipRole, empire);

            if (ship.Role == ShipData.RoleName.freighter)
                maint *= Ship.GetFreighterSizeCostMultiplier(size);

            if (ship.Role == ShipData.RoleName.freighter || ship.Role == ShipData.RoleName.platform)
            {
                maint *= empire.data.CivMaintMod;
                maint *= empire.data.Privatization ? 0.5f : 1.0f;
            }

            // Subspace Projectors do not get any more modifiers
            if (ship.Name == "Subspace Projector")
                return maint;

            if (GlobalStats.ShipMaintenanceMulti > 1)
                maint *= GlobalStats.ShipMaintenanceMulti;
            return maint;
        }

        private static float GetMaintCostShipyardProportional(ShipData shipData, float fCost, Empire empire)
        {
            return fCost * Ship.GetMaintenanceModifier(shipData, empire);
        }

        private static string GetNumberString(float stat)
        {
            if (stat < 1000f)  return stat.ToString("#.#"); // 950.7
            if (stat < 10000f) return stat.ToString("#");   // 9500
            float single = stat / 1000f;
            if (single < 100f)  return single.ToString("#.##") + "k"; // 57.75k
            if (single < 1000f) return single.ToString("#.#") + "k";  // 950.7k
            return single.ToString("#") + "k"; // 1000k
        }

        private string GetConduitGraphic(SlotStruct ss)
        {
            var conduit = new Ship.ConduitGraphic();
            foreach (SlotStruct slot in Slots)
                if (slot.Module?.ModuleType == ShipModuleType.PowerConduit)
                    conduit.Add(slot.PQ.X - ss.PQ.X, slot.PQ.Y - ss.PQ.Y);
            return conduit.GetGraphic();
        }


        public bool SlotStructFits(SlotStruct slot, ShipModule activeModule, bool rotated = false)
        {
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

        public ShipModule CreateDesignModule(string uid, ActiveModuleState state)
        {
            ShipModule m = ShipModule.CreateNoParent(uid, EmpireManager.Player, ActiveHull);
            m.ApplyModuleOrientation(m.XSIZE, m.YSIZE, state);
            return m;
        }

        // spawn a new active module under cursor
        private void SpawnActiveModule(string uid, ActiveModuleState state)
        {
            ActiveModule = CreateDesignModule(uid, state);
            ActiveModState = state;
        }

        private void ResetActiveModule()
        {
            ActiveModule = null;
            ActiveModState = ActiveModuleState.Normal;
        }
        
        public void SetActiveModule(string uid, ActiveModuleState state)
        {
            GameAudio.PlaySfxAsync("smallservo");

            SpawnActiveModule(uid, state);
            ActiveModule.SetAttributesNoParent();

            foreach (SlotStruct s in Slots)                                    
                s.SetValidity(ActiveModule);
            
            HighlightedModule = null;
            HoveredModule     = null;
        }        
        
        private void InstallModule(SlotStruct slot, ShipModule newModule, ActiveModuleState newState)
        {
            if (!SlotStructFits(slot, newModule))
            {
                PlayNegativeSound();
                return;
            }

            DesignStack.Push(new DesignAction(slot));

            ClearSlot(slot);
            ClearDestinationSlots(slot, newModule);

            slot.ModuleUID            = newModule.UID;
            slot.Module               = newModule;
            slot.Module.XSIZE         = newModule.XSIZE;
            slot.Module.YSIZE         = newModule.YSIZE;
            slot.Module.XMLPosition   = newModule.XMLPosition;
            slot.State                = newState;
            slot.Module.hangarShipUID = newModule.hangarShipUID;
            slot.Module.Facing        = newModule.Facing;
            slot.Tex                  = newModule.ModuleTexture;
            slot.Module.SetAttributesNoParent();

            RecalculatePower();
            ShipSaved = false;

            SpawnActiveModule(newModule.UID, newState);
        }

        private void InstallModuleNoStack(SlotStruct slot, ShipModule newModule, ActiveModuleState newState)
        {
            if (!SlotStructFits(slot, newModule))
            {
                PlayNegativeSound();
                return;
            }

            ClearSlotNoStack(slot);
            ClearDestinationSlots(slot, newModule);

            slot.ModuleUID            = newModule.UID;
            slot.Module               = newModule;
            slot.State                = newState;
            slot.Module.hangarShipUID = newModule.hangarShipUID;
            slot.Module.Facing        = newModule.Facing;
            slot.Tex                  = newModule.ModuleTexture;
            slot.Module.SetAttributesNoParent();

            RecalculatePower();
            ShipSaved = false;

            SpawnActiveModule(newModule.UID, newState);
        }

        private void InstallModuleFromLoad(SlotStruct slot, ShipModule newModule)
        {
            if (SlotStructFits(slot, newModule))
            {
                ActiveModuleState activeModuleState = slot.State;
                ClearSlot(slot);
                ClearDestinationSlots(slot, newModule);
                slot.ModuleUID     = newModule.UID;
                slot.Module        = newModule; 
                slot.State         = activeModuleState;
                slot.Module.Facing = slot.Facing;
                slot.Tex           = newModule.ModuleTexture;
                slot.Module.SetAttributesNoParent();
            }
            else PlayNegativeSound();
        }

        private void SetupSlots()
        {
            Slots.Clear();
            foreach (ModuleSlotData slot in ActiveHull.ModuleSlots)
                Slots.Add(new SlotStruct(slot, Offset));

            foreach (SlotStruct slot in Slots)
            {
                if (slot.ModuleUID == null)
                    continue;

                ShipModule newModule = CreateDesignModule(slot.ModuleUID, slot.State);
                InstallModuleFromLoad(slot, newModule);

                if (slot.Module?.ModuleType == ShipModuleType.Hangar)
                    slot.Module.hangarShipUID = slot.SlotOptions;
            }

            ModuleGrid = new DesignModuleGrid(Slots);
            RecalculatePower();
            ResetActiveModule();
        }

        public void PlayNegativeSound() => GameAudio.PlaySfxAsync("UI_Misc20");

        private DesignModuleGrid ModuleGrid;
        private void RecalculatePower() => ModuleGrid.RecalculatePower();

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            Camera.Zoom = MathHelper.SmoothStep(Camera.Zoom, TransitionZoom, 0.2f);
            if (Camera.Zoom < 0.3f)  Camera.Zoom = 0.3f;
            if (Camera.Zoom > 2.65f) Camera.Zoom = 2.65f;

            var modules = new Array<ShipModule>();
            for (int x = 0; x < Slots.Count; x++)
            {
                SlotStruct slot = Slots[x];
                if (slot?.Module == null) continue;
                modules.Add(slot.Module);
            }

            var role = Ship.GetDesignRole(modules.ToArray(), ActiveHull.Role, ActiveHull.Role, ActiveHull.ModuleSlots.Length, null);
            if (role != Role)
            {
                ShipData.CreateDesignRoleToolTip(role, Fonts.Arial12, DesignRoleRect, true);
                Role = role;
            }
            CameraPosition.Z = OriginalZ / Camera.Zoom;
            Vector3 camPos = CameraPosition * new Vector3(-1f, 1f, 1f);
            View = Matrix.CreateRotationY(180f.ToRadians())
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