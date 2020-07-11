using System;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public sealed class Defense : AttackSystems
    {
        /// <summary>
        /// Initializes from save a new instance of the <see cref="Defense"/> class.
        /// </summary>
        public Defense(Campaign campaign, Theater war) : base(campaign, war) => CreateSteps();

        public Defense(CampaignType campaignType, Theater war) : base(campaignType, war) => CreateSteps();

        protected override GoalStep SetupTargets()
        {
            if (Them.isFaction) return GoalStep.TryAgain;

            var targets = new Array<SolarSystem>();
            targets.AddRange(UniverseScreen.SolarSystemList.Filter(s => {
                if (s.OwnerList.Count < 1) return false;
                bool isExplored = s.IsExploredBy(Owner);
                bool theyAreThere = s.OwnerList.Contains(Them);
                if (Owner == Them)
                {
                    theyAreThere = false;
                    foreach (var empire in s.OwnerList)
                    {
                        if (Owner.IsEmpireHostile(empire))
                        {
                            theyAreThere = true;
                            break;
                        }
                    }
                }
                bool inAO = false;
                if (isExplored && theyAreThere)
                    inAO = s.Position.InRadius(OwnerTheater.TheaterAO);
                return inAO;

            }));
            //targets.AddRange(OwnerWar.GetTheirBorderSystems());
            return SetTargets(targets);
        }
    }
}