using System;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Data.Mesh;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed partial class ShipDesignScreen
    {
        Vector2 ClassifCursor;
        UICheckBox CarrierOnlyCheckBox;
        bool DisplayedBulkReplacementHint;

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
        }

        bool IsGoodDesign()
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
            RemoveObject(shipSO);
            shipSO = StaticMesh.GetSceneMesh(TransientContent, ActiveHull.ModelPath, ActiveHull.Animated);
            AddObject(shipSO);
        }

        void DoExit()
        {
            ReallyExit();
        }

        public override void ExitScreen()
        {
            bool goodDesign = IsGoodDesign();

            if (!ShipSaved && !goodDesign)
            {
                ExitMessageBox(this, SaveWIP, DoExit, GameText.ThisShipDesignIsNot);
                return;
            }
            if (ShipSaved || !goodDesign)
            {
                ReallyExit();
                return;
            }
            ExitMessageBox(this, SaveChanges, DoExit, GameText.YouHaveUnsavedChangesSave);
        }

        public void ExitToMenu(string launches)
        {
            ScreenToLaunch = launches;
            bool isEmptyDesign = ModuleGrid.IsEmptyDesign();

            bool goodDesign = IsGoodDesign();

            if (isEmptyDesign || (ShipSaved && goodDesign))
            {
                LaunchScreen();
                ReallyExit();
                return;
            }
            if (!ShipSaved && !goodDesign)
            {
                ExitMessageBox(this, SaveWIP, LaunchScreen, GameText.ThisShipDesignIsNot);
                return;
            }

            if (!ShipSaved && goodDesign)
            {
                ExitMessageBox(this, SaveChanges, LaunchScreen, GameText.YouHaveUnsavedChangesSave);
                return;
            }
            ExitMessageBox(this, SaveChanges, LaunchScreen, GameText.ThisShipDesignIsNot);
        }

        public override bool HandleInput(InputState input)
        {
            if (input.DebugMode)
            {
                HullEditMode = !HullEditMode;
                LoadContent();
                return true;
            }

            if (CategoryList.HandleInput(input))
                return true;

            if (HangarOptionsList.HandleInput(input))
                return true;

            if (DesignRoleRect.HitTest(input.CursorPosition))
                RoleData.CreateDesignRoleToolTip(Role, DesignRoleRect, false, Vector2.Zero);

            if (ActiveModule != null && !ActiveModule.DisableRotation) 
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

            EmpireUI.HandleInput(input, this);

            if (base.HandleInput(input)) // handle any buttons before any other selection logic
                return true;

            HandleInputZoom(input);
            HandleInputDebug(input);

            if (ArcsButton.R.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(GameText.TogglesTheWeaponFireArc, "Tab");

            if (ArcsButton.HandleInput(input))
            {
                ArcsButton.ToggleOn = !ArcsButton.ToggleOn;
                ShowAllArcs         = ArcsButton.ToggleOn;
                return true;
            }

            if (input.Tab && !input.IsAltKeyDown)
            {
                ShowAllArcs = !ShowAllArcs;
                ArcsButton.ToggleOn = ShowAllArcs;
                return true;
            }

            if (input.DesignMirrorToggled) // This is done only for the hotkey
            {
                OnSymmetricDesignToggle();
                return true;
            }

            HandleCameraMovement(input);

            if (HighlightedModule != null && HandleInputMoveArcs(input, HighlightedModule))
                return true;

            if (HandleModuleSelection(input))
                return true;

            HandleProjectedSlot(input);
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

        bool GetMirrorProjectedSlot(SlotStruct slot, int xSize, ModuleOrientation orientation, out SlotStruct projectedMirror)
        {
            if (GetMirrorSlot(slot, xSize, orientation, out MirrorSlot mirrored))
            {
                projectedMirror = mirrored.Slot;
                return true;
            }

            projectedMirror = default;
            return false;
        }

        bool GetMirrorSlot(SlotStruct slot, int xSize, ModuleOrientation orientation, out MirrorSlot mirrored)
        {
            int resolutionOffset = (int)slot.XMLPos.X - 256;
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
            if (input.MiddleMouseClick)
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
            if (input.MiddleMouseHeld())
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

        bool HandleModuleSelection(InputState input)
        {
            if (!ToggleOverlay)
                return false;

            SlotStruct slotStruct;
            if (HullEditMode)
            {
                if (input.LeftMouseClick)
                {
                    GameAudio.DesignSoftBeep();

                    if (Operation == SlotModOperation.Add)
                    {
                        AddHullSlot(input);
                        return true;
                    }

                    if (GetSlotUnderCursor(input, out slotStruct))
                    {
                        EditHullSlot(slotStruct.XMLPos, Operation);
                        return true;
                    }
                }
                return false;
            }

            if (!GetSlotUnderCursor(input, out slotStruct))
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
                GameAudio.DesignSoftBeep();

                SlotStruct slot = slotStruct.Parent ?? slotStruct;
                if (ActiveModule == null && slot.Module != null)
                {
                    SetActiveModule(slot.Module.UID, slot.Orientation, slot.Facing);
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
                DisplayBulkReplacementTip();
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

        void HandleProjectedSlot(InputState input)
        {
            GetSlotUnderCursor(input, out ProjectedSlot);
        }

        void DisplayBulkReplacementTip()
        {
            if (!DisplayedBulkReplacementHint && ModuleGrid.RepeatedReplaceActionsThreshold())
            {
                Vector2 pos = new Vector2(ModuleSelectComponent.X + ModuleSelectComponent.Width + 20, ModuleSelectComponent.Y + 100);
                ToolTip.CreateFloatingText(GameText.YouCanUseShiftClick, "", pos, 10);
                DisplayedBulkReplacementHint = true;
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
            if (!HullEditMode)
                return;

            if (input.KeyPressed(Keys.Enter))
            {
                ScreenManager.AddScreen(new ShipDesignSaveScreen(this, ActiveHull.Name, hullDesigner:true));
            }

            if (input.Right)
            {
                if (++Operation > SlotModOperation.Normal)
                    Operation = SlotModOperation.Delete;
            }
            else if (input.Left)
            {
                if (--Operation < SlotModOperation.Delete)
                    Operation = SlotModOperation.Normal;
            }
        }

        void HandleInputZoom(InputState input)
        {
            if (input.ScrollOut) TransitionZoom -= 0.1f;
            if (input.ScrollIn)  TransitionZoom += 0.1f;
            TransitionZoom = TransitionZoom.Clamped(0.03f, 2.65f);
        }

        bool HandleInputUndoRedo(InputState input)
        {
            if (input.Undo) { ModuleGrid.Undo(); RecalculateDesignRole(true); return true; }
            if (input.Redo) { ModuleGrid.Redo(); RecalculateDesignRole(true); return true; }
            return false;
        }

        void OnSymmetricDesignToggle()
        {
            IsSymmetricDesignMode    = !IsSymmetricDesignMode;
            BtnSymmetricDesign.Style = SymmetricDesignBtnStyle;
        }

        void OnFilterModuleToggle()
        {
            IsFilterOldModulesMode = !IsFilterOldModulesMode;
            BtnFilterModules.Style = FilterModulesBtnStyle;
            ModuleSelectComponent.ResetActiveCategory();
        }

        void OnStripShipToggle()
        {
            StripModules();
        }
        
        void JustChangeHull(ShipHull changeTo)
        {
            ShipSaved = true;
            ChangeHull(changeTo);
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
                        ScreenManager.AddScreen(new EmpireManagementScreen(Empire.Universe, EmpireUI));
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

        void OnHullListItemClicked(ShipHullListItem item)
        {
            if (item.Hull == null)
                return;

            GameAudio.AcceptClick();
            if (!ShipSaved && !IsGoodDesign() && !ModuleGrid.IsEmptyDesign())
            {
                MakeMessageBox(this, () => SaveWIPThenChangeHull(item.Hull), () => JustChangeHull(item.Hull),
                               GameText.ThisShipDesignIsNot, "Save", "No");
            }
            else
            {
                ChangeHull(item.Hull);
            }
        }

        // Gets the hull dimensions in world coordinate size
        Vector2 GetHullDimensions(ShipData hull)
        {
            float minX = 0f, maxX = 0f, minY = 0f, maxY = 0f;
            for (int i = 0; i < hull.ModuleSlots.Length; ++i)
            {
                ModuleSlotData slot = hull.ModuleSlots[i];
                Vector2 topLeft = slot.Position;
                Vector2 botRight = slot.Position + new Vector2(16f, 16f);

                if (topLeft.X  < minX) minX = topLeft.X;
                if (topLeft.Y  < minY) minY = topLeft.Y;
                if (botRight.X > maxX) maxX = botRight.X;
                if (botRight.Y > maxY) maxY = botRight.Y;
            }
            return new Vector2(maxX - minX, maxY - minY);
        }

        void UpdateViewMatrix(in Vector3 cameraPosition)
        {
            Vector3 camPos = cameraPosition * new Vector3(-1f, 1f, 1f);
            View = Matrix.CreateRotationY(180f.ToRadians())
                 * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), Vector3.Down);
        }

        float GetHullScreenSize(in Vector3 cameraPosition, float hullSize)
        {
            UpdateViewMatrix(cameraPosition);
            return ProjectToScreenSize(hullSize);
        }

        void ZoomCameraToEncloseHull(ShipData hull)
        {
            // This ensures our module grid overlay is the same size as the mesh
            CameraPosition.Z = 500;
            float hullHeight = GetHullDimensions(hull).Y;
            float visibleSize = GetHullScreenSize(CameraPosition, hullHeight);
            float ratio = visibleSize / hullHeight;
            CameraPosition.Z = (CameraPosition.Z * ratio).RoundUpTo(1);
            OriginalZ = CameraPosition.Z;

            // and now we zoom in the camera so the ship is all visible
            float desiredVisibleHeight = ScreenHeight * 0.75f;
            float currentVisibleHeight = GetHullScreenSize(CameraPosition, hullHeight);
            float newZoom = desiredVisibleHeight / currentVisibleHeight;
            TransitionZoom = newZoom.Clamped(0.03f, 2.65f);
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
            ScreenManager.AddScreen(new ShipDesignSaveScreen(this, ActiveHull.Name, hullDesigner:false));
            ShipSaved = true;
        }

        // Create full modules list for SAVING the design
        ModuleSlotData[] CreateModuleSlots()
        {
            int count = ModuleGrid.SlotsCount;
            var savedSlots = new ModuleSlotData[count];
            for (int i = 0; i < count; ++i)
            {
                savedSlots[i] = new ModuleSlotData(ModuleGrid.SlotsList[i]);
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

        ShipData CloneActiveDesign(string newName)
        {
            ShipData hull = ActiveHull.GetClone();
            hull.Name = newName;
            hull.ModuleSlots = CreateModuleSlots();
            return hull;
        }

        ShipHull CloneAsHull(string newName)
        {
            ShipData hull = ActiveHull.GetClone();
            hull.Name = newName;
            hull.Hull = newName;
            foreach (ModuleSlotData moduleSlotData in hull.ModuleSlots)
                moduleSlotData.ModuleUID = null;
            return new ShipHull(hull);
        }

        public void SaveShipDesign(string name)
        {
            ShipData toSave = CloneActiveDesign(name);
            SerializeShipDesign(toSave, $"{Dir.StarDriveAppData}/Saved Designs/{name}.xml");

            Ship newTemplate = ResourceManager.AddShipTemplate(toSave, fromSave: false, playerDesign: true);
            EmpireManager.Player.UpdateShipsWeCanBuild();
            if (!UnlockAllFactionDesigns && !EmpireManager.Player.WeCanBuildThis(newTemplate.Name))
                Log.Error("WeCanBuildThis check failed after SaveShipDesign");
            ChangeHull(newTemplate.shipData);
        }

        public void SaveHullDesign(string hullName)
        {
            ShipHull toSave = CloneAsHull(hullName);
            toSave.Save($"Content/Hulls/{ActiveHull.ShipStyle}/{hullName}.hull");

            ShipHull newHull = ResourceManager.AddHull(toSave);
            ChangeHull(newHull);
        }

        void SaveWIP()
        {
            ShipData toSave = CloneActiveDesign($"{DateTime.Now:yyyy-MM-dd}__{ActiveHull.Name}");
            SerializeShipDesign(toSave, $"{Dir.StarDriveAppData}/WIP/{toSave.Name}.xml");
        }

        void SaveWIPThenChangeHull(ShipHull changeTo)
        {
            SaveWIP();
            ChangeHull(changeTo);
        }
    }
}