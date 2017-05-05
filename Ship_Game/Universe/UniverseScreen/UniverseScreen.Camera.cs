using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public sealed partial class UniverseScreen
    {
        private Vector2 CalculateCameraPositionOnMouseZoom(Vector2 MousePosition, float DesiredCamHeight)
        {
            Vector2 vector2_1 = new Vector2(
                MousePosition.X - (float) (this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth /
                                           2),
                MousePosition.Y -
                (float) (this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2));
            Vector3 position1 = this.Viewport.Unproject(
                new Vector3(MousePosition.X, MousePosition.Y, 0.0f), this.projection, this.view, Matrix.Identity);
            Vector3 direction1 =
                this.Viewport.Unproject(new Vector3(MousePosition.X, MousePosition.Y, 1f),
                    this.projection, this.view, Matrix.Identity) - position1;
            direction1.Normalize();
            Ray ray = new Ray(position1, direction1);
            float num1 = -ray.Position.Z / ray.Direction.Z;
            Vector3 source = new Vector3(ray.Position.X + num1 * ray.Direction.X,
                ray.Position.Y + num1 * ray.Direction.Y, 0.0f);
            Matrix view = Matrix.CreateTranslation(0.0f, 0.0f, 0.0f) * Matrix.CreateRotationY(180f.ToRadians()) *
                          Matrix.CreateRotationX(0.0f.ToRadians()) *
                          Matrix.CreateLookAt(new Vector3(this.camPos.X, this.camPos.Y, DesiredCamHeight),
                              new Vector3(this.camPos.X, this.camPos.Y, 0.0f), new Vector3(0.0f, -1f, 0.0f));
            Vector3 vector3 =
                this.Viewport.Project(source, this.projection, view, Matrix.Identity);
            Vector2 vector2_2 = new Vector2((float) (int) vector3.X - vector2_1.X,
                (float) (int) vector3.Y - vector2_1.Y);
            Vector3 position2 = this.Viewport.Unproject(
                new Vector3(vector2_2.X, vector2_2.Y, 0.0f), this.projection, view, Matrix.Identity);
            Vector3 direction2 =
                this.Viewport.Unproject(new Vector3(vector2_2.X, vector2_2.Y, 1f),
                    this.projection, view, Matrix.Identity) - position2;
            direction2.Normalize();
            ray = new Ray(position2, direction2);
            float num2 = -ray.Position.Z / ray.Direction.Z;
            return new Vector2(ray.Position.X + num2 * ray.Direction.X, ray.Position.Y + num2 * ray.Direction.Y);
        }

        public void FollowPlayer(object sender)
        {
            SelectedShip.AI.State = AIState.Escort;
            SelectedShip.AI.EscortTarget = this.playerShip;
        }

        public void ViewShip(object sender)
        {
            if (this.SelectedShip == null)
                return;
            if (this.playerShip != null && this.SelectedShip == this.playerShip)
            {
                this.playerShip.PlayerShip = false;
                this.playerShip.AI.State = AIState.AwaitingOrders;
                this.playerShip = (Ship) null;
            }
            else
            {
                if (this.SelectedShip.loyalty != this.player || this.SelectedShip.isConstructor)
                    return;
                this.ShipToView = this.SelectedShip;
                this.snappingToShip = true;
                this.HeightOnSnap = this.camHeight;
                this.transitionDestination.Z = 3500f;
                if (this.playerShip != null)
                {
                    this.playerShip.PlayerShip = false;
                    this.playerShip.AI.State = AIState.AwaitingOrders;
                    this.playerShip = this.SelectedShip;
                    this.playerShip.PlayerShip = true;
                    this.playerShip.AI.State = AIState.ManualControl;
                }
                else
                {
                    this.playerShip = this.SelectedShip;
                    this.playerShip.PlayerShip = true;
                    this.playerShip.AI.State = AIState.ManualControl;
                }
                this.AdjustCamTimer = 1.5f;
                this.transitionElapsedTime = 0.0f;
                this.transitionDestination.Z = 4500f;
                this.snappingToShip = true;
                this.ViewingShip = true;
                if (!this.playerShip.isSpooling)
                    return;
                this.playerShip.HyperspaceReturn();
            }
        }

        public void ViewToShip(object sender)
        {
            if (this.SelectedShip == null)
                return;
            this.ShipToView = this.SelectedShip;
            this.ShipInfoUIElement.SetShip(this.SelectedShip); //fbedard: was not updating correctly from shiplist
            this.SelectedFleet = (Fleet) null;
            this.SelectedShipList.Clear();
            this.SelectedItem = (UniverseScreen.ClickableItemUnderConstruction) null;
            this.SelectedSystem = (SolarSystem) null;
            this.SelectedPlanet = (Planet) null;
            this.snappingToShip = true;
            this.HeightOnSnap = this.camHeight;
            this.transitionDestination.Z = 3500f;
            this.AdjustCamTimer = 1.0f;
            this.transitionElapsedTime = 0.0f;
            this.transitionDestination.Z = 4500f;
            this.snappingToShip = true;
            this.ViewingShip = true;
        }

        public void ViewPlanet(object sender)
        {
            ShowShipNames = false;
            if (SelectedPlanet == null)
                return;
            if (!SelectedPlanet.system.ExploredDict[player])
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
                workersPanel = SelectedPlanet.Owner == player || flag || Debug && SelectedPlanet.Owner != null
                    ? new ColonyScreen(SelectedPlanet, ScreenManager, EmpireUI)
                    : (SelectedPlanet.Owner == null
                        ? new UnexploredPlanetScreen(SelectedPlanet, ScreenManager)
                        : (PlanetScreen) new UnownedPlanetScreen(SelectedPlanet, ScreenManager));
                LookingAtPlanet = true;
                transitionStartPosition = camPos;
                transitionDestination = new Vector3(SelectedPlanet.Position.X, SelectedPlanet.Position.Y + 400f, 2500f);
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
                SelectedShipList.Clear();
                SelectedItem = null;
            }
        }

        public void SnapViewSystem(SolarSystem system, UnivScreenState camHeight)
        {
            float x = this.GetZfromScreenState(camHeight);
            this.transitionDestination = new Vector3(system.Position.X, system.Position.Y + 400f, x);
            this.transitionStartPosition = this.camPos;
            this.AdjustCamTimer = 2f;
            this.transitionElapsedTime = 0.0f;
            this.transDuration = 5f;
            this.ViewingShip = false;
            this.snappingToShip = false;
            if (this.ViewingShip)
                this.returnToShip = true;
            this.ViewingShip = false;
            this.snappingToShip = false;
            this.SelectedFleet = null;
            if (this.SelectedShip != null && this.previousSelection != this.SelectedShip) //fbedard
                this.previousSelection = this.SelectedShip;
            this.SelectedShip = null;
            this.SelectedShipList.Clear();
            this.SelectedItem = null;
        }

        public void SnapViewPlanet(object sender)
        {
            ShowShipNames = false;
            if (SelectedPlanet == null)
                return;
            transitionDestination = new Vector3(SelectedPlanet.Position.X, SelectedPlanet.Position.Y + 400f, 2500f);
            if (!SelectedPlanet.system.ExploredDict[player])
            {
                PlayNegativeSound();
            }
            else
            {
                bool flag = player.data.MoleList.Any(mole => mole.PlanetGuid == SelectedPlanet.guid);

                if (SelectedPlanet.Owner == player || flag || Debug && SelectedPlanet.Owner != null)
                    workersPanel = new ColonyScreen(SelectedPlanet, ScreenManager, EmpireUI);
                else if (SelectedPlanet.Owner != null)
                {
                    workersPanel = new UnownedPlanetScreen(SelectedPlanet, ScreenManager);
                    transitionDestination = new Vector3(SelectedPlanet.Position.X, SelectedPlanet.Position.Y + 400f,
                        95000f);
                }
                else
                {
                    workersPanel = new UnexploredPlanetScreen(SelectedPlanet, ScreenManager);
                    transitionDestination = new Vector3(SelectedPlanet.Position.X, SelectedPlanet.Position.Y + 400f,
                        95000f);
                }
                SelectedPlanet.ExploredDict[player] = true;
                LookingAtPlanet = true;
                transitionStartPosition = camPos;
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
            transitionDestination = new Vector3(system.Position, 147000f);
            ViewingShip = false;
            AdjustCamTimer = 1f;
            transDuration = 3f;
            transitionElapsedTime = 0.0f;
        }

        private void AdjustCamera(float elapsedTime)
        {
            if (this.ShipToView == null)
                this.ViewingShip = false;


#if DEBUG
            float minCamHeight = 400.0f;
#else
                float minCamHeight = Debug ? 1337.0f : 400.0f;
            #endif

            this.AdjustCamTimer -= elapsedTime;
            if (this.ViewingShip && !this.snappingToShip)
            {
                this.camPos.X = this.ShipToView.Center.X;
                this.camPos.Y = this.ShipToView.Center.Y;
                this.camHeight =
                    (float) (int) MathHelper.SmoothStep(this.camHeight, this.transitionDestination.Z, 0.2f);
                if (camHeight < minCamHeight)
                    camHeight = minCamHeight;
            }
            if (this.AdjustCamTimer > 0.0)
            {
                if (this.ShipToView == null)
                    this.snappingToShip = false;
                if (this.snappingToShip)
                {
                    this.transitionDestination.X = this.ShipToView.Center.X;
                    this.transitionDestination.Y = this.ShipToView.Center.Y;
                    this.transitionElapsedTime += elapsedTime;
                    float amount = (float) Math.Pow((double) this.transitionElapsedTime / (double) this.transDuration,
                        0.699999988079071);
                    this.camTransitionPosition.X =
                        MathHelper.SmoothStep(this.camPos.X, this.transitionDestination.X, amount);
                    float num1 = MathHelper.SmoothStep(this.camPos.Y, this.transitionDestination.Y, amount);
                    float num2 = MathHelper.SmoothStep(this.camHeight, this.transitionDestination.Z, amount);
                    this.camTransitionPosition.Y = num1;
                    this.camHeight = (float) (int) num2;
                    this.camPos = this.camTransitionPosition;
                    if ((double) this.AdjustCamTimer - (double) elapsedTime <= 0.0)
                    {
                        this.ViewingShip = true;
                        this.transitionElapsedTime = 0.0f;
                        this.AdjustCamTimer = -1f;
                        this.snappingToShip = false;
                    }
                }
                else
                {
                    this.transitionElapsedTime += elapsedTime;
                    float amount = (float) Math.Pow((double) this.transitionElapsedTime / (double) this.transDuration,
                        0.699999988079071);
                    this.camTransitionPosition.X =
                        MathHelper.SmoothStep(this.camPos.X, this.transitionDestination.X, amount);
                    float num1 = MathHelper.SmoothStep(this.camPos.Y, this.transitionDestination.Y, amount);
                    float num2 = MathHelper.SmoothStep(this.camHeight, this.transitionDestination.Z, amount);
                    this.camTransitionPosition.Y = num1;
                    this.camHeight = num2;
                    this.camPos = this.camTransitionPosition;
                    if ((double) this.transitionElapsedTime > (double) this.transDuration ||
                        (double) Vector2.Distance(new Vector2(this.camPos.X, this.camPos.Y),
                            new Vector2(this.transitionDestination.X, this.transitionDestination.Y)) < 50.0 &&
                        (double) Math.Abs(this.camHeight - this.transitionDestination.Z) < 50.0)
                    {
                        this.transitionElapsedTime = 0.0f;
                        this.AdjustCamTimer = -1f;
                    }
                }
                if (camHeight < minCamHeight)
                    camHeight = minCamHeight;
            }
            else if (this.LookingAtPlanet && this.SelectedPlanet != null)
            {
                this.camTransitionPosition.X =
                    MathHelper.SmoothStep(this.camPos.X, this.SelectedPlanet.Position.X, 0.2f);
                this.camTransitionPosition.Y =
                    MathHelper.SmoothStep(this.camPos.Y, this.SelectedPlanet.Position.Y + 400f, 0.2f);
                this.camPos = this.camTransitionPosition;
            }
            else if (!this.ViewingShip)
            {
                this.camTransitionPosition.X = MathHelper.SmoothStep(this.camPos.X, this.transitionDestination.X, 0.2f);
                float num1 = MathHelper.SmoothStep(this.camPos.Y, this.transitionDestination.Y, 0.2f);
                float num2 = MathHelper.SmoothStep(this.camHeight, this.transitionDestination.Z, 0.2f);
                this.camTransitionPosition.Y = num1;
                this.camHeight = num2;
                if (camHeight < minCamHeight)
                    camHeight = minCamHeight;
                this.camPos = this.camTransitionPosition;
            }

            if (this.camPos.X > this.UniverseRadius)
                this.camPos.X = this.UniverseRadius;
            if (this.camPos.X < -this.UniverseRadius) //So the camera can pan out into the new negative map coordinates -Gretman
                this.camPos.X = -this.UniverseRadius;
            if (this.camPos.Y > (double) this.UniverseRadius)
                this.camPos.Y = this.UniverseRadius;
            if ((double) this.camPos.Y < -this.UniverseRadius)
                this.camPos.Y = -this.UniverseRadius;
            if ((double) this.camHeight > (double) this.MaxCamHeight * (double) this.GameScale)
                this.camHeight = this.MaxCamHeight * this.GameScale;
            else if (camHeight < minCamHeight)
                camHeight = minCamHeight;
            foreach (UnivScreenState screenHeight in Enum.GetValues(typeof(UnivScreenState)))
            {
                if (camHeight <= GetZfromScreenState(screenHeight))
                {
                    viewState = screenHeight;
                    break;
                }
            }
        }

        private void InputZoomToShip()
        {
            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
            AdjustCamTimer = 1f;
            transitionElapsedTime = 0.0f;
            transitionDestination.Z = 4500f;
            snappingToShip = true;
            ViewingShip = true;
        }

        private void InputZoomOut()
        {
            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
            AdjustCamTimer = 1f;
            transitionElapsedTime = 0.0f;
            transitionDestination.X = camPos.X;
            transitionDestination.Y = camPos.Y;
            transitionDestination.Z = 4200000f * UniverseScreen.GameScaleStatic;
        }

        private void DefaultZoomPoints()
        {
            snappingToShip = false;
            ViewingShip = false;
            if (camHeight < GetZfromScreenState(UnivScreenState.GalaxyView) &&
                camHeight > GetZfromScreenState(UnivScreenState.SectorView))
            {
                AdjustCamTimer = 1f;
                transitionElapsedTime = 0.0f;
                transitionDestination = new Vector3(camPos.X, camPos.Y, 1175000f);
            }
            else if (camHeight > GetZfromScreenState(UnivScreenState.ShipView))
            {
                AdjustCamTimer = 1f;
                transitionElapsedTime = 0.0f;
                transitionDestination = new Vector3(camPos.X, camPos.Y, 147000f);
            }
            else if (viewState < UniverseScreen.UnivScreenState.SystemView)
                transitionDestination = new Vector3(camPos.X, camPos.Y,
                    GetZfromScreenState(UnivScreenState.SystemView));
        }

        private void ChaseCame()
        {
            if (!ViewingShip)
            {
                ViewToShip(null);
            }
            ViewingShip = !ViewingShip;
        }
    }
}