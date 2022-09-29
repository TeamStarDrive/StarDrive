using System;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class RemnantInit : Goal
    {
        [StarDataConstructor]
        public RemnantInit(Empire owner) : base(GoalType.RemnantInit, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                CreateGuardians,
                SendExplorationFleetsAstronomers
            };
        }

        GoalStep CreateGuardians()
        {
            foreach (SolarSystem solarSystem in Owner.Universum.Systems)
            {
                foreach (Planet p in solarSystem.PlanetList)
                {
                    Owner.Remnants.GenerateRemnantPresence(p);
                }
            }

            Owner.Universum.Player.AI.ThreatMatrix.UpdateAllPins(Owner.Universum.Player);
            return GoalStep.GoToNextStep;
        }

        GoalStep SendExplorationFleetsAstronomers()
        {
            foreach (Empire e in EmpireManager.MajorEmpires)
            {
                var planets = Owner.Universum.Planets.Filter(p => p.IsExploredBy(e));
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