namespace Ship_Game.Debug.Page
{
    public class DebugPage : UIElementContainer
    {
        public DebugPage(GameScreen parent, DebugModes mode) : base(parent, parent.Rect)
        {
            DebugMode = mode;
        }

        protected Array<UILabel> DebugText = new Array<UILabel>();
        
        public void HideAllDebugText()
        {
            for (int i = 0; i < DebugText.Count; i++)
            {
                DebugText[i].Hide();
            }
        }

        public DebugModes DebugMode { get; }

        public void ShowDebugGameInfo(int column, DebugTextBlock lines, float x, float y)
        {
            if (DebugText.Count <= column)
                DebugText.Add(Label(x, y, ""));

            DebugText[column].Show();
            DebugText[column].MultilineText = lines.GetFormattedLines();

        }

        protected void SetTextColumns(Array<DebugTextBlock> text)
        {
            if (text == null || text.IsEmpty)
                return;
            for (int i = 0; i < text.Count; i++)
            {
                DebugTextBlock lines = text[i];
                ShowDebugGameInfo(i, lines, Rect.X + 10 + 300 * i, Rect.Y + 250);
            }
        }
    }
}