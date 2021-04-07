using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class ShipListScreen : GameScreen
    {
        private readonly Menu2 TitleBar;
        private readonly Vector2 TitlePos;
        private readonly Menu2 EMenu;
        private Ship SelectedShip;
        private readonly ScrollList2<ShipListScreenItem> ShipSL;
        public EmpireUIOverlay EmpireUi;
        private readonly Rectangle LeftRect;
        private readonly DropOptions<int> ShowRoles;
        private readonly SortButton SortSystem;
        private readonly SortButton SortName;
        private readonly SortButton SortRole;
        private readonly SortButton SortOrder;
        private readonly SortButton SortFleet;
        private readonly Rectangle ERect;

        private bool PlayerDesignsOnly
        {
            get => GlobalStats.ShipListFilterPlayerShipsOnly;
            set => GlobalStats.ShipListFilterPlayerShipsOnly = value;
        }
        private bool InFleetsOnly
        {
            get => GlobalStats.ShipListFilterInFleetsOnly;
            set
            {
                GlobalStats.ShipListFilterInFleetsOnly = value;
                if (GlobalStats.ShipListFilterInFleetsOnly && GlobalStats.ShipListFilterNotInFleets)
                    GlobalStats.ShipListFilterNotInFleets = false;
            }

        }

        private bool NotInFleets
        {
            get => GlobalStats.ShipListFilterNotInFleets;
            set
            {
                GlobalStats.ShipListFilterNotInFleets = value;
                if (GlobalStats.ShipListFilterNotInFleets && GlobalStats.ShipListFilterInFleetsOnly)
                    GlobalStats.ShipListFilterInFleetsOnly = false;
            }
        }

        private static int IndexLast;
        private Rectangle StrIconRect;
        private readonly SortButton SB_STR;
        private Rectangle MaintRect;
        private readonly SortButton Maint;
        private Rectangle TroopRect;
        private readonly SortButton SB_Troop;
        private Rectangle FTL;
        private readonly SortButton SB_FTL;
        private Rectangle STL;
        private readonly SortButton SB_STL;

        public ShipListScreen(UniverseScreen parent, EmpireUIOverlay empUi, string audioCue = "") : base(parent)
        {
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
            ERect = new Rectangle(20, titleRect.Y + titleRect.Height + 35, ScreenWidth - 40, ScreenHeight - (titleRect.Y + titleRect.Height) - 7);
            while (ERect.Height % 80 != 0)
            {
                ERect.Height = ERect.Height - 1;
            }

            ShipSL = Add(new ScrollList2<ShipListScreenItem>(ERect, 30));
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

            SortSystem = new SortButton(EmpireUi.empire.data.SLSort, Localizer.Token(GameText.System));
            SortName   = new SortButton(EmpireUi.empire.data.SLSort, Localizer.Token(GameText.Ship));
            SortRole   = new SortButton(EmpireUi.empire.data.SLSort, Localizer.Token(GameText.Role));
            SortOrder  = new SortButton(EmpireUi.empire.data.SLSort, Localizer.Token(GameText.Orders));
            SortFleet  = new SortButton(EmpireUi.empire.data.SLSort, "Fleet");
            Maint      = new SortButton(EmpireUi.empire.data.SLSort, "maint");
            SB_FTL     = new SortButton(EmpireUi.empire.data.SLSort, "FTL");
            SB_STL     = new SortButton(EmpireUi.empire.data.SLSort, "STL");
            SB_Troop   = new SortButton(EmpireUi.empire.data.SLSort, "TROOP");
            SB_STR     = new SortButton(EmpireUi.empire.data.SLSort, "STR");
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
                SpriteFont font = Fonts.Arial20Bold;
                var cursor = new Vector2(e1.SysNameRect.CenterX() - font.TextWidth(192) / 2f, ERect.Y - font.LineSpacing + 18);
                SortSystem.rect = new Rectangle((int)cursor.X, (int)cursor.Y, font.TextWidth(192), font.LineSpacing);
                
                SortSystem.Draw(ScreenManager, font);
                cursor = new Vector2(e1.ShipNameRect.CenterX() - font.TextWidth(193) / 2f, ERect.Y - font.LineSpacing + 18);
                SortName.rect = new Rectangle((int)cursor.X, (int)cursor.Y, font.TextWidth(193), font.LineSpacing);
                
                SortName.Draw(ScreenManager, font);
                
                cursor = new Vector2(e1.RoleRect.CenterX() - font.TextWidth(194) / 2f, ERect.Y - font.LineSpacing + 18);
                SortRole.rect = new Rectangle((int)cursor.X, (int)cursor.Y, font.TextWidth(194), font.LineSpacing);
                SortRole.Draw(ScreenManager, font);

                cursor = new Vector2(e1.FleetRect.CenterX() - font.TextWidth(4195) / 2f, ERect.Y - font.LineSpacing + 18);
                SortFleet.rect = new Rectangle((int)cursor.X, (int)cursor.Y, font.TextWidth(4195), font.LineSpacing);
                SortFleet.Draw(ScreenManager, font);

                cursor = new Vector2(e1.OrdersRect.CenterX() - font.TextWidth(195) / 2f, ERect.Y - font.LineSpacing + 18);
                SortOrder.rect = new Rectangle((int)cursor.X, (int)cursor.Y, font.TextWidth(195), font.LineSpacing);
                SortOrder.Draw(ScreenManager, font);

                StrIconRect = new Rectangle(e1.StrRect.X + e1.StrRect.Width / 2 - 6, ERect.Y, 18, 18);
                SB_STR.rect = StrIconRect;
                batch.Draw(ResourceManager.Texture("UI/icon_fighting_small"), StrIconRect, Color.White);                    
                MaintRect = new Rectangle(e1.MaintRect.X + e1.MaintRect.Width / 2 - 7, ERect.Y - 2, 21, 20);
                Maint.rect = MaintRect;
                batch.Draw(ResourceManager.Texture("NewUI/icon_money"), MaintRect, Color.White);
                TroopRect = new Rectangle(e1.TroopRect.X + e1.TroopRect.Width / 2 - 5, ERect.Y - 2, 18, 22);
                SB_Troop.rect = TroopRect;
                batch.Draw(ResourceManager.Texture("UI/icon_troop"), TroopRect, Color.White);
                cursor = new Vector2(e1.FTLRect.X + e1.FTLRect.Width / 2 - Fonts.Arial12Bold.MeasureString("FTL").X / 2f + 4f, ERect.Y - Fonts.Arial12Bold.LineSpacing + 18);
                HelperFunctions.ClampVectorToInt(ref cursor);
                batch.DrawString(Fonts.Arial12Bold, "FTL", cursor, Colors.Cream);
                FTL = new Rectangle(e1.FTLRect.X, ERect.Y - 20 + 35, e1.FTLRect.Width, 20);
                SB_FTL.rect = FTL;
                STL = new Rectangle(e1.STLRect.X, ERect.Y - 20 + 35, e1.STLRect.Width, 20);
                SB_STL.rect = STL;
                cursor = new Vector2(e1.STLRect.X + e1.STLRect.Width / 2 - Fonts.Arial12Bold.MeasureString("STL").X / 2f + 4f, ERect.Y - Fonts.Arial12Bold.LineSpacing + 18);
                HelperFunctions.ClampVectorToInt(ref cursor);
                batch.DrawString(Fonts.Arial12Bold, "STL", cursor, Colors.Cream);

                void DrawLine(int aX, int aY, int bX, int bY)
                {
                    batch.DrawLine(new Vector2(aX, aY), new Vector2(bX, bY), new Color(118, 102, 67, 255));
                }
                void DrawVerticalSeparator(int x)
                {
                    DrawLine(x, ERect.Y + 26, x, ERect.Bottom - 10);
                }
                void DrawHorizontalSeparator(int y)
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
            UniverseScreen universe = Empire.Universe;
            if (universe.SelectedShip != null && universe.previousSelection != universe.SelectedShip && universe.SelectedShip != item.Ship) // fbedard
                universe.previousSelection = universe.SelectedShip;
            universe.SelectedShipList.Clear();
            universe.SelectedShip = item.Ship;
            universe.ViewToShip();
            universe.returnToShip = true;
        }

        public override bool HandleInput(InputState input)
        {
            if (!IsActive)
                return false;

            if (ShowRoles.HandleInput(input))
                return true;

            if (ShipSL.HandleInput(input))
                return true;

            if (ShowRoles.ActiveIndex != IndexLast)
            {
                ResetList(ShowRoles.ActiveValue);
                IndexLast = ShowRoles.ActiveIndex;
                return true;
            }

            void Sort<T>(SortButton button, Func<ShipListScreenItem, T> sortPredicate)
            {
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
                GameAudio.BlipClick();
                button.Ascending = !button.Ascending;
                if (button.Ascending) ShipSL.Sort(sortPredicate);
                else ShipSL.SortDescending(sortPredicate);
            }

            if (SortName.HandleInput(input))   SortAndReset(SortName,  sl => sl.Ship.VanityName);
            if (SortRole.HandleInput(input))   SortAndReset(SortRole,  sl => sl.Ship.shipData.Role);
            if (SortOrder.HandleInput(input))  SortAndReset(SortOrder, sl => ShipListScreenItem.GetStatusText(sl.Ship));
            if (SortSystem.HandleInput(input)) SortAndReset(SortOrder, sl => sl.Ship.SystemName);
            if (SortFleet.HandleInput(input))  SortAndReset(SortOrder, sl => sl.Ship.fleet?.Name ?? "None");

            if (input.KeyPressed(Keys.K) && !GlobalStats.TakingInput)
            {
                GameAudio.EchoAffirmative();
                ExitScreen();

                Empire.Universe.SelectedShipList.Clear();
                Empire.Universe.returnToShip = false;
                if (SelectedShip !=null)
                {                   
                    Empire.Universe.SelectedFleet = null;
                    Empire.Universe.SelectedItem = null;
                    Empire.Universe.SelectedSystem = null;
                    Empire.Universe.SelectedPlanet = null;
                    Empire.Universe.returnToShip = false;
                    foreach (ShipListScreenItem sel in ShipSL.AllEntries)
                        if (sel.Selected) Empire.Universe.SelectedShipList.AddUnique(sel.Ship);

                    if (Empire.Universe.SelectedShipList.Count == 1)
                    {
                        if (Empire.Universe.SelectedShip != null && Empire.Universe.previousSelection != Empire.Universe.SelectedShip) //fbedard
                            Empire.Universe.previousSelection = Empire.Universe.SelectedShip;
                        Empire.Universe.SelectedShip = SelectedShip;
                        Empire.Universe.ShipInfoUIElement.SetShip(SelectedShip);
                        Empire.Universe.SelectedShipList.Clear();
                    }
                    else if (Empire.Universe.SelectedShipList.Count > 1)
                        Empire.Universe.shipListInfoUI.SetShipList(Empire.Universe.SelectedShipList, false);
                }
                return base.HandleInput(input);
            }

            if (input.Escaped || input.RightMouseClick)
            {
                ExitScreen();
                Empire.Universe.SelectedShipList.Clear();
                Empire.Universe.returnToShip = false;
                if (SelectedShip !=null)
                {                   
                    Empire.Universe.SelectedFleet  = null;
                    Empire.Universe.SelectedItem   = null;
                    Empire.Universe.SelectedSystem = null;
                    Empire.Universe.SelectedPlanet = null;
                    Empire.Universe.returnToShip   = false;
                    foreach (ShipListScreenItem sel in ShipSL.AllEntries)
                        if (sel.Selected) Empire.Universe.SelectedShipList.AddUnique(sel.Ship);

                    if (Empire.Universe.SelectedShipList.Count == 1)
                    {
                        if (Empire.Universe.SelectedShip != null && Empire.Universe.previousSelection != Empire.Universe.SelectedShip) //fbedard
                            Empire.Universe.previousSelection = Empire.Universe.SelectedShip;
                        Empire.Universe.SelectedShip = SelectedShip;
                        Empire.Universe.ShipInfoUIElement.SetShip(SelectedShip);
                        Empire.Universe.SelectedShipList.Clear();
                    }
                    else if (Empire.Universe.SelectedShipList.Count > 1)
                        Empire.Universe.shipListInfoUI.SetShipList(Empire.Universe.SelectedShipList, false);
                }
                return true;
            }
            return base.HandleInput(input);
        }

        public void ResetList(int category)
        {
            ShipSL.Reset();
            IReadOnlyList<Ship> ships = EmpireManager.Player.GetShips();
            if (ships.Count <= 0)
                return;

            bool ShouldAddForCategory(Ship ship, int forCategory)
            {
                if (ship.IsHangarShip || ship.IsHomeDefense
                    || PlayerDesignsOnly && !ship.IsPlayerDesign
                    || InFleetsOnly && ship.fleet == null
                    || NotInFleets && ship.fleet != null)
                {
                    return false;
                }

                switch (forCategory)
                {
                    case 1:  return ship.DesignRole > ShipData.RoleName.station;
                    case 2:  return ship.DesignRole == ShipData.RoleName.fighter || ship.DesignRole == ShipData.RoleName.scout;
                    case 3:  return ship.DesignRole == ShipData.RoleName.corvette || ship.DesignRole == ShipData.RoleName.gunboat;
                    case 4:  return ship.DesignRole == ShipData.RoleName.frigate || ship.DesignRole == ShipData.RoleName.destroyer;
                    case 5:  return ship.DesignRole == ShipData.RoleName.cruiser;
                    case 6:  return ship.DesignRole == ShipData.RoleName.battleship;
                    case 7:  return ship.DesignRole == ShipData.RoleName.capital;
                    case 8:  return ship.DesignRole == ShipData.RoleName.carrier;
                    case 9:  return ship.DesignRole == ShipData.RoleName.bomber;
                    case 10: return ship.DesignRole == ShipData.RoleName.troopShip || ship.DesignRole == ShipData.RoleName.troop;
                    case 11: return ship.DesignRole == ShipData.RoleName.support;
                    case 12: return ship.DesignRole <= ShipData.RoleName.platform || ship.DesignRole == ShipData.RoleName.station;
                    case 13: return ship.IsConstructor || ship.DesignRole == ShipData.RoleName.freighter || ship.shipData.ShipCategory == ShipData.Category.Civilian;
                }

                return false;
            }

            foreach (Ship ship in ships)
            {
                if (ShouldAddForCategory(ship, category))
                {
                    ShipSL.AddItem(new ShipListScreenItem(ship, ERect.X + 130, LeftRect.Y + 20, EMenu.Menu.Width - 30, 30, this));
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
