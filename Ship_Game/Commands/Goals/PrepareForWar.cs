using System;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class PrepareForWar : Goal
    {
        [StarData] public sealed override Empire TargetEmpire { get; set; }
        [StarData] public sealed override Planet TargetPlanet { get; set; }
        [StarData] bool SkipFirstRun = true;

        [StarDataConstructor]
        public PrepareForWar(Empire owner) : base(GoalType.PrepareForWar, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                CheckIfShouldCreateStagingFleets,
                CreateTask,
                DeclareWarIfReady
            };
        }

        public PrepareForWar(Empire owner, Empire enemy) : this(owner)
        {
            TargetEmpire = enemy;
            Log.Info(ConsoleColor.Green, $"---- Prepare For War: New {Owner.Name} Vs.: {TargetEmpire.Name} ----");
        }

        bool TryGetTask(out MilitaryTask task)
        {
            task = null;
            var tasks = Owner.AI.GetTasks().Filter(t => t.Type == MilitaryTask.TaskType.StageFleet & t.TargetEmpire == TargetEmpire);
            if (tasks.Length > 0)
            {
                if (tasks.Length > 1)
                    Log.Warning($"Found multiple tasks for Prepare for War Goal. Owner: {Owner.Name}, Target Empire: {TargetEmpire.Name}");

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

            if (!Owner.TryGetPrepareForWarType(TargetEmpire, out _))
                return GoalStep.GoalFailed;

            if (Owner.IsAtWarWithMajorEmpire && Owner.GetAverageWarGrade() < 7)
                return GoalStep.TryAgain;

            var rel = Owner.GetRelations(TargetEmpire);

            if (Owner.ShouldCancelPrepareForWar())
            {
                rel.CancelPrepareForWar();
                return GoalStep.GoalFailed;
            }

            if (Owner.ShouldGoToWar(rel, TargetEmpire))
            {
                if (Owner.DetectPrepareForWarVsPlayer(TargetEmpire))
                    Owner.Universe.Notifications.NotifyPreparingForWar(Owner);

                return GoalStep.GoToNextStep;
            }

            return GoalStep.TryAgain;
        }

        GoalStep CreateTask()
        {
            if (!Owner.TryGetPrepareForWarType(TargetEmpire, out WarType warType))
                return GoalStep.GoalFailed;

            if (!Owner.GetPotentialTargetPlanets(TargetEmpire, warType, out Planet[] targetPlanets))
                return GoalStep.GoalFailed;

            TargetPlanet = Owner.SortPlanetTargets(targetPlanets, warType, TargetEmpire)[0];
            Owner.CreateStageFleetTask(TargetPlanet, TargetEmpire, this);
            return GoalStep.GoToNextStep;
        }

        GoalStep DeclareWarIfReady()
        {
            if (!TryGetTask(out MilitaryTask task))
                return GoalStep.GoalFailed;

            if (!Owner.IsPreparingForWarWith(TargetEmpire))
            {
                if (Owner.IsAtWarWith(TargetEmpire) && task.Fleet != null)
                {
                    Owner.AI.AddGoal(new WarMission(Owner, TargetEmpire, TargetPlanet, task, 2));
                    return GoalStep.GoalComplete;
                }

                task.EndTask();
                return GoalStep.GoalFailed;
            }

            if (task.Fleet?.TaskStep >= 100)
            {
                if (!Owner.TryGetPrepareForWarType(TargetEmpire, out WarType warType)
                    || !Owner.GetPotentialTargetPlanets(TargetEmpire, warType, out _))
                {
                    // no target planets were found for this war type
                    task.EndTask();
                    return GoalStep.GoalFailed;
                }

                Owner.AI.DeclareWarOn(TargetEmpire, warType);
                int initialStrikeFleetTaskStep = task.Fleet.TaskStep == 100 ? 2 : 6;
                Owner.AI.AddGoal(new WarMission(Owner, TargetEmpire, TargetPlanet, task, initialStrikeFleetTaskStep));
                return GoalStep.GoalComplete;
            }

            return GoalStep.TryAgain;
        }
    }
}