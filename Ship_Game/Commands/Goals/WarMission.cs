using System;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Data.Serialization;
using Ship_Game.Fleets;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class WarMission : Goal
    {
        [StarData] public sealed override Empire TargetEmpire { get; set; }
        [StarData] public sealed override Planet TargetPlanet { get; set; }

        [StarDataConstructor]
        public WarMission(Empire owner) : base(GoalType.WarMission, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                CreateTask,
                Process
            };
        }

        public WarMission(Empire owner, Empire enemy, Planet targetPlanet) : this(owner)
        {
            TargetEmpire = enemy;
            TargetPlanet = targetPlanet;
            Log.Info(ConsoleColor.Green, $"---- WarMission: New {Owner.Name} Vs.: {TargetEmpire.Name} ----");
        }

        public WarMission(Empire owner, Empire enemy, Planet targetPlanet, MilitaryTask task) : this(owner)
        {
            TargetEmpire  = enemy;
            TargetPlanet  = targetPlanet;
            ChangeToStep(Process);
            Fleet.CreateStrikeFromCurrentTask(task.Fleet, task, Owner, this);
            Log.Info(ConsoleColor.Green, $"---- WarMission: New Strike Force from stage fleet, {Owner.Name} Vs. {TargetEmpire.Name} ----");
        }

        public override bool IsWarMissionTarget(Planet planet) => TargetPlanet == planet;
        public override bool IsWarMissionTarget(Empire empire) => TargetEmpire == empire;

        bool TryGetTask(out MilitaryTask task)
        {
            task      = null;
            var tasks = Owner.AI.GetTasks().Filter(t => t.Goal == this);
            if (tasks.Length > 0)
            {
                if (tasks.Length > 1)
                    Log.Warning($"Found multiple tasks for WarMission Goal. Owner: {Owner.Name}, Target Empire: {TargetEmpire.Name}");

                task = tasks[0];
            }

            return tasks.Length > 0;
        }

        GoalStep CreateTask()
        {
            Owner.CreateWarTask(TargetPlanet, TargetEmpire, this);
            return GoalStep.GoToNextStep;
        }

        GoalStep Process()
        {
            if (!TryGetTask(out MilitaryTask task))
                return GoalStep.GoalFailed;

            // Update task targets since some of them can be dynamic
            TargetPlanet = task.TargetPlanet;
            TargetEmpire = task.TargetEmpire;

            if (!TargetEmpire.IsAtWarWith(Owner))
            {
                task.EndTask();
                return GoalStep.GoalComplete;
            }

            if (LifeTime > Owner.PersonalityModifiers.WarTasksLifeTime && task.Fleet == null) // check for timeout
            {
                task.EndTask();
                Log.Info(ConsoleColor.Green, $"---- WarMission: Timed out {task.Type} vs. {TargetEmpire.Name} ----");
                return GoalStep.GoalFailed;
            }

            return GoalStep.TryAgain;
        }
    }
}