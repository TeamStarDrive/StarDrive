using SDGraphics;
using Ship_Game.Audio;
using Ship_Game.Ships;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        void CreatePieMenu()
        {
            pieMenu = new PieMenu();
            planetMenu = new PieMenuNode();
            shipMenu = new PieMenuNode();
        }

        void LoadPieMenuNodesForPlanet(bool owned, bool habitable)
        {
            planetMenu.Children?.Clear();
            var viewPlanet = ResourceManager.Texture("UI/viewPlanetIcon");
            planetMenu.Add(new(GameText.ViewPlanet, viewPlanet, SnapViewColony));

            if (habitable)
            {
                var colonize = ResourceManager.Texture("UI/ColonizeIcon");
                if (!owned)
                {
                    planetMenu.Add(new(GameText.MarkForColonization, colonize, MarkForColonization));
                }

                planetMenu.Add(new(GameText.TacticalView, colonize, OpenCombatMenu));
            }

            pieMenu.Show(planetMenu);
        }

        void LoadPieMenuShipNodes(bool isPlayer)
        {
            if (SelectedShip is {CanBeScrapped: false})
                return;

            shipMenu.Children?.Clear();
            if (isPlayer)
            {
                var orders = new PieMenuNode(GameText.Orders2, ResourceManager.Texture("UI/OrdersIcon"), null);
                orders.Add(new(GameText.GoExploring, ResourceManager.Texture("UI/marketIcon"), DoExplore));
                orders.Add(new("Empire Defense", ResourceManager.Texture("UI/PatrolIcon"), DoDefense));
                shipMenu.Add(orders);

                var followIcon = ResourceManager.Texture("UI/FollowIcon");
                var holdPosition = ResourceManager.Texture("UI/HoldPositionIcon");
                var other = new PieMenuNode(GameText.Other, followIcon, null);
                if (SelectedShip is {IsPlatformOrStation: false, CanBeScrapped: true})
                {
                    other.Add(new(GameText.RefitTo, followIcon, RefitTo));
                }
                if (SelectedShip is {IsPlatformOrStation: true})
                {
                    other.Add(new("Scuttle", holdPosition, OrderScuttle));
                }
                else
                {
                    if (SelectedShip != null && SelectedShip.ShipData.Role < RoleName.construction)
                        other.Add(new(GameText.OrderScrap, holdPosition, OrderScrap));
                }
                shipMenu.Add(other);
            }
            else
            {
                shipMenu.Add(new(GameText.ContactLeader, ResourceManager.Texture("UI/viewPlanetIcon"), ContactLeader));
            }

            pieMenu.Show(shipMenu);
        }

        Vector2 GetPieMenuPosition()
        {
            if (SelectedShip != null)
                return ProjectToScreenPosition(SelectedShip.Position).ToVec2f();
            if (SelectedPlanet != null)
                return ProjectToScreenPosition(SelectedPlanet.Position3D).ToVec2f();
            return ScreenManager.ScreenCenter;
        }

        public void OpenCombatMenu()
        {
            workersPanel = new CombatScreen(this, SelectedPlanet);
            LookingAtPlanet = true;
            transitionStartPosition = CamPos;
            CamDestination = new Vector3d(SelectedPlanet.Position.X, SelectedPlanet.Position.Y + 400f, 2500.0);

            AdjustCamTimer = 2f;
            transitionElapsedTime = 0.0f;
            transDuration = 5f;

            if (ViewingShip)
                returnToShip = true;

            ViewingShip = false;
            snappingToShip = false;
        }

        public void RefitTo()
        {
            if (SelectedShip != null)
                ScreenManager.AddScreen(new RefitToWindow(this, SelectedShip));
        }

        void ToggleUIComponent(string audioCue, ref bool toggle)
        {
            GameAudio.PlaySfxAsync(audioCue);
            toggle = !toggle;
        }

        public void InputOpenDeepSpaceBuildWindow()
        {
            if (!DeepSpaceBuildWindow.Visible)
            {
                GameAudio.AcceptClick();
                DeepSpaceBuildWindow.InitializeAndShow();
            }
            else
            {
                DeepSpaceBuildWindow.Hide();
            }
        }
    }
}
