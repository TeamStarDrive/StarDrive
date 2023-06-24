using System;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.CombatTactics.UI;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.GameScreens.Universe.Debug;
using Ship_Game.Ships;
using Ship_Game.UI;
using Ship_Game.Universe;
using Point = SDGraphics.Point;
using Rectangle = SDGraphics.Rectangle;

using RectF = SDGraphics.RectF;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game
{
    public enum ModuleOrientation
    {
        Normal, Left, Right, Rear
    }

    public sealed partial class ShipDesignScreen : GameScreen
    {
        public UniverseScreen ParentUniverse;
        public Empire Player => ParentUniverse.Player;
        public DesignStanceButtons OrdersButton;

        public DesignShip DesignedShip { get; private set; }
        public ShipDesign CurrentDesign; // only Null during first time init, otherwise never Null, even in Hull Editor
        public ShipHull CurrentHull => CurrentDesign?.BaseHull; // can be null during first time init
        public DesignModuleGrid ModuleGrid;

        public string DesignOrHullName => CurrentDesign.Name;

        public EmpireUIOverlay EmpireUI;

        Vector3 CameraPos = new Vector3(0f, 0f, 1300f);
        float DesiredCamHeight = 1300f;
        Vector2 StartDragPos;

        readonly Array<ShipHull> AvailableHulls = new Array<ShipHull>();
        UIButton BtnSaveAs;
        UIButton BtnSymmetricDesign; // Symmetric Module Placement Feature by Fat Bastard
        UIButton BtnFilterModules;   // Filter Absolute Modules
        UIButton BtnStripShip;       // Removes all modules but armor, shields and command modules
        GenericButton ArcsButton;
        Rectangle SearchBar;
        Rectangle BottomSep;
        Rectangle BlackBar;

        ShipDesignInfoPanel InfoPanel;
        ShipDesignIssuesPanel IssuesPanel;

        // this contains module selection list and active module selection info
        public ModuleSelection ModuleSelectComponent { get; private set; }
        ScrollList<ShipHullListItem> HullSelectList;

        public ShipModule HighlightedModule;
        SlotStruct ProjectedSlot;
        string ScreenToLaunch;

        public ShipModule ActiveModule;
        CategoryDropDown CategoryList;
        HangarDesignationDropDown HangarOptionsList;

        bool ShowAllArcs;
        public bool ToggleOverlay = true;
        bool ShipSaved = true;
        public bool HullEditMode;
        HullEditorControls HullEditor;

        // Used in Developer Sandbox to load any design
        bool UnlockAllFactionDesigns;

        // Used in Dev SandBox to enable some special debug features
        public bool EnableDebugFeatures;

        public RoleName Role => DesignedShip.DesignRole;
        Rectangle DesignRoleRect;

        public HangarOptions HangarDesignation => HangarOptionsList.ActiveValue;

        public bool IsSymmetricDesignMode => Player.SymmetricDesignMode; // start with enabled by default

        public bool IsFilterOldModulesMode
        {
            get => ParentUniverse.UState.P.FilterOldModules;
            set => ParentUniverse.UState.P.FilterOldModules = value;
        }
          
        struct MirrorSlot
        {
            public SlotStruct Slot;
            public ModuleOrientation ModuleRot;
            public int TurretAngle;
        }

        public ShipDesignScreen(UniverseScreen universe, EmpireUIOverlay empireUi) : base(universe, toPause: universe)
        {
            ParentUniverse = universe;
            Name = "ShipDesignScreen";
            EmpireUI = empireUi;
            TransitionOnTime = 2f;
            HullEditMode = false;
            UnlockAllFactionDesigns = universe is DeveloperUniverse;
            EnableDebugFeatures = universe is DeveloperUniverse || universe.Debug;
        }

        void ReorientActiveModule(ModuleOrientation orientation)
        {
            if (ActiveModule == null)
                return;
            ShipModule template = ResourceManager.GetModuleTemplate(ActiveModule.UID);
            ActiveModule.SetModuleRotation(template.XSize, template.YSize, 
                                           orientation, ShipModule.DefaultFacingFor(orientation));
        }

        public ShipModule CreateModuleListItem(ShipModule template)
        {
            return CreateDesignModule(template.UID, ModuleOrientation.Normal, 0, DynamicHangarOptions.DynamicLaunch.ToString());
        }

        public ShipModule CreateDesignModule(string uid, ModuleOrientation moduleRot, int turretAngle, string hangarShipUID)
        {
            if (!ResourceManager.GetModuleTemplate(uid, out ShipModule moduleTemplate))
                return null; // this module UID doesn't exist anymore
            UniverseState us = ParentUniverse.UState;
            return ShipModule.CreateDesignModule(us, uid, moduleRot, turretAngle, hangarShipUID, CurrentHull);
        }

        // spawn a new active module under cursor
        // WARNING: must use Module UID string here, otherwise we can get incorrect XSIZE/YSIZE due to Orientations
        void SpawnActiveModule(string moduleUID, ModuleOrientation moduleRot, int turretAngle, string hangarShipUID)
        {
            ActiveModule = CreateDesignModule(moduleUID, moduleRot, turretAngle, hangarShipUID);
        }

        void ResetActiveModule()
        {
            ActiveModule?.UninstallModule();
            ActiveModule = null;
        }
        
        public void SetActiveModule(string moduleUID, ModuleOrientation moduleRot, int turretAngle, string hangarShipUID)
        {
            GameAudio.SmallServo();
            SpawnActiveModule(moduleUID, moduleRot, turretAngle, hangarShipUID);
            HighlightedModule = null;
        }

        class SlotInstall
        {
            public readonly SlotStruct Slot;
            public readonly ShipModule Mod;
            bool CanInstall;
            public SlotInstall() {}
            public SlotInstall(SlotStruct slot, ShipModule mod)
            {
                Slot = slot;
                Mod = mod;
            }
            public bool UpdateCanInstallTo(DesignModuleGrid grid)
            {
                if (Slot == null)
                    return false;
                if (!grid.ModuleFitsAtSlot(Slot, Mod))
                {
                    GameAudio.NegativeClick();
                    return false;
                }
                CanInstall = !Slot.IsSame(Mod, Mod.ModuleRot, Mod.TurretAngle);
                return CanInstall;
            }
            public void TryInstallTo(DesignModuleGrid designGrid)
            {
                if (CanInstall)
                    designGrid.InstallModule(Slot, Mod);
            }
        }

        SlotInstall CreateMirrorInstall(SlotInstall install)
        {
            if (IsSymmetricDesignMode && GetMirrorSlot(install.Slot, install.Mod, out MirrorSlot mirrored))
            {
                // @warning in order to get correct XSIZE/YSIZE, we MUST use Module Template UID here
                ShipModule mModule = CreateDesignModule(install.Mod.UID, mirrored.ModuleRot, mirrored.TurretAngle, install.Mod.HangarShipUID);
                return new SlotInstall(mirrored.Slot, mModule);
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
                
                OnDesignChanged();
                ShipModule m = active.Mod;
                SpawnActiveModule(m.UID, m.ModuleRot, m.TurretAngle, m.HangarShipUID);
            }
        }

        void ReplaceModulesWith(SlotStruct slot, ShipModule template)
        {
            if (!slot.IsModuleReplaceableWith(template))
            {
                GameAudio.NegativeClick();
                return;
            }

            ModuleGrid.StartUndoableAction();

            string replacementId = slot.Module.UID;
            foreach (SlotStruct replaceAt in ModuleGrid.SlotsList)
            {
                if (replaceAt.ModuleUID == replacementId)
                {
                    ShipModule m = CreateDesignModule(template.UID, replaceAt.Module.ModuleRot, 
                                                      replaceAt.Module.TurretAngle, ActiveModule.HangarShipUID);
                    ModuleGrid.InstallModule(replaceAt, m);
                }
            }
            
            OnDesignChanged();
        }

        void DeleteModuleAtSlot(SlotStruct slot)
        {
            if (slot.Module == null && slot.Parent == null)
                return;

            ModuleGrid.StartUndoableAction();
            ShipSaved = false;
            if (IsSymmetricDesignMode)
            {
                if (GetMirrorSlotStruct(slot, out SlotStruct mirrored))
                {
                    ModuleGrid.ClearSlots(mirrored.Root, mirrored.Root.Module);
                }
            }
            ModuleGrid.ClearSlots(slot.Root, slot.Root.Module);
            OnDesignChanged();
            GameAudio.SubBassWhoosh();
        }

        void StripModules()
        {
            ModuleGrid.StartUndoableAction();
            for (int i = 0; i < ModuleGrid.SlotsList.Count; i++)
            {
                SlotStruct slot = ModuleGrid.SlotsList[i];
                ShipModule module = slot.Module;
                if (module != null && module.Deflection <= 0 &&
                    !module.Is(ShipModuleType.Armor) && !module.Is(ShipModuleType.Engine) &&
                    !module.Is(ShipModuleType.Shield) && !module.Is(ShipModuleType.Command) &&
                    !module.Is(ShipModuleType.PowerPlant) && !module.Is(ShipModuleType.PowerConduit))
                {
                    ModuleGrid.ClearSlots(slot.Root, slot.Root.Module);
                    ShipSaved = false;
                }
            }

            OnDesignChanged();
        }

        void RemoveVisibleMesh()
        {
            // can be null at first load
            DesignedShip?.RemoveSceneObject();
        }

        void CreateSOFromCurrentHull()
        {
            DesignedShip.CreateSceneObject();
        }

        public void UpdateHullWorldPos()
        {
            DesignedShip.ShowSceneObjectAt(DesignedShip.Position, 0);
        }

        // @param zoomToHull whether to use the zoom-to-hull animation, not needed in some cases
        public void ChangeHull(ShipHull hullTemplate, bool zoomToHull = true)
        {
            if (hullTemplate == null) // if ShipDesignLoadScreen has no selected design
                return;

            if (HullEditMode)
                hullTemplate = hullTemplate.GetClone();

            // In Debug, show the modder HullName=`Misc/HaulerSmall` instead of VisibleName=`Small Freighter`
            string name = ParentUniverse.Debug ? hullTemplate.HullName : hullTemplate.VisibleName;
            ChangeHull(new ShipDesign(hullTemplate, name), zoomToHull: zoomToHull);
        }

        // @param zoomToHull whether to use the zoom-to-hull animation, not needed in some cases
        public void ChangeHull(IShipDesign shipDesignTemplate, bool zoomToHull = true)
        {
            if (shipDesignTemplate == null) // if ShipDesignLoadScreen has no selected design
                return;

            RemoveVisibleMesh();

            ShipDesign cloned = shipDesignTemplate.GetClone(shipDesignTemplate.Name);
            ModuleGrid = new DesignModuleGrid(this, cloned);
            CurrentDesign = cloned;
            DesignedShip = new DesignShip(ParentUniverse.UState, cloned); // TODO: make a mini-verse in Shipyard

            InstallModulesFromDesign(cloned);
            CreateSOFromCurrentHull();
            BindListsToActiveHull();

            if (!HullEditMode)
            {
                OrdersButton.ResetButtons(CurrentDesign);
                UpdateCarrierShip();
            }

            // force modules list to reset itself, so if we change from Battleship to Fighter
            // the available modules list is adjusted correctly
            ModuleSelectComponent.SelectedIndex = -1;
            if (zoomToHull)
                ZoomCameraToEncloseHull();

            // TODO: remove DesignIssues from this page
            InfoPanel.SetActiveDesign(DesignedShip);
            IssuesPanel.SetActiveDesign(DesignedShip);
            ShipSaved = DesignedShip.Modules.Length > 0;
        }

        public void UpdateDesignedShip(bool forceUpdate)
        {
            DesignedShip.UpdateDesign(ModuleGrid.CopyModulesList(), forceUpdate);
        }

        void InstallModulesFromDesign(ShipDesign design)
        {
            Point offset = design.BaseHull.GridCenter.Sub(design.GridInfo.Center);

            foreach (DesignSlot slot in design.GetOrLoadDesignSlots())
            {
                Point pos = slot.Pos.Add(offset);
                if (!ModuleGrid.Get(pos, out SlotStruct targetSlot))
                {
                    Log.Warning($"DesignModuleGrid failed to find Slot at {pos}");
                    continue;
                }

                ShipModule m = CreateDesignModule(slot.ModuleUID, slot.ModuleRot, slot.TurretAngle, slot.HangarShipUID);
                if (ModuleGrid.ModuleFitsAtSlot(targetSlot, m, logFailure: true))
                    ModuleGrid.InstallModule(targetSlot, m);
            }

            ModuleGrid.SaveDebugGrid();

            OnDesignChanged(false);
            ResetActiveModule();
        }

        public void OnDesignChanged(bool showRoleChangeTip = true)
        {
            var oldRole = Role;
            UpdateDesignedShip(forceUpdate:false);

            if (showRoleChangeTip && Role != oldRole)
                RoleData.CreateDesignRoleToolTip(Role, DesignRoleRect, true, Input.CursorPosition);

            ShipSaved = false;
            BtnSaveAs.Text = Localizer.Token(HullEditMode || IsGoodDesign() || IsEmptyHull 
                ? GameText.SaveAs 
                : GameText.SaveWIP);
        }

        bool IsEmptyHull => CurrentDesign.UniqueModuleUIDs.Length == 0;

        // true if this module can never fit into the module grid
        public bool CanNeverFitModuleGrid(ShipModule module)
        {
            foreach (SlotStruct slot in ModuleGrid.SlotsList)
            {
                if (ModuleGrid.ModuleFitsAtSlot(slot, module))
                    return false;
                ShipModule tiltedModule = CreateDesignModule(module.UID, ModuleOrientation.Right, 0, null);
                if (ModuleGrid.ModuleFitsAtSlot(slot, tiltedModule))
                    return false;
            }
            return true;
        }

        public override void Update(float fixedDeltaTime)
        {
            CameraPos.Z = CameraPos.Z.SmoothStep(DesiredCamHeight, 0.2f);
            UpdateViewMatrix(CameraPos);

            var simTime = new FixedSimTime(fixedDeltaTime);
            DesignedShip.SubLightAccelerate(100);
            DesignedShip.Velocity = new Vector2(0, 100);
            DesignedShip.UpdateThrusters(simTime);

            base.Update(fixedDeltaTime);
        }

        public override void LoadContent()
        {
            Log.Info("ShipDesignScreen.LoadContent");

            UpdateAvailableHulls();
            CreateGUI();
            InitializeCamera();
            AssignLightRig(LightRigIdentity.Shipyard, "example/ShipyardLightrig");

            ShipDesign lastWIP = ShipDesignWIP.GetLatestWipToLoad(Player);
            if (lastWIP != null)
                ChangeHull(lastWIP);
            else
                ChangeHull(AvailableHulls[0]);
        }

        void OnReloadAfterTechChange()
        {
            UpdateAvailableHulls();
            RefreshHullSelectList();
            ModuleSelectComponent.ResetActiveCategory();
            UpdateDesignedShip(forceUpdate:true);
        }

        ButtonStyle SymmetricDesignBtnStyle  => IsSymmetricDesignMode ? ButtonStyle.Military : ButtonStyle.BigDip;
        ButtonStyle FilterModulesBtnStyle    => IsFilterOldModulesMode ? ButtonStyle.Military : ButtonStyle.BigDip;

        void CreateGUI()
        {
            RemoveAll();
            ModuleSelectComponent = Add(new ModuleSelection(this, new(5, LowRes ? 45 : 100), new(305, LowRes ? 350 : 490)));

            BlackBar = new Rectangle(0, ScreenHeight - 70, 3000, 70);
            ClassifCursor = new Vector2(ScreenWidth * .5f,ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px").Height + 10);

            float ordersBarX = ClassifCursor.X - 15;
            var ordersBarPos = new Vector2(ordersBarX, ClassifCursor.Y + 20);
            OrdersButton = new DesignStanceButtons(this, ordersBarPos);
            Add(OrdersButton);

            if (HullEditMode || EnableDebugFeatures)
                HullEditor = Add(new HullEditorControls(this, ModuleSelectComponent.TopRight + new Vector2(50, 0)));

            UIList bottomListRight = AddList(new Vector2(ScreenWidth - 250f, ScreenHeight - 50f));
            bottomListRight.LayoutStyle = ListLayoutStyle.ResizeList;
            bottomListRight.Direction = new Vector2(-1, 0);
            bottomListRight.Padding = new Vector2(16f, 2f);
            BtnSaveAs = bottomListRight.Add(ButtonStyle.Medium, GameText.SaveAs, click: b =>
            {
                bool isGoodDesign = IsGoodDesign();
                if (!HullEditMode && !isGoodDesign)
                {
                    if (!ShipSaved)
                        SaveWIP();
                    else
                        GameAudio.NegativeClick();

                    return;
                    // ScreenManager.AddScreen(new MessageBoxScreen(this, Localizer.Token(GameText.ThisShipDesignIsInvalid)));
                    //return;*/
                }

                ScreenManager.AddScreen(new ShipDesignSaveScreen(this, DesignOrHullName, hullDesigner:HullEditMode));
            });
            BtnSaveAs.Tooltip = Localizer.Token(GameText.SaveShipDesignDesc);
            BtnSaveAs.Hotkey = InputBindings.FromString("Ctrl+S");
            bottomListRight.Add(ButtonStyle.Medium, GameText.Load, click: b =>
            {
                if (HullEditMode)
                    ScreenManager.AddScreen(new MessageBoxScreen(this, "Load Design is not available in Hull Edit Mode"));
                else
                    ScreenManager.AddScreen(new ShipDesignLoadScreen(this, UnlockAllFactionDesigns));
            });
            bottomListRight.Add(ButtonStyle.Medium, GameText.ToggleOverlay, click: b =>
            {
                ToggleOverlay = !ToggleOverlay;
            }).ClickSfx = "blip_click";
            BtnSymmetricDesign = bottomListRight.Add(ButtonStyle.Medium, Localizer.Token(GameText.SymmetricDesign), click: b =>
            {
                OnSymmetricDesignToggle();
            });
            BtnSymmetricDesign.ClickSfx = "blip_click";
            BtnSymmetricDesign.Tooltip = Localizer.Token(GameText.YouCanSwitchFromNormal);
            BtnSymmetricDesign.Hotkey  = InputBindings.FromString("M");
            BtnSymmetricDesign.Style   = SymmetricDesignBtnStyle;


            UIList bottomListLeft = AddList(new Vector2(50f, ScreenHeight - 50f));
            bottomListLeft.LayoutStyle = ListLayoutStyle.ResizeList;
            bottomListLeft.Direction = new Vector2(+1, 0);
            bottomListLeft.Padding = new Vector2(16f, 2f);

            BtnStripShip = bottomListLeft.Add(ButtonStyle.Medium, Localizer.Token(GameText.NormalDesign), click: b =>
            {
                OnStripShipToggle();
            });
            BtnStripShip.ClickSfx = "blip_click";
            BtnStripShip.Tooltip = Localizer.Token(GameText.StripsTheShipOfAny);

            BtnFilterModules = bottomListLeft.Add(ButtonStyle.Medium, Localizer.Token(GameText.OmitOldModules), click: b =>
            {
                OnFilterModuleToggle();
            });
            BtnFilterModules.ClickSfx = "blip_click";
            BtnFilterModules.Tooltip  = GameText.WhenToggledRedAnyModule;
            BtnFilterModules.Style    = FilterModulesBtnStyle;

            SearchBar = new Rectangle((int)ScreenCenter.X, (int)bottomListRight.Y, 210, 25);
            BottomSep = new Rectangle(BlackBar.X, BlackBar.Y, BlackBar.Width, 1);

            Vector2 hullSelSize = new(SelectSize(260, 280, 320), SelectSize(250, 400, 500));
            var hullSelectPos = new LocalPos(ScreenWidth - hullSelSize.X, ModuleSelectComponent.LocalPos.Y);
            var hullSelectSub = Add(new SubmenuScrollList<ShipHullListItem>(hullSelectPos, hullSelSize, GameText.SelectHull));
            // rounded black background
            hullSelectSub.SetBackground(Colors.TransparentBlackFill);

            HullSelectList = hullSelectSub.List;
            HullSelectList.OnClick = OnHullListItemClicked;
            HullSelectList.EnableItemHighlight = true;
            RefreshHullSelectList();
            hullSelectSub.PerformLayout();

            var dropdownRect = new Rectangle((int)(ScreenWidth * 0.375f), (int)ClassifCursor.Y + 25, 125, 18);

            CategoryList = new CategoryDropDown(dropdownRect);
            foreach (ShipCategory item in Enum.GetValues(typeof(ShipCategory)).Cast<ShipCategory>())
                CategoryList.AddOption(item.ToString(), item);

            var hangarRect = new Rectangle((int)(ScreenWidth * 0.65f), (int)ClassifCursor.Y + 25, 150, 18);
            HangarOptionsList = new HangarDesignationDropDown(hangarRect);
            foreach (HangarOptions item in Enum.GetValues(typeof(HangarOptions)).Cast<HangarOptions>())
                HangarOptionsList.AddOption(item.ToString(), item);

            var carrierOnlyPos  = new Vector2(dropdownRect.X - 200, dropdownRect.Y);
            CarrierOnlyCheckBox = Checkbox(carrierOnlyPos,
                () => CurrentDesign?.IsCarrierOnly == true,
                (b) => { if (CurrentDesign != null) CurrentDesign.IsCarrierOnly = b; }, "Carrier Only", GameText.WhenMarkedThisShipCan);

            ArcsButton = new GenericButton(new Vector2(HullSelectList.X - 32, 97f), "Arcs", Fonts.Pirulen20, Fonts.Pirulen16)
            {
                ToggleOnColor = Color.DarkOrange,
                ButtonStyle = GenericButton.Style.Shadow,
            };

            var infoRect = RectF.FromPoints((HullSelectList.X + 20), (ScreenWidth - 20),
                                            HullSelectList.Bottom + 10, BlackBar.Y);
            InfoPanel = Add(new ShipDesignInfoPanel(infoRect));

            var issuesRect = RectF.FromPoints(InfoPanel.X - 200, InfoPanel.X,
                                              HullSelectList.Bottom + 10, BlackBar.Y);
            IssuesPanel = Add(new ShipDesignIssuesPanel(this, issuesRect));

            if (EnableDebugFeatures)
            {
                var debugUnlocks = Add(new ResearchDebugUnlocks(ParentUniverse, OnReloadAfterTechChange));
                debugUnlocks.SetAbsPos(10, 45);
            }

            CloseButton(ScreenWidth - 27, 75);
        }

        void UpdateAvailableHulls()
        {
            AvailableHulls.Clear();

            if (UnlockAllFactionDesigns)
            {
                foreach (ShipHull hull in ResourceManager.Hulls)
                {
                    hull.ReloadIfNeeded();
                    AvailableHulls.Add(hull);
                }
            }
            else
            {
                string[] hulls = Player.GetUnlockedHulls();
                foreach (string hull in hulls)
                {
                    if (ResourceManager.Hull(hull, out ShipHull hullData))
                    {
                        if ((!hullData.IsShipyard || ParentUniverse.Debug))
                        {
                            hullData.ReloadIfNeeded();
                            AvailableHulls.Add(hullData);
                        }
                    }
                }
            }
        }

        void InitializeCamera()
        {
            // set shipyard's fov much lower to reduce parallax
            SetPerspectiveProjection(fovYdegrees: 20, maxDistance: 30000);
            UpdateViewMatrix(CameraPos);
        }

        void RefreshHullSelectList()
        {
            HullSelectList.Reset();

            var categories = new Array<string>();
            foreach (ShipHull hull in AvailableHulls)
            {
                string cat = Localizer.GetRole(hull.Role, Player);
                if (!categories.Contains(cat))
                    categories.Add(cat);
            }

            categories.Sort();
            foreach (string cat in categories)
            {
                var categoryItem = new ShipHullListItem(Player, cat);
                HullSelectList.AddItem(categoryItem);

                foreach (ShipHull hull in AvailableHulls)
                {
                    if (cat == Localizer.GetRole(hull.Role, Player))
                    {
                        categoryItem.AddSubItem(new ShipHullListItem(Player, hull));
                    }
                }
            }
        }
    }
}
