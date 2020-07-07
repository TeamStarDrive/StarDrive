using System;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public class CaptureBorderPlanets : AttackSystems
    {
        /// <summary>
        /// Initializes from save a new instance of the <see cref="CaptureBorderPlanets"/> class.
        /// </summary>
        public CaptureBorderPlanets(Campaign campaign, War war) : base(campaign, war) => CreateSteps();

        public CaptureBorderPlanets(CampaignType campaignType, War war) : base(campaignType, war)
        {
            CreateSteps();
        }

        protected override GoalStep SetupTargets()
        {
            var targets = new Array<SolarSystem>();
            targets.AddRange(OwnerWar.GetTheirNearSystems());
            targets.AddRange(OwnerWar.GetTheirBorderSystems());
            return SetTargets(targets);
        }
    }
}