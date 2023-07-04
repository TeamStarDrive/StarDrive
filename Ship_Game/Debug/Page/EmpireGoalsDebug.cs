using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Linq;
using System.Reflection;

namespace Ship_Game.Debug.Page;

public class EmpireGoalsDebug : DebugPage
{
    bool ShowGoalStep = false;
    public EmpireGoalsDebug(DebugInfoScreen parent) : base(parent, DebugModes.Goals)
    {
        var list = AddList(50, 160);
        list.AddCheckbox(() => ShowGoalStep, "Show Goalsteps", "Show Goalsteps");
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        int column = 0;
        foreach (Empire e in Universe.MajorEmpires)
        {
            if (!e.IsDefeated)
            {
                DrawEmpireGoals(e, column);
                ++column;
            }
        }
        base.Draw(batch, elapsed);
    }

    void DrawEmpireGoals(Empire e, int column)
    {
        Text.SetCursor(Parent.Win.X + 10 + 255 * column, Parent.Win.Y + 95, e.EmpireColor);
        Text.String(e.data.Traits.Name);
        Text.String(e.EmpireColor, "----------------------");
        Text.String(e.EmpireColor, $"Total Goals: {e.AI.Goals.Count.String()}");
        Text.String(e.EmpireColor, "----------------------");
        Text.NewLine();

        Text.String(e.EmpireColor, "New Goals");
        Text.String(e.EmpireColor, "---------------------");
        int counter = 0;
        for (int i = e.AI.Goals.Count - 1; i >= 0; i--)
        {
            Goal goal = e.AI.Goals[i];
            DrawGoals(goal.LifeTime, goal.TypeName, goal.StepName, e.EmpireColor, ref counter, newGoal: true);
        }

        Text.NewLine(2);
        Text.String(e.EmpireColor, $"Older Goals:");
        Text.String(e.EmpireColor, "----------------------");
        counter = 0;
        for (int i = e.AI.Goals.Count - 1; i >= 0; i--) 
        {
            Goal goal = e.AI.Goals[i];
            DrawGoals(goal.LifeTime, goal.TypeName, goal.StepName, e.EmpireColor, ref counter, newGoal: false);
        }
    }


    void DrawGoals(float lifetimeInt, string name, string step, Color color, ref int counter, bool newGoal)
    {
        lifetimeInt = (int)((lifetimeInt + 0.01) * 10);
        if (newGoal && lifetimeInt <= 20 || !newGoal && lifetimeInt > 20)
        {
            counter++;
            Text.String(color, $"{counter}) {name} ({lifetimeInt.String()})");
            if (ShowGoalStep)
                Text.String(color, $"   -> {step}");
        }
    }
    public override bool HandleInput(InputState input)
    {
        return base.HandleInput(input);
    }
    /*
    public override void Update(float fixedDeltaTime)
    {
        base.Update(fixedDeltaTime);
    }*/
}