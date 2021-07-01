﻿using System;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Fleets;

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
                CreateTask,
                Process
            };
        }

        public WarMission(Empire owner, Empire enemy, Planet targetPlanet) : this()
        {
            empire        = owner;
            TargetEmpire  = enemy;
            TargetPlanet  = targetPlanet;
            StarDateAdded = Empire.Universe.StarDate;
            Evaluate();
            Log.Info(ConsoleColor.Green, $"---- WarMission: New {empire.Name} Vs.: {TargetEmpire.Name} ----");
        }

        public WarMission(Empire owner, Empire enemy, Planet targetPlanet, MilitaryTask task) : this()
        {
            empire        = owner;
            TargetEmpire  = enemy;
            TargetPlanet  = targetPlanet;
            StarDateAdded = Empire.Universe.StarDate;
            ChangeToStep(Process);
            Fleet.CreateStrikeFromCurrentTask(task.Fleet, task, empire, this);
            Log.Info(ConsoleColor.Green, $"---- WarMission: New Strike Force from stage fleet, {empire.Name} Vs. {TargetEmpire.Name} ----");
        }

        float LifeTime => Empire.Universe.StarDate - StarDateAdded;

        public override bool IsWarMission => true;

        bool TryGetTask(out MilitaryTask task)
        {
            task      = null;
            var tasks = empire.GetEmpireAI().GetTasks().Filter(t => t.Goal == this);
            if (tasks.Length > 0)
            {
                if (tasks.Length > 1)
                    Log.Warning($"Found multiple tasks for WarMission Goal. Owner: {empire.Name}, Target Empire: {TargetEmpire.Name}");

                task = tasks[0];
            }

            return tasks.Length > 0;
        }

        GoalStep CreateTask()
        {
            empire.CreateWarTask(TargetPlanet, TargetEmpire, this);
            return GoalStep.GoToNextStep;
        }

        GoalStep Process()
        {
            if (!TryGetTask(out MilitaryTask task))
                return GoalStep.GoalFailed;

            // Update task targets since some of them can be dynamic
            TargetPlanet = task.TargetPlanet;
            TargetEmpire = task.TargetEmpire;

            if (!TargetEmpire.IsAtWarWith(empire))
            {
                task.EndTask();
                return GoalStep.GoalComplete;
            }

            if (LifeTime > empire.PersonalityModifiers.WarTasksLifeTime && task.Fleet == null) // check for timeout
            {
                task.EndTask();
                Log.Info(ConsoleColor.Green, $"---- WarMission: Timed out {task.Type} vs. {TargetEmpire.Name} ----");
                return GoalStep.GoalFailed;
            }

            return GoalStep.TryAgain;
        }
    }
}