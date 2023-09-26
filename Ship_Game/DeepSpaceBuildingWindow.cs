using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Ship_Game.UI;

namespace Ship_Game
{
    public sealed class DeepSpaceBuildingWindow : UIElementContainer
    {
        readonly UniverseScreen Screen;
        Empire Player => Screen.Player;
        ScrollList<ConstructionListItem> SL;
        public IShipDesign ShipToBuild;
        Vector2 TetherOffset;
        Planet TargetPlanet;
        SolarSystem TargetSystem;
        ShipInfoOverlayComponent ShipInfoOverlay;
        const float MinimumBuildDistanceFromSun = 20000;

        public DeepSpaceBuildingWindow(UniverseScreen screen)
        {
            Screen = screen;
            Visible = false;
        }

        public void InitializeAndShow()
        {
            RemoveAll();

            const int windowWidth = 320;
            RectF = new(Screen.ScreenWidth - 15 - windowWidth, 100, windowWidth, 300);

            var sl = Add(new SubmenuScrollList<ConstructionListItem>(RectF, "Build Menu"));
            sl.SetBackground(Colors.TransparentBlackFill);
            SL = sl.List;
            SL.OnClick = (item) => { ShipToBuild = item.Template; };
            SL.EnableItemHighlight = true;

            //The Doctor: Ensure Projector is always the first entry on the DSBW list so that the player never has to scroll to find it.
            foreach (IShipDesign s in Player.SpaceStationsWeCanBuild)
            {
                if (s.IsSubspaceProjector)
                {
                    SL.AddItem(new(Screen, s));
                    break;
                }
            }
            foreach (IShipDesign s in Player.SpaceStationsWeCanBuild)
            {
                if (!s.IsSubspaceProjector)
                    SL.AddItem(new(Screen, s));
            }

            ShipInfoOverlay = Add(new ShipInfoOverlayComponent(Screen, Screen.UState));
            SL.OnHovered = (item) =>
            {
                ShipInfoOverlay.ShowToLeftOf(item?.Pos ?? Vector2.Zero, item?.Template);
            };

            Visible = true;
        }

        class ConstructionListItem : ScrollListItem<ConstructionListItem>
        {
            readonly UniverseScreen Universe;
            public readonly IShipDesign Template;

            public ConstructionListItem(UniverseScreen universe, IShipDesign template)
            {
                Universe = universe;
                Template = template;
            }

            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                base.Draw(batch, elapsed);
                
                SubTexture projector = ResourceManager.Texture("ShipIcons/subspace_projector");
                SubTexture iconProd = ResourceManager.Texture("NewUI/icon_production");

                SubTexture icon = Template.IsSubspaceProjector ? projector : Template.Icon;
                float iconSize = Height;
                batch.Draw(icon, new Vector2(X, Y), new Vector2(iconSize));
          
                batch.DrawString(Fonts.Arial10, Template.Name, X+iconSize+2, Y+4);
                batch.DrawString(Fonts.Arial8Bold, Template.GetRole(), X+iconSize+2, Y+18, Color.Orange);

                float prodX = Right - 120;
                batch.DrawString(Fonts.Arial8Bold, Template.GetMaintenanceCost(Universe.Player).String(2)+" BC/Y", prodX, Y+4, Color.Salmon); // Maintenance Cost
                batch.Draw(iconProd, new Vector2(prodX+50, Y+4), iconProd.SizeF); // Production Icon
                batch.DrawString(Fonts.Arial12Bold, Template.GetCost(Universe.Player).String(1), prodX+50+iconProd.Width+2, Y+4); // Build Production Cost
            }
        }

        public override bool HandleInput(InputState input)
        {
            if (!Visible || !Enabled)
                return false;

            // only capture input from window UI if we haven't made a selection
            if (ShipToBuild == null)
            {
                if (input.Escaped) // exit this screen
                {
                    Screen.InputOpenDeepSpaceBuildWindow();
                    return true;
                }

                return base.HandleInput(input);
            }

            // we have itemToBuild, so we are in placement mode

            bool hovered = HitTest(input.CursorPosition);
            if (hovered) // disallow input while in placement and hovering our window
            {
                if (input.RightMouseClick)
                    ShipToBuild = null;

                ShipInfoOverlay.Show();
                return true;
            }
            ShipInfoOverlay.Hide();

            // right mouse click or Esc, we cancel the placement mode
            if (input.RightMouseClick || input.Escaped)
            {
                ShipToBuild = null;
                return true;
            }

            // left mouse clicked while not hovering our window? place it!
            if (input.LeftMouseClick)
            {
                TryPlaceBuildable();
                if (input.IsShiftKeyDown) // if we hold down shift, continue placing next frame
                    return true;

                ShipToBuild = null;
                return true;
            }

            return false;
        }

