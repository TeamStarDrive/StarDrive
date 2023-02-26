using System;
using SDGraphics;
using Ship_Game.Audio;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        public Vector3d GetNewCameraPos(Vector3d currentCamPos3d, Vector2 targetScreenPos, double desiredZ)
        {
            double currentZ = currentCamPos3d.Z;
            if (currentZ.AlmostEqual(desiredZ))
                return currentCamPos3d; // already there

            Vector3d targetWorldPos = UnprojectToWorldPosition3D(targetScreenPos);
            targetWorldPos.Z = desiredZ;
            return targetWorldPos;
        }

        Vector3d GetCameraPosFromCursorTarget(InputState input, double desiredCamZ)
        {
            Vector2 targetScreenPos = input.CursorPosition;
            Vector3d newPos = GetNewCameraPos(CamPos, targetScreenPos, desiredCamZ);

            // TODO: this happens quite rarely, but if it does, it's game-breaking
            if (double.IsNaN(newPos.X) || double.IsNaN(newPos.Y) || double.IsNaN(newPos.Z))
            {
                Log.Error($"New CameraPos NaN! CamPos:{CamPos} targetScreenPos:{targetScreenPos} desiredCamZ:{desiredCamZ}");

                // TODO: this is here to avoid a fatal View matrix corruption
                CamPos = new(0, 0, desiredCamZ);
                Matrix cameraMatrix = Matrices.CreateLookAtDown(CamPos.X, CamPos.Y, -CamPos.Z);
                SetViewMatrix(cameraMatrix);
                return CamPos;
            }

            // this decides how fast we zoom-average towards new camera position
            const double NewPosRate = 0.5;
            double newX = (newPos.X*NewPosRate + CamPos.X*(1.0 - NewPosRate));
            double newY = (newPos.Y*NewPosRate + CamPos.Y*(1.0 - NewPosRate));
            return new(newX, newY, desiredCamZ);
        }

        public void ViewToShip(Ship ship)
        {
            if (ship == null)
                return;

            SetSelectedShip(ship);

            ShipToView = ship;
            AdjustCamTimer = 1.0f;
            transitionElapsedTime = 0.0f;
            CamDestination.Z = CamDestination.Z.UpperBound(GetZfromScreenState(UnivScreenState.PlanetView));
            snappingToShip = true;
            ViewingShip = true;
        }

        public void SnapViewColony(Planet p, bool combatView)
        {
            ShowShipNames = false;
            bool doReturnToShip = ViewingShip;
            SetSelectedPlanet(p);
            if (p == null)
                return;

            if (combatView && Debug)
            {
                OpenCombatMenu(p);
                return;
            }

            if (!p.ParentSystem.IsExploredBy(Player))
            {
                GameAudio.NegativeClick();
            }
            else
            {
                bool flag = false;
                if (p.Owner == Player && combatView ||
                    p.Owner != Player && Player.data.MoleList.Any(m => m.PlanetId == p.Id) && combatView)
                {
                    OpenCombatMenu(p);
                    return;
                }

                foreach (Mole mole in Player.data.MoleList)
                {
                    if (mole.PlanetId == p.Id)
                    {
                        flag = true;
                        break;
                    }
                }

                if (p.Owner == Player || flag || Debug)
                {
                    if (p.Owner != null)
                        workersPanel = new ColonyScreen(this, p, EmpireUI);
                    else
                        workersPanel = new UnexploredPlanetScreen(this, p);
                }
                else if (combatView && p.Habitable
                                    && p.IsExploredBy(Player)
                                    && (p.WeAreInvadingHere(Player) || !Player.DifficultyModifiers.HideTacticalData
                                                                    || p.ParentSystem.OwnerList.Contains(Player)
                                                                    || p.OurShipsCanScanSurface(Player)))
                {
                    OpenCombatMenu(p);
                }
                else
                {
                    workersPanel = new UnownedPlanetScreen(this, p);
                }

                ClearSelectedItems();
                returnToShip = doReturnToShip;
                LookingAtPlanet = true;

                SnapViewTo(new(p.Position.X, p.Position.Y + 400f, 2500f), 5f, 2f);
            }
        }

        public void SnapViewTo(Vector3d worldPos, float duration, float adjustCamTimer = 2f)
        {
            CamDestination = worldPos;
            AdjustCamTimer = adjustCamTimer;

            transitionStartPosition = CamPos;
            transitionElapsedTime = 0.0f;
            transDuration = duration;
        }

        public void SnapViewSystem(SolarSystem s, Planet p, UnivScreenState camHeight)
        {
            double z = GetZfromScreenState(camHeight);
            SnapViewTo(new(s.Position.X, s.Position.Y + 400f, z), 5f, 2f);

            bool doReturnToShip = ViewingShip;
            SetSelectedSystem(s, p);
            returnToShip = doReturnToShip;
        }

        public void SnapViewShip(Ship s)
        {
            ShowShipNames = false;
            SetSelectedShip(s);
            if (s == null)
                return;

            SnapViewTo(new(s.Position.X, s.Position.Y + 400, 2500), 5f, 2f);
            LookingAtPlanet = false;
            snappingToShip = true;
            ViewingShip = true;
        }

        void ViewSystem(SolarSystem s)
        {
            SnapViewTo(new(s.Position, 147000f), 3f, 1f);
        }

        void ViewPlanet(Planet p, UnivScreenState zoomLevel)
        {
            SetSelectedPlanet(p);
            SnapViewTo(new(p.Position, GetZfromScreenState(zoomLevel)), 3f, 1f);
        }

        void ViewFleet(Fleet f, UnivScreenState zoomLevel)
        {
            SnapViewTo(new(f.AveragePosition(), GetZfromScreenState(zoomLevel)), 3f, 1f);
        }

        void AdjustCamera(float elapsedTime)
        {
            if (ShipToView == null)
                ViewingShip = false;

            #if DEBUG
                float minCamHeight = 400.0f;
            #else
                float minCamHeight = Debug ? 1337.0f : 400.0f;
            #endif

            AdjustCamTimer -= elapsedTime;
            if (ViewingShip && !snappingToShip && ShipToView != null)
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
                ViewToShip(SelectedShip);
            }
            else if (SelectedPlanet != null)
            {
                ViewPlanet(SelectedPlanet, UnivScreenState.PlanetView);
            }
            else if (SelectedSystem != null)
            {
                ViewSystem(SelectedSystem);
            }
            else if (SelectedFleet != null)
            {
                ViewFleet(SelectedFleet, UnivScreenState.PlanetView);
            }
        }

        public void InputZoomOut()
        {
            GameAudio.AcceptClick();
            AdjustCamTimer = 1f;
            transitionElapsedTime = 0f;
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
                transitionElapsedTime = 0f;
                CamDestination = new(CamPos.X, CamPos.Y, 1175000.0);
            }
            else if (CamPos.Z > GetZfromScreenState(UnivScreenState.ShipView))
            {
                AdjustCamTimer = 1f;
                transitionElapsedTime = 0f;
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
                ViewToShip(SelectedShip);
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