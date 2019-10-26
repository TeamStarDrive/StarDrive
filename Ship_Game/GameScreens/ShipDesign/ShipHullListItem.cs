using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        }
    }
}
