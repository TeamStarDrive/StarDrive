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
                var planets = Empire.Universe.PlanetsDict.Values.Filter(p => p.IsExploredBy(e));
                for (int i = 0; i < planets.Length; ++i)
                {
                    Planet p = planets[i];
                    e.TrySendInitialFleets(p);
                }
            }

            return GoalStep.GoalComplete;
        }
    }
}