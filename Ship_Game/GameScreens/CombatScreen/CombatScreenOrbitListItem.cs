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
            if (Hovered)
            {
                nameColor  = Color.Gold;
                statsColor = Color.Orange;
            }
            
            var bCursor = new Vector2(X + 25, Y);
            batch.Draw(Troop.TextureDefault, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);

            var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
            batch.DrawString(Fonts.Arial12Bold, Troop.Name, tCursor, nameColor);

            tCursor.Y += Fonts.Arial12Bold.LineSpacing;
            batch.DrawString(Fonts.Arial8Bold, Troop.StrengthText + ", Level: " + Troop.Level, tCursor, statsColor);
        }
    }
}
