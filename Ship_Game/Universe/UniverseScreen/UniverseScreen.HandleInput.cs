using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Debug;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using Ship_Game.Audio;
using Ship_Game.Fleets;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        bool HandleGUIClicks(InputState input)
        {
            bool captured = DeepSpaceBuildWindow.HandleInput(input);
            if (aw.HandleInput(input))
                return true;

            if (MinimapDisplayRect.HitTest(input.CursorPosition) && !SelectingWithBox)
            {
                HandleScrolls(input);
                if (input.LeftMouseDown)
                {
                    Vector2 pos = input.CursorPosition - new Vector2(MinimapDisplayRect.X, MinimapDisplayRect.Y);
                    float num = MinimapDisplayRect.Width / (UniverseSize * 2);
                    CamDestination.X = -UniverseSize + (pos.X / num); //Fixed clicking on the mini-map on location with negative coordinates -Gretman
                    CamDestination.Y = -UniverseSize + (pos.Y / num);
                    snappingToShip = false;
                    ViewingShip = false;
                }
                captured = true;
            }

            // @note Make sure HandleInputs are called here
            if (!LookingAtPlanet)
            {
                captured |= SelectedShip != null && ShipInfoUIElement.HandleInput(input);
                captured |= SelectedPlanet != null && pInfoUI.HandleInput(input);
                captured |= SelectedShipList != null && shipListInfoUI.HandleInput(input);
            }

            if (SelectedSystem == null)
            {
                sInfoUI.SelectionTimer = 0.0f;
            }
            else
            {
                captured |= !LookingAtPlanet && sInfoUI.HandleInput(input);
            }

            if (minimap.HandleInput(input, this))
                return true;

            if (NotificationManager.HandleInput(input))
                return true;

            // @todo Why are these needed??
            captured |= ShipsInCombat.Rect.HitTest(input.CursorPosition);
            captured |= PlanetsInCombat.Rect.HitTest(input.CursorPosition);

            return captured;
        }

        void HandleInputNotLookingAtPlanet(InputState input)
        {
            mouseWorldPos = UnprojectToWorldPosition(input.CursorPosition);
            if (input.DeepSpaceBuildWindow) InputOpenDeepSpaceBuildWindow();
            if (input.FTLOverlay)       ToggleUIComponent("sd_ui_accept_alt3", ref showingFTLOverlay);
            if (input.RangeOverlay)     ToggleUIComponent("sd_ui_accept_alt3", ref showingRangeOverlay);
            if (input.AutomationWindow && !Debug) aw.ToggleVisibility();
            if (input.PlanetListScreen)  ScreenManager.AddScreen(new PlanetListScreen(this, EmpireUI, "sd_ui_accept_alt3"));
            if (input.ShipListScreen)    ScreenManager.AddScreen(new ShipListScreen(this, EmpireUI, "sd_ui_accept_alt3"));
            if (input.FleetDesignScreen) ScreenManager.AddScreen(new FleetDesignScreen(this, EmpireUI, "sd_ui_accept_alt3"));
            if (input.ZoomToShip) InputZoomToShip();
            if (input.ZoomOut) InputZoomOut();
            if (input.Escaped) DefaultZoomPoints();
            if (input.Tab) ShowShipNames = !ShowShipNames;

            HandleFleetSelections(input);
            HandleShipSelectionAndOrders();

            InputClickableItems(input);

            if (!LookingAtPlanet)
            {
                LeftClickOnClickableItem(input);
                ShipPieMenuClear();
                HandleSelectionBox(input);
            }

            HandleScrolls(input);

            if (Debug)
                HandleDebugEvents(input);
        }

        void HandleDebugEvents(InputState input)
        {
            Empire empire = EmpireManager.Player;
            if (input.EmpireToggle)
                empire = EmpireManager.Corsairs;

            if (input.SpawnShip)
                Ship.CreateShipAtPoint("Bondage-Class Mk IIIa Cruiser", empire, mouseWorldPos);
            if (input.SpawnFleet2) HelperFunctions.CreateFirstFleetAt("Fleet 2", empire, mouseWorldPos);
            if (input.SpawnFleet1) HelperFunctions.CreateFirstFleetAt("Fleet 1", empire, mouseWorldPos);

            if (SelectedShip != null)
            {
                if (input.KillThis)
                {
                    var damage = 1f;
                    if (input.IsShiftKeyDown)
                        damage = .9f;
                    //Apply damage as a percent of module health to all modules.
                    SelectedShip.DebugDamage(damage);
                }
            }
            else if (SelectedPlanet != null && Debug && (input.KillThis))
            {
                foreach (string troopType in ResourceManager.TroopTypes)
                    ResourceManager.CreateTroop(troopType, EmpireManager.Remnants).TryLandTroop(SelectedPlanet);
            }

            if (input.SpawnRemnant)
            {
                if (EmpireManager.Remnants == null)
                    Log.Warning("Remnant faction missing!");
                else
                    Ship.CreateShipAtPoint(input.EmpireToggle ? "Remnant Mothership" : "Target Dummy", EmpireManager.Remnants, mouseWorldPos);
            }

            if (input.ToggleSpatialManagerType)
                Spatial.ToggleSpatialType();

            if (input.IsShiftKeyDown && input.KeyPressed(Keys.B))
                StressTestShipLoading();
        }

        void HandleInputLookingAtPlanet(InputState input)
        {
            if (input.Tab)
                ShowShipNames = !ShowShipNames;

            var colonyScreen = workersPanel as ColonyScreen;
            bool dismiss = (input.Escaped || input.RightMouseClick) && colonyScreen?.ClickedTroop == false;
            if (dismiss || !workersPanel.IsActive)
            {
                AdjustCamTimer = 1f;
                if (returnToShip)
                {
                    ViewingShip = true;
                    returnToShip = false;
                    snappingToShip = true;
                    CamDestination.Z = transitionStartPosition.Z;
                }
                else
                {
                    CamDestination = transitionStartPosition;
                }
                transitionElapsedTime = 0.0f;
                LookingAtPlanet = false;
            }
        }

        void HandleFleetButtonClick(InputState input)
        {
            InputCheckPreviousShip();
            SelectedShip = null;
            SelectedShipList.Clear();
            SelectedFleet = null;
            lock (GlobalStats.FleetButtonLocker)
            {
                for (int i = 0; i < FleetButtons.Count; ++i)
                {
                    FleetButton fleetButton = FleetButtons[i];
                    if (!fleetButton.ClickRect.HitTest(input.CursorPosition))
                        continue;

                    SelectedFleet = fleetButton.Fleet;
                    SelectedShipList.Clear();
                    for (int j = 0; j < SelectedFleet.Ships.Count; j++)
                    {
                        Ship ship = SelectedFleet.Ships[j];
                        if (ship.inSensorRange)
                            SelectedShipList.AddUnique(ship);
                    }
                    if (SelectedShipList.Count == 1)
                    {
                        InputCheckPreviousShip(SelectedShipList[0]);
                        SelectedShip = SelectedShipList[0];
                        ShipInfoUIElement.SetShip(SelectedShip);
                        SelectedShipList.Clear();
                    }
                    else if (SelectedShipList.Count > 1)
                        shipListInfoUI.SetShipList(SelectedShipList, true);

                    SelectedSomethingTimer = 3f;

                    if (Input.LeftMouseDoubleClick)
                    {
                        ViewingShip = false;
                        AdjustCamTimer = 0.5f;
                        CamDestination = SelectedFleet.AveragePosition().ToVec3(CamPos.Z);
                        if (viewState < UnivScreenState.SystemView)
                            CamDestination.Z = GetZfromScreenState(UnivScreenState.SystemView);

                        CamDestination.Z = GetZfromScreenState(UnivScreenState.ShipView);
                        return;
                    }
                }
            }
        }

        public override bool HandleInput(InputState input)
        {
            Input = input;

            if (input.PauseGame && !GlobalStats.TakingInput)
                Paused = !Paused;

            if (input.DebugMode)
            {
                Debug = !Debug;
                foreach (SolarSystem solarSystem in SolarSystemList)
                {
                    solarSystem.SetExploredBy(player);
                    foreach (Planet planet in solarSystem.PlanetList)
                        planet.SetExploredBy(player);
                }
                GlobalStats.LimitSpeed = GlobalStats.LimitSpeed || Debug;
            }

            if (Debug)
            {
                if (input.ShowDebugWindow)
                {
                    DebugWin = (DebugWin == null) ? new DebugInfoScreen(this) : null;
                }
                if (DebugWin?.HandleInput(input) == true)
                    return true;
                if (input.GetMemory)
                {
                    GC.GetTotalMemory(false);
                }
            }

            // ensure universe has the correct light rig
            ResetLighting(forceReset: false);

            HandleEdgeDetection(input);
            if (HandleDragAORect(input))
                return true;

            if (HandleTradeRoutesDefinition(input))
                return true;

            // Handle new UIElementV2 items
            if (base.HandleInput(input))
                return true;

            for (int i = SelectedShipList.Count - 1; i >= 0; --i)
            {
                Ship ship = SelectedShipList[i];
                if (ship?.Active != true)
                    SelectedShipList.RemoveSwapLast(ship);
            }

            // CG: previous target code.
            if (previousSelection != null && input.PreviousTarget)
                PreviousTargetSelection(input);

            // fbedard: Set camera chase on ship
            if (input.ChaseCam)
                ChaseCam();

            if (input.CinematicMode)
                ToggleCinematicMode();

            ShowTacticalCloseup = input.TacticalIcons;

            if (input.QuickSave)
            {
                string saveName = $"Quicksave, {EmpireManager.Player.data.Traits.Name}, {StarDate.String()}";
                RunOnEmpireThread(() =>
                {
                    var savedGame = new SavedGame(this, saveName);
                });
            }

            if (input.UseRealLights)
            {
                UseRealLights = !UseRealLights; // toggle real lights
                ResetLighting(forceReset: true);
            }
            if (input.ShowExceptionTracker)
            {
                Paused = true;
                Log.OpenURL("https://bitbucket.org/codegremlins/stardrive-blackbox/issues/new");
            }
            if (input.SendKudos)
            {
                Paused = true;
                Log.OpenURL("http://steamcommunity.com/id/v-danbe/recommended/220660");
            }

            HandleGameSpeedChange(input);

            if (!LookingAtPlanet)
            {
                if (HandleGUIClicks(input))
                    return true;
            }
            else
            {
                SelectedFleet  = null;
                InputCheckPreviousShip();
                SelectedShip   = null;
                SelectedShipList.Clear();
                SelectedItem   = null;
                SelectedSystem = null;
            }

            if (input.ScrapShip && (SelectedItem != null && SelectedItem.AssociatedGoal.empire == player))
                HandleInputScrap(input);

            pickedSomethingThisFrame = false;

            ShipsInCombat.Visible   = !LookingAtPlanet;
            PlanetsInCombat.Visible = !LookingAtPlanet;

            if (LookingAtPlanet && workersPanel.HandleInput(input))
                return true;

            if (IsActive)
                EmpireUI.HandleInput(input);
            if (ShowingPlanetToolTip && input.CursorPosition.OutsideRadius(tippedPlanet.ScreenPos, tippedPlanet.Radius))
                ResetToolTipTimer(ref ShowingPlanetToolTip);

            if (ShowingSysTooltip && input.CursorPosition.OutsideRadius(tippedPlanet.ScreenPos, tippedSystem.Radius))
                ResetToolTipTimer(ref ShowingSysTooltip);

            if (!LookingAtPlanet)
            {
                HandleInputNotLookingAtPlanet(input);
            }
            else
            {
                HandleInputLookingAtPlanet(input);
            }

            if (input.InGameSelect && !pickedSomethingThisFrame && (!input.IsShiftKeyDown && !pieMenu.Visible))
                HandleFleetButtonClick(input);

            cState = SelectedShip != null || SelectedShipList.Count > 0 ? CursorState.Move : CursorState.Normal;
            if (SelectedShip == null && SelectedShipList.Count <= 0)
                return false;

            for (int i = 0; i < ClickableShipsList.Count; i++)
            {
                ClickableShip clickableShip = ClickableShipsList[i];
                if (input.CursorPosition.InRadius(clickableShip.ScreenPos, clickableShip.Radius))
                    cState = CursorState.Follow;
            }

            if (cState == CursorState.Follow)
                return false;

            lock (GlobalStats.ClickableSystemsLock)
            {
                for (int i = 0; i < ClickPlanetList.Count; i++)
                {
                    ClickablePlanets planets = ClickPlanetList[i];
                    if (input.CursorPosition.InRadius(planets.ScreenPos, planets.Radius) &&
                        planets.planetToClick.Habitable)
                        cState = CursorState.Orbit;
                }
            }
            return false;
        }

        static int InputFleetSelection(InputState input)
        {
            if (input.Fleet1) return 1;
            if (input.Fleet2) return 2;
            if (input.Fleet3) return 3;
            if (input.Fleet4) return 4;
            if (input.Fleet5) return 5;
            if (input.Fleet6) return 6;
            if (input.Fleet7) return 7;
            if (input.Fleet8) return 8;
            if (input.Fleet9) return 9;
            return -1;
        }

        void HandleFleetSelections(InputState input)
        {
            int index = InputFleetSelection(input);
            if (index == -1) 
                return;

            bool moveToFleet = SelectedFleet == player.GetFleetsDict()[index];

            // replace ships in fleet from selection
            // or remove selected fleet if not ships are selected.
            if (input.ReplaceFleet)
            {
                var selectedFleet = player.GetFleetsDict()[index];
                if (SelectedShipList.Count == 0)
                {
                    selectedFleet.Ships.ForEach(s => s?.UnsafeClearFleet());
                    return;
                }

                selectedFleet?.Ships.ForEach(s => s?.UnsafeClearFleet());
                // clear the fleet if pressing ctrl + the same fleet number
                if (selectedFleet == SelectedFleet && SelectedFleet?.Ships.Count > 0)
                {
                    selectedFleet?.Reset();
                    RecomputeFleetButtons(true);
                    return;
                }

                var newFleet   = AddSelectedShipsToNewFleet(SelectedShipList);
                string str     = Fleet.GetDefaultFleetNames(index);
                newFleet.Name  = str + " Fleet";
                newFleet.Owner = player;

                player.GetFleetsDict()[index] = newFleet;
                RecomputeFleetButtons(true);
            }
            else if (input.AddToFleet) // added by gremlin add ships to exiting fleet
            {
                if (SelectedShipList.Count == 0 )
                {
                    GameAudio.NegativeClick();
                    return;
                }

                Fleet fleet = null;
                var targetFleet = player.GetFleetsDict()[index];
                if (targetFleet?.Ships.Count > 0)
                {
                    // create a list of ships that are not part of the target fleet. 
                    var ships = new Array<Ship>(SelectedShipList.Filter(s => s.fleet != targetFleet));

                    // bail if there are no extra ships that are not already part of the target fleet. 
                    if (ships.Count == 0)
                    {
                        GameAudio.NegativeClick();
                        return;
                    }

                    // do the work.
                    fleet = AddShipsToExistingFleet(targetFleet, ships);
                }
                else
                {
                    fleet = AddSelectedShipsToNewFleet(SelectedShipList);
                    player.GetFleetsDict()[index] = fleet;
                }

                // do UI and fleet list maintenance. 
                SelectedFleet = fleet;

                SelectedShipList = new BatchRemovalCollection<Ship>(fleet.Ships);
                shipListInfoUI.SetShipList(SelectedShipList, true);  //fbedard:display new fleet in UI
                fleet.SetCommandShip(null);
                
                if (fleet.Name.Contains("Fleet") || fleet.Name.IsEmpty())
                {
                    string str = Fleet.GetDefaultFleetNames(index);

                    fleet.Name = str + " Fleet";
                }
                
                fleet.Update(FixedSimTime.Zero/*paused during init*/);

                RecomputeFleetButtons(true);
            }
            else // populate ship info UI with ships in fleet
            {
                SelectedPlanet = null;
                InputCheckPreviousShip();

                SelectedShip = null;
                Fleet fleet = player.GetFleetsDict()[index] ?? new Fleet();
                if (fleet.Ships.Count > 0)
                {
                    SelectedFleet = fleet;
                    GameAudio.FleetClicked();
                }
                else
                    SelectedFleet = null;
                SelectedShipList.Clear();
                foreach (Ship ship in fleet.Ships)
                {
                    SelectedShipList.Add(ship);
                    SelectedSomethingTimer = 3f;
                }
                if (SelectedShipList.Count == 1) //fbedard:display new fleet in UI
                {
                    InputCheckPreviousShip(SelectedShipList[0]);
                    SelectedShip = SelectedShipList[0];
                    ShipInfoUIElement.SetShip(SelectedShip);
                }
                else if (SelectedShipList.Count > 1)
                    shipListInfoUI.SetShipList(SelectedShipList, true);

                if (SelectedFleet != null && moveToFleet)
                {
                    ViewingShip    = false;
                    AdjustCamTimer = 0.5f;
                    CamDestination = SelectedFleet.AveragePosition().ToVec3();

                    if (CamHeight < GetZfromScreenState(UnivScreenState.SystemView))
                        CamDestination.Z = GetZfromScreenState(UnivScreenState.SystemView);
                }
            }
        }


        Ship CheckShipClick(InputState input)
        {
            foreach (ClickableShip clickableShip in ClickableShipsList)
            {
                if (input.CursorPosition.InRadius(clickableShip.ScreenPos, clickableShip.Radius))
                    return clickableShip.shipToClick;
            }
            return null;
        }

        Planet CheckPlanetClick()
        {
            lock (GlobalStats.ClickableSystemsLock)
                foreach (ClickablePlanets clickablePlanets in ClickPlanetList)
                {
                    if (Input.CursorPosition.InRadius(clickablePlanets.ScreenPos, clickablePlanets.Radius + 10.0f))
                        return clickablePlanets.planetToClick;
                }
            return null;
        }

        SolarSystem CheckSolarSystemClick()
        {
            lock (GlobalStats.ClickableSystemsLock)
                for (int x = 0; x < ClickableSystems.Count; ++x)
                {
                    ClickableSystem clickableSystem = ClickableSystems[x];
                    if (!clickableSystem.Touched(Input.CursorPosition)) continue;
                    return clickableSystem.systemToClick;
                }

            return null;
        }

        Fleet CheckFleetClicked()
        {
            foreach(ClickableFleet clickableFleet in ClickableFleetsList)
            {
                if (!Input.CursorPosition.InRadius(clickableFleet.ScreenPos, clickableFleet.ClickRadius)) continue;
                return clickableFleet.fleet;
            }
            return null;
        }

        ClickableItemUnderConstruction CheckBuildItemClicked()
        {
            lock (GlobalStats.ClickableItemLocker)
                for (int x = 0; x < ItemsToBuild.Count; ++x)
                {
                    ClickableItemUnderConstruction buildItem = ItemsToBuild[x];
                    if (buildItem == null || !Input.CursorPosition.InRadius(buildItem.ScreenPos,buildItem.Radius))
                        continue;
                    return buildItem;
                }

            return null;
        }

        bool ShipPieMenu(Ship ship)
        {
            if (ship == null || ship != SelectedShip || SelectedShip.Mothership != null ||
                SelectedShip.IsConstructor) return false;

            LoadShipMenuNodes(ship.loyalty == player ? 1 : 0);
            if (!pieMenu.Visible)
            {
                pieMenu.RootNode = shipMenu;
                pieMenu.Show(pieMenu.Position);
            }
            else
                pieMenu.ChangeTo(null);
            return true;
        }

        bool ShipPieMenuClear()
        {
            if (SelectedShip != null || SelectedShipList.Count != 0 || SelectedPlanet == null || !Input.ShipPieMenu)
                return false;
            if (!pieMenu.Visible)
            {
                pieMenu.RootNode = planetMenu;
                if (SelectedPlanet.Owner == null && SelectedPlanet.Habitable)
                    LoadMenuNodes(false, true);
                else
                    LoadMenuNodes(false, false);
                pieMenu.Show(pieMenu.Position);
            }
            else
                pieMenu.ChangeTo(null);
            return true;
        }

        bool UnselectableShip(Ship ship = null)
        {
            ship = ship ?? SelectedShip;
            if (!ship.IsConstructor && ship.shipData.Role != ShipData.RoleName.supply) return false;
            GameAudio.NegativeClick();
            return true;
        }

        // @note targetPlanet/targetShip are the attack/orbit etc targets

        bool SelectShipClicks(InputState input)
        {
            foreach (ClickableShip clickableShip in ClickableShipsList)
            {
                if (!clickableShip.HitTest(input.CursorPosition))
                    continue;
                if (clickableShip.shipToClick?.inSensorRange != true || pickedSomethingThisFrame)
                    continue;

                pickedSomethingThisFrame = true;
                GameAudio.ShipClicked();
                SelectedSomethingTimer = 3f;

                if (SelectedShipList.Count > 0 && input.IsShiftKeyDown)
                {
                    if (SelectedShipList.RemoveRef(clickableShip.shipToClick))
                        return true;
                    SelectedShipList.AddUniqueRef(clickableShip.shipToClick);
                    return false;
                }

                SelectedShipList.Clear();
                SelectedShipList.AddUniqueRef(clickableShip.shipToClick);
                SelectedShip = clickableShip.shipToClick;
                return true;
            }
            return false;
        }

        void LeftClickOnClickableItem(InputState input)
        {
            if (input.ShipPieMenu)
            {
                ShipPieMenu(SelectedShip);
            }

            pieMenu.HandleInput(input);
            if (!input.LeftMouseClick || pieMenu.Visible)
                return;

            if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                previousSelection = SelectedShip;

            SelectedShip    = null;
            SelectedPlanet  = null;
            SelectedFleet   = null;
            SelectedSystem  = null;
            SelectedItem    = null;
            Project.Started = false;

            if (viewState >= UnivScreenState.SectorView)
            {
                if ((SelectedSystem = CheckSolarSystemClick()) != null)
                {
                    sInfoUI.SetSystem(SelectedSystem);
                    return;
                }
            }

            if ((SelectedFleet = CheckFleetClicked()) != null)
            {
                SelectedShipList.Clear();
                shipListInfoUI.ClearShipList();
                pickedSomethingThisFrame = true;
                GameAudio.FleetClicked();
                SelectedShipList.AddRange(SelectedFleet.Ships);
                shipListInfoUI.SetShipList(SelectedShipList, false);
                return;
            }

            SelectShipClicks(input);

            if (SelectedShip != null && SelectedShipList.Count >0)
                ShipInfoUIElement.SetShip(SelectedShip);
            else if (SelectedShipList.Count > 1)
                shipListInfoUI.SetShipList(SelectedShipList, false);

            if (SelectedShipList.Count == 1)
            {
                LoadShipMenuNodes(SelectedShipList[0].loyalty == player ? 1 : 0);
                return;
            }

            if ((SelectedPlanet = CheckPlanetClick()) != null)
            {
                SelectedSomethingTimer = 3f;
                pInfoUI.SetPlanet(SelectedPlanet);
                if (input.LeftMouseDoubleClick)
                {
                    SnapViewColony(SelectedPlanet.Owner != EmpireManager.Player && !Debug);
                    SelectionBox = new Rectangle();
                }
                else
                    GameAudio.PlanetClicked();
                return;
            }

            if ((SelectedItem = CheckBuildItemClicked()) != null)
                GameAudio.BuildItemClicked();
        }

        void HandleSelectionBox(InputState input)
        {
            if (SelectedShipList.Count == 1)
            {
                if (SelectedShip != null && previousSelection != SelectedShip && SelectedShip != SelectedShipList[0])
                    previousSelection = SelectedShip;
                SelectedShip = SelectedShipList[0];
            }

            if (input.LeftMouseHeld(0.05f)) // we started dragging selection box
            {
                Vector2 a = input.StartLeftHold;
                Vector2 b = input.EndLeftHold;
                SelectionBox.X = (int)Math.Min(a.X, b.X);
                SelectionBox.Y = (int)Math.Min(a.Y, b.Y);
                SelectionBox.Width  = (int)Math.Max(a.X, b.X) - SelectionBox.X;
                SelectionBox.Height = (int)Math.Max(a.Y, b.Y) - SelectionBox.Y;
                SelectingWithBox = true;
                return;
            }

            if (!SelectingWithBox) // mouse released, but we weren't selecting
                return;

            if (SelectingWithBox) // trigger! mouse released after selecting
                SelectingWithBox = false;

            if (!GetAllShipsInArea(SelectionBox, out Array<Ship> ships,
                out bool purgeLoyalty, out bool purgeSupply, out Fleet fleet))
            {
                SelectionBox = new Rectangle(0, 0, -1, -1);
                return;
            }

            bool isFleet = fleet != null;
            SelectedPlanet = null;
            if (input.IsShiftKeyDown)
                SelectedShipList.AddRange(ships.Filter(s => !SelectedShipList.Contains(s)));
            else
                SelectedShipList = new BatchRemovalCollection<Ship>(ships);

            SelectedSomethingTimer = 3f;
            if (purgeLoyalty)
            {
                isFleet = false;
                foreach (Ship ship in SelectedShipList)
                {
                    if (ship.loyalty != player)
                    {
                        SelectedShipList.QueuePendingRemoval(ship);
                        continue;
                    }
                    isFleet = isFleet || (fleet?.ContainsShip(ship) ?? false);
                }
                SelectedShipList.ApplyPendingRemovals();
            }
            if (!input.IsShiftKeyDown)
            {
                if (isFleet)
                {
                    foreach (Ship ship in SelectedShipList)
                    {
                        if (ship?.fleet != fleet)
                            SelectedShipList.QueuePendingRemoval(ship);
                    }
                    SelectedShipList.ApplyPendingRemovals();
                }

                if (purgeSupply && !isFleet)
                {
                    foreach (Ship ship in SelectedShipList)
                    {
                        if (NonCombatShip(ship))
                            SelectedShipList.QueuePendingRemoval(ship);
                    }
                    SelectedShipList.ApplyPendingRemovals();
                }
            }


            shipListInfoUI.SetShipList(SelectedShipList, isFleet);
            SelectedFleet = isFleet ? fleet : null;

            if (SelectedShipList.Count == 1)
            {
                if (SelectedShip != null && previousSelection != SelectedShip &&
                    SelectedShip != SelectedShipList[0]) //fbedard
                    previousSelection = SelectedShip;
                SelectedShip = SelectedShipList[0];
                ShipInfoUIElement.SetShip(SelectedShip);
                LoadShipMenuNodes(SelectedShipList[0]?.loyalty == player ? 1 : 0);
            }

            SelectionBox = new Rectangle(0, 0, -1, -1);
        }

        bool NonCombatShip(Ship ship)
        {
            return ship != null && (ship.shipData.Role <= ShipData.RoleName.freighter ||
                ship.shipData.ShipCategory == ShipData.Category.Civilian ||
                ship.AI.State == AIState.Colonize);
        }

        bool GetAllShipsInArea(Rectangle screenArea, out Array<Ship> ships, out bool purgeLoyalty, out bool purgeType, out Fleet fleet)
        {
            ships              = new Array<Ship>();
            int playerShips    = 0;
            int nonCombatShips = 0;
            int fleetShips     = 0;
            fleet              = null;

            foreach (ClickableShip clickableShip in ClickableShipsList)
            {
                Ship ship = clickableShip.shipToClick;
                if (!screenArea.HitTest(clickableShip.ScreenPos)) continue;
                if (SelectedShipList.Contains(ship)) continue;
                ships.Add(ship);
                playerShips    += ship.loyalty == player ? 1 : 0;
                nonCombatShips += NonCombatShip(ship) ? 1 : 0;

                if (fleet == null)
                    fleet = ship.fleet;
                if (fleet != null && fleet == ship.fleet)
                    fleetShips++;

            }
            bool isFleet = fleet != null && fleet.CountShips == fleetShips;
            if (!isFleet) fleet = null;
            purgeLoyalty = playerShips != 0 && playerShips != ships.Count && !isFleet;
            purgeType    = nonCombatShips != 0 && nonCombatShips != ships.Count && !isFleet;

            return ships.Count > 0;
        }

        bool IsMouseHoveringOverPlanet;
        bool IsMouseHoveringOverSystem;

        public void UpdateClickableItems()
        {
            lock (GlobalStats.ClickableItemLocker)
                ItemsToBuild.Clear();

            EmpireAI playerAI = player.GetEmpireAI();
            for (int index = 0; index < playerAI.Goals.Count; ++index)
            {
                Goal goal = playerAI.Goals[index];
                if (goal.IsDeploymentGoal)
                {
                    const float radius = 100f;
                    Vector2 buildPos    = ProjectToScreenPosition(goal.BuildPosition);
                    Vector2 buildOffSet = ProjectToScreenPosition(goal.BuildPosition.PointFromAngle(90f, radius));
                    float clickableRadius = buildOffSet.Distance(buildPos) + 10f;
                    var underConstruction = new ClickableItemUnderConstruction
                    {
                        Radius = clickableRadius, BuildPos = goal.BuildPosition, ScreenPos = buildPos,
                        UID = goal.ToBuildUID, AssociatedGoal = goal
                    };

                    lock (GlobalStats.ClickableItemLocker)
                        ItemsToBuild.Add(underConstruction);
                }
            }

            IsMouseHoveringOverPlanet = false;
            lock (GlobalStats.ClickableSystemsLock)
            {
                for (int i = 0; i < ClickPlanetList.Count; ++i)
                {
                    ClickablePlanets planet = ClickPlanetList[i];
                    if (Input.CursorPosition.InRadius(planet.ScreenPos, planet.Radius))
                    {
                        IsMouseHoveringOverPlanet = true;
                        TooltipTimer -= 0.01666667f;
                        tippedPlanet = planet;
                    }
                }
            }

            IsMouseHoveringOverSystem = false;
            if (viewState > UnivScreenState.SectorView)
            {
                lock (GlobalStats.ClickableSystemsLock)
                {
                    for (int i = 0; i < ClickableSystems.Count; ++i)
                    {
                        ClickableSystem system = ClickableSystems[i];
                        if (Input.CursorPosition.InRadius(system.ScreenPos, system.Radius))
                        {
                            sTooltipTimer -= 0.01666667f;
                            tippedSystem = system;
                            IsMouseHoveringOverSystem = true;
                        }
                    }
                }
                if (sTooltipTimer <= 0f)
                    sTooltipTimer = 0.5f;
            }

            ShowingSysTooltip = IsMouseHoveringOverSystem;

            if (TooltipTimer <= 0f && !LookingAtPlanet)
            {
                TooltipTimer = 0.5f;
            }

            if (!IsMouseHoveringOverPlanet)
            {
                ShowingPlanetToolTip = false;
                TooltipTimer = 0.5f;
            }
        }

        bool HandleTradeRoutesDefinition(InputState input)
        {
            if (!DefiningTradeRoutes)
                return false;

            DefiningTradeRoutes = !DefiningAO;
            HandleScrolls(input); // allow exclusive scrolling during Trade Route define
            if (!LookingAtPlanet && HandleGUIClicks(input))
                return true;

            if (input.LeftMouseClick || input.RightMouseClick)
                InputPlanetsForTradeRoutes(input); // add or remove a planet from the list

            if (SelectedShip == null || input.Escaped) // exit the trade routes mode
            {
                DefiningTradeRoutes = false;
                return true;
            }
            return true;
        }

        void InputPlanetsForTradeRoutes(InputState input)
        {
            if (viewState > UnivScreenState.SystemView)
                return;

            foreach (ClickablePlanets planets in ClickPlanetList)
            {
                if (input.CursorPosition.InRadius(planets.ScreenPos, planets.Radius))
                {
                    if (input.LeftMouseClick)
                    {
                        if (SelectedShip.AddTradeRoute(planets.planetToClick))
                            GameAudio.AcceptClick();
                        else
                            GameAudio.NegativeClick();
                    }
                    else
                    {
                        SelectedShip.RemoveTradeRoute(planets.planetToClick);
                        GameAudio.AffirmativeClick();
                    }
                }
            }
        }

        bool HandleDragAORect(InputState input)
        {
            if (!DefiningAO)
                return false;

            DefiningAO = !DefiningTradeRoutes;
            HandleScrolls(input); // allow exclusive scrolling during AO define
            if (!LookingAtPlanet && HandleGUIClicks(input))
                return true;

            if (input.RightMouseClick) // erase existing AOs
            {
                Vector2 cursorWorld = UnprojectToWorldPosition(input.CursorPosition);
                SelectedShip.AreaOfOperation.RemoveFirst(ao => ao.HitTest(cursorWorld));
                return true;
            }

            // no ship selection? abort
            // Easier out from defining an AO. Used to have to left and Right click at the same time.    -Gretman
            if (SelectedShip == null || input.Escaped)
            {
                DefiningAO = false;
                return true;
            }

            if (input.LeftMouseHeld(0.1f))
            {
                Vector2 start = UnprojectToWorldPosition(input.StartLeftHold);
                Vector2 end   = UnprojectToWorldPosition(input.EndLeftHold);
                AORect = new Rectangle((int)Math.Min(start.X, end.X),  (int)Math.Min(start.Y, end.Y), 
                                       (int)Math.Abs(end.X - start.X), (int)Math.Abs(end.Y - start.Y));
            }
            else if ((AORect.Width+AORect.Height) > 1000 && input.LeftMouseReleased)
            {
                if (AORect.Width >= 5000 && AORect.Height >= 5000)
                {
                    GameAudio.EchoAffirmative();
                    SelectedShip.AreaOfOperation.Add(AORect);
                }
                else
                {
                    GameAudio.NegativeClick(); // eek-eek! AO not big enough!
                }
                AORect = Rectangle.Empty;
            }
            return true;
        }

        void InputClickableItems(InputState input)
        {
            if (!input.LeftMouseDoubleClick) return;

            SelectedShipList.Clear();
            if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                previousSelection = SelectedShip;
            SelectedShip = null;


            if (viewState <= UnivScreenState.SystemView)
            {
                foreach (ClickablePlanets clickablePlanets in ClickPlanetList)
                {
                    if (clickablePlanets.HitTest(input.CursorPosition))
                    {
                        GameAudio.SubBassWhoosh();
                        SelectedPlanet = clickablePlanets.planetToClick;
                        SnapViewColony(SelectedPlanet.Owner != player && !Debug);
                    }
                }
            }

            SelectShipsByClickingonShip(input);

            if (viewState > UnivScreenState.SystemView)
            {
                for (int i = 0; i < ClickableSystems.Count; ++i)
                {
                    ClickableSystem system = ClickableSystems[i];
                    if (input.CursorPosition.InRadius(system.ScreenPos, system.Radius))
                    {
                        if (system.systemToClick.IsExploredBy(player))
                        {
                            GameAudio.SubBassWhoosh();
                            ViewSystem(system.systemToClick);
                        }
                        else
                            GameAudio.NegativeClick();
                    }
                }
            }
        }

        private void SelectShipsByClickingonShip(InputState input)
        {
            foreach (ClickableShip clickableShip in ClickableShipsList)
            {
                if (!clickableShip.HitTest(input.CursorPosition))
                    continue;

                pickedSomethingThisFrame = true;
                SelectedShipList.AddUnique(clickableShip.shipToClick);
                var clicked = clickableShip.shipToClick;

                foreach (ClickableShip clickTest in ClickableShipsList)
                {
                    var ship = clickTest.shipToClick;

                    if (clicked == ship || ship.loyalty != clicked.loyalty) continue;

                    bool sameHull   = ship.BaseHull == clicked.BaseHull;
                    bool sameRole   = ship.DesignRole == clicked.DesignRole;
                    bool sameDesign = ship.Name == clicked.Name;

                    if (input.SelectSameDesign)
                    {
                        if (sameDesign) SelectedShipList.AddUnique(ship);
                    }
                    else if (input.SelectSameRoleAndHull)
                    {
                        if (sameRole && sameHull) SelectedShipList.AddUnique(ship);
                    }
                    else if (input.SelectSameRole)
                    {
                        if (sameRole) SelectedShipList.AddUnique(ship);
                    }
                    else if (clicked.BaseHull == ship.BaseHull) SelectedShipList.AddUnique(ship);
                }
                break;
            }
        }

        void PreviousTargetSelection(InputState input)
        {
            if (previousSelection.Active)
            {
                Ship tempship = previousSelection;
                if (SelectedShip != null && SelectedShip != previousSelection)
                    previousSelection = SelectedShip;
                SelectedShip = tempship;
                ShipInfoUIElement.SetShip(SelectedShip);
                SelectedFleet  = null;
                SelectedItem   = null;
                SelectedSystem = null;
                SelectedPlanet = null;
                SelectedShipList.Clear();
                SelectedShipList.Add(SelectedShip);
                ViewingShip = false;
            }
            else
                previousSelection = null;  //fbedard: remove inactive ship
        }
        
        void CyclePlanetsInCombat(UIButton b)
        {
            if (player.empirePlanetCombat > 0)
            {
                Planet planetToView = null;
                int nbrplanet = 0;
                if (lastplanetcombat >= player.empirePlanetCombat)
                    lastplanetcombat = 0;
                bool flagPlanet;

                foreach (SolarSystem system in SolarSystemList)
                {
                    foreach (Planet p in system.PlanetList)
                    {
                        if (!p.IsExploredBy(EmpireManager.Player) || !p.RecentCombat) continue;
                        if (p.Owner == Empire.Universe.PlayerEmpire)
                        {
                            if (nbrplanet == lastplanetcombat)
                                planetToView = p;
                            nbrplanet++;
                        }
                        else
                        {
                            flagPlanet = false;
                            foreach (Troop troop in p.TroopsHere)
                            {
                                if (troop.Loyalty != null && troop.Loyalty == Empire.Universe.PlayerEmpire)
                                {
                                    flagPlanet = true;
                                    break;
                                }
                            }

                            if (!flagPlanet) continue;
                            if (nbrplanet == lastplanetcombat)
                                planetToView = p;
                            nbrplanet++;
                        }
                    }
                }

                if (planetToView == null) return;
                if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                    previousSelection = SelectedShip;
                SelectedShip = null;
                SelectedFleet = null;
                SelectedItem = null;
                SelectedSystem = null;
                SelectedPlanet = planetToView;
                SelectedShipList.Clear();
                pInfoUI.SetPlanet(planetToView);
                lastplanetcombat++;

                CamDestination = new Vector3(SelectedPlanet.Center.X, SelectedPlanet.Center.Y, 9000f);
                transitionStartPosition = CamPos;
                transitionElapsedTime = 0.0f;
                LookingAtPlanet = false;
                AdjustCamTimer = 2f;
                transDuration = 5f;
                returnToShip = false;
                ViewingShip = false;
                snappingToShip = false;
                SelectedItem = null;
            }
  
        }

        void ResetToolTipTimer(ref bool toolTipToReset, float timer = 0.5f)
        {
            toolTipToReset = false;
            TooltipTimer = 0.5f;
        }

        void InputCheckPreviousShip(Ship ship = null)
        {
            if (SelectedShip != null  && previousSelection != SelectedShip && SelectedShip != ship) //fbedard
                previousSelection = SelectedShip;
        }

        void HandleInputScrap(InputState input)
        {
            player.GetEmpireAI().Goals.QueuePendingRemoval(SelectedItem.AssociatedGoal);
            bool flag = false;
            foreach (Ship ship in player.GetShips())
            {
                if (ship.IsConstructor && ship.AI.OrderQueue.NotEmpty)
                {
                    for (int index = 0; index < ship.AI.OrderQueue.Count; ++index)
                    {
                        if (ship.AI.OrderQueue[index].Goal == SelectedItem.AssociatedGoal)
                        {
                            flag = true;
                            ship.AI.OrderScrapShip();
                            break;
                        }
                    }
                }
            }
            if (!flag)
            {
                foreach (Planet planet in player.GetPlanets())
                {
                    foreach (QueueItem qi in planet.ConstructionQueue)
                    {
                        if (qi.Goal == SelectedItem.AssociatedGoal)
                        {
                            qi.IsCancelled = true; // cancel on next SBProduction update
                        }
                    }
                }
            }
            lock (GlobalStats.ClickableItemLocker)
            {
                for (int x = 0; x < ItemsToBuild.Count; ++x)
                {
                    ClickableItemUnderConstruction item = ItemsToBuild[x];
                    if (item.BuildPos == SelectedItem.BuildPos)
                    {
                        ItemsToBuild.QueuePendingRemoval(item);
                        GameAudio.BlipClick();
                    }
                }
                ItemsToBuild.ApplyPendingRemovals();
            }
            player.GetEmpireAI().Goals.ApplyPendingRemovals();
            SelectedItem = null;
        }

        Fleet AddSelectedShipsToNewFleet(Array<Ship> ships)
        {
            if (ships?.NotEmpty != true) return null;

            ships.ForEach(s => s.UnsafeClearFleet());

            var newFleet = new Fleet(ships, player);
            
            InputCheckPreviousShip();
            GameAudio.FleetClicked();
            newFleet.SetCommandShip(null);
            newFleet.Update(FixedSimTime.Zero/*paused during init*/);
            newFleet.AutoArrange();
            shipListInfoUI.SetShipList(SelectedShipList, true);  //fbedard:display new fleet in UI
            return newFleet;
        }

        Fleet AddShipsToExistingFleet(Fleet fleet, Array<Ship> ships)
        {
            // must have a fleet must have ships to add
            if (fleet?.Ships.NotEmpty != true || ships?.NotEmpty != true)
            {
                fleet = null;
                return null;
            }

            GameAudio.FleetClicked();
            InputCheckPreviousShip();

            foreach (var ship in ships) ship?.UnsafeClearFleet();

            // create a fake fleet to assign positions.
            var positioningFleet = new Fleet(ships, player);
            positioningFleet.AutoArrange();
            
            // offset from the fleet we are adding to
            // the way that fleet relativeOffset works all we need to do is create the offset and not reset it
            // the ships will remember their fleet positions. 
            Vector2 fleetSize = new Vector2(0, fleet.GetRelativeSize().Length() / 2f);
            positioningFleet.DataNodes.ForEach(n =>
            {
                n.Ship.RelativeFleetOffset += fleetSize;
            });
            // ditch the fake fleet
            positioningFleet.Ships.ForEach(s => s?.ClearFleet());

            // put it all together
            foreach (var ship in ships) ship?.ClearFleet();
            fleet.AddShips(ships);

            return fleet;
        }

        public void RecomputeFleetButtons(bool now)
        {
            ++FBTimer;
            if (FBTimer <= 60 && !now)
                return;
            lock (GlobalStats.FleetButtonLocker)
            {
                int shipCounter = 0;
                FleetButtons.Clear();
                foreach (KeyValuePair<int, Fleet> kv in player.GetFleetsDict())
                {
                    if (kv.Value.Ships.Count <= 0) continue;

                    FleetButtons.Add(new FleetButton
                    {
                        ClickRect = new Rectangle(20, 60 + shipCounter * 60, 52, 48),
                        Fleet = kv.Value,
                        Key = kv.Key
                    });
                    ++shipCounter;
                }
                FBTimer = 0;
            }
        }

        void HandleEdgeDetection(InputState input)
        {
            if (LookingAtPlanet)
                return;

            Vector2 screenTopLeftInWorld = UnprojectToWorldPosition(Vector2.Zero);
            float worldWidthOnScreen = UnprojectToWorldPosition(ScreenArea).X - screenTopLeftInWorld.X;

            float x = input.CursorX, y = input.CursorY;
            float outer = -50f;
            float inner = +5.0f;
            float minLeft = outer, maxLeft = inner;
            float minTop  = outer, maxTop  = inner;
            float minRight  = ScreenWidth  - inner, maxRight  = ScreenWidth  - outer;
            float minBottom = ScreenHeight - inner, maxBottom = ScreenHeight - outer;

            bool InRange(float pos, float min, float max)
            {
                return min <= pos && pos <= max;
            }

            bool enableKeys = !ViewingShip;
            bool arrowKeys = Debug == false;

            if (InRange(x, minLeft, maxLeft) || (enableKeys && input.KeysLeftHeld(arrowKeys)))
            {
                CamDestination.X -= 0.008f * worldWidthOnScreen;
                snappingToShip = false;
                ViewingShip    = false;
            }
            if (InRange(x, minRight, maxRight) || (enableKeys && input.KeysRightHeld(arrowKeys)))
            {
                CamDestination.X += 0.008f * worldWidthOnScreen;
                snappingToShip = false;
                ViewingShip    = false;
            }
            if (InRange(y, minTop, maxTop) || (enableKeys && input.KeysUpHeld(arrowKeys)))
            {
                CamDestination.Y -= 0.008f * worldWidthOnScreen;
                snappingToShip = false;
                ViewingShip    = false;
            }
            if (InRange(y, minBottom, maxBottom) || (enableKeys && input.KeysDownHeld(arrowKeys)))
            {
                CamDestination.Y += 0.008f * worldWidthOnScreen;
                snappingToShip = false;
                ViewingShip    = false;
            }

            CamDestination.X = CamDestination.X.Clamped(-UniverseSize, UniverseSize);
            CamDestination.Y = CamDestination.Y.Clamped(-UniverseSize, UniverseSize);
        }

        void HandleScrolls(InputState input)
        {
            if (AdjustCamTimer >= 0f)
                return;

            float scrollAmount = 1500.0f * CamHeight / 3000.0f + 100.0f;

            if ((input.ScrollOut || input.BButtonHeld) && !LookingAtPlanet)
            {
                CamDestination.X = CamPos.X;
                CamDestination.Y = CamPos.Y;
                CamDestination.Z = CamHeight + scrollAmount;
                if (CamHeight > 12000f)
                {
                    CamDestination.Z += 3000f;
                    viewState = UnivScreenState.SectorView;
                    if (CamHeight > 32000.0f)
                        CamDestination.Z += 15000f;
                    if (CamHeight > 100000.0f)
                        CamDestination.Z += 40000f;
                }
                if (input.IsCtrlKeyDown)
                {
                    if (CamHeight < 55000f)
                    {
                        CamDestination.Z = 60000f;
                        AdjustCamTimer = 1f;
                        transitionElapsedTime = 0f;
                    }
                    else
                    {
                        CamDestination.Z = 4200000f;
                        AdjustCamTimer = 1f;
                        transitionElapsedTime = 0f;
                    }
                }
            }
            if (!input.YButtonHeld && !input.ScrollIn || LookingAtPlanet)
                return;

            CamDestination.Z = CamHeight - scrollAmount;
            if (CamHeight >= 16000f)
            {
                CamDestination.Z -= 2000f;
                if (CamHeight > 32000f)
                    CamDestination.Z -= 7500f;
                if (CamHeight > 150000f)
                    CamDestination.Z -= 40000f;
            }
            if (input.IsCtrlKeyDown && CamHeight > 10000f)
                CamDestination.Z = CamHeight <= 65000f ? 10000f : 60000f;
            if (ViewingShip)
                return;
            if (CamHeight <= 450.0f)
                CamHeight = 450f;
            float camDestinationZ = CamDestination.Z;

            //fbedard: add a scroll on selected object
            if ((!input.IsShiftKeyDown && GlobalStats.ZoomTracking) || (input.IsShiftKeyDown && !GlobalStats.ZoomTracking))
            {
                if (SelectedShip != null && SelectedShip.Active)
                {
                    CamDestination = new Vector3(SelectedShip.Position.X, SelectedShip.Position.Y, camDestinationZ);
                }
                else
                if (SelectedPlanet != null)
                {
                    CamDestination = new Vector3(SelectedPlanet.Center.X, SelectedPlanet.Center.Y, camDestinationZ);
                }
                else
                if (SelectedFleet != null && SelectedFleet.Ships.Count > 0)
                {
                    CamDestination = new Vector3(SelectedFleet.AveragePosition(), camDestinationZ);
                }
                else
                if (SelectedShipList.Count > 0 && SelectedShipList[0] != null && SelectedShipList[0].Active)
                {
                    CamDestination = new Vector3(SelectedShipList[0].Position.X, SelectedShipList[0].Position.Y, camDestinationZ);
                }
                else
                    CamDestination = new Vector3(CalculateCameraPositionOnMouseZoom(input.CursorPosition, camDestinationZ), camDestinationZ);
            }
            else
                CamDestination = new Vector3(CalculateCameraPositionOnMouseZoom(input.CursorPosition, camDestinationZ), camDestinationZ);
        }

        public bool IsShipUnderFleetIcon(Ship ship, Vector2 screenPos, float fleetIconScreenRadius)
        {
            foreach (ClickableFleet clickableFleet in ClickableFleetsList)
                if (clickableFleet.fleet == ship.fleet && screenPos.InRadius(clickableFleet.ScreenPos, fleetIconScreenRadius))
                    return true;
            return false;
        }
    }
}