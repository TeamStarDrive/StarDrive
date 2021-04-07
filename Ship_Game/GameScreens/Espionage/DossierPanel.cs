using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.GameScreens.Espionage
{
    class DossierPanel : UIPanel
    {
        readonly EspionageScreen Screen;
        public DossierPanel(EspionageScreen screen, in Rectangle rect) : base(rect, EspionageScreen.PanelBackground)
        {
            Screen = screen;
            Label(rect.X + 20, rect.Y + 10, 6092, Fonts.Arial20Bold);
        }
        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);

            Agent agent = Screen.Agents.SelectedAgent;
            if (agent == null)
                return;

            var cursor = new Vector2(X + 20, Y + 10);

            void DrawText(int token, string text, Color color)
            {
                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(token) + text, cursor, color);
                cursor.Y += (Fonts.Arial12Bold.LineSpacing + 4);
            }

            void DrawValue(int token, short value)
            {
                DrawText(token, value.ToString(), value > 0 ? Color.White : Color.LightGray);
            }

            // @todo Why is this here?
            if (agent.HomePlanet.IsEmpty())
                agent.HomePlanet = EmpireManager.Player.data.Traits.HomeworldName;

            cursor.Y += 24;
            DrawText(6108, agent.Name, Color.Orange);
            cursor.Y += 4;
            DrawText(6109, agent.HomePlanet, Color.LightGray);
            DrawText(6110, agent.Age.String(0), Color.LightGray);
            DrawText(6111, agent.ServiceYears.String(1) + Localizer.Token(GameText.Years), Color.LightGray);
            cursor.Y += 16;
            DrawValue(6112, agent.Training);
            DrawValue(6113, agent.Assassinations);
            DrawValue(6114, agent.Infiltrations);
            DrawValue(6115, agent.Sabotages);
            DrawValue(6116, agent.TechStolen);
            DrawValue(6117, agent.Robberies);
            DrawValue(6118, agent.Rebellions);
        }
    }
}
