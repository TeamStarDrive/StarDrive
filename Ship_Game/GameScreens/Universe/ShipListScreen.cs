using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SDGraphics;
using Ship_Game.Audio;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.Universe;

namespace Ship_Game
{
    public sealed class ShipListScreen : GameScreen
    {
        public readonly UniverseScreen Universe;
        public UniverseState UState => Universe.UState;
        private readonly Menu2 TitleBar;
        private readonly Vector2 TitlePos;
        private readonly Menu2 EMenu;
        private Ship SelectedShip;
        private readonly ScrollList<ShipListScreenItem> ShipSL;
        public EmpireUIOverlay EmpireUi;
        private readonly Rectangle LeftRect;
        private readonly DropOptions<int> ShowRoles;
        private readonly SortButton SortSystem;
        private readonly SortButton SortName;
        private readonly SortButton SortRole;
        private readonly SortButton SortOrder;
        private readonly SortButton SortFleet;
        private readonly RectF ERect;

        private bool PlayerDesignsOnly
        {
            get => UState.Params.ShipListFilterPlayerShipsOnly;
            set => UState.Params.ShipListFilterPlayerShipsOnly = value;
        }
        private bool InFleetsOnly
        {
            get => UState.Params.ShipListFilterInFleetsOnly;
            set
            {
                UState.Params.ShipListFilterInFleetsOnly = value;
                if (UState.Params.ShipListFilterInFleetsOnly && UState.Params.ShipListFilterNotInFleets)
                    UState.Params.ShipListFilterNotInFleets = false;
            }

        }

        private bool NotInFleets
        {
            get => UState.Params.ShipListFilterNotInFleets;
            set
            {
                UState.Params.ShipListFilterNotInFleets = value;
                if (UState.Params.ShipListFilterNotInFleets && UState.Params.ShipListFilterInFleetsOnly)
                    UState.Params.ShipListFilterInFleetsOnly = false;
            }
        }

        private static int IndexLast;
        private RectF StrIconRect;
        private readonly SortButton SB_STR;
        private RectF MaintRect;
        private readonly SortButton Maint;
        private RectF TroopRect;
        private readonly SortButton SB_Troop;
        private RectF FTL;
        private readonly SortButton SB_FTL;
        private RectF STL;
        private readonly SortButton SB_STL;

