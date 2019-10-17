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
			TitleBar.Draw(batch);
			batch.DrawString(Fonts.Laserian14, p.Name, TitlePos, new Color(255, 239, 208));
			PlanetMenu.Draw();
			PlanetInfo.Draw(batch);
			batch.Draw(p.PlanetTexture, PlanetIcon, Color.White);
			var pNameCursor = new Vector2(PlanetInfo.Menu.X + 20, PlanetInfo.Menu.Y + 45);
			batch.DrawString(Fonts.Arial20Bold, p.Name, pNameCursor, new Color(255, 239, 208));
			pNameCursor.Y = pNameCursor.Y + Fonts.Arial20Bold.LineSpacing * 2;
			float amount = 80f;
			if (GlobalStats.IsGerman)
			{
				amount = amount + 25f;
			}
			batch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(384), ":"), pNameCursor, Color.Orange);
			var infoCursor = new Vector2(pNameCursor.X + amount, pNameCursor.Y);
			batch.DrawString(Fonts.Arial12Bold, p.LocalizedCategory, infoCursor, new Color(255, 239, 208));
			pNameCursor.Y = pNameCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
			infoCursor = new Vector2(pNameCursor.X + amount, pNameCursor.Y);
            batch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(385), ":"), pNameCursor, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, p.PopulationStringForPlayer, infoCursor, new Color(255, 239, 208));
			var hoverRect = new Rectangle((int)pNameCursor.X, (int)pNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(385), ":")).X, Fonts.Arial12Bold.LineSpacing);
			if (hoverRect.HitTest(Input.CursorPosition))
			{
				ToolTip.CreateTooltip(75);
			}
			pNameCursor.Y = pNameCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
			infoCursor = new Vector2(pNameCursor.X + amount, pNameCursor.Y);
            batch.DrawString(Fonts.Arial12Bold, Localizer.Token(386)+":", pNameCursor, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, p.FertilityFor(EmpireManager.Player).String(), infoCursor, new Color(255, 239, 208));
			hoverRect = new Rectangle((int)pNameCursor.X, (int)pNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(386), ":")).X, Fonts.Arial12Bold.LineSpacing);
			if (hoverRect.HitTest(Input.CursorPosition))
			{
				ToolTip.CreateTooltip(20);
			}
			pNameCursor.Y = pNameCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
			infoCursor = new Vector2(pNameCursor.X + amount, pNameCursor.Y);
            batch.DrawString(Fonts.Arial12Bold, Localizer.Token(387)+":", pNameCursor, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, p.MineralRichness.String(), infoCursor, new Color(255, 239, 208));
			hoverRect = new Rectangle((int)pNameCursor.X, (int)pNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(387), ":")).X, Fonts.Arial12Bold.LineSpacing);
			if (hoverRect.HitTest(Input.CursorPosition))
			{
				ToolTip.CreateTooltip(21);
			}
			pNameCursor.Y = pNameCursor.Y + Fonts.Arial12Bold.LineSpacing * 2;
			batch.DrawString(Fonts.Arial12Bold, parseText(p.Description, PlanetInfo.Menu.Width - 40), pNameCursor, new Color(255, 239, 208));
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