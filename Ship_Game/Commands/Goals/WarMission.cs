using System;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;

namespace Ship_Game.Commands.Goals
{
    public class WarMission : Goal
    {
        public const string ID = "WarMission";
        public override string UID => ID;

        public WarMission() : base(GoalType.WarMission)
        {
            Steps = new Func<GoalStep>[]
            {
                CreateTasks,
                Process
            };
        }

        public WarMission(Empire owner, Empire enemy, Planet targetPlanet) : this()
        {
            empire       = owner;
            TargetEmpire = enemy;
            TargetPlanet = targetPlanet;
            Evaluate();
            Log.Info(ConsoleColor.Green, $"---- WarMission: New {empire.Name} Vs.: {TargetEmpire.Name} ----");
        }

        float LifeTime => Empire.Universe.StarDate - StarDateAdded;

        public override bool IsWarMission => true;

        GoalStep CreateTasks()
        {
            empire.CreateWarTask(TargetPlanet);
            return GoalStep.GoToNextStep;
        }

        GoalStep Process()
        {
            if (!TargetPlanet.Owner?.IsAtWarWith(empire) == true)
                return GoalStep.GoalComplete;

            if (LifeTime > 1) // check for timeout
            {
                var tasks = empire.GetEmpireAI().GetTasks().Filter(t => t.TargetEmpire == TargetEmpire && t.TargetPlanet == TargetPlanet);

                bool shouldRemoveTasks = !tasks.Any(t => t.Fleet != null);
                foreach (MilitaryTask task in tasks.Filter(t => !t.QueuedForRemoval))
                {
                    if (shouldRemoveTasks)
                    {
                        task.EndTask();
                        Log.Info(ConsoleColor.Green, $"---- WarMission: Timed out {task.type} vs. {TargetEmpire.Name} ----");
                    }
                    else // update task targets since some of them can be dynamic
                    {
                        TargetPlanet = task.TargetPlanet;
                        TargetEmpire = task.TargetEmpire;
                    }
                }

                return GoalStep.GoalFailed;
            }

            return GoalStep.TryAgain;
        }
    }
}