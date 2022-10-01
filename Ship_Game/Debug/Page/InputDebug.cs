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

        SetTextCursor(50, 100, Color.White);
        DrawString($"Mouse Moved {Screen.Input.MouseMoved}");

        DrawString($"RightHold Held  {Screen.Input.RightHold.IsHolding}");
        DrawString($"RightHold Time  {Screen.Input.RightHold.Time}");
        DrawString($"RightHold Start {Screen.Input.RightHold.StartPos}");
        DrawString($"RightHold End   {Screen.Input.RightHold.EndPos}");

        DrawString($"LeftHold Held   {Screen.Input.LeftHold.IsHolding}");
        DrawString($"LeftHold Time   {Screen.Input.LeftHold.Time}");
        DrawString($"LeftHold Start  {Screen.Input.LeftHold.StartPos}");
        DrawString($"LeftHold End    {Screen.Input.LeftHold.EndPos}");

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