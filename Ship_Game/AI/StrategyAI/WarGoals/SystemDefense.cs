using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
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
                if (pin.Ship?.Active != true || !pin.Ship.IsInBordersOf(Owner)) continue;
                if (pin.System == null) continue;

                systems.AddUnique(pin.System);
                int systemIndex = systems.IndexOf(pin.System);
                if (strengths.Count < systems.Count)
                {
                    strengths.Add((int)pin.Strength);
                }
                else
                {
                    strengths[systemIndex] += (int)pin.Strength;
                }
            }

            var enemies = Owner == Them ? EmpireManager.GetEnemies(Owner) : new Array<Empire> { Them };
            var ssps = Owner.GetProjectors().Filter(s =>
            {
                foreach (var e in enemies)
                    if (s.HasSeenEmpires.KnownBy(e)) 
                        return true;
                return false;
            });

            var ourSystems = Owner.GetCopyOfOwnedSystems();//.ToArray();

            foreach (var s in ssps)
            {
                var system = ourSystems.FindClosestTo(s);
                if (system != null)
                {
                    systems.Add(system);
                    strengths.Add((int)system.WarValueTo(Owner) * 10);
                }
            }

            Array<SolarSystem> borders = new Array<SolarSystem>();
            foreach (var e in enemies)
            {
                var border = Owner.GetBorderSystems(e, true);
                var aoSystems = OwnerTheater.TheaterAO.GetAoSystems();
                var borderSystems = border.Filter(s => aoSystems.Contains(s));
                borders.AddRange(borderSystems);
            }

            foreach (var system in borders)
            {
                systems.Add(system);
                strengths.Add((int)system.WarValueTo(Owner) );
            }
            

            DefendSystemsInList(systems, strengths);
            //if (systems.IsEmpty && pinsNotInSystems.NotEmpty)
            //{
            //    var pin = pinsNotInSystems.FindMax(p => p.Strength);
            //    AttackArea(pin.Position, 100000, pin.Strength);
            //}
            return GoalStep.GoToNextStep;
        }
    }
}
