using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game
{
    public sealed partial class ShipDesignScreen : GameScreen
    {
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
        private ScrollList<ShipHullListItem> HullSL;
        private WeaponScrollList WeaponSL;
        private Rectangle HullSelectionRect;
        private Submenu HullSelectionSub;
        private Rectangle BlackBar;

        public ShipModule HighlightedModule;
        private Vector2 CameraVelocity;
        private Vector2 StartDragPos;
        private ShipData Changeto;
        private string ScreenToLaunch;
        private float TransitionZoom = 1f;
        private SlotModOperation Operation;
        public ShipModule ActiveModule;
        private ModuleOrientation ActiveModState;
        private CategoryDropDown CategoryList;
        private ShieldBehaviorDropDown ShieldsBehaviorList;
        private HangarDesignationDropDown HangarOptionsList;

        private bool ShowAllArcs;
        public bool ToggleOverlay = true;
        private bool ShipSaved = true;
        private bool LowRes;
        public bool Debug;
        private ShipData.RoleName Role;
        private Rectangle DesignRoleRect;
        public bool IsSymmetricDesignMode = true;

        struct MirrorSlot
        {
            public SlotStruct Slot;
            public ModuleOrientation Orientation;
        }

        public ShipDesignScreen(GameScreen parent, EmpireUIOverlay empireUi) : base(parent)
        {
            Name = "ShipDesignScreen";
            EmpireUI = empireUi;
            TransitionOnTime = 2f;
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
            GameAudio.SmallServo();

            SpawnActiveModule(template, orientation, facing);
            HighlightedModule = null;
        }

        class SlotInstall
        {
            public readonly SlotStruct Slot;
            public readonly ShipModule Mod;
            public readonly ModuleOrientation Ori;
            bool CanInstall;
            public SlotInstall() {}
            public SlotInstall(SlotStruct slot, ShipModule mod, ModuleOrientation ori)
            {
                Slot = slot;
                Mod = mod;
                Ori = ori;
            }
            public bool UpdateCanInstallTo(DesignModuleGrid grid)
            {
                if (Slot == null)
                    return false;
                if (!grid.ModuleFitsAtSlot(Slot, Mod))
                {
                    PlayNegativeSound();
                    return false;
                }
                CanInstall = !Slot.IsSame(Mod, Ori, Mod.Facing);
                return CanInstall;
            }
            public void TryInstallTo(DesignModuleGrid designGrid)
            {
                if (CanInstall)
                    designGrid.InstallModule(Slot, Mod, Ori);
            }
        }

        SlotInstall CreateMirrorInstall(SlotInstall install)
        {
            if (IsSymmetricDesignMode &&
                GetMirrorSlot(install.Slot, install.Mod.XSIZE, install.Ori, out MirrorSlot mirrored))
            {
                ModuleOrientation mOri = mirrored.Orientation;
                float mFacing = ConvertOrientationToFacing(mOri);
                ShipModule mModule = CreateDesignModule(install.Mod, mOri, mFacing);
                return new SlotInstall(mirrored.Slot, mModule, mOri);
            }
            return new SlotInstall();
        }

        void InstallActiveModule(SlotInstall active)
        {
            SlotInstall mirror = CreateMirrorInstall(active);
            bool canInstall  = active.UpdateCanInstallTo(ModuleGrid);
                 canInstall |= mirror.UpdateCanInstallTo(ModuleGrid);
            if (canInstall)
            {
                ModuleGrid.StartUndoableAction();
                {
                    active.TryInstallTo(ModuleGrid);
                    mirror.TryInstallTo(ModuleGrid);
                }
                ModuleGrid.RecalculatePower();
                ShipSaved = false;
                SpawnActiveModule(active.Mod, active.Ori, active.Slot.Facing);
            }
        }

        void ReplaceModulesWith(SlotStruct slot, ShipModule template)
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

        void DeleteModuleAtSlot(SlotStruct slot)
        {
            if (slot.Module == null && slot.Parent == null)
                return;

            ModuleGrid.StartUndoableAction();

            if (IsSymmetricDesignMode)
            {
                if (GetMirrorSlotStruct(slot, out SlotStruct mirrored)
                    && mirrored.Root != slot.Root)
                {
                    ModuleGrid.ClearSlots(mirrored.Root, mirrored.Root.Module);
                }
            }
            ModuleGrid.ClearSlots(slot.Root, slot.Root.Module);
            ModuleGrid.RecalculatePower();
            GameAudio.SubBassWhoosh();
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


        public static void PlayNegativeSound() => GameAudio.NegativeClick();

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
                   * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), Vector3.Down);
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