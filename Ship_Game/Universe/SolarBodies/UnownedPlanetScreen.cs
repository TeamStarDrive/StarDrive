using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
	public sealed class UnownedPlanetScreen : PlanetScreen
	{
		private Planet p;

		private Menu2 TitleBar;

		private Vector2 TitlePos;

		private Menu1 PlanetMenu;

		//private Rectangle titleRect;

		private bool LowRes;

		private Submenu PlanetInfo;

		private Rectangle PlanetIcon;

		private MouseState currentMouse;

		private MouseState previousMouse;

		public UnownedPlanetScreen(GameScreen parent, Planet p) : base(parent)
		{
			this.p = p;
			if (ScreenWidth <= 1280)
			{
				this.LowRes = true;
			}
			Rectangle titleRect = new Rectangle(5, 44, 405, 80);
			if (this.LowRes)
			{
				titleRect.Width = 365;
			}
			this.TitleBar = new Menu2(titleRect);
			this.TitlePos = new Vector2((float)(titleRect.X + titleRect.Width / 2) - Fonts.Laserian14.MeasureString(p.Name).X / 2f, (float)(titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2));
			Rectangle leftRect = new Rectangle(5, titleRect.Y + titleRect.Height + 5, titleRect.Width, 
                ScreenHeight - (titleRect.Y + titleRect.Height) - (int)(0.4f * ScreenHeight));
			this.PlanetMenu = new Menu1(leftRect);
			Rectangle psubRect = new Rectangle(leftRect.X + 20, leftRect.Y + 20, leftRect.Width - 40, leftRect.Height - 40);
			this.PlanetInfo = new Submenu(psubRect);
			this.PlanetInfo.AddTab("Planet Info");
			this.PlanetIcon = new Rectangle(psubRect.X + psubRect.Width - 148, leftRect.Y + 45, 128, 128);
		}

		public override void Draw(SpriteBatch batch)
		{
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			this.TitleBar.Draw();
			Color c = new Color(255, 239, 208);
			batch.DrawString(Fonts.Laserian14, this.p.Name, this.TitlePos, c);
			this.PlanetMenu.Draw();
			this.PlanetInfo.Draw();
			batch.Draw(ResourceManager.Texture(string.Concat("Planets/", this.p.PlanetType)), this.PlanetIcon, Color.White);
			Vector2 PNameCursor = new Vector2((float)(this.PlanetInfo.Menu.X + 20), (float)(this.PlanetInfo.Menu.Y + 45));
			batch.DrawString(Fonts.Arial20Bold, this.p.Name, PNameCursor, new Color(255, 239, 208));
			PNameCursor.Y = PNameCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing * 2);
			string fmt = "#.#";
			float amount = 80f;
			if (GlobalStats.IsGerman)
			{
				amount = amount + 25f;
			}
			batch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(384), ":"), PNameCursor, Color.Orange);
			Vector2 InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
			batch.DrawString(Fonts.Arial12Bold, this.p.GetTypeTranslation(), InfoCursor, new Color(255, 239, 208));
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
			if (hoverRect.HitTest(MousePos))
			{
				ToolTip.CreateTooltip(75);
			}
			PNameCursor.Y = PNameCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
			this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(386), ":"), PNameCursor, Color.Orange);
			this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.p.Fertility.ToString(fmt), InfoCursor, new Color(255, 239, 208));
			hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(386), ":")).X, Fonts.Arial12Bold.LineSpacing);
			if (hoverRect.HitTest(MousePos))
			{
				ToolTip.CreateTooltip(20);
			}
			PNameCursor.Y = PNameCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
			this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(387), ":"), PNameCursor, Color.Orange);
			this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.p.MineralRichness.ToString(fmt), InfoCursor, new Color(255, 239, 208));
			hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(387), ":")).X, Fonts.Arial12Bold.LineSpacing);
			if (hoverRect.HitTest(MousePos))
			{
				ToolTip.CreateTooltip(21);
			}
			PNameCursor.Y = PNameCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing * 2);
			batch.DrawString(Fonts.Arial12Bold, this.parseText(this.p.Description, (float)(this.PlanetInfo.Menu.Width - 40)), PNameCursor, new Color(255, 239, 208));
		}

		public override bool HandleInput(InputState input)
		{
			this.currentMouse = Mouse.GetState();
			this.previousMouse = Mouse.GetState();
            return base.HandleInput(input);
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