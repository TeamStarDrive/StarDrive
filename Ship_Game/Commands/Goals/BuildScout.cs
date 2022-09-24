using System;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class BuildScout : Goal
    {
        [StarData] BuildableShip Build;
        public override IShipDesign ToBuild => Build.Template;

        [StarDataConstructor] BuildScout() : base(GoalType.BuildScout, null)
        {
            InitSteps();
        }

        public BuildScout(Empire owner) : base(GoalType.BuildScout, owner)
        {
            InitSteps();
            Owner.ChooseScoutShipToBuild(out IShipDesign scout);
            Build = new(scout);
        }

        void InitSteps()
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitForShipBuilt,
                OrderExplore
            };
        }

        GoalStep FindPlanetToBuildAt()
        {
            if (!Owner.FindPlanetToBuildShipAt(Owner.SafeSpacePorts, Build.Template, out Planet planet))
                return GoalStep.TryAgain;

            var queue    = planet.Construction.GetConstructionQueue();
            int priority = queue.Count > 0 && !planet.HasColonyShipFirstInQueue() && queue[0].ProductionNeeded > Build.Template.GetCost(Owner) * 2 ? 0 : 1;

            planet.Construction.Enqueue(Build.Template, this, notifyOnEmpty: false);
            planet.Construction.PrioritizeShip(Build.Template, priority, 2);

            return GoalStep.GoToNextStep;
        }
       
        GoalStep OrderExplore()
        {
            if (FinishedShip == null)
            {
                Log.Error($"BuildScout {Build.Template.Name} failed: BuiltShip is null!");
                return GoalStep.GoalFailed;
            }
            FinishedShip.AI.OrderExplore();
            return GoalStep.GoalComplete;
        }
    }
}
