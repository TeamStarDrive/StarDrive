using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

            foreach (ToggleButton button in CombatStatusButtons)
            {
                switch (button.Action)
                {
                    default: continue;
                    case "attack":          button.Active = CombatState == CombatState.AttackRuns;     break;
                    case "arty":            button.Active = CombatState == CombatState.Artillery;      break;
                    case "hold":            button.Active = CombatState == CombatState.HoldPosition;   break;
                    case "orbit_left":      button.Active = CombatState == CombatState.OrbitLeft;      break;
                    case "broadside_left":  button.Active = CombatState == CombatState.BroadsideLeft;  break;
                    case "broadside_right": button.Active = CombatState == CombatState.BroadsideRight; break;
                    case "short":           button.Active = CombatState == CombatState.ShortRange;     break;
                    case "evade":           button.Active = CombatState == CombatState.Evade;          break;
                    case "orbit_right":     button.Active = CombatState == CombatState.OrbitRight;     break;
                }
            }
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
                if (input.ArrowLeft)
                    ChangeModuleState(ActiveModuleState.Left);
                if (input.ArrowRight)
                    ChangeModuleState(ActiveModuleState.Right);
                if (input.ArrowDown)
                    ChangeModuleState(ActiveModuleState.Rear);
                if (input.ArrowUp)
                    ChangeModuleState(ActiveModuleState.Normal);
            }
            if (input.ShipDesignExit && !GlobalStats.TakingInput)
            {
                GameAudio.PlaySfxAsync("echo_affirm");
                ExitScreen();
            }
            if (Close.HandleInput(input))
            {
                ExitScreen();
                return true;
            }
            if (HandleInputUndo(input))
                return true;
            HandleInputZoom(input);

            HandleInputDebug(input);

            HoveredModule = null;
            var mousePos = input.CursorPosition;
            EmpireUI.HandleInput(input, this);
            HullSL.HandleInput(input);
            for (int index = HullSL.indexAtTop;
                index < HullSL.Copied.Count && index < HullSL.indexAtTop + HullSL.entriesToDisplay;
                ++index)
            {
                ScrollList.Entry e = HullSL.Copied[index];
                if (e.item is ModuleHeader moduleHeader)
                {
                    if (moduleHeader.HandleInput(input, e))
                        return true;
                }
                else if (e.clickRect.HitTest(mousePos))
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
                else
                    e.clickRectHover = 0;
            }
            if (ModSel.HandleInput(input, ActiveModule, HighlightedModule))
            {
                return true;
            }

            if (HullSelectionRect.HitTest(input.CursorPosition)
                && input.LeftMouseDown || ModSel.HitTest(input)
                && input.LeftMouseDown)
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
            if (input.RightMouseClick)
            {
                StartDragPos = input.CursorPosition;
                CameraVelocity.X = 0.0f;
            }
            int SlotFactor = 150;
            if (Slots.Count / 2 > SlotFactor) SlotFactor = Slots.Count / 2;
            float CamLimit = SlotFactor + ((3 - Camera.Zoom) * SlotFactor);
            Vector2 tempPos = Camera.WASDCamMovement(input, ScreenManager, CamLimit); //This moves the grid
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
            if (input.Escaped)
                ExitScreen();
            if (ToggleOverlay)
            {
                foreach (SlotStruct slotStruct in Slots)
                {
                    if (!IsMouseOverModule(input, slotStruct))
                        continue;

                    if (!input.LeftMouseHeld())
                    {
                        if (slotStruct.Module != null)
                            HoveredModule = slotStruct.Module;
                        else if (slotStruct.Parent != null)
                            HoveredModule = slotStruct.Parent.Module;
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
                        if (ActiveModule != null || slot.Module == null) continue;
                        SetActiveModule(CreateDesignModule(slot.Module.UID));
                        ChangeModuleState(slot.State);
                        ActiveModule.hangarShipUID = slot.Module.hangarShipUID;
                        return true;
                    }
                    else if (ActiveModule == null && !input.LeftMouseHeld())
                    {
                        HighlightedModule = slotStruct.Parent?.Module ?? slotStruct.Module;
                    }
                }
            }
 
            HandleIntputClearModule(input);
            HandleInputPlaceModule(input);
            HandleInputMoveArcs(input);
            UIButtonHandleInput(input);
            CheckToggleButton(input);
            return base.HandleInput(input);
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

        private void HandleInputPlaceModule(InputState input)
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

                //This will make the Ctrl+Z functionality in the shipyard a lot more responsive -Gretman
                InstallModule(slot); 

                LastDesignActionPos.X = slot.PQ.X;
                LastDesignActionPos.Y = slot.PQ.Y;
                LastActiveUID = ActiveModule.UID;
            }
        }

        private void HandleIntputClearModule(InputState input)
        {
            if (!input.RightMouseClick)
                return;

            //this should actually clear slots
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

        private void UIButtonHandleInput(InputState input)
        {
            Vector2 mousePos = input.CursorPosition;
            foreach (UIButton uiButton in Buttons)
            {
                if (!uiButton.Rect.HitTest(mousePos))
                {
                    uiButton.State = UIButton.PressState.Default;
                    continue;
                }
                uiButton.State = UIButton.PressState.Hover;
                if (input.LeftMouseClick)
                    uiButton.State = UIButton.PressState.Pressed;
                if (!input.LeftMouseReleased) continue;

                switch (uiButton.Launches)
                {
                    case "Toggle Overlay":
                        GameAudio.PlaySfxAsync("blip_click");
                        ToggleOverlay = !ToggleOverlay;
                        continue;
                    case "Save As...":
                        if (CheckDesign())
                        {
                            ScreenManager.AddScreen(new DesignManager(this, ActiveHull.Name));
                            continue;
                        }
                        else
                        {
                            GameAudio.PlaySfxAsync("UI_Misc20");
                            ScreenManager.AddScreen(new MessageBoxScreen(this, Localizer.Token(2049)));
                            continue;
                        }
                    case "Load":
                        ScreenManager.AddScreen(new LoadDesigns(this));
                        continue;
                    default:
                        continue;
                }
            }
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
                //&& !ChooseFighterSub.Menu.HitTest(input.CursorPosition))
            {
                if (input.ScrollOut)
                {
                    TransitionZoom -= 0.1f;
                    if (TransitionZoom < 0.300000011920929)
                        TransitionZoom = 0.3f;
                    if (TransitionZoom > 2.65000009536743)
                        TransitionZoom = 2.65f;
                }
                if (input.ScrollIn)
                {
                    TransitionZoom += 0.1f;
                    if (TransitionZoom < 0.300000011920929)
                        TransitionZoom = 0.3f;
                    if (TransitionZoom > 2.65000009536743)
                        TransitionZoom = 2.65f;
                }
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
        
        private void CheckToggleButton(InputState input)
        {
            if (ActiveHull == null) return;
            foreach (ToggleButton toggleButton in CombatStatusButtons)
            {
                if (toggleButton.HandleInput(input)) 
                {
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    switch (toggleButton.Action)
                    {
                        case "attack":          CombatState = CombatState.AttackRuns;     break;
                        case "arty":            CombatState = CombatState.Artillery;      break;
                        case "hold":            CombatState = CombatState.HoldPosition;   break;
                        case "orbit_left":      CombatState = CombatState.OrbitLeft;      break;
                        case "broadside_left":  CombatState = CombatState.BroadsideLeft;  break;
                        case "orbit_right":     CombatState = CombatState.OrbitRight;     break;
                        case "broadside_right": CombatState = CombatState.BroadsideRight; break;
                        case "evade":           CombatState = CombatState.Evade;          break;
                        case "short":           CombatState = CombatState.ShortRange;     break;
                    }
                }
                
                switch (toggleButton.Action)
                {
                    case "attack":          toggleButton.Active = CombatState == CombatState.AttackRuns;     break;
                    case "arty":            toggleButton.Active = CombatState == CombatState.Artillery;      break;
                    case "hold":            toggleButton.Active = CombatState == CombatState.HoldPosition;   break;
                    case "orbit_left":      toggleButton.Active = CombatState == CombatState.OrbitLeft;      break;
                    case "broadside_left":  toggleButton.Active = CombatState == CombatState.BroadsideLeft;  break;
                    case "orbit_right":     toggleButton.Active = CombatState == CombatState.OrbitRight;     break;
                    case "broadside_right": toggleButton.Active = CombatState == CombatState.BroadsideRight; break;
                    case "evade":           toggleButton.Active = CombatState == CombatState.Evade;          break;
                    case "short":           toggleButton.Active = CombatState == CombatState.ShortRange;     break;
                }
            }
        }

        private void JustChangeHull(object sender, EventArgs e)
        {
            ShipSaved = true;
            ChangeHull(Changeto);
        }

        private void LaunchScreen(object sender, EventArgs e)
        {
            string str = ScreenToLaunch;
            string str1 = str;
            if (str != null)
            {
                if (str1 == "Research")
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    ScreenManager.AddScreen(new ResearchScreenNew(this, EmpireUI));
                }
                else if (str1 == "Budget")
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    ScreenManager.AddScreen(new BudgetScreen(Empire.Universe));
                }
            }
            string str2 = ScreenToLaunch;
            string str3 = str2;
            if (str2 != null)
            {
                if (str3 == "Main Menu")
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    ScreenManager.AddScreen(new GameplayMMScreen(Empire.Universe));
                }
                else if (str3 == "Shipyard")
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                }
                else if (str3 == "Empire")
                {
                    ScreenManager.AddScreen(new EmpireScreen(Empire.Universe, EmpireUI));
                    GameAudio.PlaySfxAsync("echo_affirm");
                }
                else if (str3 == "Diplomacy")
                {
                    ScreenManager.AddScreen(new MainDiplomacyScreen(Empire.Universe));
                    GameAudio.PlaySfxAsync("echo_affirm");
                }
                else if (str3 == "?")
                {
                    GameAudio.PlaySfxAsync("sd_ui_tactical_pause");
                    var wiki = new InGameWiki(this)
                    {
                        TitleText = "StarDrive Help",
                        MiddleText =
                            "This help menu contains information on all of the gameplay systems contained in StarDrive. You can also watch one of several tutorial videos for a developer-guided introduction to StarDrive."
                    };
                    ScreenManager.AddScreen(wiki);
                }
            }
            ReallyExit();
        }

        public override void LoadContent()
        {
            AssignLightRig("example/ShipyardLightrig");
            if (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1280 || ScreenManager
                    .GraphicsDevice.PresentationParameters.BackBufferHeight <= 768)
            {
                LowRes = true;
            }
            //Rectangle leftRect = new Rectangle(5, 45, 405,
            //    ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 45 -
            //    (int) (0.4f * ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight) + 10);
            //ModuleSelectionMenu = new Menu1(leftRect);
            Rectangle modSelR   = new Rectangle(5, (LowRes ? 45 : 100), 305, (LowRes ? 350 : 400));
            ModSel = new ModuleSelection(this, modSelR);
            //ChooseFighterSL = ModSel.ChooseFighterSL;
            foreach (KeyValuePair<string, bool> hull in EmpireManager.Player.GetHDict())
            {
                if (!hull.Value)
                {
                    continue;
                }
                AvailableHulls.Add(ResourceManager.HullsDict[hull.Key]);
            }
            PrimitiveQuad.Device = ScreenManager.GraphicsDevice;
            float width                  = Viewport.Width;
            Viewport viewport            = Viewport;
            float aspectRatio            = width / viewport.Height;
            Offset                       = new Vector2();
            Viewport viewport1           = Viewport;
            Offset.X                     = viewport1.Width / 2 - 256;
            Viewport viewport2           = Viewport;
            Offset.Y                     = viewport2.Height / 2 - 256;
            Camera                       = new Camera2D();
            Camera2D vector2             = Camera;
            Viewport viewport3           = Viewport;
            float single                 = viewport3.Width / 2f;
            Viewport viewport4           = Viewport;
            vector2.Pos                  = new Vector2(single, viewport4.Height / 2f);
            Vector3 camPos               = CameraPosition * new Vector3(-1f, 1f, 1f);
            View                         = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(180f.ToRadians())) *
                         Matrix.CreateRotationX(0f.ToRadians())) * Matrix.CreateLookAt(camPos,
                            new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
            Projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspectRatio, 1f, 20000f);
            ChangeHull(AvailableHulls[0]);


            CreateSOFromActiveHull();



            foreach (ModuleSlotData slot in ActiveHull.ModuleSlots)
            {
                if (slot.Position.X < LowestX)
                {
                    LowestX = slot.Position.X;
                }
                if (slot.Position.X <= HighestX)
                {
                    continue;
                }
                HighestX = slot.Position.X;
            }
            float xDistance = HighestX - LowestX;
            Viewport viewport5 = Viewport;
            Vector3 pScreenSpace = viewport5.Project(Vector3.Zero, Projection, View, Matrix.Identity);
            var pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
            Vector2 radialPos = MathExt.PointOnCircle(90f, xDistance);
            Viewport viewport6 = Viewport;
            Vector3 insetRadialPos = viewport6.Project(new Vector3(radialPos, 0f), Projection, View,
                Matrix.Identity);
            Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
            float radius = Vector2.Distance(insetRadialSS, pPos) + 10f;
            if (radius >= xDistance)
            {
                while (radius > xDistance)
                {
                    camPos = CameraPosition * new Vector3(-1f, 1f, 1f);
                    View = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(180f.ToRadians())) *
                                 Matrix.CreateRotationX(0f.ToRadians())) * Matrix.CreateLookAt(camPos,
                                    new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
                    Viewport viewport7 = Viewport;
                    pScreenSpace = viewport7.Project(Vector3.Zero, Projection, View, Matrix.Identity);
                    pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    radialPos = MathExt.PointOnCircle(90f, xDistance);
                    Viewport viewport8 = Viewport;
                    insetRadialPos = viewport8.Project(new Vector3(radialPos, 0f), Projection, View,
                        Matrix.Identity);
                    insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
                    radius = Vector2.Distance(insetRadialSS, pPos) + 10f;
                    CameraPosition.Z = CameraPosition.Z + 1f;
                }
            }
            else
            {
                while (radius < xDistance)
                {
                    camPos = CameraPosition * new Vector3(-1f, 1f, 1f);
                    View = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(180f.ToRadians())) *
                                 Matrix.CreateRotationX(0f.ToRadians())) * Matrix.CreateLookAt(camPos,
                                    new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
                    Viewport viewport9 = Viewport;
                    pScreenSpace = viewport9.Project(Vector3.Zero, Projection, View, Matrix.Identity);
                    pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    radialPos = MathExt.PointOnCircle(90f, xDistance);
                    Viewport viewport10 = Viewport;
                    insetRadialPos = viewport10.Project(new Vector3(radialPos, 0f), Projection, View,
                        Matrix.Identity);
                    insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
                    radius = Vector2.Distance(insetRadialSS, pPos) + 10f;
                    CameraPosition.Z = CameraPosition.Z - 1f;
                }
            }
            BlackBar = new Rectangle(0,
                ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 70, 3000, 70);
            SideBar = new Rectangle(0, 0, 280,
                ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight);
      
     
            ClassifCursor =
                new Vector2(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * .5f,
                    ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px").Height + 10);
            var cursor = new Vector2(ClassifCursor.X, ClassifCursor.Y);
            Vector2 ordersBarPos = new Vector2(cursor.X, (int) cursor.Y + 20);
            ordersBarPos.X = ordersBarPos.X - 15;
            CombatStatusButton(ordersBarPos,"attack", "SelectionBox/icon_formation_headon",1);

            ordersBarPos.X = ordersBarPos.X + 29f;
            CombatStatusButton(ordersBarPos, "arty", "SelectionBox/icon_formation_aft", 2);

            ordersBarPos.X = ordersBarPos.X + 29f;
            CombatStatusButton(ordersBarPos, "short", "SelectionBox/icon_grid", 228);
    

            ordersBarPos.X = ordersBarPos.X + 29f;
            CombatStatusButton(ordersBarPos, "hold", "SelectionBox/icon_formation_x", 65);

            ordersBarPos.X = ordersBarPos.X + 29f;
            CombatStatusButton(ordersBarPos, "orbit_left", "SelectionBox/icon_formation_left", 3);

            ordersBarPos.Y = ordersBarPos.Y + 29f;
            CombatStatusButton(ordersBarPos, "broadside_left", "SelectionBox/icon_formation_bleft", 159);

            ordersBarPos.Y = ordersBarPos.Y - 29f;
            ordersBarPos.X = ordersBarPos.X + 29f;
            CombatStatusButton(ordersBarPos, "orbit_right", "SelectionBox/icon_formation_right", 4);

            ordersBarPos.Y = ordersBarPos.Y + 29f;
            CombatStatusButton(ordersBarPos, "broadside_right", "SelectionBox/icon_formation_bright", 160);
 
            ordersBarPos.Y = ordersBarPos.Y - 29f;
            ordersBarPos.X = ordersBarPos.X + 29f;
            CombatStatusButton(ordersBarPos, "evade", "SelectionBox/icon_formation_stop", 6);            
 
            cursor = new Vector2(
                ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 150f,
                (float) ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 47);

            BeginHLayout(cursor, -142);
            SaveButton          = ButtonMedium("Save As...", titleId: 105);            
            LoadButton          = ButtonMedium("Load", titleId: 8);            
            ToggleOverlayButton = ButtonMedium("Toggle Overlay", titleId: 106);
            Vector2 layoutEndV  = EndLayout();
            SearchBar           = new Rectangle((int)layoutEndV.X -142, (int)layoutEndV.Y, 210, 25);

            BottomSep = new Rectangle(BlackBar.X, BlackBar.Y, BlackBar.Width, 1);
            HullSelectionRect = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 285,
                                              (LowRes ? 45 : 100), 280, (LowRes ? 350 : 400));
            HullSelectionSub = new Submenu(HullSelectionRect, true);
            WeaponSL         = new WeaponScrollList(ModSel,this);
            HullSelectionSub.AddTab(Localizer.Token(107));
            HullSL = new ScrollList(HullSelectionSub);
            var categories = new Array<string>();
            foreach (KeyValuePair<string, ShipData> hull in ResourceManager.HullsDict)
            {
                if ((hull.Value.IsShipyard && !Empire.Universe.Debug) || !EmpireManager.Player.GetHDict()[hull.Key])
                {
                    continue;
                }
                string cat = Localizer.GetRole(hull.Value.Role, EmpireManager.Player);
                if (categories.Contains(cat))
                {
                    continue;
                }
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

            
            DropdownRect =
                new Rectangle((int) (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * .25f),
                    (int) ordersBarPos.Y, 100, 18);

            CategoryList = new CategoryDropDown(this, DropdownRect);
            foreach (ShipData.Category item in Enum.GetValues(typeof(ShipData.Category)).Cast<ShipData.Category>())
                CategoryList.AddOption(item.ToString(), item);

            CarrierOnly    = ActiveHull.CarrierShip;
            CoBoxCursor    = new Vector2(DropdownRect.X + 106, DropdownRect.Y);
            CarrierOnlyBox = Checkbox(CoBoxCursor, () => CarrierOnly, "Carrier Only", 0);

            ShipStats = new Menu1(shipStatsPanel);
            StatsSub  = new Submenu(shipStatsPanel);
            StatsSub.AddTab(Localizer.Token(108));
            ArcsButton = new GenericButton(new Vector2(HullSelectionRect.X - 32, 97f), "Arcs",
                Fonts.Pirulen20,
                Fonts.Pirulen16); 
            Close = new CloseButton(this, new Rectangle(
                ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 27, 99, 20, 20));
            OriginalZ = CameraPosition.Z;
        }

        private void CombatStatusButton(Vector2 ordersBarPos, string action, string iconPath, int toolTipIndex)
        {
            var toggleButton = new ToggleButton(new Rectangle((int) ordersBarPos.X, (int) ordersBarPos.Y, 24, 24),
                "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                "SelectionBox/button_formation_hover", "SelectionBox/button_formation_pressed", iconPath
            );
            CombatStatusButtons.Add(toggleButton);
            toggleButton.Action       = action;
            toggleButton.HasToolTip   = true;
            toggleButton.WhichToolTip = toolTipIndex;
        }



        private void ReallyExit()
        {
            RemoveObject(shipSO);

            if (Empire.Universe.LookingAtPlanet && Empire.Universe.workersPanel is ColonyScreen colonyScreen)
            {
                colonyScreen.Reset = true;
            }
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

        public void SaveShipDesign(string name)
        {
            ShipData toSave = ActiveHull.GetClone();
            toSave.ModuleSlots = new ModuleSlotData[Slots.Count];
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
                toSave.ModuleSlots[i] = savedSlot;
            }
            toSave.Name         = name;
            toSave.CombatState  = CombatState;
            toSave.ShipCategory = CategoryList.ActiveValue;
            toSave.CarrierShip  = CarrierOnly;

            var serializer = new XmlSerializer(typeof(ShipData));
            using (var ws = new StreamWriter($"{Dir.ApplicationData}/StarDrive/Saved Designs/{name}.xml"))
                serializer.Serialize(ws, toSave);
            ShipSaved = true;

            Ship newTemplate = ResourceManager.AddShipTemplate(toSave, fromSave: false, playerDesign: true);
            EmpireManager.Player.UpdateShipsWeCanBuild();

            ActiveHull = newTemplate.shipData;
            ActiveHull.UpdateBaseHull();
            ChangeHull(ActiveHull);
        }

        private void SaveWIP(object sender, EventArgs e)
        {
            var savedShip = new ShipData
            {
                Animated     = ActiveHull.Animated,
                CombatState  = ActiveHull.CombatState,
                Hull         = ActiveHull.Hull,
                IconPath     = ActiveHull.ActualIconPath,
                ModelPath    = ActiveHull.ModelPath,
                Name         = ActiveHull.Name,
                Role         = ActiveHull.Role,
                ShipStyle    = ActiveHull.ShipStyle,
                ThrusterList = ActiveHull.ThrusterList,
                ModuleSlots  = new ModuleSlotData[Slots.Count],
                BaseHull     = ActiveHull.BaseHull
            };
            for (int i = 0; i < Slots.Count; ++i)
            {
                SlotStruct slot = Slots[i];
                var data = new ModuleSlotData
                {
                    InstalledModuleUID = slot.ModuleUID,
                    Position           = slot.SlotReference.Position,
                    Restrictions       = slot.Restrictions,
                    Orientation        = slot.State.ToString()
                };
                if (slot.Module?.ModuleType == ShipModuleType.Hangar)
                    data.SlotOptions = slot.Module.hangarShipUID;
                savedShip.ModuleSlots[i] = data;
            }
            string path                = Dir.ApplicationData;
            CombatState defaultstate   = ActiveHull.CombatState;
            savedShip.CombatState      = CombatState;
            savedShip.Name             = $"{DateTime.Now:yyyy-MM-dd}__{ActiveHull.Name}";
            var serializer             = new XmlSerializer(typeof(ShipData));
            using (var writeFileStream = new StreamWriter($"{path}/StarDrive/WIP/{savedShip.Name}.xml"))
                serializer.Serialize(writeFileStream, savedShip);
            savedShip.CombatState = defaultstate;
            ShipSaved = true;
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
                {
                    continue;
                }
                ActiveModule = CreateDesignModule(slot.ModuleUID);
                ChangeModuleState(slot.State);
                if (ActiveModule.Area > 1)
                    ClearDestinationSlots(slot);                
                InstallModuleFromLoad(slot);
                //if (slot.Module.XSIZE * slot.Module.YSIZE > 1)
                //    ClearDestinationSlotsNoStack(slot);
                if(slot.Module?.ModuleType != ShipModuleType.Hangar)
                {
                    continue;
                }
                slot.Module.hangarShipUID = slot.SlotOptions;
            }
            //RecalculatePower();
            ActiveModule = null;
            ActiveModState = ActiveModuleState.Normal;
        }




    }
}