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
    public class ShipHullListItem : ScrollListItem<ShipHullListItem>
    {
        public ShipData Hull;
        public ShipHullListItem(string headerText) : base(headerText) {}
        public ShipHullListItem(ShipData hull) { Hull = hull; }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
            if (Hull != null)
            {
                int h = (int)Height;
                batch.Draw(Hull.Icon, new Rectangle((int)X - 2, (int)Y - 2, h+4, h+4), Color.White);

                batch.DrawString(Fonts.Arial12Bold, Hull.Name, X + h + 6, Y + 2);

                string role = Localizer.GetRole(Hull.HullRole, EmpireManager.Player);
                batch.DrawString(Fonts.Arial8Bold, role, X + h + 8, Y + 16, Color.Orange);
            }
        }
    }
}
