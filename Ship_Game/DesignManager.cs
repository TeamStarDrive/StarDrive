using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class DesignManager : GameScreen
    {
        private readonly ShipDesignScreen screen;
        private readonly string ShipName;
        private Selector selector;
        private Submenu SaveShips;
        private Menu1 SaveMenu;
        private Rectangle Window;
        private Vector2 TitlePosition;
        private Vector2 EnternamePos;

        private readonly UITextEntry EnterNameArea = new UITextEntry();
        private UIButton Save;

        private Submenu subAllDesigns;
        private ScrollList ShipDesigns;
        private MouseState currentMouse;
        private MouseState previousMouse;

        public DesignManager(ShipDesignScreen screen, string txt) : base(screen)
        {
            this.ShipName = txt;
            this.screen = screen;
            base.IsPopup = true;
            base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
            base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
        }

        protected override void Destroy()
        {
            base.Destroy();
        }

        public override void Draw(SpriteBatch batch)
        {
            base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
            batch.Begin();
            this.SaveMenu.Draw();
            this.SaveShips.Draw();
            this.EnterNameArea.Draw(Fonts.Arial20Bold, batch, this.EnternamePos, 
                Game1.Instance.GameTime, (this.EnterNameArea.Hover ? Color.White : new Color(255, 239, 208)));
            this.subAllDesigns.Draw();
            this.ShipDesigns.Draw(batch);
            var bCursor = new Vector2((float)(this.subAllDesigns.Menu.X + 20), (float)(this.subAllDesigns.Menu.Y + 20));
            foreach (ScrollList.Entry e in ShipDesigns.VisibleEntries)
            {
                var ship = (Ship)e.item;
                bCursor.Y = (float)e.Y;
                batch.Draw(ship.shipData.Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);   
                var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                batch.DrawString(Fonts.Arial12Bold, ship.Name, tCursor, Color.White);
                tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                batch.DrawString(Fonts.Arial8Bold, ship.shipData.GetRole(), tCursor, Color.Orange);
                e.DrawPlusEdit(batch);
            }
            selector?.Draw(batch);
            foreach (UIButton b in this.Buttons)
            {
                b.Draw(batch);
            }
            batch.End();
        }

        public override bool HandleInput(InputState input)
        {
            this.currentMouse = input.MouseCurr;
            this.ShipDesigns.HandleInput(input);
            if (input.Escaped || input.RightMouseClick)
            {
                this.ExitScreen();
                return true;
            }
            this.selector = null;
            foreach (ScrollList.Entry e in ShipDesigns.AllEntries)
            {
                if (e.CheckHover(input))
                {
                    this.selector = e.CreateSelector();

                    if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
                    {
                        this.EnterNameArea.Text = ((Ship)e.item).Name;
                        GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    }
                }
            }
            foreach (UIButton b in this.Buttons)
            {
                if (!b.Rect.HitTest(input.CursorPosition))
                {
                    b.State = UIButton.PressState.Default;
                }
                else
                {
                    if (b.State != UIButton.PressState.Hover && b.State != UIButton.PressState.Pressed)
                    {
                        GameAudio.PlaySfxAsync("mouse_over4");
                    }
                    b.State = UIButton.PressState.Hover;
                    if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Pressed)
                    {
                        b.State = UIButton.PressState.Pressed;
                    }
                    if (this.currentMouse.LeftButton != ButtonState.Pressed || this.previousMouse.LeftButton != ButtonState.Released)
                    {
                        continue;
                    }
                    string text = b.Text;
                    if (text == null || text != "Save")
                    {
                        continue;
                    }
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    GlobalStats.TakingInput = false;
                    this.EnterNameArea.HandlingInput = false;
                    this.TrySave();
                }
            }
            this.EnterNameArea.ClickableArea = new Rectangle((int)this.EnternamePos.X, (int)this.EnternamePos.Y, 200, 30);
            if (!this.EnterNameArea.ClickableArea.HitTest(input.CursorPosition))
            {
                this.EnterNameArea.Hover = false;
            }
            else
            {
                this.EnterNameArea.Hover = true;
                if (this.currentMouse.LeftButton == ButtonState.Released && this.previousMouse.LeftButton == ButtonState.Pressed)
                {
                    this.EnterNameArea.HandlingInput = true;
                }
            }
            if (!this.EnterNameArea.HandlingInput)
            {
                GlobalStats.TakingInput = false;
            }
            else
            {
                GlobalStats.TakingInput = true;
                EnterNameArea.HandleTextInput(ref EnterNameArea.Text, input);
                if (input.IsKeyDown(Keys.Enter))
                {
                    EnterNameArea.HandlingInput = false;
                }
            }
            this.previousMouse = input.MousePrev;
            return base.HandleInput(input);
        }

        public override void LoadContent()
        {
            this.Window = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 250, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 500, 600);
            this.SaveMenu = new Menu1(this.Window);
            Rectangle sub = new Rectangle(this.Window.X + 20, this.Window.Y + 20, this.Window.Width - 40, 80);
            this.SaveShips = new Submenu(sub);
            this.SaveShips.AddTab("Save Ship Design");
            Vector2 Cursor = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 84), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 100));
            this.TitlePosition = new Vector2((float)(sub.X + 20), (float)(sub.Y + 45));
            Rectangle scrollList = new Rectangle(sub.X, sub.Y + 90, sub.Width, this.Window.Height - sub.Height - 50);
            this.subAllDesigns = new Submenu(scrollList);
            this.subAllDesigns.AddTab("All Designs");
            this.ShipDesigns = new ScrollList(this.subAllDesigns);
            foreach (KeyValuePair<string, Ship> Ship in ResourceManager.ShipsDict)
            {
                this.ShipDesigns.AddItem(Ship.Value);
            }
            this.EnternamePos = this.TitlePosition;
            this.EnterNameArea.ClickableArea = new Rectangle((int)(this.EnternamePos.X + Fonts.Arial20Bold.MeasureString("Design Name: ").X), (int)this.EnternamePos.Y - 2, 256, Fonts.Arial20Bold.LineSpacing);
            this.EnterNameArea.Text = this.ShipName;

            Save = ButtonSmall(sub.X + sub.Width - 88, this.EnterNameArea.ClickableArea.Y - 2, "Save", "Save");

            Cursor.Y = Cursor.Y + (ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height + 15);
            base.LoadContent();
        }

        private void OverWriteAccepted(object sender, EventArgs e)
        {
            GameAudio.PlaySfxAsync("echo_affirm1");
            screen?.SaveShipDesign(EnterNameArea.Text);

            Empire emp = EmpireManager.Player;
            Ship ship = ResourceManager.ShipsDict[EnterNameArea.Text];
            try
            {
                ship.BaseStrength = ship.GetStrength();
                foreach (Planet p in emp.GetPlanets())
                {
                    foreach (QueueItem qi in p.ConstructionQueue)
                    {
                        if (!qi.isShip || qi.sData.Name != EnterNameArea.Text)
                            continue;
                        qi.sData = ship.shipData;
                        qi.Cost = ship.GetCost(emp);
                    }
                }
            }
            catch (Exception x)
            {
                Log.Error(x, "Failed to set strength or rename duing ship save");
            }
            ExitScreen();
        }

        private void TrySave()
        {
            bool saveOk = true;
            bool reserved = false;
            foreach (Ship ship in ResourceManager.ShipsDict.Values)
            {
                if (EnterNameArea.Text != ship.Name)
                    continue;
                saveOk = false;
                reserved |= ship.IsReadonlyDesign;
            }

            if (reserved && !Empire.Universe.Debug)
            {
                GameAudio.PlaySfxAsync("UI_Misc20");
                var messageBox = new MessageBoxScreen(this, $"{EnterNameArea.Text} is a reserved ship name and you cannot overwrite this design");
                ScreenManager.AddScreen(messageBox);
                return;
            }
            if (!saveOk)
            {
                var messageBox = new MessageBoxScreen(this, "Design name already exists.  Overwrite?");
                messageBox.Accepted += OverWriteAccepted;
                ScreenManager.AddScreen(messageBox);
                return;
            }
            GameAudio.PlaySfxAsync("echo_affirm1");
            screen?.SaveShipDesign(EnterNameArea.Text);
            ExitScreen();
        }
    }
}