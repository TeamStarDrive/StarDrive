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
	public class ShipListScreen : GameScreen, IDisposable
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

		private Rectangle eRect;

		private Checkbox cb_hide_proj;

		public bool HidePlatforms =true;

		private float ClickTimer;

		private float ClickDelay = 0.25f;

		private int indexLast;

		private Rectangle STRIconRect;

		private bool StrSorted = true;

		private Rectangle MaintRect;

		private Rectangle TroopRect;

		private Rectangle FTL;

		private Rectangle STL;

		private Rectangle AutoButton;

		//private bool AutoButtonHover;

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
					if (ship.Role == "construction")
					{
						continue;
					}
					ShipListScreenEntry entry = new ShipListScreenEntry(ship, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 30, this);
					this.ShipSL.AddItem(entry);
				}
				this.SelectedShip = (this.ShipSL.Entries[this.ShipSL.indexAtTop].item as ShipListScreenEntry).ship;
			}
			Ref<bool> aeRef = new Ref<bool>(() => this.HidePlatforms, (bool x) => {
				this.HidePlatforms = x;
				this.ResetList();
			});
			this.cb_hide_proj = new Checkbox(new Vector2((float)(this.TitleBar.Menu.X + this.TitleBar.Menu.Width + 15), (float)(this.TitleBar.Menu.Y + 15)), Localizer.Token(191), aeRef, Fonts.Arial12Bold);
			this.ShowRoles = new DropOptions(new Rectangle(this.TitleBar.Menu.X + this.TitleBar.Menu.Width + 175, this.TitleBar.Menu.Y + 15, 175, 18));
			this.ShowRoles.AddOption("Show All", 1);
			this.ShowRoles.AddOption("Fighters Only", 2);
			this.ShowRoles.AddOption("Frigates Only", 3);
			this.ShowRoles.AddOption("Cruisers Only", 4);
			this.ShowRoles.AddOption("Capitals Only", 5);
			this.ShowRoles.AddOption("Fleets Only", 6);
			this.ShowRoles.AddOption("Player Designs Only", 7);
            this.ShowRoles.AddOption("Freighters Only", 8);
			this.AutoButton = new Rectangle(0, 0, 243, 33);
			this.SortSystem = new SortButton();
			this.SortName = new SortButton();
			this.SortRole = new SortButton();
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (this)
				{
				}
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
					Vector2 TextCursor = new Vector2((float)(entry.SysNameRect.X + entry.SysNameRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(192)).X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 35));
					this.SortSystem.rect = new Rectangle((int)TextCursor.X, (int)TextCursor.Y, (int)Fonts.Arial20Bold.MeasureString(Localizer.Token(192)).X, Fonts.Arial20Bold.LineSpacing);
					this.SortSystem.Text = Localizer.Token(192);
					this.SortSystem.Draw(base.ScreenManager, Fonts.Arial20Bold);
					TextCursor = new Vector2((float)(entry.ShipNameRect.X + entry.SysNameRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(193)).X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 35));
					this.SortName.rect = new Rectangle((int)TextCursor.X, (int)TextCursor.Y, (int)Fonts.Arial20Bold.MeasureString(Localizer.Token(193)).X, Fonts.Arial20Bold.LineSpacing);
					this.SortName.Text = Localizer.Token(193);
					this.SortName.Draw(base.ScreenManager, Fonts.Arial20Bold);
					TextCursor = new Vector2((float)(entry.RoleRect.X + entry.RoleRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(194)).X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 35));
					this.SortRole.rect = new Rectangle((int)TextCursor.X, (int)TextCursor.Y, (int)Fonts.Arial20Bold.MeasureString(Localizer.Token(194)).X, Fonts.Arial20Bold.LineSpacing);
					this.SortRole.Text = Localizer.Token(194);
					this.SortRole.Draw(base.ScreenManager, Fonts.Arial20Bold);
					TextCursor = new Vector2((float)(entry.OrdersRect.X + entry.SysNameRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(195)).X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 35));
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(195), TextCursor, new Color(255, 239, 208));
					this.STRIconRect = new Rectangle(entry.STRRect.X + entry.STRRect.Width / 2 - 9, this.eRect.Y - 21 + 35, 18, 18);
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_fighting_small"], this.STRIconRect, Color.White);
					this.MaintRect = new Rectangle(entry.MaintRect.X + entry.MaintRect.Width / 2 - 9, this.eRect.Y - 21 + 35, 18, 18);
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_money"], this.MaintRect, Color.White);
					this.TroopRect = new Rectangle(entry.TroopRect.X + entry.TroopRect.Width / 2 - 9, this.eRect.Y - 21 + 35, 18, 18);
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_troop"], this.TroopRect, Color.White);
					TextCursor = new Vector2((float)(entry.FTLRect.X + entry.FTLRect.Width / 2) - Fonts.Arial12Bold.MeasureString("FTL").X / 2f + 3f, (float)(this.eRect.Y - Fonts.Arial12Bold.LineSpacing + 31));
					HelperFunctions.ClampVectorToInt(ref TextCursor);
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "FTL", TextCursor, new Color(255, 239, 208));
					this.FTL = new Rectangle(entry.FTLRect.X, this.eRect.Y - 20 + 35, entry.FTLRect.Width, 20);
					this.STL = new Rectangle(entry.STLRect.X, this.eRect.Y - 20 + 35, entry.STLRect.Width, 20);
					TextCursor = new Vector2((float)(entry.STLRect.X + entry.STLRect.Width / 2) - Fonts.Arial12Bold.MeasureString("STL").X / 2f + 3f, (float)(this.eRect.Y - Fonts.Arial12Bold.LineSpacing + 31));
					HelperFunctions.ClampVectorToInt(ref TextCursor);
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "STL", TextCursor, new Color(255, 239, 208));
				}
				Color smallHighlight = TextColor;
				smallHighlight.A = (byte)(TextColor.A / 2);
				for (int i = this.ShipSL.indexAtTop; i < this.ShipSL.Entries.Count && i < this.ShipSL.indexAtTop + this.ShipSL.entriesToDisplay; i++)
				{
					ShipListScreenEntry entry = this.ShipSL.Entries[i].item as ShipListScreenEntry;
					if (i % 2 == 0)
					{
						Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, entry.TotalEntrySize, smallHighlight);
					}
					if (entry.ship == this.SelectedShip)
					{
						Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, entry.TotalEntrySize, TextColor);
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

		/*protected override void Finalize()
		{
			try
			{
				this.Dispose(false);
			}
			finally
			{
				base.Finalize();
			}
		}*/
        ~ShipListScreen() {
            //should implicitly do the same thing as the original bad finalize
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
			if (this.ShowRoles.ActiveIndex != this.indexLast)
			{
				this.ResetList(this.ShowRoles.Options[this.ShowRoles.ActiveIndex].@value);
				this.indexLast = this.ShowRoles.ActiveIndex;
				return;
			}
			this.indexLast = this.ShowRoles.ActiveIndex;
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
						this.empUI.screen.SelectedShip = entry.ship;
						this.empUI.screen.ViewToShip(null);
						this.empUI.screen.returnToShip = false;
					}
					if (this.SelectedShip != entry.ship)
					{
						AudioManager.PlayCue("sd_ui_accept_alt3");
						this.SelectedShip = entry.ship;
					}
				}
			}
			if (HelperFunctions.CheckIntersection(this.FTL, input.CursorPosition))
			{
				ToolTip.CreateTooltip("Faster Than Light Speed of Ship", base.ScreenManager);
				if (input.InGameSelect)
				{
					AudioManager.PlayCue("sd_ui_accept_alt3");
					this.StrSorted = !this.StrSorted;
					if (!this.StrSorted)
					{
						IOrderedEnumerable<ScrollList.Entry> sortedList = 
							from theship in this.ShipSL.Entries
							orderby (theship.item as ShipListScreenEntry).ship.GetFTLSpeed() descending
							select theship;
						this.ResetListSorted(sortedList);
					}
					else
					{
						IOrderedEnumerable<ScrollList.Entry> sortedList = 
							from theship in this.ShipSL.Entries
							orderby (theship.item as ShipListScreenEntry).ship.GetFTLSpeed()
							select theship;
						this.ResetListSorted(sortedList);
					}
				}
			}
			if (HelperFunctions.CheckIntersection(this.STL, input.CursorPosition))
			{
				ToolTip.CreateTooltip("Sublight Speed of Ship", base.ScreenManager);
				if (input.InGameSelect)
				{
					AudioManager.PlayCue("sd_ui_accept_alt3");
					this.StrSorted = !this.StrSorted;
					if (!this.StrSorted)
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
			if (HelperFunctions.CheckIntersection(this.MaintRect, input.CursorPosition))
			{
				ToolTip.CreateTooltip("Maintenance Cost of Ship; sortable", base.ScreenManager);
                if (input.InGameSelect)
                {
                    if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.useProportionalUpkeep )
                    {
                        AudioManager.PlayCue("sd_ui_accept_alt3");
                        this.StrSorted = !this.StrSorted;
                        if (!this.StrSorted)
                        {
                            IOrderedEnumerable<ScrollList.Entry> sortedList =
                                from theship in this.ShipSL.Entries
                                orderby (theship.item as ShipListScreenEntry).ship.GetMaintCostRealism() descending
                                select theship;
                            this.ResetListSorted(sortedList);
                        }
                        else
                        {
                            IOrderedEnumerable<ScrollList.Entry> sortedList =
                                from theship in this.ShipSL.Entries
                                orderby (theship.item as ShipListScreenEntry).ship.GetMaintCostRealism()
                                select theship;
                            this.ResetListSorted(sortedList);
                        }
                    }
                    else
                    {
                        AudioManager.PlayCue("sd_ui_accept_alt3");
                        this.StrSorted = !this.StrSorted;
                        if (!this.StrSorted)
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
			if (HelperFunctions.CheckIntersection(this.TroopRect, input.CursorPosition))
			{
				ToolTip.CreateTooltip("Indicates Troops on board, friendly or hostile; sortable", base.ScreenManager);
				if (input.InGameSelect)
				{
					AudioManager.PlayCue("sd_ui_accept_alt3");
					this.StrSorted = !this.StrSorted;
					if (!this.StrSorted)
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
			if (HelperFunctions.CheckIntersection(this.STRIconRect, input.CursorPosition))
			{
				ToolTip.CreateTooltip("Indicates Ship Strength; sortable", base.ScreenManager);
				if (input.InGameSelect)
				{
					AudioManager.PlayCue("sd_ui_accept_alt3");
					this.StrSorted = !this.StrSorted;
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
						orderby (theship.item as ShipListScreenEntry).ship.Role descending
						select theship;
					this.ResetListSorted(sortedList);
				}
				else
				{
					IOrderedEnumerable<ScrollList.Entry> sortedList = 
						from theship in this.ShipSL.Entries
						orderby (theship.item as ShipListScreenEntry).ship.Role
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
						from theship in this.ShipSL.Entries
						orderby (theship.item as ShipListScreenEntry).ship.GetSystemName() descending
						select theship;
					this.ResetListSorted(sortedList);
				}
				else
				{
					IOrderedEnumerable<ScrollList.Entry> sortedList = 
						from theship in this.ShipSL.Entries
						orderby (theship.item as ShipListScreenEntry).ship.GetSystemName()
						select theship;
					this.ResetListSorted(sortedList);
				}
				this.ResetPos();
			}
			if (input.Escaped || input.RightMouseClick || this.close.HandleInput(input))
			{
				this.ExitScreen();
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
					if (ship.Role == "construction" || ship.Role == "platform" && this.HidePlatforms)
					{
						continue;
					}
					ShipListScreenEntry entry = new ShipListScreenEntry(ship, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 30, this);
					this.ShipSL.AddItem(entry);
				}
				this.SelectedShip = (this.ShipSL.Entries[this.ShipSL.indexAtTop].item as ShipListScreenEntry).ship;
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
					if (ship.Role == "platform" && this.HidePlatforms || ship.Role == "construction")
					{
						continue;
					}
					switch (this.ShowRoles.Options[this.ShowRoles.ActiveIndex].@value)
					{
						case 1:
						{
							entry = new ShipListScreenEntry(ship, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 30, this);
							this.ShipSL.AddItem(entry);
							continue;
						}
						case 2:
						{
							if (ship.Role != "fighter")
							{
								continue;
							}
							entry = new ShipListScreenEntry(ship, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 30, this);
							this.ShipSL.AddItem(entry);
							continue;
						}
						case 3:
						{
							if (ship.Role != "frigate")
							{
								continue;
							}
							entry = new ShipListScreenEntry(ship, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 30, this);
							this.ShipSL.AddItem(entry);
							continue;
						}
						case 4:
						{
							if (ship.Role != "cruiser")
							{
								continue;
							}
							entry = new ShipListScreenEntry(ship, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 30, this);
							this.ShipSL.AddItem(entry);
							continue;
						}
						case 5:
						{
							if (!(ship.Role == "capital") && !(ship.Role == "carrier"))
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
                            if (ship.Role != "freighter")
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
				if (this.ShipSL.Entries.Count<ScrollList.Entry>() > 0)
				{
					this.SelectedShip = (this.ShipSL.Entries[this.ShipSL.indexAtTop].item as ShipListScreenEntry).ship;
					return;
				}
				this.SelectedShip = null;
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
			this.SelectedShip = (this.ShipSL.Entries[this.ShipSL.indexAtTop].item as ShipListScreenEntry).ship;
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