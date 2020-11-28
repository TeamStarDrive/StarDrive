using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class WikiHelpCategoryListItem : ScrollListItem<WikiHelpCategoryListItem>
    {
        public HelpTopic Topic;
        public WikiHelpCategoryListItem(string headerText) : base(headerText) { }
        public WikiHelpCategoryListItem(HelpTopic topic) { Topic = topic; }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
            if (Topic != null)
            {
                Vector2 cursor = Pos;
                cursor.X += 15f;
                batch.DrawString(Fonts.Arial12Bold,
                    Topic.Title, cursor, (Hovered ? Color.Orange : Color.White));

                cursor.Y += Fonts.Arial12Bold.LineSpacing;
                batch.DrawString(Fonts.Arial12,
                    Topic.ShortDescription, cursor, (Hovered ? Color.White : Color.Orange));
            }
        }
    }
}
