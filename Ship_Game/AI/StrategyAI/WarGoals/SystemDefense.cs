using Ship_Game.Fleets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Ships;

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
            //var fleets    = new Array<Fleet>();
            var pins      = OwnerTheater.GetPins();
            //var ships     = new Array<Ship>();
            var systems   = new Array<SolarSystem>();
            var strengths = new Array<int>();
            foreach (var pin in pins)
            {
                if (!pin.InBorders || pin.Ship?.Active != true) continue;
                if (pin.Ship.System == null) continue;
                //if (pin.Ship.fleet == null) continue;
                //ships.AddUnique(pin.Ship);
                //fleets.AddUnique(pin.Ship.fleet);
                systems.AddUnique(pin.Ship.System);
                int systemIndex = systems.IndexOf(pin.Ship.System);
                if (strengths.Count < systems.Count)
                {
                    strengths.Add((int)pin.Strength);
                }
                else
                {
                    strengths[systemIndex] += (int)pin.Strength;
                }
            }
            if (systems.IsEmpty) return GoalStep.GoToNextStep;
            DefendSystemsInList(systems, strengths);
            return GoalStep.GoToNextStep;
        }
    }
}
