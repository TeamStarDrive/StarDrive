using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Audio;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        private Vector2 CalculateCameraPositionOnMouseZoom(Vector2 MousePosition, float DesiredCamHeight)
        {
            Vector2 vector2_1 = MousePosition - ScreenCenter;
            Vector3 position1 = Viewport.Unproject(
                new Vector3(MousePosition.X, MousePosition.Y, 0.0f), Projection, this.View, Matrix.Identity);
            Vector3 direction1 =
                Viewport.Unproject(new Vector3(MousePosition.X, MousePosition.Y, 1f),
                    Projection, this.View, Matrix.Identity) - position1;

            direction1.Normalize();
            Ray ray = new Ray(position1, direction1);
            float num1 = -ray.Position.Z / ray.Direction.Z;
            Vector3 source = new Vector3(ray.Position.X + num1 * ray.Direction.X,
                ray.Position.Y + num1 * ray.Direction.Y, 0.0f);

            Matrix view = Matrix.CreateTranslation(0.0f, 0.0f, 0.0f) * Matrix.CreateRotationY(180f.ToRadians()) *
                          Matrix.CreateRotationX(0.0f.ToRadians()) *
                          Matrix.CreateLookAt(new Vector3(CamPos.X, CamPos.Y, DesiredCamHeight),
                              new Vector3(CamPos.X, CamPos.Y, 0.0f), new Vector3(0.0f, -1f, 0.0f));
            
            Vector3 vector3 = Viewport.Project(source, Projection, view, Matrix.Identity);
            var vector2_2 = new Vector2((int) vector3.X - vector2_1.X, (int) vector3.Y - vector2_1.Y);
            Vector3 position2 = Viewport.Unproject(new Vector3(vector2_2.X, vector2_2.Y, 0.0f), Projection, view, Matrix.Identity);
            Vector3 direction2 =
                Viewport.Unproject(new Vector3(vector2_2.X, vector2_2.Y, 1f),
                    Projection, view, Matrix.Identity) - position2;
            direction2.Normalize();
            ray = new Ray(position2, direction2);
            float num2 = -ray.Position.Z / ray.Direction.Z;
            return new Vector2(ray.Position.X + num2 * ray.Direction.X, ray.Position.Y + num2 * ray.Direction.Y);
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
            CamDestination.Z = GetZfromScreenState(UniverseScreen.UnivScreenState.DetailView);
            AdjustCamTimer = 1.0f;
            transitionElapsedTime = 0.0f;
            CamDestination.Z = GetZfromScreenState(UniverseScreen.UnivScreenState.PlanetView); ;
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

            if (!SelectedPlanet.ParentSystem.IsExploredBy(player))
            {
                GameAudio.NegativeClick();
            }
            else
            {
                bool flag = false;
                if (SelectedPlanet.Owner == player && combatView)
                {
                    OpenCombatMenu();
                    return;
                }                    

                foreach (Mole mole in player.data.MoleList)
                {
                    if (mole.PlanetGuid == SelectedPlanet.guid)
                    {
                        flag = true;
                        break;
                    }
                }

                if (SelectedPlanet.Owner == player || flag || Debug)
                {
                    if (SelectedPlanet.Owner != null)
                        workersPanel = new ColonyScreen(this, SelectedPlanet, EmpireUI);
                    else
                        workersPanel = new UnexploredPlanetScreen(this, SelectedPlanet);
                }
                else if (combatView && SelectedPlanet.Habitable
                                    && SelectedPlanet.IsExploredBy(player)
                                    && (SelectedPlanet.WeAreInvadingHere(player) || !player.DifficultyModifiers.HideTacticalData
                                                                                 || SelectedPlanet.ParentSystem.OwnerList.Contains(player)
                                                                                 || SelectedPlanet.OurShipsCanScanSurface(player)))

                {
                    OpenCombatMenu();
                }
                else
                {
                    workersPanel = new UnownedPlanetScreen(this, SelectedPlanet);
                }

                LookingAtPlanet = true;
                transitionStartPosition = CamPos;
                CamDestination = new Vector3(SelectedPlanet.Center.X, SelectedPlanet.Center.Y + 400f, 2500f);
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
            float x = GetZfromScreenState(camHeight);
            CamDestination = new Vector3(system.Position.X, system.Position.Y + 400f, x);
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

            CamDestination  = new Vector3(SelectedShip.Center.X, SelectedShip.Center.Y + 400f, 2500f);
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
            CamDestination        = new Vector3(system.Position, 147000f);
            ViewingShip           = false;
            AdjustCamTimer        = 1f;
            transDuration         = 3f;
            transitionElapsedTime = 0.0f;
        }

        private void ViewPlanet(UnivScreenState zoomLevel)
        {
            CamDestination        = new Vector3(SelectedPlanet.Center, GetZfromScreenState(zoomLevel));
            ViewingShip           = false;
            SelectedFleet         = null;
            SelectedItem          = null;
            AdjustCamTimer        = 1f;
            transDuration         = 3f;
            transitionElapsedTime = 0.0f;
        }

        private void ViewFleet(UnivScreenState zoomLevel)
        {
            CamDestination        = new Vector3(SelectedFleet.AveragePosition(), GetZfromScreenState(zoomLevel));
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
                CamPos.X = ShipToView.Center.X;
                CamPos.Y = ShipToView.Center.Y;
                CamHeight =
                    (int) MathHelper.SmoothStep(CamHeight, CamDestination.Z, 0.2f);
                if (CamHeight < minCamHeight)
                    CamHeight = minCamHeight;
            }
            if (AdjustCamTimer > 0.0)
            {
                if (ShipToView == null)
                    snappingToShip = false;
                if (snappingToShip)
                {
                    CamDestination.X = ShipToView.Center.X;
                    CamDestination.Y = ShipToView.Center.Y;
                    transitionElapsedTime += elapsedTime;
                    float amount = (float) Math.Pow(transitionElapsedTime / (double) transDuration, 0.699999988079071);
                    camTransitionPosition.X = MathHelper.SmoothStep(CamPos.X, CamDestination.X, amount);
                    float num1 = MathHelper.SmoothStep(CamPos.Y, CamDestination.Y, amount);
                    float num2 = MathHelper.SmoothStep(CamHeight, CamDestination.Z, amount);
                    camTransitionPosition.Y = num1;
                    CamHeight = (int) num2;
                    CamPos = camTransitionPosition;
                    if (AdjustCamTimer - (double) elapsedTime <= 0.0)
                    {
                        ViewingShip = true;
                        transitionElapsedTime = 0.0f;
                        AdjustCamTimer = -1f;
                        snappingToShip = false;
                    }
                }
                else
                {
                    transitionElapsedTime += elapsedTime;
                    float amount = (float) Math.Pow(transitionElapsedTime / (double) transDuration, 0.699999988079071);
                    camTransitionPosition.X = MathHelper.SmoothStep(CamPos.X, CamDestination.X, amount);
                    float num1 = MathHelper.SmoothStep(CamPos.Y, CamDestination.Y, amount);
                    float num2 = MathHelper.SmoothStep(CamHeight, CamDestination.Z, amount);
                    camTransitionPosition.Y = num1;
                    CamHeight = num2;
                    CamPos = camTransitionPosition;
                    if (transitionElapsedTime > (double) transDuration ||
                        Vector2.Distance(new Vector2(CamPos.X, CamPos.Y),
                            new Vector2(CamDestination.X, CamDestination.Y)) < 50.0 &&
                        Math.Abs(CamHeight - CamDestination.Z) < 50.0)
                    {
                        transitionElapsedTime = 0.0f;
                        AdjustCamTimer = -1f;
                    }
                }
                if (CamHeight < minCamHeight)
                    CamHeight = minCamHeight;
            }
            else if (LookingAtPlanet && SelectedPlanet != null)
            {
                camTransitionPosition.X =
                    MathHelper.SmoothStep(CamPos.X, SelectedPlanet.Center.X, 0.2f);
                camTransitionPosition.Y =
                    MathHelper.SmoothStep(CamPos.Y, SelectedPlanet.Center.Y + 400f, 0.2f);
                CamPos = camTransitionPosition;
            }
            else if (!ViewingShip)
            {
                camTransitionPosition.X = MathHelper.SmoothStep(CamPos.X, CamDestination.X, 0.2f);
                float num1 = MathHelper.SmoothStep(CamPos.Y, CamDestination.Y, 0.2f);
                float num2 = MathHelper.SmoothStep(CamHeight, CamDestination.Z, 0.2f);
                camTransitionPosition.Y = num1;
                CamHeight = num2;
                if (CamHeight < minCamHeight)
                    CamHeight = minCamHeight;
                CamPos = camTransitionPosition;
            }

            CamPos.X = CamPos.X.Clamped(-UniverseSize, +UniverseSize);
            CamPos.Y = CamPos.Y.Clamped(-UniverseSize, +UniverseSize);
            CamHeight = CamHeight.Clamped(minCamHeight, MaxCamHeight);

            foreach (UnivScreenState screenHeight in Enum.GetValues(typeof(UnivScreenState)))
            {
                if (CamHeight <= GetZfromScreenState(screenHeight))
                {
                    viewState = screenHeight;
                    break;
                }
            }
        }

        public void InputZoomToShip()
        {
            GameAudio.AcceptClick();
            if (SelectedShip != null)
            {
                ViewingShip = false;
                ChaseCam();
            }
            else if (SelectedPlanet != null)
            {
                ViewPlanet( UnivScreenState.PlanetView);
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
            if (CamHeight < GetZfromScreenState(UnivScreenState.GalaxyView) &&
                CamHeight > GetZfromScreenState(UnivScreenState.SectorView))
            {
                AdjustCamTimer = 1f;
                transitionElapsedTime = 0.0f;
                CamDestination = new Vector3(CamPos.X, CamPos.Y, 1175000f);
            }
            else if (CamHeight > GetZfromScreenState(UnivScreenState.ShipView))
            {
                AdjustCamTimer = 1f;
                transitionElapsedTime = 0.0f;
                CamDestination = new Vector3(CamPos.X, CamPos.Y, 147000f);
            }
            else if (viewState < UnivScreenState.SystemView)
                CamDestination = new Vector3(CamPos.X, CamPos.Y,
                    GetZfromScreenState(UnivScreenState.SystemView));
        }

        void ChaseCam()
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
                StarDriveGame.Instance.SetCinematicCursor();
            }
            else
            {
                StarDriveGame.Instance.SetGameCursor();
            }
            IsCinematicModeEnabled = !IsCinematicModeEnabled;
        }
    }
}