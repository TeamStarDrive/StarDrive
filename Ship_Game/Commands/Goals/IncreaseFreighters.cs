using System;
using System.Linq;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class IncreaseFreighters : Goal
    {
        public const string ID = "IncreaseFreighters";
        public override string UID => ID;

        public IncreaseFreighters() : base(GoalType.BuildShips)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitMainGoalCompletion,
                ReportGoalCompleteToEmpire
            };
        }
        public IncreaseFreighters(Empire empire) : this()
        {
            this.empire = empire;
        }

        GoalStep FindPlanetToBuildAt()
        {
            Ship freighter = ShipBuilder.PickFreighter(empire);
            if (freighter == null)
                return GoalStep.GoalFailed;

            if (!empire.FindPlanetToBuildAt(empire.SafeSpacePorts, freighter, out Planet planet))
                return GoalStep.TryAgain;

            planet.Construction.AddShip(freighter, this, notifyOnEmpty: false);
            return GoalStep.GoToNextStep;
        }

        GoalStep ReportGoalCompleteToEmpire()
        {
            empire.ReportGoalComplete(this);
            return GoalStep.GoalComplete;
        }
    }
}
