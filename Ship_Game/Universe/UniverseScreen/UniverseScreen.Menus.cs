using Microsoft.Xna.Framework;
using Ship_Game.Audio;
using Ship_Game.Ships;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        void LoadMenu()
        {
            var viewPlanetIcon = ResourceManager.Texture("UI/viewPlanetIcon");
            pieMenu = new PieMenu();
            planetMenu = new PieMenuNode();
            shipMenu = new PieMenuNode();
            planetMenu.Add(new PieMenuNode("View Planet", viewPlanetIcon, SnapViewColony));
            planetMenu.Add(new PieMenuNode("Mark for Colonization", viewPlanetIcon, MarkForColonization));
        }

        void LoadMenuNodes(bool Owned, bool Habitable)
        {
            planetMenu.Children.Clear();
            planetMenu.Add(new PieMenuNode(Localizer.Token(GameText.ViewPlanet), ResourceManager.Texture("UI/viewPlanetIcon"),
                SnapViewColony));
            if (!Owned && Habitable)
                planetMenu.Add(new PieMenuNode(Localizer.Token(GameText.MarkForColonization),
                    ResourceManager.Texture("UI/ColonizeIcon"), MarkForColonization));
            if (!Habitable)
                return;
            planetMenu.Add(new PieMenuNode(Localizer.Token(GameText.TacticalView), ResourceManager.Texture("UI/ColonizeIcon"),
                OpenCombatMenu));
        }

        public void OpenCombatMenu()
        {
            workersPanel = new CombatScreen(this, SelectedPlanet);
            LookingAtPlanet = true;
            transitionStartPosition = CamPos;
            CamDestination = new Vector3d(SelectedPlanet.Center.X, SelectedPlanet.Center.Y + 400f, 2500.0);

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

        void LoadShipMenuNodes(int which)
        {
            shipMenu.Children?.Clear();
            if (SelectedShip != null && !SelectedShip.CanBeScrapped)
                return;

            if (which == 1)
            {
                var newChild1 = new PieMenuNode(Localizer.Token(GameText.Orders2), ResourceManager.Texture("UI/OrdersIcon"), null);
                shipMenu.Add(newChild1);
                newChild1.Add(new PieMenuNode(Localizer.Token(GameText.GoExploring), ResourceManager.Texture("UI/marketIcon"), DoExplore));
                newChild1.Add(new PieMenuNode("Empire Defense", ResourceManager.Texture("UI/PatrolIcon"), DoDefense));
                var newChild6 = new PieMenuNode(Localizer.Token(GameText.Other), ResourceManager.Texture("UI/FollowIcon"), null);
                shipMenu.Add(newChild6);
                if (SelectedShip != null && !SelectedShip.IsPlatformOrStation && SelectedShip.CanBeScrapped)
                {
                    newChild6.Add(new PieMenuNode(Localizer.Token(GameText.RefitTo), ResourceManager.Texture("UI/FollowIcon"), RefitTo));
                }
                if (SelectedShip != null && SelectedShip.IsPlatformOrStation)
                {
                    newChild6.Add(new PieMenuNode("Scuttle", ResourceManager.Texture("UI/HoldPositionIcon"), OrderScuttle));
                }
                else
                {
                    if (SelectedShip == null || SelectedShip.shipData.Role > RoleName.construction)
                        return;
                    newChild6.Add(new PieMenuNode(Localizer.Token(GameText.OrderScrap),
                        ResourceManager.Texture("UI/HoldPositionIcon"), OrderScrap));
                }
            }
            else
            {
                shipMenu.Add(new PieMenuNode(Localizer.Token(GameText.ContactLeader), ResourceManager.Texture("UI/viewPlanetIcon"), ContactLeader));
            }
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
