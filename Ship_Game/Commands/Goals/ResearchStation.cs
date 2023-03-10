using System;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class ResearchStation : Goal
    {
        [StarData] public SolarSystem TargetSystem;
        [StarData] public override Planet TargetPlanet { get; set; }
        [StarData] public sealed override Ship TargetShip { get; set; }
        Ship ResearchStationOrbital => TargetShip;

        [StarDataConstructor]
        public ResearchStation(Empire owner) : base(GoalType.ResearchStation, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                Research
            };
        }

        public ResearchStation(Empire owner, SolarSystem system, Ship station)
            : this(owner)
        {
            StarDateAdded = owner.Universe.StarDate;
            TargetSystem = system;
            TargetShip = station;
            Owner = owner;
        }

        public ResearchStation(Empire owner, Planet planet, Ship station)
            : this(owner)
        {
            StarDateAdded = owner.Universe.StarDate;
            TargetSystem = planet.ParentSystem;
            TargetPlanet = planet;
            TargetShip = station;
            Owner = owner;
        }

        GoalStep Research()
        {
            if (ResearchStationOrbital == null || !ResearchStationOrbital.Active)
                return GoalStep.GoalFailed;

            AddResearch();

            return GoalStep.TryAgain;
        }

        void AddResearch()
        {
            float researchToAdd = ResearchStationOrbital.GetProduction() / GlobalStats.Defaults.ResearchStationProductionPerResearch;
            Owner.Research.AddResearchStationResearchPerTurn(researchToAdd);
        }
    }
}
