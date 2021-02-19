using System;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class BuildScout : Goal
    {
        public const string ID = "Build Scout";
        public override string UID => ID;

        public BuildScout() : base(GoalType.BuildScout)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitForShipBuilt,
                OrderExplore,
                ReportGoalCompleteToEmpire
            };
        }
        public BuildScout(Empire empire) : this()
        {
            this.empire = empire;
        }

        GoalStep FindPlanetToBuildAt()
        {
            if (!empire.ChooseScoutShipToBuild(out Ship scout))
                return GoalStep.GoalFailed;

            if (!empire.FindPlanetToBuildAt(empire.SafeSpacePorts, scout, out Planet planet))
                return GoalStep.TryAgain;

            var queue    = planet.Construction.GetConstructionQueue();
            int priority = queue.Count > 0 && !planet.HasColonyShipFirstInQueue() && queue[0].ProductionNeeded > scout.GetCost(empire) * 2 ? 0 : 1;

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