        public ShipListScreen(UniverseScreen parent, EmpireUIOverlay empUi, string audioCue = "")
            : base(parent, toPause: parent)
        {
            Universe = parent;
            if (!string.IsNullOrEmpty(audioCue))
                GameAudio.PlaySfxAsync(audioCue);

            EmpireUi = empUi;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
            IsPopup = true;
            var titleRect = new Rectangle(2, 44, ScreenWidth * 2 / 3, 80);
            TitleBar = new Menu2(titleRect);
            TitlePos = new Vector2(titleRect.X + titleRect.Width / 2 - Fonts.Laserian14.MeasureString(Localizer.Token(GameText.ShipArray)).X / 2f, titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
            LeftRect = new Rectangle(2, titleRect.Y + titleRect.Height + 5, ScreenWidth - 10, ScreenHeight - (titleRect.Y + titleRect.Height) - 7);
            EMenu = new Menu2(LeftRect);
            Add(new CloseButton(LeftRect.Right - 40, LeftRect.Y + 20));


            ERect = new(20, titleRect.Y + titleRect.Height + 35, ScreenWidth - 40, ScreenHeight - (titleRect.Y + titleRect.Height) - 7);
            ERect.H = ERect.H.RoundDownTo(80);
            RectF slRect = new(ERect.X, ERect.Y + 15, ERect.W, ERect.H - 15);

            ShipSL = Add(new ScrollList<ShipListScreenItem>(slRect, 30));
            ShipSL.OnDoubleClick = OnShipListScreenItemClicked;
            ShipSL.EnableItemHighlight = true;

            Add(new UICheckBox(TitleBar.Menu.Right + 10, TitleBar.Menu.Y + 15,
                () => PlayerDesignsOnly,
                (x) => {
                    PlayerDesignsOnly = x;
                    ResetList(ShowRoles.ActiveValue);
                }, Fonts.Arial12Bold, title: GameText.PlayerDesignsOnly, tooltip: GameText.ShowPlayerDesignsOnly));

            Add(new UICheckBox(TitleBar.Menu.Right + 10, TitleBar.Menu.Y + 35,
                () => InFleetsOnly,
                (x) => {
                    InFleetsOnly = x;
                    ResetList(ShowRoles.ActiveValue);
                }, Fonts.Arial12Bold, title: GameText.InFleetsOnly, tooltip: GameText.ShowOnlyShipsWhichAre));

            Add(new UICheckBox(TitleBar.Menu.Right + 10, TitleBar.Menu.Y + 55,
                () => NotInFleets,
                (x) => {
                    NotInFleets = x;
                    ResetList(ShowRoles.ActiveValue);
                }, Fonts.Arial12Bold, title: GameText.NotInFleets, tooltip: GameText.ShowOnlyShipsWhichAre2));

            ShowRoles = new DropOptions<int>(new Rectangle(TitleBar.Menu.Right + 175, TitleBar.Menu.Y + 15, 175, 18));
            ShowRoles.AddOption("All Ships", 1);
            ShowRoles.AddOption("Fighters", 2);
            ShowRoles.AddOption("Corvettes", 3);
            ShowRoles.AddOption("Frigates", 4);
            ShowRoles.AddOption("Cruisers", 5);
            ShowRoles.AddOption("Battleships", 6);
            ShowRoles.AddOption("Titans", 7);
            ShowRoles.AddOption("Carriers", 8);
            ShowRoles.AddOption("Bombers", 9);
            ShowRoles.AddOption("Troopships", 10);
            ShowRoles.AddOption("Support Ships", 11);
            ShowRoles.AddOption("All Structures", 12);
            ShowRoles.AddOption("Civilian", 13);

            SortSystem = new SortButton(EmpireUi.Player.data.SLSort, Localizer.Token(GameText.System));
            SortName   = new SortButton(EmpireUi.Player.data.SLSort, Localizer.Token(GameText.Ship));
            SortRole   = new SortButton(EmpireUi.Player.data.SLSort, Localizer.Token(GameText.Role));
            SortOrder  = new SortButton(EmpireUi.Player.data.SLSort, Localizer.Token(GameText.Orders));
            SortFleet  = new SortButton(EmpireUi.Player.data.SLSort, "Fleet");
            Maint      = new SortButton(EmpireUi.Player.data.SLSort, "maint");
            SB_FTL     = new SortButton(EmpireUi.Player.data.SLSort, "FTL");
            SB_STL     = new SortButton(EmpireUi.Player.data.SLSort, "STL");
            SB_Troop   = new SortButton(EmpireUi.Player.data.SLSort, "TROOP");
            SB_STR     = new SortButton(EmpireUi.Player.data.SLSort, "STR");
            ShowRoles.ActiveIndex = IndexLast;  //fbedard: remember last filter
            ResetList(ShowRoles.ActiveValue);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            TitleBar.Draw(batch, elapsed);
            batch.DrawString(Fonts.Laserian14, Localizer.Token(GameText.ShipArray), TitlePos, Colors.Cream);
            EMenu.Draw(batch, elapsed);

            base.Draw(batch, elapsed);

            // Draw List Header
            if (ShipSL.NumEntries > 0)
            {
                ShipListScreenItem e1 = ShipSL.ItemAtTop;
                Graphics.Font font = Fonts.Arial20Bold;
                var cursor = new Vector2(e1.SysNameRect.CenterX() - font.TextWidth(SortSystem.Text) / 2f, ERect.Y - font.LineSpacing + 18);
                SortSystem.rect = new Rectangle((int)cursor.X, (int)cursor.Y, font.TextWidth(SortSystem.Text), font.LineSpacing);
                SortSystem.Draw(ScreenManager, font);

                cursor = new Vector2(e1.ShipNameRect.CenterX() - font.TextWidth(SortName.Text) / 2f, ERect.Y - font.LineSpacing + 18);
                SortName.rect = new Rectangle((int)cursor.X, (int)cursor.Y, font.TextWidth(SortName.Text), font.LineSpacing);
                SortName.Draw(ScreenManager, font);
                
                cursor = new Vector2(e1.RoleRect.CenterX() - font.TextWidth(SortRole.Text) / 2f, ERect.Y - font.LineSpacing + 18);
                SortRole.rect = new Rectangle((int)cursor.X, (int)cursor.Y, font.TextWidth(SortRole.Text), font.LineSpacing);
                SortRole.Draw(ScreenManager, font);

                cursor = new Vector2(e1.FleetRect.CenterX() - font.TextWidth(SortFleet.Text) / 2f, ERect.Y - font.LineSpacing + 18);
                SortFleet.rect = new Rectangle((int)cursor.X, (int)cursor.Y, font.TextWidth(SortFleet.Text), font.LineSpacing);
                SortFleet.Draw(ScreenManager, font);

                cursor = new Vector2(e1.OrdersRect.CenterX() - font.TextWidth(SortOrder.Text) / 2f, ERect.Y - font.LineSpacing + 18);
                SortOrder.rect = new Rectangle((int)cursor.X, (int)cursor.Y, font.TextWidth(SortOrder.Text), font.LineSpacing);
                SortOrder.Draw(ScreenManager, font);

                StrIconRect = new(e1.StrRect.X + e1.StrRect.Width / 2 - 6, ERect.Y, 18, 18);
                SB_STR.rect = StrIconRect;
                batch.Draw(ResourceManager.Texture("UI/icon_fighting_small"), StrIconRect, Color.White);
                MaintRect = new(e1.MaintRect.X + e1.MaintRect.Width / 2 - 7, ERect.Y - 2, 21, 20);
                Maint.rect = MaintRect;
                batch.Draw(ResourceManager.Texture("NewUI/icon_money"), MaintRect, Color.White);
                TroopRect = new(e1.TroopRect.X + e1.TroopRect.Width / 2 - 5, ERect.Y - 2, 18, 22);
                SB_Troop.rect = TroopRect;
                batch.Draw(ResourceManager.Texture("UI/icon_troop"), TroopRect, Color.White);
                cursor = new Vector2(e1.FTLRect.X + e1.FTLRect.Width / 2 - Fonts.Arial12Bold.MeasureString("FTL").X / 2f + 4f, ERect.Y - Fonts.Arial12Bold.LineSpacing + 18);
                cursor = cursor.ToFloored();
                batch.DrawString(Fonts.Arial12Bold, "FTL", cursor, Colors.Cream);
                FTL = new(e1.FTLRect.X, ERect.Y - 20 + 35, e1.FTLRect.Width, 20);
                SB_FTL.rect = FTL;
                STL = new(e1.STLRect.X, ERect.Y - 20 + 35, e1.STLRect.Width, 20);
                SB_STL.rect = STL;
                cursor = new Vector2(e1.STLRect.X + e1.STLRect.Width / 2 - Fonts.Arial12Bold.MeasureString("STL").X / 2f + 4f, ERect.Y - Fonts.Arial12Bold.LineSpacing + 18);
                cursor = cursor.ToFloored();
                batch.DrawString(Fonts.Arial12Bold, "STL", cursor, Colors.Cream);

                void DrawLine(float aX, float aY, float bX, float bY)
                {
                    batch.DrawLine(new Vector2(aX, aY), new Vector2(bX, bY), new Color(118, 102, 67, 255));
                }
                void DrawVerticalSeparator(int x)
                {
                    DrawLine(x, ERect.Y + 26, x, ERect.Bottom - 10);
                }
                void DrawHorizontalSeparator(float y)
                {
                     DrawLine(e1.TotalEntrySize.X, y, e1.TotalEntrySize.Right, y);
                }

                // Draw the borders of the ScrollList
                DrawVerticalSeparator(e1.ShipNameRect.X);
                DrawVerticalSeparator(e1.RoleRect.X);
                DrawVerticalSeparator(e1.FleetRect.X);
                DrawVerticalSeparator(e1.OrdersRect.X);
                DrawVerticalSeparator(e1.RefitRect.X);
                DrawVerticalSeparator(e1.StrRect.X);
                DrawVerticalSeparator(e1.MaintRect.X + 5);
                DrawVerticalSeparator(e1.TroopRect.X + 5);
                DrawVerticalSeparator(e1.FTLRect.X + 5);
                DrawVerticalSeparator(e1.STLRect.X + 5);
                DrawVerticalSeparator(e1.STLRect.Right + 5);
                DrawVerticalSeparator(e1.TotalEntrySize.X); //  bottom-35??
                DrawVerticalSeparator(e1.TotalEntrySize.Right);
                DrawHorizontalSeparator(ERect.Bottom - 10);
                DrawHorizontalSeparator(ERect.Y + 25);
            }
            ShowRoles.Draw(batch, elapsed);
            batch.End();
        }
        
        void OnShipListScreenItemClicked(ShipListScreenItem item)
        {
            ExitScreen();
            UniverseScreen u = Universe;
            if (u.SelectedShip != null && u.previousSelection != u.SelectedShip && u.SelectedShip != item.Ship) // fbedard
                u.previousSelection = u.SelectedShip;
            u.SelectedShipList.Clear();
            u.SelectedShip = item.Ship;
            u.ViewToShip();
            u.returnToShip = true;
        }

        public override bool HandleInput(InputState input)
        {
            if (!IsActive)
                return false;

            if (ShowRoles.HandleInput(input))
                return true;

            if (ShowRoles.ActiveIndex != IndexLast)
            {
                ResetList(ShowRoles.ActiveValue);
                IndexLast = ShowRoles.ActiveIndex;
                return true;
            }

            if (base.HandleInput(input))
                return true;

            if (HandleShipListSortButtonClick(input))
                return true;

            if (input.KeyPressed(Keys.K) && !GlobalStats.TakingInput)
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                ResetUniverseShipSelectionMessy(Universe);
                return true;
            }

            if (input.Escaped || input.RightMouseClick)
            {
                ExitScreen();
                ResetUniverseShipSelectionMessy(Universe);
                return true;
            }

            return false;
        }

