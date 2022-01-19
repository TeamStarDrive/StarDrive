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
            if (SelectedShip == null || Player.GetEmpireAI().DefensiveCoordinator.DefensiveForcePool.Contains(SelectedShip))
                return;
            Player.GetEmpireAI().DefensiveCoordinator.DefensiveForcePool.Add(SelectedShip);
            SelectedShip.AI.ClearOrders(AIState.SystemDefender);
        }

        private void MarkForColonization()
        {
            Player.GetEmpireAI().Goals.Add(new MarkForColonization(SelectedPlanet, Player));
        }
    }
}