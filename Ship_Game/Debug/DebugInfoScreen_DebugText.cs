using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Debug
{
    public sealed partial class DebugInfoScreen
    {
        Vector2 TextCursor = Vector2.Zero;
        Color TextColor = Color.White;
        SpriteFont TextFont = Fonts.Arial12Bold;
        Array<UILabel> DebugText;

        void HideAllDebugGameInfo()
        {
            if (DebugText == null) return;
            for (int i = 0; i < DebugText.Count; i++)
            {
                var column = DebugText[i];
                column.Hide();
            }
        }

        void ShowDebugGameInfo(int column, Array<string> lines, float x, float y)
        {
            if (DebugText == null)
                DebugText = new Array<UILabel>();

            if (DebugText.Count <= column)
                DebugText.Add(Label(x, y, ""));


            DebugText[column].Show();
            DebugText[column].MultilineText = lines;
        }

        protected void SetTextCursor(float x, float y, Color color)
        {
            TextCursor = new Vector2(x, y);
            TextColor = color;
        }

        protected void DrawString(string text)
        {
            ScreenManager.SpriteBatch.DrawString(TextFont, text, TextCursor, TextColor);
            NewLine(text.Count(c => c == '\n') + 1);
        }

        protected void DrawString(float offsetX, string text)
        {
            Vector2 pos = TextCursor;
            pos.X += offsetX;
            ScreenManager.SpriteBatch.DrawString(TextFont, text, pos, TextColor);
            NewLine(text.Count(c => c == '\n') + 1);
        }

        protected void DrawString(Color color, string text)
        {
            ScreenManager.SpriteBatch.DrawString(TextFont, text, TextCursor, color);
            NewLine(text.Count(c => c == '\n') + 1);
        }

        void NewLine(int lines = 1)
        {
            TextCursor.Y += (TextFont == Fonts.Arial12Bold 
                        ? TextFont.LineSpacing 
                        : TextFont.LineSpacing + 2) * lines;
        }


        
        public bool DebugLogText(string text, DebugModes mode)
        {
            if (IsOpen && (mode == DebugModes.Last || Mode == mode) && GlobalStats.VerboseLogging)
            {
                Log.Info(text);
                return true;
            }
            return false;
        }
    }
}