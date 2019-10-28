using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class DeepSpaceBuildingWindow
    {
        ScrollList<ConstructionListItem> SL;
        Submenu ConstructionSubMenu;
        UniverseScreen screen;
        Rectangle win;
        public Ship itemToBuild;
        Vector2 TetherOffset;
        Guid TargetPlanet = Guid.Empty;


        public DeepSpaceBuildingWindow(UniverseScreen screen)
        {
            this.screen = screen;

            const int windowWidth = 320;
            win = new Rectangle(screen.ScreenWidth - 5 - windowWidth, 260, windowWidth, 225);
            ConstructionSubMenu = new Submenu(win);
            ConstructionSubMenu.AddTab("Build Menu");
            SL = new ScrollList<ConstructionListItem>(ConstructionSubMenu, 40);
            SL.OnClick = (item) => { itemToBuild = item.Ship; };

            //The Doctor: Ensure Projector is always the first entry on the DSBW list so that the player never has to scroll to find it.
            foreach (string s in EmpireManager.Player.structuresWeCanBuild)
            {
                if (s == "Subspace Projector")
                {
                    SL.AddItem(new ConstructionListItem{Ship = ResourceManager.ShipsDict[s]});
                    break;
                }
            }
            foreach (string s in EmpireManager.Player.structuresWeCanBuild)
            {
                if (s != "Subspace Projector")
                {
                    SL.AddItem(new ConstructionListItem{Ship = ResourceManager.ShipsDict[s]});
                }
            }
        }

        class ConstructionListItem : ScrollListEntry<ConstructionListItem>
        {
            public Ship Ship;

            public override void Draw(SpriteBatch batch)
            {
                base.Draw(batch);
                
                SubTexture projector = ResourceManager.Texture("ShipIcons/subspace_projector");
                SubTexture iconProd = ResourceManager.Texture("NewUI/icon_production");

                batch.Draw(Ship.IsSubspaceProjector ? projector 
                          : Ship.shipData.Icon, new Rectangle((int) X, (int) Y, 29, 30), Color.White);

                var tCursor = new Vector2(X + 40f, Y + 3f);
                batch.DrawString(Fonts.Arial10, Ship.Name, tCursor, Color.White);
                tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                batch.DrawString(Fonts.Arial8Bold, Ship.shipData.GetRole(), tCursor, Color.Orange);
                
                var prodiconRect = new Rectangle((int)tCursor.X + 200, (int)tCursor.Y - Fonts.Arial12Bold.LineSpacing, iconProd.Width, iconProd.Height);
                batch.Draw(iconProd, prodiconRect, Color.White);

                tCursor = new Vector2(prodiconRect.X - 60, prodiconRect.CenterY() - Fonts.Arial12Bold.LineSpacing / 2);
                batch.DrawString(Fonts.Arial8Bold, Ship.GetMaintCost(EmpireManager.Player).ToString("F2")+" BC/Y", tCursor, Color.Salmon);

                tCursor = new Vector2(prodiconRect.X + 26, prodiconRect.Y + prodiconRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
                batch.DrawString(Fonts.Arial12Bold, Ship.GetCost(EmpireManager.Player).ToString("F2"), tCursor, Color.White);
            }
        }

        public bool HandleInput(InputState input)
        {
            if (SL.HandleInput(input))
                return true;

            if (itemToBuild == null || win.HitTest(input.CursorPosition) || input.MouseCurr.LeftButton != ButtonState.Pressed || input.MousePrev.LeftButton != ButtonState.Released)
            {
                if (input.RightMouseClick)
                {
                    itemToBuild = null;
                }
                if (!ConstructionSubMenu.HitTest(input.CursorPosition) || !input.RightMouseClick)
                {
                    return false;
                }
                screen.showingDSBW = false;
                return true;
            }

            Vector3 nearPoint = screen.Viewport.Unproject(new Vector3(input.MouseCurr.X, input.MouseCurr.Y, 0f), screen.Projection, screen.View, Matrix.Identity);
            Vector3 farPoint = screen.Viewport.Unproject(new Vector3(input.MouseCurr.X, input.MouseCurr.Y, 1f), screen.Projection, screen.View, Matrix.Identity);
            Vector3 direction = farPoint - nearPoint;
            direction.Normalize();
            Ray pickRay = new Ray(nearPoint, direction);
            float k = -pickRay.Position.Z / pickRay.Direction.Z;
            Vector3 pickedPosition = new Vector3(pickRay.Position.X + k * pickRay.Direction.X, pickRay.Position.Y + k * pickRay.Direction.Y, 0f);

            bool okToBuild = TargetPlanet == Guid.Empty
                              || TargetPlanet != Guid.Empty && !Empire.Universe.PlanetsDict[TargetPlanet].IsOutOfOrbitalsLimit(itemToBuild);

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
            
            lock (GlobalStats.ClickableItemLocker)
            {
                screen.UpdateClickableItems();
            }
            if (!input.KeysCurr.IsKeyDown(Keys.LeftShift) && (!input.KeysCurr.IsKeyDown(Keys.RightShift)))
            {
                itemToBuild = null;
            }
            return true;
        }

        public void Draw(SpriteBatch batch)
        {
            Rectangle r = ConstructionSubMenu.Rect;
            r.Y += 25;
            r.Height -= 25;
            var sel = new Selector(r, new Color(0, 0, 0, 210));

            sel.Draw(batch);
            ConstructionSubMenu.Draw(batch);
            SL.Draw(batch);

            if (itemToBuild != null)
            {
                var platform = ResourceManager.Texture("TacticalIcons/symbol_platform");
                float scale = (float)itemToBuild.SurfaceArea / platform.Width;
                Vector2 IconOrigin = new Vector2((platform.Width / 2f), (platform.Width / 2f));
                scale = scale * 4000f / screen.CamHeight;
                if (scale > 1f)
                {
                    scale = 1f;
                }
                if (scale < 0.15f)
                {
                    scale = 0.15f;
                }

                Vector3 nearPoint = screen.Viewport.Unproject(new Vector3(screen.Input.CursorPosition, 0f), screen.Projection, screen.View, Matrix.Identity);
                Vector3 farPoint = screen.Viewport.Unproject(new Vector3(screen.Input.CursorPosition, 1f), screen.Projection, screen.View, Matrix.Identity);
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
                    foreach (UniverseScreen.ClickablePlanets p in screen.ClickPlanetList)
                    {
                        if (Vector2.Distance(p.planetToClick.Center, pp) > (2500f * p.planetToClick.Scale))
                        {
                            continue;
                        }
                        TetherOffset = pp - p.planetToClick.Center;
                        TargetPlanet = p.planetToClick.guid;
                        batch.DrawLine(p.ScreenPos, screen.Input.CursorPosition, new Color(255, 165, 0, 150), 3f);
                        batch.DrawString(Fonts.Arial20Bold, "Will Orbit "+p.planetToClick.Name,
                            new Vector2(screen.Input.CursorX, screen.Input.CursorY + 34f), Color.White);
                    }
                }
                batch.Draw(platform, screen.Input.CursorPosition, new Color(0, 255, 0, 100), 0f, IconOrigin, scale, SpriteEffects.None, 1f);
            }
        }
    }
}