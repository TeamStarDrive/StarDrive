using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class WikiHelpCategoryListItem : ScrollList<WikiHelpCategoryListItem>.Entry
    {
        public ModuleHeader Header;
        public HelpTopic Topic;

        public WikiHelpCategoryListItem()
        {
        }

        public override void Draw(SpriteBatch batch)
        {
            if (Header != null)
            {
                Header.Pos = new Vector2(X + 35, Y);
                Header.Draw(batch);
            }
            else if (Topic != null)
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
