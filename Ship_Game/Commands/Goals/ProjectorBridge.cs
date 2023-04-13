using Ship_Game.AI;
using Ship_Game.Ships;
using System;
using Ship_Game.Data.Serialization;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Commands.Goals
{
     /* Fat Bastard
     AI (and for players with auto build projectors) will create SSP coverage to cover systems with remnants
     (or other enemies) in the way of other colonization target. This will allow colony ships to arrive to the
     colonization target safely. This is triggered once a colony ship or freighter is destroyed in a gravity well
     of a hostile system - so the next ship will pass safely.
     */

    [StarDataType]
    public class ProjectorBridge : Goal
    {
        [StarData] readonly BuildConstructionShip BuildGoal;
        [StarData] public sealed override Planet TargetPlanet { get; set; }
        [StarData] public sealed override Planet PlanetBuildingAt { get; set; }
        [StarData] public SolarSystem TargetSystem;
        [StarData] public Vector2 StaticBuildPosition;
        [StarData] readonly ProjectorBridgeEndCondition EndCondition;

        [StarDataConstructor]
        public ProjectorBridge(Empire owner) : base(GoalType.ProjectorBridge, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                BuildProjector,
                WaitForProjector,
                RemoveProjectorWhenSafe
            };
        }

        // This will create projector coverage for ships, if they pass in a dangerous system
        public ProjectorBridge(SolarSystem targetSystem, Vector2 originPos, Empire e,
            ProjectorBridgeEndCondition endCondition) : this(e)
        {
            TargetSystem = targetSystem;
            float distanceToDeploy = Owner.GetProjectorRadius() * 0.5f;
            Vector2 dir = targetSystem.Position.DirectionToTarget(originPos);
            StaticBuildPosition = TargetSystem.Position + dir * distanceToDeploy;
            BuildGoal = new BuildConstructionShip(StaticBuildPosition, "Subspace Projector", Owner, rush: true);
            EndCondition = endCondition;
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
                if (EndConditionConfirmed())
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
            if (EndConditionConfirmed())
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

        bool EndConditionConfirmed()
        {
            switch (EndCondition)
            {
                default:
                case ProjectorBridgeEndCondition.NoHostiles: return Owner.KnownEnemyStrengthIn(TargetSystem) == 0;
                case ProjectorBridgeEndCondition.Timer:      return LifeTime > 20;
            }
        }
    }
    public enum ProjectorBridgeEndCondition
    {
        NoHostiles,
        Timer // 200 turns 
    }
}

