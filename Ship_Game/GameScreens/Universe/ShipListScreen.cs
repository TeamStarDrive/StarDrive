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
        private Menu2 TitleBar;

        private Vector2 TitlePos;

        private Menu2 EMenu;

        private Ship SelectedShip;

        private ScrollList2<ShipListScreenItem> ShipSL;

        public EmpireUIOverlay empUI;

        private Rectangle leftRect;

        private DropOptions<int> ShowRoles;

        private SortButton SortSystem;
        private SortButton SortName;
        private SortButton SortRole;
        private SortButton SortOrder;
        private Rectangle eRect;

        public bool PlayerDesignsOnly;

        private static int indexLast;

        private Rectangle STRIconRect;
        private SortButton SB_STR;

        private Rectangle MaintRect;
        private SortButton Maint;

        private Rectangle TroopRect;
        private SortButton SB_Troop;

        private Rectangle FTL;
        private SortButton SB_FTL;

        private Rectangle STL;
        private SortButton SB_STL;

        public ShipListScreen(UniverseScreen parent, EmpireUIOverlay empUI, string audioCue = "") : base(parent)
        {
            if (!string.IsNullOrEmpty(audioCue))
                GameAudio.PlaySfxAsync(audioCue);
            this.empUI = empUI;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
            IsPopup = true;
            var titleRect = new Rectangle(2, 44, ScreenWidth * 2 / 3, 80);
            TitleBar = new Menu2(titleRect);
            TitlePos = new Vector2(titleRect.X + titleRect.Width / 2 - Fonts.Laserian14.MeasureString(Localizer.Token(190)).X / 2f, titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
            leftRect = new Rectangle(2, titleRect.Y + titleRect.Height + 5, ScreenWidth - 10, ScreenHeight - (titleRect.Y + titleRect.Height) - 7);
            EMenu = new Menu2(leftRect);
            Add(new CloseButton(leftRect.Right - 40, leftRect.Y + 20));
            eRect = new Rectangle(20, titleRect.Y + titleRect.Height + 35, ScreenWidth - 40, ScreenHeight - (titleRect.Y + titleRect.Height) - 7);
            while (eRect.Height % 80 != 0)
            {
                eRect.Height = eRect.Height - 1;
            }

            ShipSL = Add(new ScrollList2<ShipListScreenItem>(eRect, 30));
            ShipSL.OnDoubleClick = OnShipListScreenItemClicked;
            ShipSL.EnableItemHighlight = true;

            Add(new UICheckBox(TitleBar.Menu.Right + 10, TitleBar.Menu.Y + 15,
                () => PlayerDesignsOnly,
                (x) => {
                    PlayerDesignsOnly = x;
                    ResetList(ShowRoles.ActiveValue);
                }, Fonts.Arial12Bold, title: 191, tooltip: 191));

            ShowRoles = new DropOptions<int>(new Rectangle(TitleBar.Menu.Right + 175, TitleBar.Menu.Y + 15, 175, 18));
            ShowRoles.AddOption("All Ships", 1);
            ShowRoles.AddOption("Not in Fleets", 11);
            ShowRoles.AddOption("Fighters", 2);
            ShowRoles.AddOption("Corvettes", 10);
            ShowRoles.AddOption("Frigates", 3);
            ShowRoles.AddOption("Cruisers", 4);
            ShowRoles.AddOption("Capitals", 5);
            ShowRoles.AddOption("Civilian", 8);
            ShowRoles.AddOption("All Structures", 9);
            ShowRoles.AddOption("In Fleets Only", 6);

            // Replaced using the tick-box for player design filtering. Platforms now can be browsed with 'structures'
            // this.ShowRoles.AddOption("Player Designs Only", 7);
            
            SortSystem = new SortButton(this.empUI.empire.data.SLSort, Localizer.Token(192));
            SortName   = new SortButton(this.empUI.empire.data.SLSort, Localizer.Token(193));
            SortRole   = new SortButton(this.empUI.empire.data.SLSort, Localizer.Token(194));
            SortOrder  = new SortButton(this.empUI.empire.data.SLSort, Localizer.Token(195));
            Maint    = new SortButton(this.empUI.empire.data.SLSort, "maint");
            SB_FTL   = new SortButton(this.empUI.empire.data.SLSort, "FTL");
            SB_STL   = new SortButton(this.empUI.empire.data.SLSort, "STL");
            SB_Troop = new SortButton(this.empUI.empire.data.SLSort, "TROOP");
            SB_STR   = new SortButton(this.empUI.empire.data.SLSort, "STR");
            ShowRoles.ActiveIndex = indexLast;  //fbedard: remember last filter
            ResetList(ShowRoles.ActiveValue);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            TitleBar.Draw(batch, elapsed);
            batch.DrawString(Fonts.Laserian14, Localizer.Token(190), TitlePos, Colors.Cream);
            EMenu.Draw(batch, elapsed);

            base.Draw(batch, elapsed);

            // Draw List Header
            if (ShipSL.NumEntries > 0)
            {
                ShipListScreenItem e1 = ShipSL.ItemAtTop;
                SpriteFont font = Fonts.Arial20Bold;
                var cursor = new Vector2(e1.SysNameRect.CenterX() - font.TextWidth(192) / 2f, eRect.Y - font.LineSpacing + 18);
                SortSystem.rect = new Rectangle((int)cursor.X, (int)cursor.Y, font.TextWidth(192), font.LineSpacing);
                
                SortSystem.Draw(ScreenManager, font);
                cursor = new Vector2(e1.ShipNameRect.CenterX() - font.TextWidth(193) / 2f, eRect.Y - font.LineSpacing + 18);
                SortName.rect = new Rectangle((int)cursor.X, (int)cursor.Y, font.TextWidth(193), font.LineSpacing);
                
                SortName.Draw(ScreenManager, font);
                
                cursor = new Vector2(e1.RoleRect.CenterX() - font.TextWidth(194) / 2f, eRect.Y - font.LineSpacing + 18);
                SortRole.rect = new Rectangle((int)cursor.X, (int)cursor.Y, font.TextWidth(194), font.LineSpacing);
                SortRole.Draw(ScreenManager, font);

                cursor = new Vector2(e1.OrdersRect.CenterX() - font.TextWidth(195) / 2f, eRect.Y - font.LineSpacing + 18);
                SortOrder.rect = new Rectangle((int)cursor.X, (int)cursor.Y, font.TextWidth(195), font.LineSpacing);
                SortOrder.Draw(ScreenManager, font);

                STRIconRect = new Rectangle(e1.STRRect.X + e1.STRRect.Width / 2 - 6, eRect.Y, 18, 18);
                SB_STR.rect = STRIconRect;
                batch.Draw(ResourceManager.Texture("UI/icon_fighting_small"), STRIconRect, Color.White);                    
                MaintRect = new Rectangle(e1.MaintRect.X + e1.MaintRect.Width / 2 - 7, eRect.Y - 2, 21, 20);
                Maint.rect = MaintRect;
                batch.Draw(ResourceManager.Texture("NewUI/icon_money"), MaintRect, Color.White);
                TroopRect = new Rectangle(e1.TroopRect.X + e1.TroopRect.Width / 2 - 5, eRect.Y - 2, 18, 22);
                SB_Troop.rect = TroopRect;
                batch.Draw(ResourceManager.Texture("UI/icon_troop"), TroopRect, Color.White);
                cursor = new Vector2(e1.FTLRect.X + e1.FTLRect.Width / 2 - Fonts.Arial12Bold.MeasureString("FTL").X / 2f + 4f, eRect.Y - Fonts.Arial12Bold.LineSpacing + 18);
                HelperFunctions.ClampVectorToInt(ref cursor);
                batch.DrawString(Fonts.Arial12Bold, "FTL", cursor, Colors.Cream);
                FTL = new Rectangle(e1.FTLRect.X, eRect.Y - 20 + 35, e1.FTLRect.Width, 20);
                SB_FTL.rect = FTL;
                STL = new Rectangle(e1.STLRect.X, eRect.Y - 20 + 35, e1.STLRect.Width, 20);
                SB_STL.rect = STL;
                cursor = new Vector2(e1.STLRect.X + e1.STLRect.Width / 2 - Fonts.Arial12Bold.MeasureString("STL").X / 2f + 4f, eRect.Y - Fonts.Arial12Bold.LineSpacing + 18);
                HelperFunctions.ClampVectorToInt(ref cursor);
                batch.DrawString(Fonts.Arial12Bold, "STL", cursor, Colors.Cream);

                void DrawLine(int aX, int aY, int bX, int bY)
                {
                    batch.DrawLine(new Vector2(aX, aY), new Vector2(bX, bY), new Color(118, 102, 67, 255));
                }
                void DrawVerticalSeparator(int x)
                {
                    DrawLine(x, eRect.Y + 26, x, eRect.Bottom - 10);
                }
                void DrawHorizontalSeparator(int y)
                {
                     DrawLine(e1.TotalEntrySize.X, y, e1.TotalEntrySize.Right, y);
                }

                // Draw the borders of the ScrollList
                DrawVerticalSeparator(e1.ShipNameRect.X);
                DrawVerticalSeparator(e1.RoleRect.X);
                DrawVerticalSeparator(e1.OrdersRect.X);
                DrawVerticalSeparator(e1.RefitRect.X);
                DrawVerticalSeparator(e1.STRRect.X);
                DrawVerticalSeparator(e1.MaintRect.X + 5);
                DrawVerticalSeparator(e1.TroopRect.X + 5);
                DrawVerticalSeparator(e1.FTLRect.X + 5);
                DrawVerticalSeparator(e1.STLRect.X + 5);
                DrawVerticalSeparator(e1.STLRect.Right + 5);
                DrawVerticalSeparator(e1.TotalEntrySize.X); //  bottom-35??
                DrawVerticalSeparator(e1.TotalEntrySize.Right);
                DrawHorizontalSeparator(eRect.Bottom - 10);
                DrawHorizontalSeparator(eRect.Y + 25);
            }
            ShowRoles.Draw(batch, elapsed);
            batch.End();
        }
        
        void OnShipListScreenItemClicked(ShipListScreenItem item)
        {
            ExitScreen();
            UniverseScreen universe = Empire.Universe;
            if (universe.SelectedShip != null && universe.previousSelection != universe.SelectedShip && universe.SelectedShip != item.ship) //fbedard
                universe.previousSelection = universe.SelectedShip;
            universe.SelectedShipList.Clear();
            universe.SelectedShip = item.ship;                        
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

            if (ShowRoles.ActiveIndex != indexLast)
            {
                ResetList(ShowRoles.ActiveValue);
                indexLast = ShowRoles.ActiveIndex;
                return true;
            }

            void Sort<T>(SortButton button, Func<ShipListScreenItem, T> sortPredicate)
            {
                GameAudio.AcceptClick();
                button.Ascending = !button.Ascending;
                if (button.Ascending) ShipSL.Sort(sortPredicate);
                else ShipSL.SortDescending(sortPredicate);
            }

            if (SB_FTL.HandleInput(input)) Sort(SB_FTL, sl => sl.ship.MaxFTLSpeed);
            else if (SB_FTL.Hover) ToolTip.CreateTooltip("Faster Than Light Speed of Ship");

            if (SB_STL.HandleInput(input)) Sort(SB_STL, sl => sl.ship.MaxSTLSpeed);
            else if (SB_STL.Hover) ToolTip.CreateTooltip("Sublight Speed of Ship");

            if (Maint.HandleInput(input)) Sort(Maint, sl => sl.ship.GetMaintCost());
            else if (Maint.Hover) ToolTip.CreateTooltip("Maintenance Cost of Ship; sortable");

            if (SB_Troop.HandleInput(input)) Sort(SB_Troop, sl => sl.ship.TroopCount);
            else if (SB_Troop.Hover) ToolTip.CreateTooltip("Indicates Troops on board, friendly or hostile; sortable");

            if (SB_STR.HandleInput(input)) Sort(SB_STR, sl => sl.ship.GetStrength());
            else if (SB_STR.Hover) ToolTip.CreateTooltip("Indicates Ship Strength; sortable");

            void SortAndReset<T>(SortButton button, Func<ShipListScreenItem, T> sortPredicate)
            {
                GameAudio.BlipClick();
                button.Ascending = !button.Ascending;
                if (button.Ascending) ShipSL.Sort(sortPredicate);
                else ShipSL.SortDescending(sortPredicate);
            }

            if (SortName.HandleInput(input))   SortAndReset(SortName,  sl => sl.ship.VanityName);
            if (SortRole.HandleInput(input))   SortAndReset(SortRole,  sl => sl.ship.shipData.Role);
            if (SortOrder.HandleInput(input))  SortAndReset(SortOrder, sl => ShipListScreenItem.GetStatusText(sl.ship));
            if (SortSystem.HandleInput(input)) SortAndReset(SortOrder, sl => sl.ship.SystemName);

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
                        if (sel.Selected) Empire.Universe.SelectedShipList.AddUnique(sel.ship);

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
                        if (sel.Selected) Empire.Universe.SelectedShipList.AddUnique(sel.ship);

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
                if ((PlayerDesignsOnly && !ship.IsPlayerDesign) || ship.IsHangarShip || ship.IsHomeDefense)
                    return false;

                switch (forCategory)
                {
                    case 1: return ship.DesignRole > ShipData.RoleName.station;
                    case 2: return ship.DesignRole == ShipData.RoleName.fighter || ship.DesignRole == ShipData.RoleName.scout;
                    case 3: return ship.DesignRole == ShipData.RoleName.frigate || ship.DesignRole == ShipData.RoleName.destroyer;
                    case 4: return ship.DesignRole == ShipData.RoleName.cruiser;
                    case 5: return ship.DesignRole == ShipData.RoleName.capital || ship.DesignRole == ShipData.RoleName.carrier;
                    case 6: return ship.fleet != null;
                    case 7: return ship.IsPlayerDesign;
                    case 8: return ship.IsConstructor || ship.DesignRole == ShipData.RoleName.freighter || ship.shipData.ShipCategory == ShipData.Category.Civilian;
                    case 9: return ship.DesignRole <= ShipData.RoleName.construction;
                    case 10: return ship.DesignRole == ShipData.RoleName.corvette || ship.DesignRole == ShipData.RoleName.gunboat;
                    case 11: return ship.fleet == null && ship.shipData.Role > ShipData.RoleName.station;
                }
                return false;
            }

            foreach (Ship ship in ships)
            {
                if (ShouldAddForCategory(ship, category))
                {
                    ShipSL.AddItem(new ShipListScreenItem(ship, eRect.X + 130, leftRect.Y + 20, EMenu.Menu.Width - 30, 30, this));
                }
            }
            SelectedShip = null;
        }

        public void ResetStatus()
        {
            foreach (ShipListScreenItem sel in ShipSL.AllEntries)
                sel.Status_Text = ShipListScreenItem.GetStatusText(sel.ship);
        }

    }
}