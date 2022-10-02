using Microsoft.Xna.Framework.Graphics;
using static Ship_Game.AI.ThreatMatrix;

namespace Ship_Game.Debug.Page;

public class AgentsDebug : DebugPage
{
    public AgentsDebug(DebugInfoScreen parent) : base(parent, DebugModes.Agents)
    {
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        int column = 0;
        foreach (Empire e in Universe.MajorEmpires)
        {
            if (!e.data.Defeated)
            {
                DrawAgents(e, column);
                ++column;
            }
        }

        base.Draw(batch, elapsed);
    }

    void DrawAgents(Empire e, int column)
    {
        Text.SetCursor(Parent.Win.X + 10 + 255 * column, Parent.Win.Y + 95, e.EmpireColor);
        Text.String("------------------------");
        Text.String(e.Name);
        Text.String("------------------------");

        Text.NewLine();
        Text.String($"Agent list ({e.data.AgentList.Count}):");
        Text.String("------------------------");
        foreach (Agent agent in e.data.AgentList.Sorted(a => a.Level))
        {
            Empire target = Universe.GetEmpireByName(agent.TargetEmpire);
            Color color = target?.EmpireColor ?? e.EmpireColor;
            Text.String(color, $"Level: {agent.Level}, Mission: {agent.Mission}, Turns: {agent.TurnsRemaining}");
        }
    }

    public override bool HandleInput(InputState input)
    {
        return base.HandleInput(input);
    }

    public override void Update(float fixedDeltaTime)
    {
        base.Update(fixedDeltaTime);
    }
}