        bool HandleShipListSortButtonClick(InputState input)
        {
            bool clickedOnSomething = false;

            void Sort<T>(SortButton button, Func<ShipListScreenItem, T> sortPredicate)
            {
                clickedOnSomething = true;
                GameAudio.AcceptClick();
                button.Ascending = !button.Ascending;
                if (button.Ascending) ShipSL.Sort(sortPredicate);
                else ShipSL.SortDescending(sortPredicate);
            }

            if (SB_FTL.HandleInput(input)) Sort(SB_FTL, sl => sl.Ship.MaxFTLSpeed);
            else if (SB_FTL.Hover) ToolTip.CreateTooltip("Faster Than Light Speed of Ship");

            if (SB_STL.HandleInput(input)) Sort(SB_STL, sl => sl.Ship.MaxSTLSpeed);
            else if (SB_STL.Hover) ToolTip.CreateTooltip("Sublight Speed of Ship");

            if (Maint.HandleInput(input)) Sort(Maint, sl => sl.Ship.GetMaintCost());
            else if (Maint.Hover) ToolTip.CreateTooltip("Maintenance Cost of Ship; sortable");

            if (SB_Troop.HandleInput(input)) Sort(SB_Troop, sl => sl.Ship.TroopCount);
            else if (SB_Troop.Hover) ToolTip.CreateTooltip("Indicates Troops on board, friendly or hostile; sortable");

            if (SB_STR.HandleInput(input)) Sort(SB_STR, sl => sl.Ship.GetStrength());
            else if (SB_STR.Hover) ToolTip.CreateTooltip("Indicates Ship Strength; sortable");

            void SortAndReset<T>(SortButton button, Func<ShipListScreenItem, T> sortPredicate)
            {
                clickedOnSomething = true;
                GameAudio.BlipClick();
                button.Ascending = !button.Ascending;
                if (button.Ascending) ShipSL.Sort(sortPredicate);
                else ShipSL.SortDescending(sortPredicate);
            }

            if (SortName.HandleInput(input))   SortAndReset(SortName,  sl => sl.Ship.VanityName);
            if (SortRole.HandleInput(input))   SortAndReset(SortRole,  sl => sl.Ship.ShipData.Role);
            if (SortOrder.HandleInput(input))  SortAndReset(SortOrder, sl => ShipListScreenItem.GetStatusText(sl.Ship));
            if (SortSystem.HandleInput(input)) SortAndReset(SortOrder, sl => sl.Ship.SystemName);
            if (SortFleet.HandleInput(input))  SortAndReset(SortOrder, sl => sl.Ship.Fleet?.Name ?? "None");

            return clickedOnSomething;
        }

