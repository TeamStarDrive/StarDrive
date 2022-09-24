using System;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class DefendSystem : Goal
    {
        [StarData] public SolarSystem TargetSystem;

        [StarDataConstructor]
        public DefendSystem(Empire owner)
            : base(GoalType.DefendSystem, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                WaitForFleet,
                AssessDefense
            };
        }

        public DefendSystem(Empire owner, SolarSystem system, float strengthWanted, int fleetCount)
            : this(owner)
        {
            StarDateAdded  = owner.Universum.StarDate;
            TargetSystem   = system;
            Vector2 center = system.Position;
            float radius   = system.Radius * 1.5f;

            var task = new MilitaryTask(owner, center, radius, system, strengthWanted, MilitaryTask.TaskType.ClearAreaOfEnemies)
            {
                Goal = this,
                FleetCount = fleetCount,
                MinimumTaskForceStrength = strengthWanted
            };

            owner.AI.AddPendingTask(task);
        }

        bool  TryGetDefenseTask(out MilitaryTask task)
        {
            task = null;
            var tasks = Owner.AI.GetDefendSystemTasks().Filter(t => t.TargetSystem == TargetSystem);
            if (tasks.Length > 0)
                task = tasks[0];

            return task != null;
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
