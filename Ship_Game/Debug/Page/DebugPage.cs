using System;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using SDUtils;
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

    protected TextDrawerComponent Text;

    public DebugPage(DebugInfoScreen parent, DebugModes mode) : base(parent.Rect)
    {
        Parent = parent;
        Mode = mode;
        Name = mode.ToString();
        Text = new(parent);
    }
        
    void ShowDebugGameInfo(int column, DebugTextBlock block, float x, float y)
    {
        if (TextColumns.Count <= column)
        {
            TextColumns.Add(Label(x, y, "", Text.Font));
        }
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

            float header = Text.Font.MeasureString(lines.Header ?? "").X;
            float body = lines.Lines.Max(l =>
            {
                var str = l.ToString();
                return Text.Font.MeasureString(str).X;
            });
            ShowDebugGameInfo(i, lines, Rect.X + longestLine, Rect.Y + 250);
            longestLine += Math.Max(header, body) + 10; // (i > 0 ? 10 : 0);
        }
    }
}