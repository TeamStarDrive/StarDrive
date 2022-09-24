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

            planet.Construction.Enqueue(Build.Template, this, notifyOnEmpty: false);
            if (Owner.TotalFreighters < Owner.GetPlanets().Count)
                planet.Construction.PrioritizeShip(Build.Template, 1);

            return GoalStep.GoalComplete;
        }
    }
}
