using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
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
        private readonly Array<ShipData> AvailableHulls = new Array<ShipData>();
        private UIButton BtnSymmetricDesign; // Symmetric Module Placement Feature by Fat Bastard
        public ModuleSelection ModSel;
        private Submenu StatsSub;
        private Menu1 ShipStats;
        private GenericButton ArcsButton;
        private float OriginalZ;
        private Rectangle SearchBar;
        private Rectangle BottomSep;
        private ScrollList HullSL;
        private WeaponScrollList WeaponSL;
        private Rectangle HullSelectionRect;
        private Submenu HullSelectionSub;
        private Rectangle BlackBar;

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
        private ShieldBehaviorDropDown ShieldsBehaviorList;

        private bool ShowAllArcs;
        public bool ToggleOverlay = true;
        private bool ShipSaved = true;
        private bool LowRes;
        public bool Debug;
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
            public Vector2 Center => Slot.Center();
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

        private static float GetMaintCostShipyardProportional(ShipData shipData, float fCost, Empire empire)
        {
            return fCost * Ship.GetMaintenanceModifier(shipData, empire);
        }

        public ShipModule CreateDesignModule(ShipModule template)
        {
            ShipModule m = ShipModule.CreateNoParent(template, EmpireManager.Player, ActiveHull);
            m.SetAttributes();                    
            return m;
        }

        public ShipModule CreateDesignModule(ShipModule template, ModuleOrientation orientation, float facing)
        {
            ShipModule m = ShipModule.CreateNoParent(ResourceManager.GetModuleTemplate(template.UID),
                                                     EmpireManager.Player, ActiveHull);
            m.SetModuleFacing(m.XSIZE, m.YSIZE, orientation, facing);
            m.hangarShipUID = m.IsTroopBay ? Ship.GetAssaultShuttleName(EmpireManager.Player) : template.hangarShipUID;
            return m;
        }

        public ShipModule CreateDesignModule(string uid, ModuleOrientation orientation, float facing)
        {
            return CreateDesignModule(ResourceManager.GetModuleTemplate(uid), orientation, facing);
        }

        // spawn a new active module under cursor
        private void SpawnActiveModule(ShipModule template, ModuleOrientation orientation, float facing)
        {
            ActiveModule = CreateDesignModule(template, orientation, facing);
            ActiveModState = orientation;
            ActiveModule.SetAttributes();
            if (ActiveModule.ModuleType == ShipModuleType.Hangar
                && !ActiveModule.IsSupplyBay && !ActiveModule.IsTroopBay)
                ActiveModule.hangarShipUID = DynamicHangarOptions.DynamicLaunch.ToString();
        }

        private void ResetActiveModule()
        {
            ActiveModule = null;
            ActiveModState = ModuleOrientation.Normal;
        }
        
        public void SetActiveModule(ShipModule template, ModuleOrientation orientation, float facing)
        {
            GameAudio.PlaySfxAsync("smallservo");

            SpawnActiveModule(template, orientation, facing);

            HighlightedModule = null;
            HoveredModule     = null;
        }

        private void InstallModule(SlotStruct slot, ShipModule module, ModuleOrientation orientation)
        {
            if (!ModuleGrid.ModuleFitsAtSlot(slot, module))
            {
                PlayNegativeSound();
                return;
            }

            ModuleGrid.StartUndoableAction();

            bool same = slot.IsSame(module, orientation, module.Facing);

            if (IsSymmetricDesignMode)
            {
                MirrorSlot mirrored = GetMirrorSlot(slot, module.XSIZE, orientation);
                if (IsMirrorSlotPresent(mirrored, slot))
                {
                    if (!ModuleGrid.ModuleFitsAtSlot(mirrored.Slot, module))
                    {
                        PlayNegativeSound();
                        return;
                    }

                    float mirroredFacing = ConvertOrientationToFacing(mirrored.Orientation);
                    ShipModule mirroredModule = CreateDesignModule(module, mirrored.Orientation, mirroredFacing);
                    
                    if (same && mirrored.Slot.IsSame(mirroredModule, mirrored.Orientation, mirroredFacing))
                        return; // both same
                    ModuleGrid.InstallModule(mirrored.Slot, mirroredModule, mirrored.Orientation);
                }
            }
            else if (same)
                return;

            ModuleGrid.InstallModule(slot, module, orientation);
            ModuleGrid.RecalculatePower();
            ShipSaved = false;
            SpawnActiveModule(module, orientation, slot.Facing);
        }

        private void ReplaceModulesWith(SlotStruct slot, ShipModule template)
        {
            if (!slot.IsModuleReplaceableWith(template))
            {
                PlayNegativeSound();
                return;
            }

            ModuleGrid.StartUndoableAction();

            string replacementId = slot.Module.UID;
            foreach (SlotStruct replaceAt in ModuleGrid.SlotsList)
            {
                if (replaceAt.ModuleUID == replacementId)
                {
                    ShipModule m = CreateDesignModule(template, replaceAt.Orientation, replaceAt.Module.Facing);
                    ModuleGrid.InstallModule(replaceAt, m, replaceAt.Orientation);
                }
            }
            ModuleGrid.RecalculatePower();
            ShipSaved = false;
        }

        private void DeleteModuleAtSlot(SlotStruct slot)
        {
            if (slot.Module == null && slot.Parent == null)
                return;

            ModuleGrid.StartUndoableAction();

            if (IsSymmetricDesignMode)
            {
                MirrorSlot mirrored = GetMirrorSlot(slot.Root, slot.Root.Module.XSIZE, slot.Root.Orientation);
                if (IsMirrorSlotPresent(mirrored, slot) 
                    && mirrored.Slot.Root != slot.Root 
                    && IsMirrorSlotValid(slot.Root, mirrored))
                {
                    ModuleGrid.ClearSlots(mirrored.Slot.Root, mirrored.Slot.Root.Module);
                }
            }
            ModuleGrid.ClearSlots(slot.Root, slot.Root.Module);
            ModuleGrid.RecalculatePower();
            GameAudio.PlaySfxAsync("sub_bass_whoosh");
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
                if (!ModuleGrid.ModuleFitsAtSlot(slot, newModule, logFailure: true))
                    continue;

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

            var roleData = new RoleData(ActiveHull, ModuleGrid.Modules);
            Role         = roleData.DesignRole;
            //roleData.CreateDesignRoleToolTip(DesignRoleRect); FB: This was killing tool tips in ship design, disabled and should check this
            
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