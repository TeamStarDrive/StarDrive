using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        private void RenderParticles()
        {
            beamflashes             .SetCamera(view, projection);
            explosionParticles      .SetCamera(view, projection);
            photonExplosionParticles.SetCamera(view, projection);
            explosionSmokeParticles .SetCamera(view, projection);
            projectileTrailParticles.SetCamera(view, projection);
            fireTrailParticles      .SetCamera(view, projection);
            smokePlumeParticles     .SetCamera(view, projection);
            fireParticles           .SetCamera(view, projection);
            engineTrailParticles    .SetCamera(view, projection);
            flameParticles          .SetCamera(view, projection);
            SmallflameParticles     .SetCamera(view, projection);
            sparks                  .SetCamera(view, projection);
            lightning               .SetCamera(view, projection);
            flash                   .SetCamera(view, projection);
            star_particles          .SetCamera(view, projection);
            neb_particles           .SetCamera(view, projection);
        }

        Map<PlanetGlow, SubTexture> Glows = new Map<PlanetGlow, SubTexture>(new []
        {
            (PlanetGlow.Terran, ResourceManager.Texture("PlanetGlows/Glow_Terran")),
            (PlanetGlow.Red,    ResourceManager.Texture("PlanetGlows/Glow_Red")),
            (PlanetGlow.White,  ResourceManager.Texture("PlanetGlows/Glow_White")),
            (PlanetGlow.Aqua,   ResourceManager.Texture("PlanetGlows/Glow_Aqua")),
            (PlanetGlow.Orange, ResourceManager.Texture("PlanetGlows/Glow_Orange"))
        });

        private void RenderBackdrop()
        {
            if (GlobalStats.DrawStarfield)
                bg.Draw(this, StarField);

            if (GlobalStats.DrawNebulas)
               bg3d.Draw();

            ScreenManager.SpriteBatch.Begin();

            UpdateKnownShipsScreenState();

            lock (GlobalStats.ClickableSystemsLock)
            {
                ClickPlanetList.Clear();
                ClickableSystems.Clear();
            }

            for (int index = 0; index < SolarSystemList.Count; index++)
            {
                SolarSystem solarSystem = SolarSystemList[index];
                if (!Frustum.Contains(solarSystem.Position, 100000f))
                    continue;

                ProjectToScreenCoords(solarSystem.Position, 4500f, out Vector2 sysScreenPos,
                    out float sysScreenPosDisToRight);

                lock (GlobalStats.ClickableSystemsLock)
                {
                    ClickableSystems.Add(new ClickableSystem
                    {
                        Radius = sysScreenPosDisToRight < 8f ? 8f : sysScreenPosDisToRight,
                        ScreenPos = sysScreenPos,
                        systemToClick = solarSystem
                    });
                }
                if (viewState <= UnivScreenState.SectorView)
                {
                    if (solarSystem.IsExploredBy(EmpireManager.Player))
                    {
                        for (int i = 0; i < solarSystem.PlanetList.Count; i++)
                        {
                            DrawPlanetInSectorView(solarSystem.PlanetList[i]);
                        }
                    }
                }
                if (viewState < UnivScreenState.GalaxyView)
                {
                    DrawSolarSysWithOrbits(solarSystem, sysScreenPos);
                }
            }

            ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            var rs = ScreenManager.GraphicsDevice.RenderState;
            rs.AlphaBlendEnable = true;
            rs.AlphaBlendOperation = BlendFunction.Add;
            rs.SourceBlend = Blend.SourceAlpha;
            rs.DestinationBlend = Blend.One;
            rs.DepthBufferWriteEnable = false;
            rs.CullMode = CullMode.None;
            rs.DepthBufferWriteEnable = true;
            rs.MultiSampleAntiAlias = true;            
            ScreenManager.SpriteBatch.End();
        }

        void UpdateKnownShipsScreenState()
        {
            var viewport = new Rectangle(0, 0, ScreenWidth, ScreenHeight);

            ClickableShipsList.Clear();

            using (player.KnownShips.AcquireReadLock())
            {
                for (int i = 0; i < player.KnownShips.Count; i++)
                {
                    Ship ship = player.KnownShips[i];
                    if (ship == null || !ship.Active ||
                        (viewState == UnivScreenState.GalaxyView && ship.IsPlatform))
                        continue;

                    ProjectToScreenCoords(ship.Position, ship.Radius,
                        out Vector2 shipScreenPos, out float screenRadius);

                    if (viewport.HitTest(shipScreenPos))
                    {
                        if (screenRadius < 7.0f) screenRadius = 7f;
                        ship.ScreenRadius = screenRadius;
                        ship.ScreenPosition = shipScreenPos;
                        ClickableShipsList.Add(new ClickableShip
                        {
                            Radius = screenRadius,
                            ScreenPos = shipScreenPos,
                            shipToClick = ship
                        });
                    }
                    else
                    {
                        ship.ScreenPosition = new Vector2(-1f, -1f);
                    }
                }
            }
        }

        // This draws the hi-res 3D sun and orbital circles
        void DrawSolarSysWithOrbits(SolarSystem solarSystem, Vector2 sysScreenPos)
        {
            SubTexture sunTexture = solarSystem.SunTexture;

            DrawTransparentModel(SunModel,
                Matrix.CreateRotationZ(Zrotate) *
                Matrix.CreateTranslation(solarSystem.Position.ToVec3()), sunTexture, 10.0f);

            DrawTransparentModel(SunModel,
                Matrix.CreateRotationZ((float)(-Zrotate / 2.0)) *
                Matrix.CreateTranslation(solarSystem.Position.ToVec3()), sunTexture, 10.0f);

            if (!solarSystem.IsExploredBy(EmpireManager.Player))
                return;

            for (int i = 0; i < solarSystem.PlanetList.Count; i++)
            {
                Planet planet = solarSystem.PlanetList[i];
                Vector2 planetScreenPos = ProjectToScreenPosition(planet.Center, 2500f);
                float planetOrbitRadius = sysScreenPos.Distance(planetScreenPos);

                if (viewState > UnivScreenState.ShipView)
                {
                    var transparentDarkGray = new Color(50, 50, 50, 90);
                    DrawCircle(sysScreenPos, planetOrbitRadius, transparentDarkGray, 3f);

                    if (planet.Owner == null)
                    {
                        DrawCircle(sysScreenPos, planetOrbitRadius, transparentDarkGray, 3f);
                    }
                    else
                    {
                        var empireColor = new Color(planet.Owner.EmpireColor, 100);
                        DrawCircle(sysScreenPos, planetOrbitRadius, empireColor, 3f);
                    }
                }
            }
        }

        void DrawPlanetInSectorView(Planet planet)
        {
            ProjectToScreenCoords(planet.Center, 2500f, planet.SO.WorldBoundingSphere.Radius,
                out Vector2 planetScreenPos, out float planetScreenRadius);
            float scale = planetScreenRadius / 115f;

            // atmospheric glow
            if (planet.Type.Glow != PlanetGlow.None)
            {
                SubTexture glow = Glows[planet.Type.Glow];
                ScreenManager.SpriteBatch.Draw(glow, planetScreenPos,
                    Color.White, 0.0f, new Vector2(128f, 128f), scale,
                    SpriteEffects.None, 1f);
            }

            lock (GlobalStats.ClickableSystemsLock)
            {
                ClickPlanetList.Add(new ClickablePlanets
                {
                    ScreenPos = planetScreenPos,
                    Radius = planetScreenRadius < 8f ? 8f : planetScreenRadius,
                    planetToClick = planet
                });
            }
        }

        // @todo This is unused??? Maybe some legacy code?
        void RenderGalaxyBackdrop()
        {
            bg.DrawGalaxyBackdrop(this, StarField);
            ScreenManager.SpriteBatch.Begin();
            for (int index = 0; index < 41; ++index)
            {
                Vector3 vector3_1 = Viewport.Project(
                    new Vector3((float) (index * (double) UniverseSize / 40.0), 0.0f, 0.0f), projection,
                    view, Matrix.Identity);
                Vector3 vector3_2 = Viewport.Project(
                    new Vector3((float) (index * (double) UniverseSize / 40.0), UniverseSize, 0.0f),
                    projection, view, Matrix.Identity);
                ScreenManager.SpriteBatch.DrawLine(new Vector2(vector3_1.X, vector3_1.Y),
                    new Vector2(vector3_2.X, vector3_2.Y), new Color(211, 211, 211, 70));
            }
            for (int index = 0; index < 41; ++index)
            {
                Vector3 vector3_1 = Viewport.Project(
                    new Vector3(0.0f, (float) (index * (double) UniverseSize / 40.0), 40f), projection,
                    view, Matrix.Identity);
                Vector3 vector3_2 = Viewport.Project(
                    new Vector3(UniverseSize, (float) (index * (double) UniverseSize / 40.0), 0.0f),
                    projection, view, Matrix.Identity);
                ScreenManager.SpriteBatch.DrawLine(new Vector2(vector3_1.X, vector3_1.Y),
                    new Vector2(vector3_2.X, vector3_2.Y), new Color(211, 211, 211, 70));
            }
            ScreenManager.SpriteBatch.End();
        }

        void RenderOverFog(SpriteBatch batch, GameTime gameTime)
        {
            foreach (SolarSystem solarSystem in SolarSystemList)
            {
                if (viewState >= UnivScreenState.SectorView)
                {
                    DrawSolarSystemSectorView(gameTime, solarSystem);
                }
                if (viewState >= UnivScreenState.GalaxyView) // super zoomed out
                {
                    DrawLowResSun(batch, solarSystem);
                }
            }
        }

        void DrawLowResSun(SpriteBatch batch, SolarSystem solarSystem)
        {
            float scale = 0.05f;
            Vector2 position = Viewport.Project(solarSystem.Position.ToVec3(), projection, view, Matrix.Identity).ToVec2();

            SubTexture sunTex = solarSystem.SunTexture;
            batch.Draw(sunTex, position, Color.White, Zrotate, sunTex.CenterF, scale, SpriteEffects.None, 0.9f);
            batch.Draw(sunTex, position, Color.White, Zrotate/-2f, sunTex.CenterF, scale, SpriteEffects.None, 0.9f);
        }

        void DrawSolarSystemSectorView(GameTime gameTime, SolarSystem solarSystem)
        {
            if (!Frustum.Contains(solarSystem.Position, 10f))
                return;

            Vector3 vector3_4 =
                Viewport.Project(solarSystem.Position.ToVec3(), projection, view,
                    Matrix.Identity);
            Vector2 position = new Vector2(vector3_4.X, vector3_4.Y);
            Vector3 vector3_5 =
                Viewport.Project(
                    new Vector3(solarSystem.Position.PointOnCircle(90f, 25000f), 0.0f), projection,
                    view, Matrix.Identity);
            float num2 = Vector2.Distance(new Vector2(vector3_5.X, vector3_5.Y), position);
            Vector2 vector2 = new Vector2(position.X, position.Y);
            if ((solarSystem.IsExploredBy(player) || Debug) && SelectedSystem != solarSystem)
            {
                if (Debug)
                {
                    solarSystem.SetExploredBy(player);
                    foreach (Planet planet in solarSystem.PlanetList)
                        planet.SetExploredBy(player);
                }

                Vector3 vector3_6 =
                    Viewport.Project(
                        new Vector3(
                            new Vector2(100000f * GameScaleStatic, 0.0f) +
                            solarSystem.Position, 0.0f), projection, view, Matrix.Identity);
                float radius = Vector2.Distance(new Vector2(vector3_6.X, vector3_6.Y), position);
                if (viewState == UnivScreenState.SectorView)
                {
                    vector2.Y += radius;
                    var transparentDarkGray = new Color(50, 50, 50, 90);
                    DrawCircle(new Vector2(vector3_4.X, vector3_4.Y), radius, transparentDarkGray);
                }
                else
                    vector2.Y += num2;

                vector2.X -= SystemInfoUIElement.SysFont.MeasureString(solarSystem.Name).X / 2f;
                Vector2 pos = Input.CursorPosition;

                Array<Empire> owners = new Array<Empire>();
                bool wellKnown = false;

                foreach (Empire e in solarSystem.OwnerList)
                {
                    EmpireManager.Player.TryGetRelations(e, out Relationship ssRel);
                    wellKnown = Debug || EmpireManager.Player == e || ssRel.Treaty_Alliance;
                    if (wellKnown) break;
                    if (ssRel.Known) // (ssRel.Treaty_Alliance || ssRel.Treaty_Trade || ssRel.Treaty_OpenBorders))
                        owners.Add(e);
                }

                if (wellKnown)
                {
                    owners = solarSystem.OwnerList.ToArrayList();
                }

                if (owners.Count == 0)
                {
                    if (SelectedSystem != solarSystem ||
                        viewState < UnivScreenState.GalaxyView)
                        ScreenManager.SpriteBatch.DrawString(SystemInfoUIElement.SysFont,
                            solarSystem.Name, vector2, Color.Gray);
                    int num3 = 0;
                    --vector2.Y;
                    vector2.X += SystemInfoUIElement.SysFont.MeasureString(solarSystem.Name).X + 6f;
                    bool flag = false;
                    foreach (Planet planet in solarSystem.PlanetList)
                    {
                        if (planet.IsExploredBy(player))
                        {
                            for (int index = 0; index < planet.BuildingList.Count; ++index)
                            {
                                if (!string.IsNullOrEmpty(planet.BuildingList[index].EventTriggerUID))
                                {
                                    flag = true;
                                    break;
                                }
                            }

                            if (flag)
                                break;
                        }
                    }

                    if (flag)
                    {
                        vector2.Y -= 2f;
                        Color color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue,
                            (byte) (Math.Abs((float) Math.Sin(gameTime.TotalGameTime.TotalSeconds)) *
                                    byte.MaxValue));
                        Rectangle rectangle2 = new Rectangle((int) vector2.X, (int) vector2.Y, 15, 15);
                        ScreenManager.SpriteBatch.Draw(
                            ResourceManager.Texture("UI/icon_anomaly_small"), rectangle2, color);
                        if (rectangle2.HitTest(pos))
                            ToolTip.CreateTooltip(138);
                        ++num3;
                    }

                    TimeSpan totalGameTime;
                    if (solarSystem.CombatInSystem)
                    {
                        vector2.X += num3 * 20;
                        vector2.Y -= 2f;
                        totalGameTime = gameTime.TotalGameTime;
                        Color color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue,
                            (byte) (Math.Abs((float) Math.Sin(totalGameTime.TotalSeconds)) *
                                    byte.MaxValue));
                        Rectangle rectangle2 = new Rectangle((int) vector2.X, (int) vector2.Y,
                            ResourceManager.Texture("Ground_UI/Ground_Attack").Width,
                            ResourceManager.Texture("Ground_UI/Ground_Attack").Height);
                        ScreenManager.SpriteBatch.Draw(
                            ResourceManager.Texture("Ground_UI/Ground_Attack"), rectangle2, color);
                        if (rectangle2.HitTest(pos))
                            ToolTip.CreateTooltip(122);
                        ++num3;
                    }

                    if (solarSystem.DangerTimer > 0f)
                    {
                        if (num3 == 1 || num3 == 2)
                            vector2.X += 20f;
                        totalGameTime = gameTime.TotalGameTime;
                        Color color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue,
                            (byte) (Math.Abs((float) Math.Sin(totalGameTime.TotalSeconds)) *
                                    byte.MaxValue));
                        Rectangle rectangle2 = new Rectangle((int) vector2.X, (int) vector2.Y,
                            ResourceManager.Texture("Ground_UI/Ground_Attack").Width,
                            ResourceManager.Texture("Ground_UI/Ground_Attack").Height);
                        ScreenManager.SpriteBatch.Draw(
                            ResourceManager.Texture("Ground_UI/EnemyHere"), rectangle2, color);
                        if (rectangle2.HitTest(pos))
                            ToolTip.CreateTooltip(123);
                    }
                }
                else
                {
                    int num3 = 0;
                    if (owners.Count == 1)
                    {
                        if (SelectedSystem != solarSystem ||
                            viewState < UnivScreenState.GalaxyView)
                            HelperFunctions.DrawDropShadowText(ScreenManager, solarSystem.Name,
                                vector2, SystemInfoUIElement.SysFont,
                                owners.ToList()[0].EmpireColor);
                    }
                    else if (SelectedSystem != solarSystem ||
                             viewState < UnivScreenState.GalaxyView)
                    {
                        Vector2 Pos = vector2;
                        int length = solarSystem.Name.Length;
                        int num4 = length / owners.Count;
                        int index1 = 0;
                        for (int index2 = 0; index2 < length; ++index2)
                        {
                            if (index2 + 1 > num4 + num4 * index1)
                                ++index1;
                            HelperFunctions.DrawDropShadowText(ScreenManager,
                                solarSystem.Name[index2].ToString(), Pos, SystemInfoUIElement.SysFont,
                                owners.Count > index1
                                    ? owners.ToList()[index1].EmpireColor
                                    : (owners).Last()
                                    .EmpireColor);
                            Pos.X += SystemInfoUIElement.SysFont
                                .MeasureString(solarSystem.Name[index2].ToString())
                                .X;
                        }
                    }

                    --vector2.Y;
                    vector2.X += SystemInfoUIElement.SysFont.MeasureString(solarSystem.Name).X + 6f;
                    bool flag = false;
                    foreach (Planet planet in solarSystem.PlanetList)
                    {
                        if (planet.IsExploredBy(player))
                        {
                            for (int index = 0; index < planet.BuildingList.Count; ++index)
                            {
                                if (!string.IsNullOrEmpty(planet.BuildingList[index].EventTriggerUID))
                                {
                                    flag = true;
                                    break;
                                }
                            }

                            if (flag)
                                break;
                        }
                    }

                    if (flag)
                    {
                        vector2.Y -= 2f;
                        Color color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue,
                            (byte) (Math.Abs((float) Math.Sin(gameTime.TotalGameTime.TotalSeconds)) *
                                    byte.MaxValue));
                        Rectangle rectangle2 = new Rectangle((int) vector2.X, (int) vector2.Y, 15, 15);
                        ScreenManager.SpriteBatch.Draw(
                            ResourceManager.Texture("UI/icon_anomaly_small"), rectangle2, color);
                        if (rectangle2.HitTest(pos))
                            ToolTip.CreateTooltip(138);
                        ++num3;
                    }

                    TimeSpan totalGameTime;
                    if (solarSystem.CombatInSystem)
                    {
                        vector2.X += num3 * 20;
                        vector2.Y -= 2f;
                        totalGameTime = gameTime.TotalGameTime;
                        Color color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue,
                            (byte) (Math.Abs((float) Math.Sin(totalGameTime.TotalSeconds)) *
                                    byte.MaxValue));
                        Rectangle rectangle2 = new Rectangle((int) vector2.X, (int) vector2.Y,
                            ResourceManager.Texture("Ground_UI/Ground_Attack").Width,
                            ResourceManager.Texture("Ground_UI/Ground_Attack").Height);
                        ScreenManager.SpriteBatch.Draw(
                            ResourceManager.Texture("Ground_UI/Ground_Attack"), rectangle2, color);
                        if (rectangle2.HitTest(pos))
                            ToolTip.CreateTooltip(122);
                        ++num3;
                    }

                    if (solarSystem.DangerTimer > 0.0)
                    {
                        if (num3 == 1 || num3 == 2)
                            vector2.X += 20f;
                        totalGameTime = gameTime.TotalGameTime;
                        Color color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue,
                            (byte) (Math.Abs((float) Math.Sin(totalGameTime.TotalSeconds)) *
                                    byte.MaxValue));
                        Rectangle rectangle2 = new Rectangle((int) vector2.X, (int) vector2.Y,
                            ResourceManager.Texture("Ground_UI/Ground_Attack").Width,
                            ResourceManager.Texture("Ground_UI/Ground_Attack").Height);
                        ScreenManager.SpriteBatch.Draw(
                            ResourceManager.Texture("Ground_UI/EnemyHere"), rectangle2, color);
                        if (rectangle2.HitTest(pos))
                            ToolTip.CreateTooltip(123);
                    }
                }
            }
            else
                vector2.X -= SystemInfoUIElement.SysFont.MeasureString(solarSystem.Name).X / 2f;
        }


        private void RenderThrusters()
        {
            if (viewState > UnivScreenState.ShipView)
                return;
            using (player.KnownShips.AcquireReadLock())
            for (int i = 0; i < player.KnownShips.Count; ++i)
            {
                Ship ship = player.KnownShips[i];
                if (ship != null && ship.InFrustum && ship.inSensorRange)
                {
                    ship.RenderThrusters(ref view, ref projection);
                }
            }
        }

        public void Render(GameTime gameTime)
        {
            if (Frustum == null)
                Frustum = new BoundingFrustum(view * projection);
            else
                Frustum.Matrix = view * projection;

            ScreenManager.BeginFrameRendering(gameTime, ref view, ref projection);

            RenderBackdrop();
            ScreenManager.SpriteBatch.Begin();
            if (DefiningAO && Input.LeftMouseDown)
            {
                DrawRectangleProjected(AORect, Color.Orange);
            }
            if (DefiningAO && SelectedShip != null)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, Localizer.Token(1411),
                    new Vector2(SelectedStuffRect.X,
                        SelectedStuffRect.Y - Fonts.Pirulen16.LineSpacing - 2), Color.White);
                for (int index = 0; index < SelectedShip.AreaOfOperation.Count; index++)
                {
                    Rectangle rectangle = SelectedShip.AreaOfOperation[index];
                    DrawRectangleProjected(rectangle, Color.Red, new Color(Color.Red, 50));
                }
            }
            else
                DefiningAO = false;
            SelectShipLinesToDraw();
            ScreenManager.SpriteBatch.End();
            DrawBombs();
            ScreenManager.RenderSceneObjects();

            if (viewState < UnivScreenState.SectorView)
            {
                using (player.KnownShips.AcquireReadLock())
                    for (int j = player.KnownShips.Count - 1; j >= 0; j--)
                    {
                        Ship ship = player.KnownShips[j];
                        if (ship?.InFrustum != true || ship?.Active != true) continue;
                        ship.DrawRepairDrones(this);
                        ship.DrawBeams(this);                        
                    }
            }

            var renderState = ScreenManager.GraphicsDevice.RenderState;
            renderState.DepthBufferWriteEnable = true;
            renderState.SourceBlend = Blend.SourceAlpha;
            renderState.DestinationBlend = Blend.One;
            for (int x = 0; x < anomalyManager.AnomaliesList.Count; x++)
            {
                Anomaly anomaly = anomalyManager.AnomaliesList[x];
                anomaly.Draw();
            }
            DrawSolarSystemsClose();
            renderState.AlphaBlendEnable = true;
            renderState.SourceBlend = Blend.SourceAlpha;
            renderState.DestinationBlend = Blend.One;
            renderState.DepthBufferWriteEnable = false;
            renderState.CullMode = CullMode.None;
            if (viewState < UnivScreenState.SectorView)
            {
                RenderThrusters();
                RenderParticles();

                DrawWarpFlash();
                beamflashes.Draw(gameTime);
                explosionParticles.Draw(gameTime);
                photonExplosionParticles.Draw(gameTime);
                explosionSmokeParticles.Draw(gameTime);
                projectileTrailParticles.Draw(gameTime);
                fireTrailParticles.Draw(gameTime);
                smokePlumeParticles.Draw(gameTime);
                fireParticles.Draw(gameTime);
                engineTrailParticles.Draw(gameTime);
                star_particles.Draw(gameTime);
                neb_particles.Draw(gameTime);
                flameParticles.Draw(gameTime);
                SmallflameParticles.Draw(gameTime);
                sparks.Draw(gameTime);
                lightning.Draw(gameTime);
                flash.Draw(gameTime);
            }
            if (!Paused) // Particle pools need to be updated
            {
                beamflashes.Update(gameTime);
                explosionParticles.Update(gameTime);
                photonExplosionParticles.Update(gameTime);
                explosionSmokeParticles.Update(gameTime);
                projectileTrailParticles.Update(gameTime);
                fireTrailParticles.Update(gameTime);
                smokePlumeParticles.Update(gameTime);
                fireParticles.Update(gameTime);
                engineTrailParticles.Update(gameTime);
                star_particles.Update(gameTime);
                neb_particles.Update(gameTime);
                flameParticles.Update(gameTime);
                SmallflameParticles.Update(gameTime);
                sparks.Update(gameTime);
                lightning.Update(gameTime);
                flash.Update(gameTime);
            }
            ScreenManager.EndFrameRendering();
            if (viewState < UnivScreenState.SectorView)
                DrawShields();
            renderState.DepthBufferWriteEnable = true;
        }

        private void DrawWarpFlash()
        {
            FTLManager.DrawFTLModels(this);
            MuzzleFlashManager.Draw(this);
        }

        private void DrawSolarSystemsClose()
        {
            if (viewState >= UnivScreenState.SectorView) return;
            for (int i = 0; i < SolarSystemList.Count; i++)
            {
                SolarSystem solarSystem = SolarSystemList[i];
                if (!solarSystem.IsExploredBy(player)) continue;

                for (int j = 0; j < solarSystem.PlanetList.Count; j++)
                {
                    Planet p = solarSystem.PlanetList[j];
                    if (Frustum.Contains(p.SO.WorldBoundingSphere) != ContainmentType.Disjoint)
                    {
                        if (p.Type.EarthLike)
                        {
                            DrawClouds(xnaPlanetModel, p.CloudMatrix, view, projection, p);
                            DrawAtmo(xnaPlanetModel, p.CloudMatrix, view, projection, p);
                        }
                        if (p.HasRings)
                            DrawRings(p.RingWorld, view, projection, p.Scale);
                    }
                }
            }
        }

        private void SelectShipLinesToDraw()
        {
            float num = (float) (150.0 * SelectedSomethingTimer / 3f);
            if (num < 0f)
                num = 0.0f;
            byte alpha = (byte) num;
            if (alpha > 0)
            {
                if (SelectedShip != null && (Debug || CurrentGame.Difficulty < UniverseData.GameDifficulty.Hard || !player.IsEmpireAttackable(SelectedShip.loyalty)))
                {
                    DrawShipLines(SelectedShip, alpha);
                }
                else if (SelectedShipList.Count > 0)
                {
                    for (int index1 = 0; index1 < SelectedShipList.Count; ++index1)
                    {
                        try
                        {
                            Ship ship = SelectedShipList[index1];
                            if (player.IsEmpireAttackable(SelectedShip?.loyalty) && !Debug) continue;
                            DrawShipLines(ship, alpha);
                        }
                        catch
                        {
                            Log.Warning("DrawShipLines Blew Up");
                        }
                    }
                }
            }
        }
    }
}