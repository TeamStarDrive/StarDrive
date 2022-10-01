using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Ships;
using System.Linq;
using System.Collections.Generic;

namespace Ship_Game.Debug.Page;

public class PiratesDebug : DebugPage
{
    public PiratesDebug(DebugInfoScreen parent) : base(parent, DebugModes.Pirates)
    {
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        int column = 0;
        foreach (Empire e in Universe.PirateFactions)
        {
            if (!e.data.Defeated)
            {
                DrawPirates(e, column);
                column += 3;
            }
        }

        base.Draw(batch, elapsed);
    }

    void DrawPirates(Empire e, int column)
    {
        IReadOnlyList<Goal> goals = e.Pirates.Owner.AI.Goals;
        SetTextCursor(Parent.Win.X + 10 + 255 * column, Parent.Win.Y + 95, e.EmpireColor);
        DrawString("------------------------");
        DrawString(e.Name);
        DrawString("------------------------");
        DrawString($"Level: {e.Pirates.Level}");
        DrawString($"Pirate Bases Goals: {goals.Count(g => g.Type == GoalType.PirateBase)}");
        DrawString($"Spawned Ships: {e.Pirates.SpawnedShips.Count}");
        NewLine();
        DrawString($"Payment Management Goals ({goals.Count(g => g.Type == GoalType.PirateDirectorPayment)})");
        DrawString("---------------------------------------------"); foreach (Goal g in goals)
        {
            if (g.Type == GoalType.PirateDirectorPayment)
            {
                Empire target     = g.TargetEmpire;
                string targetName = target.Name;
                int threatLevel   = e.Pirates.ThreatLevelFor(g.TargetEmpire);
                DrawString(target.EmpireColor, $"Payment Director For: {targetName}, Threat Level: {threatLevel}, Timer: {e.Pirates.PaymentTimerFor(target)}");
            }
        }

        NewLine();
        DrawString($"Raid Management Goals ({goals.Count(g => g.Type == GoalType.PirateDirectorRaid)})");
        DrawString("---------------------------------------------");
        foreach (Goal g in goals)
        {
            if (g.Type == GoalType.PirateDirectorRaid)
            {
                Empire target = g.TargetEmpire;
                string targetName = target.Name;
                int threatLevel = e.Pirates.ThreatLevelFor(g.TargetEmpire);
                DrawString(target.EmpireColor, $"Raid Director For: {targetName}, Threat Level: {threatLevel}");
            }
        }

        NewLine();
        DrawString($"Ongoing Raids ({goals.Count(g => g.IsRaid)}/{e.Pirates.Level})");
        DrawString("---------------------------------------------");
        foreach (Goal g in goals)
        {
            if (g.IsRaid)
            {
                Empire target = g.TargetEmpire;
                string targetName = target.Name;
                Ship targetShip = g.TargetShip;
                string shipName = targetShip?.Name ?? "None";
                DrawString(target.EmpireColor, $"{g.Type} vs. {targetName}, Target Ship: {shipName} in {targetShip?.SystemName ?? "None"}");
            }
        }

        NewLine();

        DrawString($"Base Defense Goals ({goals.Count(g => g.Type == GoalType.PirateDefendBase)})");
        DrawString("---------------------------------------------");
        foreach (Goal g in goals)
        {
            if (g.Type == GoalType.PirateDefendBase)
            {
                Ship targetShip = g.TargetShip;
                string shipName = targetShip?.Name ?? "None";
                DrawString($"Defending {shipName} in {targetShip?.SystemName ?? "None"}");
            }
        }

        NewLine();

        DrawString($"Fighter Designs We Can Launch ({e.Pirates.ShipsWeCanBuild.Count})");
        DrawString("---------------------------------------------");
        foreach (string shipName in e.Pirates.ShipsWeCanBuild)
            DrawString(shipName);

        NewLine();

        DrawString($"Ship Designs We Can Spawn ({e.Pirates.ShipsWeCanSpawn.Count})");
        DrawString("---------------------------------------------");
        foreach (string shipName in e.Pirates.ShipsWeCanSpawn)
            DrawString(shipName);
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