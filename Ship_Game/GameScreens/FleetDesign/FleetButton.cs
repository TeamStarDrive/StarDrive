using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Fleets;
using System;

namespace Ship_Game.GameScreens.FleetDesign;

// Fleet hotkey button for FleetDesigner
public class FleetButton : UIPanel
{
    readonly UniverseScreen Screen;
    public readonly int FleetKey;
    public bool FleetDesigner = true;

    public Action<FleetButton> OnClick;
    public Func<FleetButton, bool> IsActive;

    public FleetButton(UniverseScreen us, int key, Vector2 size)
        : base(UI.LocalPos.Zero, size, Color.TransparentBlack)
    {
        Screen = us;
        FleetKey = key;
    }

    public override bool HandleInput(InputState input)
    {
        if (input.LeftMouseClick && HitTest(input.CursorPosition))
        {
            OnClick?.Invoke(this);
            return true;
        }
        return base.HandleInput(input);
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        base.Draw(batch, elapsed);

        bool isActive = IsActive(this);
        RectF r = RectF;

        if (FleetDesigner)
        {
            var sel = new Selector(r, Color.TransparentBlack);
            batch.Draw(ResourceManager.Texture("NewUI/rounded_square"), r, isActive ? new(0, 0, 255, 80) : Color.Black);
            sel.Draw(batch, elapsed);

            Fleet f = Screen.Player?.GetFleetOrNull(FleetKey);
            if (f?.DataNodes.Count > 0)
            {
                RectF firect = new(r.X + 6, r.Y + 6, r.W - 12, r.W - 12);
                batch.Draw(f.Icon, firect, Screen.Player.EmpireColor);
                if (f.AutoRequisition)
                {
                    RectF autoReq = new(firect.X + 54, firect.Y + 12, 20, 27);
                    var colorReq = Screen.ApplyCurrentAlphaToColor(Screen.Player.EmpireColor);
                    batch.Draw(ResourceManager.Texture("NewUI/AutoRequisition"), autoReq, colorReq);
                }
            }

            Vector2 cursor = new(r.X + 4, r.Y + 4);
            batch.DrawString(Fonts.Pirulen12, FleetKey.ToString(), cursor, Color.Orange);
            cursor.X += (r.W + 5);
            if (f != null)
                batch.DrawString(Fonts.Pirulen12, f.Name, cursor, isActive ? Color.White : Color.Gray);
        }
    }
}
