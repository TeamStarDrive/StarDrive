using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Vector2 = SDGraphics.Vector2;
using System;
using SDGraphics;
using Font = Ship_Game.Graphics.Font;

namespace Ship_Game;

/// <summary>
/// Generic text-only button, initially used in DiplomacyScreen, but has been adopted in other places
/// </summary>
public class GenericButton : UIElementV2
{
    readonly LocalizedText Text;
    readonly Font Capital;
    readonly Font Regular;

    public LocalizedText Tooltip;

    // this is managed somewhat manually right now, legacy code reasons
    public bool ToggleOn;

    public Color ToggleOnColor = Color.White;
    public Color HoveredColor = Color.White;
    public Color UnHoveredColor = Color.DarkGray;

    bool Hover;

    public Action<GenericButton> OnClick;

    public enum Style
    {
        Normal,
        Shadow,
    }

    public Style ButtonStyle;

    public GenericButton(RectF r, in LocalizedText text, Font font)
        : base(r)
    {
        Regular = font;
        Text = text;
    }

    public GenericButton(Vector2 v, in LocalizedText text, Font capitalFont, Font smallFont)
    {
        Regular = smallFont;
        Capital = capitalFont;
        Text = text;

        string txt = text.Text;
        string capitalCase = txt[0].ToString();
        string lowerCase = txt.Substring(1);
        
        float capitalW = Capital.TextWidth(capitalCase);
        float textW = Regular.TextWidth(lowerCase);
        Pos = new(v.X - capitalW - textW, v.Y);
        Size = new(capitalW + textW, Capital.LineSpacing);
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        string text = Text.Text;

        Color color;
        if (ToggleOn) color = ToggleOnColor;
        else if (Hover) color = HoveredColor;
        else color = UnHoveredColor;

        if (Capital != null)
        {
            string capitalText = text[0].ToString();
            string lowercaseText = text.Substring(1);
            float capitalW = Capital.TextWidth(capitalText);

            Vector2 capitalPos = Pos;
            Vector2 lowercasePos = new(X + capitalW + 1f, Y + Capital.LineSpacing - Regular.LineSpacing - 3f);
            DrawText(batch, Capital, capitalText, capitalPos, color);
            DrawText(batch, Regular, lowercaseText, lowercasePos, color);
        }
        else
        {
            Vector2 pos = new(CenterX - Regular.TextWidth(text) / 2f, CenterY - Regular.LineSpacing / 2f);
            DrawText(batch, Regular, text, pos, color);
        }
    }

    void DrawText(SpriteBatch batch, Font font, string text, Vector2 pos, Color color)
    {
        if (ButtonStyle == Style.Normal)
        {
            batch.DrawString(font, text, pos, color);
        }
        else
        {
            batch.DrawDropShadowText(text, pos, font, color);
        }
    }

    public override bool HandleInput(InputState input)
    {
        if (!Enabled || !Visible)
            return false;

        Hover = HitTest(input.CursorPosition);
        if (Hover && input.LeftMouseClick)
        {
            GameAudio.EchoAffirmative();
            OnClick?.Invoke(this);
            return true;
        }

        if (Hover && Tooltip.IsValid)
            ToolTip.CreateTooltip(Tooltip);

        return false;
    }
}
