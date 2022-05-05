using System;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = SDGraphics.Vector2;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Ship_Game.GameScreens.Espionage
{
    public class AgentListItem : ScrollListItem<AgentListItem>
    {
        public Agent Agent;
        public UniverseScreen Universe;

        public AgentListItem(Agent agent, UniverseScreen universe)
        {
            Agent = agent;
            Universe = universe;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            SubTexture spy  = ResourceManager.Texture("UI/icon_spy");
            SubTexture star = ResourceManager.Texture("UI/icon_star");

            var r = new Rectangle((int)X, (int)Y, 25, 26);
            batch.Draw(spy, r, Color.White);
            var namecursor = new Vector2(r.X + 30, r.Y);

            batch.DrawString(Fonts.Arial12Bold, Agent.Name, namecursor, Color.White);
            namecursor.Y += (Fonts.Arial12Bold.LineSpacing + 2);
            batch.DrawString(Fonts.Arial12, Agent.MissionName, namecursor, Color.Gray);

            for (int j = 0; j < Agent.Level; j++)
            {
                var levelRect = new Rectangle((int)Right - 18 - 12 * j, (int)Y, 12, 11);
                batch.Draw(star, levelRect, Color.White);
            }

            if (Agent.Mission != AgentMission.Defending)
            {
                if (Agent.TargetEmpire.NotEmpty() && Agent.Mission != AgentMission.Training &&
                    Agent.Mission != AgentMission.Undercover)
                {
                    Vector2 targetCursor = namecursor;
                    targetCursor.X += 75f;
                    string mission = Localizer.Token(GameText.Target) + ": " +
                                     EmpireManager.GetEmpireByName(Agent.TargetEmpire).data.Traits.Plural;
                    batch.DrawString(Fonts.Arial12, mission, targetCursor, Color.Gray);
                }
                else if (Agent.TargetPlanetId != 0 && Agent.Mission == AgentMission.Undercover)
                {
                    Vector2 targetCursor = namecursor;
                    targetCursor.X += 75f;
                    string mission = Localizer.Token(GameText.Target) + ": " + Universe.UState.GetPlanet(Agent.TargetPlanetId).Name;
                    batch.DrawString(Fonts.Arial12, mission, targetCursor, Color.Gray);
                }

                if (Agent.Mission != AgentMission.Undercover)
                {
                    Vector2 turnsCursor = namecursor;
                    turnsCursor.X += 193f;
                    string mission = Localizer.Token(GameText.Turns) + ": " + Agent.TurnsRemaining;
                    batch.DrawString(Fonts.Arial12, mission, turnsCursor, Color.Gray);
                }
            }
        }
    }
}
