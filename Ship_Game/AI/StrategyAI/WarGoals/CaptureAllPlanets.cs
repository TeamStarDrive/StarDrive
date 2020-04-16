using System;
using Microsoft.Xna.Framework;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public class CaptureAllPlanets : Campaign
    {
        SolarSystem CurrentTarget;

        /// <summary>
        /// Initializes from save a new instance of the <see cref="CaptureAllPlanets"/> class.
        /// </summary>
        public CaptureAllPlanets(Campaign campaign, War war) : base(campaign, war) => CreateSteps();

        public CaptureAllPlanets(CampaignType campaignType, War war) : base(campaignType, war)
        {
            CreateSteps();
        }

        void CreateSteps()
        {
            Steps = new Func<GoalStep>[] 
            {
                SetTargets,
                SetupRallyPoint,
                AttackSystems
            };
        }

        GoalStep SetTargets()
        {
            Vector2 empireCenter = Owner.GetWeightedCenter();
            AddTargetSystems(Them.GetOwnedSystems().Filter(s => s.IsExploredBy(Owner)));

            AddTargetSystems(OwnerWar.GetHistoricLostSystems().Filter(s => s.OwnerList.Contains(Them) && !s.OwnerList.Contains(Owner)));

            if (TargetSystems.IsEmpty) return GoalStep.GoalFailed;

            return GoalStep.GoToNextStep;
        }

        GoalStep AttackSystems()
        {
            if (HaveConqueredTargets()) return GoalStep.GoalComplete;

            var fleets        = Owner.AllFleetsReady();
            var nearestSystem = Owner.FindNearestOwnedSystemTo(TargetSystems);
            int priorityMod   = 0;
            float strength    = fleets.AccumulatedStrength;
            
            var tasks = new WarTasks(Owner, Them);
            foreach(var system in TargetSystems.Sorted(s=> s.Position.SqDist(nearestSystem.Position)))
            {
                if (!HaveConqueredTarget(system))
                {
                    float defense = Owner.GetEmpireAI().ThreatMatrix.PingHostileStr(system.Position, Owner.GetProjectorRadius(), Owner);
                    strength -= defense;
                    float distanceToCenter = system.Position.SqDist(nearestSystem.Position);
                    tasks.StandardAssault(system, OwnerWar.Priority() + priorityMod);
                }
                if (strength < 0) break;
                priorityMod++;
            }
            Owner.GetEmpireAI().AddPendingTasks(tasks.GetNewTasks());
            return GoalStep.RestartGoal;
        }

        bool HaveConqueredTargets()
        {
            foreach(var system in TargetSystems)
            {
                if (!HaveConqueredTarget(system)) return false;
            }
            return true;
        }

        bool HaveConqueredTarget(SolarSystem system) => !system.OwnerList.Contains(Them);
    }
}