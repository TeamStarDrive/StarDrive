using System;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class IncreaseFreighters : BuildShipsGoalBase
    {
        [StarDataConstructor]
        public IncreaseFreighters(Empire owner) : base(GoalType.IncreaseFreighters, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitForShipBuilt,
                CompleteGoal
            };
        }

        GoalStep FindPlanetToBuildAt()
        {
            if (!GetFreighter(out IShipDesign freighter))
                return GoalStep.GoalFailed;

            if (!Owner.FindPlanetToBuildShipAt(Owner.SafeSpacePorts, freighter, out Planet planet, priority: 0.1f))
                return GoalStep.GoalFailed;

            planet.Construction.Enqueue(freighter, this, notifyOnEmpty: false);
            if (Owner.TotalFreighters < Owner.GetPlanets().Count)
                planet.Construction.PrioritizeShip(freighter, 1);

            return GoalStep.GoToNextStep;
        }

        GoalStep CompleteGoal()
        {
            return GoalStep.GoalComplete;
        }
    }
}
