using Ship_Game.AI;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using System;
using Ship_Game.Data.Serialization;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class DeployFleetProjector : FleetGoal
    {
        [StarData] BuildConstructionShip BuildGoal;

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
            Vector2 dir = Fleet.FleetTask.TargetPlanet.Position.DirectionToTarget(Fleet.AveragePosition());
            BuildPosition = ColonizationTarget.Position + dir * distanceToDeploy;
            BuildGoal = new(BuildPosition, "Subspace Projector", Owner);
            Owner.AI.AddGoal(BuildGoal);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitAndPrioritizeProjector()
        {
            // make sure we are still doing it
            Goal constructionGoal = Owner.AI.FindGoal(g => g == BuildGoal);
            if (constructionGoal == null)
                return GoalStep.RestartGoal; // ughhh wtf

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
