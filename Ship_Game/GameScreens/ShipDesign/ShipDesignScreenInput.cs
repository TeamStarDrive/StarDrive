using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens;
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
            ShipDesign design = CurrentDesign;
            if (design.HullRole == RoleName.drone)
                design.IsCarrierOnly = true;

            if (CarrierOnlyCheckBox == null)
                return; // it is null the first time ship design screen is loaded

            CarrierOnlyCheckBox.Visible = design.HullRole != RoleName.drone
                                          && design.HullRole != RoleName.platform
                                          && design.HullRole != RoleName.station;
        }

        void BindListsToActiveHull()
        {
            ShipDesign design = CurrentDesign;
            CategoryList.Visible = design != null;
            HangarOptionsList.Visible = design != null;

            // bind hull editor to current hull
            HullEditor?.Initialize(CurrentHull);

            if (design == null)
                return;

            CategoryList.PropertyBinding = () => design.ShipCategory;

            if (design.ShipCategory == ShipCategory.Unclassified)
            {
                // Defaults based on hull types
                // Freighter hull type defaults to Civilian behaviour when the hull is selected, player has to actively opt to change classification to disable flee/freighter behaviour
                if (design.Role == RoleName.freighter)
                    CategoryList.SetActiveValue(ShipCategory.Civilian);
                // Scout hull type defaults to Recon behaviour. Not really important, as the 'Recon' tag is going to supplant the notion of having 'Fighter' class hulls automatically be scouts, but it makes things easier when working with scout hulls without existing categorisation.
                else if (design.Role == RoleName.scout)
                    CategoryList.SetActiveValue(ShipCategory.Recon);
                else
                    CategoryList.SetActiveValue(ShipCategory.Unclassified);
            }
            else
            {
                CategoryList.SetActiveValue(design.ShipCategory);
            }

            HangarOptionsList.PropertyBinding = () => design.HangarDesignation;
            HangarOptionsList.SetActiveValue(design.HangarDesignation);
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
            return (hasBridge || Role == RoleName.platform || Role == RoleName.station);
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

            if (ArcsButton.R.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(GameText.TogglesTheWeaponFireArc, "Tab");

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

            HandleCameraMovement(input);

            if (HighlightedModule != null && HandleInputMoveArcs(input, HighlightedModule))
                return true;

            (SlotUnderCursor, GridPosUnderCursor) = GetSlotUnderCursor();

            if (HandleModuleSelection(input, SlotUnderCursor))
                return true;

            ProjectedSlot = SlotUnderCursor;
            HandleDeleteModule(input, SlotUnderCursor);
            HandlePlaceNewModule(input, SlotUnderCursor);
            return false;
        }

        public (SlotStruct Slot, Point Pos) GetSlotUnderCursor()
        {
            Point gridPos = ModuleGrid.WorldToGridPos(CursorWorldPosition2D);
            return (ModuleGrid.Get(gridPos), gridPos);
        }

        void SetFiringArc(SlotStruct slot, float arc, bool round)
        {
            int turretAngle;
            if (!round) turretAngle = (int)Math.Round(arc);
            else        turretAngle = (int)Math.Round(arc / 15f) * 15;

            slot.Module.TurretAngle = turretAngle;
            if (IsSymmetricDesignMode && GetMirrorModule(slot, out ShipModule mirrored))
            {
                mirrored.TurretAngle = GetMirroredTurretAngle(turretAngle);
            }
        }

        void HandleCameraMovement(InputState input)
        {
            if (input.MiddleMouseClick)
            {
                StartDragPos = input.CursorPosition;
            }

            if (input.MiddleMouseHeld())
            {
                float dx = input.CursorPosition.X - StartDragPos.X;
                float dy = input.CursorPosition.Y - StartDragPos.Y;
                StartDragPos = input.CursorPosition;
                CameraPos.X += -dx;
                CameraPos.Y += -dy;
            }
            else
            {
                float limit = 2000f;
                float cameraPanSpeed = GlobalStats.CameraPanSpeed * 2;
                if (input.WASDLeft  && CameraPos.X > -limit) CameraPos.X -= cameraPanSpeed;
                if (input.WASDRight && CameraPos.X < +limit) CameraPos.X += cameraPanSpeed;
                if (input.WASDUp   && CameraPos.Y > -limit) CameraPos.Y -= cameraPanSpeed;
                if (input.WASDDown && CameraPos.Y < +limit) CameraPos.Y += cameraPanSpeed;
            }
        }

        bool HandleModuleSelection(InputState input, SlotStruct slotUnderCursor)
        {
            if (!ToggleOverlay || HullEditMode)
                return false;

            if (slotUnderCursor == null)
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

                SlotStruct slot = slotUnderCursor.Parent ?? slotUnderCursor;
                if (ActiveModule == null && slot.Module != null)
                {
                    SetActiveModule(slot.Module.UID, slot.Module.ModuleRot, slot.Module.TurretAngle, slot.Module.HangarShipUID);
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
                ShipModule highlighted = slotUnderCursor.Module ?? slotUnderCursor.Parent?.Module;
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
                    float arc = slotStruct.WorldPos.AngleToTarget(CursorWorldPosition2D);

                    if (Input.IsShiftKeyDown)
                    {
                        SetFiringArc(slotStruct, arc, round:false);
                        return true;
                    }

                    if (Input.IsAltKeyDown) // modify all turrets
                    {
                        int minAngle = int.MinValue;
                        int maxAngle = int.MinValue;
                        foreach (SlotStruct slot in ModuleGrid.SlotsList)
                        {
                            if (slot.Module != null && IsArcTurret(slot.Module))
                            {
                                int turretAngle = slot.Module.TurretAngle;
                                if (minAngle == int.MinValue) minAngle = maxAngle = turretAngle;
                                if (turretAngle > minAngle && turretAngle < arc) minAngle = turretAngle;
                                if (turretAngle < maxAngle && turretAngle > arc) maxAngle = turretAngle;
                            }
                        }

                        if (minAngle != int.MinValue)
                        {
                            highlighted.TurretAngle = (arc - minAngle) < (maxAngle - arc) ? minAngle : maxAngle;
                        }
                        changedArcs = true;
                    }
                    else
                    {
                        SetFiringArc(slotStruct, arc, round:true);
                        return true;
                    }
                }
            }
            return changedArcs;
        }

        void HandlePlaceNewModule(InputState input, SlotStruct slotUnderCursor)
        {
            if (!(input.LeftMouseClick || input.LeftMouseHeld()) || ActiveModule == null)
                return;

            if (slotUnderCursor == null)
            { 
                GameAudio.NegativeClick();
                return;
            }

            if (!input.IsShiftKeyDown)
            {
                GameAudio.SubBassMouseOver();
                InstallActiveModule(new SlotInstall(slotUnderCursor, ActiveModule));
                DisplayBulkReplacementTip();
            }
            else if (slotUnderCursor.ModuleUID != ActiveModule.UID || slotUnderCursor.Module?.HangarShipUID != ActiveModule.HangarShipUID)
            {
                GameAudio.SubBassMouseOver();
                ReplaceModulesWith(slotUnderCursor, ActiveModule); // ReplaceModules created by Fat Bastard
            }
            else
            {
                GameAudio.NegativeClick();
            }
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

        void HandleDeleteModule(InputState input, SlotStruct slotUnderCursor)
        {
            if (!input.RightMouseClick)
                return;

            if (slotUnderCursor != null)
                DeleteModuleAtSlot(slotUnderCursor);
            else
                ActiveModule = null;
        }

        void HandleInputZoom(InputState input)
        {
            if (input.ScrollOut) DesiredCamHeight *= 1.05f;
            if (input.ScrollIn)  DesiredCamHeight *= 0.95f;
            DesiredCamHeight = DesiredCamHeight.Clamped(1000, 5000);
        }

        bool HandleInputUndoRedo(InputState input)
        {
            if (input.Undo) { ModuleGrid.Undo(); return true; }
            if (input.Redo) { ModuleGrid.Redo(); return true; }
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

        void UpdateViewMatrix(in Vector3 cameraPosition)
        {
            Vector3 camPos = cameraPosition * new Vector3(-1f, 1f, 1f);
            var lookAt = new Vector3(camPos.X, camPos.Y, 0f);
            SetViewMatrix(Matrix.CreateRotationY(180f.ToRadians())
                        * Matrix.CreateLookAt(camPos, lookAt, Vector3.Down));
        }

        float GetHullScreenSize(in Vector3 cameraPosition, float hullSize)
        {
            UpdateViewMatrix(cameraPosition);
            return (float)ProjectToScreenSize(hullSize);
        }

        void ZoomCameraToEncloseHull()
        {
            // This ensures our module grid overlay is the same size as the mesh
            CameraPos.Z = 500;
            float hullHeight = (DesignedShip?.Radius ?? shipSO.WorldBoundingSphere.Radius) * 2;
            float visibleSize = GetHullScreenSize(CameraPos, hullHeight);
            float ratio = visibleSize / hullHeight;
            CameraPos.Z = (CameraPos.Z * ratio).RoundUpTo(1);

            // and now we zoom in the camera so the ship is all visible
            float wantedHeight = ScreenHeight * 0.75f;
            float currentHeight = GetHullScreenSize(CameraPos, hullHeight);

            float diff = wantedHeight - currentHeight;
            float camHeight = CameraPos.Z;

            // zoom in or out until we are past the desired visual height,
            // the scaling is not linear which is why we step through it with a loop
            while (Math.Abs(diff) > 20)
            {
                camHeight += diff < 0 ? 10 : -10;
                currentHeight = GetHullScreenSize(new Vector3(CameraPos.X, CameraPos.Y, camHeight), hullHeight);
                float newDiff = wantedHeight - currentHeight;
                if (diff < 0 && newDiff > 0 || diff > 0 && newDiff < 0)
                    break; // overshoot, quit the loop
                diff = newDiff;
            }

            UpdateViewMatrix(CameraPos);
            DesiredCamHeight = camHeight.Clamped(1000, 5000);
        }

        void ReallyExit()
        {
            RemoveVisibleMesh();

            // this should go some where else, need to find it a home
            ScreenManager.RemoveScreen(this);
            base.ExitScreen();
        }

        void SaveChanges()
        {
            ScreenManager.AddScreen(new ShipDesignSaveScreen(this, DesignOrHullName, hullDesigner:false));
            ShipSaved = true;
        }

        ShipDesign CloneCurrentDesign(string newName)
        {
            ShipDesign design = CurrentDesign.GetClone();
            design.Name = newName;
            design.SetDesignSlots(DesignSlot.FromModules(ModuleGrid.CopyModulesList()));
            return design;
        }

        ShipHull CloneCurrentHull(string newName)
        {
            ShipHull toSave = CurrentHull.GetClone();
            toSave.HullName = newName;
            toSave.VisibleName = newName;
            return toSave;
        }

        void SaveDesign(ShipDesign design, FileInfo designFile)
        {
            try
            {
                design.Save(designFile);
                ShipSaved = true;
                Log.Write($"Share it with your friends: {design.Name}\n{design.GetBase64DesignString()}\n");
            }
            catch (Exception e)
            {
                Log.Error(e, $"Failed to Save: '{design.Name}'");
            }
        }

        void SaveHull(ShipHull hull, FileInfo hullFile)
        {
            try
            {
                hull.Save(hullFile);
                ShipSaved = true;
            }
            catch (Exception e)
            {
                Log.Error(e, $"Failed to Save: '{hull.HullName}'");
            }
        }

        public void SaveShipDesign(string name, FileInfo overwriteProtected)
        {
            ShipDesign toSave = CloneCurrentDesign(name);
            SaveDesign(toSave, overwriteProtected ?? new FileInfo($"{Dir.StarDriveAppData}/Saved Designs/{name}.design"));

            bool playerDesign = overwriteProtected == null;
            bool readOnlyDesign = overwriteProtected != null;

            ResourceManager.AddShipTemplate(toSave, playerDesign: playerDesign, readOnly: readOnlyDesign);
            EmpireManager.Player.UpdateShipsWeCanBuild();
            if (!UnlockAllFactionDesigns && !EmpireManager.Player.WeCanBuildThis(toSave.Name))
                Log.Error("WeCanBuildThis check failed after SaveShipDesign");
            ChangeHull(toSave);
        }

        public void SaveHullDesign(string hullName, FileInfo overwriteProtected)
        {
            ShipHull toSave = CloneCurrentHull(hullName);
            SaveHull(toSave, overwriteProtected ?? new FileInfo($"Content/Hulls/{hullName}.hull"));

            ShipHull newHull = ResourceManager.AddHull(toSave);
            ChangeHull(newHull);
        }

        void SaveWIP()
        {
            if (CurrentDesign != null)
            {
                ShipDesign toSave = CloneCurrentDesign($"{DateTime.Now:yyyy-MM-dd}__{DesignOrHullName}");
                SaveDesign(toSave, new FileInfo($"{Dir.StarDriveAppData}/WIP/{toSave.Name}.design"));
            }
            else
            {
                ShipHull toSave = CloneCurrentHull($"{DateTime.Now:yyyy-MM-dd}__{DesignOrHullName}");
                SaveHull(toSave, new FileInfo($"{Dir.StarDriveAppData}/WIP/{toSave.VisibleName}.hull"));
            }
        }

        void SaveWIPThenChangeHull(ShipHull changeTo)
        {
            SaveWIP();
            ChangeHull(changeTo);
        }
    }
}
