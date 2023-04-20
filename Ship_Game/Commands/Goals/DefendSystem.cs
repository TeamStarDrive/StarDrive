using System;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Data.Serialization;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class DefendSystem : FleetGoal
    {
        [StarData] public SolarSystem TargetSystem;
        [StarData] MilitaryTaskImportance Importance;

        [StarDataConstructor]
        public DefendSystem(Empire owner) : base(GoalType.DefendSystem, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                WaitForFleet,
                AssessDefense
            };
        }

        public DefendSystem(Empire owner, SolarSystem system, float strengthWanted, int fleetCount
            , MilitaryTaskImportance importance): this(owner)
        {
            StarDateAdded  = owner.Universe.StarDate;
            TargetSystem   = system;
            Vector2 center = system.Position;
            float radius   = system.Radius * 1.5f;
            Importance     = importance;

            var task = new MilitaryTask(MilitaryTask.TaskType.ClearAreaOfEnemies, owner, center, radius,
                system, strengthWanted, importance)
            {
                Goal = this,
                FleetCount = fleetCount,
                MinimumTaskForceStrength = strengthWanted
            };

            owner.AI.AddPendingTask(task);
        }

        bool  TryGetDefenseTask(out MilitaryTask task)
        {
            return (task = Owner.AI.GetDefendSystemTasks().Find(t => t.TargetSystem == TargetSystem)) != null;
        }

        GoalStep WaitForFleet()
        {
            if (!TryGetDefenseTask(out MilitaryTask task))
                return GoalStep.GoalFailed;

            if (task.Fleet == null)
            {
                if (LifeTime > 10 && !Owner.SystemsWithThreat.Any(ts => !ts.ThreatTimedOut && ts.TargetSystem == TargetSystem))
                {
                    task.EndTask(); // Timeout
                    return GoalStep.GoalFailed;
                }
            }
            else
            {
                Fleet = task.Fleet;
                return GoalStep.GoToNextStep;
            }

            return GoalStep.TryAgain;
        }

        GoalStep AssessDefense()
        {
            return TryGetDefenseTask(out _) ? GoalStep.TryAgain : GoalStep.GoalComplete;
        }
    }
}
