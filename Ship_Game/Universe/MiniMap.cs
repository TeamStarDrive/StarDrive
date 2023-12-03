using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Empires.Components;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed class MiniMap : UIElementContainer
    {
        readonly UniverseScreen Universe;

        readonly Rectangle Housing;

        Rectangle ActualMap;

        //to get rid of these I need to find a solution for hover and the setting of the active setting
        readonly ToggleButton ZoomOut;
        readonly ToggleButton ZoomToShip;
        readonly ToggleButton PlanetScreen;
        readonly ToggleButton ExoticBonuses;
        //readonly ToggleButton ExoticBonusesSmall;
        readonly ToggleButton ExoticScreen;
        readonly ToggleButton ShipScreen;
        readonly ToggleButton AIScreen;
        readonly ToggleButton DeepSpaceBuild;
        readonly ToggleButton Fleets;

        readonly SubTexture MiniMapHousing;
        readonly SubTexture Node;
        readonly SubTexture Node1;
        readonly float Scale;
        readonly Vector2 MiniMapZero;
        Empire Player => Universe.Player;
        float pulseTime => Universe.NormalFlashTimer;
        float quickPulseTime => Universe.FastFlashTimer;

        public MiniMap(UniverseScreen universe, in Rectangle housing) : base(housing)
        {
            Universe = universe;
            Housing        = housing;
            MiniMapHousing = ResourceManager.Texture("Minimap/radar_over");
            Node           = ResourceManager.Texture("UI/node");
            Node1          = ResourceManager.Texture("UI/node1");
            ActualMap      = new Rectangle(housing.X + 61 + 20, housing.Y + 33, 200, 210);

            UIList listL = AddList(new Vector2(Housing.X + 10, Housing.Y + 70));
            listL.Name = "MiniMapButtons";
            ZoomToShip     = listL.Add(new ToggleButton(ToggleButtonStyle.ButtonC, "Minimap/icons_zoomctrl", ZoomToShip_OnClick));
            PlanetScreen   = listL.Add(new ToggleButton(ToggleButtonStyle.ButtonB, "UI/icon_planetslist", PlanetScreen_OnClick));
            ExoticBonuses  = listL.Add(new ToggleButton(ToggleButtonStyle.ButtonB, "NewUI/icon_exotic_Bonuses_big", ExoticBonusScreen_OnClick));
            ShipScreen     = listL.Add(new ToggleButton(ToggleButtonStyle.Button,  "UI/icon_ftloverlay", ShipScreen_OnClick));
            Fleets         = listL.Add(new ToggleButton(ToggleButtonStyle.Button,  "UI/icon_rangeoverlay", Fleets_OnClick));
            DeepSpaceBuild = listL.Add(new ToggleButton(ToggleButtonStyle.Button,  "UI/icon_dsbw", DeepSpaceBuild_OnClick));
            AIScreen       = listL.Add(new ToggleButton(ToggleButtonStyle.ButtonDown, "AI", AIScreen_OnClick));

            UIList listR = AddList(new Vector2(Housing.X + 38, Housing.Y + 70));
            listR.Name = "MiniMapButtonsRight";
            ZoomOut            = listR.Add(new ToggleButton(ToggleButtonStyle.ButtonC, "Minimap/icons_zoomout", ZoomOut_OnClick));
            ExoticScreen       = listR.Add(new ToggleButton(ToggleButtonStyle.ButtonB, "UI/icon_exotic_systems", ExoticScreen_OnClick));
            // will use the below for freighters screen
            //ExoticBonusesSmall = listR.Add(new ToggleButton(ToggleButtonStyle.ButtonB, "NewUI/icon_exotic_Bonuses_small", ExoticBonusScreen_OnClick));

            Scale = ActualMap.Width / (Universe.UState.Size * 2.1f); // Updated to play nice with the new negative map values
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

            Rectangle inflateMap = ActualMap;
            inflateMap.Inflate(10, 10);
            Universe.DrawRectangle(inflateMap, Color.Black, Color.Black);
            batch.Draw(MiniMapHousing, Housing, Color.White);
            
            foreach (SolarSystem system in Universe.UState.Systems)
            {
                Vector2 miniSystemPos = WorldToMiniPos(system.Position);
                var star = new Rectangle((int)miniSystemPos.X, (int)miniSystemPos.Y, 2, 2);
                batch.FillRectangle(star, Color.Gray);
            }

            try
            {
                DrawMinimapInfluenceNodes(batch);
                DrawSelected(batch, Player);
                DrawWarnings(batch);
            }
            catch (Exception e)
            {
                Log.Error(e, $"MiniMap Draw crashed {e.InnerException}");
            }

            Vector2 upperLeftView = Universe.UnprojectToWorldPosition(new Vector2(0f, 0f));
            upperLeftView = new Vector2(HelperFunctions.RoundTo(upperLeftView.X, 1), HelperFunctions.RoundTo(upperLeftView.Y, 1));
            
            var right = Universe.UnprojectToWorldPosition(new Vector2(Universe.ScreenWidth, 0f));

            right = new Vector2(HelperFunctions.RoundTo(right.X, 1), 0f);
            
            float xdist = (right.X - upperLeftView.X) * Scale;
            xdist = HelperFunctions.RoundTo(xdist, 1);

            float ydist = xdist * Universe.ScreenHeight / Universe.ScreenWidth;
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

            ShipScreen.IsToggled     = Universe.ShowingFTLOverlay;
            DeepSpaceBuild.IsToggled = Universe.DeepSpaceBuildWindow.Visible;
            AIScreen.IsToggled       = Universe.aw.IsOpen;
            ExoticBonuses.IsToggled  = Universe.ExoticBonusesWindow.IsOpen;
            Fleets.IsToggled         = Universe.ShowingRangeOverlay;
            
            base.Draw(batch, elapsed);
        }

        void DrawWarnings(SpriteBatch batch)
        {
            float radius = 0.02f;
            float ringRad = 0.023f * pulseTime;
            foreach (IncomingThreat threat in Player.SystemsWithThreat)
            {
                DrawThreats(Color.Red, threat);
            }

            foreach (IncomingThreat threat in Player.AlliedSystemsWithThreat())
            {
                DrawThreats(Color.Orange, threat);
            }

            foreach (var system in Player.AI.ThreatMatrix.GetAllSystemsWithFactions())
            {
                if (system.OwnerList.Count > 0) continue;
                var point = WorldToMiniPos(system.Position);
                radius = 0.025f * Universe.SlowFlashTimer;
                batch.Draw(Node1, point, Color.Black, 0f, Node.CenterF, radius, SpriteEffects.None, 1f);
                batch.Draw(Node1, point, Color.Yellow, 0f, Node.CenterF, radius - 0.0055f, SpriteEffects.None, 1f);
                batch.Draw(Node1, point, Color.Black, 0f, Node.CenterF, radius - 0.0055f * 2, SpriteEffects.None, 1f);
            }

            foreach (ThreatCluster c in Player.AI.ThreatMatrix.GetAllFactionBases())
            {
                var point = WorldToMiniPos(c.Position);
                radius = 0.025f * Universe.SlowFlashTimer;
                var warningColor = new Color(Color.Yellow, 200);
                batch.Draw(Node1, point, warningColor, 0f, Node.CenterF, radius, SpriteEffects.None, 1f);
                batch.Draw(Node1, point, Color.Black, 0f, Node.CenterF, radius - 0.005f, SpriteEffects.None, 1f);
                batch.Draw(Node1, point, c.Loyalty.EmpireColor, 0f, Node.CenterF, 0.012f, SpriteEffects.None, 1f);
            }

            void DrawThreats(Color color, IncomingThreat threat)
            {
                var system = threat.TargetSystem;
                Vector2 miniSystemPos = WorldToMiniPos(system.Position);
                float pulseRad = radius + ringRad;
                batch.Draw(Node1, miniSystemPos, color, 0f, Node.CenterF, pulseRad + 0.009f, SpriteEffects.None, 0f);
                batch.Draw(Node1, miniSystemPos, Color.Black, 0f, Node.CenterF, pulseRad + 0.002f, SpriteEffects.None, 0f);
                batch.Draw(Node1, miniSystemPos, color, 0f, Node.CenterF, radius, SpriteEffects.None, 0f);
            }
        }

        void DrawSelected(SpriteBatch batch, Empire empire)
        {
            Ship ship     = Universe.SelectedShip;
            Planet planet = Universe.SelectedPlanet;
            var system    = Universe.SelectedSystem;
            var fleet     = Universe.SelectedFleet;
            Ship[] selectedShips = Universe.SelectedShips.ToArr();

            Array<Vector2> centers = new();

            if (ship != null) centers.Add(ship.Position);
            else if (planet != null) centers.Add(planet.Position);
            else if (system != null) centers.Add(system.Position);
            else if (fleet != null) centers = new(fleet.Ships.Select(s=> s.Position));
            else if (selectedShips.Length > 0)  centers = new(selectedShips.Select(s => s.Position));

            float radius = 0.023f;
            foreach (var center in centers)
            {
                var nodePos = WorldToMiniPos(center);

                batch.Draw(Node1, nodePos, new Color(Color.Black, (byte)(255 * quickPulseTime)), 0f, Node.CenterF, radius, SpriteEffects.None, 1f);
                batch.Draw(Node1, nodePos, new Color(Color.LightGray, (byte)(255 * quickPulseTime)), 0f, Node.CenterF, radius * quickPulseTime, SpriteEffects.None, 1f);
            }
        }

        void DrawMinimapNodes(SpriteBatch batch, Empire empire,
                              Empire.InfluenceNode[] nodes, bool excludeProjectors)
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
                var ec = new Color(empire.EmpireColor, 150);
                if (empire.isPlayer)
                {
                    if (node.Source is Ship ship)
                    {
                        if (ship.Loyalty != empire) // ignore allied nodes, they are drawn in their own loop
                            continue;
                        if (excludeProjectors && ship.IsSubspaceProjector)
                            continue;
                        if (empire.isPlayer && ship.OnHighAlert)
                            combat = true;
                    }
                    else if (node.Source is Planet planet)
                    {
                        if (planet.Owner != empire) // ignore allied nodes, they are drawn in their own loop
                            continue;
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

        void DrawMinimapInfluenceNodes(SpriteBatch batch)
        {
            UniverseState uState = Universe.UState;
            for (int i = 0; i < uState.Empires.Count; i++)
            {
                Empire e = uState.Empires[i];
                // Draw player nodes last so it will be over allied races
                if (e.isPlayer)
                    continue;

                Relationship rel = uState.Player.GetRelations(e);
                if (rel.Known || Universe.Debug)
                {
                    DrawMinimapEmpireNodes(batch, e);
                }
            }

            DrawMinimapEmpireNodes(batch, uState.Player);
        }
        
        void DrawMinimapEmpireNodes(SpriteBatch batch, Empire e)
        {
            DrawMinimapNodes(batch, e, e.BorderNodes, excludeProjectors:false);
            DrawMinimapNodes(batch, e, e.SensorNodes, excludeProjectors:true);
        }

        void ZoomToShip_OnClick(ToggleButton toggleButton)
        {
            Universe.InputZoomToShip();
            GameAudio.AcceptClick();
        }

        void ZoomOut_OnClick(ToggleButton toggleButton)
        {
            Universe.InputZoomOut();
            GameAudio.AcceptClick();
        }

        public void DeepSpaceBuild_OnClick(ToggleButton toggleButton)
        {
            Universe.InputOpenDeepSpaceBuildWindow();
        }

        public void PlanetScreen_OnClick(ToggleButton toggleButton)
        {
            GameAudio.AcceptClick();
            PlanetScreen.IsToggled = false;
            Universe.ScreenManager.AddScreen(new PlanetListScreen(Universe, Universe.EmpireUI));
        }

        public void ExoticScreen_OnClick(ToggleButton toggleButton)
        {
            GameAudio.AcceptClick();
            ExoticScreen.IsToggled = false;
            Universe.ScreenManager.AddScreen(new ExoticSystemsListScreen(Universe, Universe.EmpireUI));
        }

        public void ShipScreen_OnClick(ToggleButton toggleButton)
        {
            GameAudio.AcceptClick();
            Universe.ShowingFTLOverlay = !Universe.ShowingFTLOverlay;
        }

        public void Fleets_OnClick(ToggleButton toggleButton)
        {
            GameAudio.AcceptClick();
            Universe.ShowingRangeOverlay = !Universe.ShowingRangeOverlay;            
        }

        public void AIScreen_OnClick(ToggleButton toggleButton)
        {
            Universe.aw.ToggleVisibility();
        }

        public void ExoticBonusScreen_OnClick(ToggleButton toggleButton)
        {
            Universe.ExoticBonusesWindow.ToggleVisibility();
        }

        public override bool HandleInput(InputState input)
        {
            if (!Housing.HitTest(input.CursorPosition))
                return false;

            if (ZoomToShip.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(GameText.ZoomsToYourCurrentlySelected, "Page Up");

            if (ZoomOut.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(GameText.ZoomOutToTheGalaxy, "Page Down");

            if (DeepSpaceBuild.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(GameText.OpensTheDeepSpaceBuilding, "B");

            if (PlanetScreen.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(GameText.OpensPlanetReconnaissancePanel, "L");

            if (ExoticScreen.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(GameText.OpensExoticPlanetsPanel, "G");

            if (ShipScreen.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(GameText.FtlOverlayVisualisesSubspaceProjection, "F1");

            if (Fleets.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(GameText.WeaponsRangeOverlayVisualisesShips, "F2");

            if (AIScreen.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(GameText.OpensTheAutomationPanelWhich, "H");

            return base.HandleInput(input);
        }
    }
}