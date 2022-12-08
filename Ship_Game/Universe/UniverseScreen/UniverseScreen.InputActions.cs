using System;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        public void OrderScrap()
        {
            SelectedShip.AI.OrderScrapShip();
        }

        public void OrderScuttle()
        {
            if (SelectedShip != null)
                SelectedShip.ScuttleTimer = 10f;
        }

        public void DoExplore()
        {
            SelectedShip?.AI.OrderExplore();
        }

        public void DoDefense()
        {
            if (SelectedShip == null || Player.AI.DefensiveCoordinator.DefensiveForcePool.Contains(SelectedShip))
                return;
            Player.AI.DefensiveCoordinator.DefensiveForcePool.Add(SelectedShip);
            SelectedShip.AI.ClearOrders(AIState.SystemDefender);
        }

        private void MarkForColonization()
        {
            Player.AI.AddGoalAndEvaluate(new MarkForColonization(SelectedPlanet, Player, isManual:true));
        }
    }
}