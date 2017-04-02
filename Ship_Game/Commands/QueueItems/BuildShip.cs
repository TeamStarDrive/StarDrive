using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.AI;
using Ship_Game.Gameplay;

namespace Ship_Game.Commands
{
    class BuildShip:QueueItem
    {
        public BuildShip(ArtificialIntelligence.ShipGoal goal )
        {
            isShip = true;
            productionTowards = 0f;
            sData = ResourceManager.ShipsDict[goal.VariableString].GetShipData();
        }
    }
}