        void ResetUniverseShipSelectionMessy(UniverseScreen u)
        {
            u.SelectedShipList.Clear();
            u.returnToShip = false;
            if (SelectedShip != null)
            {
                u.SelectedFleet  = null;
                u.SelectedItem   = null;
                u.SelectedSystem = null;
                u.SelectedPlanet = null;
                u.returnToShip   = false;
                foreach (ShipListScreenItem sel in ShipSL.AllEntries)
                    if (sel.Selected) u.SelectedShipList.AddUnique(sel.Ship);

                if (u.SelectedShipList.Count == 1)
                {
                    if (u.SelectedShip != null && u.previousSelection != u.SelectedShip) //fbedard
                        u.previousSelection = u.SelectedShip;
                    u.SelectedShip = SelectedShip;
                    u.ShipInfoUIElement.SetShip(SelectedShip);
                    u.SelectedShipList.Clear();
                }
                else if (u.SelectedShipList.Count > 1)
                    u.shipListInfoUI.SetShipList(u.SelectedShipList, false);
            }
        }

        public void ResetList(int category)
        {
            ShipSL.Reset();
            IReadOnlyList<Ship> ships = Universe.Player.OwnedShips;
            if (ships.Count <= 0)
                return;

            bool ShouldAddForCategory(Ship ship, int forCategory)
            {
                if (ship.IsHangarShip || ship.IsHomeDefense
                    || PlayerDesignsOnly && !ship.ShipData.IsPlayerDesign
                    || InFleetsOnly && ship.Fleet == null
                    || NotInFleets && ship.Fleet != null)
                {
                    return false;
                }

                switch (forCategory)
                {
                    case 1:  return ship.DesignRole > RoleName.station;
                    case 2:  return ship.DesignRole == RoleName.fighter || ship.DesignRole == RoleName.scout;
                    case 3:  return ship.DesignRole == RoleName.corvette || ship.DesignRole == RoleName.gunboat;
                    case 4:  return ship.DesignRole == RoleName.frigate || ship.DesignRole == RoleName.destroyer;
                    case 5:  return ship.DesignRole == RoleName.cruiser;
                    case 6:  return ship.DesignRole == RoleName.battleship;
                    case 7:  return ship.DesignRole == RoleName.capital;
                    case 8:  return ship.DesignRole == RoleName.carrier;
                    case 9:  return ship.DesignRole == RoleName.bomber;
                    case 10: return ship.DesignRole == RoleName.troopShip || ship.DesignRole == RoleName.troop;
                    case 11: return ship.DesignRole == RoleName.support;
                    case 12: return ship.DesignRole <= RoleName.platform || ship.DesignRole == RoleName.station;
                    case 13: return ship.IsConstructor || ship.DesignRole == RoleName.freighter || ship.ShipData.ShipCategory == ShipCategory.Civilian;
                }

                return false;
            }

            foreach (Ship ship in ships)
            {
                if (ShouldAddForCategory(ship, category))
                {
                    ShipSL.AddItem(new ShipListScreenItem(ship, (int)ERect.X + 130, LeftRect.Y + 20, EMenu.Menu.Width - 30, 30, this));
                }
            }

            SelectedShip = null;
        }

        public void ResetStatus()
        {
            foreach (ShipListScreenItem sel in ShipSL.AllEntries)
                sel.StatusText = ShipListScreenItem.GetStatusText(sel.Ship);
        }

    }
}
