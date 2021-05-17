using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Ships;


namespace Ship_Game.Commands.Goals  // Created by Fat Bastard
{
    class ScrapShip : Goal
    {
        public const string ID = "ScrapShip";
        public override string UID => ID;

        public ScrapShip() : base(GoalType.ScrapShip)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToScrapAndOrderScrap,
                WaitForOldShipAtPlanet,
                ScrapTheShip
            };
        }

        public ScrapShip(Ship shipToScrap, Empire owner) : this()
        {
            OldShip = shipToScrap;
            empire  = owner;

            Evaluate();
        }

        GoalStep FindPlanetToScrapAndOrderScrap()
        {
            if (OldShip == null) return GoalStep.GoalFailed;
            if (OldShip.AI.State == AIState.Scrap)
                RemoveOldScrapGoal(); // todo test this

            if (!OldShip.CanBeScrapped)
                return GoalStep.GoalFailed;

            empire.EmpireShipLists.RemoveShipFromFleetAndPools(OldShip);
            if (OldShip.shipData.Role <= ShipData.RoleName.station && OldShip.ScuttleTimer < 0
                || !empire.FindPlanetToScrapIn(OldShip, out PlanetBuildingAt))
            {
                ScuttleShip();
                return GoalStep.GoalFailed;  // No planet to refit, scuttling ship
            }

            OldShip.AI.IgnoreCombat = true;
            OldShip.AI.OrderMoveAndScrap(PlanetBuildingAt);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForOldShipAtPlanet()
        {
            if (!OldShipOnPlan)
                return GoalStep.GoalFailed;

            if (OldShip.Center.InRadius(PlanetBuildingAt.Center, PlanetBuildingAt.ObjectRadius + 300f))
                return GoalStep.GoToNextStep;

            return GoalStep.TryAgain;
        }

        GoalStep ScrapTheShip()
        {
            if (!OldShipOnPlan)
                return GoalStep.GoalFailed;

            empire.RefundCreditsPostRemoval(OldShip);
            PlanetBuildingAt.ProdHere += OldShip.GetScrapCost();
            empire.TryUnlockByScrap(OldShip);
            OldShip.QueueTotalRemoval();
            empire.GetEmpireAI().Recyclepool++;
            return GoalStep.GoalComplete;
        }

        bool OldShipOnPlan
        {
            get
            {
                if (OldShip == null || !OldShip.Active)
                    return false; // Ship was removed from game, probably destroyed

                return OldShip.AI.State == AIState.Scrap;
            }
        }

        void RemoveOldScrapGoal()
        {
            if (OldShip.AI.FindGoal(ShipAI.Plan.Scrap, out _))
                OldShip.loyalty.GetEmpireAI().FindAndRemoveGoal(GoalType.ScrapShip, g => g.OldShip == OldShip);
        }

        void ScuttleShip()
        {
            OldShip.ScuttleTimer = 1;
            OldShip.AI.ClearOrders(AIState.Scuttle, priority: true);
            OldShip.QueueTotalRemoval(); // fbedard
        }
    }
}
