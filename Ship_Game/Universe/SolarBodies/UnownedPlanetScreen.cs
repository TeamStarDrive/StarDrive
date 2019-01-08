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
				LowRes = true;
			}
			Rectangle titleRect = new Rectangle(5, 44, 405, 80);
			if (LowRes)
			{
				titleRect.Width = 365;
			}
			TitleBar = new Menu2(titleRect);
			TitlePos = new Vector2(titleRect.X + titleRect.Width / 2 - Fonts.Laserian14.MeasureString(p.Name).X / 2f, titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
			Rectangle leftRect = new Rectangle(5, titleRect.Y + titleRect.Height + 5, titleRect.Width, 
                ScreenHeight - (titleRect.Y + titleRect.Height) - (int)(0.4f * ScreenHeight));
			PlanetMenu = new Menu1(leftRect);
			Rectangle psubRect = new Rectangle(leftRect.X + 20, leftRect.Y + 20, leftRect.Width - 40, leftRect.Height - 40);
			PlanetInfo = new Submenu(psubRect);
			PlanetInfo.AddTab("Planet Info");
			PlanetIcon = new Rectangle(psubRect.X + psubRect.Width - 148, leftRect.Y + 45, 128, 128);
		}

		public override void Draw(SpriteBatch batch)
		{
			float x = Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, state.Y);
			TitleBar.Draw(batch);
			Color c = new Color(255, 239, 208);
			batch.DrawString(Fonts.Laserian14, p.Name, TitlePos, c);
			PlanetMenu.Draw();
			PlanetInfo.Draw();
			batch.Draw(ResourceManager.Texture(string.Concat("Planets/", p.PlanetType)), PlanetIcon, Color.White);
			Vector2 PNameCursor = new Vector2(PlanetInfo.Menu.X + 20, PlanetInfo.Menu.Y + 45);
			batch.DrawString(Fonts.Arial20Bold, p.Name, PNameCursor, new Color(255, 239, 208));
			PNameCursor.Y = PNameCursor.Y + Fonts.Arial20Bold.LineSpacing * 2;
			float amount = 80f;
			if (GlobalStats.IsGerman)
			{
				amount = amount + 25f;
			}
			batch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(384), ":"), PNameCursor, Color.Orange);
			Vector2 InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
			batch.DrawString(Fonts.Arial12Bold, p.GetTypeTranslation(), InfoCursor, new Color(255, 239, 208));
			PNameCursor.Y = PNameCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
			InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(385), ":"), PNameCursor, Color.Orange);
			SpriteBatch spriteBatch1 = ScreenManager.SpriteBatch;
			SpriteFont arial12Bold = Fonts.Arial12Bold;
			spriteBatch1.DrawString(arial12Bold, p.PopulationString, InfoCursor, new Color(255, 239, 208));
			Rectangle hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(385), ":")).X, Fonts.Arial12Bold.LineSpacing);
			if (hoverRect.HitTest(MousePos))
			{
				ToolTip.CreateTooltip(75);
			}
			PNameCursor.Y = PNameCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
			InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(386), ":"), PNameCursor, Color.Orange);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, p.Fertility.String(), InfoCursor, new Color(255, 239, 208));
			hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(386), ":")).X, Fonts.Arial12Bold.LineSpacing);
			if (hoverRect.HitTest(MousePos))
			{
				ToolTip.CreateTooltip(20);
			}
			PNameCursor.Y = PNameCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
			InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(387), ":"), PNameCursor, Color.Orange);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, p.MineralRichness.String(), InfoCursor, new Color(255, 239, 208));
			hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(387), ":")).X, Fonts.Arial12Bold.LineSpacing);
			if (hoverRect.HitTest(MousePos))
			{
				ToolTip.CreateTooltip(21);
			}
			PNameCursor.Y = PNameCursor.Y + Fonts.Arial12Bold.LineSpacing * 2;
			batch.DrawString(Fonts.Arial12Bold, parseText(p.Description, PlanetInfo.Menu.Width - 40), PNameCursor, new Color(255, 239, 208));
		}

		public override bool HandleInput(InputState input)
		{
			currentMouse = Mouse.GetState();
			previousMouse = Mouse.GetState();
            return base.HandleInput(input);
		}

		private string parseText(string text, float Width)
		{
			string line = string.Empty;
			string returnString = string.Empty;
			string[] strArrays = text.Split(' ');
			for (int i = 0; i < strArrays.Length; i++)
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