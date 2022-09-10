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
        [StarData] Pirates Pirates;

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
            TargetShip   = targetBase; // TargetShip is the pirate base
            PostInit();
            Log.Info(ConsoleColor.Green, $"---- Retaliation vs. Pirates: New {Owner.Name} Assault Base vs. {TargetEmpire.Name} ----");
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

            EmpireAI ai          = Owner.GetEmpireAI();
            var filteredBases    = bases.Filter(s => !ai.HasAssaultPirateBaseTask(s, out _));

            if (!GetClosestBaseToCenter(filteredBases, out TargetShip)) // TargetShip is the pirate base
                return GoalStep.GoalFailed;
            
            return GoalStep.GoToNextStep;
        }

        GoalStep CreateTask()
        { 
            var task       = MilitaryTask.CreateAssaultPirateBaseTask(TargetShip, Owner);
            Owner.GetEmpireAI().AddPendingTask(task);
            return GoalStep.GoToNextStep;
        }

        GoalStep CheckBaseDestroyed()
        {
            EmpireAI ai = Owner.GetEmpireAI();
            if (ai.HasAssaultPirateBaseTask(TargetShip, out MilitaryTask task))
            {
                if (Pirates.PaidBy(Owner))
                {
                    task.EndTask();
                    return GoalStep.GoalComplete;
                }

                if (TargetShip != null && TargetShip.Active)
                    return GoalStep.TryAgain;

                Owner.DecreaseFleetStrEmpireMultiplier(TargetEmpire); // base is destroyed
            }

            return GoalStep.GoalComplete;
        }

        bool GetClosestBaseToCenter(Ship[] basesList, out Ship closestPirateBase)
        {
            closestPirateBase    = null;
            Vector2 empireCenter = Owner.WeightedCenter;
            closestPirateBase    = basesList.FindMin(b => b.Position.Distance(empireCenter));
            return closestPirateBase != null;
        }
    }
}
