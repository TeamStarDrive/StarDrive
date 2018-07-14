using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class RefitToWindow : GameScreen
    {
        private readonly ShipListScreen Screen;
        private readonly Ship Shiptorefit;
        private Submenu sub_ships;
        private ScrollList ShipSL;
        private UIButton RefitOne;
        private UIButton RefitAll;
        private string RefitTo;
        private DanButton ConfirmRefit;
        private Selector selector;

        public RefitToWindow(ShipListScreen screen, ShipListScreenEntry entry) : base(screen)
        {
            this.Screen = screen;
            this.Shiptorefit = entry.ship;
            base.IsPopup = true;
            base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
            base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
        }

        public RefitToWindow(GameScreen parent, Ship ship) : base(parent)
        {
            this.Shiptorefit = ship;
            base.IsPopup = true;
            base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
            base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
            base.ScreenManager.SpriteBatch.Begin();
            this.sub_ships.Draw();
            Rectangle r = this.sub_ships.Menu;
            r.Y = r.Y + 25;
            r.Height = r.Height - 25;
            var sel = new Selector(r, new Color(0, 0, 0, 210));
            sel.Draw(ScreenManager.SpriteBatch);
            selector?.Draw(ScreenManager.SpriteBatch);
            this.ShipSL.Draw(base.ScreenManager.SpriteBatch);
            var bCursor = new Vector2(sub_ships.Menu.X + 5, sub_ships.Menu.Y + 25);
            foreach (ScrollList.Entry e in ShipSL.FlattenedEntries)
            {
                Ship ship = ResourceManager.ShipsDict[e.item as string];
                bCursor.Y = (float)e.clickRect.Y;
                base.ScreenManager.SpriteBatch.Draw(ship.shipData.Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ship.Name, tCursor, Color.White);
                tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                if (this.sub_ships.Tabs[0].Selected)
                {
                    base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ship.shipData.GetRole(), tCursor, Color.Orange);
                }
                Rectangle moneyRect = new Rectangle(e.clickRect.X + 165, e.clickRect.Y, 21, 20);
                Vector2 moneyText = new Vector2((float)(moneyRect.X + 25), (float)(moneyRect.Y - 2));
                base.ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_production"), moneyRect, Color.White);
                int refitCost = (int)(ship.GetCost(ship.loyalty) - this.Shiptorefit.GetCost(ship.loyalty));
                if (refitCost < 0)
                {
                    refitCost = 0;
                }
                refitCost = refitCost + 10;
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, refitCost.ToString(), moneyText, Color.White);
            }
            if (this.RefitTo != null)
            {
                this.RefitOne.Draw(base.ScreenManager.SpriteBatch);
                this.RefitAll.Draw(base.ScreenManager.SpriteBatch);
                Vector2 Cursor = new Vector2((float)this.ConfirmRefit.r.X, (float)(this.ConfirmRefit.r.Y + 30));
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, HelperFunctions.ParseText(Fonts.Arial12Bold, string.Concat("Refit ", this.Shiptorefit.Name, " to ", this.RefitTo), 270f), Cursor, Color.White);
            }
            if (base.IsActive)
            {
                ToolTip.Draw(ScreenManager.SpriteBatch);
            }
            base.ScreenManager.SpriteBatch.End();
        }

        public override void ExitScreen()
        {
            Screen?.ResetStatus();
            base.ExitScreen();
        }

        public override bool HandleInput(InputState input)
        {
            this.ShipSL.HandleInput(input);
            if (input.Escaped || input.MouseCurr.RightButton == ButtonState.Pressed)
            {
                this.ExitScreen();
                return true;
            }
            this.selector = null;
            foreach (ScrollList.Entry e in ShipSL.FlattenedEntries)
            {
                if (e.clickRect.HitTest(input.CursorPosition))
                {
                    this.selector = new Selector(e.clickRect);
                    if (input.InGameSelect)
                    {
                        GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        this.RefitTo = e.item as string;
                    }
                }
            }
            if (this.RefitTo != null)
            {
                if (this.RefitOne.Rect.HitTest(input.CursorPosition))
                {
                    ToolTip.CreateTooltip(Localizer.Token(2267));
                    if (input.InGameSelect)
                    {
                        this.Shiptorefit.AI.OrderRefitTo(this.RefitTo);
                        GameAudio.PlaySfxAsync("echo_affirm");
                        this.ExitScreen();
                        return true;
                    }
                }
                if (this.RefitAll.Rect.HitTest(input.CursorPosition))
                {
                    ToolTip.CreateTooltip(Localizer.Token(2268));
                    if (input.InGameSelect)
                    {
                        foreach (Ship ship in EmpireManager.Player.GetShips())
                        {
                            if (ship.Name != this.Shiptorefit.Name)
                            {
                                continue;
                            }
                            ship.AI.OrderRefitTo(this.RefitTo);
                        }
                        GameAudio.PlaySfxAsync("echo_affirm");
                        this.ExitScreen();
                        return true;
                    }
                }
            }
            return base.HandleInput(input);
        }

        public override void LoadContent()
        {
            var shipDesignsRect = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 140, 100, 280, 500);
            sub_ships = new Submenu(shipDesignsRect);
            ShipSL = new ScrollList(sub_ships, 40);
            sub_ships.AddTab("Refit to...");
            foreach (string shipname in Shiptorefit.loyalty.ShipsWeCanBuild)
            {
                Ship weCanBuild = ResourceManager.GetShipTemplate(shipname);
                if (weCanBuild.shipData.Hull != Shiptorefit.shipData.Hull
                    || shipname == Shiptorefit.Name 
                    || weCanBuild.shipData.ShipRole.Protected)
                {
                    continue;
                }
                ShipSL.AddItem(shipname);
            }
            ConfirmRefit = new DanButton(new Vector2(shipDesignsRect.X, (shipDesignsRect.Y + 505)), "Do Refit");

            RefitOne = ButtonLow(shipDesignsRect.X + 25, shipDesignsRect.Y + 505, "", titleId:2265);
            RefitAll = ButtonLow(shipDesignsRect.X + 140, shipDesignsRect.Y + 505, "", titleId:2266);

            base.LoadContent();
        }
    }
}