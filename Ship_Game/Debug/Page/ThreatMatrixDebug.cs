using Ship_Game.AI;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;

namespace Ship_Game.Debug.Page;

public class ThreatMatrixDebug : DebugPage
{
    readonly DebugEmpireSelectionSubmenu EmpireSelect;

    public ThreatMatrixDebug(DebugInfoScreen parent) : base(parent, DebugModes.ThreatMatrix)
    {
        EmpireSelect = base.Add(new DebugEmpireSelectionSubmenu(parent, parent.ModesTab.ClientArea.CutTop(10)));
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        float baseRadius = (int)Universe.ViewState / 100f;

        Empire e = EmpireSelect.Selected;
        var pins = e.AI.ThreatMatrix.GetPinsCopy();
        for (int i = 0; i < pins.Length; i++)
        {
            ThreatMatrix.Pin pin = pins[i];
            if (pin?.Ship != null && pin.Position != Vector2.Zero)
            {
                float radius = baseRadius + pin.Ship.Radius;
                if (Screen.IsInFrustum(pin.Position, radius))
                {
                    // the hexagon marks the observed pin with observed empire's color
                    Screen.DrawCircleProjected(pin.Position, radius, 6, pin.Empire.EmpireColor);
                    if (Universe.ViewState <= UniverseScreen.UnivScreenState.SystemView)
                        Screen.DrawStringProjected(pin.Position, radius*0.25f, pin.Empire.EmpireColor, $"{pin.Ship}");

                    // if it's within our borders, draw "InBorders" using our color
                    if (pin.InBorders && Universe.ViewState <= UniverseScreen.UnivScreenState.SystemView)
                    {
                        Screen.DrawStringProjected(pin.Position-new Vector2(radius,radius*1.5f), radius*0.5f, e.EmpireColor, "InBorders");
                    }

                    // put a rectangle around the pin to mark observed empire's color
                    Screen.DrawRectangleProjected(pin.Position, new(radius*2), 0f, e.EmpireColor);
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