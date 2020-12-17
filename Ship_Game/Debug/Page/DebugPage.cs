using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

namespace Ship_Game.Debug.Page
{
    public class DebugPage : UIElementContainer
    {
        GameScreen ParentScreen;
        public DebugModes Mode { get; }
        protected Array<UILabel> TextColumns = new Array<UILabel>();

        Vector2 TextCursor  = Vector2.Zero;
        Color TextColor     = Color.White;
        SpriteFont TextFont = Fonts.Arial12Bold;

        public DebugPage(GameScreen parent, DebugModes mode) : base(parent.Rect)
        {
            ParentScreen = parent;
            Mode = mode;
            Name = mode.ToString();
        }
        
        void ShowDebugGameInfo(int column, DebugTextBlock block, float x, float y)
        {
            if (TextColumns.Count <= column)
                TextColumns.Add(Label(x, y, "", TextFont));
            else
            {
                TextColumns[column].SetRelPos(x, y);
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
            TextCursor = new Vector2(x, y);
            TextColor = color;
        }

        protected void DrawString(string text)
        {
            var batch = Empire.Universe.ScreenManager.SpriteBatch;
            batch.DrawString(TextFont, text, TextCursor, TextColor);
            NewLine(text.Count(c => c == '\n') + 1);
        }

        protected void NewLine(int lines = 1)
        {
            TextCursor.Y += (TextFont == Fonts.Arial12Bold 
                        ? TextFont.LineSpacing 
                        : TextFont.LineSpacing + 2) * lines;
        }
    }
}