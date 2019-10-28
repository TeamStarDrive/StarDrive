using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;

namespace Ship_Game
{
    public class ShipDesignListItem : ScrollListEntry<ShipDesignListItem>
    {
        public Ship Ship;
        public ShipDesignListItem(Ship template)
        {
            Ship = template;
        }

        public override void Draw(SpriteBatch batch)
        {
            var bCursor = new Vector2(List.X + 20, List.Y + 20);
            batch.Draw(Ship.shipData.Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
            
            var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
            batch.DrawString(Fonts.Arial12Bold, Ship.Name, tCursor, Color.White);
            tCursor.Y += Fonts.Arial12Bold.LineSpacing;

            batch.DrawString(Fonts.Arial8Bold, Ship.shipData.GetRole(), tCursor, Color.Orange);

            base.Draw(batch);
        }
    }
}
