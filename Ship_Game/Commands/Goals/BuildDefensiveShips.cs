using System;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class BuildDefensiveShips : Goal
    {
        public const string ID = "BuildDefensiveShips";
        public override string UID => ID;

        public BuildDefensiveShips() : base(GoalType.BuildShips)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildDefensiveShipsAt,
                WaitMainGoalCompletion,
                OrderBuiltShipToDefend
            };
        }

        GoalStep FindPlanetToBuildDefensiveShipsAt()
        {
            if (!ResourceManager.GetShipTemplate(ToBuildUID, out Ship template))
                return GoalStep.GoalFailed;

            if (!empire.TryFindSpaceportToBuildShipAt(template, out Planet spacePort))
                return GoalStep.TryAgain;

            spacePort.Construction.AddShip(template, this);
            PlanetBuildingAt = spacePort;
            return GoalStep.GoToNextStep;
        }

        GoalStep OrderBuiltShipToDefend()
        {
            FinishedShip.DoDefense();
            empire.ForcePoolAdd(FinishedShip);
            return GoalStep.GoalComplete;
        }

    }
}
