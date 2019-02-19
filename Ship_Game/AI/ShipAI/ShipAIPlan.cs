using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Ships;

namespace Ship_Game.AI
{
    internal abstract class ShipAIPlan
    {
        public ShipAI AI;
        public readonly Ship Owner;

        protected ShipAIPlan(ShipAI ai)
        {
            AI = ai;
            Owner = ai.Owner;
        }

        public abstract void Execute(float elapsedTime, ShipAI.ShipGoal g);
    }
}
