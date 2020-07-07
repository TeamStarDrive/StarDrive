using System;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public class Capture : AttackSystems
    {
        /// <summary>
        /// Initializes from save a new instance of the <see cref="Capture"/> class.
        /// </summary>
        public Capture(Campaign campaign, War war) : base(campaign, war) => CreateSteps();

        public Capture(CampaignType campaignType, War war) : base(campaignType, war)
        {
            CreateSteps();
        }

        protected override GoalStep SetupTargets()
        {
            var targets = new Array<SolarSystem>();
            targets.AddRange(OwnerWar.GetTheirBorderSystems());
            return SetTargets(targets);
        }
    }
}