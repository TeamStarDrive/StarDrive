using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens;
using Ship_Game.GameScreens.ShipDesignScreen;
using Ship_Game.Ships;
using Ship_Game.UI;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed partial class ShipDesignScreen
    {
        public void ChangeHull(ShipData hull)
        {
        #if SHIPYARD
            TotalI = TotalO = TotalE = TotalIO = TotalIE = TotalOE = TotalIOE = 0;
        #endif
            if (hull == null) return;
            ModSel.ResetLists();
            DesignStack.Clear();
            LastDesignActionPos = Vector2.Zero;
            LastActiveUID = "";

            RemoveObject(shipSO);
            ActiveHull = new ShipData
            {
                Animated     = hull.Animated,
                CombatState  = hull.CombatState,
                Hull         = hull.Hull,
                IconPath     = hull.ActualIconPath,
                ModelPath    = hull.HullModel,
                Name         = hull.Name,
                Role         = hull.Role,
                ShipStyle    = hull.ShipStyle,
                ThrusterList = hull.ThrusterList,
                ShipCategory = hull.ShipCategory,
                CarrierShip  = hull.CarrierShip,
                BaseHull     = hull.BaseHull
            };
            ActiveHull.UpdateBaseHull();

            CarrierOnly  = hull.CarrierShip;
            LoadCategory = hull.ShipCategory;
            Fml = true;
            Fmlevenmore = true;

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
            #if SHIPYARD
                if (data.Restrictions == Restrictions.I)   TotalI++;
                if (data.Restrictions == Restrictions.O)   TotalO++;
                if (data.Restrictions == Restrictions.E)   TotalE++;
                if (data.Restrictions == Restrictions.IO)  TotalIO++;
                if (data.Restrictions == Restrictions.IE)  TotalIE++;
                if (data.Restrictions == Restrictions.OE)  TotalOE++;
                if (data.Restrictions == Restrictions.IOE) TotalIOE++;
            #endif
            }
            CombatState = hull.CombatState;

            CreateSOFromActiveHull();
            UpdateActiveCombatButton();
            SetupSlots();
        }

        private bool CheckDesign()
        {
            bool hasBridge = false;
            foreach (SlotStruct slot in Slots)
            {
                if (slot.ModuleUID == null && slot.Parent == null)
                    return false; // empty slots not allowed!
                hasBridge |= slot.Module?.IsCommandModule == true;                
            }
            return (hasBridge || ActiveHull.Role == ShipData.RoleName.platform || ActiveHull.Role == ShipData.RoleName.station);
        }

       
        private void CreateSOFromActiveHull()
        {
            if (shipSO != null)
                RemoveObject(shipSO);

            shipSO = ResourceManager.GetSceneMesh(TransientContent, ActiveHull.ModelPath, ActiveHull.Animated);

            AddObject(shipSO);
            SetupSlots();
        }

        private void DoExit(object sender, EventArgs e)
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
            bool noSlotsFilled = true;
            foreach (SlotStruct slot in Slots)
            {
                if (slot.ModuleUID == null && slot.Parent == null) continue;
                noSlotsFilled = false;
                break;
            }

            bool goodDesign = CheckDesign();

            if (noSlotsFilled || (ShipSaved && goodDesign))
            {
                LaunchScreen(null, null);
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

        public bool IsMouseOverModule(InputState input, SlotStruct slot)
        {
            Vector2 moduleScreenPos = Camera.GetScreenSpaceFromWorldSpace(slot.Position);
            var moduleRect = new Rectangle((int)moduleScreenPos.X,     (int)moduleScreenPos.Y,
                                           (int)(16.0f * Camera.Zoom), (int)(16.0f * Camera.Zoom));

            return moduleRect.HitTest(input.CursorPosition);
        }

        public override bool HandleInput(InputState input)
        {
            CategoryList.HandleInput(input);
            CarrierOnlyBox.HandleInput(input);
            if (DesignRoleRect.HitTest(input.CursorPosition))
                ShipData.CreateDesignRoleToolTip(Role, Fonts.Arial12, DesignRoleRect, true);
            if (ActiveModule != null && ActiveModule.IsRotatable) 
            {
                if (input.ArrowLeft)  ChangeModuleState(ActiveModuleState.Left);
                if (input.ArrowRight) ChangeModuleState(ActiveModuleState.Right);
                if (input.ArrowDown)  ChangeModuleState(ActiveModuleState.Rear);
                if (input.ArrowUp)    ChangeModuleState(ActiveModuleState.Normal);
            }
            if (input.ShipDesignExit && !GlobalStats.TakingInput)
            {
                GameAudio.PlaySfxAsync("echo_affirm");
                ExitScreen();
                return true;
            }
            if (HandleInputUndo(input))
                return true;

            HandleInputZoom(input);
            HandleInputDebug(input);

            HoveredModule = null;
            EmpireUI.HandleInput(input, this);

            if (base.HandleInput(input)) // handle any buttons before any other selection logic
                return true;

            if (HandleShipHullListSelection(input))
                return true;

            if (ModSel.HandleInput(input, ActiveModule, HighlightedModule))
                return true;

            if (input.LeftMouseDown)
                if (HullSelectionRect.HitTest(input.CursorPosition) || ModSel.HitTest(input))
                    return true;

            if (ArcsButton.R.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(134);
            if (ArcsButton.HandleInput(input))
            {
                ArcsButton.ToggleOn = !ArcsButton.ToggleOn;
                ShowAllArcs = ArcsButton.ToggleOn;
            }
            if (input.Tab)
            {
                ShowAllArcs = !ShowAllArcs;
                ArcsButton.ToggleOn = ShowAllArcs;
            }

            HandleCameraMovement(input);

            if (input.Escaped)
            {
                ExitScreen();
                return true;
            }

            if (HandleModuleSelection(input))
                return true;

            HandleDeleteModule(input);
            HandlePlaceNewModule(input);
            HandleInputMoveArcs(input);
            return false;
        }

        private void HandleCameraMovement(InputState input)
        {
            if (input.RightMouseClick)
            {
                StartDragPos = input.CursorPosition;
                CameraVelocity.X = 0.0f;
            }

            int slotFactor = 150;
            if (Slots.Count / 2 > slotFactor) slotFactor = Slots.Count / 2;
            float camLimit = slotFactor + ((3 - Camera.Zoom) * slotFactor);
            Vector2 tempPos = Camera.WASDCamMovement(input, ScreenManager, camLimit); //This moves the grid
            CameraPosition.X = tempPos.X; //This moves the model
            CameraPosition.Y = tempPos.Y;
            //Log.Info("CamPosX: {0}  CamPosY: {1}  Camera.PosX: {2}  Camera.PosY: {3}  Zoom: {4}  Limit: {5}",
            //      CameraPosition.X, CameraPosition.Y, Camera.Pos.X, Camera.Pos.Y, Camera.Zoom, CamLimit);

            //i cant get this to work right. 
            //if (Input.MiddleMouseClick)
            //{
            //    Vector2 test = Camera.GetScreenSpaceFromWorldSpace(shipSO.WorldBoundingSphere.Center.ToVec2());          
            //    CameraPosition = test.ToVec3(TransitionZoom);
            //}
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

            CameraVelocity.X = MathHelper.Clamp(CameraVelocity.X, -10f, 10f);
            CameraVelocity.Y = MathHelper.Clamp(CameraVelocity.Y, -10f, 10f);
        }

        private bool HandleShipHullListSelection(InputState input)
        {
            HullSL.HandleInput(input);
            int max = HullSL.indexAtTop + HullSL.entriesToDisplay;
            for (int i = HullSL.indexAtTop; i < HullSL.Copied.Count && i < max; ++i)
            {
                ScrollList.Entry e = HullSL.Copied[i];
                if (e.item is ModuleHeader moduleHeader)
                {
                    if (moduleHeader.HandleInput(input, e))
                        return true;
                }
                else if (e.clickRect.HitTest(input.CursorPosition))
                {
                    selector = new Selector(e.clickRect);
                    e.clickRectHover = 1;
                    selector = new Selector(e.clickRect);
                    if (!input.InGameSelect) continue;
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    if (!ShipSaved && !CheckDesign())
                    {
                        Changeto = e.item as ShipData;
                        MakeMessageBox(this, JustChangeHull, SaveWIPThenChangeHull, 2121, "Save", "No");
                        return true;
                    }
                    ChangeHull(e.item as ShipData);
                    return true;
                }
                else e.clickRectHover = 0;
            }

            return false;
        }

        private bool HandleModuleSelection(InputState input)
        {
            if (!ToggleOverlay)
                return false;

            foreach (SlotStruct slotStruct in Slots)
            {
                if (!IsMouseOverModule(input, slotStruct))
                    continue;

                if (!input.LeftMouseHeld())
                {
                    HoveredModule = slotStruct.Module ?? slotStruct.Parent?.Module;
                }
                else if (HighlightedModule != null)
                {
                    HoveredModule = HighlightedModule;
                }

                if (input.LeftMouseReleased && !input.LeftMouseWasHeld)
                {
                    GameAudio.PlaySfxAsync("simple_beep");
                    if (Debug)
                    {
                        DebugAlterSlot(slotStruct.SlotReference.Position, Operation);
                        return true;
                    }

                    SlotStruct slot = slotStruct.Parent ?? slotStruct;
                    if (ActiveModule != null || slot.Module == null)
                        continue;
                    SetActiveModule(CreateDesignModule(slot.Module.UID));
                    ChangeModuleState(slot.State);
                    ActiveModule.hangarShipUID = slot.Module.hangarShipUID;
                    return true;
                }
                if (ActiveModule == null && !input.LeftMouseHeld())
                {
                    HighlightedModule = slotStruct.Parent?.Module ?? slotStruct.Module;
                }
            }
            return false;
        }

        private void HandleInputMoveArcs(InputState input)
        {
            foreach (SlotStruct slotStruct in Slots)
            {
                if (slotStruct.ModuleUID == null || HighlightedModule == null ||
                    (slotStruct.Module != HighlightedModule || !(slotStruct.Module.FieldOfFire > 0f)) ||
                    slotStruct.Module.ModuleType != ShipModuleType.Turret)
                    continue;

                //I am not sure what the below was trying to do. It wasnt doing anything...
                //Ok i remember what this does. it restricts the arc change 
                //float fieldOfFire = slotStruct.Module.FieldOfFire / 2f;
                //float angleToTarget = spaceFromWorldSpace.AngleToTarget(vector2);
                //float facing = HighlightedModule.Facing;
                //float angle = Math.Abs(angleToTarget - facing);
                //if (angle > fieldOfFire)
                //{
                //    if (angleToTarget > 180f)
                //        angleToTarget = -1f * (360f - angleToTarget);
                //    if (facing > 180f)
                //        facing = -1f * (360f - facing);
                //    angle = Math.Abs(angleToTarget - facing);
                //}

                if (input.ShipYardArcMove())
                {
                    Vector2 spaceFromWorldSpace = Camera.GetScreenSpaceFromWorldSpace(slotStruct.Center());
                    float arc = spaceFromWorldSpace.AngleToTarget(input.CursorPosition);
                    
                    if (Input.IsShiftKeyDown)
                    {
                        HighlightedModule.Facing = (float)Math.Round(arc);
                        return;
                    }

                    if (!Input.IsAltKeyDown)
                    {
                        HighlightedModule.Facing = (float)Math.Round(arc / 15f) * 15;
                        return;
                    }
                    float minCompare = float.MinValue;
                    float maxCompare = float.MaxValue;
                    foreach(SlotStruct slot in Slots)
                    {
                        if (slot.ModuleUID == null || slot.Tex == null || slot.Module.ModuleType != ShipModuleType.Turret) continue;
                        float facing = slot.Module.Facing;
                        if (facing > minCompare && facing < arc)
                        {                            
                            minCompare = slot.Module.Facing;
                        }
                        if (facing < maxCompare && facing > arc)
                        {
                            maxCompare = slot.Module.Facing;
                        }
                    }

                    HighlightedModule.Facing = arc - minCompare < maxCompare - arc ? minCompare : maxCompare;
                }
            }
        }

        private void HandlePlaceNewModule(InputState input)
        {
            if (!(input.LeftMouseClick || input.LeftMouseHeld()) || ActiveModule == null)
                return;

            foreach (SlotStruct slot in Slots)
            {
                if (!IsMouseOverModule(input, slot))
                    continue;

                GameAudio.PlaySfxAsync("sub_bass_mouseover");

                if (slot.PQ.X == (int) LastDesignActionPos.X && slot.PQ.Y == (int) LastDesignActionPos.Y &&
                    ActiveModule.UID == LastActiveUID) continue;

                // This will make the Ctrl+Z functionality in the shipyard a lot more responsive -Gretman
                InstallModule(slot); 

                LastDesignActionPos.X = slot.PQ.X;
                LastDesignActionPos.Y = slot.PQ.Y;
                LastActiveUID = ActiveModule.UID;
            }
        }

        private void HandleDeleteModule(InputState input)
        {
            if (!input.RightMouseClick)
                return;

            // this should actually clear slots
            ActiveModule = null;
            foreach (SlotStruct slot in Slots)
            {
                slot.SetValidity();
                if (!IsMouseOverModule(input, slot))
                    continue;

                bool slotModuleExists = slot.Module != null;
                if (!slotModuleExists && slot.Parent == null) continue;
                
                var designAction = DesignateSlotForAction(slotModuleExists ? slot : slot.Parent);
                DesignStack.Push(designAction);
                GameAudio.PlaySfxAsync("sub_bass_whoosh");
                ClearParentSlot(slotModuleExists ? slot : slot.Parent);
                RecalculatePower();
            }
        }

        private DesignAction DesignateSlotForAction(SlotStruct slot)
        {
            slot.SetValidity(slot.Module);
            var designAction = new DesignAction
            {
                clickedSS = new SlotStruct
                {
                    PQ            = slot.PQ,
                    Restrictions  = slot.Restrictions,
                    Facing        = slot.Module?.Facing ?? 0.0f,
                    ModuleUID     = slot.ModuleUID,
                    Module        = slot.Module,
                    SlotReference = slot.SlotReference
                }
            };
            return designAction;
        }

        private void HandleInputDebug(InputState input)
        {
            if (!Debug) return;
            if (input.WasKeyPressed(Keys.Enter))
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

        private void HandleInputZoom(InputState input)
        {
            if (!ModSel.HitTest(input) && !HullSelectionRect.HitTest(input.CursorPosition))
            {
                if (input.ScrollOut) TransitionZoom -= 0.1f;
                if (input.ScrollIn)  TransitionZoom += 0.1f;
                TransitionZoom = TransitionZoom.Clamp(0.3f, 2.65f);
            }
        }

        private bool HandleInputUndo(InputState input)
        {
            if (!input.Undo) return false;
            if (DesignStack.Count <= 0)
                return true;
            LastActiveUID = "";
            ShipModule shipModule = ActiveModule;
            DesignAction designAction = DesignStack.Pop();
            var slot1 = new SlotStruct();
            foreach (SlotStruct slot2 in Slots)
            {
                if (slot2.PQ == designAction.clickedSS.PQ)
                {
                    ClearSlotNoStack(slot2);
                    slot1 = slot2;
                    slot1.Facing = designAction.clickedSS.Facing;                      
                }
                foreach (SlotStruct slotStruct in designAction.AlteredSlots)
                {
                    if (slot2.PQ != slotStruct.PQ)
                        continue;
                    slot2.State = slotStruct.State;
                    ClearSlotNoStack(slot2);                    
                    break;
                }
            }
            if (designAction.clickedSS.ModuleUID != null)
            {
                ActiveModule = CreateDesignModule(designAction.clickedSS.ModuleUID);
                ActiveModule.Facing = slot1.Facing;
                ActiveModState = slot1.State;
                ChangeModuleState(slot1.State);
                InstallModuleNoStack(slot1);
            }
            foreach (SlotStruct slotStruct in designAction.AlteredSlots)
            {
                foreach (SlotStruct slot2 in Slots)
                {
                    if (slot2.PQ != slotStruct.PQ || slotStruct.ModuleUID == null)
                        continue;

                    ActiveModule = CreateDesignModule(slotStruct.ModuleUID);
                    ActiveModState = slotStruct.State;                    
                    slot2.Facing = slotStruct.Facing;
                    slot2.ModuleUID = slotStruct.ModuleUID;
                    ChangeModuleState(ActiveModState);
                    InstallModuleNoStack(slot2);
                }
            }
            ActiveModule = shipModule;
            ResetModuleState();
            return true;
        }

        private static CombatState CombatStateFromAction(ToggleButton button)
        {
            return (CombatState)Enum.Parse(typeof(CombatState), button.Action);
        }
        
        private void UpdateActiveCombatButton()
        {
            foreach (ToggleButton button in CombatStatusButtons)
                button.Active = (CombatState == CombatStateFromAction(button));
        }

        private void OnCombatButtonPressed(ToggleButton button)
        {
            if (ActiveHull == null)
                return;
            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
            CombatState = CombatStateFromAction(button);
            UpdateActiveCombatButton();
        }

        private void JustChangeHull(object sender, EventArgs e)
        {
            ShipSaved = true;
            ChangeHull(Changeto);
        }

        private void LaunchScreen(object sender, EventArgs e)
        {
            if (ScreenToLaunch != null)
            {
                switch (ScreenToLaunch)
                {
                    case "Research":
                        GameAudio.PlaySfxAsync("echo_affirm");
                        ScreenManager.AddScreen(new ResearchScreenNew(this, EmpireUI));
                        break;
                    case "Budget":
                        GameAudio.PlaySfxAsync("echo_affirm");
                        ScreenManager.AddScreen(new BudgetScreen(Empire.Universe));
                        break;
                    case "Main Menu":
                        GameAudio.PlaySfxAsync("echo_affirm");
                        ScreenManager.AddScreen(new GameplayMMScreen(Empire.Universe));
                        break;
                    case "Shipyard":
                        GameAudio.PlaySfxAsync("echo_affirm");
                        break;
                    case "Empire":
                        ScreenManager.AddScreen(new EmpireScreen(Empire.Universe, EmpireUI));
                        GameAudio.PlaySfxAsync("echo_affirm");
                        break;
                    case "Diplomacy":
                        ScreenManager.AddScreen(new MainDiplomacyScreen(Empire.Universe));
                        GameAudio.PlaySfxAsync("echo_affirm");
                        break;
                    case "?":
                        GameAudio.PlaySfxAsync("sd_ui_tactical_pause");
                        ScreenManager.AddScreen(new InGameWiki(this));
                        break;
                }
            }
            ReallyExit();
        }

        public override void LoadContent()
        {
            AssignLightRig("example/ShipyardLightrig");
            if (ScreenWidth  <= 1280 || ScreenHeight <= 768)
            {
                LowRes = true;
            }
            ModSel = new ModuleSelection(this, new Rectangle(5, (LowRes ? 45 : 100), 305, (LowRes ? 350 : 400)));
            foreach (KeyValuePair<string, bool> hull in EmpireManager.Player.GetHDict())
            {
                if (!hull.Value)
                    continue;
                AvailableHulls.Add(ResourceManager.HullsDict[hull.Key]);
            }

            PrimitiveQuad.Device = ScreenManager.GraphicsDevice;
            Offset = new Vector2(Viewport.Width / 2 - 256, Viewport.Height / 2 - 256);
            Camera = new Camera2D { Pos = new Vector2(Viewport.Width / 2f, Viewport.Height / 2f) };
            Vector3 camPos = CameraPosition * new Vector3(-1f, 1f, 1f);
            View = Matrix.CreateRotationY(180f.ToRadians())
                 * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));

            float aspectRatio = (float)Viewport.Width / Viewport.Height;
            Projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspectRatio, 1f, 20000f);
            
            ChangeHull(AvailableHulls[0]);
            CreateSOFromActiveHull();

            foreach (ModuleSlotData slot in ActiveHull.ModuleSlots)
            {
                if (slot.Position.X < LowestX)  LowestX  = slot.Position.X;
                if (slot.Position.X > HighestX) HighestX = slot.Position.X;
            }

            float hullWidth = HighestX - LowestX;

            // So, this attempts to zoom so the entire design is visible
            float UpdateCameraMatrix()
            {
                camPos = CameraPosition * new Vector3(-1f, 1f, 1f);

                View = Matrix.CreateRotationY(180f.ToRadians())
                     * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));

                Vector3 center   = Viewport.Project(Vector3.Zero, Projection, View, Matrix.Identity);
                Vector3 hullEdge = Viewport.Project(new Vector3(hullWidth, 0, 0), Projection, View, Matrix.Identity);
                return center.Distance(hullEdge) + 10f;
            }

            float visibleHullRadius = UpdateCameraMatrix();
            if (visibleHullRadius >= hullWidth)
            {
                while (visibleHullRadius > hullWidth)
                {
                    CameraPosition.Z += 10f;
                    visibleHullRadius = UpdateCameraMatrix();
                }
            }
            else
            {
                while (visibleHullRadius < hullWidth)
                {
                    CameraPosition.Z -= 10f;
                    visibleHullRadius = UpdateCameraMatrix();
                }
            }

            BlackBar = new Rectangle(0, ScreenHeight - 70, 3000, 70);
            SideBar = new Rectangle(0, 0, 280, ScreenHeight);
      
            ClassifCursor = new Vector2(ScreenWidth * .5f,
                    ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px").Height + 10);

            float ordersBarX = ClassifCursor.X - 15;
            var ordersBarPos = new Vector2(ordersBarX, ClassifCursor.Y + 20);
            void AddCombatStatusBtn(CombatState state, string iconPath, int toolTip)
            {
                var button = new ToggleButton(ordersBarPos, ToggleButtonStyle.Formation, iconPath)
                {
                    Action       = state.ToString(),
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
 
 
            BeginHLayout(ScreenWidth - 150f, ScreenHeight - 47f, -142);

            SaveButton = ButtonMedium(titleId:105, click: b =>
            {
                if (!CheckDesign()) {
                    GameAudio.PlaySfxAsync("UI_Misc20");
                    ScreenManager.AddScreen(new MessageBoxScreen(this, Localizer.Token(2049)));
                    return;
                }
                ScreenManager.AddScreen(new DesignManager(this, ActiveHull.Name));
            });

            LoadButton = ButtonMedium(titleId:8, click: b =>
            {
                ScreenManager.AddScreen(new LoadDesigns(this));
            });

            ToggleOverlayButton = ButtonMedium(titleId:106, clickSfx:"blip_click", click: b =>
            {
                ToggleOverlay = !ToggleOverlay;
            });

            Vector2 layoutEndV = EndLayout();
            SearchBar = new Rectangle((int)layoutEndV.X -142, (int)layoutEndV.Y, 210, 25);
            LoadContentFinish();
        }

        private void LoadContentFinish()
        {
            BottomSep = new Rectangle(BlackBar.X, BlackBar.Y, BlackBar.Width, 1);
            HullSelectionRect = new Rectangle(ScreenWidth - 285, (LowRes ? 45 : 100), 280, (LowRes ? 350 : 400));
            HullSelectionSub = new Submenu(HullSelectionRect, true);
            WeaponSL = new WeaponScrollList(ModSel, this);
            HullSelectionSub.AddTab(Localizer.Token(107));
            HullSL = new ScrollList(HullSelectionSub);
            var categories = new Array<string>();
            foreach (KeyValuePair<string, ShipData> hull in ResourceManager.HullsDict)
            {
                if ((hull.Value.IsShipyard && !Empire.Universe.Debug) || !EmpireManager.Player.GetHDict()[hull.Key])
                    continue;
                string cat = Localizer.GetRole(hull.Value.Role, EmpireManager.Player);
                if (!categories.Contains(cat))
                    categories.Add(cat);
            }

            categories.Sort();
            foreach (string cat in categories)
            {
                var type = new ModuleHeader(cat, 240f);
                HullSL.AddItem(type);
            }

            foreach (ScrollList.Entry e in HullSL.Entries)
            {
                foreach (KeyValuePair<string, ShipData> hull in ResourceManager.HullsDict)
                {
                    if ((hull.Value.IsShipyard && !Empire.Universe.Debug) || !EmpireManager.Player.GetHDict()[hull.Key] ||
                        ((ModuleHeader) e.item).Text != Localizer.GetRole(hull.Value.Role, EmpireManager.Player))
                    {
                        continue;
                    }

                    e.AddItem(hull.Value);
                }
            }

            var shipStatsPanel = new Rectangle(HullSelectionRect.X + 50,
                HullSelectionRect.Y + HullSelectionRect.Height - 20, 280, 320);

            DropdownRect = new Rectangle((int)(ScreenWidth * 0.25f), (int)ClassifCursor.Y + 20, 100, 18);

            CategoryList = new CategoryDropDown(this, DropdownRect);
            foreach (ShipData.Category item in Enum.GetValues(typeof(ShipData.Category)).Cast<ShipData.Category>())
                CategoryList.AddOption(item.ToString(), item);

            CarrierOnly = ActiveHull.CarrierShip;
            CoBoxCursor = new Vector2(DropdownRect.X + 106, DropdownRect.Y);
            CarrierOnlyBox = Checkbox(CoBoxCursor, () => CarrierOnly, "Carrier Only", 0);

            ShipStats = new Menu1(shipStatsPanel);
            StatsSub = new Submenu(shipStatsPanel);
            StatsSub.AddTab(Localizer.Token(108));
            ArcsButton = new GenericButton(new Vector2(HullSelectionRect.X - 32, 97f), "Arcs",
                Fonts.Pirulen20,
                Fonts.Pirulen16);

            Close = CloseButton(ScreenWidth - 27, 99);

            OriginalZ = CameraPosition.Z;
        }

        private void ReallyExit()
        {
            RemoveObject(shipSO);

            if (Empire.Universe?.LookingAtPlanet == true && Empire.Universe.workersPanel is ColonyScreen colonyScreen)
                colonyScreen.Reset = true;

            // this should go some where else, need to find it a home
            ScreenManager.RemoveScreen(this);
            base.ExitScreen();
        }

        public void ResetLists()
        {
            WeaponSL.ResetOnNextDraw = true;
            WeaponSL.indexAtTop = 0;
        }

        public void ResetModuleState()
        {
            ActiveModState = ActiveModuleState.Normal;
        }

        private void SaveChanges(object sender, EventArgs e)
        {
            ScreenManager.AddScreen(new DesignManager(this, ActiveHull.Name));
            ShipSaved = true;
        }

        private ModuleSlotData[] CreateModuleSlots()
        {
            var savedSlots = new ModuleSlotData[Slots.Count];
            for (int i = 0; i < Slots.Count; ++i)
            {
                SlotStruct slot = Slots[i];
                var savedSlot = new ModuleSlotData
                {
                    InstalledModuleUID = slot.ModuleUID ?? "Dummy",
                    Position           = slot.SlotReference.Position,
                    Restrictions       = slot.Restrictions,
                    Orientation        = slot.State.ToString()
                };
                if (slot.Module != null)
                {
                    savedSlot.Facing = slot.Module.Facing;
                    if (slot.Module.ModuleType == ShipModuleType.Hangar)
                        savedSlot.SlotOptions = slot.Module.hangarShipUID;
                }
                savedSlots[i] = savedSlot;
            }
            return savedSlots;
        }

        private void SerializeShipDesign(ShipData shipData, string designFile)
        {
            var serializer = new XmlSerializer(typeof(ShipData));
            using (var ws = new StreamWriter(designFile))
                serializer.Serialize(ws, shipData);
            ShipSaved = true;
        }

        public void SaveShipDesign(string name)
        {
            ShipData toSave = ActiveHull.GetClone();
            toSave.Name         = name;
            toSave.CombatState  = CombatState;
            toSave.ShipCategory = CategoryList.ActiveValue;
            toSave.CarrierShip  = CarrierOnly;
            toSave.ModuleSlots  = CreateModuleSlots();
            SerializeShipDesign(toSave, $"{Dir.ApplicationData}/StarDrive/Saved Designs/{name}.xml");

            Ship newTemplate = ResourceManager.AddShipTemplate(toSave, fromSave: false, playerDesign: true);
            EmpireManager.Player.UpdateShipsWeCanBuild();

            ActiveHull = newTemplate.shipData;
            ActiveHull.UpdateBaseHull();
            ChangeHull(ActiveHull);
        }

        private void SaveWIP(object sender, EventArgs e)
        {
            var toSave = new ShipData
            {
                Animated     = ActiveHull.Animated,
                CombatState  = CombatState,
                Hull         = ActiveHull.Hull,
                IconPath     = ActiveHull.ActualIconPath,
                ModelPath    = ActiveHull.ModelPath,
                Name         = $"{DateTime.Now:yyyy-MM-dd}__{ActiveHull.Name}",
                Role         = ActiveHull.Role,
                ShipStyle    = ActiveHull.ShipStyle,
                ThrusterList = ActiveHull.ThrusterList,
                BaseHull     = ActiveHull.BaseHull,
                ModuleSlots  = CreateModuleSlots()
            };
            SerializeShipDesign(toSave, $"{Dir.ApplicationData}/StarDrive/WIP/{toSave.Name}.xml");
        }

        private void SaveWIPThenChangeHull(object sender, EventArgs e)
        {
            SaveWIP(sender, e);
            ChangeHull(Changeto);
        }
        
        private void SetupSlots()
        {
            Slots.Clear();
            foreach (ModuleSlotData slot in ActiveHull.ModuleSlots)
            {
                Slots.Add(new SlotStruct(slot, Offset));
            }
            foreach (SlotStruct slot in Slots)
            {
                slot.SetValidity();
                if (slot.ModuleUID == null)
                    continue;
                ActiveModule = CreateDesignModule(slot.ModuleUID);
                ChangeModuleState(slot.State);
                if (ActiveModule.Area > 1)
                    ClearDestinationSlots(slot);                
                InstallModuleFromLoad(slot);
                if (slot.Module?.ModuleType != ShipModuleType.Hangar)
                    continue;
                slot.Module.hangarShipUID = slot.SlotOptions;
            }
            RecalculatePower();
            ActiveModule = null;
            ActiveModState = ActiveModuleState.Normal;
        }
    }
}