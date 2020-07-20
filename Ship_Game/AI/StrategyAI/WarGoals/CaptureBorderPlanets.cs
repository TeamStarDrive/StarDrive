using System;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public class CaptureBorderPlanets : AttackSystems
    {
        /// <summary>
        /// Initializes from save a new instance of the <see cref="CaptureBorderPlanets"/> class.
        /// </summary>
        public CaptureBorderPlanets(Campaign campaign, Theater war) : base(campaign, war) => CreateSteps();

        public CaptureBorderPlanets(CampaignType campaignType, Theater war) : base(campaignType, war)
        {
            CreateSteps();
        }

        protected override GoalStep SetupTargets() => SetTargets(SystemsWithThem());
    }
}