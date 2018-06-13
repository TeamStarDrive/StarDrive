using System;
using Microsoft.Xna.Framework;
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
        private Vector2 Offset;
        private CombatState CombatState = CombatState.AttackRuns;
        private readonly Array<ShipData> AvailableHulls = new Array<ShipData>();
        private UIButton ToggleOverlayButton;
        private UIButton SymmetricDesignButton; // Symmetric Module Placement Feature Created by Fat Bastard
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
        private ModuleOrientation ActiveModState;
        private Selector selector;
        private CategoryDropDown CategoryList;
        private Rectangle DropdownRect;
        private Vector2 ClassifCursor;
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
        public bool IsSymmetricDesignMode = true;


    #if SHIPYARD
        short TotalI, TotalO, TotalE, TotalIO, TotalIE, TotalOE, TotalIOE = 0; //For Gretman's debug shipyard
    #endif

        private struct MirrorSlot
        {
            public SlotStruct Slot;
            public ModuleOrientation Orientation;
        }

        public ShipDesignScreen(GameScreen parent, EmpireUIOverlay empireUi) : base(parent)
        {
            EmpireUI         = empireUi;
            TransitionOnTime = TimeSpan.FromSeconds(2);
        #if SHIPYARD
            Debug = true;
        #endif
        }

        private void ReorientActiveModule(ModuleOrientation orientation)
        {
            if (ActiveModule == null)
                return;
            ActiveModState = orientation;
            ShipModule template = ResourceManager.GetModuleTemplate(ActiveModule.UID);
            ActiveModule.SetModuleFacing(template.XSIZE, template.YSIZE, 
                                         orientation, ShipModule.DefaultFacingFor(orientation));
        }

        private bool FindStructFromOffset(SlotStruct offsetBase, int dx, int dy, out SlotStruct found)
        {
            found = null;
            if (dx == 0 && dy == 0)
                return false; // ignore self, {0,0} is offsetBase

            var pos = new Point(offsetBase.PQ.X + dx*16, offsetBase.PQ.Y + dy*16);
            return ModuleGrid.Get(pos, out found);
        }

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

        public ShipModule CreateDesignModule(string uid)
        {
            ShipModule m = ShipModule.CreateNoParent(uid, EmpireManager.Player, ActiveHull);
            m.SetAttributes();                    
            return m;
        }

        public ShipModule CreateDesignModule(string uid, ModuleOrientation orientation, float facing)
        {
            ShipModule m = ShipModule.CreateNoParent(uid, EmpireManager.Player, ActiveHull);
            m.SetModuleFacing(m.XSIZE, m.YSIZE, orientation, facing);
            return m;
        }

        // spawn a new active module under cursor
        private void SpawnActiveModule(string uid, ModuleOrientation orientation, float facing)
        {
            ActiveModule = CreateDesignModule(uid, orientation, facing);
            ActiveModState = orientation;
            ActiveModule.SetAttributes();
        }

        private void ResetActiveModule()
        {
            ActiveModule = null;
            ActiveModState = ModuleOrientation.Normal;
        }
        
        public void SetActiveModule(string uid, ModuleOrientation orientation, float facing)
        {
            GameAudio.PlaySfxAsync("smallservo");

            SpawnActiveModule(uid, orientation, facing);

            HighlightedModule = null;
            HoveredModule     = null;
        }

        private ShipModule CreateMirrorModule(MirrorSlot mirrored, ShipModule module)
        {
            ShipModule m = CreateDesignModule(module.UID, mirrored.Orientation, ConvertOrientationToFacing(mirrored.Orientation));
            m.hangarShipUID = module.hangarShipUID;
            return m;
        }

        private void InstallModule(SlotStruct slot, ShipModule module, ModuleOrientation orientation)
        {
            if (IsSymmetricDesignMode)
            {
                MirrorSlot mirrored = GetMirrorSlot(slot, module.XSIZE, orientation);
                if (IsMirrorSlotPresent(mirrored, slot))
                {
                    if (!ModuleGrid.ModuleFitsAtSlot(slot, module) || !ModuleGrid.ModuleFitsAtSlot(mirrored.Slot, module))
                    {
                        PlayNegativeSound();
                        return;
                    }
                    ShipModule mirroredModule = CreateMirrorModule(mirrored, module);
                    ModuleGrid.ClearSlots(mirrored.Slot, module.XSIZE, module.YSIZE);
                    ModuleGrid.InstallModule(mirrored.Slot, mirroredModule, mirrored.Orientation);
                }
            }
            if (!ModuleGrid.ModuleFitsAtSlot(slot, module))
            {
                PlayNegativeSound();
                return;
            }
            GameAudio.PlaySfxAsync("sub_bass_mouseover");
            ModuleGrid.ClearSlots(slot, module.XSIZE, module.YSIZE);
            ModuleGrid.InstallModule(slot, module, orientation);
            ModuleGrid.RecalculatePower();
            ShipSaved = false;
            SpawnActiveModule(module.UID, orientation, slot.Facing);
        }

        private void HandleBulkModuleReplacement(SlotStruct slot, ShipModule module, ModuleOrientation orientation)
        {
            Log.Info("bulkpossible? " + IsBulkModuleReplacementPossible(slot, module, orientation));
            if (!IsBulkModuleReplacementPossible(slot, module, orientation))
            {
                PlayNegativeSound();
                return;
            }
            DoBulkModuleReplacement(slot.Module, module);
        }

        private bool IsBulkModuleReplacementPossible(SlotStruct slot, ShipModule module, ModuleOrientation orientation)
        {
            if (slot == null || slot.ModuleUID == null)
                return false;
            if (slot.Module.XSIZE != module.XSIZE || slot.Module.YSIZE != module.YSIZE || slot.Module.Restrictions != module.Restrictions)
                return false;
            return true;
        }

        private void DoBulkModuleReplacement(ShipModule oldModule, ShipModule templateModule)
        {
            foreach (SlotStruct slot in ModuleGrid.SlotsList)
            {
                if (slot.ModuleUID == oldModule.UID)
                    ReplaceModule(slot, oldModule, templateModule);
            }
            ModuleGrid.RecalculatePower();
            ShipSaved = false;
        }

        private void ReplaceModule(SlotStruct oldSlot, ShipModule oldModule, ShipModule templateModule)
        {
            ShipModule newModule = CreateDesignModule(templateModule.UID, oldSlot.Orientation, oldSlot.Module.Facing);
            ModuleGrid.ClearSlots(oldSlot, oldModule.XSIZE, oldModule.YSIZE);
            ModuleGrid.InstallModule(oldSlot, newModule, oldSlot.Orientation);
        }

        private DesignModuleGrid ModuleGrid;

        private void SetupSlots()
        {
            ModuleGrid = new DesignModuleGrid(ActiveHull.ModuleSlots, Offset);

            foreach (SlotStruct slot in ModuleGrid.SlotsList)
            {
                string uid = slot.ModuleUID;
                if (uid == null || uid == "Dummy") // @note Backwards savegame compatibility for ship designs, dummy modules are deprecated
                    continue;

                ShipModule newModule = CreateDesignModule(slot.ModuleUID, slot.Orientation, slot.Facing);
                if (!ModuleGrid.ModuleFitsAtSlot(slot, newModule))
                {
                    Log.Warning($"InstallModuleFromLoad failed! {newModule}");
                    continue;
                }

                ModuleGrid.InstallModule(slot, newModule, slot.Orientation);

                if (slot.Module?.ModuleType == ShipModuleType.Hangar)
                    slot.Module.hangarShipUID = slot.SlotOptions;
            }

            ModuleGrid.RecalculatePower();
            ResetActiveModule();
        }

        public bool IsBadModuleSize(ShipModule module)
        {
            foreach (SlotStruct slot in ModuleGrid.SlotsList)
                if (ModuleGrid.ModuleFitsAtSlot(slot, module))
                    return false;
            return true;
        }


        public void PlayNegativeSound() => GameAudio.PlaySfxAsync("UI_Misc20");


        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            Camera.Zoom = MathHelper.SmoothStep(Camera.Zoom, TransitionZoom, 0.2f);
            if (Camera.Zoom < 0.3f)  Camera.Zoom = 0.3f;
            if (Camera.Zoom > 2.65f) Camera.Zoom = 2.65f;

            var modules = new Array<ShipModule>();
            foreach (SlotStruct slot in ModuleGrid.SlotsList)
            {
                if (slot.Module != null)
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

    public enum ModuleOrientation
    {
        Normal, Left, Right, Rear
    }
}