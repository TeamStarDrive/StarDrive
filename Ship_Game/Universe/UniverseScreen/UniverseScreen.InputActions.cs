using System;
using Ship_Game.AI;

namespace Ship_Game {
    public sealed partial class UniverseScreen
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
            if (this.SelectedShip == null)
                return;
            this.SelectedShip.AI.HoldPosition();
        }

        public void DoExplore(object sender)
        {
            if (this.SelectedShip == null)
                return;
            this.SelectedShip.AI.OrderExplore();
        }

        public void DoTransport(object sender)
        {
            if (this.SelectedShip == null)
                return;
            this.SelectedShip.AI.OrderTransportPassengers(5f);
        }

        public void DoDefense(object sender)
        {
            if (this.SelectedShip == null || this.player.GetGSAI()
                    .DefensiveCoordinator.DefensiveForcePool.Contains(this.SelectedShip))
                return;
            this.player.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Add(this.SelectedShip);
            this.SelectedShip.AI.OrderQueue.Clear();
            this.SelectedShip.AI.HasPriorityOrder = false;
            this.SelectedShip.AI.SystemToDefend = (SolarSystem) null;
            this.SelectedShip.AI.SystemToDefendGuid = Guid.Empty;
            this.SelectedShip.AI.State = AIState.SystemDefender;
        }

        public void DoTransportGoods(object sender)
        {
            if (this.SelectedShip == null)
                return;
            this.SelectedShip.AI.State = AIState.SystemTrader;
            this.SelectedShip.AI.start = null;
            this.SelectedShip.AI.end = null;
            this.SelectedShip.AI.OrderTrade(5f);
        }

        private void MarkForColonization(object sender)
        {
            player.GetGSAI().Goals.Add(new Goal(SelectedPlanet, player));
        }
    }
}