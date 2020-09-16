using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Gameplay;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed class MiniMap : UIElementContainer
    {
        readonly Rectangle Housing;

        Rectangle ActualMap;

        //to get rid of these I need to find a solution for hover and the setting of the active setting
        readonly ToggleButton ZoomOut;
        readonly ToggleButton ZoomToShip;
        readonly ToggleButton PlanetScreen;
        readonly ToggleButton ShipScreen;
        readonly ToggleButton AIScreen;
        readonly ToggleButton DeepSpaceBuild;
        readonly ToggleButton Fleets;
        public UniverseScreen Screen => Empire.Universe;

        readonly SubTexture MiniMapHousing;
        readonly SubTexture Node;
        readonly SubTexture Node1;
        readonly float Scale;
        readonly Vector2 MiniMapZero;

        public MiniMap(in Rectangle housing) : base(housing)
        {
            Housing        = housing;
            MiniMapHousing = ResourceManager.Texture("Minimap/radar_over");
            Node           = ResourceManager.Texture("UI/node");
            Node1          = ResourceManager.Texture("UI/node1");
            ActualMap      = new Rectangle(housing.X + 61 + 20, housing.Y + 33, 200, 210);

            UIList list = AddList(new Vector2(Housing.X + 14, Housing.Y + 70));
            list.Name = "MiniMapButtons";
            ZoomToShip     = list.Add(new ToggleButton(ToggleButtonStyle.ButtonC, "Minimap/icons_zoomctrl", ZoomToShip_OnClick));
            ZoomOut        = list.Add(new ToggleButton(ToggleButtonStyle.ButtonC, "Minimap/icons_zoomout", ZoomOut_OnClick));
            PlanetScreen   = list.Add(new ToggleButton(ToggleButtonStyle.ButtonB, "UI/icon_planetslist", PlanetScreen_OnClick));
            ShipScreen     = list.Add(new ToggleButton(ToggleButtonStyle.Button,  "UI/icon_ftloverlay", ShipScreen_OnClick));
            Fleets         = list.Add(new ToggleButton(ToggleButtonStyle.Button,  "UI/icon_rangeoverlay", Fleets_OnClick));
            DeepSpaceBuild = list.Add(new ToggleButton(ToggleButtonStyle.Button,  "UI/icon_dsbw", DeepSpaceBuild_OnClick));
            AIScreen       = list.Add(new ToggleButton(ToggleButtonStyle.ButtonDown, "AI", AIScreen_OnClick));

            Scale = ActualMap.Width / (Screen.UniverseSize * 2.1f); // Updated to play nice with the new negative map values
            MiniMapZero = new Vector2((float)ActualMap.X + 100, (float)ActualMap.Y + 100);
        }

        Vector2 WorldToMiniPos(Vector2 pos)        
            => new Vector2(MiniMapZero.X + pos.X * Scale, MiniMapZero.Y + pos.Y * Scale);

        float WorldToMiniRadius(float radius)
        {
            float miniRadius = radius * Scale;
            float rscale = miniRadius * 0.004f;
            rscale = Math.Max(0.006f, rscale);
            return rscale;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            UniverseScreen screen = Empire.Universe;
            Rectangle inflateMap = ActualMap;
            inflateMap.Inflate(10, 10);
            screen.DrawRectangle(inflateMap, Color.Black, Color.Black);
            batch.Draw(MiniMapHousing, Housing, Color.White);
            
            foreach (SolarSystem system in UniverseScreen.SolarSystemList)
            {
                Vector2 miniSystemPos = WorldToMiniPos(system.Position);
                var star = new Rectangle((int)miniSystemPos.X, (int)miniSystemPos.Y, 2, 2);
                batch.FillRectangle(star, Color.Gray);
            }
            DrawInfluenceNodes(batch);
            
            Vector2 upperLeftView = screen.UnprojectToWorldPosition(new Vector2(0f, 0f));
            upperLeftView = new Vector2(HelperFunctions.RoundTo(upperLeftView.X, 1), HelperFunctions.RoundTo(upperLeftView.Y, 1));
            
            var right = screen.UnprojectToWorldPosition(new Vector2(screen.ScreenWidth, 0f));

            right = new Vector2(HelperFunctions.RoundTo(right.X, 1), 0f);
            
            float xdist = (right.X - upperLeftView.X) * Scale;
            xdist = HelperFunctions.RoundTo(xdist, 1);

            float ydist = xdist * screen.ScreenHeight / screen.ScreenWidth;
            ydist = HelperFunctions.RoundTo(ydist, 1);
            // draw and clamp minimap viewing area rectangle.
            var lookingAt = new Rectangle((int)MiniMapZero.X + (int)(upperLeftView.X * Scale), 
                                          (int)MiniMapZero.Y + (int)(upperLeftView.Y * Scale),
                                          (int)xdist, (int)ydist);
            if (lookingAt.Width < 2)
            {
                lookingAt.Width  = 2;
                lookingAt.Height = 2;
            }
            float lookRightEdge = lookingAt.X;
            float lookBottomEdge = lookingAt.Y;

            lookingAt.X = (int)lookRightEdge.UpperBound(ActualMap.X + ActualMap.Width - lookingAt.Width);
            lookingAt.Y = (int)lookBottomEdge.UpperBound(ActualMap.Height + ActualMap.Y - lookingAt.Height);
            lookingAt.X = (int)lookingAt.X.LowerBound(ActualMap.X);
            lookingAt.Y = (int)lookingAt.Y.LowerBound(ActualMap.Y);

            batch.FillRectangle(lookingAt, new Color(255, 255, 255, 30));
            batch.DrawRectangle(lookingAt, Color.White);
            var topMiddleView   = new Vector2(lookingAt.X +  lookingAt.Width / 2, lookingAt.Y);
            var botMiddleView   = new Vector2(topMiddleView.X - 1f, lookingAt.Y + lookingAt.Height);
            var leftMiddleView  = new Vector2(lookingAt.X, lookingAt.Y + lookingAt.Height / 2);
            var rightMiddleView = new Vector2(lookingAt.X + lookingAt.Width, leftMiddleView.Y + 1f);
            batch.DrawLine(new Vector2(topMiddleView.X, MiniMapZero.Y - 100), topMiddleView, Color.White);
            batch.DrawLine(new Vector2(botMiddleView.X, ActualMap.Y + ActualMap.Height), botMiddleView, Color.White);
            batch.DrawLine(new Vector2(ActualMap.X, leftMiddleView.Y), leftMiddleView, Color.White);
            batch.DrawLine(new Vector2(ActualMap.X + ActualMap.Width, rightMiddleView.Y), rightMiddleView, Color.White);

            ShipScreen.IsToggled     = screen.showingFTLOverlay;
            DeepSpaceBuild.IsToggled = screen.DeepSpaceBuildWindow.Visible;
            AIScreen.IsToggled       = screen.aw.IsOpen;
            Fleets.IsToggled         = screen.showingRangeOverlay;            
            base.Draw(batch, elapsed);
        }

        void DrawNode(Empire empire, IList<Empire.InfluenceNode> list, SpriteBatch batch)
        {
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Empire.InfluenceNode node = list[i];
                    if (node == null || !node.KnownToPlayer)
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
                    var ec = new Color(empire.EmpireColor, 200);

                    batch.Draw(Node1, nodePos, ec, 0f, Node.CenterF, radius, SpriteEffects.None, 1f);
                    batch.Draw(Node1, nodePos, new Color(Color.Black, 40), 0f, Node.CenterF, radius, SpriteEffects.None, 1f);
                }
            }
        }

        void DrawInfluenceNodes(SpriteBatch batch)
        {
            for (int i = 0; i < EmpireManager.Empires.Count; i++)
            {
                Empire e = EmpireManager.Empires[i];
                // Draw player nodes last so it will be over allied races - this is a temp solution
                if (e.isPlayer) 
                    continue; 

                Relationship rel = EmpireManager.Player.GetRelations(e);
                if (Screen.Debug || e == EmpireManager.Player || rel.Known)
                {
                    DrawNode(e, batch);
                }
            }
            Empire player = EmpireManager.Player;
            DrawNode(player, batch);
        }
        
        void DrawNode(Empire e, SpriteBatch batch)
        {
            DrawNode(e, e.BorderNodes.AtomicCopy(), batch);
            DrawNode(e, e.SensorNodes.AtomicCopy(), batch);
        }

        void ZoomToShip_OnClick(ToggleButton toggleButton)
        {
            Empire.Universe.InputZoomToShip();
            GameAudio.AcceptClick();
        }

        void ZoomOut_OnClick(ToggleButton toggleButton)
        {
            Empire.Universe.InputZoomOut();
            GameAudio.AcceptClick();
        }

        public void DeepSpaceBuild_OnClick(ToggleButton toggleButton)
        {
            Screen.InputOpenDeepSpaceBuildWindow();
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

        public void AIScreen_OnClick(ToggleButton toggleButton)
        {
            Screen.aw.ToggleVisibility();
        }
        
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