using System;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public abstract class AttackSystems : Campaign
    {
        /// <summary>
        /// Initializes from save a new instance of the <see cref="Capture"/> class.
        /// </summary>
        protected AttackSystems(Campaign campaign, War war) : base(campaign, war) => CreateSteps();

        protected AttackSystems(CampaignType campaignType, War war) : base(campaignType, war)
        {
            CreateSteps();
        }

        protected void CreateSteps()
        {
            Steps = new Func<GoalStep>[]
            {
               SetupTargets,
               SetupRallyPoint,
               AttackSystems,
               AssesCampaign,
               CustomExtension
            };
        }

        protected abstract GoalStep SetupTargets();
        protected GoalStep AssesCampaign()
        {
            return GoalStep.GoToNextStep;
        }
        protected abstract GoalStep CustomExtension();

        protected GoalStep SetTargets(Array<SolarSystem> targets)
        {
            CreateTargetList(targets);

            if (TargetSystems.IsEmpty)
                return GoalStep.TryAgain;

            UpdateTargetSystemList();

            return GoalStep.GoToNextStep;
        }
    }
}
