using Ship_Game.Fleets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Ships;
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
            Pin[] pins           = OwnerTheater.GetPins();
            //var ships          = new Array<Ship>();
            var systems          = new Array<SolarSystem>();
            var strengths        = new Array<int>();
            var ownedSystems     = Owner.GetOwnedSystems();
            var pinsNotInSystems = new Array<Pin>();
            for (int i = 0; i < pins.Length; i++)
            {
                var pin = pins[i];
                if (!pin.InBorders || pin.Ship?.Active != true) continue;
                if (pin.System == null) continue;

                systems.AddUnique(pin.System);
                int systemIndex = systems.IndexOf(pin.System);
                if (strengths.Count < systems.Count)
                {
                    strengths.Add((int) pin.Strength);
                }
                else
                {
                    strengths[systemIndex] += (int) pin.Strength;
                }
            }

            DefendSystemsInList(systems, strengths);
            if (systems.IsEmpty && pinsNotInSystems.NotEmpty)
            {
                var pin = pinsNotInSystems.FindMax(p => p.Strength);
                AttackArea(pin.Position, 100000, pin.Strength);
            }
            return GoalStep.GoToNextStep;
        }
    }
}
