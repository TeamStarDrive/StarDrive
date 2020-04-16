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
                SetupRallyPoint,
                AttackSystems
            };
        }

        GoalStep AttackSystems()
        {
            if (HaveConqueredTargets()) return GoalStep.GoalComplete;

            var fleets = Owner.AllFleetsReady();
            float strength = fleets.AccumulatedStrength;

            var tasks = new WarTasks(Owner, Them);
            foreach(var system in TargetSystems)
            {
                float defense = Owner.GetEmpireAI().ThreatMatrix.PingHostileStr(system.Position, Owner.GetProjectorRadius(), Owner);
                strength -= defense;

                if (HaveConqueredTarget(system)) continue;
                tasks.StandardAssault(system, OwnerWar.Priority() + 5);
                
                if (strength < 0) break; 
            }
            Owner.GetEmpireAI().AddPendingTasks(tasks.GetNewTasks());
            return GoalStep.RestartGoal;
        }

        bool HaveConqueredTargets()
        {
            foreach(var system in TargetSystems)
            {
                if (!HaveConqueredTarget(system)) return false;
            }
            return true;
        }

        bool HaveConqueredTarget(SolarSystem system) => !system.OwnerList.Contains(Them);
    }
}