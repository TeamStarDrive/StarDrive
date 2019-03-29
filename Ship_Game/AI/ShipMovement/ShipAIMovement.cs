using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Ships;

namespace Ship_Game.AI.ShipMovement
{
    internal class ShipAIMovement
    {
        public ShipAI AI;
        public readonly Ship Owner;

        protected ShipAIMovement(ShipAI ai)
        {
            AI = ai;
            Owner = ai.Owner;
        }        
    }
}
