using System;
using Microsoft.Xna.Framework;
using Ship_Game.Empires.DataPackets;
using static Ship_Game.AI.ThreatMatrix;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public sealed class SystemDefense : AttackShips
    {
        /// <summary>
        /// Initializes from save a new instance of the <see cref="Defense"/> class.
        /// </summary>
        public SystemDefense(Campaign campaign, Theater war) : base(campaign, war) => CreateSteps();

        public SystemDefense(CampaignType campaignType, Theater war) : base(campaignType, war) => CreateSteps();

        protected override void CreateSteps()
        {
            Steps = new Func<GoalStep>[]
            {
               SetupShipTargets,
               AssesCampaign
            };
        }

        protected override GoalStep SetupShipTargets()
        {
            //var fleets         = new Array<Fleet>();
            Pin[] pins = OwnerTheater.GetPins();
            //var ships          = new Array<Ship>();
            var systems = new Array<SolarSystem>();
            var priorities = new Array<int>();
            int basePriority = OwnerTheater.Priority * 2;
            int important = basePriority - 1;
            int normal = basePriority;
            int casual = basePriority + 1;
            int unImportant = basePriority + 2;

            var ownedSystems = Owner.GetOwnedSystems();
            if (OwnerWar.WarType == WarType.EmpireDefense)
            {
                foreach (IncomingThreat threatenedSystem in Owner.SystemWithThreat)
                {
                    if (threatenedSystem.ThreatTimedOut) continue;
                    systems.Add(threatenedSystem.TargetSystem);
                    var priority = basePriority / threatenedSystem.TargetSystem.PlanetList.Sum(p => p.Owner == Owner ? p.Level : 0).LowerBound(1);
                    Tasks.StandardSystemDefense(threatenedSystem.TargetSystem, priority, threatenedSystem.Strength, 1);
                }
            }


            foreach (var system in Owner.GetOwnedSystems())
            {
                if (!system.HostileForcesPresent(Owner)) continue;
                var priority = basePriority / system.PlanetList.Sum(p => p.Owner == Owner ? p.Level : 0).LowerBound(1);
                var strength = system.GetKnownStrengthHostileTo(Owner);
                Tasks.StandardSystemDefense(system, priority, strength, 1);
            }

            return GoalStep.GoToNextStep;
           

        }
    }
}
