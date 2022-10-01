using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using static Ship_Game.AI.ThreatMatrix;

namespace Ship_Game.Debug.Page;

public class FleetMultipliersDebug : DebugPage
{
    public FleetMultipliersDebug(DebugInfoScreen parent) : base(parent, DebugModes.FleetMulti)
    {
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        int column = 0;
        foreach (Empire e in Universe.ActiveNonPlayerMajorEmpires)
        {
            if (!e.data.Defeated)
            {
                DrawMultipliers(e, column);
                ++column;
            }
        }

        base.Draw(batch, elapsed);
    }
    void DrawMultipliers(Empire e, int column)
    {
        SetTextCursor(Parent.Win.X + 10 + 255 * column, Parent.Win.Y + 95, e.EmpireColor);
        DrawString("--------------------------");
        DrawString(e.Name);
        DrawString($"{e.Personality}");
        DrawString("----------------------------");
        NewLine(2);
        DrawString("Remnants Strength Multipliers");
        DrawString("---------------------------");
        Empire remnants = Universe.Remnants;
        DrawString(remnants.EmpireColor, $"{remnants.Name}: {e.GetFleetStrEmpireMultiplier(remnants).String(2)}");
        NewLine(2);
        DrawString("Empire Strength Multipliers");
        DrawString("---------------------------");
        foreach (Empire empire in Universe.ActiveMajorEmpires.Filter(empire => empire != e))
            DrawString($"{empire.Name}: {e.GetFleetStrEmpireMultiplier(empire).String(2)}");

        NewLine(2);
        DrawString("Pirates Strength Multipliers");
        DrawString("---------------------------");
        foreach (Empire empire in Universe.PirateFactions.Filter(faction => faction != Universe.Unknown))
            DrawString(empire.EmpireColor, $"{empire.Name}: {e.GetFleetStrEmpireMultiplier(empire).String(2)}");
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