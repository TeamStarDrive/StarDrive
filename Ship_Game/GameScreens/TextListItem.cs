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
        public string Text;
        public SpriteFont Font = Fonts.Consolas18;

        public TextListItem(string text)
        {
            Text = text;
        }

        public TextListItem(string text, SpriteFont font)
        {
            Text = text;
            Font = font;
        }

        public override void Draw(SpriteBatch batch)
        {
            var pos = new Vector2(X, Y - 33);
            batch.DrawDropShadowText(Text, pos, Font, Color.White, shadowOffset:2f);
        }
    }
}
