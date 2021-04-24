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
            Label(rect.X + 20, rect.Y + 10, GameText.AgentDossier, Fonts.Arial20Bold);
        }
        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);

            Agent agent = Screen.Agents.SelectedAgent;
            if (agent == null)
                return;

            var cursor = new Vector2(X + 20, Y + 10);

            void DrawText(GameText prefix, string text, Color color)
            {
                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(prefix) + text, cursor, color);
                cursor.Y += (Fonts.Arial12Bold.LineSpacing + 4);
            }

            void DrawValue(GameText prefix, short value)
            {
                DrawText(prefix, value.ToString(), value > 0 ? Color.White : Color.LightGray);
            }

            // @todo Why is this here?
            if (agent.HomePlanet.IsEmpty())
                agent.HomePlanet = EmpireManager.Player.data.Traits.HomeworldName;

            cursor.Y += 24;
            DrawText(GameText.Alias, agent.Name, Color.Orange);
            cursor.Y += 4;
            DrawText(GameText.Home, agent.HomePlanet, Color.LightGray);
            DrawText(GameText.Age, agent.Age.String(0), Color.LightGray);
            DrawText(GameText.Service, agent.ServiceYears.String(1) + Localizer.Token(GameText.Years), Color.LightGray);
            cursor.Y += 16;
            DrawValue(GameText.TrainingExercises, agent.Training);
            DrawValue(GameText.AgentsAssassinated, agent.Assassinations);
            DrawValue(GameText.ColoniesInfiltrated, agent.Infiltrations);
            DrawValue(GameText.ColoniesSabotaged, agent.Sabotages);
            DrawValue(GameText.TechnologiesStolen, agent.TechStolen);
            DrawValue(GameText.TreasuriesRobbed, agent.Robberies);
            DrawValue(GameText.RebellionsStarted, agent.Rebellions);
        }
    }
}
