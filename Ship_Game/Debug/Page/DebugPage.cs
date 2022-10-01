using System;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Ship_Game.Universe;

namespace Ship_Game.Debug.Page;

public class DebugPage : UIElementContainer
{
    public new DebugInfoScreen Parent;
    public UniverseScreen Screen => Parent.Screen;
    public UniverseState Universe => Parent.Universe;
    public Empire Player => Parent.Universe.Player;

    public DebugModes Mode { get; }
    protected Array<UILabel> TextColumns = new();

    protected Vector2 TextCursor = Vector2.Zero;
    protected Color TextColor = Color.White;
    protected Graphics.Font TextFont = Fonts.Arial12Bold;

    public DebugPage(DebugInfoScreen parent, DebugModes mode) : base(parent.Rect)
    {
        Parent = parent;
        Mode = mode;
        Name = mode.ToString();
    }
        
    void ShowDebugGameInfo(int column, DebugTextBlock block, float x, float y)
    {
        if (TextColumns.Count <= column)
            TextColumns.Add(Label(x, y, "", TextFont));
        else
        {
            TextColumns[column].SetLocalPos(x, y);
        }

        TextColumns[column].Show();
        TextColumns[column].MultilineText = block.GetFormattedLines();
    }

    protected void SetTextColumns(Array<DebugTextBlock> text)
    {
        for (int i = 0; i < TextColumns.Count; i++)
            TextColumns[i].Hide();

        if (text == null || text.IsEmpty)
            return;
        float longestLine = 0;
        for (int i = 0; i < text.Count; i++)
        {
            DebugTextBlock lines = text[i];

            float header = TextFont.MeasureString(lines.Header ?? "").X;
            float body = lines.Lines.Max(l =>
            {
                var str = l.ToString();
                return TextFont.MeasureString(str).X;
            });
            ShowDebugGameInfo(i, lines, Rect.X + longestLine, Rect.Y + 250);
            longestLine += Math.Max(header, body) + 10; // (i > 0 ? 10 : 0);
        }
    }

    protected void SetTextCursor(float x, float y, Color color)
    {
        TextCursor = new(x, y);
        TextColor = color;
    }

    protected void DrawString(string text)
    {
        Parent.ScreenManager.SpriteBatch.DrawString(TextFont, text, TextCursor, TextColor);
        NewLine(text.Count(c => c == '\n') + 1);
    }

    protected void DrawString(float offsetX, string text)
    {
        TextCursor.X += offsetX;
        Parent.ScreenManager.SpriteBatch.DrawString(TextFont, text, TextCursor, TextColor);
        NewLine(text.Count(c => c == '\n') + 1);
    }

    protected void DrawString(Color color, string text)
    {
        Parent.ScreenManager.SpriteBatch.DrawString(TextFont, text, TextCursor, color);
        NewLine(text.Count(c => c == '\n') + 1);
    }

    protected void NewLine(int lines = 1)
    {
        int spacing = TextFont == Fonts.Arial12Bold ? TextFont.LineSpacing : TextFont.LineSpacing + 2;
        TextCursor.Y += spacing * lines;
    }
}