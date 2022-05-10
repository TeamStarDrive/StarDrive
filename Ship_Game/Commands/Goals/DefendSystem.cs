using System;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Commands.Goals
{
    public class DefendSystem : Goal
    {
        public const string ID = "Defend System";
        public override string UID => ID;

        public DefendSystem(int id, UniverseState us)
            : base(GoalType.DefendSystem, id, us)
        {
            Steps = new Func<GoalStep>[]
            {
                WaitForFleet,
                AssessDefense
            };
        }

        public DefendSystem(Empire empire, SolarSystem system, float strengthWanted, int fleetCount)
            : this(empire.Universum.CreateId(), empire.Universum)
        {
            this.empire    = empire;
            StarDateAdded  = empire.Universum.StarDate;
            TargetSystem   = system;
            Vector2 center = system.Position;
            float radius   = system.Radius * 1.5f;

            MilitaryTask task = new MilitaryTask(empire, center, radius, system, strengthWanted, MilitaryTask.TaskType.ClearAreaOfEnemies)
            {
                FleetCount               = fleetCount,
                MinimumTaskForceStrength = strengthWanted,
                Goal   = this,
                GoalId = Id
            };

            empire.GetEmpireAI().AddPendingTask(task);
        }

        bool  TryGetDefenseTask(out MilitaryTask task)
        {
            task = null;
            var tasks = empire.GetEmpireAI().GetDefendSystemTasks().Filter(t => t.TargetSystem == TargetSystem);
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
                if (LifeTime > 10 && !empire.SystemsWithThreat.Any(ts => !ts.ThreatTimedOut && ts.TargetSystem == TargetSystem))
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
