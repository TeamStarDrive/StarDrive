using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Debug;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public sealed partial class UniverseScreen
    {
        Vector2 startDrag;
        Vector2 startDragWorld;
        Vector2 ProjectedPosition;
        Vector2 endDragWorld;

        private bool HandleGUIClicks(InputState input)
        {
            bool flag = dsbw != null && showingDSBW && dsbw.HandleInput(input);
            if (aw.isOpen && aw.HandleInput(input))
                return true;
            if (MinimapDisplayRect.HitTest(input.CursorPosition) && !SelectingWithBox)
            {
                HandleScrolls(input);
                if (input.MouseCurr.LeftButton == ButtonState.Pressed)
                {
                    Vector2 pos = input.CursorPosition - new Vector2(MinimapDisplayRect.X, MinimapDisplayRect.Y);
                    float num = MinimapDisplayRect.Width / (UniverseSize * 2);
                    CamDestination.X = -UniverseSize + (pos.X / num); //Fixed clicking on the mini-map on location with negative coordinates -Gretman
                    CamDestination.Y = -UniverseSize + (pos.Y / num);
                    snappingToShip = false;
                    ViewingShip = false;
                }
                flag = true;
            }

            // @note Make sure HandleInputs are called here
            flag |= SelectedShip != null && ShipInfoUIElement.HandleInput(input) && !LookingAtPlanet;
            flag |= SelectedPlanet != null && pInfoUI.HandleInput(input) && !LookingAtPlanet;
            flag |= SelectedShipList != null && shipListInfoUI.HandleInput(input) && !LookingAtPlanet;

            if (SelectedSystem != null)
            {
                flag |= sInfoUI.HandleInput(input) && !LookingAtPlanet;
            }
            else sInfoUI.SelectionTimer = 0.0f;

            flag |= minimap.HandleInput(input, this);
            flag |= NotificationManager.HandleInput(input);

            // @todo Why are these needed??
            flag |= ShipsInCombat.Rect.HitTest(input.CursorPosition);
            flag |= PlanetsInCombat.Rect.HitTest(input.CursorPosition);

            return flag;
        }

        private void HandleInputNotLookingAtPlanet(InputState input)
        {
            mouseWorldPos = UnprojectToWorldPosition(input.MouseScreenPos);
            if (input.DeepSpaceBuildWindow) InputOpenDeepSpaceBuildWindow();

            if (input.FTLOverlay)       ToggleUIComponent("sd_ui_accept_alt3", ref showingFTLOverlay);
            if (input.RangeOverlay)     ToggleUIComponent("sd_ui_accept_alt3", ref showingRangeOverlay);
            if (input.AutomationWindow) ToggleUIComponent("sd_ui_accept_alt3", ref aw.isOpen);
            if (input.PlanetListScreen)
                ScreenManager.AddScreen(new PlanetListScreen(this, EmpireUI, "sd_ui_accept_alt3"));
            if (input.ShipListScreen) ScreenManager.AddScreen(new ShipListScreen(this, EmpireUI, "sd_ui_accept_alt3"));
            if (input.FleetDesignScreen)
                ScreenManager.AddScreen(new FleetDesignScreen(this, EmpireUI, "sd_ui_accept_alt3"));
            if (input.ZoomToShip) InputZoomToShip();
            if (input.ZoomOut) InputZoomOut();
            if (input.Escaped) DefaultZoomPoints();
            if (input.Tab) ShowShipNames = !ShowShipNames;
            if (Debug)
            {
                Empire empire = EmpireManager.Player;
                if (input.EmpireToggle)
                    empire = EmpireManager.Corsairs;

                if (input.SpawnShip)
                    Ship.CreateShipAtPoint("Bondage-Class Mk IIIa Cruiser", empire, mouseWorldPos);

                if (input.SpawnFleet1) HelperFunctions.CreateFleetAt("Fleet 1", empire, mouseWorldPos);
                if (input.SpawnFleet2) HelperFunctions.CreateFleetAt("Fleet 2", empire, mouseWorldPos);

                if (SelectedShip != null)
                {
                    //#if DEBUG
                    //    if (SelectedShip.Center.InRadius(mouseWorldPos, SelectedShip.Radius*2) && input.RightMouseClick)
                    //        SelectedShip.ShowGridLocalDebugPoint(mouseWorldPos);
                    //#endif

                    if (input.KillThis)
                    {
                        if (input.EmpireToggle)
                            SelectedShip.TestShipModuleDamage();
                        else
                            SelectedShip.Die(null, false);
                    }
                }
                else if (SelectedPlanet != null && Debug && (input.KillThis))
                {
                    foreach (string troopType in ResourceManager.TroopTypes)
                        SelectedPlanet.AssignTroopToTile(
                            ResourceManager.CreateTroop(troopType, EmpireManager.Remnants));
                }

                if (input.SpawnRemnantShip)
                {
                    Ship.CreateShipAtPoint(input.EmpireToggle ? "Remnant Mothership" : "Target Dummy",
                        EmpireManager.Remnants, mouseWorldPos);
                }

                //This little sections added to stress-test the resource manager, and load lots of models into memory.      -Gretman
                //this is a model memory load test. we can do this by iterating models instead of calling them out specifically.
                if (input.KeysCurr.IsKeyDown(Keys.LeftShift) &&
                    input.KeysCurr.IsKeyDown(Keys.B) && !input.KeysPrev.IsKeyDown(Keys.B))
                {
                    if (DebugInfoScreen.Loadmodels == 5) //Repeat
                        DebugInfoScreen.Loadmodels = 0;

                    if (DebugInfoScreen.Loadmodels == 4) //Capital and Carrier
                    {
                        Ship.CreateShipAtPoint("Mordaving L",                       player, mouseWorldPos); //Cordrazine
                        Ship.CreateShipAtPoint("Revenant-Class Dreadnought",        player, mouseWorldPos); //Draylock
                        Ship.CreateShipAtPoint("Draylok Warbird",                   player, mouseWorldPos); //Draylock
                        Ship.CreateShipAtPoint("Archangel-Class Dreadnought",       player, mouseWorldPos); //Human
                        Ship.CreateShipAtPoint("Zanbato-Class Mk IV Battleship",    player, mouseWorldPos); //Kulrathi
                        Ship.CreateShipAtPoint("Tarantula-Class Mk V Battleship",   player, mouseWorldPos); //Opteris
                        Ship.CreateShipAtPoint("Black Widow-Class Dreadnought",     player, mouseWorldPos); //Opteris
                        Ship.CreateShipAtPoint("Corpse Flower III",                 player, mouseWorldPos); //Pollops
                        Ship.CreateShipAtPoint("Wolfsbane-Class Mk III Battleship", player, mouseWorldPos); //Pollops
                        Ship.CreateShipAtPoint("Sceptre Torp",                      player, mouseWorldPos); //Rayleh
                        Ship.CreateShipAtPoint("Devourer-Class Mk V Battleship",    player, mouseWorldPos); //Vulfen
                        Ship.CreateShipAtPoint("SS-Fighter Base Alpha",             player, mouseWorldPos); //Station
                        ++DebugInfoScreen.Loadmodels;
                    }

                    if (DebugInfoScreen.Loadmodels == 3) //Cruiser
                    {
                        Ship.CreateShipAtPoint("Storving Laser",          player, mouseWorldPos); //Cordrazine
                        Ship.CreateShipAtPoint("Draylok Bird of Prey",    player, mouseWorldPos); //Draylock
                        Ship.CreateShipAtPoint("Terran Torpedo Cruiser",  player, mouseWorldPos); //Human
                        Ship.CreateShipAtPoint("Terran Inhibitor",        player, mouseWorldPos); //Human
                        Ship.CreateShipAtPoint("Mauler Carrier",          player, mouseWorldPos); //Kulrathi
                        Ship.CreateShipAtPoint("Chitin Cruiser Zero L",   player, mouseWorldPos); //Opteris
                        Ship.CreateShipAtPoint("Doom Flower",             player, mouseWorldPos); //Pollops
                        Ship.CreateShipAtPoint("Missile Acolyte II",      player, mouseWorldPos); //Rayleh
                        Ship.CreateShipAtPoint("Ancient Torpedo Cruiser", player, mouseWorldPos); //Remnant
                        Ship.CreateShipAtPoint("Type X Artillery",        player, mouseWorldPos); //Vulfen
                        ++DebugInfoScreen.Loadmodels;
                    }

                    if (DebugInfoScreen.Loadmodels == 2) //Frigate
                    {
                        Ship.CreateShipAtPoint("Owlwok Beamer",    player, mouseWorldPos); //Cordrazine
                        Ship.CreateShipAtPoint("Scythe Torpedo",   player, mouseWorldPos); //Draylock
                        Ship.CreateShipAtPoint("Laser Frigate",    player, mouseWorldPos); //Human
                        Ship.CreateShipAtPoint("Missile Corvette", player, mouseWorldPos); //Human
                        Ship.CreateShipAtPoint("Kulrathi Railer",  player, mouseWorldPos); //Kulrathi
                        Ship.CreateShipAtPoint("Stormsoldier",     player, mouseWorldPos); //Opteris
                        Ship.CreateShipAtPoint("Fern Artillery",   player, mouseWorldPos); //Pollops
                        Ship.CreateShipAtPoint("Adv Zion Railer",  player, mouseWorldPos); //Rayleh
                        Ship.CreateShipAtPoint("Corsair",          player, mouseWorldPos); //Remnant
                        Ship.CreateShipAtPoint("Type VII Laser",   player, mouseWorldPos); //Vulfen
                        ++DebugInfoScreen.Loadmodels;
                    }

                    if (DebugInfoScreen.Loadmodels == 1) //Corvette
                    {
                        Ship.CreateShipAtPoint("Laserlitving I",         player, mouseWorldPos); //Cordrazine
                        Ship.CreateShipAtPoint("Crescent Rocket",        player, mouseWorldPos); //Draylock
                        Ship.CreateShipAtPoint("Missile Hunter",         player, mouseWorldPos); //Human
                        Ship.CreateShipAtPoint("Razor RS",               player, mouseWorldPos); //Kulrathi
                        Ship.CreateShipAtPoint("Armored Worker",         player, mouseWorldPos); //Opteris
                        Ship.CreateShipAtPoint("Thicket Attack Fighter", player, mouseWorldPos); //Pollops
                        Ship.CreateShipAtPoint("Ralyeh Railship",        player, mouseWorldPos); //Rayleh
                        Ship.CreateShipAtPoint("Heavy Drone",            player, mouseWorldPos); //Remnant
                        Ship.CreateShipAtPoint("Grinder",                player, mouseWorldPos); //Vulfen
                        Ship.CreateShipAtPoint("Stalker III Hvy Laser",  player, mouseWorldPos); //Vulfen
                        Ship.CreateShipAtPoint("Listening Post",         player, mouseWorldPos); //Platform
                        ++DebugInfoScreen.Loadmodels;
                    }

                    if (DebugInfoScreen.Loadmodels == 0) //Fighters and freighters
                    {
                        Ship.CreateShipAtPoint("Laserving",            player, mouseWorldPos); //Cordrazine
                        Ship.CreateShipAtPoint("Owlwok Freighter S",   player, mouseWorldPos); //Cordrazine
                        Ship.CreateShipAtPoint("Owlwok Freighter M",   player, mouseWorldPos); //Cordrazine
                        Ship.CreateShipAtPoint("Owlwok Freighter L",   player, mouseWorldPos); //Cordrazine
                        Ship.CreateShipAtPoint("Laserwisp",            player, mouseWorldPos); //Draylock
                        Ship.CreateShipAtPoint("Draylok Transporter",  player, mouseWorldPos); //Draylock
                        Ship.CreateShipAtPoint("Draylok Medium Trans", player, mouseWorldPos); //Draylock
                        Ship.CreateShipAtPoint("Draylok Mobilizer",    player, mouseWorldPos); //Draylock
                        Ship.CreateShipAtPoint("Rocket Scout",         player, mouseWorldPos); //Human
                        Ship.CreateShipAtPoint("Small Transport",      player, mouseWorldPos); //Human
                        Ship.CreateShipAtPoint("Medium Transport",     player, mouseWorldPos); //Human
                        Ship.CreateShipAtPoint("Large Transport",      player, mouseWorldPos); //Human
                        Ship.CreateShipAtPoint("Flak Fang",            player, mouseWorldPos); //Kulrathi
                        Ship.CreateShipAtPoint("Drone Railer",         player, mouseWorldPos); //Opteris
                        Ship.CreateShipAtPoint("Creeper Transport",    player, mouseWorldPos); //Opteris
                        Ship.CreateShipAtPoint("Crawler Transport",    player, mouseWorldPos); //Opteris
                        Ship.CreateShipAtPoint("Trawler Transport",    player, mouseWorldPos); //Opteris
                        Ship.CreateShipAtPoint("Rocket Thorn",         player, mouseWorldPos); //Pollops
                        Ship.CreateShipAtPoint("Seeder Transport",     player, mouseWorldPos); //Pollops
                        Ship.CreateShipAtPoint("Sower Transport",      player, mouseWorldPos); //Pollops
                        Ship.CreateShipAtPoint("Grower Transport",     player, mouseWorldPos); //Pollops
                        Ship.CreateShipAtPoint("Ralyeh Interceptor",   player, mouseWorldPos); //Rayleh
                        Ship.CreateShipAtPoint("Vessel S",             player, mouseWorldPos); //Rayleh
                        Ship.CreateShipAtPoint("Vessel M",             player, mouseWorldPos); //Rayleh
                        Ship.CreateShipAtPoint("Vessel L",             player, mouseWorldPos); //Rayleh
                        Ship.CreateShipAtPoint("Xeno Fighter",         player, mouseWorldPos); //Remnant
                        Ship.CreateShipAtPoint("Type I Vulcan",        player, mouseWorldPos); //Vulfen
                        ++DebugInfoScreen.Loadmodels;
                    }
                }
            }
            HandleFleetSelections(input);

            HandleRightMouseNew();
            if (input.LeftMouseClick) InputClickableItems(input);
            if (!LookingAtPlanet)
            {
                LeftClickOnClickableItem(input);
                ShipPieMenuClear();
                HandleSelectionBox(input);
            }
            
            HandleScrolls(input);
        }

        private void HandleInputLookingAtPlanet(InputState input)
        {
            if (input.Tab)
                ShowShipNames = !ShowShipNames;

            var colonyScreen = workersPanel as ColonyScreen;
            if (colonyScreen?.ClickedTroop == true ||
                (!input.Escaped && !input.RightMouseClick && colonyScreen?.close.HandleInput(input) != true))
                return;

            if (colonyScreen != null && colonyScreen.p.Owner == null)
            {
                AdjustCamTimer = 1f;
                if (returnToShip)
                {
                    ViewingShip      = true;
                    returnToShip     = false;
                    snappingToShip   = true;
                    CamDestination.Z = transitionStartPosition.Z;
                }
                else
                    CamDestination    = transitionStartPosition;
                transitionElapsedTime = 0.0f;
                LookingAtPlanet       = false;
            }
            else
            {
                AdjustCamTimer = 1f;
                if (returnToShip)
                {
                    ViewingShip      = true;
                    returnToShip     = false;
                    snappingToShip   = true;
                    CamDestination.Z = transitionStartPosition.Z;
                }
                else
                    CamDestination    = transitionStartPosition;
                transitionElapsedTime = 0.0f;
                LookingAtPlanet       = false;
            }
        }

        private bool InputIsDoubleClick()
        {
            if (ClickTimer < TimerDelay)
                return true;
            ClickTimer = 0f;
            return false;
        }

        private void HandleFleetButtonClick(InputState input)
        {
            InputCheckPreviousShip();
            SelectedShip = (Ship)null;
            SelectedShipList.Clear();
            SelectedFleet = (Fleet)null;
            lock (GlobalStats.FleetButtonLocker)
            {
                for (int i = 0; i < FleetButtons.Count; ++i)
                {
                    UniverseScreen.FleetButton fleetButton = FleetButtons[i];
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

                    if (!InputIsDoubleClick()) return;

                    ViewingShip    = false;
                    AdjustCamTimer = 0.5f;
                    CamDestination = SelectedFleet.FindAveragePosition().ToVec3();

                    if (viewState < UnivScreenState.SystemView)
                        CamDestination.Z = GetZfromScreenState(UnivScreenState.SystemView);
                    return;
                }
            }
        }

        public override bool HandleInput(InputState input)
        {
            this.Input = input;

            if (input.PauseGame && !GlobalStats.TakingInput) Paused = !Paused;
            if (ScreenManager.UpdateExitTimeer(!LookingAtPlanet))
                return true; //if planet screen is still exiting prevent further input

            for (int index = SelectedShipList.Count - 1; index >= 0; --index)
            {
                Ship ship = SelectedShipList[index];
                if (!ship.Active)
                    SelectedShipList.RemoveSwapLast(ship);
            }
            // CG: previous target code. 
            if (previousSelection != null && input.PreviousTarget)
                PreviousTargetSelection(input);

            // fbedard: Set camera chase on ship
            if (input.ChaseCam)
                ChaseCame();

            ShowTacticalCloseup = input.TacticalIcons;

            if (input.UseRealLights)
            {
                UseRealLights = !UseRealLights; // toggle real lights
                SetLighting(UseRealLights);
            }
            if (input.ShowExceptionTracker && !ExceptionTracker.Visible) ReportManual("Manual Report", false);

            if (input.SendKudos && !ExceptionTracker.Visible) ReportManual("Kudos", true);

            if (input.DebugMode)
            {
                Debug = !Debug;
                foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
                    solarSystem.ExploredDict[player] = true;
                GlobalStats.LimitSpeed = GlobalStats.LimitSpeed || Debug;
            }

            HandleEdgeDetection(input);
            GameSpeedIncrease(input.SpeedUp);
            GameSpeedDecrease(input.SpeedDown);


            // fbedard: Click button to Cycle through ships in Combat
            if (!ShipsInCombat.Rect.HitTest(input.CursorPosition))
            {
                ShipsInCombat.State = UIButton.PressState.Default;
            }
            else CycleShipsInCombat(input);


            // fbedard: Click button to Cycle through Planets in Combat
            if (!PlanetsInCombat.Rect.HitTest(input.CursorPosition))
            {
                PlanetsInCombat.State = UIButton.PressState.Default;
            }
            else CyclePlanetsInCombat(input);

            if (!LookingAtPlanet)
            {
                if (HandleGUIClicks(input))
                {
                    SkipRightOnce = true;
                    NeedARelease = true;
                    return true;
                }
            }
            else
            {
                SelectedFleet = null;
                InputCheckPreviousShip();
                SelectedShip = null;
                SelectedShipList.Clear();
                SelectedItem = null;
                SelectedSystem = null;
            }
            if (input.ScrapShip && (SelectedItem != null && SelectedItem.AssociatedGoal.empire == player))
                HandleInputScrap(input);

            if (Debug)
            {
                if (input.ShowDebugWindow)
                {
                    if (!showdebugwindow)
                        DebugWin = new DebugInfoScreen(ScreenManager, this);
                    else
                        DebugWin = null;
                    showdebugwindow = !showdebugwindow;
                }
                if (Debug && showdebugwindow)
                {
                    DebugWin.HandleInput(input);
                }
                if (input.GetMemory)
                {
                    Memory = GC.GetTotalMemory(false) / 1024f;
                }
            }

            if (DefiningAO)
            {
                if (NeedARelease)
                {
                    if (input.LeftMouseDown)
                        NeedARelease = false;
                }
                else
                {
                    DefineAO(input);
                    return true;
                }
            }
            pickedSomethingThisFrame = false;
            if (LookingAtPlanet)
                workersPanel.HandleInput(input);
            if (IsActive)
                EmpireUI.HandleInput(input);
            if (ShowingPlanetToolTip && input.MouseScreenPos.OutsideRadius(tippedPlanet.ScreenPos, tippedPlanet.Radius))
                ResetToolTipTimer(ref ShowingPlanetToolTip);

            if (ShowingSysTooltip && input.MouseScreenPos.OutsideRadius(tippedPlanet.ScreenPos, tippedSystem.Radius))
                ResetToolTipTimer(ref ShowingSysTooltip);

            if (!LookingAtPlanet)
                HandleInputNotLookingAtPlanet(input);
            else
                HandleInputLookingAtPlanet(input);

            if (input.InGameSelect && !pickedSomethingThisFrame &&
                (!input.IsShiftKeyDown && !pieMenu.Visible))
                HandleFleetButtonClick(input);

            cState = SelectedShip != null || SelectedShipList.Count > 0
                ? UniverseScreen.CursorState.Move
                : UniverseScreen.CursorState.Normal;
            if (SelectedShip == null && SelectedShipList.Count <= 0)
                return false;
            for (int i = 0; i < ClickableShipsList.Count; i++)
            {
                UniverseScreen.ClickableShip clickableShip = ClickableShipsList[i];
                if (input.CursorPosition.InRadius(clickableShip.ScreenPos, clickableShip.Radius))
                    cState = UniverseScreen.CursorState.Follow;
            }
            if (cState == UniverseScreen.CursorState.Follow)
                return false;
            lock (GlobalStats.ClickableSystemsLock)
            {
                for (int i = 0; i < ClickPlanetList.Count; i++)
                {
                    UniverseScreen.ClickablePlanets planets = ClickPlanetList[i];
                    if (input.CursorPosition.InRadius(planets.ScreenPos, planets.Radius) &&
                        planets.planetToClick.habitable)
                        cState = UniverseScreen.CursorState.Orbit;
                }
            }
            return base.HandleInput(input);
        }

        private int InputFleetSelection(InputState input)
        {
            if (input.Fleet1)
                return 1;
            if (input.Fleet2)
                return 2;
            if (input.Fleet3)
                return 3;
            if (input.Fleet4)
                return 4;
            if (input.Fleet5)
                return 5;
            if (input.Fleet6)
                return 6;
            if (input.Fleet7)
                return 7;
            if (input.Fleet8)
                return 8;
            if (input.Fleet9)
                return 9;

            return 10;
        }

        private void HandleFleetSelections(InputState input)
        {
            int index = InputFleetSelection(input);
            if (index == 10) return;

            //replace ships in fleet from selection
            if (input.ReplaceFleet)
            {
                if (SelectedShipList.Count == 0) return;

                for (int i = player.GetFleetsDict()[index].Ships.Count - 1; i >= 0; i--)
                {
                    Ship ship = player.GetFleetsDict()[index].Ships[i];
                    ship?.ClearFleet();
                }

                string str = Fleet.GetDefaultFleetNames(index);
                foreach (Ship ship in SelectedShipList)
                    ship.ClearFleet();
                Fleet fleet = new Fleet();
                fleet.Name = str + " Fleet";
                fleet.Owner = player;

                AddSelectedShipsToFleet(fleet);
                player.GetFleetsDict()[index] = fleet;
            }
            //added by gremlin add ships to exiting fleet
            else if (input.AddToFleet)
            {
                if (SelectedShipList.Count == 0) return;

                string str = Fleet.GetDefaultFleetNames(index);
                Fleet fleet = player.GetFleetsDict()[index];
                foreach (Ship ship in SelectedShipList)
                {
                    if (ship.fleet == fleet) continue;
                    ship.ClearFleet();
                }

                if (fleet != null && fleet.Ships.Count == 0)
                {
                    fleet = new Fleet();
                    fleet.Name = str + " Fleet";
                    fleet.Owner = player;
                }
                AddSelectedShipsToFleet(fleet);
                player.GetFleetsDict()[index] = fleet;
            }
            //end of added by
            else //populate ship info UI with ships in fleet
            {
                if (index != 10)
                {
                    SelectedPlanet = (Planet)null;
                    InputCheckPreviousShip();

                    SelectedShip = (Ship)null;
                    Fleet fleet = player.GetFleetsDict()[index] ?? new Fleet();
                    if (fleet.Ships.Count > 0)
                    {
                        SelectedFleet = fleet;
                        GameAudio.PlaySfxAsync("techy_affirm1");
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

                    if (SelectedFleet != null && ClickTimer < TimerDelay)
                    {
                        ViewingShip = false;
                        AdjustCamTimer = 0.5f;
                        CamDestination = SelectedFleet.FindAveragePosition().ToVec3();

                        if (CamHeight < GetZfromScreenState(UnivScreenState.SystemView))
                            CamDestination.Z = GetZfromScreenState(UnivScreenState.SystemView);
                    }
                    else if (SelectedFleet != null)
                        ClickTimer = 0.0f;
                }
            }
        }
        

        private Ship CheckShipClick(InputState input)
        {
            foreach (ClickableShip clickableShip in ClickableShipsList)
            {
                if (input.CursorPosition.InRadius(clickableShip.ScreenPos, clickableShip.Radius))
                    return clickableShip.shipToClick;
            }
            return null;
        }

        private Planet CheckPlanetClick()
        {
            lock (GlobalStats.ClickableSystemsLock)
                foreach (ClickablePlanets clickablePlanets in ClickPlanetList)
                {
                    if (Input.CursorPosition.InRadius(clickablePlanets.ScreenPos, clickablePlanets.Radius + 10.0f))
                        return clickablePlanets.planetToClick;
                }
            return null;
        }

        private SolarSystem CheckSolarSystemClick()
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

        private Fleet CheckFleetClicked()
        {
            foreach(ClickableFleet clickableFleet in ClickableFleetsList)
            {
                if (!Input.CursorPosition.InRadius(clickableFleet.ScreenPos, clickableFleet.ClickRadius)) continue;
                return clickableFleet.fleet;                
            }
            return null;
        }

        private ClickableItemUnderConstruction CheckBuildItemClicked()
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
        private bool AttackSpecifcShip(Ship ship, Ship target)
        {

            if (ship.isConstructor ||
                ship.shipData.Role == ShipData.RoleName.supply)
            {
                GameAudio.NegativeClick();
                return false;
            }

            GameAudio.AffirmativeClick();
            if (ship.loyalty == player)
            {
                if (ship.shipData.Role == ShipData.RoleName.troop)
                {
                    if (ship.TroopList.Count < ship.TroopCapacity)
                        ship.AI.OrderTroopToShip(target);
                    else
                        ship.DoEscort(target);
                }
                else
                    ship.DoEscort(target);
                return true;
            }

            if (ship.loyalty != player)
            {
                if (ship.shipData.Role == ShipData.RoleName.troop)
                    ship.AI.OrderTroopToBoardShip(target);
                else if (Input.QueueAction)
                    ship.AI.OrderQueueSpecificTarget(target);
                else
                    ship.AI.OrderAttackSpecificTarget(target);
            }
            return true;
        }

        private void MoveShipGroupToLocation(ShipGroup shipGroup, Array<Ship> selectedShips)
        {
            foreach (Ship groupShip in shipGroup.GetShips)
            {
                foreach (Ship selectedShip in selectedShips)
                {
                    if (groupShip.guid != selectedShip.guid)
                        continue;                    

                    MoveShipToLocation(groupShip.projectedPosition, shipGroup.ProjectedFacing, groupShip);
                }
            }
        }

        private void MoveFleetToLocation(Ship shipClicked, Planet planetClicked, Vector2 movePosition, Vector2 targetVector, ShipGroup fleet = null)
        {
            fleet = fleet ?? SelectedFleet;
            GameAudio.AffirmativeClick();
            float targetFacingR = fleet.Position.RadiansToTarget(targetVector);
            Vector2 vectorToTarget =
                Vector2.Zero.DirectionToTarget(fleet.Position.PointFromRadians(targetFacingR, 1f));
            PlayerEmpire.GetGSAI().DefensiveCoordinator.RemoveShipList(SelectedShipList);

            if (shipClicked != null && shipClicked.loyalty != player)
            {
                fleet.Position = shipClicked.Center;
                fleet.AssignPositions(0.0f);
                foreach (Ship fleetShip in fleet.Ships)
                    AttackSpecifcShip(fleetShip, shipClicked);
            }
            else if (planetClicked != null)
            {
                fleet.Position = planetClicked.Center; //fbedard: center fleet on planet
                foreach (Ship ship2 in fleet.Ships)
                    RightClickOnPlanet(ship2, planetClicked, false);
            }
            else if (Input.QueueAction)
                fleet.FormationWarpTo(movePosition, targetFacingR, vectorToTarget, true);
            else if (Input.KeysCurr.IsKeyDown(Keys.LeftAlt))
                fleet.MoveToDirectly(movePosition, targetFacingR, vectorToTarget);
            else
                fleet.FormationWarpTo(movePosition, targetFacingR, vectorToTarget);
        }

        private void MoveShipToLocation(Vector2 targetVector, float facingToTargetR, Ship ship = null)
        {
            ship = ship ?? SelectedShip;
            GameAudio.AffirmativeClick();
            if (Input.QueueAction)
            {
                if (Input.OrderOption)
                    ship.AI.OrderMoveDirectlyTowardsPosition(targetVector, facingToTargetR, false);
                else
                    ship.AI.OrderMoveTowardsPosition(targetVector, facingToTargetR, false, null);
            }
            else if (Input.OrderOption)
                ship.AI.OrderMoveDirectlyTowardsPosition(targetVector, facingToTargetR, true);
            else if (Input.KeysCurr.IsKeyDown(Keys.LeftControl))
            {
                ship.AI.OrderMoveTowardsPosition(targetVector, facingToTargetR, true, null);
                ship.AI.OrderQueue.Enqueue(new ShipAI.ShipGoal(ShipAI.Plan.HoldPosition,
                    targetVector, facingToTargetR));
                ship.AI.HasPriorityOrder = true;
                ship.AI.IgnoreCombat = true;
            }
            else
                ship.AI.OrderMoveTowardsPosition(targetVector, facingToTargetR, true, null);
        }

        private bool ShipPieMenu(Ship ship)
        {
            if (ship == null || ship != SelectedShip || SelectedShip.Mothership != null ||
                SelectedShip.isConstructor) return false;

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

        private bool ShipPieMenuClear()
        {
            if (SelectedShip != null || SelectedShipList.Count != 0 ||
                SelectedPlanet == null || !Input.ShipPieMenu) return false;
            if (!pieMenu.Visible)
            {
                pieMenu.RootNode = planetMenu;
                if (SelectedPlanet.Owner == null && SelectedPlanet.habitable)
                    LoadMenuNodes(false, true);
                else
                    LoadMenuNodes(false, false);
                pieMenu.Show(pieMenu.Position);
            }
            else
                pieMenu.ChangeTo(null);
            return true;
        }
        private void UnprojectMouse()
        {
            startDrag = Input.CursorPosition;
            startDragWorld = UnprojectToWorldPosition(startDrag);
            ProjectedPosition = startDragWorld;
        }

        private Vector2 UnprojectMouseWithFacing(ref float factingToTargetR, ref Vector2 unitVectorToTarget)
        {
            Vector3 position = Viewport.Unproject(Input.CursorPosition.ToVec3(), projection, view, Matrix.Identity);
            Vector3 direction = Viewport.Unproject(Input.CursorPosition.ToVec3(1f), projection, view,
                                    Matrix.Identity) - position;
            direction.Normalize();
            Ray ray = new Ray(position, direction);
            float num1 = -ray.Position.Z / ray.Direction.Z;
            Vector3 vector3 = new Vector3(ray.Position.X + num1 * ray.Direction.X,
                ray.Position.Y + num1 * ray.Direction.Y, 0.0f);
            Vector2 target = Input.CursorPosition;
            factingToTargetR = startDrag.RadiansToTarget(target);
            unitVectorToTarget = Vector2.Normalize(target - startDrag);
            return vector3.ToVec2();
        }

        private bool UnselectableShip(Ship ship = null)
        {
            ship = ship ?? SelectedShip;
            if (!ship.isConstructor && ship.shipData.Role != ShipData.RoleName.supply) return false;
            GameAudio.NegativeClick();
            return true;
        }

        private void HandleRightMouseNew()
        {
            if (Input.RightMouseHeldUp) return;
            if (Input.RightMouseClick)
            {
                SelectedSomethingTimer = 3f;
                UnprojectMouse();
            }
            if (SelectedShip != null && SelectedShip.AI.State == AIState.ManualControl &&
                startDragWorld.InRadius(SelectedShip.Center, 5000f))
                return;

            Ship shipClicked = CheckShipClick(Input);
            Planet planetClicked = CheckPlanetClick();


            if (Input.RightMouseReleased)
            {
                float facingToTargetR = 0;
                Vector2 unitVectorToTarget = new Vector2();
                //this is stupid as the values come back as the mouse location... 
                //I mean they arent different than they were in the event of a single click. 
                //unitvector and facing are based on the mouses previous and current location which are the same inthe even of a single click 
                Vector2 targetVector = UnprojectMouseWithFacing(ref facingToTargetR, ref unitVectorToTarget);

                if (!Input.RightMouseWasHeld)
                {
                    if (SelectedFleet != null && SelectedFleet.Owner.isPlayer)
                    {
                        SelectedSomethingTimer = 3f;
                        MoveFleetToLocation(shipClicked, planetClicked, targetVector, targetVector); //, targetFacingR, vectorToTarget);
                    }
                    else if (SelectedShip != null && SelectedShip.loyalty.isPlayer)
                    {
                        player.GetGSAI().DefensiveCoordinator.Remove(SelectedShip);
                        SelectedSomethingTimer = 3f;

                        if (shipClicked != null && shipClicked != SelectedShip)
                        {
                            if (!UnselectableShip())
                                return;

                            GameAudio.AffirmativeClick();
                            AttackSpecifcShip(SelectedShip, shipClicked);
                        }
                        else if (ShipPieMenu(shipClicked)) { } //i think i fd this up. come back to it later. 
                        else if (planetClicked != null) RightClickOnPlanet(SelectedShip, planetClicked, true);
                        else if (UnselectableShip()) return;
                        else                        
                            MoveShipToLocation(targetVector, facingToTargetR);
                        
                    }
                    else if (SelectedShipList.Count > 0)
                    {
                        SelectedSomethingTimer = 3f;
                        foreach (Ship ship in SelectedShipList)                        
                            if (UnselectableShip(ship)) return;

                        GameAudio.AffirmativeClick();

                        if (shipClicked != null || planetClicked != null)
                        {
                            foreach (Ship selectedShip in SelectedShipList)
                            {
                                player.GetGSAI().DefensiveCoordinator.Remove(selectedShip);
                                RightClickOnShip(selectedShip, shipClicked);
                                RightClickOnPlanet(selectedShip, planetClicked);                                
                            }
                        }
                        else
                        {
                            SelectedSomethingTimer = 3f;
                            foreach (Ship ship2 in SelectedShipList)                            
                                if (UnselectableShip(ship2)) return;

                            GameAudio.AffirmativeClick();
                            endDragWorld = UnprojectToWorldPosition(Input.CursorPosition);                            
                            Vector2 fVec = new Vector2(-unitVectorToTarget.Y, unitVectorToTarget.X);

                            if (projectedGroup != null && projectedGroup.GetShips.SequenceEqual(SelectedShipList))
                            {
                                projectedGroup.ProjectPos(endDragWorld, projectedGroup.FindAveragePosition().RadiansToTarget(endDragWorld));                                
                                MoveShipGroupToLocation(projectedGroup, SelectedShipList);
                                ProjectingPosition = false;
                                return;
                            }

                            projectedGroup = new ShipGroup();
                            projectedGroup.AssembleAdhocGroup(SelectedShipList, endDragWorld, ProjectedPosition, facingToTargetR, fVec, player);
                            MoveShipGroupToLocation(projectedGroup, SelectedShipList);                                                       
                        }
                    }
                    if (SelectedFleet == null && SelectedItem == null &&
                        SelectedShip == null && SelectedPlanet == null && SelectedShipList.Count == 0)
                    {
                        if (shipClicked != null && shipClicked.Mothership == null &&
                            !shipClicked.isConstructor) //fbedard: prevent hangar ship and constructor
                        {
                            if (SelectedShip != null && previousSelection != SelectedShip &&
                                SelectedShip != shipClicked) //fbedard
                                previousSelection = SelectedShip;
                            SelectedShip = shipClicked;
                            ShipPieMenu(SelectedShip);
                            
                        }
                    }
                    ProjectingPosition = false;
                    return;
                }

                ProjectingPosition = true;
                if (SelectedFleet != null && SelectedFleet.Owner == player)
                {
                    SelectedSomethingTimer = 3f;                        
                    MoveFleetToLocation(null, null, ProjectedPosition, targetVector);                        
                }
                else if (SelectedShip != null && SelectedShip?.loyalty == player) 
                {
                    player.GetGSAI().DefensiveCoordinator.Remove(SelectedShip);
                    SelectedSomethingTimer = 3f;
                    if (UnselectableShip())
                    {
                        if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                            previousSelection = SelectedShip;
                        return;
                    }
                    MoveShipToLocation(ProjectedPosition, facingToTargetR);                        
                }
                else if (SelectedShipList.Count > 0)
                {
                    SelectedSomethingTimer = 3f;
                    foreach (Ship ship in SelectedShipList)
                    {
                        if (ship.loyalty != player && UnselectableShip(ship))
                            return;                            
                    }

                    GameAudio.AffirmativeClick();
                    endDragWorld = UnprojectToWorldPosition(Input.CursorPosition);
                    Vector2 fVec = new Vector2(-unitVectorToTarget.Y, unitVectorToTarget.X);

                    var fleet = new ShipGroup();
                    fleet.AssembleAdhocGroup(SelectedShipList, endDragWorld, ProjectedPosition, facingToTargetR, fVec, player);
                        
                    fleet.ProjectPos(ProjectedPosition, facingToTargetR - 1.570796f);
                    foreach (Ship ship1 in fleet.Ships)
                    {
                        foreach (Ship ship2 in SelectedShipList)
                        {
                            if (ship1.guid != ship2.guid)
                                continue;
                                 
                            MoveShipToLocation(ship1.projectedPosition, facingToTargetR - 1.570796f, ship1);      
                        }
                    }
                    projectedGroup = fleet;
                }
            }




            if (Input.RightMouseHeld())
            {
                var target = Input.CursorPosition;
                float facing = startDrag.RadiansToTarget(target);
                Vector2 fVec1 = Vector2.Normalize(target - startDrag);
                ProjectingPosition = true;
                if (SelectedFleet != null && SelectedFleet.Owner == player)
                {
                    ProjectingPosition = true;
                    SelectedFleet.ProjectPos(ProjectedPosition, facing);
                    projectedGroup = SelectedFleet;
                }
                else if (SelectedShip != null && SelectedShip.loyalty == player)
                {
                    if (SelectedShip.isConstructor || SelectedShip.shipData.Role == ShipData.RoleName.supply)
                    {
                        if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                            previousSelection = SelectedShip;
                        SelectedShip = null;
                        GameAudio.NegativeClick();
                    }
                    else
                    {
                        ShipGroup shipGroup = new ShipGroup();
                        shipGroup.AddShip(SelectedShip);
                        shipGroup.ProjectPos(ProjectedPosition, facing);
                        projectedGroup = shipGroup;
                    }
                }
                else if (SelectedShipList.Count > 0)
                {
                    foreach (Ship ship in SelectedShipList)
                    {
                        if (ship.loyalty != player)
                            return;
                    }
                    endDragWorld = UnprojectToWorldPosition(Input.CursorPosition);
                    Vector2 fVec2 = new Vector2(-fVec1.Y, fVec1.X);
                    
                    var fleet = new ShipGroup();
                    fleet.AssembleAdhocGroup(SelectedShipList, endDragWorld, startDragWorld, facing, fVec2, player);
                    
                    projectedGroup = fleet;
                }
            }
            else
                ProjectingPosition = false;
        }

        private void HandleShipListInput(InputState input)
        {
            foreach (ClickableShip clickableShip in ClickableShipsList)
            {
                if (!input.CursorPosition.InRadius(clickableShip.ScreenPos, clickableShip.Radius)) continue;
                if (input.IsCtrlKeyDown &&
                    SelectedShipList.Count > 1 &&
                    SelectedShipList.Contains(clickableShip.shipToClick))
                {
                    SelectedShipList.Remove(clickableShip.shipToClick);
                    pickedSomethingThisFrame = true;
                    GameAudio.ShipClicked();
                    break;
                }

                if (SelectedShipList.Count > 0 &&
                    !input.IsShiftKeyDown &&
                    !pickedSomethingThisFrame)
                    SelectedShipList.Clear();
                pickedSomethingThisFrame = true;
                GameAudio.ShipClicked();                
                SelectedSomethingTimer = 3f;
                if (clickableShip.shipToClick?.inSensorRange == true)                
                    SelectedShipList.AddUnique(clickableShip.shipToClick);
                
                break;
            }
        }

        private void LeftClickOnClickableItem(InputState input)
        {
            if (LookingAtPlanet)
                return;

            if (input.ShipPieMenu)
            {
                ShipPieMenu(SelectedShip);
            }
            Vector2 vector2 = input.CursorPosition - pieMenu.Position;
            vector2.Y *= -1f;
            Vector2 selectionVector = vector2 / pieMenu.Radius;
            pieMenu.HandleInput(input, selectionVector);
            if (input.LeftMouseClick && !pieMenu.Visible)
            {
                if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                    previousSelection = SelectedShip;

                SelectedShip = (Ship)null;
                SelectedPlanet = (Planet)null;
                SelectedFleet = (Fleet)null;
                SelectedSystem = (SolarSystem)null;
                SelectedItem = (UniverseScreen.ClickableItemUnderConstruction)null;
                ProjectingPosition = false;
                projectedGroup = (ShipGroup)null;
                bool systemClicked = false;

                if (viewState >= UnivScreenState.SectorView)
                {
                    if ((SelectedSystem = CheckSolarSystemClick()) != null)
                    {
                        sInfoUI.SetSystem(SelectedSystem);
                        systemClicked = true;
                    }
                }
                bool fleetClicked = false;
                if (!systemClicked)
                {
                    if ((SelectedFleet = CheckFleetClicked()) != null)
                    {
                        SelectedShipList.Clear();
                        fleetClicked = true;
                        pickedSomethingThisFrame = true;
                        GameAudio.FleetClicked();
                        SelectedShipList.AddRange(SelectedFleet.Ships);
                    }

                    if (!fleetClicked)
                    {
                        HandleShipListInput(input);

                        if (SelectedShip != null && SelectedShipList.Count == 1)
                            ShipInfoUIElement.SetShip(SelectedShip);
                        else if (SelectedShipList.Count > 1)
                            shipListInfoUI.SetShipList(SelectedShipList, false);

                        bool planetClicked = false;
                        if (SelectedShipList.Count == 1)
                        {
                            if (SelectedShipList[0] == playerShip)
                                LoadShipMenuNodes(1);
                            else if (SelectedShipList[0].loyalty == player)
                                LoadShipMenuNodes(1);
                            else
                                LoadShipMenuNodes(0);
                        }
                        else
                        {

                            if ((SelectedPlanet = CheckPlanetClick()) != null)
                            {
                                planetClicked = true;
                                SelectedSomethingTimer = 3f;
                                pInfoUI.SetPlanet(SelectedPlanet);
                                if (input.LeftMouseDoubleClick)
                                {
                                    ViewPlanet(null);
                                    SelectionBox = new Rectangle();
                                }
                                else
                                    GameAudio.PlanetClicked();
                            }
                        }
                        if (!planetClicked)
                        {
                            if ((SelectedItem = CheckBuildItemClicked()) != null)
                                GameAudio.BuildItemClicked();
                        }
                    }
                }
            }
        }

        private void HandleSelectionBox(InputState input)
        {
            if (LookingAtPlanet)
                return;
          
            if (input.LeftMouseClick)
                SelectionBox = new Rectangle(input.MouseCurr.X, input.MouseCurr.Y, 0, 0);
            if (SelectedShipList.Count == 1)
            {
                if (SelectedShip != null && previousSelection != SelectedShip &&
                    SelectedShip != SelectedShipList[0]) //fbedard
                    previousSelection = SelectedShip;
                SelectedShip = SelectedShipList[0];
            }
            if (input.LeftMouseDown)
            {
                SelectingWithBox = true;
                if (SelectionBox.X == 0 || SelectionBox.Y == 0)
                    return;
                SelectionBox = new Rectangle(SelectionBox.X, SelectionBox.Y,
                    input.MouseCurr.X - SelectionBox.X, input.MouseCurr.Y - SelectionBox.Y);
            }
            else if (input.KeysCurr.IsKeyDown(Keys.LeftShift) &&
                     input.MouseCurr.LeftButton == ButtonState.Released &&
                     input.MousePrev.LeftButton == ButtonState.Pressed)
            {
                if (input.MouseCurr.X < SelectionBox.X)
                    SelectionBox.X = input.MouseCurr.X;
                if (input.MouseCurr.Y < SelectionBox.Y)
                    SelectionBox.Y = input.MouseCurr.Y;
                SelectionBox.Width = Math.Abs(SelectionBox.Width);
                SelectionBox.Height = Math.Abs(SelectionBox.Height);
                bool flag1 = true;
                Array<Ship> list = new Array<Ship>();
                foreach (UniverseScreen.ClickableShip clickableShip in ClickableShipsList)
                {
                    if (SelectionBox.Contains(
                            new Point((int)clickableShip.ScreenPos.X, (int)clickableShip.ScreenPos.Y)) &&
                        !SelectedShipList.Contains(clickableShip.shipToClick))
                    {
                        SelectedPlanet = (Planet)null;
                        SelectedShipList.Add(clickableShip.shipToClick);
                        SelectedSomethingTimer = 3f;
                        list.Add(clickableShip.shipToClick);
                    }
                }
                if (SelectedShipList.Count > 0 && flag1)
                {
                    bool flag2 = false;
                    bool flag3 = false;
                    foreach (Ship ship in list)
                    {
                        if (ship.shipData.Role <= ShipData.RoleName.supply)
                            flag2 = true;
                        else
                            flag3 = true;
                    }
                    if (flag3 && flag2)
                    {
                        foreach (Ship ship in (Array<Ship>)SelectedShipList)
                        {
                            if (ship.shipData.Role <= ShipData.RoleName.supply)
                                SelectedShipList.QueuePendingRemoval(ship);
                        }
                    }
                    SelectedShipList.ApplyPendingRemovals();
                }
                if (SelectedShipList.Count > 1)
                {
                    bool flag2 = false;
                    bool flag3 = false;
                    foreach (Ship ship in (Array<Ship>)SelectedShipList)
                    {
                        if (ship.loyalty == player)
                            flag2 = true;
                        if (ship.loyalty != player)
                            flag3 = true;
                    }
                    if (flag2 && flag3)
                    {
                        foreach (Ship ship in (Array<Ship>)SelectedShipList)
                        {
                            if (ship.loyalty != player)
                                SelectedShipList.QueuePendingRemoval(ship);
                        }
                        SelectedShipList.ApplyPendingRemovals();
                    }
                    if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                        previousSelection = SelectedShip;
                    SelectedShip = (Ship)null;
                    //shipListInfoUI.SetShipList((Array<Ship>)SelectedShipList, true);
                    shipListInfoUI.SetShipList((Array<Ship>)SelectedShipList,
                        false); //fbedard: this is not a fleet!
                }
                else if (SelectedShipList.Count == 1)
                {
                    if (SelectedShip != null && previousSelection != SelectedShip &&
                        SelectedShip != SelectedShipList[0]) //fbedard
                        previousSelection = SelectedShip;
                    SelectedShip = SelectedShipList[0];
                    ShipInfoUIElement.SetShip(SelectedShip);
                }
                SelectionBox = new Rectangle(0, 0, -1, -1);
            }
            else
            {
                if (input.MouseCurr.LeftButton != ButtonState.Released ||
                    input.MousePrev.LeftButton != ButtonState.Pressed)
                    return;
                SelectingWithBox = false;
                if (input.MouseCurr.X < SelectionBox.X)
                    SelectionBox.X = input.MouseCurr.X;
                if (input.MouseCurr.Y < SelectionBox.Y)
                    SelectionBox.Y = input.MouseCurr.Y;
                SelectionBox.Width = Math.Abs(SelectionBox.Width);
                SelectionBox.Height = Math.Abs(SelectionBox.Height);
                bool flag1 = SelectedShipList.Count == 0;
                foreach (UniverseScreen.ClickableShip clickableShip in ClickableShipsList)
                {
                    if (SelectionBox.Contains(
                        new Point((int)clickableShip.ScreenPos.X, (int)clickableShip.ScreenPos.Y)))
                    {
                        SelectedPlanet = (Planet)null;
                        SelectedShipList.Add(clickableShip.shipToClick);
                        SelectedSomethingTimer = 3f;
                    }
                }
                if (SelectedShipList.Count > 0 && flag1)
                {
                    bool flag2 = false;
                    bool flag3 = false;
                    try
                    {
                        foreach (Ship ship in (Array<Ship>)SelectedShipList)
                        {
                            if (ship.shipData.Role <= ShipData.RoleName.freighter ||
                                ship.shipData.ShipCategory == ShipData.Category.Civilian ||
                                ship.AI.State == AIState.Colonize)
                                flag2 = true;
                            else
                                flag3 = true;
                        }
                    }
                    catch { }
                    if (flag3)
                    {
                        if (flag2)
                        {
                            try
                            {
                                foreach (Ship ship in (Array<Ship>)SelectedShipList)
                                {
                                    if (ship.shipData.Role <= ShipData.RoleName.freighter ||
                                        ship.shipData.ShipCategory == ShipData.Category.Civilian ||
                                        ship.AI.State == AIState.Colonize)
                                        SelectedShipList.QueuePendingRemoval(ship);
                                }
                            }
                            catch { }
                        }
                    }
                    SelectedShipList.ApplyPendingRemovals();
                }
                if (SelectedShipList.Count > 1)
                {
                    bool flag2 = false;
                    bool flag3 = false;
                    foreach (Ship ship in (Array<Ship>)SelectedShipList)
                    {
                        if (ship.loyalty == player)
                            flag2 = true;
                        if (ship.loyalty != player)
                            flag3 = true;
                    }
                    if (flag2 && flag3)
                    {
                        foreach (Ship ship in (Array<Ship>)SelectedShipList)
                        {
                            if (ship.loyalty != player)
                                SelectedShipList.QueuePendingRemoval(ship);
                        }
                        SelectedShipList.ApplyPendingRemovals();
                    }
                    if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                        previousSelection = SelectedShip;
                    SelectedShip = (Ship)null;
                    bool flag4 = true;
                    if (SelectedShipList.Count > 0)
                    {
                        if (SelectedShipList[0].fleet != null)
                        {
                            if (SelectedShipList.Count == SelectedShipList[0].fleet.Ships.Count)
                            {
                                try
                                {
                                    foreach (Ship ship in SelectedShipList)
                                    {
                                        if (ship.fleet == null || ship.fleet != SelectedShipList[0].fleet)
                                            flag4 = false;
                                    }
                                    if (flag4)
                                        SelectedFleet = SelectedShipList[0].fleet;
                                }
                                catch { }
                            }
                        }
                        if (SelectedFleet != null)
                            shipListInfoUI.SetShipList(SelectedShipList, true);
                        else
                            shipListInfoUI.SetShipList(SelectedShipList, false);
                    }
                    if (SelectedFleet == null)
                        ShipInfoUIElement.SetShip(SelectedShipList[0]);
                }
                else if (SelectedShipList.Count == 1)
                {
                    if (SelectedShip != null && previousSelection != SelectedShip &&
                        SelectedShip != SelectedShipList[0]) //fbedard
                        previousSelection = SelectedShip;
                    SelectedShip = SelectedShipList[0];
                    ShipInfoUIElement.SetShip(SelectedShip);
                    if (SelectedShipList[0] == playerShip)
                        LoadShipMenuNodes(1);
                    else if (SelectedShipList[0].loyalty == player)
                        LoadShipMenuNodes(1);
                    else
                        LoadShipMenuNodes(0);
                }
                SelectionBox = new Rectangle(0, 0, -1, -1);
            }
        }

        private bool RightClickOnShip(Ship selectedShip, Ship targetShip)
        {
            if (targetShip == null || selectedShip == targetShip) return false;

            if (targetShip.loyalty == player)
            {
                if (selectedShip.shipData.Role == ShipData.RoleName.troop)
                {
                    if (targetShip.TroopList.Count < targetShip.TroopCapacity)
                        selectedShip.AI.OrderTroopToShip(targetShip);
                    else
                        selectedShip.DoEscort(targetShip);
                }
                else
                    selectedShip.DoEscort(targetShip);
            }
            else if (selectedShip.shipData.Role == ShipData.RoleName.troop)
                selectedShip.AI.OrderTroopToBoardShip(targetShip);
            else if (Input.KeysCurr.IsKeyDown(Keys.LeftShift))
                selectedShip.AI.OrderQueueSpecificTarget(targetShip);
            else
                selectedShip.AI.OrderAttackSpecificTarget(targetShip);
            return true;
        }
    
        private void RightClickOnPlanet(Ship ship, Planet planet, bool audio = false)
        {
            if (planet == null) return;

            if (ship.isConstructor)
            {
                if (!audio)
                    return;
                GameAudio.PlaySfxAsync("UI_Misc20");
            }
            else
            {
                if (audio)
                    GameAudio.PlaySfxAsync("echo_affirm1");
                if (ship.isColonyShip)
                {
                    if (planet.Owner == null && planet.habitable)
                        ship.AI.OrderColonization(planet);
                    else
                        ship.AI.OrderToOrbit(planet, true);
                }
                else if (ship.shipData.Role == ShipData.RoleName.troop || (ship.TroopList.Count > 0 && (ship.HasTroopBay || ship.hasTransporter)))
                {
                    if (planet.Owner != null && planet.Owner == this.player && (!ship.HasTroopBay && !ship.hasTransporter))
                    {
                        if (Input.KeysCurr.IsKeyDown(Keys.LeftShift))
                            ship.AI.OrderToOrbit(planet, false);
                        else
                            ship.AI.OrderRebase(planet, true);
                    }
                    else if (planet.habitable && (planet.Owner == null || planet.Owner != player && (ship.loyalty.GetRelations(planet.Owner).AtWar || planet.Owner.isFaction || planet.Owner.data.Defeated)))
                    {
                        //add new right click troop and troop ship options on planets
                        if (Input.KeysCurr.IsKeyDown(Keys.LeftShift))
                            ship.AI.OrderToOrbit(planet, false);
                        else
                        {
                            ship.AI.State = AIState.AssaultPlanet;
                            ship.AI.OrderLandAllTroops(planet);
                        }
                    }
                    else
                    {
                        ship.AI.OrderOrbitPlanet(planet);// OrderRebase(planet, true);
                    }
                }
                else if (ship.BombBays.Count > 0)
                {
                    float enemies = planet.GetGroundStrengthOther(this.player) * 1.5f;
                    float friendlies = planet.GetGroundStrength(this.player);
                    if (planet.Owner != this.player)
                    {
                        if (planet.Owner == null || this.player.GetRelations(planet.Owner).AtWar || planet.Owner.isFaction || planet.Owner.data.Defeated)
                        {
                            if (Input.KeysCurr.IsKeyDown(Keys.LeftShift))
                                ship.AI.OrderBombardPlanet(planet);
                            else if (enemies > friendlies || planet.Population > 0f)
                                ship.AI.OrderBombardPlanet(planet);
                            else
                            {
                                ship.AI.OrderToOrbit(planet, false);
                            }
                        }
                        else
                        {
                            ship.AI.OrderToOrbit(planet, false);
                        }


                    }
                    else if (enemies > friendlies && Input.KeysCurr.IsKeyDown(Keys.LeftShift))
                    {
                        ship.AI.OrderBombardPlanet(planet);
                    }
                    else
                        ship.AI.OrderToOrbit(planet, true);
                }
                else if (Input.KeysCurr.IsKeyDown(Keys.LeftShift))
                    ship.AI.OrderToOrbit(planet, false);
                else
                    ship.AI.OrderToOrbit(planet, true);
            }
                            



        }

        public void UpdateClickableItems()
        {
            lock (GlobalStats.ClickableItemLocker)
                this.ItemsToBuild.Clear();
            for (int index = 0; index < EmpireManager.Player.GetGSAI().Goals.Count; ++index)
            {
                Goal goal = player.GetGSAI().Goals[index];
                if (goal.GoalName != "BuildConstructionShip") continue;
                const float radius = 100f;                    
                Vector2 buildPos = Viewport.Project(new Vector3(goal.BuildPosition, 0.0f), this.projection, this.view, Matrix.Identity).ToVec2();
                Vector3 buildOffSet = this.Viewport.Project(new Vector3(goal.BuildPosition.PointOnCircle(90f, radius), 0.0f), this.projection, this.view, Matrix.Identity);
                float num = Vector2.Distance(new Vector2(buildOffSet.X, buildOffSet.Y), buildPos) + 10f;
                var underConstruction = new ClickableItemUnderConstruction
                {
                    Radius         = num,
                    BuildPos       = goal.BuildPosition,
                    ScreenPos      = buildPos,
                    UID            = goal.ToBuildUID,
                    AssociatedGoal = goal
                };
                lock (GlobalStats.ClickableItemLocker)
                    ItemsToBuild.Add(underConstruction);
            }
        }

        private void DefineAO(InputState input)
        {
            this.HandleScrolls(input);
            if (this.SelectedShip == null)
            {
                this.DefiningAO = false;
                return;
            }
            if (input.Escaped)      //Easier out from defining an AO. Used to have to left and Right click at the same time.    -Gretman
            {
                this.DefiningAO = false;
                return;
            }               
            Vector3 position = this.Viewport.Unproject(new Vector3(input.MouseCurr.X, input.MouseCurr.Y, 0.0f)
                , this.projection, this.view, Matrix.Identity);
            Vector3 direction = this.Viewport.Unproject(new Vector3(input.MouseCurr.X, input.MouseCurr.Y, 1f)
                , this.projection, this.view, Matrix.Identity) - position;
            direction.Normalize();
            var ray = new Ray(position, direction);
            float num = -ray.Position.Z / ray.Direction.Z;
            var vector3 = new Vector3(ray.Position.X + num * ray.Direction.X, ray.Position.Y + num * ray.Direction.Y, 0.0f);
            if (input.LeftMouseClick)
                this.AORect = new Rectangle((int)vector3.X, (int)vector3.Y, 0, 0);
            if (input.LeftMouseHeld())
            {
                int x = AORect.X;
                int y = AORect.Y;
                int x2 = (int)vector3.X;
                int y2 = (int)vector3.Y;
                    
                this.AORect = new Rectangle(x, y,  x2 - x, y2 - y);
            }
            if (input.LeftMouseReleased)
            {
                if (this.AORect.X > vector3.X)
                    this.AORect.X = (int)vector3.X;
                if (this.AORect.Y > vector3.Y)
                    this.AORect.Y = (int)vector3.Y;
                this.AORect.Width = Math.Abs(this.AORect.Width);
                this.AORect.Height = Math.Abs(this.AORect.Height);
                if (this.AORect.Width > 100 && this.AORect.Height > 100)
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    this.SelectedShip.AreaOfOperation.Add(this.AORect);
                }
            }
            for (int index = 0; index < this.SelectedShip.AreaOfOperation.Count; ++index)
            {
                if (this.SelectedShip.AreaOfOperation[index].HitTest(new Vector2(vector3.X, vector3.Y)) && input.MouseCurr.RightButton == ButtonState.Pressed && input.MousePrev.RightButton == ButtonState.Released)
                    this.SelectedShip.AreaOfOperation.Remove(this.SelectedShip.AreaOfOperation[index]);
            }
        }

        private void InputClickableItems(InputState input)
        {
            if (ClickTimer >= TimerDelay)
            {
                if (SelectedShip != null)
                    ClickTimer = 0.0f;
                return;
            }
            SelectedShipList.Clear();
            if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                previousSelection = SelectedShip;
            SelectedShip = null;
            if (viewState <= UnivScreenState.SystemView)
            {
                foreach (ClickablePlanets clickablePlanets in ClickPlanetList)
                {
                    if (input.CursorPosition.InRadius(clickablePlanets.ScreenPos, clickablePlanets.Radius))
                    {
                        GameAudio.PlaySfxAsync("sub_bass_whoosh");
                        SelectedPlanet = clickablePlanets.planetToClick;
                        if (!SnapBackToSystem)
                            HeightOnSnap = CamHeight;
                        ViewPlanet(SelectedPlanet);
                    }
                }
            }
            foreach (ClickableShip clickableShip in ClickableShipsList)
            {
                if(!input.CursorPosition.InRadius(clickableShip.ScreenPos, clickableShip.Radius)) continue;
                pickedSomethingThisFrame = true;
                SelectedShipList.AddUnique(clickableShip.shipToClick);

                foreach (ClickableShip ship in ClickableShipsList)
                {
                    if (clickableShip.shipToClick != ship.shipToClick &&
                        ship.shipToClick.loyalty == clickableShip.shipToClick.loyalty &&
                        ship.shipToClick.shipData.Role == clickableShip.shipToClick.shipData.Role)
                    {
                        SelectedShipList.AddUnique(ship.shipToClick);
                    }
                }
                break;
            }
            if (viewState > UnivScreenState.SystemView)
            {
                for (int i = 0; i < ClickableSystems.Count; ++i)
                {
                    ClickableSystem system = ClickableSystems[i];
                    if (input.CursorPosition.InRadius(system.ScreenPos, system.Radius))
                    {
                        if (system.systemToClick.ExploredDict[player])
                        {
                            GameAudio.PlaySfxAsync("sub_bass_whoosh");
                            HeightOnSnap = CamHeight;
                            ViewSystem(system.systemToClick);
                        }
                        else
                            PlayNegativeSound();
                    }
                }
            }
        }

        private void PreviousTargetSelection(InputState input)
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
                return;
            }
            else
                previousSelection = null;  //fbedard: remove inactive ship
        }

        private void CycleShipsInCombat(InputState input)
        {
            ShipsInCombat.State = UIButton.PressState.Hover;
            ToolTip.CreateTooltip("Cycle through ships not in fleet that are in combat");
            if (input.InGameSelect)
            {
                if (player.empireShipCombat > 0)
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    int nbrship = 0;
                    if (lastshipcombat >= player.empireShipCombat)
                        lastshipcombat = 0;
                    foreach (Ship ship in EmpireManager.Player.GetShips())
                    {
                        if (ship.fleet != null || !ship.InCombat || ship.Mothership != null || !ship.Active)
                            continue;
                        else
                        {
                            if (nbrship == lastshipcombat)
                            {
                                if (SelectedShip != null && SelectedShip != previousSelection && SelectedShip != ship)
                                    previousSelection = SelectedShip;
                                SelectedShip = ship;
                                ViewToShip(null);
                                SelectedShipList.Add(SelectedShip);
                                lastshipcombat++;
                                break;
                            }
                            else nbrship++;
                        }
                    }
                }
                else
                {
                    GameAudio.PlaySfxAsync("blip_click");
                }
            }
        }

        private void CyclePlanetsInCombat(InputState input)
        {
            PlanetsInCombat.State = UIButton.PressState.Hover;
            ToolTip.CreateTooltip("Cycle through planets that are in combat");
            if (input.InGameSelect)
            {
                if (player.empirePlanetCombat > 0)
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    Planet PlanetToView = (Planet)null;
                    int nbrplanet = 0;
                    if (lastplanetcombat >= player.empirePlanetCombat)
                        lastplanetcombat = 0;
                    bool flagPlanet;

                    foreach (SolarSystem system in UniverseScreen.SolarSystemList)
                    {
                        foreach (Planet p in system.PlanetList)
                        {
                            if (p.IsExploredBy(EmpireManager.Player) && p.RecentCombat)
                            {
                                if (p.Owner == Empire.Universe.PlayerEmpire)
                                {
                                    if (nbrplanet == lastplanetcombat)
                                        PlanetToView = p;
                                    nbrplanet++;
                                }
                                else
                                {
                                    flagPlanet = false;
                                    foreach (Troop troop in p.TroopsHere)
                                    {
                                        if (troop.GetOwner() != null && troop.GetOwner() == Empire.Universe.PlayerEmpire)
                                        {
                                            flagPlanet = true;
                                            break;
                                        }
                                    }
                                    if (flagPlanet)
                                    {
                                        if (nbrplanet == lastplanetcombat)
                                            PlanetToView = p;
                                        nbrplanet++;
                                    }
                                }
                            }
                        }
                    }
                    if (PlanetToView != null)
                    {
                        if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                            previousSelection = SelectedShip;
                        SelectedShip = (Ship)null;
                        SelectedFleet = (Fleet)null;
                        SelectedItem = (UniverseScreen.ClickableItemUnderConstruction)null;
                        SelectedSystem = (SolarSystem)null;
                        SelectedPlanet = PlanetToView;
                        SelectedShipList.Clear();
                        pInfoUI.SetPlanet(PlanetToView);
                        lastplanetcombat++;

                        CamDestination = new Vector3(SelectedPlanet.Center.X, SelectedPlanet.Center.Y, 9000f);
                        LookingAtPlanet = false;
                        transitionStartPosition = CamPos;
                        AdjustCamTimer = 2f;
                        transitionElapsedTime = 0.0f;
                        transDuration = 5f;
                        returnToShip = false;
                        ViewingShip = false;
                        snappingToShip = false;
                        SelectedItem = (UniverseScreen.ClickableItemUnderConstruction)null;
                    }
                }
                else
                {
                    GameAudio.PlaySfxAsync("blip_click");
                }
            }
        }

        private void ResetToolTipTimer(ref bool toolTipToReset, float timer = 0.5f)
        {
            toolTipToReset = false;
            TooltipTimer = 0.5f;
        }

        private void InputCheckPreviousShip(Ship ship = null)
        {
            if (SelectedShip != null  && previousSelection != SelectedShip && SelectedShip != ship) //fbedard
                previousSelection = SelectedShip;
        }

        private void HandleInputScrap(InputState input)
        {
            player.GetGSAI().Goals.QueuePendingRemoval(SelectedItem.AssociatedGoal);
            bool flag = false;
            foreach (Ship ship in player.GetShips())
            {
                if (ship.isConstructor && ship.AI.OrderQueue.NotEmpty)
                {
                    for (int index = 0; index < ship.AI.OrderQueue.Count; ++index)
                    {
                        if (ship.AI.OrderQueue[index].goal == SelectedItem.AssociatedGoal)
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
                    foreach (QueueItem queueItem in planet.ConstructionQueue)
                    {
                        if (queueItem.Goal == SelectedItem.AssociatedGoal)
                        {
                            planet.ProductionHere += queueItem.productionTowards;
                            if ((double)planet.ProductionHere > (double)planet.MAX_STORAGE)
                                planet.ProductionHere = planet.MAX_STORAGE;
                            planet.ConstructionQueue.QueuePendingRemoval(queueItem);
                        }
                    }
                    planet.ConstructionQueue.ApplyPendingRemovals();
                }
            }
            lock (GlobalStats.ClickableItemLocker)
            {
                for (int local_10 = 0; local_10 < ItemsToBuild.Count; ++local_10)
                {
                    ClickableItemUnderConstruction local_11 = ItemsToBuild[local_10];
                    if (local_11.BuildPos == SelectedItem.BuildPos)
                    {
                        ItemsToBuild.QueuePendingRemoval(local_11);
                        GameAudio.PlaySfxAsync("blip_click");
                    }
                }
                ItemsToBuild.ApplyPendingRemovals();
            }
            player.GetGSAI().Goals.ApplyPendingRemovals();
            SelectedItem = null;
        }

        private void AddSelectedShipsToFleet(Fleet fleet)
        {
            foreach (Ship ship in SelectedShipList)
            {
                if (ship.loyalty == player && !ship.isConstructor && ship.Mothership == null && ship.fleet == null)  //fbedard: cannot add ships from hangar in fleet
                    fleet.Ships.Add(ship);
            }
            fleet.AutoArrange();
            InputCheckPreviousShip();

            SelectedShip = (Ship)null;
            SelectedShipList.Clear();
            if (fleet.Ships.Count > 0)
            {
                SelectedFleet = fleet;
                GameAudio.PlaySfxAsync("techy_affirm1");
            }
            else
                SelectedFleet = (Fleet)null;
            foreach (Ship ship in fleet.Ships)
            {
                SelectedShipList.Add(ship);
                ship.fleet = fleet;
            }
            RecomputeFleetButtons(true);
            shipListInfoUI.SetShipList(SelectedShipList, true);  //fbedard:display new fleet in UI            
        }

        public void RecomputeFleetButtons(bool now)
        {
            ++FBTimer;
            if (FBTimer <= 60 && !now)
                return;
            lock (GlobalStats.FleetButtonLocker)
            {
                int local_0 = 0;
                int local_1 = 60;
                int local_2 = 20;
                FleetButtons.Clear();
                foreach (KeyValuePair<int, Fleet> item_0 in player.GetFleetsDict())
                {
                    if (item_0.Value.Ships.Count > 0)
                    {
                        FleetButtons.Add(new FleetButton()
                        {
                            ClickRect = new Rectangle(local_2, local_1 + local_0 * local_1, 52, 48),
                            Fleet = item_0.Value,
                            Key = item_0.Key
                        });
                        ++local_0;
                    }
                }
                FBTimer = 0;
            }
        }

        private void HandleEdgeDetection(InputState input)
        {
            if (this.LookingAtPlanet || ViewingShip )
                return;
            PresentationParameters p = ScreenManager.GraphicsDevice.PresentationParameters;
            Vector2 spaceFromScreenSpace1 = UnprojectToWorldPosition(new Vector2(0.0f, 0.0f));
            float num = UnprojectToWorldPosition(new Vector2(p.BackBufferWidth, p.BackBufferHeight)).X - spaceFromScreenSpace1.X;
            input.Repeat = true;
            if (input.CursorPosition.X <= 1f || input.Left)
            {
                CamDestination.X -= 0.008f * num;
                snappingToShip = false;
                ViewingShip    = false;
            }
            if (input.CursorPosition.X >= (p.BackBufferWidth - 1) || input.Right )
            {
                CamDestination.X += 0.008f * num;
                snappingToShip = false;
                ViewingShip    = false;
            }
            if (input.CursorPosition.Y <= 0.0f || input.Up )
            {
                CamDestination.Y -= 0.008f * num;
                snappingToShip = false;
                ViewingShip    = false;
            }
            if (input.CursorPosition.Y >= (p.BackBufferHeight - 1) || input.Down )
            {
                CamDestination.Y += 0.008f * num;
                snappingToShip = false;
                ViewingShip    = false;
            }
            input.Repeat = false;

            CamDestination.X = CamDestination.X.Clamp(-UniverseSize, UniverseSize);
            CamDestination.Y = CamDestination.Y.Clamp(-UniverseSize, UniverseSize);

            //fbedard: remove middle button scrolling
            //if (input.CurrentMouseState.MiddleButton == ButtonState.Pressed)
            //{
            //    this.snappingToShip = false;
            //    this.ViewingShip = false;
            //}
            //if (input.CurrentMouseState.MiddleButton != ButtonState.Pressed || input.LastMouseState.MiddleButton != ButtonState.Released)
            //    return;
            //Vector2 spaceFromScreenSpace2 = this.GetWorldSpaceFromScreenSpace(input.CursorPosition);
            //this.transitionDestination.X = spaceFromScreenSpace2.X;
            //this.transitionDestination.Y = spaceFromScreenSpace2.Y;
            //this.transitionDestination.Z = this.camHeight;
            //this.AdjustCamTimer = 1f;
            //this.transitionElapsedTime = 0.0f;
        }

        private void HandleScrolls(InputState input)
        {
            if ((double)this.AdjustCamTimer >= 0.0)
                return;

            float scrollAmount = 1500.0f * CamHeight / 3000.0f + 100.0f;

            if ((input.ScrollOut || input.BButtonHeld) && !this.LookingAtPlanet)
            {
                this.CamDestination.X = this.CamPos.X;
                this.CamDestination.Y = this.CamPos.Y;
                this.CamDestination.Z = this.CamHeight + scrollAmount;
                if ((double)this.CamHeight > 12000.0)
                {
                    this.CamDestination.Z += 3000f;
                    this.viewState = UniverseScreen.UnivScreenState.SectorView;
                    if ((double)this.CamHeight > 32000.0)
                        this.CamDestination.Z += 15000f;
                    if ((double)this.CamHeight > 100000.0)
                        this.CamDestination.Z += 40000f;
                }
                if (input.KeysCurr.IsKeyDown(Keys.LeftControl))
                {
                    if ((double)this.CamHeight < 55000.0)
                    {
                        this.CamDestination.Z = 60000f;
                        this.AdjustCamTimer = 1f;
                        this.transitionElapsedTime = 0.0f;
                    }
                    else
                    {
                        this.CamDestination.Z = 4200000f * this.GameScale;
                        this.AdjustCamTimer = 1f;
                        this.transitionElapsedTime = 0.0f;
                    }
                }
            }
            if (!input.YButtonHeld && !input.ScrollIn || this.LookingAtPlanet)
                return;

            this.CamDestination.Z = this.CamHeight - scrollAmount;
            if ((double)this.CamHeight >= 16000.0)
            {
                this.CamDestination.Z -= 2000f;
                if ((double)this.CamHeight > 32000.0)
                    this.CamDestination.Z -= 7500f;
                if ((double)this.CamHeight > 150000.0)
                    this.CamDestination.Z -= 40000f;
            }
            if (input.KeysCurr.IsKeyDown(Keys.LeftControl) && (double)this.CamHeight > 10000.0)
                this.CamDestination.Z = (double)this.CamHeight <= 65000.0 ? 10000f : 60000f;
            if (this.ViewingShip)
                return;
            if ((double)this.CamHeight <= 450.0f)
                this.CamHeight = 450f;
            float num2 = this.CamDestination.Z;
            
            //fbedard: add a scroll on selected object
            if ((!input.KeysCurr.IsKeyDown(Keys.LeftShift) && GlobalStats.ZoomTracking) || (input.KeysCurr.IsKeyDown(Keys.LeftShift) && !GlobalStats.ZoomTracking))
            {
                if (this.SelectedShip != null && this.SelectedShip.Active)
                {
                    this.CamDestination = new Vector3(this.SelectedShip.Position.X, this.SelectedShip.Position.Y, num2);
                }
                else
                if (this.SelectedPlanet != null)
                {
                    this.CamDestination = new Vector3(this.SelectedPlanet.Center.X, this.SelectedPlanet.Center.Y, num2);
                }  
                else
                if (this.SelectedFleet != null && this.SelectedFleet.Ships.Count > 0)
                {
                    this.CamDestination = new Vector3(this.SelectedFleet.FindAveragePosition().X, this.SelectedFleet.FindAveragePosition().Y, num2);
                }
                else
                if (this.SelectedShipList.Count > 0 && this.SelectedShipList[0] != null && this.SelectedShipList[0].Active)
                {
                    this.CamDestination = new Vector3(this.SelectedShipList[0].Position.X, this.SelectedShipList[0].Position.Y, num2);
                }
                else
                    this.CamDestination = new Vector3(this.CalculateCameraPositionOnMouseZoom(new Vector2((float)input.MouseCurr.X, (float)input.MouseCurr.Y), num2), num2);
            }
            else
                this.CamDestination = new Vector3(this.CalculateCameraPositionOnMouseZoom(new Vector2((float)input.MouseCurr.X, (float)input.MouseCurr.Y), num2), num2);
        }

        private void HandleScrollsSectorMiniMap(InputState input)
        {
            this.SectorMiniMapHeight = MathHelper.SmoothStep(this.SectorMiniMapHeight, this.desiredSectorZ, 0.2f);
            if ((double)this.SectorMiniMapHeight < 6000.0)
                this.SectorMiniMapHeight = 6000f;
            if (input.InGameSelect)
            {
                this.CamDestination.Z = this.SectorMiniMapHeight;
                this.CamDestination.X = this.playerShip.Center.X;
                this.CamDestination.Y = this.playerShip.Center.Y;
            }
            if ((input.ScrollOut || input.BButtonHeld) && !this.LookingAtPlanet)
            {
                float num = (float)(1500.0 * (double)this.SectorMiniMapHeight / 3000.0 + 550.0);
                if ((double)this.SectorMiniMapHeight < 10000.0)
                    num -= 200f;
                this.desiredSectorZ = this.SectorMiniMapHeight + num;
                if ((double)this.SectorMiniMapHeight > 12000.0)
                {
                    this.desiredSectorZ += 3000f;
                    this.viewState = UniverseScreen.UnivScreenState.SectorView;
                    if ((double)this.SectorMiniMapHeight > 32000.0)
                        this.desiredSectorZ += 15000f;
                    if ((double)this.SectorMiniMapHeight > 100000.0)
                        this.desiredSectorZ += 40000f;
                }
            }
            if ((input.YButtonHeld || input.ScrollIn) && !this.LookingAtPlanet)
            {
                float num = (float)(1500.0 * (double)this.SectorMiniMapHeight / 3000.0 + 550.0);
                if ((double)this.SectorMiniMapHeight < 10000.0)
                    num -= 200f;
                this.desiredSectorZ = this.SectorMiniMapHeight - num;
                if ((double)this.SectorMiniMapHeight >= 16000.0)
                {
                    this.desiredSectorZ -= 3000f;
                    if ((double)this.SectorMiniMapHeight > 32000.0)
                        this.desiredSectorZ -= 7500f;
                    if ((double)this.SectorMiniMapHeight > 150000.0)
                        this.desiredSectorZ -= 40000f;
                }
            }
            if ((double)this.CamHeight <= 168471840.0 * (double)this.GameScale)
                return;
            this.CamHeight = 1.684718E+08f * this.GameScale;
        }

        public bool IsShipUnderFleetIcon(Ship ship, Vector2 screenPos, float fleetIconScreenRadius)
        {
            foreach (ClickableFleet clickableFleet in ClickableFleetsList)
                if (clickableFleet.fleet == ship.fleet && screenPos.InRadius(clickableFleet.ScreenPos, fleetIconScreenRadius))
                    return true;
            return false;
        }

        private Circle GetSelectionCircles(Vector2 WorldPos, float WorldRadius, float radiusMin = 0, float radiusIncrease = 0 )
        {
            ProjectToScreenCoords(WorldPos, WorldRadius, out Vector2 screenPos, out float screenRadius);
            if (radiusMin > 0)
                screenRadius = screenRadius < radiusMin ? radiusMin : screenRadius;            
            return new Circle(screenPos, screenRadius + radiusIncrease);

        }

        private Circle GetSelectionCirclesAroundShip(Ship ship)
            => GetSelectionCircles(ship.Center, ship.GetSO().WorldBoundingSphere.Radius, 5, 0);
    }
}