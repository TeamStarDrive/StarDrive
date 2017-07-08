using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed class MiniMap: UIElementContainer
    {
        private readonly Rectangle Housing;

        private Rectangle ActualMap;

        private readonly ToggleButton ZoomOut;

        private readonly ToggleButton ZoomToShip;

        private readonly ToggleButton PlanetScreen;

        private readonly ToggleButton ShipScreen;

        private readonly ToggleButton AIScreen;

        private readonly ToggleButton DeepSpaceBuild;

        private readonly ToggleButton Fleets;
        

        private int ButtonOffset;

        private const string CNormal       = "Minimap/button_C_normal";
        private const string BNormal       = "Minimap/button_B_normal";
        private const string Normal        = "Minimap/button_normal";
        private const string Hover         = "Minimap/button_hover";
        private const string CHover        = "Minimap/button_hover";
        private const string Active        = "Minimap/button_active";
        private const string BHover        = "Minimap/button_B_hover";

        private readonly Texture2D MiniMapHousing;
        private readonly Texture2D Node;
        private readonly Texture2D Node1;

        public MiniMap(Rectangle housing) : base(null, housing)
        {
            Housing        = housing;
            Rectangle size = housing;
            MiniMapHousing = ResourceManager.Texture("Minimap/radar_over");
            Node           = ResourceManager.Texture("UI/node");
            Node1          = ResourceManager.Texture("UI/node1");

            BeginVLayout(Housing.X + 14, Housing.Y + 70, 25);
            //button spacing isnt quite right. 
            ZoomToShip     = ToggleButtonLayout(22, 25, CNormal, CNormal, CHover, CNormal, "Minimap/icons_zoomctrl");
            ZoomOut        = ToggleButtonLayout(22, 25, CNormal, CNormal, CHover, CNormal, "Minimap/icons_zoomout");
            PlanetScreen   = ToggleButtonLayout(22, 25, BNormal, BNormal, BHover, BNormal, "UI/icon_planetslist");
            ShipScreen     = ToggleButtonLayout(22, 25, Active, Normal, Hover, Normal, "UI/icon_ftloverlay");
            Fleets         = ToggleButtonLayout(22, 25, Active, Normal, Hover, Normal, "UI/icon_rangeoverlay");
            DeepSpaceBuild = ToggleButtonLayout(22, 25, Active, Normal, Hover, Normal, "UI/icon_dsbw");
            AIScreen       = ToggleButtonLayout(26, 25, Active, "Minimap/button_down_inactive", "Minimap/button_down_hover"
                , "Minimap/button_down_inactive", "AI");

            EndLayout();
        }        
        
        public void Draw(ScreenManager screenManager, UniverseScreen screen)
        {
            screenManager.SpriteBatch.Draw(MiniMapHousing, Housing, Color.White);
            float scale     = ActualMap.Width / (screen.UniverseSize * 2);        //Updated to play nice with the new negative map values
            var minimapZero = new Vector2((float)ActualMap.X + 100, (float)ActualMap.Y + 100);
            var uiNode      = Node;
            var uiNode1     = Node1;

            foreach (Empire e in EmpireManager.Empires)
            {
                if (e != EmpireManager.Player && !EmpireManager.Player.GetRelations(e).Known)
                    continue;

                using (e.BorderNodes.AcquireReadLock())
                    foreach (Empire.InfluenceNode node in e.BorderNodes)
                    {
                        float radius = node.Radius * scale;
                        var nodepos = new Vector2(minimapZero.X + node.Position.X * scale,
                            minimapZero.Y + node.Position.Y * scale);
                        var ec = new Color(e.EmpireColor.R, e.EmpireColor.G, e.EmpireColor.B, 30);
                        float rscale = radius * 0.005f;

                        if (rscale < 0.006f) rscale = 0.006f;
                        screenManager.SpriteBatch.Draw(uiNode1, nodepos, null, ec, 0f, uiNode.Center(), rscale,
                            SpriteEffects.None, 1f);
                    }
            }
            foreach (SolarSystem system in UniverseScreen.SolarSystemList)
            {
                var star = new Rectangle((int)(minimapZero.X + system.Position.X * scale), (int)(minimapZero.Y + system.Position.Y * scale), 2, 2);
                if (system.OwnerList.Count <= 0 || !system.ExploredDict[EmpireManager.Player])
                    screenManager.SpriteBatch.FillRectangle(star, Color.Gray);
                else
                    screenManager.SpriteBatch.FillRectangle(star, system.OwnerList.ToList()[0].EmpireColor);
            }
            Vector2 upperLeftView = screen.UnprojectToWorldPosition(new Vector2(0f, 0f));
            upperLeftView         = new Vector2(HelperFunctions.RoundTo(upperLeftView.X, 20000), HelperFunctions.RoundTo(upperLeftView.Y, 20000));
            Vector2 right         = screen.UnprojectToWorldPosition(new Vector2(screenManager.GraphicsDevice.PresentationParameters.BackBufferWidth, 0f));
            right                 = new Vector2(HelperFunctions.RoundTo(right.X, 20000), 0f);
            float xdist           = (right.X - upperLeftView.X) * scale;
            xdist                 = HelperFunctions.RoundTo(xdist, 1);
            float ydist           = xdist * screenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / screenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
            Rectangle lookingAt   = new Rectangle((int)minimapZero.X + (int)(upperLeftView.X * scale), (int)minimapZero.Y + (int)(upperLeftView.Y * scale), (int)xdist, (int)ydist);
            if (lookingAt.Width < 2)
            {
                lookingAt.Width = 2;
                lookingAt.Height = 2;
            }
            if (lookingAt.X < ActualMap.X)
            {
                lookingAt.X = ActualMap.X;
            }
            if (lookingAt.Y < ActualMap.Y)
            {
                lookingAt.Y = ActualMap.Y;
            }
            float lookRightEdge  = lookingAt.X + lookingAt.Width;
            float lookBottomEdge = lookingAt.Y + lookingAt.Height;
            lookingAt.X          = lookRightEdge > ActualMap.Width + ActualMap.X ? ActualMap.X + ActualMap.Width - lookingAt.Width : lookingAt.X;
            lookingAt.Y          = lookBottomEdge > ActualMap.Height + ActualMap.Y ? ActualMap.Height + ActualMap.Y  - lookingAt.Height : lookingAt.Y;

            screenManager.SpriteBatch.FillRectangle(lookingAt, new Color(255, 255, 255, 30));
            screenManager.SpriteBatch.DrawRectangle(lookingAt, Color.White);
            var topMiddleView   = new Vector2(lookingAt.X + lookingAt.Width / 2, lookingAt.Y);
            var botMiddleView   = new Vector2(topMiddleView.X - 1f, lookingAt.Y + lookingAt.Height);
            var leftMiddleView  = new Vector2(lookingAt.X, lookingAt.Y + lookingAt.Height / 2);
            var rightMiddleView = new Vector2(lookingAt.X + lookingAt.Width, leftMiddleView.Y + 1f);
            screenManager.SpriteBatch.DrawLine(new Vector2(topMiddleView.X, minimapZero.Y - 100), topMiddleView, Color.White);
            screenManager.SpriteBatch.DrawLine(new Vector2(botMiddleView.X, ActualMap.Y + ActualMap.Height), botMiddleView, Color.White);
            screenManager.SpriteBatch.DrawLine(new Vector2(ActualMap.X, leftMiddleView.Y), leftMiddleView, Color.White);
            screenManager.SpriteBatch.DrawLine(new Vector2(ActualMap.X + ActualMap.Width, rightMiddleView.Y), rightMiddleView, Color.White);

            ShipScreen.Active     = screen.showingFTLOverlay;
            DeepSpaceBuild.Active = screen.showingDSBW;
            AIScreen.Active       = screen.aw.IsOpen;
            Fleets.Active         = screen.showingRangeOverlay;
            base.Draw(screenManager.SpriteBatch);
        }

        public bool HandleInput(InputState input, UniverseScreen screen)
        {
            if (ZoomToShip.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(57, "Page Up");

            if (ZoomToShip.HandleInput(input))
            {
                GameAudio.MiniMapButton();
                screen.AdjustCamTimer = 1f;
                screen.transitionElapsedTime = 0f;
                screen.CamDestination.Z = 4500f;
                screen.snappingToShip = true;
                screen.ViewingShip = true;
                return true;
            }
            if (ZoomOut.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(58, "Page Down");

            if (ZoomOut.HandleInput(input))
            {
                GameAudio.MiniMapButton();
                screen.AdjustCamTimer        = 1f;
                screen.transitionElapsedTime = 0f;
                screen.CamDestination.X      = screen.CamPos.X;
                screen.CamDestination.Y      = screen.CamPos.Y;
                screen.CamDestination.Z      = 4200000f * UniverseScreen.GameScaleStatic;
                return true;
            }
            if (DeepSpaceBuild.Rect.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(54, "B");
            }
            if (DeepSpaceBuild.HandleInput(input))
            {
                GameAudio.MiniMapButton();
                if (screen.showingDSBW)
                {
                    screen.showingDSBW = false;
                }
                else
                {
                    screen.dsbw = new DeepSpaceBuildingWindow(screen.ScreenManager, screen);
                    screen.showingDSBW = true;
                }
                return true;
            }
            if (PlanetScreen.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(56);

            if (PlanetScreen.HandleInput(input))
            {
                GameAudio.MiniMapButton();
                screen.ScreenManager.AddScreen(new PlanetListScreen(screen, screen.EmpireUI));
                return true;
            }
            if (ShipScreen.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(223, "F1");

            if (ShipScreen.HandleInput(input))
            {                
                GameAudio.MiniMapButton();
                screen.showingFTLOverlay = !screen.showingFTLOverlay;
                return true;
            }
            if (Fleets.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(224, "F2");

            if (Fleets.HandleInput(input))
            {
                GameAudio.MiniMapButton();
                screen.showingRangeOverlay = !screen.showingRangeOverlay;
                return true;
            }
            if (AIScreen.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(59, "H");

            if (!AIScreen.HandleInput(input)) return false;

            screen.aw.ToggleVisibility();
            return true;
        }
    }
}