﻿using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Ship_Game
{
    struct TippedItem
    {
        public Rectangle Rect;
        public LocalizedText Tooltip;
        public TippedItem(in Rectangle rect, in LocalizedText tooltip)
        {
            Rect = rect;
            Tooltip = tooltip;
        }
    }
}
