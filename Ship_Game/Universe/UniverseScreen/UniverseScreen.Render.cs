using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDGraphics.Sprites;
using SDUtils;
using Ship_Game.Empires.Components;
using Ship_Game.Graphics;
using Ship_Game.Ships;
using Vector3 = SDGraphics.Vector3;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        public void DrawStarField(SpriteRenderer sr)
        {
            if (GlobalStats.DrawStarfield)
            {
                bg?.Draw(sr);
            }
        }

        public void DrawNebulae(SpriteRenderer sr)
        {
            if (GlobalStats.DrawNebulas)
            {
                bg3d?.Draw(sr);
            }
        }

        void RenderBackdrop(SpriteRenderer sr, SpriteBatch batch)
        {
            BackdropPerf.Start();

            DrawStarField(sr);
            DrawNebulae(sr);

            batch.SafeBegin();

            // if we're zoomed in enough, display solar system overlays with orbits
            if (viewState < UnivScreenState.GalaxyView)
            {
                DrawSolarSystemsWithOrbits();
            }

            batch.SafeEnd();

            BackdropPerf.Stop();
        }

        void DrawSolarSystemsWithOrbits()
        {
            for (int i = 0; i < UState.Systems.Count; i++)
            {
                SolarSystem solarSystem = UState.Systems[i];
                if (IsInFrustum(solarSystem.Position, solarSystem.Radius))
                {
                    ProjectToScreenCoords(solarSystem.Position, 4500f, out Vector2d sysScreenPos, out double sysScreenPosDisToRight);
                    Vector2 screenPos = sysScreenPos.ToVec2f();
                    DrawSolarSysWithOrbits(solarSystem, screenPos);
                }
            }
        }

        // This draws the hi-res 3D sun and orbital circles
        void DrawSolarSysWithOrbits(SolarSystem sys, Vector2 sysScreenPos)
        {
            RenderStates.BasicBlendMode(Device, additive:false, depthWrite:true);
            sys.Sun.DrawSunMesh(sys, View, Projection);
            //DrawSunMesh(sys, sys.Zrotate);
            //if (sys.Sun.DoubleLayered) // draw second sun layer
            //    DrawSunMesh(sys, sys.Zrotate / -2.0f);

            if (!sys.IsExploredBy(Player))
                return;

            for (int i = 0; i < sys.PlanetList.Count; i++)
            {
                Planet planet = sys.PlanetList[i];
                Vector2 planetScreenPos = ProjectToScreenPosition(planet.Position3D).ToVec2f();
                float planetOrbitRadius = sysScreenPos.Distance(planetScreenPos);

                if (viewState > UnivScreenState.ShipView && !IsCinematicModeEnabled)
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

        void DrawColoredBordersRT(SpriteBatch batch)
        {
            DrawOverFog.Start();
            if (viewState >= UnivScreenState.SectorView) // draw colored empire borders only if zoomed out
            {
                batch.SafeBegin(SpriteBlendMode.AlphaBlend);
                // set the alpha value depending on camera height
                int maxAlpha = 70;
                double relHeight = CamPos.Z / 1800000.0;
                int alpha = (int)(maxAlpha * relHeight);
                if (alpha > maxAlpha) alpha = maxAlpha;
                else if (alpha < 10) alpha = 0;

                var color = new Color(255, 255, 255, (byte)alpha);
                batch.Draw(BorderRT.GetTexture(), new Rectangle(0, 0, ScreenWidth, ScreenHeight), color);
                batch.SafeEnd();
            }
            DrawOverFog.Stop();
        }

        void DrawSolarSystems(SpriteBatch batch)
        {
            foreach (SolarSystem sys in UState.Systems)
            {
                if (viewState >= UnivScreenState.GalaxyView) // super zoomed out
                {
                    sys.Sun.DrawLowResSun(batch, sys, View, Projection);
                }
                if (viewState >= UnivScreenState.SectorView)
                {
                    DrawSolarSystemNames(batch, sys);
                }
            }
        }

        void DrawSystemThreatIndicators(SpriteBatch batch)
        {
            if (viewState > UnivScreenState.SectorView)
            {
                DrawEnemiesDetectedByProjectors(batch);
                DrawSystemThreatCirclesAnimation(batch);
            }
        }

        void DrawEnemiesDetectedByProjectors(SpriteBatch batch)
        {
            var enemies = UState.GetEnemies(Player);
            var playerProjectors = Player.OwnedProjectors;
            for (int i = 0; i < playerProjectors.Count; i++)
            {
                Ship projector = playerProjectors[i];
                int spacing = 1;
                for (int j = 0; j < enemies.Count; j++)
                {
                    var enemy = enemies[j];
                    if (projector.PlayerProjectorHasSeenEmpires.KnownBy(enemy))
                    {
                        var screenPos = ProjectToScreenPosition(projector.Position);
                        var flag = enemy.data.Traits.FlagIndex;
                        int xPos = (int)screenPos.X + (15 + GlobalStats.IconSize) * spacing;
                        var rectangle2 = new RectF(xPos, (int)screenPos.Y, 15 + GlobalStats.IconSize, 15 + GlobalStats.IconSize);
                        batch.Draw(ResourceManager.Flag(flag), rectangle2, ApplyCurrentAlphaToColor(enemy.EmpireColor));
                        spacing++;
                    }
                }
            }
        }

        void DrawSystemThreatCirclesAnimation(SpriteBatch batch)
        {
            var red = new Color(Color.Red, 80);
            var orange = new Color(Color.Orange, 80);
            var black = new Color(Color.Black, 40);

            foreach (IncomingThreat threat in Player.SystemsWithThreat)
            {
                DrawThreat(red, threat);
            }

            foreach (IncomingThreat threat in Player.AlliedSystemsWithThreat())
            {
                DrawThreat(orange, threat);
            }

            void DrawThreat(Color color, IncomingThreat threat)
            {
                var system = threat.TargetSystem;
                float pulseRad = PulseTimer * (system.Radius * 1.5f);

                Vector2d posOnScreen = ProjectToScreenPosition(system.Position);
                batch.DrawCircle(posOnScreen, ProjectToScreenSize(pulseRad), color, 10);
                batch.DrawCircle(posOnScreen, ProjectToScreenSize(pulseRad * 1.001f), black, 5);
                batch.DrawCircle(posOnScreen, ProjectToScreenSize(pulseRad * 1.3f), color, 10);
                batch.DrawCircle(posOnScreen, ProjectToScreenSize(pulseRad * 1.301f), black, 5);
            }
        }

        void DrawSolarSystemNames(SpriteBatch batch, SolarSystem sys)
        {
            if (!Debug && !sys.IsExploredBy(Player))
                return;
            if (!IsInFrustum(sys.Position, 10f))
                return;

            if (Debug)
            {
                sys.SetExploredBy(Player);
                foreach (Planet planet in sys.PlanetList)
                    planet.SetExploredBy(Player);
            }

            Vector2d solarSysPos = ProjectToScreenPosition(sys.Position.ToVec3());
            Vector2 sysPos = solarSysPos.ToVec2f();

            float solarSysRadius = sys.RingList.NotEmpty ? sys.RingList.Last.OrbitalDistance + 7500 : sys.Radius;
            if (viewState > UnivScreenState.SectorView)
                solarSysRadius = 35_000f;

            Vector2d edge = ProjectToScreenPosition(new Vector3(sys.Position + new Vector2(solarSysRadius, 0f)));
            float radiusOnScreen = (float)solarSysPos.Distance(edge);
            sysPos.Y += radiusOnScreen;

            if (viewState <= UnivScreenState.SectorView)
            {
                DrawCircle(solarSysPos, radiusOnScreen, Colors.TransparentDarkGray);
            }

            sysPos.X -= SolarsystemOverlay.SysFont.TextWidth(sys.Name) / 2f;

            DrawSolarSystemName(batch, sys, sysPos);
            DrawSolarSystemAnomalyAndDangerIcons(batch, sys, sysPos);
        }

        void DrawSolarSystemName(SpriteBatch batch, SolarSystem sys, Vector2 sysPos)
        {
            Array<Empire> owners = sys.GetKnownOwners(Player);

            if (owners.Count == 0)
            {
                batch.DrawOutlineText(sys.Name, sysPos, SolarsystemOverlay.SysFont, Color.Gray, Color.Black, 1f);
            }
            else if (owners.Count == 1)
            {
                batch.DrawOutlineText(sys.Name, sysPos, SolarsystemOverlay.SysFont, owners.First.EmpireColor, Color.Black, 1f);
            }
            else
            {
                // multi-color solar system name
                int num4 = sys.Name.Length / owners.Count;
                int i = 0;
                for (int j = 0; j < sys.Name.Length; ++j)
                {
                    if (j + 1 > num4 + num4 * i)
                        ++i;
                    string namePart = sys.Name[j].ToString();
                    Empire current = owners.Count > i ? owners[i] : owners.Last;
                    batch.DrawOutlineText(namePart, sysPos, SolarsystemOverlay.SysFont, current.EmpireColor, Color.Black, 1f);
                    sysPos.X += SolarsystemOverlay.SysFont.TextWidth(namePart);
                }
            }
        }

        void DrawSolarSystemAnomalyAndDangerIcons(SpriteBatch batch, SolarSystem sys, Vector2 sysPos)
        {
            sysPos.Y -= 3f;
            sysPos.X += SolarsystemOverlay.SysFont.TextWidth(sys.Name) + 6f;

            bool isAnomalyHere = sys.IsAnomalyOnAnyKnownPlanets(Player);
            if (isAnomalyHere)
            {
                var anomalyHere = ResourceManager.Texture("UI/icon_anomaly_small");
                var anomalyRect = new RectF(sysPos.X, sysPos.Y, anomalyHere.Width, anomalyHere.Height);
                sysPos.X += 20f;

                batch.Draw(anomalyHere, anomalyRect, CurrentFlashColor);
                if (anomalyRect.HitTest(Input.CursorPosition))
                    ToolTip.CreateTooltip(GameText.IndicatesThatAnAnomalyHas);
            }

            if (sys.IsResearchable && !sys.IsResearchStationDeployedBy(Player) || sys.IsAnyKnownPlanetCanBeResearched(Player))
            {
                var research_icon = ResourceManager.Texture("NewUI/icon_science");
                var researchRect = new RectF(sysPos.X, sysPos.Y, research_icon.Width, research_icon.Height);
                sysPos.X += 20f;

                batch.Draw(research_icon, researchRect, CurrentFlashColor);
                if (researchRect.HitTest(Input.CursorPosition))
                    ToolTip.CreateTooltip(sys.IsResearchable ? GameText.ResearchStationCanBePlacedSystem 
                                                              : GameText.ResearchStationCanBePlaced);
            }

            if (sys.IsAnyKnownPlanetCanBeMined(Player))
            {
                var mining_icon = Mineable.Icon;
                var miningRect = new RectF(sysPos.X, sysPos.Y, mining_icon.Width, mining_icon.Height);
                sysPos.X += 20f;

                batch.Draw(mining_icon, miningRect, CurrentFlashColor);
                if (miningRect.HitTest(Input.CursorPosition))
                    ToolTip.CreateTooltip(GameText.MiningStationsCanBePlaced);
            }

            if (Player.KnownEnemyStrengthIn(sys) > 0f)
            {
                var enemyHere = ResourceManager.Texture("Ground_UI/EnemyHere");
                var enemyRect = new RectF(sysPos.X, sysPos.Y, enemyHere.Width, enemyHere.Height);
                sysPos.X += 20f;

                batch.Draw(enemyHere, enemyRect, CurrentFlashColor);
                if (enemyRect.HitTest(Input.CursorPosition))
                    ToolTip.CreateTooltip(GameText.IndicatesThatHostileForcesWere);

                if (sys.HasPlanetsOwnedBy(Player) && sys.PlanetList.Any(p => p.SpaceCombatNearPlanet))
                {
                    var battleHere = ResourceManager.Texture("Ground_UI/Ground_Attack");
                    var battleRect = new RectF(sysPos.X, sysPos.Y, battleHere.Width, battleHere.Height);
                    sysPos.X += 20f;

                    batch.Draw(battleHere, battleRect, CurrentFlashColor);
                    if (battleRect.HitTest(Input.CursorPosition))
                        ToolTip.CreateTooltip(GameText.IndicatesThatSpaceCombatIs);
                }
            }
        }

        void RenderThrusters()
        {
            if (viewState > UnivScreenState.ShipView)
                return;

            Ship[] ships = UState.Objects.VisibleShips;
            for (int i = 0; i < ships.Length; ++i)
            {
                Ship ship = ships[i];
                if (ship.InPlayerSensorRange)
                {
                    ship.RenderThrusters(ref View, ref Projection);
                }
            }
        }

        public void DrawZones(Graphics.Font font, string text, ref int cursorY, Color color)
        {
            Vector2 rect = new Vector2(SelectedStuffRect.X, cursorY);
            ScreenManager.SpriteBatch.DrawString(font, text, rect, color);
            cursorY += font.LineSpacing + 2;
        }

        public void DrawShipAOAndTradeRoutes()
        {
            if (DefiningAO && Input.LeftMouseDown)
                DrawRectangleProjected(new RectF(AORect), Color.Orange);

            if ((DefiningAO || DefiningTradeRoutes) && SelectedShip != null)
            {
                string title  = DefiningAO ? Localizer.Token(GameText.AssignAreaOfOperation) + " (ESC to exit)" : Localizer.Token(GameText.AssignPlanetsToTradeRoute);
                int cursorY   = 100;
                int numAo     = SelectedShip.AreaOfOperation.Count;
                int numRoutes = SelectedShip.TradeRoutes.Count;

                DrawZones(Fonts.Pirulen20, title, ref cursorY, Color.Red);
                if (numAo > 0)
                    DrawZones(Fonts.Pirulen16, $"Current Area of Operation Number: {numAo}", ref cursorY, Color.Pink);

                if (numRoutes > 0)
                    DrawZones(Fonts.Pirulen16, $"Current list of planets in trade route: {numRoutes}", ref cursorY, Color.White);

                foreach (Rectangle ao in SelectedShip.AreaOfOperation)
                    DrawRectangleProjected(new RectF(ao), Color.Red, new Color(Color.Red, 50));

                // Draw Specific Trade Routes to planets
                if (SelectedShip.IsFreighter)
                {
                    foreach (int planetId in SelectedShip.TradeRoutes)
                    {
                        Planet planet = UState.GetPlanet(planetId);
                        if (planet.Owner != null)
                        {
                            DrawLineToPlanet(SelectedShip.Position, planet.Position, planet.Owner.EmpireColor);
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
                else if (ship.IsVisibleToPlayer)
                {
                    ship.CreateSceneObject();
                    SceneObjBackQueue.RemoveAtSwapLast(i);
                }
                // else: we keep it in the back queue until it dies or comes into frustum
            }
        }

        // @return TRUE if Circle(posInWorld, radiusInWorld) overlaps the screen Frustum
        public bool IsInFrustum(in Vector2 posInWorld, float radiusInWorld)
        {
            return Frustum.Contains(new BoundingSphere(new Vector3(posInWorld, 0f), radiusInWorld))
                != ContainmentType.Disjoint; // Disjoint: no intersection at all
        }

        // @return TRUE if Circle(posInWorld, radiusInWorld) overlaps the screen Frustum
        public bool IsInFrustum(in Vector3 posInWorld, float radiusInWorld)
        {
            return Frustum.Contains(new BoundingSphere(posInWorld, radiusInWorld))
                != ContainmentType.Disjoint; // Disjoint: no intersection at all
        }

        void Render(SpriteRenderer sr, SpriteBatch batch, DrawTimes elapsed)
        {
            if (IsDisposed)
                return;

            RenderGroupTotalPerf.Start();

            Frustum.Matrix = ViewProjection;

            CreateShipSceneObjects();

            BeginSunburnPerf.Start();
            ScreenManager.BeginFrameRendering(elapsed, ref View, ref Projection);
            BeginSunburnPerf.Stop();

            RenderBackdrop(sr, batch);

            RenderStates.BasicBlendMode(Device, additive:false, depthWrite:true);
            RenderStates.EnableMultiSampleAA(Device);

            batch.SafeBegin();
            DrawShipAOAndTradeRoutes();
            SelectShipLinesToDraw();
            batch.SafeEnd();

            DrawBombs();

            SunburnDrawPerf.Start();
            {
                ScreenManager.RenderSceneObjects();
            }
            SunburnDrawPerf.Stop();

            DrawAnomalies(sr);
            DrawPlanets();

            EndSunburnPerf.Start();
            {
                ScreenManager.EndFrameRendering();
            }
            EndSunburnPerf.Stop();

            // render shield and particle effects after Sunburn 3D models
            DrawShields();
            DrawAndUpdateParticles(elapsed, Device);
            DrawExplosions(batch);
            DrawOverlayShieldBubbles(batch);

            RenderGroupTotalPerf.Stop();

            RenderStates.EnableDepthWrite(Device);
        }

        private void DrawAnomalies(SpriteRenderer sr)
        {
            if (anomalyManager == null || anomalyManager.AnomaliesList.Count == 0)
                return;

            RenderStates.BasicBlendMode(sr.Device, additive:true, depthWrite:true);
            sr.Begin(ViewProjection);

            for (int i = 0; i < anomalyManager.AnomaliesList.Count; i++)
            {
                Anomaly anomaly = anomalyManager.AnomaliesList[i];
                anomaly.Draw(sr);
            }

            sr.End();
        }

        private void DrawAndUpdateParticles(DrawTimes elapsed, GraphicsDevice device)
        {
            DrawParticles.Start();

            RenderStates.BasicBlendMode(device, additive:true, depthWrite:false);

            if (viewState < UnivScreenState.SectorView)
            {
                RenderThrusters();
                FTLManager.DrawFTLModels(ScreenManager.SpriteBatch, this);
            }

            Particles.Draw(View, Projection, nearView: viewState < UnivScreenState.SectorView);

            if (!UState.Paused) // Particle pools need to be updated
            {
                Particles.Update(CurrentSimTime);
            }

            DrawParticles.Stop();
        }

        private void DrawPlanets()
        {
            DrawPlanetsPerf.Start();
            if (viewState < UnivScreenState.SectorView)
            {
                var r = ResourceManager.Planets.Renderer;
                r.BeginRendering(Device, CamPos.ToVec3f(), View, Projection);

                foreach (SolarSystem system in UState.Systems)
                {
                    if (system.InFrustum)
                    {
                        foreach (Planet p in system.PlanetList)
                            if (p.InFrustum)
                                r.Render(p);
                    }
                }

                r.EndRendering();
            }
            DrawPlanetsPerf.Stop();
        }

        private void SelectShipLinesToDraw()
        {
            byte alpha = (byte)Math.Max(0f, 150f * SelectedSomethingTimer / 3f);
            if (alpha > 0)
            {
                if (SelectedShip != null && (Debug
                                             || SelectedShip.Loyalty.isPlayer
                                             || !Player.DifficultyModifiers.HideTacticalData 
                                             || Player.IsAlliedWith(SelectedShip.Loyalty)
                                             || SelectedShip.AI.Target != null))
                {
                    DrawShipGoalsAndWayPoints(SelectedShip, alpha);
                }
                else 
                {
                    for (int i = 0; i < SelectedShipList.Count; ++i)
                    {
                        Ship ship = SelectedShipList[i];
                        if (ship.Loyalty.isPlayer
                            || Player.IsAlliedWith(ship.Loyalty)
                            || Debug
                            || !Player.DifficultyModifiers.HideTacticalData
                            || ship.AI.Target != null)
                        {
                            DrawShipGoalsAndWayPoints(ship, alpha);
                        }
                    }
                }
            }
        }
    }
}
