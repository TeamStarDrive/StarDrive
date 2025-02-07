﻿using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals  // Created by Fat Bastard
{
    [StarDataType]
    class ScrapShip : Goal
    {
        [StarData] public sealed override Ship OldShip { get; set; }
        [StarData] public sealed override Planet PlanetBuildingAt { get; set; }

        [StarDataConstructor]
        public ScrapShip(Empire owner) : base(GoalType.ScrapShip, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToScrapAndOrderScrap,
                WaitForOldShipAtPlanet,
                ScrapTheShip,
                ImmediateScuttleSelfDestruct
            };
        }

        public ScrapShip(Ship shipToScrap, Empire owner, bool immediateScuttle) : this(owner)
        {
            OldShip = shipToScrap;
            // This will remove an older scrap goal if the ship is already in scrap state.
            if (OldShip.AI.State == AIState.Scrap)
                RemoveOldScrapGoal();

            if (immediateScuttle)
                ChangeToStep(ImmediateScuttleSelfDestruct);
        }

        GoalStep FindPlanetToScrapAndOrderScrap()
        {
            if (OldShip == null || !OldShip.CanBeScrapped) 
                return GoalStep.GoalFailed;

            OldShip.RemoveFromPoolAndFleet(clearOrders: false);
            if (OldShip.ShipData.Role <= RoleName.station && OldShip.ScuttleTimer < 0
                || !Owner.FindPlanetToScrapIn(OldShip, out Planet buildAt))
            {
                // No planet to refit, scuttling ship
                return ImmediateScuttleSelfDestruct();
            }

            PlanetBuildingAt = buildAt;
            OldShip.AI.IgnoreCombat = true;
            OldShip.AI.OrderMoveAndScrap(buildAt);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForOldShipAtPlanet()
        {
            if (!OldShipOnPlan)
                return GoalStep.GoalFailed;

            if (!PlanetBuildingAt.Safe)
            {
                OldShip.AI.ClearOrders();
                if (!Owner.FindPlanetToScrapIn(OldShip, out Planet buildAt))
                {
                    return GoalStep.GoalFailed;
                }
                else
                {
                    PlanetBuildingAt = buildAt;
                    OldShip.AI.IgnoreCombat = true;
                    OldShip.AI.OrderMoveAndScrap(buildAt);
                }
            }


            if (OldShip.Position.InRadius(PlanetBuildingAt.Position, PlanetBuildingAt.Radius + 300f))
                return GoalStep.GoToNextStep;

            return GoalStep.TryAgain;
        }

        GoalStep ScrapTheShip()
        {
            if (!OldShipOnPlan)
                return GoalStep.GoalFailed;

            Owner.RefundCreditsPostRemoval(OldShip);
            PlanetBuildingAt.ProdHere += OldShip.GetScrapCost();
            Owner.TryUnlockByScrap(OldShip);
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
                OldShip.Loyalty.AI.FindAndRemoveGoal(GoalType.ScrapShip, g => g.OldShip == OldShip);
        }

        GoalStep ImmediateScuttleSelfDestruct()
        {
            // Possible Hack. The ship should not be able to go null here. 
            // the error message was a null ref here "OldShip.ScuttleTimer = 1;" which indicates that the OldShip was null.
            // there may be a deeper problem.

            if (OldShip?.Active == true)
            {
                if (OldShip.IsResearchStation)
                {
                    Planet planet = OldShip.GetTether();
                    if (planet != null)
                        Owner.Universe.RemoveEmpireFromResearchableList(Owner, planet);
                    else if (OldShip.System != null)
                        Owner.Universe.RemoveEmpireFromResearchableList(Owner, OldShip.System);
                    else
                        Log.Error($"Scrap Ship - research station {OldShip.Name} System was null!");
                }

                OldShip.ScuttleTimer = 1;
                OldShip.AI.ClearOrders(AIState.Scuttle, priority: true);
                OldShip.QueueTotalRemoval(); // fbedard
            }
            return GoalStep.GoalComplete;
        }
    }
}
