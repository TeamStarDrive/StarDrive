using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Gameplay;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed class MiniMap: UIElementContainer
    {
        private readonly Rectangle Housing;

        private Rectangle ActualMap;

        //to get rid of these I need to find a solution for hover and the setting of the active setting
        private readonly ToggleButton ZoomOut;
        private readonly ToggleButton ZoomToShip;
        private readonly ToggleButton PlanetScreen;
        private readonly ToggleButton ShipScreen;
        private readonly ToggleButton AIScreen;
        private readonly ToggleButton DeepSpaceBuild;
        private readonly ToggleButton Fleets;
        public UniverseScreen Screen => Empire.Universe;

        private readonly SubTexture MiniMapHousing;
        private readonly SubTexture Node;
        private readonly SubTexture Node1;
        private readonly float Scale;
        private readonly Vector2 MiniMapZero;

        public MiniMap(Rectangle housing) : base(null, housing)
        {
            Housing        = housing;
            MiniMapHousing = ResourceManager.Texture("Minimap/radar_over");
            Node           = ResourceManager.Texture("UI/node");
            Node1          = ResourceManager.Texture("UI/node1");
            ActualMap      = new Rectangle(housing.X + 61 + 20, housing.Y + 33, 200, 210);

            BeginVLayout(Housing.X + 14, Housing.Y + 70, 25);
                ZoomToShip     = ToggleButton(ToggleButtonStyle.ButtonC, "Minimap/icons_zoomctrl", ZoomToShip_OnClick);
                ZoomOut        = ToggleButton(ToggleButtonStyle.ButtonC, "Minimap/icons_zoomout", ZoomOut_OnClick);
                PlanetScreen   = ToggleButton(ToggleButtonStyle.ButtonB, "UI/icon_planetslist", PlanetScreen_OnClick);
                ShipScreen     = ToggleButton(ToggleButtonStyle.Button, "UI/icon_ftloverlay", ShipScreen_OnClick);
                Fleets         = ToggleButton(ToggleButtonStyle.Button, "UI/icon_rangeoverlay", Fleets_OnClick);
                DeepSpaceBuild = ToggleButton(ToggleButtonStyle.Button, "UI/icon_dsbw", DeepSpaceBuild_OnClick);
                AIScreen       = ToggleButton(ToggleButtonStyle.ButtonDown, "AI", AIScreen_OnClick);
            EndLayout();

            Scale = ActualMap.Width / (Screen.UniverseSize * 2.1f); // Updated to play nice with the new negative map values
            MiniMapZero = new Vector2((float)ActualMap.X + 100, (float)ActualMap.Y + 100);
        }     
        
        private Vector2 WorldToMiniPos(Vector2 pos)        
            => new Vector2(MiniMapZero.X + pos.X * Scale, MiniMapZero.Y + pos.Y * Scale);
        
        private float WorldToMiniRadius(float radius)
        {
            float miniRadius = radius * Scale;
            float rscale = miniRadius * 0.004f;
            
            rscale = Math.Max(.006f, rscale);
            return rscale;
        }


        public void Draw(ScreenManager screenManager, UniverseScreen screen)
        {
            Rectangle inflateMap = ActualMap;
            inflateMap.Inflate(10, 10);
            screen.DrawRectangle(inflateMap, Color.Black, Color.Black);
            screenManager.SpriteBatch.Draw(MiniMapHousing, Housing, Color.White);
            
            foreach (SolarSystem system in UniverseScreen.SolarSystemList)
            {
                Vector2 miniSystemPos = WorldToMiniPos(system.Position);
                var star = new Rectangle((int)miniSystemPos.X, (int)miniSystemPos.Y, 2, 2);
                screenManager.SpriteBatch.FillRectangle(star, Color.Gray);
            }
            DrawInfluenceNodes(screenManager);
            
            Vector2 upperLeftView = screen.UnprojectToWorldPosition(new Vector2(0f, 0f));
            upperLeftView         = new Vector2(HelperFunctions.RoundTo(upperLeftView.X, 1), HelperFunctions.RoundTo(upperLeftView.Y, 1));
            Vector2 right         = screen.UnprojectToWorldPosition(new Vector2(screenManager.GraphicsDevice.PresentationParameters.BackBufferWidth, 0f));
            right                 = new Vector2(HelperFunctions.RoundTo(right.X, 1), 0f);
            float xdist           = (right.X - upperLeftView.X) * Scale;
            xdist                 = HelperFunctions.RoundTo(xdist, 1);
            float ydist           = xdist * screenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / screenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
            ydist = HelperFunctions.RoundTo(ydist, 1);
            //draw and clamp minimap viewing area rectangle.
            Rectangle lookingAt   = new Rectangle((int)MiniMapZero.X + (int)(upperLeftView.X * Scale), (int)MiniMapZero.Y + (int)(upperLeftView.Y * Scale), (int)xdist, (int)ydist);
            if (lookingAt.Width < 2)
            {
                lookingAt.Width  = 2;
                lookingAt.Height = 2;
            }
            if (lookingAt.X < ActualMap.X) lookingAt.X = ActualMap.X;
            if (lookingAt.Y < ActualMap.Y) lookingAt.Y = ActualMap.Y;

            float lookRightEdge  = lookingAt.X + lookingAt.Width;
            float lookBottomEdge = lookingAt.Y + lookingAt.Height;
            lookingAt.X          = lookRightEdge > ActualMap.Width + ActualMap.X ? ActualMap.X + ActualMap.Width - lookingAt.Width : lookingAt.X;
            lookingAt.Y          = lookBottomEdge > ActualMap.Height + ActualMap.Y ? ActualMap.Height + ActualMap.Y  - lookingAt.Height : lookingAt.Y;

            screenManager.SpriteBatch.FillRectangle(lookingAt, new Color(255, 255, 255, 30));
            screenManager.SpriteBatch.DrawRectangle(lookingAt, Color.White);
            var topMiddleView   = new Vector2(lookingAt.X +  lookingAt.Width / 2, lookingAt.Y);
            var botMiddleView   = new Vector2(topMiddleView.X - 1f, lookingAt.Y + lookingAt.Height);
            var leftMiddleView  = new Vector2(lookingAt.X, lookingAt.Y + lookingAt.Height / 2);
            var rightMiddleView = new Vector2(lookingAt.X + lookingAt.Width, leftMiddleView.Y + 1f);
            screenManager.SpriteBatch.DrawLine(new Vector2(topMiddleView.X, MiniMapZero.Y - 100), topMiddleView, Color.White);
            screenManager.SpriteBatch.DrawLine(new Vector2(botMiddleView.X, ActualMap.Y + ActualMap.Height), botMiddleView, Color.White);
            screenManager.SpriteBatch.DrawLine(new Vector2(ActualMap.X, leftMiddleView.Y), leftMiddleView, Color.White);
            screenManager.SpriteBatch.DrawLine(new Vector2(ActualMap.X + ActualMap.Width, rightMiddleView.Y), rightMiddleView, Color.White);

            ShipScreen.Active     = screen.showingFTLOverlay;
            DeepSpaceBuild.Active = screen.showingDSBW;
            AIScreen.Active       = screen.aw.IsOpen;
            Fleets.Active         = screen.showingRangeOverlay;            
            base.Draw(screenManager.SpriteBatch);
        }

        void DrawNode(Empire empire, BatchRemovalCollection<Empire.InfluenceNode> list, ScreenManager screenManager)
        {
            using (list.AcquireReadLock())
                for (int i = 0; i < list.Count; i++)
                {
                    Empire.InfluenceNode node = list[i];
                    if (!Empire.Universe.Debug)
                        if (!node.Known)
                            continue;

                    float nodeRad = WorldToMiniRadius(node.Radius);
                    if (node.SourceObject is GameplayObject)
                    {
                        nodeRad = Math.Max(nodeRad, .007f);
                    }
                    else
                    {
                        nodeRad = Math.Max(nodeRad, .016f);
                    }
                    float radius = nodeRad;
                    Vector2 nodePos = WorldToMiniPos(node.Position);
                    Color ec = new Color(empire.EmpireColor, 200);

                    screenManager.SpriteBatch.Draw(Node1, nodePos, ec, 0f, Node.CenterF, radius,
                        SpriteEffects.None, 1f);
                    screenManager.SpriteBatch.Draw(Node1, nodePos, new Color(Color.Black, 40), 0f, Node.CenterF, radius,
                        SpriteEffects.None, 1f);
                }
        }

        private void DrawInfluenceNodes(ScreenManager screenManager)
        {

            foreach (Empire e in EmpireManager.Empires)
            {
                Relationship rel = EmpireManager.Player.GetRelations(e);
                if (!Screen.Debug && e != EmpireManager.Player && !rel.Known)
                    continue;
                DrawNode(e, e.BorderNodes, screenManager);
                DrawNode(e, e.SensorNodes, screenManager);
            }
        }

        private void ZoomToShip_OnClick(ToggleButton toggleButton)
        {
            Empire.Universe.InputZoomToShip();
            GameAudio.AcceptClick();

        }

        private void ZoomOut_OnClick(ToggleButton toggleButton)
        {
            Empire.Universe.InputZoomOut();
            GameAudio.AcceptClick();

        }
        public void DeepSpaceBuild_OnClick(ToggleButton toggleButton)
        {
            GameAudio.AcceptClick();
            if (Screen.showingDSBW)
            {
                Screen.showingDSBW = false;
            }
            else
            {
                Screen.dsbw = new DeepSpaceBuildingWindow(Screen.ScreenManager, Screen);
                Screen.showingDSBW = true;
            }
        }
        public void PlanetScreen_OnClick(ToggleButton toggleButton)
        {
            GameAudio.AcceptClick();
            Screen.ScreenManager.AddScreen(new PlanetListScreen(Screen, Screen.EmpireUI));

        }
        public void ShipScreen_OnClick(ToggleButton toggleButton)
        {

            GameAudio.AcceptClick();
            Screen.showingFTLOverlay = !Screen.showingFTLOverlay;
            
        }
        public void Fleets_OnClick(ToggleButton toggleButton)
        {
            GameAudio.AcceptClick();
            Screen.showingRangeOverlay = !Screen.showingRangeOverlay;            
        }
        public void AIScreen_OnClick(ToggleButton toggleButton) => Screen.aw.ToggleVisibility();
        
        public bool HandleInput(InputState input, UniverseScreen screen)
        {
            if (!Housing.HitTest(input.CursorPosition)) return false;
            if (ZoomToShip.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(57, "Page Up");

            if (ZoomOut.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(58, "Page Down");

            if (DeepSpaceBuild.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(54, "B");

            if (PlanetScreen.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(56);

            if (ShipScreen.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(223, "F1");

            if (Fleets.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(224, "F2");

            if (AIScreen.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(59, "H");

            return HandleInput(input);
            
        }
    }
}