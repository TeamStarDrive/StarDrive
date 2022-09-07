using System;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class RemnantInit : Goal
    {
        [StarDataConstructor]
        public RemnantInit(int id, UniverseState us)
            : base(GoalType.RemnantInit, id, us)
        {
            Steps = new Func<GoalStep>[]
            {
                CreateGuardians,
                SendExplorationFleetsAstronomers
            };
        }
        public RemnantInit(Empire owner)
            : this(owner.Universum.CreateId(), owner.Universum)
        {
            empire = owner;
        }

        GoalStep CreateGuardians()
        {
            foreach (SolarSystem solarSystem in empire.Universum.Systems)
            {
                foreach (Planet p in solarSystem.PlanetList)
                {
                    empire.Remnants.GenerateRemnantPresence(p);
                }
            }

            EmpireManager.Player.GetEmpireAI().ThreatMatrix.UpdateAllPins(EmpireManager.Player);
            return GoalStep.GoToNextStep;
        }

        GoalStep SendExplorationFleetsAstronomers()
        {
            foreach (Empire e in EmpireManager.MajorEmpires)
            {
                var planets = empire.Universum.Planets.Filter(p => p.IsExploredBy(e));
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