using System;
using Microsoft.Xna.Framework;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public class CaptureAllPlanets : AttackSystems
    {
        /// <summary>
        /// Initializes from save a new instance of the <see cref="CaptureAllPlanets"/> class.
        /// </summary>
        public CaptureAllPlanets(Campaign campaign, War war) : base(campaign, war) => CreateSteps();

        public CaptureAllPlanets(CampaignType campaignType, War war) : base(campaignType, war)
        {
            CreateSteps();
        }

        protected override GoalStep SetupTargets()
        {
            var targets = new Array<SolarSystem>();
            targets.AddRange(Them.GetOwnedSystems().Filter(s => s.IsExploredBy(Owner)));
            return SetTargets(targets);
        }
    }
}