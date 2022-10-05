using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Debug.Page;

public class InputDebug : DebugPage
{
    public InputDebug(DebugInfoScreen parent) : base(parent, DebugModes.Input)
    {
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        Text.SetCursor(50, 100, Color.White);
        Text.String($"Mouse Moved {Screen.Input.MouseMoved}");

        Text.String($"RightHold Held  {Screen.Input.RightHold.IsHolding}");
        Text.String($"RightHold Time  {Screen.Input.RightHold.Time}");
        Text.String($"RightHold Start {Screen.Input.RightHold.StartPos}");
        Text.String($"RightHold End   {Screen.Input.RightHold.EndPos}");

        Text.String($"LeftHold Held   {Screen.Input.LeftHold.IsHolding}");
        Text.String($"LeftHold Time   {Screen.Input.LeftHold.Time}");
        Text.String($"LeftHold Start  {Screen.Input.LeftHold.StartPos}");
        Text.String($"LeftHold End    {Screen.Input.LeftHold.EndPos}");

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