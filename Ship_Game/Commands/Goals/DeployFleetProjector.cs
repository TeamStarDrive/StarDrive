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
        [StarData] BuildConstructionShip BuildGoal; // the build goal will direct its constructor to deploy ssp in wanted position
        [StarData] public sealed override Planet TargetPlanet { get; set; }
        [StarData] public sealed override Planet PlanetBuildingAt { get; set; }

        [StarDataConstructor]
        public DeployFleetProjector(Empire owner) : base(GoalType.DeployFleetProjector, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                BuildProjector,
                WaitAndPrioritizeProjector,
                WaitForProjectorDeployed,
                RemoveProjectorWhenCompleted
            };
        }

        public DeployFleetProjector(Fleet fleet, Planet claim, Empire e) : this(e)
        {
            Fleet = fleet;
            TargetPlanet = claim;
        }

        GoalStep BuildProjector()
        {
            if (Fleet == null || TargetPlanet.System.HasPlanetsOwnedBy(Owner))
                return GoalStep.GoalComplete;

            float distanceToDeploy = Owner.GetProjectorRadius() * 0.8f;
            Vector2 dir = TargetPlanet.Position.DirectionToTarget(Fleet.AveragePosition());
            BuildGoal = new(TargetPlanet.Position + dir * distanceToDeploy, "Subspace Projector", Owner, rush: true);
            Owner.AI.AddGoalAndEvaluate(BuildGoal);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitAndPrioritizeProjector()
        {
            // make sure we are still doing it
            if (BuildGoal == null)
                return GoalStep.RestartGoal; // BuildGoal was canceled

            if (BuildGoal.FinishedShip == null)
            {
                if (Fleet != null)
                {
                    BuildGoal.PlanetBuildingAt?.Construction.PrioritizeProjector(BuildGoal.BuildPosition);
                    return GoalStep.TryAgain;
                }

                // Fleet was canceled
                BuildGoal.PlanetBuildingAt?.Construction.Cancel(BuildGoal);
                return GoalStep.GoalFailed;

            }

            // We have a construction ship on the way
            FinishedShip = BuildGoal.FinishedShip;
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForProjectorDeployed()
        {
            if (Fleet?.FleetTask == null)
            {
                FinishedShip?.AI.OrderScrapShip();
                return GoalStep.GoalFailed;
            }

            if (FinishedShip == null || !FinishedShip.Active)
            {
                var projectors = Owner.OwnedProjectors;
                for (int i = 0; i < projectors.Count; i++)
                {
                    Ship projector = projectors[i];
                    if (projector.Position.InRadius(BuildGoal.BuildPosition, 200))
                    {
                        FinishedShip = projector; // Projector is deployed and now assigned to this goal
                        return GoalStep.GoToNextStep;
                    }
                }
            }
            else
            {
                return GoalStep.TryAgain; // Consturctor still enroute
            }

            return GoalStep.GoalFailed; // construcor died without deploying the projector
        }

        GoalStep RemoveProjectorWhenCompleted()
        {
            if (FinishedShip == null || !FinishedShip.Active)
                return GoalStep.GoalFailed; 

            if (Fleet?.FleetTask == null)
            {
                if (FinishedShip != null)
                    FinishedShip.ScuttleTimer = 30;

                return GoalStep.GoalComplete;
            }

            return GoalStep.TryAgain;
        }
    }
}
