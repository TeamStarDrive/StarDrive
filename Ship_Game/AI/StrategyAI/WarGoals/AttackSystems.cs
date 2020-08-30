﻿using System;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public abstract class AttackSystems : Campaign
    {
        protected Array<SolarSystem> SystemsWithThem()
        {
            if (OwnerWar.WarType != WarType.EmpireDefense)
                return new Array<SolarSystem>(OwnerTheater.GetSystems().Filter(s =>
                    s.IsExploredBy(Owner) && s.OwnerList.Contains(Them)));
            return new Array<SolarSystem>(OwnerTheater.GetSystems().Filter(s =>
                s.IsExploredBy(Owner) && s.OwnerList.Count > 1 && s.OwnerList.Contains(Owner)));
        }
        /// <summary>
        /// Initializes from save a new instance of the <see cref="Capture"/> class.
        /// </summary>
        protected AttackSystems(Campaign campaign, Theater theater) : base(campaign, theater){}

        protected AttackSystems(CampaignType campaignType, Theater theater) : base(campaignType, theater){}

        protected void CreateSteps()
        {
            Steps = new Func<GoalStep>[]
            {
               SetupTargets,
               SetupRallyPoint,
               AttackSystems,
               AssesCampaign
            };
        }

        protected abstract GoalStep SetupTargets();
        protected virtual GoalStep AssesCampaign()
        {
            if (!TargetSystems.Any(s=> s.OwnerList.Contains(Them)))
            {
                OwnerTheater.RemoveCampaign(this);
                return GoalStep.GoalComplete;
            }
            return GoalStep.RestartGoal;
        }

        protected GoalStep SetTargets(Array<SolarSystem> targets)
        {
            CreateTargetSystemList(targets);


            //if (TargetSystems.IsEmpty)
            //    return GoalStep.TryAgain;

            //UpdateTargetSystemList();

            return GoalStep.GoToNextStep;
        }
    }
}
