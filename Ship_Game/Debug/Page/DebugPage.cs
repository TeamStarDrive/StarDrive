namespace Ship_Game.Debug.Page
{
    public class DebugPage : UIElementContainer
    {
        public DebugModes Mode { get; }
        protected Array<UILabel> TextColumns = new Array<UILabel>();

        public DebugPage(GameScreen parent, DebugModes mode) : base(parent, parent.Rect)
        {
            Mode = mode;
        }
        
        void ShowDebugGameInfo(int column, DebugTextBlock block, float x, float y)
        {
            if (TextColumns.Count <= column)
                TextColumns.Add(Label(x, y, ""));

            TextColumns[column].Show();
            TextColumns[column].MultilineText = block.GetFormattedLines();
        }

        protected void SetTextColumns(Array<DebugTextBlock> text)
        {
            for (int i = 0; i < TextColumns.Count; i++)
                TextColumns[i].Hide();

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