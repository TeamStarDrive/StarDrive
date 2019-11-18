using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.ShipDesignScreen;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game
{
    public enum ModuleOrientation
    {
        Normal, Left, Right, Rear
    }

    public sealed partial class ShipDesignScreen : GameScreen
    {
        public Camera2D Camera;
        public Array<ToggleButton> CombatStatusButtons = new Array<ToggleButton>();
        public ShipData ActiveHull;
        public EmpireUIOverlay EmpireUI;
        SceneObject shipSO;
        Vector3 CameraPosition = new Vector3(0f, 0f, 1300f);
        Vector2 Offset;
        readonly Array<ShipData> AvailableHulls = new Array<ShipData>();
        UIButton BtnSymmetricDesign; // Symmetric Module Placement Feature by Fat Bastard
        Submenu StatsSub;
        Menu1 ShipStats;
        GenericButton ArcsButton;
        float OriginalZ;
        Rectangle SearchBar;
        Rectangle BottomSep;
        Rectangle BlackBar;

        public ModuleSelection ModulesList;
        ScrollList<ShipHullListItem> HullSelectList;

        public ShipModule HighlightedModule;
        Vector2 CameraVelocity;
        Vector2 StartDragPos;
        ShipData Changeto;
        string ScreenToLaunch;
        float TransitionZoom = 1f;
        SlotModOperation Operation;
        public ShipModule ActiveModule;
        ModuleOrientation ActiveModState;
        CategoryDropDown CategoryList;
        ShieldBehaviorDropDown ShieldsBehaviorList;
        HangarDesignationDropDown HangarOptionsList;

        bool ShowAllArcs;
        public bool ToggleOverlay = true;
        bool ShipSaved = true;
        public bool Debug;
        ShipData.RoleName Role;
        Rectangle DesignRoleRect;
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

        void ReorientActiveModule(ModuleOrientation orientation)
        {
            if (ActiveModule == null)
                return;
            ActiveModState = orientation;
            ShipModule template = ResourceManager.GetModuleTemplate(ActiveModule.UID);
            ActiveModule.SetModuleFacing(template.XSIZE, template.YSIZE, 
                                         orientation, ShipModule.DefaultFacingFor(orientation));
        }

        ModuleSlotData FindModuleSlotAtPos(Vector2 slotPos)
        {
            ModuleSlotData[] slots = ActiveHull.ModuleSlots;
            for (int i = 0; i < slots.Length; ++i)
                if (slots[i].Position == slotPos)
                    return slots[i];
            return null;
        }

        void DebugAlterSlot(Vector2 slotPos, SlotModOperation op)
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

        static float GetMaintCostShipyardProportional(ShipData shipData, float fCost, Empire empire)
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
        void SpawnActiveModule(ShipModule template, ModuleOrientation orientation, float facing)
        {
            ActiveModule = CreateDesignModule(template, orientation, facing);
            ActiveModState = orientation;
            ActiveModule.SetAttributes();
            if (ActiveModule.ModuleType == ShipModuleType.Hangar
                && !ActiveModule.IsSupplyBay && !ActiveModule.IsTroopBay)
                ActiveModule.hangarShipUID = DynamicHangarOptions.DynamicLaunch.ToString();
        }

        void ResetActiveModule()
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

        DesignModuleGrid ModuleGrid;

        void SetupSlots()
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

        enum SlotModOperation
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

        public override void LoadContent()
        {
            Log.Info("ShipDesignScreen.LoadContent");
            RemoveAll();
            ModulesList = Add(new ModuleSelection(this, new Rectangle(5, (LowRes ? 45 : 100), 305, (LowRes ? 350 : 490))));

            var hulls = EmpireManager.Player.GetHDict();
            foreach (KeyValuePair<string, bool> hull in hulls)
                if (hull.Value && ResourceManager.Hull(hull.Key, out ShipData hullData))
                    AvailableHulls.Add(hullData);

            PrimitiveQuad.Device = ScreenManager.GraphicsDevice;
            Offset = new Vector2(Viewport.Width / 2 - 256, Viewport.Height / 2 - 256);
            Camera = new Camera2D { Pos = new Vector2(Viewport.Width / 2f, Viewport.Height / 2f) };
            Vector3 camPos = CameraPosition * new Vector3(-1f, 1f, 1f);
            View = Matrix.CreateRotationY(180f.ToRadians())
                   * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), Vector3.Down);

            float aspectRatio = (float)Viewport.Width / Viewport.Height;
            Projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspectRatio, 1f, 20000f);
            
            ChangeHull(AvailableHulls[0]);

            float lowestX  = ActiveHull.ModuleSlots[0].Position.X;
            float highestX = lowestX;
            foreach (ModuleSlotData slot in ActiveHull.ModuleSlots)
            {
                if (slot.Position.X < lowestX)  lowestX  = slot.Position.X;
                if (slot.Position.X > highestX) highestX = slot.Position.X;
            }

            // FB: added the *2 below since vulfar ships were acting strangly without it (too small vs modulegrid). 
            // Maybe because they are long and narrow. This code is an enigma.
            float hullWidth = (highestX - lowestX) * 2;

            // So, this attempts to zoom so the entire design is visible
            float UpdateCameraMatrix()
            {
                camPos = CameraPosition * new Vector3(-1f, 1f, 1f);

                View = Matrix.CreateRotationY(180f.ToRadians())
                       * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), Vector3.Down);

                Vector3 center   = Viewport.Project(Vector3.Zero, Projection, View, Matrix.Identity);
                Vector3 hullEdge = Viewport.Project(new Vector3(hullWidth, 0, 0), Projection, View, Matrix.Identity);
                return center.Distance(hullEdge) + 10f;
            }

            float visibleHullWidth = UpdateCameraMatrix();
            if (visibleHullWidth >= hullWidth)
            {
                while (visibleHullWidth > hullWidth)
                {
                    CameraPosition.Z += 10f;
                    visibleHullWidth = UpdateCameraMatrix();
                }
            }
            else
            {
                while (visibleHullWidth < hullWidth)
                {
                    CameraPosition.Z -= 10f;
                    visibleHullWidth = UpdateCameraMatrix();
                }
            }

            BlackBar = new Rectangle(0, ScreenHeight - 70, 3000, 70);
      
            ClassifCursor = new Vector2(ScreenWidth * .5f,
                ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px").Height + 10);

            float ordersBarX = ClassifCursor.X - 15;
            var ordersBarPos = new Vector2(ordersBarX, ClassifCursor.Y + 20);
            void AddCombatStatusBtn(CombatState state, string iconPath, ToolTipText toolTip)
            {
                var button = new ToggleButton(ordersBarPos, ToggleButtonStyle.Formation, iconPath)
                {
                    CombatState = state,
                    Tooltip = toolTip
                };
                button.OnClick = (b) => OnCombatButtonPressed(state);
                Add(button);
                CombatStatusButtons.Add(button);
                ordersBarPos.X += 29f;
            }

            AddCombatStatusBtn(CombatState.AttackRuns,   "SelectionBox/icon_formation_headon", toolTip: 1);
            AddCombatStatusBtn(CombatState.Artillery,    "SelectionBox/icon_formation_aft",    toolTip: 2);
            AddCombatStatusBtn(CombatState.ShortRange,   "SelectionBox/icon_grid",             toolTip: 228);
            AddCombatStatusBtn(CombatState.HoldPosition, "SelectionBox/icon_formation_x",      toolTip: 65);
            AddCombatStatusBtn(CombatState.OrbitLeft,    "SelectionBox/icon_formation_left",   toolTip: 3);
            AddCombatStatusBtn(CombatState.OrbitRight,   "SelectionBox/icon_formation_right",  toolTip: 4);
            AddCombatStatusBtn(CombatState.Evade,        "SelectionBox/icon_formation_stop",   toolTip: 6);
            ordersBarPos = new Vector2(ordersBarX + 4*29f, ordersBarPos.Y + 29f);
            AddCombatStatusBtn(CombatState.BroadsideLeft,  "SelectionBox/icon_formation_bleft", 159);
            AddCombatStatusBtn(CombatState.BroadsideRight, "SelectionBox/icon_formation_bright", 160);
            
            UIList bottomList = AddList(new Vector2(ScreenWidth - 250f, ScreenHeight - 50f));
            bottomList.LayoutStyle = ListLayoutStyle.Resize;
            bottomList.Direction = new Vector2(-1, 0);
            bottomList.Padding = new Vector2(16f, 2f);
            bottomList.Add(ButtonStyle.Medium, 105, click: b =>
            {
                if (!CheckDesign()) {
                    GameAudio.NegativeClick();
                    ScreenManager.AddScreen(new MessageBoxScreen(this, Localizer.Token(2049)));
                    return;
                }
                ScreenManager.AddScreen(new DesignManager(this, ActiveHull.Name));
            });
            bottomList.Add(ButtonStyle.Medium, 8, click: b =>
            {
                ScreenManager.AddScreen(new LoadDesigns(this));
            });
            bottomList.Add(ButtonStyle.Medium, 106, click: b =>
            {
                ToggleOverlay = !ToggleOverlay;
            }).ClickSfx = "blip_click";
            BtnSymmetricDesign = bottomList.Add(ButtonStyle.Medium, 1985, click: b =>
            {
                OnSymmetricDesignToggle();
            });
            BtnSymmetricDesign.ClickSfx = "blip_click";
            BtnSymmetricDesign.Tooltip  = Localizer.Token(1984);
            BtnSymmetricDesign.Style    = ButtonStyle.Military;

            SearchBar = new Rectangle((int)ScreenCenter.X, (int)bottomList.Y, 210, 25);
            LoadContentFinish();
            BindListsToActiveHull();

            AssignLightRig("example/ShipyardLightrig");
        }

        void LoadContentFinish()
        {
            BottomSep = new Rectangle(BlackBar.X, BlackBar.Y, BlackBar.Width, 1);

            var hullSelectionBkg = new Submenu(ScreenWidth - 285, (LowRes ? 45 : 100), 280, (LowRes ? 350 : 400));
            // rounded black background
            hullSelectionBkg.Background = new Selector(hullSelectionBkg.Rect.CutTop(25), new Color(0,0,0,210));
            hullSelectionBkg.AddTab(Localizer.Token(107));

            HullSelectList = Add(new ScrollList<ShipHullListItem>(hullSelectionBkg));
            HullSelectList.OnClick = OnHullListItemClicked;
            HullSelectList.EnableItemHighlight = true;
            InitializeShipHullsList();

            var dropdownRect = new Rectangle((int)(ScreenWidth * 0.375f), (int)ClassifCursor.Y + 25, 125, 18);

            CategoryList = new CategoryDropDown(dropdownRect);
            foreach (ShipData.Category item in Enum.GetValues(typeof(ShipData.Category)).Cast<ShipData.Category>())
                CategoryList.AddOption(item.ToString(), item);

            var hangarRect = new Rectangle((int)(ScreenWidth * 0.65f), (int)ClassifCursor.Y + 25, 150, 18);
            HangarOptionsList = new HangarDesignationDropDown(hangarRect);
            foreach (ShipData.HangarOptions item in Enum.GetValues(typeof(ShipData.HangarOptions)).Cast<ShipData.HangarOptions>())
                HangarOptionsList.AddOption(item.ToString(), item);

            var behaviorRect    = new Rectangle((int)(ScreenWidth * 0.15f), (int)ClassifCursor.Y + 50, 150, 18);
            ShieldsBehaviorList = new ShieldBehaviorDropDown(behaviorRect);
            foreach (ShieldsWarpBehavior item in Enum.GetValues(typeof(ShieldsWarpBehavior)).Cast<ShieldsWarpBehavior>())
                ShieldsBehaviorList.AddOption(item.ToString(), item);
                
            var carrierOnlyPos  = new Vector2(dropdownRect.X - 200, dropdownRect.Y);
            CarrierOnlyCheckBox = Checkbox(carrierOnlyPos, () => ActiveHull.CarrierShip, "Carrier Only", 1978);
            
            var shipStatsPanel = new Rectangle((int)HullSelectList.X + 50, (int)HullSelectList.Bottom - 20, 280, 320);
            ShipStats = new Menu1(shipStatsPanel);
            StatsSub  = new Submenu(shipStatsPanel);
            StatsSub.AddTab(Localizer.Token(108));
            ArcsButton = new GenericButton(new Vector2(HullSelectList.X - 32, 97f), "Arcs", Fonts.Pirulen20, Fonts.Pirulen16);

            CloseButton(ScreenWidth - 27, 99);
            OriginalZ = CameraPosition.Z;
        }

        void InitializeShipHullsList()
        {
            var categories = new Array<string>();
            foreach (ShipData hull in ResourceManager.Hulls)
            {
                if ((hull.IsShipyard && !Empire.Universe.Debug) || !EmpireManager.Player.IsHullUnlocked(hull.Hull))
                    continue;
                string cat = Localizer.GetRole(hull.Role, EmpireManager.Player);
                if (!categories.Contains(cat))
                    categories.Add(cat);
            }

            categories.Sort();
            foreach (string cat in categories)
            {
                var categoryItem = new ShipHullListItem(cat);
                HullSelectList.AddItem(categoryItem);

                foreach (ShipData hull in ResourceManager.Hulls)
                {
                    if ((!hull.IsShipyard || Empire.Universe.Debug) &&
                        EmpireManager.Player.IsHullUnlocked(hull.Hull) &&
                        cat == Localizer.GetRole(hull.Role, EmpireManager.Player))
                    {
                        categoryItem.AddSubItem(new ShipHullListItem(hull));
                    }
                }
            }
        }
    }
}