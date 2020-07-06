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

        public Defense(CampaignType campaignType, War war) : base(campaignType, war) => CreateSteps();

        void CreateSteps()
        {
            IsCoreCampaign = false;
            Steps = new Func<GoalStep>[]
            {
                SetTargets,
                SetupRallyPoint,
                AttackSystems
            };
        }

        GoalStep SetTargets()
        {
            if (Them.isFaction) return GoalStep.TryAgain;
            AddTargetSystems(Owner.GetOwnedSystems().Filter(s=> s.OwnerList.Contains(Them)));
            AddTargetSystems(OwnerWar.GetTheirBorderSystems());
            if (TargetSystems.IsEmpty)
                return GoalStep.TryAgain;
            return GoalStep.GoToNextStep;
        }

        GoalStep SetupRallyPoint() => SetupRallyPoint(TargetSystems);

        GoalStep AttackSystems()
        {
            if (HaveConqueredTargets()) return GoalStep.RestartGoal;

            var tasks = new WarTasks(Owner, Them);
            foreach(var system in TargetSystems)
            {
                if (HaveConqueredTarget(system)) continue;
                tasks.StandardAssault(system, -5);
            }
            Owner.GetEmpireAI().AddPendingTasks(tasks.GetNewTasks());
            return GoalStep.RestartGoal;
        }
    }
}