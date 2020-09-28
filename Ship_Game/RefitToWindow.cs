using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Audio;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class RefitToWindow : GameScreen
    {
        readonly ShipListScreen Screen;
        readonly Ship ShipToRefit;
        Submenu sub_ships;
        ScrollList2<RefitShipListItem> RefitShipList;
        UIButton RefitOne;
        UIButton RefitAll;
        Ship RefitTo;
        DanButton ConfirmRefit;
        ShipInfoOverlayComponent ShipInfoOverlay;

        public RefitToWindow(ShipListScreen screen, ShipListScreenItem item) : base(screen)
        {
            Screen = screen;
            ShipToRefit = item.ship;
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
        }

        public RefitToWindow(GameScreen parent, Ship ship) : base(parent)
        {
            ShipToRefit = ship;
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
        }

        class RefitShipListItem : ScrollListItem<RefitShipListItem>
        {
            readonly RefitToWindow Screen;
            public readonly Ship Ship;

            public RefitShipListItem(RefitToWindow screen, Ship template)
            {
                Screen = screen;
                Ship = template;
            }
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                batch.Draw(Ship.shipData.Icon, new Rectangle((int)X, (int)Y, 29, 30), Color.White);

                var tCursor = new Vector2(X + 40f, Y + 3f);
                batch.DrawString(Fonts.Arial12Bold, Ship.Name, tCursor, Color.White);

                if (Screen.sub_ships.SelectedIndex == 0)
                {
                    tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                    batch.DrawString(Fonts.Arial12Bold, Ship.shipData.GetRole(), tCursor, Color.Orange);
                }

                var moneyRect = new Rectangle((int)X + 285, (int)Y, 21, 20);
                var moneyText = new Vector2((moneyRect.X + 25), (moneyRect.Y - 2));
                batch.Draw(ResourceManager.Texture("NewUI/icon_production"), moneyRect, Color.White);
                int refitCost = Screen.ShipToRefit.RefitCost(Ship);
                batch.DrawString(Fonts.Arial12Bold, refitCost.ToString(), moneyText, Color.White);
            }
        }

        public override void LoadContent()
        {
            var shipDesignsRect = new Rectangle(ScreenWidth / 2 - 200, 200, 400, 500);
            sub_ships = new Submenu(shipDesignsRect);
            sub_ships.Background = new Selector(sub_ships.Rect.CutTop(25), new Color(0, 0, 0, 210)); // Black fill
            
            RefitShipList = Add(new ScrollList2<RefitShipListItem>(sub_ships, 40));
            sub_ships.AddTab("Refit to...");
            RefitShipList.EnableItemHighlight = true;
            RefitShipList.OnClick = OnRefitShipItemClicked;

            foreach (string shipId in ShipToRefit.loyalty.ShipsWeCanBuild)
            {
                Ship weCanBuild = ResourceManager.GetShipTemplate(shipId);
                if (weCanBuild.shipData.Hull == ShipToRefit.shipData.Hull && shipId != ShipToRefit.Name &&
                    !weCanBuild.shipData.ShipRole.Protected)
                {
                    RefitShipList.AddItem(new RefitShipListItem(this, weCanBuild));
                }
            }

            ConfirmRefit = new DanButton(new Vector2(shipDesignsRect.X, (shipDesignsRect.Y + 505)), "Do Refit");

            RefitOne = ButtonMedium(shipDesignsRect.X + 25, shipDesignsRect.Y + 505, text:2265, click: OnRefitOneClicked);
            RefitOne.Tooltip = Localizer.Token(2267);
            RefitAll = ButtonMedium(shipDesignsRect.X + 250, shipDesignsRect.Y + 505, text:2266, click: OnRefitAllClicked);
            RefitAll.Tooltip = Localizer.Token(2268);

            ShipInfoOverlay = Add(new ShipInfoOverlayComponent(this));
            RefitShipList.OnHovered = (item) =>
            {
                ShipInfoOverlay.ShowToLeftOf(item?.Pos ?? Vector2.Zero, item?.Ship);
            };

            base.LoadContent();
        }

        void OnRefitShipItemClicked(RefitShipListItem item)
        {
            RefitTo = item.Ship;
            RefitOne.Enabled = RefitTo != null;
            RefitAll.Enabled = RefitTo != null;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            base.Draw(batch, elapsed);
            if (RefitTo != null)
            {
                var cursor = new Vector2(ConfirmRefit.r.X, (ConfirmRefit.r.Y + 30));
                string text = Fonts.Arial12Bold.ParseText($"Refit {ShipToRefit.Name} to {RefitTo.Name}", 270f);
                batch.DrawString(Fonts.Arial12Bold, text, cursor, Color.White);
            }
            batch.End();

            RefitOne.Visible = RefitTo != null;
            RefitAll.Visible = RefitTo != null;
        }

        public override void ExitScreen()
        {
            Screen?.ResetStatus();
            base.ExitScreen();
        }

        void OnRefitOneClicked(UIButton b)
        {
            EmpireManager.Player.GetEmpireAI().Goals.Add(GetRefitGoal(ShipToRefit));
            GameAudio.EchoAffirmative();
            ExitScreen();
        }

        void OnRefitAllClicked(UIButton b)
        {
            foreach (Ship ship in EmpireManager.Player.GetShips())
            {
                if (ship.Name == ShipToRefit.Name)
                    EmpireManager.Player.GetEmpireAI().Goals.Add(GetRefitGoal(ship));
            }

            GameAudio.EchoAffirmative();
            ExitScreen();
        }

        Goal GetRefitGoal(Ship ship)
        {
            Goal refitShip;
            if (ShipToRefit.IsPlatformOrStation)
                refitShip = new RefitOrbital(ship, RefitTo.Name, EmpireManager.Player);
            else
                refitShip = new RefitShip(ship, RefitTo.Name, EmpireManager.Player);

            return refitShip;
        }
    }
}