        void TryPlaceBuildable()
        {
            if (OkToBuild(TargetPlanet, TargetSystem))
            {
                Vector2 worldPos = Screen.CursorWorldPosition2D;
                if (ShipToBuild.IsResearchStation)
                {
                    if (TargetPlanet != null)      Player.AI.AddGoalAndEvaluate(new ProcessResearchStation(Player, TargetPlanet, ShipToBuild, worldPos));
                    else if (TargetSystem != null) Player.AI.AddGoalAndEvaluate(new ProcessResearchStation(Player, TargetSystem, worldPos, ShipToBuild));
                }
                else if (ShipToBuild.IsMiningStation)
                {
                    Player.AI.AddGoalAndEvaluate(new MiningOps(Player, TargetPlanet));
                }
                else
                {
                    if (TargetPlanet != null)
                        Player.AI.AddGoalAndEvaluate(new BuildConstructionShip(worldPos, ShipToBuild.Name, Player, TargetPlanet, TetherOffset));
                    else
                        Player.AI.AddGoalAndEvaluate(new BuildConstructionShip(worldPos, ShipToBuild.Name, Player));
                }

                GameAudio.EchoAffirmative();
            }
            else
            {
                GameAudio.NegativeClick();
            }

            Screen.UpdateClickableItems();
        }

        bool OkToBuild(Planet targetPlanet, SolarSystem targetSystem)
        {
            if (ShipToBuild == null)
                return false;

            if (targetSystem != null && (Screen.CursorWorldPosition2D.InRadius(targetSystem.Position, MinimumBuildDistanceFromSun) 
                                         || !targetSystem.InSafeDistanceFromRadiation(Screen.CursorWorldPosition2D)))
            {
                return false;
            }

            if (targetSystem != null && targetPlanet == null && ShipToBuild.IsMiningStation)
                return false;

            if (targetPlanet != null)
            {
                if (targetPlanet.IsOutOfOrbitalsLimit(ShipToBuild))
                    return false;

                if (ShipToBuild.IsResearchStation && !targetPlanet.CanBeResearchedBy(Player))
                    return false;

                if (ShipToBuild.IsMiningStation && (!targetPlanet.IsMineable || !targetPlanet.Mining.CanAddMiningStationFor(Player)))
                    return false;
            }
            else // There is no target planet
            {
                if (ShipToBuild.IsShipyard || ShipToBuild.IsMiningStation)
                    return false;

                if (targetSystem != null && ShipToBuild.IsResearchStation)
                {
                    if (Screen.CursorWorldPosition2D.OutsideRadius(targetSystem.Position, targetSystem.Radius * 0.3f))
                        return false;
                    if (!targetSystem.CanBeResearchedBy(Player))
                        return false;
                }

                if (targetSystem == null && ShipToBuild.IsResearchStation)
                    return false;
            }

            return true;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            var nodeTex = ResourceManager.Texture("UI/node1");

            Planet[] planets = Screen.UState.GetVisiblePlanets();
            SolarSystem[] systems = Screen.UState.GetVisibleSystems();

            foreach (Planet planet in planets)
            {
                if (OkToBuild(planet, null))
                {
                    float radius = 2500f * planet.Scale;
                    Screen.DrawCircleProjected(planet.Position, radius, new Color(255, 165, 0, 100), 2f,
                                               nodeTex, new Color(0, 0, 255, 100));
                }
            }

            foreach (SolarSystem system in systems)
            {
                if (OkToBuild(null, system))
                {
                    if (ShipToBuild?.IsResearchStation == true)
                    {
                        Screen.DrawCircleProjected(system.Position, system.Radius * 0.3f, new Color(255, 165, 0, 100), 2f,
                            nodeTex, new Color(0, 0, 255, 100));
                    }
                }

                Screen.DrawCircleProjected(system.Position, system.SunDangerRadius.LowerBound(MinimumBuildDistanceFromSun),
                    new Color(255, 0, 0, 100), 2f, nodeTex, new Color(255, 0, 0, 50));
            }

            base.Draw(batch, elapsed);

            if (ShipToBuild != null)
            {
                SubTexture platform = ResourceManager.Texture("TacticalIcons/symbol_platform");
                var IconOrigin = new Vector2((platform.Width / 2f), (platform.Width / 2f));

                double scale = (double)ShipToBuild.Grid.SurfaceArea / platform.Width;
                scale *= 4000.0 / Screen.CamPos.Z;
                if (scale > 1f)
                    scale = 1f;
                if (scale < 0.2f)
                    scale = 0.2f;

                Vector2 cursorWorldPos = Screen.CursorWorldPosition2D;
                Vector2 cursorPos = Screen.Input.CursorPosition;
                TargetPlanet = null;
                TetherOffset = Vector2.Zero;

                foreach (Planet planet in planets)
                {
                    Vector2 planetPos = planet.Position;
                    if (planetPos.Distance(cursorWorldPos) <= (2500f * planet.Scale))
                    {
                        TetherOffset = cursorWorldPos - planetPos;

                        // FIX: there's a potential issue here reported in Sentry
                        if (TetherOffset.IsNaN())
                        {
                            Log.Error($"NaN TetherOffset: {TetherOffset}  cursorWorldPos={cursorWorldPos} planetPos={planetPos}");
                            TetherOffset = Vector2.Zero;
                        }
                        else
                        {
                            TargetPlanet = planet;

                            Vector2 planetScreenPos = Screen.ProjectToScreenPosition(planet.Position).ToVec2f();
                            batch.DrawLine(planetScreenPos, cursorPos, new Color(255, 165, 0, 150), 3f);
                            batch.DrawString(Fonts.Arial20Bold, "Will Orbit " + planet.Name, cursorPos + new Vector2(0, 34f), Color.White);
                            break;
                        }
                    }
                }

                foreach (SolarSystem system in systems)
                {
                    if (system.Position.Distance(cursorWorldPos) <= system.Radius * 0.8f)
                    {
                        TargetSystem = system;
                        break;
                    }

                    TargetSystem = null;
                }

                Color shipColor = OkToBuild(TargetPlanet, TargetSystem) ? new Color(0, 255, 0, 100) : Color.Red.Alpha(100);
                batch.Draw(platform, cursorPos, shipColor, 0f, IconOrigin, (float)scale, SpriteEffects.None, 1f);
            }
        }

