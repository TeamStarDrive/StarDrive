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

        public void DoHoldPosition()
        {
            SelectedShip?.AI.HoldPosition();
        }

        public void DoExplore()
        {
            SelectedShip?.AI.OrderExplore();
        }

        public void DoDefense()
        {
            if (SelectedShip == null || player.GetEmpireAI().DefensiveCoordinator.DefensiveForcePool.Contains(SelectedShip))
                return;
            player.GetEmpireAI().DefensiveCoordinator.DefensiveForcePool.Add(SelectedShip);
            SelectedShip.AI.ClearOrders(AIState.SystemDefender);
            SelectedShip.AI.SystemToDefend = null;
            SelectedShip.AI.SystemToDefendGuid = Guid.Empty;
        }

        public void DoTransportGoods() // @todo FB - check this
        {
            if (SelectedShip == null)
                return;
            SelectedShip.AI.State = AIState.SystemTrader;
        }

        private void MarkForColonization()
        {
            player.GetEmpireAI().Goals.Add(new MarkForColonization(SelectedPlanet, player));
        }
    }
}