using System;
using SDGraphics;
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
        float ProductionPerResearch => GlobalStats.Defaults.ResearchStationProductionPerResearch;

        public override bool IsResearchStationGoal(Planet planet) => TargetPlanet == planet;

        public override bool IsResearchStationGoal(SolarSystem system) => TargetSystem == system && TargetPlanet == null;

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

            // TODO handle boarding

            // do to handle multiple stations on the same planet/system

            float production = ResearchStationOrbital.GetProduction();
            if (ResearchStationOrbital.CargoSpaceFree > Owner.AverageFreighterCargoCap 
                || production / ResearchStationOrbital.CargoSpaceMax < 0.5f)
            {
                // todo - check if supply goal to this staion exists
                // Todo - create supply cargo goal
            }

            AddResearch(production);
            return GoalStep.TryAgain;
        }

        void AddResearch(float production)
        {
            if (production <= 0) 
                return;

            float upperbound = production / ProductionPerResearch;
            float researchToAdd = ResearchStationOrbital.ResearchPerTurn.UpperBound(upperbound);
            ResearchStationOrbital.UnloadProduction(researchToAdd * ProductionPerResearch);
            Owner.Research.AddResearchStationResearchPerTurn(researchToAdd);
        }
    }
}