        float CurrentRadiusSmoothed;

        // this draws green build goal icons, with SSP radius as orange
        public void DrawBlendedBuildIcons(UniverseScreen.ClickableSpaceBuildGoal[] buildGoals)
        {
            if (!Visible)
                return;

            var platform = ResourceManager.Texture("TacticalIcons/symbol_platform");
            for (int i = 0; i < buildGoals.Length; ++i)
            {
                UniverseScreen.ClickableSpaceBuildGoal item = buildGoals[i];

                if (ResourceManager.Ships.Get(item.UID, out Ship buildTemplate))
                {
                    Screen.ProjectToScreenCoords(item.BuildPos, platform.Width, out Vector2d posOnScreen, out double size);

                    float scale = ScaleIconSize((float)size, 0.2f, 0.4f);
                    Screen.DrawTextureSized(platform, posOnScreen, 0.0f, platform.Width * scale,
                                            platform.Height * scale, new Color(0, 255, 0, 100));

                    float borderRadius = buildTemplate.IsSubspaceProjector ? Player.GetProjectorRadius() 
                                                                           : buildTemplate.SensorRange;

                    Screen.ProjectToScreenCoords(item.BuildPos, borderRadius, out _, out double screenRadius);
                    Screen.DrawCircle(posOnScreen, screenRadius, Color.Orange, 2f);
                }
            }

            // show the object placement/build circle
            if (ShipToBuild != null && ShipToBuild.IsSubspaceProjector && Screen.AdjustCamTimer <= 0f)
            {
                Vector2 center = Screen.Input.CursorPosition;
                float screenRadius = (float)Screen.ProjectToScreenSize(Player.GetProjectorRadius());

                CurrentRadiusSmoothed = CurrentRadiusSmoothed.SmoothStep(screenRadius, 0.3);
                Screen.DrawCircle(center, CurrentRadiusSmoothed, Color.Orange, 2f);
            }
        }

        float ScaleIconSize(float screenRadius, float minSize = 0, float maxSize = 0)
        {
            float size = screenRadius * 2;
            if (size < minSize && minSize != 0)
                size = minSize;
            else if (maxSize > 0f && size > maxSize)
                size = maxSize;
            return size + GlobalStats.IconSize;
        }
    }
}