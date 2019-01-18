using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Debug;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game
{
    public partial class UniverseScreen
    {   
        private bool HandleGUIClicks(InputState input)
        {
            bool flag = dsbw != null && showingDSBW && dsbw.HandleInput(input);
            if (aw.IsOpen && aw.HandleInput(input))
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
            mouseWorldPos = UnprojectToWorldPosition(input.CursorPosition);
            if (input.DeepSpaceBuildWindow) InputOpenDeepSpaceBuildWindow();
            if (input.FTLOverlay)       ToggleUIComponent("sd_ui_accept_alt3", ref showingFTLOverlay);
            if (input.RangeOverlay)     ToggleUIComponent("sd_ui_accept_alt3", ref showingRangeOverlay);
            if (input.AutomationWindow) aw.ToggleVisibility();
            if (input.PlanetListScreen)  ScreenManager.AddScreen(new PlanetListScreen(this, EmpireUI, "sd_ui_accept_alt3"));
            if (input.ShipListScreen)    ScreenManager.AddScreen(new ShipListScreen(this, EmpireUI, "sd_ui_accept_alt3"));
            if (input.FleetDesignScreen) ScreenManager.AddScreen(new FleetDesignScreen(this, EmpireUI, "sd_ui_accept_alt3"));
            if (input.ZoomToShip) InputZoomToShip();
            if (input.ZoomOut) InputZoomOut();
            if (input.Escaped) DefaultZoomPoints();
            if (input.Tab) ShowShipNames = !ShowShipNames;

            if (Debug)
                HandleDebugEvents(input);

            HandleFleetSelections(input);

            SelectedShipsHandleRightMouse();
            if (input.LeftMouseDoubleClick)
                InputClickableItems(input);
            
            if (!LookingAtPlanet)
            {
                LeftClickOnClickableItem(input);
                ShipPieMenuClear();
                HandleSelectionBox(input);
            }
            
            HandleScrolls(input);
        }

        private void HandleDebugEvents(InputState input)
        {
            Empire empire = EmpireManager.Player;
            if (input.EmpireToggle)
                empire = EmpireManager.Corsairs;

            if (input.SpawnShip)
                Ship.CreateShipAtPoint("Bondage-Class Mk IIIa Cruiser", empire, mouseWorldPos);
            if (input.SpawnFleet2) HelperFunctions.CreateFleetAt("Fleet 2", empire, mouseWorldPos);
            if (input.SpawnFleet1) HelperFunctions.CreateFleetAt("Fleet 1", empire, mouseWorldPos);

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
                    ResourceManager.CreateTroop(troopType, EmpireManager.Remnants).AssignTroopToTile(SelectedPlanet);
            }

            if (input.SpawnRemnantShip)
            {
                if (EmpireManager.Remnants == null)
                    Log.Warning("Remnant faction missing!");
                else
                    Ship.CreateShipAtPoint(input.EmpireToggle ? "Remnant Mothership" : "Target Dummy",
                        EmpireManager.Remnants, mouseWorldPos);
            }

            if (input.IsShiftKeyDown && input.WasKeyPressed(Keys.B))
                StressTestShipLoading();
        }

        // This little section added to stress-test the resource manager, and load lots of models into memory.      -Gretman
        // this is a model memory load test. we can do this by iterating models instead of calling them out specifically.
        private void StressTestShipLoading()
        {
            if (DebugInfoScreen.Loadmodels == 5) // Repeat
                DebugInfoScreen.Loadmodels = 0;

            if (DebugInfoScreen.Loadmodels == 4) // Capital and Carrier
            {
                Ship.CreateShipAtPoint("Mordaving L", player, mouseWorldPos); //Cordrazine
                Ship.CreateShipAtPoint("Revenant-Class Dreadnought", player, mouseWorldPos); //Draylock
                Ship.CreateShipAtPoint("Draylok Warbird", player, mouseWorldPos); //Draylock
                Ship.CreateShipAtPoint("Archangel-Class Dreadnought", player, mouseWorldPos); //Human
                Ship.CreateShipAtPoint("Zanbato-Class Mk IV Battleship", player, mouseWorldPos); //Kulrathi
                Ship.CreateShipAtPoint("Tarantula-Class Mk V Battleship", player, mouseWorldPos); //Opteris
                Ship.CreateShipAtPoint("Black Widow-Class Dreadnought", player, mouseWorldPos); //Opteris
                Ship.CreateShipAtPoint("Corpse Flower III", player, mouseWorldPos); //Pollops
                Ship.CreateShipAtPoint("Wolfsbane-Class Mk III Battleship", player, mouseWorldPos); //Pollops
                Ship.CreateShipAtPoint("Sceptre Torp", player, mouseWorldPos); //Rayleh
                Ship.CreateShipAtPoint("Devourer-Class Mk V Battleship", player, mouseWorldPos); //Vulfen
                Ship.CreateShipAtPoint("SS-Fighter Base Alpha", player, mouseWorldPos); //Station
                ++DebugInfoScreen.Loadmodels;
            }

            if (DebugInfoScreen.Loadmodels == 3) //Cruiser
            {
                Ship.CreateShipAtPoint("Storving Laser", player, mouseWorldPos); //Cordrazine
                Ship.CreateShipAtPoint("Draylok Bird of Prey", player, mouseWorldPos); //Draylock
                Ship.CreateShipAtPoint("Terran Torpedo Cruiser", player, mouseWorldPos); //Human
                Ship.CreateShipAtPoint("Terran Inhibitor", player, mouseWorldPos); //Human
                Ship.CreateShipAtPoint("Mauler Carrier", player, mouseWorldPos); //Kulrathi
                Ship.CreateShipAtPoint("Chitin Cruiser Zero L", player, mouseWorldPos); //Opteris
                Ship.CreateShipAtPoint("Doom Flower", player, mouseWorldPos); //Pollops
                Ship.CreateShipAtPoint("Missile Acolyte II", player, mouseWorldPos); //Rayleh
                Ship.CreateShipAtPoint("Ancient Torpedo Cruiser", player, mouseWorldPos); //Remnant
                Ship.CreateShipAtPoint("Type X Artillery", player, mouseWorldPos); //Vulfen
                ++DebugInfoScreen.Loadmodels;
            }

            if (DebugInfoScreen.Loadmodels == 2) //Frigate
            {
                Ship.CreateShipAtPoint("Owlwok Beamer", player, mouseWorldPos); //Cordrazine
                Ship.CreateShipAtPoint("Scythe Torpedo", player, mouseWorldPos); //Draylock
                Ship.CreateShipAtPoint("Laser Frigate", player, mouseWorldPos); //Human
                Ship.CreateShipAtPoint("Missile Corvette", player, mouseWorldPos); //Human
                Ship.CreateShipAtPoint("Kulrathi Railer", player, mouseWorldPos); //Kulrathi
                Ship.CreateShipAtPoint("Stormsoldier", player, mouseWorldPos); //Opteris
                Ship.CreateShipAtPoint("Fern Artillery", player, mouseWorldPos); //Pollops
                Ship.CreateShipAtPoint("Adv Zion Railer", player, mouseWorldPos); //Rayleh
                Ship.CreateShipAtPoint("Corsair", player, mouseWorldPos); //Remnant
                Ship.CreateShipAtPoint("Type VII Laser", player, mouseWorldPos); //Vulfen
                ++DebugInfoScreen.Loadmodels;
            }

            if (DebugInfoScreen.Loadmodels == 1) //Corvette
            {
                Ship.CreateShipAtPoint("Laserlitving I", player, mouseWorldPos); //Cordrazine
                Ship.CreateShipAtPoint("Crescent Rocket", player, mouseWorldPos); //Draylock
                Ship.CreateShipAtPoint("Missile Hunter", player, mouseWorldPos); //Human
                Ship.CreateShipAtPoint("Razor RS", player, mouseWorldPos); //Kulrathi
                Ship.CreateShipAtPoint("Armored Worker", player, mouseWorldPos); //Opteris
                Ship.CreateShipAtPoint("Thicket Attack Fighter", player, mouseWorldPos); //Pollops
                Ship.CreateShipAtPoint("Ralyeh Railship", player, mouseWorldPos); //Rayleh
                Ship.CreateShipAtPoint("Heavy Drone", player, mouseWorldPos); //Remnant
                Ship.CreateShipAtPoint("Grinder", player, mouseWorldPos); //Vulfen
                Ship.CreateShipAtPoint("Stalker III Hvy Laser", player, mouseWorldPos); //Vulfen
                Ship.CreateShipAtPoint("Listening Post", player, mouseWorldPos); //Platform
                ++DebugInfoScreen.Loadmodels;
            }

            if (DebugInfoScreen.Loadmodels == 0) //Fighters and freighters
            {
                Ship.CreateShipAtPoint("Laserving", player, mouseWorldPos); //Cordrazine
                Ship.CreateShipAtPoint("Owlwok Freighter S", player, mouseWorldPos); //Cordrazine
                Ship.CreateShipAtPoint("Owlwok Freighter M", player, mouseWorldPos); //Cordrazine
                Ship.CreateShipAtPoint("Owlwok Freighter L", player, mouseWorldPos); //Cordrazine
                Ship.CreateShipAtPoint("Laserwisp", player, mouseWorldPos); //Draylock
                Ship.CreateShipAtPoint("Draylok Transporter", player, mouseWorldPos); //Draylock
                Ship.CreateShipAtPoint("Draylok Medium Trans", player, mouseWorldPos); //Draylock
                Ship.CreateShipAtPoint("Draylok Mobilizer", player, mouseWorldPos); //Draylock
                Ship.CreateShipAtPoint("Rocket Scout", player, mouseWorldPos); //Human
                Ship.CreateShipAtPoint("Small Transport", player, mouseWorldPos); //Human
                Ship.CreateShipAtPoint("Medium Transport", player, mouseWorldPos); //Human
                Ship.CreateShipAtPoint("Large Transport", player, mouseWorldPos); //Human
                Ship.CreateShipAtPoint("Flak Fang", player, mouseWorldPos); //Kulrathi
                Ship.CreateShipAtPoint("Drone Railer", player, mouseWorldPos); //Opteris
                Ship.CreateShipAtPoint("Creeper Transport", player, mouseWorldPos); //Opteris
                Ship.CreateShipAtPoint("Crawler Transport", player, mouseWorldPos); //Opteris
                Ship.CreateShipAtPoint("Trawler Transport", player, mouseWorldPos); //Opteris
                Ship.CreateShipAtPoint("Rocket Thorn", player, mouseWorldPos); //Pollops
                Ship.CreateShipAtPoint("Seeder Transport", player, mouseWorldPos); //Pollops
                Ship.CreateShipAtPoint("Sower Transport", player, mouseWorldPos); //Pollops
                Ship.CreateShipAtPoint("Grower Transport", player, mouseWorldPos); //Pollops
                Ship.CreateShipAtPoint("Ralyeh Interceptor", player, mouseWorldPos); //Rayleh
                Ship.CreateShipAtPoint("Vessel S", player, mouseWorldPos); //Rayleh
                Ship.CreateShipAtPoint("Vessel M", player, mouseWorldPos); //Rayleh
                Ship.CreateShipAtPoint("Vessel L", player, mouseWorldPos); //Rayleh
                Ship.CreateShipAtPoint("Xeno Fighter", player, mouseWorldPos); //Remnant
                Ship.CreateShipAtPoint("Type I Vulcan", player, mouseWorldPos); //Vulfen
                ++DebugInfoScreen.Loadmodels;
            }
        }

        private void HandleInputLookingAtPlanet(InputState input)
        {
            if (input.Tab)
                ShowShipNames = !ShowShipNames;

            var colonyScreen = workersPanel as ColonyScreen;
            if (colonyScreen?.ClickedTroop == true ||
                (!input.Escaped && !input.RightMouseClick && colonyScreen?.close.HandleInput(input) != true))
                return;

            if (colonyScreen != null && colonyScreen.P.Owner == null)
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

        private void HandleFleetButtonClick(InputState input)
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
                        CamDestination = SelectedFleet.FindAveragePosition().ToVec3(CamPos.Z);
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
            if (ScreenManager.UpdateExitTimeer(!LookingAtPlanet))
                return true; //if planet screen is still exiting prevent further input
            HandleEdgeDetection(input);
            if (DefiningAO)
            {
                DefineAO(input);
                return false;
            }
                        
            for (int index = SelectedShipList.Count - 1; index >= 0; --index)
            {
                Ship ship = SelectedShipList[index];
                if (ship?.Active != true)
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
            if (input.ShowExceptionTracker)
            {
                Paused = true;
                Log.OpenURL("https://bitbucket.org/CrunchyGremlin/sd-blackbox/issues/new");
            }
            if (input.SendKudos)
            {
                Paused = true;
                Log.OpenURL("http://steamcommunity.com/id/v-danbe/recommended/220660");                
            }
            

            if (input.DebugMode)
            {
                Debug = !Debug;
                foreach (SolarSystem solarSystem in SolarSystemList)
                {
                    solarSystem.SetExploredBy(player);
                    foreach (var planet in solarSystem.PlanetList)
                        planet.SetExploredBy(player);
                }
                GlobalStats.LimitSpeed = GlobalStats.LimitSpeed || Debug;
            }

            
            HandleGameSpeedChange(input);

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
                    NeedARelease  = true;
                    return true;
                }
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

      
            pickedSomethingThisFrame = false;

            ShipsInCombat.Visible   = !LookingAtPlanet;
            PlanetsInCombat.Visible = !LookingAtPlanet;

            if (LookingAtPlanet)
                workersPanel.HandleInput(input);
            if (IsActive)
                EmpireUI.HandleInput(input);
            if (ShowingPlanetToolTip && input.CursorPosition.OutsideRadius(tippedPlanet.ScreenPos, tippedPlanet.Radius))
                ResetToolTipTimer(ref ShowingPlanetToolTip);

            if (ShowingSysTooltip && input.CursorPosition.OutsideRadius(tippedPlanet.ScreenPos, tippedSystem.Radius))
                ResetToolTipTimer(ref ShowingSysTooltip);

            if (!LookingAtPlanet)
                HandleInputNotLookingAtPlanet(input);
            else
                HandleInputLookingAtPlanet(input);

            if (input.InGameSelect && !pickedSomethingThisFrame &&
                (!input.IsShiftKeyDown && !pieMenu.Visible))
                HandleFleetButtonClick(input);

            cState = SelectedShip != null || SelectedShipList.Count > 0
                ? CursorState.Move
                : CursorState.Normal;
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
            return base.HandleInput(input);
        }

        private static int InputFleetSelection(InputState input)
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

        private void HandleFleetSelections(InputState input)
        {
            int index = InputFleetSelection(input);
            if (index == -1) return;

            // replace ships in fleet from selection
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
                var fleet = new Fleet();
                fleet.Name = str + " Fleet";
                fleet.Owner = player;

                AddSelectedShipsToFleet(fleet);
                player.GetFleetsDict()[index] = fleet;
                RecomputeFleetButtons(true);
            }
            else if (input.AddToFleet) // added by gremlin add ships to exiting fleet
            {
                if (SelectedShipList.Count == 0) return;

                string str = Fleet.GetDefaultFleetNames(index);
                Fleet fleet = player.GetFleetsDict()[index];
                foreach (Ship ship in SelectedShipList)
                {
                    if (ship.fleet == fleet) continue;
                    ship.ClearFleet();
                }

                if (fleet != null && fleet.Ships.Count > 0)
                {
                    fleet = new Fleet();
                    fleet.Name = str + " Fleet";
                    fleet.Owner = player;
                }
                AddSelectedShipsToFleet(fleet);
                player.GetFleetsDict()[index] = fleet;
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

                if (SelectedFleet != null)
                {
                    if (Input.LeftMouseDoubleClick)
                    {
                        ViewingShip = false;
                        AdjustCamTimer = 0.5f;
                        CamDestination = SelectedFleet.FindAveragePosition().ToVec3();

                        if (CamHeight < GetZfromScreenState(UnivScreenState.SystemView))
                            CamDestination.Z = GetZfromScreenState(UnivScreenState.SystemView);
                    }
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
            if (target.loyalty == player)
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

            //if (ship.loyalty == player)
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

        private bool MoveFleetToPlanet(Planet planetClicked, ShipGroup fleet)
        {
            if (planetClicked == null || fleet == null) return false;
            fleet.Position = planetClicked.Center; //fbedard: center fleet on planet
            using (fleet.Ships.AcquireReadLock())
                foreach (Ship ship2 in fleet.Ships)
                    RightClickOnPlanet(ship2, planetClicked, false);
            return true;
        }

        private bool MoveFleetToShip(Ship shipClicked, ShipGroup fleet)
        {
            if (shipClicked == null || shipClicked.loyalty == player) return false;
            
                fleet.Position = shipClicked.Center;
                fleet.AssignPositions(0.0f);
                foreach (Ship fleetShip in fleet.Ships)
                    AttackSpecifcShip(fleetShip, shipClicked);
            return true;
            
        }

        private bool QueueFleetMovement(Vector2 movePosition, float facing, ShipGroup fleet)
        {
            if (!Input.QueueAction || fleet.Ships[0].AI.WayPoints.Count() == 0) return false;

            Vector2 vectorToTarget =
                Vector2.Zero.DirectionToTarget(fleet.Position.PointFromRadians(facing, 1f));
            using (fleet.Ships.AcquireReadLock())
                foreach (var ship in fleet.Ships)
                    ship.AI.ClearOrderIfCombat();

            fleet.FormationWarpTo(movePosition, facing, vectorToTarget, true);
            return true; 
        }

        private void MoveFleetToLocation(Ship shipClicked, Planet planetClicked, Vector2 movePosition, float  facing, Vector2 fvec, ShipGroup fleet = null)
        {            
            fleet = fleet ?? SelectedFleet;            
            fleet?.FleetTargetList.Clear();
            GameAudio.AffirmativeClick();
            
            
            using (fleet.Ships.AcquireReadLock())
                foreach (var ship in fleet.Ships)
                {
                    ship.AI.Target = null;
                    ship.AI.SetPriorityOrder(!Input.QueueAction);
                }
            PlayerEmpire.GetEmpireAI().DefensiveCoordinator.RemoveShipList(SelectedShipList);

            if (MoveFleetToShip(shipClicked, fleet)) return;

            if (MoveFleetToPlanet(planetClicked, fleet)) return;

            if (QueueFleetMovement(movePosition, facing, fleet)) return;

            using (fleet.Ships.AcquireReadLock())
                foreach (var ship in fleet.Ships)
                    ship.AI.OrderQueue.Clear();

            Vector2 vectorToTarget =
                Vector2.Zero.DirectionToTarget(fleet.Position.PointFromRadians(facing, 1f));

            if (Input.KeysCurr.IsKeyDown(Keys.LeftAlt))            
                fleet.MoveToNow(movePosition, facing, vectorToTarget);            
            else                
                fleet.FormationWarpTo(movePosition, facing, vectorToTarget, Input.QueueAction);
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
        //private void UnprojectMouse()
        //{
        //    startDrag = Input.CursorPosition;
        //    startDragWorld = UnprojectToWorldPosition(startDrag);
        //    ProjectedPosition = startDragWorld;
        //}

        private Vector2 UnprojectMouseWithFacing(out float factingToTargetR, out Vector2 unitVectorToTarget)
        {
            Vector2 worldStartPos  = SelectedFleet?.Position ?? SelectedShip?.Position ?? projectedGroup.FindAveragePosition() ;
            Vector2 worldEndPos = UnprojectToWorldPosition(Input.RightMouseWasHeld ? Input.StartRighthold: Input.CursorPosition);

            Vector2 facingPos = UnprojectToWorldPosition(Input.RightMouseWasHeld ? Input.EndRightHold : Input.CursorPosition);
            Vector2 facingDir = worldEndPos - facingPos;
            facingDir.Normalize();
            Ray ray           = new Ray(facingPos.ToVec3(1), facingDir.ToVec3(1));
            float num1        = -ray.Position.Z / ray.Direction.Z;
            Vector3 vector3   = new Vector3(ray.Position.X + num1 * ray.Direction.X,
                ray.Position.Y + num1 * ray.Direction.Y, 0.0f);
            

            factingToTargetR = (Input.RightMouseWasHeld ? worldEndPos : worldStartPos).RadiansToTarget(facingPos);
            
            unitVectorToTarget = Vector2.Normalize(facingDir);
            return worldEndPos;// vector3.ToVec2();
        }

        private bool UnselectableShip(Ship ship = null)
        {
            ship = ship ?? SelectedShip;
            if (!ship.isConstructor && ship.shipData.Role != ShipData.RoleName.supply) return false;
            GameAudio.NegativeClick();
            return true;
        }

        private void MoveFleetToMouse(Planet planet, Ship ship)
        {            
            Vector2 targetVector = UnprojectMouseWithFacing(out float facingToTargetR,
                out Vector2 unitVectorToTarget);
            MoveFleetToLocation(ship, planet, targetVector, facingToTargetR, unitVectorToTarget);
        }
        private void MoveShipToMouse()
        {
            Vector2 targetVector = UnprojectMouseWithFacing(out float facingToTargetR,
                out Vector2 unitVectorToTarget);
            MoveShipToLocation(targetVector, facingToTargetR);
        }

        private void MoveShipGroupToMouse()
        {
            if (Input.RightMouseWasHeld)
            {
                MoveShipGroupToLocation(projectedGroup, SelectedShipList);                
                return;
            }


            float facingToTargetR;
            Vector2 unitVectorToTarget;
            Vector2 targetVector;
            if (!projectedGroup?.GetShips.SequenceEqual(SelectedShipList) ?? true) 
            {
                projectedGroup = new ShipGroup();
                projectedGroup.AssembleAdhocGroup(SelectedShipList, Vector2.Zero, Vector2.Zero,
                    0, Vector2.Zero, player);

                targetVector = UnprojectMouseWithFacing(out facingToTargetR,
                    out unitVectorToTarget);
                projectedGroup.ProjectPos(targetVector, facingToTargetR);
                MoveShipGroupToLocation(projectedGroup, SelectedShipList);
                return;
            }
            projectedGroup.FindAveragePositionset();
            targetVector = UnprojectMouseWithFacing(out facingToTargetR,
                out unitVectorToTarget);
            projectedGroup.AssembleFleet(facingToTargetR, unitVectorToTarget, true);
            projectedGroup.MoveToNow(targetVector, facingToTargetR, unitVectorToTarget);

        }

        private void SelectedShipsHandleRightMouse()
        {
            if (Input.RightMouseHeldUp || NotificationManager.HitTest) return;
            if (Input.RightMouseClick)            
                SelectedSomethingTimer = 3f;
            
            if (SelectedShip != null && SelectedShip.AI.State == AIState.ManualControl &&
                Input.StartRighthold.InRadius(SelectedShip.Center, 5000f))
                return;

            if (SelectedShipsRightMouseClick()) return;

            if (SelectedShipsRightMouseWasHeld()) return;


            SelectedShipsRightMouseHeld();
        }

        private void SelectedShipsRightMouseHeld()
        {
            if (!Input.RightMouseHeld(.3f)) return;

            ProjectingPosition = true;
            Vector2 endDragWorld      = UnprojectToWorldPosition(Input.EndRightHold);
            Vector2 startDragWorld    = UnprojectToWorldPosition(Input.StartRighthold);
            float facing = Input.StartRighthold.RadiansToTarget(Input.CursorPosition);
            Vector2 facingVector = Input.StartRighthold.DirectionToTarget(Input.CursorPosition);

            if (SelectedFleet?.Owner == EmpireManager.Player)
            {
                ProjectingPosition = true;
                SelectedFleet.ProjectPos(startDragWorld, facing);
                projectedGroup = SelectedFleet;
            }
            else if (SelectedShip?.loyalty == player)
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
                    shipGroup.ProjectPosNoOffset(startDragWorld, facing);
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

       
                Vector2 fVec2  = new Vector2(-facingVector.Y, facingVector.X);

                var fleet = new ShipGroup();
                fleet.AssembleAdhocGroup(SelectedShipList, endDragWorld, startDragWorld, facing, fVec2, player);

                projectedGroup = fleet;
            }
        }

        private bool SelectedShipsRightMouseWasHeld()
        {
            if (!Input.RightMouseWasHeld) return false;
            ProjectingPosition = false;
            if (SelectedFleet != null && SelectedFleet.Owner == player)
            {
                SelectedSomethingTimer = 3f;
                MoveFleetToMouse(null, null);
            }
            else if (SelectedShip != null && SelectedShip?.loyalty == player)
            {
                player.GetEmpireAI().DefensiveCoordinator.Remove(SelectedShip);
                SelectedSomethingTimer = 3f;
                if (UnselectableShip())
                {
                    if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                        previousSelection = SelectedShip;
                    return true;
                }

                MoveShipToMouse();
            }
            else if (SelectedShipList.Count > 0)
            {
                SelectedSomethingTimer = 3f;
                foreach (Ship ship in SelectedShipList)
                {
                    if (ship.loyalty != player || UnselectableShip(ship))
                        return true;
                }

                GameAudio.AffirmativeClick();
                MoveShipGroupToMouse();
            }

            return true;

        }

        private bool SelectedShipsRightMouseClick()
        {
            Ship shipClicked = CheckShipClick(Input);
            Planet planetClicked = CheckPlanetClick();


            if (Input.RightMouseReleased && !Input.RightMouseWasHeld)
            {
                ProjectingPosition = false;

                if (SelectedFleet != null && SelectedFleet.Owner.isPlayer)
                {
                    SelectedSomethingTimer = 3f;
                    MoveFleetToMouse(planetClicked, shipClicked);
                }
                else if (SelectedShip != null && SelectedShip.loyalty.isPlayer)
                {
                    player.GetEmpireAI().DefensiveCoordinator.Remove(SelectedShip);
                    SelectedSomethingTimer = 3f;

                    if (shipClicked != null && shipClicked != SelectedShip)
                    {
                        if (UnselectableShip())
                            return true;

                        GameAudio.AffirmativeClick();
                        AttackSpecifcShip(SelectedShip, shipClicked);
                    }
                    else if (ShipPieMenu(shipClicked)) { } //i think i fd this up. come back to it later. 
                    else if (planetClicked != null) RightClickOnPlanet(SelectedShip, planetClicked, true);
                    else if (UnselectableShip()) return true;
                    else
                        MoveShipToMouse();
                }
                else if (SelectedShipList.Count > 0)
                {
                    SelectedSomethingTimer = 3f;
                    foreach (Ship ship in SelectedShipList)
                        if (UnselectableShip(ship) || !ship.loyalty.isPlayer)
                            return true;

                    GameAudio.AffirmativeClick();

                    if (shipClicked != null || planetClicked != null)
                    {
                        foreach (Ship selectedShip in SelectedShipList)
                        {
                            player.GetEmpireAI().DefensiveCoordinator.Remove(selectedShip);
                            RightClickOnShip(selectedShip, shipClicked);
                            RightClickOnPlanet(selectedShip, planetClicked);
                        }
                    }
                    else
                    {
                        SelectedSomethingTimer = 3f;
                        foreach (Ship ship2 in SelectedShipList)
                            if (UnselectableShip(ship2))
                                return true;

                        GameAudio.AffirmativeClick();
                        MoveShipGroupToMouse();
                    }
                }

                if (SelectedFleet != null || SelectedItem != null || SelectedShip != null || SelectedPlanet != null ||
                    SelectedShipList.Count != 0) return true;
                if (shipClicked == null || shipClicked.Mothership != null || shipClicked.isConstructor) return true;
                if (SelectedShip != null && previousSelection != SelectedShip &&
                    SelectedShip != shipClicked) //fbedard
                    previousSelection = SelectedShip;
                SelectedShip = shipClicked;
                ShipPieMenu(SelectedShip);
                return true;
            }

            return false;
        }

        private bool SelectShipClicks(InputState input)
        {
            foreach (ClickableShip clickableShip in ClickableShipsList)
            {
                if (!input.CursorPosition.InRadius(clickableShip.ScreenPos, clickableShip.Radius)) continue;

                if (clickableShip.shipToClick?.inSensorRange != true || pickedSomethingThisFrame) continue;

                pickedSomethingThisFrame = true;
                GameAudio.ShipClicked();
                SelectedSomethingTimer = 3f;

                if (SelectedShipList.Count > 0 && input.IsShiftKeyDown)
                {
                    if (SelectedShipList.Contains(clickableShip.shipToClick))
                    {
                        SelectedShipList.Remove(clickableShip.shipToClick);
                        return true;
                    }                        
                    
                    SelectedShipList.AddUnique(clickableShip.shipToClick);
                    return false;
                }
                
                SelectedShipList.Clear();
                SelectedShipList.AddUnique(clickableShip.shipToClick);
                SelectedShip = clickableShip.shipToClick;
                return true;
            }
            return false;
        }

        private void LeftClickOnClickableItem(InputState input)
        {
            if (input.ShipPieMenu)
            {
                ShipPieMenu(SelectedShip);
            }

            Vector2 vector2 = input.CursorPosition - pieMenu.Position;
            vector2.Y *= -1f;
            Vector2 selectionVector = vector2 / pieMenu.Radius;
            pieMenu.HandleInput(input, selectionVector);
            if (!input.LeftMouseClick || pieMenu.Visible)
                return;

            if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                previousSelection = SelectedShip;

            SelectedShip       = null;
            SelectedPlanet     = null;
            SelectedFleet      = null;
            SelectedSystem     = null;
            SelectedItem       = null;
            ProjectingPosition = false;
            projectedGroup     = null;
            //SelectedShipList.Clear();
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
                if (SelectedShipList[0] == playerShip)
                    LoadShipMenuNodes(1);
                else if (SelectedShipList[0].loyalty == player)
                    LoadShipMenuNodes(1);
                else
                    LoadShipMenuNodes(0);
                return;
            }

            if ((SelectedPlanet = CheckPlanetClick()) != null)
            {
                SelectedSomethingTimer = 3f;
                pInfoUI.SetPlanet(SelectedPlanet);
                if (input.LeftMouseDoubleClick)
                {
                    ViewPlanet();
                    SelectionBox = new Rectangle();
                }
                else
                    GameAudio.PlanetClicked();
                return;
            }

            if ((SelectedItem = CheckBuildItemClicked()) != null)
                GameAudio.BuildItemClicked();
        }
        private void HandleSelectionBox(InputState input)
        {
            if (input.LeftMouseClick)
                SelectionBox = new Rectangle(input.MouseCurr.X, input.MouseCurr.Y, 0, 0);
            if (SelectedShipList.Count == 1)
            {
                if (SelectedShip != null && previousSelection != SelectedShip &&
                    SelectedShip != SelectedShipList[0]) //fbedard
                    previousSelection = SelectedShip;
                SelectedShip          = SelectedShipList[0];
            }
            if (input.LeftMouseHeld() && (SelectingWithBox || !minimap.HitTest(input.CursorPosition)))
            {
                SelectingWithBox = true;
                if (SelectionBox.X == 0 || SelectionBox.Y == 0)
                    return;
                SelectionBox = new Rectangle(SelectionBox.X, SelectionBox.Y,
                    input.MouseCurr.X - SelectionBox.X, input.MouseCurr.Y - SelectionBox.Y);
                return;
            }
            if (!input.LeftMouseWasHeld || !SelectingWithBox)
            {
                SelectingWithBox = false;
                return;
            }

            if (input.MouseCurr.X < SelectionBox.X)
                SelectionBox.X  = input.MouseCurr.X;
            if (input.MouseCurr.Y < SelectionBox.Y)
                SelectionBox.Y  = input.MouseCurr.Y;
            SelectionBox.Width  = Math.Abs(SelectionBox.Width);
            SelectionBox.Height = Math.Abs(SelectionBox.Height);

            if (!GetAllShipsInArea(SelectionBox, out Array<Ship> ships, out bool purgeLoyalty, out bool purgeSupply,
                out Fleet fleet))
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
                if (SelectedShipList[0] == playerShip)
                    LoadShipMenuNodes(1);
                else if (SelectedShipList[0]?.loyalty == player)
                    LoadShipMenuNodes(1);
                else
                    LoadShipMenuNodes(0);
            }

            SelectionBox = new Rectangle(0, 0, -1, -1);
        }

        private bool NonCombatShip(Ship ship)
        {
            return ship != null && (ship.shipData.Role <= ShipData.RoleName.freighter ||
                ship.shipData.ShipCategory == ShipData.Category.Civilian ||
                ship.AI.State == AIState.Colonize);
        }

        private bool GetAllShipsInArea(Rectangle screenArea, out Array<Ship> ships, out bool purgeLoyalty, out bool purgeType, out Fleet fleet)
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
        
        private bool RightClickOnShip(Ship selectedShip, Ship targetShip)
        {
            if (targetShip == null || selectedShip == targetShip) return false;

            if (targetShip.loyalty == player)
            {
                if (selectedShip.DesignRole == ShipData.RoleName.troop)
                {
                    if (targetShip.TroopList.Count < targetShip.TroopCapacity)
                        selectedShip.AI.OrderTroopToShip(targetShip);
                    else
                        selectedShip.DoEscort(targetShip);
                }
                else
                    selectedShip.DoEscort(targetShip);
            }
            else if (selectedShip.DesignRole == ShipData.RoleName.troop)
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
                GameAudio.NegativeClick();
                return;
            }

            ship.AI.HasPriorityOrder = true;
            if (audio)
                GameAudio.PlaySfxAsync("echo_affirm1");
            if (ship.isColonyShip)
            {
                if (planet.Owner == null && planet.Habitable)
                    ship.AI.OrderColonization(planet);
                else
                    ship.AI.OrderToOrbit(planet, true);
            }
            else if (ship.DesignRole == ShipData.RoleName.troop ||
                     (ship.TroopList.Count > 0 && ship.DesignRole == ShipData.RoleName.troopShip))
            {
                if (planet.Owner != null && planet.Owner == player && (!ship.Carrier.HasTroopBays && !ship.Carrier.HasTransporters))
                {
                    if (Input.IsShiftKeyDown)
                        ship.AI.OrderToOrbit(planet, true);
                    else
                        ship.AI.OrderRebase(planet, true);
                }
                else if (planet.Habitable && (planet.Owner == null || ship.loyalty.IsEmpireAttackable(planet.Owner)))
                {
                    //add new right click troop and troop ship options on planets
                    if (Input.IsShiftKeyDown)
                        ship.AI.OrderToOrbit(planet, true);
                    else
                    {
                        ship.AI.State = AIState.AssaultPlanet;
                        ship.AI.OrderLandAllTroops(planet);
                    }
                }
                else
                    ship.AI.OrderOrbitPlanet(planet);
            }
            else if (ship.BombBays.Count > 0)
            {
                float enemies = planet.GetGroundStrengthOther(player) * 1.5f;
                float friendlies = planet.GetGroundStrength(player);
                if (planet.Owner != player)
                {
                    if (player.IsEmpireAttackable(planet.Owner))
                    {
                        if (Input.IsShiftKeyDown)
                            ship.AI.OrderBombardPlanet(planet);
                        else if (enemies > friendlies || planet.Population > 0f)
                            ship.AI.OrderBombardPlanet(planet);
                        else
                            ship.AI.OrderToOrbit(planet, true);
                    }
                    else
                        ship.AI.OrderToOrbit(planet, true);
                }
                else if (enemies > friendlies && Input.IsShiftKeyDown)
                    ship.AI.OrderBombardPlanet(planet);
                else
                    ship.AI.OrderToOrbit(planet, true);
            }
            else if (Input.IsShiftKeyDown)
                ship.AI.OrderToOrbit(planet, true);
            else
                ship.AI.OrderToOrbit(planet, true);
        }

        public void UpdateClickableItems()
        {
            lock (GlobalStats.ClickableItemLocker)
                ItemsToBuild.Clear();
            for (int index = 0; index < EmpireManager.Player.GetEmpireAI().Goals.Count; ++index)
            {
                Goal goal = player.GetEmpireAI().Goals[index];
                if (!(goal is BuildConstructionShip))
                    continue;
                const float radius = 100f;                    
                Vector2 buildPos = Viewport.Project(new Vector3(goal.BuildPosition, 0.0f), projection, view, Matrix.Identity).ToVec2();
                Vector3 buildOffSet = Viewport.Project(new Vector3(goal.BuildPosition.PointOnCircle(90f, radius), 0.0f), projection, view, Matrix.Identity);
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
            HandleScrolls(input);
            if (SelectedShip == null)
            {
                DefiningAO = false;
                return;
            }
            if (input.Escaped) //Easier out from defining an AO. Used to have to left and Right click at the same time.    -Gretman
            {
                DefiningAO = false;
                return;
            }
            if (input.RightMouseClick)
                for (int index = 0; index < SelectedShip.AreaOfOperation.Count; ++index)
                {
                    if (SelectedShip.AreaOfOperation[index].HitTest(UnprojectToWorldPosition(input.CursorPosition)))
                        SelectedShip.AreaOfOperation.Remove(SelectedShip.AreaOfOperation[index]);
                }

            Vector2 start;
            Vector2 end;
            if (input.LeftMouseHeld(0) && input.MouseDrag)
            {

                start  = UnprojectToWorldPosition(input.StartLeftHold);
                end    = UnprojectToWorldPosition(input.CursorPosition);
                AORect = new Rectangle((int) start.X, (int) start.Y, (int) (end.X - start.X), (int) (end.Y - start.Y));
                return;
            }
            if (!input.LeftMouseWasHeld)
            {
                AORect = new Rectangle();
                return;
            }
            start  = UnprojectToWorldPosition(input.StartLeftHold);
            end    = UnprojectToWorldPosition(input.EndLeftHold);
            AORect = new Rectangle((int) start.X, (int) start.Y, (int) (end.X - start.X), (int) (end.Y - start.Y));


            if (AORect.X > end.X)
                AORect.X = (int)end.X;
            if (AORect.Y > end.Y)
                AORect.Y = (int)end.Y;

            AORect.Width = Math.Abs(AORect.Width);
            AORect.Height = Math.Abs(AORect.Height);

            if (AORect.Width <= 5000 || AORect.Height <= 5000) return;

            GameAudio.PlaySfxAsync("echo_affirm");
            SelectedShip.AreaOfOperation.Add(AORect);            
        }

        private void InputClickableItems(InputState input)
        {
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
                        GameAudio.SubBassWhoosh();
                        SelectedPlanet = clickablePlanets.planetToClick;
                        if (!SnapBackToSystem)
                            HeightOnSnap = CamHeight;
                        ViewPlanet();
                    }
                }
            }
            foreach (ClickableShip clickableShip in ClickableShipsList)
            {
                if (!input.CursorPosition.InRadius(clickableShip.ScreenPos, clickableShip.Radius))
                    continue;
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
                        if (system.systemToClick.IsExploredBy(player))
                        {
                            GameAudio.OpenSolarSystemPopUp();
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
                        if (nbrship == lastshipcombat)
                        {
                            if (SelectedShip != null && SelectedShip != previousSelection && SelectedShip != ship)
                                previousSelection = SelectedShip;
                            SelectedShip = ship;
                            ViewToShip();
                            SelectedShipList.Add(SelectedShip);
                            lastshipcombat++;
                            break;
                        }

                        nbrship++;
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
                                    if (troop.GetOwner() != null && troop.GetOwner() == Empire.Universe.PlayerEmpire)
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
                    SelectedShip   = null;
                    SelectedFleet  = null;
                    SelectedItem   = null;
                    SelectedSystem = null;
                    SelectedPlanet = planetToView;
                    SelectedShipList.Clear();
                    pInfoUI.SetPlanet(planetToView);
                    lastplanetcombat++;

                    CamDestination = new Vector3(SelectedPlanet.Center.X, SelectedPlanet.Center.Y, 9000f);
                    transitionStartPosition = CamPos;
                    transitionElapsedTime   = 0.0f;
                    LookingAtPlanet = false;
                    AdjustCamTimer  = 2f;
                    transDuration   = 5f;
                    returnToShip    = false;
                    ViewingShip     = false;
                    snappingToShip  = false;
                    SelectedItem    = null;
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
            player.GetEmpireAI().Goals.QueuePendingRemoval(SelectedItem.AssociatedGoal);
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
                        if (queueItem.Goal != SelectedItem.AssociatedGoal) continue;

                        planet.ProdHere += queueItem.productionTowards;
                        planet.ConstructionQueue.QueuePendingRemoval(queueItem);
                    }
                    planet.ConstructionQueue.ApplyPendingRemovals();
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
                        GameAudio.PlaySfxAsync("blip_click");
                    }
                }
                ItemsToBuild.ApplyPendingRemovals();
            }
            player.GetEmpireAI().Goals.ApplyPendingRemovals();
            SelectedItem = null;
        }

        private void AddSelectedShipsToFleet(Fleet fleet)
        {
            using (fleet.Ships.AcquireWriteLock())
            {
                
                foreach (Ship ship in SelectedShipList)
                {
                    ship.ClearFleet();
                    if (ship.loyalty == player && !ship.isConstructor && ship.Mothership == null)  //fbedard: cannot add ships from hangar in fleet
                    {                        
                        ship.AI.OrderQueue.Clear();
                        ship.AI.ClearWayPoints();
                        ship.AI.ClearPriorityOrder();
                        fleet.Ships.Add(ship);
                    }
                }
                //fleet.StoredFleetDistancetoMove = 0;
                fleet.StoredFleetPosition = Vector2.Zero;
                fleet.AutoArrange();                
            }
            InputCheckPreviousShip();

            SelectedShip = null;
            SelectedShipList.Clear();
            if (fleet.Ships.Count > 0)
            {
                SelectedFleet = fleet;
                GameAudio.PlaySfxAsync("techy_affirm1");
            }
            else
                SelectedFleet = null;
            foreach (Ship ship in fleet.Ships)
            {
                SelectedShipList.Add(ship);
                ship.fleet = fleet;
            }
            
            shipListInfoUI.SetShipList(SelectedShipList, true);  //fbedard:display new fleet in UI            
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

        private void HandleEdgeDetection(InputState input)
        {
            if (LookingAtPlanet  )
                return;
            PresentationParameters p = ScreenManager.GraphicsDevice.PresentationParameters;
            Vector2 spaceFromScreenSpace1 = UnprojectToWorldPosition(new Vector2(0.0f, 0.0f));
            float num = UnprojectToWorldPosition(new Vector2(p.BackBufferWidth, p.BackBufferHeight)).X - spaceFromScreenSpace1.X;
            input.Repeat = true;

            if (input.CursorPosition.X <= 1f || input.Left && !ViewingShip)
            {
                CamDestination.X -= 0.008f * num;
                snappingToShip = false;
                ViewingShip    = false;
            }
            if (input.CursorPosition.X >= p.BackBufferWidth - 1 || input.Right && !ViewingShip)
            {
                CamDestination.X += 0.008f * num;
                snappingToShip = false;
                ViewingShip    = false;
            }
            if (input.CursorPosition.Y <= 0.0f || input.Up && !ViewingShip)
            {
                CamDestination.Y -= 0.008f * num;
                snappingToShip = false;
                ViewingShip    = false;
            }
            if (input.CursorPosition.Y >= p.BackBufferHeight - 1 || input.Down && !ViewingShip)
            {
                CamDestination.Y += 0.008f * num;
                snappingToShip = false;
                ViewingShip    = false;
            }
            input.Repeat = false;

            CamDestination.X = CamDestination.X.Clamped(-UniverseSize, UniverseSize);
            CamDestination.Y = CamDestination.Y.Clamped(-UniverseSize, UniverseSize);

        }

        private void HandleScrolls(InputState input)
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
                        transitionElapsedTime = 0.0f;
                    }
                    else
                    {
                        CamDestination.Z = 4200000f * GameScale;
                        AdjustCamTimer = 1f;
                        transitionElapsedTime = 0.0f;
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
                    CamDestination = new Vector3(SelectedFleet.FindAveragePosition().X, SelectedFleet.FindAveragePosition().Y, camDestinationZ);
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