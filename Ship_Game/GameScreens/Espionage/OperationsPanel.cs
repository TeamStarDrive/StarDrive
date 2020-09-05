using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.GameScreens.Espionage
{
    class OperationsPanel : UIPanel
    {
        readonly EspionageScreen Screen;
        readonly UILabel AgentName;
        readonly UILabel AgentLevel;
        public OperationsPanel(EspionageScreen screen, in Rectangle rect) : base(rect, EspionageScreen.PanelBackground)
        {
            Screen = screen;
            AgentName = Label(rect.X + 20, rect.Y + 10, "", Fonts.Arial20Bold);
            AgentLevel = Label(AgentName.X, AgentName.Y + Fonts.Arial20Bold.LineSpacing + 2, "", Fonts.Arial12Bold);
            AgentName.DropShadow = true;
            AgentLevel.Color = Color.Gray;
        }
        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            Agent agent = Screen.Agents.SelectedAgent;
            AgentName.Visible = agent != null;
            AgentLevel.Visible = agent != null;
            if (agent != null)
            {
                AgentName.Text = agent.Name;
                AgentLevel.Text = $"Level {agent.Level} Agent";
            }
            base.Draw(batch, elapsed);
        }
    }
}