using System;
using System.Linq;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class BuildOffensiveShips : Goal
    {
        public const string ID = "BuildOffensiveShips";
        public override string UID => ID;

        public BuildOffensiveShips() : base(GoalType.BuildOffensiveShips)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                MainGoalKeepRushingProductionOfOurShip,
                OrderShipToAwaitOrders
            };
        }

        public BuildOffensiveShips(string shipType, Empire e) : this()
        {
            ToBuildUID = shipType;
            empire = e;
            Evaluate();
        }

        GoalStep FindPlanetToBuildAt()
        {
            if (!ResourceManager.GetShipTemplate(ToBuildUID, out Ship template))
                return GoalStep.GoalFailed;

            if (!empire.TryFindSpaceportToBuildShipAt(template, out Planet planet))
                return GoalStep.TryAgain;

            planet.Construction.AddShip(template, this);
            return GoalStep.GoToNextStep;
        }

        GoalStep MainGoalKeepRushingProductionOfOurShip()
        {
            if (MainGoalCompleted) // @todo Refactor this messy logic
                return GoalStep.GoToNextStep;

            if (PlanetBuildingAt == null || PlanetBuildingAt.NotConstructing)
                return GoalStep.TryAgain;

            if (PlanetBuildingAt.ConstructionQueue[0].Goal == this)
            {
                if (PlanetBuildingAt.Storage.ProdRatio > 0.5f)
                    PlanetBuildingAt.Construction.RushProduction(0);
            }
            return GoalStep.TryAgain;
        }

        GoalStep OrderShipToAwaitOrders()
        {
            if (FinishedShip == null)
            {
                Log.Warning($"BeingBuilt was null in {type} completion");
                return GoalStep.GoalFailed;
            }

            FinishedShip.AI.State = AIState.AwaitingOrders;
            return GoalStep.GoalComplete;
        }
    }
}
