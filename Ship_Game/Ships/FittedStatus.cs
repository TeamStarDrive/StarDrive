using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Color = Microsoft.Xna.Framework.Color;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Ships;

// a status line fitted to its width, kept while the text, the width and the fonts stay: one line in the panel's font, or in the smaller font where the language's text is wider, split onto two lines at the space nearest the middle where even that is too wide
public sealed class FittedStatus
{
    string FittedFrom;
    float FittedWidth;
    Graphics.Font FittedWith;
    public Graphics.Font Font { get; private set; }
    public string Line1 { get; private set; }
    public string Line2 { get; private set; }

    public void Fit(string text, float width)
    {
        if (ReferenceEquals(text, FittedFrom) && width == FittedWidth && FittedWith == Fonts.Arial12Bold)
            return;
        FittedFrom = text;
        FittedWidth = width;
        FittedWith = Font = Fonts.Arial12Bold;
        Line1 = text;
        Line2 = null;
        if (Font.TextWidth(text) <= width)
            return;
        Font = Fonts.Arial10;
        if (Font.TextWidth(text) <= width)
            return;

        int middle = text.Length / 2, split = -1;
        for (int i = 0; i < text.Length; ++i)
            if (text[i] == ' ' && (split < 0 || Math.Abs(i - middle) < Math.Abs(split - middle)))
                split = i;
        if (split > 0)
        {
            Line1 = text.Substring(0, split);
            Line2 = text.Substring(split + 1);
        }
    }

    // centered on x, or from x; one line on y, or two either side of it
    public void Draw(SpriteBatch batch, Color color, float x, float y, bool centered)
    {
        if (Line2 == null)
        {
            DrawLine(batch, Line1, color, x, y, centered);
            return;
        }
        DrawLine(batch, Line1, color, x, y - Font.LineSpacing / 2, centered);
        DrawLine(batch, Line2, color, x, y + Font.LineSpacing / 2, centered);
    }

    void DrawLine(SpriteBatch batch, string line, Color color, float x, float centerY, bool centered)
    {
        var pos = new Vector2(centered ? x - Font.TextWidth(line) / 2 : x, centerY - Font.LineSpacing / 2);
        batch.DrawString(Font, line, pos, color);
    }
}
