using System;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public sealed class Defense : AttackSystems
    {
        /// <summary>
        /// Initializes from save a new instance of the <see cref="Defense"/> class.
        /// </summary>
        public Defense(Campaign campaign, War war) : base(campaign, war) => CreateSteps();

        public Defense(CampaignType campaignType, War war) : base(campaignType, war) => CreateSteps();

        protected override GoalStep SetupTargets()
        {
            if (Them.isFaction) return GoalStep.TryAgain;

            var targets = new Array<SolarSystem>();
            targets.AddRange(Owner.GetOwnedSystems().Filter(s => s.OwnerList.Contains(Them)));
            targets.AddRange(OwnerWar.GetTheirBorderSystems());
            return SetTargets(targets);
        }

        protected override GoalStep CustomExtension()
        {
            return GoalStep.RestartGoal;
        }
    }
}