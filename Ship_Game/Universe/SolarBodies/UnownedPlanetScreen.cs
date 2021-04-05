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
		private Vector2 NotePos;
		private Submenu PlanetInfo;
		private Rectangle PlanetIcon;

		public UnownedPlanetScreen(GameScreen parent, Planet p) : base(parent)
		{
			this.p = p;
			IsPopup = true; // allow right-click dismiss
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
			NotePos = new Vector2(psubRect.X, psubRect.Y + 100);
			PlanetInfo = new Submenu(psubRect);
			PlanetInfo.AddTab("Planet Info");
			PlanetIcon = new Rectangle(psubRect.X + psubRect.Width - 148, leftRect.Y + 45, 128, 128);
		}

		public override void Draw(SpriteBatch batch, DrawTimes elapsed)
		{
			TitleBar.Draw(batch, elapsed);
			batch.DrawString(Fonts.Laserian14, p.Name, TitlePos, Colors.Cream);
			PlanetMenu.Draw(batch, elapsed);
			PlanetInfo.Draw(batch, elapsed);
			batch.Draw(p.PlanetTexture, PlanetIcon, Color.White);
			var pNameCursor = new Vector2(PlanetInfo.X + 20, PlanetInfo.Y + 45);
			batch.DrawString(Fonts.Arial20Bold, p.Name, pNameCursor, Colors.Cream);
			pNameCursor.Y = pNameCursor.Y + Fonts.Arial20Bold.LineSpacing * 2;
			float amount = 80f;
			batch.DrawString(Fonts.Arial12Bold, Localizer.Token(384)+":", pNameCursor, Color.Orange);
			var infoCursor = new Vector2(pNameCursor.X + amount, pNameCursor.Y);
			batch.DrawString(Fonts.Arial12Bold, p.LocalizedCategory, infoCursor, Colors.Cream);
			pNameCursor.Y = pNameCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
			infoCursor = new Vector2(pNameCursor.X + amount, pNameCursor.Y);
            batch.DrawString(Fonts.Arial12Bold, Localizer.Token(385)+":", pNameCursor, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, p.PopulationStringForPlayer, infoCursor, Colors.Cream);
			var hoverRect = new Rectangle((int)pNameCursor.X, (int)pNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(385)+":").X, Fonts.Arial12Bold.LineSpacing);
			if (hoverRect.HitTest(Input.CursorPosition))
			{
				ToolTip.CreateTooltip(75);
			}
			pNameCursor.Y = pNameCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
			infoCursor = new Vector2(pNameCursor.X + amount, pNameCursor.Y);
            batch.DrawString(Fonts.Arial12Bold, Localizer.Token(386)+":", pNameCursor, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, p.FertilityFor(EmpireManager.Player).String(), infoCursor, Colors.Cream);
			hoverRect = new Rectangle((int)pNameCursor.X, (int)pNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(386)+":").X, Fonts.Arial12Bold.LineSpacing);
			if (hoverRect.HitTest(Input.CursorPosition))
			{
				ToolTip.CreateTooltip(20);
			}
			pNameCursor.Y = pNameCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
			infoCursor = new Vector2(pNameCursor.X + amount, pNameCursor.Y);
            batch.DrawString(Fonts.Arial12Bold, Localizer.Token(387)+":", pNameCursor, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, p.MineralRichness.String(), infoCursor, Colors.Cream);
			hoverRect = new Rectangle((int)pNameCursor.X, (int)pNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(387)+":").X, Fonts.Arial12Bold.LineSpacing);
			if (hoverRect.HitTest(Input.CursorPosition))
			{
				ToolTip.CreateTooltip(21);
			}
			pNameCursor.Y += Fonts.Arial12Bold.LineSpacing * 2;
			batch.DrawString(Fonts.Arial12Bold, Fonts.Arial12Bold.ParseText(p.Description, PlanetInfo.Width - 40), pNameCursor, Colors.Cream);
			if (EmpireManager.Player.DifficultyModifiers.HideTacticalData)
			{
				pNameCursor.Y += NotePos.Y;
				batch.DrawString(Fonts.Arial12Bold, Fonts.Arial12Bold.ParseText(Localizer.Token(1863), PlanetInfo.Width - 40), pNameCursor, Color.Gold);
			}
		}
	}
}