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
            foreach (SolarSystem solarSystem in Owner.Universe.Systems)
            {
                foreach (Planet p in solarSystem.PlanetList)
                {
                    Owner.Remnants.GenerateRemnantPresence(p);
                }

                foreach (Empire e in Owner.Universe.MajorEmpires)
                {
                    if (solarSystem.IsFullyExploredBy(e) && solarSystem.ShipList.Count > 0)
                        e.AI.ThreatMatrix.UpdateRemanantPresenceAstronomers(solarSystem);
                }
            }

            Owner.Universe.Player.AI.ThreatMatrix.Update();
            return GoalStep.GoToNextStep;
        }

        GoalStep SendExplorationFleetsAstronomers()
        {
            foreach (Empire e in Owner.Universe.MajorEmpires)
            {
                var planets = Owner.Universe.Planets.Filter(p => p.IsExploredBy(e));
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