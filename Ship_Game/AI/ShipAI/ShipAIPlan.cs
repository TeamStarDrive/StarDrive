using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Ships;

namespace Ship_Game.AI
{
    internal abstract class ShipAIPlan : IDisposable
    {
        public ShipAI AI;
        public Ship Owner;

        protected ShipAIPlan(ShipAI ai)
        {
            AI = ai;
            Owner = ai.Owner;
        }

        public abstract void Execute(FixedSimTime timeStep, ShipAI.ShipGoal g);

        public void Dispose()
        {
            AI = null;
            Owner = null;
        }
    }
}
