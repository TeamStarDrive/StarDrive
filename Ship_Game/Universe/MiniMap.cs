using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Empires.Components;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

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
        Empire Player => EmpireManager.Player;
        float pulseTime => Screen.NormalFlashTimer;
        float quickPulseTime => Screen.FastFlashTimer;

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

            try
            {
                DrawInfluenceNodes(batch);
                DrawSelected(batch, Player);
                DrawWarnings(batch);
            }
            catch (Exception e)
            {
                Log.Error($"MiniMap Draw crashed {e.InnerException}");
            }

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

        void DrawWarnings(SpriteBatch batch)
        {
            float radius = 0.02f;
            float ringRad = 0.023f * pulseTime;
            foreach (IncomingThreat threat in Player.SystemsWithThreat)
            {
                if (threat.ThreatTimedOut) continue;

                var system            = threat.TargetSystem;
                Vector2 miniSystemPos = WorldToMiniPos(system.Position);
                float pulseRad = radius + ringRad;
                batch.Draw(Node1, miniSystemPos, Color.Red, 0f, Node.CenterF, pulseRad + 0.009f, SpriteEffects.None, 0f);
                batch.Draw(Node1, miniSystemPos, Color.Black, 0f, Node.CenterF, pulseRad + 0.002f, SpriteEffects.None, 0f);
                batch.Draw(Node1, miniSystemPos, Color.Red, 0f, Node.CenterF, radius , SpriteEffects.None, 0f);
            }

            //foreach (var system in Screen.SolarSystemDict)
            foreach (var system in Player.GetEmpireAI().ThreatMatrix.GetAllSystemsWithFactions())
            {
                if (system.OwnerList.Count > 0) continue;
                var pin = system;
                var point = WorldToMiniPos(pin.Position);
                radius = 0.025f * Screen.SlowFlashTimer;
                var color = Color.Yellow;
                batch.Draw(Node1, point, Color.Black, 0f, Node.CenterF, radius, SpriteEffects.None, 1f);
                batch.Draw(Node1, point, color, 0f, Node.CenterF, radius - 0.0055f, SpriteEffects.None, 1f);
                batch.Draw(Node1, point, Color.Black, 0f, Node.CenterF, radius - 0.0055f * 2,
                    SpriteEffects.None, 1f);
            }

            foreach (ThreatMatrix.Pin badBase in Player.GetEmpireAI().ThreatMatrix.GetAllFactionBases())
            {
                var pin = badBase;
                var point = WorldToMiniPos(pin.Position);
                radius = 0.025f * Screen.SlowFlashTimer;
                var color = pin.GetEmpire().EmpireColor;
                var warningColor = new Color(Color.Yellow, 200);
                batch.Draw(Node1, point, warningColor, 0f, Node.CenterF, radius, SpriteEffects.None, 1f);
                batch.Draw(Node1, point, Color.Black, 0f, Node.CenterF, radius - 0.005f, SpriteEffects.None, 1f);
                batch.Draw(Node1, point, color, 0f, Node.CenterF, 0.012f,
                    SpriteEffects.None, 1f);
            }
        }

        void DrawSelected(SpriteBatch batch, Empire empire)
        {
            Ship ship     = Screen.SelectedShip;
            Planet planet = Screen.SelectedPlanet;
            var system    = Screen.SelectedSystem;
            var fleet     = Screen.SelectedFleet;
            Ship[] containsShip = Screen.SelectedShipList.ToArray();

            Array<Vector2> centers = new Array<Vector2>();

            if (ship != null) centers.Add(ship.Position);
            else if (planet != null) centers.Add(planet.Center);
            else if (system != null) centers.Add(system.Position);
            else if (fleet != null) centers = new Array<Vector2>(fleet.Ships.Select(s=> s.Position));
            else if (containsShip?.Length > 0)  centers = new Array<Vector2>(containsShip.Select(s => s.Position));

            float radius = 0.023f;
            foreach (var center in centers)
            {
                var nodePos = WorldToMiniPos(center);

                batch.Draw(Node1, nodePos, new Color(Color.Black, (byte)(255 * quickPulseTime)), 0f, Node.CenterF, radius, SpriteEffects.None, 1f);
                batch.Draw(Node1, nodePos, new Color(Color.LightGray, (byte)(255 * quickPulseTime)), 0f, Node.CenterF, radius * quickPulseTime, SpriteEffects.None, 1f);
            }
        }

        void DrawNodes(SpriteBatch batch, Empire empire, Empire.InfluenceNode[] nodes, bool excludeProjectors)
        {
            Vector2 nodeOrigin = Node.CenterF;
            var transparentBlack = new Color(Color.Black, 80);

            for (int i = 0; i < nodes.Length; i++)
            {
                ref Empire.InfluenceNode node = ref nodes[i];
                if (!node.KnownToPlayer)
                    continue;

                bool combat = false;
                float intensity = 0.005f;
                if (empire.isPlayer)
                {
                    if (node.SourceObject is Ship ship)
                    {
                        if (excludeProjectors && ship.IsSubspaceProjector)
                            continue;
                        if (empire.isPlayer && ship.InCombat)
                        {
                            combat = true;
                        }
                    }
                    else if (node.SourceObject is Planet planet)
                    {
                        if (planet.RecentCombat)
                        {
                            combat = true;
                            intensity += 0.001f;
                        }
                        else if (planet.SpaceCombatNearPlanet)
                        {
                            combat = true;
                        }
                    }
                }

                float nodeRad = WorldToMiniRadius(node.Radius);
                var ec = new Color(empire.EmpireColor, 150);
                Vector2 nodePos = WorldToMiniPos(node.Position);
                
                if (combat)
                {
                    float radius = Math.Max(0.02f, nodeRad) * pulseTime;
                    batch.Draw(Node1, nodePos, Color.Black, 0f, nodeOrigin, radius - intensity, SpriteEffects.None, 0f);
                    batch.Draw(Node1, nodePos, Color.Red,   0f, nodeOrigin, radius, SpriteEffects.None, 0f);
                    batch.Draw(Node1, nodePos, Color.Black, 0f, nodeOrigin, radius - intensity * 2, SpriteEffects.None, 0f);
                }
                
                {
                    float radius = Math.Min(0.09f, nodeRad);
                    // draw a shade to dim the color. 
                    batch.Draw(Node1, nodePos, ec, 0f, nodeOrigin, radius, SpriteEffects.None, 1f);
                    batch.Draw(Node1, nodePos, transparentBlack, 0f, nodeOrigin, nodeRad, SpriteEffects.None, 1f);
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
                if (rel.Known || Screen.Debug)
                {
                    DrawEmpireNodes(batch, e);
                }
            }

            DrawEmpireNodes(batch, EmpireManager.Player);
        }
        
        void DrawEmpireNodes(SpriteBatch batch, Empire e)
        {
            DrawNodes(batch, e, e.BorderNodes, excludeProjectors:false);
            DrawNodes(batch, e, e.SensorNodes, excludeProjectors:true);
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
                ToolTip.CreateTooltip(GameText.ZoomsToYourCurrentlySelected, "Page Up");

            if (ZoomOut.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(GameText.ZoomOutToTheGalaxy, "Page Down");

            if (DeepSpaceBuild.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(GameText.OpensTheDeepSpaceBuilding, "B");

            if (PlanetScreen.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(GameText.OpensPlanetReconnaissancePanel, "L");

            if (ShipScreen.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(GameText.FtlOverlayVisualisesSubspaceProjection, "F1");

            if (Fleets.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(GameText.WeaponsRangeOverlayVisualisesShips, "F2");

            if (AIScreen.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(GameText.OpensTheAutomationPanelWhich, "H");

            return HandleInput(input);
        }
    }
}