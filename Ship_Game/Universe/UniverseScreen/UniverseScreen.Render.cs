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
            beamflashes             .SetCamera(View, Projection);
            explosionParticles      .SetCamera(View, Projection);
            photonExplosionParticles.SetCamera(View, Projection);
            explosionSmokeParticles .SetCamera(View, Projection);
            projectileTrailParticles.SetCamera(View, Projection);
            fireTrailParticles      .SetCamera(View, Projection);
            smokePlumeParticles     .SetCamera(View, Projection);
            fireParticles           .SetCamera(View, Projection);
            engineTrailParticles    .SetCamera(View, Projection);
            flameParticles          .SetCamera(View, Projection);
            SmallflameParticles     .SetCamera(View, Projection);
            sparks                  .SetCamera(View, Projection);
            lightning               .SetCamera(View, Projection);
            flash                   .SetCamera(View, Projection);
            star_particles          .SetCamera(View, Projection);
            neb_particles           .SetCamera(View, Projection);
        }

        Map<PlanetGlow, SubTexture> Glows;

        void RenderBackdrop(SpriteBatch batch)
        {
            if (GlobalStats.DrawStarfield)
                bg.Draw(this, StarField);

            if (GlobalStats.DrawNebulas)
               bg3d.Draw();

            batch.Begin();

            UpdateKnownShipsScreenState();

            lock (GlobalStats.ClickableSystemsLock)
            {
                ClickPlanetList.Clear();
                ClickableSystems.Clear();
            }

            for (int index = 0; index < SolarSystemList.Count; index++)
            {
                SolarSystem solarSystem = SolarSystemList[index];
                if (!Frustum.Contains(solarSystem.Position, solarSystem.Radius))
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
            batch.End();
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
        void DrawSolarSysWithOrbits(SolarSystem sys, Vector2 sysScreenPos)
        {
            sys.Sun.DrawSunMesh(sys, View, Projection);
            //DrawSunMesh(sys, sys.Zrotate);
            //if (sys.Sun.DoubleLayered) // draw second sun layer
            //    DrawSunMesh(sys, sys.Zrotate / -2.0f);

            if (!sys.IsExploredBy(EmpireManager.Player))
                return;

            for (int i = 0; i < sys.PlanetList.Count; i++)
            {
                Planet planet = sys.PlanetList[i];
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
                    new Vector3((float) (index * (double) UniverseSize / 40.0), 0.0f, 0.0f), Projection,
                    View, Matrix.Identity);
                Vector3 vector3_2 = Viewport.Project(
                    new Vector3((float) (index * (double) UniverseSize / 40.0), UniverseSize, 0.0f),
                    Projection, View, Matrix.Identity);
                ScreenManager.SpriteBatch.DrawLine(new Vector2(vector3_1.X, vector3_1.Y),
                    new Vector2(vector3_2.X, vector3_2.Y), new Color(211, 211, 211, 70));
            }
            for (int index = 0; index < 41; ++index)
            {
                Vector3 vector3_1 = Viewport.Project(
                    new Vector3(0.0f, (float) (index * (double) UniverseSize / 40.0), 40f), Projection,
                    View, Matrix.Identity);
                Vector3 vector3_2 = Viewport.Project(
                    new Vector3(UniverseSize, (float) (index * (double) UniverseSize / 40.0), 0.0f),
                    Projection, View, Matrix.Identity);
                ScreenManager.SpriteBatch.DrawLine(new Vector2(vector3_1.X, vector3_1.Y),
                    new Vector2(vector3_2.X, vector3_2.Y), new Color(211, 211, 211, 70));
            }
            ScreenManager.SpriteBatch.End();
        }

        void RenderOverFog(SpriteBatch batch, GameTime gameTime)
        {
            foreach (SolarSystem sys in SolarSystemList)
            {
                if (viewState >= UnivScreenState.SectorView)
                {
                    DrawSolarSystemSectorView(gameTime, sys);
                }
                if (viewState >= UnivScreenState.GalaxyView) // super zoomed out
                {
                    sys.Sun.DrawLowResSun(batch, sys, View, Projection);
                }
            }
        }

        void DrawSolarSystemSectorView(GameTime gameTime, SolarSystem solarSystem)
        {
            if (!Frustum.Contains(solarSystem.Position, 10f))
                return;

            SpriteBatch batch = ScreenManager.SpriteBatch;

            Vector3 vector3_4 =
                Viewport.Project(solarSystem.Position.ToVec3(), Projection, View,
                    Matrix.Identity);
            Vector2 position = new Vector2(vector3_4.X, vector3_4.Y);
            Vector3 vector3_5 =
                Viewport.Project(
                    new Vector3(solarSystem.Position.PointFromAngle(90f, 25000f), 0.0f), Projection,
                    View, Matrix.Identity);
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
                        new Vector3(new Vector2(100000f, 0f) + solarSystem.Position, 0f), Projection, View, Matrix.Identity);
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
                                if (planet.BuildingList[index].EventHere)
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
                        Rectangle rectangle2 = new Rectangle((int) vector2.X, (int) vector2.Y, 15, 15);
                        ScreenManager.SpriteBatch.Draw(
                            ResourceManager.Texture("UI/icon_anomaly_small"), rectangle2, CurrentFlashColor);
                        if (rectangle2.HitTest(pos))
                            ToolTip.CreateTooltip(138);
                        ++num3;
                    }

                    if (solarSystem.HostileForcesPresent(EmpireManager.Player)
                        && solarSystem.IsExploredBy(EmpireManager.Player))
                    {
                        vector2.X += num3 * 20;
                        vector2.Y -= 2f;
                        Rectangle rectangle2 = new Rectangle((int)vector2.X, (int)vector2.Y,
                            ResourceManager.Texture("Ground_UI/Ground_Attack").Width,
                            ResourceManager.Texture("Ground_UI/Ground_Attack").Height);
                        ScreenManager.SpriteBatch.Draw(
                            ResourceManager.Texture("Ground_UI/EnemyHere"), rectangle2, CurrentFlashColor);
                        if (rectangle2.HitTest(pos))
                            ToolTip.CreateTooltip(123);
                        ++num3;

                        if (solarSystem.PlanetList.Any(p => p.Owner == EmpireManager.Player && p.SpaceCombatNearPlanet))
                        {
                            if (num3 == 1 || num3 == 2)
                                vector2.X += 20f;
                            Rectangle rectangle3 = new Rectangle((int)vector2.X, (int)vector2.Y,
                                ResourceManager.Texture("Ground_UI/Ground_Attack").Width,
                                ResourceManager.Texture("Ground_UI/Ground_Attack").Height);
                            ScreenManager.SpriteBatch.Draw(
                                ResourceManager.Texture("Ground_UI/Ground_Attack"), rectangle3, CurrentFlashColor);
                            if (rectangle3.HitTest(pos))
                                ToolTip.CreateTooltip(122);
                        }
                    }
                }
                else
                {
                    int num3 = 0;
                    if (owners.Count == 1)
                    {
                        if (SelectedSystem != solarSystem ||
                            viewState < UnivScreenState.GalaxyView)
                            HelperFunctions.DrawDropShadowText(batch, solarSystem.Name,
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
                            HelperFunctions.DrawDropShadowText(batch,
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
                                if (planet.BuildingList[index].EventHere)
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
                        Rectangle rectangle2 = new Rectangle((int) vector2.X, (int) vector2.Y, 15, 15);
                        ScreenManager.SpriteBatch.Draw(
                            ResourceManager.Texture("UI/icon_anomaly_small"), rectangle2, CurrentFlashColor);
                        if (rectangle2.HitTest(pos))
                            ToolTip.CreateTooltip(138);
                        ++num3;
                    }

                    if (solarSystem.HostileForcesPresent(EmpireManager.Player))
                    {
                        vector2.X += num3 * 20;
                        vector2.Y -= 2f;
                        Rectangle rectangle3 = new Rectangle((int)vector2.X, (int)vector2.Y,
                            ResourceManager.Texture("Ground_UI/Ground_Attack").Width,
                            ResourceManager.Texture("Ground_UI/Ground_Attack").Height);
                        ScreenManager.SpriteBatch.Draw(
                            ResourceManager.Texture("Ground_UI/EnemyHere"), rectangle3, CurrentFlashColor);
                        if (rectangle3.HitTest(pos))
                            ToolTip.CreateTooltip(123);
                        ++num3;


                        if (solarSystem.PlanetList.Any(p => p.Owner == EmpireManager.Player && p.SpaceCombatNearPlanet))
                        {
                            if (num3 == 1 || num3 == 2)
                                vector2.X += 20f;
                            var rectangle2 = new Rectangle((int)vector2.X, (int)vector2.Y,
                                ResourceManager.Texture("Ground_UI/Ground_Attack").Width,
                                ResourceManager.Texture("Ground_UI/Ground_Attack").Height);
                            ScreenManager.SpriteBatch.Draw(
                                ResourceManager.Texture("Ground_UI/Ground_Attack"), rectangle2, CurrentFlashColor);
                            if (rectangle2.HitTest(pos))
                                ToolTip.CreateTooltip(122);
                        }
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
                    ship.RenderThrusters(ref View, ref Projection);
                }
            }
        }

        public void DrawZones(SpriteFont font, string text, ref int cursorY, Color color)
        {
            Vector2 rect = new Vector2(SelectedStuffRect.X, cursorY);
            ScreenManager.SpriteBatch.DrawString(font, text, rect, color);
            cursorY += font.LineSpacing + 2;
        }

        public void DrawShipAOAndTradeRoutes()
        {
            if (DefiningAO && Input.LeftMouseDown)
                DrawRectangleProjected(AORect, Color.Orange);

            if ((DefiningAO || DefiningTradeRoutes) && SelectedShip != null)
            {
                string title  = DefiningAO ? Localizer.Token(1411) + " (ESC to exit)" : Localizer.Token(1943);
                int cursorY   = 100;
                int numAo     = SelectedShip.AreaOfOperation.Count;
                int numRoutes = SelectedShip.TradeRoutes.Count;

                DrawZones(Fonts.Pirulen20, title, ref cursorY, Color.Red);
                if (numAo > 0)
                    DrawZones(Fonts.Pirulen16, $"Current Area of Operation Number: {numAo}", ref cursorY, Color.Pink);

                if (numRoutes > 0)
                    DrawZones(Fonts.Pirulen16, $"Current list of planets in trade route: {numRoutes}", ref cursorY, Color.White);

                foreach (Rectangle ao in SelectedShip.AreaOfOperation)
                    DrawRectangleProjected(ao, Color.Red, new Color(Color.Red, 50));

                // Draw Specific Trade Routes to planets
                if (SelectedShip.IsFreighter)
                {
                    foreach (Guid planetGuid in SelectedShip.TradeRoutes)
                    {
                        Planet planet = GetPlanet(planetGuid);
                        if (planet.Owner != null)
                        {
                            DrawLineToPlanet(SelectedShip.Center, planet.Center, planet.Owner.EmpireColor);
                            DrawZones(Fonts.Arial14Bold, $"- {planet.Name}", ref cursorY, planet.Owner.EmpireColor);
                        }
                    }
                }
            }
            else
            {
                DefiningAO          = false;
                DefiningTradeRoutes = false;
            }
        }

        // Deferred SceneObject loading jobs use a double buffered queue.
        readonly Array<Ship> SceneObjFrontQueue = new Array<Ship>(32);
        readonly Array<Ship> SceneObjBackQueue  = new Array<Ship>(32);

        public void QueueSceneObjectCreation(Ship ship)
        {
            lock (SceneObjFrontQueue)
            {
                SceneObjFrontQueue.Add(ship);
            }
        }

        // Only create ship scene objects on the main UI thread
        void CreateShipSceneObjects()
        {
            lock (SceneObjFrontQueue)
            {
                SceneObjBackQueue.AddRange(SceneObjFrontQueue);
                SceneObjFrontQueue.Clear();
            }

            for (int i = SceneObjBackQueue.Count - 1; i >= 0; --i)
            {
                Ship ship = SceneObjBackQueue[i];
                if (!ship.Active) // dead or removed
                {
                    SceneObjBackQueue.RemoveAtSwapLast(i);
                }
                else if (ship.GetSO() != null) // already created
                {
                    SceneObjBackQueue.RemoveAtSwapLast(i);
                }
                else if (ship.inSensorRange && viewState <= UnivScreenState.SystemView
                     && (ship.System == null || ship.System.isVisible) 
                     && Frustum.Contains(ship.Center, 2000f))
                {
                    ship.CreateSceneObject();
                    SceneObjBackQueue.RemoveAtSwapLast(i);
                }
                // else: we keep it in the back queue until it dies or comes into frustum
            }
        }

        void Render(SpriteBatch batch, GameTime gameTime)
        {
            if (Frustum == null)
                Frustum = new BoundingFrustum(View * Projection);
            else
                Frustum.Matrix = View * Projection;

            CreateShipSceneObjects();
            ScreenManager.BeginFrameRendering(gameTime, ref View, ref Projection);

            RenderBackdrop(batch);

            batch.Begin();
            DrawShipAOAndTradeRoutes();
            SelectShipLinesToDraw();
            batch.End();

            DrawBombs();
            ScreenManager.RenderSceneObjects();

            if (viewState < UnivScreenState.SectorView)
            {
                using (player.KnownShips.AcquireReadLock())
                    for (int j = player.KnownShips.Count - 1; j >= 0; j--)
                    {
                        Ship ship = player.KnownShips[j];
                        if (ship?.InFrustum != true || ship?.Active != true)
                            continue;
                        ship.DrawDroneBeams(this);
                        ship.DrawBeams(this);                        
                    }
            }

            var rs = ScreenManager.GraphicsDevice.RenderState;
            rs.DepthBufferWriteEnable = true;
            rs.SourceBlend = Blend.SourceAlpha;
            rs.DestinationBlend = Blend.One;
            for (int x = 0; x < anomalyManager.AnomaliesList.Count; x++)
            {
                Anomaly anomaly = anomalyManager.AnomaliesList[x];
                anomaly.Draw();
            }
            DrawSolarSystemsClose();
            rs.AlphaBlendEnable = true;
            rs.SourceBlend = Blend.SourceAlpha;
            rs.DestinationBlend = Blend.One;
            rs.DepthBufferWriteEnable = false;
            rs.CullMode = CullMode.None;
            if (viewState < UnivScreenState.SectorView)
            {
                RenderThrusters();
                RenderParticles();

                DrawWarpFlash();

                beamflashes.Draw();
                explosionParticles.Draw();
                photonExplosionParticles.Draw();
                explosionSmokeParticles.Draw();
                projectileTrailParticles.Draw();
                fireTrailParticles.Draw();
                smokePlumeParticles.Draw();
                fireParticles.Draw();
                engineTrailParticles.Draw();
                star_particles.Draw();
                neb_particles.Draw();
                flameParticles.Draw();
                SmallflameParticles.Draw();
                sparks.Draw();
                lightning.Draw();
                flash.Draw();
            }
            if (!Paused) // Particle pools need to be updated
            {
                float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
                beamflashes.Update(deltaTime);
                explosionParticles.Update(deltaTime);
                photonExplosionParticles.Update(deltaTime);
                explosionSmokeParticles.Update(deltaTime);
                projectileTrailParticles.Update(deltaTime);
                fireTrailParticles.Update(deltaTime);
                smokePlumeParticles.Update(deltaTime);
                fireParticles.Update(deltaTime);
                engineTrailParticles.Update(deltaTime);
                star_particles.Update(deltaTime);
                neb_particles.Update(deltaTime);
                flameParticles.Update(deltaTime);
                SmallflameParticles.Update(deltaTime);
                sparks.Update(deltaTime);
                lightning.Update(deltaTime);
                flash.Update(deltaTime);
            }
            ScreenManager.EndFrameRendering();
            if (viewState < UnivScreenState.SectorView)
                DrawShields();
            rs.DepthBufferWriteEnable = true;
        }

        private void DrawWarpFlash()
        {
            FTLManager.DrawFTLModels(ScreenManager.SpriteBatch, this);
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
                            DrawClouds(xnaPlanetModel, p.CloudMatrix, p);
                            DrawAtmo(xnaPlanetModel, p.CloudMatrix, p);
                        }
                        if (p.HasRings)
                            DrawRings(p.RingWorld, p.Scale);
                    }
                }
            }
        }

        private void SelectShipLinesToDraw()
        {
            byte alpha = (byte)Math.Max(0f, 150f * SelectedSomethingTimer / 3f);
            if (alpha > 0)
            {
                if (SelectedShip != null &&
                    (Debug || !player.DifficultyModifiers.HideTacticalData
                           || !player.IsEmpireAttackable(SelectedShip.loyalty)))
                {
                    DrawShipGoalsAndWayPoints(SelectedShip, alpha);
                }
                else if (SelectedShipList.Count > 0)
                {
                    for (int i = 0; i < SelectedShipList.Count; ++i)
                    {
                        Ship ship = SelectedShipList[i];
                        if (!player.IsEmpireAttackable(ship.loyalty) || Debug)
                            DrawShipGoalsAndWayPoints(ship, alpha);
                    }
                }
            }
        }
    }
}