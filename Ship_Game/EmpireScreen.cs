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
	public class EmpireScreen : GameScreen, IDisposable
	{
		private EmpireUIOverlay eui;

		//private bool LowRes;

		private Menu2 TitleBar;

		private Vector2 TitlePos;

		private Menu2 EMenu;

		private ScrollList ColoniesList;

		private Submenu ColonySubMenu;

		private Rectangle leftRect;

		private DropOptions GovernorDropdown;

		private CloseButton close;

		private Rectangle eRect;

		private float ClickDelay = 0.25f;

		public float ClickTimer;

		private SortButton pop;

		private SortButton food;

		private SortButton prod;

		private SortButton res;

		private SortButton money;

		private Rectangle AutoButton;

		//private bool AutoButtonHover;

		private Planet SelectedPlanet;

		public EmpireScreen(Ship_Game.ScreenManager ScreenManager, EmpireUIOverlay empUI)
		{
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
			base.IsPopup = true;
			this.eui = empUI;
			base.ScreenManager = ScreenManager;
			if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1280)
			{
				//this.LowRes = true;
			}
			this.pop = new SortButton();
			this.food = new SortButton();
			this.prod = new SortButton();
			this.res = new SortButton();
			this.money = new SortButton();
			Rectangle titleRect = new Rectangle(2, 44, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * 2 / 3, 80);
			this.TitleBar = new Menu2(ScreenManager, titleRect);
			this.TitlePos = new Vector2((float)(titleRect.X + titleRect.Width / 2) - Fonts.Laserian14.MeasureString(Localizer.Token(383)).X / 2f, (float)(titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2));
			this.leftRect = new Rectangle(2, titleRect.Y + titleRect.Height + 5, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 10, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - (titleRect.Y + titleRect.Height) - 7);
			this.close = new CloseButton(new Rectangle(this.leftRect.X + this.leftRect.Width - 40, this.leftRect.Y + 20, 20, 20));
			this.EMenu = new Menu2(ScreenManager, this.leftRect);
			this.eRect = new Rectangle(2, titleRect.Y + titleRect.Height + 25, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 40, (int)(0.66f * (float)(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - (titleRect.Y + titleRect.Height) - 7)));
			while (this.eRect.Height % 80 != 0)
			{
				this.eRect.Height = this.eRect.Height - 1;
			}
			this.ColonySubMenu = new Submenu(ScreenManager, this.eRect);
			this.ColoniesList = new ScrollList(this.ColonySubMenu, 80);
			foreach (Planet p in EmpireManager.GetEmpireByName(empUI.screen.PlayerLoyalty).GetPlanets())
			{
				EmpireScreenEntry entry = new EmpireScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 80, this);
				this.ColoniesList.AddItem(entry);
			}
			this.SelectedPlanet = (this.ColoniesList.Entries[this.ColoniesList.indexAtTop].item as EmpireScreenEntry).p;
			this.GovernorDropdown = new DropOptions(new Rectangle(0, 0, 100, 18));
			this.GovernorDropdown.AddOption("--", 1);
			this.GovernorDropdown.AddOption(Localizer.Token(4064), 0);
			this.GovernorDropdown.AddOption(Localizer.Token(4065), 2);
			this.GovernorDropdown.AddOption(Localizer.Token(4066), 4);
			this.GovernorDropdown.AddOption(Localizer.Token(4067), 3);
			this.GovernorDropdown.AddOption(Localizer.Token(4068), 5);
			this.GovernorDropdown.ActiveIndex = ColonyScreen.GetIndex(this.SelectedPlanet);
			if (this.GovernorDropdown.Options[this.GovernorDropdown.ActiveIndex].@value != (int)this.SelectedPlanet.colonyType)
			{
				this.SelectedPlanet.colonyType = (Planet.ColonyType)this.GovernorDropdown.Options[this.GovernorDropdown.ActiveIndex].@value;
				if (this.SelectedPlanet.colonyType != Planet.ColonyType.Colony)
				{
					this.SelectedPlanet.FoodLocked = true;
					this.SelectedPlanet.ProdLocked = true;
					this.SelectedPlanet.ResLocked = true;
					this.SelectedPlanet.GovernorOn = true;
				}
				else
				{
					this.SelectedPlanet.GovernorOn = false;
					this.SelectedPlanet.FoodLocked = false;
					this.SelectedPlanet.ProdLocked = false;
					this.SelectedPlanet.ResLocked = false;
				}
			}
			this.AutoButton = new Rectangle(0, 0, 140, 33);
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
			Rectangle buildingsRect;
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
			base.ScreenManager.SpriteBatch.Begin();
			this.TitleBar.Draw();
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Laserian14, Localizer.Token(383), this.TitlePos, new Color(255, 239, 208));
			this.EMenu.Draw();
			Color TextColor = new Color(118, 102, 67, 50);
			this.ColoniesList.Draw(base.ScreenManager.SpriteBatch);
			EmpireScreenEntry e1 = this.ColoniesList.Entries[this.ColoniesList.indexAtTop].item as EmpireScreenEntry;
			Rectangle PlanetInfoRect = new Rectangle(this.eRect.X + 22, this.eRect.Y + this.eRect.Height, (int)((float)base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * 0.3f), base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - this.eRect.Y - this.eRect.Height - 22);
			int iconSize = PlanetInfoRect.X + PlanetInfoRect.Height - (int)((float)(PlanetInfoRect.X + PlanetInfoRect.Height) * 0.4f);
			Rectangle PlanetIconRect = new Rectangle(PlanetInfoRect.X + 10, PlanetInfoRect.Y + PlanetInfoRect.Height / 2 - iconSize / 2, iconSize, iconSize);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Planets/", this.SelectedPlanet.planetType)], PlanetIconRect, Color.White);
			Vector2 nameCursor = new Vector2((float)(PlanetIconRect.X + PlanetIconRect.Width / 2) - Fonts.Pirulen16.MeasureString(this.SelectedPlanet.Name).X / 2f, (float)(PlanetInfoRect.Y + 15));
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, this.SelectedPlanet.Name, nameCursor, Color.White);
			Vector2 PNameCursor = new Vector2((float)(PlanetIconRect.X + PlanetIconRect.Width + 5), nameCursor.Y + 20f);
			string fmt = "0.#";
			float amount = 80f;
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "Polish")
			{
				amount = amount + 25f;
			}
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(384), ":"), PNameCursor, Color.Orange);
			Vector2 InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.SelectedPlanet.Type, InfoCursor, new Color(255, 239, 208));
			PNameCursor.Y = PNameCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(385), ":"), PNameCursor, Color.Orange);
			SpriteBatch spriteBatch = base.ScreenManager.SpriteBatch;
			SpriteFont arial12Bold = Fonts.Arial12Bold;
			float population = this.SelectedPlanet.Population / 1000f;
			string str = population.ToString(fmt);
			float maxPopulation = (this.SelectedPlanet.MaxPopulation + this.SelectedPlanet.MaxPopBonus) / 1000f;
			spriteBatch.DrawString(arial12Bold, string.Concat(str, "/", maxPopulation.ToString(fmt)), InfoCursor, new Color(255, 239, 208));
			Rectangle hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(385), ":")).X, Fonts.Arial12Bold.LineSpacing);
			if (HelperFunctions.CheckIntersection(hoverRect, MousePos))
			{
				ToolTip.CreateTooltip(75, base.ScreenManager);
			}
			PNameCursor.Y = PNameCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(386), ":"), PNameCursor, Color.Orange);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.SelectedPlanet.Fertility.ToString(fmt), InfoCursor, new Color(255, 239, 208));
			hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(386), ":")).X, Fonts.Arial12Bold.LineSpacing);
			if (HelperFunctions.CheckIntersection(hoverRect, MousePos))
			{
				ToolTip.CreateTooltip(20, base.ScreenManager);
			}
			PNameCursor.Y = PNameCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(387), ":"), PNameCursor, Color.Orange);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.SelectedPlanet.MineralRichness.ToString(fmt), InfoCursor, new Color(255, 239, 208));
			hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(387), ":")).X, Fonts.Arial12Bold.LineSpacing);
			if (HelperFunctions.CheckIntersection(hoverRect, MousePos))
			{
				ToolTip.CreateTooltip(21, base.ScreenManager);
			}
			PNameCursor.Y = PNameCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			PNameCursor.Y = PNameCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			string text = HelperFunctions.parseText(Fonts.Arial12Bold, this.SelectedPlanet.Description, (float)(PlanetInfoRect.Width - PlanetIconRect.Width + 15));
			if (Fonts.Arial12Bold.MeasureString(text).Y + PNameCursor.Y <= (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 20))
			{
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, PNameCursor, Color.White);
			}
			else
			{
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, HelperFunctions.parseText(Fonts.Arial12, this.SelectedPlanet.Description, (float)(PlanetInfoRect.Width - PlanetIconRect.Width + 15)), PNameCursor, Color.White);
			}
			Rectangle MapRect = new Rectangle(PlanetInfoRect.X + PlanetInfoRect.Width, PlanetInfoRect.Y, e1.QueueRect.X - (PlanetInfoRect.X + PlanetInfoRect.Width), PlanetInfoRect.Height);
			int desiredWidth = 700;
			int desiredHeight = 500;
			for (buildingsRect = new Rectangle(MapRect.X, MapRect.Y, desiredWidth, desiredHeight); !MapRect.Contains(buildingsRect); buildingsRect = new Rectangle(MapRect.X, MapRect.Y, desiredWidth, desiredHeight))
			{
				desiredWidth = desiredWidth - 7;
				desiredHeight = desiredHeight - 5;
			}
			buildingsRect = new Rectangle(MapRect.X + MapRect.Width / 2 - desiredWidth / 2, MapRect.Y, desiredWidth, desiredHeight);
			MapRect.X = buildingsRect.X;
			MapRect.Width = buildingsRect.Width;
			int xsize = buildingsRect.Width / 7;
			int ysize = buildingsRect.Height / 5;
			List<PlanetGridSquare> localPgsList = new List<PlanetGridSquare>();
			foreach (PlanetGridSquare pgs in this.SelectedPlanet.TilesList)
			{
				PlanetGridSquare pgnew = new PlanetGridSquare()
				{
					Biosphere = pgs.Biosphere,
					building = pgs.building,
					ClickRect = new Rectangle(buildingsRect.X + pgs.x * xsize, buildingsRect.Y + pgs.y * ysize, xsize, ysize),
					foodbonus = pgs.foodbonus,
					Habitable = pgs.Habitable,
					prodbonus = pgs.prodbonus,
					TroopsHere = pgs.TroopsHere,
					resbonus = pgs.resbonus
				};
				localPgsList.Add(pgnew);
			}
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("PlanetTiles/", this.SelectedPlanet.GetTile())], buildingsRect, Color.White);
			foreach (PlanetGridSquare pgs in localPgsList)
			{
				if (!pgs.Habitable)
				{
					Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, pgs.ClickRect, new Color(0, 0, 0, 200));
				}
				Primitives2D.DrawRectangle(base.ScreenManager.SpriteBatch, pgs.ClickRect, new Color(211, 211, 211, 70), 2f);
				if (pgs.building != null)
				{
					Rectangle bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 24, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 24, 48, 48);
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Buildings/icon_", pgs.building.Icon, "_48x48")], bRect, Color.White);
				}
				else if (pgs.QItem != null)
				{
					Rectangle bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 24, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 24, 48, 48);
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Buildings/icon_", pgs.QItem.Building.Icon, "_48x48")], bRect, new Color(255, 255, 255, 128));
				}
				this.DrawPGSIcons(pgs);
			}
			int xpos = (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - MapRect.Width) / 2;
			int ypos = (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - MapRect.Height) / 2;
			Rectangle rectangle = new Rectangle(xpos, ypos, MapRect.Width, MapRect.Height);
			Primitives2D.DrawRectangle(base.ScreenManager.SpriteBatch, MapRect, new Color(118, 102, 67, 255));
			Rectangle GovernorRect = new Rectangle(MapRect.X + MapRect.Width, MapRect.Y, e1.TotalEntrySize.X + e1.TotalEntrySize.Width - (MapRect.X + MapRect.Width), MapRect.Height);
			Primitives2D.DrawRectangle(base.ScreenManager.SpriteBatch, GovernorRect, new Color(118, 102, 67, 255));
			Rectangle portraitRect = new Rectangle(GovernorRect.X + 25, GovernorRect.Y + 25, 124, 148);
			if ((float)portraitRect.Width > 0.35f * (float)GovernorRect.Width)
			{
				portraitRect.Height = portraitRect.Height - (int)(0.25 * (double)portraitRect.Height);
				portraitRect.Width = portraitRect.Width - (int)(0.25 * (double)portraitRect.Width);
			}
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Portraits/", EmpireManager.GetEmpireByName(this.eui.screen.PlayerLoyalty).data.PortraitName)], portraitRect, Color.White);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Portraits/portrait_shine"], portraitRect, Color.White);
			if (this.SelectedPlanet.colonyType == Planet.ColonyType.Colony)
			{
				base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/x_red"], portraitRect, Color.White);
			}
			Primitives2D.DrawRectangle(base.ScreenManager.SpriteBatch, portraitRect, new Color(118, 102, 67, 255));
			Vector2 TextPosition = new Vector2((float)(portraitRect.X + portraitRect.Width + 25), (float)portraitRect.Y);
			Vector2 GovPos = TextPosition;
			switch (this.SelectedPlanet.colonyType)
			{
				case Planet.ColonyType.Core:
				{
					Localizer.Token(372);
					break;
				}
				case Planet.ColonyType.Colony:
				{
					Localizer.Token(376);
					break;
				}
				case Planet.ColonyType.Industrial:
				{
					Localizer.Token(373);
					break;
				}
				case Planet.ColonyType.Research:
				{
					Localizer.Token(375);
					break;
				}
				case Planet.ColonyType.Agricultural:
				{
					Localizer.Token(371);
					break;
				}
				case Planet.ColonyType.Military:
				{
					Localizer.Token(374);
					break;
				}
			}
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Governor", TextPosition, Color.White);
			TextPosition.Y = (float)(this.GovernorDropdown.r.Y + 25);
			string desc = "";
			switch (this.SelectedPlanet.colonyType)
			{
				case Planet.ColonyType.Core:
				{
					desc = HelperFunctions.parseText(Fonts.Arial12Bold, Localizer.Token(378), (float)(GovernorRect.Width - 50 - portraitRect.Width - 25));
					break;
				}
				case Planet.ColonyType.Colony:
				{
					desc = HelperFunctions.parseText(Fonts.Arial12Bold, Localizer.Token(382), (float)(GovernorRect.Width - 50 - portraitRect.Width - 25));
					break;
				}
				case Planet.ColonyType.Industrial:
				{
					desc = HelperFunctions.parseText(Fonts.Arial12Bold, Localizer.Token(379), (float)(GovernorRect.Width - 50 - portraitRect.Width - 25));
					break;
				}
				case Planet.ColonyType.Research:
				{
					desc = HelperFunctions.parseText(Fonts.Arial12Bold, Localizer.Token(381), (float)(GovernorRect.Width - 50 - portraitRect.Width - 25));
					break;
				}
				case Planet.ColonyType.Agricultural:
				{
					desc = HelperFunctions.parseText(Fonts.Arial12Bold, Localizer.Token(377), (float)(GovernorRect.Width - 50 - portraitRect.Width - 25));
					break;
				}
				case Planet.ColonyType.Military:
				{
					desc = HelperFunctions.parseText(Fonts.Arial12Bold, Localizer.Token(380), (float)(GovernorRect.Width - 50 - portraitRect.Width - 25));
					break;
				}
			}
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, desc, TextPosition, Color.White);
			desc = Localizer.Token(388);
			TextPosition = new Vector2((float)(this.AutoButton.X + this.AutoButton.Width / 2) - Fonts.Pirulen16.MeasureString(desc).X / 2f, (float)(this.AutoButton.Y + this.AutoButton.Height / 2 - Fonts.Pirulen16.LineSpacing / 2));
			this.GovernorDropdown.r.X = (int)GovPos.X;
			this.GovernorDropdown.r.Y = (int)GovPos.Y + Fonts.Arial12Bold.LineSpacing + 5;
			this.GovernorDropdown.Reset();
			this.GovernorDropdown.Draw(base.ScreenManager.SpriteBatch);
			if (this.ColoniesList.Entries.Count > 0)
			{
				EmpireScreenEntry entry = this.ColoniesList.Entries[this.ColoniesList.indexAtTop].item as EmpireScreenEntry;
				Vector2 TextCursor = new Vector2((float)(entry.SysNameRect.X + 30), (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 33));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(192), TextCursor, new Color(255, 239, 208));
				TextCursor = new Vector2((float)(entry.PlanetNameRect.X + 30), (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 33));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(389), TextCursor, new Color(255, 239, 208));
				this.pop.rect = new Rectangle(entry.PopRect.X + 15 - ResourceManager.TextureDict["NewUI/icon_food"].Width / 2, (int)TextCursor.Y, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
				base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_pop"], this.pop.rect, Color.White);
				this.food.rect = new Rectangle(entry.FoodRect.X + 15 - ResourceManager.TextureDict["NewUI/icon_food"].Width / 2, (int)TextCursor.Y, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
				base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_food"], this.food.rect, Color.White);
				this.prod.rect = new Rectangle(entry.ProdRect.X + 15 - ResourceManager.TextureDict["NewUI/icon_production"].Width / 2, (int)TextCursor.Y, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
				base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], this.prod.rect, Color.White);
				this.res.rect = new Rectangle(entry.ResRect.X + 15 - ResourceManager.TextureDict["NewUI/icon_science"].Width / 2, (int)TextCursor.Y, ResourceManager.TextureDict["NewUI/icon_science"].Width, ResourceManager.TextureDict["NewUI/icon_science"].Height);
				base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_science"], this.res.rect, Color.White);
				this.money.rect = new Rectangle(entry.MoneyRect.X + 15 - ResourceManager.TextureDict["NewUI/icon_money"].Width / 2, (int)TextCursor.Y, ResourceManager.TextureDict["NewUI/icon_money"].Width, ResourceManager.TextureDict["NewUI/icon_money"].Height);
				base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_money"], this.money.rect, Color.White);
				TextCursor = new Vector2((float)(entry.SliderRect.X + 30), (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 33));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(390), TextCursor, new Color(255, 239, 208));
				TextCursor = new Vector2((float)(entry.StorageRect.X + 30), (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 33));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(391), TextCursor, new Color(255, 239, 208));
				TextCursor = new Vector2((float)(entry.QueueRect.X + 30), (float)(this.eRect.Y - Fonts.Arial20Bold.LineSpacing + 33));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(392), TextCursor, new Color(255, 239, 208));
			}
			Color smallHighlight = TextColor;
			smallHighlight.A = (byte)(TextColor.A / 2);
			for (int i = this.ColoniesList.indexAtTop; i < this.ColoniesList.Entries.Count && i < this.ColoniesList.indexAtTop + this.ColoniesList.entriesToDisplay; i++)
			{
				EmpireScreenEntry entry = this.ColoniesList.Entries[i].item as EmpireScreenEntry;
				if (i % 2 == 0)
				{
					Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, entry.TotalEntrySize, smallHighlight);
				}
				if (entry.p == this.SelectedPlanet)
				{
					Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, entry.TotalEntrySize, TextColor);
				}
				entry.SetNewPos(this.eRect.X + 22, this.ColoniesList.Entries[i].clickRect.Y);
				entry.Draw(base.ScreenManager);
				Primitives2D.DrawRectangle(base.ScreenManager.SpriteBatch, entry.TotalEntrySize, TextColor);
			}
			Color lineColor = new Color(118, 102, 67, 255);
			Vector2 topLeftSL = new Vector2((float)e1.SysNameRect.X, (float)(this.eRect.Y + 35));
			Vector2 botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
			Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
			topLeftSL = new Vector2((float)e1.PlanetNameRect.X, (float)(this.eRect.Y + 35));
			botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
			Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
			topLeftSL = new Vector2((float)e1.PopRect.X, (float)(this.eRect.Y + 35));
			botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
			Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
			topLeftSL = new Vector2((float)e1.FoodRect.X, (float)(this.eRect.Y + 35));
			botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
			Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, new Color(lineColor, 100));
			topLeftSL = new Vector2((float)e1.ProdRect.X, (float)(this.eRect.Y + 35));
			botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
			Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, new Color(lineColor, 100));
			topLeftSL = new Vector2((float)e1.ResRect.X, (float)(this.eRect.Y + 35));
			botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
			Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, new Color(lineColor, 100));
			topLeftSL = new Vector2((float)e1.MoneyRect.X, (float)(this.eRect.Y + 35));
			botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
			Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, new Color(lineColor, 100));
			topLeftSL = new Vector2((float)e1.SliderRect.X, (float)(this.eRect.Y + 35));
			botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
			Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
			topLeftSL = new Vector2((float)(e1.StorageRect.X + 5), (float)(this.eRect.Y + 35));
			botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
			Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
			topLeftSL = new Vector2((float)e1.QueueRect.X, (float)(this.eRect.Y + 35));
			botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
			Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
			topLeftSL = new Vector2((float)e1.TotalEntrySize.X, (float)(this.eRect.Y + 35));
			botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
			Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
			topLeftSL = new Vector2((float)(e1.TotalEntrySize.X + e1.TotalEntrySize.Width), (float)(this.eRect.Y + 35));
			botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
			Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, topLeftSL, botSL, lineColor);
			Vector2 leftBot = new Vector2((float)e1.TotalEntrySize.X, (float)PlanetInfoRect.Y);
			Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, leftBot, botSL, lineColor);
			leftBot = new Vector2((float)e1.TotalEntrySize.X, (float)(this.eRect.Y + 35));
			botSL = new Vector2(topLeftSL.X, (float)(this.eRect.Y + 35));
			Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, leftBot, botSL, lineColor);
			Vector2 pos = new Vector2((float)base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - Fonts.Pirulen16.MeasureString("Paused").X - 13f, 44f);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, "Paused", pos, Color.White);
			this.close.Draw(base.ScreenManager);
			ToolTip.Draw(base.ScreenManager);
			base.ScreenManager.SpriteBatch.End();
		}

		private void DrawPGSIcons(PlanetGridSquare pgs)
		{
			if (pgs.Biosphere)
			{
				Rectangle biosphere = new Rectangle(pgs.ClickRect.X, pgs.ClickRect.Y, 20, 20);
				base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Buildings/icon_biosphere_48x48"], biosphere, Color.White);
			}
			if (pgs.TroopsHere.Count > 0)
			{
				pgs.TroopClickRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width - 36, pgs.ClickRect.Y, 35, 35);
				base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Troops/", pgs.TroopsHere[0].TexturePath)], pgs.TroopClickRect, Color.White);
			}
			float numFood = 0f;
			float numProd = 0f;
			float numRes = 0f;
			if (pgs.building != null)
			{
				if (pgs.building.PlusFlatFoodAmount > 0f || pgs.building.PlusFoodPerColonist > 0f)
				{
					numFood = numFood + pgs.building.PlusFoodPerColonist * this.SelectedPlanet.Population / 1000f * this.SelectedPlanet.FarmerPercentage;
					numFood = numFood + pgs.building.PlusFlatFoodAmount;
				}
				if (pgs.building.PlusFlatProductionAmount > 0f || pgs.building.PlusProdPerColonist > 0f)
				{
					numProd = numProd + pgs.building.PlusFlatProductionAmount;
					numProd = numProd + pgs.building.PlusProdPerColonist * this.SelectedPlanet.Population / 1000f * this.SelectedPlanet.WorkerPercentage;
				}
				if (pgs.building.PlusResearchPerColonist > 0f || pgs.building.PlusFlatResearchAmount > 0f)
				{
					numRes = numRes + pgs.building.PlusResearchPerColonist * this.SelectedPlanet.Population / 1000f * this.SelectedPlanet.ResearcherPercentage;
					numRes = numRes + pgs.building.PlusFlatResearchAmount;
				}
			}
			float total = numFood + numProd + numRes;
			float totalSpace = (float)(pgs.ClickRect.Width - 30);
			float spacing = totalSpace / total;
			Rectangle rect = new Rectangle(pgs.ClickRect.X, pgs.ClickRect.Y + pgs.ClickRect.Height - ResourceManager.TextureDict["NewUI/icon_food"].Height, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
			for (int i = 0; (float)i < numFood; i++)
			{
				if (numFood - (float)i <= 0f || numFood - (float)i >= 1f)
				{
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_food"], rect, Color.White);
				}
				else
				{
					Rectangle? nullable = null;
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_food"], new Vector2((float)rect.X, (float)rect.Y), nullable, Color.White, 0f, Vector2.Zero, numFood - (float)i, SpriteEffects.None, 1f);
				}
				rect.X = rect.X + (int)spacing;
			}
			for (int i = 0; (float)i < numProd; i++)
			{
				if (numProd - (float)i <= 0f || numProd - (float)i >= 1f)
				{
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], rect, Color.White);
				}
				else
				{
					Rectangle? nullable1 = null;
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], new Vector2((float)rect.X, (float)rect.Y), nullable1, Color.White, 0f, Vector2.Zero, numProd - (float)i, SpriteEffects.None, 1f);
				}
				rect.X = rect.X + (int)spacing;
			}
			for (int i = 0; (float)i < numRes; i++)
			{
				if (numRes - (float)i <= 0f || numRes - (float)i >= 1f)
				{
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_science"], rect, Color.White);
				}
				else
				{
					Rectangle? nullable2 = null;
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_science"], new Vector2((float)rect.X, (float)rect.Y), nullable2, Color.White, 0f, Vector2.Zero, numRes - (float)i, SpriteEffects.None, 1f);
				}
				rect.X = rect.X + (int)spacing;
			}
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
        ~EmpireScreen() {
            //should implicitly do the same thing as the original bad finalize
        }

		public override void HandleInput(InputState input)
		{
			Vector2 MousePos = new Vector2((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y);
			this.ColoniesList.HandleInput(input);
			if (HelperFunctions.CheckIntersection(this.pop.rect, MousePos))
			{
				ToolTip.CreateTooltip(Localizer.Token(2278), base.ScreenManager);
			}
			if (this.pop.HandleInput(input))
			{
				if (!this.pop.Ascending)
				{
					IOrderedEnumerable<Planet> sortedList = 
						from p in EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).GetPlanets()
						orderby p.Population
						select p;
					this.pop.Ascending = true;
					this.ResetListSorted(sortedList);
				}
				else
				{
					IOrderedEnumerable<Planet> sortedList = 
						from p in EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).GetPlanets()
						orderby p.Population descending
						select p;
					this.ResetListSorted(sortedList);
					this.pop.Ascending = false;
				}
			}
			if (HelperFunctions.CheckIntersection(this.food.rect, MousePos))
			{
				ToolTip.CreateTooltip(139, base.ScreenManager);
			}
			if (this.food.HandleInput(input))
			{
				if (!this.food.Ascending)
				{
					IOrderedEnumerable<Planet> sortedList = 
						from p in EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).GetPlanets()
						orderby p.NetFoodPerTurn - p.consumption
						select p;
					this.food.Ascending = true;
					this.ResetListSorted(sortedList);
				}
				else
				{
					IOrderedEnumerable<Planet> sortedList = 
						from p in EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).GetPlanets()
						orderby p.NetFoodPerTurn - p.consumption descending
						select p;
					this.ResetListSorted(sortedList);
					this.food.Ascending = false;
				}
			}
			if (HelperFunctions.CheckIntersection(this.prod.rect, MousePos))
			{
				ToolTip.CreateTooltip(140, base.ScreenManager);
			}
			if (this.prod.HandleInput(input))
			{
				if (!this.prod.Ascending)
				{
					IOrderedEnumerable<Planet> sortedList = EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).GetPlanets().OrderBy<Planet, float>((Planet p) => {
						if (p.Owner.data.Traits.Cybernetic == 0)
						{
							return p.NetProductionPerTurn;
						}
						return p.NetProductionPerTurn - p.consumption;
					});
					this.prod.Ascending = true;
					this.ResetListSorted(sortedList);
				}
				else
				{
					IOrderedEnumerable<Planet> sortedList = EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).GetPlanets().OrderByDescending<Planet, float>((Planet p) => {
						if (p.Owner.data.Traits.Cybernetic == 0)
						{
							return p.NetProductionPerTurn;
						}
						return p.NetProductionPerTurn - p.consumption;
					});
					this.ResetListSorted(sortedList);
					this.prod.Ascending = false;
				}
			}
			if (HelperFunctions.CheckIntersection(this.res.rect, MousePos))
			{
				ToolTip.CreateTooltip(141, base.ScreenManager);
			}
			if (this.res.HandleInput(input))
			{
				if (!this.res.Ascending)
				{
					IOrderedEnumerable<Planet> sortedList = 
						from p in EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).GetPlanets()
						orderby p.NetResearchPerTurn
						select p;
					this.res.Ascending = true;
					this.ResetListSorted(sortedList);
				}
				else
				{
					IOrderedEnumerable<Planet> sortedList = 
						from p in EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).GetPlanets()
						orderby p.NetResearchPerTurn descending
						select p;
					this.ResetListSorted(sortedList);
					this.res.Ascending = false;
				}
			}
			if (HelperFunctions.CheckIntersection(this.money.rect, MousePos))
			{
				ToolTip.CreateTooltip(142, base.ScreenManager);
			}
			if (this.money.HandleInput(input))
			{
				if (!this.money.Ascending)
				{
					IOrderedEnumerable<Planet> sortedList = 
						from p in EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).GetPlanets()
						orderby p.GrossMoneyPT + p.Owner.data.Traits.TaxMod * p.GrossMoneyPT - (p.TotalMaintenanceCostsPerTurn + p.TotalMaintenanceCostsPerTurn * p.Owner.data.Traits.MaintMod)
						select p;
					this.money.Ascending = true;
					this.ResetListSorted(sortedList);
				}
				else
				{
					IOrderedEnumerable<Planet> sortedList = 
						from p in EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).GetPlanets()
						orderby p.GrossMoneyPT + p.Owner.data.Traits.TaxMod * p.GrossMoneyPT - (p.TotalMaintenanceCostsPerTurn + p.TotalMaintenanceCostsPerTurn * p.Owner.data.Traits.MaintMod) descending
						select p;
					this.ResetListSorted(sortedList);
					this.money.Ascending = false;
				}
			}
			for (int i = this.ColoniesList.indexAtTop; i < this.ColoniesList.Entries.Count && i < this.ColoniesList.indexAtTop + this.ColoniesList.entriesToDisplay; i++)
			{
				EmpireScreenEntry entry = this.ColoniesList.Entries[i].item as EmpireScreenEntry;
				entry.HandleInput(input, base.ScreenManager);
				if (HelperFunctions.CheckIntersection(entry.TotalEntrySize, MousePos) && input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
				{
					if (this.SelectedPlanet != entry.p)
					{
						AudioManager.PlayCue("sd_ui_accept_alt3");
						this.SelectedPlanet = entry.p;
						this.GovernorDropdown.ActiveIndex = ColonyScreen.GetIndex(this.SelectedPlanet);
						if (this.GovernorDropdown.Options[this.GovernorDropdown.ActiveIndex].@value != (int)this.SelectedPlanet.colonyType)
						{
							this.SelectedPlanet.colonyType = (Planet.ColonyType)this.GovernorDropdown.Options[this.GovernorDropdown.ActiveIndex].@value;
							if (this.SelectedPlanet.colonyType != Planet.ColonyType.Colony)
							{
								this.SelectedPlanet.FoodLocked = true;
								this.SelectedPlanet.ProdLocked = true;
								this.SelectedPlanet.ResLocked = true;
								this.SelectedPlanet.GovernorOn = true;
							}
							else
							{
								this.SelectedPlanet.GovernorOn = false;
								this.SelectedPlanet.FoodLocked = false;
								this.SelectedPlanet.ProdLocked = false;
								this.SelectedPlanet.ResLocked = false;
							}
						}
					}
					if (this.ClickTimer >= this.ClickDelay || this.SelectedPlanet == null)
					{
						this.ClickTimer = 0f;
					}
					else
					{
						this.eui.screen.SelectedPlanet = this.SelectedPlanet;
						this.eui.screen.ViewPlanet(null);
						this.ExitScreen();
					}
				}
			}
			this.GovernorDropdown.HandleInput(input);
			if (this.GovernorDropdown.Options[this.GovernorDropdown.ActiveIndex].@value != (int)this.SelectedPlanet.colonyType)
			{
				this.SelectedPlanet.colonyType = (Planet.ColonyType)this.GovernorDropdown.Options[this.GovernorDropdown.ActiveIndex].@value;
				if (this.SelectedPlanet.colonyType != Planet.ColonyType.Colony)
				{
					this.SelectedPlanet.FoodLocked = true;
					this.SelectedPlanet.ProdLocked = true;
					this.SelectedPlanet.ResLocked = true;
					this.SelectedPlanet.GovernorOn = true;
				}
				else
				{
					this.SelectedPlanet.GovernorOn = false;
					this.SelectedPlanet.FoodLocked = false;
					this.SelectedPlanet.ProdLocked = false;
					this.SelectedPlanet.ResLocked = false;
				}
			}
            if (input.CurrentKeyboardState.IsKeyDown(Keys.U) && !input.LastKeyboardState.IsKeyDown(Keys.U) && !GlobalStats.TakingInput)
            {
                AudioManager.PlayCue("echo_affirm");
                this.ExitScreen();
            }                
			if (input.Escaped || input.RightMouseClick || this.close.HandleInput(input))
			{
				this.ExitScreen();
			}
		}

		public void ResetListSorted(IOrderedEnumerable<Planet> SortedList)
		{
			this.ColoniesList.Entries.Clear();
			this.ColoniesList.indexAtTop = 0;
			foreach (Planet p in SortedList)
			{
				EmpireScreenEntry entry = new EmpireScreenEntry(p, this.eRect.X + 22, this.leftRect.Y + 20, this.EMenu.Menu.Width - 30, 80, this);
				this.ColoniesList.AddItem(entry);
			}
			this.SelectedPlanet = (this.ColoniesList.Entries[this.ColoniesList.indexAtTop].item as EmpireScreenEntry).p;
			this.GovernorDropdown.ActiveIndex = ColonyScreen.GetIndex(this.SelectedPlanet);
			if (this.GovernorDropdown.Options[this.GovernorDropdown.ActiveIndex].@value != (int)this.SelectedPlanet.colonyType)
			{
				this.SelectedPlanet.colonyType = (Planet.ColonyType)this.GovernorDropdown.Options[this.GovernorDropdown.ActiveIndex].@value;
				if (this.SelectedPlanet.colonyType != Planet.ColonyType.Colony)
				{
					this.SelectedPlanet.FoodLocked = true;
					this.SelectedPlanet.ProdLocked = true;
					this.SelectedPlanet.ResLocked = true;
					this.SelectedPlanet.GovernorOn = true;
				}
				else
				{
					this.SelectedPlanet.GovernorOn = false;
					this.SelectedPlanet.FoodLocked = false;
					this.SelectedPlanet.ProdLocked = false;
					this.SelectedPlanet.ResLocked = false;
				}
			}
			for (int i = this.ColoniesList.indexAtTop; i < this.ColoniesList.Entries.Count && i < this.ColoniesList.indexAtTop + this.ColoniesList.entriesToDisplay; i++)
			{
				EmpireScreenEntry entry = this.ColoniesList.Entries[i].item as EmpireScreenEntry;
				entry.SetNewPos(this.eRect.X + 22, this.ColoniesList.Entries[i].clickRect.Y);
			}
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			EmpireScreen clickTimer = this;
			clickTimer.ClickTimer = clickTimer.ClickTimer + elapsedTime;
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}