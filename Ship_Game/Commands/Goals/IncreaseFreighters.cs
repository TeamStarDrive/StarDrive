using System;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class IncreaseFreighters : BuildShipsGoalBase
    {
        public const string ID = "IncreaseFreighters";
        public override string UID => ID;

        public IncreaseFreighters() : base(GoalType.IncreaseFreighters)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitForShipBuilt,
                CompleteGoal
            };
        }

        public IncreaseFreighters(Empire empire) : this()
        {
            this.empire = empire;
        }

        GoalStep FindPlanetToBuildAt()
        {
            if (!GetFreighter(out ShipDesign freighter))
                return GoalStep.GoalFailed;

            if (!empire.FindPlanetToBuildShipAt(empire.SafeSpacePorts, freighter, out Planet planet, priority: 0.1f))
                return GoalStep.GoalFailed;

            planet.Construction.Enqueue(freighter, this, notifyOnEmpty: false);

            return GoalStep.GoToNextStep;
        }

        GoalStep CompleteGoal()
        {
            return GoalStep.GoalComplete;
        }
    }
}
