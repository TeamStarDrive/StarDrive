using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Ships;
using System;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class AssaultPirateBase : Goal
    {
        [StarData] Pirates Pirates;

        [StarDataConstructor]
        public AssaultPirateBase(int id, UniverseState us)
            : base(GoalType.AssaultPirateBase, id, us)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPirateBase,
                CreateTask,
                CheckBaseDestroyed
            };
        }

        public AssaultPirateBase(Empire e, Empire pirateEmpire, Ship targetBase = null)
            : this(e.Universum.CreateId(), e.Universum)
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
            var task       = MilitaryTask.CreateAssaultPirateBaseTask(TargetShip, empire);
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

                empire.DecreaseFleetStrEmpireMultiplier(TargetEmpire); // base is destroyed
            }

            return GoalStep.GoalComplete;
        }

        bool GetClosestBaseToCenter(Ship[] basesList, out Ship closestPirateBase)
        {
            closestPirateBase    = null;
            Vector2 empireCenter = empire.WeightedCenter;
            closestPirateBase    = basesList.FindMin(b => b.Position.Distance(empireCenter));
            return closestPirateBase != null;
        }
    }
}
