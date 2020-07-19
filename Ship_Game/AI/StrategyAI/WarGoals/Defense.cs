using System;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public sealed class Defense : AttackSystems
    {
        /// <summary>
        /// Initializes from save a new instance of the <see cref="Defense"/> class.
        /// </summary>
        public Defense(Campaign campaign, Theater war) : base(campaign, war) => CreateSteps();

        public Defense(CampaignType campaignType, Theater war) : base(campaignType, war) => CreateSteps();

        protected override GoalStep SetupTargets()
        {
            if (Them.isFaction) return GoalStep.TryAgain;
            if (Them != Owner)  return SetTargets(SystemsWithThem());

            var systems = new Array<SolarSystem>();
            Array<SolarSystem> AOSystems = OwnerTheater.GetSystems();
            for (int i = 0; i < AOSystems.Count; i++)
            {
                var s = AOSystems[i];
                foreach (var owner in s.OwnerList)
                {
                    if (Owner.GetRelations(owner)?.AtWar != true) continue;
                    systems.Add(s);
                    break;
                }
            }
            return SetTargets(systems);
        }
    }
}