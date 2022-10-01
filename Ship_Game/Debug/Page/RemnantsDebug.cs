using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Fleets;

namespace Ship_Game.Debug.Page;

public class RemnantsDebug : DebugPage
{
    public RemnantsDebug(DebugInfoScreen parent) : base(parent, DebugModes.Remnants)
    {
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        Empire e = Universe.Remnants;
        SetTextCursor(Parent.Win.X + 10 + 255, Parent.Win.Y + 250, e.EmpireColor);
        DrawString($"Remnant Story: {e.Remnants.Story}");
        DrawString(!e.Remnants.Activated
            ? $"Trigger Progress: {e.Remnants.StoryTriggerKillsXp}/{e.Remnants.ActivationXpNeeded.String()}"
            : $"Level Up Stardate: {e.Remnants.NextLevelUpDate}");

        DrawString(!e.Remnants.Hibernating
            ? $"Next Hibernation in: {e.Remnants.NextLevelUpDate - e.Remnants.NeededHibernationTurns / 10f}"
            : $"Hibernating for: {e.Remnants.HibernationTurns} turns");

        string activatedString = e.Remnants.Activated ? "Yes" : "No";
        activatedString        = e.data.Defeated ? "Defeated" : activatedString;
        DrawString($"Activated: {activatedString}");
        DrawString($"Level: {e.Remnants.Level}");
        DrawString($"Resources: {e.Remnants.Production.String()}");
        NewLine();
        DrawString("Empires Population and Strength:");
        for (int i = 0; i < Universe.MajorEmpires.Length; i++)
        {
            Empire empire = Universe.MajorEmpires[i];
            if (!empire.data.Defeated)
                DrawString(empire.EmpireColor, $"{empire.data.Name} - Pop: {empire.TotalPopBillion.String()}, Strength: {empire.CurrentMilitaryStrength.String(0)}");
        }

        var empiresList = GlobalStats.RestrictAIPlayerInteraction ? Universe.NonPlayerMajorEmpires.Filter(emp => !emp.data.Defeated)
                                                                  : Universe.MajorEmpires.Filter(emp => !emp.data.Defeated);

        NewLine();
        float averagePop = empiresList.Average(empire => empire.TotalPopBillion);
        float averageStr = empiresList.Average(empire => empire.CurrentMilitaryStrength);
        DrawString($"AI Empire Average Pop:         {averagePop.String(1)}");
        DrawString($"AI Empire Average Strength: {averageStr.String(0)}");

        NewLine();
        Empire bestPop  = empiresList.FindMax(empire => empire.TotalPopBillion);
        Empire bestStr  = empiresList.FindMax(empire => empire.CurrentMilitaryStrength);
        Empire worstStr = empiresList.FindMin(empire => empire.CurrentMilitaryStrength);

        float diffFromAverageScore    = bestPop.TotalPopBillion / averagePop.LowerBound(1) * 100;
        float diffFromAverageStrBest  = bestStr.CurrentMilitaryStrength / averageStr.LowerBound(1) * 100;
        float diffFromAverageStrWorst = worstStr.CurrentMilitaryStrength / averageStr.LowerBound(1) * 100;

        DrawString(bestPop.EmpireColor, $"Highest Pop Empire: {bestPop.data.Name} ({(diffFromAverageScore - 100).String(1)}% above average)");
        DrawString(bestStr.EmpireColor, $"Strongest Empire:   {bestStr.data.Name} ({(diffFromAverageStrBest - 100).String(1)}% above average)");
        DrawString(worstStr.EmpireColor, $"Weakest Empire:     {worstStr.data.Name} ({(diffFromAverageStrWorst - 100).String(1)}% below average)");

        NewLine();
        DrawString("Goals:");
        foreach (Goal goal in e.AI.Goals)
        {
            if (goal.TargetPlanet != null)
            {
                Color color = goal.TargetPlanet.Owner?.EmpireColor ?? e.EmpireColor;
                DrawString(color, $"{goal.Type}, Target Planet: {goal.TargetPlanet.Name}");
            }
            else
            {
                DrawString($"{goal.Type}");
            }
        }

        NewLine();
        DrawString("Fleets:");
        foreach (Fleet fleet in e.Fleets)
        {
            if (fleet.FleetTask == null)
                continue;

            Color color = fleet.FleetTask.TargetPlanet?.Owner?.EmpireColor ?? e.EmpireColor;
            DrawString(color,$"Target Planet: {fleet.FleetTask.TargetPlanet?.Name ?? ""}, Ships: {fleet.Ships.Count}" +
                              $", str: {fleet.GetStrength().String()}, Task Step: {fleet.TaskStep}");
        }

        base.Draw(batch, elapsed);
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