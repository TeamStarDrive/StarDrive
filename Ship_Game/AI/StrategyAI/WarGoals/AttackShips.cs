using System;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    /*
    public abstract class AttackShips : Campaign
    {
        /// <summary>
        /// Initializes from save a new instance of the <see cref="Capture"/> class.
        /// </summary>
        protected AttackShips(Campaign campaign, Theater war) : base(campaign, war) { }

        protected AttackShips(CampaignType campaignType, Theater war) : base(campaignType, war) { }

        protected virtual void CreateSteps()
        {
            Steps = new Func<GoalStep>[]
            {
               SetupShipTargets,
               SetupRallyPoint,
               AttackSystems,
               AssesCampaign
            };
        }

        protected abstract GoalStep SetupShipTargets();
        protected virtual GoalStep AssesCampaign() => GoalStep.RestartGoal;

        protected virtual GoalStep SetTargets(Array<SolarSystem> targets)
        {
            CreateTargetSystemList(targets);

            if (TargetSystems.IsEmpty)
                return GoalStep.TryAgain;

            UpdateTargetSystemList();

            return GoalStep.GoToNextStep;
        }
    }*/
}