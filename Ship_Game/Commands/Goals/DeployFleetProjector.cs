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
            Vector2 deployPos = TargetPlanet.Position + dir * distanceToDeploy;

            // did another projector already take the spot?
            if (Owner.FindProjectorAt(deployPos, 100, out Ship _))
            {
                Log.Info($"Build pos of fleet projector is near {deployPos}");
                return GoalStep.GoalComplete;
            }

            BuildGoal = new(deployPos, "Subspace Projector", Owner, rush: true);
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

            // TODO: this logic is really convoluted, it needs to be redesigned
            if (FinishedShip == null || !FinishedShip.Active)
            {
                // check if we had a duplicate projector at that location?
                if (Owner.FindProjectorAt(BuildGoal.BuildPosition, 200, out Ship projector))
                {
                    FinishedShip = projector; // Projector is deployed and now assigned to this goal
                    return GoalStep.GoToNextStep;
                }
                return GoalStep.GoalFailed; // constructor died without deploying the projector
            }
            else
            {
                return GoalStep.TryAgain; // constructo still en route
            }
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
