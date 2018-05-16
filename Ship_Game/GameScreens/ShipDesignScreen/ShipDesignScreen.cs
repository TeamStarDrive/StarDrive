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
        private Point GridOffset;

        // this constructs a [GridWidth][GridHeight] array of current hull
        // and allows for quick lookup for neighbours
        private void ConstructModuleGrid(Array<SlotStruct> slots)
        {
            Point min = slots[0].Position;
            Point max = min;
            foreach (SlotStruct slot in slots)
            {
                Point pos  = slot.Position;
                Point size = slot.ModuleSize;
                if (pos.X < min.X) min.X = pos.X;
                if (pos.Y < min.Y) min.Y = pos.Y;
                if (pos.X+size.X > max.X) max.X = pos.X+size.X;
                if (pos.Y+size.Y > max.Y) max.Y = pos.Y+size.Y;
            }

            float width  = max.X - min.X;
            float height = max.Y - min.Y;
            GridWidth  = (int)(width  / 16f);
            GridHeight = (int)(height / 16f);
            GridOffset = min;

            ModuleGrid = new SlotStruct[GridWidth * GridHeight];
            foreach (SlotStruct slot in slots)
            {
                Point pt = ToGridPos(slot.Position);
                ModuleGrid[pt.X + pt.Y * GridWidth] = slot;
            }
        }

        private Point ToGridPos(Point modulePos) => new Point((modulePos.X - GridOffset.X) / 16,
                                                              (modulePos.Y - GridOffset.Y) / 16);

        private void ClampGridCoords(ref int x0, ref int x1, ref int y0, ref int y1)
        {
            x0 = Math.Max(0, x0);
            y0 = Math.Max(0, y0);
            x1 = Math.Min(x1, GridWidth  - 1);
            y1 = Math.Min(y1, GridHeight - 1);
        }

        private void SetInPowerRadius(int x0, int x1, int y0, int y1)
        {
            ClampGridCoords(ref x0, ref x1, ref y0, ref y1);
            for (int y = y0; y <= y1; ++y)
            for (int x = x0; x <= x1; ++x)
            {
                ++NumPowerChecks;
                SlotStruct m = ModuleGrid[x + y*GridWidth];
                if (m != null) m.InPowerRadius = true;
            }
        }

        private void SetInPowerRadius(int x0, int x1, int y0, int y1, int powerX, int powerY, int radius)
        {
            ClampGridCoords(ref x0, ref x1, ref y0, ref y1);
            for (int y = y0; y <= y1; ++y)
            for (int x = x0; x <= x1; ++x)
            {
                ++NumPowerChecks;
                int dx = Math.Abs(x - powerX);
                int dy = Math.Abs(y - powerY);
                if ((dx + dy) > radius) continue;
                SlotStruct m = ModuleGrid[x + y*GridWidth];
                if (m != null) m.InPowerRadius = true;
            }
        }

        private void ModuleCoords(SlotStruct m, out int x0, out int x1, out int y0, out int y1)
        {
            x0 = (m.PQ.X - GridOffset.X)/16;
            y0 = (m.PQ.Y - GridOffset.Y)/16;
            x1 = x0 + m.Module.XSIZE - 1;
            y1 = y0 + m.Module.YSIZE - 1; 
        }

        // set all modules in power range as InPowerRadius
        private void DistributePowerFrom(SlotStruct source)
        {
            source.PowerChecked   = true;
            source.InPowerRadius  = true;
            source.Module.Powered = true;
            int radius = source.Module.PowerRadius;

            ModuleCoords(source, out int x0, out int x1, out int y0, out int y1);

            SetInPowerRadius(x0, x1, y0-radius, y0-1); // Check North
            SetInPowerRadius(x0, x1, y1+1, y1+radius); // Check South
            SetInPowerRadius(x0-radius, x0-1, y0, y1); // Check West
            SetInPowerRadius(x1+1, x1+radius, y0, y1); // Check East

            SetInPowerRadius(x0-radius, x0-1, y0-radius, y0-1, x0, y0, radius); // Check NorthWest
            SetInPowerRadius(x1+1, x1+radius, y0-radius, y0-1, x1, y0, radius); // Check NorthEast
            SetInPowerRadius(x1+1, x1+radius, y1+1, y0+radius, x1, y1, radius); // Check SouthEast
            SetInPowerRadius(x0-radius, x0-1, y0-1, y0+radius, x0, y1, radius); // Check SouthWest
        }

        private void GetNeighbouringConduits(int x0, int x1, int y0, int y1, Array<SlotStruct> open)
        {
            ClampGridCoords(ref x0, ref x1, ref y0, ref y1);
            for (int y = y0; y <= y1; ++y)
            for (int x = x0; x <= x1; ++x)
            {
                ++NumPowerChecks;
                SlotStruct m = ModuleGrid[x + y * GridWidth];
                if (m != null && !m.PowerChecked && m.Module?.ModuleType == ShipModuleType.PowerConduit)
                    open.Add(m);
            }
        }
        
        private void GetNeighbouringConduits(SlotStruct source, Array<SlotStruct> open)
        {
            ModuleCoords(source, out int x0, out int x1, out int y0, out int y1);

            GetNeighbouringConduits(x0, x1, y0-1, y0-1, open); // Check North;
            GetNeighbouringConduits(x0, x1, y1+1, y1+1, open); // Check South;
            GetNeighbouringConduits(x0-1, x0-1, y0, y1, open); // Check West;
            GetNeighbouringConduits(x1+1, x1+1, y0, y1, open); // Check East;
        }

        private void ConnectPowerConduits(SlotStruct powerPlant)
        {
            var open = new Array<SlotStruct>();
            GetNeighbouringConduits(powerPlant, open);

            while (open.NotEmpty) // floodfill through unpowered neighbouring conduits
            {
                SlotStruct conduit = open.PopLast();
                if (conduit.PowerChecked)
                    continue;
                DistributePowerFrom(conduit);
                GetNeighbouringConduits(conduit, open);
            }
        }

        private void RecalculatePower()
        {
            Stopwatch sw = Stopwatch.StartNew();
            NumModules = Slots.Count;
            NumPowerChecks = 0;

            foreach (SlotStruct slot in Slots) // reset everything
            {
                slot.InPowerRadius    = false;
                slot.PowerChecked = false;
                if (slot.Module != null) slot.Module.Powered = false;
            }

            ConstructModuleGrid(Slots);

            foreach (SlotStruct slot in Slots)
            {
                SlotStruct powerSource = slot.Module != null ? slot : slot.Parent;
                if (powerSource?.PowerChecked != false)
                    continue;

                ShipModule module = powerSource.Module;
                if (module == null || module.PowerRadius <= 0 || module.ModuleType == ShipModuleType.PowerConduit)
                    continue;

                DistributePowerFrom(powerSource);

                // only PowerPlants can power conduits
                if (module.ModuleType == ShipModuleType.PowerPlant)
                    ConnectPowerConduits(powerSource);
            }

            foreach (SlotStruct slot in Slots)
            {
                if (slot.InPowerRadius)
                {
                    // apply power to modules, except for conduits which require direct connection
                    if (slot.Module != null && slot.Module.ModuleType != ShipModuleType.PowerConduit)
                        slot.Module.Powered = true;
                    if (slot.Parent?.Module != null)
                        slot.Parent.Module.Powered = true;                    
                }
                else if (slot.Module != null && (slot.Module.AlwaysPowered || slot.Module.PowerDraw <= 0))
                {
                    slot.Module.Powered = true;
                }
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