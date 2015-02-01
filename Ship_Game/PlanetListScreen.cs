using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Ship_Game
{
	public class PlanetListScreen : GameScreen, IDisposable
	{
		//private bool LowRes;

		private Menu2 TitleBar;

		private Vector2 TitlePos;

		private Menu2 EMenu;

		private float ClickTimer;

		private float ClickDelay = 0.15f;

		private Planet SelectedPlanet;

		private ScrollList PlanetSL;

		public EmpireUIOverlay empUI;

		private Submenu ShipSubMenu;

		private Rectangle leftRect;

		private SortButton sb_Sys;

		private SortButton sb_Name;

		private SortButton sb_Fert;

		private SortButton sb_Rich;

		private SortButton sb_Pop;

		private SortButton sb_Owned;

		private CloseButton close;

		private Checkbox cb_hideOwned;

		private Checkbox cb_hideUninhabitable;

		private bool HideOwned;

		private bool HideUninhab = true;

		private List<Planet> planets = new List<Planet>();

		private Rectangle eRect;

		private SortButton LastSorted;

		private Rectangle AutoButton;

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;

		//private bool AutoButtonHover;

		public PlanetListScreen(Ship_Game.ScreenManager ScreenManager, EmpireUIOverlay empUI)
		{
			this.empUI = empUI;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
			base.IsPopup = true;
			base.ScreenManager = ScreenManager;
			if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1280)
			{
				//this.LowRes = true;
			}
			Rectangle titleRect = new Rectangle(2, 44, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * 2 / 3, 80);
			this.TitleBar = new Menu2(ScreenManager, titleRect);
			this.TitlePos = new Vector2((float)(titleRect.X + titleRect.Width / 2) - Fonts.Laserian14.MeasureString(Localizer.Token(1402)).X / 2f, (float)(titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2));
			this.leftRect = new Rectangle(2, titleRect.Y + titleRect.Height + 5, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 10, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - (titleRect.Y + titleRect.Height) - 7);
			this.EMenu = new Menu2(ScreenManager, this.leftRect);
			this.close = new CloseButton(new Rectangle(this.leftRect.X + this.leftRect.Width - 40, this.leftRect.Y + 20, 20, 20));
			this.eRect = new Rectangle(2, titleRect.Y + titleRect.Height + 25, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 40, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - (titleRect.Y + titleRect.Height) - 15);
			this.sb_Sys = new SortButton();
			this.sb_Name = new SortButton();
			this.sb_Fert = new SortButton();
			this.sb_Rich = new SortButton();
			this.sb_Pop = new SortButton();
			this.sb_Owned = new SortButton();
			while (this.eRect.Height % 40 != 0)
			{
				this.eRect.Height = this.eRect.Height - 1;
			}
			this.eRect.Height = this.eRect.Height - 20;
			this.ShipSubMenu = new Submenu(ScreenManager, this.eRect);
			this.PlanetSL = new ScrollList(this.ShipSubMenu, 40);
			foreach (SolarSystem system in UniverseScreen.SolarSystemList)
			{
				foreach (Planet p in system.PlanetList)
				{
					if (!p.ExploredDict[EmpireManager.GetEmpireByName(empUI.screen.PlayerLoyalty)])
					{
						continue;
					}
					this.planets.Add(p);
				}
			}
			foreach (Planet p in this.planets)
			{
				if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
				{
					continue;
				}
				PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
				this.PlanetSL.AddItem(entry);
			}
			this.SelectedPlanet = (this.PlanetSL.Entries[this.PlanetSL.indexAtTop].item as PlanetListScreenEntry).planet;
			Ref<bool> aeRef = new Ref<bool>(() => this.HideOwned, (bool x) => {
				this.HideOwned = x;
				this.ResetList();
			});
			this.cb_hideOwned = new Checkbox(new Vector2((float)(this.TitleBar.Menu.X + this.TitleBar.Menu.Width + 15), (float)(this.TitleBar.Menu.Y + 15)), "Hide Owned", aeRef, Fonts.Arial12Bold);
			aeRef = new Ref<bool>(() => this.HideUninhab, (bool x) => {
				this.HideUninhab = x;
				this.ResetList();
			});
			this.cb_hideUninhabitable = new Checkbox(new Vector2((float)(this.TitleBar.Menu.X + this.TitleBar.Menu.Width + 15), (float)(this.TitleBar.Menu.Y + 35)), "Hide Uninhabitable", aeRef, Fonts.Arial12Bold);
			this.AutoButton = new Rectangle(0, 0, 243, 33);
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.PlanetSL != null)
                        this.PlanetSL.Dispose();
                }
                this.PlanetSL = null;
                this.disposed = true;
            }
        }

		public override void Draw(GameTime gameTime)
		{
			base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
			base.ScreenManager.SpriteBatch.Begin();
			this.TitleBar.Draw();
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Laserian14, Localizer.Token(1402), this.TitlePos, new Color(255, 239, 208));
			this.EMenu.Draw();
			Color TextColor = new Color(118, 102, 67, 50);
			this.PlanetSL.Draw(base.ScreenManager.SpriteBatch);
			if (this.PlanetSL.Entries.Count > 0)
			{
				PlanetListScreenEntry e1 = this.PlanetSL.Entries[this.PlanetSL.indexAtTop].item as PlanetListScreenEntry;
				PlanetListScreenEntry entry = this.PlanetSL.Entries[this.PlanetSL.indexAtTop].item as PlanetListScreenEntry;
				Vector2 TextCursor = new Vector2((float)(entry.SysNameRect.X + entry.SysNameRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(192)).X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
				this.sb_Sys.Text = Localizer.Token(192);
				this.sb_Sys.Update(TextCursor);
				this.sb_Sys.Draw(base.ScreenManager);
				TextCursor = new Vector2((float)(entry.PlanetNameRect.X + entry.PlanetNameRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(389)).X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
				this.sb_Name.Text = Localizer.Token(389);
				this.sb_Name.Update(TextCursor);
				this.sb_Name.Draw(base.ScreenManager);
				TextCursor = new Vector2((float)(entry.FertRect.X + entry.FertRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(386)).X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
				if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "Polish")
				{
					TextCursor = TextCursor + new Vector2(10f, 10f);
				}
				this.sb_Fert.Text = Localizer.Token(386);
				this.sb_Fert.Update(TextCursor);
				this.sb_Fert.Draw(base.ScreenManager, (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "Polish" ? Fonts.Arial12Bold : Fonts.Arial20Bold));
				TextCursor = new Vector2((float)(entry.RichRect.X + entry.RichRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(387)).X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
				if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "Polish")
				{
					TextCursor = TextCursor + new Vector2(10f, 10f);
				}
				this.sb_Rich.Text = Localizer.Token(387);
				this.sb_Rich.Update(TextCursor);
				this.sb_Rich.Draw(base.ScreenManager, (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "Polish" ? Fonts.Arial12Bold : Fonts.Arial20Bold));
				TextCursor = new Vector2((float)(entry.PopRect.X + entry.PopRect.Width / 2) - Fonts.Arial20Bold.MeasureString(Localizer.Token(1403)).X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
				if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "Polish")
				{
					TextCursor = TextCursor + new Vector2(15f, 10f);
				}
				this.sb_Pop.Text = Localizer.Token(1403);
				this.sb_Pop.Update(TextCursor);
				this.sb_Pop.Draw(base.ScreenManager, (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "Polish" ? Fonts.Arial12Bold : Fonts.Arial20Bold));
				TextCursor = new Vector2((float)(entry.OwnerRect.X + entry.OwnerRect.Width / 2) - Fonts.Arial20Bold.MeasureString("Owner").X / 2f, (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 28));
				if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "Polish")
				{
					TextCursor = TextCursor + new Vector2(10f, 10f);
				}
				this.sb_Owned.Text = "Owner";
				this.sb_Owned.Update(TextCursor);
				this.sb_Owned.Draw(base.ScreenManager, (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "Polish" ? Fonts.Arial12Bold : Fonts.Arial20Bold));
				Color smallHighlight = TextColor;
				smallHighlight.A = (byte)(TextColor.A / 2);
				for (int i = this.PlanetSL.indexAtTop; i < this.PlanetSL.Entries.Count && i < this.PlanetSL.indexAtTop + this.PlanetSL.entriesToDisplay; i++)
				{
					PlanetListScreenEntry entry2 = this.PlanetSL.Entries[i].item as PlanetListScreenEntry;
					if (i % 2 == 0)
					{
						Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, entry2.TotalEntrySize, smallHighlight);
					}
					if (entry2.planet == this.SelectedPlanet)
					{
						Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, entry2.TotalEntrySize, TextColor);
					}
					entry2.SetNewPos(this.eRect.X + 22, this.PlanetSL.Entries[i].clickRect.Y);
					entry2.Draw(base.ScreenManager, gameTime);
					Primitives2D.DrawRectangle(base.ScreenManager.SpriteBatch, entry2.TotalEntrySize, TextColor);
				}
				Color lineColor = new Color(118, 102, 67, 255);
				Vector2 topLeftSL = new Vector2((float)e1.SysNameRect.X, (float)(this.eRect.Y + 35));
				Vector2 botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
				topLeftSL = new Vector2((float)e1.PlanetNameRect.X, (float)(this.eRect.Y + 35));
				botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
				topLeftSL = new Vector2((float)e1.FertRect.X, (float)(this.eRect.Y + 35));
				botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
				topLeftSL = new Vector2((float)(e1.RichRect.X + 5), (float)(this.eRect.Y + 35));
				botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
				topLeftSL = new Vector2((float)e1.PopRect.X, (float)(this.eRect.Y + 35));
				botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
				topLeftSL = new Vector2((float)(e1.PopRect.X + e1.PopRect.Width), (float)(this.eRect.Y + 35));
				botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
				topLeftSL = new Vector2((float)(e1.OwnerRect.X + e1.OwnerRect.Width), (float)(this.eRect.Y + 35));
				botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
				topLeftSL = new Vector2((float)e1.TotalEntrySize.X, (float)(this.eRect.Y + 35));
				botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height - 35));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
				topLeftSL = new Vector2((float)(e1.TotalEntrySize.X + e1.TotalEntrySize.Width), (float)(this.eRect.Y + 35));
				botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + this.eRect.Height));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
				Vector2 leftBot = new Vector2((float)e1.TotalEntrySize.X, (float)(this.eRect.Y + this.eRect.Height));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, leftBot, botSL, lineColor);
				leftBot = new Vector2((float)e1.TotalEntrySize.X, (float)(this.eRect.Y + 35));
				botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + 35));
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, leftBot, botSL, lineColor);
			}
			this.cb_hideUninhabitable.Draw(base.ScreenManager);
			this.cb_hideOwned.Draw(base.ScreenManager);
			this.close.Draw(base.ScreenManager);
			ToolTip.Draw(base.ScreenManager);
			base.ScreenManager.SpriteBatch.End();
		}


		public override void HandleInput(InputState input)
		{
			this.PlanetSL.HandleInput(input);
			this.cb_hideOwned.HandleInput(input);
			this.cb_hideUninhabitable.HandleInput(input);
			if (this.sb_Sys.HandleInput(input))
			{
				this.LastSorted = this.sb_Sys;
				AudioManager.PlayCue("blip_click");
				this.sb_Sys.Ascending = !this.sb_Sys.Ascending;
				this.PlanetSL.Entries.Clear();
				this.PlanetSL.Copied.Clear();
				if (!this.sb_Sys.Ascending)
				{
					IOrderedEnumerable<Planet> sortedList = 
						from planet in this.planets
						orderby planet.system.Name descending
						select planet;
					foreach (Planet p in sortedList)
					{
						if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
						{
							continue;
						}
						PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
						this.PlanetSL.AddItem(entry);
					}
				}
				else
				{
					IOrderedEnumerable<Planet> sortedList = 
						from planet in this.planets
						orderby planet.system.Name
						select planet;
					foreach (Planet p in sortedList)
					{
						if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
						{
							continue;
						}
						PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
						this.PlanetSL.AddItem(entry);
					}
				}
			}
			if (this.sb_Name.HandleInput(input))
			{
				this.LastSorted = this.sb_Name;
				AudioManager.PlayCue("blip_click");
				this.sb_Name.Ascending = !this.sb_Name.Ascending;
				this.PlanetSL.Entries.Clear();
				this.PlanetSL.Copied.Clear();
				if (!this.sb_Name.Ascending)
				{
					IOrderedEnumerable<Planet> sortedList = 
						from planet in this.planets
						orderby planet.Name descending
						select planet;
					foreach (Planet p in sortedList)
					{
						if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
						{
							continue;
						}
						PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
						this.PlanetSL.AddItem(entry);
					}
				}
				else
				{
					IOrderedEnumerable<Planet> sortedList = 
						from planet in this.planets
						orderby planet.Name
						select planet;
					foreach (Planet p in sortedList)
					{
						if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
						{
							continue;
						}
						PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
						this.PlanetSL.AddItem(entry);
					}
				}
			}
			if (this.sb_Fert.HandleInput(input))
			{
				this.LastSorted = this.sb_Fert;
				AudioManager.PlayCue("blip_click");
				this.sb_Fert.Ascending = !this.sb_Fert.Ascending;
				this.PlanetSL.Entries.Clear();
				this.PlanetSL.Copied.Clear();
				if (!this.sb_Fert.Ascending)
				{
					IOrderedEnumerable<Planet> sortedList = 
						from planet in this.planets
						orderby planet.Fertility descending
						select planet;
					foreach (Planet p in sortedList)
					{
						if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
						{
							continue;
						}
						PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
						this.PlanetSL.AddItem(entry);
					}
				}
				else
				{
					IOrderedEnumerable<Planet> sortedList = 
						from planet in this.planets
						orderby planet.Fertility
						select planet;
					foreach (Planet p in sortedList)
					{
						if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
						{
							continue;
						}
						PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
						this.PlanetSL.AddItem(entry);
					}
				}
			}
			if (this.sb_Rich.HandleInput(input))
			{
				this.LastSorted = this.sb_Rich;
				AudioManager.PlayCue("blip_click");
				this.sb_Rich.Ascending = !this.sb_Rich.Ascending;
				this.PlanetSL.Entries.Clear();
				this.PlanetSL.Copied.Clear();
				if (!this.sb_Rich.Ascending)
				{
					IOrderedEnumerable<Planet> sortedList = 
						from planet in this.planets
						orderby planet.MineralRichness descending
						select planet;
					foreach (Planet p in sortedList)
					{
						if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
						{
							continue;
						}
						PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
						this.PlanetSL.AddItem(entry);
					}
				}
				else
				{
					IOrderedEnumerable<Planet> sortedList = 
						from planet in this.planets
						orderby planet.MineralRichness
						select planet;
					foreach (Planet p in sortedList)
					{
						if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
						{
							continue;
						}
						PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
						this.PlanetSL.AddItem(entry);
					}
				}
			}
			if (this.sb_Pop.HandleInput(input))
			{
				this.LastSorted = this.sb_Pop;
				AudioManager.PlayCue("blip_click");
				this.sb_Pop.Ascending = !this.sb_Pop.Ascending;
				this.PlanetSL.Entries.Clear();
				this.PlanetSL.Copied.Clear();
				if (!this.sb_Pop.Ascending)
				{
					IOrderedEnumerable<Planet> sortedList = 
						from planet in this.planets
						orderby planet.MaxPopulation descending
						select planet;
					foreach (Planet p in sortedList)
					{
						if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
						{
							continue;
						}
						PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
						this.PlanetSL.AddItem(entry);
					}
				}
				else
				{
					IOrderedEnumerable<Planet> sortedList = 
						from planet in this.planets
						orderby planet.MaxPopulation
						select planet;
					foreach (Planet p in sortedList)
					{
						if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
						{
							continue;
						}
						PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
						this.PlanetSL.AddItem(entry);
					}
				}
			}
			if (this.sb_Owned.HandleInput(input))
			{
				this.LastSorted = this.sb_Owned;
				AudioManager.PlayCue("blip_click");
				this.sb_Owned.Ascending = !this.sb_Owned.Ascending;
				this.PlanetSL.Entries.Clear();
				this.PlanetSL.Copied.Clear();
				if (!this.sb_Owned.Ascending)
				{
					IOrderedEnumerable<Planet> sortedList = 
						from planet in this.planets
						orderby planet.GetOwnerName() descending
						select planet;
					foreach (Planet p in sortedList)
					{
						if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
						{
							continue;
						}
						PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
						this.PlanetSL.AddItem(entry);
					}
				}
				else
				{
					IOrderedEnumerable<Planet> sortedList = 
						from planet in this.planets
						orderby planet.GetOwnerName()
						select planet;
					foreach (Planet p in sortedList)
					{
						if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
						{
							continue;
						}
						PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
						this.PlanetSL.AddItem(entry);
					}
				}
			}
			for (int i = this.PlanetSL.indexAtTop; i < this.PlanetSL.Entries.Count && i < this.PlanetSL.indexAtTop + this.PlanetSL.entriesToDisplay; i++)
			{
				PlanetListScreenEntry entry = this.PlanetSL.Entries[i].item as PlanetListScreenEntry;
				entry.HandleInput(input);
				entry.SetNewPos(this.eRect.X + 22, this.PlanetSL.Entries[i].clickRect.Y);
				if (HelperFunctions.CheckIntersection(entry.TotalEntrySize, input.CursorPosition) && input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
				{
					if (this.ClickTimer >= this.ClickDelay)
					{
						this.ClickTimer = 0f;
					}
					else
					{
						this.ExitScreen();
						this.empUI.screen.SelectedPlanet = entry.planet;
						this.empUI.screen.ViewPlanet(null);
						this.empUI.screen.transitionStartPosition = new Vector3(this.SelectedPlanet.Position.X, this.SelectedPlanet.Position.Y, 10000f);
						this.empUI.screen.returnToShip = false;
					}
					if (this.SelectedPlanet != entry.planet)
					{
						AudioManager.PlayCue("sd_ui_accept_alt3");
						this.SelectedPlanet = entry.planet;
					}
				}
			}
            if (input.CurrentKeyboardState.IsKeyDown(Keys.L) && !input.LastKeyboardState.IsKeyDown(Keys.L) && !GlobalStats.TakingInput)
            {
                AudioManager.PlayCue("echo_affirm");
                this.ExitScreen();
                return;
            }
			if (input.Escaped || input.RightMouseClick || this.close.HandleInput(input))
			{
				this.ExitScreen();
			}
		}

		public void ResetList()
		{
			List<Planet> pList = new List<Planet>();
			foreach (ScrollList.Entry entry in this.PlanetSL.Entries)
			{
				pList.Add((entry.item as PlanetListScreenEntry).planet);
			}
			this.PlanetSL.Entries.Clear();
			this.PlanetSL.Copied.Clear();
			this.PlanetSL.indexAtTop = 0;
			if (this.LastSorted == null)
			{
				foreach (Planet p in this.planets)
				{
					if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
					{
						continue;
					}
					PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
					this.PlanetSL.AddItem(entry);
				}
			}
			else
			{
				if (this.sb_Sys == this.LastSorted)
				{
					if (!this.sb_Sys.Ascending)
					{
						IOrderedEnumerable<Planet> sortedList = 
							from planet in this.planets
							orderby planet.system.Name descending
							select planet;
						foreach (Planet p in sortedList)
						{
							if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
							{
								continue;
							}
							PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
							this.PlanetSL.AddItem(entry);
						}
					}
					else
					{
						IOrderedEnumerable<Planet> sortedList = 
							from planet in this.planets
							orderby planet.system.Name
							select planet;
						foreach (Planet p in sortedList)
						{
							if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
							{
								continue;
							}
							PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
							this.PlanetSL.AddItem(entry);
						}
					}
				}
				if (this.sb_Name == this.LastSorted)
				{
					if (!this.sb_Name.Ascending)
					{
						IOrderedEnumerable<Planet> sortedList = 
							from planet in this.planets
							orderby planet.Name descending
							select planet;
						foreach (Planet p in sortedList)
						{
							if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
							{
								continue;
							}
							PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
							this.PlanetSL.AddItem(entry);
						}
					}
					else
					{
						IOrderedEnumerable<Planet> sortedList = 
							from planet in this.planets
							orderby planet.Name
							select planet;
						foreach (Planet p in sortedList)
						{
							if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
							{
								continue;
							}
							PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
							this.PlanetSL.AddItem(entry);
						}
					}
				}
				if (this.sb_Fert == this.LastSorted)
				{
					if (!this.sb_Fert.Ascending)
					{
						IOrderedEnumerable<Planet> sortedList = 
							from planet in this.planets
							orderby planet.Fertility descending
							select planet;
						foreach (Planet p in sortedList)
						{
							if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
							{
								continue;
							}
							PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
							this.PlanetSL.AddItem(entry);
						}
					}
					else
					{
						IOrderedEnumerable<Planet> sortedList = 
							from planet in this.planets
							orderby planet.Fertility
							select planet;
						foreach (Planet p in sortedList)
						{
							if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
							{
								continue;
							}
							PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
							this.PlanetSL.AddItem(entry);
						}
					}
				}
				if (this.sb_Rich == this.LastSorted)
				{
					this.LastSorted = this.sb_Rich;
					AudioManager.PlayCue("blip_click");
					this.sb_Rich.Ascending = !this.sb_Rich.Ascending;
					this.PlanetSL.Entries.Clear();
					this.PlanetSL.Copied.Clear();
					if (!this.sb_Rich.Ascending)
					{
						IOrderedEnumerable<Planet> sortedList = 
							from planet in this.planets
							orderby planet.MineralRichness descending
							select planet;
						foreach (Planet p in sortedList)
						{
							if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
							{
								continue;
							}
							PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
							this.PlanetSL.AddItem(entry);
						}
					}
					else
					{
						IOrderedEnumerable<Planet> sortedList = 
							from planet in this.planets
							orderby planet.MineralRichness
							select planet;
						foreach (Planet p in sortedList)
						{
							if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
							{
								continue;
							}
							PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
							this.PlanetSL.AddItem(entry);
						}
					}
				}
				if (this.sb_Pop == this.LastSorted)
				{
					this.LastSorted = this.sb_Pop;
					AudioManager.PlayCue("blip_click");
					this.sb_Pop.Ascending = !this.sb_Pop.Ascending;
					this.PlanetSL.Entries.Clear();
					this.PlanetSL.Copied.Clear();
					if (!this.sb_Pop.Ascending)
					{
						IOrderedEnumerable<Planet> sortedList = 
							from planet in this.planets
							orderby planet.MaxPopulation descending
							select planet;
						foreach (Planet p in sortedList)
						{
							if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
							{
								continue;
							}
							PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
							this.PlanetSL.AddItem(entry);
						}
					}
					else
					{
						IOrderedEnumerable<Planet> sortedList = 
							from planet in this.planets
							orderby planet.MaxPopulation
							select planet;
						foreach (Planet p in sortedList)
						{
							if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
							{
								continue;
							}
							PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
							this.PlanetSL.AddItem(entry);
						}
					}
				}
				if (this.sb_Owned == this.LastSorted)
				{
					this.LastSorted = this.sb_Owned;
					AudioManager.PlayCue("blip_click");
					this.sb_Owned.Ascending = !this.sb_Owned.Ascending;
					this.PlanetSL.Entries.Clear();
					this.PlanetSL.Copied.Clear();
					if (!this.sb_Owned.Ascending)
					{
						IOrderedEnumerable<Planet> sortedList = 
							from planet in this.planets
							orderby planet.GetOwnerName() descending
							select planet;
						foreach (Planet p in sortedList)
						{
							if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
							{
								continue;
							}
							PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
							this.PlanetSL.AddItem(entry);
						}
					}
					else
					{
						IOrderedEnumerable<Planet> sortedList = 
							from planet in this.planets
							orderby planet.GetOwnerName()
							select planet;
						foreach (Planet p in sortedList)
						{
							if (this.HideOwned && p.Owner != null || this.HideUninhab && !p.habitable)
							{
								continue;
							}
							PlanetListScreenEntry entry = new PlanetListScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 40, this);
							this.PlanetSL.AddItem(entry);
						}
					}
				}
			}
			if (this.PlanetSL.Entries.Count <= 0)
			{
				this.SelectedPlanet = null;
				return;
			}
			this.SelectedPlanet = (this.PlanetSL.Entries[this.PlanetSL.indexAtTop].item as PlanetListScreenEntry).planet;
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			PlanetListScreen clickTimer = this;
			clickTimer.ClickTimer = clickTimer.ClickTimer + elapsedTime;
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}