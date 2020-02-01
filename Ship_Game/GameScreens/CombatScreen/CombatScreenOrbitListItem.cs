using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public class CombatScreenOrbitListItem : ScrollListItem<CombatScreenOrbitListItem>
    {
        public Troop Troop;

        public CombatScreenOrbitListItem(Troop troop)
        {
            Troop = troop;
        }

        public override void Draw(SpriteBatch batch)
        {
            Color nameColor = Color.LightGray;
            Color statsColor = nameColor;
            Color backColor = Color.Black;
            if (Hovered)
            {
                nameColor  = Color.Gold;
                statsColor = Color.Orange;
                backColor = Color.Black.AddRgb(0.2f);
            }
            
            batch.FillRectangle(Rect, backColor.Alpha(0.3f));
            Troop.Draw(batch, new RectF(X + 2, Y, Height, Height));
            batch.DrawString(Fonts.Arial12Bold, Troop.Name, X + 40, Y + 2, nameColor);
            batch.DrawString(Fonts.Arial8Bold, $"{Troop.StrengthText}, Level: {Troop.Level}", X + 40, Y + 14, statsColor);
        }
    }
}
