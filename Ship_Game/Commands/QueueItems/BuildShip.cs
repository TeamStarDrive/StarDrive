using Ship_Game.AI;

namespace Ship_Game.Commands
{
    class BuildShip : QueueItem
    {
        public BuildShip(ShipAI.ShipGoal goal, Planet planet) : base(planet)
        {
            isShip = true;
            productionTowards = 0f;
            sData = ResourceManager.ShipsDict[goal.VariableString].shipData;
        }
    }
}
