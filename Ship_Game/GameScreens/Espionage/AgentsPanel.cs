using Microsoft.Xna.Framework;

namespace Ship_Game.GameScreens.Espionage
{
    public class AgentsPanel : UIPanel
    {
        public AgentsPanel(EspionageScreen screen, in Rectangle rect) : base(rect, EspionageScreen.PanelBackground)
        {
            Label(rect.X + 20, rect.Y + 10, GameText.EspionageAgents, Fonts.Arial20Bold);
        }
    }
}