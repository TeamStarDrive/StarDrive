using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Ships;
using System;
using SDUtils;
using Ship_Game.Data.Serialization;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class AssaultPirateBase : Goal
    {
        Pirates Pirates => TargetEmpire.Pirates;
        Ship PirateBase => TargetShip;

        [StarDataConstructor]
        public AssaultPirateBase(Empire owner) : base(GoalType.AssaultPirateBase, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPirateBase,
                CreateTask,
                CheckBaseDestroyed
            };
        }

        public AssaultPirateBase(Empire e, Empire pirateEmpire, Ship targetBase = null) : this(e)
        {
            TargetEmpire = pirateEmpire;
            TargetShip = targetBase; // TargetShip is the pirate base
            Log.Info(ConsoleColor.Green, $"---- Retaliation vs. Pirates: New {Owner.Name} Assault Base vs. {TargetEmpire.Name} ----");
        }

        GoalStep FindPirateBase()
        {
            if (TargetShip != null)
                return GoalStep.GoToNextStep;

            if (!Pirates.GetBases(out Array<Ship> bases))
                return GoalStep.GoalFailed;

            EmpireAI ai          = Owner.GetEmpireAI();
            var filteredBases    = bases.Filter(s => !ai.HasAssaultPirateBaseTask(s, out _));

            if (!GetClosestBaseToCenter(filteredBases, out TargetShip)) // TargetShip is the pirate base
                return GoalStep.GoalFailed;
            
            return GoalStep.GoToNextStep;
        }

        GoalStep CreateTask()
        { 
            var task = MilitaryTask.CreateAssaultPirateBaseTask(PirateBase, Owner);
            Owner.GetEmpireAI().AddPendingTask(task);
            return GoalStep.GoToNextStep;
        }

        GoalStep CheckBaseDestroyed()
        {
            EmpireAI ai = Owner.GetEmpireAI();
            if (ai.HasAssaultPirateBaseTask(PirateBase, out MilitaryTask task))
            {
                if (Pirates.PaidBy(Owner))
                {
                    task.EndTask();
                    return GoalStep.GoalComplete;
                }

                if (PirateBase is { Active: true })
                    return GoalStep.TryAgain;

                Owner.DecreaseFleetStrEmpireMultiplier(TargetEmpire); // base is destroyed
            }

            return GoalStep.GoalComplete;
        }

        bool GetClosestBaseToCenter(Ship[] basesList, out Ship closestPirateBase)
        {
            Vector2 empireCenter = Owner.WeightedCenter;
            closestPirateBase = basesList.FindMin(b => b.Position.Distance(empireCenter));
            return closestPirateBase != null;
        }
    }
}
