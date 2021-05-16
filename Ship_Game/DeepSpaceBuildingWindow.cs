using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class DeepSpaceBuildingWindow : UIElementContainer
    {
        readonly UniverseScreen Screen;
        ScrollList2<ConstructionListItem> SL;
        public Ship itemToBuild;
        Vector2 TetherOffset;
        Guid TargetPlanet = Guid.Empty;
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
            Rect = new Rectangle(Screen.ScreenWidth - 15 - windowWidth, 100, windowWidth, 300);

            var background = new Submenu(Rect);
            background.Background = new Selector(Rect.CutTop(25), new Color(0, 0, 0, 210)); // Black fill
            background.AddTab("Build Menu");
            SL = Add(new ScrollList2<ConstructionListItem>(background, 40));
            SL.OnClick = (item) => { itemToBuild = item.Ship; };
            SL.EnableItemHighlight = true;

            //The Doctor: Ensure Projector is always the first entry on the DSBW list so that the player never has to scroll to find it.
            foreach (string s in EmpireManager.Player.structuresWeCanBuild)
            {
                if (s == "Subspace Projector")
                {
                    SL.AddItem(new ConstructionListItem{Ship = ResourceManager.GetShipTemplate(s)});
                    break;
                }
            }
            foreach (string s in EmpireManager.Player.structuresWeCanBuild)
            {
                if (s != "Subspace Projector")
                {
                    SL.AddItem(new ConstructionListItem{Ship = ResourceManager.GetShipTemplate(s)});
                }
            }

            ShipInfoOverlay = Add(new ShipInfoOverlayComponent(Screen));
            SL.OnHovered = (item) =>
            {
                ShipInfoOverlay.ShowToLeftOf(item?.Pos ?? Vector2.Zero, item?.Ship);
            };

            Visible = true;
        }

        class ConstructionListItem : ScrollListItem<ConstructionListItem>
        {
            public Ship Ship;

            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                base.Draw(batch, elapsed);
                
                SubTexture projector = ResourceManager.Texture("ShipIcons/subspace_projector");
                SubTexture iconProd = ResourceManager.Texture("NewUI/icon_production");

                SubTexture icon = Ship.IsSubspaceProjector ? projector : Ship.shipData.Icon;
                float iconSize = Height;
                batch.Draw(icon, new Vector2(X, Y), new Vector2(iconSize));
          
                batch.DrawString(Fonts.Arial10, Ship.Name, X+iconSize+2, Y+4);
                batch.DrawString(Fonts.Arial8Bold, Ship.shipData.GetRole(), X+iconSize+2, Y+18, Color.Orange);

                float prodX = Right - 120;
                batch.DrawString(Fonts.Arial8Bold, Ship.GetMaintCost(EmpireManager.Player).String(2)+" BC/Y", prodX, Y+4, Color.Salmon); // Maintenance Cost
                batch.Draw(iconProd, new Vector2(prodX+50, Y+4), iconProd.SizeF); // Production Icon
                batch.DrawString(Fonts.Arial12Bold, Ship.GetCost(EmpireManager.Player).String(1), prodX+50+iconProd.Width+2, Y+4); // Build Production Cost
            }
        }

        public override bool HandleInput(InputState input)
        {
            if (!Visible || !Enabled)
                return false;

            // only capture input from window UI if we haven't made a selection
            if (itemToBuild == null)
            {
                if (input.Escaped) // exit this screen
                {
                    Screen.InputOpenDeepSpaceBuildWindow();
                    return true;
                }

                return base.HandleInput(input);
            }

            // we have itemToBuild, so we are in placement mode

            bool hovered = Rect.HitTest(input.CursorPosition);
            if (hovered) // disallow input while in placement and hovering our window
            {
                if (input.RightMouseClick)
                    itemToBuild = null;

                ShipInfoOverlay.Show();
                return true;
            }
            ShipInfoOverlay.Hide();

            // right mouse click or Esc, we cancel the placement mode
            if (input.RightMouseClick || input.Escaped)
            {
                itemToBuild = null;
                return true;
            }

            // left mouse clicked while not hovering our window? place it!
            if (input.LeftMouseClick)
            {
                TryPlaceBuildable(input);

                if (input.IsShiftKeyDown) // if we hold down shift, continue placing next frame
                    return true;

                itemToBuild = null;
                return true;
            }

            return false;
        }

        void TryPlaceBuildable(InputState input)
        {
            Vector3 nearPoint = Screen.Viewport.Unproject(new Vector3(input.CursorPosition, 0f),
                Screen.Projection, Screen.View, Matrix.Identity);
            Vector3 farPoint = Screen.Viewport.Unproject(new Vector3(input.CursorPosition, 1f),
                Screen.Projection, Screen.View, Matrix.Identity);
            var pickRay = new Ray(nearPoint, nearPoint.DirectionToTarget(farPoint));
            float k = -pickRay.Position.Z / pickRay.Direction.Z;
            var pickedPosition = new Vector3(pickRay.Position.X + k * pickRay.Direction.X,
                pickRay.Position.Y + k * pickRay.Direction.Y, 0f);

            bool okToBuild = TargetPlanet == Guid.Empty
                             || TargetPlanet != Guid.Empty &&
                             !Empire.Universe.PlanetsDict[TargetPlanet].IsOutOfOrbitalsLimit(itemToBuild);

            if (okToBuild)
            {
                Goal buildStuff = new BuildConstructionShip(pickedPosition.ToVec2(), itemToBuild.Name, EmpireManager.Player);
                if (TargetPlanet != Guid.Empty)
                {
                    buildStuff.TetherOffset = TetherOffset;
                    buildStuff.TetherTarget = TargetPlanet;
                }

                EmpireManager.Player.GetEmpireAI().Goals.Add(buildStuff);
                GameAudio.EchoAffirmative();
            }
            else
                GameAudio.NegativeClick();

            Screen.UpdateClickableItems();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            var nodeTex = ResourceManager.Texture("UI/node1");
            lock (GlobalStats.ClickableSystemsLock)
            {
                foreach (UniverseScreen.ClickablePlanets cplanet in Screen.ClickPlanetList)
                {
                    float radius = 2500f * cplanet.planetToClick.Scale;
                    Screen.DrawCircleProjected(cplanet.planetToClick.Center, radius, new Color(255, 165, 0, 150), 1f,
                        nodeTex, new Color(0, 0, 255, 50));
                }
            }

            base.Draw(batch, elapsed);

            if (itemToBuild != null)
            {
                var platform = ResourceManager.Texture("TacticalIcons/symbol_platform");
                float scale = (float)itemToBuild.SurfaceArea / platform.Width;
                Vector2 IconOrigin = new Vector2((platform.Width / 2f), (platform.Width / 2f));
                scale = scale * 4000f / Screen.CamHeight;
                if (scale > 1f)
                {
                    scale = 1f;
                }
                if (scale < 0.15f)
                {
                    scale = 0.15f;
                }

                Vector3 nearPoint = Screen.Viewport.Unproject(new Vector3(Screen.Input.CursorPosition, 0f), Screen.Projection, Screen.View, Matrix.Identity);
                Vector3 farPoint = Screen.Viewport.Unproject(new Vector3(Screen.Input.CursorPosition, 1f), Screen.Projection, Screen.View, Matrix.Identity);
                Vector3 direction = farPoint - nearPoint;
                direction.Normalize();
                Ray pickRay = new Ray(nearPoint, direction);
                float k = -pickRay.Position.Z / pickRay.Direction.Z;
                Vector3 pickedPosition = new Vector3(pickRay.Position.X + k * pickRay.Direction.X, pickRay.Position.Y + k * pickRay.Direction.Y, 0f);
                Vector2 pp = new Vector2(pickedPosition.X, pickedPosition.Y);
                TargetPlanet = Guid.Empty;
                TetherOffset = Vector2.Zero;
                lock (GlobalStats.ClickableSystemsLock)
                {
                    foreach (UniverseScreen.ClickablePlanets p in Screen.ClickPlanetList)
                    {
                        if (Vector2.Distance(p.planetToClick.Center, pp) > (2500f * p.planetToClick.Scale))
                        {
                            continue;
                        }
                        TetherOffset = pp - p.planetToClick.Center;
                        TargetPlanet = p.planetToClick.guid;
                        batch.DrawLine(p.ScreenPos, Screen.Input.CursorPosition, new Color(255, 165, 0, 150), 3f);
                        batch.DrawString(Fonts.Arial20Bold, "Will Orbit "+p.planetToClick.Name,
                            new Vector2(Screen.Input.CursorX, Screen.Input.CursorY + 34f), Color.White);
                    }
                }
                batch.Draw(platform, Screen.Input.CursorPosition, new Color(0, 255, 0, 100), 0f, IconOrigin, scale, SpriteEffects.None, 1f);
            }
        }

        float CurrentRadiusSmoothed;

        public void DrawBlendedBuildIcons()
        {
            if (!Visible)
                return;

            lock (GlobalStats.ClickableItemLocker)
            {
                var platform = ResourceManager.Texture("TacticalIcons/symbol_platform");
                for (int i = 0; i < Screen.ItemsToBuild.Count; ++i)
                {
                    UniverseScreen.ClickableItemUnderConstruction item = Screen.ItemsToBuild[i];

                    if (ResourceManager.GetShipTemplate(item.UID, out Ship buildTemplate))
                    {
                        Screen.ProjectToScreenCoords(item.BuildPos, platform.Width, out Vector2 posOnScreen, out float size);

                        float scale = ScaleIconSize(size, 0.01f, 0.125f);
                        Screen.DrawTextureSized(platform, posOnScreen, 0.0f, platform.Width * scale,
                                   platform.Height * scale, new Color(0, 255, 0, 100));

                        if (item.UID == "Subspace Projector")
                        {
                            Screen.DrawCircle(posOnScreen, EmpireManager.Player.GetProjectorRadius(), Color.Orange, 2f);
                        }
                        else if (buildTemplate.SensorRange > 0f)
                        {
                            Screen.DrawCircle(posOnScreen, buildTemplate.SensorRange, Color.Orange, 2f);
                        }
                    }
                }
            }

            // show the object placement/build circle
            if (itemToBuild != null && itemToBuild.IsSubspaceProjector && Screen.AdjustCamTimer <= 0f)
            {
                Vector2 center = Screen.Input.CursorPosition;
                float screenRadius = Screen.ProjectToScreenSize(EmpireManager.Player.GetProjectorRadius());
                Screen.DrawCircle(center, MathExt.SmoothStep(ref CurrentRadiusSmoothed, screenRadius, 0.3f),
                                  Color.Orange, 2f); //
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