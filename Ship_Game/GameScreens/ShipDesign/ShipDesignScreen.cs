using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.AI.CombatTactics.UI;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens;
using Ship_Game.GameScreens.ShipDesign;
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
        //public Array<ToggleButton> CombatStatusButtons = new Array<ToggleButton>();
        public DesignStanceButtons OrdersButton;

        // this can be Null if we are in HullEdit mode
        public DesignShip DesignedShip { get; private set; }
        public ShipData CurrentDesign;
        public ShipHull CurrentHull; // never Null

        public string DesignOrHullName => CurrentDesign?.Name ?? CurrentHull.HullName;

        public EmpireUIOverlay EmpireUI;
        SceneObject shipSO;

        Vector3 CameraPosition = new Vector3(0f, 0f, 1300f);
        float DesiredCamHeight = 1300f;
        Vector2 StartDragPos;

        readonly Array<ShipHull> AvailableHulls = new Array<ShipHull>();
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
        ModuleSelection ModuleSelectComponent;
        ScrollList2<ShipHullListItem> HullSelectList;

        public ShipModule HighlightedModule;
        SlotStruct ProjectedSlot;
        string ScreenToLaunch;

        SlotModOperation Operation;
        public ShipModule ActiveModule;
        CategoryDropDown CategoryList;
        HangarDesignationDropDown HangarOptionsList;

        bool ShowAllArcs;
        public bool ToggleOverlay = true;
        bool ShipSaved = true;
        bool HullEditMode;

        // Used in Developer Sandbox to load any design
        bool UnlockAllFactionDesigns;

        public ShipData.RoleName Role { get; private set; }
        Rectangle DesignRoleRect;

        public bool IsSymmetricDesignMode
        {
            get => GlobalStats.SymmetricDesign;
            set => GlobalStats.SymmetricDesign = value;
        }

        public bool IsFilterOldModulesMode
        {
            get => GlobalStats.FilterOldModules;
            set => GlobalStats.FilterOldModules = value;
        }
          
        struct MirrorSlot
        {
            public SlotStruct Slot;
            public ModuleOrientation ModuleRot;
            public int TurretAngle;
        }

        public ShipDesignScreen(GameScreen parent, EmpireUIOverlay empireUi) : base(parent)
        {
            Name = "ShipDesignScreen";
            EmpireUI = empireUi;
            TransitionOnTime = 2f;
            HullEditMode = false;
            UnlockAllFactionDesigns = parent is DeveloperUniverse;
        }

        void ReorientActiveModule(ModuleOrientation orientation)
        {
            if (ActiveModule == null)
                return;
            ShipModule template = ResourceManager.GetModuleTemplate(ActiveModule.UID);
            ActiveModule.SetModuleRotation(template.XSIZE, template.YSIZE, 
                                           orientation, ShipModule.DefaultFacingFor(orientation));
        }

        void AddHullSlot(InputState input)
        {
            Point gridPos = ModuleGrid.WorldToGridPos(input.CursorPosition);

            // make sure there's no accidental overlap!
            if (ModuleGrid.Get(gridPos, out SlotStruct _))
            {
                GameAudio.NegativeClick();
                return;
            }

            GameAudio.NegativeClick();

            // TODO: Implement this again
            
            //var slots = new Array<ModuleSlotData>(ActiveHull.ModuleSlots);
            //slots.Add(new ModuleSlotData(position, Restrictions.IO));
            //ActiveHull.ModuleSlots = slots.ToArray();
            //Array.Sort(ActiveHull.ModuleSlots, ModuleSlotData.Sorter);

            //ChangeHull(ActiveHull); // rebuild the hull
        }

        void EditHullSlot(SlotStruct ss, SlotModOperation op)
        {
            HullSlot slot = CurrentHull.FindSlot(ss.Pos);
            if (slot == null)
                return;

            ShipHull newHull = CurrentHull.GetClone();
            switch (op)
            {
                default: return;
                case SlotModOperation.Delete: newHull.HullSlots.Remove(slot, out newHull.HullSlots); break;
                case SlotModOperation.I:      slot.R = Restrictions.I;  break;
                case SlotModOperation.O:      slot.R = Restrictions.O;  break;
                case SlotModOperation.E:      slot.R = Restrictions.E;  break;
                case SlotModOperation.IO:     slot.R = Restrictions.IO; break;
                case SlotModOperation.IE:     slot.R = Restrictions.IE; break;
                case SlotModOperation.OE:     slot.R = Restrictions.OE; break;
                case SlotModOperation.IOE:    slot.R = Restrictions.IOE; break;
            }
            ChangeHull(newHull);
        }

        public ShipModule CreateModuleListItem(ShipModule template)
        {
            ShipModule m = ShipModule.CreateNoParent(template, EmpireManager.Player, CurrentHull);
            m.SetAttributes();
            return m;
        }

        public ShipModule CreateDesignModule(string uid, ModuleOrientation moduleRot, int turretAngle)
        {
            return ShipModule.CreateDesignModule(ResourceManager.GetModuleTemplate(uid),
                                                 moduleRot, turretAngle, CurrentHull);
        }

        // spawn a new active module under cursor
        // WARNING: must use Module UID string here, otherwise we can get incorrect XSIZE/YSIZE due to Orientations
        void SpawnActiveModule(string moduleUID, ModuleOrientation moduleRot, int turretAngle)
        {
            ActiveModule = CreateDesignModule(moduleUID, moduleRot, turretAngle);
            ActiveModule.SetAttributes();

            if (!ActiveModule.IsSupplyBay && !ActiveModule.IsTroopBay
                && ActiveModule.ModuleType == ShipModuleType.Hangar)
            {
                ActiveModule.HangarShipUID = DynamicHangarOptions.DynamicLaunch.ToString();
            }
        }

        void ResetActiveModule()
        {
            ActiveModule = null;
        }
        
        public void SetActiveModule(string moduleUID, ModuleOrientation moduleRot, int turretAngle)
        {
            GameAudio.SmallServo();

            SpawnActiveModule(moduleUID, moduleRot, turretAngle);
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
            if (IsSymmetricDesignMode &&
                GetMirrorSlot(install.Slot, install.Mod.XSIZE, install.Mod.ModuleRot, out MirrorSlot mirrored))
            {
                // @warning in order to get correct XSIZE/YSIZE, we MUST use Module Template UID here
                ShipModule mModule = CreateDesignModule(install.Mod.UID, mirrored.ModuleRot, mirrored.TurretAngle);
                mModule.HangarShipUID = install.Mod.HangarShipUID;
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
                
                ShipSaved = false;
                OnDesignChanged();
                SpawnActiveModule(active.Mod.UID, active.Mod.ModuleRot, active.Mod.TurretAngle);
                ActiveModule.HangarShipUID = active.Mod.HangarShipUID;
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
                    ShipModule m = CreateDesignModule(template.UID, replaceAt.Module.ModuleRot, replaceAt.Module.TurretAngle);
                    m.HangarShipUID = ActiveModule.HangarShipUID;
                    ModuleGrid.InstallModule(replaceAt, m);
                }
            }
            
            ShipSaved = false;
            OnDesignChanged();
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
            OnDesignChanged();
            GameAudio.SubBassWhoosh();
        }

        void StripModules()
        {
            ModuleGrid.StartUndoableAction();
            for (int i = 0; i < ModuleGrid.SlotsList.Count; i++)
            {
                SlotStruct slot = ModuleGrid.SlotsList[i];
                if (slot.Module == null)
                    continue;

                ShipModule module = slot.Module;
                if (module.Is(ShipModuleType.Armor)
                    || module.Is(ShipModuleType.Engine)
                    || module.Is(ShipModuleType.Shield)
                    || module.Is(ShipModuleType.Command)
                    || module.DamageThreshold > 0)
                {
                    continue;
                }

                ModuleGrid.ClearSlots(slot.Root, slot.Root.Module);
            }

            OnDesignChanged();
        }

        DesignModuleGrid ModuleGrid;

        public void ChangeHull(ShipData shipDesignTemplate)
        {
            if (shipDesignTemplate == null) // if ShipDesignLoadScreen has no selected design
                return;
            ShipData cloned = shipDesignTemplate.GetClone();
            ModuleGrid = new DesignModuleGrid(cloned);
            ModuleGrid.OnGridChanged = UpdateDesignedShip;

            CurrentDesign = cloned;
            CurrentHull   = cloned.BaseHull;
            DesignedShip = new DesignShip(cloned);

            InstallModulesFromDesign(cloned);
            AfterHullChange();
        }

        public void ChangeHull(ShipHull hullTemplate)
        {
            if (!HullEditMode)
            {
                ChangeHull(new ShipData(hullTemplate));
                return;
            }

            ModuleGrid = new DesignModuleGrid(hullTemplate.HullName, hullTemplate);

            CurrentDesign = null;
            CurrentHull   = hullTemplate.GetClone();
            DesignedShip  = null;

            AfterHullChange();
        }

        void AfterHullChange()
        {
            CreateSOFromCurrentHull();

            if (DesignedShip != null)
            {
                BindListsToActiveHull();
                OrdersButton.ResetButtons(DesignedShip);
                UpdateCarrierShip();
            }

            // force modules list to reset itself, so if we change from Battleship to Fighter
            // the available modules list is adjusted correctly
            ModuleSelectComponent.SelectedIndex = -1;
            ZoomCameraToEncloseHull(CurrentHull);

            // TODO: remove DesignIssues from this page
            InfoPanel.SetActiveDesign(DesignedShip);
            IssuesPanel.SetActiveDesign(DesignedShip);
        }

        void UpdateDesignedShip()
        {
            DesignedShip?.UpdateDesign(CreateModuleSlots());
        }

        void InstallModulesFromDesign(ShipData design)
        {
            foreach (DesignSlot designSlot in design.ModuleSlots)
            {
                if (!ModuleGrid.Get(designSlot.Pos, out SlotStruct targetSlot))
                {
                    Log.Warning($"DesignModuleGrid failed to find Slot at {designSlot.Pos}");
                    continue;
                }

                ShipModule newModule = CreateDesignModule(designSlot.ModuleUID, designSlot.ModuleRot, designSlot.TurretAngle);
                if (!ModuleGrid.ModuleFitsAtSlot(targetSlot, newModule, logFailure: true))
                    continue;
                
                if (newModule.ModuleType == ShipModuleType.Hangar)
                    newModule.HangarShipUID = designSlot.HangarShipUID;

                ModuleGrid.InstallModule(targetSlot, newModule);
            }

            ModuleGrid.SaveDebugGrid();

            OnDesignChanged(false);
            ResetActiveModule();
        }

        void OnDesignChanged(bool showRoleChangeTip = true)
        {
            ModuleGrid.OnModuleGridChanged();
            RecalculateDesignRole(showRoleChangeTip);
        }

        void RecalculateDesignRole(bool showRoleChangeTip)
        {
            if (CurrentDesign == null)
                return;

            var oldRole = Role;
            Role = new RoleData(CurrentDesign, ModuleGrid.CopyModulesList()).DesignRole;

            if (Role != oldRole && showRoleChangeTip)
            {
                Vector2 pos = new Vector2(ScreenCenter.X-100, ModuleSelectComponent.Y + 50);
                RoleData.CreateDesignRoleToolTip(Role, DesignRoleRect, true, pos);
            }
        }

        // true if this module can never fit into the module grid
        public bool CanNeverFitModuleGrid(ShipModule module)
        {
            foreach (SlotStruct slot in ModuleGrid.SlotsList)
            {
                if (ModuleGrid.ModuleFitsAtSlot(slot, module))
                    return false;
                ShipModule tiltedModule = CreateDesignModule(module.UID, ModuleOrientation.Right, 0);
                if (ModuleGrid.ModuleFitsAtSlot(slot, tiltedModule))
                    return false;
            }
            return true;
        }

        public override void Update(UpdateTimes elapsed, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            CameraPosition.Z = MathHelper.SmoothStep(CameraPosition.Z, DesiredCamHeight, 0.2f);
            UpdateViewMatrix(CameraPosition);
            base.Update(elapsed, otherScreenHasFocus, coveredByOtherScreen);
        }

        enum SlotModOperation
        {
            Delete,
            Add,
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
            UpdateAvailableHulls();
            CreateGUI();
            InitializeCamera();
            ChangeHull(AvailableHulls[0]);
            BindListsToActiveHull();

            AssignLightRig(LightRigIdentity.Shipyard, "example/ShipyardLightrig");
        }

        ButtonStyle SymmetricDesignBtnStyle  => GlobalStats.SymmetricDesign ? ButtonStyle.Military : ButtonStyle.BigDip;
        ButtonStyle FilterModulesBtnStyle    => GlobalStats.FilterOldModules ? ButtonStyle.Military : ButtonStyle.BigDip;

        void CreateGUI()
        {
            RemoveAll();
            ModuleSelectComponent = Add(new ModuleSelection(this, new Rectangle(5, (LowRes ? 45 : 100), 305, (LowRes ? 350 : 490))));

            BlackBar = new Rectangle(0, ScreenHeight - 70, 3000, 70);
            ClassifCursor = new Vector2(ScreenWidth * .5f,ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px").Height + 10);

            float ordersBarX = ClassifCursor.X - 15;
            var ordersBarPos = new Vector2(ordersBarX, ClassifCursor.Y + 20);
            OrdersButton = new DesignStanceButtons(this, ordersBarPos);
            Add(OrdersButton);

            UIList bottomListRight = AddList(new Vector2(ScreenWidth - 250f, ScreenHeight - 50f));
            bottomListRight.LayoutStyle = ListLayoutStyle.ResizeList;
            bottomListRight.Direction = new Vector2(-1, 0);
            bottomListRight.Padding = new Vector2(16f, 2f);
            bottomListRight.Add(ButtonStyle.Medium, GameText.SaveAs, click: b =>
            {
                if (!HullEditMode && !IsGoodDesign())
                {
                    GameAudio.NegativeClick();
                    ScreenManager.AddScreen(new MessageBoxScreen(this, Localizer.Token(GameText.ThisShipDesignIsInvalid)));
                    return;
                }
                ScreenManager.AddScreen(new ShipDesignSaveScreen(this, DesignOrHullName, hullDesigner:HullEditMode));
            });
            bottomListRight.Add(ButtonStyle.Medium, GameText.Load, click: b =>
            {
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
            BtnSymmetricDesign.Tooltip  = Localizer.Token(GameText.YouCanSwitchFromNormal);
            BtnSymmetricDesign.HotKey   = "M";
            BtnSymmetricDesign.Style    = SymmetricDesignBtnStyle;


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

            int hullSelY = SelectSize(45, 100, 100);
            int hullSelW = SelectSize(260, 280, 320);
            int hullSelH = SelectSize(250, 400, 500);
            var hullSelectionBkg = new Submenu(ScreenWidth - 285, hullSelY, hullSelW, hullSelH);
            // rounded black background
            hullSelectionBkg.Background = new Selector(hullSelectionBkg.Rect.CutTop(25), new Color(0,0,0,210));
            hullSelectionBkg.AddTab(Localizer.Token(GameText.SelectHull));

            HullSelectList = Add(new ScrollList2<ShipHullListItem>(hullSelectionBkg));
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

            var carrierOnlyPos  = new Vector2(dropdownRect.X - 200, dropdownRect.Y);
            CarrierOnlyCheckBox = Checkbox(carrierOnlyPos,
                () => CurrentDesign?.CarrierShip == true,
                (b) => { if (CurrentDesign != null) CurrentDesign.CarrierShip = b; }, "Carrier Only", GameText.WhenMarkedThisShipCan);

            ArcsButton = new GenericButton(new Vector2(HullSelectList.X - 32, 97f), "Arcs", Fonts.Pirulen20, Fonts.Pirulen16);

            var infoRect = RectF.FromPoints((HullSelectList.X + 20), (ScreenWidth - 20),
                                            HullSelectList.Bottom + 10, BlackBar.Y);
            InfoPanel = Add(new ShipDesignInfoPanel(infoRect));

            var issuesRect = RectF.FromPoints(InfoPanel.X - 200, InfoPanel.X,
                                              HullSelectList.Bottom + 10, BlackBar.Y);
            IssuesPanel = Add(new ShipDesignIssuesPanel(this, issuesRect));

            CloseButton(ScreenWidth - 27, 99);
        }

        void UpdateAvailableHulls()
        {
            AvailableHulls.Clear();

            if (UnlockAllFactionDesigns)
            {
                AvailableHulls.AddRange(ResourceManager.Hulls);
            }
            else
            {
                string[] hulls = EmpireManager.Player.GetUnlockedHulls();
                foreach (string hull in hulls)
                {
                    if (ResourceManager.Hull(hull, out ShipHull hullData))
                    {
                        if ((!hullData.IsShipyard || Empire.Universe.Debug))
                        {
                            AvailableHulls.Add(hullData);
                        }
                    }
                }
            }
        }

        void InitializeCamera()
        {
            float aspectRatio = (float)Viewport.Width / Viewport.Height;
            Projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspectRatio, 1f, 120000f);
            UpdateViewMatrix(CameraPosition);
        }

        void InitializeShipHullsList()
        {
            var categories = new Array<string>();
            foreach (ShipHull hull in AvailableHulls)
            {
                string cat = Localizer.GetRole(hull.Role, EmpireManager.Player);
                if (!categories.Contains(cat))
                    categories.Add(cat);
            }

            categories.Sort();
            foreach (string cat in categories)
            {
                var categoryItem = new ShipHullListItem(cat);
                HullSelectList.AddItem(categoryItem);

                foreach (ShipHull hull in AvailableHulls)
                {
                    if (cat == Localizer.GetRole(hull.Role, EmpireManager.Player))
                    {
                        categoryItem.AddSubItem(new ShipHullListItem(hull));
                    }
                }
            }
        }
    }
}
