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

            if (!GetClosestBaseToBorders(filteredBases, out TargetShip))
                return GoalStep.GoalFailed;

            return GoalStep.GoToNextStep;
        }

        bool GetClosestBaseToBorders(Ship[] basesList, out Ship closestPirateBase)
        {
            closestPirateBase          = null;
            var empirePlanets          = empire.GetPlanets();
            float bestShortestDistance = float.MaxValue;
            for (int i = 0; i < basesList.Length; i++)
            {
                Ship pirateBase        = basesList[i];
                float shortestDistance = empirePlanets.Min(p => p.Center.Distance(pirateBase.Center));
                if (shortestDistance < bestShortestDistance)
                {
                    bestShortestDistance = shortestDistance;
                    closestPirateBase    = pirateBase;
                }
            }

            return closestPirateBase != null;
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
