using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Ships
{
    public class MockShip : Ship
    {
        public int NumDieCalls; // TEST: # of times Die() has been called

        public MockShip(Ship template, Empire owner, Vector2 position) : base(template, owner, position)
        {
        }

        public override void Die(GameplayObject source, bool cleanupOnly)
        {
            ++NumDieCalls;
            base.Die(source, cleanupOnly);
        }
    }
}
