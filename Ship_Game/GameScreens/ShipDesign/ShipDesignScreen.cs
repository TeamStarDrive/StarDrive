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
        public Camera2D Camera;
        //public Array<ToggleButton> CombatStatusButtons = new Array<ToggleButton>();
        public DesignStanceButtons OrdersButton;
        public DesignShip DesignedShip { get; private set; }
        public ShipData ActiveHull;
        public EmpireUIOverlay EmpireUI;
        SceneObject shipSO;
        Vector3 CameraPosition = new Vector3(0f, 0f, 1300f);
        Vector2 Offset;
        readonly Array<ShipData> AvailableHulls = new Array<ShipData>();
        UIButton BtnSymmetricDesign; // Symmetric Module Placement Feature by Fat Bastard
        UIButton BtnFilterModules;   // Filter Absolute Modules
        UIButton BtnStripShip;       // Removes all modules but armor, shields and command modules
        GenericButton ArcsButton;
        float OriginalZ;
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
        Vector2 CameraVelocity;
        Vector2 StartDragPos;
        ShipData ChangeTo;
        string ScreenToLaunch;
        float TransitionZoom = 1f;
        SlotModOperation Operation;
        public ShipModule ActiveModule;
        ModuleOrientation ActiveModState;
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
            public ModuleOrientation Orientation;
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

        Vector2 WorldToDesignCoords(Vector2 worldCoord)
        {
            return new Vector2((int)((worldCoord.X - Offset.X) / 16f) * 16f + Offset.X,
                               (int)((worldCoord.Y - Offset.Y) / 16f) * 16f + Offset.Y);
        }

        void AddHullSlot(InputState input)
        {
            Vector2 cursor = Camera.GetWorldSpaceFromScreenSpace(input.CursorPosition);

            // make sure there's no accidental overlap!
            if (ModuleGrid.Get(new Point((int)cursor.X, (int)cursor.Y), out SlotStruct _))
            {
                GameAudio.NegativeClick();
                return;
            }
            
            Vector2 position = WorldToDesignCoords(cursor) - Offset + new Vector2(8f, 8f);
            var slots = new Array<ModuleSlotData>(ActiveHull.ModuleSlots);
            slots.Add(new ModuleSlotData(position, Restrictions.IO));
            ActiveHull.ModuleSlots = slots.ToArray();
            Array.Sort(ActiveHull.ModuleSlots, ModuleSlotData.Sorter);

            ChangeHull(ActiveHull); // rebuild the hull
        }

        void EditHullSlot(Vector2 slotPos, SlotModOperation op)
        {
            ModuleSlotData target = FindModuleSlotAtPos(slotPos);
            if (target == null)
                return;

            switch (op)
            {
                default: return;
                case SlotModOperation.Delete: ActiveHull.ModuleSlots.Remove(target, out ActiveHull.ModuleSlots); break;
                case SlotModOperation.I:      target.Restrictions = Restrictions.I;  break;
                case SlotModOperation.O:      target.Restrictions = Restrictions.O;  break;
                case SlotModOperation.E:      target.Restrictions = Restrictions.E;  break;
                case SlotModOperation.IO:     target.Restrictions = Restrictions.IO; break;
                case SlotModOperation.IE:     target.Restrictions = Restrictions.IE; break;
                case SlotModOperation.OE:     target.Restrictions = Restrictions.OE; break;
                case SlotModOperation.IOE:    target.Restrictions = Restrictions.IOE; break;
            }
            ChangeHull(ActiveHull);
        }

        public ShipModule CreateModuleListItem(ShipModule template)
        {
            ShipModule m = ShipModule.CreateNoParent(template, EmpireManager.Player, ActiveHull);
            m.SetAttributes();
            return m;
        }

        public ShipModule CreateDesignModule(string uid, ModuleOrientation orientation, float facing)
        {
            return ShipModule.CreateDesignModule(ResourceManager.GetModuleTemplate(uid), orientation, facing, ActiveHull);
        }

        // spawn a new active module under cursor
        // WARNING: must use Module UID string here, otherwise we can get incorrect XSIZE/YSIZE due to Orientations
        void SpawnActiveModule(string moduleUID, ModuleOrientation orientation, float facing)
        {
            ActiveModule = CreateDesignModule(moduleUID, orientation, facing);
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
        
        public void SetActiveModule(string moduleUID, ModuleOrientation orientation, float facing)
        {
            GameAudio.SmallServo();

            SpawnActiveModule(moduleUID, orientation, facing);
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
                    GameAudio.NegativeClick();
                    return false;
                }
                CanInstall = !Slot.IsSame(Mod, Ori, Mod.FacingDegrees);
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
                // @warning in order to get correct XSIZE/YSIZE, we MUST use Module Template UID here
                ShipModule mModule = CreateDesignModule(install.Mod.UID, mOri, mFacing);
                mModule.hangarShipUID = install.Mod.hangarShipUID;
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
                
                ShipSaved = false;
                OnDesignChanged();
                SpawnActiveModule(active.Mod.UID, active.Ori, active.Slot.Facing);
                ActiveModule.hangarShipUID = active.Mod.hangarShipUID;
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
                    ShipModule m    = CreateDesignModule(template.UID, replaceAt.Orientation, replaceAt.Module.FacingDegrees);
                    m.hangarShipUID = ActiveModule.hangarShipUID;
                    ModuleGrid.InstallModule(replaceAt, m, replaceAt.Orientation);
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
        
        public void ChangeHull(ShipData hull)
        {
            if (hull == null)
                return;

            ModuleGrid = new DesignModuleGrid(hull, Offset);
            ModuleGrid.OnGridChanged = UpdateDesignedShip;

            ActiveHull = ModuleGrid.Hull;
            DesignedShip = new DesignShip(ActiveHull);
            
            InstallModulesFromDesign();

            CreateSOFromActiveHull();
            BindListsToActiveHull();
            OrdersButton.ResetButtons(DesignedShip);
            UpdateCarrierShip();

            // force modules list to reset itself, so if we change from Battleship to Fighter
            // the available modules list is adjusted correctly
            ModuleSelectComponent.SelectedIndex = -1;

            ZoomCameraToEncloseHull(ActiveHull);

            // TODO: remove DesignIssues from this page
            InfoPanel.SetActiveDesign(DesignedShip);
            IssuesPanel.SetActiveDesign(DesignedShip);
        }

        void UpdateDesignedShip()
        {
            ActiveHull.UpdateGridInfo();
            DesignedShip?.UpdateDesign(CreateModuleSlots());
        }

        void InstallModulesFromDesign()
        {
            foreach (SlotStruct designSlot in ModuleGrid.SlotsList)
            {
                string uid = designSlot.ModuleUID;
                if (uid == null || uid == "Dummy") // @note Backwards savegame compatibility for ship designs, dummy modules are deprecated
                    continue;

                if (!ModuleGrid.Get(designSlot.Position, out SlotStruct targetSlot))
                {
                    Log.Warning($"DesignModuleGrid failed to find Slot at {designSlot.Position}");
                    continue;
                }

                ShipModule newModule = CreateDesignModule(designSlot.ModuleUID, designSlot.Orientation, designSlot.Facing);
                if (!ModuleGrid.ModuleFitsAtSlot(targetSlot, newModule, logFailure: true))
                    continue;

                ModuleGrid.InstallModule(targetSlot, newModule, designSlot.Orientation);

                if (designSlot.Module?.ModuleType == ShipModuleType.Hangar)
                    designSlot.Module.hangarShipUID = designSlot.SlotOptions;
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
            var oldRole = Role;
            Role = new RoleData(ActiveHull, ModuleGrid.CopyModulesList()).DesignRole;

            if (Role != oldRole && showRoleChangeTip)
            {
                Vector2 pos = new Vector2(ScreenCenter.X-100, ModuleSelectComponent.Y + 50);
                RoleData.CreateDesignRoleToolTip(Role, DesignRoleRect, true, pos);
            }
        }

        public bool IsBadModuleSize(ShipModule module)
        {
            foreach (SlotStruct slot in ModuleGrid.SlotsList)
            {
                ShipModule tiltedModule = CreateDesignModule(module.UID, ModuleOrientation.Right, slot.Facing);
                if (ModuleGrid.ModuleFitsAtSlot(slot, module) || ModuleGrid.ModuleFitsAtSlot(slot, tiltedModule))
                    return false;
            }
            return true;
        }

        public override void Update(UpdateTimes elapsed, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            float zoom = MathHelper.SmoothStep(Camera.Zoom, TransitionZoom, 0.2f);

            // this crappy fix is to try and prevent huge jumps in z axis when camera zoom becomes very small. 
            // at about 0.1 the zoom zaxis change jumps uncontrollably. 
            if (zoom < 0.1f)
            {
                TransitionZoom = Math.Max(zoom-0.01f, TransitionZoom);
            }
            
            Camera.Zoom = zoom;
            if (Camera.Zoom < 0.03f) Camera.Zoom = 0.03f;
            if (Camera.Zoom > 2.65f) Camera.Zoom = 2.65f;

            CameraPosition.Z = (OriginalZ / Camera.Zoom);
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
            InitializeHullAndCamera();
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
                ScreenManager.AddScreen(new ShipDesignSaveScreen(this, ActiveHull.Name, hullDesigner:HullEditMode));
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
            CarrierOnlyCheckBox = Checkbox(carrierOnlyPos, () => ActiveHull.CarrierShip, "Carrier Only", GameText.WhenMarkedThisShipCan);

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
                    if (ResourceManager.Hull(hull, out ShipData hullData))
                    {
                        if ((!hullData.IsShipyard || Empire.Universe.Debug))
                        {
                            AvailableHulls.Add(hullData);
                        }
                    }
                }
            }
        }

        void InitializeHullAndCamera()
        {
            Offset = new Vector2(Viewport.Width / 2 - 256, Viewport.Height / 2 - 256);
            Camera = new Camera2D { Pos = new Vector2(Viewport.Width / 2f, Viewport.Height / 2f) };
            Vector3 camPos = CameraPosition * new Vector3(-1f, 1f, 1f);
            View = Matrix.CreateRotationY(180f.ToRadians())
                 * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), Vector3.Down);

            float aspectRatio = (float)Viewport.Width / Viewport.Height;
            Projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspectRatio, 1f, 120000f);
            
            ChangeHull(AvailableHulls[0]);

            float minX = 0f, maxX = 0f;
            for (int i = 0; i < ActiveHull.ModuleSlots.Length; ++i)
            {
                ModuleSlotData slot = ActiveHull.ModuleSlots[i];
                Vector2 topLeft = slot.Position;
                var botRight = new Vector2(topLeft.X + slot.Position.X * 16.0f,
                                           topLeft.Y + slot.Position.Y * 16.0f);

                if (topLeft.X < minX) minX = topLeft.X;
                if (botRight.X > maxX) maxX = botRight.X;
            }

            float hullWidth = maxX - minX;

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
            OriginalZ = CameraPosition.Z;
        }

        void InitializeShipHullsList()
        {
            var categories = new Array<string>();
            foreach (ShipData hull in AvailableHulls)
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

                foreach (ShipData hull in AvailableHulls)
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
