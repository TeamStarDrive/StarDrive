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

        // For defending allied empires. If it's null - we are defending ourselves
        [StarData] public sealed override Empire TargetEmpire { get; set; } 

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
            , MilitaryTaskImportance importance, Empire targetEmpire): this(owner)
        {
            StarDateAdded  = owner.Universe.StarDate;
            TargetSystem   = system;
            TargetEmpire   = targetEmpire;
            Vector2 center = system.Position;
            float radius   = system.Radius * 1.5f;

            Task = new MilitaryTask(MilitaryTask.TaskType.ClearAreaOfEnemies, owner, center, radius,
                system, strengthWanted, importance)
            {
                Goal = this,
                FleetCount = fleetCount,
                MinimumTaskForceStrength = strengthWanted
            };

            owner.AI.AddPendingTask(Task);
        }

        GoalStep WaitForFleet()
        {
            if (Task == null)
                return GoalStep.GoalFailed;

            if (Task.Fleet == null)
            {
                if (!AlliedWithTargetEmpire
                    || LifeTime > 10
                       && !Owner.IsSystemUnderThreatForUs(TargetSystem) 
                       && !Owner.IsSystemUnderThreatForAllies(TargetSystem))
                {
                    Task.EndTask(); // Timeout or we are not allied with target empire anymore
                    return GoalStep.GoalFailed;
                }
            }
            else
            {
                Fleet = Task.Fleet;
                return GoalStep.GoToNextStep;
            }

            return GoalStep.TryAgain;
        }

        GoalStep AssessDefense()
        {
            return Task != null ? GoalStep.TryAgain : GoalStep.GoalComplete;
        }

        bool AlliedWithTargetEmpire => TargetEmpire == null  // If its null, that we need to return true here.
                                       || TargetEmpire.IsAlliedWith(Owner) == true;
    }
}
