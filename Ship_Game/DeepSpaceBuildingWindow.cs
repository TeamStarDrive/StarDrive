using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
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
        ShipInfoOverlayComponent ShipInfoOverlay;

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
            bool okToBuild = TargetPlanet == null && !ShipToBuild.IsShipyard
                || TargetPlanet != null && !TargetPlanet.IsOutOfOrbitalsLimit(ShipToBuild);

            if (okToBuild)
            {
                Vector2 worldPos = Screen.CursorWorldPosition2D;
                Player.AI.AddGoalAndEvaluate(new BuildConstructionShip(worldPos, ShipToBuild.Name, Player, TargetPlanet, TetherOffset));
                GameAudio.EchoAffirmative();
            }
            else
            {
                GameAudio.NegativeClick();
            }

            Screen.UpdateClickableItems();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            var nodeTex = ResourceManager.Texture("UI/node1");
            foreach (UniverseScreen.ClickablePlanet cplanet in Screen.ClickablePlanets)
            {
                float radius = 2500f * cplanet.Planet.Scale;
                Screen.DrawCircleProjected(cplanet.Planet.Position, radius, new Color(255, 165, 0, 100), 2f,
                                           nodeTex, new Color(0, 0, 255, 100));
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
                if (scale < 0.15f)
                    scale = 0.15f;

                Vector2 cursorWorldPos = Screen.CursorWorldPosition2D;
                TargetPlanet = null;
                TetherOffset = Vector2.Zero;
                foreach (UniverseScreen.ClickablePlanet p in Screen.ClickablePlanets)
                {
                    if (p.Planet.Position.Distance(cursorWorldPos) <= (2500f * p.Planet.Scale))
                    {
                        TetherOffset = cursorWorldPos - p.Planet.Position;
                        TargetPlanet = p.Planet;
                        batch.DrawLine(p.ScreenPos, Screen.Input.CursorPosition, new Color(255, 165, 0, 150), 3f);
                        batch.DrawString(Fonts.Arial20Bold, "Will Orbit " + p.Planet.Name,
                            new Vector2(Screen.Input.CursorX, Screen.Input.CursorY + 34f), Color.White);
                    }
                }
                batch.Draw(platform, Screen.Input.CursorPosition, new Color(0, 255, 0, 100), 0f, IconOrigin, (float)scale, SpriteEffects.None, 1f);
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

                    float scale = ScaleIconSize((float)size, 0.01f, 0.125f);
                    Screen.DrawTextureSized(platform, posOnScreen, 0.0f, platform.Width * scale,
                                            platform.Height * scale, new Color(0, 255, 0, 100));

                    if (buildTemplate.IsSubspaceProjector)
                    {
                        Screen.DrawCircle(posOnScreen, Player.GetProjectorRadius(), Color.Orange, 2f);
                    }
                    else if (buildTemplate.SensorRange > 0f)
                    {
                        Screen.DrawCircle(posOnScreen, buildTemplate.SensorRange, Color.Orange, 2f);
                    }
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