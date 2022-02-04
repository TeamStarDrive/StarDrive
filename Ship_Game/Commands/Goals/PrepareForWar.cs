using System;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Universe;

namespace Ship_Game.Commands.Goals
{
    public class PrepareForWar : Goal
    {
        public const string ID = "PrepareForWar";
        public override string UID => ID;
        private bool SkipFirstRun = true;

        public PrepareForWar(int id, UniverseState us)
            : base(GoalType.PrepareForWar, id, us)
        {
            Steps = new Func<GoalStep>[]
            {
                CheckIfShouldCreateStagingFleets,
                CreateTask,
                DeclareWarIfReady
            };
        }

        public PrepareForWar(Empire owner, Empire enemy)
            : this(owner.Universum.CreateId(), owner.Universum)
        {
            empire        = owner;
            TargetEmpire  = enemy;
            StarDateAdded = empire.Universum.StarDate;
            Log.Info(ConsoleColor.Green, $"---- Prepare For War: New {empire.Name} Vs.: {TargetEmpire.Name} ----");
        }

        bool TryGetTask(out MilitaryTask task)
        {
            task      = null;
            var tasks = empire.GetEmpireAI().GetTasks().Filter(t => t.Type == MilitaryTask.TaskType.StageFleet & t.TargetEmpire == TargetEmpire);
            if (tasks.Length > 0)
            {
                if (tasks.Length > 1)
                    Log.Warning($"Found multiple tasks for Prepare for War Goal. Owner: {empire.Name}, Target Empire: {TargetEmpire.Name}");

                task = tasks[0];
            }

            return tasks.Length > 0;
        }

        GoalStep CheckIfShouldCreateStagingFleets()
        {
            if (SkipFirstRun) // Hack - skipping first run to prevent insta war dec when loading a save. 
            {
                SkipFirstRun = false;
                return GoalStep.TryAgain;
            }

            if (!empire.TryGetPrepareForWarType(TargetEmpire, out _))
                return GoalStep.GoalFailed;

            if (empire.IsAtWarWithMajorEmpire && empire.GetAverageWarGrade() < 7)
                return GoalStep.TryAgain;

            var rel = empire.GetRelations(TargetEmpire);

            if (empire.ShouldCancelPrepareForWar())
            {
                rel.CancelPrepareForWar();
                return GoalStep.GoalFailed;
            }

            if (empire.ShouldGoToWar(rel, TargetEmpire))
            {
                if (empire.DetectPrepareForWarVsPlayer(TargetEmpire))
                    empire.Universum.Notifications.NotifyPreparingForWar(empire);

                return GoalStep.GoToNextStep;
            }

            return GoalStep.TryAgain;
        }

        GoalStep CreateTask()
        {
            if (!empire.TryGetPrepareForWarType(TargetEmpire, out WarType warType))
                return GoalStep.GoalFailed;

            if (!empire.GetPotentialTargetPlanets(TargetEmpire, warType, out Planet[] targetPlanets))
                return GoalStep.GoalFailed;

            TargetPlanet = empire.SortPlanetTargets(targetPlanets, warType, TargetEmpire)[0];
            empire.CreateStageFleetTask(TargetPlanet, TargetEmpire, this);
            return GoalStep.GoToNextStep;
        }

        GoalStep DeclareWarIfReady()
        {
            if (!TryGetTask(out MilitaryTask task))
                return GoalStep.GoalFailed;

            if (!empire.IsPreparingForWarWith(TargetEmpire))
            {
                task.EndTask();
                return GoalStep.GoalFailed;
            }

            if (task.Fleet?.TaskStep == 2)
            {
                if (!empire.TryGetPrepareForWarType(TargetEmpire, out WarType warType)
                    || !empire.GetPotentialTargetPlanets(TargetEmpire, warType, out _))
                {
                    // no target planets were found for this war type
                    task.EndTask();
                    return GoalStep.GoalFailed;
                }

                empire.GetEmpireAI().DeclareWarOn(TargetEmpire, warType);
                empire.GetEmpireAI().Goals.Add(new WarMission(empire, TargetEmpire, TargetPlanet, task));
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