using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Debug.Page;

public class DefenseCoordinatorDebug : DebugPage
{
    public DefenseCoordinatorDebug(DebugInfoScreen parent) : base(parent, DebugModes.DefenseCo)
    {
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        foreach (Empire e in Universe.Empires)
        {
            DefensiveCoordinator defco = e.AI.DefensiveCoordinator;
            foreach (var kv in defco.DefenseDict)
            {
                Screen.DrawCircleProjectedZ(kv.Value.System.Position, kv.Value.RankImportance * 100, e.EmpireColor, 6);
                Screen.DrawCircleProjectedZ(kv.Value.System.Position, kv.Value.IdealShipStrength * 10, e.EmpireColor, 3);
                Screen.DrawCircleProjectedZ(kv.Value.System.Position, kv.Value.TroopsWanted * 100, e.EmpireColor, 4);
            }
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