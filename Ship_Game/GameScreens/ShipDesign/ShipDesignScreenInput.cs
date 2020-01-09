using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Data.Mesh;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens;
using Ship_Game.GameScreens.ShipDesignScreen;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed partial class ShipDesignScreen
    {
        Vector2 ClassifCursor;
        UICheckBox CarrierOnlyCheckBox;
        public void ChangeHull(ShipData hull)
        {
            if (hull == null) return;
            ModSel.ResetLists();
            RemoveObject(shipSO);
            ActiveHull = new ShipData
            {
                Animated          = hull.Animated,
                CombatState       = hull.CombatState,
                Hull              = hull.Hull,
                IconPath          = hull.ActualIconPath,
                ModelPath         = hull.HullModel,
                Name              = hull.Name,
                Role              = hull.Role,
                ShipStyle         = hull.ShipStyle,
                ThrusterList      = hull.ThrusterList,
                ShipCategory      = hull.ShipCategory,
                HangarDesignation = hull.HangarDesignation,
                ShieldsBehavior   = hull.ShieldsBehavior,
                CarrierShip       = hull.CarrierShip,
                BaseHull          = hull.BaseHull
            };
            ActiveHull.UpdateBaseHull();

            ActiveHull.ModuleSlots = new ModuleSlotData[hull.ModuleSlots.Length];
            for (int i = 0; i < hull.ModuleSlots.Length; ++i)
            {
                ModuleSlotData hullSlot = hull.ModuleSlots[i];
                var data = new ModuleSlotData
                {
                    Position           = hullSlot.Position,
                    Restrictions       = hullSlot.Restrictions,
                    Facing             = hullSlot.Facing,
                    InstalledModuleUID = hullSlot.InstalledModuleUID,
                    Orientation        = hullSlot.Orientation,
                    SlotOptions        = hullSlot.SlotOptions
                };
                ActiveHull.ModuleSlots[i] = data;
            }

            BindListsToActiveHull();
            CreateSOFromActiveHull();
            UpdateActiveCombatButton();
            UpdateCarrierShip();
        }

        void UpdateCarrierShip()
        {
            if (ActiveHull.HullRole == ShipData.RoleName.drone)
                ActiveHull.CarrierShip = true;

            if (CarrierOnlyCheckBox == null)
                return; // it is null the first time ship design screen is loaded

            CarrierOnlyCheckBox.Visible = ActiveHull.HullRole != ShipData.RoleName.drone
                                          && ActiveHull.HullRole != ShipData.RoleName.platform
                                          && ActiveHull.HullRole != ShipData.RoleName.station;
        }

        void BindListsToActiveHull()
        {
            if (CategoryList == null)
                return;

            CategoryList.PropertyBinding = () => ActiveHull.ShipCategory;

            if (ActiveHull.ShipCategory == ShipData.Category.Unclassified)
            {
                // Defaults based on hull types
                // Freighter hull type defaults to Civilian behaviour when the hull is selected, player has to actively opt to change classification to disable flee/freighter behaviour
                if (ActiveHull.Role == ShipData.RoleName.freighter)
                    CategoryList.SetActiveValue(ShipData.Category.Civilian);
                // Scout hull type defaults to Recon behaviour. Not really important, as the 'Recon' tag is going to supplant the notion of having 'Fighter' class hulls automatically be scouts, but it makes things easier when working with scout hulls without existing categorisation.
                else if (ActiveHull.Role == ShipData.RoleName.scout)
                    CategoryList.SetActiveValue(ShipData.Category.Recon);
                else
                    CategoryList.SetActiveValue(ShipData.Category.Unclassified);
            }
            else
            {
                CategoryList.SetActiveValue(ActiveHull.ShipCategory);
            }


            HangarOptionsList.PropertyBinding = () => ActiveHull.HangarDesignation;
            HangarOptionsList.SetActiveValue(ActiveHull.HangarDesignation);

            if (GlobalStats.WarpBehaviorsEnabled) // FB: enable shield warp state
            {
                ShieldsBehaviorList.PropertyBinding = () => ActiveHull.ShieldsBehavior;
                ShieldsBehaviorList.SetActiveValue(ActiveHull.ShieldsBehavior);
            }
        }

        bool CheckDesign()
        {
            bool hasBridge = false;
            foreach (SlotStruct slot in ModuleGrid.SlotsList)
            {
                if (slot.ModuleUID == null && slot.Parent == null)
                    return false; // empty slots not allowed!
                hasBridge |= slot.Module?.IsCommandModule == true;                
            }
            return (hasBridge || ActiveHull.Role == ShipData.RoleName.platform || ActiveHull.Role == ShipData.RoleName.station);
        }


        void CreateSOFromActiveHull()
        {
            if (shipSO != null)
                RemoveObject(shipSO);

            shipSO = StaticMesh.GetSceneMesh(TransientContent, ActiveHull.ModelPath, ActiveHull.Animated);

            AddObject(shipSO);
            SetupSlots();
        }

        void DoExit()
        {
            ReallyExit();
        }

        public override void ExitScreen()
        {
            bool goodDesign = CheckDesign();

            if (!ShipSaved && !goodDesign)
            {
                ExitMessageBox(this, DoExit, SaveWIP, 2121);
                return;
            }
            if (ShipSaved || !goodDesign)
            {
                ReallyExit();
                return;
            }
            ExitMessageBox(this, DoExit, SaveChanges, 2137);
        }

        public void ExitToMenu(string launches)
        {
            ScreenToLaunch = launches;
            bool isEmptyDesign = ModuleGrid.IsEmptyDesign();

            bool goodDesign = CheckDesign();

            if (isEmptyDesign || (ShipSaved && goodDesign))
            {
                LaunchScreen();
                ReallyExit();
                return;
            }
            if (!ShipSaved && !goodDesign)
            {
                ExitMessageBox(this, LaunchScreen, SaveWIP, 2121);
                return;
            }

            if (!ShipSaved && goodDesign)
            {
                ExitMessageBox(this, LaunchScreen, SaveChanges, 2137);
                return;
            }
            ExitMessageBox(this, LaunchScreen, SaveChanges, 2121);
        }

        public override bool HandleInput(InputState input)
        {
            if (input.DebugMode)
            {
                LoadContent();
                return true;
            }

            if (CategoryList.HandleInput(input))
                return true;

            if (HangarOptionsList.HandleInput(input))
                return true;

            if (ShieldsBehaviorList.HandleInput(input))
                return true;

            if (DesignRoleRect.HitTest(input.CursorPosition))
                RoleData.CreateDesignRoleToolTip(Role, DesignRoleRect);

            if (ActiveModule != null && ActiveModule.IsRotatable) 
            {
                if (input.ArrowLeft)  { ReorientActiveModule(ModuleOrientation.Left);   return true; }
                if (input.ArrowRight) { ReorientActiveModule(ModuleOrientation.Right);  return true; }
                if (input.ArrowDown)  { ReorientActiveModule(ModuleOrientation.Rear);   return true; }
                if (input.ArrowUp)    { ReorientActiveModule(ModuleOrientation.Normal); return true; }
            }

            if (input.ShipDesignExit && !GlobalStats.TakingInput)
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }

            if (HandleInputUndoRedo(input))
                return true;

            HandleInputZoom(input);
            HandleInputDebug(input);

            EmpireUI.HandleInput(input, this);

            if (base.HandleInput(input)) // handle any buttons before any other selection logic
                return true;

            if (HandleShipHullListSelection(input))
                return true;

            if (ModSel.HandleInput(input, ActiveModule, HighlightedModule))
                return true;

            if (input.LeftMouseDown && (HullSelectionRect.HitTest(input.CursorPosition) || ModSel.HitTest(input)))
                return true;

            if (ArcsButton.R.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(134);

            if (ArcsButton.HandleInput(input))
            {
                ArcsButton.ToggleOn = !ArcsButton.ToggleOn;
                ShowAllArcs = ArcsButton.ToggleOn;
                return true;
            }

            if (input.Tab && !input.IsAltKeyDown)
            {
                ShowAllArcs = !ShowAllArcs;
                ArcsButton.ToggleOn = ShowAllArcs;
                return true;
            }

            if (input.DesignMirrorToggled)
            {
                OnSymmetricDesignToggle();
                return true;
            }

            HandleCameraMovement(input);

            if (input.Escaped)
            {
                ExitScreen();
                return true;
            }

            if (HighlightedModule != null && HandleInputMoveArcs(input, HighlightedModule))
                return true;

            if (HandleModuleSelection(input))
                return true;

            HandleDeleteModule(input);
            HandlePlaceNewModule(input);
            return false;
        }

        public bool GetSlotUnderCursor(InputState input, out SlotStruct slot)
        {
            Vector2 cursor = Camera.GetWorldSpaceFromScreenSpace(input.CursorPosition);
            return ModuleGrid.Get(new Point((int)cursor.X, (int)cursor.Y), out slot);
        }

        ModuleOrientation GetMirroredOrientation(ModuleOrientation orientation)
        {
            switch (orientation)
            {
                default:                      return orientation;
                case ModuleOrientation.Left:  return ModuleOrientation.Right;
                case ModuleOrientation.Right: return ModuleOrientation.Left;
            }
        }

        static float ConvertOrientationToFacing(ModuleOrientation orientation)
        {
            switch (orientation)
            {
                default:                       return 0;
                case ModuleOrientation.Left:   return 270;
                case ModuleOrientation.Right:  return 90;
                case ModuleOrientation.Rear:   return 180;
            }
        }
        
        bool GetMirrorSlot(SlotStruct slot, int xSize, ModuleOrientation orientation, out MirrorSlot mirrored)
        {
            int resolutionOffset = (int)slot.SlotReference.Position.X - 256;
            int center = slot.PQ.X - resolutionOffset;
            int mirrorOffset = (xSize - 1) * 16;
            int mirrorX;
            int x = slot.PQ.X;
            int y = slot.PQ.Y;
            if (x > center)
                mirrorX = center - 8 - mirrorOffset - (x - (center + 8));
            else
                mirrorX = center + 8 - mirrorOffset + (center - 8 - x);

            if (ModuleGrid.Get(new Point(mirrorX, y), out SlotStruct mirrorSS) &&
                Math.Abs(x - mirrorX) > mirrorOffset && // !overlapping
                slot.PQ.X != mirrorSS.PQ.X && // !overlapping
                slot.Root != mirrorSS.Root) // !overlapping @todo some of these checks may be redundant
            {
                mirrored = new MirrorSlot{ Slot = mirrorSS, Orientation = GetMirroredOrientation(orientation) };
                return true;
            }

            mirrored = default;
            return false;
        }
        
        bool GetMirrorSlotStruct(SlotStruct slot, out SlotStruct mirrored)
        {
            SlotStruct root = slot.Root;
            if (GetMirrorSlot(root, root.Module.XSIZE, root.Orientation, out MirrorSlot ms))
            {
                if (ms.Slot?.Module != null)
                {
                    mirrored = ms.Slot;
                    return true;
                }
            }
            mirrored = null;
            return false;
        }

        bool GetMirrorModule(SlotStruct slot, out ShipModule module)
        {
            if (GetMirrorSlotStruct(slot, out SlotStruct mirrored))
            {
                module = mirrored.Root.Module;
                if (module != null
                    && module.UID == slot.Module.UID
                    && module.XSIZE == slot.Module.XSIZE
                    && module.YSIZE == slot.Module.YSIZE)
                    return true;
            }
            module = null;
            return false;
        }

        void SetFiringArc(SlotStruct slot, float arc)
        {
            slot.Module.FacingDegrees = arc;
            if (IsSymmetricDesignMode && GetMirrorModule(slot, out ShipModule mirrored))
                mirrored.FacingDegrees = 360 - arc;
        }

        void HandleCameraMovement(InputState input)
        {
            if (input.RightMouseClick)
            {
                StartDragPos = input.CursorPosition;
                CameraVelocity.X = 0.0f;
            }

            int slotFactor = 150;
            if (ModuleGrid.SlotsCount / 2 > slotFactor)
                slotFactor = ModuleGrid.SlotsCount / 2;
            
            float camLimit = slotFactor + ((3 - Camera.Zoom) * slotFactor);
            Vector2 tempPos = Camera.WASDCamMovement(input, this, camLimit); //This moves the grid
            CameraPosition.X = tempPos.X; //This moves the model
            CameraPosition.Y = tempPos.Y;
            if (input.RightMouseHeld())
            {
                float num1 = input.CursorPosition.X - StartDragPos.X;
                float num2 = input.CursorPosition.Y - StartDragPos.Y;
                Camera.Pos += new Vector2(-num1, -num2);
                StartDragPos = input.CursorPosition;
                CameraPosition.X += -num1;
                CameraPosition.Y += -num2;
            }
            else
            {
                CameraVelocity.X = 0.0f;
                CameraVelocity.Y = 0.0f;
            }

            CameraVelocity.X = CameraVelocity.X.Clamped(-10f, 10f);
            CameraVelocity.Y = CameraVelocity.Y.Clamped(-10f, 10f);
        }

        bool HandleShipHullListSelection(InputState input)
        {
            HullSL.HandleInput(input);
            foreach (ScrollList.Entry e in HullSL.VisibleExpandedEntries)
            {
                if (e.item is ModuleHeader moduleHeader)
                {
                    if (moduleHeader.HandleInput(input, e))
                        return true;
                }
                else if (e.CheckHover(input))
                {
                    selector = e.CreateSelector();
                    if (!input.InGameSelect)
                        continue;
                    GameAudio.AcceptClick();
                    if (!ShipSaved && !CheckDesign() && !ModuleGrid.IsEmptyDesign())
                    {
                        Changeto = e.item as ShipData;
                        MakeMessageBox(this, JustChangeHull, SaveWIPThenChangeHull, 2121, "Save", "No");
                        return true;
                    }
                    ChangeHull(e.item as ShipData);
                    return true;
                }
            }
            return false;
        }

        bool HandleModuleSelection(InputState input)
        {
            if (!ToggleOverlay)
                return false;

            if (!GetSlotUnderCursor(input, out SlotStruct slotStruct))
            {
                // we clicked on empty space
                if (input.LeftMouseReleased)
                {
                    if (!input.LeftMouseWasHeldDown || input.LeftMouseHoldDuration < 0.1f)
                        HighlightedModule = null;
                }
                return false;
            }
            
            // mouse was released and we weren't performing ARC drag with left mouse down
            if (input.LeftMouseReleased && !input.LeftMouseHeldDown)
            {
                GameAudio.PlaySfxAsync("simple_beep");
                if (Debug)
                {
                    DebugAlterSlot(slotStruct.SlotReference.Position, Operation);
                    return true;
                }

                SlotStruct slot = slotStruct.Parent ?? slotStruct;
                if (ActiveModule == null && slot.Module != null)
                {
                    SetActiveModule(slot.Module, slot.Orientation, slot.Facing);
                    return true;
                }

                // we click on empty tile, clear current selection
                if (slot.Module == null)
                {
                    HighlightedModule = null;
                }
                return true;
            }

            if (ActiveModule == null && !input.LeftMouseHeld(0.1f))
            {
                ShipModule highlighted = slotStruct.Module ?? slotStruct.Parent?.Module;
                // RedFox: ARC ROTATE issue fix; prevents clearing highlighted module
                if (highlighted != null)
                    HighlightedModule = highlighted; 
            }
            return false;
        }

        static bool IsArcTurret(ShipModule module)
        {
            return module.ModuleType == ShipModuleType.Turret
                && module.FieldOfFire > 0f;
        }

        bool HandleInputMoveArcs(InputState input, ShipModule highlighted)
        {
            bool changedArcs = false;

            foreach (SlotStruct slotStruct in ModuleGrid.SlotsList)
            {
                if (slotStruct.Module == null ||
                    slotStruct.Module != highlighted ||
                    !IsArcTurret(slotStruct.Module))
                    continue;

                if (input.ShipYardArcMove())
                {
                    Vector2 spaceFromWorldSpace = Camera.GetScreenSpaceFromWorldSpace(slotStruct.Center);
                    float arc = spaceFromWorldSpace.AngleToTarget(input.CursorPosition);

                    if (Input.IsShiftKeyDown)
                    {
                        SetFiringArc(slotStruct, (float)Math.Round(arc));
                        return true;
                    }

                    if (!Input.IsAltKeyDown)
                    {
                        SetFiringArc(slotStruct, (float)Math.Round(arc / 15f) * 15);
                        return true;
                    }

                    float minFacing = float.NaN;
                    float maxFacing = float.NaN;
                    foreach(SlotStruct slot in ModuleGrid.SlotsList)
                    {
                        if (slot.Module?.ModuleType == ShipModuleType.Turret)
                        {
                            float facing = slot.Module.FacingDegrees;
                            if (float.IsNaN(minFacing)) minFacing = maxFacing = facing;
                            if (facing > minFacing && facing < arc) minFacing = facing;
                            if (facing < maxFacing && facing > arc) maxFacing = facing;
                        }
                    }

                    if (!float.IsNaN(minFacing))
                    {
                        highlighted.FacingDegrees = (arc - minFacing) < (maxFacing - arc) ? minFacing : maxFacing;
                    }
                    changedArcs = true;
                }
            }
            return changedArcs;
        }

        void HandlePlaceNewModule(InputState input)
        {
            if (!(input.LeftMouseClick || input.LeftMouseHeld()) || ActiveModule == null)
                return;

            if (!GetSlotUnderCursor(input, out SlotStruct slot))
            { 
                GameAudio.NegativeClick();
                return;
            }

            if (!input.IsShiftKeyDown)
            {
                GameAudio.SubBassMouseOver();
                InstallActiveModule(new SlotInstall(slot, ActiveModule, ActiveModState));
            }
            else if (slot.ModuleUID != ActiveModule.UID || slot.Module?.hangarShipUID != ActiveModule.hangarShipUID)
            {
                GameAudio.SubBassMouseOver();
                ReplaceModulesWith(slot, ActiveModule); // ReplaceModules created by Fat Bastard
            }
            else
            {
                GameAudio.NegativeClick();
            }
        }

        void HandleDeleteModule(InputState input)
        {
            if (!input.RightMouseClick)
                return;

            if (GetSlotUnderCursor(input, out SlotStruct slot))
                DeleteModuleAtSlot(slot);
            else
                ActiveModule = null;

        }

        void HandleInputDebug(InputState input)
        {
            if (!Debug) return;
            if (input.KeyPressed(Keys.Enter))
            {
                foreach (ModuleSlotData moduleSlotData in ActiveHull.ModuleSlots)
                    moduleSlotData.InstalledModuleUID = null;

                var serializer = new XmlSerializer(typeof(ShipData));
                using (var outStream = new StreamWriter($"Content/Hulls/{ActiveHull.ShipStyle}/{ActiveHull.Name}.xml"))
                    serializer.Serialize(outStream, ActiveHull);
            }
            if (input.Right)
                ++Operation;
            if (Operation > SlotModOperation.Normal)
                Operation = SlotModOperation.Delete;
        }

        void HandleInputZoom(InputState input)
        {
            if (ModSel.HitTest(input) || HullSelectionRect.HitTest(input.CursorPosition))
                return;
            if (input.ScrollOut) TransitionZoom -= 0.1f;
            if (input.ScrollIn)  TransitionZoom += 0.1f;
            TransitionZoom = TransitionZoom.Clamped(0.3f, 2.65f);
        }

        bool HandleInputUndoRedo(InputState input)
        {
            if (input.Undo) { ModuleGrid.Undo(); return true; }
            if (input.Redo) { ModuleGrid.Redo(); return true; }
            return false;
        }

        void OnSymmetricDesignToggle()
        {
            IsSymmetricDesignMode       = !IsSymmetricDesignMode;
            BtnSymmetricDesign.Text     = SymmetricDesignBtnText;
            BtnSymmetricDesign.Style    = SymmetricDesignBtnStyle;
        }

        void UpdateActiveCombatButton()
        {
            foreach (ToggleButton button in CombatStatusButtons)
                button.Active = (ActiveHull.CombatState == (CombatState)button.State);
        }

        void OnCombatButtonPressed(ToggleButton button)
        {
            if (ActiveHull == null)
                return;
            GameAudio.AcceptClick();
            ActiveHull.CombatState = (CombatState)button.State;
            UpdateActiveCombatButton();
        }

        void JustChangeHull()
        {
            ShipSaved = true;
            ChangeHull(Changeto);
        }

        void LaunchScreen()
        {
            if (ScreenToLaunch != null)
            {
                switch (ScreenToLaunch)
                {
                    case "Research":
                        GameAudio.EchoAffirmative();
                        ScreenManager.AddScreen(new ResearchScreenNew(this, EmpireUI));
                        break;
                    case "Budget":
                        GameAudio.EchoAffirmative();
                        ScreenManager.AddScreen(new BudgetScreen(Empire.Universe));
                        break;
                    case "Main Menu":
                        GameAudio.EchoAffirmative();
                        ScreenManager.AddScreen(new GameplayMMScreen(Empire.Universe));
                        break;
                    case "Shipyard":
                        GameAudio.EchoAffirmative();
                        break;
                    case "Empire":
                        ScreenManager.AddScreen(new EmpireScreen(Empire.Universe, EmpireUI));
                        GameAudio.EchoAffirmative();
                        break;
                    case "Diplomacy":
                        ScreenManager.AddScreen(new MainDiplomacyScreen(Empire.Universe));
                        GameAudio.EchoAffirmative();
                        break;
                    case "?":
                        GameAudio.TacticalPause();
                        ScreenManager.AddScreen(new InGameWiki(this));
                        break;
                }
            }
            ReallyExit();
        }

        public override void LoadContent()
        {
            Log.Info("ShipDesignScreen.LoadContent");
            RemoveAll();
            ModSel = new ModuleSelection(this, new Rectangle(5, (LowRes ? 45 : 100), 305, (LowRes ? 350 : 490)));

            var hulls = EmpireManager.Player.GetHDict();
            foreach (KeyValuePair<string, bool> hull in hulls)
                if (hull.Value && ResourceManager.Hull(hull.Key, out ShipData shipHull))
                    AvailableHulls.Add(shipHull);

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
            void AddCombatStatusBtn(CombatState state, string iconPath, int toolTip)
            {
                var button = new ToggleButton(ordersBarPos, ToggleButtonStyle.Formation, iconPath)
                {
                    State        = state,
                    HasToolTip   = true,
                    WhichToolTip = toolTip
                };
                button.OnClick += OnCombatButtonPressed;
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
            bottomList.Add(ButtonStyle.Medium, text:105, click: b =>
            {
                if (!CheckDesign()) {
                    GameAudio.NegativeClick();
                    ScreenManager.AddScreen(new MessageBoxScreen(this, Localizer.Token(2049)));
                    return;
                }
                ScreenManager.AddScreen(new DesignManager(this, ActiveHull.Name));
            });
            bottomList.Add(ButtonStyle.Medium, text:8, click: b =>
            {
                ScreenManager.AddScreen(new LoadDesigns(this));
            });
            bottomList.Add(ButtonStyle.Medium, text:106, click: b =>
            {
                ToggleOverlay = !ToggleOverlay;
            }).ClickSfx = "blip_click";
            BtnSymmetricDesign = bottomList.Add(ButtonStyle.Medium, text: SymmetricDesignBtnText, click: b =>
            {
                OnSymmetricDesignToggle();
            });
            BtnSymmetricDesign.ClickSfx = "blip_click";
            BtnSymmetricDesign.Tooltip  = Localizer.Token(1984);
            BtnSymmetricDesign.Style    = SymmetricDesignBtnStyle;

            SearchBar = new Rectangle((int)ScreenCenter.X, (int)bottomList.Y, 210, 25);
            LoadContentFinish();
            BindListsToActiveHull();

            AssignLightRig(LightRigIdentity.Shipyard, "example/ShipyardLightrig");
        }

        ButtonStyle SymmetricDesignBtnStyle  => IsSymmetricDesignMode ? ButtonStyle.Military : ButtonStyle.BigDip;
        LocalizedText SymmetricDesignBtnText => IsSymmetricDesignMode ? 1985 : 1986;

        void LoadContentFinish()
        {
            BottomSep = new Rectangle(BlackBar.X, BlackBar.Y, BlackBar.Width, 1);
            HullSelectionRect = new Rectangle(ScreenWidth - 285, (LowRes ? 45 : 100), 280, (LowRes ? 350 : 400));
            HullSelectionSub = new Submenu(HullSelectionRect);
            WeaponSL = new WeaponScrollList(ModSel, this);
            HullSelectionSub.AddTab(Localizer.Token(107));
            HullSL = new ScrollList(HullSelectionSub);
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
                HullSL.AddItem(new ModuleHeader(cat, 240));
            }

            foreach (ScrollList.Entry e in HullSL.AllEntries)
            {
                foreach (ShipData hull in ResourceManager.Hulls)
                {
                    if ((hull.IsShipyard && !Empire.Universe.Debug) || !EmpireManager.Player.IsHullUnlocked(hull.Hull) ||
                        ((ModuleHeader)e.item).Text != Localizer.GetRole(hull.Role, EmpireManager.Player))
                    {
                        continue;
                    }

                    e.AddSubItem(hull);
                }
            }

            var shipStatsPanel = new Rectangle(HullSelectionRect.X + 50,
                HullSelectionRect.Y + HullSelectionRect.Height - 20, 280, 320);

            var dropdownRect = new Rectangle((int)(ScreenWidth * 0.375f), (int)ClassifCursor.Y + 25, 125, 18);

            CategoryList = new CategoryDropDown(this, dropdownRect);
            foreach (ShipData.Category item in Enum.GetValues(typeof(ShipData.Category)).Cast<ShipData.Category>())
                CategoryList.AddOption(item.ToString(), item);

            var hangarRect = new Rectangle((int)(ScreenWidth * 0.65f), (int)ClassifCursor.Y + 25, 150, 18);
            HangarOptionsList = new HangarDesignationDropDown(this, hangarRect);
            foreach (ShipData.HangarOptions item in Enum.GetValues(typeof(ShipData.HangarOptions)).Cast<ShipData.HangarOptions>())
                HangarOptionsList.AddOption(item.ToString(), item);

            var behaviorRect    = new Rectangle((int)(ScreenWidth * 0.15f), (int)ClassifCursor.Y + 50, 150, 18);
            ShieldsBehaviorList = new ShieldBehaviorDropDown(this, behaviorRect);
            foreach (ShieldsWarpBehavior item in Enum.GetValues(typeof(ShieldsWarpBehavior)).Cast<ShieldsWarpBehavior>())
                ShieldsBehaviorList.AddOption(item.ToString(), item);
                
            var carrierOnlyPos  = new Vector2(dropdownRect.X - 200, dropdownRect.Y);
            CarrierOnlyCheckBox = Checkbox(carrierOnlyPos, () => ActiveHull.CarrierShip, "Carrier Only", 1978);

            ShipStats  = new Menu1(shipStatsPanel);
            StatsSub   = new Submenu(shipStatsPanel);
            StatsSub.AddTab(Localizer.Token(108));
            ArcsButton = new GenericButton(new Vector2(HullSelectionRect.X - 32, 97f), "Arcs", Fonts.Pirulen20, Fonts.Pirulen16);

            CloseButton(ScreenWidth - 27, 99);
            OriginalZ = CameraPosition.Z;
        }

        void ReallyExit()
        {
            RemoveObject(shipSO);

            // this should go some where else, need to find it a home
            ScreenManager.RemoveScreen(this);
            base.ExitScreen();
        }

        void SaveChanges()
        {
            ScreenManager.AddScreen(new DesignManager(this, ActiveHull.Name));
            ShipSaved = true;
        }

        ModuleSlotData[] CreateModuleSlots()
        {
            int count = ModuleGrid.SlotsCount;
            var savedSlots = new ModuleSlotData[count];
            for (int i = 0; i < count; ++i)
            {
                SlotStruct slot = ModuleGrid.SlotsList[i];
                var savedSlot = new ModuleSlotData
                {
                    InstalledModuleUID = slot.ModuleUID,
                    Position           = slot.SlotReference.Position,
                    Restrictions       = slot.Restrictions,
                    Orientation        = slot.Orientation.ToString()
                };
                if (slot.Module != null)
                {
                    savedSlot.Facing = slot.Module.FacingDegrees;
                    if (slot.Module.ModuleType == ShipModuleType.Hangar)
                        savedSlot.SlotOptions = slot.Module.hangarShipUID;
                }
                savedSlots[i] = savedSlot;
            }
            return savedSlots;
        }

        void SerializeShipDesign(ShipData shipData, string designFile)
        {
            var serializer = new XmlSerializer(typeof(ShipData));
            using (var ws = new StreamWriter(designFile))
                serializer.Serialize(ws, shipData);
            ShipSaved = true;
        }

        ShipData CloneActiveHull(string newName)
        {
            ShipData hull = ActiveHull.GetClone();
            hull.Name = newName;
            // save name of the mod, so we can ignore it in vanilla
            hull.ModName = GlobalStats.ActiveModInfo?.ModName;
            hull.ModuleSlots = CreateModuleSlots();
            return hull;
        }

        public void SaveShipDesign(string name)
        {
            ShipData toSave = CloneActiveHull(name);
            SerializeShipDesign(toSave, $"{Dir.StarDriveAppData}/Saved Designs/{name}.xml");

            Ship newTemplate = ResourceManager.AddShipTemplate(toSave, fromSave: false, playerDesign: true);
            EmpireManager.Player.UpdateShipsWeCanBuild();

            ActiveHull = newTemplate.shipData;
            ActiveHull.UpdateBaseHull();
            ChangeHull(ActiveHull);
        }

        void SaveWIP()
        {
            ShipData toSave = CloneActiveHull($"{DateTime.Now:yyyy-MM-dd}__{ActiveHull.Name}");
            SerializeShipDesign(toSave, $"{Dir.StarDriveAppData}/WIP/{toSave.Name}.xml");
        }

        void SaveWIPThenChangeHull()
        {
            SaveWIP();
            ChangeHull(Changeto);
        }
    }
}