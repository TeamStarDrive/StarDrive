using System;
using SDGraphics;
using Ship_Game.Audio;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        Vector3d GetCameraPosFromCursorTarget(InputState input, double desiredCamZ)
        {
            Vector2 mousePos = input.CursorPosition;

            double currentZ = CamPos.Z;
            if (currentZ == desiredCamZ) // currentZ cannot equal desiredCamZ, or we'll get NaN-s
                currentZ += 1.0;

            // nearPoint is the point inside the camera lens
            Vector3d nearPoint = Viewport.Unproject(new(mousePos, currentZ), Projection, View);
            // farPoint points away into the world
            Vector3d farPoint = Viewport.Unproject(new(mousePos, desiredCamZ), Projection, View);

            // get the direction towards the world plane
            Vector3d dir = (farPoint - nearPoint).Normalized();

            double num = -nearPoint.Z / dir.Z;
            Vector3d pos2 = (nearPoint + dir * num);
            if (double.IsNaN(pos2.X) || double.IsNaN(pos2.Y))
            {
                Log.Error("CameraPos NaN!!!");

                // TODO: this is here to avoid a fatal View matrix corruption
                CamPos = new(0, 0, desiredCamZ);
                Matrix cameraMatrix = Matrices.CreateLookAtDown(CamPos.X, CamPos.Y, -CamPos.Z);
                SetViewMatrix(cameraMatrix);
                return CamPos;
            }

            double newX = (pos2.X + CamPos.X) / 2.0;
            double newY = (pos2.Y + CamPos.Y) / 2.0;
            return new(newX, newY, desiredCamZ);
        }

        public void ViewToShip()
        {
            if (SelectedShip == null)
                return;

            ShipToView = SelectedShip;
            ShipInfoUIElement.SetShip(SelectedShip); //fbedard: was not updating correctly from shiplist
            SelectedFleet = null;
            SelectedShipList.Clear();
            SelectedItem = null;
            SelectedSystem = null;
            SelectedPlanet = null;
            snappingToShip = true;
            AdjustCamTimer = 1.0f;
            transitionElapsedTime = 0.0f;
            CamDestination.Z = CamDestination.Z.UpperBound(GetZfromScreenState(UnivScreenState.PlanetView));
            snappingToShip = true;
            ViewingShip = true;
        }

        public void SnapViewColony() => SnapViewColony(true);

        public void SnapViewColony(bool combatView)
        {
            ShowShipNames = false;
            if (SelectedPlanet == null)
                return;

            if (combatView && Debug)
            {
                OpenCombatMenu();
                return;
            }

            if (!SelectedPlanet.ParentSystem.IsExploredBy(Player))
            {
                GameAudio.NegativeClick();
            }
            else
            {
                bool flag = false;
                if (SelectedPlanet.Owner == Player && combatView 
                    || SelectedPlanet.Owner != Player && Player.data.MoleList.Any(m => m.PlanetId == SelectedPlanet.Id) && combatView)
                {
                    OpenCombatMenu();
                    return;
                }                    

                foreach (Mole mole in Player.data.MoleList)
                {
                    if (mole.PlanetId == SelectedPlanet.Id)
                    {
                        flag = true;
                        break;
                    }
                }

                if (SelectedPlanet.Owner == Player || flag || Debug)
                {
                    if (SelectedPlanet.Owner != null)
                        workersPanel = new ColonyScreen(this, SelectedPlanet, EmpireUI);
                    else
                        workersPanel = new UnexploredPlanetScreen(this, SelectedPlanet);
                }
                else if (combatView && SelectedPlanet.Habitable
                                    && SelectedPlanet.IsExploredBy(Player)
                                    && (SelectedPlanet.WeAreInvadingHere(Player) || !Player.DifficultyModifiers.HideTacticalData
                                                                                 || SelectedPlanet.ParentSystem.OwnerList.Contains(Player)
                                                                                 || SelectedPlanet.OurShipsCanScanSurface(Player)))

                {
                    OpenCombatMenu();
                }
                else
                {
                    workersPanel = new UnownedPlanetScreen(this, SelectedPlanet);
                }

                LookingAtPlanet = true;
                transitionStartPosition = CamPos;
                CamDestination = new Vector3d(SelectedPlanet.Position.X, SelectedPlanet.Position.Y + 400f, 2500f);
                AdjustCamTimer = 2f;
                transitionElapsedTime = 0.0f;
                transDuration = 5f;
                if (ViewingShip)
                    returnToShip = true;
                ViewingShip = false;
                snappingToShip = false;
                SelectedFleet = null;
                if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                    previousSelection = SelectedShip;
                SelectedShip = null;
                SelectedItem = null;
                SelectedShipList.Clear();

            }
        }

        public void SnapViewSystem(SolarSystem system, UnivScreenState camHeight)
        {
            CamDestination = new Vector3d(system.Position.X, system.Position.Y + 400f, GetZfromScreenState(camHeight));
            transitionStartPosition = CamPos;
            AdjustCamTimer = 2f;
            transitionElapsedTime = 0.0f;
            transDuration = 5f;
            ViewingShip = false;
            snappingToShip = false;
            if (ViewingShip)
                returnToShip = true;
            ViewingShip = false;
            snappingToShip = false;
            SelectedFleet = null;
            if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                previousSelection = SelectedShip;
            SelectedShip = null;
            SelectedShipList.Clear();
            SelectedItem = null;
        }

        public void SnapViewShip(object sender)
        {
            ShowShipNames = false;
            if (SelectedShip == null)
                return;

            CamDestination = new Vector3d(SelectedShip.Position.X, SelectedShip.Position.Y + 400f, 2500);
            LookingAtPlanet = false;
            transitionStartPosition = CamPos;
            AdjustCamTimer  = 2f;
            transitionElapsedTime = 0.0f;
            transDuration  = 5f;
            snappingToShip = true;
            ViewingShip    = true;
            SelectedFleet  = null;
            SelectedItem   = null;
            SelectedShipList.Clear();
        }

        private void ViewSystem(SolarSystem system)
        {
            CamDestination        = new Vector3d(system.Position, 147000f);
            ViewingShip           = false;
            AdjustCamTimer        = 1f;
            transDuration         = 3f;
            transitionElapsedTime = 0.0f;
        }

        private void ViewPlanet(UnivScreenState zoomLevel)
        {
            CamDestination        = new Vector3d(SelectedPlanet.Position, GetZfromScreenState(zoomLevel));
            ViewingShip           = false;
            SelectedFleet         = null;
            SelectedItem          = null;
            AdjustCamTimer        = 1f;
            transDuration         = 3f;
            transitionElapsedTime = 0.0f;
        }

        private void ViewFleet(UnivScreenState zoomLevel)
        {
            CamDestination        = new Vector3d(SelectedFleet.AveragePosition(), GetZfromScreenState(zoomLevel));
            ViewingShip           = false;
            SelectedItem          = null;
            AdjustCamTimer        = 1f;
            transDuration         = 3f;
            transitionElapsedTime = 0.0f;
        }

        private void AdjustCamera(float elapsedTime)
        {
            if (ShipToView == null) 
                ViewingShip = false;

            #if DEBUG
                float minCamHeight = 400.0f;
            #else
                float minCamHeight = Debug ? 1337.0f : 400.0f;
            #endif

            AdjustCamTimer -= elapsedTime;
            if (ViewingShip && !snappingToShip)
            {
                UState.CamPos.X = ShipToView.Position.X;
                UState.CamPos.Y = ShipToView.Position.Y;
                UState.CamPos.Z = UState.CamPos.Z.SmoothStep(CamDestination.Z, 0.2);
                if (UState.CamPos.Z < minCamHeight)
                    UState.CamPos.Z = minCamHeight;
            }

            if (AdjustCamTimer > 0.0)
            {
                if (ShipToView == null)
                    snappingToShip = false;

                transitionElapsedTime += elapsedTime;
                double amount = Math.Pow(transitionElapsedTime / (double)transDuration, 0.7);

                if (snappingToShip && ShipToView != null)
                {
                    CamDestination.X = ShipToView.Position.X;
                    CamDestination.Y = ShipToView.Position.Y;
                    CamPos = CamPos.SmoothStep(CamDestination, amount);

                    if (AdjustCamTimer - elapsedTime <= 0f)
                    {
                        ViewingShip = true;
                        transitionElapsedTime = 0.0f;
                        AdjustCamTimer = -1f;
                        snappingToShip = false;
                    }
                }
                else
                {
                    CamPos = CamPos.SmoothStep(CamDestination, amount);

                    if (transitionElapsedTime > transDuration ||
                        CamPos.ToVec2f().Distance(CamDestination.ToVec2f()) < 50f &&
                        Math.Abs(CamPos.Z - CamDestination.Z) < 50f)
                    {
                        transitionElapsedTime = 0.0f;
                        AdjustCamTimer = -1f;
                    }
                }
                if (UState.CamPos.Z < minCamHeight)
                    UState.CamPos.Z = minCamHeight;
            }
            else if (LookingAtPlanet && SelectedPlanet != null)
            {
                UState.CamPos.X = UState.CamPos.X.SmoothStep(SelectedPlanet.Position.X, 0.2);
                UState.CamPos.Y = UState.CamPos.Y.SmoothStep(SelectedPlanet.Position.Y + 400f, 0.2);
            }
            else if (!ViewingShip) // regular free camera movement in Universe
            {
                UState.CamPos = UState.CamPos.SmoothStep(CamDestination, 0.2);
                if (UState.CamPos.Z < minCamHeight)
                    UState.CamPos.Z = minCamHeight;
            }

            UState.CamPos.X = UState.CamPos.X.Clamped(-UState.Size, +UState.Size);
            UState.CamPos.Y = UState.CamPos.Y.Clamped(-UState.Size, +UState.Size);
            UState.CamPos.Z = UState.CamPos.Z.Clamped(minCamHeight, MaxCamHeight);

            //Log.Write(ConsoleColor.Green, $"CamPos {CamPos.X:0.00} {CamPos.Y:0.00} {CamPos.Z:0.00}  Dest {CamDestination.X:0.00} {CamDestination.Y:0.00} {CamDestination.Z:0.00}");

            var newViewState = UnivScreenState.DetailView;
            foreach (UnivScreenState state in Enum.GetValues(typeof(UnivScreenState)))
            {
                if (CamPos.Z <= GetZfromScreenState(state))
                {
                    newViewState = state;
                    break;
                }
            }

            // We reset the Perspective Matrix because at close zoom levels
            // we need to reduce the MaxDistance of the Projection matrix
            // Otherwise our screen projection is extremely inaccurate due to float errors
            if (viewState != newViewState)
            {
                viewState = newViewState;

                const double maxDetailNebulaDist = 15_000_000;
                double maxDistance = maxDetailNebulaDist;
                switch (newViewState)
                {
                    case UnivScreenState.DetailView: maxDistance += (int)UnivScreenState.ShipView; break;
                    case UnivScreenState.ShipView:   maxDistance += (int)UnivScreenState.PlanetView; break;
                    case UnivScreenState.PlanetView: maxDistance += (int)UnivScreenState.SystemView; break;
                    case UnivScreenState.SystemView: maxDistance += (int)UnivScreenState.SectorView; break;
                    case UnivScreenState.SectorView: maxDistance += (int)UnivScreenState.GalaxyView; break;
                    case UnivScreenState.GalaxyView: maxDistance += maxDetailNebulaDist; break;
                }

                //Log.Info($"View: {newViewState} MaxDistance: {maxDistance}  CamHeight: {CamPos.Z}");
                SetPerspectiveProjection(maxDistance: maxDistance);
            }
        }

        public void InputZoomToShip()
        {
            GameAudio.AcceptClick();
            if (SelectedShip != null)
            {
                ViewingShip = false;
                ToggleViewingShip();
            }
            else if (SelectedPlanet != null)
            {
                ViewPlanet(UnivScreenState.PlanetView);
            }
            else if (SelectedSystem != null)
            {
                ViewSystem(SelectedSystem);
            }
            else if (SelectedFleet != null)
            {
                ViewFleet(UnivScreenState.PlanetView);
            }
        }

        public void InputZoomOut()
        {
            GameAudio.AcceptClick();
            AdjustCamTimer = 1f;
            transitionElapsedTime = 0.0f;
            CamDestination.X = CamPos.X;
            CamDestination.Y = CamPos.Y;
            CamDestination.Z = 4200000f;
        }

        void DefaultZoomPoints()
        {
            snappingToShip = false;
            ViewingShip = false;
            if (CamPos.Z < GetZfromScreenState(UnivScreenState.GalaxyView) &&
                CamPos.Z > GetZfromScreenState(UnivScreenState.SectorView))
            {
                AdjustCamTimer = 1f;
                transitionElapsedTime = 0.0f;
                CamDestination = new(CamPos.X, CamPos.Y, 1175000.0);
            }
            else if (CamPos.Z > GetZfromScreenState(UnivScreenState.ShipView))
            {
                AdjustCamTimer = 1f;
                transitionElapsedTime = 0.0f;
                CamDestination = new(CamPos.X, CamPos.Y, 147000.0);
            }
            else if (viewState < UnivScreenState.SystemView)
            {
                CamDestination = new(CamPos.X, CamPos.Y, GetZfromScreenState(UnivScreenState.SystemView));
            }
        }

        void ToggleViewingShip()
        {
            if (!ViewingShip)
            {
                ViewToShip();
            }
            ViewingShip = !ViewingShip;
        }

        void ToggleCinematicMode()
        {
            if (!IsCinematicModeEnabled)
            {
                CinematicModeTextTimer = 3;
            }
            IsCinematicModeEnabled = !IsCinematicModeEnabled;
        }
    }
}