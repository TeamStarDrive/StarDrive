using System;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Data.Serialization;
using Ship_Game.Fleets;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class WarMission : FleetGoal
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

        GoalStep CreateTask()
        {
            Owner.CreateWarTask(TargetPlanet, TargetEmpire, this, out Task);
            return GoalStep.GoToNextStep;
        }

        GoalStep Process()
        {
            // Updating task since some war fleet have dynamic tasks
            // like post invasion or reclaim after succssesful defense
            if (Fleet != null)
                Task = Fleet.FleetTask; 

            if (Task == null)
                return GoalStep.GoalFailed;

            // Update task targets since some of them can be dynamic
            TargetPlanet = Task.TargetPlanet;
            TargetEmpire = Task.TargetEmpire;
            Fleet        = Task.Fleet;

            if (TargetEmpire == null)
                return GoalStep.GoalComplete;

            if (!TargetEmpire.IsAtWarWith(Owner))
            {
                Task.EndTask();
                return GoalStep.GoalComplete;
            }

            if (LifeTime > Owner.PersonalityModifiers.WarTasksLifeTime && Task.Fleet == null) // check for timeout
            {
                Task.EndTask();
                Log.Info(ConsoleColor.Green, $"---- WarMission: Timed out {Task.Type} vs. {TargetEmpire.Name} ----");
                return GoalStep.GoalFailed;
            }

            return GoalStep.TryAgain;
        }
    }
}