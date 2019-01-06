using System;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        public void OrderScrap(object sender)
        {
            SelectedShip.AI.OrderScrapShip();
        }

        public void OrderScuttle(object sender)
        {
            if (SelectedShip != null)
                SelectedShip.ScuttleTimer = 10f;
        }

        public void DoHoldPosition(object sender)
        {
            SelectedShip?.AI.HoldPosition();
        }

        public void DoExplore(object sender)
        {
            SelectedShip?.AI.OrderExplore();
        }

        public void DoTransport(object sender)
        {
            SelectedShip?.AI.OrderTransportPassengers(5f);
        }

        public void DoDefense(object sender)
        {
            if (SelectedShip == null || player.GetEmpireAI().DefensiveCoordinator.DefensiveForcePool.Contains(SelectedShip))
                return;
            player.GetEmpireAI().DefensiveCoordinator.DefensiveForcePool.Add(SelectedShip);
            SelectedShip.AI.OrderQueue.Clear();
            SelectedShip.AI.HasPriorityOrder = false;
            SelectedShip.AI.SystemToDefend = null;
            SelectedShip.AI.SystemToDefendGuid = Guid.Empty;
            SelectedShip.AI.State = AIState.SystemDefender;
        }

        public void DoTransportGoods(object sender)
        {
            if (SelectedShip == null)
                return;
            SelectedShip.AI.State = AIState.SystemTrader;
            SelectedShip.AI.start = null;
            SelectedShip.AI.end = null;
            SelectedShip.AI.OrderTrade(5f);
        }

        private void MarkForColonization(object sender)
        {
            player.GetEmpireAI().Goals.Add(new MarkForColonization(SelectedPlanet, player));
        }
    }
}