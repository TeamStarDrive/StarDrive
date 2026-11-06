using System;
using System.Linq.Expressions;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Audio;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = SDGraphics.Rectangle;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.GameScreens.NewGame;

// a game extension as its icon and name, either of which turns it on or off: lit while on, dimmed while off, rimmed in gold under the mouse
public sealed class ExtensionToggle : UIElementV2
{
    public const int IconWidth = 58, IconHeight = 49;
    const int TextPadding = 12;
    static readonly Color OffColor = new(120, 120, 120);
    static readonly Color LockedColor = new(80, 80, 80);

    readonly Ref<bool> Binding;
    readonly SubTexture Icon;
    readonly SubTexture LitIcon;
    readonly Graphics.Font Font = Fonts.Arial12Bold;
    readonly string Text;
    readonly LocalizedText Tooltip;
    public bool Locked; // drawn gray and keeps its tooltip, but clicks do not change it
    int HoverFrame = -1;

    public ExtensionToggle(Vector2 pos, float width, Expression<Func<bool>> binding, string icon, in LocalizedText title, in LocalizedText tooltip)
    {
        Binding = new Ref<bool>(binding);
        Icon = ResourceManager.Texture("ExtensionIcons/" + icon);
        LitIcon = ResourceManager.Texture("ExtensionIcons/" + icon + "_On");
        Text = Font.ParseText(title, width - IconWidth - TextPadding);
        Tooltip = tooltip;
        Pos = new Vector2((int)pos.X, (int)pos.Y);
        Size = new Vector2(IconWidth + TextPadding + Font.MeasureString(Text).X, IconHeight);
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        var icon = new Rectangle((int)Pos.X, (int)Pos.Y, IconWidth, IconHeight);
        bool on = Binding.Value && !Locked;
        Color color = Locked ? LockedColor : on ? Color.White : OffColor;
        batch.Draw(ResourceManager.Texture("TechIcons/techbg"), icon, color);
        batch.Draw(on ? LitIcon : Icon, icon, color);
        // gold only while the mouse is over it in the input this toggle was given this frame
        if (!Locked && HoverFrame == GameBase.Base.FrameId)
        {
            batch.DrawRectangle(icon.Bevel(2), Color.Gold);
            batch.DrawRectangle(icon.Bevel(3), Color.Gold);
        }
        float textHeight = Font.MeasureString(Text).Y;
        batch.DrawString(Font, Text, new Vector2(icon.Right + TextPadding, (int)(CenterY - textHeight / 2)), color);
    }

    public override bool HandleInput(InputState input)
    {
        if (!Rect.HitTest(input.CursorPosition))
            return false;

        HoverFrame = GameBase.Base.FrameId;
        if (input.LeftMouseClick)
        {
            if (Locked)
                GameAudio.NegativeClick();
            else
                Binding.Value = !Binding.Value;
            return true;
        }
        if (Tooltip.IsValid)
            ToolTip.CreateTooltip(Tooltip);
        return input.RightMouseClick; // a right-click on it does nothing, while hovering leaves Esc to the screen
    }
}
