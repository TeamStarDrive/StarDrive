using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.AI.Tasks;

namespace Ship_Game.Debug.Page;

public class TasksDebug : DebugPage
{
    public TasksDebug(DebugInfoScreen parent) : base(parent, DebugModes.Tasks)
    {
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        int column = 0;
        foreach (Empire e in Universe.NonPlayerMajorEmpires)
        {
            if (!e.data.Defeated)
            {
                DrawTasks(e, column);
                ++column;
            }
        }

        base.Draw(batch, elapsed);
    }

    void DrawTasks(Empire e, int column)
    {
        SetTextCursor(Parent.Win.X + 10 + 300 * column, Parent.Win.Y + 200, e.EmpireColor);
        DrawString("--------------------------");
        DrawString(e.Name);
        DrawString($"{e.Personality}");
        DrawString($"Average War Grade: {e.GetAverageWarGrade()}");
        DrawString("----------------------------");
        int taskEvalLimit   = e.IsAtWarWithMajorEmpire ? (int)e.GetAverageWarGrade().LowerBound(3) : 10;
        int taskEvalCounter = 0;
        var tasks = e.AI.GetTasks().Filter(t => !t.QueuedForRemoval).OrderByDescending(t => t.Priority)
            .ThenByDescending(t => t.MinimumTaskForceStrength).ToArr();

        var tasksWithFleets = tasks.Filter(t => t.Fleet != null);
        if (tasksWithFleets.Length > 0)
        {
            DrawString(Color.Gray, "-----Tasks with Fleets------");
            for (int i = tasksWithFleets.Length - 1; i >= 0; i--)
            {
                MilitaryTask task = tasksWithFleets[i];
                DrawTask(task, e);
            }
        }

        var tasksForEval = tasks.Filter(t => t.NeedEvaluation);
        NewLine();
        DrawString(Color.Gray, "--Tasks Being Evaluated ---");
        for (int i = tasksForEval.Length - 1; i >= 0; i--)
        {
            if (taskEvalCounter == taskEvalLimit)
            {
                NewLine();
                DrawString(Color.Gray, "--------Queued Tasks--------");
            }

            MilitaryTask task = tasksForEval[i];
            DrawTask(task, e);
            if (task.NeedEvaluation)
                taskEvalCounter += 1;
        }
    }

    void DrawTask(MilitaryTask t, Empire e)
    {
        Color color   = t.TargetEmpire?.EmpireColor ?? e.EmpireColor;
        string target = t.TargetPlanet?.Name ?? t.TargetSystem?.Name ?? "";
        string fleet  = t.Fleet != null ? $"Fleet Step: {t.Fleet.TaskStep}" : "";
        float str     = t.Fleet?.GetStrength() ?? t.MinimumTaskForceStrength;
        DrawString(color, $"({t.Priority}) {t.Type}, {target}, str: {(int)str}, {fleet}");
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