using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game
{
    public class FighterListItem : ScrollListItem<FighterListItem>
    {
        public Ship Ship;

        public FighterListItem(Ship template)
        {
            Ship = template;
        }

        public override bool HandleInput(InputState input)
        {
            return base.HandleInput(input);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            var bCursor = new Vector2(List.X + 15, Y);

            batch.Draw(Ship.shipData.Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
            var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
            Color color = ShipBuilder.GetHangarTextColor(Ship.Name);
            batch.DrawString(Fonts.Arial12Bold, Ship.ShipName, tCursor, color);
        }
    }
}
