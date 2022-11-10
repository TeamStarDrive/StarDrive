using System;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class IncreaseFreighters : BuildShipsGoalBase
    {
        [StarDataConstructor]
        public IncreaseFreighters() : base(GoalType.IncreaseFreighters, null)
        {
            InitSteps();
        }

        public IncreaseFreighters(Empire owner) : base(GoalType.IncreaseFreighters, owner)
        {
            InitSteps();
            Build = new(BuildableShip.GetFreighter(owner));
        }

        void InitSteps()
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitForShipBuilt
            };
        }

        GoalStep FindPlanetToBuildAt()
        {
            if (!Owner.FindPlanetToBuildShipAt(Owner.SafeSpacePorts, Build.Template, out Planet planet, priority: 0.1f))
                return GoalStep.GoalFailed;

            PlanetBuildingAt = planet;
            planet.Construction.Enqueue(Build.Template, this, notifyOnEmpty: false);
            int priority = Owner.TotalFreighters < Owner.GetPlanets().Count ? 1 : Owner.GetPlanets().Count / 3;
            planet.Construction.PrioritizeShip(Build.Template, priority, priority * 2);

            return GoalStep.GoToNextStep;
        }
    }
}
