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

        protected override GoalStep SetupTargets()
        {
            var targets = new Array<SolarSystem>();
            targets.AddRange(OwnerWar.GetTheirNearSystems().Filter(s=>
            {
                bool isExplored = s.IsExploredBy(Owner);
                bool inAO = false;
                if (isExplored)
                    inAO = s.Position.InRadius(OwnerTheater.TheaterAO);
                return isExplored && inAO;
            }));
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