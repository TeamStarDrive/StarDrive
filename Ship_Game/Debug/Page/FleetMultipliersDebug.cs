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
            if (!e.IsDefeated)
            {
                DrawMultipliers(e, column);
                ++column;
            }
        }

        base.Draw(batch, elapsed);
    }
    void DrawMultipliers(Empire e, int column)
    {
        Text.SetCursor(Parent.Win.X + 10 + 255 * column, Parent.Win.Y + 95, e.EmpireColor);
        Text.String("--------------------------");
        Text.String(e.Name);
        Text.String($"{e.Personality}");
        Text.String("----------------------------");
        Text.NewLine(2);
        Text.String("Remnants Strength Multipliers");
        Text.String("---------------------------");
        Empire remnants = Universe.Remnants;
        Text.String(remnants.EmpireColor, $"{remnants.Name}: {e.GetFleetStrEmpireMultiplier(remnants).String(2)}");
        Text.NewLine(2);
        Text.String("Empire Strength Multipliers");
        Text.String("---------------------------");
        foreach (Empire empire in Universe.ActiveMajorEmpires.Filter(empire => empire != e))
            Text.String($"{empire.Name}: {e.GetFleetStrEmpireMultiplier(empire).String(2)}");

        Text.NewLine(2);
        Text.String("Pirates Strength Multipliers");
        Text.String("---------------------------");
        foreach (Empire empire in Universe.PirateFactions.Filter(faction => faction != Universe.Unknown))
            Text.String(empire.EmpireColor, $"{empire.Name}: {e.GetFleetStrEmpireMultiplier(empire).String(2)}");
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