using System;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public class Defense : Campaign
    {
        SolarSystem CurrentTarget;

        /// <summary>
        /// Initializes from save a new instance of the <see cref="Defense"/> class.
        /// </summary>
        public Defense(Campaign campaign, War war) : base(campaign, war) => CreateSteps();

        public Defense(CampaignType campaignType, War war) : base(campaignType, war)
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
            AddTargetSystems(OwnerWar.ContestedSystems);
            if (TargetSystems.IsEmpty) return GoalStep.GoalComplete;
            return GoalStep.GoToNextStep;
        }

        GoalStep AttackSystems()
        {
            if (HaveConqueredTargets()) return GoalStep.GoalComplete;

            var tasks = new WarTasks(Owner, Them);
            foreach(var system in TargetSystems)
            {
                if (HaveConqueredTarget(system)) continue;
                tasks.StandardAssault(system, 0, 2);
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