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
            ShipModule module = ResourceManager.GetModuleTemplate(ActiveModule.UID);
            ActiveModule.ApplyModuleOrientation(module.XSIZE, module.YSIZE, state);
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
                //RecalculatePower();
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

        private static int NumModules;
        private static int NumPowerChecks;
        private SlotStruct[] ModuleGrid;
        private int GridWidth;
        private int GridHeight;
        private Vector2 GridOffset;

        private void ConstructModuleGrid(Array<SlotStruct> slots)
        {
            Vector2 min = slots[0].Position;
            Vector2 max = min;
            foreach (SlotStruct slot in slots)
            {
                Vector2 pos  = slot.Position;
                Vector2 size = slot.ModuleSize;
                if (pos.X < min.X) min.X = pos.X;
                if (pos.Y < min.Y) min.Y = pos.Y;
                if (pos.X+size.X > max.X) max.X = pos.X+size.X;
                if (pos.Y+size.Y > max.Y) max.Y = pos.Y+size.Y;
            }

            float width  = max.X - min.X;
            float height = max.Y - min.Y;
            GridWidth  = (int)(width  / 16.0f);
            GridHeight = (int)(height / 16.0f);
            GridOffset = min;

            ModuleGrid = new SlotStruct[GridWidth * GridHeight];
            foreach (SlotStruct slot in slots)
            {
                Point pt = ToGridPos(slot.Position);
                ModuleGrid[pt.X + pt.Y * GridWidth] = slot;
            }
        }

        private Point ToGridPos(Vector2 modulePos)
        {
            Vector2 pos = modulePos - GridOffset;
            return new Point((int)(pos.X / 16.0f), (int)(pos.Y / 16.0f));
        }

        private bool GetUnpoweredConduit(SlotStruct conduit, int dx, int dy, out SlotStruct neighbour)
        {
            ++NumPowerChecks;
            neighbour = null;
            Point pos = ToGridPos(conduit.Position);
            pos.X += dx;
            pos.Y += dy;
            if (pos.X < 0 || pos.Y < 0 || pos.X >= GridWidth || pos.Y >= GridHeight)
                return false;

            neighbour = ModuleGrid[pos.X + pos.Y * GridWidth];
            return neighbour != null && !neighbour.CheckedConduits &&
                   neighbour.Module?.ModuleType == ShipModuleType.PowerConduit;
        }
        
        private void ConnectPowerConduits(SlotStruct firstConduit)
        {
            var open = new Array<SlotStruct>{ firstConduit };
            // floodfill through neighbouring conduits
            while (open.NotEmpty)
            {
                SlotStruct conduit = open.PopLast();
                conduit.Module.Powered  = true;
                conduit.CheckedConduits = true;
                if (GetUnpoweredConduit(conduit,  0, -1, out SlotStruct north)) open.Add(north);
                if (GetUnpoweredConduit(conduit,  0, +1, out SlotStruct south)) open.Add(south);
                if (GetUnpoweredConduit(conduit, -1,  0, out SlotStruct west))  open.Add(west);
                if (GetUnpoweredConduit(conduit, +1,  0, out SlotStruct east))  open.Add(east);
            }
        }


        private void RecalculatePower()
        {
            Stopwatch sw = Stopwatch.StartNew();
            NumModules = Slots.Count;
            NumPowerChecks = 0;

            // reset everything
            foreach (SlotStruct slot in Slots)
            {
                slot.Powered = false;
                slot.CheckedConduits = false;
                if (slot.Module != null)
                    slot.Module.Powered = false;
            }

            ConstructModuleGrid(Slots);

            // foreach powerplant tile
            foreach (SlotStruct powerPlant in Slots)
            {
                if (powerPlant.Module?.ModuleType        != ShipModuleType.PowerPlant &&
                    powerPlant.Parent?.Module.ModuleType != ShipModuleType.PowerPlant)
                    continue;

                // check for neighbouring power conduits
                if (GetUnpoweredConduit(powerPlant,  0, -1, out SlotStruct north)) ConnectPowerConduits(north);
                if (GetUnpoweredConduit(powerPlant,  0, +1, out SlotStruct south)) ConnectPowerConduits(south);
                if (GetUnpoweredConduit(powerPlant, -1,  0, out SlotStruct west))  ConnectPowerConduits(west);
                if (GetUnpoweredConduit(powerPlant, +1,  0, out SlotStruct east))  ConnectPowerConduits(east);
            }

            foreach (SlotStruct slotStruct1 in Slots)
            {
                if (slotStruct1.Module != null && slotStruct1.Module.PowerRadius > 0 && (slotStruct1.Module.ModuleType != ShipModuleType.PowerConduit || slotStruct1.Module.Powered))
                {
                    foreach (SlotStruct slotStruct2 in Slots)
                    {
                        ++NumPowerChecks;
                        if (Math.Abs(slotStruct1.PQ.X - slotStruct2.PQ.X) / 16 + Math.Abs(slotStruct1.PQ.Y - slotStruct2.PQ.Y) / 16 <= (int)slotStruct1.Module.PowerRadius)
                            slotStruct2.Powered = true;
                    }
                    if (slotStruct1.Module.XSIZE <= 1 && slotStruct1.Module.YSIZE <= 1)
                        continue;

                    for (int y = 0; y < slotStruct1.Module.YSIZE; ++y)
                    {
                        for (int x = 0; x < slotStruct1.Module.XSIZE; ++x)
                        {
                            if (x == 0 && y == 0)
                                continue;

                            foreach (SlotStruct slotStruct2 in Slots)
                            {
                                if (slotStruct2.PQ.Y == slotStruct1.PQ.Y + 16 * y && slotStruct2.PQ.X == slotStruct1.PQ.X + 16 * x)
                                {
                                    foreach (SlotStruct slotStruct3 in Slots)
                                    {
                                        ++NumPowerChecks;
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
                ++NumPowerChecks;
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

            double elapsed = sw.Elapsed.TotalMilliseconds;
            Log.Info($"RecalculatePower elapsed:{elapsed:G5}ms  modules:{NumModules}  totalchecks:{NumPowerChecks}");
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