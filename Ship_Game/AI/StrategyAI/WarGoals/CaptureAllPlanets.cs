using System;
using Microsoft.Xna.Framework;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public class CaptureAllPlanets : Campaign
    {
        SolarSystem CurrentTarget;
        Array<SolarSystem> CurrentTargets;

        /// <summary>
        /// Initializes from save a new instance of the <see cref="CaptureAllPlanets"/> class.
        /// </summary>
        public CaptureAllPlanets(Campaign campaign, War war) : base(campaign, war) => CreateSteps();

        public CaptureAllPlanets(CampaignType campaignType, War war) : base(campaignType, war)
        {
            CreateSteps();
        }

        void CreateSteps()
        {
            Steps = new Func<GoalStep>[] 
            {
                AddTargets,
                SetupRallyPoint,
                SetTargets,
                AttackSystems
            };
        }

        GoalStep AddTargets()
        {
            AddTargetSystems(Them.GetOwnedSystems().Filter(s => s.IsExploredBy(Owner)));

            AddTargetSystems(OwnerWar.GetHistoricLostSystems().Filter(s => s.OwnerList.Contains(Them) && !s.OwnerList.Contains(Owner)));

            CurrentTargets = new Array<SolarSystem>();
            CreateTargetList(CurrentTargets);
            return GoalHasValidTargets();
        }

        GoalStep SetupRallyPoint() => SetupRallyPoint(CurrentTargets);

        GoalStep GoalHasValidTargets()
        {
            if (TargetSystems.IsEmpty)             return GoalStep.TryAgain;
            if (HaveConqueredTargets())            return GoalStep.TryAgain;
            if (Owner.GetOwnedSystems().Count < 1) return GoalStep.GoalFailed;

            return GoalStep.GoToNextStep;
        }

        GoalStep SetTargets()
        {
            return CreateTargetList(CurrentTargets);
        }

        GoalStep AttackSystems()
        {
            if (Owner.GetOwnedSystems().Count == 0) return GoalStep.GoalFailed;
            UpdateTargetSystemList();
            UpdateTargetSystemList(CurrentTargets);
            if (HaveConqueredTargets() || CurrentTargets.IsEmpty) 
                return GoalStep.RestartGoal;
            if (RallyAO == null || RallyAO.CoreWorld?.Owner != Owner) return GoalStep.RestartGoal;

            AttackSystemsInList(CurrentTargets);
            return GoalStep.TryAgain;
        }
    }
}