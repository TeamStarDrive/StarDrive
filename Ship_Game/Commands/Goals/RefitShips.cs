using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Ships;


namespace Ship_Game.Commands.Goals  // Created by Fat Bastard
{
    class RefitShips : Goal
    {
        public const string ID = "RefitShips";
        public override string UID => ID;

        public RefitShips() : base(GoalType.Refit)
        {
            Steps = new Func<GoalStep>[]
            {
                FindShipAndPlanetToRefit,
                WaitForOldShipAtPlanet,
                BuildNewShip,
                WaitMainGoalCompletion,
                AddShipDataAndFleet
            };
        }

        public RefitShips(Ship oldShip, Ship shipToBuild, Empire owner) : this()
        {
            OldShip     = oldShip;
            ShipLevel   = oldShip.Level;
            VanityName  = oldShip.VanityName;
            ShipToBuild = shipToBuild;
            Fleet       = oldShip.fleet;
            empire      = owner;
            Evaluate();
        }

        GoalStep FindShipAndPlanetToRefit()
        {
            if (ShipToBuild == null)
                return GoalStep.GoalFailed;  // No better ship is available

            PlanetBuildingAt = empire.RallyShipYardNearestTo(OldShip.Center);
            if (PlanetBuildingAt == null)
            {
                OldShip.AI.ClearOrders();
                return GoalStep.GoalFailed;  // No planet to refit the ship was found
            }

            OldShip.ClearFleet();
            OldShip.AI.OrderRefitTo(PlanetBuildingAt, ShipToBuild);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForOldShipAtPlanet()
        {
            if (!OldShipOnPlan)
                return GoalStep.GoalFailed;

            if (OldShip.Center.InRadius(PlanetBuildingAt.Center, PlanetBuildingAt.ObjectRadius + 150f))
                return GoalStep.GoToNextStep;

            return GoalStep.TryAgain;
        }

        GoalStep BuildNewShip()
        {
            if (!OldShipOnPlan)
                return GoalStep.GoalFailed;

            var qi  = new QueueItem(PlanetBuildingAt) {sData = ShipToBuild.shipData};
            qi.Cost = ShipToBuild.RefitCost(qi.sData.Name);
            qi.Goal = this;
            PlanetBuildingAt.ConstructionQueue.Add(qi);
            OldShip.QueueTotalRemoval();
            return GoalStep.GoToNextStep;
        }

        GoalStep AddShipDataAndFleet()
        {
            if (FinishedShip == null)
                return GoalStep.GoalFailed;

            FinishedShip.VanityName = VanityName;
            FinishedShip.Level      = ShipLevel;
            // not completed yet
            // add to fleet back here - something.blabla == Fleet; if not null
            return GoalStep.GoalComplete;
        }

        bool OldShipOnPlan
        {
            get
            {
                if (!OldShip.Active)
                    return false; // Ship was removed from game, probably destroyed

                if (OldShip.AI.State != AIState.Refit)
                    return false; // Someone gave this ship new orders, maybe the player

                return true;
            }
        }
    }
}
