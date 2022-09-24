using Ship_Game.AI;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using System;
using System.Linq;
using SDUtils;
using Ship_Game.Data.Serialization;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class DeployFleetProjector : Goal
    {
        [StarDataConstructor]
        public DeployFleetProjector(Empire owner) : base(GoalType.DeployFleetProjector, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                BuildProjector,
                WaitAndPrioritizeProjector,
                RemoveProjectorWhenCompleted
            };
        }

        public DeployFleetProjector(Fleet fleet, Planet claim, Empire e) : this(e)
        {
            Fleet = fleet;
            ColonizationTarget = claim;
        }

        GoalStep BuildProjector()
        {
            if (Fleet == null || ColonizationTarget.ParentSystem.HasPlanetsOwnedBy(Owner))
                return GoalStep.GoalComplete;

            float distanceToDeploy = Owner.GetProjectorRadius() * 0.8f;
            Vector2 direction      = Fleet.FleetTask.TargetPlanet.Position.DirectionToTarget(Fleet.AveragePosition());
            BuildPosition          = ColonizationTarget.Position + direction.Normalized() * distanceToDeploy;
            Goal goal              = new BuildConstructionShip(BuildPosition, "Subspace Projector", Owner);
            goal.Fleet             = Fleet;
            Owner.GetEmpireAI().AddGoal(goal);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitAndPrioritizeProjector()
        {
            var goals = Owner.GetEmpireAI().SearchForGoals(GoalType.DeepSpaceConstruction).Filter(g => g.Fleet == Fleet);
            if (goals.Length > 0)
            {
                Goal constructionGoal = goals.First();
                if (constructionGoal.FinishedShip == null)
                {
                    if (Fleet == null)
                    {
                        constructionGoal.PlanetBuildingAt?.Construction.Cancel(constructionGoal);
                        return GoalStep.GoalFailed;
                    }

                    constructionGoal.PlanetBuildingAt?.Construction.PrioritizeProjector(BuildPosition);
                    return GoalStep.TryAgain;
                }

                FinishedShip = constructionGoal.FinishedShip; // We have a construction ship on the way
                return GoalStep.GoToNextStep;
            }

            return GoalStep.GoalFailed;
        }

        GoalStep RemoveProjectorWhenCompleted()
        {
            if (Fleet?.FleetTask == null)
            {
                FinishedShip?.AI.OrderScrapShip();
                var projectors = Owner.GetProjectors();
                for (int i = 0; i < projectors.Count; i++)
                {
                    Ship ship = projectors[i];
                    if (ship.Position.InRadius(BuildPosition, 1000))
                        ship.ScuttleTimer = 120;
                }

                return GoalStep.GoalComplete;
            }

            return GoalStep.TryAgain;
        }
    }
}
