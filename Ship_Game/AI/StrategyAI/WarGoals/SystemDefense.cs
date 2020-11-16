using System;
using Microsoft.Xna.Framework;
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
            var ownedSystems = Owner.GetOwnedSystems();

            foreach (var theater in Owner.AllActiveWarTheaters)
            {
                if (theater.RallyAO?.CoreWorld?.ParentSystem != null)
                {
                    systems.Add(theater.RallyAO.CoreWorld.ParentSystem);
                    priorities.Add(0);
                }

                if (theater.RallyAO?.CoreWorld?.ParentSystem.OwnerList.Count > 1)
                {
                    systems.Add(theater.RallyAO.CoreWorld.ParentSystem);
                    priorities.Add(0);
                }

                foreach (var planet in theater.TheaterAO.GetPlanets())
                {
                    if (planet.ParentSystem.OwnerList.Contains(Them))
                    {
                        systems.Add(planet.ParentSystem);
                        priorities.Add(0);
                    }
                    if (planet.ParentSystem.OwnerList.Contains(Owner))
                    {
                        systems.Add(planet.ParentSystem);
                        priorities.Add(OwnerTheater.Priority);
                    }
                }
            }

            var pinsNotInSystems = new Array<Pin>();
            for (int i = 0; i < pins.Length; i++)
            {
                var pin = pins[i];
                if (pin.Ship?.Active != true) continue;
                if (pin.System == null)
                {
                    if (pin.Ship?.IsPlatformOrStation == true)
                        pinsNotInSystems.AddUnique(pin);
                    continue;
                }

                systems.AddUnique(pin.System);
                int systemIndex = systems.IndexOf(pin.System);
                int priority = pin.Strength > 0 ? 0 : OwnerTheater.Priority;
                if (priorities.Count < systems.Count)
                {
                    priorities.Add(priority);
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

            var ourSystems = Owner.GetOwnedSystems().ToArray();

            foreach (var s in ssps)
            {
                var system = ourSystems.FindClosestTo(s);
                if (system != null)
                {
                    systems.Add(system);
                    priorities.Add(0);
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

            if (systems.IsEmpty)
            {
                foreach (var system in borders)
                {
                    systems.Add(system);
                    priorities.Add((int)system.WarValueTo(Owner));
                }
            }

            foreach (var pin in pinsNotInSystems)
                AttackArea(pin.Position, 100000, pin.Strength);

            DefendSystemsInList(systems, priorities);

            return GoalStep.GoToNextStep;
        }
    }
}
