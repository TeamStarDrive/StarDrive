using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Debug.Page
{
    internal class SolarDebug : DebugPage
    {
        SolarSystem System;
        readonly UniverseScreen Screen;
        public SolarDebug(UniverseScreen screen, DebugInfoScreen parent) : base(parent, DebugModes.Trade)
        {
            Screen = screen;
        }
        
        public override void Update(float fixedDeltaTime)
        {
            foreach (SolarSystem system in Empire.Universe.SolarSystemDict.Values)
            {
                if (system.isVisible)
                {
                    System = system;
                    break;
                }
            }
            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch spriteBatch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            base.Draw(spriteBatch, elapsed);
        }
    }
}
