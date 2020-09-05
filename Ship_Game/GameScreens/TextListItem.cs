using System;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class TextListItem : ScrollListItem<TextListItem>
    {
        public UILabel TextLabel;
        public string Text => TextLabel.Text.Text;

        public TextListItem(string text, SpriteFont font)
        {
            TextLabel = new UILabel(text, font);
        }

        public override void PerformLayout()
        {
            TextLabel.Pos = Pos;
            RequiresLayout = false;
        }

        // custom override, because it's faster
        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            TextLabel.Draw(batch, elapsed);
        }
    }
}
