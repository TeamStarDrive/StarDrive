using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        private Vector2 CalculateCameraPositionOnMouseZoom(Vector2 MousePosition, float DesiredCamHeight)
        {
            Vector2 vector2_1 = new Vector2(
                MousePosition.X - ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth /
                2,
                MousePosition.Y -
                ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2);
            Vector3 position1 = Viewport.Unproject(
                new Vector3(MousePosition.X, MousePosition.Y, 0.0f), projection, this.view, Matrix.Identity);
            Vector3 direction1 =
                Viewport.Unproject(new Vector3(MousePosition.X, MousePosition.Y, 1f),
                    projection, this.view, Matrix.Identity) - position1;
            direction1.Normalize();
            Ray ray = new Ray(position1, direction1);
            float num1 = -ray.Position.Z / ray.Direction.Z;
            Vector3 source = new Vector3(ray.Position.X + num1 * ray.Direction.X,
                ray.Position.Y + num1 * ray.Direction.Y, 0.0f);
            Matrix view = Matrix.CreateTranslation(0.0f, 0.0f, 0.0f) * Matrix.CreateRotationY(180f.ToRadians()) *
                          Matrix.CreateRotationX(0.0f.ToRadians()) *
                          Matrix.CreateLookAt(new Vector3(CamPos.X, CamPos.Y, DesiredCamHeight),
                              new Vector3(CamPos.X, CamPos.Y, 0.0f), new Vector3(0.0f, -1f, 0.0f));
            Vector3 vector3 =
                Viewport.Project(source, projection, view, Matrix.Identity);
            Vector2 vector2_2 = new Vector2((int) vector3.X - vector2_1.X,
                (int) vector3.Y - vector2_1.Y);
            Vector3 position2 = Viewport.Unproject(
                new Vector3(vector2_2.X, vector2_2.Y, 0.0f), projection, view, Matrix.Identity);
            Vector3 direction2 =
                Viewport.Unproject(new Vector3(vector2_2.X, vector2_2.Y, 1f),
                    projection, view, Matrix.Identity) - position2;
            direction2.Normalize();
            ray = new Ray(position2, direction2);
            float num2 = -ray.Position.Z / ray.Direction.Z;
            return new Vector2(ray.Position.X + num2 * ray.Direction.X, ray.Position.Y + num2 * ray.Direction.Y);
        }

        public void FollowPlayer()
        {
            SelectedShip.AI.State = AIState.Escort;
            SelectedShip.AI.EscortTarget = playerShip;
        }

        public void ViewShip()
        {
            if (SelectedShip == null)
                return;
            if (playerShip != null && SelectedShip == playerShip)
            {
                playerShip.PlayerShip = false;
                playerShip.AI.State = AIState.AwaitingOrders;
                playerShip = null;
            }
            else
            {
                if (SelectedShip.loyalty != player || SelectedShip.isConstructor)
                    return;
                ShipToView = SelectedShip;
                snappingToShip = true;
                HeightOnSnap = CamHeight;
                CamDestination.Z = 3500f;
                if (playerShip != null)
                {
                    playerShip.PlayerShip = false;
                    playerShip.AI.State = AIState.AwaitingOrders;
                    playerShip = SelectedShip;
                    playerShip.PlayerShip = true;
                    playerShip.AI.State = AIState.ManualControl;
                }
                else
                {
                    playerShip = SelectedShip;
                    playerShip.PlayerShip = true;
                    playerShip.AI.State = AIState.ManualControl;
                }
                AdjustCamTimer = 1.5f;
                transitionElapsedTime = 0.0f;
                CamDestination.Z = 4500f;
                snappingToShip = true;
                ViewingShip = true;
                if (!playerShip.isSpooling)
                    return;
                playerShip.HyperspaceReturn();
            }
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
            HeightOnSnap = CamHeight;
            CamDestination.Z = 3500f;
            AdjustCamTimer = 1.0f;
            transitionElapsedTime = 0.0f;
            CamDestination.Z = 4500f;
            snappingToShip = true;
            ViewingShip = true;
        }

        public void ViewPlanet()
        {
            ShowShipNames = false;
            if (SelectedPlanet == null)
                return;
            if (!SelectedPlanet.ParentSystem.IsExploredBy(player))
            {
                PlayNegativeSound();
            }
            else
            {
                bool flag = false;
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
                else
                    workersPanel = new UnownedPlanetScreen(this, SelectedPlanet);

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

        public void SnapViewPlanet(object sender)
        {
            ShowShipNames = false;
            if (SelectedPlanet == null)
                return;
            CamDestination = new Vector3(SelectedPlanet.Center.X, SelectedPlanet.Center.Y + 400f, 2500f);
            if (!SelectedPlanet.ParentSystem.IsExploredBy(player))
            {
                PlayNegativeSound();
            }
            else
            {
                bool flag = player.data.MoleList.Any(mole => mole.PlanetGuid == SelectedPlanet.guid);

                if (SelectedPlanet.Owner == player || flag || Debug && SelectedPlanet.Owner != null)
                    workersPanel = new ColonyScreen(this, SelectedPlanet, EmpireUI);
                else if (SelectedPlanet.Owner != null)
                {
                    workersPanel = new UnownedPlanetScreen(this, SelectedPlanet);
                    CamDestination = new Vector3(SelectedPlanet.Center.X, SelectedPlanet.Center.Y + 400f,
                        95000f);
                }
                else
                {
                    workersPanel = new UnexploredPlanetScreen(this, SelectedPlanet);
                    CamDestination = new Vector3(SelectedPlanet.Center.X, SelectedPlanet.Center.Y + 400f,
                        95000f);
                }
                SelectedPlanet.SetExploredBy(player);
                LookingAtPlanet = true;
                transitionStartPosition = CamPos;
                AdjustCamTimer = 2f;
                transitionElapsedTime = 0.0f;
                transDuration = 5f;
                if (ViewingShip) returnToShip = true;
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

        private void ViewSystem(SolarSystem system)
        {
            CamDestination = new Vector3(system.Position, 147000f);
            ViewingShip = false;
            AdjustCamTimer = 1f;
            transDuration = 3f;
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
                    float amount = (float) Math.Pow(transitionElapsedTime / (double) transDuration,
                        0.699999988079071);
                    camTransitionPosition.X =
                        MathHelper.SmoothStep(CamPos.X, CamDestination.X, amount);
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
                    float amount = (float) Math.Pow(transitionElapsedTime / (double) transDuration,
                        0.699999988079071);
                    camTransitionPosition.X =
                        MathHelper.SmoothStep(CamPos.X, CamDestination.X, amount);
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

            if (CamPos.X > UniverseSize)
                CamPos.X = UniverseSize;
            if (CamPos.X < -UniverseSize) //So the camera can pan out into the new negative map coordinates -Gretman
                CamPos.X = -UniverseSize;
            if (CamPos.Y > (double) UniverseSize)
                CamPos.Y = UniverseSize;
            if ((double) CamPos.Y < -UniverseSize)
                CamPos.Y = -UniverseSize;
            if (CamHeight > MaxCamHeight * (double) GameScale)
                CamHeight = MaxCamHeight * GameScale;
            else if (CamHeight < minCamHeight)
                CamHeight = minCamHeight;
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
            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
            AdjustCamTimer = 1f;
            transitionElapsedTime = 0.0f;
            CamDestination.Z = 4500f;
            snappingToShip = true;
            ViewingShip = true;
        }

        public void InputZoomOut()
        {
            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
            AdjustCamTimer = 1f;
            transitionElapsedTime = 0.0f;
            CamDestination.X = CamPos.X;
            CamDestination.Y = CamPos.Y;
            CamDestination.Z = 4200000f * GameScaleStatic;
        }

        private void DefaultZoomPoints()
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

        private void ChaseCame()
        {
            if (!ViewingShip)
            {
                ViewToShip();
            }
            ViewingShip = !ViewingShip;
        }
    }
}