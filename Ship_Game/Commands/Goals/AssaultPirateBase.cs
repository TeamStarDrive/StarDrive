using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Ships;
using System;
using Microsoft.Xna.Framework;

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

        public AssaultPirateBase(Empire e, Empire pirateEmpire, Ship targetBase = null) : this()
        {
            empire       = e;
            TargetEmpire = pirateEmpire;
            TargetShip   = targetBase; // TargetShip is the pirate base

            PostInit();
            Log.Info(ConsoleColor.Green, $"---- Retaliation vs. Pirates: New {empire.Name} Assault Base vs. {TargetEmpire.Name} ----");
        }

        public sealed override void PostInit()
        {
            Pirates = TargetEmpire.Pirates;
        }

        GoalStep FindPirateBase()
        {
            if (TargetShip != null)
                return GoalStep.GoToNextStep;

            if (!Pirates.GetBases(out Array<Ship> bases))
                return GoalStep.GoalFailed;

            EmpireAI ai          = empire.GetEmpireAI();
            var filteredBases    = bases.Filter(s => !ai.HasAssaultPirateBaseTask(s, out _));

            if (!GetClosestBaseToCenter(filteredBases, out TargetShip)) // TargetShip is the pirate base
                return GoalStep.GoalFailed;
            
            return GoalStep.GoToNextStep;
        }

        GoalStep CreateTask()
        {
            empire.UpdateTargetsStrMultiplier(TargetShip.guid, out float multiplier);
            float minStr = TargetShip.GetStrength() * empire.DifficultyModifiers.TaskForceStrength;
            var task     = MilitaryTask.CreateAssaultPirateBaseTask(TargetShip, minStr * multiplier * 2);
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

        bool GetClosestBaseToCenter(Ship[] basesList, out Ship closestPirateBase)
        {
            closestPirateBase    = null;
            Vector2 empireCenter = empire.WeightedCenter;
            closestPirateBase    = basesList.FindMin(b => b.Center.Distance(empireCenter));
            return closestPirateBase != null;
        }
    }
}
