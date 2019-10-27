using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class TextListItem : ScrollList<TextListItem>.Entry
    {
        public UILabel TextLabel;
        public string Text => TextLabel.Text;

        public TextListItem(string text, SpriteFont font)
        {
            TextLabel = new UILabel(text, font);
        }

        public override void PerformLayout()
        {
            TextLabel.Rect = Rect;
            base.PerformLayout();
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
        }

        public override void Draw(SpriteBatch batch)
        {
            TextLabel.Draw(batch);
        }
    }
}
