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
        public ModuleHeader Header;
        public ShipData Hull;

        public override bool HandleInput(InputState input)
        {
            return base.HandleInput(input);
        }

        public override void Draw(SpriteBatch batch)
        {
            base.Draw(batch);
            if (Header != null)
            {
                Header.Pos = Pos;
                Header.Draw(batch);
            }
            else if (Hull != null)
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
