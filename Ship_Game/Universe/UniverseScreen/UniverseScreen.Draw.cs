using System;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public sealed partial class UniverseScreen
    {
        private void DrawRings(Matrix world, Matrix view, Matrix projection, float scale)
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
                basicEffect.View = view;
                basicEffect.DiffuseColor = new Vector3(1f, 1f, 1f);
                basicEffect.Texture = this.RingTexture;
                basicEffect.TextureEnabled = true;
                basicEffect.Projection = projection;
            }
            xnaPlanetModel.Meshes[1].Draw();
            ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
        }

        private void DrawAtmo(Model model, Matrix world, Matrix view, Matrix projection, Planet planet)
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
            foreach (BasicEffect basicEffect in modelMesh.Effects)
            {
                basicEffect.World = Matrix.CreateScale(4.1f) * world;
                basicEffect.View = view;
                basicEffect.Texture = ResourceManager.TextureDict["Atmos"];
                basicEffect.TextureEnabled = true;
                basicEffect.Projection = projection;
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
            this.DrawAtmo1(world, view, projection);
            renderState.DepthBufferWriteEnable = true;
            renderState.CullMode = CullMode.CullCounterClockwiseFace;
            renderState.AlphaBlendEnable = false;
        }

        private void DrawAtmo1(Matrix world, Matrix view, Matrix projection)
        {
            this.AtmoEffect.Parameters["World"].SetValue(Matrix.CreateScale(3.83f) * world);
            this.AtmoEffect.Parameters["Projection"].SetValue(projection);
            this.AtmoEffect.Parameters["View"].SetValue(view);
            this.AtmoEffect.Parameters["CameraPosition"].SetValue(new Vector3(0.0f, 0.0f, 1500f));
            this.AtmoEffect.Parameters["DiffuseLightDirection"].SetValue(new Vector3(-0.98f, 0.425f, -0.4f));
            for (int index1 = 0; index1 < this.AtmoEffect.CurrentTechnique.Passes.Count; ++index1)
            {
                for (int index2 = 0; index2 < this.atmoModel.Meshes.Count; ++index2)
                {
                    ModelMesh modelMesh = ((ReadOnlyCollection<ModelMesh>) this.atmoModel.Meshes)[index2];
                    for (int index3 = 0; index3 < modelMesh.MeshParts.Count; ++index3)
                        modelMesh.MeshParts[index3].Effect = this.AtmoEffect;
                    modelMesh.Draw();
                }
            }
        }

        private void DrawClouds(Model model, Matrix world, Matrix view, Matrix projection, Planet p)
        {
            this.ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            this.ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            var renderState = ScreenManager.GraphicsDevice.RenderState;
            renderState.AlphaBlendEnable = true;
            renderState.AlphaBlendOperation = BlendFunction.Add;
            renderState.SourceBlend = Blend.SourceAlpha;
            renderState.DestinationBlend = Blend.InverseSourceAlpha;
            renderState.DepthBufferWriteEnable = false;
            ModelMesh modelMesh = ((ReadOnlyCollection<ModelMesh>) model.Meshes)[0];
            foreach (BasicEffect basicEffect in modelMesh.Effects)
            {
                basicEffect.World = Matrix.CreateScale(4.05f) * world;
                basicEffect.View = view;
                basicEffect.Texture = this.cloudTex;
                basicEffect.TextureEnabled = true;
                basicEffect.Projection = projection;
                basicEffect.LightingEnabled = true;
                basicEffect.DirectionalLight0.DiffuseColor = new Vector3(1f, 1f, 1f);
                basicEffect.DirectionalLight0.Enabled = true;
                basicEffect.DirectionalLight0.SpecularColor = new Vector3(1f, 1f, 1f);
                if (this.UseRealLights)
                {
                    Vector3 vector3 = new Vector3(p.Center - p.ParentSystem.Position, 0.0f);
                    vector3 = Vector3.Normalize(vector3);
                    basicEffect.DirectionalLight0.Direction = vector3;
                }
                else
                    basicEffect.DirectionalLight0.Direction = new Vector3(0.98f, -0.025f, 0.2f);
            }
            modelMesh.Draw();
            renderState.DepthBufferWriteEnable = true;
        }

        private void DrawToolTip()
        {
            if (this.SelectedSystem != null && !this.LookingAtPlanet)
            {
                float num = 4500f;
                Vector3 vector3_1 = this.Viewport.Project(
                    new Vector3(this.SelectedSystem.Position, 0.0f), this.projection, this.view, Matrix.Identity);
                Vector2 Position = new Vector2(vector3_1.X, vector3_1.Y);
                Vector3 vector3_2 = this.Viewport.Project(
                    new Vector3(new Vector2(this.SelectedSystem.Position.X + num, this.SelectedSystem.Position.Y),
                        0.0f), this.projection, this.view, Matrix.Identity);
                float Radius = Vector2.Distance(new Vector2(vector3_2.X, vector3_2.Y), Position);
                if ((double) Radius < 5.0)
                    Radius = 5f;
                Rectangle rectangle = new Rectangle((int) Position.X - (int) Radius, (int) Position.Y - (int) Radius,
                    (int) Radius * 2, (int) Radius * 2);
                this.ScreenManager.SpriteBatch.BracketRectangle(Position, Radius, Color.White);
            }
            if (this.SelectedPlanet == null || this.LookingAtPlanet ||
                this.viewState >= UniverseScreen.UnivScreenState.GalaxyView)
                return;
            float radius = this.SelectedPlanet.SO.WorldBoundingSphere.Radius;
            Vector3 vector3_3 = this.Viewport.Project(
                new Vector3(this.SelectedPlanet.Center, 2500f), this.projection, this.view, Matrix.Identity);
            Vector2 Position1 = new Vector2(vector3_3.X, vector3_3.Y);
            Vector3 vector3_4 = this.Viewport.Project(
                new Vector3(SelectedPlanet.Center.PointOnCircle(90f, radius), 2500f), this.projection, this.view,
                Matrix.Identity);
            float Radius1 = Vector2.Distance(new Vector2(vector3_4.X, vector3_4.Y), Position1);
            if ((double) Radius1 < 8.0)
                Radius1 = 8f;
            Vector2 vector2 = new Vector2(vector3_3.X, vector3_3.Y - Radius1);
            Rectangle rectangle1 = new Rectangle((int) Position1.X - (int) Radius1, (int) Position1.Y - (int) Radius1,
                (int) Radius1 * 2, (int) Radius1 * 2);
            this.ScreenManager.SpriteBatch.BracketRectangle(Position1, Radius1,
                this.SelectedPlanet.Owner != null ? this.SelectedPlanet.Owner.EmpireColor : Color.Gray);
        }

        private void DrawFogNodes()
        {
            var uiNode = ResourceManager.TextureDict["UI/node"];
            var viewport = Viewport;

            foreach (FogOfWarNode fogOfWarNode in FogNodes)
            {
                if (!fogOfWarNode.Discovered)
                    continue;

                Vector3 vector3_1 = viewport.Project(fogOfWarNode.Position.ToVec3(), this.projection, this.view,
                    Matrix.Identity);
                Vector2 vector2 = vector3_1.ToVec2();
                Vector3 vector3_2 = viewport.Project(
                    new Vector3(fogOfWarNode.Position.PointOnCircle(90f, fogOfWarNode.Radius * 1.5f), 0.0f),
                    this.projection, this.view, Matrix.Identity);
                float num = Math.Abs(new Vector2(vector3_2.X, vector3_2.Y).X - vector2.X);
                Rectangle destinationRectangle =
                    new Rectangle((int) vector2.X, (int) vector2.Y, (int) num * 2, (int) num * 2);
                ScreenManager.SpriteBatch.Draw(uiNode, destinationRectangle, null, new Color(70, 255, 255, 255), 0.0f,
                    uiNode.Center(), SpriteEffects.None, 1f);
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
                        new Vector3(influ.Position.PointOnCircle(90f, influ.Radius * 1.5f), 0.0f), this.projection,
                        this.view, Matrix.Identity);

                    float local_6 = Math.Abs(new Vector2(local_4.X, local_4.Y).X - screenPos.X) * 2.59999990463257f;
                    Rectangle local_7 = new Rectangle((int)screenPos.X, (int)screenPos.Y, (int)local_6, (int)local_6);

                    ScreenManager.SpriteBatch.Draw(uiNode, local_7, null, Color.White, 0.0f, uiNode.Center(),
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
                    foreach (Empire.InfluenceNode influ in empire.BorderNodes)
                    {
                        if (!Frustum.Contains(influ.Position, influ.Radius))
                            continue;
                        if (!influ.Known)
                            continue;
                        Vector2 nodePos = ProjectToScreenPosition(influ.Position);
                        int size = (int)Math.Abs(
                            ProjectToScreenPosition(influ.Position.PointOnCircle(90f, influ.Radius)).X - nodePos.X);

                        Rectangle rect = new Rectangle((int)nodePos.X, (int)nodePos.Y, size * 5, size * 5);
                        spriteBatch.Draw(nodeCorrected, rect, null, empireColor, 0.0f, nodeCorrected.Center(),
                            SpriteEffects.None, 1f);

                        foreach (Empire.InfluenceNode influ2 in empire.BorderNodes)
                        {
                            if (!influ2.Known)
                                continue;
                            if (influ.Position == influ2.Position || influ.Radius > influ2.Radius ||
                                influ.Position.OutsideRadius(influ2.Position, influ.Radius + influ2.Radius + 150000.0f))
                                continue;
                            
                            Vector2 endPos = ProjectToScreenPosition(influ2.Position);


                            float rotation = nodePos.RadiansToTarget(endPos);
                            rect = new Rectangle((int)endPos.X, (int)endPos.Y, size * 3 / 2,
                                (int)Vector2.Distance(nodePos, endPos));
                            spriteBatch.Draw(nodeConnect, rect, null, empireColor, rotation, new Vector2(2f, 2f),
                                SpriteEffects.None, 1f);
                        }
                    }
                }
            }
            spriteBatch.End();
        }

        private void DrawMain(GameTime gameTime)
        {
            Render(gameTime);
            ScreenManager.SpriteBatch.Begin(SpriteBlendMode.Additive);
            ExplosionManager.DrawExplosions(ScreenManager, view, projection);
            ScreenManager.SpriteBatch.End();
            if (!ShowShipNames || LookingAtPlanet)
                return;
            foreach (ClickableShip clickableShip in ClickableShipsList)
                if (clickableShip.shipToClick.InFrustum)
                    clickableShip.shipToClick.DrawShieldBubble(this);
        }

        private void DrawLights(GameTime gameTime)
        {
            this.ScreenManager.GraphicsDevice.SetRenderTarget(0, this.FogMapTarget);
            this.ScreenManager.GraphicsDevice.Clear(Color.TransparentWhite);
            this.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.Additive);
            this.ScreenManager.SpriteBatch.Draw(this.FogMap, new Rectangle(0, 0, 512, 512), Color.White);
            float num = 512f / UniverseSize;
            var uiNode = ResourceManager.TextureDict["UI/node"];
            foreach (Ship ship in player.GetShips())
            {
                if (ScreenRectangle.HitTest(ship.ScreenPosition))
                {
                    Rectangle destinationRectangle = new Rectangle(
                        (int) (ship.Position.X * num),
                        (int) (ship.Position.Y * num),
                        (int) (ship.SensorRange * num * 2.0),
                        (int) (ship.SensorRange * num * 2.0));
                    ScreenManager.SpriteBatch.Draw(uiNode, destinationRectangle, null, new Color(255, 0, 0, 255), 0.0f,
                        uiNode.Center(), SpriteEffects.None, 1f);
                }
            }
            this.ScreenManager.SpriteBatch.End();
            this.ScreenManager.GraphicsDevice.SetRenderTarget(0, null);
            this.FogMap = this.FogMapTarget.GetTexture();

            this.ScreenManager.GraphicsDevice.SetRenderTarget(0, this.LightsTarget);
            this.ScreenManager.GraphicsDevice.Clear(Color.White);

            this.Viewport.Project(new Vector3(this.UniverseSize / 2f, this.UniverseSize / 2f, 0.0f),
                this.projection, this.view, Matrix.Identity);
            this.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend);
            if (!Debug) // don't draw fog of war in debug
            {
                Vector3 vector3_1 =
                    Viewport.Project(Vector3.Zero, this.projection, this.view,
                        Matrix.Identity);
                Vector3 vector3_2 =
                    Viewport.Project(new Vector3(this.UniverseSize, this.UniverseSize, 0.0f),
                        this.projection, this.view, Matrix.Identity);

                Rectangle fogRect = new Rectangle((int) vector3_1.X, (int) vector3_1.Y,
                    (int) vector3_2.X - (int) vector3_1.X, (int) vector3_2.Y - (int) vector3_1.Y);
                ScreenManager.SpriteBatch.FillRectangle(new Rectangle(0, 0, 
                        ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth,
                        ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight),
                    new Color(0, 0, 0, 170));
                ScreenManager.SpriteBatch.Draw(FogMap, fogRect, new Color(255, 255, 255, 55));
            }
            this.DrawFogNodes();
            this.DrawInfluenceNodes();
            this.ScreenManager.SpriteBatch.End();
            this.ScreenManager.GraphicsDevice.SetRenderTarget(0, null);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            GameTime gameTime = Game1.Instance.GameTime;

            // Wait for ProcessTurns to finish before we start drawing
            if (ProcessTurnsThread != null && ProcessTurnsThread.IsAlive) // check if thread is alive to avoid deadlock
                if (!ProcessTurnsCompletedEvt.WaitOne(100))
                    Log.Warning("Universe ProcessTurns Wait timed out: ProcessTurns was taking too long!");

            lock (GlobalStats.BeamEffectLocker)
            {
                Beam.BeamEffect.Parameters["View"].SetValue(view);
                Beam.BeamEffect.Parameters["Projection"].SetValue(projection);
            }
            AdjustCamera((float) gameTime.ElapsedGameTime.TotalSeconds);
            CamPos.Z = CamHeight;
            view = Matrix.CreateTranslation(0.0f, 0.0f, 0.0f)
                   * Matrix.CreateRotationY(180f.ToRadians())
                   * Matrix.CreateRotationX(0.0f.ToRadians())
                   * Matrix.CreateLookAt(new Vector3(-CamPos.X, CamPos.Y, CamHeight),
                       new Vector3(-CamPos.X, CamPos.Y, 0.0f), new Vector3(0.0f, -1f, 0.0f));
            Matrix matrix = view;

            var graphics = ScreenManager.GraphicsDevice;
            graphics.SetRenderTarget(0, MainTarget);
            DrawMain(gameTime);
            graphics.SetRenderTarget(0, null);
            DrawLights(gameTime);

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
            basicFogOfWarEffect.Parameters["LightsTexture"].SetValue((Texture) texture2);
            ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate,
                SaveStateMode.SaveState);
            basicFogOfWarEffect.Begin();
            basicFogOfWarEffect.CurrentTechnique.Passes[0].Begin();
            ScreenManager.SpriteBatch.Draw(texture1,
                new Rectangle(0, 0, graphics.PresentationParameters.BackBufferWidth,
                    graphics.PresentationParameters.BackBufferHeight), Color.White);
            basicFogOfWarEffect.CurrentTechnique.Passes[0].End();
            basicFogOfWarEffect.End();
            ScreenManager.SpriteBatch.End();
            view = matrix;
            if (drawBloom) bloomComponent.Draw(gameTime);
            ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend);

            if (viewState >= UnivScreenState.SectorView) // draw colored empire borders only if zoomed out
            {
                // set the alpha value depending on camera height
                int alpha = (int) (90.0f * CamHeight / 1800000.0f);
                if (alpha > 90) alpha = 90;
                else if (alpha < 10) alpha = 0;
                var color = new Color(255, 255, 255, (byte) alpha);

                ScreenManager.SpriteBatch.Draw(BorderRT.GetTexture(),
                    new Rectangle(0, 0,
                        graphics.PresentationParameters.BackBufferWidth,
                        graphics.PresentationParameters.BackBufferHeight), color);
            }

            RenderOverFog(gameTime);
            ScreenManager.SpriteBatch.End();
            ScreenManager.SpriteBatch.Begin();
            DrawPlanetInfo();

            if (LookingAtPlanet && SelectedPlanet != null)
                workersPanel?.Draw(ScreenManager.SpriteBatch);
            DrawShipsInRange();

            foreach (SolarSystem solarSystem in SolarSystemList)
            {
                if (!solarSystem.isVisible)
                    continue;
                foreach (Planet planet in solarSystem.PlanetList)
                {
                    foreach (Projectile projectile in planet.Projectiles)
                    {
                        projectile.DrawProjectile(this);
                    }
                }
            }

            DrawTacticalPlanetIcons();
            if (showingFTLOverlay && GlobalStats.PlanetaryGravityWells && !LookingAtPlanet)
            {
                var inhibit = ResourceManager.Texture("UI/node_inhibit");
                lock (GlobalStats.ClickableSystemsLock)
                {
                    foreach (ClickablePlanets cplanet in ClickPlanetList)
                    {
                        float radius = cplanet.planetToClick.GravityWellRadius;
                        DrawCircleProjected(cplanet.planetToClick.Center, radius, new Color(255, 50, 0, 150), 50, 1f,
                            inhibit, new Color(200, 0, 0, 50));
                    }
                }
                foreach (ClickableShip ship in ClickableShipsList)
                {
                    if (ship.shipToClick != null && ship.shipToClick.InhibitionRadius > 0f)
                    {
                        float radius = ship.shipToClick.InhibitionRadius;
                        DrawCircleProjected(ship.shipToClick.Position, radius, new Color(255, 50, 0, 150), 50, 1f,
                            inhibit, new Color(200, 0, 0, 40));
                    }
                }
                if (viewState >= UnivScreenState.SectorView)
                {
                    foreach (Empire.InfluenceNode influ in player.BorderNodes.AtomicCopy())
                    {
                        DrawCircleProjected(influ.Position, influ.Radius, new Color(30, 30, 150, 150), 50, 1f, inhibit,
                            new Color(0, 200, 0, 20));
                    }
                }
            }

            if (showingRangeOverlay && !LookingAtPlanet)
            {
                var shipRangeTex = ResourceManager.Texture("UI/node_shiprange");
                foreach (ClickableShip ship in ClickableShipsList)
                {
                    if (ship.shipToClick != null && ship.shipToClick.RangeForOverlay > 0f)
                    {
                        Color color = (ship.shipToClick.loyalty == EmpireManager.Player)
                            ? new Color(0, 200, 0, 30)
                            : new Color(200, 0, 0, 30);
                        float radius = ship.shipToClick.RangeForOverlay;                        
                        DrawTextureProjected(shipRangeTex, ship.shipToClick.Position, radius, color);
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
                        float radius = 2500f * cplanet.planetToClick.scale;
                        DrawCircleProjected(cplanet.planetToClick.Center, radius, new Color(255, 165, 0, 150), 50, 1f,
                            nodeTex, new Color(0, 0, 255, 50));
                    }
                }
                dsbw.Draw(gameTime);
            }
            DrawFleetIcons(gameTime);

            //fbedard: display values in new buttons
            ShipsInCombat.Text = "Ships: " + player.empireShipCombat;
            if (player.empireShipCombat > 0)
            {
                ShipsInCombat.Style = ButtonStyle.Medium;
            }
            else
            {
                ShipsInCombat.Style = ButtonStyle.MediumMenu;
            }
            ShipsInCombat.Draw(ScreenManager.SpriteBatch);

            PlanetsInCombat.Text = "Planets: " + player.empirePlanetCombat;
            if (player.empirePlanetCombat > 0)
            {
                PlanetsInCombat.Style = ButtonStyle.Medium;
            }
            else
            {
                PlanetsInCombat.Style = ButtonStyle.MediumMenu;
            }
            PlanetsInCombat.Draw(ScreenManager.SpriteBatch);

            if (!LookingAtPlanet)
                pieMenu.Draw(this.ScreenManager.SpriteBatch, Fonts.Arial12Bold);

            ScreenManager.SpriteBatch.DrawRectangle(SelectionBox, Color.Green, 1f);
            EmpireUI.Draw(ScreenManager.SpriteBatch);
            if (!LookingAtPlanet)
                DrawShipUI(gameTime);

            if (!LookingAtPlanet || LookingAtPlanet && workersPanel is UnexploredPlanetScreen ||
                LookingAtPlanet && workersPanel is UnownedPlanetScreen)
            {
                minimap.Draw(ScreenManager, this);
            }
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
                for (int index = 0; index < SelectedItem.AssociatedGoal.empire.GetGSAI().Goals.Count; ++index)
                {
                    if (SelectedItem.AssociatedGoal.empire.GetGSAI().Goals[index].guid !=
                        SelectedItem.AssociatedGoal.guid) continue;
                    flag = true;
                    break;
                }
                if (flag)
                {
                    string titleText = "(" + ResourceManager.ShipsDict[SelectedItem.UID].Name + ")";
                    string bodyText = Localizer.Token(1410) + SelectedItem.AssociatedGoal.GetPlanetWhereBuilding().Name;
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
                NotificationManager.Draw();

            if (Debug && showdebugwindow)
                DebugWin.Draw(gameTime);

            if (aw.IsOpen && !LookingAtPlanet)
                aw.Draw(spriteBatch);

            if (Paused)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, Localizer.Token(4005),
                    new Vector2(
                        graphics.PresentationParameters.BackBufferWidth / 2f -
                        Fonts.Pirulen16.MeasureString(Localizer.Token(4005)).X / 2f, 45f), Color.White);
            }
            if (RandomEventManager.ActiveEvent != null && RandomEventManager.ActiveEvent.InhibitWarp)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, "Hyperspace Flux",
                    new Vector2(
                        graphics.PresentationParameters.BackBufferWidth / 2f -
                        Fonts.Pirulen16.MeasureString(Localizer.Token(4005)).X / 2f,
                        45 + Fonts.Pirulen16.LineSpacing + 2), Color.Yellow);
            }
            if (IsActive && SavedGame.IsSaving)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, "Saving...",
                    new Vector2(
                        graphics.PresentationParameters.BackBufferWidth / 2f -
                        Fonts.Pirulen16.MeasureString(Localizer.Token(4005)).X / 2f,
                        45 + Fonts.Pirulen16.LineSpacing * 2 + 4),
                    new Color(255, 255, 255, (float) Math.Abs(Math.Sin(gameTime.TotalGameTime.TotalSeconds)) * 255f));
            }

            if (IsActive && (GameSpeed != 1f)) //don't show "1.0x"
            {
                string speed = GameSpeed.ToString("0.0##") + "x";
                Vector2 speedTextPos = new Vector2(
                    ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth -
                    Fonts.Pirulen16.MeasureString(speed).X - 13f, 64f);
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, speed, speedTextPos, Color.White);
            }
            if (Debug)
            {
                var lines = new Array<string>();
                lines.Add("Comparisons:      " + GlobalStats.Comparisons);
                lines.Add("Dis Check Avg:    " + GlobalStats.DistanceCheckTotal / GlobalStats.ComparisonCounter);
                lines.Add("Modules Moved:    " + GlobalStats.ModulesMoved);
                lines.Add("Modules Updated:  " + GlobalStats.ModuleUpdates);
                lines.Add("Arc Checks:       " + GlobalStats.WeaponArcChecks);
                lines.Add("Beam Tests:       " + GlobalStats.BeamTests);
                lines.Add("Memory:           " + Memory);
                lines.Add("");
                lines.Add("Ship Count:       " + MasterShipList.Count);
                lines.Add("Ship Time:        " + Perfavg2);
                lines.Add("Empire Time:      " + EmpireUpdatePerf);
                lines.Add("PreEmpire Time:   " + PreEmpirePerf);
                lines.Add("Post Empire Time: " + perfavg4);
                lines.Add("");
                lines.Add("Total Time:       " + perfavg5);

                Vector2 pos = new Vector2(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 250f,
                    44f);
                DrawLinesToScreen(pos, lines);
            }
            if (IsActive)
                ToolTip.Draw(ScreenManager.SpriteBatch);
            ScreenManager.SpriteBatch.End();

            // Notify ProcessTurns that Drawing has finished and while SwapBuffers is blocking,
            // the game logic can be updated
            DrawCompletedEvt.Set();
        }

        private void DrawFleetIcons(GameTime gameTime)
        {
            ClickableFleetsList.Clear();
            if (viewState < UnivScreenState.SectorView)
                return;

            foreach (Empire empire in EmpireManager.Empires)
            {
                var fleetdic =
                    empire.GetFleetsDict()
                        .AtomicValuesArray(); //not sure if this is the right way to do this but its hitting a crash here on collection change when the fleet loop is a foreach
                for (int i = 0; i < fleetdic.Length; i++)
                {
                    var kv = fleetdic[i];
                    if (kv.Ships.Count <= 0)
                        continue;

                    Vector2 averagePosition = kv.FindAveragePositionset();
                    bool flag = player.IsPointInSensors(averagePosition);

                    if (flag || Debug || kv.Owner == player)
                    {
                        var icon = ResourceManager.TextureDict["FleetIcons/" + kv.FleetIconIndex];
                        Vector3 vector3_1 =
                            Viewport.Project(new Vector3(averagePosition, 0.0f),
                                projection, view, Matrix.Identity);
                        Vector2 vector2 = new Vector2(vector3_1.X, vector3_1.Y);
                        foreach (Ship ship in kv.Ships)
                        {
                            if ((!ship?.Active ?? true)) continue;
                            Vector3 vector3_2 =
                                Viewport.Project(
                                    new Vector3(ship.Center.X, ship.Center.Y, 0.0f), projection, view, Matrix.Identity);
                            ScreenManager.SpriteBatch.DrawLine(new Vector2(vector3_2.X, vector3_2.Y),
                                vector2, new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte) 20));
                        }
                        ClickableFleetsList.Add(new ClickableFleet
                        {
                            fleet = kv,
                            ScreenPos = vector2,
                            ClickRadius = 15f
                        });
                        ScreenManager.SpriteBatch.Draw(icon, vector2, new Rectangle?(), empire.EmpireColor, 0.0f,
                            icon.Center(), 0.35f, SpriteEffects.None, 1f);
                        HelperFunctions.DrawDropShadowText(ScreenManager, kv.Name,
                            new Vector2(vector2.X + 10f, vector2.Y - 6f), Fonts.Arial8Bold);
                    }
                }
            }
        }

        private void DrawTacticalPlanetIcons()
        {
            if (this.LookingAtPlanet || this.viewState <= UniverseScreen.UnivScreenState.SystemView 
                ||this.viewState >= UniverseScreen.UnivScreenState.GalaxyView)
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
                   
                    float fIconScale = 0.1875f * (0.7f + ((float) (Math.Log(planet.scale)) / 2.75f));
                    if (planet.Owner != null && wellKnown )
                    {

                        Vector3 vector3 =
                            this.Viewport.Project(new Vector3(planet.Center, 2500f),
                                this.projection, this.view, Matrix.Identity);
                        Vector2 position = new Vector2(vector3.X, vector3.Y);
                        Rectangle rectangle = new Rectangle((int) position.X - 8, (int) position.Y - 8, 16, 16);
                        Vector2 origin = new Vector2(
                            (float) (ResourceManager.TextureDict["Planets/" + (object) planet.planetType].Width /
                                     2f),
                            (float) (ResourceManager.TextureDict["Planets/" + (object) planet.planetType].Height /
                                     2f));
                        this.ScreenManager.SpriteBatch.Draw(
                            ResourceManager.TextureDict["Planets/" + (object) planet.planetType], position,
                            new Rectangle?(), Color.White, 0.0f, origin, fIconScale, SpriteEffects.None, 1f);
                        origin =
                            new Vector2(
                                (float) (ResourceManager.FlagTextures[planet.Owner.data.Traits.FlagIndex]
                                             .Value.Width / 2),
                                (float) (ResourceManager.FlagTextures[planet.Owner.data.Traits.FlagIndex]
                                             .Value.Height / 2));
                        this.ScreenManager.SpriteBatch.Draw(
                            ResourceManager.FlagTextures[planet.Owner.data.Traits.FlagIndex].Value, position,
                            new Rectangle?(), planet.Owner.EmpireColor, 0.0f, origin, 0.045f, SpriteEffects.None,
                            1f);
                    }
                    else
                    {
                        Vector3 vector3 =
                            this.Viewport.Project(new Vector3(planet.Center, 2500f),
                                this.projection, this.view, Matrix.Identity);
                        Vector2 position = new Vector2(vector3.X, vector3.Y);
                        Rectangle rectangle = new Rectangle((int) position.X - 8, (int) position.Y - 8, 16, 16);
                        Vector2 origin = new Vector2(
                            (float) (ResourceManager.Texture("Planets/" + (object) planet.planetType).Width /
                                     2),
                            (float) (ResourceManager.Texture("Planets/" + (object) planet.planetType).Height /
                                     2f));
                        this.ScreenManager.SpriteBatch.Draw(
                            ResourceManager.Texture("Planets/" + (object) planet.planetType), position,
                            new Rectangle?(), Color.White, 0.0f, origin, fIconScale, SpriteEffects.None, 1f);
                    }
                }
            }
        }

        private void DrawItemInfoForUI()
        {
            var goal = SelectedItem?.AssociatedGoal;
            if (goal == null) return;
            DrawCircleProjected(goal.BuildPosition, 50f, 50, goal.empire.EmpireColor);            
        }

        private void DrawShipUI(GameTime gameTime)
        {
            Vector2 vector2 = new Vector2((float) Mouse.GetState().X, (float) Mouse.GetState().Y);
            lock (GlobalStats.FleetButtonLocker)
            {
                foreach (UniverseScreen.FleetButton item_0 in this.FleetButtons)
                {
                    Selector local_1 = new Selector(item_0.ClickRect, Color.TransparentBlack);
                    Rectangle local_2 = new Rectangle(item_0.ClickRect.X + 6, item_0.ClickRect.Y + 6,
                        item_0.ClickRect.Width - 12, item_0.ClickRect.Width - 12);
                    bool local_3 = false;
                    for (int local_4 = 0; local_4 < item_0.Fleet.Ships.Count; ++local_4)
                    {
                        try
                        {
                            if (item_0.Fleet.Ships[local_4].InCombat)
                            {
                                local_3 = true;
                                break;
                            }
                        }
                        catch { }
                    }
                    float local_6_1 = Math.Abs((float) Math.Sin(gameTime.TotalGameTime.TotalSeconds)) * 200f;
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/rounded_square"],
                        item_0.ClickRect,
                        local_3
                            ? new Color(byte.MaxValue, (byte) 0, (byte) 0, (byte) local_6_1)
                            : new Color((byte) 0, (byte) 0, (byte) 0, (byte) 80));
                    local_1.Draw(ScreenManager.SpriteBatch);
                    this.ScreenManager.SpriteBatch.Draw(
                        ResourceManager.TextureDict["FleetIcons/" + item_0.Fleet.FleetIconIndex.ToString()], local_2,
                        EmpireManager.Player.EmpireColor);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, item_0.Key.ToString(),
                        new Vector2((float) (item_0.ClickRect.X + 4), (float) (item_0.ClickRect.Y + 4)), Color.Orange);
                    Vector2 local_7 = new Vector2((float) (item_0.ClickRect.X + 50), (float) item_0.ClickRect.Y);
                    for (int local_8 = 0; local_8 < item_0.Fleet.Ships.Count; ++local_8)
                    {
                        try
                        {
                            Ship local_9 = item_0.Fleet.Ships[local_8];

                            Rectangle local_10 = new Rectangle((int) local_7.X, (int) local_7.Y, 15, 15);
                            local_7.X += (float) (15);
                            if ((double) local_7.X > 200.0)
                            {
                                local_7.X = (float) (item_0.ClickRect.X + 50);
                                local_7.Y += 15f;
                            }
                            this.ScreenManager.SpriteBatch.Draw(
                                ResourceManager.TextureDict[
                                    "TacticalIcons/symbol_" +
                                    (local_9.isConstructor ? "construction" : local_9.shipData.GetRole())], local_10,
                                item_0.Fleet.Owner.EmpireColor);
                        }
                        catch { }
                    }
                }
            }
        }

        private void DrawShipsInRange()
        {
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            var renderState = ScreenManager.GraphicsDevice.RenderState;
            renderState.AlphaBlendEnable = true;
            renderState.AlphaBlendOperation = BlendFunction.Add;
            renderState.SourceBlend = Blend.SourceAlpha;
            renderState.DestinationBlend = Blend.One;
            renderState.DepthBufferWriteEnable = false;
            renderState.CullMode = CullMode.None;

            using (player.KnownShips.AcquireReadLock())
            {
                foreach (Ship ship in player.KnownShips)
                {
                    if (!ship.Active) continue;
                    DrawInRange(ship);
                }
            }
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            renderState.AlphaBlendEnable = true;
            renderState.AlphaBlendOperation = BlendFunction.Add;
            renderState.SourceBlend = Blend.SourceAlpha;
            renderState.DestinationBlend = Blend.InverseSourceAlpha;
            renderState.DepthBufferWriteEnable = false;
            renderState.CullMode = CullMode.None;

            using (player.KnownShips.AcquireReadLock())
            {
                foreach (Ship ship in player.KnownShips)
                {
                    if (!ship.Active || !ScreenRectangle.HitTest(ship.ScreenPosition))
                        continue;

                    DrawTacticalIcon(ship);
                    DrawOverlay(ship);

                    if (SelectedShip == ship || SelectedShipList.Contains(ship))
                    {
                        Color color = Color.LightGreen;
                        color = player.IsEmpireAttackable(ship.loyalty) ? Color.Red : Color.Gray;
                        ScreenManager.SpriteBatch.BracketRectangle(ship.ScreenPosition, ship.ScreenRadius,
                            color);
                    }
                }
            }
            if (ProjectingPosition)
                DrawProjectedGroup();

            var platform = ResourceManager.Texture("TacticalIcons/symbol_platform");

            lock (GlobalStats.ClickableItemLocker)
            {
                for (int i = 0; i < ItemsToBuild.Count; ++i)
                {
                    ClickableItemUnderConstruction item = ItemsToBuild[i];

                    if (ResourceManager.GetShipTemplate(item.UID, out Ship buildTemplate))
                    {
                        //float scale2 = 0.07f;
                        float scale = ((float) buildTemplate.Size / platform.Width) * 4000f / CamHeight;
                        DrawTextureProjected(platform, item.BuildPos, scale, 0.0f, new Color(0, 255, 0, 100));
                        if (showingDSBW)
                        {
                            if (item.UID == "Subspace Projector")
                            {
                                DrawCircleProjected(item.BuildPos, Empire.ProjectorRadius, 50, Color.Orange, 2f);
                            }
                            else if (buildTemplate.SensorRange > 0f)
                            {
                                DrawCircleProjected(item.BuildPos, buildTemplate.SensorRange, 50, Color.Blue, 2f);
                            }
                        }
                    }
                }
            }

            // show the object placement/build circle
            if (showingDSBW && dsbw.itemToBuild != null && dsbw.itemToBuild.Name == "Subspace Projector" &&
                AdjustCamTimer <= 0f)
            {
                Vector2 center = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
                float screenRadius = ProjectToScreenSize(Empire.ProjectorRadius);
                DrawCircle(center, MathExt.SmoothStep(ref radlast, screenRadius, .3f), 50, Color.Orange, 2f); //
            }
        }

        private void DrawProjectedGroup()
        {
            if (projectedGroup == null)
                return;

            foreach (Ship ship in projectedGroup.Ships)
            {
                if (!ship.Active)
                    continue;

                var symbol = ResourceManager.Texture("TacticalIcons/symbol_" +
                                                     (ship.isConstructor ? "construction" : ship.shipData.GetRole()));

                float num = ship.Size / (30f + symbol.Width);
                float scale = num * 4000f / CamHeight;
                if (scale > 1.0f) scale = 1f;
                else if (scale <= 0.1f)
                    scale = ship.shipData.Role != ShipData.RoleName.platform || viewState < UnivScreenState.SectorView
                        ? 0.15f
                        : 0.08f;

                DrawTextureProjected(symbol, ship.projectedPosition, scale, projectedGroup.ProjectedFacing,
                    new Color(0, 255, 0, 100));
            }
        }

        public void DrawWeaponArc(ShipModule module, Vector2 posOnScreen, float rotation)
        {
            Color color = GetWeaponArcColor(module.InstalledWeapon);
            Texture2D arc = GetArcTexture(module.FieldOfFire);
            float arcLength = ProjectToScreenSize(module.InstalledWeapon.Range);
            DrawTextureSized(arc, posOnScreen, rotation, arcLength, arcLength, color);
        }

        public void DrawWeaponArc(Ship parent, ShipModule module)
        {
            float rotation = parent.Rotation + module.Facing.ToRadians();
            Vector2 posOnScreen = ProjectToScreenPosition(module.Center);
            DrawWeaponArc(module, posOnScreen, rotation);
        }

        private void DrawOverlay(Ship ship)
        {
            if (ship.InFrustum && !ship.dying && !LookingAtPlanet && ShowShipNames &&
                viewState <= UnivScreenState.SystemView)
                ship.DrawModulesOverlay(this);
        }

        private void DrawTacticalIcon(Ship ship)
        {
            if (LookingAtPlanet || ship.IsPlatform && (
                    (!showingFTLOverlay && viewState == UnivScreenState.GalaxyView) ||
                    (showingFTLOverlay && ship.Name != "Subspace Projector")))
                return;
            ship.DrawTacticalIcon(this, viewState);
        }

        private void DrawBombs()
        {
            using (BombList.AcquireReadLock())
            {
                for (int i = 0; i < this.BombList.Count; i++)
                {
                    Bomb bomb = this.BombList[i];
                    DrawTransparentModel(bomb.Model, bomb.World, bomb.Texture, 0.5f);
                }
            }
        }

        private void DrawInRange(Ship ship)
        {
            if (viewState > UnivScreenState.SystemView)
                return;
            
            for (int i = 0; i < ship.Projectiles.Count; i++)
            {
                Projectile projectile = ship.Projectiles[i];
                projectile.DrawProjectile(this);
            }
        }

        private void DrawShipLines(Ship ship, byte alpha)
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
                int spots = ship.AI.OrbitTarget.GetGroundLandingSpots();
                if (spots > 4)
                    DrawLineProjected(start, ship.AI.OrbitTarget.Center, Colors.CombatOrders(alpha), 2500f);
                else if (spots > 0)
                    DrawLineProjected(start, ship.AI.OrbitTarget.Center, Colors.Warning(alpha), 2500f);
                else
                    DrawLineProjected(start, ship.AI.OrbitTarget.Center, Colors.Error(alpha), 2500f);
                DrawWayPointLines(ship, new Color(Color.Lime, alpha));
                return;
            }

            DrawWayPointLines(ship, Colors.WayPoints(alpha));
        }

        public void DrawWayPointLines(Ship ship, Color color)
        {
            if (ship.AI.ActiveWayPoints.Count < 1)
                return;

            Vector2[] waypoints;
            lock (ship.AI.WayPointLocker)
                waypoints = ship.AI.ActiveWayPoints.ToArray();

            DrawLineProjected(ship.Center, waypoints[0], color);

            for (int i = 1; i < waypoints.Length; ++i)
            {
                DrawLineProjected(waypoints[i - 1], waypoints[i], color);
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
            ShieldManager.Draw(view, projection);
        }

        private void DrawPlanetInfo()
        {
            if (LookingAtPlanet || viewState > UnivScreenState.SectorView || viewState < UnivScreenState.ShipView)
                return;
            Vector2 mousePos = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
            Texture2D planetNamePointer = ResourceManager.Texture("UI/planetNamePointer");
            Texture2D icon_fighting_small = ResourceManager.Texture("UI/icon_fighting_small");
            Texture2D icon_spy_small = ResourceManager.Texture("UI/icon_spy_small");
            Texture2D icon_anomaly_small = ResourceManager.Texture("UI/icon_anomaly_small");
            Texture2D icon_troop = ResourceManager.Texture("UI/icon_troop");
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
                        if (string.IsNullOrEmpty(building.EventTriggerUID)) continue;
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

        public void DrawPointerWithText(Vector2 screenPos, Texture2D planetNamePointer, Color pointerColor, string text,
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

        public void DrawSunModel(Matrix world, Texture2D texture, float scale)
            => DrawTransparentModel(SunModel, world, texture, scale);

        public void DrawTransparentModel(Model model, Matrix world, Texture2D projTex, float scale)
        {
            DrawModelMesh(model, Matrix.CreateScale(scale) * world, view, new Vector3(1f, 1f, 1f), projection, projTex);
            ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
        }

    }
}