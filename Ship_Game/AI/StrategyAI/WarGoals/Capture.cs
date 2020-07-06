using System;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public class Capture : Campaign
    {
        SolarSystem CurrentTarget;

        /// <summary>
        /// Initializes from save a new instance of the <see cref="Capture"/> class.
        /// </summary>
        public Capture(Campaign campaign, War war) : base(campaign, war) => CreateSteps();

        public Capture(CampaignType campaignType, War war) : base(campaignType, war)
        {
            CreateSteps();
        }

        void CreateSteps()
        {
            Steps = new Func<GoalStep>[] 
            {
               VerifyTargets,
               SetupRallyPoint,
               AttackSystems
            };
        }

        GoalStep VerifyTargets()
        {
            UpdateTargetSystemList();
            if (TargetSystems.IsEmpty) return GoalStep.GoalComplete;
            return GoalStep.GoToNextStep;
        }

        GoalStep SetupRallyPoint()
        {
            return SetupRallyPoint(TargetSystems);
        }

        GoalStep AttackSystems()
        {
            Array<SolarSystem> currentTargets = new Array<SolarSystem>();
            CreateTargetList(currentTargets);

            if (Owner.GetOwnedSystems().Count == 0) return GoalStep.GoalFailed;
            UpdateTargetSystemList();
            if (HaveConqueredTargets() || currentTargets.IsEmpty) return GoalStep.GoalComplete;
            if (RallyAO == null || RallyAO.CoreWorld?.Owner != Owner) return GoalStep.RestartGoal;

            AttackSystemsInList(currentTargets);
            return GoalStep.TryAgain;
        }
    }
}