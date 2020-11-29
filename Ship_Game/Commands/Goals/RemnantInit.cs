using System;
using Ship_Game.AI;

namespace Ship_Game.Commands.Goals
{
    public class RemnantInit : Goal
    {
        public const string ID = "RemnantInit";
        public override string UID => ID;

        public RemnantInit() : base(GoalType.RemnantInit)
        {
            Steps = new Func<GoalStep>[]
            {
                CreateGuardians,
                SendExplorationFleetsAstronomers
            };
        }
        public RemnantInit(Empire owner) : this()
        {
            empire = owner;
        }

        GoalStep CreateGuardians()
        {
            foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
            {
                foreach (Planet p in solarSystem.PlanetList)
                {
                    empire.Remnants.GenerateRemnantPresence(p);
                }
            }

            return GoalStep.GoToNextStep;
        }

        GoalStep SendExplorationFleetsAstronomers()
        {
            foreach (Empire e in EmpireManager.MajorEmpires.Filter(e => e.data.Traits.BonusExplored > 0))
            {
                Planet homeWorld             = e.GetPlanets()[0];
                var solarSystems             = Empire.Universe.SolarSystemDict.Values;
                SolarSystem[] closestSystems = solarSystems.Sorted(system => homeWorld.Center.Distance(system.Position));
                int numExplored              = solarSystems.Count >= 20 ? e.data.Traits.BonusExplored : solarSystems.Count;

                for (int i = 0; i < numExplored; ++i)
                {
                    SolarSystem ss = closestSystems[i];
                    ss.SetExploredBy(e);
                    foreach (Planet planet in ss.PlanetList)
                    {
                        planet.SetExploredBy(e);
                        Empire.TrySendInitialFleets(planet, e);
                    }
                }
            }

            return GoalStep.GoalComplete;
        }
    }
}