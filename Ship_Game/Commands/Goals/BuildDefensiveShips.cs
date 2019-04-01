using System;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class BuildDefensiveShips : BuildShipsGoalBase
    {
        public const string ID = "BuildDefensiveShips";
        public override string UID => ID;

        public BuildDefensiveShips() : base(GoalType.BuildDefensiveShips)
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
            return FindPlanetToBuildAt(SpacePortType.Any);
        }

        GoalStep OrderBuiltShipToDefend()
        {
            FinishedShip.DoDefense();
            return GoalStep.GoalComplete;
        }
    }
}
