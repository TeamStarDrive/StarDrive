using Microsoft.Xna.Framework;

namespace Ship_Game {
    public sealed partial class UniverseScreen
    {
        private void LoadMenu()
        {
            var viewPlanetIcon = ResourceManager.TextureDict["UI/viewPlanetIcon"];
            pieMenu = new PieMenu();
            planetMenu = new PieMenuNode();
            shipMenu = new PieMenuNode();
            planetMenu.Add(new PieMenuNode("View Planet", viewPlanetIcon, ViewPlanet));
            planetMenu.Add(new PieMenuNode("Mark for Colonization", viewPlanetIcon, MarkForColonization));
            shipMenu.Add(new PieMenuNode("Commandeer Ship", viewPlanetIcon, ViewShip));
        }

        private void LoadMenuNodes(bool Owned, bool Habitable)
        {
            this.planetMenu.Children.Clear();
            this.planetMenu.Add(new PieMenuNode(Localizer.Token(1421), ResourceManager.TextureDict["UI/viewPlanetIcon"],
                new SimpleDelegate(this.ViewPlanet)));
            if (!Owned && Habitable)
                this.planetMenu.Add(new PieMenuNode(Localizer.Token(1422),
                    ResourceManager.TextureDict["UI/ColonizeIcon"], new SimpleDelegate(this.MarkForColonization)));
            if (!Habitable)
                return;
            this.planetMenu.Add(new PieMenuNode(Localizer.Token(1423), ResourceManager.TextureDict["UI/ColonizeIcon"],
                new SimpleDelegate(this.OpenCombatMenu)));
        }

        public void OpenCombatMenu(object sender)
        {
            this.workersPanel = new CombatScreen(this.ScreenManager, this.SelectedPlanet);
            this.LookingAtPlanet = true;
            this.transitionStartPosition = this.camPos;
            this.transitionDestination = new Vector3(this.SelectedPlanet.Position.X,
                this.SelectedPlanet.Position.Y + 400f, 2500f);
            this.AdjustCamTimer = 2f;
            this.transitionElapsedTime = 0.0f;
            this.transDuration = 5f;
            if (this.ViewingShip)
                this.returnToShip = true;
            this.ViewingShip = false;
            this.snappingToShip = false;
        }

        public void RefitTo(object sender)
        {
            if (SelectedShip != null)
                ScreenManager.AddScreen(new RefitToWindow(this, SelectedShip));
        }

        private void LoadShipMenuNodes(int which)
        {
            shipMenu.Children.Clear();
            if (which == 1)
            {
                if (SelectedShip != null && SelectedShip == playerShip)
                    shipMenu.Add(new PieMenuNode("Relinquish Control", ResourceManager.TextureDict["UI/viewPlanetIcon"],
                        ViewShip));
                else
                    shipMenu.Add(new PieMenuNode(Localizer.Token(1412),
                        ResourceManager.TextureDict["UI/viewPlanetIcon"], ViewShip));
                PieMenuNode newChild1 = new PieMenuNode(Localizer.Token(1413),
                    ResourceManager.TextureDict["UI/OrdersIcon"], null);
                shipMenu.Add(newChild1);
                if (SelectedShip != null && SelectedShip.CargoSpaceMax > 0.0)
                {
                    newChild1.Add(new PieMenuNode(Localizer.Token(1414), ResourceManager.TextureDict["UI/PatrolIcon"],
                        DoTransport));
                    newChild1.Add(new PieMenuNode(Localizer.Token(1415), ResourceManager.TextureDict["UI/marketIcon"],
                        DoTransportGoods));
                }
                newChild1.Add(new PieMenuNode(Localizer.Token(1416), ResourceManager.TextureDict["UI/marketIcon"],
                    DoExplore));
                newChild1.Add(
                    new PieMenuNode("Empire Defense", ResourceManager.TextureDict["UI/PatrolIcon"], DoDefense));
                PieMenuNode newChild6 = new PieMenuNode(Localizer.Token(1417),
                    ResourceManager.TextureDict["UI/FollowIcon"], null);
                shipMenu.Add(newChild6);
                if (SelectedShip != null && SelectedShip.shipData.Role != ShipData.RoleName.station &&
                    SelectedShip.shipData.Role != ShipData.RoleName.platform)
                {
                    newChild6.Add(new PieMenuNode(Localizer.Token(1418), ResourceManager.TextureDict["UI/FollowIcon"],
                        RefitTo));
                }
                if (SelectedShip != null && (SelectedShip.shipData.Role == ShipData.RoleName.station ||
                                             SelectedShip.shipData.Role == ShipData.RoleName.platform))
                {
                    newChild6.Add(new PieMenuNode("Scuttle", ResourceManager.TextureDict["UI/HoldPositionIcon"],
                        OrderScuttle));
                }
                else
                {
                    if (SelectedShip == null || SelectedShip.shipData.Role > ShipData.RoleName.construction)
                        return;
                    newChild6.Add(new PieMenuNode(Localizer.Token(1419),
                        ResourceManager.TextureDict["UI/HoldPositionIcon"], OrderScrap));
                }
            }
            else
                shipMenu.Add(new PieMenuNode(Localizer.Token(1420), ResourceManager.TextureDict["UI/viewPlanetIcon"],
                    ContactLeader));
        }

        private void ToggleUIComponent(string audioCue, ref bool toggle)
        {
            GameAudio.PlaySfxAsync(audioCue);
            toggle = !toggle;
        }

        private void InputOpenDeepSpaceBuildWindow(string audioCue = "echo_affirm")
        {
            if (showingDSBW)
            {
                showingDSBW = !showingDSBW;
                return;
            }
            dsbw = new DeepSpaceBuildingWindow(ScreenManager, this);
            GameAudio.PlaySfxAsync(audioCue);
            showingDSBW = true;
        }
    }
}