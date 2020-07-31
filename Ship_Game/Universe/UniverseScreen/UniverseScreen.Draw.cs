using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using Ship_Game.Ships.AI;
using SynapseGaming.LightingSystem.Lights;
using Ship_Game.Fleets;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        private void DrawRings(in Matrix world, float scale)
        {
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressU          = TextureAddressMode.Wrap;
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressV          = TextureAddressMode.Wrap;
            ScreenManager.GraphicsDevice.RenderState.AlphaBlendEnable       = true;
            ScreenManager.GraphicsDevice.RenderState.AlphaBlendOperation    = BlendFunction.Add;
            ScreenManager.GraphicsDevice.RenderState.SourceBlend            = Blend.SourceAlpha;
            ScreenManager.GraphicsDevice.RenderState.DestinationBlend       = Blend.InverseSourceAlpha;
            ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = false;
            ScreenManager.GraphicsDevice.RenderState.CullMode               = CullMode.None;
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
            ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
        }

        private void DrawAtmo(Model model, in Matrix world, Planet planet)
        {
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            var renderState = ScreenManager.GraphicsDevice.RenderState;
            renderState.AlphaBlendEnable = true;
            renderState.AlphaBlendOperation = BlendFunction.Add;
            renderState.SourceBlend = Blend.SourceAlpha;
            renderState.DestinationBlend = Blend.InverseSourceAlpha;
            renderState.DepthBufferWriteEnable = false;
            renderState.CullMode = CullMode.CullClockwiseFace;

            ModelMesh modelMesh = model.Meshes[0];
            SubTexture atmosphere = ResourceManager.Texture("Atmos");

            foreach (BasicEffect basicEffect in modelMesh.Effects)
            {
                basicEffect.World = Matrix.CreateScale(4.1f) * world;
                basicEffect.View = View;
                // @todo 3D Texture Atlas support?
                basicEffect.Texture = atmosphere.Texture;
                basicEffect.TextureEnabled = true;
                basicEffect.Projection = Projection;
                basicEffect.LightingEnabled = true;
                basicEffect.DirectionalLight0.DiffuseColor = new Vector3(1f, 1f, 1f);
                basicEffect.DirectionalLight0.Enabled = true;
                basicEffect.DirectionalLight0.SpecularColor = new Vector3(1f, 1f, 1f);
                basicEffect.DirectionalLight0.Direction = new Vector3(0.98f, -0.025f, 0.2f);
                basicEffect.DirectionalLight1.DiffuseColor = new Vector3(1f, 1f, 1f);
                basicEffect.DirectionalLight1.Enabled = true;
                basicEffect.DirectionalLight1.SpecularColor = new Vector3(1f, 1f, 1f);
                basicEffect.DirectionalLight1.Direction = new Vector3(0.98f, -0.025f, 0.2f);
            }
            modelMesh.Draw();
            DrawAtmo1(world);
            renderState.DepthBufferWriteEnable = true;
            renderState.CullMode = CullMode.CullCounterClockwiseFace;
            renderState.AlphaBlendEnable = false;
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

        private void DrawClouds(Model model, in Matrix world, Planet p)
        {
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            var renderState                                             = ScreenManager.GraphicsDevice.RenderState;
            renderState.AlphaBlendEnable                                = true;
            renderState.AlphaBlendOperation                             = BlendFunction.Add;
            renderState.SourceBlend                                     = Blend.SourceAlpha;
            renderState.DestinationBlend                                = Blend.InverseSourceAlpha;
            renderState.DepthBufferWriteEnable                          = false;
            ModelMesh modelMesh                                         = model.Meshes[0];
            foreach (BasicEffect basicEffect in modelMesh.Effects)
            {
                basicEffect.World                           = Matrix.CreateScale(4.05f) * world;
                basicEffect.View                            = View;
                basicEffect.Texture                         = cloudTex;
                basicEffect.TextureEnabled                  = true;
                basicEffect.Projection                      = Projection;
                basicEffect.LightingEnabled                 = true;
                basicEffect.DirectionalLight0.DiffuseColor  = new Vector3(1f, 1f, 1f);
                basicEffect.DirectionalLight0.SpecularColor = new Vector3(1f, 1f, 1f);
                basicEffect.SpecularPower                   = 4;
                if (UseRealLights)
                {
                    Vector2 sunToPlanet = p.Center - p.ParentSystem.Position;
                    basicEffect.DirectionalLight0.Direction = sunToPlanet.ToVec3().Normalized();
                }
                else
                {
                    Vector2 universeCenterToPlanet = p.Center - new Vector2(0, 0);
                    basicEffect.DirectionalLight0.Direction = universeCenterToPlanet.ToVec3().Normalized();
                }
                basicEffect.DirectionalLight0.Enabled = true;
            }
            modelMesh.Draw();
            renderState.DepthBufferWriteEnable = true;
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
            var uiNode = ResourceManager.Texture("UI/node");
            var viewport = Viewport;
            using (player.SensorNodes.AcquireReadLock())
                foreach (Empire.InfluenceNode influ in player.SensorNodes)
                {
                    //Vector3 local_1 = viewport.Project(influ.Position.ToVec3(), this.projection, this.view,
                    //    Matrix.Identity);
                    //Vector2 unProject = ProjectToScreenPosition(influ.Position);
                    Vector2 screenPos = ProjectToScreenPosition(influ.Position);  //local_1.ToVec2();
                    Vector3 local_4 = viewport.Project(
                        new Vector3(influ.Position.PointFromAngle(90f, influ.Radius * 1.5f), 0.0f), Projection,
                        View, Matrix.Identity);

                    float local_6 = Math.Abs(new Vector2(local_4.X, local_4.Y).X - screenPos.X) * 2.59999990463257f;
                    Rectangle local_7 = new Rectangle((int)screenPos.X, (int)screenPos.Y, (int)local_6, (int)local_6);

                    ScreenManager.SpriteBatch.Draw(uiNode, local_7, Color.White, 0.0f, uiNode.CenterF,
                        SpriteEffects.None, 1f);
                }
        }



        private void DrawColoredEmpireBorders()
        {
            var spriteBatch = ScreenManager.SpriteBatch;
            var graphics = ScreenManager.GraphicsDevice;
            ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            graphics.RenderState.SeparateAlphaBlendEnabled = true;
            graphics.RenderState.AlphaBlendOperation = BlendFunction.Add;
            graphics.RenderState.AlphaSourceBlend = Blend.One;
            graphics.RenderState.AlphaDestinationBlend = Blend.One;
            graphics.RenderState.MultiSampleAntiAlias = true;

            var nodeCorrected = ResourceManager.Texture("UI/nodecorrected");
            var nodeConnect = ResourceManager.Texture("UI/nodeconnect");

            foreach (Empire empire in EmpireManager.Empires)
            {
                if (!Debug && empire != player && !player.GetRelations(empire).Known)
                    continue;

                var empireColor = empire.EmpireColor;
                using (empire.BorderNodes.AcquireReadLock())
                {
                    for (int x = 0; x < empire.BorderNodes.Count; x++)
                    {
                        Empire.InfluenceNode influ = empire.BorderNodes[x];
                        if (!Frustum.Contains(influ.Position, influ.Radius))
                            continue;
                        if (!influ.Known)
                            continue;
                        Vector2 nodePos = ProjectToScreenPosition(influ.Position);
                        int size = (int) Math.Abs(
                            ProjectToScreenPosition(influ.Position.PointFromAngle(90f, influ.Radius)).X - nodePos.X);

                        Rectangle rect = new Rectangle((int) nodePos.X, (int) nodePos.Y, size * 5, size * 5);
                        spriteBatch.Draw(nodeCorrected, rect, empireColor, 0.0f, nodeCorrected.CenterF,
                            SpriteEffects.None, 1f);

                        for (int i = 0; i < empire.BorderNodes.Count; i++)
                        {
                            Empire.InfluenceNode influ2 = empire.BorderNodes[i];
                            if (!influ2.Known)
                                continue;
                            if (influ.Position == influ2.Position || influ.Radius > influ2.Radius ||
                                influ.Position.OutsideRadius(influ2.Position, influ.Radius + influ2.Radius + 150000.0f))
                                continue;

                            Vector2 endPos = ProjectToScreenPosition(influ2.Position);
                            float rotation = nodePos.RadiansToTarget(endPos);
                            rect = new Rectangle((int) endPos.X, (int) endPos.Y, size * 3 / 2,
                                (int) nodePos.Distance(endPos));
                            spriteBatch.Draw(nodeConnect, rect, empireColor, rotation, new Vector2(2f, 2f),
                                SpriteEffects.None, 1f);
                        }
                    }
                }
            }
            spriteBatch.End();
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

        void DrawMain(SpriteBatch batch, GameTime gameTime)
        {
            Render(batch, gameTime);

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
        }

        void DrawLights(SpriteBatch batch)
        {
            var device = ScreenManager.GraphicsDevice;
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
            ScreenManager.GraphicsDevice.SetRenderTarget(0, null);
        }

        public override void Draw(SpriteBatch batch)
        {
            GameTime gameTime = StarDriveGame.Instance.GameTime;

            // Wait for ProcessTurns to finish before we start drawing
            if (ProcessTurnsThread != null && ProcessTurnsThread.IsAlive) // check if thread is alive to avoid deadlock
                if (!ProcessTurnsCompletedEvt.WaitOne(100))
                    Log.Warning("Universe ProcessTurns Wait timed out: ProcessTurns was taking too long!");

            lock (GlobalStats.BeamEffectLocker)
            {
                Beam.BeamEffect.Parameters["View"].SetValue(View);
                Beam.BeamEffect.Parameters["Projection"].SetValue(Projection);
            }

            AdjustCamera((float) gameTime.ElapsedGameTime.TotalSeconds);
            CamPos.Z = CamHeight;
            View = Matrix.CreateTranslation(0.0f, 0.0f, 0.0f)
                   * Matrix.CreateRotationY(180f.ToRadians())
                   * Matrix.CreateRotationX(0.0f.ToRadians())
                   * Matrix.CreateLookAt(new Vector3(-CamPos.X, CamPos.Y, CamHeight),
                       new Vector3(-CamPos.X, CamPos.Y, 0.0f), new Vector3(0.0f, -1f, 0.0f));
            Matrix matrix = View;

            var graphics = ScreenManager.GraphicsDevice;
            graphics.SetRenderTarget(0, MainTarget);
            DrawMain(batch, gameTime);
            graphics.SetRenderTarget(0, null);
            DrawLights(batch);

            if (viewState >= UnivScreenState.SectorView) // draw colored empire borders only if zoomed out
            {
                graphics.SetRenderTarget(0, BorderRT);
                graphics.Clear(Color.TransparentBlack);
                DrawColoredEmpireBorders();
            }

            graphics.SetRenderTarget(0, null);
            Texture2D texture1 = MainTarget.GetTexture();
            Texture2D texture2 = LightsTarget.GetTexture();
            graphics.SetRenderTarget(0, null);
            graphics.Clear(Color.Black);
            basicFogOfWarEffect.Parameters["LightsTexture"].SetValue(texture2);

            batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate,
                SaveStateMode.SaveState);
            basicFogOfWarEffect.Begin();
            basicFogOfWarEffect.CurrentTechnique.Passes[0].Begin();
            batch.Draw(texture1,
                new Rectangle(0, 0, graphics.PresentationParameters.BackBufferWidth,
                    graphics.PresentationParameters.BackBufferHeight), Color.White);
            basicFogOfWarEffect.CurrentTechnique.Passes[0].End();
            basicFogOfWarEffect.End();
            batch.End();

            View = matrix;
            if (GlobalStats.RenderBloom)
            {
                bloomComponent?.Draw(gameTime);
            }

            batch.Begin(SpriteBlendMode.AlphaBlend);

            if (viewState >= UnivScreenState.SectorView) // draw colored empire borders only if zoomed out
            {
                // set the alpha value depending on camera height
                int alpha = (int) (90.0f * CamHeight / 1800000.0f);
                if (alpha > 90) alpha = 90;
                else if (alpha < 10) alpha = 0;
                var color = new Color(255, 255, 255, (byte) alpha);

                batch.Draw(BorderRT.GetTexture(),
                    new Rectangle(0, 0,
                        graphics.PresentationParameters.BackBufferWidth,
                        graphics.PresentationParameters.BackBufferHeight), color);
            }

            RenderOverFog(batch, gameTime);
            batch.End();

            batch.Begin();
            DrawPlanetInfo();
            if (LookingAtPlanet) workersPanel?.Draw(batch);

            DrawShipsInRange(batch);
            DrawPlanetProjectiles(batch);

            DrawTacticalPlanetIcons(batch);
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

            if (showingRangeOverlay && !LookingAtPlanet)
            {
                var shipRangeTex = ResourceManager.Texture("UI/node_shiprange");
                foreach (ClickableShip clickable in ClickableShipsList)
                {
                    Ship ship = clickable.shipToClick;
                    if (ship != null && ship.WeaponsMaxRange > 0f)
                    {
                        Color color = (ship.loyalty == EmpireManager.Player)
                            ? new Color(0, 200, 0, 30)
                            : new Color(200, 0, 0, 30);
                        DrawTextureProjected(shipRangeTex, ship.Position, ship.WeaponsMaxRange, color);
                    }
                }
            }

            if (showingDSBW && !LookingAtPlanet)
            {
                var nodeTex = ResourceManager.Texture("UI/node1");
                lock (GlobalStats.ClickableSystemsLock)
                {
                    foreach (ClickablePlanets cplanet in ClickPlanetList)
                    {
                        float radius = 2500f * cplanet.planetToClick.Scale;
                        DrawCircleProjected(cplanet.planetToClick.Center, radius, new Color(255, 165, 0, 150), 1f,
                            nodeTex, new Color(0, 0, 255, 50));
                    }
                }
                dsbw.Draw(batch);
            }
            DrawFleetIcons();

            //fbedard: display values in new buttons
            ShipsInCombat.Text = "Ships: " + player.empireShipCombat;
            ShipsInCombat.Style = player.empireShipCombat > 0 ? ButtonStyle.Medium : ButtonStyle.MediumMenu;
            ShipsInCombat.Draw(batch);

            PlanetsInCombat.Text = "Planets: " + player.empirePlanetCombat;
            PlanetsInCombat.Style = player.empirePlanetCombat > 0 ? ButtonStyle.Medium : ButtonStyle.MediumMenu;
            PlanetsInCombat.Draw(batch);

            if (!LookingAtPlanet)
                pieMenu.Draw(batch, Fonts.Arial12Bold);

            batch.DrawRectangle(SelectionBox, Color.Green, 1f);
            EmpireUI.Draw(batch);
            if (!LookingAtPlanet)
                DrawShipUI(batch);

            minimap.Visible = !LookingAtPlanet || LookingAtPlanet && workersPanel is UnexploredPlanetScreen ||
                              LookingAtPlanet && workersPanel is UnownedPlanetScreen;

            if (SelectedShipList.Count == 0)
                shipListInfoUI.ClearShipList();
            if (SelectedSystem != null && !LookingAtPlanet)
            {
                sInfoUI.SetSystem(SelectedSystem);
                sInfoUI.Update(gameTime);
                if (viewState == UnivScreenState.GalaxyView)
                    sInfoUI.Draw(gameTime);
            }
            if (SelectedPlanet != null && !LookingAtPlanet)
            {
                pInfoUI.SetPlanet(SelectedPlanet);
                pInfoUI.Update(gameTime);
                pInfoUI.Draw(gameTime);
            }
            else if (SelectedShip != null && !LookingAtPlanet)
            {
                ShipInfoUIElement.Ship = SelectedShip;
                ShipInfoUIElement.ShipNameArea.Text = SelectedShip.VanityName;
                ShipInfoUIElement.Update(gameTime);
                ShipInfoUIElement.Draw(gameTime);
            }
            else if (SelectedShipList.Count > 1 && SelectedFleet == null)
            {
                shipListInfoUI.Update(gameTime);
                shipListInfoUI.Draw(gameTime);
            }
            else if (SelectedItem != null)
            {
                bool flag = false;
                for (int index = 0; index < SelectedItem.AssociatedGoal.empire.GetEmpireAI().Goals.Count; ++index)
                {
                    if (SelectedItem.AssociatedGoal.empire.GetEmpireAI().Goals[index].guid !=
                        SelectedItem.AssociatedGoal.guid) continue;
                    flag = true;
                    break;
                }
                if (flag)
                {
                    string titleText = "(" + ResourceManager.ShipsDict[SelectedItem.UID].Name + ")";
                    string bodyText = Localizer.Token(1410) + SelectedItem.AssociatedGoal.PlanetBuildingAt.Name;
                    vuiElement.Draw(gameTime, titleText, bodyText);
                    DrawItemInfoForUI();
                }
                else
                    SelectedItem = null;
            }
            else if (SelectedFleet != null && !LookingAtPlanet)
            {
                shipListInfoUI.Update(gameTime);
                shipListInfoUI.Draw(gameTime);
            }
            if (SelectedShip == null || LookingAtPlanet)
                ShipInfoUIElement.ShipNameArea.HandlingInput = false;
            DrawToolTip();
            if (!LookingAtPlanet)
                NotificationManager.Draw(batch);

            if (Debug)
                DebugWin?.Draw(gameTime);

            if (Paused)
            {
                var textPos = new Vector2(ScreenWidth / 2f - Fonts.Pirulen16.MeasureString(Localizer.Token(4005)).X / 2f, 45f);
                batch.DrawString(Fonts.Pirulen16, Localizer.Token(4005), textPos, Color.White);
            }
            if (RandomEventManager.ActiveEvent != null && RandomEventManager.ActiveEvent.InhibitWarp)
            {
                var textPos = new Vector2(ScreenWidth / 2f - Fonts.Pirulen16.MeasureString(Localizer.Token(4005)).X / 2f,
                                          45 + Fonts.Pirulen16.LineSpacing + 2);
                batch.DrawString(Fonts.Pirulen16, "Hyperspace Flux", textPos, Color.Yellow);
            }
            if (IsActive && SavedGame.IsSaving)
            {
                var textPos = new Vector2(ScreenWidth / 2f - Fonts.Pirulen16.MeasureString(Localizer.Token(4005)).X / 2f,
                                          45 + Fonts.Pirulen16.LineSpacing * 2 + 4);
                batch.DrawString(Fonts.Pirulen16, "Saving...", textPos, CurrentFlashColor);
            }

            if (IsActive && (GameSpeed != 1f)) //don't show "1.0x"
            {
                string speed = GameSpeed.ToString("0.0##") + "x";
                var textPos = new Vector2(ScreenWidth - Fonts.Pirulen16.MeasureString(speed).X - 13f, 64f);
                batch.DrawString(Fonts.Pirulen16, speed, textPos, Color.White);
            }

            if (Debug) ShowDebugGameInfo();
            else       HideDebugGameInfo();

            aw.Visible = aw.IsOpen && !LookingAtPlanet;

            base.Draw(batch);  // UIElementV2 Draw

            batch.End();

            // Notify ProcessTurns that Drawing has finished and while SwapBuffers is blocking,
            // the game logic can be updated
            DrawCompletedEvt.Set();
        }


        UILabel DebugText;

        void HideDebugGameInfo()
        {
            DebugText?.Hide();
        }

        void ShowDebugGameInfo()
        {
            if (DebugText == null)
                DebugText = Label(ScreenWidth - 250f, 44f, "");

            DebugText.Show();
            DebugText.MultilineText = new Array<string>
            {
                "Comparisons:      " + GlobalStats.Comparisons,
                "Dis Check Avg:    " + GlobalStats.DistanceCheckTotal / GlobalStats.ComparisonCounter,
                "Modules Updated:  " + GlobalStats.ModuleUpdates,
                "Arc Checks:       " + GlobalStats.WeaponArcChecks,
                "Beam Tests:       " + GlobalStats.BeamTests,
                "Memory:           " + Memory,
                "",
                "Ship Count:       " + MasterShipList.Count,
                "Ship Time:        " + Perfavg2,
                "Empire Time:      " + EmpireUpdatePerf,
                "PreEmpire Time:   " + PreEmpirePerf,
                "Post Empire Time: " + perfavg4,
                "",
                "Total Time:       " + perfavg5
            };
        }

        void DrawFleetIcons()
        {
            ClickableFleetsList.Clear();
            if (viewState < UnivScreenState.SectorView)
                return;

            foreach (Empire empire in EmpireManager.Empires)
            {
                // not sure if this is the right way to do this but its hitting a crash here on collection change when the fleet loop is a foreach
                Fleet[] fleets = empire.GetFleetsDict().AtomicValuesArray();
                for (int i = 0; i < fleets.Length; i++)
                {
                    Fleet fleet = fleets[i];
                    if (fleet.Ships.Count <= 0)
                        continue;
                    if (!Debug && player.DifficultyModifiers.HideTacticalData && player.IsEmpireAttackable(fleet.Owner))
                        continue;
                    Vector2 averagePos = fleet.AveragePosition();
                    bool inSensors = player.IsPointInSensors(averagePos);
                    if (!inSensors && !Debug && fleet.Owner != player)
                        continue;

                    SubTexture icon = fleet.Icon;
                    Vector2 fleetCenterOnScreen = ProjectToScreenPosition(averagePos);

                    FleetIconLines(fleet, fleetCenterOnScreen);
                    ClickableFleetsList.Add(new ClickableFleet
                    {
                        fleet = fleet,
                        ScreenPos = fleetCenterOnScreen,
                        ClickRadius = 15f
                    });
                    ScreenManager.SpriteBatch.Draw(icon, fleetCenterOnScreen, empire.EmpireColor, 0.0f,
                        icon.CenterF, 0.35f, SpriteEffects.None, 1f);
                    HelperFunctions.DrawDropShadowText(ScreenManager.SpriteBatch, fleet.Name,
                        fleetCenterOnScreen + new Vector2(10f, -6f), Fonts.Arial8Bold);
                }
            }
        }

        void FleetIconLines(Fleet kv, Vector2 fleetCenterOnScreen)
        {
            foreach (Ship ship in kv.Ships)
            {
                if (ship?.Active == true)
                {
                    Vector2 shipScreenPos = ProjectToScreenPosition(ship.Center);
                    ScreenManager.SpriteBatch.DrawLine(shipScreenPos,
                        fleetCenterOnScreen, new Color(255, 255, 255, 20));
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
                    EmpireManager.Player.TryGetRelations(e, out Relationship ssRel);
                    wellKnown = Debug || EmpireManager.Player == e || ssRel.Treaty_Alliance;
                    if (wellKnown) break;

                }
                if (!solarSystem.IsExploredBy(player))
                    continue;

                foreach (Planet planet in solarSystem.PlanetList)
                {

                    if (!wellKnown && planet.Owner != null)
                    {
                        Empire e = planet.Owner;
                        if (EmpireManager.Player.TryGetRelations(e, out Relationship ssRel) && ssRel.Known)
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

        void DrawShipUI(SpriteBatch batch)
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

                    byte buttonFlashTimer = (byte)(Math.Abs(RadMath.Sin(GameBase.Base.GameTime.TotalGameTime.TotalSeconds)) * 200f);
                    batch.Draw(ResourceManager.Texture("NewUI/rounded_square"),
                        fleetButton.ClickRect,
                        inCombat ? new Color(255, 0,  0,  buttonFlashTimer)
                                 : new Color( 0,  0,  0,  80));

                    buttonSelector.Draw(batch);
                    batch.Draw(fleetButton.Fleet.Icon, housing,
                        EmpireManager.Player.EmpireColor);
                    batch.DrawString(Fonts.Pirulen12, fleetButton.Key.ToString(),
                        new Vector2(fleetButton.ClickRect.X + 4, fleetButton.ClickRect.Y + 4), Color.Orange);

                    //draw ship icons to right of button
                    Vector2 shipSpacingH = new Vector2( fleetButton.ClickRect.X + 50, fleetButton.ClickRect.Y);
                    for (int x = 0; x < fleetButton.Fleet.Ships.Count; ++x)
                    {
                        Ship ship = fleetButton.Fleet.Ships[x];
                        var iconHousing = new Rectangle((int) shipSpacingH.X, (int) shipSpacingH.Y, 15, 15);
                        shipSpacingH.X +=  15f;
                        if (shipSpacingH.X > 200f)
                        {
                            shipSpacingH.X = fleetButton.ClickRect.X + 50f;
                            shipSpacingH.Y += 15f;
                        }
                        batch.Draw(ship.GetTacticalIcon(), iconHousing, fleetButton.Fleet.Owner.EmpireColor);
                    }
                }
            }
        }

        void DrawShipsInRange(SpriteBatch batch)
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

            using (player.KnownShips.AcquireReadLock())
            {
                foreach (Ship ship in player.KnownShips)
                {
                    if (viewState <= UnivScreenState.SystemView)
                    {
                        ship.DrawProjectiles(batch, this);
                    }
                }
            }
            Device.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            Device.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            rs.AlphaBlendEnable                           = true;
            rs.AlphaBlendOperation                        = BlendFunction.Add;
            rs.SourceBlend                                = Blend.SourceAlpha;
            rs.DestinationBlend                           = Blend.InverseSourceAlpha;
            rs.DepthBufferWriteEnable                     = false;
            rs.CullMode                                   = CullMode.None;

            // @todo This should make use of spatial manager's quadtree
            //       We can select all ships inside screen area and check if those ships are Known
            using (player.KnownShips.AcquireReadLock())
            {
                foreach (Ship ship in player.KnownShips)
                {
                    if (!ship.Active || !ship.inSensorRange || !ScreenRectangle.HitTest(ship.ScreenPosition))
                        continue;

                    DrawTacticalIcon(ship);
                    DrawOverlay(ship);

                    if (SelectedShip != ship && !SelectedShipList.Contains(ship))
                        continue;

                    Color color = Color.LightGreen;
                    if (player != ship.loyalty)
                        color = player.IsEmpireAttackable(ship.loyalty) ? Color.Red : Color.Gray;
                    batch.BracketRectangle(ship.ScreenPosition, ship.ScreenRadius, color);
                }
            }

            DrawProjectedGroup();

            if (showingDSBW && !LookingAtPlanet)
                lock (GlobalStats.ClickableItemLocker)
                {
                    var platform = ResourceManager.Texture("TacticalIcons/symbol_platform");
                    for (int i = 0; i < ItemsToBuild.Count; ++i)
                    {
                        ClickableItemUnderConstruction item = ItemsToBuild[i];

                        if (ResourceManager.GetShipTemplate(item.UID, out Ship buildTemplate))
                        {
                            ProjectToScreenCoords(item.BuildPos, platform.Width, out Vector2 posOnScreen, out float size);

                            float scale = ScaleIconSize(size, 0.01f, 0.125f);
                            DrawTextureSized(platform, posOnScreen, 0.0f, platform.Width * scale,
                                       platform.Height * scale, new Color(0, 255, 0, 100));

                            if (item.UID == "Subspace Projector")
                            {
                                DrawCircle(posOnScreen, EmpireManager.Player.GetProjectorRadius(), Color.Orange, 2f);
                            }
                            else if (buildTemplate.SensorRange > 0f)
                            {
                                DrawCircle(posOnScreen, buildTemplate.SensorRange, Color.Orange, 2f);
                            }
                        }
                    }
                }

            // show the object placement/build circle
            if (showingDSBW && dsbw.itemToBuild != null && dsbw.itemToBuild.IsSubspaceProjector &&
                AdjustCamTimer <= 0f)
            {
                Vector2 center = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
                float screenRadius = ProjectToScreenSize(EmpireManager.Player.GetProjectorRadius());
                DrawCircle(center, MathExt.SmoothStep(ref radlast, screenRadius, .3f), Color.Orange, 2f); //
            }
        }
        private float ScaleIconSize(float screenRadius, float minSize = 0, float maxSize = 0)
        {
            float size = screenRadius * 2;
            if (size < minSize && minSize != 0)
                size = minSize;
            else if (maxSize > 0f && size > maxSize)
                size = maxSize;
            return size + GlobalStats.IconSize;
        }
        void DrawPlanetProjectiles(SpriteBatch batch)
        {
            foreach (SolarSystem sys in SolarSystemList)
            {
                if (!sys.isVisible)
                    continue;

                Array<Planet> planets = sys.PlanetList;
                for (int i = 0; i < planets.Count; i++)
                {
                    Planet planet = planets[i];
                    using (planet.Projectiles.AcquireReadLock())
                    {
                        foreach (Projectile p in planets[i].Projectiles)
                        {
                            if (p?.Active ?? false) p.Draw(batch, this);
                        }
                    }
                }
            }
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
            SubTexture symbol = ship.GetTacticalIcon();

            float num = ship.SurfaceArea / (30f + symbol.Width);
            float scale = num * 4000f / CamHeight;
            if (scale > 1.0f)
            {
                scale = 1f;
            }
            else if (scale <= 0.1f)
            {
                scale = ship.shipData.Role != ShipData.RoleName.platform || viewState < UnivScreenState.SectorView
                      ? 0.15f : 0.08f;
            }

            DrawTextureProjected(symbol, position, scale, direction.ToRadians(), color);
        }

        void DrawOverlay(Ship ship)
        {
            if (ship.InFrustum && !ship.dying && !LookingAtPlanet && ShowShipNames &&
                viewState <= UnivScreenState.SystemView)
                ship.DrawModulesOverlay(this);
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
                    if (ship.Mothership != null)
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
            var renderState = ScreenManager.GraphicsDevice.RenderState;
            renderState.AlphaBlendEnable = true;
            renderState.AlphaBlendOperation = BlendFunction.Add;
            renderState.SourceBlend = Blend.SourceAlpha;
            renderState.DestinationBlend = Blend.One;
            renderState.DepthBufferWriteEnable = false;
            ShieldManager.Draw(View, Projection);
        }

        private void DrawPlanetInfo()
        {
            if (LookingAtPlanet || viewState > UnivScreenState.SectorView || viewState < UnivScreenState.ShipView)
                return;
            Vector2 mousePos = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
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
                    EmpireManager.Player.TryGetRelations(e, out Relationship ssRel);
                    wellKnown = Debug || EmpireManager.Player == e || ssRel.Treaty_Alliance;
                    if (wellKnown) break;

                }

                for (int j = 0; j < solarSystem.PlanetList.Count; j++)
                {
                    Planet planet = solarSystem.PlanetList[j];
                    if (!planet.IsExploredBy(player) && !wellKnown)
                        continue;
                    if (planet.Owner != null && wellKnown)
                    {
                        Empire e = planet.Owner;
                        if (EmpireManager.Player.TryGetRelations(e, out Relationship ssRel) && ssRel.Known)
                            wellKnown = true;

                    }
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
                        DrawTextureWithToolTip(icon_fighting_small, Color.White, 121, mousePos, (int)posOffSet.X,
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
                                DrawTextureWithToolTip(icon_spy_small, Color.White, 121, mousePos,
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