using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    /// <summary>
    /// Information panel which is drawn when an unexplored planet is double-clicked
    /// </summary>
    public sealed class UnexploredPlanetScreen : PlanetScreen
    {
        Menu2 TitleBar;
        Vector2 TitlePos;
        Menu1 PlanetInfoBkg;
        Submenu PlanetInfo;
        RectF PlanetIcon;

        public UnexploredPlanetScreen(GameScreen screen, Planet p) : base(screen, p)
        {
            var titleRect = new Rectangle(5, 44, LowRes ? 365 : 405, 80);
            TitleBar = new Menu2(titleRect);
            TitlePos = new Vector2(titleRect.X + titleRect.Width / 2 - Fonts.Laserian14.MeasureString(p.Name).X / 2f, titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
            var leftRect = new Rectangle(5, titleRect.Y + titleRect.Height + 5, 
                                         titleRect.Width, ScreenHeight - (titleRect.Y + titleRect.Height) - (int)(0.4f * ScreenHeight));
            if (leftRect.Height < 350)
                leftRect.Height = 350;

            PlanetInfoBkg = new(leftRect);
            RectF psubRect = new(leftRect.X + 20, leftRect.Y + 20, leftRect.Width - 40, leftRect.Height - 40);
            PlanetInfo = new(psubRect, GameText.PlanetInfo);
            PlanetIcon = new(psubRect.X + psubRect.W - 148, leftRect.Y + 55, 128, 128);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            // Title: /Extorm III/
            TitleBar.Draw(batch, elapsed);
            batch.DrawString(Fonts.Laserian14, P.Name, TitlePos, Colors.Cream);

            // Menu: Planet Info with background    
            PlanetInfoBkg.Draw(batch, elapsed);
            PlanetInfo.Draw(batch, elapsed);

            // Planet icon
            batch.Draw(P.PlanetTexture, PlanetIcon, Color.White);

            float x = PlanetInfo.X + 20;
            float y = PlanetInfo.Y + 40;

            // second title: Extorm III
            batch.DrawString(Fonts.Arial20Bold, P.Name, new Vector2(x, y), Colors.Cream);

            // Class: Swamp
            DrawInfo(batch, x, y + 25, GameText.Class, P.LocalizedCategory, "");

            Graphics.Font font = Fonts.Arial12Bold;
            if (!P.Habitable)
            {
                batch.DrawString(font, Localizer.Token(GameText.Uninhabitable), x, y + 32, Color.Orange);
            }
            else
            {
                // Population: 0 / 1.5
                DrawInfo(batch, x, y + 41, GameText.Population, P.PopulationStringForPlayer, GameText.AColonysPopulationIsA);
                // Fertility: 0.8
                DrawInfo(batch, x, y + 57, GameText.Fertility, P.FertilityFor(P.Universe.Player).String(), GameText.IndicatesHowMuchFoodThis);
                // Richness: 0.5
                DrawInfo(batch, x, y + 73, GameText.Richness, P.MineralRichness.String(), GameText.APlanetsMineralRichnessDirectly);
            }

            // planet description flavor text
            string desc = font.ParseText(P.Description, PlanetInfo.Width - 40);
            batch.DrawString(font, desc, x, PlanetIcon.Bottom + 20, Colors.Cream);
        }

        void DrawInfo(SpriteBatch batch, float x, float y, LocalizedText title, string value, LocalizedText tooltip)
        {
            const int width = 80;
            Graphics.Font font = Fonts.Arial12Bold;
            batch.DrawString(font, title.Text + ":", x, y, Color.Orange);
            batch.DrawString(font, value, new Vector2(x + width, y), Colors.Cream);

            if (tooltip.IsValid)
            {
                var hoverRect = new Rectangle((int)x, (int)y, width + 20, font.LineSpacing);
                if (hoverRect.HitTest(Input.CursorPosition))
                {
                    ToolTip.CreateTooltip(GameText.APlanetsMineralRichnessDirectly);
                }
            }
        }
    }
}
