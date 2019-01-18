using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
            Screen = screen;
            Shiptorefit = entry.ship;
            IsPopup = true;
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
        }

        public RefitToWindow(GameScreen parent, Ship ship) : base(parent)
        {
            Shiptorefit = ship;
            IsPopup = true;
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            sub_ships.Draw(batch);
            Rectangle r = sub_ships.Menu;
            r.Y += 25;
            r.Height -= 25;
            var sel = new Selector(r, new Color(0, 0, 0, 210));
            sel.Draw(batch);
            selector?.Draw(batch);
            ShipSL.Draw(batch);
            var bCursor = new Vector2(sub_ships.Menu.X + 5, sub_ships.Menu.Y + 25);
            foreach (ScrollList.Entry e in ShipSL.VisibleExpandedEntries)
            {
                Ship ship = ResourceManager.ShipsDict[e.item as string];
                bCursor.Y = e.Y;
                batch.Draw(ship.shipData.Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                batch.DrawString(Fonts.Arial12Bold, ship.Name, tCursor, Color.White);
                tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                if (sub_ships.Tabs[0].Selected)
                {
                    batch.DrawString(Fonts.Arial12Bold, ship.shipData.GetRole(), tCursor, Color.Orange);
                }
                Rectangle moneyRect = new Rectangle(e.X + 165, e.Y, 21, 20);
                Vector2 moneyText = new Vector2((moneyRect.X + 25), (moneyRect.Y - 2));
                batch.Draw(ResourceManager.Texture("NewUI/icon_production"), moneyRect, Color.White);
                int refitCost = (int)(ship.GetCost(ship.loyalty) - Shiptorefit.GetCost(ship.loyalty));
                if (refitCost < 0)
                {
                    refitCost = 0;
                }
                refitCost = refitCost + 10;
                batch.DrawString(Fonts.Arial12Bold, refitCost.ToString(), moneyText, Color.White);
            }
            if (RefitTo != null)
            {
                var cursor = new Vector2(ConfirmRefit.r.X, (ConfirmRefit.r.Y + 30));
                string text = Fonts.Arial12Bold.ParseText($"Refit {Shiptorefit.Name} to {RefitTo}", 270f);
                batch.DrawString(Fonts.Arial12Bold, text, cursor, Color.White);
            }
            if (IsActive)
            {
                ToolTip.Draw(batch);
            }
            base.Draw(batch);
            batch.End();
        }

        public override void ExitScreen()
        {
            Screen?.ResetStatus();
            base.ExitScreen();
        }

        private void OnRefitOneClicked(UIButton b)
        {
            Shiptorefit.AI.OrderRefitTo(RefitTo);
            GameAudio.EchoAffirmative();
            ExitScreen();
        }

        private void OnRefitAllClicked(UIButton b)
        {
            foreach (Ship ship in EmpireManager.Player.GetShips())
            {
                if (ship.Name == Shiptorefit.Name)
                    ship.AI.OrderRefitTo(RefitTo);
            }
            GameAudio.EchoAffirmative();
            ExitScreen();
        }

        public override bool HandleInput(InputState input)
        {
            ShipSL.HandleInput(input);
            if (input.Escaped || input.MouseCurr.RightButton == ButtonState.Pressed)
            {
                ExitScreen();
                return true;
            }
            selector = null;
            foreach (ScrollList.Entry e in ShipSL.VisibleExpandedEntries)
            {
                if (e.CheckHover(input))
                {
                    selector = e.CreateSelector();
                    if (input.InGameSelect)
                    {
                        GameAudio.AcceptClick();
                        RefitTo = e.Get<string>();
                    }
                }
            }

            RefitOne.Enabled = RefitTo != null;
            RefitAll.Enabled = RefitTo != null;
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

            RefitOne = ButtonLow(shipDesignsRect.X + 25, shipDesignsRect.Y + 505, titleId:2265, click: OnRefitOneClicked);
            RefitOne.Tooltip = Localizer.Token(2267);
            RefitAll = ButtonLow(shipDesignsRect.X + 140, shipDesignsRect.Y + 505, titleId:2266, click: OnRefitAllClicked);
            RefitAll.Tooltip = Localizer.Token(2268);

            base.LoadContent();
        }
    }
}