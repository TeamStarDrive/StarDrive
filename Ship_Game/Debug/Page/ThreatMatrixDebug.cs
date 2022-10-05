using Ship_Game.AI;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;

namespace Ship_Game.Debug.Page;

public class ThreatMatrixDebug : DebugPage
{
    public ThreatMatrixDebug(DebugInfoScreen parent) : base(parent, DebugModes.ThreatMatrix)
    {
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        float baseRadius = (int)Universe.ViewState / 100f;

        foreach (Empire e in Universe.Empires)
        {
            var pins = e.AI.ThreatMatrix.GetPinsCopy();
            for (int i = 0; i < pins.Length; i++)
            {
                ThreatMatrix.Pin pin = pins[i];
                if (pin?.Ship != null && pin.Position != Vector2.Zero)
                {
                    Screen.DrawCircleProjected(pin.Position,
                        baseRadius + pin.Ship.Radius, 6, e.EmpireColor);

                    if (pin.InBorders) Screen.DrawCircleProjected(pin.Position,
                        baseRadius + pin.Ship.Radius, 3, e.EmpireColor);
                }
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