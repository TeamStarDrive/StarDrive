using System;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class BuildScout : Goal
    {
        [StarDataConstructor]
        public BuildScout(Empire owner) : base(GoalType.BuildScout, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitForShipBuilt,
                OrderExplore,
                ReportGoalCompleteToEmpire
            };
        }

        GoalStep FindPlanetToBuildAt()
        {
            if (!Owner.ChooseScoutShipToBuild(out IShipDesign scout))
                return GoalStep.GoalFailed;

            if (!Owner.FindPlanetToBuildShipAt(Owner.SafeSpacePorts, scout, out Planet planet))
                return GoalStep.TryAgain;

            var queue    = planet.Construction.GetConstructionQueue();
            int priority = queue.Count > 0 && !planet.HasColonyShipFirstInQueue() && queue[0].ProductionNeeded > scout.GetCost(Owner) * 2 ? 0 : 1;

            planet.Construction.Enqueue(scout, this, notifyOnEmpty: false);
            planet.Construction.PrioritizeShip(scout, priority, 2);

            return GoalStep.GoToNextStep;
        }
       
        GoalStep OrderExplore()
        {
            if (FinishedShip == null)
            {
                Log.Error($"BuildScout {ToBuildUID} failed: BuiltShip is null!");
                return GoalStep.GoalFailed;
            }
            FinishedShip.AI.OrderExplore();
            return GoalStep.GoalComplete;
        }

        GoalStep ReportGoalCompleteToEmpire() // FB - Not used: remove this in Mars, when we can break saves
        {
            return GoalStep.GoalComplete;
        }
    }
}
