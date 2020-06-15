using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Ship_Game.Commands.Goals
{
    public class AssaultPirateBase : Goal
    {
        public const string ID = "AssaultPirateBase";
        public override string UID => ID;
        private Pirates Pirates;

        public AssaultPirateBase() : base(GoalType.AssaultPirateBase)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPirateBase,
                CreateTask,
                CheckBaseDestroyed
            };
        }

        public AssaultPirateBase(Empire e, Empire pirateEmpire) : this()
        {
            empire       = e;
            TargetEmpire = pirateEmpire;

            PostInit();
            Log.Info(ConsoleColor.Green, $"---- Retaliation vs. Pirates: New {empire.Name} Assault Base vs. {TargetEmpire.Name} ----");
        }

        public sealed override void PostInit()
        {
            Pirates = TargetEmpire.Pirates;
        }

        GoalStep FindPirateBase()
        {
            if (!Pirates.GetBases(out Array<Ship> bases))
                return GoalStep.GoalFailed;

            EmpireAI ai          = empire.GetEmpireAI();
            var filteredBases    = bases.Filter(s => !ai.HasAssaultPirateBaseTask(s, out _));
            Vector2 empireCenter = empire.GetWeightedCenter();

            filteredBases.Sort(s => s.Center.SqDist(empireCenter));
            TargetShip = filteredBases.First();
            return GoalStep.GoToNextStep;
        }

        GoalStep CreateTask()
        {
            var task      = MilitaryTask.CreateAssaultPirateBaseTask(TargetShip);
            task.Priority = -5;
            empire.GetEmpireAI().AddPendingTask(task);
            return GoalStep.GoToNextStep;
        }

        GoalStep CheckBaseDestroyed()
        {
            EmpireAI ai = empire.GetEmpireAI();
            if (ai.HasAssaultPirateBaseTask(TargetShip, out MilitaryTask task))
            {
                if (Pirates.PaidBy(empire))
                {
                    task.EndTask();
                    return GoalStep.GoalComplete;
                }

                if (TargetShip != null && TargetShip.Active)
                    return GoalStep.TryAgain;
            }

            return GoalStep.GoalComplete;
        }
    }
}
