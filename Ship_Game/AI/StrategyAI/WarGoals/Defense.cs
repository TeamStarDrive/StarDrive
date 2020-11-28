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
            if (Them != Owner)  return SetTargets(SystemsWithThem());

            var systems = new Array<SolarSystem>();
            Array<SolarSystem> aoSystems = OwnerTheater.GetSystems();

            //if (OwnerTheater.TheaterAO.ThreatLevel > 0)
            //{
            //    for (int i = 0; i < aoSystems.Count; i++)
            //    {
            //        var s = aoSystems[i];
            //        float str = Owner.GetEmpireAI().ThreatMatrix.PingHostileStr(s.Position, s.Radius * 1.5f, Owner);
            //        if (str > 0) systems.AddUniqueRef(s);
            //        else 
            //            TargetSystems.RemoveRef(s);
            //    }
            //}
            return SetTargets(systems);
        }
    }
}