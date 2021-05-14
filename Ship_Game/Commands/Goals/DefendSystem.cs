using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Empires.DataPackets;

namespace Ship_Game.Commands.Goals
{
    public class DefendSystem : Goal
    {
        public const string ID = "Defend System";
        public override string UID => ID;

        public DefendSystem() : base(GoalType.DefendSystem)
        {
            Steps = new Func<GoalStep>[]
            {
                WaitForFleet,
                AssessDefense
            };
        }

        public DefendSystem(Empire empire, SolarSystem system, int priority, float strengthWanted, int fleetCount) : this()
        {
            this.empire    = empire;
            StarDateAdded  = Empire.Universe.StarDate;
            TargetSystem   = system;
            Vector2 center = system.Position;
            float radius   = system.Radius * 1.5f;

            MilitaryTask task = new MilitaryTask(empire, center, radius, system, strengthWanted, MilitaryTask.TaskType.ClearAreaOfEnemies)
            {
                Priority                 = priority,
                FleetCount               = fleetCount,
                MinimumTaskForceStrength = strengthWanted
            };

            empire.GetEmpireAI().AddPendingTask(task);
        }

        float LifeTime => Empire.Universe.StarDate - StarDateAdded;

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
                if (LifeTime > 50 && !empire.SystemsWithThreat.Any(ts => ts.TargetSystem == TargetSystem))
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
