using System;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public class CaptureBorderPlanets : Campaign
    {
        SolarSystem CurrentTarget;

        /// <summary>
        /// Initializes from save a new instance of the <see cref="CaptureBorderPlanets"/> class.
        /// </summary>
        public CaptureBorderPlanets(Campaign campaign, War war) : base(campaign, war) => CreateSteps();

        public CaptureBorderPlanets(CampaignType campaignType, War war) : base(campaignType, war)
        {
            CreateSteps();
        }

        void CreateSteps()
        {
            Steps = new Func<GoalStep>[] 
            {
                SetTargets,
                SetupRallyPoint,
                AttackSystems
            };
        }

        GoalStep SetTargets()
        {
            AddTargetSystems(OwnerWar.GetTheirNearSystems());
            AddTargetSystems(OwnerWar.GetTheirBorderSystems());
            if (TargetSystems.IsEmpty) return GoalStep.GoalComplete;

            return GoalStep.GoToNextStep;
        }

        GoalStep SetupRallyPoint() => SetupRallyPoint(TargetSystems);

        GoalStep AttackSystems()
        {
            Array<SolarSystem> currentTargets = new Array<SolarSystem>();
            CreateTargetList(currentTargets);

            if (Owner.GetOwnedSystems().Count == 0) return GoalStep.GoalFailed;
            UpdateTargetSystemList();
            if (HaveConqueredTargets() || currentTargets.IsEmpty)     return GoalStep.RestartGoal;
            if (RallyAO == null || RallyAO.CoreWorld?.Owner != Owner) return GoalStep.RestartGoal;

            AttackSystemsInList(currentTargets);
            return GoalStep.TryAgain;
        }
    }
}