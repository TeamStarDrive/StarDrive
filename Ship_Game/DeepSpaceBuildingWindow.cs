using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class DeepSpaceBuildingWindow : IDisposable
    {
        private Ship_Game.ScreenManager ScreenManager;

        private ScrollList SL;

        private Submenu ConstructionSubMenu;

        private UniverseScreen screen;

        private Rectangle win;

        private Vector2 TextPos;

        public Ship itemToBuild;

        private Selector selector;

        private Vector2 TetherOffset = new Vector2();

        private Guid TargetPlanet = Guid.Empty;


        public DeepSpaceBuildingWindow(ScreenManager screenManager, UniverseScreen screen)
        {
            this.screen = screen;
            ScreenManager = screenManager;

            const int windowWidth = 320;
            this.win = new Rectangle(screenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 5 - windowWidth, 260, windowWidth, 225);
            this.ConstructionSubMenu = new Submenu(this.win);
            this.ConstructionSubMenu.AddTab("Build Menu");
            this.SL = new ScrollList(this.ConstructionSubMenu, 40);

            //The Doctor: Ensure Subspace Projector is always the first entry on the DSBW list so that the player never has to scroll to find it.
            var buildables = EmpireManager.Player.structuresWeCanBuild;
            foreach (string s in buildables)
            {
                if (s != "Subspace Projector") continue;
                SL.AddItem(ResourceManager.ShipsDict[s], 0, 0);
                break;
            }
            foreach (string s in buildables)
            {
                if (s != "Subspace Projector")
                    SL.AddItem(ResourceManager.ShipsDict[s], 0, 0);
            }
            this.TextPos = new Vector2((float)(this.win.X + this.win.Width / 2) - Fonts.Arial12Bold.MeasureString("Deep Space Construction").X / 2f, (float)(this.win.Y + 25));
        }

       public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

       ~DeepSpaceBuildingWindow() { Destroy(); }

       private void Destroy()
       {
            SL?.Dispose(ref SL);
        }

        public void Draw(GameTime gameTime)
        {
            Rectangle r = this.ConstructionSubMenu.Menu;
            r.Y = r.Y + 25;
            r.Height = r.Height - 25;
            Selector sel = new Selector(r, new Color(0, 0, 0, 210));
            sel.Draw(ScreenManager.SpriteBatch);
            this.ConstructionSubMenu.Draw();
            this.SL.Draw(this.ScreenManager.SpriteBatch);
            Vector2 bCursor = new Vector2((float)(this.ConstructionSubMenu.Menu.X + 20), (float)(this.ConstructionSubMenu.Menu.Y + 45));
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            foreach (ScrollList.Entry e in SL.VisibleEntries)
            {
                bCursor.Y = (float)e.clickRect.Y;
                bCursor.X = (float)e.clickRect.X - 9;
                var ship = (Ship)e.item;
                if (e.clickRectHover != 0)
                {
                    if (ship.Name == "Subspace Projector")
                    {
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("ShipIcons/subspace_projector"), new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                    }
                    else
                    {
                        ScreenManager.SpriteBatch.Draw(ship.shipData.Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                    }
                    

                    Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                    string name = ship.Name;
                    SpriteFont nameFont = Fonts.Arial10;
                    this.ScreenManager.SpriteBatch.DrawString(nameFont, name, tCursor, Color.White);
                    tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, ship.shipData.GetRole(), tCursor, Color.Orange);

                    // Costs and Upkeeps for the deep space build menu - The Doctor
                    
                    string cost = ship.GetCost(EmpireManager.Player).ToString();

                    string upkeep = ship.GetMaintCost(EmpireManager.Player).ToString("F2");
                    
                    Rectangle prodiconRect = new Rectangle((int)tCursor.X + 200, (int)tCursor.Y - Fonts.Arial12Bold.LineSpacing, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_production"), prodiconRect, Color.White);

                    tCursor = new Vector2((float)(prodiconRect.X - 60), (float)(prodiconRect.Y + prodiconRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, string.Concat(upkeep, " BC/Y"), tCursor, Color.Salmon);

                    tCursor = new Vector2((float)(prodiconRect.X + 26), (float)(prodiconRect.Y + prodiconRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, cost, tCursor, Color.White);


                    if (e.Plus != 0)
                    {
                        if (e.PlusHover != 0)
                        {
                            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_build_add_hover2"), e.addRect, Color.White);
                        }
                        else
                        {
                            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_build_add_hover1"), e.addRect, Color.White);
                        }
                    }
                    if (e.Edit != 0)
                    {
                        if (e.EditHover != 0)
                        {
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_edit_hover2"], e.editRect, Color.White);
                        }
                        else
                        {
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_edit_hover1"], e.editRect, Color.White);
                        }
                    }
                    if (e.clickRect.Y == 0)
                    {
                    }
                }
                else
                {
                    if (ship.Name == "Subspace Projector")
                    {
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ShipIcons/subspace_projector"], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                    }
                    else
                    {
                        this.ScreenManager.SpriteBatch.Draw(ship.shipData.Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                    }
                    Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                    string name = ship.Name;
                    SpriteFont nameFont = Fonts.Arial10;
                    this.ScreenManager.SpriteBatch.DrawString(nameFont, name, tCursor, Color.White);
                    tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, ship.shipData.GetRole(), tCursor, Color.Orange);

                    // Costs and Upkeeps for the deep space build menu - The Doctor

                    string cost = ship.GetCost(EmpireManager.Player).ToString();

                    string upkeep = "Doctor rocks";
                    upkeep = ship.GetMaintCost(EmpireManager.Player).ToString("F2");

                    Rectangle prodiconRect = new Rectangle((int)tCursor.X + 200, (int)tCursor.Y - Fonts.Arial12Bold.LineSpacing, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], prodiconRect, Color.White);

                    tCursor = new Vector2((float)(prodiconRect.X - 60), (float)(prodiconRect.Y + prodiconRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, string.Concat(upkeep, " BC/Y"), tCursor, Color.Salmon);

                    tCursor = new Vector2((float)(prodiconRect.X + 26), (float)(prodiconRect.Y + prodiconRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, cost, tCursor, Color.White);

                    if (e.Plus != 0)
                    {
                        if (e.PlusHover != 0)
                        {
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add_hover2"], e.addRect, Color.White);
                        }
                        else
                        {
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add"], e.addRect, Color.White);
                        }
                    }
                    if (e.Edit != 0)
                    {
                        if (e.EditHover != 0)
                        {
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_edit_hover2"], e.editRect, Color.White);
                        }
                        else
                        {
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_edit"], e.editRect, Color.White);
                        }
                    }
                    if (e.clickRect.Y == 0)
                    {
                    }
                }
            }
            if (this.selector != null)
            {
                this.selector.Draw(ScreenManager.SpriteBatch);
            }
            if (this.itemToBuild != null)
            {
                var platform = ResourceManager.TextureDict["TacticalIcons/symbol_platform"];
                float scale = (float)((float)this.itemToBuild.Size) / platform.Width;
                Vector2 IconOrigin = new Vector2((platform.Width / 2f), (platform.Width / 2f));
                scale = scale * 4000f / this.screen.CamHeight;
                if (scale > 1f)
                {
                    scale = 1f;
                }
                if (scale < 0.15f)
                {
                    scale = 0.15f;
                }
                Vector3 nearPoint = screen.Viewport.Unproject(new Vector3(MousePos.X, MousePos.Y, 0f), this.screen.projection, this.screen.view, Matrix.Identity);
                Vector3 farPoint = screen.Viewport.Unproject(new Vector3(MousePos.X, MousePos.Y, 1f), this.screen.projection, this.screen.view, Matrix.Identity);
                Vector3 direction = farPoint - nearPoint;
                direction.Normalize();
                Ray pickRay = new Ray(nearPoint, direction);
                float k = -pickRay.Position.Z / pickRay.Direction.Z;
                Vector3 pickedPosition = new Vector3(pickRay.Position.X + k * pickRay.Direction.X, pickRay.Position.Y + k * pickRay.Direction.Y, 0f);
                Vector2 pp = new Vector2(pickedPosition.X, pickedPosition.Y);
                this.TargetPlanet = Guid.Empty;
                this.TetherOffset = Vector2.Zero;
                lock (GlobalStats.ClickableSystemsLock)
                {
                    foreach (UniverseScreen.ClickablePlanets p in this.screen.ClickPlanetList)
                    {
                        if (Vector2.Distance(p.planetToClick.Center, pp) > (2500f * p.planetToClick.Scale))
                        {
                            continue;
                        }
                        this.TetherOffset = pp - p.planetToClick.Center;
                        this.TargetPlanet = p.planetToClick.guid;
                        this.ScreenManager.SpriteBatch.DrawLine(p.ScreenPos, MousePos, new Color(255, 165, 0, 150), 3f);
                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat("Will Orbit ", p.planetToClick.Name), new Vector2(MousePos.X, MousePos.Y + 34f), Color.White);
                    }
                }
                Rectangle? nullable = null;
                this.ScreenManager.SpriteBatch.Draw(platform, MousePos, nullable, new Color(0, 255, 0, 100), 0f, IconOrigin, scale, SpriteEffects.None, 1f);
            }
        }

        public bool HandleInput(InputState input)
        {
            this.selector = null;
            this.SL.HandleInput(input);
            foreach (ScrollList.Entry e in SL.AllEntries)
            {
                if (!e.clickRect.HitTest(input.CursorPosition))
                {
                    e.clickRectHover = 0;
                }
                else
                {
                    this.selector = new Selector(e.clickRect);
                    if (e.clickRectHover == 0)
                    {
                        GameAudio.PlaySfxAsync("sd_ui_mouseover");
                    }
                    e.clickRectHover = 1;
                    if (input.LeftMouseClick)
                    {
                        this.itemToBuild = e.item as Ship;
                        return true;
                    }
                }
            }
            if (this.itemToBuild == null || this.win.HitTest(input.CursorPosition) || input.MouseCurr.LeftButton != ButtonState.Pressed || input.MousePrev.LeftButton != ButtonState.Released)
            {
                if (input.RightMouseClick)
                {
                    this.itemToBuild = null;
                }
                if (!this.ConstructionSubMenu.Menu.HitTest(input.CursorPosition) || !input.RightMouseClick)
                {
                    return false;
                }
                this.screen.showingDSBW = false;
                return true;
            }
            Vector3 nearPoint = screen.Viewport.Unproject(new Vector3((float)input.MouseCurr.X, (float)input.MouseCurr.Y, 0f), this.screen.projection, this.screen.view, Matrix.Identity);
            Vector3 farPoint = screen.Viewport.Unproject(new Vector3((float)input.MouseCurr.X, (float)input.MouseCurr.Y, 1f), this.screen.projection, this.screen.view, Matrix.Identity);
            Vector3 direction = farPoint - nearPoint;
            direction.Normalize();
            Ray pickRay = new Ray(nearPoint, direction);
            float k = -pickRay.Position.Z / pickRay.Direction.Z;
            Vector3 pickedPosition = new Vector3(pickRay.Position.X + k * pickRay.Direction.X, pickRay.Position.Y + k * pickRay.Direction.Y, 0f);
            Goal buildstuff = new BuildConstructionShip(pickedPosition.ToVec2(), itemToBuild.Name, EmpireManager.Player);
            if (this.TargetPlanet != Guid.Empty)
            {
                buildstuff.TetherOffset = this.TetherOffset;
                buildstuff.TetherTarget = this.TargetPlanet;
            }
            EmpireManager.Player.GetGSAI().Goals.Add(buildstuff);
            GameAudio.PlaySfxAsync("echo_affirm");
            lock (GlobalStats.ClickableItemLocker)
            {
                this.screen.UpdateClickableItems();
            }
            if (!input.KeysCurr.IsKeyDown(Keys.LeftShift) && (!input.KeysCurr.IsKeyDown(Keys.RightShift)))
            {
                this.itemToBuild = null;
            }
            return true;
        }
    }
}