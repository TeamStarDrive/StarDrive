using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
    /// <summary>
    /// Information panel which is drawn when an unexplored planet is double-clicked
    /// </summary>
    public sealed class UnexploredPlanetScreen : PlanetScreen
    {
        Planet Planet;
        Menu2 TitleBar;
        Vector2 TitlePos;
        Menu1 PlanetInfoBkg;
        Submenu PlanetInfo;
        Rectangle PlanetIcon;

        public UnexploredPlanetScreen(GameScreen screen, Planet p) : base(screen)
        {
            Planet = p;
            IsPopup = true; // allow right-click dismiss

            var titleRect = new Rectangle(5, 44, LowRes ? 365 : 405, 80);
            TitleBar = new Menu2(titleRect);
            TitlePos = new Vector2(titleRect.X + titleRect.Width / 2 - Fonts.Laserian14.MeasureString(p.Name).X / 2f, titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
            var leftRect = new Rectangle(5, titleRect.Y + titleRect.Height + 5, 
                                         titleRect.Width, ScreenHeight - (titleRect.Y + titleRect.Height) - (int)(0.4f * ScreenHeight));
            if (leftRect.Height < 350)
                leftRect.Height = 350;

            PlanetInfoBkg = new Menu1(leftRect);
            var psubRect = new Rectangle(leftRect.X + 20, leftRect.Y + 20, leftRect.Width - 40, leftRect.Height - 40);
            PlanetInfo = new Submenu(psubRect);
            PlanetInfo.AddTab(GameText.PlanetInfo);
            PlanetIcon = new Rectangle(psubRect.X + psubRect.Width - 148, leftRect.Y + 55, 128, 128);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            // Title: /Extorm III/
            TitleBar.Draw(batch, elapsed);
            batch.DrawString(Fonts.Laserian14, Planet.Name, TitlePos, Colors.Cream);

            // Menu: Planet Info with background
            PlanetInfoBkg.Draw(batch, elapsed);
            PlanetInfo.Draw(batch, elapsed);

            // Planet icon
            batch.Draw(Planet.PlanetTexture, PlanetIcon, Color.White);

            float x = PlanetInfo.X + 20;
            float y = PlanetInfo.Y + 45;

            // second title: Extorm III
            batch.DrawString(Fonts.Arial20Bold, Planet.Name, new Vector2(x, y), Colors.Cream);

            // Class: Swamp
            DrawInfo(batch, x, y + 16, GameText.Class, Planet.LocalizedCategory, "");

            Graphics.Font font = Fonts.Arial12Bold;
            if (!Planet.Habitable)
            {
                batch.DrawString(font, Localizer.Token(GameText.Uninhabitable), x, y + 32, Color.Orange);
            }
            else
            {
                // Population: 0 / 1.5
                DrawInfo(batch, x, y + 32, GameText.Population, Planet.PopulationStringForPlayer, GameText.AColonysPopulationIsA);
                // Fertility: 0.8
                DrawInfo(batch, x, y + 48, GameText.Fertility, Planet.FertilityFor(EmpireManager.Player).String(), GameText.IndicatesHowMuchFoodThis);
                // Richness: 0.5
                DrawInfo(batch, x, y + 64, GameText.Richness, Planet.MineralRichness.String(), GameText.APlanetsMineralRichnessDirectly);
            }

            // planet description flavor text
            string desc = font.ParseText(Planet.Description, PlanetInfo.Width - 40);
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
