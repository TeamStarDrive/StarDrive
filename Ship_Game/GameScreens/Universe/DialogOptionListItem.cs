using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Gameplay
{
    public class DialogOptionListItem : ScrollListItem<DialogOptionListItem>
    {
        public readonly DialogOption Option;
        public DialogOptionListItem(DialogOption option)
        {
            Option = option;
        }
        public override void Draw(SpriteBatch batch)
        {
            Color color = (Hovered ? Color.White : new Color(255, 255, 255, 220));
            batch.DrawDropShadowText(Option.Number+". "+Option.Words, Pos, Fonts.Consolas18, color);
        }
    }
}
