using System;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;

namespace Ship_Game.Commands.Goals
{
    public class PrepareForWar : Goal
    {
        public const string ID = "PrepareForWar";
        public override string UID => ID;

        public PrepareForWar() : base(GoalType.WarMission)
        {
            Steps = new Func<GoalStep>[]
            {
                CreateTask,
                CheckIfReadyForWar
            };
        }

        public PrepareForWar(Empire owner, Empire enemy) : this()
        {
            empire        = owner;
            TargetEmpire  = enemy;
            StarDateAdded = Empire.Universe.StarDate;
            Evaluate();
            Log.Info(ConsoleColor.Green, $"---- Prepare For War: New {empire.Name} Vs.: {TargetEmpire.Name} ----");
        }

        bool TryGetTask(out MilitaryTask task)
        {
            task      = null;
            var tasks = empire.GetEmpireAI().GetTasks().Filter(t => t.type == MilitaryTask.TaskType.StageFleet & t.TargetEmpire == TargetEmpire);
            if (tasks.Length > 0)
            {
                if (tasks.Length > 1)
                    Log.Warning($"Found multiple tasks for Prepare for War Goal. Owner: {empire.Name}, Target Empire: {TargetEmpire.Name}");

                task = tasks[0];
            }

            return tasks.Length > 0;
        }

        GoalStep CreateTask()
        {
            if (!empire.TryGetPrepareForWarType(empire, out WarType warType))
                return GoalStep.GoalFailed;

            if (!empire.GetPotentialTargetPlanets(TargetEmpire, warType, out Planet[] targetPlanets))
                return GoalStep.GoalFailed;

            TargetPlanet = empire.SortPlanetTargets(targetPlanets, warType, TargetEmpire)[0];
            empire.CreateStageFleetTask(TargetPlanet, TargetEmpire);
            return GoalStep.GoToNextStep;
        }

        GoalStep CheckIfReadyForWar()
        {
            if (!TryGetTask(out MilitaryTask task))
                return GoalStep.GoalFailed;

            if (task.Fleet?.TaskStep == 2)
            {
                // todo create warmission from this fleet and declare war
                return GoalStep.GoalComplete;
            }

            if (empire.IsAtWarWith(TargetEmpire))
            {
                task.EndTask();
                return GoalStep.GoalComplete;
            }

            return GoalStep.TryAgain;
        }
    }
}