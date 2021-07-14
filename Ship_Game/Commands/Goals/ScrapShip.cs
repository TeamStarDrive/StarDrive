﻿using System;
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
                ScrapTheShip,
                ImmediateScuttleSelfDestruct
            };
        }

        public ScrapShip(Ship shipToScrap, Empire owner, bool immediateScuttle) : this()
        {
            OldShip = shipToScrap;
            empire  = owner;
            if (immediateScuttle)
                ChangeToStep(ImmediateScuttleSelfDestruct);
            Evaluate();
        }

        GoalStep FindPlanetToScrapAndOrderScrap()
        {
            if (OldShip == null) return GoalStep.GoalFailed;
            if (OldShip.AI.State == AIState.Scrap)
                RemoveOldScrapGoal(); // todo test this

            if (!OldShip.CanBeScrapped)
                return GoalStep.GoalFailed;

            OldShip.RemoveFromPoolAndFleet(clearOrders: false);

            if (OldShip.shipData.Role <= ShipData.RoleName.station && OldShip.ScuttleTimer < 0
                || !empire.FindPlanetToScrapIn(OldShip, out PlanetBuildingAt))
            {
                // No planet to refit, scuttling ship
                return ImmediateScuttleSelfDestruct();
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

        GoalStep ImmediateScuttleSelfDestruct()
        {
            OldShip.ScuttleTimer = 1;
            OldShip.AI.ClearOrders(AIState.Scuttle, priority: true);
            OldShip.QueueTotalRemoval(); // fbedard
            return GoalStep.GoalComplete;
        }
    }
}
