using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class UnexploredPlanetScreen : PlanetScreen
	{
		private Planet p;

		private Menu2 TitleBar;

		private Vector2 TitlePos;

		private Menu1 PlanetMenu;

		//private Rectangle titleRect;

		private bool LowRes;

		private Submenu PlanetInfo;

		private Rectangle PlanetIcon;

		private Ship_Game.ScreenManager ScreenManager;

		private MouseState currentMouse;

		private MouseState previousMouse;

		public UnexploredPlanetScreen(Planet p, Ship_Game.ScreenManager ScreenManager)
		{
			this.ScreenManager = ScreenManager;
			this.p = p;
			if (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1280)
			{
				this.LowRes = true;
			}
			Rectangle titleRect = new Rectangle(5, 44, 405, 80);
			if (this.LowRes)
			{
				titleRect.Width = 365;
			}
			this.TitleBar = new Menu2(ScreenManager, titleRect);
			this.TitlePos = new Vector2((float)(titleRect.X + titleRect.Width / 2) - Fonts.Laserian14.MeasureString(p.Name).X / 2f, (float)(titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2));
			Rectangle leftRect = new Rectangle(5, titleRect.Y + titleRect.Height + 5, titleRect.Width, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - (titleRect.Y + titleRect.Height) - (int)(0.4f * (float)ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight));
			if (leftRect.Height < 350)
			{
				leftRect.Height = 350;
			}
			this.PlanetMenu = new Menu1(ScreenManager, leftRect);
			Rectangle psubRect = new Rectangle(leftRect.X + 20, leftRect.Y + 20, leftRect.Width - 40, leftRect.Height - 40);
			this.PlanetInfo = new Submenu(ScreenManager, psubRect);
			this.PlanetInfo.AddTab("Planet Info");
			this.PlanetIcon = new Rectangle(psubRect.X + psubRect.Width - 148, leftRect.Y + 55, 128, 128);
		}

		public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
		{
			string d;
			this.TitleBar.Draw();
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			Color c = new Color(255, 239, 208);
			spriteBatch.DrawString(Fonts.Laserian14, this.p.Name, this.TitlePos, c);
			this.PlanetMenu.Draw();
			this.PlanetInfo.Draw();
			spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Planets/", this.p.planetType)], this.PlanetIcon, Color.White);
			Vector2 PNameCursor = new Vector2((float)(this.PlanetInfo.Menu.X + 20), (float)(this.PlanetInfo.Menu.Y + 45));
			spriteBatch.DrawString(Fonts.Arial20Bold, this.p.Name, PNameCursor, new Color(255, 239, 208));
			PNameCursor.Y = PNameCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing * 2);
			float amount = 80f;
			if (GlobalStats.Config.Language == "German")
			{
				amount = amount + 25f;
			}
			string fmt = "0.#";
			spriteBatch.DrawString(Fonts.Arial12Bold, "Class:", PNameCursor, Color.Orange);
			Vector2 InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
			spriteBatch.DrawString(Fonts.Arial12Bold, this.p.GetTypeTranslation(), InfoCursor, new Color(255, 239, 208));
			if (!this.p.habitable)
			{
				PNameCursor.Y = PNameCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				InfoCursor = new Vector2(PNameCursor.X + 80f, PNameCursor.Y);
				spriteBatch.DrawString(Fonts.Arial12Bold, "Uninhabitable", PNameCursor, Color.Orange);
			}
			else
			{
				PNameCursor.Y = PNameCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(385), ":"), PNameCursor, Color.Orange);
				SpriteBatch spriteBatch1 = this.ScreenManager.SpriteBatch;
				SpriteFont arial12Bold = Fonts.Arial12Bold;
				float population = this.p.Population / 1000f;
				string str = population.ToString(fmt);
				float maxPopulation = (this.p.MaxPopulation + this.p.MaxPopBonus) / 1000f;
				spriteBatch1.DrawString(arial12Bold, string.Concat(str, " / ", maxPopulation.ToString(fmt)), InfoCursor, new Color(255, 239, 208));
				Rectangle hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(385), ":")).X, Fonts.Arial12Bold.LineSpacing);
				if (HelperFunctions.CheckIntersection(hoverRect, MousePos))
				{
					ToolTip.CreateTooltip(75, this.ScreenManager);
				}
				PNameCursor.Y = PNameCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(386), ":"), PNameCursor, Color.Orange);
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.p.Fertility.ToString(fmt), InfoCursor, new Color(255, 239, 208));
				hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(386), ":")).X, Fonts.Arial12Bold.LineSpacing);
				if (HelperFunctions.CheckIntersection(hoverRect, MousePos))
				{
					ToolTip.CreateTooltip(20, this.ScreenManager);
				}
				PNameCursor.Y = PNameCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(387), ":"), PNameCursor, Color.Orange);
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.p.MineralRichness.ToString(fmt), InfoCursor, new Color(255, 239, 208));
				hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(387), ":")).X, Fonts.Arial12Bold.LineSpacing);
				if (HelperFunctions.CheckIntersection(hoverRect, MousePos))
				{
					ToolTip.CreateTooltip(21, this.ScreenManager);
				}
			}
			PNameCursor.Y = (float)(this.PlanetIcon.Y + this.PlanetIcon.Height + 20);
			string desc = this.parseText(this.p.Description, (float)(this.PlanetInfo.Menu.Width - 40));
			spriteBatch.DrawString(Fonts.Arial12Bold, desc, PNameCursor, new Color(255, 239, 208));
			if (this.p.Special != "None")
			{
				PNameCursor.Y = PNameCursor.Y + (Fonts.Arial12Bold.MeasureString(desc).Y + 10f);
				string special = this.p.Special;
				string str1 = special;
				if (special != null)
				{
					if (str1 == "Gold Deposits")
					{
						d = this.parseText("This planet has extensive gold deposits and would produce +5 credits per turn if colonized.", (float)(this.PlanetInfo.Menu.Width - 40));
						spriteBatch.DrawString(Fonts.Arial12Bold, d, PNameCursor, new Color(255, 239, 208));
						return;
					}
					if (str1 == "Platinum Deposits")
					{
						d = this.parseText("This planet has extensive platinum deposits and would produce +10 credits per turn if colonized.", (float)(this.PlanetInfo.Menu.Width - 40));
						spriteBatch.DrawString(Fonts.Arial12Bold, d, PNameCursor, new Color(255, 239, 208));
						return;
					}
					if (str1 == "Artifacts")
					{
						d = this.parseText("This planet has extensive archaeological curosities, and would provide +2 research points per turn if colonized.", (float)(this.PlanetInfo.Menu.Width - 40));
						spriteBatch.DrawString(Fonts.Arial12Bold, d, PNameCursor, new Color(255, 239, 208));
						return;
					}
					if (str1 == "Ancient Machinery")
					{
						d = this.parseText("This planet has a cache of ancient but functional alien machinery, and would reap +2 production per turn if colonized.", (float)(this.PlanetInfo.Menu.Width - 40));
						spriteBatch.DrawString(Fonts.Arial12Bold, d, PNameCursor, new Color(255, 239, 208));
						return;
					}
					if (str1 != "Spice")
					{
						return;
					}
					d = this.parseText("The native creatures of this planet secrete an incredible spice-like element with brain-enhancing properties.  If colonized, this planet would produce +5 research per turn", (float)(this.PlanetInfo.Menu.Width - 40));
					spriteBatch.DrawString(Fonts.Arial12Bold, d, PNameCursor, new Color(255, 239, 208));
				}
			}
		}

		public override void HandleInput(InputState input)
		{
			this.currentMouse = Mouse.GetState();
			this.previousMouse = Mouse.GetState();
		}

		private string parseText(string text, float Width)
		{
			string line = string.Empty;
			string returnString = string.Empty;
			string[] strArrays = text.Split(new char[] { ' ' });
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				string word = strArrays[i];
				if (Fonts.Arial12Bold.MeasureString(string.Concat(line, word)).Length() > Width)
				{
					returnString = string.Concat(returnString, line, '\n');
					line = string.Empty;
				}
				line = string.Concat(line, word, ' ');
			}
			return string.Concat(returnString, line);
		}
	}
}