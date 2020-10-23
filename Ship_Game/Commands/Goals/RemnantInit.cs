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
                CreateGuardians
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

            return GoalStep.GoalComplete;
        }
    }
}