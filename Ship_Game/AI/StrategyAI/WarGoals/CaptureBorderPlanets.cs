using System;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public class CaptureBorderPlanets : Campaign
    {
        SolarSystem CurrentTarget;

        /// <summary>
        /// Initializes from save a new instance of the <see cref="CaptureBorderPlanets"/> class.
        /// </summary>
        public CaptureBorderPlanets(Campaign campaign, War war) : base(campaign, war) => CreateSteps();

        public CaptureBorderPlanets(CampaignType campaignType, War war) : base(campaignType, war)
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
            AddTargetSystems(OwnerWar.GetTheirNearSystems().UniqueExclude(OwnerWar.ContestedSystems));
            AddTargetSystems(OwnerWar.GetTheirBorderSystems().UniqueExclude(OwnerWar.ContestedSystems));
            if (TargetSystems.IsEmpty) return GoalStep.GoalFailed;

            return GoalStep.GoToNextStep;
        }

        GoalStep AttackSystems()
        {
            if (HaveConqueredTargets()) return GoalStep.GoalComplete;
            
            var fleets = Owner.AllFleetsReady();
            float strength = fleets.AccumulatedStrength;

            var tasks = new WarTasks(Owner, Them);
            
            foreach(var system in TargetSystems)
            {
                if (HaveConqueredTarget(system)) continue;

                float defense = Owner.GetEmpireAI().ThreatMatrix.PingHostileStr(system.Position, Owner.GetProjectorRadius(), Owner);
                strength -= defense * 2;

                tasks.StandardAssault(system, OwnerWar.Priority(), 2);
                if (strength < 0) break;
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