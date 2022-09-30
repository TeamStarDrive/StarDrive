using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Audio;
using Ship_Game.Fleets;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public sealed class RefitToWindow : GameScreen
    {
        readonly ShipListScreen Screen;
        readonly Ship ShipToRefit;
        Empire Player => ShipToRefit.Universe.Player;
        Submenu sub_ships;
        ScrollList2<RefitShipListItem> RefitShipList;
        UIButton RefitOne;
        UIButton RefitAll;
        IShipDesign RefitTo;
        DanButton ConfirmRefit;
        ShipInfoOverlayComponent ShipInfoOverlay;

        public RefitToWindow(ShipListScreen screen, ShipListScreenItem item) : base(screen, toPause: null)
        {
            Screen = screen;
            ShipToRefit = item.Ship;
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
        }

        public RefitToWindow(UniverseScreen parent, Ship ship) : base(parent, toPause: parent)
        {
            ShipToRefit = ship;
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
        }

        class RefitShipListItem : ScrollListItem<RefitShipListItem>
        {
            readonly RefitToWindow Screen;
            public readonly IShipDesign Design;

            public RefitShipListItem(RefitToWindow screen, IShipDesign design)
            {
                Screen = screen;
                Design = design;
            }
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                batch.Draw(Design.Icon, new Rectangle((int)X, (int)Y, 29, 30), Color.White);

                var tCursor = new Vector2(X + 40f, Y + 3f);
                batch.DrawString(Fonts.Arial12Bold, Design.Name, tCursor, Color.White);

                if (Screen.sub_ships.SelectedIndex == 0)
                {
                    tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                    batch.DrawString(Fonts.Arial12Bold, Design.GetRole(), tCursor, Color.Orange);
                }

                var moneyRect = new Rectangle((int)X + 285, (int)Y, 21, 20);
                var moneyText = new Vector2((moneyRect.X + 25), (moneyRect.Y - 2));
                batch.Draw(ResourceManager.Texture("NewUI/icon_production"), moneyRect, Color.White);
                int refitCost = Screen.ShipToRefit.RefitCost(Design);
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

            foreach (string shipId in ShipToRefit.Loyalty.ShipsWeCanBuild)
            {
                IShipDesign weCanBuild = ResourceManager.Ships.GetDesign(shipId);
                if (weCanBuild.Hull == ShipToRefit.ShipData.Hull && shipId != ShipToRefit.Name &&
                    !weCanBuild.ShipRole.Protected)
                {
                    RefitShipList.AddItem(new RefitShipListItem(this, weCanBuild));
                }
            }

            ConfirmRefit = new DanButton(new Vector2(shipDesignsRect.X, (shipDesignsRect.Y + 505)), "Do Refit");

            RefitOne = ButtonMedium(shipDesignsRect.X + 25, shipDesignsRect.Y + 505, text:GameText.RefitOne, click: OnRefitOneClicked);
            RefitOne.Tooltip = Localizer.Token(GameText.RefitOnlyThisShipTo);
            RefitAll = ButtonMedium(shipDesignsRect.X + 250, shipDesignsRect.Y + 505, text:GameText.RefitAll, click: OnRefitAllClicked);
            RefitAll.Tooltip = Localizer.Token(GameText.RefitAllShipsOfThis);

            ShipInfoOverlay = Add(new ShipInfoOverlayComponent(this, ShipToRefit.Universe));
            RefitShipList.OnHovered = (item) =>
            {
                ShipInfoOverlay.ShowToLeftOf(item?.Pos ?? Vector2.Zero, item?.Design);
            };

            base.LoadContent();
        }

        void OnRefitShipItemClicked(RefitShipListItem item)
        {
            RefitTo = item.Design;
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
            Player.AI.AddGoal(GetRefitGoal(ShipToRefit));
            GameAudio.EchoAffirmative();
            ExitScreen();
        }

        void OnRefitAllClicked(UIButton b)
        {
            var ships = Player.OwnedShips;
            foreach (Ship ship in ships)
            {
                if (ship.Name == ShipToRefit.Name)
                    Player.AI.AddGoal(GetRefitGoal(ship));
            }

            foreach (Planet planet in Player.GetPlanets())
                planet.Construction.RefitShipsBeingBuilt(ShipToRefit, RefitTo);

            foreach (Fleet fleet in Player.Fleets)
                fleet.RefitNodeName(ShipToRefit.Name, RefitTo.Name);

            GameAudio.EchoAffirmative();
            ExitScreen();
        }

        Goal GetRefitGoal(Ship ship)
        {
            Goal refitShip;
            if (ShipToRefit.IsPlatformOrStation)
                refitShip = new RefitOrbital(ship, RefitTo, Player);
            else
                refitShip = new RefitShip(ship, RefitTo, Player);

            return refitShip;
        }
    }
}
