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
        private ScreenManager ScreenManager;

        private ScrollList SL;

        private Submenu ConstructionSubMenu;

        private UniverseScreen screen;

        private Rectangle win;

        private Vector2 TextPos;

        public Ship itemToBuild;

        private Selector selector;

        private Vector2 TetherOffset;

        private Guid TargetPlanet = Guid.Empty;


        public DeepSpaceBuildingWindow(ScreenManager screenManager, UniverseScreen screen)
        {
            this.screen = screen;
            ScreenManager = screenManager;

            const int windowWidth = 320;
            win = new Rectangle(screenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 5 - windowWidth, 260, windowWidth, 225);
            ConstructionSubMenu = new Submenu(win);
            ConstructionSubMenu.AddTab("Build Menu");
            SL = new ScrollList(ConstructionSubMenu, 40);

            //The Doctor: Ensure Projector is always the first entry on the DSBW list so that the player never has to scroll to find it.
            var buildables = EmpireManager.Player.structuresWeCanBuild;
            foreach (string s in buildables)
            {
                if (s == "Subspace Projector")
                {
                    SL.AddItem(ResourceManager.ShipsDict[s], false, false);
                    break;
                }
            }
            foreach (string s in buildables)
            {
                if (s != "Subspace Projector")
                    SL.AddItem(ResourceManager.ShipsDict[s], false, false);
            }
            TextPos = new Vector2(win.X + win.Width / 2 - Fonts.Arial12Bold.MeasureString("Deep Space Construction").X / 2f, win.Y + 25);
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
            var bCursor = new Vector2(ConstructionSubMenu.X + 20, ConstructionSubMenu.Y + 45);

            SubTexture projector = ResourceManager.Texture("ShipIcons/subspace_projector");
            SubTexture iconProd = ResourceManager.Texture("NewUI/icon_production");

            Vector2 mousePos = Mouse.GetState().Pos();
            foreach (ScrollList.Entry e in SL.VisibleEntries)
            {
                bCursor.Y = e.Y;
                bCursor.X = (float)e.X - 9;
                var ship = e.Get<Ship>();
                if (e.Hovered)
                {
                    batch.Draw(ship.IsSubspaceProjector ? projector
                              : ship.shipData.Icon, new Rectangle((int) bCursor.X, (int) bCursor.Y, 29, 30), Color.White);


                    var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                    string name = ship.Name;
                    SpriteFont nameFont = Fonts.Arial10;
                    batch.DrawString(nameFont, name, tCursor, Color.White);
                    tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                    batch.DrawString(Fonts.Arial8Bold, ship.shipData.GetRole(), tCursor, Color.Orange);

                    // Costs and Upkeeps for the deep space build menu - The Doctor
                    
                    string cost = ship.GetCost(EmpireManager.Player).ToString("F2");
                    string upkeep = ship.GetMaintCost(EmpireManager.Player).ToString("F2");
                    
                    var prodiconRect = new Rectangle((int)tCursor.X + 200, (int)tCursor.Y - Fonts.Arial12Bold.LineSpacing, iconProd.Width, iconProd.Height);
                    batch.Draw(iconProd, prodiconRect, Color.White);

                    tCursor = new Vector2(prodiconRect.X - 60, prodiconRect.Y + prodiconRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
                    batch.DrawString(Fonts.Arial8Bold, string.Concat(upkeep, " BC/Y"), tCursor, Color.Salmon);

                    tCursor = new Vector2(prodiconRect.X + 26, prodiconRect.Y + prodiconRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
                    batch.DrawString(Fonts.Arial12Bold, cost, tCursor, Color.White);

                    e.DrawPlusEdit(batch);
                }
                else
                {
                    if (ship.IsSubspaceProjector)
                    {
                        batch.Draw(ResourceManager.Texture("ShipIcons/subspace_projector"), new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                    }
                    else
                    {
                        batch.Draw(ship.shipData.Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                    }
                    var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                    string name = ship.Name;
                    SpriteFont nameFont = Fonts.Arial10;
                    batch.DrawString(nameFont, name, tCursor, Color.White);
                    tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                    batch.DrawString(Fonts.Arial8Bold, ship.shipData.GetRole(), tCursor, Color.Orange);

                    // Costs and Upkeeps for the deep space build menu - The Doctor

                    string cost = ship.GetCost(EmpireManager.Player).ToString();
                    string upkeep = ship.GetMaintCost(EmpireManager.Player).ToString("F2");

                    Rectangle prodiconRect = new Rectangle((int)tCursor.X + 200, (int)tCursor.Y - Fonts.Arial12Bold.LineSpacing, ResourceManager.Texture("NewUI/icon_production").Width, ResourceManager.Texture("NewUI/icon_production").Height);
                    batch.Draw(ResourceManager.Texture("NewUI/icon_production"), prodiconRect, Color.White);

                    tCursor = new Vector2(prodiconRect.X - 60, prodiconRect.Y + prodiconRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
                    batch.DrawString(Fonts.Arial8Bold, string.Concat(upkeep, " BC/Y"), tCursor, Color.Salmon);

                    tCursor = new Vector2(prodiconRect.X + 26, prodiconRect.Y + prodiconRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
                    batch.DrawString(Fonts.Arial12Bold, cost, tCursor, Color.White);

                    e.DrawPlusEdit(batch);
                }
            }
            if (selector != null)
            {
                selector.Draw(batch);
            }
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
                Vector3 nearPoint = screen.Viewport.Unproject(new Vector3(mousePos.X, mousePos.Y, 0f), screen.Projection, screen.View, Matrix.Identity);
                Vector3 farPoint = screen.Viewport.Unproject(new Vector3(mousePos.X, mousePos.Y, 1f), screen.Projection, screen.View, Matrix.Identity);
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
                        batch.DrawLine(p.ScreenPos, mousePos, new Color(255, 165, 0, 150), 3f);
                        batch.DrawString(Fonts.Arial20Bold, string.Concat("Will Orbit ", p.planetToClick.Name), new Vector2(mousePos.X, mousePos.Y + 34f), Color.White);
                    }
                }
                batch.Draw(platform, mousePos, new Color(0, 255, 0, 100), 0f, IconOrigin, scale, SpriteEffects.None, 1f);
            }
        }

        public bool HandleInput(InputState input)
        {
            selector = null;
            SL.HandleInput(input);
            foreach (ScrollList.Entry e in SL.AllEntries)
            {
                if (e.CheckHover(input))
                {
                    selector = e.CreateSelector();

                    if (input.LeftMouseClick)
                    {
                        itemToBuild = e.item as Ship;
                        return true;
                    }
                }
            }
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
                              || TargetPlanet != Guid.Empty 
                              && !Empire.Universe.GetPlanet(TargetPlanet).IsOutOfOrbitalsLimit(itemToBuild, EmpireManager.Player);

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

        public void Update(float deltaTime)
        {

        }
    }
}