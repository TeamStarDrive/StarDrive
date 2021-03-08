namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public class CaptureAllPlanets : AttackSystems
    {
        /// <summary>
        /// Initializes from save a new instance of the <see cref="CaptureAllPlanets"/> class.
        /// </summary>
        public CaptureAllPlanets(Campaign campaign, Theater war) : base(campaign, war) => CreateSteps();

        public CaptureAllPlanets(CampaignType campaignType, Theater war) : base(campaignType, war)
        {
            CreateSteps();
        }

        protected override GoalStep SetupTargets()
        {
            return SetTargets(SystemsWithThem());
        }
    }
}