using System;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public class Capture : AttackSystems
    {
        /// <summary>
        /// Initializes from save a new instance of the <see cref="Capture"/> class.
        /// </summary>
        public Capture(Campaign campaign, Theater theater) : base(campaign, theater) => CreateSteps();

        public Capture(CampaignType campaignType, Theater theater) : base(campaignType, theater)
        {
            CreateSteps();
        }

        protected override GoalStep SetupTargets()
        {
            var targets = new Array<SolarSystem>();
            targets.AddRange(OwnerWar.GetTheirBorderSystems().Filter(s=>
            {
                bool isExplored = s.IsExploredBy(Owner);
                bool inAO = false;
                if (isExplored)
                    inAO = s.Position.InRadius(OwnerTheater.TheaterAO);
                return isExplored && inAO;
            }));
            return SetTargets(targets);
        }
    }
}