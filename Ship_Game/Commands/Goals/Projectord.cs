using Ship_Game.AI;
using Ship_Game.Ships;
using System;
using Ship_Game.Data.Serialization;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class ProjectorBridge : Goal
    {
        [StarData] readonly BuildConstructionShip BuildGoal;
        [StarData] public sealed override Planet TargetPlanet { get; set; }
        [StarData] public sealed override Planet PlanetBuildingAt { get; set; }
        [StarData] public SolarSystem TargetSystem;
        [StarData] public Vector2 StaticBuildPosition;

        [StarDataConstructor]
        public ProjectorBridge(Empire owner) : base(GoalType.Refit, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                BuildProjector,
                WaitForProjector,
                RemoveProjectorWhenSafe
            };
        }

        // This will create projector coverage for ships, if they pass in a dangerous system
        public ProjectorBridge(SolarSystem targetSystem, SolarSystem origin, Empire e) : this(e)
        {
            TargetSystem = targetSystem;

            float distanceToDeploy = Owner.GetProjectorRadius() * 0.5f;
            Vector2 dir = targetSystem.Position.DirectionToTarget(origin.Position);
            StaticBuildPosition = TargetSystem.Position + dir * distanceToDeploy;
            BuildGoal = new BuildConstructionShip(StaticBuildPosition, "Subspace Projector", Owner, rush: true);
        }

        GoalStep BuildProjector()
        {
            Owner.AI.AddGoalAndEvaluate(BuildGoal);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForProjector()
        {
            Goal constructionGoal = Owner.AI.FindGoal(g => g == BuildGoal);
            if (constructionGoal == null)
                return GoalStep.GoalFailed;

            if (constructionGoal.FinishedShip == null)
            {
                if (Owner.KnownEnemyStrengthIn(TargetSystem) == 0)
                {
                    constructionGoal.PlanetBuildingAt?.Construction.Cancel(constructionGoal);
                    return GoalStep.GoalFailed;
                }

                return GoalStep.TryAgain;
            }

            FinishedShip = constructionGoal.FinishedShip; // We have a construction ship on the way
            return GoalStep.GoToNextStep;
        }

        GoalStep RemoveProjectorWhenSafe()
        {
            if (Owner.KnownEnemyStrengthIn(TargetSystem) == 0)
            {
                FinishedShip?.AI.OrderScrapShip();
                var projectors = Owner.OwnedProjectors;
                for (int i = 0; i < projectors.Count; i++)
                {
                    Ship ship = projectors[i];
                    if (ship.Position.InRadius(StaticBuildPosition, 1000))
                        ship.ScuttleTimer = 10;
                }

                return GoalStep.GoalComplete;
            }

            return GoalStep.TryAgain;
        }
    }
}

