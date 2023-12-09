using System;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;

namespace Ship_Game.Debug;

public class TextDrawerComponent
{
    readonly GameScreen Parent;
    SpriteBatch Batch => Parent.ScreenManager.SpriteBatch;

    public Vector2 Cursor = Vector2.Zero;
    public Color Color = Color.White;
    public Graphics.Font Font = Fonts.Arial12Bold;

    public TextDrawerComponent(GameScreen parent)
    {
        Parent = parent;
    }

    public void SetCursor(float x, float y, Color color)
    {
        Cursor = new(x, y);
        Color = color;
    }

    public void SetFont(Graphics.Font font)
    {
        Font = font;
    }

    public void String(string text)
    {
        Batch.DrawString(Font, text, Cursor, Color);
        NewLine(text.Count(c => c == '\n') + 1);
    }

    public void String(float offsetX, string text, bool newLine = true)
    {
        Batch.DrawString(Font, text, new(Cursor.X+offsetX,Cursor.Y), Color);
        if (newLine)
            NewLine(text.Count(c => c == '\n') + 1);
    }
    public void String(Color color, string text)
    {
        Batch.DrawString(Font, text, Cursor, color);
        NewLine(text.Count(c => c == '\n') + 1);
    }

    public void NewLine(int lines = 1)
    {
        int spacing = Font == Fonts.Arial12Bold ? Font.LineSpacing : Font.LineSpacing + 2;
        Cursor.Y += spacing * lines;
    }
}