using System;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class TextListItem : ScrollList<TextListItem>.Entry
    {
        public UILabel TextLabel;
        public string Text => TextLabel.Text;

        public TextListItem(string text, SpriteFont font)
        {
            TextLabel = Add(new UILabel(text, font));
        }

        public override void PerformLayout()
        {
            base.PerformLayout();
        }
    }
}
