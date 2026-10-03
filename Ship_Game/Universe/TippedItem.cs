using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    struct TippedItem
    {
        public Rectangle Rect;
        public LocalizedText Tooltip;
        public string CodexUid;
        public TippedItem(in Rectangle rect, in LocalizedText tooltip, string codexUid = null)
        {
            Rect = rect;
            Tooltip = tooltip;
            CodexUid = codexUid;
        }
    }
}
