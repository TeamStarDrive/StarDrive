using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using SDGraphics;
using SDUtils;
using Ship_Game.Ships.AI;
using Ship_Game.Fleets;
using Ship_Game.Graphics;
using Matrix = SDGraphics.Matrix;
using Vector2 = SDGraphics.Vector2;
using Vector2d = SDGraphics.Vector2d;
using Ship_Game.Universe;
using Rectangle = SDGraphics.Rectangle;
using System.Diagnostics;
using SDGraphics.Sprites;
using SDGraphics.Rendering;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        static readonly Color FleetLineColor    = new Color(255, 255, 255, 20);
        static readonly Vector2 FleetNameOffset = new Vector2(10f, -6f);
        public static float PulseTimer { get; private set; }

        void DrawSystemAndPlanetBrackets(SpriteBatch batch)
        {
            if (SelectedSystem != null && !LookingAtPlanet)
            {
                ProjectToScreenCoords(SelectedSystem.Position, 4500f, out Vector2d sysPos, out double sysRadius);
                if (sysRadius < 5.0)
                    sysRadius = 5.0;
                batch.BracketRectangle(sysPos, sysRadius, Color.White);
            }
            if (SelectedPlanet != null && !LookingAtPlanet &&  viewState < UnivScreenState.GalaxyView)
            {
                ProjectToScreenCoords(SelectedPlanet.Position3D, SelectedPlanet.Radius,
                                      out Vector2d planetPos, out double planetRadius);
                if (planetRadius < 8.0)
                    planetRadius = 8.0;
                batch.BracketRectangle(planetPos, planetRadius, SelectedPlanet.Owner?.EmpireColor ?? Color.Gray);
            }
        }

        // this crucial part draws clear-ish spots in the fogmap
        // giving us clear visibility under sensor coverage
        void DrawSensorNodesHighlights(SpriteBatch batch)
        {
            var uiNode = ResourceManager.Texture("UI/node");
            var sensorNodes = Player.SensorNodes;
            // Phase 3.7 step 3: sensor halo at full white — sensor-covered area
            // shows 100% scene brightness. Falloff to baseline happens naturally
            // through the soft uiNode alpha mask.
            var sensorTint = Color.White;
            for (int i = 0; i < sensorNodes.Length; ++i)
            {
                ref Empire.InfluenceNode node = ref sensorNodes[i];

                ProjectToScreenCoords(node.Position, node.Radius * 2f, out Vector2d nodePos, out double nodeRadius);
                RectF worldRect = RectF.FromPointRadius(nodePos, nodeRadius);
                batch.Draw(uiNode, worldRect, sensorTint, 0f, Vector2.Zero, SpriteEffects.None, 1f);
            }
        }

        // Test changing these in Debug Solar by holding comma/period and using UP/DOWN keys
        internal Blend BorderBlendSrc = Blend.SourceAlphaSaturation; // Blend.InverseDestinationColor;
        internal Blend BorderBlendDest = Blend.One;

        // Draws SSP - Subspace Projector influence
        void DrawColoredEmpireBorders(SpriteRenderer draw3d, GraphicsDevice graphics)
        {
            DrawBorders.Start();

            graphics.SetRenderTarget(BorderRT);
            graphics.Clear(Color.Transparent);

            if (GlobalStats.InfluenceNodeAlpha > 0.1f)
            {
                // the node texture has a smooth fade, so we need to scale it by a lot to match the actual SSP radius
                float nodeScale = 1.8f;
                float connectorScale = 1.2f;
                float currentZ = 0;
                var nodeTex = ResourceManager.Texture("UI/node");
                var connectTex = ResourceManager.Texture("UI/nodeconnect"); // simple horizontal gradient

                var frustum = VisibleWorldRect;

                Empire[] empires = UState.Empires.Sorted(e => e.MilitaryScore);
                foreach (Empire empire in empires)
                {
                    bool canShowBorders = Debug || empire == Player || Player.IsKnown(empire);
                    if (!canShowBorders)
                        continue;

                    empire.BorderNodeCache.Update(empire);

                    Empire.InfluenceNode[] nodes = empire.BorderNodeCache.BorderNodes;
                    if (nodes.Length == 0)
                        continue;

                    draw3d.Begin(ViewProjection);

                    // since we draw every empire's influence in its own layer, depth is not needed
                    // drawing every empire in its own layer will solve almost all artifact issues
                    RenderStates.BasicBlendMode(graphics, additive: false, depthWrite: false);

                    // enable additive only for the alpha channel, this will smoothly blend multiple
                    // overlapping gradient edges into nice blobs
                    RenderStates.EnableSeparateAlphaBlend(graphics, BorderBlendSrc, BorderBlendDest);
                    RenderStates.EnableAlphaTest(graphics, CompareFunction.Greater);
                    //RenderStates.DisableAlphaTest(graphics);

                    Color empireColor = empire.EmpireColor.Alpha(GlobalStats.InfluenceNodeAlpha);
                    for (int x = 0; x < nodes.Length; x++)
                    {
                        ref Empire.InfluenceNode inf = ref nodes[x];
                        if (inf.KnownToPlayer && frustum.Overlaps(inf.Position, inf.Radius))
                        {
                            Quad3D nodeQuad = new(inf.Position, inf.Radius * nodeScale, zValue: currentZ);
                            currentZ += 10f;
                            draw3d.Draw(nodeTex, nodeQuad, empireColor);
                        }
                    }

                    // draw connection bridges
                    // NOTE: all BorderNodeCache.Connections are those which are `KnownToPlayer`
                    foreach (InfluenceConnection c in empire.BorderNodeCache.Connections)
                    {
                        Empire.InfluenceNode a = c.Node1;
                        Empire.InfluenceNode b = c.Node2;
                        if (frustum.Overlaps(a.Position, a.Radius) || frustum.Overlaps(b.Position, b.Radius))
                        {
                            // always use the smaller radius to prevent artifacts when
                            // a really big system connects to a tiny projector
                            float radius = Math.Min(a.Radius, b.Radius);
                            float width = 2.0f * radius * connectorScale;

                            // make a quad by reusing the Quad3D line constructor
                            Quad3D connectLine = new(a.Position, b.Position, width, zValue: currentZ);
                            currentZ += 10f;
                            draw3d.Draw(connectTex, connectLine, empireColor);
                        }
                    }

                    draw3d.End();
                    RenderStates.DisableSeparateAlphaChannelBlend(graphics);
                }
            }

            graphics.SetRenderTarget(null);

            DrawBorders.Stop();
        }

        void DrawExplosions(SpriteBatch batch)
        {
            DrawExplosionsPerf.Start();
            batch.SafeBegin(SpriteBlendMode.Additive);
            ExplosionManager.DrawExplosions(batch, View, Projection);
            batch.SafeEnd();
            DrawExplosionsPerf.Stop();
        }

        void DrawOverlayShieldBubbles(SpriteBatch sb)
        {
            if (ShowShipNames && !LookingAtPlanet &&
                viewState <= UnivScreenState.SystemView && 
                Shields != null && Shields.VisibleShields.Length != 0)
            {
                var uiNode = ResourceManager.Texture("UI/node");

                sb.SafeBegin(SpriteBlendMode.Additive);
                for (int i = 0; i < Shields.VisibleShields.Length; i++)
                {
                    if (Shields.VisibleShields[i].Owner is ShipModule m)
                    {
                        ProjectToScreenCoords(m.Position, m.ShieldRadius * 2.75f, 
                            out Vector2d posOnScreen, out double radiusOnScreen);

                        float shieldRate = 0.001f + m.ShieldPower / m.ActualShieldPowerMax;
                        DrawTextureSized(uiNode, posOnScreen, 0f, radiusOnScreen, radiusOnScreen, 
                            Shield.GetBubbleColor(shieldRate, m.ShieldBubbleColor));
                    }
                }
                sb.SafeEnd();
            }
        }

        void EnsureFogMapRenderTargets(GraphicsDevice device)
        {
            if (FogMapTargetA == null || FogMapTargetA.IsDisposed || FogMapTargetA.IsContentLost)
            {
                FogMapTargetA?.Dispose();
                FogMapTargetA = RenderTargets.Create(device, 512, 512);
            }
            if (FogMapTargetB == null || FogMapTargetB.IsDisposed || FogMapTargetB.IsContentLost)
            {
                FogMapTargetB?.Dispose();
                FogMapTargetB = RenderTargets.Create(device, 512, 512);
            }
        }

        void UpdateFogMap(SpriteBatch batch, GraphicsDevice device)
        {
            EnsureFogMapRenderTargets(device);

            // Ping-pong: read from front (current FogMap), render to back, swap.
            // Reading and writing the same texture is undefined under D3D11, which
            // is why pre-migration's `fogMapTarget.GetTexture()` snapshot semantics
            // mattered (and silently broke when migrated to `(rt as Texture2D)`).
            RenderTarget2D back = (FogMap == FogMapTargetA) ? FogMapTargetB : FogMapTargetA;
            Texture2D       front = FogMap;

            device.SetRenderTarget(back);
            device.Clear(Color.Transparent);

            // Step 1: blit front→back with Opaque so alpha values copy 1:1.
            // Additive squashes alpha (`src*srcA + dst` ⇒ a²/255), which would
            // make persistent exploration fade out frame by frame.
            batch.SafeBegin(SpriteBlendMode.None);
            batch.Draw(front, new Rectangle(0, 0, 512, 512), Color.White);
            batch.SafeEnd();

            // Step 2: stamp current sensor coverage additively on top.
            batch.SafeBegin(SpriteBlendMode.Additive);
            double universeWidth = UState.Size * 2.0;
            double worldSizeToMaskSize = (512.0 / universeWidth);

            var uiNode = ResourceManager.Texture("UI/node");
            var ships = Player.OwnedShips;
            // White stamp keeps rgb tracking alpha so the FogMap stays premul-
            // correct for the AlphaBlend composite in UpdateFogOfWarInfluences.
            var shipSensorMask = new Color(255, 255, 255, 255);
            foreach (Ship ship in ships)
            {
                if (ship != null && ship.InFrustum)
                {
                    double posX = ship.Position.X * worldSizeToMaskSize + 256;
                    double posY = ship.Position.Y * worldSizeToMaskSize + 256;
                    double size = (ship.SensorRange * 2.0) * worldSizeToMaskSize;
                    var rect = new RectF(posX, posY, size, size);
                    batch.Draw(uiNode, rect, shipSensorMask, 0f, uiNode.CenterF, SpriteEffects.None, 1f);
                }
            }
            batch.SafeEnd();
            device.SetRenderTarget(null);
            FogMap = back;
        }

        void UpdateFogOfWarInfluences(SpriteBatch batch, GraphicsDevice device)
        {
            DrawFogInfluence.Start();

            UpdateFogMap(batch, device);

            device.SetRenderTarget(LightsTarget);
            device.Clear(Color.White); // clear the lights RT to White
            batch.SafeBegin(SpriteBlendMode.AlphaBlend);

            if (!Debug) // draw fog of war if we're not in debug
            {
                // fill screen with transparent black and draw FogMap darker light on top of it
                Rectangle fogRect = ProjectToScreenCoords(new Vector2(-UState.Size), UState.Size*2f);
                // Phase 3.7 step 3: fillrect alpha 170 matches pre-migration —
                // ~33% unexplored scene visibility (1 - 170/255).
                batch.FillRectangle(new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 170));
                // Phase 3.7 step 3: persistent "I've been here" tint, premul-correct
                // (rgb == alpha so FogMap composites correctly under premul AlphaBlend).
                // Color(56,56,56,56) lifts fully-explored memory pixels to ~48%
                // scene brightness, matching pre-migration.
                batch.Draw(FogMap, fogRect, new Color(56, 56, 56, 56));
            }

            // draw all sensor nodes as clear white
            DrawSensorNodesHighlights(batch);

            batch.SafeEnd();
            device.SetRenderTarget(null);

            DrawFogInfluence.Stop();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            DrawGroupTotalPerf.Start();
            PulseTimer -= elapsed.RealTime.Seconds;
            if (PulseTimer < 0) PulseTimer = 1;

            AdjustCamera(elapsed.RealTime.Seconds);

            Matrix cameraMatrix = Matrices.CreateLookAtDown(CamPos.X, CamPos.Y, -CamPos.Z);
            SetViewMatrix(cameraMatrix);

            SpriteRenderer sr = ScreenManager.SpriteRenderer;

            GraphicsDevice graphics = ScreenManager.GraphicsDevice;
            graphics.SetRenderTarget(MainTarget);
            Render(sr, batch, elapsed);
            graphics.SetRenderTarget(null);
            
            OverlaysGroupTotalPerf.Start();
            {
                UpdateFogOfWarInfluences(batch, graphics);
                if (viewState >= UnivScreenState.SectorView) // draw colored empire borders only if zoomed out
                    DrawColoredEmpireBorders(sr, graphics);

                // §3.7 step 1: bloom processes MainTarget -> PostBloomTarget,
                // which then becomes the input to the fog-of-war composite.
                // Originally bloom ran on the post-fog back buffer (XNA's
                // ResolveBackBuffer); MonoGame can't read the back buffer
                // directly, so we run bloom on the pre-fog scene RT instead.
                // Visually equivalent because fog-of-war is a darkening
                // overlay that wouldn't generate extra bright pixels.
                RenderTarget2D mainSource = MainTarget;
                if (GlobalStats.RenderBloom && bloomComponent != null && PostBloomTarget != null)
                {
                    bloomComponent.Draw(batch, MainTarget, PostBloomTarget);
                    mainSource = PostBloomTarget;
                }

                // §3.7 step 2: shield-hit distortion runs after bloom, before
                // fog-of-war. The PS samples `mainSource` and writes a warped
                // copy to PostDistortTarget. If no shield is currently being
                // hit, BuildDistortionSources returns an empty list, the
                // component skips the pass, and we keep using `mainSource`.
                //
                // Mirror DrawShields' zoom gate: only run when 3D ship meshes
                // are actually rendered (viewState < SystemView). At higher
                // zoom levels there's no ship mesh to be hit, so the ripple
                // would distort backdrop / strategic-overlay pixels around
                // an invisible point — visually wrong, and wasted GPU work.
                // BuildDistortionSources also frustum-culls per shield, so
                // off-screen hits are skipped even at close zoom.
                if (GlobalStats.RenderShieldDistortion
                    && distortionComponent != null
                    && PostDistortTarget != null
                    && Shields != null
                    && viewState < UnivScreenState.SystemView)
                {
                    DistortionSources.Clear();
                    Shields.BuildDistortionSources(PostDistortTarget.Width, PostDistortTarget.Height, DistortionSources);
                    if (DistortionSources.Count > 0)
                    {
                        // Wall-clock seconds drive the ripple's animation
                        // phase. DrawTimes.RealTime.Seconds is a per-frame
                        // delta, not cumulative, so we read the host stopwatch
                        // directly. Static accumulator avoids time-warp on
                        // start (Environment.TickCount wraps every ~25 days
                        // but the shader only consumes sin() of it).
                        float time = (float)(System.Environment.TickCount * 0.001);
                        distortionComponent.Draw(batch, mainSource, PostDistortTarget,
                                                 DistortionSources, time);
                        mainSource = PostDistortTarget;
                    }
                }

                // this draws the MainTarget RT which has the entire background and 3D ships
                DrawMainRTWithFogOfWarEffect(batch, graphics, mainSource);

                SetViewMatrix(cameraMatrix);

                DrawColoredBordersRT(batch);
            }
            OverlaysGroupTotalPerf.Stop();

            // these are all background elements, such as ship overlays, fleet icons, etc..
            IconsGroupTotalPerf.Start();
            batch.SafeBegin();
            {
                DrawShipsAndProjectiles(batch);
                DrawShipAndPlanetIcons(batch);
                DrawSolarSystems(batch);
                DrawSystemThreatIndicators(batch);
                DrawGeneralUI(batch, elapsed);
            }
            batch.SafeEnd();
            IconsGroupTotalPerf.Stop();

            // Advance the simulation time just before we Notify
            if (!UState.Paused && IsActive)
            {
                AdvanceSimulationTargetTime(elapsed.RealTime.Seconds);
            }

            // Notify ProcessTurns that Drawing has finished and while SwapBuffers is blocking,
            // the game logic can be updated
            DrawCompletedEvt.Set();

            DrawGroupTotalPerf.Stop();
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
                    pieMenu.DrawAt(batch, GetPieMenuPosition(), Fonts.Arial12Bold);
                    NotificationManager.Draw(batch);
                }
            }

            batch.DrawRectangle(SelectionBox, Color.Green, 1f);

            // This uses the new UIElementV2 system to automatically toggle visibility of items
            // In general, a much saner way than the old cluster-f*ck of IF statements :)
            PlanetsInCombat.Visible = ShipsInCombat.Visible = showGeneralUI && !LookingAtPlanet;
            aw.Visible = showGeneralUI && aw.IsOpen && !LookingAtPlanet;
            ExoticBonusesWindow.Visible = showGeneralUI && ExoticBonusesWindow.IsOpen && !LookingAtPlanet;
            FreighterUtilizationWindow.Visible = showGeneralUI && FreighterUtilizationWindow.IsOpen && !LookingAtPlanet;

            Minimap.Visible = showGeneralUI && (!LookingAtPlanet ||
                              LookingAtPlanet && workersPanel is UnexploredPlanetScreen ||
                              LookingAtPlanet && workersPanel is UnownedPlanetScreen);

            DrawSelectedItems(batch, elapsed);
            DrawSystemAndPlanetBrackets(batch);

            if (Debug)
                DebugWin?.Draw(batch, elapsed);

            DrawGeneralStatusText(batch, elapsed);

            base.Draw(batch, elapsed);  // UIElementV2 Draw

            DrawUI.Stop();
        }

        private void DrawMainRTWithFogOfWarEffect(SpriteBatch batch, GraphicsDevice graphics, RenderTarget2D source)
        {
            DrawFogOfWar.Start();

            Texture2D scene  = source as Microsoft.Xna.Framework.Graphics.Texture2D;
            Texture2D lights = LightsTarget as Microsoft.Xna.Framework.Graphics.Texture2D;
            graphics.Clear(Color.Black);

            // §3.7 step 3: BasicFogOfWar restored. The PS sets alpha = lights.r;
            // with AlphaBlend on, dark LightsTarget pixels alpha-blend the scene
            // toward the black-cleared back buffer (= fog), bright pixels show
            // full scene (= visible). Pass the effect to SpriteBatch.Begin's
            // `effect:` argument — the manual Pass.Apply()-after-Begin pattern
            // produces silent black output under MGFX 3.8.1.303 / DX11, which was
            // the failure mode of the 2026-05-02 attempt. Set MatrixTransform
            // ourselves (SpriteBatch only auto-populates it on SpriteEffect-typed
            // effects; see BloomComponent.SetMatrixTransform).
            if (basicFogOfWarEffect != null)
            {
                basicFogOfWarEffect.Parameters["LightsTexture"]?.SetValue(lights);
                EffectParameter mt = basicFogOfWarEffect.Parameters["MatrixTransform"];
                if (mt != null)
                {
                    Microsoft.Xna.Framework.Matrix.CreateOrthographicOffCenter(
                        0, ScreenWidth, ScreenHeight, 0, 0, 1,
                        out Microsoft.Xna.Framework.Matrix proj);
                    mt.SetValue(proj);
                }

                // BlendState.NonPremultiplied (NOT AlphaBlend): the PS outputs
                // (scene.rgb, lights.r) — straight non-premul alpha. AlphaBlend
                // in MonoGame is premultiplied (`src + dst*(1-srcA)`), which on
                // a black-cleared back buffer returns `(red, red, red, 0) + 0 =
                // red` and the fog side never goes dark. NonPremultiplied does
                // `src.rgb * srcA + dst*(1-srcA)`, giving black on fog and the
                // full scene on visible — the original D3DX behavior.
                batch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied,
                            SamplerState.LinearClamp, DepthStencilState.None,
                            RasterizerState.CullNone, basicFogOfWarEffect);
            }
            else
            {
                // Fallback: effect failed to load. Draw the scene RT directly so
                // the player still sees something, even if fog is flat.
                batch.SafeBegin(SpriteBlendMode.AlphaBlend, sortImmediate:true, saveState:true);
            }

            batch.Draw(scene, new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.White);
            batch.End();

            DrawFogOfWar.Stop();
        }

        private void DrawShipAndPlanetIcons(SpriteBatch batch)
        {
            DrawIcons.Start();
            DrawProjectedGroup();
            if (!LookingAtPlanet)
                DeepSpaceBuildWindow.DrawBlendedBuildIcons(ClickableBuildGoals);
            DrawTacticalPlanetIcons(batch);
            DrawFTLInhibitionNodes();
            DrawShipRangeOverlay();
            DrawFleetIcons(batch);
            DrawIcons.Stop();
        }

        void DrawTopCenterStatusText(SpriteBatch batch, in LocalizedText status, Color color, int lineOffset)
        {
            Font font = Fonts.Pirulen16;
            var pos = new Vector2(ScreenCenter.X - font.TextWidth(status) / 2f, 45f + (font.LineSpacing + 2)*lineOffset);
            batch.DrawString(font, status, pos, color);
        }

        void DrawGeneralStatusText(SpriteBatch batch, DrawTimes elapsed)
        {
            if (UState.Paused)
            {
                DrawTopCenterStatusText(batch, GameText.Paused, Color.Gold, 0);
            }

            if (UState.Events.ActiveEvent != null && UState.Events.ActiveEvent.InhibitWarp)
            {
                DrawTopCenterStatusText(batch, "Hyperspace Flux", Color.Yellow, 1);
            }

            if (IsActive && IsSaving)
            {
                DrawTopCenterStatusText(batch, "Saving...", CurrentFlashColor, 2);
            }
            else if (Debug)
            {
                DrawTopCenterStatusText(batch, "Debug", Color.GreenYellow, 2);
            }

            if (IsActive && UState.GameSpeed.NotEqual(1)) //don't show "1.0x"
            {
                string speed = UState.GameSpeed.ToString("0.0##") + "x";
                Font font = UState.GameSpeed is > 3 or < 0.25f ? Fonts.Pirulen20 : Fonts.Pirulen16;
                Color color = font == Fonts.Pirulen20 ? Color.Red : Color.LightGreen;
                var pos = new Vector2(ScreenWidth - font.TextWidth(speed) - 20f, 90f);
                batch.DrawString(font, speed, pos, color);
            }

            if (IsActive && !IsCinematicModeEnabled && (Debug || Debugger.IsAttached))
            {
                Font font = Fonts.Pirulen16;
                Color color = Color.LightGreen;
                batch.DrawString(font, "FPS " + ActualDrawFPS, new Vector2(ScreenWidth - 100f, 130f), color);
                batch.DrawString(font, "SIM  " + ActualSimFPS, new Vector2(ScreenWidth - 100f, 160f), color);
            }

            if (IsCinematicModeEnabled && CinematicModeTextTimer > 0f)
            {
                CinematicModeTextTimer -= elapsed.RealTime.Seconds;
                DrawTopCenterStatusText(batch, "Cinematic Mode - Press F11 to exit", Color.White, 3);
            }

            if (!Player.Research.NoResearchLeft && Player.Research.NoTopic && !Player.AutoResearch && !Debug)
            {
                DrawTopCenterStatusText(batch, "No Research!",  ApplyCurrentAlphaToColor(Color.Red), 2);
            }
        }

        void DrawShipRangeOverlay()
        {
            if (ShowingRangeOverlay && !LookingAtPlanet)
            {
                var shipRangeTex = ResourceManager.Texture("UI/node_shiprange");
                foreach (Ship ship in UState.Objects.VisibleShips)
                {
                    if (ship is { WeaponsMaxRange: > 0f, IsVisibleToPlayer: true })
                    {
                        Color color = ship.Loyalty == Player
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
                        if (SelectedShip == ship)
                        {
                            Color color = (ship.Loyalty.isPlayer)
                                ? new Color(0, 100, 200, 20)
                                : new Color(200, 0, 0, 10);
                            float sensorRange = ship.AI.GetSensorRadius();
                            DrawTextureProjected(shipRangeTex, ship.Position, sensorRange, color);
                            DrawCircleProjected(ship.Position, sensorRange, new Color(Color.Blue, 85));
                        }
                    }
                }
            }
        }

        // this is called quite rarely, only when ShowingFTLOverlay is enabled
        void DrawFTLInhibitionNodes()
        {
            if (ShowingFTLOverlay && UState.P.GravityWellRange > 0f && !LookingAtPlanet)
            {
                var inhibit = ResourceManager.Texture("UI/node_inhibit");

                Planet[] visiblePlanets = UState.GetVisiblePlanets();
                foreach (Planet planet in visiblePlanets)
                {
                    if (planet.System.IsExploredBy(Player))
                    {
                        DrawCircleProjected(planet.Position, planet.GravityWellRadius,
                                            new Color(255, 50, 0, 150), 1f, inhibit, new Color(200, 0, 0, 50));
                    }
                }

                foreach (Ship ship in UState.Objects.VisibleShips)
                {
                    if (ship is { InhibitionRadius: > 0f, IsVisibleToPlayer: true })
                    {
                        DrawCircleProjected(ship.Position, ship.InhibitionRadius, 
                                            new Color(255, 50, 0, 150), 1f, inhibit, new Color(200, 0, 0, 40));
                    }
                }

                // draw blue positive influence nodes from bordernodes
                if (viewState >= UnivScreenState.SectorView)
                {
                    var transparentBlue = new Color(30, 30, 150, 150);
                    var transparentGreen = new Color(0, 200, 0, 20);
                    var frustum = VisibleWorldRect;

                    foreach (ref Empire.InfluenceNode n in Player.BorderNodes.AsSpan())
                    {
                        if (n.KnownToPlayer && frustum.Overlaps(n.Position, n.Radius))
                            DrawCircleProjected(n.Position, n.Radius, transparentBlue, 1f, inhibit, transparentGreen);
                    }
                }
            }
        }

        bool CanShowInfo => !LookingAtPlanet && !IsCinematicModeEnabled;
        bool ShowSystemInfoOverlay => SelectedSystem != null && CanShowInfo && viewState == UnivScreenState.GalaxyView;
        bool ShowPlanetInfo => SelectedPlanet != null && CanShowInfo;
        bool ShowShipInfo => SelectedShip != null && CanShowInfo;
        bool ShowShipList => SelectedShips.Count > 1 && SelectedFleet == null && CanShowInfo;
        bool ShowFleetInfo => SelectedFleet != null && CanShowInfo;

        private void DrawSelectedItems(SpriteBatch batch, DrawTimes elapsed)
        {
            if (ShowSystemInfoOverlay)
            {
                SystemInfoOverlay.Draw(batch, elapsed);
            }

            if (ShowPlanetInfo)
            {
                pInfoUI.Draw(batch, elapsed);
            }
            else if (ShowShipInfo)
            {
                if (Debug && DebugWin != null)
                {
                    DebugWin.DrawCircleImm(SelectedShip.Position,
                        SelectedShip.AI.GetSensorRadius(), Color.Crimson);
                    for (int i = 0; i < SelectedShip.AI.NearByShips.Count; i++)
                    {
                        var target = SelectedShip.AI.NearByShips[i];
                        DebugWin.DrawCircleImm(target.Ship.Position,
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
                if (goal.Owner.AI.HasGoal(goal))
                {
                    string titleText = $"({ResourceManager.GetShipTemplate(SelectedItem.UID).Name})";
                    string bodyText = goal.PlanetBuildingAt != null
                        ? Localizer.Token(GameText.UnderConstructionAt) + goal.PlanetBuildingAt.Name
                        : Fonts.Arial12Bold.ParseText(Localizer.Token(GameText.NoPortsFoundForBuild), 300);

                    vuiElement.Draw(titleText, bodyText);
                    DrawItemInfoForUI();
                }
                else
                {
                    ClearSelectedItems();
                }
            }
            else if (ShowFleetInfo)
            {
                shipListInfoUI.Draw(batch, elapsed);
            }
        }


        void DrawFleetIcons(SpriteBatch batch)
        {
            ClickableFleetsList.Clear();
            if (viewState < UnivScreenState.SectorView)
                return;

            bool debug = Debug && SelectedShip == null;
            Empire viewer = Debug ? SelectedShip?.Loyalty ?? Player : Player;

            for (int i = 0; i < UState.Empires.Count; i++)
            {
                Empire empire = UState.Empires[i];
                bool doDraw = debug || !(Player.DifficultyModifiers.HideTacticalData && viewer.IsEmpireAttackable(empire));
                if (doDraw)
                {
                    foreach (Fleet f in empire.ActiveFleets)
                        DrawVisibleShips(batch, f, viewer, debug);
                }
            }
        }

        void DrawVisibleShips(SpriteBatch batch, Fleet fleet, Empire viewer, bool debug)
        {
            var visibleShips = fleet.Ships.Filter(s => s?.KnownByEmpires.KnownBy(viewer) == true);
            if (visibleShips.Length >= (fleet.Ships.Count * 0.75f))
            {
                SubTexture icon = fleet.Icon;
                Vector2 commandShipCenterOnScreen = ProjectToScreenPosition(fleet.FleetCommandShipPosition).ToVec2fRounded();

                FleetIconLines(batch, visibleShips, commandShipCenterOnScreen);

                ClickableFleetsList.Add(new ClickableFleet
                {
                    fleet = fleet,
                    ScreenPos = commandShipCenterOnScreen,
                    ClickRadius = 15f
                });

                batch.Draw(icon, commandShipCenterOnScreen, fleet.Owner.EmpireColor, 0.0f, icon.CenterF, 0.35f, SpriteEffects.None, 1f);
                
                if (!Player.DifficultyModifiers.HideTacticalData || debug || fleet.Owner.isPlayer || fleet.Owner.AlliedWithPlayer)
                {
                    batch.DrawDropShadowText(fleet.Name, commandShipCenterOnScreen + FleetNameOffset, Fonts.Arial8Bold);
                }

                if (debug)
                {
                    Vector2 fleetAveragePosOnScreen = ProjectToScreenPosition(fleet.AveragePosition()).ToVec2fRounded();
                    batch.Draw(icon, fleetAveragePosOnScreen, fleet.Owner.EmpireColor.Alpha(0.5f), 0.0f, icon.CenterF, 0.35f, SpriteEffects.None, 1f); ;
                    FleetAveragePosLine(batch, commandShipCenterOnScreen, fleetAveragePosOnScreen, fleet.Owner.EmpireColor);
                }
            }
        }

        void FleetIconLines(SpriteBatch batch, Ship[] ships, Vector2 fleetCenterOnScreen)
        {
            for (int i = 0; i < ships.Length; i++)
            {
                Ship ship = ships[i];
                if (ship == null || !ship.Active)
                    continue;

                if (Debug || ship.Loyalty.isPlayer || ship.Loyalty.IsAlliedWith(Player) || !Player.DifficultyModifiers.HideTacticalData)
                {
                    Vector2 shipScreenPos = ProjectToScreenPosition(ship.Position).ToVec2fRounded();
                    batch.DrawLine(shipScreenPos, fleetCenterOnScreen, FleetLineColor);
                }
            }
        }

        void FleetAveragePosLine(SpriteBatch batch, Vector2 comanfshipCenterOnScreen, Vector2 fleetCenterOnScreen, Color color)
        {
            if (comanfshipCenterOnScreen != fleetCenterOnScreen)
                batch.DrawLine(comanfshipCenterOnScreen, fleetCenterOnScreen, color.Alpha(0.75f));

        }

        void DrawTacticalPlanetIcons(SpriteBatch batch)
        {
            if (LookingAtPlanet || viewState <= UnivScreenState.SystemView || viewState >= UnivScreenState.GalaxyView)
                return;

            for (int i = 0; i < UState.Systems.Count; i++)
            {
                SolarSystem system = UState.Systems[i];
                if (!system.IsExploredBy(Player) || !system.InFrustum)
                    continue;

                foreach (Planet planet in system.PlanetList)
                {
                    float fIconScale      = 0.1875f * (0.7f + ((float) (Math.Log(planet.Scale)) / 2.75f));
                    Vector2 planetIconPos = ProjectToScreenPosition(planet.Position3D).ToVec2fRounded();
                    Vector2 flagIconPos   = planetIconPos - new Vector2(0, 15);

                    SubTexture planetTex = planet.PlanetTexture;
                    if (planet.Owner != null && (Player.IsKnown(planet.Owner) || planet.Owner == Player))
                    {
                        batch.Draw(planetTex, planetIconPos, Color.White, 0.0f, planetTex.CenterF, fIconScale, SpriteEffects.None, 1f);
                        SubTexture flag = ResourceManager.Flag(planet.Owner);
                        batch.Draw(flag, flagIconPos, planet.Owner.EmpireColor, 0.0f, flag.CenterF, 0.2f, SpriteEffects.None, 1f);
                    }
                    else
                    {
                        batch.Draw(planetTex, planetIconPos, Color.White, 0.0f, planetTex.CenterF, fIconScale, SpriteEffects.None, 1f);
                    }
                }
            }
        }

        void DrawItemInfoForUI()
        {
            var goal = SelectedItem?.AssociatedGoal;
            if (goal == null) return;
            if (!LookingAtPlanet)
                DrawCircleProjected(goal.BuildPosition, 50f, goal.Owner.EmpireColor);
        }

        void DrawShipsAndProjectiles(SpriteBatch batch)
        {
            Ship[] ships = UState.Objects.VisibleShips;

            if (viewState <= UnivScreenState.PlanetView)
            {
                DrawProj.Start();
                RenderStates.BasicBlendMode(Device, additive:true, depthWrite:true);

                Projectile[] projectiles = UState.Objects.VisibleProjectiles;
                Beam[] beams = UState.Objects.VisibleBeams;

                for (int i = 0; i < projectiles.Length; ++i)
                {
                    Projectile proj = projectiles[i];
                    proj.Draw(batch, this);
                }

                if (beams.Length > 0)
                    Beam.UpdateBeamEffect(this);

                for (int i = 0; i < beams.Length; ++i)
                {
                    Beam beam = beams[i];
                    beam.Draw(this);
                }

                DrawProj.Stop();
            }

            RenderStates.BasicBlendMode(Device, additive:false, depthWrite:false);

            DrawShips.Start();
            for (int i = 0; i < ships.Length; ++i)
            {
                Ship ship = ships[i];
                if (ship.InFrustum && ship.InPlayerSensorRange)
                {
                        if (!IsCinematicModeEnabled)
                            DrawTacticalIcon(ship);

                        DrawOverlay(ship);

                    if (SelectedShip == ship || SelectedShips.Contains(ship))
                    {
                        Color color = Color.LightGreen;
                        if (Player != ship.Loyalty)
                            color = Player.IsEmpireAttackable(ship.Loyalty) ? Color.Red : Color.Yellow;
                        else if (ship.Resupplying)
                            color = Color.Gray;

                        ProjectToScreenCoords(ship.Position, ship.Radius,
                            out Vector2d shipScreenPos, out double screenRadius);

                        double radius = screenRadius < 7f ? 7f : screenRadius;
                        batch.BracketRectangle(shipScreenPos, radius, color);
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
                    DrawShipProjectionIcon(ship, ship.ProjectedPosition, CurrentGroup.ProjectedDirection, projectedColor);
            }
        }

        void DrawShipProjectionIcon(Ship ship, Vector2 position, Vector2 direction, Color color)
        {
            TacticalIcon icon = ship.TacticalIcon();
            double num = ship.SurfaceArea / (30.0 + icon.Primary.Width);
            double scale = (num * 4000.0 / CamPos.Z).UpperBound(1);

            if (scale <= 0.1)
                scale = ship.ShipData.Role != RoleName.platform || viewState < UnivScreenState.SectorView ? 0.15 : 0.08;

            float rotation = direction.ToRadians();
            DrawTextureProjected(icon.Primary, position, (float)scale, rotation, color);
            if (icon.Secondary != null)
                DrawTextureProjected(icon.Secondary, position, (float)scale, rotation, color);
        }

        void DrawOverlay(Ship ship)
        {
            if (ship.InFrustum && ship.Active && !ship.Dying && !LookingAtPlanet && viewState <= UnivScreenState.DetailView)
            {
                // if we check for a missing model here we can show the ship modules instead. 
                // that will solve invisible ships when the ship model load hits an OOM.
                if (!ship.IsLaunching && (ShowShipNames || ship.GetSO()?.HasMeshes == false))
                {
                    ship.DrawModulesOverlay(this, CamPos.Z,
                        showDebugSelect:Debug && ship == SelectedShip,
                        showDebugStats: Debug && DebugWin?.IsOpen == true);
                }
            }
        }

        void DrawTacticalIcon(Ship ship)
        {
            if (!LookingAtPlanet && (!ship.IsSubspaceProjector || ShowingFTLOverlay))  
            {
                ship.DrawTacticalIcon(this, viewState);
            }
        }

        void DrawBombs()
        {
            Span<Bomb> bombs = BombList.AsSpan();
            for (int i = bombs.Length - 1; i >= 0; --i)
            {
                Bomb bomb = bombs[i];
                if (bomb?.Model != null)
                {
                    Projectile.DrawMesh(this, bomb.Model, bomb.World, bomb.Texture.Texture, scale:25f);
                }
            }
        }
        // FB - This cf needs refactor
        void DrawShipGoalsAndWayPoints(Ship ship, byte alpha)
        {
            if (ship == null)
                return;
            Vector2 start = ship.Position;

            if (ship.OnLowAlert || ship.AI.HasPriorityOrder)
            {
                Color color = Colors.Orders(alpha);
                if (ship.AI.State == AIState.Ferrying && ship.AI.EscortTarget != null)
                {
                    DrawLineProjected(start, ship.AI.EscortTarget.Position, color);
                    return;
                }
                if (ship.AI.State == AIState.ReturnToHangar)
                {
                    if (ship.IsHangarShip)
                        DrawLineProjected(start, ship.Mothership.Position, color);
                    return;
                }
                if (ship.AI.State == AIState.Escort && ship.AI.EscortTarget != null)
                {
                    DrawLineProjected(start, ship.AI.EscortTarget.Position, color);
                    return;
                }

                if (ship.AI.State == AIState.Explore && ship.AI.ExplorationTarget != null)
                {
                    DrawLineProjected(start, ship.AI.ExplorationTarget.Position, color);
                    return;
                }

                if (ship.AI.State == AIState.Colonize && ship.AI.ColonizeTarget != null)
                {
                    Vector2d screenPos = DrawLineProjected(start, ship.AI.ColonizeTarget.Position, color, 2500f, 0);
                    string text = $"Colonize\nSystem : {ship.AI.ColonizeTarget.System.Name}\nPlanet : {ship.AI.ColonizeTarget.Name}";
                    DrawPointerWithText(screenPos.ToVec2f(), ResourceManager.Texture("UI/planetNamePointer"), color, text, new Color(ship.Loyalty.EmpireColor, alpha));
                    return;
                }
                if (ship.AI.State == AIState.Orbit && ship.AI.OrbitTarget != null)
                {
                    DrawLineProjected(start, ship.AI.OrbitTarget.Position, color, 2500f , 0);
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
                    DrawLineProjected(ship.Position, goal.TargetPlanet.Position, Colors.CombatOrders(alpha), 2500f);
                    DrawWayPointLines(ship, Colors.CombatOrders(alpha));
                }
            }
            if (!ship.AI.HasPriorityOrder &&
                (ship.AI.State == AIState.AttackTarget || ship.AI.State == AIState.Combat) && ship.AI.Target is Ship)
            {
                DrawLineProjected(ship.Position, ship.AI.Target.Position, Colors.Attack(alpha));
                if (ship.AI.TargetQueue.Count > 1)
                {
                    for (int i = 0; i < ship.AI.TargetQueue.Count - 1; ++i)
                    {
                        var target = ship.AI.TargetQueue[i];
                        if (target == null || !target.Active)
                            continue;
                        DrawLineProjected(target.Position, ship.AI.TargetQueue[i].Position,
                            Colors.Attack((byte) (alpha * .5f)));
                    }
                }
                return;
            }
            if (ship.AI.State == AIState.Boarding && ship.AI.EscortTarget != null)
            {
                DrawLineProjected(start, ship.AI.EscortTarget.Position, Colors.CombatOrders(alpha));
                return;
            }

            var planet = ship.AI.OrbitTarget;
            if (ship.AI.State == AIState.AssaultPlanet && planet != null)
            {
                int spots = planet.GetFreeTiles(Player);
                if (spots > 4)
                    DrawLineToPlanet(start, planet.Position, Colors.CombatOrders(alpha));
                else if (spots > 0)
                {
                    DrawLineToPlanet(start, planet.Position, Colors.Warning(alpha));
                    ToolTip.PlanetLandingSpotsTip($"{planet.Name}: Warning!", spots);
                }
                else
                {
                    DrawLineToPlanet(start, planet.Position, Colors.Error(alpha));
                    ToolTip.PlanetLandingSpotsTip($"{planet.Name}: Critical!", spots);
                }
                DrawWayPointLines(ship, new Color(Color.Lime, alpha));
                return;
            }

            if (ship.AI.State == AIState.SystemTrader && 
                ship.AI.OrderQueue.TryPeekLast(out ShipAI.ShipGoal g) && g.Trade != null)
            {
                Vector2 importPosition = g.Trade.ImportTo?.Position ?? g.Trade.TargetStation.Position;
                Vector2 exportPosition = g.Trade.ExportFrom.Position;

                if (g.Plan is ShipAI.Plan.PickupGoods or ShipAI.Plan.PickupGoodsForStation )
                {
                    DrawLineToPlanet(start, exportPosition, Color.Blue);
                    DrawLineToPlanet(exportPosition, importPosition, Color.Gold);
                }
                else
                    DrawLineToPlanet(start, importPosition, Color.Gold);
            }

            if (ship.IsConstructor && ship.AI.State == AIState.MoveTo 
                && ship.AI.OrderQueue.TryPeekLast(out ShipAI.ShipGoal cg)) 
            {
                if (cg.Plan is ShipAI.Plan.DeployOrbital or ShipAI.Plan.DeployStructure)
                    DrawLineToPlanet(start, cg.MovePosition, Color.Yellow);
            }

            DrawWayPointLines(ship, Colors.WayPoints(alpha));
        }

        void DrawWayPointLines(Ship ship, Color color)
        {
            if (!ship.AI.HasWayPoints)
                return;

            WayPoint[] wayPoints = ship.AI.CopyWayPoints();

            DrawLineProjected(ship.Position, wayPoints[0].Position, color);

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

        void DrawFleetPatrolPlan(Fleet fleet, Color color)
        {
            float texScale = viewState <= UnivScreenState.SectorView ? 0.5f : 0.75f;
            WayPoint[] waypoints = fleet.Patrol.WayPoints.ToArray();
            SubTexture icon = fleet.Icon;
            for (int i = 0; i < waypoints.Length-1; ++i)
            {
                DrawLineWideProjected(waypoints[i].Position, waypoints[i+1].Position, color, 2);
                DrawTextureProjected(icon, waypoints[i].Position, texScale, 0.0f, color);
            }

            if (waypoints.Length > 2)
                DrawLineWideProjected(waypoints[waypoints.Length - 1].Position, waypoints[0].Position, color, 2);

            DrawTextureProjected(icon, waypoints[waypoints.Length - 1].Position, texScale, 0.0f, color);
        }

        void DrawShields()
        {
            DrawShieldsPerf.Start();
            if (viewState < UnivScreenState.SystemView)
            {
                RenderStates.BasicBlendMode(Device, additive:true, depthWrite:false);
                Shields?.Draw(View, Projection);
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
            SubTexture icon_research = ResourceManager.Texture("NewUI/icon_science");
            for (int k = 0; k < UState.Systems.Count; k++)
            {
                SolarSystem solarSystem = UState.Systems[k];
                if (!solarSystem.IsExploredBy(Player) || !solarSystem.InFrustum)
                    continue;

                for (int j = 0; j < solarSystem.PlanetList.Count; j++)
                {
                    Planet planet = solarSystem.PlanetList[j];
                    Vector2 screenPosPlanet = ProjectToScreenPosition(planet.Position3D).ToVec2fRounded();
                    Vector2 posOffSet = screenPosPlanet;
                    posOffSet.X += 20f;
                    posOffSet.Y += 37f;
                    int drawLocationOffset = 0;
                    Color textColor = planet.Owner?.EmpireColor ?? Color.Gray;

                    DrawPointerWithText(screenPosPlanet, planetNamePointer, textColor, planet.Name, textColor);

                    posOffSet = new Vector2(screenPosPlanet.X + 10f, screenPosPlanet.Y + 60f);

                    if (planet.RecentCombat)
                    {
                        DrawTextureWithToolTip(icon_fighting_small, Color.White, GameText.IndicatesThatAnAnomalyWas, mousePos,
                                               (int)posOffSet.X + 18*drawLocationOffset, (int)posOffSet.Y, 14, 14);
                        ++drawLocationOffset;
                    }

                    if (planet.IsResearchable && !planet.IsResearchStationDeployedBy(Player))
                    {
                        DrawTextureWithToolTip(icon_research, Color.White, GameText.ResearchStationCanBePlaced, mousePos,
                                               (int)posOffSet.X + 18*drawLocationOffset, (int)posOffSet.Y, 14, 14);
                        ++drawLocationOffset;
                    }

                    if (planet.IsMineable)
                    {
                        if (!planet.Mining.AreMiningOpsPresent())
                        {
                            DrawTextureWithToolTip(Mineable.Icon, Color.White, GameText.MiningStationsCanBePlaced, mousePos,
                                (int)posOffSet.X + 18*drawLocationOffset, (int)posOffSet.Y, 17, 14);
                        }
                        else
                        {
                            var flag = planet.Mining.Owner.data.Traits.FlagIndex;
                            DrawTextureWithToolTip(ResourceManager.Flag(flag), planet.Mining.Owner.EmpireColor, 
                                GameText.MiningStationsOpsOwned, mousePos, (int)posOffSet.X + 18 * drawLocationOffset, (int)posOffSet.Y, 14, 14);
                        }

                        ++drawLocationOffset;

                        DrawTextureWithToolTip(planet.Mining.ExoticResourceIcon, Color.White, planet.Mining.ResourceDescription, mousePos,
                                               (int)posOffSet.X + 18*drawLocationOffset, (int)posOffSet.Y, 14, 14);
                        ++drawLocationOffset;
                    }

                    if (Player.data.MoleList.Count > 0)
                    {
                        for (int i = 0; i < Player.data.MoleList.Count; i++)
                        {
                            Mole mole = Player.data.MoleList[i];
                            if (mole.PlanetId == planet.Id)
                            {
                                posOffSet.X += (18 * drawLocationOffset);
                                DrawTextureWithToolTip(icon_spy_small, Color.White, GameText.IndicatesThatAFriendlyAgent, mousePos,
                                                       (int)posOffSet.X, (int)posOffSet.Y, 14, 14);
                                ++drawLocationOffset;
                                break;
                            }
                        }
                    }

                    Building evtBuilding = planet.FindBuilding(b => b.EventHere);
                    if (evtBuilding != null)
                    {
                        posOffSet.X += (18 * drawLocationOffset);
                        DrawTextureWithToolTip(icon_anomaly_small, Color.White, evtBuilding.DescriptionText, mousePos,
                                               (int)posOffSet.X, (int)posOffSet.Y, 14, 14);
                    }

                    int troopCount = planet.CountEmpireTroops(Player);
                    if (troopCount > 0)
                    {
                        posOffSet.X += (18 * drawLocationOffset);
                        DrawTextureWithToolTip(icon_troop, new Color((byte)255, (byte)255, (byte)255, (byte)0), $"Troops {troopCount}", mousePos,
                                               (int)posOffSet.X, (int)posOffSet.Y, 14, 14);
                        ++drawLocationOffset;
                    }
                }
            }
        }

        public void DrawPointerWithText(Vector2 screenPos, SubTexture planetNamePointer, Color pointerColor, string text,
            Color textColor, Graphics.Font font = null, float xOffSet = 20f, float yOffSet = 34f)
        {
            font ??= Fonts.Tahoma10;
            DrawTextureRect(planetNamePointer, screenPos, pointerColor);
            Vector2 posOffSet = screenPos;
            posOffSet.X += xOffSet;
            posOffSet.Y += yOffSet;
            posOffSet = posOffSet.ToFloored();
            ScreenManager.SpriteBatch.DrawDropShadowText(text, posOffSet, font, textColor);
        }

    }
}
