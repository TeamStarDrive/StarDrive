using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
	public sealed class UnownedPlanetScreen : PlanetScreen
	{
		private Menu2 TitleBar;
		private Vector2 TitlePos;
		private Menu1 PlanetMenu;
		private Vector2 NotePos;
		private Submenu PlanetInfo;
		private RectF PlanetIcon;

		public UnownedPlanetScreen(UniverseScreen universe, Planet p) : base(universe, p)
		{
			Rectangle titleRect = new Rectangle(5, 44, 405, 80);
			if (LowRes)
			{
				titleRect.Width = 365;
			}
			TitleBar = new(titleRect);
			TitlePos = new(titleRect.X + titleRect.Width / 2 - Fonts.Laserian14.MeasureString(p.Name).X / 2f, titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
			RectF leftRect = new(5, titleRect.Y + titleRect.Height + 5, titleRect.Width, 
                                 ScreenHeight - (titleRect.Y + titleRect.Height) - (int)(0.4f * ScreenHeight));
			PlanetMenu = new(leftRect);
			RectF psubRect = new(leftRect.X + 20, leftRect.Y + 20, leftRect.W - 40, leftRect.H - 40);
			NotePos = new(psubRect.X, psubRect.Y + 100);
			PlanetInfo = new(psubRect, "Planet Info");
			PlanetIcon = new(psubRect.X + psubRect.W - 148, leftRect.Y + 45, 128, 128);
		}

		public override void Draw(SpriteBatch batch, DrawTimes elapsed)
		{
			TitleBar.Draw(batch, elapsed);
			batch.DrawString(Fonts.Laserian14, P.Name, TitlePos, Colors.Cream);
			PlanetMenu.Draw(batch, elapsed);
			PlanetInfo.Draw(batch, elapsed);
			batch.Draw(P.PlanetTexture, PlanetIcon, Color.White);
			var pNameCursor = new Vector2(PlanetInfo.X + 20, PlanetInfo.Y + 45);
			batch.DrawString(Fonts.Arial20Bold, P.Name, pNameCursor, Colors.Cream);
			pNameCursor.Y += Fonts.Arial20Bold.LineSpacing * 2;
			float amount = 80f;
			batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.Class)+":", pNameCursor, Color.Orange);
			var infoCursor = new Vector2(pNameCursor.X + amount, pNameCursor.Y);
			batch.DrawString(Fonts.Arial12Bold, P.LocalizedCategory, infoCursor, Colors.Cream);
			pNameCursor.Y += Fonts.Arial12Bold.LineSpacing + 2;
			if (P.IsExploredBy(Player))
			{
				infoCursor = new Vector2(pNameCursor.X + amount, pNameCursor.Y);
				batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.Population) + ":", pNameCursor, Color.Orange);
				batch.DrawString(Fonts.Arial12Bold, P.PopulationStringForPlayer, infoCursor, Colors.Cream);
				var hoverRect = new Rectangle((int)pNameCursor.X, (int)pNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(GameText.Population) + ":").X, Fonts.Arial12Bold.LineSpacing);
				if (hoverRect.HitTest(Input.CursorPosition))
				{
					ToolTip.CreateTooltip(GameText.AColonysPopulationIsA);
				}
				pNameCursor.Y += Fonts.Arial12Bold.LineSpacing + 2;
				infoCursor = new Vector2(pNameCursor.X + amount, pNameCursor.Y);
				batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.Fertility) + ":", pNameCursor, Color.Orange);
				batch.DrawString(Fonts.Arial12Bold, P.FertilityFor(Player).String(), infoCursor, Colors.Cream);
				hoverRect = new Rectangle((int)pNameCursor.X, (int)pNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(GameText.Fertility) + ":").X, Fonts.Arial12Bold.LineSpacing);
				if (hoverRect.HitTest(Input.CursorPosition))
				{
					ToolTip.CreateTooltip(GameText.IndicatesHowMuchFoodThis);
				}
				pNameCursor.Y += Fonts.Arial12Bold.LineSpacing + 2;
				infoCursor = new Vector2(pNameCursor.X + amount, pNameCursor.Y);
                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.Richness) + ":", pNameCursor, P.IsMineable ? Color.Gold : Colors.Cream);
                batch.DrawString(Fonts.Arial12Bold, P.MineralRichness.String(), infoCursor, Colors.Cream);
				hoverRect = new Rectangle((int)pNameCursor.X, (int)pNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(GameText.Richness) + ":").X, Fonts.Arial12Bold.LineSpacing);
				if (hoverRect.HitTest(Input.CursorPosition))
				{
                    ToolTip.CreateTooltip(P.IsMineable ? GameText.MineableRichnessTip : GameText.APlanetsMineralRichnessDirectly);
                }
				if (P.IsMineable)
				{
                    pNameCursor.Y += Fonts.Arial12Bold.LineSpacing + 2;
                    infoCursor = new Vector2(pNameCursor.X + amount, pNameCursor.Y);
                    batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.ResourceName) + ":", pNameCursor, Color.Gold);
                    batch.DrawString(Fonts.Arial12Bold, P.Mining.TranslatedResourceName.Text, infoCursor, Colors.Cream);
                    hoverRect = new Rectangle((int)pNameCursor.X, (int)pNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(GameText.Richness) + ":").X, Fonts.Arial12Bold.LineSpacing);
                    if (hoverRect.HitTest(Input.CursorPosition))
                    {
                        ToolTip.CreateTooltip(P.Mining.ResourceDescription.Text);
                    }
                }
				pNameCursor.Y += Fonts.Arial12Bold.LineSpacing * 2;
				batch.DrawString(Fonts.Arial12Bold, Fonts.Arial12Bold.ParseText(P.Description, PlanetInfo.Width - 40), pNameCursor, Colors.Cream);
			}
			else if (!P.IsExploredBy(Player))
			{
				pNameCursor.Y += Fonts.Arial12Bold.LineSpacing + 2;
				batch.DrawString(Fonts.Arial20Bold, Localizer.Token(GameText.Unexplored), pNameCursor, Color.Gray);
				pNameCursor.Y += Fonts.Arial12Bold.LineSpacing + 2;
			}

			if (Player.DifficultyModifiers.HideTacticalData)
			{
				pNameCursor.Y += NotePos.Y - 40;
				batch.DrawString(Fonts.Arial12Bold, Fonts.Arial12Bold.ParseText(Localizer.Token(GameText.NoteInOrderToSee), PlanetInfo.Width - 40), pNameCursor, Color.Gold);
			}
		}
	}
}
