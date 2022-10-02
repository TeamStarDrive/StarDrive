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
        Text.SetCursor(Parent.Win.X + 10 + 255 * column, Parent.Win.Y + 95, e.EmpireColor);
        Text.String("------------------------");
        Text.String(e.Name);
        Text.String("------------------------");
        Text.String($"Level: {e.Pirates.Level}");
        Text.String($"Pirate Bases Goals: {goals.Count(g => g.Type == GoalType.PirateBase)}");
        Text.String($"Spawned Ships: {e.Pirates.SpawnedShips.Count}");
        Text.NewLine();
        Text.String($"Payment Management Goals ({goals.Count(g => g.Type == GoalType.PirateDirectorPayment)})");
        Text.String("---------------------------------------------"); foreach (Goal g in goals)
        {
            if (g.Type == GoalType.PirateDirectorPayment)
            {
                Empire target     = g.TargetEmpire;
                string targetName = target.Name;
                int threatLevel   = e.Pirates.ThreatLevelFor(g.TargetEmpire);
                Text.String(target.EmpireColor, $"Payment Director For: {targetName}, Threat Level: {threatLevel}, Timer: {e.Pirates.PaymentTimerFor(target)}");
            }
        }

        Text.NewLine();
        Text.String($"Raid Management Goals ({goals.Count(g => g.Type == GoalType.PirateDirectorRaid)})");
        Text.String("---------------------------------------------");
        foreach (Goal g in goals)
        {
            if (g.Type == GoalType.PirateDirectorRaid)
            {
                Empire target = g.TargetEmpire;
                string targetName = target.Name;
                int threatLevel = e.Pirates.ThreatLevelFor(g.TargetEmpire);
                Text.String(target.EmpireColor, $"Raid Director For: {targetName}, Threat Level: {threatLevel}");
            }
        }

        Text.NewLine();
        Text.String($"Ongoing Raids ({goals.Count(g => g.IsRaid)}/{e.Pirates.Level})");
        Text.String("---------------------------------------------");
        foreach (Goal g in goals)
        {
            if (g.IsRaid)
            {
                Empire target = g.TargetEmpire;
                string targetName = target.Name;
                Ship targetShip = g.TargetShip;
                string shipName = targetShip?.Name ?? "None";
                Text.String(target.EmpireColor, $"{g.Type} vs. {targetName}, Target Ship: {shipName} in {targetShip?.SystemName ?? "None"}");
            }
        }

        Text.NewLine();

        Text.String($"Base Defense Goals ({goals.Count(g => g.Type == GoalType.PirateDefendBase)})");
        Text.String("---------------------------------------------");
        foreach (Goal g in goals)
        {
            if (g.Type == GoalType.PirateDefendBase)
            {
                Ship targetShip = g.TargetShip;
                string shipName = targetShip?.Name ?? "None";
                Text.String($"Defending {shipName} in {targetShip?.SystemName ?? "None"}");
            }
        }

        Text.NewLine();

        Text.String($"Fighter Designs We Can Launch ({e.Pirates.ShipsWeCanBuild.Count})");
        Text.String("---------------------------------------------");
        foreach (string shipName in e.Pirates.ShipsWeCanBuild)
            Text.String(shipName);

        Text.NewLine();

        Text.String($"Ship Designs We Can Spawn ({e.Pirates.ShipsWeCanSpawn.Count})");
        Text.String("---------------------------------------------");
        foreach (string shipName in e.Pirates.ShipsWeCanSpawn)
            Text.String(shipName);
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