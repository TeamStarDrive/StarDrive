using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using Ship_Game.Ships.AI;
using Ship_Game.Fleets;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        static readonly Color FleetLineColor    = new Color(255, 255, 255, 20);
        static readonly Vector2 FleetNameOffset = new Vector2(10f, -6f);
        public static float PulseTimer { get; private set; }

        private void DrawRings(GraphicsDevice device, in Matrix world, float scale)
        {
            device.SamplerStates[0].AddressU          = TextureAddressMode.Wrap;
            device.SamplerStates[0].AddressV          = TextureAddressMode.Wrap;
            device.RenderState.AlphaBlendEnable       = true;
            device.RenderState.AlphaBlendOperation    = BlendFunction.Add;
            device.RenderState.SourceBlend            = Blend.SourceAlpha;
            device.RenderState.DestinationBlend       = Blend.InverseSourceAlpha;
            device.RenderState.DepthBufferWriteEnable = false;
            device.RenderState.CullMode               = CullMode.None;
            foreach (BasicEffect basicEffect in xnaPlanetModel.Meshes[1].Effects)
            {
                basicEffect.World = Matrix.CreateScale(3f) * Matrix.CreateScale(scale) * world;
                basicEffect.View = View;
                basicEffect.DiffuseColor = new Vector3(1f, 1f, 1f);
                basicEffect.Texture = RingTexture;
                basicEffect.TextureEnabled = true;
                basicEffect.Projection = Projection;
            }
            xnaPlanetModel.Meshes[1].Draw();
            device.RenderState.DepthBufferWriteEnable = true;
        }

        private void DrawAtmo(GraphicsDevice device, Model model, in Matrix world)
        {
            device.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            device.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            RenderState rs = device.RenderState;
            rs.AlphaBlendEnable = true;
            rs.AlphaBlendOperation = BlendFunction.Add;
            rs.SourceBlend = Blend.SourceAlpha;
            rs.DestinationBlend = Blend.InverseSourceAlpha;
            rs.DepthBufferWriteEnable = false;
            rs.CullMode = CullMode.CullClockwiseFace;

            ModelMesh modelMesh = model.Meshes[0];
            SubTexture atmosphere = ResourceManager.Texture("Atmos");

            foreach (BasicEffect effect in modelMesh.Effects)
            {
                effect.World = Matrix.CreateScale(4.1f) * world;
                effect.View = View;
                // @todo 3D Texture Atlas support?
                effect.Texture = atmosphere.Texture;
                effect.TextureEnabled = true;
                effect.Projection = Projection;
                effect.LightingEnabled = true;
                effect.DirectionalLight0.DiffuseColor = new Vector3(1f, 1f, 1f);
                effect.DirectionalLight0.Enabled = true;
                effect.DirectionalLight0.SpecularColor = new Vector3(1f, 1f, 1f);
                effect.DirectionalLight0.Direction = new Vector3(0.98f, -0.025f, 0.2f);
                effect.DirectionalLight1.DiffuseColor = new Vector3(1f, 1f, 1f);
                effect.DirectionalLight1.Enabled = true;
                effect.DirectionalLight1.SpecularColor = new Vector3(1f, 1f, 1f);
                effect.DirectionalLight1.Direction = new Vector3(0.98f, -0.025f, 0.2f);
            }
            modelMesh.Draw();
            DrawAtmo1(world);
            rs.DepthBufferWriteEnable = true;
            rs.CullMode = CullMode.CullCounterClockwiseFace;
            rs.AlphaBlendEnable = false;
        }

        private void DrawAtmo1(in Matrix world)
        {
            AtmoEffect.Parameters["World"].SetValue(Matrix.CreateScale(3.83f) * world);
            AtmoEffect.Parameters["Projection"].SetValue(Projection);
            AtmoEffect.Parameters["View"].SetValue(View);
            AtmoEffect.Parameters["CameraPosition"].SetValue(new Vector3(0.0f, 0.0f, 1500f));
            AtmoEffect.Parameters["DiffuseLightDirection"].SetValue(new Vector3(-0.98f, 0.425f, -0.4f));
            for (int pass = 0; pass < AtmoEffect.CurrentTechnique.Passes.Count; ++pass)
            {
                for (int mesh = 0; mesh < atmoModel.Meshes.Count; ++mesh)
                {
                    ModelMesh modelMesh = atmoModel.Meshes[mesh];
                    for (int part = 0; part < modelMesh.MeshParts.Count; ++part)
                        modelMesh.MeshParts[part].Effect = AtmoEffect;
                    modelMesh.Draw();
                }
            }
        }

        private void DrawClouds(GraphicsDevice device, Model model, in Matrix world, Planet p)
        {
            device.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            device.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            RenderState rs = device.RenderState;
            rs.AlphaBlendEnable       = true;
            rs.AlphaBlendOperation    = BlendFunction.Add;
            rs.SourceBlend            = Blend.SourceAlpha;
            rs.DestinationBlend       = Blend.InverseSourceAlpha;
            rs.DepthBufferWriteEnable = false;

            ModelMesh modelMesh = model.Meshes[0];
            foreach (BasicEffect effect in modelMesh.Effects)
            {
                effect.World                           = Matrix.CreateScale(4.05f) * world;
                effect.View                            = View;
                effect.Texture                         = cloudTex;
                effect.TextureEnabled                  = true;
                effect.Projection                      = Projection;
                effect.LightingEnabled                 = true;
                effect.DirectionalLight0.DiffuseColor  = new Vector3(1f, 1f, 1f);
                effect.DirectionalLight0.SpecularColor = new Vector3(1f, 1f, 1f);
                effect.SpecularPower                   = 4;
                if (UseRealLights)
                {
                    Vector2 sunToPlanet = p.Center - p.ParentSystem.Position;
                    effect.DirectionalLight0.Direction = sunToPlanet.ToVec3().Normalized();
                }
                else
                {
                    Vector2 universeCenterToPlanet = p.Center - new Vector2(0, 0);
                    effect.DirectionalLight0.Direction = universeCenterToPlanet.ToVec3().Normalized();
                }
                effect.DirectionalLight0.Enabled = true;
            }
            modelMesh.Draw();
            rs.DepthBufferWriteEnable = true;
        }

        private void DrawToolTip()
        {
            if (SelectedSystem != null && !LookingAtPlanet)
            {
                float num = 4500f;
                Vector3 vector3_1 = Viewport.Project(
                    new Vector3(SelectedSystem.Position, 0.0f), Projection, View, Matrix.Identity);
                Vector2 Position = new Vector2(vector3_1.X, vector3_1.Y);
                Vector3 vector3_2 = Viewport.Project(
                    new Vector3(new Vector2(SelectedSystem.Position.X + num, SelectedSystem.Position.Y),
                        0.0f), Projection, View, Matrix.Identity);
                float Radius = Vector2.Distance(new Vector2(vector3_2.X, vector3_2.Y), Position);
                if (Radius < 5.0)
                    Radius = 5f;
                ScreenManager.SpriteBatch.BracketRectangle(Position, Radius, Color.White);
            }
            if (SelectedPlanet == null || LookingAtPlanet ||
                viewState >= UnivScreenState.GalaxyView)
                return;
            float radius = SelectedPlanet.SO.WorldBoundingSphere.Radius;
            Vector3 vector3_3 = Viewport.Project(
                new Vector3(SelectedPlanet.Center, 2500f), Projection, View, Matrix.Identity);
            Vector2 Position1 = new Vector2(vector3_3.X, vector3_3.Y);
            Vector3 vector3_4 = Viewport.Project(
                new Vector3(SelectedPlanet.Center.PointFromAngle(90f, radius), 2500f), Projection, View,
                Matrix.Identity);
            float Radius1 = Vector2.Distance(new Vector2(vector3_4.X, vector3_4.Y), Position1);
            if (Radius1 < 8.0)
                Radius1 = 8f;
            ScreenManager.SpriteBatch.BracketRectangle(Position1, Radius1,
                SelectedPlanet.Owner?.EmpireColor ?? Color.Gray);
        }

        private void DrawFogNodes()
        {
            var uiNode = ResourceManager.Texture("UI/node");
            var viewport = Viewport;

            foreach (FogOfWarNode fogOfWarNode in FogNodes)
            {
                if (!fogOfWarNode.Discovered)
                    continue;

                Vector3 vector3_1 = viewport.Project(fogOfWarNode.Position.ToVec3(), Projection, View,
                    Matrix.Identity);
                Vector2 vector2 = vector3_1.ToVec2();
                Vector3 vector3_2 = viewport.Project(
                    new Vector3(fogOfWarNode.Position.PointFromAngle(90f, fogOfWarNode.Radius * 1.5f), 0.0f),
                    Projection, View, Matrix.Identity);
                float num = Math.Abs(new Vector2(vector3_2.X, vector3_2.Y).X - vector2.X);
                Rectangle destinationRectangle =
                    new Rectangle((int) vector2.X, (int) vector2.Y, (int) num * 2, (int) num * 2);
                ScreenManager.SpriteBatch.Draw(uiNode, destinationRectangle, new Color(70, 255, 255, 255), 0.0f,
                    uiNode.CenterF, SpriteEffects.None, 1f);
            }
        }

        private void DrawInfluenceNodes()
        {
            var uiNode= ResourceManager.Texture("UI/node");
            var viewport = Viewport;
            using (player.SensorNodes.AcquireReadLock())
                foreach (Empire.InfluenceNode influ in player.SensorNodes)
                {
                    Vector2 screenPos = ProjectToScreenPosition(influ.Position);
                    Vector3 local_4 = viewport.Project(
                        new Vector3(influ.Position.PointFromAngle(90f, influ.Radius * 1.5f), 0.0f), Projection,
                        View, Matrix.Identity);

                    float local_6 = Math.Abs(new Vector2(local_4.X, local_4.Y).X - screenPos.X) * 2.59999990463257f;
                    Rectangle local_7 = new Rectangle((int)screenPos.X, (int)screenPos.Y, (int)local_6, (int)local_6);

                    ScreenManager.SpriteBatch.Draw(uiNode, local_7, Color.White, 0.0f, uiNode.CenterF, SpriteEffects.None, 1f);
                }
        }



        private void DrawColoredEmpireBorders(SpriteBatch batch, GraphicsDevice graphics)
        {
            DrawBorders.Start();

            graphics.SetRenderTarget(0, BorderRT);
            graphics.Clear(Color.TransparentBlack);
            batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            graphics.RenderState.SeparateAlphaBlendEnabled = true;
            graphics.RenderState.AlphaBlendOperation = BlendFunction.Add;
            graphics.RenderState.AlphaSourceBlend = Blend.One;
            graphics.RenderState.AlphaDestinationBlend = Blend.One;
            graphics.RenderState.MultiSampleAntiAlias = true;

            var nodeCorrected = ResourceManager.Texture("UI/nodecorrected");
            var nodeConnect = ResourceManager.Texture("UI/nodeconnect");

            foreach (Empire empire in EmpireManager.Empires.Sorted(e=> e.MilitaryScore))
            {
                if (!Debug && empire != player && !player.IsKnown(empire))
                    continue;

                var empireColor = empire.EmpireColor;
                {
                    var nodes = empire.BorderNodes.AtomicCopy();
                    for (int x = 0; x < nodes.Length; x++)
                    {
                        Empire.InfluenceNode influ = nodes[x];
                        if (influ?.KnownToPlayer != true)
                            continue;
                        if (!Frustum.Contains(influ.Position, influ.Radius))
                            continue;
                 
                        Vector2 nodePos = ProjectToScreenPosition(influ.Position);
                        int size = (int) Math.Abs(
                            ProjectToScreenPosition(influ.Position.PointFromAngle(90f, influ.Radius)).X - nodePos.X);

                        Rectangle rect = new Rectangle((int) nodePos.X, (int) nodePos.Y, size * 5, size * 5);
                        batch.Draw(nodeCorrected, rect, empireColor, 0.0f, nodeCorrected.CenterF, SpriteEffects.None, 1f);

                        for (int i = 0; i < nodes.Length; i++)
                        {
                            Empire.InfluenceNode influ2 = nodes[i];
                            if (influ2?.KnownToPlayer != true)
                                continue;
                            if (influ.Position == influ2.Position || influ.Radius > influ2.Radius ||
                                influ.Position.OutsideRadius(influ2.Position, influ.Radius + influ2.Radius + 150000.0f))
                                continue;

                            Vector2 endPos = ProjectToScreenPosition(influ2.Position);
                            float rotation = nodePos.RadiansToTarget(endPos);
                            rect = new Rectangle((int) endPos.X, (int) endPos.Y, size * 3 / 2,
                                (int) nodePos.Distance(endPos));
                            batch.Draw(nodeConnect, rect, empireColor, rotation, new Vector2(2f, 2f), SpriteEffects.None, 1f);
                        }
                    }
                }
            }
            batch.End();

            DrawBorders.Stop();
        }

        void DrawDebugPlanetBudgets()
        {
            if (viewState < UnivScreenState.SectorView)
            {
                foreach (Empire empire in EmpireManager.Empires)
                {
                    if (empire.GetEmpireAI().PlanetBudgets != null)
                    {
                        foreach (var budget in empire.GetEmpireAI().PlanetBudgets)
                            budget.DrawBudgetInfo(this);
                    }
                }
            }
        }

        void DrawMain(SpriteBatch batch, DrawTimes elapsed)
        {
            DrawMain3D.Start();

            Render(batch, elapsed);

            batch.Begin(SpriteBlendMode.Additive);
            ExplosionManager.DrawExplosions(batch, View, Projection);
            #if DEBUG
            DrawDebugPlanetBudgets();
            #endif
            batch.End();

            if (ShowShipNames && !LookingAtPlanet)
            {
                foreach (ClickableShip clickable in ClickableShipsList)
                    if (clickable.shipToClick.InFrustum)
                        clickable.shipToClick.DrawShieldBubble(this);
            }

            DrawMain3D.Stop();
        }

        void DrawLights(SpriteBatch batch, GraphicsDevice device)
        {
            DrawFogInfluence.Start();

            device.SetRenderTarget(0, FogMapTarget);
            device.Clear(Color.TransparentWhite);
            batch.Begin(SpriteBlendMode.Additive);
            batch.Draw(FogMap, new Rectangle(0, 0, 512, 512), Color.White);
            float num = 512f / UniverseSize;
            var uiNode = ResourceManager.Texture("UI/node");
            foreach (Ship ship in player.GetShips())
            {
                if (ship != null && ScreenRectangle.HitTest(ship.ScreenPosition))
                {
                    Rectangle destinationRectangle = new Rectangle(
                        (int) (ship.Position.X * num),
                        (int) (ship.Position.Y * num),
                        (int) (ship.SensorRange * num * 2.0),
                        (int) (ship.SensorRange * num * 2.0));
                    ScreenManager.SpriteBatch.Draw(uiNode, destinationRectangle, new Color(255, 0, 0, 255), 0.0f,
                        uiNode.CenterF, SpriteEffects.None, 1f);
                }
            }
            batch.End();
            device.SetRenderTarget(0, null);
            FogMap = FogMapTarget.GetTexture();

            device.SetRenderTarget(0, LightsTarget);
            device.Clear(Color.White);

            batch.Begin(SpriteBlendMode.AlphaBlend);
            if (!Debug) // don't draw fog of war in debug
            {
                Rectangle fogRect = ProjectToScreenCoords(Vector2.Zero, UniverseSize);
                batch.FillRectangle(new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 170));
                batch.Draw(FogMap, fogRect, new Color(255, 255, 255, 55));
            }
            DrawFogNodes();
            DrawInfluenceNodes();
            batch.End();
            device.SetRenderTarget(0, null);

            DrawFogInfluence.Stop();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            DrawPerf.Start();

            PulseTimer -= elapsed.RealTime.Seconds;
            if (PulseTimer < 0) PulseTimer = 1;

            lock (GlobalStats.BeamEffectLocker)
            {
                Beam.BeamEffect.Parameters["View"].SetValue(View);
                Beam.BeamEffect.Parameters["Projection"].SetValue(Projection);
            }

            AdjustCamera(elapsed.RealTime.Seconds);
            CamPos.Z = CamHeight;
            View = Matrix.CreateTranslation(0.0f, 0.0f, 0.0f)
                   * Matrix.CreateRotationY(180f.ToRadians())
                   * Matrix.CreateRotationX(0.0f.ToRadians())
                   * Matrix.CreateLookAt(new Vector3(-CamPos.X, CamPos.Y, CamHeight),
                       new Vector3(-CamPos.X, CamPos.Y, 0.0f), new Vector3(0.0f, -1f, 0.0f));
            Matrix matrix = View;

            GraphicsDevice graphics = ScreenManager.GraphicsDevice;
            graphics.SetRenderTarget(0, MainTarget);
            DrawMain(batch, elapsed);
            graphics.SetRenderTarget(0, null);

            DrawLights(batch, graphics);

            if (viewState >= UnivScreenState.SectorView) // draw colored empire borders only if zoomed out
            {
                DrawColoredEmpireBorders(batch, graphics);
            }

            DrawFogOfWarEffect(batch, graphics);

            View = matrix;
            if (GlobalStats.RenderBloom)
            {
                bloomComponent?.Draw();
            }

            batch.Begin(SpriteBlendMode.AlphaBlend);
            RenderOverFog(batch);
            batch.End();

             // these are all background elements, such as ship overlays, fleet icons, etc..
            batch.Begin();
            {
                DrawShipsAndProjectiles(batch);
                DrawShipAndPlanetIcons(batch);
                DrawGeneralUI(batch, elapsed);
            }
            batch.End();

            // Advance the simulation time just before we Notify
            if (!Paused && IsActive)
            {
                AdvanceSimulationTargetTime(elapsed.RealTime.Seconds);
            }

            // Notify ProcessTurns that Drawing has finished and while SwapBuffers is blocking,
            // the game logic can be updated
            DrawCompletedEvt.Set();

            DrawPerf.Stop();
        }

        private void DrawGeneralUI(SpriteBatch batch, DrawTimes elapsed)
        {
            DrawUI.Start();

            // in cinematic mode we disable all of these GUI elements
            bool showGeneralUI = !IsCinematicModeEnabled;
            if (showGeneralUI)
            {
                DrawPlanetInfo();
                EmpireUI.Draw(batch);
                if (LookingAtPlanet)
                {
                    workersPanel?.Draw(batch, elapsed);
                }
                else
                {
                    DeepSpaceBuildWindow.Draw(batch, elapsed);
                    pieMenu.Draw(batch, Fonts.Arial12Bold);
                    DrawShipUI(batch, elapsed);
                    NotificationManager.Draw(batch);
                }
            }

            batch.DrawRectangle(SelectionBox, Color.Green, 1f);

            // This uses the new UIElementV2 system to automatically toggle visibility of items
            // In general, a much saner way than the old cluster-f*ck of IF statements :)
            PlanetsInCombat.Visible = ShipsInCombat.Visible = showGeneralUI && !LookingAtPlanet;
            aw.Visible = showGeneralUI && aw.IsOpen && !LookingAtPlanet;

            minimap.Visible = showGeneralUI && (!LookingAtPlanet ||
                              LookingAtPlanet && workersPanel is UnexploredPlanetScreen ||
                              LookingAtPlanet && workersPanel is UnownedPlanetScreen);

            DrawSelectedItems(batch, elapsed);

            if (SelectedShip == null || LookingAtPlanet)
                ShipInfoUIElement.ShipNameArea.HandlingInput = false;

            DrawToolTip();

            if (Debug)
                DebugWin?.Draw(batch, elapsed);

            DrawGeneralStatusText(batch, elapsed);

            if (Debug) ShowDebugGameInfo();
            else HideDebugGameInfo();

            base.Draw(batch, elapsed);  // UIElementV2 Draw

            DrawUI.Stop();
        }

        private void DrawFogOfWarEffect(SpriteBatch batch, GraphicsDevice graphics)
        {
            DrawFogOfWar.Start();

            Texture2D texture1 = MainTarget.GetTexture();
            Texture2D texture2 = LightsTarget.GetTexture();
            graphics.SetRenderTarget(0, null);
            graphics.Clear(Color.Black);
            basicFogOfWarEffect.Parameters["LightsTexture"].SetValue(texture2);


            batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState);
            basicFogOfWarEffect.Begin();
            basicFogOfWarEffect.CurrentTechnique.Passes[0].Begin();
            batch.Draw(texture1, new Rectangle(0, 0, graphics.PresentationParameters.BackBufferWidth,
                    graphics.PresentationParameters.BackBufferHeight), Color.White);
            basicFogOfWarEffect.CurrentTechnique.Passes[0].End();
            basicFogOfWarEffect.End();
            batch.End();

            DrawFogOfWar.Stop();
        }

        private void DrawShipAndPlanetIcons(SpriteBatch batch)
        {
            DrawIcons.Start();
            DrawProjectedGroup();
            if (!LookingAtPlanet)
                DeepSpaceBuildWindow.DrawBlendedBuildIcons();
            DrawTacticalPlanetIcons(batch);
            DrawFTLInhibitionNodes();
            DrawShipRangeOverlay();
            DrawFleetIcons();
            DrawIcons.Stop();
        }

        void DrawTopCenterStatusText(SpriteBatch batch, in LocalizedText status, Color color, int lineOffset)
        {
            SpriteFont font = Fonts.Pirulen16;
            string text = status.Text;
            var pos = new Vector2(ScreenCenter.X - font.TextWidth(text) / 2f, 45f + (font.LineSpacing + 2)*lineOffset);
            batch.DrawString(font, text, pos, color);
        }

        void DrawGeneralStatusText(SpriteBatch batch, DrawTimes elapsed)
        {
            if (Paused)
            {
                DrawTopCenterStatusText(batch, GameText.Paused, Color.Gold, 0);
            }

            if (RandomEventManager.ActiveEvent != null && RandomEventManager.ActiveEvent.InhibitWarp)
            {
                DrawTopCenterStatusText(batch, "Hyperspace Flux", Color.Yellow, 1);
            }

            if (IsActive && SavedGame.IsSaving)
            {
                DrawTopCenterStatusText(batch, "Saving...", CurrentFlashColor, 2);
            }

            if (IsActive && !GameSpeed.AlmostEqual(1)) //don't show "1.0x"
            {
                string speed = GameSpeed.ToString("0.0##") + "x";
                var pos = new Vector2(ScreenWidth - Fonts.Pirulen16.TextWidth(speed) - 13f, 64f);
                batch.DrawString(Fonts.Pirulen16, speed, pos, Color.White);
            }

            if (IsCinematicModeEnabled && CinematicModeTextTimer > 0f)
            {
                CinematicModeTextTimer -= elapsed.RealTime.Seconds;
                DrawTopCenterStatusText(batch, "Cinematic Mode - Press F11 to exit", Color.White, 3);
            }

            if (!EmpireManager.Player.Research.NoResearchLeft 
                && EmpireManager.Player.Research.NoTopic
                && !EmpireManager.Player.AutoResearch
                && !Empire.Universe.Debug)
            {
                DrawTopCenterStatusText(batch, "No Research!",  ApplyCurrentAlphaToColor(Color.Red), 2);
            }
        }

        void DrawShipRangeOverlay()
        {
            if (showingRangeOverlay && !LookingAtPlanet)
            {
                var shipRangeTex = ResourceManager.Texture("UI/node_shiprange");
                foreach (ClickableShip clickable in ClickableShipsList)
                {
                    Ship ship = clickable.shipToClick;
                    if (ship != null && ship.WeaponsMaxRange > 0f)
                    {
                        Color color = ship.loyalty == EmpireManager.Player
                                        ? new Color(0, 200, 0, 30)
                                        : new Color(200, 0, 0, 30);

                        byte edgeAlpha = 70;
                        DrawCircleProjected(ship.Position, ship.WeaponsMaxRange, new Color(color, edgeAlpha));
                        if (SelectedShip == ship)
                        {
                            edgeAlpha = 70;
                            DrawTextureProjected(shipRangeTex, ship.Position, ship.WeaponsMaxRange, color);
                            DrawCircleProjected(ship.Position, ship.WeaponsAvgRange, new Color(Color.Orange, edgeAlpha));
                            DrawCircleProjected(ship.Position, ship.WeaponsMinRange, new Color(Color.Yellow, edgeAlpha));
                        }
                    }

                    if ((ship?.SensorRange ?? 0) > 0)
                    {
                        if (Empire.Universe.SelectedShip == ship)
                        {
                            Color color = (ship.loyalty == EmpireManager.Player)
                                ? new Color(0, 100, 200, 20)
                                : new Color(200, 0, 0, 10);
                            byte edgeAlpha = 85;
                            DrawTextureProjected(shipRangeTex, ship.Position, ship.SensorRange, color);
                            DrawCircleProjected(ship.Position, ship.SensorRange, new Color(Color.Blue, edgeAlpha));
                        }
                    }
                }
            }
        }

        void DrawFTLInhibitionNodes()
        {
            if (showingFTLOverlay && GlobalStats.PlanetaryGravityWells && !LookingAtPlanet)
            {
                var inhibit = ResourceManager.Texture("UI/node_inhibit");
                lock (GlobalStats.ClickableSystemsLock)
                {
                    foreach (ClickablePlanets cplanet in ClickPlanetList)
                    {
                        float radius = cplanet.planetToClick.GravityWellRadius;
                        DrawCircleProjected(cplanet.planetToClick.Center, radius, new Color(255, 50, 0, 150), 1f,
                            inhibit, new Color(200, 0, 0, 50));
                    }
                }

                foreach (ClickableShip ship in ClickableShipsList)
                {
                    if (ship.shipToClick != null && ship.shipToClick.InhibitionRadius > 0f)
                    {
                        float radius = ship.shipToClick.InhibitionRadius;
                        DrawCircleProjected(ship.shipToClick.Position, radius, new Color(255, 50, 0, 150), 1f,
                            inhibit, new Color(200, 0, 0, 40));
                    }
                }

                if (viewState >= UnivScreenState.SectorView)
                {
                    foreach (Empire.InfluenceNode influ in player.BorderNodes.AtomicCopy())
                    {
                        DrawCircleProjected(influ.Position, influ.Radius, new Color(30, 30, 150, 150), 1f, inhibit,
                            new Color(0, 200, 0, 20));
                    }
                }
            }
        }

        bool ShowSystemInfo => SelectedSystem != null && !LookingAtPlanet && !IsCinematicModeEnabled
                                                      && viewState == UnivScreenState.GalaxyView;

        bool ShowPlanetInfo => SelectedPlanet != null && !LookingAtPlanet && !IsCinematicModeEnabled;

        bool ShowShipInfo => SelectedShip != null && !LookingAtPlanet && !IsCinematicModeEnabled;

        bool ShowShipList => SelectedShipList.Count > 1 && SelectedFleet == null && !IsCinematicModeEnabled;

        bool ShowFleetInfo => SelectedFleet != null && !LookingAtPlanet && !IsCinematicModeEnabled;

        private void DrawSelectedItems(SpriteBatch batch, DrawTimes elapsed)
        {
            if (SelectedShipList.Count == 0)
                shipListInfoUI.ClearShipList();

            if (ShowSystemInfo)
            {
                sInfoUI.Draw(batch, elapsed);
            }

            if (ShowPlanetInfo)
            {
                pInfoUI.Draw(batch, elapsed);
            }
            else if (ShowShipInfo)
            {
                if (Debug && DebugWin != null)
                {
                    DebugWin.DrawCircleImm(SelectedShip.Center,
                        SelectedShip.AI.GetSensorRadius(), Color.Crimson);
                    for (int i = 0; i < SelectedShip.AI.NearByShips.Count; i++)
                    {
                        var target = SelectedShip.AI.NearByShips[i];
                        DebugWin.DrawCircleImm(target.Ship.Center,
                            target.Ship.AI.GetSensorRadius(), Color.Crimson);
                    }
                }

                ShipInfoUIElement.Draw(batch, elapsed);
            }
            else if (ShowShipList)
            {
                shipListInfoUI.Draw(batch, elapsed);
            }
            else if (SelectedItem != null)
            {
                Goal goal = SelectedItem.AssociatedGoal;
                EmpireAI ai = goal.empire.GetEmpireAI();
                if (ai.HasGoal(goal.guid))
                {
                    string titleText = $"({ResourceManager.GetShipTemplate(SelectedItem.UID).Name})";
                    string bodyText = Localizer.Token(GameText.UnderConstructionAt) + goal.PlanetBuildingAt.Name;
                    vuiElement.Draw(titleText, bodyText);
                    DrawItemInfoForUI();
                }
                else
                    SelectedItem = null;
            }
            else if (ShowFleetInfo)
            {
                shipListInfoUI.Draw(batch, elapsed);
            }
        }


        void DrawFleetIcons()
        {
            ClickableFleetsList.Clear();
            if (viewState < UnivScreenState.SectorView)
                return;
            bool debug = Debug && SelectedShip == null;
            Empire empireLooking = Debug ? SelectedShip?.loyalty ?? player : player;
            for (int i = 0; i < EmpireManager.Empires.Count; i++)
            { 
                Empire empire = EmpireManager.Empires[i];
                bool doDraw = debug || !(player.DifficultyModifiers.HideTacticalData && empireLooking.IsEmpireAttackable(empire));
                if (!doDraw) 
                    continue;

                // not sure if this is the right way to do this but its hitting a crash here on collection change when the fleet loop is a foreach
                Fleet[] fleets = empire.GetFleetsDict().AtomicValuesArray();
                for (int j = 0; j < fleets.Length; j++)
                {
                    Fleet fleet = fleets[j];
                    if (fleet.Ships.Count <= 0)
                        continue;

                    Vector2 averagePos = fleet.CachedAveragePos;

                    var shipsVisible = fleet.Ships.Filter(s=> s?.KnownByEmpires.KnownBy(empireLooking) == true);

                    if (shipsVisible.Length < fleet.Ships.Count * 0.75f)
                        continue;

                    SubTexture icon = fleet.Icon;
                    Vector2 fleetCenterOnScreen = ProjectToScreenPosition(averagePos);

                    FleetIconLines(shipsVisible, fleetCenterOnScreen);

                    ClickableFleetsList.Add(new ClickableFleet
                    {
                        fleet       = fleet,
                        ScreenPos   = fleetCenterOnScreen,
                        ClickRadius = 15f
                    });
                    ScreenManager.SpriteBatch.Draw(icon, fleetCenterOnScreen, empire.EmpireColor, 0.0f, icon.CenterF, 0.35f, SpriteEffects.None, 1f);
                    if (!player.DifficultyModifiers.HideTacticalData || debug || fleet.Owner.isPlayer || fleet.Owner.IsAlliedWith(empireLooking))
                        ScreenManager.SpriteBatch.DrawDropShadowText(fleet.Name, fleetCenterOnScreen + FleetNameOffset, Fonts.Arial8Bold);
                }
            }
        }

        void FleetIconLines(Ship[] ships, Vector2 fleetCenterOnScreen)
        {
            for (int i = 0; i < ships.Length; i++)
            {
                Ship ship = ships[i];
                if (ship == null || !ship.Active)
                    continue;

                if (Debug || ship.loyalty.isPlayer || ship.loyalty.IsAlliedWith(player) || !player.DifficultyModifiers.HideTacticalData)
                {
                    Vector2 shipScreenPos = ProjectToScreenPosition(ship.Center);
                    ScreenManager.SpriteBatch.DrawLine(shipScreenPos, fleetCenterOnScreen, FleetLineColor);
                }
            }
        }

        void DrawTacticalPlanetIcons(SpriteBatch batch)
        {
            if (LookingAtPlanet || viewState <= UnivScreenState.SystemView
                ||viewState >= UnivScreenState.GalaxyView)
                return;

            foreach (SolarSystem solarSystem in SolarSystemList)
            {
                bool wellKnown = false;

                foreach (Empire e in solarSystem.OwnerList)
                {
                    EmpireManager.Player.GetRelations(e, out Relationship ssRel);
                    wellKnown = Debug || e.isPlayer || ssRel.Treaty_Alliance;
                    if (wellKnown) break;

                }
                if (!solarSystem.IsExploredBy(player))
                    continue;

                foreach (Planet planet in solarSystem.PlanetList)
                {
                    if (!wellKnown && planet.Owner != null)
                    {
                        Empire e = planet.Owner;
                        if (EmpireManager.Player.IsKnown(e))
                            wellKnown = true;
                    }

                    float fIconScale = 0.1875f * (0.7f + ((float) (Math.Log(planet.Scale)) / 2.75f));

                    Vector3 vector3 = Viewport.Project(new Vector3(planet.Center, 2500f), Projection, View, Matrix.Identity);
                    var position = new Vector2(vector3.X, vector3.Y);
                    SubTexture planetTex = planet.PlanetTexture;

                    if (planet.Owner != null && wellKnown)
                    {
                        batch.Draw(planetTex, position, Color.White,
                                   0.0f, planetTex.CenterF, fIconScale, SpriteEffects.None, 1f);
                        SubTexture flag = ResourceManager.Flag(planet.Owner);
                        batch.Draw(flag, position, planet.Owner.EmpireColor, 0.0f, flag.CenterF, 0.045f, SpriteEffects.None, 1f);
                    }
                    else
                    {
                        batch.Draw(planetTex, position, Color.White,
                                   0.0f, planetTex.CenterF, fIconScale, SpriteEffects.None, 1f);
                    }
                }
            }
        }

        void DrawItemInfoForUI()
        {
            var goal = SelectedItem?.AssociatedGoal;
            if (goal == null) return;
            if (!LookingAtPlanet)
                DrawCircleProjected(goal.BuildPosition, 50f, goal.empire.EmpireColor);
        }

        void DrawShipUI(SpriteBatch batch, DrawTimes elapsed)
        {
            if (DefiningAO || DefiningTradeRoutes)
                return; // FB dont show fleet list when selected AOs and Trade Routes

            lock (GlobalStats.FleetButtonLocker)
            {
                foreach (FleetButton fleetButton in FleetButtons)
                {
                    var buttonSelector = new Selector(fleetButton.ClickRect, Color.TransparentBlack);
                    var housing = new Rectangle(fleetButton.ClickRect.X + 6, fleetButton.ClickRect.Y + 6,
                        fleetButton.ClickRect.Width - 12, fleetButton.ClickRect.Width - 12);

                    bool inCombat = false;
                    for (int ship = 0; ship < fleetButton.Fleet.Ships.Count; ++ship)
                    {
                        try
                        {
                            if (!fleetButton.Fleet.Ships[ship].InCombat) continue;
                            inCombat = true;
                            break;
                        }
                        catch { }
                    }

                    Color fleetKey       = Color.Orange;
                    SpriteFont fleetFont = Fonts.Pirulen12;
                    bool needShadow      = false;
                    Vector2 keyPos       = new Vector2(fleetButton.ClickRect.X + 4, fleetButton.ClickRect.Y + 4);
                    if (SelectedFleet == fleetButton.Fleet)
                    {
                        fleetKey   = Color.White;
                        fleetFont  = Fonts.Pirulen16;
                        needShadow = true;
                        keyPos     = new Vector2(keyPos.X, keyPos.Y - 2);
                    }

                    batch.Draw(ResourceManager.Texture("NewUI/rounded_square"),
                        fleetButton.ClickRect, inCombat ? ApplyCurrentAlphaToColor(new Color(255, 0, 0))
                                                        : new Color( 0,  0,  0,  80));

                    if (fleetButton.Fleet.AutoRequisition)
                    {
                        Rectangle autoReq = new Rectangle(fleetButton.ClickRect.X - 18, fleetButton.ClickRect.Y + 5, 15, 20);
                        batch.Draw(ResourceManager.Texture("NewUI/AutoRequisition"), autoReq, EmpireManager.Player.EmpireColor);
                    }

                    buttonSelector.Draw(batch, elapsed);
                    batch.Draw(fleetButton.Fleet.Icon, housing, EmpireManager.Player.EmpireColor);
                    if (needShadow)
                        batch.DrawString(fleetFont, fleetButton.Key.ToString(), new Vector2(keyPos.X + 2, keyPos.Y + 2), Color.Black);

                    batch.DrawString(fleetFont, fleetButton.Key.ToString(), keyPos, fleetKey);
                    DrawFleetShipIcons(batch, fleetButton);
                }
            }
        }

        void DrawFleetShipIcons(SpriteBatch batch, FleetButton fleetButton)
        {
            int x = fleetButton.ClickRect.X + 55; // Offset from the button
            int y = fleetButton.ClickRect.Y;

            if (fleetButton.Fleet.Ships.Count <= 30)
                DrawFleetShipIcons30(batch, fleetButton, x, y);
            else 
                DrawFleetShipIconsSums(batch, fleetButton, x, y);
        }

        void DrawFleetShipIcons30(SpriteBatch batch, FleetButton fleetButton, int x, int y)
        {
            // Draw ship icons to right of button
            Vector2 shipSpacingH = new Vector2(x, y);
            for (int i = 0; i < fleetButton.Fleet.Ships.Count; ++i)
            {
                Ship ship       = fleetButton.Fleet.Ships[i];
                var iconHousing = new Rectangle((int)shipSpacingH.X, (int)shipSpacingH.Y, 15, 15);
                shipSpacingH.X += 18f;
                if (shipSpacingH.X > 237) // 10 Ships per row
                {
                    shipSpacingH.X  = x;
                    shipSpacingH.Y += 18f;
                }

                SubTexture icon = ship.GetTacticalIcon(out SubTexture secondary, out Color statColor);
                if (statColor != Color.Black)
                    batch.Draw(ResourceManager.Texture("TacticalIcons/symbol_status"), iconHousing, ApplyCurrentAlphaToColor(statColor));

                batch.Draw(icon, iconHousing, fleetButton.Fleet.Owner.EmpireColor);
                if (secondary != null)
                    batch.Draw(secondary, iconHousing, fleetButton.Fleet.Owner.EmpireColor);
            }
        }

        void DrawFleetShipIconsSums(SpriteBatch batch, FleetButton fleetButton, int x, int y)
        {
            Color color  = fleetButton.Fleet.Owner.EmpireColor;
            var sums = new Map<string, int>();
            for (int i = 0; i < fleetButton.Fleet.Ships.Count; ++i)
            {
                Ship ship   = fleetButton.Fleet.Ships[i];
                string icon = GetFullTacticalIconPaths(ship);
                if (sums.TryGetValue(icon, out int value))
                    sums[icon] = value + 1;
                else
                    sums.Add(icon, 1);
            }

            Vector2 shipSpacingH = new Vector2(x, y);
            int roleCounter = 1;
            Color sumColor = Color.Goldenrod;
            if (sums.Count > 12) // Switch to default sum views if too many icon sums
            {
                sums = RecalculateExcessIcons(sums);
                sumColor = Color.Gold;
            }

            foreach (string iconPaths in sums.Keys.ToArray())
            {
                var iconHousing = new Rectangle((int)shipSpacingH.X, (int)shipSpacingH.Y, 15, 15);
                string space = sums[iconPaths] < 9 ? "  " : "";
                string sum = $"{space}{sums[iconPaths]}x";
                batch.DrawString(Fonts.Arial10, sum, iconHousing.X, iconHousing.Y, sumColor);
                float ident = Fonts.Arial10.MeasureString(sum).X;
                shipSpacingH.X += ident;
                iconHousing.X  += (int)ident;
                DrawIconSums(iconPaths, iconHousing);
                shipSpacingH.X += 25f;
                if (roleCounter % 4 == 0) // 4 roles per line
                {
                    shipSpacingH.X  = x;
                    shipSpacingH.Y += 15f;
                }

                roleCounter += 1;
            }

            // Ignore secondary icons and returns only the hull role icons
            Map<string, int> RecalculateExcessIcons(Map<string, int> excessSums)
            {
                Map<string, int> recalculated = new Map<string, int>();
                foreach (string iconPaths in excessSums.Keys.ToArray())
                {
                    var hullPath = iconPaths.Split('|')[0];
                    if (recalculated.TryGetValue(hullPath, out _))
                        recalculated[hullPath] += excessSums[iconPaths];
                    else
                        recalculated.Add(hullPath, excessSums[iconPaths]);
                }

                return recalculated;
            }

            string GetFullTacticalIconPaths(Ship s)
            {
                string icon = $"TacticalIcons/{s.GetTacticalIcon(out SubTexture secondary, out _).Name}";
                if (secondary != null)
                    icon = $"{icon}|TacticalIcons/{secondary.Name}";

                return icon;
            }

            void DrawIconSums(string iconPaths, Rectangle r)
            {
                var paths = iconPaths.Split('|');
                batch.Draw(ResourceManager.Texture(paths[0]), r, color);
                if (paths.Length > 1)
                    batch.Draw(ResourceManager.Texture(paths[1]), r, color);
            }
        }

        void DrawShipsAndProjectiles(SpriteBatch batch)
        {
            Device.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            Device.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            var rs = Device.RenderState;
            rs.AlphaBlendEnable = true;
            rs.AlphaBlendOperation = BlendFunction.Add;
            rs.SourceBlend = Blend.SourceAlpha;
            rs.DestinationBlend = Blend.One;
            rs.DepthBufferWriteEnable = false;
            rs.CullMode = CullMode.None;

            Ship[] ships = Objects.VisibleShips;

            if (viewState <= UnivScreenState.PlanetView)
            {
                DrawProj.Start();

                Projectile[] projectiles = Objects.VisibleProjectiles;
                Beam[] beams = Objects.VisibleBeams;
                for (int i = 0; i < projectiles.Length; ++i)
                {
                    Projectile proj = projectiles[i];
                    proj.Draw(batch, this);
                }
                for (int i = 0; i < beams.Length; ++i)
                {
                    Beam beam = beams[i];
                    beam.Draw(this);
                }

                DrawProj.Stop();
            }

            Device.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            Device.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            rs.AlphaBlendEnable       = true;
            rs.AlphaBlendOperation    = BlendFunction.Add;
            rs.SourceBlend            = Blend.SourceAlpha;
            rs.DestinationBlend       = Blend.InverseSourceAlpha;
            rs.DepthBufferWriteEnable = false;
            rs.CullMode               = CullMode.None;

            DrawShips.Start();
            for (int i = 0; i < ships.Length; ++i)
            {
                Ship ship = ships[i];
                if (ship.inSensorRange)
                {
                    if (!IsCinematicModeEnabled)
                        DrawTacticalIcon(ship);
                    DrawOverlay(ship);

                    if (SelectedShip == ship || SelectedShipList.Contains(ship))
                    {
                        Color color = Color.LightGreen;
                        if (player != ship.loyalty)
                            color = player.IsEmpireAttackable(ship.loyalty) ? Color.Red : Color.Gray;
                        batch.BracketRectangle(ship.ScreenPosition, ship.ScreenRadius, color);
                    }
                }
            }
            DrawShips.Stop();
        }

        void DrawProjectedGroup()
        {
            if (!Project.Started || CurrentGroup == null)
                return;

            var projectedColor = new Color(0, 255, 0, 100);
            foreach (Ship ship in CurrentGroup.Ships)
            {
                if (ship.Active)
                    DrawShipProjectionIcon(ship, ship.projectedPosition, CurrentGroup.ProjectedDirection, projectedColor);
            }
        }

        void DrawShipProjectionIcon(Ship ship, Vector2 position, Vector2 direction, Color color)
        {
            SubTexture symbol = ship.GetTacticalIcon(out SubTexture secondary, out _);
            float num         = ship.SurfaceArea / (30f + symbol.Width);
            float scale       = (num * 4000f / CamHeight).UpperBound(1);

            if (scale <= 0.1f)
                scale = ship.shipData.Role != ShipData.RoleName.platform || viewState < UnivScreenState.SectorView ? 0.15f : 0.08f;

            DrawTextureProjected(symbol, position, scale, direction.ToRadians(), color);
            if (secondary != null)
                DrawTextureProjected(secondary, position, scale, direction.ToRadians(), color);
        }

        void DrawOverlay(Ship ship)
        {
            if (ship.InFrustum && !ship.dying && !LookingAtPlanet && viewState <= UnivScreenState.DetailView)
            {
                // if we check for a missing model here we can show the ship modules instead. 
                // that will solve invisible ships when the ship model load hits an OOM.
                if (ShowShipNames || ship.GetSO()?.HasMeshes == false)
                {
                    ship.DrawModulesOverlay(this, CamHeight,
                        showDebugSelect:Debug && ship == SelectedShip,
                        showDebugStats: Debug && DebugWin?.IsOpen == true);
                }
            }
        }

        void DrawTacticalIcon(Ship ship)
        {
            if (!LookingAtPlanet && (!ship.IsPlatform ||
                                     ((showingFTLOverlay || viewState != UnivScreenState.GalaxyView) &&
                                      (!showingFTLOverlay || ship.IsSubspaceProjector))))
            {
                ship.DrawTacticalIcon(this, viewState);
            }
        }

        void DrawBombs()
        {
            using (BombList.AcquireReadLock())
            {
                for (int i = 0; i < BombList.Count; i++)
                {
                    Bomb bomb = BombList[i];
                    DrawTransparentModel(bomb.Model, bomb.World, bomb.Texture, 0.5f);
                }
            }
        }

        void DrawShipGoalsAndWayPoints(Ship ship, byte alpha)
        {
            if (ship == null)
                return;
            Vector2 start = ship.Center;

            if (!ship.InCombat || ship.AI.HasPriorityOrder)
            {
                Color color = Colors.Orders(alpha);
                if (ship.AI.State == AIState.Ferrying)
                {
                    DrawLineProjected(start, ship.AI.EscortTarget.Center, color);
                    return;
                }
                if (ship.AI.State == AIState.ReturnToHangar)
                {
                    if (ship.IsHangarShip)
                        DrawLineProjected(start, ship.Mothership.Center, color);
                    else
                        ship.AI.State = AIState.AwaitingOrders; //@todo this looks like bug fix hack. investigate and fix.
                    return;
                }
                if (ship.AI.State == AIState.Escort && ship.AI.EscortTarget != null)
                {
                    DrawLineProjected(start, ship.AI.EscortTarget.Center, color);
                    return;
                }

                if (ship.AI.State == AIState.Explore && ship.AI.ExplorationTarget != null)
                {
                    DrawLineProjected(start, ship.AI.ExplorationTarget.Position, color);
                    return;
                }

                if (ship.AI.State == AIState.Colonize && ship.AI.ColonizeTarget != null)
                {
                    Vector2 screenPos = DrawLineProjected(start, ship.AI.ColonizeTarget.Center, color, 2500f, 0);
                    string text = $"Colonize\nSystem : {ship.AI.ColonizeTarget.ParentSystem.Name}\nPlanet : {ship.AI.ColonizeTarget.Name}";
                    DrawPointerWithText(screenPos, ResourceManager.Texture("UI/planetNamePointer"), color, text, new Color(ship.loyalty.EmpireColor, alpha));
                    return;
                }
                if (ship.AI.State == AIState.Orbit && ship.AI.OrbitTarget != null)
                {
                    DrawLineProjected(start, ship.AI.OrbitTarget.Center, color, 2500f , 0);
                    return;
                }
                if (ship.AI.State == AIState.Rebase)
                {
                    DrawWayPointLines(ship, color);
                    return;
                }
                ShipAI.ShipGoal goal = ship.AI.OrderQueue.PeekFirst;
                if (ship.AI.State == AIState.Bombard && goal?.TargetPlanet != null)
                {
                    DrawLineProjected(ship.Center, goal.TargetPlanet.Center, Colors.CombatOrders(alpha), 2500f);
                    DrawWayPointLines(ship, Colors.CombatOrders(alpha));
                }
            }
            if (!ship.AI.HasPriorityOrder &&
                (ship.AI.State == AIState.AttackTarget || ship.AI.State == AIState.Combat) && ship.AI.Target is Ship)
            {
                DrawLineProjected(ship.Center, ship.AI.Target.Center, Colors.Attack(alpha));
                if (ship.AI.TargetQueue.Count > 1)
                {
                    for (int i = 0; i < ship.AI.TargetQueue.Count - 1; ++i)
                    {
                        var target = ship.AI.TargetQueue[i];
                        if (target == null || !target.Active)
                            continue;
                        DrawLineProjected(target.Center, ship.AI.TargetQueue[i].Center,
                            Colors.Attack((byte) (alpha * .5f)));
                    }
                }
                return;
            }
            if (ship.AI.State == AIState.Boarding && ship.AI.EscortTarget != null)
            {
                DrawLineProjected(start, ship.AI.EscortTarget.Center, Colors.CombatOrders(alpha));
                return;
            }
            if (ship.AI.State == AIState.AssaultPlanet && ship.AI.OrbitTarget != null)
            {
                var planet = ship.AI.OrbitTarget;
                int spots = planet.GetFreeTiles(EmpireManager.Player);
                if (spots > 4)
                    DrawLineToPlanet(start, ship.AI.OrbitTarget.Center, Colors.CombatOrders(alpha));
                else if (spots > 0)
                {
                    DrawLineToPlanet(start, ship.AI.OrbitTarget.Center, Colors.Warning(alpha));
                    ToolTip.PlanetLandingSpotsTip($"{planet.Name}: Warning!", spots);
                }
                else
                {
                    DrawLineToPlanet(start, ship.AI.OrbitTarget.Center, Colors.Error(alpha));
                    ToolTip.PlanetLandingSpotsTip($"{planet.Name}: Critical!", spots);
                }
                DrawWayPointLines(ship, new Color(Color.Lime, alpha));
                return;
            }
            if (ship.AI.State == AIState.SystemTrader  && ship.AI.OrderQueue.TryPeekLast(out ShipAI.ShipGoal g))
            {
                Planet importPlanet = g.Trade.ImportTo;
                Planet exportPlanet = g.Trade.ExportFrom;

                if (g.Plan == ShipAI.Plan.PickupGoods)
                {
                    DrawLineToPlanet(start, exportPlanet.Center, Color.Blue);
                    DrawLineToPlanet(exportPlanet.Center, importPlanet.Center, Color.Gold);
                }
                else
                    DrawLineToPlanet(start, importPlanet.Center, Color.Gold);
            }

            DrawWayPointLines(ship, Colors.WayPoints(alpha));
        }

        void DrawWayPointLines(Ship ship, Color color)
        {
            if (!ship.AI.HasWayPoints)
                return;

            WayPoint[] wayPoints = ship.AI.CopyWayPoints();

            DrawLineProjected(ship.Center, wayPoints[0].Position, color);

            for (int i = 1; i < wayPoints.Length; ++i)
            {
                DrawLineProjected(wayPoints[i-1].Position, wayPoints[i].Position, color);
            }

            // Draw tactical icons after way point lines (looks better this way)
            var tactical = new Color(color, (byte)(color.A + 70));

            WayPoint wp = wayPoints[0];
            DrawShipProjectionIcon(ship, wp.Position, wp.Direction, tactical);
            for (int i = 1; i < wayPoints.Length; ++i)
            {
                wp = wayPoints[i];
                DrawShipProjectionIcon(ship, wp.Position, wp.Direction, tactical);
            }
        }

        private void DrawShields()
        {
            DrawShieldsPerf.Start();
            if (viewState < UnivScreenState.SectorView)
            {
                var renderState = ScreenManager.GraphicsDevice.RenderState;
                renderState.AlphaBlendEnable = true;
                renderState.AlphaBlendOperation = BlendFunction.Add;
                renderState.SourceBlend = Blend.SourceAlpha;
                renderState.DestinationBlend = Blend.One;
                renderState.DepthBufferWriteEnable = false;
                ShieldManager.Draw(View, Projection);
            }
            DrawShieldsPerf.Stop();
        }

        private void DrawPlanetInfo()
        {
            if (LookingAtPlanet || viewState > UnivScreenState.SectorView || viewState < UnivScreenState.ShipView)
                return;
            Vector2 mousePos = Input.CursorPosition;
            SubTexture planetNamePointer = ResourceManager.Texture("UI/planetNamePointer");
            SubTexture icon_fighting_small = ResourceManager.Texture("UI/icon_fighting_small");
            SubTexture icon_spy_small = ResourceManager.Texture("UI/icon_spy_small");
            SubTexture icon_anomaly_small = ResourceManager.Texture("UI/icon_anomaly_small");
            SubTexture icon_troop = ResourceManager.Texture("UI/icon_troop");
            for (int k = 0; k < SolarSystemList.Count; k++)
            {
                SolarSystem solarSystem = SolarSystemList[k];
                if (!solarSystem.isVisible)
                    continue;

                bool wellKnown = false;

                foreach (Empire e in solarSystem.OwnerList)
                {
                    wellKnown = Debug || EmpireManager.Player == e || EmpireManager.Player.IsAlliedWith(e);
                    if (wellKnown) break;
                }

                for (int j = 0; j < solarSystem.PlanetList.Count; j++)
                {
                    Planet planet = solarSystem.PlanetList[j];
                    if (!planet.IsExploredBy(player) && !wellKnown)
                        continue;

                    Vector2 screenPosPlanet = ProjectToScreenPosition(planet.Center, 2500f);
                    Vector2 posOffSet = screenPosPlanet;
                    posOffSet.X += 20f;
                    posOffSet.Y += 37f;
                    int drawLocationOffset = 0;
                    Color textColor = wellKnown ? planet.Owner?.EmpireColor ?? Color.White : Color.White;

                    DrawPointerWithText(screenPosPlanet, planetNamePointer, Color.Green, planet.Name, textColor);

                    posOffSet = new Vector2(screenPosPlanet.X + 10f, screenPosPlanet.Y + 60f);

                    if (planet.RecentCombat)
                    {
                        DrawTextureWithToolTip(icon_fighting_small, Color.White, GameText.IndicatesThatAnAnomalyWas, mousePos, (int)posOffSet.X,
                            (int)posOffSet.Y, 14, 14);
                        ++drawLocationOffset;
                    }
                    if (player.data.MoleList.Count > 0)
                    {
                        for (int i = 0; i < player.data.MoleList.Count; i++)
                        {
                            Mole mole = player.data.MoleList[i];
                            if (mole.PlanetGuid == planet.guid)
                            {
                                posOffSet.X += (18 * drawLocationOffset);
                                DrawTextureWithToolTip(icon_spy_small, Color.White, GameText.IndicatesThatAFriendlyAgent, mousePos,
                                    (int)posOffSet.X, (int)posOffSet.Y, 14, 14);
                                ++drawLocationOffset;
                                break;
                            }
                        }
                    }
                    for (int i = 0; i < planet.BuildingList.Count; i++)
                    {
                        Building building = planet.BuildingList[i];
                        if (!building.EventHere) continue;
                        posOffSet.X += (18 * drawLocationOffset);
                        string text = Localizer.Token(building.DescriptionIndex);
                        DrawTextureWithToolTip(icon_anomaly_small, Color.White, text, mousePos, (int)posOffSet.X,
                            (int)posOffSet.Y, 14, 14);
                        break;
                    }
                    int troopCount = planet.CountEmpireTroops(player);
                    if (troopCount > 0)
                    {
                        posOffSet.X += (18 * drawLocationOffset);
                        DrawTextureWithToolTip(icon_troop, Color.TransparentWhite, $"Troops {troopCount}", mousePos,
                            (int)posOffSet.X, (int)posOffSet.Y, 14, 14);
                        ++drawLocationOffset;
                    }
                }
            }
        }

        public void DrawPointerWithText(Vector2 screenPos, SubTexture planetNamePointer, Color pointerColor, string text,
            Color textColor, SpriteFont font = null, float xOffSet = 20f, float yOffSet = 37f)
        {
            font = font ?? Fonts.Tahoma10;
            DrawTextureRect(planetNamePointer, screenPos, pointerColor);
            Vector2 posOffSet = screenPos;
            posOffSet.X += xOffSet;
            posOffSet.Y += yOffSet;
            HelperFunctions.ClampVectorToInt(ref posOffSet);
            ScreenManager.SpriteBatch.DrawString(font, text, posOffSet, textColor);
        }

    }
}