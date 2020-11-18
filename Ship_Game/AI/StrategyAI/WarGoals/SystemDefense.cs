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
            int basePriority = OwnerTheater.Priority * 2;
            int important = basePriority - 1;
            int normal = basePriority;
            int casual = basePriority + 1;
            int unImportant = basePriority + 2;

            var ownedSystems = Owner.GetOwnedSystems();

            //foreach (var theater in Owner.AllActiveWarTheaters)
            //{
            //    var rallySystem = theater.RallyAO?.CoreWorld?.ParentSystem;

            //    if (rallySystem != null)
            //    {
            //        systems.Add(rallySystem);
            //        int priority = casual - rallySystem.OwnerList.Count;
            //        priorities.Add(priority);
            //    }

            //    foreach (var planet in theater.TheaterAO.GetOurPlanets())
            //    {
            //        int priority = casual - planet.ParentSystem.OwnerList.Count;

            //        if (Owner.RallyPoints.Contains(planet))
            //        {
            //            priority -= 1;
            //        }
            //        if (planet.ParentSystem.OwnerList.Contains(Them))
            //        {
            //            systems.Add(planet.ParentSystem);
            //            priorities.Add(priority);
            //        }
            //        else if (planet.ParentSystem.OwnerList.Contains(Owner))
            //        {
            //            systems.Add(planet.ParentSystem);
            //            priorities.Add(priority);
            //        }
            //    }
            //}

            //foreach(var planet in Owner.RallyPoints)
            //{
            //    var system = planet.ParentSystem;
            //    systems.Add(system);
            //    int priority = casual - system.OwnerList.Count;
            //    priorities.Add(priority);
            //}

            //var pinsNotInSystems = new Array<Pin>();
            //for (int i = 0; i < pins.Length; i++)
            //{
            //    var pin = pins[i];
            //    if (pin.Ship?.Active != true) continue;
            //    if (pin.System == null)
            //    {
            //        if (pin.Ship?.IsPlatformOrStation == true)
            //            pinsNotInSystems.AddUnique(pin);
            //        continue;
            //    }

            //    systems.AddUnique(pin.System);
            //    int systemIndex = systems.IndexOf(pin.System);
            //    int priority = pin.Strength > 0 ? normal : unImportant;
            //    if (priorities.Count < systems.Count)
            //    {
            //        priorities.Add(priority);
            //    }
            //}

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
                    priorities.Add(normal);
                }
            }

            //Array<SolarSystem> borders = new Array<SolarSystem>();
            //foreach (var e in enemies)
            //{
            //    var border = Owner.GetBorderSystems(e, true);
            //    var aoSystems = OwnerTheater.TheaterAO.GetAoSystems();
            //    var borderSystems = border.Filter(s => aoSystems.Contains(s));
            //    borders.AddRange(borderSystems);
            //}

            //if (systems.IsEmpty)
            //{
            //    foreach (var system in borders)
            //    {
            //        systems.Add(system);
            //        priorities.Add(unImportant);
            //    }
            //}

            //foreach (var pin in pinsNotInSystems)
            //    AttackArea(pin.Position, 100000, pin.Strength);

            DefendSystemsInList(systems, priorities);

            return GoalStep.GoToNextStep;
        }
    }
}
