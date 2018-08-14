using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Linq;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class ShipListScreen : GameScreen
    {
        private EmpireUIOverlay eui;

        //private bool LowRes;

        private Menu2 TitleBar;

        private Vector2 TitlePos;

        private Menu2 EMenu;

        private Ship SelectedShip;

        private ScrollList ShipSL;

        public EmpireUIOverlay empUI;

        private Submenu ShipSubMenu;

        private Rectangle leftRect;

        private CloseButton close;

        private DropOptions<int> ShowRoles;

        private SortButton SortSystem;

        private SortButton SortName;

        private SortButton SortRole;

        private SortButton SortOrder;

        private Rectangle eRect;

        private UICheckBox cb_hide_proj;

        public bool HidePlatforms;

        private float ClickTimer;

        private float ClickDelay = 0.25f;

        private static int indexLast;

        private Rectangle STRIconRect;
        private SortButton SB_STR;

        private bool StrSorted = true;

        private Rectangle MaintRect;
        private SortButton Maint;

        private Rectangle TroopRect;
        private SortButton SB_Troop;

        private Rectangle FTL;
        private SortButton SB_FTL;

        private Rectangle STL;
        private SortButton SB_STL;

        private Rectangle AutoButton;

        //private bool AutoButtonHover;

        private int CurrentLine;

        public ShipListScreen(GameScreen parent, EmpireUIOverlay empUI, string audioCue = "") : base(parent)
        {
            if (!string.IsNullOrEmpty(audioCue))
                GameAudio.PlaySfxAsync(audioCue);
            this.empUI = empUI;
            base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
            base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
            base.IsPopup = true;
            this.eui = empUI;
            if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1280)
            {
                //this.LowRes = true;
            }
            Rectangle titleRect = new Rectangle(2, 44, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * 2 / 3, 80);
            this.TitleBar = new Menu2(titleRect);
            this.TitlePos = new Vector2((float)(titleRect.X + titleRect.Width / 2) - Fonts.Laserian14.MeasureString(Localizer.Token(190)).X / 2f, (float)(titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2));
            this.leftRect = new Rectangle(2, titleRect.Y + titleRect.Height + 5, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 10, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - (titleRect.Y + titleRect.Height) - 7);
            this.EMenu = new Menu2(this.leftRect);
            this.close = new CloseButton(this, new Rectangle(this.leftRect.X + this.leftRect.Width - 40, this.leftRect.Y + 20, 20, 20));
            this.eRect = new Rectangle(2, titleRect.Y + titleRect.Height + 25, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 40, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - (titleRect.Y + titleRect.Height) - 7);
            while (this.eRect.Height % 80 != 0)
            {
                this.eRect.Height = this.eRect.Height - 1;
            }
            this.ShipSubMenu = new Submenu(this.eRect);
            this.ShipSL = new ScrollList(this.ShipSubMenu, 30);
            if (EmpireManager.Player.GetShips().Count > 0)
            {
                foreach (Ship ship in EmpireManager.Player.GetShips())
                {
                    if (!ship.IsPlayerDesign && this.HidePlatforms)
                    {
                        continue;
                    }
                    ShipListScreenEntry entry = new ShipListScreenEntry(ship, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 30, this);
                    this.ShipSL.AddItem(entry);
                }
                this.SelectedShip = null;
            }

            cb_hide_proj = new UICheckBox(this, TitleBar.Menu.X + TitleBar.Menu.Width + 10, TitleBar.Menu.Y + 15,
                () => HidePlatforms, x => {
                    HidePlatforms = x;
                    ResetList(ShowRoles.ActiveValue);
                }, Fonts.Arial12Bold, title: 191, tooltip:0);

            this.ShowRoles = new DropOptions<int>(this, new Rectangle(this.TitleBar.Menu.X + this.TitleBar.Menu.Width + 175, this.TitleBar.Menu.Y + 15, 175, 18));
            this.ShowRoles.AddOption("All Ships", 1);
            this.ShowRoles.AddOption("Not in Fleets", 11);
            this.ShowRoles.AddOption("Fighters", 2);
            this.ShowRoles.AddOption("Corvettes", 10);
            this.ShowRoles.AddOption("Frigates", 3);
            this.ShowRoles.AddOption("Cruisers", 4);
            this.ShowRoles.AddOption("Capitals", 5);
            this.ShowRoles.AddOption("Civilian", 8);
            this.ShowRoles.AddOption("All Structures", 9);
            this.ShowRoles.AddOption("In Fleets Only", 6);

            // Replaced using the tick-box for player design filtering. Platforms now can be browsed with 'structures'
            // this.ShowRoles.AddOption("Player Designs Only", 7);
            
            this.AutoButton = new Rectangle(0, 0, 243, 33);
            this.SortSystem = new SortButton(this.empUI.empire.data.SLSort,Localizer.Token(192));
            this.SortName = new SortButton(this.empUI.empire.data.SLSort, Localizer.Token(193));
            this.SortRole = new SortButton(this.empUI.empire.data.SLSort,Localizer.Token(194));
            this.SortOrder = new SortButton(this.empUI.empire.data.SLSort, Localizer.Token(195));
            this.Maint = new SortButton(this.empUI.empire.data.SLSort, "maint");
            this.SB_FTL = new SortButton(this.empUI.empire.data.SLSort, "FTL");
            this.SB_STL = new SortButton(this.empUI.empire.data.SLSort, "STL");
            this.SB_Troop = new SortButton(this.empUI.empire.data.SLSort, "TROOP");
            this.SB_STR = new SortButton(this.empUI.empire.data.SLSort, "STR");
            //this.Maint.rect = this.MaintRect;
            this.ShowRoles.ActiveIndex = indexLast;  //fbedard: remember last filter
            this.ResetList(this.ShowRoles.ActiveValue);
        }

        public override void Draw(SpriteBatch batch)
        {
            base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
            base.ScreenManager.SpriteBatch.Begin();
            this.TitleBar.Draw();
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Laserian14, Localizer.Token(190), this.TitlePos, new Color(255, 239, 208));
            this.EMenu.Draw();
            Color TextColor = new Color(118, 102, 67, 50);
            this.ShipSL.Draw(base.ScreenManager.SpriteBatch);
            this.cb_hide_proj.Draw(ScreenManager.SpriteBatch);
            if (this.ShipSL.NumExpandedEntries > 0)
            {
                var e1 = ShipSL.ItemAtTop<ShipListScreenEntry>();
                var TextCursor = new Vector2((float)(e1.SysNameRect.X + e1.SysNameRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(192)).X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
                this.SortSystem.rect = new Rectangle((int)TextCursor.X, (int)TextCursor.Y, (int)Fonts.Arial20Bold.MeasureString(Localizer.Token(192)).X, Fonts.Arial20Bold.LineSpacing);
                
                this.SortSystem.Draw(base.ScreenManager, Fonts.Arial20Bold);
                TextCursor = new Vector2((float)(e1.ShipNameRect.X + e1.ShipNameRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(193)).X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
                this.SortName.rect = new Rectangle((int)TextCursor.X, (int)TextCursor.Y, (int)Fonts.Arial20Bold.MeasureString(Localizer.Token(193)).X, Fonts.Arial20Bold.LineSpacing);
                
                this.SortName.Draw(base.ScreenManager, Fonts.Arial20Bold);
                
                TextCursor = new Vector2((float)(e1.RoleRect.X + e1.RoleRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(194)).X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
                this.SortRole.rect = new Rectangle((int)TextCursor.X, (int)TextCursor.Y, (int)Fonts.Arial20Bold.MeasureString(Localizer.Token(194)).X, Fonts.Arial20Bold.LineSpacing);					
                this.SortRole.Draw(base.ScreenManager, Fonts.Arial20Bold);

                TextCursor = new Vector2((float)(e1.OrdersRect.X + e1.OrdersRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(195)).X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 30));
                this.SortOrder.rect = new Rectangle((int)TextCursor.X, (int)TextCursor.Y, (int)Fonts.Arial20Bold.MeasureString(Localizer.Token(195)).X, Fonts.Arial20Bold.LineSpacing);
                this.SortOrder.Draw(base.ScreenManager, Fonts.Arial20Bold);
                //base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(195), TextCursor, new Color(255, 239, 208));

                this.STRIconRect = new Rectangle(e1.STRRect.X + e1.STRRect.Width / 2 - 6, this.eRect.Y - 18 + 30, 18, 18);
                this.SB_STR.rect = this.STRIconRect;
                base.ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_fighting_small"), this.STRIconRect, Color.White);                    
                this.MaintRect = new Rectangle(e1.MaintRect.X + e1.MaintRect.Width / 2 - 7, this.eRect.Y - 20 + 30, 21, 20);
                this.Maint.rect = this.MaintRect;
                //this.Maint.Draw(base.ScreenManager, null);
                base.ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_money"), this.MaintRect, Color.White);
                this.TroopRect = new Rectangle(e1.TroopRect.X + e1.TroopRect.Width / 2 - 5, this.eRect.Y - 22 + 30, 18, 22);
                this.SB_Troop.rect = this.TroopRect;
                base.ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_troop"), this.TroopRect, Color.White);
                TextCursor = new Vector2((float)(e1.FTLRect.X + e1.FTLRect.Width / 2) - Fonts.Arial12Bold.MeasureString("FTL").X / 2f + 4f, (float)(this.eRect.Y - Fonts.Arial12Bold.LineSpacing + 28));
                HelperFunctions.ClampVectorToInt(ref TextCursor);
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "FTL", TextCursor, new Color(255, 239, 208));
                this.FTL = new Rectangle(e1.FTLRect.X, this.eRect.Y - 20 + 35, e1.FTLRect.Width, 20);
                this.SB_FTL.rect = this.FTL;
                this.STL = new Rectangle(e1.STLRect.X, this.eRect.Y - 20 + 35, e1.STLRect.Width, 20);
                this.SB_STL.rect = this.STL;
                TextCursor = new Vector2((float)(e1.STLRect.X + e1.STLRect.Width / 2) - Fonts.Arial12Bold.MeasureString("STL").X / 2f + 4f, (float)(this.eRect.Y - Fonts.Arial12Bold.LineSpacing + 28));
                HelperFunctions.ClampVectorToInt(ref TextCursor);
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "STL", TextCursor, new Color(255, 239, 208));
            
                Color smallHighlight = Color.DarkGreen;
                foreach (ScrollList.Entry e in ShipSL.VisibleEntries)
                {
                    var entry = e.item as ShipListScreenEntry;
                    if (entry.Selected)
                    {
                        //Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, entry.TotalEntrySize, TextColor);
                        ScreenManager.SpriteBatch.FillRectangle(entry.TotalEntrySize, smallHighlight);
                    }
                    entry.SetNewPos(this.eRect.X + 22, e.Y);
                    entry.Draw(base.ScreenManager, GameTime);
                    base.ScreenManager.SpriteBatch.DrawRectangle(entry.TotalEntrySize, TextColor);
                }
                Color lineColor = new Color(118, 102, 67, 255);
                Vector2 topLeftSL = new Vector2((float)e1.SysNameRect.X, (float)(this.eRect.Y + 35));
                Vector2 botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
                base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((float)e1.ShipNameRect.X, (float)(this.eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
                base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((float)e1.RoleRect.X, (float)(this.eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
                base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((float)e1.OrdersRect.X, (float)(this.eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
                base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((float)(e1.RefitRect.X + 5), (float)(this.eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
                base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((float)e1.STRRect.X, (float)(this.eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
                base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((float)(e1.MaintRect.X + 5), (float)(this.eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
                base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((float)(e1.TroopRect.X + 5), (float)(this.eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
                base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((float)(e1.FTLRect.X + 5), (float)(this.eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
                base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((float)(e1.STLRect.X + 5), (float)(this.eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
                base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((float)(e1.STLRect.X + 5 + e1.STRRect.Width), (float)(this.eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
                base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((float)e1.TotalEntrySize.X, (float)(this.eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 35));
                base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
                topLeftSL = new Vector2((float)(e1.TotalEntrySize.X + e1.TotalEntrySize.Width), (float)(this.eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
                base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
                Vector2 leftBot = new Vector2((float)e1.TotalEntrySize.X, (float)(this.eRect.Y + this.eRect.Height - 10));
                base.ScreenManager.SpriteBatch.DrawLine(leftBot, botSL, lineColor);
                leftBot = new Vector2((float)e1.TotalEntrySize.X, (float)(this.eRect.Y + 35));
                botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + 35));
                base.ScreenManager.SpriteBatch.DrawLine(leftBot, botSL, lineColor);
            }
            this.ShowRoles.Draw(batch);
            this.close.Draw(batch);
            if (base.IsActive)
            {
                ToolTip.Draw(batch);
            }
            batch.End();
        }


        public override bool HandleInput(InputState input)
        {
            if (!IsActive)
                return false;

            ShipSL.HandleInput(input);
            cb_hide_proj.HandleInput(input);
            ShowRoles.HandleInput(input);
            if (ShowRoles.ActiveIndex != indexLast)
            {
                ResetList(ShowRoles.ActiveValue);
                indexLast = ShowRoles.ActiveIndex;
                return true;
            }

            int i = ShipSL.FirstVisibleIndex;
            foreach (ScrollList.Entry e in ShipSL.VisibleEntries)
            {
                var entry = e.Get<ShipListScreenEntry>();
                entry.HandleInput(input);
                if (entry.TotalEntrySize.HitTest(input.CursorPosition) && input.LeftMouseClick)
                {
                    if (ClickTimer >= ClickDelay)
                    {
                        ClickTimer = 0f;
                    }
                    else
                    {
                        ExitScreen();
                        if (Empire.Universe.SelectedShip != null && Empire.Universe.previousSelection != Empire.Universe.SelectedShip && Empire.Universe.SelectedShip != entry.ship) //fbedard
                            Empire.Universe.previousSelection = Empire.Universe.SelectedShip;
                        Empire.Universe.SelectedShipList.Clear();
                        Empire.Universe.SelectedShip = entry.ship;                        
                        Empire.Universe.ViewToShip(null);
                        Empire.Universe.returnToShip = true;
                    }
                    if (SelectedShip != entry.ship)
                    {
                        GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        if (!input.KeysCurr.IsKeyDown(Keys.LeftShift) && !input.KeysCurr.IsKeyDown(Keys.LeftControl))
                        {
                            foreach (ShipListScreenEntry slEntry in ShipSL.AllExpandedItems<ShipListScreenEntry>())
                                slEntry.Selected = false;
                        }
                        if (input.KeysCurr.IsKeyDown(Keys.LeftShift) && SelectedShip != null)
                        {
                            if (i >= CurrentLine)
                                for (int l = CurrentLine; l <= i; l++)
                                    ShipSL.ItemAt<ShipListScreenEntry>(l).Selected = true;
                            else
                                for (int l = i; l <= CurrentLine; l++)
                                    ShipSL.ItemAt<ShipListScreenEntry>(l).Selected = true;
                        }

                        SelectedShip = entry.ship;
                        entry.Selected = true;
                        CurrentLine = i;
                    }
                }
                ++i;
            }

            void Sort<T>(SortButton button, Func<ShipListScreenEntry, T> sortPredicate)
            {
                GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                button.Ascending = !button.Ascending;
                StrSorted = button.Ascending;
                if (button.Ascending) ShipSL.Sort(sortPredicate);
                else ShipSL.SortDescending(sortPredicate);
            }

            if (SB_FTL.HandleInput(input)) Sort(SB_FTL, sl => sl.ship.GetmaxFTLSpeed);
            else if (SB_FTL.Hover) ToolTip.CreateTooltip("Faster Than Light Speed of Ship");

            if (SB_STL.HandleInput(input)) Sort(SB_STL, sl => sl.ship.GetSTLSpeed());
            else if (SB_STL.Hover) ToolTip.CreateTooltip("Sublight Speed of Ship");

            if (Maint.HandleInput(input)) Sort(Maint, sl => sl.ship.GetMaintCost());
            else if (Maint.Hover) ToolTip.CreateTooltip("Maintenance Cost of Ship; sortable");

            if (SB_Troop.HandleInput(input)) Sort(SB_Troop, sl => sl.ship.TroopList.Count);
            else if (SB_Troop.Hover) ToolTip.CreateTooltip("Indicates Troops on board, friendly or hostile; sortable");

            if (SB_STR.HandleInput(input)) Sort(SB_STR, sl => sl.ship.GetStrength());
            else if (SB_STR.Hover) ToolTip.CreateTooltip("Indicates Ship Strength; sortable");

            void SortAndReset<T>(SortButton button, Func<ShipListScreenEntry, T> sortPredicate)
            {
                GameAudio.PlaySfxAsync("blip_click");
                button.Ascending = !button.Ascending;
                if (button.Ascending) ShipSL.Sort(sortPredicate);
                else ShipSL.SortDescending(sortPredicate);
                ResetPos();
            }

            if (SortName.HandleInput(input))   SortAndReset(SortName,  sl => sl.ship.VanityName);
            if (SortRole.HandleInput(input))   SortAndReset(SortRole,  sl => sl.ship.shipData.Role);
            if (SortOrder.HandleInput(input))  SortAndReset(SortOrder, sl => ShipListScreenEntry.GetStatusText(sl.ship));
            if (SortSystem.HandleInput(input)) SortAndReset(SortOrder, sl => sl.ship.SystemName);

            if (input.WasKeyPressed(Keys.K) && !GlobalStats.TakingInput)
            {
                GameAudio.PlaySfxAsync("echo_affirm");
                this.ExitScreen();

                Empire.Universe.SelectedShipList.Clear();
                Empire.Universe.returnToShip = false;
                Empire.Universe.SkipRightOnce = true;
                if (this.SelectedShip !=null)
                {                   
                    Empire.Universe.SelectedFleet = null;
                    Empire.Universe.SelectedItem = null;
                    Empire.Universe.SelectedSystem = null;
                    Empire.Universe.SelectedPlanet = null;
                    Empire.Universe.returnToShip = false;
                    foreach (ShipListScreenEntry sel in ShipSL.AllItems<ShipListScreenEntry>())
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

            if (input.Escaped || input.RightMouseClick || this.close.HandleInput(input))
            {
                this.ExitScreen();
                Empire.Universe.SelectedShipList.Clear();
                Empire.Universe.returnToShip = false;
                Empire.Universe.SkipRightOnce = true;
                if (this.SelectedShip !=null)
                {                   
                    Empire.Universe.SelectedFleet  = null;
                    Empire.Universe.SelectedItem   = null;
                    Empire.Universe.SelectedSystem = null;
                    Empire.Universe.SelectedPlanet = null;
                    Empire.Universe.returnToShip   = false;
                    foreach (ShipListScreenEntry sel in ShipSL.AllItems<ShipListScreenEntry>())
                        if (sel.Selected) Empire.Universe.SelectedShipList.AddUnique(sel.ship);

                    if (Empire.Universe.SelectedShipList.Count == 1)
                    {
                        if (Empire.Universe.SelectedShip != null && Empire.Universe.previousSelection != Empire.Universe.SelectedShip) //fbedard
                            Empire.Universe.previousSelection = Empire.Universe.SelectedShip;
                        Empire.Universe.SelectedShip = this.SelectedShip;
                        Empire.Universe.ShipInfoUIElement.SetShip(this.SelectedShip);
                        Empire.Universe.SelectedShipList.Clear();
                    }
                    else if (Empire.Universe.SelectedShipList.Count > 1)
                        Empire.Universe.shipListInfoUI.SetShipList((Array<Ship>)Empire.Universe.SelectedShipList, false);
                }
                return true;
            }
            return base.HandleInput(input);
        }

        private void AddShipEntry(Ship ship)
        {
            ShipSL.AddItem(new ShipListScreenEntry(ship, eRect.X + 22, leftRect.Y + 20, EMenu.Menu.Width - 30, 30, this));
        }

        public void ResetList(int omit)
        {
            ShipSL.Reset();
            if (EmpireManager.Player.GetShips().Count <= 0)
                return;
            foreach (Ship ship in EmpireManager.Player.GetShips())
            {
                if ((!ship.IsPlayerDesign && HidePlatforms) || ship.Mothership != null || ship.isConstructor)  //fbedard: never list ships created from hangar or constructor
                    continue;
                switch (omit)  // fbedard
                {
                    case 1:
                        if (ship.shipData.Role > ShipData.RoleName.station)
                            AddShipEntry(ship);
                        break;
                    case 2:
                        if (ship.shipData.Role == ShipData.RoleName.fighter || ship.shipData.Role == ShipData.RoleName.scout)
                            AddShipEntry(ship);
                        break;
                    case 3:
                        if (ship.shipData.Role == ShipData.RoleName.frigate || ship.shipData.Role == ShipData.RoleName.destroyer)
                            AddShipEntry(ship);
                        break;
                    case 4:
                        if (ship.shipData.Role == ShipData.RoleName.cruiser)
                            AddShipEntry(ship);
                        break;
                    case 5:
                        if (ship.shipData.Role == ShipData.RoleName.capital || ship.shipData.Role == ShipData.RoleName.carrier)
                            AddShipEntry(ship);
                        break;
                    case 6:
                        if (ship.fleet != null)
                            AddShipEntry(ship);
                        break;
                    case 7:
                        if (ship.IsPlayerDesign)
                            AddShipEntry(ship);
                        break;
                    case 8:
                        if (ship.shipData.Role == ShipData.RoleName.freighter || ship.isConstructor || ship.shipData.ShipCategory == ShipData.Category.Civilian)
                            AddShipEntry(ship);
                        break;
                    case 9:
                        if (ship.shipData.Role <= ShipData.RoleName.construction)
                            AddShipEntry(ship);
                        break;
                    case 10:
                        if (ship.shipData.Role == ShipData.RoleName.corvette || ship.shipData.Role == ShipData.RoleName.gunboat)
                            AddShipEntry(ship);
                        break;
                    case 11:
                        if (ship.fleet == null && ship.shipData.Role > ShipData.RoleName.station)
                            AddShipEntry(ship);
                        break;
                }
            }
            this.SelectedShip = null;
            CurrentLine = 0;
        }

        private void ResetPos()
        {
            foreach (ScrollList.Entry e in ShipSL.VisibleEntries)
            {
                var entry = (ShipListScreenEntry)e.item;
                entry.SetNewPos(eRect.X + 22, e.Y);
            }
        }

        public void ResetStatus()
        {
            foreach (ShipListScreenEntry sel in ShipSL.AllItems<ShipListScreenEntry>())
                sel.Status_Text = ShipListScreenEntry.GetStatusText(sel.ship);
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            ClickTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }
    }
}