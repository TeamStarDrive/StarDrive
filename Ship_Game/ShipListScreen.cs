using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Ship_Game
{
	public sealed class ShipListScreen : GameScreen, IDisposable
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

		private DropOptions ShowRoles;

		private SortButton SortSystem;

		private SortButton SortName;

		private SortButton SortRole;

        private SortButton SortOrder;

		private Rectangle eRect;

		private Checkbox cb_hide_proj;

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

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;

		//private bool AutoButtonHover;

        private int CurrentLine;

		public ShipListScreen(Ship_Game.ScreenManager ScreenManager, EmpireUIOverlay empUI)
		{
			this.empUI = empUI;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
			base.IsPopup = true;
			this.eui = empUI;
			base.ScreenManager = ScreenManager;
			if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1280)
			{
				//this.LowRes = true;
			}
			Rectangle titleRect = new Rectangle(2, 44, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * 2 / 3, 80);
			this.TitleBar = new Menu2(ScreenManager, titleRect);
			this.TitlePos = new Vector2((float)(titleRect.X + titleRect.Width / 2) - Fonts.Laserian14.MeasureString(Localizer.Token(190)).X / 2f, (float)(titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2));
			this.leftRect = new Rectangle(2, titleRect.Y + titleRect.Height + 5, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 10, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - (titleRect.Y + titleRect.Height) - 7);
			this.EMenu = new Menu2(ScreenManager, this.leftRect);
			this.close = new CloseButton(new Rectangle(this.leftRect.X + this.leftRect.Width - 40, this.leftRect.Y + 20, 20, 20));
			this.eRect = new Rectangle(2, titleRect.Y + titleRect.Height + 25, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 40, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - (titleRect.Y + titleRect.Height) - 7);
			while (this.eRect.Height % 80 != 0)
			{
				this.eRect.Height = this.eRect.Height - 1;
			}
			this.ShipSubMenu = new Submenu(ScreenManager, this.eRect);
			this.ShipSL = new ScrollList(this.ShipSubMenu, 30);
			if (EmpireManager.GetEmpireByName(empUI.screen.PlayerLoyalty).GetShips().Count > 0)
			{
				foreach (Ship ship in EmpireManager.GetEmpireByName(empUI.screen.PlayerLoyalty).GetShips())
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
			Ref<bool> aeRef = new Ref<bool>(() => this.HidePlatforms, (bool x) => {
				this.HidePlatforms = x;
               this.ResetList(this.ShowRoles.Options[this.ShowRoles.ActiveIndex].@value);
			});
			this.cb_hide_proj = new Checkbox(new Vector2((float)(this.TitleBar.Menu.X + this.TitleBar.Menu.Width + 10), (float)(this.TitleBar.Menu.Y + 15)), Localizer.Token(191), aeRef, Fonts.Arial12Bold);
			this.ShowRoles = new DropOptions(new Rectangle(this.TitleBar.Menu.X + this.TitleBar.Menu.Width + 175, this.TitleBar.Menu.Y + 15, 175, 18));
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
            this.ResetList(this.ShowRoles.Options[indexLast].@value);
		}


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ShipListScreen() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.ShipSL != null)
                        this.ShipSL.Dispose();
          
                }
                this.ShipSL = null;
                this.disposed = true;
            }
        }
		public override void Draw(GameTime gameTime)
		{
			base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
			base.ScreenManager.SpriteBatch.Begin();
			this.TitleBar.Draw();
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Laserian14, Localizer.Token(190), this.TitlePos, new Color(255, 239, 208));
			this.EMenu.Draw();
			Color TextColor = new Color(118, 102, 67, 50);
			this.ShipSL.Draw(base.ScreenManager.SpriteBatch);
			this.cb_hide_proj.Draw(base.ScreenManager);
			if (this.ShipSL.Copied.Count > 0)
			{
				ShipListScreenEntry e1 = this.ShipSL.Entries[this.ShipSL.indexAtTop].item as ShipListScreenEntry;
				if (this.ShipSL.Copied.Count > 0)
				{
					ShipListScreenEntry entry = this.ShipSL.Copied[this.ShipSL.indexAtTop].item as ShipListScreenEntry;
					Vector2 TextCursor = new Vector2((float)(entry.SysNameRect.X + entry.SysNameRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(192)).X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
					this.SortSystem.rect = new Rectangle((int)TextCursor.X, (int)TextCursor.Y, (int)Fonts.Arial20Bold.MeasureString(Localizer.Token(192)).X, Fonts.Arial20Bold.LineSpacing);
					
					this.SortSystem.Draw(base.ScreenManager, Fonts.Arial20Bold);
					TextCursor = new Vector2((float)(entry.ShipNameRect.X + entry.ShipNameRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(193)).X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
					this.SortName.rect = new Rectangle((int)TextCursor.X, (int)TextCursor.Y, (int)Fonts.Arial20Bold.MeasureString(Localizer.Token(193)).X, Fonts.Arial20Bold.LineSpacing);
					
					this.SortName.Draw(base.ScreenManager, Fonts.Arial20Bold);
					
                    TextCursor = new Vector2((float)(entry.RoleRect.X + entry.RoleRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(194)).X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
					this.SortRole.rect = new Rectangle((int)TextCursor.X, (int)TextCursor.Y, (int)Fonts.Arial20Bold.MeasureString(Localizer.Token(194)).X, Fonts.Arial20Bold.LineSpacing);					
					this.SortRole.Draw(base.ScreenManager, Fonts.Arial20Bold);

                    TextCursor = new Vector2((float)(entry.OrdersRect.X + entry.OrdersRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(195)).X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 30));
                    this.SortOrder.rect = new Rectangle((int)TextCursor.X, (int)TextCursor.Y, (int)Fonts.Arial20Bold.MeasureString(Localizer.Token(195)).X, Fonts.Arial20Bold.LineSpacing);
                    this.SortOrder.Draw(base.ScreenManager, Fonts.Arial20Bold);
					//base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(195), TextCursor, new Color(255, 239, 208));

                    this.STRIconRect = new Rectangle(entry.STRRect.X + entry.STRRect.Width / 2 - 6, this.eRect.Y - 18 + 30, 18, 18);
                    this.SB_STR.rect = this.STRIconRect;
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_fighting_small"], this.STRIconRect, Color.White);                    
                    this.MaintRect = new Rectangle(entry.MaintRect.X + entry.MaintRect.Width / 2 - 7, this.eRect.Y - 20 + 30, 21, 20);
                    this.Maint.rect = this.MaintRect;
                    //this.Maint.Draw(base.ScreenManager, null);
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_money"], this.MaintRect, Color.White);
					this.TroopRect = new Rectangle(entry.TroopRect.X + entry.TroopRect.Width / 2 - 5, this.eRect.Y - 22 + 30, 18, 22);
                    this.SB_Troop.rect = this.TroopRect;
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_troop"], this.TroopRect, Color.White);
					TextCursor = new Vector2((float)(entry.FTLRect.X + entry.FTLRect.Width / 2) - Fonts.Arial12Bold.MeasureString("FTL").X / 2f + 4f, (float)(this.eRect.Y - Fonts.Arial12Bold.LineSpacing + 28));
					HelperFunctions.ClampVectorToInt(ref TextCursor);
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "FTL", TextCursor, new Color(255, 239, 208));
					this.FTL = new Rectangle(entry.FTLRect.X, this.eRect.Y - 20 + 35, entry.FTLRect.Width, 20);
                    this.SB_FTL.rect = this.FTL;
					this.STL = new Rectangle(entry.STLRect.X, this.eRect.Y - 20 + 35, entry.STLRect.Width, 20);
                    this.SB_STL.rect = this.STL;
					TextCursor = new Vector2((float)(entry.STLRect.X + entry.STLRect.Width / 2) - Fonts.Arial12Bold.MeasureString("STL").X / 2f + 4f, (float)(this.eRect.Y - Fonts.Arial12Bold.LineSpacing + 28));
					HelperFunctions.ClampVectorToInt(ref TextCursor);
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "STL", TextCursor, new Color(255, 239, 208));
				}
				Color smallHighlight = TextColor;
				//smallHighlight.A = (byte)(TextColor.A / 2);
                smallHighlight = Color.DarkGreen;
				for (int i = this.ShipSL.indexAtTop; i < this.ShipSL.Entries.Count && i < this.ShipSL.indexAtTop + this.ShipSL.entriesToDisplay; i++)
				{
					ShipListScreenEntry entry = this.ShipSL.Entries[i].item as ShipListScreenEntry;
					//if (i % 2 == 0)
					//{
					//	Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, entry.TotalEntrySize, smallHighlight);
					//}
					//if (entry.ship == this.SelectedShip)
                    if (entry.Selected)
					{
						//Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, entry.TotalEntrySize, TextColor);
                        Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, entry.TotalEntrySize, smallHighlight);
					}
					entry.SetNewPos(this.eRect.X + 22, this.ShipSL.Entries[i].clickRect.Y);
					entry.Draw(base.ScreenManager, gameTime);
					Primitives2D.DrawRectangle(base.ScreenManager.SpriteBatch, entry.TotalEntrySize, TextColor);
				}
				Color lineColor = new Color(118, 102, 67, 255);
				Vector2 topLeftSL = new Vector2((float)e1.SysNameRect.X, (float)(this.eRect.Y + 35));
				Vector2 botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
				topLeftSL = new Vector2((float)e1.ShipNameRect.X, (float)(this.eRect.Y + 35));
				botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
				topLeftSL = new Vector2((float)e1.RoleRect.X, (float)(this.eRect.Y + 35));
				botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
				topLeftSL = new Vector2((float)e1.OrdersRect.X, (float)(this.eRect.Y + 35));
				botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
				topLeftSL = new Vector2((float)(e1.RefitRect.X + 5), (float)(this.eRect.Y + 35));
				botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
				topLeftSL = new Vector2((float)e1.STRRect.X, (float)(this.eRect.Y + 35));
				botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
				topLeftSL = new Vector2((float)(e1.MaintRect.X + 5), (float)(this.eRect.Y + 35));
				botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
				topLeftSL = new Vector2((float)(e1.TroopRect.X + 5), (float)(this.eRect.Y + 35));
				botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
				topLeftSL = new Vector2((float)(e1.FTLRect.X + 5), (float)(this.eRect.Y + 35));
				botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
				topLeftSL = new Vector2((float)(e1.STLRect.X + 5), (float)(this.eRect.Y + 35));
				botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
				topLeftSL = new Vector2((float)(e1.STLRect.X + 5 + e1.STRRect.Width), (float)(this.eRect.Y + 35));
				botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
				topLeftSL = new Vector2((float)e1.TotalEntrySize.X, (float)(this.eRect.Y + 35));
				botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 35));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
				topLeftSL = new Vector2((float)(e1.TotalEntrySize.X + e1.TotalEntrySize.Width), (float)(this.eRect.Y + 35));
				botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 10));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
				Vector2 leftBot = new Vector2((float)e1.TotalEntrySize.X, (float)(this.eRect.Y + this.eRect.Height - 10));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, leftBot, botSL, lineColor);
				leftBot = new Vector2((float)e1.TotalEntrySize.X, (float)(this.eRect.Y + 35));
				botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + 35));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, leftBot, botSL, lineColor);
			}
			this.ShowRoles.Draw(base.ScreenManager.SpriteBatch);
			this.close.Draw(base.ScreenManager);
			if (base.IsActive)
			{
				ToolTip.Draw(base.ScreenManager);
			}
			base.ScreenManager.SpriteBatch.End();
		}


		public override void HandleInput(InputState input)
		{
			if (!base.IsActive)
			{
				return;
			}
			this.ShipSL.HandleInput(input);
			this.cb_hide_proj.HandleInput(input);
			this.ShowRoles.HandleInput(input);
			if (this.ShowRoles.ActiveIndex != indexLast)
			{
				this.ResetList(this.ShowRoles.Options[this.ShowRoles.ActiveIndex].@value);
                indexLast = this.ShowRoles.ActiveIndex;
				return;
			}
			//this.indexLast = this.ShowRoles.ActiveIndex;
			for (int i = this.ShipSL.indexAtTop; i < this.ShipSL.Copied.Count && i < this.ShipSL.indexAtTop + this.ShipSL.entriesToDisplay; i++)
			{
				ShipListScreenEntry entry = this.ShipSL.Copied[i].item as ShipListScreenEntry;
				entry.HandleInput(input);
				if (HelperFunctions.CheckIntersection(entry.TotalEntrySize, input.CursorPosition) && input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
				{
					if (this.ClickTimer >= this.ClickDelay)
					{
						this.ClickTimer = 0f;
					}
					else
					{
						this.ExitScreen();
                        if (this.empUI.screen.SelectedShip != null && this.empUI.screen.previousSelection != this.empUI.screen.SelectedShip && this.empUI.screen.SelectedShip != entry.ship) //fbedard
                            this.empUI.screen.previousSelection = this.empUI.screen.SelectedShip;
                        this.empUI.screen.SelectedShipList.Clear();
                        this.empUI.screen.SelectedShip = entry.ship;                        
						this.empUI.screen.ViewToShip(null);
						this.empUI.screen.returnToShip = true;
					}
					if (this.SelectedShip != entry.ship)
					{
						AudioManager.PlayCue("sd_ui_accept_alt3");
                        if (!input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) && !input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl))
                        {
                            foreach (ScrollList.Entry sel in this.ShipSL.Entries)
                                (sel.item as ShipListScreenEntry).Selected = false;
			            }
                        if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) && this.SelectedShip != null)
                            if (i >= CurrentLine)
                                for (int l = CurrentLine; l <= i; l++)
                                    (this.ShipSL.Copied[l].item as ShipListScreenEntry).Selected = true;
                            else
                                for (int l = i; l <= CurrentLine; l++)
                                    (this.ShipSL.Copied[l].item as ShipListScreenEntry).Selected = true;

						this.SelectedShip = entry.ship;
                        entry.Selected = true;
                        CurrentLine = i;
					}
				}
			}
			if (this.SB_FTL.HandleInput(input))  //HelperFunctions.CheckIntersection(this.FTL, input.CursorPosition))
			{
				
				//if (input.InGameSelect)
				{
					AudioManager.PlayCue("sd_ui_accept_alt3");
                    this.SB_FTL.Ascending = !this.SB_FTL.Ascending;
                    this.StrSorted = this.SB_FTL.Ascending;
                    if (!this.SB_FTL.Ascending)
					{
						IOrderedEnumerable<ScrollList.Entry> sortedList = 
							from theship in this.ShipSL.Entries
							orderby (theship.item as ShipListScreenEntry).ship.GetmaxFTLSpeed descending
							select theship;
						this.ResetListSorted(sortedList);
					}
					else
					{
						IOrderedEnumerable<ScrollList.Entry> sortedList = 
							from theship in this.ShipSL.Entries
							orderby (theship.item as ShipListScreenEntry).ship.GetmaxFTLSpeed
							select theship;
						this.ResetListSorted(sortedList);
					}
				}
			}
            else if(this.SB_FTL.Hover)
                ToolTip.CreateTooltip("Faster Than Light Speed of Ship", base.ScreenManager);
			if (this.SB_STL.HandleInput(input))//HelperFunctions.CheckIntersection(this.STL, input.CursorPosition))
			{
				
				//if (input.InGameSelect)
				{
					AudioManager.PlayCue("sd_ui_accept_alt3");
                    this.SB_STL.Ascending = !this.SB_STL.Ascending;
                    this.StrSorted = this.SB_STL.Ascending;
                    if (!this.SB_STL.Ascending)
					{
						IOrderedEnumerable<ScrollList.Entry> sortedList = 
							from theship in this.ShipSL.Entries
							orderby (theship.item as ShipListScreenEntry).ship.GetSTLSpeed() descending
							select theship;
						this.ResetListSorted(sortedList);
					}
					else
					{
						IOrderedEnumerable<ScrollList.Entry> sortedList = 
							from theship in this.ShipSL.Entries
							orderby (theship.item as ShipListScreenEntry).ship.GetSTLSpeed()
							select theship;
						this.ResetListSorted(sortedList);
					}
				}
			}
            else if (this.SB_STL.Hover)
                ToolTip.CreateTooltip("Sublight Speed of Ship", base.ScreenManager);
			if (this.Maint.HandleInput(input))//  HelperFunctions.CheckIntersection(this.MaintRect, input.CursorPosition))
			{
				
                //if (input.InGameSelect)
                {
                    //reduntant maintenance check no longer needed.
                    {
                        AudioManager.PlayCue("sd_ui_accept_alt3");
                        this.Maint.Ascending = !this.Maint.Ascending;
                        this.StrSorted = this.Maint.Ascending;
                        if (!this.Maint.Ascending)
                        {
                            IOrderedEnumerable<ScrollList.Entry> sortedList =
                                from theship in this.ShipSL.Entries
                                orderby (theship.item as ShipListScreenEntry).ship.GetMaintCost() descending
                                select theship;
                            this.ResetListSorted(sortedList);
                        }
                        else
                        {
                            IOrderedEnumerable<ScrollList.Entry> sortedList =
                                from theship in this.ShipSL.Entries
                                orderby (theship.item as ShipListScreenEntry).ship.GetMaintCost()
                                select theship;
                            this.ResetListSorted(sortedList);
                        }
                    }
                }
			}
            else if (this.Maint.Hover)
                ToolTip.CreateTooltip("Maintenance Cost of Ship; sortable", base.ScreenManager);
			if (SB_Troop.HandleInput(input)  )//)HelperFunctions.CheckIntersection(this.TroopRect, input.CursorPosition))
			{
				
				//if (input.InGameSelect)
				{
					AudioManager.PlayCue("sd_ui_accept_alt3");
					//this.StrSorted = !this.StrSorted;
                    this.SB_Troop.Ascending = !this.SB_Troop.Ascending;
                    if (!this.SB_Troop.Ascending)
					{
						IOrderedEnumerable<ScrollList.Entry> sortedList = 
							from theship in this.ShipSL.Entries
							orderby (theship.item as ShipListScreenEntry).ship.TroopList.Count descending
							select theship;
						this.ResetListSorted(sortedList);
					}
					else
					{
						IOrderedEnumerable<ScrollList.Entry> sortedList = 
							from theship in this.ShipSL.Entries
							orderby (theship.item as ShipListScreenEntry).ship.TroopList.Count
							select theship;
						this.ResetListSorted(sortedList);
					}
				}
			}
            else if(this.SB_Troop.Hover)
                ToolTip.CreateTooltip("Indicates Troops on board, friendly or hostile; sortable", base.ScreenManager);
			if (this.SB_STR.HandleInput(input))//HelperFunctions.CheckIntersection(this.STRIconRect, input.CursorPosition))
			{
				
				//if (input.InGameSelect)
				{
					AudioManager.PlayCue("sd_ui_accept_alt3");
                    this.SB_STR.Ascending = !this.SB_STR.Ascending;
                    this.StrSorted = this.SB_STR.Ascending ;
					if (!this.StrSorted)
					{
						IOrderedEnumerable<ScrollList.Entry> sortedList = 
							from theship in this.ShipSL.Entries
							orderby (theship.item as ShipListScreenEntry).ship.GetStrength() descending
							select theship;
						this.ResetListSorted(sortedList);
					}
					else
					{
						IOrderedEnumerable<ScrollList.Entry> sortedList = 
							from theship in this.ShipSL.Entries
							orderby (theship.item as ShipListScreenEntry).ship.GetStrength()
							select theship;
						this.ResetListSorted(sortedList);
					}
				}
			}
            else if(this.SB_STR.Hover)
                ToolTip.CreateTooltip("Indicates Ship Strength; sortable", base.ScreenManager);
			if (this.SortName.HandleInput(input))
			{
				AudioManager.PlayCue("blip_click");
				this.SortName.Ascending = !this.SortName.Ascending;
				if (!this.SortName.Ascending)
				{
					IOrderedEnumerable<ScrollList.Entry> sortedList = 
						from theship in this.ShipSL.Entries
						orderby (theship.item as ShipListScreenEntry).ship.VanityName descending
						select theship;
					this.ResetListSorted(sortedList);
				}
				else
				{
					IOrderedEnumerable<ScrollList.Entry> sortedList = 
						from theship in this.ShipSL.Entries
						orderby (theship.item as ShipListScreenEntry).ship.VanityName
						select theship;
					this.ResetListSorted(sortedList);
				}
				this.ResetPos();
			}
			if (this.SortRole.HandleInput(input))
			{
				AudioManager.PlayCue("blip_click");
				this.SortRole.Ascending = !this.SortRole.Ascending;
				if (!this.SortRole.Ascending)
				{
					IOrderedEnumerable<ScrollList.Entry> sortedList = 
						from theship in this.ShipSL.Entries
                        orderby (theship.item as ShipListScreenEntry).ship.shipData.Role descending
						select theship;
					this.ResetListSorted(sortedList);
				}
				else
				{
					IOrderedEnumerable<ScrollList.Entry> sortedList = 
						from theship in this.ShipSL.Entries
                        orderby (theship.item as ShipListScreenEntry).ship.shipData.Role
						select theship;
					this.ResetListSorted(sortedList);
				}
				this.ResetPos();
			}
            if (this.SortOrder.HandleInput(input))  //fbedard
            {
                AudioManager.PlayCue("blip_click");
                this.SortOrder.Ascending = !this.SortOrder.Ascending;
                if (!this.SortOrder.Ascending)
                {
                    IOrderedEnumerable<ScrollList.Entry> sortedList =
                        from theship in this.ShipSL.Entries
                        orderby ShipListScreenEntry.GetStatusText((theship.item as ShipListScreenEntry).ship) descending
                        select theship;
                    this.ResetListSorted(sortedList);
                }
                else
                {
                    IOrderedEnumerable<ScrollList.Entry> sortedList =
                        from theship in this.ShipSL.Entries
                        orderby ShipListScreenEntry.GetStatusText((theship.item as ShipListScreenEntry).ship)
                        select theship;
                    this.ResetListSorted(sortedList);
                }
                this.ResetPos();
            }
			if (this.SortSystem.HandleInput(input))
			{
				AudioManager.PlayCue("blip_click");
				this.SortSystem.Ascending = !this.SortSystem.Ascending;
				if (!this.SortSystem.Ascending)
				{
					IOrderedEnumerable<ScrollList.Entry> sortedList = 
						from theship in ShipSL.Entries
						orderby (theship.item as ShipListScreenEntry)?.ship.SystemName descending
						select theship;
					this.ResetListSorted(sortedList);
				}
				else
				{
					IOrderedEnumerable<ScrollList.Entry> sortedList = 
						from theship in this.ShipSL.Entries
						orderby (theship.item as ShipListScreenEntry).ship.SystemName						select theship;
					this.ResetListSorted(sortedList);
				}
				this.ResetPos();
			}
            if (input.CurrentKeyboardState.IsKeyDown(Keys.K) && !input.LastKeyboardState.IsKeyDown(Keys.K) && !GlobalStats.TakingInput)
            {
                AudioManager.PlayCue("echo_affirm");
                this.ExitScreen();

                this.empUI.screen.SelectedShipList.Clear();
                this.empUI.screen.returnToShip = false;
                this.empUI.screen.SkipRightOnce = true;
                if (this.SelectedShip !=null)
                {                   
                    this.empUI.screen.SelectedFleet = (Fleet)null;
                    this.empUI.screen.SelectedItem = (UniverseScreen.ClickableItemUnderConstruction)null;
                    this.empUI.screen.SelectedSystem = (SolarSystem)null;
                    this.empUI.screen.SelectedPlanet = (Planet)null;
                    this.empUI.screen.returnToShip = false;
                    foreach (ScrollList.Entry sel in this.ShipSL.Entries)
                        if ((sel.item as ShipListScreenEntry).Selected)
                            this.empUI.screen.SelectedShipList.Add((sel.item as ShipListScreenEntry).ship);

                    if (this.empUI.screen.SelectedShipList.Count == 1)
                    {
                        if (this.empUI.screen.SelectedShip != null && this.empUI.screen.previousSelection != this.empUI.screen.SelectedShip) //fbedard
                            this.empUI.screen.previousSelection = this.empUI.screen.SelectedShip;
                        this.empUI.screen.SelectedShip = this.SelectedShip;
                        this.empUI.screen.ShipInfoUIElement.SetShip(this.SelectedShip);
                        this.empUI.screen.SelectedShipList.Clear();
                    }
                    else if (this.empUI.screen.SelectedShipList.Count > 1)
                        this.empUI.screen.shipListInfoUI.SetShipList((List<Ship>)this.empUI.screen.SelectedShipList, false);
                }
                return;
            }

			if (input.Escaped || input.RightMouseClick || this.close.HandleInput(input))
			{
				this.ExitScreen();
                this.empUI.screen.SelectedShipList.Clear();
                this.empUI.screen.returnToShip = false;
                this.empUI.screen.SkipRightOnce = true;
                if (this.SelectedShip !=null)
                {                   
                    this.empUI.screen.SelectedFleet = (Fleet)null;
                    this.empUI.screen.SelectedItem = (UniverseScreen.ClickableItemUnderConstruction)null;
                    this.empUI.screen.SelectedSystem = (SolarSystem)null;
                    this.empUI.screen.SelectedPlanet = (Planet)null;
                    this.empUI.screen.returnToShip = false;
                    foreach (ScrollList.Entry sel in this.ShipSL.Entries)
                        if ((sel.item as ShipListScreenEntry).Selected)
                            this.empUI.screen.SelectedShipList.Add((sel.item as ShipListScreenEntry).ship);

                    if (this.empUI.screen.SelectedShipList.Count == 1)
                    {
                        if (this.empUI.screen.SelectedShip != null && this.empUI.screen.previousSelection != this.empUI.screen.SelectedShip) //fbedard
                            this.empUI.screen.previousSelection = this.empUI.screen.SelectedShip;
                        this.empUI.screen.SelectedShip = this.SelectedShip;
                        this.empUI.screen.ShipInfoUIElement.SetShip(this.SelectedShip);
                        this.empUI.screen.SelectedShipList.Clear();
                    }
                    else if (this.empUI.screen.SelectedShipList.Count > 1)
                        this.empUI.screen.shipListInfoUI.SetShipList((List<Ship>)this.empUI.screen.SelectedShipList, false);
                }
			}
		}

		public void ResetList()
		{
			this.ShipSL.Copied.Clear();
			this.ShipSL.Entries.Clear();
			this.ShipSL.indexAtTop = 0;
			if (EmpireManager.GetEmpireByName(this.empUI.screen.PlayerLoyalty).GetShips().Count > 0)
			{
				foreach (Ship ship in EmpireManager.GetEmpireByName(this.empUI.screen.PlayerLoyalty).GetShips())
				{
					if (!ship.IsPlayerDesign && this.HidePlatforms)
					{
						continue;
					}
					ShipListScreenEntry entry = new ShipListScreenEntry(ship, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 30, this);
					this.ShipSL.AddItem(entry);
				}
                this.SelectedShip = null;
                CurrentLine = 0;
			}
		}

		public void ResetList(int omit)
		{
			ShipListScreenEntry entry;
			this.ShipSL.Entries.Clear();
			this.ShipSL.Copied.Clear();
			this.ShipSL.indexAtTop = 0;
			if (EmpireManager.GetEmpireByName(this.empUI.screen.PlayerLoyalty).GetShips().Count > 0)
			{
				foreach (Ship ship in EmpireManager.GetEmpireByName(this.empUI.screen.PlayerLoyalty).GetShips())
				{
                    if ((!ship.IsPlayerDesign && this.HidePlatforms) || ship.Mothership != null || ship.isConstructor)  //fbedard: never list ships created from hangar or constructor
					{
						continue;
					}
					//switch (this.ShowRoles.Options[this.ShowRoles.ActiveIndex].@value)
                    switch (omit)  //fbedard
					{
						case 1:
						{
                            if (ship.shipData.Role <= ShipData.RoleName.station)
                            {
                                continue;
                            }
							entry = new ShipListScreenEntry(ship, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 30, this);
							this.ShipSL.AddItem(entry);
							continue;
						}
						case 2:
						{
                            if ((ship.shipData.Role != ShipData.RoleName.fighter) && (ship.shipData.Role != ShipData.RoleName.scout))
							{
								continue;
							}
							entry = new ShipListScreenEntry(ship, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 30, this);
							this.ShipSL.AddItem(entry);
							continue;
						}
						case 3:
						{
                            if ((ship.shipData.Role != ShipData.RoleName.frigate) && (ship.shipData.Role != ShipData.RoleName.destroyer))
							{
								continue;
							}
							entry = new ShipListScreenEntry(ship, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 30, this);
							this.ShipSL.AddItem(entry);
							continue;
						}
						case 4:
						{
                            if (ship.shipData.Role != ShipData.RoleName.cruiser)
							{
								continue;
							}
							entry = new ShipListScreenEntry(ship, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 30, this);
							this.ShipSL.AddItem(entry);
							continue;
						}
						case 5:
						{
                            if (!(ship.shipData.Role == ShipData.RoleName.capital) && !(ship.shipData.Role == ShipData.RoleName.carrier))
							{
								continue;
							}
							entry = new ShipListScreenEntry(ship, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 30, this);
							this.ShipSL.AddItem(entry);
							continue;
						}
						case 6:
						{
							if (ship.fleet == null)
							{
								continue;
							}
							entry = new ShipListScreenEntry(ship, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 30, this);
							this.ShipSL.AddItem(entry);
							continue;
						}
						case 7:
						{
							if (!ship.IsPlayerDesign)
							{
								continue;
							}
							entry = new ShipListScreenEntry(ship, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 30, this);
							this.ShipSL.AddItem(entry);
							continue;
						}
                        case 8:
                        {
                            if ((ship.shipData.Role != ShipData.RoleName.freighter) && (!ship.isConstructor) && (ship.shipData.ShipCategory != ShipData.Category.Civilian))
                            {
                                continue;
                            }
                            entry = new ShipListScreenEntry(ship, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 30, this);
                            this.ShipSL.AddItem(entry);
                            continue;
                        }
                        case 9:
                        {
                            if ((ship.shipData.Role > ShipData.RoleName.construction))
                            {
                                continue;
                            }
                            entry = new ShipListScreenEntry(ship, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 30, this);
                            this.ShipSL.AddItem(entry);
                            continue;
                        }
                        case 10:
                        {
                            if ((ship.shipData.Role != ShipData.RoleName.corvette && ship.shipData.Role != ShipData.RoleName.gunboat))
                            {
                                continue;
                            }
                            entry = new ShipListScreenEntry(ship, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 30, this);
                            this.ShipSL.AddItem(entry);
                            continue;
                        }
                        case 11: 
                        {
                            if (ship.fleet != null || ship.shipData.Role <= ShipData.RoleName.station)
                            {
                                continue;
                            }
                            entry = new ShipListScreenEntry(ship, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 30, this);
                            this.ShipSL.AddItem(entry);
                            continue;
                        }
						default:
						{
							continue;
						}
					}
				}
				this.SelectedShip = null;
                CurrentLine = 0;
			}
		}

		public void ResetListSorted(IOrderedEnumerable<ScrollList.Entry> SortedList)
		{
			this.ShipSL.Copied.Clear();
			List<ShipListScreenEntry> shipslist = new List<ShipListScreenEntry>();
			foreach (ScrollList.Entry e in SortedList)
			{
				shipslist.Add(e.item as ShipListScreenEntry);
			}
			this.ShipSL.Entries.Clear();
			foreach (ShipListScreenEntry ship in shipslist)
			{
				this.ShipSL.AddItem(ship);
			}
			//this.SelectedShip = (this.ShipSL.Entries[this.ShipSL.indexAtTop].item as ShipListScreenEntry).ship;
            //(this.ShipSL.Entries[this.ShipSL.indexAtTop].item as ShipListScreenEntry).Selected = true;
		}

		private void ResetPos()
		{
			for (int i = this.ShipSL.indexAtTop; i < this.ShipSL.Entries.Count && i < this.ShipSL.indexAtTop + this.ShipSL.entriesToDisplay; i++)
			{
				ShipListScreenEntry entry = this.ShipSL.Entries[i].item as ShipListScreenEntry;
				entry.SetNewPos(this.eRect.X + 22, this.ShipSL.Entries[i].clickRect.Y);
			}
		}

		public void ResetStatus()
		{
			foreach (ScrollList.Entry entry in this.ShipSL.Entries)
			{
				(entry.item as ShipListScreenEntry).Status_Text = ShipListScreenEntry.GetStatusText((entry.item as ShipListScreenEntry).ship);
			}
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			ShipListScreen clickTimer = this;
			clickTimer.ClickTimer = clickTimer.ClickTimer + elapsedTime;
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}