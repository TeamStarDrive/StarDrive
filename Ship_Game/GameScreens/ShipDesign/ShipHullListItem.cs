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
    public class ShipHullListItem : ScrollList<ShipHullListItem>.Entry
    {
        public ShipData Hull;
        public ShipHullListItem(string headerText) : base(headerText) {}
        public ShipHullListItem(ShipData hull) { Hull = hull; }

        public override void Draw(SpriteBatch batch)
        {
            base.Draw(batch);
            if (Hull != null)
            {
                batch.Draw(Hull.Icon, new Rectangle((int)X, (int)Y, 29, 30), Color.White);

                var tCursor = new Vector2(X + 40f, Y + 3f);
                batch.DrawString(Fonts.Arial12Bold, Hull.Name, tCursor, Color.White);

                tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                batch.DrawString(Fonts.Arial8Bold, Localizer.GetRole(Hull.HullRole, EmpireManager.Player), tCursor, Color.Orange);
            }
        }
    }
}
