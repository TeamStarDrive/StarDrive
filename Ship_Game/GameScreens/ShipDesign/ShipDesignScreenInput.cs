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
            Point gridPos = ModuleGrid.WorldToGridPos(CursorWorldPosition.ToVec2());
            return ModuleGrid.Get(gridPos, out slot);
        }

        void SetFiringArc(SlotStruct slot, int turretAngle)
        {
            slot.Module.TurretAngle = turretAngle;
            if (IsSymmetricDesignMode && GetMirrorModule(slot, out ShipModule mirrored))
            {
                mirrored.TurretAngle = GetMirroredTurretAngle(slot.Module.ModuleRot, slot.Module.TurretAngle);
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
                CameraPosition.X += -dx;
                CameraPosition.Y += -dy;
            }
            else
            {
                float limit = 2000f;
                if (input.WASDLeft  && CameraPosition.X > -limit) CameraPosition.X -= GlobalStats.CameraPanSpeed;
                if (input.WASDRight && CameraPosition.X < +limit) CameraPosition.X += GlobalStats.CameraPanSpeed;
                if (input.WASDUp   && CameraPosition.Y > -limit) CameraPosition.Y -= GlobalStats.CameraPanSpeed;
                if (input.WASDDown && CameraPosition.Y < +limit) CameraPosition.Y += GlobalStats.CameraPanSpeed;
            }
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
                        EditHullSlot(slotStruct, Operation);
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
                    SetActiveModule(slot.Module.UID, slot.ModuleRot, slot.TurrentAngle);
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
                    Vector2 worldCursor = CursorWorldPosition.ToVec2();
                    float arc = Center.AngleToTarget(worldCursor);

                    if (Input.IsShiftKeyDown)
                    {
                        SetFiringArc(slotStruct, (int)Math.Round(arc));
                        return true;
                    }

                    if (!Input.IsAltKeyDown)
                    {
                        SetFiringArc(slotStruct, (int)Math.Round(arc / 15f) * 15);
                        return true;
                    }

                    int minAngle = int.MinValue;
                    int maxAngle = int.MinValue;
                    foreach(SlotStruct slot in ModuleGrid.SlotsList)
                    {
                        if (slot.Module?.ModuleType == ShipModuleType.Turret)
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
            if (input.ScrollOut) DesiredCamHeight *= 1.05f;
            if (input.ScrollIn)  DesiredCamHeight *= 0.95f;
            DesiredCamHeight = DesiredCamHeight.Clamped(10f, 2000f);
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
            float hullHeight = hull.BaseHull.Size.Y * 16f;
            float visibleSize = GetHullScreenSize(CameraPosition, hullHeight);
            float ratio = visibleSize / hullHeight;
            CameraPosition.Z = (CameraPosition.Z * ratio).RoundUpTo(1);

            // and now we zoom in the camera so the ship is all visible
            float desiredVisibleHeight = ScreenHeight * 0.75f;
            float currentVisibleHeight = GetHullScreenSize(CameraPosition, hullHeight);
            float newZoom = currentVisibleHeight / desiredVisibleHeight;
            DesiredCamHeight = CameraPosition.Z * newZoom.Clamped(0.03f, 2.65f);
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
        DesignSlot[] CreateModuleSlots()
        {
            int count = ModuleGrid.SlotsCount;
            var savedSlots = new DesignSlot[count];
            for (int i = 0; i < count; ++i)
            {
                SlotStruct s = ModuleGrid.SlotsList[i];
                savedSlots[i] = new DesignSlot(s.GridPos, s.ModuleUID, s.Size, s.TurrentAngle,
                                               s.ModuleRot, s.SlotOptions);
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
            foreach (DesignSlot slot in hull.ModuleSlots)
                slot.ModuleUID = null;
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